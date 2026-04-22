// <copyright file="KeywordWithValuePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Base class for placeholder handlers that generate SQL clauses with a keyword and a value.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for handlers like LIMIT and OFFSET
/// that follow the pattern: KEYWORD value (e.g., "LIMIT 10", "OFFSET 20").
/// </remarks>
public abstract class KeywordWithValuePlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the SQL keyword for this handler (e.g., "LIMIT", "OFFSET").
    /// </summary>
    protected abstract string Keyword { get; }

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options)
    {
        // Both --count and --param are static: SQL is generated at Prepare() time
        // --count: generates "KEYWORD 10" (literal value)
        // --param: generates "KEYWORD @param" (parameterized, value bound at execution)
        return PlaceholderType.Static;
    }

    /// <summary>
    /// Generates the dialect-specific clause for a given value string.
    /// </summary>
    protected virtual string FormatClause(PlaceholderContext context, string value) =>
        $"{Keyword} {value}";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCount(options);
        if (count is not null)
        {
            return FormatClause(context, count.Value.ToString(CultureInfo.InvariantCulture));
        }

        // For dynamic values, generate parameterized SQL
        var paramName = ParseParam(options);
        if (paramName is not null)
        {
            return FormatClause(context, context.Dialect.ParameterPrefix + paramName);
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException($"{{{{{Keyword.ToLower()}}}}} requires --count or --param option.");
        var value = GetParam(parameters, paramName);
        return value is not null ? FormatClause(context, Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)) : string.Empty;
    }
}
