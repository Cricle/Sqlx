// -----------------------------------------------------------------------
// <copyright file="GenerationContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Unified context for code generation containing all necessary information.
/// </summary>
public class GenerationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationContext"/> class for repository generation.
    /// </summary>
    public GenerationContext(
        GeneratorExecutionContext executionContext,
        INamedTypeSymbol repositoryClass,
        ISqlxGeneratorService generatorService)
    {
        ExecutionContext = executionContext;
        RepositoryClass = repositoryClass;
        GeneratorService = generatorService;
        StringBuilder = new IndentedStringBuilder(string.Empty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationContext"/> class for method generation.
    /// </summary>
    public GenerationContext(
        GeneratorExecutionContext executionContext,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string tableName,
        ISqlxGeneratorService generatorService,
        IndentedStringBuilder stringBuilder)
    {
        ExecutionContext = executionContext;
        Method = method;
        EntityType = entityType;
        TableName = tableName;
        GeneratorService = generatorService;
        StringBuilder = stringBuilder;
        IsAsync = method.ReturnType.Name.Contains("Task");
        MethodName = method.Name;
    }

    /// <summary>
    /// Gets the execution context.
    /// </summary>
    public GeneratorExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Gets the repository class (if applicable).
    /// </summary>
    public INamedTypeSymbol? RepositoryClass { get; }

    /// <summary>
    /// Gets the method symbol (if applicable).
    /// </summary>
    public IMethodSymbol? Method { get; }

    /// <summary>
    /// Gets the entity type symbol.
    /// </summary>
    public INamedTypeSymbol? EntityType { get; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; } = string.Empty;

    /// <summary>
    /// Gets the unified generator service.
    /// </summary>
    public ISqlxGeneratorService GeneratorService { get; }

    /// <summary>
    /// Gets the string builder for code generation.
    /// </summary>
    public IndentedStringBuilder StringBuilder { get; }

    /// <summary>
    /// Gets a value indicating whether the operation is async.
    /// </summary>
    public bool IsAsync { get; }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string MethodName { get; } = string.Empty;

    /// <summary>
    /// Gets the compilation context.
    /// </summary>
    public Compilation Compilation => ExecutionContext.Compilation;
}
