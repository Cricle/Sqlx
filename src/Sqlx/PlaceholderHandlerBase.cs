// <copyright file="PlaceholderHandlerBase.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Base class for placeholder handlers with common utility methods.
/// </summary>
#if NET7_0_OR_GREATER
public abstract partial class PlaceholderHandlerBase : IPlaceholderHandler
#else
public abstract class PlaceholderHandlerBase : IPlaceholderHandler
#endif
{
#if !NET7_0_OR_GREATER
    private static readonly Regex ParamOptionRegexInstance = new(@"--param\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CountOptionRegexInstance = new(@"--count\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExcludeOptionRegexInstance = new(@"--exclude\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AscFlagRegexInstance = new(@"--asc(?:\s|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DescFlagRegexInstance = new(@"--desc(?:\s|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
#endif

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public virtual PlaceholderType GetType(string options) => PlaceholderType.Static;

    /// <inheritdoc/>
    public abstract string Process(PlaceholderContext context, string options);

    /// <summary>
    /// Parses a named option value from options string.
    /// Example: "--param limit" returns "limit" for optionName "param".
    /// </summary>
    /// <param name="options">The options string.</param>
    /// <param name="optionName">The option name without --.</param>
    /// <returns>The option value or null if not found.</returns>
    protected static string? ParseOption(string options, string optionName)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return null;
        }

        var match = GetOptionRegex(optionName).Match(options);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Checks if an option flag exists in options string.
    /// Example: "--asc" returns true for optionName "asc".
    /// </summary>
    /// <param name="options">The options string.</param>
    /// <param name="optionName">The option name without --.</param>
    /// <returns>True if the option flag exists.</returns>
    protected static bool HasOption(string options, string optionName)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return false;
        }

        return GetFlagRegex(optionName).IsMatch(options);
    }

    /// <summary>
    /// Parses --param option value.
    /// </summary>
    protected static string? ParseParamOption(string options) => ParseOption(options, "param");

    /// <summary>
    /// Parses --count option value as integer.
    /// </summary>
    protected static int? ParseCountOption(string options)
    {
        var value = ParseOption(options, "count");
        return value is not null && int.TryParse(value, out var count) ? count : null;
    }

    /// <summary>
    /// Parses --exclude option value as comma-separated list.
    /// </summary>
    protected static HashSet<string>? ParseExcludeOption(string options)
    {
        var value = ParseOption(options, "exclude");
        if (value is null)
        {
            return null;
        }

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
    /// Gets columns filtered by --exclude option.
    /// </summary>
    protected static IEnumerable<ColumnMeta> GetFilteredColumns(
        IReadOnlyList<ColumnMeta> columns,
        string options)
    {
        var exclude = ParseExcludeOption(options);
        if (exclude is null || exclude.Count == 0)
        {
            return columns;
        }

        return columns.Where(c => !exclude.Contains(c.Name) && !exclude.Contains(c.PropertyName));
    }

    /// <summary>
    /// Quotes an identifier using the SQL dialect.
    /// </summary>
    protected static string QuoteIdentifier(SqlDialect dialect, string identifier)
    {
        return dialect.WrapColumn(identifier);
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"--param\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex ParamOptionRegex();

    [GeneratedRegex(@"--count\s+(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex CountOptionRegex();

    [GeneratedRegex(@"--exclude\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex ExcludeOptionRegex();

    [GeneratedRegex(@"--asc(?:\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex AscFlagRegex();

    [GeneratedRegex(@"--desc(?:\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex DescFlagRegex();
#else
    private static Regex ParamOptionRegex() => ParamOptionRegexInstance;
    private static Regex CountOptionRegex() => CountOptionRegexInstance;
    private static Regex ExcludeOptionRegex() => ExcludeOptionRegexInstance;
    private static Regex AscFlagRegex() => AscFlagRegexInstance;
    private static Regex DescFlagRegex() => DescFlagRegexInstance;
#endif

    private static Regex GetOptionRegex(string optionName)
    {
        return optionName.ToLowerInvariant() switch
        {
            "param" => ParamOptionRegex(),
            "count" => CountOptionRegex(),
            "exclude" => ExcludeOptionRegex(),
            _ => new Regex($@"--{Regex.Escape(optionName)}\s+(\S+)", RegexOptions.IgnoreCase),
        };
    }

    private static Regex GetFlagRegex(string optionName)
    {
        return optionName.ToLowerInvariant() switch
        {
            "asc" => AscFlagRegex(),
            "desc" => DescFlagRegex(),
            _ => new Regex($@"--{Regex.Escape(optionName)}(?:\s|$)", RegexOptions.IgnoreCase),
        };
    }
}
