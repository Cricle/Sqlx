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
public sealed class ValuesPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ValuesPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "values";

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
            // Generate single parameter placeholder for IN clause
            // This will be expanded at runtime in Render()
            return context.Dialect.CreateParameter(paramName);
        }

        // Original behavior: generate entity property list
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

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Check if --param option is present
        var paramName = ParseParam(options);
        if (paramName != null && parameters != null)
        {
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

        // Fall back to static processing
        return Process(context, options);
    }
}
