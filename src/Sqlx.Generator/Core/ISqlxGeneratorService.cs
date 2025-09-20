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
    /// Gets the type inference service.
    /// </summary>
    ITypeInferenceService TypeInferenceService { get; }

    /// <summary>
    /// Gets the code generation service.
    /// </summary>
    ICodeGenerationService CodeGenerationService { get; }

    /// <summary>
    /// Gets the template engine.
    /// </summary>
    ISqlTemplateEngine TemplateEngine { get; }

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    AttributeHandler AttributeHandler { get; }

    /// <summary>
    /// Gets the method analyzer.
    /// </summary>
    MethodAnalyzer MethodAnalyzer { get; }
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
