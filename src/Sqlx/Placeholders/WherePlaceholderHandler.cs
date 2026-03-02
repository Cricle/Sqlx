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
        var dict = ValidateAndGetDictionary(parameters, objectParamName);
        if (dict is null || dict.Count == 0)
        {
            return "1=1"; // Null or empty dictionary returns always-true condition
        }

        var columnLookup = BuildColumnLookup(context.Columns);
        var conditions = BuildConditions(dict, columnLookup, context.Columns, context.Dialect);

        return FormatConditions(conditions);
    }

    private static IReadOnlyDictionary<string, object?>? ValidateAndGetDictionary(IReadOnlyDictionary<string, object?>? parameters, string paramName)
    {
        var obj = GetParam(parameters, paramName);
        if (obj is null)
        {
            return null;
        }

        if (obj is not IReadOnlyDictionary<string, object?> dict)
        {
            throw new InvalidOperationException(
                $"Parameter '{paramName}' for --object must be IReadOnlyDictionary<string, object?>. " +
                $"Use entity.ToDictionary() or create a dictionary manually.");
        }

        return dict;
    }

    private static Dictionary<string, ColumnMeta>? BuildColumnLookup(IReadOnlyList<ColumnMeta> columns)
    {
        if (columns.Count <= 4)
        {
            return null; // Use linear search for small column sets
        }

        var lookup = new Dictionary<string, ColumnMeta>(columns.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var col in columns)
        {
            lookup[col.PropertyName] = col;
            lookup[col.Name] = col;
        }

        return lookup;
    }

    private static List<string> BuildConditions(
        IReadOnlyDictionary<string, object?> dict,
        Dictionary<string, ColumnMeta>? columnLookup,
        IReadOnlyList<ColumnMeta> columns,
        SqlDialect dialect)
    {
        var conditions = new List<string>(dict.Count);

        foreach (var kvp in dict)
        {
            var column = FindColumn(kvp.Key, columnLookup, columns);
            if (column is null)
            {
                continue; // Skip unknown properties
            }

            var condition = BuildCondition(column, kvp.Value, dialect);
            conditions.Add(condition);
        }

        return conditions;
    }

    private static ColumnMeta? FindColumn(
        string key,
        Dictionary<string, ColumnMeta>? lookup,
        IReadOnlyList<ColumnMeta> columns)
    {
        if (lookup != null)
        {
            return lookup.TryGetValue(key, out var col) ? col : null;
        }

        // Linear search for small column sets
        foreach (var col in columns)
        {
            if (string.Equals(col.PropertyName, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(col.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                return col;
            }
        }

        return null;
    }

    private static string BuildCondition(ColumnMeta column, object? value, SqlDialect dialect)
    {
        var columnName = dialect.WrapColumn(column.Name);

        if (value is null)
        {
            return $"{columnName} IS NULL";
        }

        return $"{columnName} = {dialect.CreateParameter(column.Name)}";
    }

    private static string FormatConditions(List<string> conditions)
    {
        if (conditions.Count == 0)
        {
            return "1=1"; // No valid conditions, return always-true
        }

        return conditions.Count == 1
            ? conditions[0]
            : $"({string.Join(" AND ", conditions)})";
    }
}
