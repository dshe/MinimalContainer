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
using System.Text;

namespace InternalContainer
{
    internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

    internal sealed class Map
    {
        internal TypeInfo SuperType, ConcreteType;
        internal Lifestyle Lifestyle;
        internal bool AutoRegistered;
        internal Func<object> Factory;
        internal int Instances;
        public override string ToString()
        {
            var path = Equals(ConcreteType, null) || Equals(ConcreteType, SuperType) ? "" : Container.Pretty(ConcreteType) + "->";
            return $"{path}{Container.Pretty(SuperType)}, {Lifestyle}, AutoRegistered={AutoRegistered}, InstancesCreated={Instances}.";
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
            Log(()=>"Creating Container.");
            allConcreteTypes = new Lazy<List<TypeInfo>>(() => (assembly ?? this.GetType().GetTypeInfo().Assembly)
                .DefinedTypes.Where(t =>t.IsClass && !t.IsAbstract).ToList());
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
                throw new ArgumentNullException(nameof(concretetype));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            TypeInfo superType = supertype.GetTypeInfo(), concreteType = concretetype?.GetTypeInfo();

            if (concreteType != null)
            {
                if (concreteType.IsAbstract && !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(concreteType))
                {
                    var assignables = allConcreteTypes.Value.Where(concreteType.IsAssignableFrom).ToList();
                    if (assignables.Count != 1)
                        throw new ArgumentException(nameof(concreteType), $"{assignables.Count} types found assignable to '{Pretty(concreteType)}'.");
                    concreteType = assignables.Single();
                }
                if (!superType.IsAssignableFrom(concreteType))
                    throw new ArgumentException(nameof(concreteType), $"Type '{Pretty(concreteType)}' is not assignable to type '{Pretty(superType)}'.");
            }
            lock (maps)
            {
                var map = new Map { SuperType = superType, ConcreteType = concreteType, Factory = factory, Lifestyle = lifestyle, AutoRegistered = autoRegister };
                try
                {
                    maps.Add(superType, map);
                }
                catch (Exception ex)
                {
                    throw new TypeAccessException($"Type '{Pretty(superType)}' is already registered.", ex);
                }
                if (concreteType != null && !concreteTypes.Add(concreteType))
                {
                    maps.Remove(superType);
                    var dup = maps.Values.First(m => concreteType.Equals(m.ConcreteType));
                    throw new TypeAccessException($"Type '{Pretty(dup.SuperType)}' is already registered to return '{Pretty(concreteType)}'.");
                }
                if (factory == null)
                    Log(()=>$"Registering {lifestyle} type '{Pretty(concreteType)}'{(superType.Equals(concreteType) ? "" : "->'" + Pretty(superType) + "'")}.");
                else if (concreteType != null)
                    Log(()=>$"Registering instance of type {(superType.Equals(concreteType) ? "" : "'" + Pretty(concreteType) + "'->")}'{Pretty(superType)}'.");
                else
                    Log(()=>$"Registering type '{Pretty(superType)}' factory.");
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
                Log(()=>$"Getting instance of type '{Pretty(supertype)}'.");
                typeStack.Clear();
                try
                {
                    var instance = GetInstance(supertype.GetTypeInfo());
                    Debug.Assert(!typeStack.Any());
                    return instance;
                }
                catch (TypeAccessException ex)
                {
                    var typePath = string.Join("->", typeStack.Select(Pretty));
                    var message = $"Could not get instance of type '{typePath}'\n{ex.Message}\n";
                    Log(() =>message);
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
                var lifestyle = autoLifestyle;
                if (lifestyle == Lifestyle.AutoRegisterDisabled && dependent != null && typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(dependent.SuperType))
                    lifestyle = dependent.Lifestyle;
                if (dependent?.Lifestyle == Lifestyle.Transient && autoLifestyle == Lifestyle.Singleton)
                {
                    lifestyle = Lifestyle.Singleton;
                    Log(()=>$"Warning: type '{Pretty(superType)}' lifestyle set to singleton because dependent '{Pretty(dependent.SuperType)}' is singleton.");
                }
                if (lifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{Pretty(superType)}'.");
                map = Register(superType.AsType(), superType.AsType(), null, lifestyle, true);
            }
            return GetInstanceFromMap(map, dependent);
        }

        private object GetInstanceFromMap(Map map, Map dependent)
        {
            if (dependent?.Lifestyle == Lifestyle.Singleton && map.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{Pretty(dependent.SuperType)}' depends on transient '{Pretty(map.SuperType)}'.");
            if (map.Factory == null)
            {
                if (map.Lifestyle == Lifestyle.Singleton)
                {
                    var value = CreateInstanceOrList(map);
                    map.Factory = () => value;
                }
                else
                    map.Factory = () => CreateInstanceOrList(map);
            }
            return map.Factory();
        }

        private object CreateInstanceOrList(Map map)
        {
            map.Instances++;
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(map.ConcreteType))
                return CreateList(map);
            return CreateInstance(map);
        }

        private object CreateInstance(Map map)
        {
            var type = map.ConcreteType;
            var constructors = type.DeclaredConstructors.Where(t => !t.IsPrivate).ToList();
            if (constructors.Count != 1)
                throw new TypeAccessException($"Type '{Pretty(type)}' has {constructors.Count} constructors." +
                    $" Instantiation requires exactly 1 constructor, public or internal.");
            var constructor = constructors.Single();
            var parameters = constructor.GetParameters()
                .Select(p => p.HasDefaultValue ? p.DefaultValue : GetInstance(p.ParameterType.GetTypeInfo(), map)).ToList();
            var parametersText = string.Join(", ", parameters.Select(p => p.GetType().Name));
            Log(()=>$"Constructing {map.Lifestyle} instance of type '{Pretty(type)}({parametersText})'.");
            return constructor.Invoke(parameters.ToArray());
        }

        private object CreateList(Map map)
        {
            var type = map.ConcreteType;
            var generictype = type.GenericTypeArguments.Single();
            var genericType = generictype.GetTypeInfo();
            var genericListType = typeof(List<>).MakeGenericType(generictype);
            var genericList = (IList)Activator.CreateInstance(genericListType);
            var assignables = allConcreteTypes.Value.Where(genericType.IsAssignableFrom).ToList();
            Log(()=>$"Creating list of {assignables.Count} registered types assignable to '{Pretty(generictype)}'.");
            foreach (var assignable in assignables)
                genericList.Add(GetInstance(assignable, map));
            return genericList;
        }

        internal IList<Map> Maps() // diagnostic
        {
            lock (maps)
                return maps.Values.ToList();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Container: {autoLifestyle}");
            sb.AppendLine($"concrete types in assembly: {allConcreteTypes.Value.Count}, registered concrete types: {concreteTypes.Count}, maps: {maps.Count}.");
            foreach (var map in maps.Values)
                sb.AppendLine(map.ToString());
            return sb.ToString();
        }

        internal static string Pretty(Type type) => Pretty(type?.GetTypeInfo());
        internal static string Pretty(TypeInfo type)
        {
            if (type == null)
                return null;
            if (type.IsGenericParameter || !type.IsGenericType)
                return type.Name;

            var sb = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                name = name.Substring(0, index);
            sb.AppendFormat(name);
            sb.Append('<');
            var first = true;
            foreach (var arg in type.GenericTypeArguments)
            {
                if (!first)
                    sb.Append(',');
                sb.Append(Pretty(arg.GetTypeInfo()));
                first = false;
            }
            sb.Append('>');
            return sb.ToString();
        }

        public void Dispose()
        {
            lock (maps)
            {
                foreach (var instance in maps.Values.Where(m => m.Lifestyle.Equals(Lifestyle.Singleton) && m.Factory != null)
                    .Select(m => m.Factory()).OfType<IDisposable>())
                {
                    Log(()=>$"Disposing type '{Pretty(instance.GetType())}'.");
                    instance.Dispose();
                }
                maps.Clear();
                concreteTypes.Clear();
            }
            Log(()=>"Container disposed.");
        }

        private void Log(Func<string> message)
        {
            if (log == null)
                return;
            var msg = message();
            if (!string.IsNullOrEmpty(msg))
                log(msg);
        }
    }
}
