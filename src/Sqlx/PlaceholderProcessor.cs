// <copyright file="PlaceholderProcessor.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sqlx.Placeholders;

/// <summary>
/// Processes SQL templates with placeholders.
/// Two-phase: Prepare (static, once) + Render (dynamic, each execution).
/// </summary>
#if NET7_0_OR_GREATER
public sealed partial class PlaceholderProcessor
#else
public sealed class PlaceholderProcessor
#endif
{
#if !NET7_0_OR_GREATER
    private static readonly Regex PlaceholderRegexInstance = new(@"\{\{(\w+)(?:\s+([^}]+))?\}\}", RegexOptions.Compiled);
    private static readonly Regex ParameterRegexInstance = new(@"[@$:](\w+)", RegexOptions.Compiled);
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
    /// Registers a custom placeholder handler.
    /// </summary>
    /// <param name="handler">The handler to register.</param>
    public static void RegisterHandler(IPlaceholderHandler handler)
    {
        Handlers[handler.Name] = handler;
    }

    /// <summary>
    /// Checks if a template contains any dynamic placeholders.
    /// </summary>
    /// <param name="template">The SQL template.</param>
    /// <returns>True if the template contains dynamic placeholders.</returns>
    public static bool ContainsDynamicPlaceholders(string template)
    {
        foreach (Match match in PlaceholderRegex().Matches(template))
        {
            var name = match.Groups[1].Value;
            var options = match.Groups[2].Value;

            if (Handlers.TryGetValue(name, out var handler) &&
                handler.GetType(options) == PlaceholderType.Dynamic)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts parameter names from SQL (e.g., @param, $param, :param).
    /// </summary>
    /// <param name="sql">The SQL string.</param>
    /// <returns>List of parameter names without prefix.</returns>
    public static IReadOnlyList<string> ExtractParameters(string sql)
    {
        var parameters = new List<string>();
        foreach (Match match in ParameterRegex().Matches(sql))
        {
            var name = match.Groups[1].Value;
            if (!parameters.Contains(name))
            {
                parameters.Add(name);
            }
        }

        return parameters;
    }

    /// <summary>
    /// Prepares a template by processing all static placeholders.
    /// Called once at initialization.
    /// </summary>
    /// <param name="template">The SQL template.</param>
    /// <param name="context">The placeholder context.</param>
    /// <returns>The prepared SQL with static placeholders resolved.</returns>
    public static string Prepare(string template, PlaceholderContext context)
    {
        return PlaceholderRegex().Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            var options = match.Groups[2].Value;

            if (!Handlers.TryGetValue(name, out var handler))
            {
                throw new InvalidOperationException($"Unknown placeholder: {{{{{name}}}}}");
            }

            // Only process static placeholders during Prepare
            if (handler.GetType(options) == PlaceholderType.Static)
            {
                return handler.Process(context, options);
            }

            // Keep dynamic placeholders for Render phase
            return match.Value;
        });
    }

    /// <summary>
    /// Renders a prepared template by processing all dynamic placeholders.
    /// Called each execution when HasDynamicPlaceholders is true.
    /// </summary>
    /// <param name="preparedSql">The prepared SQL from Prepare phase.</param>
    /// <param name="context">The placeholder context with DynamicParameters.</param>
    /// <returns>The final SQL with all placeholders resolved.</returns>
    public static string Render(string preparedSql, PlaceholderContext context)
    {
        return PlaceholderRegex().Replace(preparedSql, match =>
        {
            var name = match.Groups[1].Value;
            var options = match.Groups[2].Value;

            if (!Handlers.TryGetValue(name, out var handler))
            {
                throw new InvalidOperationException($"Unknown placeholder: {{{{{name}}}}}");
            }

            return handler.Process(context, options);
        });
    }

#if NET7_0_OR_GREATER
    // Matches {{name}} or {{name --options}}
    [GeneratedRegex(@"\{\{(\w+)(?:\s+([^}]+))?\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    // Matches @param, $param, :param
    [GeneratedRegex(@"[@$:](\w+)", RegexOptions.Compiled)]
    private static partial Regex ParameterRegex();
#else
    private static Regex PlaceholderRegex() => PlaceholderRegexInstance;
    private static Regex ParameterRegex() => ParameterRegexInstance;
#endif
}
