using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MinimalContainer
{
    /// <summary>
    /// The container can create instances of types using public and internal constructors. 
    /// In case a type has more than one constructor, indicate the constructor to be used with the ContainerConstructor attribute.
    /// Otherwise, the constructor with the smallest number of arguments is selected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class ContainerConstructor : Attribute { }

    internal static class MinimalContainerEx
    {
        internal static TypeInfo FindConcreteType(this List<TypeInfo> allTypesConcrete, TypeInfo type)
        {
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

        internal static bool GetFuncArgumentType(this TypeInfo type, out TypeInfo? funcType)
        {
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
            var ctors = type.DeclaredConstructors.Where(c => !c.IsPrivate).ToList();
            if (ctors.Count == 1)
                return ctors.Single();
            if (!ctors.Any())
                throw new TypeAccessException($"Type '{type.AsString()}' has no public or internal constructor.");

            var ctorsWithAttribute = ctors.Where(c => c.GetCustomAttribute<ContainerConstructor>() != null).ToList();
            if (ctorsWithAttribute.Count == 1)
                return ctorsWithAttribute.Single();
            if (ctorsWithAttribute.Count > 1)
                throw new TypeAccessException($"Type '{type.AsString()}' has more than one constructor decorated with '{nameof(ContainerConstructor)}'.");

             return ctors.OrderBy(c => c.GetParameters().Length).First();
        }

        internal static bool IsEnumerable(this TypeInfo type) => typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type);

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

        public static void Then(this bool b, Action action)
        {
             if (b) action();
        }
    }
}
