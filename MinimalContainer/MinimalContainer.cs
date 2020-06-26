using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MinimalContainer
{
    public enum DefaultLifestyle { Undefined, Transient, Singleton }
    internal enum Lifestyle { Undefined, Transient, Singleton, Instance, Factory }

    internal sealed class Registration
    {
        internal TypeInfo Type;
        internal TypeInfo? TypeConcrete;
        internal Lifestyle Lifestyle;
        internal Func<object>? Factory;
        internal Expression? Expression;
        internal Delegate? FuncDelegate;
        internal Registration(TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.AsType() == typeof(string) || (!type.IsClass && !type.IsInterface))
                throw new TypeAccessException("Type is neither a class nor an interface.");
            Type = type;
        }
        public override string ToString() =>
            $"{(TypeConcrete == null || Equals(TypeConcrete, Type) ? "" : TypeConcrete.AsString() + "->")}{Type.AsString()}, {Lifestyle}.";
    }

    public sealed class Container : IDisposable
    {
        private readonly DefaultLifestyle DefaultLifestyle;
        private readonly Lazy<List<TypeInfo>> AllTypesConcrete;
        private readonly Dictionary<Type, Registration> Registrations = new Dictionary<Type, Registration>();
        private readonly Stack<TypeInfo> TypeStack = new Stack<TypeInfo>();
        private readonly ILogger Logger;

        public Container(DefaultLifestyle defaultLifestyle = DefaultLifestyle.Undefined, ILogger<Container>? logger = null, params Assembly[]? assemblies)
        {
            Logger = logger ?? NullLogger<Container>.Instance;
            Logger.LogInformation("Constructing Container.");

            DefaultLifestyle = defaultLifestyle;

            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));
            var assemblyList = assemblies.ToList();
            if (!assemblyList.Any())
                assemblyList.Add(Assembly.GetCallingAssembly());

            AllTypesConcrete = new Lazy<List<TypeInfo>>(() => assemblyList
                .Select(a => a.DefinedTypes.Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface))
                .SelectMany(x => x)
                .ToList());
        }

        public Container RegisterTransient<T>() => RegisterTransient(typeof(T));
        public Container RegisterTransient<T, TConcrete>() where TConcrete : T => RegisterTransient(typeof(T), typeof(TConcrete));
        public Container RegisterTransient(Type type, Type? typeConcrete = null) => Register(type, typeConcrete, Lifestyle.Transient);

        public Container RegisterSingleton<T>() => RegisterSingleton(typeof(T));
        public Container RegisterSingleton<T, TConcrete>() where TConcrete : T => RegisterSingleton(typeof(T), typeof(TConcrete));
        public Container RegisterSingleton(Type type, Type? typeConcrete = null) => Register(type, typeConcrete, Lifestyle.Singleton);

        public Container RegisterInstance<T>(T instance) => RegisterInstance(typeof(T), instance);
        public Container RegisterInstance(Type type, object? instance) => Register(type, null, Lifestyle.Instance, instance);

        public Container RegisterFactory<T>(Func<T> factory) where T : class => RegisterFactory(typeof(T), factory);
        public Container RegisterFactory(Type type, Func<object> factory) => Register(type, null, Lifestyle.Factory, null, factory);

        private Container Register(Type type, Type? typeConcrete, Lifestyle lifestyle, object? instance = null, Func<object>? factory = null, [CallerMemberName] string? caller = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var reg = AddRegistration(type.GetTypeInfo());
            Logger.LogInformation($"Registering {caller}: {reg},");
            reg.TypeConcrete = typeConcrete?.GetTypeInfo();
            reg.Lifestyle = lifestyle;
            if (lifestyle == Lifestyle.Instance)
            {
                if (instance == null)
                    throw new ArgumentNullException(nameof(instance));
                object notNullInstance = instance;
                reg.Factory = () => notNullInstance;
                reg.Expression = Expression.Constant(instance);
            }
            else if (lifestyle == Lifestyle.Factory)
            {
                reg.Factory = factory ?? throw new ArgumentNullException(nameof(factory));
                reg.Expression = Expression.Call(Expression.Constant(factory.Target), factory.GetMethodInfo());
            }
            return this;
        }

        //////////////////////////////////////////////////////////////////////////////

        private Registration AddRegistration(TypeInfo type)
        {
            try
            {
                var reg = new Registration(type);
                Registrations.Add(type.AsType(), reg);
                return reg;
            }
            catch (ArgumentException ex)
            {
                throw new TypeAccessException($"Type '{type.AsString()}' is already registered.", ex);
            }
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
                {
                    var factory = reg.Factory ?? throw new Exception("Factory is null.");
                    return factory();
                }
                return reg.FuncDelegate ??= Expression.Lambda(reg.Expression).Compile();
            }
            catch (TypeAccessException ex)
            {
                if (!TypeStack.Any())
                    TypeStack.Push(type.GetTypeInfo());
                var typePath = TypeStack.Select(t => t.AsString()).JoinStrings("->");
                throw new TypeAccessException($"Could not get instance of type {typePath}. {ex.Message}", ex);
            }
        }

        private Expression GetOrAddExpression(in Type type, in Registration dependent)
        {
            (Registration reg, bool isFunc) = GetOrAddInitializeRegistration(type, dependent);
            var expression = reg.Expression ?? throw new Exception("Expression is null.");
            return isFunc ? Expression.Lambda(expression) : expression;
        }

        private (Registration reg, bool isFunc) GetOrAddInitializeRegistration(Type type, in Registration? dependent)
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
                if (Registrations.TryGetValue(type, out reg))
                    return;
                TypeInfo typeInfo = type.GetTypeInfo();
                if (typeInfo.GetFuncArgumentType(out TypeInfo? funcType))
                {
                    isFunc = true;
                    typeInfo = funcType ?? throw new NullReferenceException();
                    if (Registrations.TryGetValue(typeInfo.AsType(), out reg))
                    {
                        if (reg.Lifestyle == Lifestyle.Singleton || reg.Lifestyle == Lifestyle.Instance)
                            throw new TypeAccessException($"Func argument type '{typeInfo.AsString()}' must be Transient or Factory.");
                        return;
                    }
                }
                if (DefaultLifestyle == DefaultLifestyle.Undefined && !typeInfo.IsEnumerable())
                    throw new TypeAccessException($"Cannot resolve unregistered type '{typeInfo.AsString()}'.");
                reg = AddRegistration(typeInfo);
                if (isFunc)
                    reg.Lifestyle = Lifestyle.Transient;
            }
        }

        private void Initialize(Registration reg, Registration? dependent, bool isFunc)
        {
            if (dependent == null)
            {
                TypeStack.Clear();
                Logger.LogDebug($"Getting instance of type: '{reg.Type.AsString()}'.");
            }
            else if (reg.Lifestyle == Lifestyle.Undefined && (dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance))
                reg.Lifestyle = Lifestyle.Singleton;

            TypeStack.Push(reg.Type);
            if (TypeStack.Count(t => t.Equals(reg.Type)) != 1)
                throw new TypeAccessException("Recursive dependency.");

            InitializeTypes();

            if ((dependent?.Lifestyle == Lifestyle.Singleton || dependent?.Lifestyle == Lifestyle.Instance)
                && (reg.Lifestyle == Lifestyle.Transient || reg.Lifestyle == Lifestyle.Factory) && !isFunc)
                throw new TypeAccessException($"Captive dependency: the singleton '{dependent?.Type.AsString()}' depends on transient '{reg.Type.AsString()}'.");

            TypeStack.Pop();

            // local function
            void InitializeTypes()
            {
                if (reg.Type.IsEnumerable())
                    InitializeList(reg);
                else
                    InitializeType(reg);

                if (reg.Lifestyle == Lifestyle.Undefined)
                    reg.Lifestyle = (DefaultLifestyle == DefaultLifestyle.Singleton)
                        ? Lifestyle.Singleton : Lifestyle.Transient;

                if (reg.Lifestyle == Lifestyle.Singleton)
                {
                    if (reg.Factory == null)
                        throw new Exception("Factory is null.");
                    var instance = reg.Factory();
                    reg.Factory = () => instance;
                    reg.Expression = Expression.Constant(instance);
                }
            }
        }

        private void InitializeType(Registration reg)
        {
            if (reg.TypeConcrete == null)
                reg.TypeConcrete = AllTypesConcrete.Value.FindConcreteType(reg.Type);

            var ctor = reg.TypeConcrete.GetConstructor();

            var parameters = ctor.GetParameters()
                .Select(GetValueOfParameter)
                .ToArray();

            Logger.LogDebug($"Constructing type '{reg.TypeConcrete.AsString()}" +
                $"({parameters.Select(p => (p == null ? "" : p.Type.AsString())).JoinStrings(", ")})'.");

            reg.Expression = Expression.New(ctor, parameters);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();

            // local function
            Expression GetValueOfParameter(ParameterInfo p)
            {
                var type = p.ParameterType;
                if (type.IsByRef) 
                {
                    if (!p.GetCustomAttributes().Any(x => x.ToString().EndsWith("IsReadOnlyAttribute")))
                        throw new TypeAccessException($"Invalid ref or out parameter type '{type.Name}'.");
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

            var assignableTypes = (DefaultLifestyle == DefaultLifestyle.Undefined
                ? Registrations.Values.Select(r => r.Type) : AllTypesConcrete.Value)
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
            Logger.LogDebug($"Constructing list of {expressions.Count} types assignable to '{genericType.AsString()}'.");
            reg.Expression = Expression.NewArrayInit(genericType.AsType(), expressions);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();

            // local function
            bool IsTransientRegistration(TypeInfo type) =>
                (Registrations.TryGetValue(type.AsType(), out Registration r) ||
                (type.GetFuncArgumentType(out TypeInfo? funcType) && funcType != null && Registrations.TryGetValue(funcType.AsType(), out r))) &&
                (r.Lifestyle == Lifestyle.Transient || r.Lifestyle == Lifestyle.Factory);
        }

        //////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            var regs = Registrations.Values.Select(r => r.ToString()).ToList();

            return new StringBuilder()
                .AppendLine($"Container: {DefaultLifestyle}, {regs.Count} registered types:")
                .AppendLine(regs.JoinStrings(Environment.NewLine))
                .ToString();
        }

        /// <summary>
        /// Disposing the container disposes any registered disposable singletons and instances.
        /// </summary>
        public void Dispose()
        {
            foreach (var disposable in Registrations.Values
                .Where(r => r.Lifestyle == Lifestyle.Singleton || r.Lifestyle == Lifestyle.Instance)
                .Select(r => r.Factory)
                .Where(f => f != null)
                .Select(f => new Func<object>(f!))
                .Select(f => f())
                .Where(i => i != null && i != this)
                .OfType<IDisposable>())
            {
                Logger.LogTrace($"Disposing type '{disposable.GetType().AsString()}'.");
                disposable.Dispose();
            }

            Registrations.Clear();
            Logger.LogTrace("Container disposed.");
        }
    }
}
