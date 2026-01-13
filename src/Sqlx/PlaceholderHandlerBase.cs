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
/// </list>
/// </remarks>
#if NET7_0_OR_GREATER
public abstract partial class PlaceholderHandlerBase : IPlaceholderHandler
#else
public abstract class PlaceholderHandlerBase : IPlaceholderHandler
#endif
{
#if !NET7_0_OR_GREATER
    private static readonly Regex ParamRegex = new(@"--param\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CountRegex = new(@"--count\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExcludeRegex = new(@"--exclude\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
    /// Parses the --param option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>The parameter name if found; otherwise, null.</returns>
    protected static string? ParseParam(string options)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var m = ParamOptionRegex().Match(options);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// Parses the --count option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>The count value if found and valid; otherwise, null.</returns>
    protected static int? ParseCount(string options)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var m = CountOptionRegex().Match(options);
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    /// <summary>
    /// Parses the --exclude option from the options string.
    /// </summary>
    /// <param name="options">The options string to parse.</param>
    /// <returns>A set of column names to exclude (case-insensitive); null if not specified.</returns>
    protected static HashSet<string>? ParseExclude(string options)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var m = ExcludeOptionRegex().Match(options);
        if (!m.Success) return null;
#if NET5_0_OR_GREATER
        return new HashSet<string>(
            m.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
#else
        return new HashSet<string>(
            m.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()),
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
    [GeneratedRegex(@"--param\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex ParamOptionRegex();

    [GeneratedRegex(@"--count\s+(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex CountOptionRegex();

    [GeneratedRegex(@"--exclude\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex ExcludeOptionRegex();
#else
    private static Regex ParamOptionRegex() => ParamRegex;
    private static Regex CountOptionRegex() => CountRegex;
    private static Regex ExcludeOptionRegex() => ExcludeRegex;
#endif
}
