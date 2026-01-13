// <copyright file="WherePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;

/// <summary>
/// Handles {{where}} placeholder. Always dynamic.
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
        => throw new InvalidOperationException("{{where}} is dynamic, use Render instead.");

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{where}} requires --param option.");
        return GetParam(parameters, paramName)?.ToString() ?? string.Empty;
    }
}
