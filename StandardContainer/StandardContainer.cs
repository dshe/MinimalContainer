/*
StandardContainer.cs 0.1.*
Copyright 2016 dshe
Licensed under the Apache License 2.0: http://www.apache.org/licenses/LICENSE-2.0
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace StandardContainer
{
    public enum Lifestyle { Transient, Singleton, Instance, Factory };
    public enum DefaultLifestyle { Transient, Singleton, None };

    public sealed class Registration
    {
        public Lifestyle Lifestyle;
        public TypeInfo Type, TypeConcrete;
        public object Instance;
        public Func<object> Factory;
        internal Expression Expression;
        public int Count;
        public override string ToString() =>
            $"{(TypeConcrete == null || Equals(TypeConcrete, Type) ? "" : TypeConcrete.AsString() + "->")}{Type.AsString()}, {Lifestyle}({Count})";
    }

    public sealed class Container : IDisposable
    {
        private readonly DefaultLifestyle defaultLifestyle;
        private readonly List<TypeInfo> allTypesConcrete;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        public IList<Registration> GetRegistrations() => registrations.Values.OrderBy(r => r.Type.Name).ToList();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(DefaultLifestyle defaultLifestyle = DefaultLifestyle.None, Action<string> log = null, params Assembly[] assemblies)
        {
            this.defaultLifestyle = defaultLifestyle;
            this.log = log;
            Log("Creating Container.");
            var assemblyList = assemblies.ToList();
            if (!assemblyList.Any())
                assemblyList.Add((Assembly) typeof(Assembly)
                    .GetTypeInfo()
                    .GetDeclaredMethod("GetCallingAssembly")
                    .Invoke(null, new object[0]));
            allTypesConcrete = assemblyList
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract).ToList())
                .SelectMany(x => x)
                .ToList();
            RegisterInstance(this); // container self-registration
        }

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type concreteType = null) => Register(Lifestyle.Transient, type, concreteType);

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type concreteType = null) => Register(Lifestyle.Singleton, type, concreteType);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return Register(Lifestyle.Instance, type, instance.GetType(), instance);
        }

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return Register(Lifestyle.Factory, type, null, null, factory);
        }

        private Container Register(Lifestyle lifestyle, Type type, Type concreteType, object instance = null,
            Func<object> factory = null, [CallerMemberName] string caller = null)
        {
            AddRegistration(lifestyle, type?.GetTypeInfo(), concreteType?.GetTypeInfo(), instance, factory, caller);
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(Lifestyle lifestyle, TypeInfo type, TypeInfo concreteType,
            object instance, Func<object> factory, string caller)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (concreteType == null && lifestyle != Lifestyle.Instance && lifestyle != Lifestyle.Factory)
                concreteType = allTypesConcrete.FindConcreteType(type);
            if (concreteType != null)
            {
                if (!type.IsAssignableFrom(concreteType))
                    throw new TypeAccessException($"Type {concreteType.AsString()} is not assignable to type {type.AsString()}.");
                if (concreteType.IsValueType)
                    throw new TypeAccessException("Cannot register value type.");
                if (typeof(string).GetTypeInfo().IsAssignableFrom(concreteType))
                    throw new TypeAccessException("Cannot register type string.");
            }
            lock (registrations)
                return AddRegistrationCore(lifestyle, type, concreteType, instance, factory, caller);
        }

        private Registration AddRegistrationCore(Lifestyle lifestyle, TypeInfo type, TypeInfo concreteType,
            object instance, Func<object> factory, string caller)
        {
            var reg = new Registration
            {
                Lifestyle = lifestyle,
                Type = type,
                TypeConcrete = concreteType,
                Instance = instance,
                Factory = factory
            };
            if (reg.Instance != null)
            {
                reg.Expression = Expression.Constant(reg.Instance);
                reg.Count = 1;
            }
            if (reg.Factory != null)
            {
                Expression<Func<object>> expression = () => factory();
                reg.Expression = expression;
                reg.Factory = expression.Compile();
            }
            Log(() => $"{caller}: {reg}");
            try
            {
                registrations.Add(type.AsType(), reg);
            }
            catch (ArgumentException ex)
            {
                throw new TypeAccessException($"Type {type.AsString()} is already registered.", ex);
            }
            return reg;
        }

        //////////////////////////////////////////////////////////////////////////////

        public T GetInstance<T>() => (T)GetInstance(typeof(T));
        public object GetInstance(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            lock (registrations)
            {
                try
                {
                    var reg = GetRegistration(type, null);
                    return reg.Instance ?? reg.Factory();
                }
                catch (TypeAccessException ex)
                {
                    if (!typeStack.Any())
                        throw new TypeAccessException($"Could not get instance of type {type}. {ex.Message}", ex);
                    var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                    throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
                }
            }
        }

        private Registration GetRegistration(Type type, Registration dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(type, out reg))
            {
                if (defaultLifestyle == DefaultLifestyle.None)
                    throw new TypeAccessException($"Cannot resolve unregistered type {type.AsString()}.");
                var style = (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance || defaultLifestyle == DefaultLifestyle.Singleton)
                    ? Lifestyle.Singleton : Lifestyle.Transient;
                reg = AddRegistration(style, type.GetTypeInfo(), null, null, null, "Auto-registration");
            }
            if (reg.Instance == null && reg.Factory == null)
                Initialize(reg, dependent);
            reg.Count = reg.Instance == null ? reg.Count + 1 : 1;
            return reg;
        }

        private void Initialize(Registration reg, Registration dependent)
        {
            if (dependent == null)
            {
                typeStack.Clear();
                Log(() => $"Getting instance of type: {reg.Type.AsString()}.");
            }
            typeStack.Push(reg.Type);
            if (typeStack.Count(t => t.Equals(reg.Type)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton {dependent.Type.AsString()} depends on transient {reg.Type.AsString()}.");

            reg.Expression = GetExpression(reg);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
            if (reg.Lifestyle == Lifestyle.Transient)
                return;
            reg.Instance = reg.Factory();
            reg.Expression = Expression.Constant(reg.Instance);
            reg.Factory = null;
            typeStack.Pop();
        }

        private Expression GetExpression(Registration reg)
        {
            // For singleton registrations, use a previously registered singleton instance, if any.
            if (reg.Lifestyle == Lifestyle.Singleton)
            {
                var expression = registrations.Values.Where(r =>
                        Equals(r.TypeConcrete, reg.TypeConcrete) &&
                        r.Lifestyle == Lifestyle.Singleton &&
                        r.Expression != null)
                    .Select(r => r.Expression)
                    .SingleOrDefault();
                if (expression != null)
                    return expression;
            }
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(reg.Type))
                return GetExpressionArray(reg);
            return GetExpressionNew(reg);
        }

        private Expression GetExpressionNew(Registration reg)
        {
            var type = reg.TypeConcrete;
            var ctor = type.GetConstructor();
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: {type.AsString()}({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")}).");
            return Expression.New(ctor, parameters);
        }

        private Expression GetExpressionArray(Registration reg)
        {
            var genericType = reg.TypeConcrete.GenericTypeArguments.Single().GetTypeInfo();
            var expressions = allTypesConcrete
                .Where(t => genericType.IsAssignableFrom(t))
                .Select(x => GetRegistration(x.AsType(), reg).Expression)
                .ToList();
            if (!expressions.Any())
                throw new TypeAccessException($"No types found assignable to generic type {genericType.AsString()}.");
            Log(() => $"Creating list of {expressions.Count} types assignable to {genericType.AsString()}.");
            return Expression.NewArrayInit(genericType.AsType(), expressions);
        }

        //////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            var reg = GetRegistrations();
            return new StringBuilder()
                .AppendLine($"Container: {defaultLifestyle}, {reg.Count} registered types:")
                .AppendLine(reg.Select(x => x.ToString()).JoinStrings(Environment.NewLine))
                .ToString();
        }

        public void Log() => Log(ToString());
        private void Log(string message) => Log(() => message);
        private void Log(Func<string> message)
        {
            if (log == null)
                return;
            var msg = message?.Invoke();
            if (!string.IsNullOrEmpty(msg))
                log(msg);
        }

        /// <summary>
        /// Disposing the container disposes any registered disposable singleton instances.
        /// </summary>
        public void Dispose()
        {
            lock (registrations)
            {
                foreach (var instance in GetRegistrations()
                    .Select(r => r.Instance)
                    .Where(i => i != null && i != this)
                    .OfType<IDisposable>())
                {
                    Log($"Disposing type {instance.GetType().AsString()}.");
                    instance.Dispose();
                }
                registrations.Clear();
            }
            Log("Container disposed.");
        }
    }

    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructorAttribute : Attribute { }

    internal static class StandardContainerEx
    {
        /// When a non-concrete type is indicated (register or get instance), the concrete type is determined automatically.
        /// In this case, the non-concrete type must be assignable to exactly one concrete type.
        internal static TypeInfo FindConcreteType(this List<TypeInfo> concreteTypes, TypeInfo type)
        {
            if (!type.IsAbstract || typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type))
                return type;
            var assignableTypes = concreteTypes.Where(type.IsAssignableFrom).ToList(); // slow
            if (assignableTypes.Count != 1)
                throw new TypeAccessException($"{assignableTypes.Count} types found assignable to {type.AsString()}.");
            return assignableTypes.Single();
        }

        internal static ConstructorInfo GetConstructor(this TypeInfo type)
        {
            var ctors = type.DeclaredConstructors.Where(c => !c.IsPrivate).ToList();
            if (ctors.Count == 1)
                return ctors.Single();
            if (!ctors.Any())
                throw new TypeAccessException($"Type {type.AsString()} has no public or internal constructor.");
            var ctorsWithAttribute = ctors.Where(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null).ToList();
            if (ctorsWithAttribute.Count == 1)
                return ctorsWithAttribute.Single();
            if (ctorsWithAttribute.Count > 1)
                throw new TypeAccessException($"Type {type.AsString()} has more than one constructor decorated with {nameof(ContainerConstructorAttribute)}.");
            return ctors.OrderBy(c => c.GetParameters().Length).First();
        }

        internal static string JoinStrings(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
        internal static string AsString(this Type type) => type.GetTypeInfo().AsString();
        internal static string AsString(this TypeInfo type)
        {
            var name = type.Name;
            if (type.IsGenericParameter || !type.IsGenericType)
                return name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                name = name.Substring(0, index);
            var args = type.GenericTypeArguments
                .Select(a => a.GetTypeInfo().AsString())
                .JoinStrings(",");
            return $"{name}<{args}>";
        }
    }
}
