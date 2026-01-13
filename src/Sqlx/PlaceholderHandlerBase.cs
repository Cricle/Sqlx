// <copyright file="PlaceholderHandlerBase.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Base class for placeholder handlers.
/// </summary>
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
    /// Parses --param option.
    /// </summary>
    protected static string? ParseParam(string options)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var m = ParamOptionRegex().Match(options);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// Parses --count option.
    /// </summary>
    protected static int? ParseCount(string options)
    {
        if (string.IsNullOrEmpty(options)) return null;
        var m = CountOptionRegex().Match(options);
        return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : null;
    }

    /// <summary>
    /// Parses --exclude option.
    /// </summary>
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
    /// Gets filtered columns by --exclude option.
    /// </summary>
    protected static IEnumerable<ColumnMeta> FilterColumns(IReadOnlyList<ColumnMeta> columns, string options)
    {
        var exclude = ParseExclude(options);
        return exclude is null ? columns : columns.Where(c => !exclude.Contains(c.Name) && !exclude.Contains(c.PropertyName));
    }

    /// <summary>
    /// Gets parameter value from dictionary.
    /// </summary>
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
