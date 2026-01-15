// <copyright file="OffsetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Handles the {{offset}} placeholder for generating OFFSET clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler can be either static (with --count) or dynamic (with --param).
/// For dynamic offsets, it generates parameterized SQL for better performance.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--count n</c> - Static offset value resolved at prepare time</description></item>
/// <item><description><c>--param name</c> - Dynamic offset value resolved at render time (parameterized)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM users LIMIT 10 {{offset --count 20}}
/// // Output: SELECT * FROM users LIMIT 10 OFFSET 20
/// 
/// // Dynamic: SELECT * FROM users {{limit --param pageSize}} {{offset --param offset}}
/// // Output: SELECT * FROM users LIMIT @pageSize OFFSET @offset (parameters added to command)
/// </code>
/// </example>
public sealed class OffsetPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static OffsetPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "offset";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options)
    {
        // Both --count and --param are static: SQL is generated at Prepare() time
        // --count: generates "OFFSET 20" (literal value)
        // --param: generates "OFFSET @offset" (parameterized, value bound at execution)
        return PlaceholderType.Static;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCount(options);
        if (count is not null)
        {
            return $"OFFSET {count.Value}";
        }

        // For dynamic offsets, generate parameterized SQL
        var paramName = ParseParam(options);
        if (paramName is not null)
        {
            return $"OFFSET {context.Dialect.ParameterPrefix}{paramName}";
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // For dynamic offsets with parameters, the SQL is already generated in Process()
        // This method is kept for backward compatibility but should not be called in optimized path
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{offset}} requires --count or --param option.");
        var value = GetParam(parameters, paramName);
        return value is not null ? $"OFFSET {Convert.ToInt32(value, CultureInfo.InvariantCulture)}" : string.Empty;
    }
}
