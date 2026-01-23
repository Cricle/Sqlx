// <copyright file="PlaceholderHandlerBase.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Base class for placeholder handlers providing common option parsing functionality.
/// </summary>
/// <remarks>
/// <para>
/// Placeholder handlers process SQL template placeholders like <c>{{columns}}</c> or <c>{{limit --count 10}}</c>.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--param name</c> - Specifies a dynamic parameter name</description></item>
/// <item><description><c>--count n</c> - Specifies a static count value</description></item>
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns</description></item>
/// <item><description><c>--name alias</c> - Specifies an alias name</description></item>
/// <item><description><c>--from source</c> - Specifies a source parameter</description></item>
/// </list>
/// </remarks>
#if NET7_0_OR_GREATER
public abstract partial class PlaceholderHandlerBase : IPlaceholderHandler
#else
public abstract class PlaceholderHandlerBase : IPlaceholderHandler
#endif
{
#if !NET7_0_OR_GREATER
    private static readonly Regex OptionRegex = new(@"--(\w+)\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConditionRegex = new(@"(\w+)=(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
#endif

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public virtual PlaceholderType GetType(string options) => PlaceholderType.Static;

    /// <inheritdoc/>
    public abstract string Process(PlaceholderContext context, string options);

    /// <inheritdoc/>
    public virtual string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
        => Process(context, options);

    /// <summary>
    /// Parses a named option from the options string (--option value format).
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <param name="optionName">The option name (without --).</param>
    /// <returns>The option value if found; otherwise, null.</returns>
    protected static string? ParseOption(string options, string optionName)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var matches = GenericOptionRegex().Matches(options);
        foreach (Match m in matches)
        {
            if (string.Equals(m.Groups[1].Value, optionName, StringComparison.OrdinalIgnoreCase))
            {
                return m.Groups[2].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a condition from the options string (key=value format).
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <param name="conditionName">The condition name.</param>
    /// <returns>The condition value if found; otherwise, null.</returns>
    protected static string? ParseCondition(string options, string conditionName)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var matches = GenericConditionRegex().Matches(options);
        foreach (Match m in matches)
        {
            if (string.Equals(m.Groups[1].Value, conditionName, StringComparison.OrdinalIgnoreCase))
            {
                return m.Groups[2].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses the --param option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>The parameter name if found; otherwise, null.</returns>
    protected static string? ParseParam(string options) => ParseOption(options, "param");

    /// <summary>
    /// Parses the --count option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>The count value if found and valid; otherwise, null.</returns>
    protected static int? ParseCount(string options)
    {
        var value = ParseOption(options, "count");
        return value != null && int.TryParse(value, out var v) ? v : null;
    }

    /// <summary>
    /// Parses the --exclude option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>A set of column names to exclude (case-insensitive); null if not specified.</returns>
    protected static HashSet<string>? ParseExclude(string options)
    {
        var value = ParseOption(options, "exclude");
        if (value == null) return null;
#if NET5_0_OR_GREATER
        return new HashSet<string>(
            value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
#else
        return new HashSet<string>(
            value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase);
#endif
    }

    /// <summary>
    /// Parses the --inline option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>A dictionary of property expressions (PropertyName -> expression); null if not specified.</returns>
    protected static Dictionary<string, string>? ParseInlineExpressions(string options)
    {
        if (string.IsNullOrEmpty(options)) return null;

        // 查找 --inline 的位置
        var inlineIndex = options.IndexOf("--inline", StringComparison.OrdinalIgnoreCase);
        if (inlineIndex < 0) return null;

        // 提取 --inline 后面的内容，直到下一个 -- 选项或字符串结束
        var startIndex = inlineIndex + "--inline".Length;
        var nextOptionIndex = options.IndexOf("--", startIndex, StringComparison.Ordinal);
        var value = nextOptionIndex > 0
            ? options.Substring(startIndex, nextOptionIndex - startIndex).Trim()
            : options.Substring(startIndex).Trim();

        if (string.IsNullOrEmpty(value)) return null;

        var expressions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // 智能分割：考虑括号嵌套，只在顶层逗号处分割
        var pairs = SplitRespectingParentheses(value);

        foreach (var pair in pairs)
        {
            var equalIndex = pair.IndexOf('=');
            if (equalIndex > 0 && equalIndex < pair.Length - 1)
            {
                var propertyName = pair.Substring(0, equalIndex).Trim();
                var expression = pair.Substring(equalIndex + 1).Trim();
                expressions[propertyName] = expression;
            }
        }

        return expressions.Count > 0 ? expressions : null;
    }

    /// <summary>
    /// Splits a string by commas, but respects parentheses and quotes.
    /// </summary>
    /// <param name="input">The input string to split.</param>
    /// <returns>An array of split strings.</returns>
    private static string[] SplitRespectingParentheses(string input)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var depth = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            // 处理引号
            if (ch == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                current.Append(ch);
            }
            else if (ch == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                current.Append(ch);
            }
            // 处理括号
            else if (ch == '(' && !inSingleQuote && !inDoubleQuote)
            {
                depth++;
                current.Append(ch);
            }
            else if (ch == ')' && !inSingleQuote && !inDoubleQuote)
            {
                depth--;
                current.Append(ch);
            }
            // 处理逗号
            else if (ch == ',' && depth == 0 && !inSingleQuote && !inDoubleQuote)
            {
                // 顶层逗号，分割
                var part = current.ToString().Trim();
                if (!string.IsNullOrEmpty(part))
                {
                    result.Add(part);
                }
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        // 添加最后一部分
        var lastPart = current.ToString().Trim();
        if (!string.IsNullOrEmpty(lastPart))
        {
            result.Add(lastPart);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Replaces property names in an expression with dialect-wrapped column names.
    /// </summary>
    /// <param name="expression">The expression containing property names.</param>
    /// <param name="context">The placeholder context with column metadata.</param>
    /// <returns>The expression with property names replaced by wrapped column names.</returns>
    protected static string ReplacePropertyNamesWithColumns(string expression, PlaceholderContext context)
    {
        // 匹配标识符（属性名或列名）
        // 排除参数占位符（@param, :param, $param）
        var identifierPattern = @"(?<![:\$@])(?<![:\$@]\w)\b([A-Z][a-zA-Z0-9_]*)\b";
        
        var result = System.Text.RegularExpressions.Regex.Replace(expression, identifierPattern, match =>
        {
            var identifier = match.Value;
            
            // 查找匹配的列（通过属性名）
            var column = context.Columns.FirstOrDefault(c =>
                string.Equals(c.PropertyName, identifier, StringComparison.Ordinal));
            
            // 如果找到匹配的列，替换为包装后的列名
            return column != null ? context.Dialect.WrapColumn(column.Name) : identifier;
        });

        return result;
    }

    /// <summary>
    /// Filters columns by applying the --exclude option.
    /// </summary>
    /// <param name="columns">The columns to filter.</param>
    /// <param name="options">The options string containing potential --exclude directive.</param>
    /// <returns>Filtered columns excluding those specified in --exclude.</returns>
    protected static IEnumerable<ColumnMeta> FilterColumns(IReadOnlyList<ColumnMeta> columns, string options)
    {
        var exclude = ParseExclude(options);
        return exclude is null ? columns : columns.Where(c => !exclude.Contains(c.Name) && !exclude.Contains(c.PropertyName));
    }

    /// <summary>
    /// Gets a parameter value from the dynamic parameters dictionary.
    /// </summary>
    /// <param name="parameters">The parameters dictionary.</param>
    /// <param name="name">The parameter name to retrieve.</param>
    /// <returns>The parameter value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the parameter is not found.</exception>
    protected static object? GetParam(IReadOnlyDictionary<string, object?>? parameters, string name)
    {
        if (parameters is null || !parameters.TryGetValue(name, out var value))
            throw new InvalidOperationException($"Parameter '{name}' not provided.");
        return value;
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"--(\w+)\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex GenericOptionRegex();

    [GeneratedRegex(@"(\w+)=(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex GenericConditionRegex();
#else
    private static Regex GenericOptionRegex() => OptionRegex;
    private static Regex GenericConditionRegex() => ConditionRegex;
#endif
}
