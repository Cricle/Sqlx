// <copyright file="LimitPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

/// <summary>
/// Handles the {{limit}} placeholder for generating LIMIT clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler can be either static (with --count) or dynamic (with --param).
/// For dynamic limits, it generates parameterized SQL for better performance.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--count n</c> - Static limit value resolved at prepare time</description></item>
/// <item><description><c>--param name</c> - Dynamic limit value resolved at render time (parameterized)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM users {{limit --count 10}}
/// // Output: SELECT * FROM users LIMIT 10
/// 
/// // Dynamic: SELECT * FROM users {{limit --param pageSize}}
/// // Output: SELECT * FROM users LIMIT @pageSize (parameter added to command)
/// </code>
/// </example>
public sealed class LimitPlaceholderHandler : KeywordWithValuePlaceholderHandler
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static LimitPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "limit";

    /// <inheritdoc/>
    protected override string Keyword => "LIMIT";

    /// <inheritdoc/>
    protected override string FormatClause(PlaceholderContext context, string value) =>
        context.Dialect.LimitClause(value);
}
