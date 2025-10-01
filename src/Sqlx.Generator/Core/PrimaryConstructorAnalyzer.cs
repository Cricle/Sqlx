// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sqlx;

/// <summary>
/// Analyzer for primary constructors and record types in C# 12+.
/// </summary>
internal static class PrimaryConstructorAnalyzer
{
    /// <summary>
    /// Determines if a type is a record type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRecord(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Class && type.IsRecord;
    }

    /// <summary>
    /// Determines if a type has a primary constructor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasPrimaryConstructor(INamedTypeSymbol type)
    {
        // Check if the type has any constructor that could be a primary constructor
        var constructors = type.Constructors;

        // Primary constructors are typically the first constructor and have parameters
        // that correspond to properties or fields
        foreach (var constructor in constructors)
        {
            if (constructor.Parameters.Length > 0 && IsPrimaryConstructor(constructor, type))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the primary constructor of a type, if it exists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMethodSymbol? GetPrimaryConstructor(INamedTypeSymbol type)
    {
        var constructors = type.Constructors;

        foreach (var constructor in constructors)
        {
            if (constructor.Parameters.Length > 0 && IsPrimaryConstructor(constructor, type))
            {
                return constructor;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if a constructor is likely a primary constructor.
    /// </summary>
    private static bool IsPrimaryConstructor(IMethodSymbol constructor, INamedTypeSymbol containingType)
    {
        // For records, the synthesized constructor with all properties is the primary constructor
        if (IsRecord(containingType))
        {
            return constructor.Parameters.Length > 0;
        }

        if (constructor.Parameters.Length == 0)
            return false;

        // In C# 12+, primary constructors are marked with specific attributes or characteristics
        // Check if this is a compiler-generated primary constructor
        if (constructor.IsImplicitlyDeclared)
        {
            return true;
        }

        // Check if the constructor is declared in the class declaration syntax (primary constructor)
        // This is a heuristic: primary constructors typically appear first and have no body in source
        var allConstructors = containingType.Constructors.Where(c => !c.IsStatic).ToArray();
        if (allConstructors.Length > 0 && ReferenceEquals(constructor, allConstructors[0]))
        {
            // If this is the first constructor and it's not explicitly defined with a body,
            // it's likely a primary constructor
            return true;
        }

        // Fallback: check if constructor parameters match properties (old logic)
        var properties = containingType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName)
            .ToList();

        // Check if constructor parameters correspond to properties
        var matchingParams = 0;
        foreach (var param in constructor.Parameters)
        {
            var propertyName = GetPropertyNameFromParameter(param.Name);
            var correspondingProperty = properties.FirstOrDefault(p =>
                string.Equals(p.Name, propertyName, System.StringComparison.OrdinalIgnoreCase));

            if (correspondingProperty != null &&
                SymbolEqualityComparer.Default.Equals(param.Type, correspondingProperty.Type))
            {
                matchingParams++;
            }
        }

        // Consider it a primary constructor if most parameters have corresponding properties
        // OR if no properties exist but we have parameters (common for primary constructors)
        if (properties.Count == 0 && constructor.Parameters.Length > 0)
        {
            return true; // Likely a primary constructor with no corresponding properties
        }

        return matchingParams >= constructor.Parameters.Length * 0.7; // 70% threshold
    }

    /// <summary>
    /// Converts a parameter name to the corresponding property name (PascalCase).
    /// </summary>
    private static string GetPropertyNameFromParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return parameterName;

        // Convert camelCase to PascalCase
        return char.ToUpper(parameterName[0]) + parameterName.Substring(1);
    }
}
