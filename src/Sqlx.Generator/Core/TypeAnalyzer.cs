// -----------------------------------------------------------------------
// <copyright file="TypeAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx;

/// <summary>
/// Simplified type analyzer for source generation.
/// </summary>
internal static class TypeAnalyzer
{
    /// <summary>
    /// Determines if a type is likely an entity type.
    /// </summary>
    public static bool IsLikelyEntityType(ITypeSymbol? type)
    {
        if (type == null) return false;

        // Skip primitive and system types
        if (type.SpecialType != SpecialType.None) return false;

        // Skip system namespaces
        var namespaceName = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (namespaceName.StartsWith("System") ||
            namespaceName.StartsWith("Microsoft") ||
            string.IsNullOrEmpty(namespaceName)) return false;

        // Must be a class with properties
        return type.TypeKind == TypeKind.Class &&
               type.GetMembers().OfType<IPropertySymbol>().Any();
    }

    /// <summary>
    /// Determines if a type is a collection type.
    /// </summary>
    public static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return false;

        var typeName = namedType.Name;
        return typeName is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList" ||
               (namedType.IsGenericType &&
                (typeName is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList"));
    }


    /// <summary>
    /// Gets the inner type from async types (Task&lt;T&gt; -> T).
    /// </summary>
    public static ITypeSymbol GetInnerType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }
        return type;
    }

    /// <summary>
    /// Determines if a return type is scalar.
    /// </summary>
    public static bool IsScalarReturnType(ITypeSymbol type, bool isAsync)
    {
        var actualType = isAsync ? GetInnerType(type) : type;
        return actualType.SpecialType == SpecialType.System_Int32 ||
               actualType.SpecialType == SpecialType.System_Boolean ||
               actualType.SpecialType == SpecialType.System_Int64 ||
               actualType.SpecialType == SpecialType.System_Decimal ||
               actualType.SpecialType == SpecialType.System_Double ||
               actualType.SpecialType == SpecialType.System_String;
    }

    /// <summary>
    /// Extracts entity type from generic types.
    /// </summary>
    public static ITypeSymbol? ExtractEntityType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }
        return null;
    }

}
