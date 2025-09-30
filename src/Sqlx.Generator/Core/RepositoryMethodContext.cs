// -----------------------------------------------------------------------
// <copyright file="RepositoryMethodContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator;

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
        string processedSql,
        AttributeHandler attributeHandler,
        INamedTypeSymbol classSymbol)
    {
        StringBuilder = stringBuilder;
        Method = method;
        EntityType = entityType;
        TableName = tableName;
        ProcessedSql = processedSql;
        AttributeHandler = attributeHandler;
        ClassSymbol = classSymbol;
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
    /// Gets the processed SQL.
    /// </summary>
    public string ProcessedSql { get; }

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    public AttributeHandler AttributeHandler { get; }

    /// <summary>
    /// Gets the class symbol.
    /// </summary>
    public INamedTypeSymbol ClassSymbol { get; }
}
