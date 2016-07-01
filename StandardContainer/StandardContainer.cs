// StandardContainer.cs 1.25
// Copyright 2016 David Shepherd
// Licensed under the Apache License 2.0: http://www.apache.org/licenses/LICENSE-2.0

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
    internal sealed class ContainerConstructorAttribute : Attribute { }

    internal sealed class Container : IDisposable
    {
        internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

        internal sealed class Registration
        {
            internal readonly TypeInfo SuperType;
            internal TypeInfo ConcreteType;
            internal readonly Lifestyle Lifestyle;
            internal Expression Expression;
            internal Func<object> Factory;
            internal object Instance;

            internal Registration(Type supertype, Type concretetype, Lifestyle lifestyle)
            {
                if (supertype == null)
                    throw new ArgumentNullException(nameof(supertype));
                SuperType = supertype.GetTypeInfo();
                ConcreteType = concretetype?.GetTypeInfo();
                if (lifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new ArgumentException(nameof(lifestyle));
                Lifestyle = lifestyle;
            }

            public override string ToString()
            {
                return $"'{(Equals(ConcreteType, null) || Equals(ConcreteType, SuperType) ? "" : ConcreteType.AsString() + "->")}" +
                       $"{SuperType.AsString()}', {Lifestyle}.";
            }
        }

        private readonly Lifestyle autoLifestyle;
        private readonly Lazy<List<TypeInfo>> allConcreteTypes;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, Action<string> log = null, params Assembly[] assemblies)
        {
            this.autoLifestyle = autoLifestyle;
            this.log = log;
            Log("Creating Container.");
            allConcreteTypes = new Lazy<List<TypeInfo>>(() =>
                (assemblies.Any() ? assemblies : new[] { this.GetType().GetTypeInfo().Assembly })
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract).ToList())
                .SelectMany(x => x).ToList());
            RegisterInstance(this); // container self-register
        }

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<TSuper, TConcrete>() where TConcrete : TSuper =>
            RegisterSingleton(typeof(TSuper), typeof(TConcrete));
        public Container RegisterSingleton(Type supertype, Type concretetype = null) =>
            Register(new Registration(supertype, concretetype, Lifestyle.Singleton));

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<TSuper, TConcrete>() where TConcrete : TSuper =>
            RegisterTransient(typeof(TSuper), typeof(TConcrete));
        public Container RegisterTransient(Type supertype, Type concretetype = null) =>
            Register(new Registration(supertype, concretetype, Lifestyle.Transient));

        public Container RegisterInstance<TSuper>(TSuper instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var reg = new Registration(typeof(TSuper), instance.GetType(), Lifestyle.Singleton)
            { Instance = instance, Expression = Expression.Constant(instance) };
            return Register(reg);
        }

        public Container RegisterFactory<TSuper>(Func<TSuper> factory) where TSuper : class =>
            RegisterFactory(typeof(TSuper), factory);
        internal Container RegisterFactory(Type supertype, Func<object> factory) // used for testing performance
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            Expression<Func<object>> expression = () => factory();
            var reg = new Registration(supertype, null, Lifestyle.Transient) { Factory = factory, Expression = expression };
            return Register(reg);
        }

        private Container Register(Registration reg, [CallerMemberName] string caller = null)
        {
            if (reg.ConcreteType != null && !reg.SuperType.IsAssignableFrom(reg.ConcreteType))
                throw new TypeAccessException($"Type '{reg.ConcreteType.AsString()}' is not assignable to type '{reg.SuperType.AsString()}'.");
            lock (registrations)
            {
                try
                {
                    AddRegistration(reg, caller);
                }
                catch (ArgumentException ex)
                {
                    throw new TypeAccessException($"Type '{reg.SuperType.AsString()}' is already registered.", ex);
                }
            }
            return this;
        }

        private void AddRegistration(Registration reg, string caller)
        {
            if (reg.ConcreteType == null && reg.Factory == null)
                reg.ConcreteType = FindConcreteType(reg.SuperType);

            if (reg.ConcreteType != null)
            {
                if (reg.ConcreteType.IsValueType)
                    throw new TypeAccessException("Cannot register value type.");
                if (typeof(string).GetTypeInfo().IsAssignableFrom(reg.ConcreteType))
                    throw new TypeAccessException("Cannot register type 'string'.");
            }

            Log(() => $"{caller}: {reg}");
            registrations.Add(reg.SuperType.AsType(), reg);
            if (reg.ConcreteType == null || reg.SuperType.Equals(reg.ConcreteType))
                return;
            try
            {
                registrations.Add(reg.ConcreteType.AsType(), reg);
            }
            catch (Exception ex)
            {
                registrations.Remove(reg.SuperType.AsType());
                throw new TypeAccessException($"Type '{reg.ConcreteType.AsString()}' is already registered.", ex);
            }
        }

        private TypeInfo FindConcreteType(TypeInfo superType)
        {
            if (!superType.IsAbstract || superType.IsGenericType || typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(superType))
                return superType;
            var types = allConcreteTypes.Value.Where(superType.IsAssignableFrom).ToList(); // slow
            if (types.Count == 1)
                return types.Single();
            throw new TypeAccessException($"{types.Count} types found assignable to '{superType.AsString()}'.");
        }

        //////////////////////////////////////////////////////////////////////////////

        public TSuper GetInstance<TSuper>() => (TSuper)GetInstance(typeof(TSuper));
        public object GetInstance(Type supertype)
        {
            if (supertype == null)
                throw new ArgumentNullException(nameof(supertype));
            lock (registrations)
            {
                try
                {
                    var reg = GetRegistration(supertype, null);
                    return reg.Instance ?? reg.Factory();
                }
                catch (TypeAccessException ex)
                {
                    if (!typeStack.Any())
                        throw new TypeAccessException($"Could not get instance of type '{supertype}'. {ex.Message}", ex);
                    var typePath = typeStack.Select(t => t.AsString()).JoinString("->");
                    throw new TypeAccessException($"Could not get instance of type '{typePath}'. {ex.Message}", ex);
                }
            }
        }

        private Registration GetRegistration(Type supertype, Registration dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(supertype, out reg))
            {
                if (autoLifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{supertype.AsString()}'.");
                var lifestyle = dependent?.Lifestyle == Lifestyle.Singleton ? Lifestyle.Singleton : autoLifestyle;
                reg = new Registration(supertype, null, lifestyle);
                AddRegistration(reg, "Auto-registration");
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
                Log(() => $"Getting instance of type: '{reg.SuperType.AsString()}'.");
            }
            typeStack.Push(reg.SuperType);
            if (typeStack.Count(t => t.Equals(reg.SuperType)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent.SuperType.AsString()}' depends on transient '{reg.SuperType.AsString()}'.");
            Initialize(reg);
            typeStack.Pop();
        }

        private void Initialize(Registration reg)
        {
            SetExpression(reg);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
            if (reg.Lifestyle == Lifestyle.Transient)
                return;
            reg.Instance = reg.Factory();
            reg.Expression = Expression.Constant(reg.Instance);
            reg.Factory = null;
        }

        private void SetExpression(Registration reg)
        {
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(reg.SuperType))
                SetExpressionArray(reg);
            else
                SetExpressionNew(reg);
        }

        private void SetExpressionNew(Registration reg)
        {
            var type = reg.ConcreteType;
            var ctor = GetConstructor(type);
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: '{type.AsString()}({parameters.Select(p => p.Type.AsString()).JoinString(", ")})'.");
            reg.Expression = Expression.New(ctor, parameters);
        }

        private static ConstructorInfo GetConstructor(TypeInfo type)
        {
            var allCtors = type.DeclaredConstructors.Where(c => !c.IsPrivate).ToList();
            if (allCtors.Count == 1)
                return allCtors[0];
            if (!allCtors.Any())
                throw new TypeAccessException($"Type '{type.AsString()}' has no public or internal constructor.");
            var ctors = allCtors.Where(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null).ToList();
            if (ctors.Count == 1)
                return ctors[0];
            if (ctors.Count > 1)
                throw new TypeAccessException($"Type '{type.AsString()}' has more than one constructor decorated with '{nameof(ContainerConstructorAttribute)}'.");
            return allCtors.OrderBy(c => c.GetParameters().Length).First();
        }

        private void SetExpressionArray(Registration reg)
        {
            var genericType = reg.ConcreteType.GenericTypeArguments.Single().GetTypeInfo();
            var expressions = allConcreteTypes.Value
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
                return registrations.Values.Distinct().OrderBy(r => r.SuperType.Name).ToList();
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

    internal static class StandardContainerExtensionMethods
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
    }

}
