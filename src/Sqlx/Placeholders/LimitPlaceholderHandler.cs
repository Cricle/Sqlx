// <copyright file="LimitPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Handles {{limit}} placeholder. Static with --count, dynamic with --param.
/// </summary>
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
