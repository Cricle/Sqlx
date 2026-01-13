// <copyright file="LimitPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Globalization;

/// <summary>
/// Handles {{limit}} placeholder.
/// Static with --count option, Dynamic with --param option.
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
    {
        // --count makes it static, --param makes it dynamic
        return ParseCountOption(options) is not null ? PlaceholderType.Static : PlaceholderType.Dynamic;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCountOption(options);
        if (count is not null)
        {
            return $"LIMIT {count.Value}";
        }

        var paramName = ParseParamOption(options)
            ?? throw new InvalidOperationException("{{limit}} placeholder requires --count or --param option.");

        var value = context.GetDynamicParameterValue(paramName, "limit");
        return value is not null
            ? $"LIMIT {Convert.ToInt32(value, CultureInfo.InvariantCulture)}"
            : string.Empty;
    }
}
