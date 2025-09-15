// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sqlx.Core;

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
    /// Gets the parameters of the primary constructor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<IParameterSymbol> GetPrimaryConstructorParameters(INamedTypeSymbol type)
    {
        var primaryConstructor = GetPrimaryConstructor(type);
        return primaryConstructor?.Parameters ?? Enumerable.Empty<IParameterSymbol>();
    }

    /// <summary>
    /// Gets all accessible members (properties from primary constructor + regular properties).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<IMemberInfo> GetAccessibleMembers(INamedTypeSymbol type)
    {
        var members = new List<IMemberInfo>();

        // Add regular properties
        var properties = type.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName &&
                       (p.SetMethod != null || IsRecord(type)) &&
                       p.Name != "EqualityContract"); // Exclude Record's EqualityContract

        foreach (var prop in properties)
        {
            members.Add(new PropertyMemberInfo(prop));
        }

        // For records or types with primary constructors, ensure all primary constructor parameters
        // are included, either as properties or as parameters
        if (IsRecord(type) || HasPrimaryConstructor(type))
        {
            var primaryConstructorParams = GetPrimaryConstructorParameters(type);
            var existingPropertyNames = new HashSet<string>(properties.Select(p => p.Name), System.StringComparer.OrdinalIgnoreCase);

            foreach (var param in primaryConstructorParams)
            {
                var propertyName = GetPropertyNameFromParameter(param.Name);

                // Check if there's already a property with this name
                var existingProperty = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, propertyName, System.StringComparison.OrdinalIgnoreCase));

                if (existingProperty == null)
                {
                    // Add parameter as a member if no corresponding property exists
                    members.Add(new PrimaryConstructorParameterMemberInfo(param, propertyName));
                }
                // If property exists, it's already added above, so we're good
            }
        }

        return members;
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

/// <summary>
/// Represents information about a member (property or primary constructor parameter).
/// </summary>
internal abstract class IMemberInfo
{
    public abstract string Name { get; }
    public abstract ITypeSymbol Type { get; }
    public abstract bool CanWrite { get; }
    public abstract bool IsFromPrimaryConstructor { get; }
    public abstract string GetAccessExpression(string instanceName);
}

/// <summary>
/// Member info for regular properties.
/// </summary>
internal class PropertyMemberInfo : IMemberInfo
{
    private readonly IPropertySymbol _property;

    public PropertyMemberInfo(IPropertySymbol property)
    {
        _property = property;
    }

    public override string Name => _property.Name;
    public override ITypeSymbol Type => _property.Type;
    public override bool CanWrite => _property.SetMethod != null;
    public override bool IsFromPrimaryConstructor => false;

    public override string GetAccessExpression(string instanceName)
    {
        return $"{instanceName}.{Name}";
    }

    public IPropertySymbol Property => _property;
}

/// <summary>
/// Member info for primary constructor parameters.
/// </summary>
internal class PrimaryConstructorParameterMemberInfo : IMemberInfo
{
    private readonly IParameterSymbol _parameter;
    private readonly string _propertyName;

    public PrimaryConstructorParameterMemberInfo(IParameterSymbol parameter, string propertyName)
    {
        _parameter = parameter;
        _propertyName = propertyName;
    }

    public override string Name => _propertyName;
    public override ITypeSymbol Type => _parameter.Type;
    public override bool CanWrite => true; // Primary constructor parameters are typically writable
    public override bool IsFromPrimaryConstructor => true;

    public override string GetAccessExpression(string instanceName)
    {
        return $"{instanceName}.{Name}";
    }

    public IParameterSymbol Parameter => _parameter;
}
