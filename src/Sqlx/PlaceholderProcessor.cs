// <copyright file="PlaceholderProcessor.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sqlx.Placeholders;

/// <summary>
/// Manages placeholder handlers.
/// </summary>
#if NET7_0_OR_GREATER
public static partial class PlaceholderProcessor
#else
public static class PlaceholderProcessor
#endif
{
#if !NET7_0_OR_GREATER
    private static readonly Regex ParamRegex = new(@"[@$:](\w+)", RegexOptions.Compiled);
#endif

    private static readonly Dictionary<string, IPlaceholderHandler> Handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["columns"] = ColumnsPlaceholderHandler.Instance,
        ["values"] = ValuesPlaceholderHandler.Instance,
        ["set"] = SetPlaceholderHandler.Instance,
        ["table"] = TablePlaceholderHandler.Instance,
        ["where"] = WherePlaceholderHandler.Instance,
        ["limit"] = LimitPlaceholderHandler.Instance,
        ["offset"] = OffsetPlaceholderHandler.Instance,
    };

    /// <summary>
    /// Registers a custom handler.
    /// </summary>
    public static void RegisterHandler(IPlaceholderHandler handler) => Handlers[handler.Name] = handler;

    /// <summary>
    /// Tries to get a handler by name.
    /// </summary>
    public static bool TryGetHandler(string name, out IPlaceholderHandler handler)
        => Handlers.TryGetValue(name, out handler!);

    /// <summary>
    /// Extracts parameter names from SQL.
    /// </summary>
    public static IReadOnlyList<string> ExtractParameters(string sql)
    {
        var list = new List<string>();
        foreach (Match m in ParameterRegex().Matches(sql))
        {
            var name = m.Groups[1].Value;
            if (!list.Contains(name)) list.Add(name);
        }
        return list;
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"[@$:](\w+)")]
    private static partial Regex ParameterRegex();
#else
    private static Regex ParameterRegex() => ParamRegex;
#endif
}
