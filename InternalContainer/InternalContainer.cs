/*
InternalContainer.cs 1.03
Copyright 2016 David Shepherd
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace InternalContainer
{
    internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

    internal sealed class Map
    {
        internal TypeInfo SuperType, ConcreteType;
        internal Lifestyle Lifestyle;
        internal bool AutoRegistered;
        internal Func<object> Factory;
        internal int InstancesCreated;
        public override string ToString()
        {
            return $"{(Equals(SuperType, ConcreteType) ? "" : SuperType.Name + "<-")}" +
                   $"{ConcreteType.Name}, {Lifestyle}, AutoRegistered={AutoRegistered}, InstancesCreated={InstancesCreated}.";
        }
    }

    internal sealed class Container : IDisposable
    {
        private readonly Lifestyle autoLifestyle;
        private readonly Dictionary<TypeInfo, Map> maps = new Dictionary<TypeInfo, Map>();
        private readonly HashSet<TypeInfo> concreteTypes = new HashSet<TypeInfo>();
        private readonly Lazy<List<TypeInfo>> allConcreteTypes;
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        internal Container(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, Action<string> log = null, Assembly assembly = null)
        {
            this.autoLifestyle = autoLifestyle;
            this.log = log;
            Log("Creating Container.");
            allConcreteTypes = new Lazy<List<TypeInfo>>(() => (assembly ?? this.GetType().GetTypeInfo().Assembly)
                .DefinedTypes.Where(t => !t.IsAbstract).ToList());
        }

        internal void RegisterSingleton<T>() => Register(typeof(T), typeof(T), null, Lifestyle.Singleton);
        internal void RegisterTransient<T>() => Register(typeof(T), typeof(T), null, Lifestyle.Transient);
        internal void RegisterSingleton<TSuper, TConcrete>() where TConcrete : TSuper => Register(typeof(TSuper), typeof(TConcrete), null, Lifestyle.Singleton);
        internal void RegisterTransient<TSuper, TConcrete>() where TConcrete : TSuper => Register(typeof(TSuper), typeof(TConcrete), null, Lifestyle.Transient);
        internal void RegisterInstance<TSuper>(TSuper instance) => Register(typeof(TSuper), instance?.GetType(), () => instance, Lifestyle.Singleton);
        internal void RegisterFactory<TSuper>(Func<TSuper> factory) where TSuper : class => Register(typeof(TSuper), null, factory, Lifestyle.Transient);
        internal Map Register(Type supertype, Type concretetype, Func<object> factory, Lifestyle lifestyle, bool autoRegister = false)
        {
            if (supertype == null)
                throw new ArgumentNullException(nameof(supertype));
            if (concretetype == null && (factory == null || lifestyle == Lifestyle.Singleton))
                throw new ArgumentException("Invalid", nameof(factory));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            TypeInfo superType = supertype.GetTypeInfo(), concreteType = concretetype?.GetTypeInfo();

            if (concretetype != null)
            {
                if (concreteType.IsAbstract && !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(concreteType))
                {
                    var assignables = allConcreteTypes.Value.Where(concreteType.IsAssignableFrom).ToList();
                    if (assignables.Count != 1)
                        throw new TypeAccessException($"{assignables.Count} types found assignable to '{concreteType.Name}'.");
                    concreteType = assignables.Single();
                }
                if (!superType.IsAssignableFrom(concreteType))
                    throw new ArgumentException($"Type '{concretetype.Name}' is not assignable to type '{supertype.Name}'.");
            }
            lock (maps)
            {
                if (factory == null)
                    Log($"Registering {lifestyle} type '{concretetype.Name}'{(supertype == concretetype ? "" : "->'" + supertype.Name + "'")}.");
                else if (concretetype != null)
                    Log($"Registering instance of type {(supertype != concretetype ? "'" + concretetype.Name + "'->" : "")}'{supertype.Name}'.");
                else
                    Log($"Registering type '{supertype.Name}' factory.");

                var map = new Map { SuperType = superType, ConcreteType = concreteType, Factory = factory, Lifestyle = lifestyle, AutoRegistered = autoRegister};
                try
                {
                    maps.Add(superType, map);
                }
                catch (Exception ex)
                {
                    throw new TypeAccessException($"Type '{superType.Name}' is already registered.", ex);
                }
                if (concreteType != null && !concreteTypes.Add(concreteType))
                {
                    maps.Remove(superType);
                    var dup = maps.Values.First(m => concreteType.Equals(m.ConcreteType));
                    throw new TypeAccessException($"Type '{dup.SuperType.Name}' is already registered to return '{concreteType.Name}'.");
                }
                return map;
            }
        }

        internal T GetInstance<T>() => (T)GetInstance(typeof(T));
        internal object GetInstance(Type supertype)
        {
            if (supertype == null)
                throw new ArgumentNullException(nameof(supertype));
            lock (maps)
            {
                Log($"Getting instance of type '{supertype.Name}'.");
                typeStack.Clear();
                try
                {
                    var instance = GetInstance(supertype.GetTypeInfo());
                    Debug.Assert(!typeStack.Any());
                    return instance;
                }
                catch (TypeAccessException ex)
                {
                    var message = $"Could not get instance of type '{string.Join("->", typeStack.Select(t => t.Name))}'.\n";
                    Log(message + ex.Message);
                    throw new TypeAccessException(message, ex);
                }
            }
        }

        private object GetInstance(TypeInfo superType, Map dependent = null)
        {
            typeStack.Push(superType);
            if (typeStack.Count(t => t.Equals(superType)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            var instance = GetInstanceInternal(superType, dependent);
            typeStack.Pop();
            return instance;
        }

        private object GetInstanceInternal(TypeInfo superType, Map dependent)
        {
            Map map;
            if (!maps.TryGetValue(superType, out map))
            {   // auto-register
                var lifestyle = GetLifeStyle(superType, dependent);
                if (autoLifestyle == Lifestyle.AutoRegisterDisabled && !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(dependent.SuperType))
                    throw new TypeAccessException($"Cannot resolve unregistered type '{superType.Name}'.");
                map = Register(superType.AsType(), superType.AsType(), null, lifestyle, true);
            }
            return GetInstanceFromMap(map, dependent);
        }

        private object GetInstanceFromMap(Map map, Map dependent)
        {
            if (dependent?.Lifestyle == Lifestyle.Singleton && map.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent.SuperType.Name}' depends on transient '{map.SuperType.Name}'.");
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
            return map.Factory();
        }

        private object CreateInstance(Map map)
        {
            map.InstancesCreated++;
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(map.ConcreteType))
                return GetGenericList(map);
            return CreateInstancex(map);
        }

        private object CreateInstancex(Map map)
        {
            var type = map.ConcreteType;
            var constructors = type.DeclaredConstructors.Where(t => !t.IsPrivate).ToList();
            if (constructors.Count != 1)
                throw new TypeAccessException($"Type '{type.Name}' has {constructors.Count} constructors." +
                    $" Instantiation requires exactly 1 constructor, public or internal.");
            var constructor = constructors.Single();
            var parameters = constructor.GetParameters()
                .Select(p => p.HasDefaultValue ? p.DefaultValue : GetInstance(p.ParameterType.GetTypeInfo(), map)).ToList();
            var parametersText = string.Join(", ", parameters.Select(p => p.GetType().Name));
            Log($"Constructing {map.Lifestyle} instance of type '{type.Name}({parametersText})'.");
            return constructor.Invoke(parameters.ToArray());
        }

        private object GetGenericList(Map map)
        {
            var type = map.ConcreteType;
            var generictype = type.GenericTypeArguments.Single();
            var genericType = generictype.GetTypeInfo();
            var genericListType = typeof(List<>).MakeGenericType(generictype);
            var genericList = (IList)Activator.CreateInstance(genericListType);
            var assignables = allConcreteTypes.Value.Where(genericType.IsAssignableFrom).ToList();
            Log($"Creating list of {assignables.Count} registered types assignable to '{generictype.Name}'.");
            foreach (var assignable in assignables)
                genericList.Add(GetInstance(assignable, map));
            return genericList;
        }

        private Lifestyle GetLifeStyle(TypeInfo superType, Map dependent)
        {
            if (autoLifestyle == Lifestyle.AutoRegisterDisabled && dependent != null &&
                typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(dependent.SuperType))
                    return dependent.Lifestyle;
            if (dependent?.Lifestyle != Lifestyle.Singleton || autoLifestyle != Lifestyle.Transient)
                return autoLifestyle;
            Log($"Warning: type '{superType.Name}' lifestyle set to singleton because dependent '{dependent.SuperType.Name}' is singleton.");
            return Lifestyle.Singleton;
        }

        internal IList<Map> Maps() // diagnostic
        {
            lock (maps)
                return maps.Values.ToList();
        }

        public void Dispose()
        {
            lock (maps)
            {
                foreach (var instance in maps.Values.Where(m => m.Lifestyle.Equals(Lifestyle.Singleton) && m.Factory != null)
                    .Select(m => m.Factory()).OfType<IDisposable>())
                {
                    Log($"Disposing type '{instance.GetType().Name}'.");
                    instance.Dispose();
                }
                maps.Clear();
                concreteTypes.Clear();
            }
            Log("Container disposed.");
        }

        private void Log(string message)
        {
            if (!string.IsNullOrEmpty(message))
                log?.Invoke(message);
        }
    }
}
