//InternalContainer.cs 1.03
//Copyright 2016 David Shepherd. Licensed under the Apache License 2.0: http://www.apache.org/licenses/LICENSE-2.0
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace InternalContainer
{
    internal enum Lifestyle { AutoRegisterDisabled, Transient, Singleton };

    internal sealed class Mapp
    {
        internal readonly TypeInfo SuperType, ConcreteType;
        internal readonly Lifestyle Lifestyle;
        internal readonly bool AutoRegistered;
        internal Func<object> Factory;
        internal int Instances;
        public Mapp(TypeInfo superType, TypeInfo concreteType, Lifestyle lifestyle, bool autoRegistered)
        {
            SuperType = superType;
            ConcreteType = concreteType;
            Lifestyle = lifestyle;
            AutoRegistered = autoRegistered;
        }
        public override string ToString()
        {
            return $"{(Equals(ConcreteType, null) || Equals(ConcreteType, SuperType) ? "" : ConcreteType.AsString() + "->")}" +
                   $"{SuperType.AsString()}, {Lifestyle}, AutoRegistered={AutoRegistered}, InstancesCreated={Instances}.";
        }
    }

    internal sealed class Container : IDisposable
    {
        private readonly Lifestyle autoLifestyle;
        private readonly Lazy<List<TypeInfo>> allConcreteTypes;
        private readonly HashSet<TypeInfo> concreteTypes = new HashSet<TypeInfo>();
        private readonly Dictionary<TypeInfo, List<Mapp>> maps = new Dictionary<TypeInfo, List<Mapp>>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(Lifestyle autoLifestyle = Lifestyle.AutoRegisterDisabled, Action<string> log = null, params Assembly[] assemblies)
        {
            this.autoLifestyle = autoLifestyle;
            this.log = log;
            Log(() => "Creating Container.");
            allConcreteTypes = new Lazy<List<TypeInfo>>(() =>
                (assemblies ?? new [] {this.GetType().GetTypeInfo().Assembly})
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract).ToList())
                .SelectMany(x => x).ToList());
        }

        public void RegisterSingleton<T>() => Register(typeof(T).GetTypeInfo(), null, Lifestyle.Singleton);
        public void RegisterTransient<T>() => Register(typeof(T).GetTypeInfo(), null, Lifestyle.Transient);
        public void RegisterSingleton<TSuper, TConcrete>() where TConcrete : TSuper => Register(typeof(TSuper).GetTypeInfo(), typeof(TConcrete).GetTypeInfo(), Lifestyle.Singleton);
        public void RegisterTransient<TSuper, TConcrete>() where TConcrete : TSuper => Register(typeof(TSuper).GetTypeInfo(), typeof(TConcrete).GetTypeInfo(), Lifestyle.Transient);
        private List<Mapp> Register(TypeInfo superType, TypeInfo concreteType, Lifestyle lifestyle, bool autoRegister = false)
        {
            if (superType == null)
                throw new ArgumentNullException(nameof(superType));
            if (lifestyle == Lifestyle.AutoRegisterDisabled)
                throw new ArgumentException("Invalid", nameof(lifestyle));
            var concretes = new List<TypeInfo>();
            if (concreteType != null)
            {
                if (!superType.IsAssignableFrom(concreteType))
                    throw new TypeAccessException($"Type '{concreteType.AsString()}' is not assignable to type '{superType.AsString()}'.");
                concretes.Add(concreteType);
            }
            else if (typeof (IEnumerable).GetTypeInfo().IsAssignableFrom(superType))
                concretes.Add(superType);
            else
            {
                var assignables = allConcreteTypes.Value.Where(superType.IsAssignableFrom).ToList();
                if (!assignables.Any())
                    throw new TypeAccessException($"No types found assignable to '{superType.AsString()}'.");
                concretes.AddRange(assignables);
            }
            lock (maps)
            {
                concretes
                    .FirstOrDefault(t => !concreteTypes.Add(t))
                    .Map(t => Maps().First(x => t.Equals(x.ConcreteType)))
                    .Map(m => $"Type '{m.SuperType.AsString()}' is already registered to return '{m.ConcreteType.AsString()}'.")
                    .Do(msg => { throw new TypeAccessException(msg);});

                var list = GetList(superType);

                list.AddRange(concretes.Select(concrete =>
                    new Mapp(superType, concrete, lifestyle, autoRegister)
                    .Do(m => Log(() => $"Registering type {m}"))));

                return list;
            }
        }

        public void RegisterInstance<TSuper>(TSuper instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            var map = new Mapp(typeof(TSuper).GetTypeInfo(), instance.GetType().GetTypeInfo(), Lifestyle.Singleton, false)
                { Factory = () => instance };
            lock (maps)
            {
                if (!concreteTypes.Add(map.ConcreteType))
                {
                    Maps().FirstOrDefault(m => map.ConcreteType.Equals(m.ConcreteType))
                        .Map(m => $"Type '{m.SuperType.AsString()}' is already registered to return '{map.ConcreteType.AsString()}'.")
                        .Do(msg => { throw new TypeAccessException(msg); });
                }
                Log(() => $"Registering instance of type. {map}");
                GetList(map.SuperType).Add(map);
            }
        }

        public void RegisterFactory<TSuper>(Func<TSuper> factory) where TSuper : class =>
            RegisterFactory(typeof (TSuper).GetTypeInfo(), factory);
        internal void RegisterFactory(TypeInfo superType, Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var map = new Mapp(superType, null, Lifestyle.Transient, false) { Factory = factory };
            lock (maps)
            {
                Log(() => $"Registering type factory. {map}");
                GetList(superType).Add(map);
            }
        }

        private List<Mapp> GetList(TypeInfo superType)
        {
            List<Mapp> list;
            if (!maps.TryGetValue(superType, out list))
            {
                list = new List<Mapp>();
                maps.Add(superType, list);
            }
            return list;
        }

        public T GetInstance<T>() => (T)GetInstance(typeof(T));
        public object GetInstance(Type supertype)
        {
            if (supertype == null)
                throw new ArgumentNullException(nameof(supertype));
            lock (maps)
            {
                Log(() => $"Getting instance of type '{supertype.AsString()}'.");
                typeStack.Clear();
                try
                {
                    var instance = GetInstance(supertype.GetTypeInfo());
                    Debug.Assert(!typeStack.Any());
                    return instance;
                }
                catch (TypeAccessException ex)
                {
                    var typePath = string.Join("->", typeStack.Select(t => t.AsString()));
                    var message = $"Could not get instance of type '{typePath}'{Environment.NewLine}{ex.Message}{Environment.NewLine}";
                    Log(() => message);
                    throw new TypeAccessException(message, ex);
                }
            }
        }

        private object GetInstance(TypeInfo superType, Mapp dependent = null)
        {
            typeStack.Push(superType);
            if (typeStack.Count(t => t.Equals(superType)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            var instance = GetInstanceInternal(superType, dependent);
            typeStack.Pop();
            return instance;
        }

        private object GetInstanceInternal(TypeInfo superType, Mapp dependent)
        {
            List<Mapp> list;
            if (!maps.TryGetValue(superType, out list))
            {
                var lifestyle = autoLifestyle;
                if (dependent != null)
                    lifestyle = dependent.Lifestyle;
                if (lifestyle == Lifestyle.AutoRegisterDisabled)
                    throw new TypeAccessException($"Cannot resolve unregistered type '{superType.AsString()}'.");
                // auto-register
                list = Register(superType, null, lifestyle, true);
            }
            var map = list.SingleOrDefault();
            if (map == null)
                throw new TypeAccessException($"{list.Count} types found!");
            return GetInstanceFromMap(map, dependent);
        }

        private object GetInstanceFromMap(Mapp mapp, Mapp dependent)
        {
            if (dependent?.Lifestyle == Lifestyle.Singleton && mapp.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent.SuperType.AsString()}' depends on transient '{mapp.SuperType.AsString()}'.");
            if (mapp.Factory == null)
            {
                if (mapp.Lifestyle == Lifestyle.Singleton)
                {
                    var value = CreateInstanceOrList(mapp);
                    mapp.Factory = () => value;
                }
                else
                    mapp.Factory = () => CreateInstanceOrList(mapp);
            }
            return mapp.Factory();
        }

        private object CreateInstanceOrList(Mapp mapp)
        {
            mapp.Instances++;
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(mapp.SuperType))
                return CreateList(mapp);
            return CreateInstance(mapp);
        }

        private object CreateInstance(Mapp mapp)
        {
            var type = mapp.ConcreteType;
            var constructors = type.DeclaredConstructors.Where(t => !t.IsPrivate).ToList();
            var constructor = constructors.SingleOrDefault();
            if (constructor == null)
                throw new TypeAccessException($"Type '{type.AsString()}' has {constructors.Count} constructors." +
                    $" Instantiation requires exactly 1 constructor, public or internal.");
            var parameters = constructor.GetParameters()
                .Select(p => p.HasDefaultValue ? p.DefaultValue : GetInstance(p.ParameterType.GetTypeInfo(), mapp)).ToList();
            var parametersText = string.Join(", ", parameters.Select(p => p.GetType().Name));
            Log(() => $"Constructing {mapp.Lifestyle} instance of type '{type.AsString()}({parametersText})'.");
            return constructor.Invoke(parameters.ToArray());
        }

        private object CreateList(Mapp mapp)
        {
            var generictype = mapp.ConcreteType.GenericTypeArguments.Single();
            var genericList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(generictype));
            var assignables = allConcreteTypes.Value.Where(generictype.GetTypeInfo().IsAssignableFrom).ToList();
            Log(() => $"Creating list of {assignables.Count} registered types assignable to '{generictype.AsString()}'.");
            foreach (var assignable in assignables)
                genericList.Add(GetInstance(assignable, mapp));
            return genericList;
        }

        public IList<Mapp> Maps() // diagnostic
        {
            lock (maps)
                return maps.Values.SelectMany(x => x).OrderBy(x => x.SuperType.Name).ThenBy(x => x.ConcreteType?.Name).ToList();
        }

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
                .AppendLine($"Container: {autoLifestyle}")
                .AppendLine($"Registered super types: {maps.Count}, concrete types: {concreteTypes.Count}.")
                .AppendLine(string.Join(Environment.NewLine, Maps()))
                .ToString();
        }

        public void Dispose()
        {
            lock (maps)
            {
                Maps()
                    .Where(m => m.Lifestyle.Equals(Lifestyle.Singleton) && m.Factory != null)
                    .Select(m => m.Factory())
                    .OfType<IDisposable>()
                    .Do(d => Log(() => $"Disposing type '{d.GetType().AsString()}'."))
                    .ToList().ForEach(x => x.Dispose());
                maps.Clear();
                concreteTypes.Clear();
            }
            Log(() => "Container disposed.");
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

        public static TResult Map<TSource, TResult>(this TSource @this, Func<TSource, TResult> fcn) where TSource : class where TResult : class
            => @this == null ? null : fcn(@this);

        public static T Where<T>(this T @this, Func<bool> predicate) where T : class
            => @this == null || !predicate() ? null : @this;

        public static T Do<T>(this T @this, Action<T> act) where T: class
        {
            if (@this != null)
                act(@this);
            return @this;
        }
        public static List<T> ForEach<T>(this IEnumerable<T> items, Action<T> act)
        {
            var list = items.ToList();
            foreach (var item in list)
                act(item);
            return list;
        }
    }
}
