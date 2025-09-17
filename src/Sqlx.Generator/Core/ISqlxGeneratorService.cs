// -----------------------------------------------------------------------
// <copyright file="ISqlxGeneratorService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Unified service interface for Sqlx code generation.
/// </summary>
public interface ISqlxGeneratorService
{
    /// <summary>
    /// Infers the entity type from a service interface.
    /// </summary>
    INamedTypeSymbol? InferEntityTypeFromInterface(INamedTypeSymbol serviceInterface);

    /// <summary>
    /// Infers the entity type from a method.
    /// </summary>
    INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method);

    /// <summary>
    /// Gets the service interface from repository class.
    /// </summary>
    INamedTypeSymbol? GetServiceInterface(INamedTypeSymbol repositoryClass, Compilation compilation);

    /// <summary>
    /// Gets the table name for an entity type.
    /// </summary>
    string GetTableName(INamedTypeSymbol? entityType, INamedTypeSymbol? tableNameAttributeSymbol);

    /// <summary>
    /// Analyzes a method to determine its operation characteristics.
    /// </summary>
    MethodAnalysisResult AnalyzeMethod(IMethodSymbol method);

    /// <summary>
    /// Generates repository implementation.
    /// </summary>
    void GenerateRepositoryImplementation(GenerationContext context);

    /// <summary>
    /// Generates or copies attributes for a method.
    /// </summary>
    void GenerateAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName);
}
