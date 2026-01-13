// <copyright file="SqlTemplate.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a SQL template with two-phase processing: Prepare (static) + Render (dynamic).
/// </summary>
public sealed class SqlTemplate
{
    private readonly PlaceholderContext? _context;
    private readonly string _preparedSql;
    private readonly bool _hasDynamicPlaceholders;
    private readonly IReadOnlyList<string> _staticParameters;

    private SqlTemplate(
        string template,
        PlaceholderContext context,
        string preparedSql,
        bool hasDynamicPlaceholders,
        IReadOnlyList<string> staticParameters)
    {
        Template = template;
        _context = context;
        _preparedSql = preparedSql;
        _hasDynamicPlaceholders = hasDynamicPlaceholders;
        _staticParameters = staticParameters;
    }

    /// <summary>
    /// Gets the original SQL template with placeholders.
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// Gets the prepared SQL with static placeholders resolved.
    /// </summary>
    public string PreparedSql => _preparedSql;

    /// <summary>
    /// Gets whether the template contains dynamic placeholders.
    /// </summary>
    public bool HasDynamicPlaceholders => _hasDynamicPlaceholders;

    /// <summary>
    /// Gets the parameter names extracted from the prepared SQL.
    /// </summary>
    public IReadOnlyList<string> StaticParameters => _staticParameters;

    /// <summary>
    /// Gets the final SQL. If no dynamic placeholders, returns PreparedSql.
    /// For dynamic templates, call Render() with parameters.
    /// </summary>
    public string Sql => _preparedSql;

    /// <summary>
    /// Prepares a SQL template by processing static placeholders.
    /// </summary>
    /// <param name="template">The SQL template string.</param>
    /// <param name="context">The placeholder context.</param>
    /// <returns>A prepared SqlTemplate instance.</returns>
    public static SqlTemplate Prepare(string template, PlaceholderContext context)
    {
        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var hasDynamic = PlaceholderProcessor.ContainsDynamicPlaceholders(template);
        var preparedSql = PlaceholderProcessor.Prepare(template, context);
        var staticParams = PlaceholderProcessor.ExtractParameters(preparedSql);

        return new SqlTemplate(template, context, preparedSql, hasDynamic, staticParams);
    }

    /// <summary>
    /// Renders the template with dynamic parameters.
    /// Only needed when HasDynamicPlaceholders is true.
    /// </summary>
    /// <param name="dynamicParameters">The dynamic parameter values.</param>
    /// <returns>The final SQL with all placeholders resolved.</returns>
    public string Render(IReadOnlyDictionary<string, object?> dynamicParameters)
    {
        if (!_hasDynamicPlaceholders || _context is null)
        {
            return _preparedSql;
        }

        var contextWithParams = _context.WithDynamicParameters(dynamicParameters);
        return PlaceholderProcessor.Render(_preparedSql, contextWithParams);
    }
}
