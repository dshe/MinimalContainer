/*
StandardContainer version 2.13
https://github.com/dshe/StandardContainer
Copyright(c) 2017 DavidS.
Licensed under the Apache License 2.0:
http://www.apache.org/licenses/LICENSE-2.0
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

        public object Resolve(Type type)
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

        private Expression GetOrAddExpression(Type type, Registration dependent)
        {
            (Registration reg, bool isFunc) = GetOrAddInitializeRegistration(type, dependent);
            return isFunc ? Expression.Lambda(reg.Expression) : reg.Expression;
        }

        private (Registration reg, bool isFunc) GetOrAddInitializeRegistration(Type type, Registration dependent)
        {
            (Registration reg, bool isFunc) = GetOrAddRegistration(type);
            if (reg.Expression == null)
                Initialize(reg, dependent, isFunc);
            return (reg, isFunc);
        }

        private (Registration reg, bool isFunc) GetOrAddRegistration(Type type)
        {
            var isFunc = false;
            if (registrations.TryGetValue(type, out Registration reg))
                return (reg, isFunc);
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.GetFuncArgumentType(out TypeInfo funcType))
            {
                isFunc = true;
                typeInfo = funcType;
                if (registrations.TryGetValue(typeInfo.AsType(), out reg))
                {
                    if (reg.Lifestyle == Lifestyle.Singleton || reg.Lifestyle == Lifestyle.Instance)
                        throw new TypeAccessException($"Func argument type '{typeInfo.AsString()}' must be Transient or Factory.");
                    return (reg, isFunc);
                }
            }
            if (defaultLifestyle == DefaultLifestyle.Undefined && !typeInfo.IsEnumerable())
                throw new TypeAccessException($"Cannot resolve unregistered type '{typeInfo.AsString()}'.");
            reg = AddRegistration(typeInfo);
            if (isFunc)
                reg.Lifestyle = Lifestyle.Transient;
            return (reg, isFunc);
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
                .Select(p => p.HasDefaultValue ? Expression.Constant(p.DefaultValue, p.ParameterType) : GetOrAddExpression(p.ParameterType, reg))
                .ToArray();
            Log($"Constructing type '{reg.TypeConcrete.AsString()}({parameters.Select(p => p?.Type.AsString()).JoinStrings(", ")})'.");
            reg.Expression = Expression.New(ctor, parameters);
            reg.Factory = Expression.Lambda<Func<object>>(reg.Expression).Compile();
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

    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructorAttribute : Attribute {}

    internal static class StandardContainerEx
    {
        internal static TypeInfo FindConcreteType(this List<TypeInfo> allTypesConcrete, TypeInfo type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!type.IsAbstract && !type.IsInterface)
                return type;
            // When a non-concrete type is indicated, the concrete type is determined automatically.
            var assignableTypes = allTypesConcrete.Where(type.IsAssignableFrom).ToList(); // slow
            // The non-concrete type must be assignable to exactly one concrete type.
            if (assignableTypes.Count == 1)
                return assignableTypes.Single();
            var types = assignableTypes.Select(t => t.FullName).JoinStrings(", ");
            throw new TypeAccessException($"{assignableTypes.Count} concrete types found assignable to '{type.AsString()}': {types}.");
        }

        internal static bool GetFuncArgumentType(this TypeInfo type, out TypeInfo funcType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.IsGenericType && !type.IsGenericTypeDefinition &&
                typeof(Delegate).GetTypeInfo().IsAssignableFrom(type.BaseType?.GetTypeInfo()))
            {
                funcType = type.GenericTypeArguments.SingleOrDefault()?.GetTypeInfo();
                return true;
            }
            funcType = null;
            return false;
        }

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
                .Select(a => a.GetTypeInfo().AsString())
                .JoinStrings(",");
            return $"{name}<{args}>";
        }
    }
}
