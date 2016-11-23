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
    internal enum Lifestyle { Undefined, Transient, Singleton, Instance, Factory }
    public enum DefaultLifestyle { Undefined, Transient, Singleton }

    internal sealed class Registration
    {
        internal TypeInfo Type, TypeConcrete;
        internal Lifestyle Lifestyle;
        internal Func<object> Factory;
        internal Expression Expression;
        public override string ToString() =>
            $"{(TypeConcrete == null || Equals(TypeConcrete, Type) ? "" : TypeConcrete.AsString() + "->")}{Type.AsString()}, {Lifestyle}.";
    }

    public sealed class Container : IDisposable
    {
        private readonly DefaultLifestyle defaultLifestyle;
        private readonly Lazy<List<TypeInfo>> allTypesConcrete;
        private readonly Dictionary<Type, Registration> registrations = new Dictionary<Type, Registration>();
        private readonly Stack<TypeInfo> typeStack = new Stack<TypeInfo>();
        private readonly Action<string> log;

        public Container(DefaultLifestyle defaultLifestyle = DefaultLifestyle.Undefined, Action<string> log = null, params Assembly[] assemblies)
        {
            this.defaultLifestyle = defaultLifestyle;
            this.log = log;
            Log("Creating Container.");
            var assemblyList = assemblies.ToList();
            if (!assemblyList.Any())
            {
                var method = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                if (method == null)
                    throw new ArgumentException("Since the calling assembly cannot be determined, one or more assemblies must be indicated when constructing the container.");
                assemblyList.Add((Assembly)method.Invoke(null, new object[0]));
            }
            allTypesConcrete = new Lazy<List<TypeInfo>>(() => assemblyList
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface).ToList())
                .SelectMany(x => x)
                .ToList());
            RegisterInstance(this); // container self-registration
        }

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type typeConcrete = null) => Register(type, typeConcrete, Lifestyle.Transient);

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type typeConcrete = null) => Register(type, typeConcrete, Lifestyle.Singleton);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            return Register(type, null, Lifestyle.Instance, () => instance, Expression.Constant(instance));
        }

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            return Register(type, null, Lifestyle.Factory, factory, (Expression<Func<object>>)(() => factory()));
        }

        private Container Register(Type type, Type typeConcrete, Lifestyle lifestyle, Func<object> factory = null, Expression expression = null, [CallerMemberName] string caller = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            var reg = AddRegistration(type.GetTypeInfo());
            reg.TypeConcrete = typeConcrete?.GetTypeInfo();
            reg.Lifestyle = lifestyle;
            reg.Factory = factory;
            reg.Expression = expression;
            Log(() => $"{caller}: {reg}");
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(TypeInfo type)
        {
            if (type.AsType() == typeof(string) || (!type.IsClass && !type.IsInterface))
                throw new TypeAccessException("not a class or interface.");
            var reg = new Registration { Type = type };
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

        public T Resolve<T>() => (T)Resolve(typeof(T));
        public object Resolve(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            try
            {
                return GetRegistration(type, null).Factory();
            }
            catch (TypeAccessException ex)
            {
                if (!typeStack.Any())
                    throw new TypeAccessException($"Could not get instance of type '{type.AsString()}'. {ex.Message}", ex);
                var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
            }
        }

        private Registration GetRegistration(Type type, Registration dependent)
        {
            Registration reg;
            if (!registrations.TryGetValue(type, out reg))
            {
                var typeInfo = type.GetTypeInfo();
                if (defaultLifestyle == DefaultLifestyle.Undefined && !typeInfo.IsFunc() && !typeInfo.IsEnumerable())
                    throw new TypeAccessException($"Cannot resolve unregistered type '{type.AsString()}'.");
                reg = AddRegistration(typeInfo);
            }
            if (reg.Expression == null)
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
            if (typeStack.Count(t => t.Equals(reg.Type)) != 1)
                throw new TypeAccessException("Recursive dependency.");

            InitializeTypes(reg, dependent);

            if (dependent?.Lifestyle == Lifestyle.Singleton && reg.Lifestyle == Lifestyle.Transient)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent.Type.AsString()}' depends on transient '{reg.Type.AsString()}'.");
            typeStack.Pop();
        }

        private void InitializeTypes(Registration reg, Registration dependent)
        {
            if (reg.Type.IsFunc())
                InitializeFunc(reg);
            else if (reg.Type.IsEnumerable())
                InitializeEnumerable(reg);
            else
                InitializeType(reg, dependent);
        }

        private void InitializeFunc(Registration reg)
        {
            reg.Lifestyle = Lifestyle.Singleton;
            var genericType = reg.Type.GenericTypeArguments.Single();
            var regDependent = new Registration { Type = reg.Type, Lifestyle = Lifestyle.Transient };
            var genericReg = GetRegistration(genericType, regDependent);
            if (genericReg.Lifestyle == Lifestyle.Transient)
                reg.Expression = Expression.Lambda(genericReg.Expression);
            else if (genericReg.Lifestyle == Lifestyle.Factory)
                reg.Expression = Expression.Constant(genericReg.Factory);
            else
                throw new TypeAccessException($"Type from factory '{reg.Type.AsString()}' is a {genericReg.Lifestyle}.");
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
        }

        private void InitializeEnumerable(Registration reg)
        {
            reg.Lifestyle = Lifestyle.Singleton;
            var genericType = reg.Type.GenericTypeArguments.Single().GetTypeInfo();
            var types = defaultLifestyle == DefaultLifestyle.Undefined
                ? registrations.Values.Select(r => r.Type)
                : allTypesConcrete.Value;
            var regDependent = new Registration { Type = reg.Type, Lifestyle = Lifestyle.Singleton };
            var expressions = types
                .Where(t => genericType.IsAssignableFrom(t))
                .Select(t => GetRegistration(t.AsType(), regDependent))
                .Select(r => r.Expression)
                .ToList();
            if (!expressions.Any())
                throw new TypeAccessException($"No types found assignable to generic type '{genericType.AsString()}'.");
            Log($"Creating list of {expressions.Count} types assignable to '{genericType.AsString()}'.");
            reg.Expression = Expression.NewArrayInit(genericType.AsType(), expressions);
            //var genericList = typeof(List<>).MakeGenericType(genericType.AsType());
            //var newExpression = Expression.New(genericList);
            //reg.Expression = Expression.ListInit(newExpression, expressions);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
        }

        private void InitializeType(Registration reg, Registration dependent)
        {
            if (reg.TypeConcrete == null)
                reg.TypeConcrete = FindConcreteType(reg.Type);

            // Use a previously registered instance, if any.
            var previous = registrations.Values
                .Where(r => Equals(r.TypeConcrete, reg.TypeConcrete))
                .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Transient)
                .SingleOrDefault(r => r.Expression != null);
            if (previous != null)
            {
                if (reg.Lifestyle != Lifestyle.Undefined && reg.Lifestyle != previous.Lifestyle)
                    throw new TypeAccessException($"{reg.Lifestyle} '{reg.Type.AsString()}' already registered as {previous.Lifestyle}.");
                reg.Lifestyle = previous.Lifestyle;
                reg.Expression = previous.Expression;
                reg.Factory = previous.Factory;
                return;
            }

            if (reg.Lifestyle == Lifestyle.Undefined)
            {
                if (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Transient)
                    reg.Lifestyle = dependent.Lifestyle;
                else if (defaultLifestyle == DefaultLifestyle.Singleton)
                    reg.Lifestyle = Lifestyle.Singleton;
                else
                    reg.Lifestyle = Lifestyle.Transient;
            }

            SetExpression(reg);
        }

        private void SetExpression(Registration reg)
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

        private Expression GetExpression(Registration reg)
        {
            var ctor = reg.TypeConcrete.GetConstructor();
            var parameters = ctor.GetParameters()
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetRegistration(p.ParameterType, reg).Expression)
                .ToList();
            Log($"Constructing {reg.Lifestyle} instance: '{reg.TypeConcrete.AsString()}({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")})'.");
            return Expression.New(ctor, parameters);
        }

        private TypeInfo FindConcreteType(TypeInfo type)
        {
            // When a non-concrete type is indicated (register or get instance), the concrete type is determined automatically.
            // In this case, the non-concrete type must be assignable to exactly one concrete type.
           if (!type.IsAbstract && !type.IsInterface)
                return type;
           var assignableTypes = allTypesConcrete.Value.Where(type.IsAssignableFrom).ToList(); // slow
           if (assignableTypes.Count == 1)
                return assignableTypes.Single();
            throw new TypeAccessException($"{assignableTypes.Count} types found assignable to '{type.AsString()}'.");
        }

        //////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            var reg = registrations.Values.ToList();
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
            var instances = registrations.Values
                .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Instance)
                .Select(r => r.Factory())
                .Where(i => i != null && i != this);
            foreach (var disposable in instances.OfType<IDisposable>())
            {
                Log($"Disposing type '{disposable.GetType().AsString()}'.");
                disposable.Dispose();
            }
            registrations.Clear();
            Log("Container disposed.");
        }
    }

    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructorAttribute : Attribute {}

    internal static class StandardContainerEx
    {
        internal static ConstructorInfo GetConstructor(this TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
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

        internal static bool IsFunc(this TypeInfo type) => type.Name == "Func`1";
        internal static bool IsEnumerable(this TypeInfo type) => typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type);
        internal static string JoinStrings(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
        internal static string AsString(this Type type) => type.GetTypeInfo().AsString();
        internal static string AsString(this TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            var name = type.Name;
            if (type.IsGenericParameter || !type.IsGenericType)
                return name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (index >= 0)
                name = name.Substring(0, index);
            var args = type.GenericTypeArguments
                .Select(a => a.AsString())
                .JoinStrings(",");
            return $"{name}<{args}>";
        }
    }
}
