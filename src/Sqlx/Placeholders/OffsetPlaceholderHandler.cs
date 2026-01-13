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
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--count n</c> - Static offset value resolved at prepare time</description></item>
/// <item><description><c>--param name</c> - Dynamic offset value resolved at render time</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM users LIMIT 10 {{offset --count 20}}
/// // Output: SELECT * FROM users LIMIT 10 OFFSET 20
/// 
/// // Dynamic: SELECT * FROM users {{limit --param pageSize}} {{offset --param offset}}
/// // Render with: { "pageSize": 10, "offset": 30 }
/// // Output: SELECT * FROM users LIMIT 10 OFFSET 30
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
        => ParseCount(options) is not null ? PlaceholderType.Static : PlaceholderType.Dynamic;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCount(options);
        return count is not null ? $"OFFSET {count.Value}" : string.Empty;
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{offset}} requires --count or --param option.");
        var value = GetParam(parameters, paramName);
        return value is not null ? $"OFFSET {Convert.ToInt32(value, CultureInfo.InvariantCulture)}" : string.Empty;
    }
}
