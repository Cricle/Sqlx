// -----------------------------------------------------------------------
// <copyright file="RepositoryMethodContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Context for repository method generation.
/// </summary>
public class RepositoryMethodContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryMethodContext"/> class.
    /// </summary>
    public RepositoryMethodContext(
        IndentedStringBuilder stringBuilder,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string tableName,
        IOperationGenerator operationGenerator,
        IAttributeHandler attributeHandler,
        IMethodAnalyzer methodAnalyzer)
    {
        StringBuilder = stringBuilder;
        Method = method;
        EntityType = entityType;
        TableName = tableName;
        OperationGenerator = operationGenerator;
        AttributeHandler = attributeHandler;
        MethodAnalyzer = methodAnalyzer;
    }

    /// <summary>
    /// Gets the string builder.
    /// </summary>
    public IndentedStringBuilder StringBuilder { get; }

    /// <summary>
    /// Gets the method symbol.
    /// </summary>
    public IMethodSymbol Method { get; }

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public INamedTypeSymbol? EntityType { get; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the operation generator.
    /// </summary>
    public IOperationGenerator OperationGenerator { get; }

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    public IAttributeHandler AttributeHandler { get; }

    /// <summary>
    /// Gets the method analyzer.
    /// </summary>
    public IMethodAnalyzer MethodAnalyzer { get; }
}
