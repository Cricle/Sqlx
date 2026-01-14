// <copyright file="ArgPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Collections.Generic;

/// <summary>
/// Handles the {{arg}} placeholder for generating dialect-specific parameter references.
/// </summary>
/// <remarks>
/// <para>
/// This handler generates parameter references with the correct dialect prefix.
/// It can be either static (with --param only) or dynamic (with --param and runtime value).
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--param name</c> - The parameter name (required)</description></item>
/// <item><description><c>--name alias</c> - Optional alias for the parameter name in SQL</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: SELECT * FROM users WHERE id = {{arg --param id}}
/// // SQLite output: SELECT * FROM users WHERE id = @id
/// // PostgreSQL output: SELECT * FROM users WHERE id = $id
/// // Oracle output: SELECT * FROM users WHERE id = :id
/// 
/// // Template: SELECT * FROM users WHERE id = {{arg --param userId --name id}}
/// // SQLite output: SELECT * FROM users WHERE id = @id
/// </code>
/// </example>
public sealed class ArgPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ArgPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "arg";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options) => PlaceholderType.Static;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var paramName = ParseParam(options)
            ?? throw new System.InvalidOperationException("{{arg}} requires --param option.");
        
        var sqlName = ParseOption(options, "name") ?? paramName;
        return context.Dialect.CreateParameter(sqlName);
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Static placeholder, just return the processed result
        return Process(context, options);
    }
}
