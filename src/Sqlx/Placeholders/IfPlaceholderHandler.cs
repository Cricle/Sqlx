// <copyright file="IfPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System;
using System.Collections.Generic;

/// <summary>
/// Handles conditional {{if}} placeholders for dynamic SQL generation.
/// </summary>
/// <remarks>
/// <para>
/// Supports the following conditions:
/// </para>
/// <list type="bullet">
/// <item><description><c>{{if null=param}}</c> - Content included when param is null</description></item>
/// <item><description><c>{{if notnull=param}}</c> - Content included when param is not null</description></item>
/// <item><description><c>{{if empty=param}}</c> - Content included when param is null or empty collection</description></item>
/// <item><description><c>{{if notempty=param}}</c> - Content included when param is not null and not empty</description></item>
/// </list>
/// <para>
/// The handler processes paired {{if ...}} and {{/if}} tags.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Template: SELECT * FROM users WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}
/// // With name = "Alice" -> SELECT * FROM users WHERE 1=1 AND name = @name
/// // With name = null    -> SELECT * FROM users WHERE 1=1 
/// </code>
/// </example>
public sealed class IfPlaceholderHandler : PlaceholderHandlerBase, IBlockPlaceholderHandler
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static IfPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "if";

    /// <inheritdoc/>
    public string ClosingTagName => "/if";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
        => throw new InvalidOperationException("{{if}} placeholder is always dynamic and requires Render.");

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
        => string.Empty; // Block handlers don't render content directly

    /// <inheritdoc/>
    public bool ShouldInclude(string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Check notnull=param first (before null= to avoid partial match)
        var notNullParam = ParseCondition(options, "notnull");
        if (notNullParam != null)
        {
            var value = parameters?.TryGetValue(notNullParam, out var v) == true ? v : null;
            return value is not null;
        }

        // Check null=param
        var nullParam = ParseCondition(options, "null");
        if (nullParam != null)
        {
            var value = parameters?.TryGetValue(nullParam, out var v) == true ? v : null;
            return value is null;
        }

        // Check notempty=param first (before empty= to avoid partial match)
        var notEmptyParam = ParseCondition(options, "notempty");
        if (notEmptyParam != null)
        {
            var value = parameters?.TryGetValue(notEmptyParam, out var v) == true ? v : null;
            return !IsEmpty(value);
        }

        // Check empty=param
        var emptyParam = ParseCondition(options, "empty");
        if (emptyParam != null)
        {
            var value = parameters?.TryGetValue(emptyParam, out var v) == true ? v : null;
            return IsEmpty(value);
        }

        throw new InvalidOperationException($"Invalid {{{{if}}}} condition: {options}. Use null=, notnull=, empty=, or notempty=.");
    }

    private static bool IsEmpty(object? value)
    {
        if (value is null) return true;
        if (value is string s) return string.IsNullOrEmpty(s);
        if (value is System.Collections.ICollection c) return c.Count == 0;
        if (value is System.Collections.IEnumerable e)
        {
            var enumerator = e.GetEnumerator();
            try { return !enumerator.MoveNext(); }
            finally { (enumerator as IDisposable)?.Dispose(); }
        }
        return false;
    }
}
