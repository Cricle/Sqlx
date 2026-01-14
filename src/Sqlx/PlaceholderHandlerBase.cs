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
