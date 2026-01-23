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
/// </code>
/// </example>
public sealed class ValuesPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ValuesPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "values";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var columns = FilterColumns(context.Columns, options);
        var inlineExpressions = ParseInlineExpressions(options);

        var values = new List<string>();

        foreach (var column in columns)
        {
            // 检查是否有内联表达式（通过属性名匹配）
            if (inlineExpressions != null && inlineExpressions.TryGetValue(column.PropertyName, out var expression))
            {
                // 使用表达式，将表达式中的属性名替换为包装后的列名
                var wrappedExpression = ReplacePropertyNamesWithColumns(expression, context);
                values.Add(wrappedExpression);
            }
            else
            {
                // 使用标准参数占位符
                values.Add(context.Dialect.CreateParameter(column.Name));
            }
        }

        return string.Join(", ", values);
    }
}
