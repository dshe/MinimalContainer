//InternalContainer.cs 1.06
//Copyright 2016 David Shepherd. Licensed under the Apache License 2.0: http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace InternalContainer2
{
    internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

    internal sealed class Registration2
    {
        internal readonly TypeInfo SuperType, ConcreteType;
        internal readonly Lifestyle Lifestyle;
        internal Func<object> Factory;
        internal bool FactoryProvided;
        //internal Expression expression;

        internal int Instances;
        public Registration2(TypeInfo superType, TypeInfo concreteType, Lifestyle lifestyle)
        {
            SuperType = superType;
            ConcreteType = concreteType;
            Lifestyle = lifestyle;
        }
        public override string ToString()
        {
            return $"'{(Equals(ConcreteType, null) || Equals(ConcreteType, SuperType) ? "" : ConcreteType.AsString() + "->")}" +
                   $"{SuperType.AsString()}', {Lifestyle}, InstancesCreated={Instances}.";
        }
    }

    internal sealed class Container2 : IDisposable
    {
        private readonly Lifestyle autoLifestyle;
        private readonly Lazy<List<TypeInfo>> allConcreteTypes;
        private readonly Dictionary<TypeInfo, Registration> registrations = new Dictionary<TypeInfo, Registration>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container2(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, Action<string> log = null, params Assembly[] assemblies)
        {
            this.autoLifestyle = autoLifestyle;
            this.log = log;
            Log("Creating Container.");
            allConcreteTypes = new Lazy<List<TypeInfo>>(() =>
                (assemblies.Any() ? assemblies : new[] { this.GetType().GetTypeInfo().Assembly })
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract).ToList())
                .SelectMany(x => x).ToList());
        }

        public void RegisterSingleton<T>() => Register(typeof(T).GetTypeInfo(), null, Lifestyle.Singleton);
        public void RegisterTransient<T>() => Register(typeof(T).GetTypeInfo(), null, Lifestyle.Transient);
        public void RegisterSingleton<TSuper, TConcrete>() where TConcrete : TSuper =>
            Register(typeof(TSuper).GetTypeInfo(), typeof(TConcrete).GetTypeInfo(), Lifestyle.Singleton);
        public void RegisterTransient<TSuper, TConcrete>() where TConcrete : TSuper =>
            Register(typeof(TSuper).GetTypeInfo(), typeof(TConcrete).GetTypeInfo(), Lifestyle.Transient);
        public void Register(TypeInfo superType, TypeInfo concreteType, Lifestyle lifestyle)
        {
            if (superType == null)
                throw new ArgumentNullException(nameof(superType));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            concreteType = concreteType ?? FindConcreteType(superType);
            var reg = new Registration(superType, concreteType, lifestyle);
            Register(reg, "Registering type");
        }

        public void RegisterInstance<TSuper>(TSuper instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var reg = new Registration(typeof(TSuper).GetTypeInfo(), instance.GetType().GetTypeInfo(),
                Lifestyle.Singleton)
            { Factory = () => instance };
            Register(reg, "Registering instance of type");
        }

        public void RegisterFactory<TSuper>(Func<TSuper> factory) where TSuper : class =>
            RegisterFactory(typeof(TSuper).GetTypeInfo(), factory);
        internal void RegisterFactory(TypeInfo superType, Func<object> factory) // used for testing performance
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var reg = new Registration(superType, null, Lifestyle.Transient) { Factory = factory, FactoryProvided = true; };
            Register(reg, "Registering type factory");
        }

        private void Register(Registration registration, string message)
        {
            if (registration.ConcreteType != null && !registration.SuperType.IsAssignableFrom(registration.ConcreteType))
                throw new TypeAccessException($"Type '{registration.ConcreteType.AsString()}' is not assignable to type '{registration.SuperType.AsString()}'.");
            lock (registrations)
            {
                Registration reg;
                if (registrations.TryGetValue(registration.SuperType, out reg))
                    throw new TypeAccessException($"Type '{registration.SuperType.AsString()}' is already registered: {reg}");
                AddRegistration(registration, message);
                // reverse this, to catch exception
            }
        }

        private void AddRegistration(Registration registration, string message)
        {
            if (registration.ConcreteType != null && !Equals(registration.ConcreteType, registration.SuperType))
            {
                Registration reg;
                if (registrations.TryGetValue(registration.ConcreteType, out reg))
                    throw new TypeAccessException($"Type '{reg.SuperType.AsString()}' is already registered to return concrete type: {reg}");
            }
            Log(() => $"{message}: {registration}");
            registrations.Add(registration.SuperType, registration);
            if (registration.ConcreteType != null && !registration.SuperType.Equals(registration.ConcreteType))
                registrations.Add(registration.ConcreteType, registration);
        }

        //////////////////////////////////////////////////////////////////////////////

        public TSuper GetInstance1<TSuper>()
        {
            var superType = typeof(TSuper).GetTypeInfo();
            lock (registrations)
            {
                Registration registration;
                if (!registrations.TryGetValue(superType, out registration))
                {
                    registration.
                }


                    return (TSuper)registration.Factory();

            }
        }

        public TSuper GetInstance<TSuper>()
        {
            var supertype = typeof (TSuper);
            lock (registrations)
            {
                Log(() => $"Getting instance of type: '{supertype.AsString()}'.");
                typeStack.Clear();
                try
                {
                    var instance = GetInstance(supertype.GetTypeInfo());
                    Debug.Assert(!typeStack.Any());
                    return (TSuper) instance;
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

        private object GetInstance(TypeInfo superType, Registration dependent = null)
        {
            typeStack.Push(superType);
            if (typeStack.Count(t => t.Equals(superType)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            var instance = GetInstanceInternal(superType, dependent);
            typeStack.Pop();
            return instance;
        }

        private object GetInstanceInternal(TypeInfo superType, Registration dependent)
        {
            Registration registration;
            if (!registrations.TryGetValue(superType, out registration))
            {
                if (autoLifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{superType.AsString()}'.");
                var lifestyle = dependent?.Lifestyle == Lifestyle.Singleton ? Lifestyle.Singleton : autoLifestyle;
                var concreteType = FindConcreteType(superType);
                registration = new Registration(superType, concreteType, lifestyle);
                AddRegistration(registration, "Registering type");
            }
            //if (registration.Factory != null)
            //    return registration.Factory();

            return GetInstanceFromRegistration(registration, dependent);
        }

        private object GetInstanceFromRegistration(Registration reg, Registration dependent)
        {
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException(
                    $"Captive dependency: the singleton '{dependent.SuperType.AsString()}' depends on transient '{reg.SuperType.AsString()}'.");
            if (reg.Factory == null)
            {
                if (reg.Lifestyle == Lifestyle.Singleton)
                {
                    var value = CreateInstanceOrList(reg);
                    reg.Factory = () => value;
                }
                else
                    reg.Factory = () => CreateInstanceOrList(reg);
            }
            return reg.Factory();
        }

        private Expression CreateInstanceOrList(Registration reg)
        {
            //if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(reg.SuperType))
            //    return CreateList(reg);
            return GetNewInstanceExpression(reg);
        }


        public Expression GetNewInstanceExpression(Registration reg)
        {
            var type = reg.ConcreteType;
            var ctor = type.DeclaredConstructors.Where(t => !t.IsPrivate).OrderBy(t => t.GetParameters().Length).LastOrDefault();
            if (ctor == null)
                throw new TypeAccessException($"Type '{type.AsString()}' has no public or internal constructor.");
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue) : GetNewInstanceExpression(p.ParameterType.GetTypeInfo(), reg))
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: '{type.AsString()}({string.Join(", ", parameters.Select(p => p.GetType().AsString()))})'.");
            return Expression.New(ctor, parameters);
        }




        private object CreateList(Registration reg)
        {
            var genericType = reg.ConcreteType.GenericTypeArguments.Single().GetTypeInfo();
            var genericList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericType.AsType()));
            var assignables = allConcreteTypes.Value.Where(t => genericType.IsAssignableFrom(t)).ToList();
            if (!assignables.Any())
                throw new TypeAccessException($"No types found assignable to generic type '{genericType.AsString()}'.");
            Log(() => $"Creating list of {assignables.Count} types assignable to '{genericType.AsString()}'.");
            foreach (var assignable in assignables)
                genericList.Add(GetInstance(assignable, reg));
            return genericList;
        }

        private TypeInfo FindConcreteType(TypeInfo superType)
        {
            if (!superType.IsAbstract || superType.IsGenericType || typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(superType))
                return superType;
            var types = allConcreteTypes.Value.Where(superType.IsAssignableFrom).ToList();
            if (types.Count == 1)
                return types.Single();
            throw new TypeAccessException($"{types.Count} types found assignable to '{superType.AsString()}'.");
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

}
