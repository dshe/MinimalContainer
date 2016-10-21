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
    public enum Lifestyle { Transient, Singleton, Instance, Factory };
    public enum DefaultLifestyle { Transient, Singleton, None };

    public sealed class Registration
    {
        public Lifestyle Lifestyle;
        public TypeInfo Type, TypeConcrete;
        public Func<object> Factory;
        internal Expression Expression;
        public int Count;
        public override string ToString() =>
            $"{(TypeConcrete == null || Equals(TypeConcrete, Type) ? "" : TypeConcrete.AsString() + "->")}{Type.AsString()}, {Lifestyle}({Count})";
    }

    public sealed class Container : IDisposable
    {
        private readonly DefaultLifestyle defaultLifestyle;
        private readonly List<TypeInfo> allTypesConcrete;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        public int Types => registrations.Count - 1;
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(DefaultLifestyle defaultLifestyle = DefaultLifestyle.None, Action<string> log = null, params Assembly[] assemblies)
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

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type typeConcrete = null) => Register(Lifestyle.Transient, type, typeConcrete);

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type typeConcrete = null) => Register(Lifestyle.Singleton, type, typeConcrete);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return Register(Lifestyle.Instance, type, instance.GetType(), () => instance);
        }

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory)
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

            //if (typeConcrete == null && lifestyle != Lifestyle.Instance && lifestyle != Lifestyle.Factory && GetFuncGenericType(type) == null)
            if (typeConcrete == null && lifestyle != Lifestyle.Instance && lifestyle != Lifestyle.Factory)
                typeConcrete = allTypesConcrete.FindTypeConcrete(type);
            if (typeConcrete != null)
            {
                if (!type.IsAssignableFrom(typeConcrete))
                    throw new TypeAccessException($"Type {typeConcrete.AsString()} is not assignable to type {type.AsString()}.");
                if (typeConcrete.IsValueType)
                    throw new TypeAccessException("Cannot register value type.");
                if (typeof(string).GetTypeInfo().IsAssignableFrom(typeConcrete))
                    throw new TypeAccessException("Cannot register type string.");
            }
            lock (registrations)
                return AddRegistrationCore(lifestyle, type, typeConcrete, factory, caller);
        }

        private Registration AddRegistrationCore(Lifestyle lifestyle, TypeInfo type, TypeInfo typeConcrete,
            Func<object> factory, string caller)
        {
            var reg = new Registration
            {
                Lifestyle = lifestyle,
                Type = type,
                TypeConcrete = typeConcrete,
                Factory = factory
            };
            Log(() => $"{caller}: {reg}");
            try
            {
                registrations.Add(type.AsType(), reg);
            }
            catch (ArgumentException ex)
            {
                throw new TypeAccessException($"Type {type.Name} is already registered.", ex);
            }
            return reg;
        }

        //////////////////////////////////////////////////////////////////////////////

        //public Func<T> GetFactory<T>() where T : class => (Func<T>)GetFactory(typeof(Func<T>));
        //public Func<T> GetFactory<T>() where T : class => () => (T)GetInstance<T>();
        //Func<SomeClass> func2 = () => (SomeClass)func();
        public object GetFactory(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            lock (registrations)
            {
                try
                {
                    var generic = GetFuncGenericType(type.GetTypeInfo());
                    var reg = GetRegistration(generic.AsType(), null);
                    var f = reg.Factory;

                    //var c = Convert(f, type).DynamicInvoke();

                    //return c;

                    return f;
                }
                catch (TypeAccessException ex)
                {
                    if (!typeStack.Any())
                        throw new TypeAccessException($"Could not get instance of type {type}. {ex.Message}", ex);
                    var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                    throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
                }
            }
        }

        //public Func<T> GetFactory<T>() where T : class => () => (T)GetInstance<T>();
        //public Func<T> GetFactory2<T>() where T : class => () => (T)GetInstance<T>();


        public T GetInstance<T>() => (T)GetInstance(typeof(T));
        public object GetInstance(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            lock (registrations)
            {
                try
                {
                    /*
                    //Type gener = typeof(Func<>).MakeGenericType(type);
                    //gener.GetRuntimeMethod().
                    // create the func with expression tree
                    var generic = GetFuncGenericType(type);
                    if (generic != null)
                    {
                        //Type gener = typeof(Func<>).MakeGenericType(generic);
                        var f = GetRegistration(generic, null).Expression;
                        //f.GetMethodInfo().
                        Expression<Func<object>> ff;

                        //ff.Update(() => "x", 1)
                        var xx = Expression.GetFuncType(generic);


                        //var f2 = p => f(()p);
                        //Func<T>

                        return f;
                    }
                    */
                    //var generic = GetFuncGenericType(type.GetTypeInfo());
                    //if (generic != null)
                     //   return GetRegistration(type, null).Factory;


                    return GetRegistration(type, null).Factory();
                }
                catch (TypeAccessException ex)
                {
                    if (!typeStack.Any())
                        throw new TypeAccessException($"Could not get instance of type {type}. {ex.Message}", ex);
                    var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                    throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
                }
            }
        }

        private Registration GetRegistration(Type type, Registration dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(type, out reg))
            {
                if (defaultLifestyle == DefaultLifestyle.None)
                    throw new TypeAccessException($"Cannot resolve unregistered type {type.AsString()}.");
                var style = (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance || defaultLifestyle == DefaultLifestyle.Singleton)
                    ? Lifestyle.Singleton : Lifestyle.Transient;
                reg = AddRegistration(style, type.GetTypeInfo(), null, null, "Auto-registration");
            }
            if (reg.Expression == null)
                Initialize(reg, dependent);
            reg.Count = reg.Lifestyle == Lifestyle.Transient || reg.Lifestyle == Lifestyle.Factory ? reg.Count + 1 : 1;
            return reg;
        }

        private void Initialize(Registration reg, Registration dependent)
        {
            if (dependent == null)
            {
                typeStack.Clear();
                Log(() => $"Getting instance of type: {reg.Type.AsString()}.");
            }
            typeStack.Push(reg.Type);
            if (typeStack.Count(t => t.Equals(reg.Type)) > 1)
                throw new TypeAccessException("Recursive dependency.");
            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton {dependent.Type.AsString()} depends on transient {reg.Type.AsString()}.");

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
            //var factory = GetFactoryExpression(reg); ;
            //if (factory != null)
            //    return factory;
            var arrayExpression = GetArrayExpression(reg);
            if (arrayExpression != null)
                return arrayExpression;
            return GetExpressionNew(reg);
        }

        private Expression GetFactoryExpression(Registration reg)
        {
            var generic = reg.Type.GenericTypeArguments.SingleOrDefault();
            if (generic == null)
                return null;

            var reg2 = GetRegistration(generic, reg);
            var exp = reg2.Expression;
            var ee = Expression.Convert(exp, reg2.Type.AsType());
            return ee;
        }

        private Expression GetExpressionNew(Registration reg)
        {
            var type = reg.TypeConcrete;
            var ctor = type.GetConstructor();
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log(() => $"Constructing {reg.Lifestyle} instance: {type.AsString()}({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")}).");
            return Expression.New(ctor, parameters);
        }

        private Expression GetArrayExpression(Registration reg)
        {
            if (!typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(reg.Type))
                return null;
            var genericType = reg.TypeConcrete.GenericTypeArguments.Single().GetTypeInfo();
            var expressions = allTypesConcrete
                .Where(t => genericType.IsAssignableFrom(t))
                .Select(x => GetRegistration(x.AsType(), reg).Expression)
                .ToList();
            if (!expressions.Any())
                throw new TypeAccessException($"No types found assignable to generic type {genericType.AsString()}.");
            Log(() => $"Creating list of {expressions.Count} types assignable to {genericType.AsString()}.");
            return Expression.NewArrayInit(genericType.AsType(), expressions);
        }

        //////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            var reg = registrations.Values.OrderBy(r => r.Type.Name).ToList();
            return new StringBuilder()
                .AppendLine($"Container: {defaultLifestyle}, {reg.Count} registered types:")
                .AppendLine(reg.Select(x => x.ToString()).JoinStrings(Environment.NewLine))
                .ToString();
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

        /// <summary>
        /// Disposing the container disposes any registered disposable instances.
        /// </summary>
        public void Dispose()
        {
            lock (registrations)
            {
                foreach (var instance in registrations.Values
                    .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Instance)
                    .Select(r => r.Factory())
                    .Where(i => i != null && i != this)
                    .OfType<IDisposable>())
                {
                    Log($"Disposing type {instance.GetType().AsString()}.");
                    instance.Dispose();
                }
                registrations.Clear();
            }
            Log("Container disposed.");
        }

        private static bool IsFunc(Type type)
        {
            if (type == typeof(Func<>))
                return true;
            Type generic = null;
            if (type.GetTypeInfo().IsGenericTypeDefinition)
                generic = type;
            else if (type.GetTypeInfo().IsGenericType)
                generic = type.GetGenericTypeDefinition();
            return generic == typeof(Func<>);
        }
        private static TypeInfo GetFuncGenericType(TypeInfo type)
        {
            var single = type.GenericTypeArguments.SingleOrDefault();
            return single?.GetTypeInfo();
        }

    }

    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructorAttribute : Attribute { }

    internal static class StandardContainerEx
    {
        /// When a non-concrete type is indicated (register or get instance), the concrete type is determined automatically.
        /// In this case, the non-concrete type must be assignable to exactly one concrete type.
        internal static TypeInfo FindTypeConcrete(this List<TypeInfo> allTypesConcrete, TypeInfo type)
        {
            if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type) || (!type.IsAbstract && !type.IsInterface))
                return type;
            var assignableTypes = allTypesConcrete.Where(type.IsAssignableFrom).ToList(); // slow
            if (assignableTypes.Count != 1)
                throw new TypeAccessException($"{assignableTypes.Count} types found assignable to {type.AsString()}.");
            return assignableTypes.Single();
        }

        internal static ConstructorInfo GetConstructor(this TypeInfo type)
        {
            var ctors = type.DeclaredConstructors.Where(c => !c.IsPrivate).ToList();
            if (ctors.Count == 1)
                return ctors.Single();
            if (!ctors.Any())
                throw new TypeAccessException($"Type {type.AsString()} has no public or internal constructor.");
            var ctorsWithAttribute = ctors.Where(c => c.GetCustomAttribute<ContainerConstructorAttribute>() != null).ToList();
            if (ctorsWithAttribute.Count == 1)
                return ctorsWithAttribute.Single();
            if (ctorsWithAttribute.Count > 1)
                throw new TypeAccessException($"Type {type.AsString()} has more than one constructor decorated with {nameof(ContainerConstructorAttribute)}.");
            return ctors.OrderBy(c => c.GetParameters().Length).First();
        }

        internal static string JoinStrings(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
        internal static string AsString(this Type type) => type.GetTypeInfo().AsString();
        internal static string AsString(this TypeInfo type)
        {
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
