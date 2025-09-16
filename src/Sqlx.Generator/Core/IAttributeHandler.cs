// -----------------------------------------------------------------------
// <copyright file="IAttributeHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Interface for handling method attributes.
/// </summary>
public interface IAttributeHandler
{
    /// <summary>
    /// Generates or copies attributes for a method.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="method">The method.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="tableName">The table name.</param>
    void GenerateOrCopyAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName);

    /// <summary>
    /// Generates a Sqlx attribute for a method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns>The attribute string.</returns>
    string GenerateSqlxAttribute(IMethodSymbol method, INamedTypeSymbol? entityType, string tableName);

    /// <summary>
    /// Generates an attribute from existing attribute data.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The attribute string.</returns>
    string GenerateSqlxAttribute(AttributeData attribute);
}
