// <copyright file="OffsetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Globalization;

/// <summary>
/// Handles {{offset}} placeholder.
/// Static with --count option, Dynamic with --param option.
/// </summary>
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
        // --count makes it static, --param makes it dynamic
        return ParseCountOption(options) is not null ? PlaceholderType.Static : PlaceholderType.Dynamic;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCountOption(options);
        if (count is not null)
        {
            return $"OFFSET {count.Value}";
        }

        var paramName = ParseParamOption(options)
            ?? throw new InvalidOperationException("{{offset}} placeholder requires --count or --param option.");

        var value = context.GetDynamicParameterValue(paramName, "offset");
        return value is not null
            ? $"OFFSET {Convert.ToInt32(value, CultureInfo.InvariantCulture)}"
            : string.Empty;
    }
}
