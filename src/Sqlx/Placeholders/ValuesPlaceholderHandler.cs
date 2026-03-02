// <copyright file="ValuesPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles the {{values}} placeholder for generating INSERT parameter placeholders.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always static and generates a comma-separated list of parameter placeholders.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--param name</c> - Generates a single parameter placeholder for IN clause (e.g., @ids)</description></item>
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns from the list</description></item>
/// <item><description><c>--inline PropertyName=expression</c> - Specifies inline expressions using property names</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: INSERT INTO users ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})
/// // Output:   INSERT INTO users ([name], [email]) VALUES (@name, @email)
/// 
/// // Template: INSERT INTO users ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP}})
/// // Output:   INSERT INTO users ([id], [name], [email], [created_at]) VALUES (@id, @name, @email, CURRENT_TIMESTAMP)
/// 
/// // Template: UPDATE users SET priority = @priority WHERE id IN ({{values --param ids}})
/// // Output:   UPDATE users SET priority = @priority WHERE id IN (@ids)
/// </code>
/// </example>
public sealed class ValuesPlaceholderHandler : InlineExpressionPlaceholderHandler
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ValuesPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "values";

    /// <inheritdoc/>
    protected override string ProcessDynamicParam(PlaceholderContext context, string paramName)
    {
        // Generate single parameter placeholder for IN clause
        // This will be expanded at runtime in RenderDynamicParam()
        return context.Dialect.CreateParameter(paramName);
    }

    /// <inheritdoc/>
    protected override string FormatInlineExpression(PlaceholderContext context, ColumnMeta column, string wrappedExpression)
    {
        // For VALUES clause, inline expressions are used directly
        return wrappedExpression;
    }

    /// <inheritdoc/>
    protected override string FormatStandardParameter(PlaceholderContext context, ColumnMeta column)
    {
        // For VALUES clause, use standard parameter placeholder
        return context.Dialect.CreateParameter(column.Name);
    }

    /// <inheritdoc/>
    protected override string RenderDynamicParam(PlaceholderContext context, string paramName, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters == null)
        {
            return context.Dialect.CreateParameter(paramName);
        }

        // Get the parameter value
        var value = GetParam(parameters, paramName);

        // Check if it's a collection
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            // Expand collection into multiple parameters: @ids0, @ids1, @ids2, ...
            var expandedParams = new List<string>();
            var index = 0;
            foreach (var item in enumerable)
            {
                expandedParams.Add(context.Dialect.CreateParameter($"{paramName}{index}"));
                index++;
            }

            return expandedParams.Count > 0 ? string.Join(", ", expandedParams) : "NULL";
        }

        // Not a collection, return single parameter
        return context.Dialect.CreateParameter(paramName);
    }
}
