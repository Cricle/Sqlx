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
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--count n</c> - Static limit value resolved at prepare time</description></item>
/// <item><description><c>--param name</c> - Dynamic limit value resolved at render time</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM users {{limit --count 10}}
/// // Output: SELECT * FROM users LIMIT 10
/// 
/// // Dynamic: SELECT * FROM users {{limit --param pageSize}}
/// // Render with: { "pageSize": 20 }
/// // Output: SELECT * FROM users LIMIT 20
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
        => ParseCount(options) is not null ? PlaceholderType.Static : PlaceholderType.Dynamic;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCount(options);
        return count is not null ? $"LIMIT {count.Value}" : string.Empty;
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{limit}} requires --count or --param option.");
        var value = GetParam(parameters, paramName);
        return value is not null ? $"LIMIT {Convert.ToInt32(value, CultureInfo.InvariantCulture)}" : string.Empty;
    }
}
