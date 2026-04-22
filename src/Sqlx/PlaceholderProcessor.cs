// <copyright file="PlaceholderProcessor.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<string, IPlaceholderHandler> Handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["columns"] = ColumnsPlaceholderHandler.Instance,
        ["values"] = ValuesPlaceholderHandler.Instance,
        ["set"] = SetPlaceholderHandler.Instance,
        ["table"] = TablePlaceholderHandler.Instance,
        ["where"] = WherePlaceholderHandler.Instance,
        ["limit"] = LimitPlaceholderHandler.Instance,
        ["offset"] = OffsetPlaceholderHandler.Instance,
        ["paginate"] = PaginatePlaceholderHandler.Instance,
        ["if"] = IfPlaceholderHandler.Instance,
        ["arg"] = ArgPlaceholderHandler.Instance,
        ["var"] = VarPlaceholderHandler.Instance,
    };

    private static readonly ConcurrentDictionary<string, byte> BlockClosingTags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/if"] = 0,
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
            BlockClosingTags[blockHandler.ClosingTagName] = 0;
        }
    }

    /// <summary>
    /// Registers a block closing tag name.
    /// </summary>
    /// <param name="closingTagName">The closing tag name (e.g., "/if", "/foreach").</param>
    public static void RegisterBlockClosingTag(string closingTagName)
    {
        BlockClosingTags[closingTagName] = 0;
    }

    /// <summary>
    /// Checks if a tag name is a block closing tag.
    /// </summary>
    /// <param name="tagName">The tag name to check.</param>
    /// <returns><c>true</c> if it's a closing tag; otherwise, <c>false</c>.</returns>
    public static bool IsBlockClosingTag(string tagName)
    {
        return BlockClosingTags.ContainsKey(tagName);
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
        if (string.IsNullOrEmpty(sql))
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var state = ExtractState.None;
        string? dollarQuotedDelimiter = null;

        for (var i = 0; i < sql.Length; i++)
        {
            if (TryAdvanceQuotedOrCommentState(sql, ref i, ref state, ref dollarQuotedDelimiter))
            {
                continue;
            }

            if (!IsParameterPrefix(sql[i]) || !CanStartParameter(sql, i))
            {
                continue;
            }

            var start = i + 1;
            if (start >= sql.Length || !IsParameterNameChar(sql[start]))
            {
                continue;
            }

            var end = start + 1;
            while (end < sql.Length && IsParameterNameChar(sql[end]))
            {
                end++;
            }

            var name = sql.Substring(start, end - start);
            if (seen.Add(name))
            {
                list.Add(name);
            }

            i = end - 1;
        }

        return list;
    }

    private static bool TryAdvanceQuotedOrCommentState(
        string sql,
        ref int index,
        ref ExtractState state,
        ref string? dollarQuotedDelimiter)
    {
        var current = sql[index];
        var hasNext = index + 1 < sql.Length;
        var next = hasNext ? sql[index + 1] : '\0';

        switch (state)
        {
            case ExtractState.SingleQuotedString:
                if (current == '\'')
                {
                    if (hasNext && next == '\'')
                    {
                        index++;
                    }
                    else
                    {
                        state = ExtractState.None;
                    }
                }
                return true;

            case ExtractState.DoubleQuotedIdentifier:
                if (current == '"')
                {
                    if (hasNext && next == '"')
                    {
                        index++;
                    }
                    else
                    {
                        state = ExtractState.None;
                    }
                }
                return true;

            case ExtractState.BacktickIdentifier:
                if (current == '`')
                {
                    if (hasNext && next == '`')
                    {
                        index++;
                    }
                    else
                    {
                        state = ExtractState.None;
                    }
                }
                return true;

            case ExtractState.BracketIdentifier:
                if (current == ']')
                {
                    if (hasNext && next == ']')
                    {
                        index++;
                    }
                    else
                    {
                        state = ExtractState.None;
                    }
                }
                return true;

            case ExtractState.LineComment:
                if (current is '\r' or '\n')
                {
                    state = ExtractState.None;
                }
                return true;

            case ExtractState.BlockComment:
                if (current == '*' && hasNext && next == '/')
                {
                    state = ExtractState.None;
                    index++;
                }
                return true;

            case ExtractState.DollarQuotedString:
                if (dollarQuotedDelimiter != null &&
                    index + dollarQuotedDelimiter.Length <= sql.Length &&
                    string.CompareOrdinal(sql, index, dollarQuotedDelimiter, 0, dollarQuotedDelimiter.Length) == 0)
                {
                    state = ExtractState.None;
                    index += dollarQuotedDelimiter.Length - 1;
                    dollarQuotedDelimiter = null;
                }
                return true;
        }

        if (current == '\'')
        {
            state = ExtractState.SingleQuotedString;
            return true;
        }

        if (current == '"')
        {
            state = ExtractState.DoubleQuotedIdentifier;
            return true;
        }

        if (current == '`')
        {
            state = ExtractState.BacktickIdentifier;
            return true;
        }

        if (current == '[')
        {
            state = ExtractState.BracketIdentifier;
            return true;
        }

        if (current == '-' && hasNext && next == '-')
        {
            state = ExtractState.LineComment;
            index++;
            return true;
        }

        if (current == '/' && hasNext && next == '*')
        {
            state = ExtractState.BlockComment;
            index++;
            return true;
        }

        if (current == '$' && TryGetDollarQuotedDelimiter(sql, index, out var delimiter))
        {
            state = ExtractState.DollarQuotedString;
            dollarQuotedDelimiter = delimiter;
            index += delimiter.Length - 1;
            return true;
        }

        return false;
    }

    private static bool TryGetDollarQuotedDelimiter(string sql, int index, out string delimiter)
    {
        var i = index + 1;
        while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_'))
        {
            i++;
        }

        if (i < sql.Length && sql[i] == '$')
        {
            delimiter = sql.Substring(index, i - index + 1);
            return true;
        }

        delimiter = string.Empty;
        return false;
    }

    private static bool CanStartParameter(string sql, int index)
    {
        if (index == 0)
        {
            return true;
        }

        var previous = sql[index - 1];
        if (char.IsLetterOrDigit(previous) || previous == '_')
        {
            return false;
        }

        return !(sql[index] == ':' && previous == ':');
    }

    private static bool IsParameterPrefix(char value)
    {
        return value is '@' or '$' or ':';
    }

    private static bool IsParameterNameChar(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private enum ExtractState
    {
        None,
        SingleQuotedString,
        DoubleQuotedIdentifier,
        BacktickIdentifier,
        BracketIdentifier,
        LineComment,
        BlockComment,
        DollarQuotedString,
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"[@$:](\w+)")]
    private static partial Regex ParameterRegex();
#else
    private static Regex ParameterRegex() => ParamRegex;
#endif
}
