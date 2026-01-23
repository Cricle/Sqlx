// <copyright file="SetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Handles the {{set}} placeholder for generating UPDATE SET clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always static and generates a comma-separated list of column=parameter assignments.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns from the SET clause</description></item>
/// <item><description><c>--inline PropertyName=expression</c> - Specifies inline expressions using property names</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: UPDATE users SET {{set --exclude Id}} WHERE id = @id
/// // Output:   UPDATE users SET [name] = @name, [email] = @email WHERE id = @id
/// 
/// // Template: UPDATE users SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id
/// // Output:   UPDATE users SET [name] = @name, [email] = @email, [version] = [version] + 1 WHERE id = @id
/// </code>
/// </example>
public sealed class SetPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static SetPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "set";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var columns = FilterColumns(context.Columns, options);
        var inlineExpressions = ParseInlineExpressions(options);

        var setClauses = new List<string>();

        foreach (var column in columns)
        {
            // 检查是否有内联表达式（通过属性名匹配）
            if (inlineExpressions != null && inlineExpressions.TryGetValue(column.PropertyName, out var expression))
            {
                // 使用表达式，将表达式中的属性名替换为包装后的列名
                var wrappedExpression = ReplacePropertyNamesWithColumns(expression, context);
                setClauses.Add($"{context.Dialect.WrapColumn(column.Name)} = {wrappedExpression}");
            }
            else
            {
                // 使用标准参数赋值
                setClauses.Add($"{context.Dialect.WrapColumn(column.Name)} = {context.Dialect.CreateParameter(column.Name)}");
            }
        }

        return string.Join(", ", setClauses);
    }
}
