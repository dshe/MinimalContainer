/*
InternalContainer.cs 1.02
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
        internal readonly TypeInfo SuperType, ConcreteType;
        internal readonly Lifestyle Lifestyle;
        internal readonly bool AutoRegistered;
        internal Func<object> Factory;
        internal int InstancesCreated;
        internal Map(TypeInfo superType, TypeInfo concreteType, Func<object> factory, Lifestyle lifestyle, bool autoRegistered)
        {
            SuperType = superType;
            ConcreteType = concreteType;
            Factory = factory;
            Lifestyle = lifestyle;
            AutoRegistered = autoRegistered;
        }
        public override string ToString()
        {
            return $"{(Equals(SuperType, ConcreteType) ? "" : SuperType.Name + "<-")}" +
                   $"{ConcreteType.Name}, {Lifestyle}, AutoRegistered={AutoRegistered}, InstancesCreated={InstancesCreated}.";
        }
    }

    internal sealed class Container : IDisposable
    {
        private readonly Lifestyle autoLifestyle;
        private readonly List<Map> maps = new List<Map>();
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
                if (concreteType.IsAbstract)
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
                if (concretetype != null)
                {
                    var anotherSuperType = maps.Where(m => m.ConcreteType.Equals(concreteType)).Select(m => m.SuperType).FirstOrDefault(); // slow
                    if (anotherSuperType != null)
                        throw new TypeAccessException($"Type '{anotherSuperType.Name}' is already registered to return '{concretetype.Name}'.");
                }
                var map = new Map(superType, concreteType, factory, lifestyle, autoRegister);
                maps.Add(map);
                return map;
            }
        }

        internal void RegisterSingletonAll<TSuper>() => RegisterAll(typeof(TSuper), Lifestyle.Singleton);
        internal void RegisterTransientAll<TSuper>() => RegisterAll(typeof(TSuper), Lifestyle.Transient);
        internal List<Map> RegisterAll(Type supertype, Lifestyle lifestyle)
        {
            if (supertype == null)
                throw new ArgumentNullException(nameof(supertype));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            lock (maps)
            {
                var assignables = allConcreteTypes.Value.Where(supertype.GetTypeInfo().IsAssignableFrom).ToList();
                if (!assignables.Any())
                    throw new ArgumentException(Log($"No types found assignable to '{supertype.Name}'."));
                Log($"Registering {assignables.Count} type(s) assignable to '{supertype.Name}'.");
                return assignables.Select(t => Register(supertype, t.AsType(), null, lifestyle)).ToList();
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

        private object GetInstance(TypeInfo superType, Map dependentMap = null)
        {
            typeStack.Push(superType);
            if (typeStack.Count(t => t.Equals(superType)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            var instance = GetInstanceInternal(superType, dependentMap);
            typeStack.Pop();
            return instance;
        }

        private object GetInstanceInternal(TypeInfo superType, Map dependentMap)
        {
            var result = maps.Where(m => m.SuperType.Equals(superType)).ToList(); // slow
            Map map;
            if (result.Count == 1)
                map = result.Single();
            else if (result.Count > 1)
                throw new TypeAccessException($"There are ({result.Count}) types registered for type '{superType.Name}'.");
            else if (typeof (IEnumerable).GetTypeInfo().IsAssignableFrom(superType))
                return GetGenericList(superType, dependentMap);
            else if (autoLifestyle == Lifestyle.AutoRegisterDisabled)
                throw new TypeAccessException($"Cannot resolve unregistered type '{superType.Name}'.");
            else
                map = Register(superType.AsType(), superType.AsType(), null, GetLifeStyle(superType, dependentMap), true);
            return GetInstanceFromMap(map, dependentMap);
        }

        private object GetGenericList(TypeInfo superType, Map dependentMap)
        {
            var generictype = superType.GenericTypeArguments.Single();
            var genericType = generictype.GetTypeInfo();
            var genericListType = typeof(List<>).MakeGenericType(generictype);
            var genericList = (IList)Activator.CreateInstance(genericListType);
            var assignableMaps = maps.Where(m => Equals(m.SuperType, genericType)).ToList(); // slow
            if (!assignableMaps.Any() && autoLifestyle != Lifestyle.AutoRegisterDisabled)
                assignableMaps = RegisterAll(generictype, GetLifeStyle(superType, dependentMap));
            if (!assignableMaps.Any())
                throw new TypeAccessException($"No types found assignable to '{generictype.Name}'.");
            Log($"Creating list of {assignableMaps.Count} registered types assignable to '{generictype.Name}'.");
            foreach (var assignableMap in assignableMaps)
            {
                typeStack.Push(assignableMap.ConcreteType);
                genericList.Add(GetInstanceFromMap(assignableMap, dependentMap));
                typeStack.Pop();
            }
            return genericList;
        }

        private object GetInstanceFromMap(Map map, Map dependentMap = null)
        {
            if (dependentMap?.Lifestyle == Lifestyle.Singleton && map.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependentMap.SuperType.Name}' depends on transient '{map.SuperType.Name}'.");
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
            var constructors = map.ConcreteType.DeclaredConstructors.Where(t => !t.IsPrivate).ToList();
            if (constructors.Count != 1)
                throw new TypeAccessException($"Type '{map.ConcreteType.Name}' has {constructors.Count} constructors." +
                    $" Instantiation requires exactly 1 constructor, public or internal.");
            var constructor = constructors.Single();
            var parameters = constructor.GetParameters()
                .Select(p => p.HasDefaultValue ? p.DefaultValue : GetInstance(p.ParameterType.GetTypeInfo(), map)).ToList();
            var parametersText = string.Join(", ", parameters.Select(p => p.GetType().Name));
            Log($"Constructing {map.Lifestyle} instance of type '{map.ConcreteType.Name}({parametersText})'.");
            return constructor.Invoke(parameters.ToArray());
        }

        private Lifestyle GetLifeStyle(TypeInfo superType, Map dependentMap)
        {
            if (dependentMap?.Lifestyle != Lifestyle.Singleton || autoLifestyle != Lifestyle.Transient)
                return autoLifestyle;
            Log($"Warning: type '{superType.Name}' lifestyle set to singleton because dependent '{dependentMap.SuperType.Name}' is singleton.");
            return Lifestyle.Singleton;
        }

        internal IList<Map> Maps() // diagnostic
        {
            lock (maps)
                return maps.ToList();
        }

        public void Dispose()
        {
            lock (maps)
            {
                foreach (var instance in maps.Where(m => m.Lifestyle.Equals(Lifestyle.Singleton) && m.Factory != null)
                    .Select(m => m.Factory()).OfType<IDisposable>())
                {
                    Log($"Disposing type '{instance.GetType().Name}'.");
                    instance.Dispose();
                }
                maps.Clear();
            }
            Log("Container disposed.");
        }

        private string Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            log?.Invoke(message);
            return message + "\n";
        }
    }
}
