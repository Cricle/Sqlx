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
/// This handler is always dynamic and requires a --param option specifying the parameter name
/// that will contain the WHERE clause at render time.
/// </para>
/// <para>
/// Required options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--param name</c> - Specifies the parameter name containing the WHERE clause</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: SELECT * FROM users WHERE {{where --param predicate}}
/// // Render with: { "predicate": "age > 18 AND status = 'active'" }
/// // Output:   SELECT * FROM users WHERE age > 18 AND status = 'active'
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
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{where}} requires --param option.");
        return GetParam(parameters, paramName)?.ToString() ?? string.Empty;
    }
}
