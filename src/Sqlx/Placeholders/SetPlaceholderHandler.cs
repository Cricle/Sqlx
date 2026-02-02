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
public sealed class SetPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static SetPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "set";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options)
    {
        // 如果有 --param 选项，则为动态占位符
        return ParseParam(options) != null ? PlaceholderType.Dynamic : PlaceholderType.Static;
    }

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        // 如果有 --param 选项，静态处理时返回空字符串（需要动态渲染）
        if (ParseParam(options) != null)
        {
            return string.Empty;
        }

        // 标准静态处理
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

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options);
        
        // 如果没有 --param 选项，使用标准静态处理
        if (paramName == null)
        {
            return Process(context, options);
        }

        // 动态渲染：从参数中获取 SET 子句
        var paramValue = GetParam(parameters, paramName);
        
        // 如果参数为 null，返回空字符串
        if (paramValue == null)
        {
            return string.Empty;
        }

        // 返回参数值的字符串表示
        return paramValue.ToString() ?? string.Empty;
    }
}
