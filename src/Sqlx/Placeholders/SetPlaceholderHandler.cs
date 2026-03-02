// <copyright file="SetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Collections.Generic;

/// <summary>
/// Handles the {{set}} placeholder for generating UPDATE SET clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler supports both static and dynamic modes:
/// </para>
/// <list type="bullet">
/// <item><description>Static mode (default): Generates SET clause at compile-time from entity columns</description></item>
/// <item><description>Dynamic mode (--param): Uses runtime parameter for flexible SET clause construction</description></item>
/// </list>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns from the SET clause (static mode)</description></item>
/// <item><description><c>--inline PropertyName=expression</c> - Specifies inline expressions using property names (static mode)</description></item>
/// <item><description><c>--param name</c> - Uses a runtime parameter for dynamic SET clause (dynamic mode)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static mode: UPDATE users SET {{set --exclude Id}} WHERE id = @id
/// // Output:   UPDATE users SET [name] = @name, [email] = @email WHERE id = @id
/// 
/// // Static with inline: UPDATE users SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id
/// // Output:   UPDATE users SET [name] = @name, [email] = @email, [version] = [version] + 1 WHERE id = @id
/// 
/// // Dynamic mode: UPDATE users SET {{set --param updates}} WHERE id = @id
/// // Render with: { "updates": "[name] = @name, [email] = @email" }
/// // Output:   UPDATE users SET [name] = @name, [email] = @email WHERE id = @id
/// 
/// // Dynamic with expression: UPDATE users SET {{set --param updates}} WHERE id = @id
/// // Render with: { "updates": "[priority] = [priority] + 1, [updated_at] = @updatedAt" }
/// // Output:   UPDATE users SET [priority] = [priority] + 1, [updated_at] = @updatedAt WHERE id = @id
/// </code>
/// </example>
public sealed class SetPlaceholderHandler : InlineExpressionPlaceholderHandler
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static SetPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "set";

    /// <inheritdoc/>
    protected override string ProcessDynamicParam(PlaceholderContext context, string paramName)
    {
        // For SET clause with --param, return empty string (needs dynamic rendering)
        return string.Empty;
    }

    /// <inheritdoc/>
    protected override string FormatInlineExpression(PlaceholderContext context, ColumnMeta column, string wrappedExpression)
    {
        // For SET clause, format as: [column] = expression
        return $"{context.Dialect.WrapColumn(column.Name)} = {wrappedExpression}";
    }

    /// <inheritdoc/>
    protected override string FormatStandardParameter(PlaceholderContext context, ColumnMeta column)
    {
        // For SET clause, format as: [column] = @parameter
        return $"{context.Dialect.WrapColumn(column.Name)} = {context.Dialect.CreateParameter(column.Name)}";
    }

    /// <inheritdoc/>
    protected override string RenderDynamicParam(PlaceholderContext context, string paramName, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Get the parameter value
        var paramValue = GetParam(parameters, paramName);

        // If parameter is null, return empty string
        if (paramValue == null)
        {
            return string.Empty;
        }

        // Return string representation of parameter value
        return paramValue.ToString() ?? string.Empty;
    }
}
