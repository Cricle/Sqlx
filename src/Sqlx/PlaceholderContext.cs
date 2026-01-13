// <copyright file="PlaceholderContext.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;

/// <summary>
/// Context for placeholder processing.
/// </summary>
public sealed class PlaceholderContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderContext"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The entity columns.</param>
    /// <param name="dynamicParameters">Optional dynamic parameters for Render phase.</param>
    public PlaceholderContext(
        SqlDialect dialect,
        string tableName,
        IReadOnlyList<ColumnMeta> columns,
        IReadOnlyDictionary<string, object?>? dynamicParameters = null)
    {
        Dialect = dialect;
        TableName = tableName;
        Columns = columns;
        DynamicParameters = dynamicParameters;
    }

    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the entity columns.
    /// </summary>
    public IReadOnlyList<ColumnMeta> Columns { get; }

    /// <summary>
    /// Gets the dynamic parameters for Render phase.
    /// Key is parameter name, value is the parameter value.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? DynamicParameters { get; }

    /// <summary>
    /// Creates a new context with dynamic parameters for Render phase.
    /// </summary>
    /// <param name="dynamicParameters">The dynamic parameters.</param>
    /// <returns>A new context with the specified dynamic parameters.</returns>
    public PlaceholderContext WithDynamicParameters(IReadOnlyDictionary<string, object?> dynamicParameters)
    {
        return new PlaceholderContext(Dialect, TableName, Columns, dynamicParameters);
    }

    /// <summary>
    /// Gets a dynamic parameter value or throws if not found.
    /// </summary>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="placeholderName">The placeholder name for error message.</param>
    /// <returns>The parameter value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when parameter is not found.</exception>
    public object? GetDynamicParameterValue(string paramName, string placeholderName)
    {
        if (DynamicParameters is null || !DynamicParameters.TryGetValue(paramName, out var value))
        {
            throw new InvalidOperationException(
                $"Dynamic parameter '{paramName}' required by placeholder '{{{{{placeholderName}}}}}' was not provided.");
        }

        return value;
    }
}
