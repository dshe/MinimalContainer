// StandardContainer.cs 1.26.3.0
// Copyright 2016 dshe
// License: http://www.apache.org/licenses/LICENSE-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace StandardContainer
{
    internal enum Lifestyle { Transient, Singleton, Instance, Factory };
    internal enum DefaultLifestyle { Transient, Singleton, None };

    internal sealed class Registration
    {
        internal Lifestyle Lifestyle;
        internal TypeInfo Type, TypeConcrete;
        internal Func<object> Factory;
        internal Expression Expression;
        public override string ToString() =>
            $"{(TypeConcrete == null || Equals(TypeConcrete, Type) ? "" : TypeConcrete.AsString() + "->")}{Type.AsString()}, {Lifestyle}.";
    }

    internal sealed class Container : IDisposable
    {
        private readonly DefaultLifestyle defaultLifestyle;
        private readonly List<TypeInfo> allTypesConcrete;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        internal Container(DefaultLifestyle defaultLifestyle = DefaultLifestyle.None, Action<string> log = null, params Assembly[] assemblies)
        {
            this.defaultLifestyle = defaultLifestyle;
            this.log = log;
            Log("Creating Container.");
            var assemblyList = assemblies.ToList();
            if (!assemblyList.Any())
            {
                var method = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                if (method == null)
                    throw new ArgumentException("Since calling assembly cannot be determined, one or more assemblies must be indicated when constructing the container.");
                assemblyList.Add((Assembly)method.Invoke(null, new object[0]));
            }
            allTypesConcrete = assemblyList
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface).ToList())
                .SelectMany(x => x)
                .ToList();
            RegisterInstance(this); // container self-registration
        }

        internal Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        internal Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        internal Container RegisterTransient(Type type, Type typeConcrete = null) => Register(Lifestyle.Transient, type, typeConcrete);

        internal Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        internal Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        internal Container RegisterSingleton(Type type, Type typeConcrete = null) => Register(Lifestyle.Singleton, type, typeConcrete);

        internal Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        internal Container RegisterInstance(Type type, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return Register(Lifestyle.Instance, type, instance.GetType(), () => instance);
        }

        internal Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        internal Container RegisterFactory(Type type, Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return Register(Lifestyle.Factory, type, null, factory);
        }

        private Container Register(Lifestyle lifestyle, Type type, Type typeConcrete,
            Func<object> factory = null, [CallerMemberName] string caller = null)
        {
            AddRegistration(lifestyle, type?.GetTypeInfo(), typeConcrete?.GetTypeInfo(), factory, caller);
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(Lifestyle lifestyle, TypeInfo type, TypeInfo typeConcrete, Func<object> factory, string caller)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.IsFunc())
                throw new ArgumentException("Register Func<T> with RegisterFactory().");
            if (typeConcrete == null && lifestyle != Lifestyle.Instance && lifestyle != Lifestyle.Factory)
                typeConcrete = allTypesConcrete.FindTypeConcrete(type);
            if (typeConcrete != null)
            {
                if (!type.IsAssignableFrom(typeConcrete))
                    throw new TypeAccessException($"Type '{typeConcrete.AsString()}' is not assignable to type '{type.AsString()}'.");
                if (typeConcrete.IsValueType)
                    throw new TypeAccessException("Cannot register value type.");
                if (typeof(string).GetTypeInfo().IsAssignableFrom(typeConcrete))
                    throw new TypeAccessException("Cannot register type 'string'.");
            }
            var reg = new Registration
            {
                Lifestyle = lifestyle, Type = type, TypeConcrete = typeConcrete, Factory = factory
            };
            Log(() => $"{caller}: {reg}");
            try
            {
                 registrations.Add(type.AsType(), reg);
            }
            catch (ArgumentException ex)
            {
                throw new TypeAccessException($"Type '{type.AsString()}' is already registered.", ex);
            }
            return reg;
        }

        //////////////////////////////////////////////////////////////////////////////

        internal T Resolve<T>() => (T)Resolve(typeof(T));
        internal object Resolve(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            try
            {
                return GetRegistration(type, null).Factory();
            }
            catch (TypeAccessException ex)
            {
                if (!typeStack.Any())
                    throw new TypeAccessException($"Could not get instance of type '{type.AsString()}'. {ex.Message}", ex);
                var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
            }
        }

        private Registration GetRegistration(Type type, Registration dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(type, out reg))
            {
                if (type.GetTypeInfo().IsFunc())
                {
                    var generic = type.GenericTypeArguments.Single();
                    var regDependent = new Registration { Lifestyle = Lifestyle.Factory };
                    var genericReg = GetRegistration(generic, regDependent);
                    reg = new Registration { Lifestyle = genericReg.Lifestyle };
                    if (genericReg.Lifestyle == Lifestyle.Transient)
                        reg.Expression = Expression.Lambda(genericReg.Expression);
                    else if (genericReg.Lifestyle == Lifestyle.Factory)
                        reg.Expression = Expression.Constant(genericReg.Factory);
                    else
                        throw new TypeAccessException($"Type from factory '{type.AsString()}' is an instance or singleton.");
                    reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
                    return reg;
                }
                if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != typeof(string))
                {
                    var genericType = type.GenericTypeArguments.Single().GetTypeInfo();
                    if (defaultLifestyle != DefaultLifestyle.None)
                    {
                        foreach (var t in allTypesConcrete.Where(t => genericType.IsAssignableFrom(t)))
                            GetRegistration(t.AsType(), dependent);
                    }
                    var expressions = registrations.Values
                        .Select(x => x.Type)
                        .Where(t => genericType.IsAssignableFrom(t))
                        .Select(t => GetRegistration(t.AsType(), dependent))
                        .Select(r => r.Expression)
                        .ToList();
                    if (!expressions.Any())
                        throw new TypeAccessException($"No types found assignable to generic type '{genericType.AsString()}'.");
                    Log($"Creating list of {expressions.Count} types assignable to '{genericType.AsString()}'.");
                    reg = new Registration
                    {
                        Lifestyle = Lifestyle.Transient,
                        Expression = Expression.NewArrayInit(genericType.AsType(), expressions)
                    };
                    reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
                    return reg;
                }
                if (defaultLifestyle == DefaultLifestyle.None)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{type.AsString()}'.");
                var style = dependent?.Lifestyle != Lifestyle.Factory &&
                            (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance ||
                             defaultLifestyle == DefaultLifestyle.Singleton) ? Lifestyle.Singleton : Lifestyle.Transient;
                reg = AddRegistration(style, type.GetTypeInfo(), null, null, "Auto-registration");
            }
            if (reg.Expression == null)
                Initialize(reg, dependent);
            return reg;
        }

        private void Initialize(Registration reg, Registration dependent)
        {
            if (dependent == null)
            {
                typeStack.Clear();
                Log(() => $"Getting instance of type: '{reg.Type.AsString()}'.");
            }
            typeStack.Push(reg.Type);
            if (typeStack.Count(t => t.Equals(reg.Type)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent.Type.AsString()}' depends on transient '{reg.Type.AsString()}'.");

            if (reg.Lifestyle == Lifestyle.Instance)
                reg.Expression = Expression.Constant(reg.Factory());
            else if (reg.Lifestyle == Lifestyle.Factory)
            {
                Expression<Func<object>> expression = () => reg.Factory();
                reg.Expression = expression;
            }
            else
            {
                reg.Expression = GetExpression(reg);
                reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
                if (reg.Lifestyle == Lifestyle.Singleton)
                {
                    var instance = reg.Factory();
                    reg.Expression = Expression.Constant(instance);
                    reg.Factory = () => instance;
                }
            }
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
            var type = reg.TypeConcrete;
            var ctor = type.GetConstructor();
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log($"Constructing {reg.Lifestyle} instance: '{type.AsString()}'({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")}).");
            return Expression.New(ctor, parameters);
        }

        //////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            var reg = registrations.Values.ToList();
            return new StringBuilder()
                .AppendLine($"Container: {defaultLifestyle}, {reg.Count} registered types:")
                .AppendLine(reg.Select(x => x.ToString()).JoinStrings(Environment.NewLine))
                .ToString();
        }

        internal void Log() => Log(ToString());
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
        /// Disposing the container disposes any registered disposable instances.
        /// </summary>
        public void Dispose()
        {
            foreach (var instance in registrations.Values
                .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Instance)
                .Select(r => r.Factory())
                .Where(i => i != null && i != this)
                .OfType<IDisposable>())
            {
                Log($"Disposing type '{instance.GetType().AsString()}'.");
                instance.Dispose();
            }
            registrations.Clear();
            Log("Container disposed.");
        }
    }

    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    internal sealed class ContainerConstructorAttribute : Attribute { }

    internal static class StandardContainerEx
    {
        /// When a non-concrete type is indicated (register or get instance), the concrete type is determined automatically.
        /// In this case, the non-concrete type must be assignable to exactly one concrete type.
        internal static TypeInfo FindTypeConcrete(this List<TypeInfo> allTypesConcrete, TypeInfo type)
        {
            if (allTypesConcrete == null)
                throw new ArgumentNullException(nameof(allTypesConcrete));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type) || (!type.IsAbstract && !type.IsInterface))
                return type;
            var assignableTypes = allTypesConcrete.Where(type.IsAssignableFrom).ToList(); // slow
            if (assignableTypes.Count != 1)
                throw new TypeAccessException($"{assignableTypes.Count} types found assignable to '{type.AsString()}'.");
            return assignableTypes.Single();
        }

        internal static ConstructorInfo GetConstructor(this TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            var ctors = type.DeclaredConstructors.Where(c => !c.IsPrivate).ToList();
            if (ctors.Count == 1)
                return ctors.Single();
            if (!ctors.Any())
                throw new TypeAccessException($"Type '{type.AsString()}' has no public or internal constructor.");
            var ctorsWithAttribute = ctors.Where(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null).ToList();
            if (ctorsWithAttribute.Count == 1)
                return ctorsWithAttribute.Single();
            if (ctorsWithAttribute.Count > 1)
                throw new TypeAccessException($"Type '{type.AsString()}' has more than one constructor decorated with '{nameof(ContainerConstructorAttribute)}'.");
            return ctors.OrderBy(c => c.GetParameters().Length).First();
        }

        internal static bool IsFunc(this TypeInfo type) => type.Name == "Func`1";
        internal static string JoinStrings(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
        internal static string AsString(this Type type) => type.GetTypeInfo().AsString();
        internal static string AsString(this TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
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
