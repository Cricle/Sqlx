// <copyright file="PlaceholderProcessor.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sqlx.Placeholders;

/// <summary>
/// Manages placeholder handlers for SQL template processing.
/// </summary>
/// <remarks>
/// <para>
/// PlaceholderProcessor provides an extensible registry of placeholder handlers.
/// Handlers can be registered, replaced, or retrieved at runtime using <see cref="RegisterHandler"/>
/// and <see cref="TryGetHandler"/>.
/// </para>
/// <para>
/// Default registered handlers (can be replaced or extended):
/// </para>
/// <list type="bullet">
/// <item><description><c>columns</c> - Generates column list for SELECT</description></item>
/// <item><description><c>values</c> - Generates parameter placeholders for INSERT</description></item>
/// <item><description><c>set</c> - Generates SET clause for UPDATE</description></item>
/// <item><description><c>table</c> - Generates quoted table name</description></item>
/// <item><description><c>where</c> - Dynamic WHERE clause (requires --param)</description></item>
/// <item><description><c>limit</c> - LIMIT clause (static with --count, dynamic with --param)</description></item>
/// <item><description><c>offset</c> - OFFSET clause (static with --count, dynamic with --param)</description></item>
/// <item><description><c>in</c> - IN clause with parentheses (requires --param)</description></item>
/// <item><description><c>if</c> - Conditional block (requires condition like notnull=param)</description></item>
/// </list>
/// <para>
/// Custom handlers can be registered to add new placeholder types or override existing ones.
/// </para>
/// </remarks>
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
        ["in"] = InPlaceholderHandler.Instance,
        ["if"] = IfPlaceholderHandler.Instance,
    };

    private static readonly HashSet<string> BlockClosingTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "/if",
    };

    /// <summary>
    /// Registers a custom placeholder handler.
    /// </summary>
    /// <param name="handler">The handler to register. Its <see cref="IPlaceholderHandler.Name"/> will be used as the key.</param>
    /// <remarks>
    /// If a handler with the same name already exists, it will be replaced.
    /// Handler names are case-insensitive.
    /// If the handler implements <see cref="IBlockPlaceholderHandler"/>, its closing tag is automatically registered.
    /// </remarks>
    public static void RegisterHandler(IPlaceholderHandler handler)
    {
        Handlers[handler.Name] = handler;
        if (handler is IBlockPlaceholderHandler blockHandler)
        {
            BlockClosingTags.Add(blockHandler.ClosingTagName);
        }
    }

    /// <summary>
    /// Registers a block closing tag name.
    /// </summary>
    /// <param name="closingTagName">The closing tag name (e.g., "/if", "/foreach").</param>
    public static void RegisterBlockClosingTag(string closingTagName)
    {
        BlockClosingTags.Add(closingTagName);
    }

    /// <summary>
    /// Checks if a tag name is a block closing tag.
    /// </summary>
    /// <param name="tagName">The tag name to check.</param>
    /// <returns><c>true</c> if it's a closing tag; otherwise, <c>false</c>.</returns>
    public static bool IsBlockClosingTag(string tagName)
    {
        return BlockClosingTags.Contains(tagName);
    }

    /// <summary>
    /// Tries to get a placeholder handler by name.
    /// </summary>
    /// <param name="name">The placeholder name (case-insensitive).</param>
    /// <param name="handler">When this method returns, contains the handler if found; otherwise, null.</param>
    /// <returns><c>true</c> if a handler was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetHandler(string name, out IPlaceholderHandler handler)
        => Handlers.TryGetValue(name, out handler!);

    /// <summary>
    /// Extracts parameter names from a SQL string.
    /// </summary>
    /// <param name="sql">The SQL string to extract parameters from.</param>
    /// <returns>A list of unique parameter names found in the SQL (without prefix).</returns>
    /// <remarks>
    /// Recognizes parameters with @, $, or : prefixes (e.g., @id, $1, :name).
    /// </remarks>
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
