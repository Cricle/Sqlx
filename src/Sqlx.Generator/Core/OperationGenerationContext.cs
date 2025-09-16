// -----------------------------------------------------------------------
// <copyright file="OperationGenerationContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Context for operation generation containing all necessary information.
/// </summary>
public class OperationGenerationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationGenerationContext"/> class.
    /// </summary>
    public OperationGenerationContext(
        IndentedStringBuilder stringBuilder,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string tableName,
        bool isAsync,
        string methodName)
    {
        StringBuilder = stringBuilder;
        Method = method;
        EntityType = entityType;
        TableName = tableName;
        IsAsync = isAsync;
        MethodName = methodName;
    }

    /// <summary>
    /// Gets the string builder for code generation.
    /// </summary>
    public IndentedStringBuilder StringBuilder { get; }

    /// <summary>
    /// Gets the method symbol.
    /// </summary>
    public IMethodSymbol Method { get; }

    /// <summary>
    /// Gets the entity type symbol.
    /// </summary>
    public INamedTypeSymbol? EntityType { get; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets a value indicating whether the operation is async.
    /// </summary>
    public bool IsAsync { get; }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string MethodName { get; }
}
