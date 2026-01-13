// <copyright file="WherePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;

/// <summary>
/// Handles {{where}} placeholder. Always dynamic.
/// Requires --param option to specify the parameter containing WHERE clause.
/// </summary>
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
    {
        var paramName = ParseParamOption(options)
            ?? throw new InvalidOperationException("{{where}} placeholder requires --param option.");

        var value = context.GetDynamicParameterValue(paramName, "where");
        return value?.ToString() ?? string.Empty;
    }
}
