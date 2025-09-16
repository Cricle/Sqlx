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
        INamedTypeSymbol sqlxAttributeSymbol,
        ITypeInferenceService typeInferenceService,
        ICodeGenerationService codeGenerationService,
        OperationGeneratorFactory operationFactory,
        IAttributeHandler attributeHandler,
        IMethodAnalyzer methodAnalyzer)
    {
        ExecutionContext = executionContext;
        RepositoryClass = repositoryClass;
        RepositoryForAttributeSymbol = repositoryForAttributeSymbol;
        TableNameAttributeSymbol = tableNameAttributeSymbol;
        SqlxAttributeSymbol = sqlxAttributeSymbol;
        TypeInferenceService = typeInferenceService;
        CodeGenerationService = codeGenerationService;
        OperationFactory = operationFactory;
        AttributeHandler = attributeHandler;
        MethodAnalyzer = methodAnalyzer;
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
    /// Gets the Sqlx attribute symbol.
    /// </summary>
    public INamedTypeSymbol SqlxAttributeSymbol { get; }

    /// <summary>
    /// Gets the type inference service.
    /// </summary>
    public ITypeInferenceService TypeInferenceService { get; }

    /// <summary>
    /// Gets the code generation service.
    /// </summary>
    public ICodeGenerationService CodeGenerationService { get; }

    /// <summary>
    /// Gets the operation factory.
    /// </summary>
    public OperationGeneratorFactory OperationFactory { get; }

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    public IAttributeHandler AttributeHandler { get; }

    /// <summary>
    /// Gets the method analyzer.
    /// </summary>
    public IMethodAnalyzer MethodAnalyzer { get; }
}
