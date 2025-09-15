// -----------------------------------------------------------------------
// <copyright file="TypeAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sqlx;

/// <summary>
/// Simple type analyzer for source generation.
/// Focus on ADO.NET types. Cache APIs are retained as no-ops for test compatibility.
/// </summary>
internal static class TypeAnalyzer
{
    /// <summary>
    /// Determines if a type is likely an entity type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLikelyEntityType(ITypeSymbol? type)
    {
        if (type == null) return false;

        // Skip primitive and system types
        if (type.SpecialType != SpecialType.None) return false;

        // Skip system namespaces
        var namespaceName = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (IsSystemNamespace(namespaceName)) return false;

        // Must be a class with properties
        return type.TypeKind == TypeKind.Class &&
               type.GetMembers().OfType<IPropertySymbol>().Any();
    }

    /// <summary>
    /// Determines if a type is a collection type (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return false;

        var typeName = namedType.Name;
        return typeName is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList" ||
               (namedType.IsGenericType &&
                (typeName is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList"));
    }

    /// <summary>
    /// Extracts entity type from generic collections or Task types (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static INamedTypeSymbol? ExtractEntityType(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType) return null;

        // Handle Task<T> and Task<IList<T>>
        if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
        {
            var taskInnerType = namedType.TypeArguments[0];
            if (taskInnerType is INamedTypeSymbol taskInner)
            {
                // Task<IList<T>> -> T
                if (IsCollectionType(taskInner) && taskInner.TypeArguments.Length == 1)
                {
                    return taskInner.TypeArguments[0] as INamedTypeSymbol;
                }
                // Task<T> -> T
                return taskInner;
            }
        }

        // Handle IList<T>, List<T>, IEnumerable<T>
        if (IsCollectionType(namedType) && namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0] as INamedTypeSymbol;
        }

        // Direct type
        return IsLikelyEntityType(namedType) ? namedType : null;
    }

    /// <summary>
    /// Determines if a type is async (Task or Task&lt;T&gt;) (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsyncType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return false;
        return namedType.Name == "Task";
    }

    /// <summary>
    /// Gets the inner type of Task&lt;T&gt; or returns the original type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ITypeSymbol GetInnerType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.Name == "Task" &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }
        return type;
    }

    /// <summary>
    /// Determines if a method returns a scalar value (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsScalarReturnType(ITypeSymbol returnType, bool isAsync)
    {
        var actualType = isAsync ? GetInnerType(returnType) : returnType;
        return actualType.SpecialType switch
        {
            SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Boolean
            or SpecialType.System_String or SpecialType.System_Decimal or SpecialType.System_Double
            or SpecialType.System_Single or SpecialType.System_Byte or SpecialType.System_Int16 => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a default value expression for a type (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDefaultValue(ITypeSymbol type)
    {
        return ComputeDefaultValue(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ComputeDefaultValue(ITypeSymbol type)
    {
        if (IsAsyncType(type))
        {
            var innerType = GetInnerType(type);
            if (innerType.SpecialType == SpecialType.System_Void)
            {
                return "global::System.Threading.Tasks.Task.CompletedTask";
            }
            var innerDefault = GetDefaultValue(innerType);
            return $"global::System.Threading.Tasks.Task.FromResult<{innerType.ToDisplayString()}>({innerDefault})";
        }

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Int32 => "0",
            SpecialType.System_Int64 => "0L",
            SpecialType.System_String => "string.Empty",
            SpecialType.System_Void => "",
            _ when type.CanBeReferencedByName && type.IsReferenceType => "null!",
            _ when type.CanBeReferencedByName => "default",
            _ => "null!"
        };
    }

    /// <summary>
    /// Checks if a namespace is a system namespace (cached).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSystemNamespace(string namespaceName)
    {
        return namespaceName.StartsWith("System", StringComparison.Ordinal) ||
               namespaceName.StartsWith("Microsoft", StringComparison.Ordinal);
    }

}
