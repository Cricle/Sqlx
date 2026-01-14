// <copyright file="InPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Handles the {{in}} placeholder for generating IN clause with parentheses.
/// </summary>
/// <remarks>
/// <para>
/// This handler generates parameterized IN clauses like <c>(@p0, @p1, @p2)</c>.
/// It is always dynamic and requires a --param option specifying the collection parameter.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Template: SELECT * FROM users WHERE id IN {{in --param ids}}
/// // With ids = [1, 2, 3]
/// // Output:   SELECT * FROM users WHERE id IN (@ids_0, @ids_1, @ids_2)
/// </code>
/// </example>
public sealed class InPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static InPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "in";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
        => throw new InvalidOperationException("{{in}} placeholder requires --param option and is always dynamic.");

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException("{{in}} placeholder requires --param option.");

        var value = GetParam(parameters, paramName);
        if (value is null)
        {
            return "(NULL)";
        }

        if (value is not IEnumerable enumerable)
        {
            throw new InvalidOperationException($"Parameter '{paramName}' must be a collection for IN clause.");
        }

        var sb = new StringBuilder("(");
        var index = 0;
        foreach (var item in enumerable)
        {
            if (index > 0) sb.Append(", ");
            sb.Append('@').Append(paramName).Append('_').Append(index);
            index++;
        }

        if (index == 0)
        {
            return "(NULL)"; // Empty collection
        }

        sb.Append(')');
        return sb.ToString();
    }
}
