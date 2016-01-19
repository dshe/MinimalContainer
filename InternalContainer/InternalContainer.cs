//InternalContainer.cs 1.07
//Copyright 2016 David Shepherd. Licensed under the Apache License 2.0: http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace InternalContainer
{
    internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

    internal sealed class Registration
    {
        internal TypeInfo SuperType, ConcreteType;
        internal Lifestyle Lifestyle;
        internal Expression Expression;
        internal Func<object> Factory;
        public override string ToString()
        {
            return $"'{(Equals(ConcreteType, null) || Equals(ConcreteType, SuperType) ? "" : ConcreteType.AsString() + "->")}" +
                   $"{SuperType.AsString()}', {Lifestyle}.";
        }
    }

    internal sealed class Container : IDisposable
    {
        private readonly Lifestyle autoLifestyle;
        private readonly Lazy<List<TypeInfo>> allConcreteTypes;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, Action<string> log = null, params Assembly[] assemblies)
        {
            this.autoLifestyle = autoLifestyle;
            this.log = log;
            Log("Creating InternalContainer.");
            allConcreteTypes = new Lazy<List<TypeInfo>>(() =>
                (assemblies.Any() ? assemblies : new[] { this.GetType().GetTypeInfo().Assembly })
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract).ToList())
                .SelectMany(x => x).ToList());
        }

        public void RegisterSingleton<T>() => RegisterClass(typeof(T), null, Lifestyle.Singleton);
        public void RegisterTransient<T>() => RegisterClass(typeof(T), null, Lifestyle.Transient);
        public void RegisterSingleton<TSuper, TConcrete>() where TConcrete : TSuper =>
            RegisterClass(typeof(TSuper), typeof(TConcrete), Lifestyle.Singleton);
        public void RegisterTransient<TSuper, TConcrete>() where TConcrete : TSuper =>
            RegisterClass(typeof(TSuper), typeof(TConcrete), Lifestyle.Transient);
        public void RegisterClass(Type supertype, Type concretetype, Lifestyle lifestyle)
        {
            if (supertype == null)
                throw new ArgumentNullException(nameof(supertype));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            var reg = new Registration { SuperType = supertype.GetTypeInfo(), ConcreteType = concretetype?.GetTypeInfo(), Lifestyle = lifestyle};
            Register(reg, "Registering type");
        }

        public void RegisterInstance<TSuper>(TSuper instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var reg = new Registration { SuperType = typeof (TSuper).GetTypeInfo(), ConcreteType = instance.GetType().GetTypeInfo(),
                Lifestyle = Lifestyle.Singleton, Factory = () => instance, Expression = Expression.Constant(instance) };
            reg.Expression = Expression.Constant(instance);

            Register(reg, "Registering instance of type");
        }

        public void RegisterFactory<TSuper>(Func<TSuper> factory) where TSuper : class =>
            RegisterFactory(typeof(TSuper), factory);
        internal void RegisterFactory(Type supertype, Func<object> factory) // used for testing performance
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var reg = new Registration { SuperType = supertype.GetTypeInfo(), Lifestyle = Lifestyle.Transient, Factory = factory };
            Expression<Func<object>> expression = () => factory();
            reg.Expression = expression;
            Register(reg, "Registering type factory");
        }

        private void Register(Registration registration, string message)
        {
            if (registration.ConcreteType != null && !registration.SuperType.IsAssignableFrom(registration.ConcreteType))
                throw new TypeAccessException($"Type '{registration.ConcreteType.AsString()}' is not assignable to type '{registration.SuperType.AsString()}'.");
            lock (registrations)
            {
                try
                {
                    AddRegistration(registration, message);
                }
                catch (ArgumentException ex)
                {
                    throw new TypeAccessException($"Type '{registration.SuperType.AsString()}' is already registered.", ex);
                }
            }
        }

        private void AddRegistration(Registration registration, string message)
        {
            if (registration.ConcreteType == null && registration.Factory == null)
                registration.ConcreteType = FindConcreteType(registration.SuperType);
            Log(() => $"{message}: {registration}");
            registrations.Add(registration.SuperType.AsType(), registration);
            if (registration.ConcreteType == null || registration.SuperType.Equals(registration.ConcreteType))
                return;
            try
            {
                registrations.Add(registration.ConcreteType.AsType(), registration);
            }
            catch (Exception ex)
            {
                registrations.Remove(registration.SuperType.AsType());
                throw new TypeAccessException($"Type '{registration.ConcreteType.AsString()}' is already registered.", ex);
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
                    return GetRegistration(supertype).Factory();
                }
                catch (TypeAccessException ex)
                {
                    var typePath = string.Join("->", typeStack.Select(t => t.AsString()));
                    var message = $"Could not get instance of type '{typePath}'. {ex.Message}";
                    Log(message + Environment.NewLine);
                    throw new TypeAccessException(message, ex);
                }
            }
        }

        private Registration GetRegistration(Type supertype, Registration dependent = null)
        {
            Registration registration;
            if (!registrations.TryGetValue(supertype, out registration))
            {
                if (autoLifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{supertype.AsString()}'.");
                var lifestyle = dependent?.Lifestyle == Lifestyle.Singleton ? Lifestyle.Singleton : autoLifestyle;
                registration = new Registration { SuperType = supertype.GetTypeInfo(), Lifestyle = lifestyle};
                AddRegistration(registration, "Registering type");
            }
            if (registration.Factory == null)
                CreateFactory(registration, dependent);
            return registration;
        }

        private void CreateFactory(Registration reg, Registration dependent = null)
        {
            if (dependent == null)
            {
                typeStack.Clear();
                Log(() => $"Getting instance of type: '{reg.SuperType.AsString()}'.");
            }

            typeStack.Push(reg.SuperType);
            if (typeStack.Count(t => t.Equals(reg.SuperType)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            SetFactory(reg, dependent);
            typeStack.Pop();
        }

        private void SetFactory(Registration reg, Registration dependent)
        {
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException(
                    $"Captive dependency: the singleton '{dependent.SuperType.AsString()}' depends on transient '{reg.SuperType.AsString()}'.");
            SetExpression(reg);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
            if (reg.Lifestyle == Lifestyle.Singleton)
            {
                var instance = reg.Factory();
                reg.Expression = Expression.Constant(instance);
                reg.Factory = () => instance;
            }
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
            var ctor = type.DeclaredConstructors.Where(t => !t.IsPrivate).OrderBy(t => t.GetParameters().Length).LastOrDefault();
            if (ctor == null)
                throw new TypeAccessException($"Type '{type.AsString()}' has no public or internal constructor.");
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: '{type.AsString()}({string.Join(", ", parameters.Select(p => p.GetType().AsString()))})'.");
            reg.Expression = Expression.New(ctor, parameters);
        }

        private void SetExpressionArray(Registration reg)
        {
            var generictype = reg.ConcreteType.GenericTypeArguments.Single();
            var genericType = generictype.GetTypeInfo();
            var expressions = allConcreteTypes.Value
                .Where(t => genericType.IsAssignableFrom(t))
                .Select(x => GetRegistration(x.AsType(), reg).Expression)
                .ToList();
            if (!expressions.Any())
                throw new TypeAccessException($"No types found assignable to generic type '{genericType.AsString()}'.");
            Log(() => $"Creating list of {expressions.Count} types assignable to '{genericType.AsString()}'.");
            reg.Expression = Expression.NewArrayInit(generictype, expressions);
        }

        //////////////////////////////////////////////////////////////////////////////

        public IList<Registration> Registrations()
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
            var msg = message();
            if (!string.IsNullOrEmpty(msg))
                log(msg);
        }

        public override string ToString()
        {
            return new StringBuilder()
                .AppendLine($"Container: {autoLifestyle}, {Registrations().Count} registered types:")
                .AppendLine(string.Join(Environment.NewLine, Registrations()))
                .ToString();
        }

        public void Dispose()
        {
            lock (registrations)
            {
                Registrations()
                    .Where(r => r.Lifestyle.Equals(Lifestyle.Singleton) && r.Factory != null)
                    .Select(r => r.Factory())
                    .OfType<IDisposable>()
                    .ToList().ForEach(instance =>
                    {
                        Log($"Disposing type '{instance.GetType().AsString()}'.");
                        instance.Dispose();
                    });
                registrations.Clear();
            }
            Log("Container disposed.");
        }
    }

    internal static class InternalContainerExtensionMethods
    {
        public static string AsString(this Type type) => type.GetTypeInfo().AsString();

        public static string AsString(this TypeInfo type)
        {
            if (type == null)
                return null;
            var name = type.Name;
            if (type.IsGenericParameter || !type.IsGenericType)
                return name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                name = name.Substring(0, index);
            var args = type.GenericTypeArguments.Select(a => a.GetTypeInfo().AsString());
            return $"{name}<{string.Join(",", args)}>";
        }
    }
}
