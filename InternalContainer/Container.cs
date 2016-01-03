using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InternalContainer
{
    internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

    internal sealed class Map
    {
        internal TypeInfo SuperTypeInfo, ConcreteTypeInfo;
        internal Func<object> Factory;
        internal Lifestyle Lifestyle;
        internal bool AutoRegistered;
        internal int Instances;
    }

    internal sealed class Container : IDisposable
    {
        private readonly Lifestyle autoLifestyle;
        private readonly List<Map> maps = new List<Map>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly IObserver<string> observer;
        private readonly Lazy<List<TypeInfo>> concreteTypes;

        internal Container(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, IObserver<string> observer = null, Assembly assembly = null)
        {
            this.autoLifestyle = autoLifestyle;
            this.observer = observer;
            Observe("Creating Container.");
            concreteTypes = new Lazy<List<TypeInfo>>(() => (assembly ?? this.GetType().GetTypeInfo().Assembly)
                .DefinedTypes.Where(t => !t.IsAbstract).ToList());
        }

        internal void RegisterSingleton<TConcrete>() => Register(typeof(TConcrete), typeof(TConcrete), null, Lifestyle.Singleton);
        internal void RegisterTransient<TConcrete>() => Register(typeof(TConcrete), typeof(TConcrete), null, Lifestyle.Transient);
        internal void RegisterSingleton<TSuper, TConcrete>() where TConcrete : TSuper => Register(typeof(TSuper), typeof(TConcrete), null, Lifestyle.Singleton);
        internal void RegisterTransient<TSuper, TConcrete>() where TConcrete : TSuper => Register(typeof(TSuper), typeof(TConcrete), null, Lifestyle.Transient);
        internal void RegisterInstance<TSuper>(TSuper instance) => Register(typeof(TSuper), instance?.GetType(), () => instance, Lifestyle.Singleton);
        internal void RegisterFactory<TSuper>(Func<TSuper> factory) where TSuper : class => Register(typeof(TSuper), null, factory, Lifestyle.Transient);
        internal Map Register(Type superType, Type concreteType, Func<object> factory, Lifestyle lifestyle)
        {
            if (superType == null)
                throw new ArgumentNullException(nameof(superType));
            if (concreteType == null && (factory == null || lifestyle == Lifestyle.Singleton))
                throw new ArgumentException("Invalid", nameof(factory));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            TypeInfo superTypeInfo = superType.GetTypeInfo(), concreteTypeInfo = concreteType?.GetTypeInfo();
            if (concreteType != null)
            {
                if (concreteTypeInfo.IsAbstract)
                    throw new ArgumentException($"Type '{concreteType.Name}' is abstract, and must be registered with a concrete type.");
                if (!superTypeInfo.IsAssignableFrom(concreteTypeInfo))
                    throw new ArgumentException($"Type '{concreteType.Name}' is not assignable to type '{superType.Name}'.");
            }
            lock (maps)
            {
                if (factory == null)
                    Observe($"Registering {lifestyle} type '{concreteType.Name}'{(superType == concreteType ? "" : "->'" + superType.Name + "'")}.");
                else if (concreteType != null)
                    Observe($"Registering instance of type {(superType != concreteType ? "'" + concreteType.Name + "'->" : "")}'{superType.Name}'.");
                else
                    Observe($"Registering type '{superType.Name}' factory.");
                if (concreteType != null)
                {
                    var superTypes = maps.Where(m => Equals(m.ConcreteTypeInfo, concreteTypeInfo)).Select(m => m.SuperTypeInfo).Distinct().ToList(); // slow
                    if (superTypes.Any())
                        throw new ArgumentException($"Type(s) '{string.Join(", ", superTypes.Select(t => t.Name))}' already registered to return '{concreteType.Name}'.");
                }
                var map = new Map { SuperTypeInfo = superTypeInfo, ConcreteTypeInfo = concreteTypeInfo, Factory = factory, Lifestyle = lifestyle };
                maps.Add(map);
                return map;
            }
        }

        internal void RegisterSingletonAll<TSuper>() => RegisterAll(typeof(TSuper), Lifestyle.Singleton);
        internal void RegisterTransientAll<TSuper>() => RegisterAll(typeof(TSuper), Lifestyle.Transient);
        internal List<Map> RegisterAll(Type superType, Lifestyle lifestyle)
        {
            if (superType == null)
                throw new ArgumentNullException(nameof(superType));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            lock (maps)
            {
                var assignables = concreteTypes.Value.Where(superType.GetTypeInfo().IsAssignableFrom).ToList();
                if (!assignables.Any())
                    throw new ArgumentException(Observe($"No types found assignable to '{superType.Name}'."));
                Observe($"Registering {assignables.Count} type(s) assignable to '{superType.Name}'.");
                return assignables.Select(t => Register(superType, t.AsType(), null, lifestyle)).ToList();
            }
        }

        internal T GetInstance<T>() => (T)GetInstance(typeof(T));
        internal object GetInstance(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            lock (maps)
            {
                Observe($"Getting instance of type '{type.Name}'.");
                typeStack.Clear();
                try
                {
                    return GetInstance(type.GetTypeInfo());
                }
                catch (TypeAccessException ex)
                {
                    var message = $"Could not get instance of type '{string.Join("->", typeStack.Select(t => t.Name))}'.\n";
                    Observe(message + ex.Message);
                    throw new TypeAccessException(message, ex);
                }
            }
        }

        private object GetInstance(TypeInfo type, Map dependentMap = null)
        {
            typeStack.Push(type);
            if (typeStack.Count(t => Equals(t, type)) > 1)
                throw new TypeAccessException("Recursive dependency.");

            var result = maps.Where(x => Equals(x.SuperTypeInfo, type)).ToList();
            if (result.Count > 1)
                throw new TypeAccessException($"There is more than one ({result.Count}) registered type which is assignable to type '{type.Name}'.");
            Map map;
            if (result.Count == 1)
                map = result.Single();
            else if (typeof (IEnumerable).GetTypeInfo().IsAssignableFrom(type))
            {
                var genericTypeArg = type.GenericTypeArguments.Single();
                var genericTypeInfo = genericTypeArg.GetTypeInfo();
                var genericListType = typeof (List<>).MakeGenericType(genericTypeArg);
                var genericList = (IList) Activator.CreateInstance(genericListType);
                var assignableMaps = maps.Where(m => Equals(m.SuperTypeInfo, genericTypeInfo)).ToList();
                if (!assignableMaps.Any() && autoLifestyle != Lifestyle.AutoRegisterDisabled)
                    assignableMaps = RegisterAll(genericTypeInfo.AsType(), autoLifestyle);
                if (!assignableMaps.Any())
                    throw new TypeAccessException($"No types found assignable to '{genericTypeArg.Name}'.");
                Observe($"{assignableMaps.Count} registered types found assignable to '{genericTypeArg.Name}'.");
                foreach (var assignableMap in assignableMaps)
                {
                    typeStack.Push(assignableMap.ConcreteTypeInfo);
                    genericList.Add(GetInstance(assignableMap, dependentMap));
                    typeStack.Pop();
                }
                typeStack.Pop();
                return genericList;
            }
            else if (autoLifestyle == Lifestyle.AutoRegisterDisabled)
                throw new TypeAccessException($"Cannot autoresolve unregistered type '{type.Name}'.");
            else // auto-register
            {
                var concreteType = type;
                if (type.IsAbstract)
                {
                    var assignables = concreteTypes.Value.Where(type.IsAssignableFrom).ToList();
                    if (!assignables.Any())
                        throw new TypeAccessException($"No types found assignable to '{type.Name}'.");
                    if (assignables.Count > 1)
                        throw new TypeAccessException($"There is more than one ({assignables.Count}) registered type which is assignable to type '{type.Name}'.");
                    concreteType = assignables.Single();
                }
                map = new Map {SuperTypeInfo = type, ConcreteTypeInfo = concreteType, Lifestyle = autoLifestyle, AutoRegistered = true};
                maps.Add(map);
            }
            var instance = GetInstance(map, dependentMap);
            typeStack.Pop(); // remove the path entry which was added above
            return instance;
        }

        private object GetInstance(Map map, Map dependentMap = null)
        {
            if (dependentMap != null && dependentMap.Lifestyle == Lifestyle.Singleton && map.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependentMap.SuperTypeInfo.Name}' depends on transient '{map.SuperTypeInfo.Name}'.");

            var mapWithConcreteType = maps.FirstOrDefault(m => !Equals(m.SuperTypeInfo, map.SuperTypeInfo) && m.ConcreteTypeInfo != null && Equals(m.ConcreteTypeInfo, map.SuperTypeInfo));
            if (mapWithConcreteType != null)
                throw new TypeAccessException($"Configuration error: type '{mapWithConcreteType.SuperTypeInfo.Name}' is registered to return type '{map.SuperTypeInfo.Name}'.");

            if (map.Factory == null)
            {
                if (map.Lifestyle == Lifestyle.Singleton)
                {
                    var value = CreateInstance(map);
                    map.Factory = () => value;
                }
                else
                    map.Factory = () => CreateInstance(map);
            }
            var instance = map.Factory(); // get or create the instance
            return instance;
        }

        private object CreateInstance(Map map)
        {
            map.Instances++;
            var type = map.ConcreteTypeInfo;
            var constructors = type.DeclaredConstructors.Where(t => !t.IsPrivate).ToList();
            if (constructors.Count != 1)
                throw new TypeAccessException($"Type '{type.Name}' has {constructors.Count} public or internal constructors. Instantiation requires exactly 1 constructor, public or internal.");
            var constructor = constructors.Single();
            var parameters = constructor.GetParameters()
                .Select(p => p.HasDefaultValue ? p.DefaultValue : GetInstance(p.ParameterType.GetTypeInfo(), map)).ToList();
            Observe($"Constructing {map.Lifestyle} instance of type '{type.Name}({string.Join(", ", parameters.Select(p => p.GetType().Name))})'.");
            return constructor.Invoke(parameters.ToArray());
        }

        internal IList<Map> Dump() // diagnostic
        {
            lock (maps)
                return maps.ToList();
        }

        public void Dispose()
        {
            lock (maps)
            {
                foreach (var instance in maps.Where(m => m.Lifestyle == Lifestyle.Singleton && m.Factory != null)
                    .Select(m => m.Factory()).OfType<IDisposable>())
                {
                    Observe($"Disposing type '{instance.GetType().Name}'.");
                    instance.Dispose();
                }
                maps.Clear();
            }
            Observe("Container disposed.");
        }

        private string Observe(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            observer?.OnNext(message);
            return message + "\n";
        }
    }
}
