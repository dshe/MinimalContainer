using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MinimalContainer
{
    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructor : Attribute { }


    public enum DefaultLifestyle { Undefined, Transient, Singleton }
    internal enum Lifestyle { Undefined, Transient, Singleton, Instance, Factory }

    internal sealed class Registration
    {
        internal TypeInfo Type, TypeConcrete;
        internal Lifestyle Lifestyle;
        internal Func<object> Factory;
        internal Expression Expression;
        internal Delegate FuncDelegate;
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
                assemblyList.Add(Assembly.GetCallingAssembly());

            allTypesConcrete = new Lazy<List<TypeInfo>>(() => assemblyList
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface))
                .SelectMany(x => x)
                .ToList());
        }

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type typeConcrete = null) => Register(type, typeConcrete, Lifestyle.Transient);

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type typeConcrete = null) => Register(type, typeConcrete, Lifestyle.Singleton);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object instance) => Register(type, null, Lifestyle.Instance, instance);

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory) => Register(type, null, Lifestyle.Factory, null, factory);

        private Container Register(Type type, Type typeConcrete, Lifestyle lifestyle, object instance = null, Func<object> factory = null, [CallerMemberName] string caller = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            var reg = AddRegistration(type.GetTypeInfo());
            reg.TypeConcrete = typeConcrete?.GetTypeInfo();
            reg.Lifestyle = lifestyle;
            if (lifestyle == Lifestyle.Instance)
            {
                if (instance == null)
                    throw new ArgumentNullException(nameof(instance));
                reg.Factory = () => instance;
                reg.Expression = Expression.Constant(instance);
            }
            else if (lifestyle == Lifestyle.Factory)
            {
                reg.Factory = factory ?? throw new ArgumentNullException(nameof(factory));
                reg.Expression = Expression.Call(Expression.Constant(factory.Target), factory.GetMethodInfo());
            }
            Log(() => $"{caller}: {reg}");
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(TypeInfo type)
        {
            if (type.AsType() == typeof(string) || (!type.IsClass && !type.IsInterface))
                throw new TypeAccessException("Type is neither a class nor an interface.");
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

        public object Resolve(in Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            try
            {
                (Registration reg, bool isFunc) = GetOrAddInitializeRegistration(type, null);
                if (!isFunc)
                    return reg.Factory();
                return reg.FuncDelegate ?? (reg.FuncDelegate = Expression.Lambda(reg.Expression).Compile());
            }
            catch (TypeAccessException ex)
            {
                if (!typeStack.Any())
                    typeStack.Push(type.GetTypeInfo());
                var typePath = typeStack.Select(t => t.AsString()).JoinStrings("->");
                throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
            }
        }

        private Expression GetOrAddExpression(in Type type, in Registration dependent)
        {
            (Registration reg, bool isFunc) = GetOrAddInitializeRegistration(type, dependent);
            return isFunc ? Expression.Lambda(reg.Expression) : reg.Expression;
        }

        private (Registration reg, bool isFunc) GetOrAddInitializeRegistration(Type type, in Registration dependent)
        {
            Registration reg;
            bool isFunc = false;
            GetOrAddRegistration();
            if (reg.Expression == null)
                Initialize(reg, dependent, isFunc);
            return (reg, isFunc);

            // local function
            void GetOrAddRegistration()
            {
                if (registrations.TryGetValue(type, out reg))
                    return;
                var typeInfo = type.GetTypeInfo();
                if (typeInfo.GetFuncArgumentType(out TypeInfo funcType))
                {
                    isFunc = true;
                    typeInfo = funcType;
                    if (registrations.TryGetValue(typeInfo.AsType(), out reg))
                    {
                        if (reg.Lifestyle == Lifestyle.Singleton || reg.Lifestyle == Lifestyle.Instance)
                            throw new TypeAccessException($"Func argument type '{typeInfo.AsString()}' must be Transient or Factory.");
                        return;
                    }
                }
                if (defaultLifestyle == DefaultLifestyle.Undefined && !typeInfo.IsEnumerable())
                    throw new TypeAccessException($"Cannot resolve unregistered type '{typeInfo.AsString()}'.");
                reg = AddRegistration(typeInfo);
                if (isFunc)
                    reg.Lifestyle = Lifestyle.Transient;
            }
        }

        private void Initialize(Registration reg, Registration dependent, bool isFunc)
        {
            if (dependent == null)
            {
                typeStack.Clear();
                Log(() => $"Getting instance of type: '{reg.Type.AsString()}'.");
            }
            else if (reg.Lifestyle == Lifestyle.Undefined && (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance))
                reg.Lifestyle = Lifestyle.Singleton;

            typeStack.Push(reg.Type);
            if (typeStack.Count(t => t.Equals(reg.Type)) != 1)
                throw new TypeAccessException("Recursive dependency.");

            InitializeTypes(reg);

            if ((dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance)
                && (reg.Lifestyle == Lifestyle.Transient || reg.Lifestyle == Lifestyle.Factory) && !isFunc)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent.Type.AsString()}' depends on transient '{reg.Type.AsString()}'.");

            typeStack.Pop();
        }

        private void InitializeTypes(Registration reg)
        {
            if (reg.Type.IsEnumerable())
                InitializeList(reg);
            else
                InitializeType(reg);

            if (reg.Lifestyle == Lifestyle.Undefined)
                reg.Lifestyle = (defaultLifestyle == DefaultLifestyle.Singleton)
                    ? Lifestyle.Singleton : Lifestyle.Transient;

            if (reg.Lifestyle == Lifestyle.Singleton)
            {
                var instance = reg.Factory();
                reg.Factory = () => instance;
                reg.Expression = Expression.Constant(instance);
            }
        }

        private void InitializeType(Registration reg)
        {
            if (reg.TypeConcrete == null)
                reg.TypeConcrete = allTypesConcrete.Value.FindConcreteType(reg.Type);

            var ctor = reg.TypeConcrete.GetConstructor();

            var parameters = ctor.GetParameters()
                .Select(GetValueOfParameter)
                .ToArray();

            Log($"Constructing type '{reg.TypeConcrete.AsString()}({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")})'.");
            reg.Expression = Expression.New(ctor, parameters);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();

            // local function
            Expression GetValueOfParameter(ParameterInfo p)
            {

                var type = p.ParameterType;
                if (type.IsByRef) 
                {
                    var readOnly= p.GetCustomAttributes().Any(x => x.ToString().EndsWith("IsReadOnlyAttribute"));
                    if (!readOnly)
                        throw new TypeAccessException($"Invalid ref or out type '{type.Name}'.");
                    type = type.GetElementType(); // support 'In' parameter type only
                }

                if (p.HasDefaultValue)
                    return Expression.Constant(p.DefaultValue, type);

                return GetOrAddExpression(type, reg);
            }
        }

        private void InitializeList(Registration reg)
        {
            var genericType = reg.Type.GenericTypeArguments.Single().GetTypeInfo();
            var assignableTypes = (defaultLifestyle == DefaultLifestyle.Undefined
                ? registrations.Values.Select(r => r.Type) : allTypesConcrete.Value)
                .Where(t => genericType.IsAssignableFrom(t)).ToList();
            if (!assignableTypes.Any())
                throw new TypeAccessException($"No types found assignable to generic type '{genericType.AsString()}'.");
            if (assignableTypes.Any(IsTransientRegistration))
            {
                if (reg.Lifestyle == Lifestyle.Singleton || reg.Lifestyle == Lifestyle.Instance)
                    throw new TypeAccessException($"Captive dependency: the singleton list '{reg.Type.AsString()}' depends on one of more transient items.");
                reg.Lifestyle = Lifestyle.Transient;
            }
            var expressions = assignableTypes.Select(t => GetOrAddExpression(t.AsType(), reg)).ToList();
            Log($"Constructing list of {expressions.Count} types assignable to '{genericType.AsString()}'.");
            reg.Expression = Expression.NewArrayInit(genericType.AsType(), expressions);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
        }

        private bool IsTransientRegistration(TypeInfo type)
            => (registrations.TryGetValue(type.AsType(), out Registration reg) ||
                (type.GetFuncArgumentType(out TypeInfo funcType) && registrations.TryGetValue(funcType.AsType(), out reg)))
                && (reg.Lifestyle == Lifestyle.Transient || reg.Lifestyle == Lifestyle.Factory);

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
        /// Disposing the container disposes any registered disposable singletons and instances.
        /// </summary>
        public void Dispose()
        {
            foreach (var disposable in registrations.Values
                .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Instance)
                .Where(r => r.Factory != null)
                .Select(r => r.Factory())
                .Where(i => i != null && i != this)
                .OfType<IDisposable>())
            {
                Log($"Disposing type '{disposable.GetType().AsString()}'.");
                disposable.Dispose();
            }
            registrations.Clear();
            Log("Container disposed.");
        }
    }

}
