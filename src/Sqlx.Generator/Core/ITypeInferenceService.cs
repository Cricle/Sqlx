// -----------------------------------------------------------------------
// <copyright file="ITypeInferenceService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Interface for type inference services.
/// </summary>
public interface ITypeInferenceService
{
    /// <summary>
    /// Infers the entity type from a service interface.
    /// </summary>
    /// <param name="serviceInterface">The service interface.</param>
    /// <returns>The inferred entity type.</returns>
    INamedTypeSymbol? InferEntityTypeFromServiceInterface(INamedTypeSymbol serviceInterface);

    /// <summary>
    /// Infers the entity type from a method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>The inferred entity type.</returns>
    INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method);

    /// <summary>
    /// Gets the service interface from repository class.
    /// </summary>
    /// <param name="repositoryClass">The repository class.</param>
    /// <param name="compilation">The compilation context.</param>
    /// <returns>The service interface.</returns>
    INamedTypeSymbol? GetServiceInterfaceFromSyntax(INamedTypeSymbol repositoryClass, Compilation compilation);

    /// <summary>
    /// Gets the table name for an entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="tableNameAttributeSymbol">The table name attribute symbol.</param>
    /// <returns>The table name.</returns>
    string GetTableNameFromEntity(INamedTypeSymbol? entityType, INamedTypeSymbol? tableNameAttributeSymbol);

    /// <summary>
    /// Gets the table name for a repository class and service type.
    /// </summary>
    /// <param name="repositoryClass">The repository class.</param>
    /// <param name="serviceType">The service type.</param>
    /// <param name="tableNameAttributeSymbol">The table name attribute symbol.</param>
    /// <returns>The table name.</returns>
    string GetTableName(INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceType, INamedTypeSymbol? tableNameAttributeSymbol);
}
