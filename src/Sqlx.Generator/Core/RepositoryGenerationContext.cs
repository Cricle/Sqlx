// -----------------------------------------------------------------------
// <copyright file="RepositoryGenerationContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Context for repository generation.
/// </summary>
public class RepositoryGenerationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryGenerationContext"/> class.
    /// </summary>
    public RepositoryGenerationContext(
        GeneratorExecutionContext executionContext,
        INamedTypeSymbol repositoryClass,
        INamedTypeSymbol? repositoryForAttributeSymbol,
        INamedTypeSymbol? tableNameAttributeSymbol,
        ITypeInferenceService typeInferenceService,
        ISqlTemplateEngine templateEngine,
        AttributeHandler attributeHandler)
    {
        ExecutionContext = executionContext;
        RepositoryClass = repositoryClass;
        RepositoryForAttributeSymbol = repositoryForAttributeSymbol;
        TableNameAttributeSymbol = tableNameAttributeSymbol;
        TypeInferenceService = typeInferenceService;
        TemplateEngine = templateEngine;
        AttributeHandler = attributeHandler;
    }

    /// <summary>
    /// Gets the execution context.
    /// </summary>
    public GeneratorExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Gets the repository class.
    /// </summary>
    public INamedTypeSymbol RepositoryClass { get; }

    /// <summary>
    /// Gets the repository for attribute symbol.
    /// </summary>
    public INamedTypeSymbol? RepositoryForAttributeSymbol { get; }

    /// <summary>
    /// Gets the table name attribute symbol.
    /// </summary>
    public INamedTypeSymbol? TableNameAttributeSymbol { get; }

    /// <summary>
    /// Gets the type inference service.
    /// </summary>
    public ITypeInferenceService TypeInferenceService { get; }

    /// <summary>
    /// Gets the template engine.
    /// </summary>
    public ISqlTemplateEngine TemplateEngine { get; }

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    public AttributeHandler AttributeHandler { get; }
}
