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
using System.Runtime.CompilerServices;
using System.Text;

namespace StandardContainer
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructorAttribute : Attribute { }

    public sealed class Container : IDisposable
    {
        public enum Lifestyle { AutoRegisterDisabled, Singleton, Transient };
        public enum Origin { Type, Instance, Factory };

        public sealed class Registration
        {
            public Lifestyle Lifestyle;
            public Origin Origin;
            public TypeInfo Type;
            public TypeInfo ConcreteType;
            public object Instance;
            public Func<object> Factory;
            internal Expression Expression;
            public override string ToString() =>
                $"'{(Equals(ConcreteType, null) || Equals(ConcreteType, Type) ? "" : ConcreteType.AsString() + "->")}"
                + $"{Type.AsString()}', {Lifestyle}.";
        }

        private readonly Lifestyle autoLifestyle;
        private readonly List<TypeInfo> concreteTypes;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, Action<string> log = null, params Assembly[] assemblies)
        {
            this.autoLifestyle = autoLifestyle;
            this.log = log;
            Log("Creating Container.");
            concreteTypes = (assemblies.Any() ? assemblies : new[] {this.GetType().GetTypeInfo().Assembly})
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract).ToList())
                .SelectMany(x => x)
                .ToList();
            RegisterInstance(this); // container self-register
        }

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T 
            => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type concreteType = null)
            => Register(Lifestyle.Singleton, Origin.Type, type, concreteType);
          
        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T 
            => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type concreteType = null)
            => Register(Lifestyle.Transient, Origin.Type, type, concreteType);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return Register(Lifestyle.Singleton, Origin.Instance, type, instance.GetType(), instance);
        }

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return Register(Lifestyle.Transient, Origin.Factory, type, null, null, factory);
        }

        private Container Register(Lifestyle lifestyle, Origin origin, Type type, Type concreteType, object instance = null,
            Func<object> factory = null, [CallerMemberName] string caller = null)
        {
            AddRegistration(lifestyle, origin, type?.GetTypeInfo(), concreteType?.GetTypeInfo(), instance, factory, caller);
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(Lifestyle lifestyle, Origin origin, TypeInfo type, TypeInfo concreteType,
            object instance, Func<object> factory, string caller)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (concreteType == null && origin == Origin.Type)
                concreteType = concreteTypes.FindConcreteType(type);
            if (concreteType != null)
            {
                if (!type.IsAssignableFrom(concreteType))
                    throw new TypeAccessException(
                        $"Type '{concreteType.AsString()}' is not assignable to type '{type.AsString()}'.");
                if (concreteType.IsValueType)
                    throw new TypeAccessException("Cannot register value type.");
                if (typeof(string).GetTypeInfo().IsAssignableFrom(concreteType))
                    throw new TypeAccessException("Cannot register type 'string'.");
            }
            lock (registrations)
                return AddRegistration2(lifestyle, origin, type, concreteType, instance, factory, caller);
        }

        private Registration AddRegistration2(Lifestyle lifestyle, Origin origin, TypeInfo type, TypeInfo concreteType,
            object instance, Func<object> factory, string caller)
        {
            var reg = new Registration
            {
                Lifestyle = lifestyle,
                Origin = origin,
                Type = type,
                ConcreteType = concreteType,
                Instance = instance,
                Factory = factory
            };
            if (reg.Instance != null)
                reg.Expression = Expression.Constant(instance);
            else if (reg.Factory != null)
            {
                Expression<Func<object>> expression = () => factory();
                reg.Expression = expression;
            }
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
                        throw new TypeAccessException($"Could not get instance of type '{type}'. {ex.Message}", ex);
                    var typePath = typeStack.Select(t => t.AsString()).JoinString("->");
                    throw new TypeAccessException($"Could not get instance of type '{typePath}'. {ex.Message}", ex);
                }
            }
        }

        private Registration GetRegistration(Type type, Registration dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(type, out reg))
            {
                if (autoLifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{type.AsString()}'.");
                var lifestyle = dependent?.Lifestyle == Lifestyle.Singleton ? Lifestyle.Singleton : autoLifestyle;
                reg = AddRegistration(lifestyle, Origin.Type, type.GetTypeInfo(), null, null, null, "Auto-registration");
            }
            if (reg.Instance == null && reg.Factory == null)
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
            Initialize(reg);
            typeStack.Pop();
        }

        private void Initialize(Registration reg)
        {
            if (reg.Lifestyle == Lifestyle.Singleton && reg.Origin == Origin.Type)
            {
                var previousReg = registrations.Values.SingleOrDefault(r =>
                    Equals(r.ConcreteType, reg.ConcreteType) &&
                    r.Origin == Origin.Type &&
                    r.Lifestyle == Lifestyle.Singleton &&
                    r.Instance != null);
                if (previousReg != null)
                {
                    reg.Instance = previousReg.Instance;
                    return;
                }
            }
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(reg.Type))
                SetExpressionArray(reg);
            else
                SetExpressionNew(reg);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
            if (reg.Lifestyle == Lifestyle.Transient)
                return;
            reg.Instance = reg.Factory();
            reg.Expression = Expression.Constant(reg.Instance);
            reg.Factory = null;
        }

        private void SetExpressionNew(Registration reg)
        {
            var type = reg.ConcreteType;
            var ctor = type.GetConstructor();
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: '{type.AsString()}({parameters.Select(p => p.Type.AsString()).JoinString(", ")})'.");
            reg.Expression = Expression.New(ctor, parameters);
        }

        private void SetExpressionArray(Registration reg)
        {
            var genericType = reg.ConcreteType.GenericTypeArguments.Single().GetTypeInfo();
            var expressions = concreteTypes
                .Where(t => genericType.IsAssignableFrom(t))
                .Select(x => GetRegistration(x.AsType(), reg).Expression)
                .ToList();
            if (!expressions.Any())
                throw new TypeAccessException($"No types found assignable to generic type '{genericType.AsString()}'.");
            Log(() => $"Creating list of {expressions.Count} types assignable to '{genericType.AsString()}'.");
            reg.Expression = Expression.NewArrayInit(genericType.AsType(), expressions);
        }

        //////////////////////////////////////////////////////////////////////////////

        public List<Registration> GetRegistrations()
        {
            lock (registrations)
                return registrations.Values.OrderBy(r => r.Type.Name).ToList();
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

        public override string ToString()
        {
            var reg = GetRegistrations();
            return new StringBuilder()
                .AppendLine($"Container: {autoLifestyle}, {reg.Count} registered types:")
                .AppendLine(reg.Select(x => x.ToString()).JoinString(Environment.NewLine))
                .ToString();
        }

        public void Dispose()
        {
            lock (registrations)
            {
                foreach (var instance in GetRegistrations()
                    .Where(r => r.Lifestyle.Equals(Lifestyle.Singleton) && r.Instance != null && r.Instance != this)
                    .Select(r => r.Instance).OfType<IDisposable>())
                {
                    Log($"Disposing type '{instance.GetType().AsString()}'.");
                    instance.Dispose();
                }
                registrations.Clear();
            }
            Log("Container disposed.");
        }
    }

    internal static class StandardContainerExtensions
    {
        internal static string JoinString(this IEnumerable<string> strings, string separator) =>
            string.Join(separator, strings);
        internal static string AsString(this Type type) =>
            type.GetTypeInfo().AsString();
        internal static string AsString(this TypeInfo type)
        {
            if (type == null)
                return null;
            var name = type.Name;
            if (type.IsGenericParameter || !type.IsGenericType)
                return name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                name = name.Substring(0, index);
            var args = type.GenericTypeArguments
                .Select(a => a.GetTypeInfo().AsString())
                .JoinString(",");
            return $"{name}<{args}>";
        }

        internal static ConstructorInfo GetConstructor(this TypeInfo type)
        {
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

        internal static TypeInfo FindConcreteType(this List<TypeInfo> concreteTypes, TypeInfo type)
        {
            if (!type.IsAbstract || type.IsGenericType || typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type))
                return type;
            var assignableTypes = concreteTypes.Where(type.IsAssignableFrom).ToList(); // slow
            if (assignableTypes.Count != 1)
                throw new TypeAccessException($"{assignableTypes.Count} types found assignable to '{type.AsString()}'.");
            return assignableTypes.Single();
        }

    }

}
