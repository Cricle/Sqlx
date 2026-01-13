// <copyright file="SqlTemplate.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Represents a prepared SQL template with efficient rendering capabilities.
/// </summary>
/// <remarks>
/// <para>
/// SqlTemplate provides a two-phase approach for SQL generation:
/// </para>
/// <list type="number">
/// <item><description><see cref="Prepare"/> - Pre-processes static placeholders and records dynamic placeholder positions</description></item>
/// <item><description><see cref="Render"/> - Efficiently renders dynamic placeholders using StringBuilder</description></item>
/// </list>
/// <para>
/// Supported placeholder syntax: <c>{{name}}</c> or <c>{{name --option value}}</c>
/// </para>
/// <para>
/// Built-in placeholders: columns, values, set, table, where, limit, offset
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns);
/// var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}", context);
/// var sql = template.Render(new Dictionary&lt;string, object?&gt; { ["predicate"] = "age > 18" });
/// </code>
/// </example>
#if NET7_0_OR_GREATER
public sealed partial class SqlTemplate
#else
public sealed class SqlTemplate
#endif
{
    private readonly string[] _segments;
    private readonly DynamicPlaceholder[] _placeholders;

    private SqlTemplate(string sql, string[] segments, DynamicPlaceholder[] placeholders)
    {
        Sql = sql;
        _segments = segments;
        _placeholders = placeholders;
    }

    /// <summary>
    /// Gets the prepared SQL with static placeholders resolved.
    /// </summary>
    /// <remarks>
    /// This SQL string has all static placeholders (like {{columns}}, {{table}}) already replaced.
    /// Dynamic placeholders (like {{where --param predicate}}) are not included in this string.
    /// </remarks>
    public string Sql { get; }

    /// <summary>
    /// Gets a value indicating whether the template contains dynamic placeholders.
    /// </summary>
    /// <remarks>
    /// Dynamic placeholders require runtime parameters and must be rendered using <see cref="Render"/>.
    /// </remarks>
    public bool HasDynamicPlaceholders => _placeholders.Length > 0;

    /// <summary>
    /// Prepares a SQL template by pre-processing static placeholders and recording dynamic placeholder positions.
    /// </summary>
    /// <param name="template">The SQL template string containing placeholders.</param>
    /// <param name="context">The placeholder context providing dialect, table name, and column metadata.</param>
    /// <returns>A prepared <see cref="SqlTemplate"/> instance ready for rendering.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unknown placeholder is encountered.</exception>
    public static SqlTemplate Prepare(string template, PlaceholderContext context)
    {
        var segments = new List<string>();
        var placeholders = new List<DynamicPlaceholder>();
        var lastIndex = 0;

        foreach (Match match in PlaceholderRegex().Matches(template))
        {
            var name = match.Groups[1].Value;
            var options = match.Groups[2].Value;

            if (!PlaceholderProcessor.TryGetHandler(name, out var handler))
            {
                throw new InvalidOperationException($"Unknown placeholder: {{{{{name}}}}}");
            }

            var type = handler.GetType(options);
            if (type == PlaceholderType.Static)
            {
                // 静态占位符：直接替换
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                var replacement = handler.Process(context, options);
                segments.Add(before + replacement);
            }
            else
            {
                // 动态占位符：记录位置
                var before = template.Substring(lastIndex, match.Index - lastIndex);
                segments.Add(before);
                placeholders.Add(new DynamicPlaceholder(segments.Count - 1, handler, options, context));
            }

            lastIndex = match.Index + match.Length;
        }

        // 添加最后一段
        if (lastIndex < template.Length)
        {
            segments.Add(template.Substring(lastIndex));
        }

        var sql = string.Concat(segments);
        return new SqlTemplate(sql, segments.ToArray(), placeholders.ToArray());
    }

    /// <summary>
    /// Renders the template with dynamic parameters, producing the final SQL string.
    /// </summary>
    /// <param name="dynamicParameters">Dictionary of parameter names and values for dynamic placeholders.</param>
    /// <returns>The fully rendered SQL string with all placeholders resolved.</returns>
    /// <remarks>
    /// If the template has no dynamic placeholders, this method returns <see cref="Sql"/> directly.
    /// Otherwise, it uses StringBuilder to efficiently concatenate pre-computed segments with rendered dynamic values.
    /// </remarks>
    public string Render(IReadOnlyDictionary<string, object?>? dynamicParameters)
    {
        if (_placeholders.Length == 0)
        {
            return Sql;
        }

        var sb = new StringBuilder();
        var partIndex = 0;

        for (var i = 0; i < _segments.Length; i++)
        {
            sb.Append(_segments[i]);

            if (partIndex < _placeholders.Length && _placeholders[partIndex].SegmentIndex == i)
            {
                var p = _placeholders[partIndex];
                sb.Append(p.Handler.Render(p.Context, p.Options, dynamicParameters));
                partIndex++;
            }
        }

        return sb.ToString();
    }

#if NET7_0_OR_GREATER
    [System.Text.RegularExpressions.GeneratedRegex(@"\{\{(\w+)(?:\s+([^}]+))?\}\}")]
    private static partial Regex PlaceholderRegex();
#else
    private static readonly Regex PlaceholderRegexInstance = new(@"\{\{(\w+)(?:\s+([^}]+))?\}\}", RegexOptions.Compiled);
    private static Regex PlaceholderRegex() => PlaceholderRegexInstance;
#endif

    private readonly struct DynamicPlaceholder
    {
        public readonly int SegmentIndex;
        public readonly IPlaceholderHandler Handler;
        public readonly string Options;
        public readonly PlaceholderContext Context;

        public DynamicPlaceholder(int segmentIndex, IPlaceholderHandler handler, string options, PlaceholderContext context)
        {
            SegmentIndex = segmentIndex;
            Handler = handler;
            Options = options;
            Context = context;
        }
    }
}
