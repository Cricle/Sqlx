// <copyright file="TablePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;

/// <summary>
/// Handles the {{table}} placeholder for generating quoted table names.
/// </summary>
/// <remarks>
/// <para>
/// This handler can be either static (default) or dynamic (with --param).
/// For dynamic table names, it generates parameterized SQL for runtime table selection.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description>No options - Uses static table name from context</description></item>
/// <item><description><c>--param name</c> - Dynamic table name resolved at render time</description></item>
/// </list>
/// <para><strong>Security Warning:</strong></para>
/// <para>
/// Dynamic table names should only be used with trusted input or validated against
/// a whitelist of allowed table names. While table names are quoted using dialect-specific
/// delimiters, malicious input could still cause issues if not properly validated.
/// </para>
/// <para>
/// Example of safe usage:
/// </para>
/// <code>
/// var allowedTables = new HashSet&lt;string&gt; { "users", "orders", "products" };
/// if (!allowedTables.Contains(tableName))
///     throw new ArgumentException("Invalid table name");
/// 
/// var sql = template.Render(new Dictionary&lt;string, object?&gt; { ["tableName"] = tableName });
/// </code>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM {{table}}
/// // SQLite:   SELECT * FROM [users]
/// // MySQL:    SELECT * FROM `users`
/// // PostgreSQL: SELECT * FROM "users"
/// 
/// // Dynamic: SELECT * FROM {{table --param tableName}}
/// // Output: SELECT * FROM [dynamic_table] (table name from parameter)
/// </code>
/// </example>
public sealed class TablePlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static TablePlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "table";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options)
    {
        // If --param is specified, it's dynamic
        return ParseParam(options) is not null ? PlaceholderType.Dynamic : PlaceholderType.Static;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        // For static table names (no --param option)
        var paramName = ParseParam(options);
        if (paramName is null)
        {
            return context.Dialect.WrapColumn(context.TableName);
        }

        // For dynamic table names, this will be handled in Render()
        throw new InvalidOperationException(
            $"{{{{table --param {paramName}}}}} is dynamic and must be rendered with parameters. " +
            $"Use SqlTemplate.Render() instead of accessing Sql property directly.");
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options);
        
        // If no --param, use static table name
        if (paramName is null)
        {
            return context.Dialect.WrapColumn(context.TableName);
        }

        // Get dynamic table name from parameters
        var tableName = GetParam(parameters, paramName);
        if (tableName is null)
        {
            throw new InvalidOperationException($"Table name parameter '{paramName}' cannot be null.");
        }

        var tableNameStr = tableName.ToString();
        if (string.IsNullOrWhiteSpace(tableNameStr))
        {
            throw new InvalidOperationException($"Table name parameter '{paramName}' cannot be null, empty, or whitespace.");
        }

        return context.Dialect.WrapColumn(tableNameStr);
    }
}
