// <copyright file="WherePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;

/// <summary>
/// Handles the {{where}} placeholder for generating dynamic WHERE clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always dynamic and supports two modes:
/// </para>
/// <list type="bullet">
/// <item><description><c>--param name</c> - Uses a pre-built WHERE clause string from the parameter</description></item>
/// <item><description><c>--object name</c> - Builds WHERE clause from dictionary (non-null values become AND conditions)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Mode 1: Pre-built WHERE clause
/// // Template: SELECT * FROM users WHERE {{where --param predicate}}
/// // Render with: { "predicate": "age > 18 AND status = 'active'" }
/// // Output:   SELECT * FROM users WHERE age > 18 AND status = 'active'
///
/// // Mode 2: Dictionary-based WHERE clause (AOT compatible)
/// // Template: SELECT * FROM users WHERE {{where --object filter}}
/// // Render with: { "filter": new Dictionary&lt;string, object?&gt; { ["name"] = "John", ["age"] = 25 } }
/// // Output:   SELECT * FROM users WHERE ([name] = @name AND [age] = @age)
/// </code>
/// </example>
public sealed class WherePlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static WherePlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "where";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
        => throw new InvalidOperationException("{{where}} is dynamic, use Render instead.");

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Check for --object mode first
        var objectParamName = ParseOption(options, "object");
        if (objectParamName != null)
        {
            return RenderFromObject(context, objectParamName, parameters);
        }

        // Fallback to --param mode
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{where}} requires --param or --object option.");
        return GetParam(parameters, paramName)?.ToString() ?? string.Empty;
    }

    private static string RenderFromObject(PlaceholderContext context, string objectParamName, IReadOnlyDictionary<string, object?>? parameters)
    {
        var obj = GetParam(parameters, objectParamName);
        if (obj is null)
        {
            return "1=1"; // Null object returns always-true condition
        }

        if (obj is not IReadOnlyDictionary<string, object?> dict)
        {
            throw new InvalidOperationException(
                $"Parameter '{objectParamName}' for --object must be IReadOnlyDictionary<string, object?>. " +
                $"Use entity.ToDictionary() or create a dictionary manually.");
        }

        if (dict.Count == 0)
        {
            return "1=1"; // Empty dictionary returns always-true condition
        }

        var conditions = new List<string>(dict.Count);
        var columns = context.Columns;
        var dialect = context.Dialect;

        // Build column name lookup for O(1) access
        Dictionary<string, ColumnMeta>? columnLookup = null;
        if (columns.Count > 4) // Only build lookup for larger column sets
        {
            columnLookup = new Dictionary<string, ColumnMeta>(columns.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
            {
                columnLookup[col.PropertyName] = col;
                columnLookup[col.Name] = col;
            }
        }

        foreach (var kvp in dict)
        {
            if (kvp.Value is null)
                continue;

            // Find matching column
            ColumnMeta? column = null;
            if (columnLookup != null)
            {
                columnLookup.TryGetValue(kvp.Key, out column);
            }
            else
            {
                foreach (var col in columns)
                {
                    if (string.Equals(col.PropertyName, kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(col.Name, kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        column = col;
                        break;
                    }
                }
            }

            if (column is null)
                continue; // Skip unknown properties

            // column = @column
            conditions.Add($"{dialect.WrapColumn(column.Name)} = {dialect.CreateParameter(column.Name)}");
        }

        if (conditions.Count == 0)
        {
            return "1=1"; // No valid conditions, return always-true
        }

        // Wrap in parentheses and join with AND
        return conditions.Count == 1
            ? conditions[0]
            : $"({string.Join(" AND ", conditions)})";
    }
}
