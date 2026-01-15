// <copyright file="LimitPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Handles the {{limit}} placeholder for generating LIMIT clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler can be either static (with --count) or dynamic (with --param).
/// For dynamic limits, it generates parameterized SQL for better performance.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--count n</c> - Static limit value resolved at prepare time</description></item>
/// <item><description><c>--param name</c> - Dynamic limit value resolved at render time (parameterized)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM users {{limit --count 10}}
/// // Output: SELECT * FROM users LIMIT 10
/// 
/// // Dynamic: SELECT * FROM users {{limit --param pageSize}}
/// // Output: SELECT * FROM users LIMIT @pageSize (parameter added to command)
/// </code>
/// </example>
public sealed class LimitPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static LimitPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "limit";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options)
    {
        // Both --count and --param are static: SQL is generated at Prepare() time
        // --count: generates "LIMIT 10" (literal value)
        // --param: generates "LIMIT @limit" (parameterized, value bound at execution)
        return PlaceholderType.Static;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCount(options);
        if (count is not null)
        {
            return $"LIMIT {count.Value}";
        }

        // For dynamic limits, generate parameterized SQL
        var paramName = ParseParam(options);
        if (paramName is not null)
        {
            return $"LIMIT {context.Dialect.ParameterPrefix}{paramName}";
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // For dynamic limits with parameters, the SQL is already generated in Process()
        // This method is kept for backward compatibility but should not be called in optimized path
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{limit}} requires --count or --param option.");
        var value = GetParam(parameters, paramName);
        return value is not null ? $"LIMIT {Convert.ToInt32(value, CultureInfo.InvariantCulture)}" : string.Empty;
    }
}
