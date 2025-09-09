// -----------------------------------------------------------------------
// <copyright file="TypeAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// High-performance type analyzer with caching for source generation.
/// </summary>
internal static class TypeAnalyzer
{
    private static readonly ConcurrentDictionary<ITypeSymbol, bool> _isEntityTypeCache = new();
    private static readonly ConcurrentDictionary<ITypeSymbol, bool> _isCollectionTypeCache = new();
    private static readonly ConcurrentDictionary<ITypeSymbol, INamedTypeSymbol?> _entityTypeCache = new();
    private static readonly ConcurrentDictionary<ITypeSymbol, bool> _isAsyncTypeCache = new();

    /// <summary>
    /// Determines if a type is likely an entity type (cached).
    /// </summary>
    public static bool IsLikelyEntityType(ITypeSymbol? type)
    {
        if (type == null) return false;

        return _isEntityTypeCache.GetOrAdd(type, static t =>
        {
            // Skip primitive and system types
            if (t.SpecialType != SpecialType.None) return false;

            // Skip system namespace types
            var namespaceName = t.ContainingNamespace?.ToDisplayString() ?? "";
            if (namespaceName.StartsWith("System", StringComparison.Ordinal)) return false;

            // Must be a class with properties
            return t.TypeKind == TypeKind.Class && 
                   t.GetMembers().OfType<IPropertySymbol>().Any();
        });
    }

    /// <summary>
    /// Determines if a type is a collection type (cached).
    /// </summary>
    public static bool IsCollectionType(ITypeSymbol type)
    {
        return _isCollectionTypeCache.GetOrAdd(type, static t =>
        {
            if (t is not INamedTypeSymbol namedType) return false;

            var typeName = namedType.Name;
            return typeName is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList" ||
                   (namedType.IsGenericType && 
                    (typeName is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList"));
        });
    }

    /// <summary>
    /// Extracts entity type from generic collections or Task types (cached).
    /// </summary>
    public static INamedTypeSymbol? ExtractEntityType(ITypeSymbol type)
    {
        return _entityTypeCache.GetOrAdd(type, static t =>
        {
            if (t is not INamedTypeSymbol namedType) return null;

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
        });
    }

    /// <summary>
    /// Determines if a type is async (Task or Task&lt;T&gt;) (cached).
    /// </summary>
    public static bool IsAsyncType(ITypeSymbol type)
    {
        return _isAsyncTypeCache.GetOrAdd(type, static t =>
        {
            if (t is not INamedTypeSymbol namedType) return false;
            return namedType.Name == "Task";
        });
    }

    /// <summary>
    /// Gets the inner type of Task&lt;T&gt; or returns the original type.
    /// </summary>
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
    /// Determines if a method returns a scalar value.
    /// </summary>
    public static bool IsScalarReturnType(ITypeSymbol returnType, bool isAsync)
    {
        var actualType = isAsync ? GetInnerType(returnType) : returnType;
        
        return actualType.SpecialType switch
        {
            SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Boolean 
            or SpecialType.System_String or SpecialType.System_Decimal or SpecialType.System_Double => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a default value expression for a type.
    /// </summary>
    public static string GetDefaultValue(ITypeSymbol type)
    {
        if (IsAsyncType(type))
        {
            var innerType = GetInnerType(type);
            if (innerType.SpecialType == SpecialType.System_Void)
            {
                return "global::System.Threading.Tasks.Task.CompletedTask";
            }
            var innerDefault = GetDefaultValue(innerType);
            return $"global::System.Threading.Tasks.Task.FromResult({innerDefault})";
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
    /// Clears all caches (useful for testing).
    /// </summary>
    public static void ClearCaches()
    {
        _isEntityTypeCache.Clear();
        _isCollectionTypeCache.Clear();
        _entityTypeCache.Clear();
        _isAsyncTypeCache.Clear();
    }
}
