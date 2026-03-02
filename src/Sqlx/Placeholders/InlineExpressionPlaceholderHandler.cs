// <copyright file="InlineExpressionPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Collections.Generic;

/// <summary>
/// Base class for placeholder handlers that support inline expressions and --param options.
/// </summary>
/// <remarks>
/// This base class provides common functionality for handlers that:
/// <list type="bullet">
/// <item><description>Support --exclude option to filter columns</description></item>
/// <item><description>Support --inline option for inline expressions</description></item>
/// <item><description>Support --param option for dynamic rendering</description></item>
/// </list>
/// </remarks>
public abstract class InlineExpressionPlaceholderHandler : PlaceholderHandlerBase
{
    /// <inheritdoc/>
    public override PlaceholderType GetType(string options)
    {
        // If --param is present, this is a dynamic placeholder that needs runtime rendering
        var paramName = ParseParam(options);
        return paramName != null ? PlaceholderType.Dynamic : PlaceholderType.Static;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        // Check if --param option is present
        var paramName = ParseParam(options);
        if (paramName != null)
        {
            // Dynamic mode: delegate to ProcessDynamicParam
            return ProcessDynamicParam(context, paramName);
        }

        // Static mode: process columns with inline expressions
        var columns = FilterColumns(context.Columns, options);
        var inlineExpressions = ParseInlineExpressions(options);

        var results = new List<string>();

        foreach (var column in columns)
        {
            // Check if there's an inline expression for this column
            if (inlineExpressions != null && inlineExpressions.TryGetValue(column.PropertyName, out var expression))
            {
                // Use expression with property names replaced by wrapped column names
                var wrappedExpression = ReplacePropertyNamesWithColumns(expression, context);
                results.Add(FormatInlineExpression(context, column, wrappedExpression));
            }
            else
            {
                // Use standard parameter placeholder
                results.Add(FormatStandardParameter(context, column));
            }
        }

        return string.Join(", ", results);
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options);

        // If no --param option, use standard static processing
        if (paramName == null)
        {
            return Process(context, options);
        }

        // Dynamic rendering: delegate to RenderDynamicParam
        return RenderDynamicParam(context, paramName, parameters);
    }

    /// <summary>
    /// Processes the --param option in static mode (compile-time).
    /// </summary>
    /// <param name="context">The placeholder context.</param>
    /// <param name="paramName">The parameter name from --param option.</param>
    /// <returns>The static placeholder string.</returns>
    protected abstract string ProcessDynamicParam(PlaceholderContext context, string paramName);

    /// <summary>
    /// Formats an inline expression for a column.
    /// </summary>
    /// <param name="context">The placeholder context.</param>
    /// <param name="column">The column metadata.</param>
    /// <param name="wrappedExpression">The expression with property names replaced by column names.</param>
    /// <returns>The formatted inline expression.</returns>
    protected abstract string FormatInlineExpression(PlaceholderContext context, ColumnMeta column, string wrappedExpression);

    /// <summary>
    /// Formats a standard parameter placeholder for a column.
    /// </summary>
    /// <param name="context">The placeholder context.</param>
    /// <param name="column">The column metadata.</param>
    /// <returns>The formatted parameter placeholder.</returns>
    protected abstract string FormatStandardParameter(PlaceholderContext context, ColumnMeta column);

    /// <summary>
    /// Renders the --param option at runtime (dynamic mode).
    /// </summary>
    /// <param name="context">The placeholder context.</param>
    /// <param name="paramName">The parameter name from --param option.</param>
    /// <param name="parameters">The runtime parameters dictionary.</param>
    /// <returns>The rendered dynamic parameter string.</returns>
    protected abstract string RenderDynamicParam(PlaceholderContext context, string paramName, IReadOnlyDictionary<string, object?>? parameters);
}
