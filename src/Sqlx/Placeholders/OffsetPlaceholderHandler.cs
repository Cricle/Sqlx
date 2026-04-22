// <copyright file="OffsetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

/// <summary>
/// Handles the {{offset}} placeholder for generating OFFSET clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler can be either static (with --count) or dynamic (with --param).
/// For dynamic offsets, it generates parameterized SQL for better performance.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--count n</c> - Static offset value resolved at prepare time</description></item>
/// <item><description><c>--param name</c> - Dynamic offset value resolved at render time (parameterized)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Static: SELECT * FROM users LIMIT 10 {{offset --count 20}}
/// // Output: SELECT * FROM users LIMIT 10 OFFSET 20
/// 
/// // Dynamic: SELECT * FROM users {{limit --param pageSize}} {{offset --param offset}}
/// // Output: SELECT * FROM users LIMIT @pageSize OFFSET @offset (parameters added to command)
/// </code>
/// </example>
public sealed class OffsetPlaceholderHandler : KeywordWithValuePlaceholderHandler
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static OffsetPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "offset";

    /// <inheritdoc/>
    protected override string Keyword => "OFFSET";

    /// <inheritdoc/>
    protected override string FormatClause(PlaceholderContext context, string value) =>
        context.Dialect.OffsetClause(value);
}
