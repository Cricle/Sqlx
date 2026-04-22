// <copyright file="PaginatePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Collections.Generic;

/// <summary>
/// Handles the {{paginate}} placeholder for generating combined LIMIT+OFFSET clauses.
/// Uses dialect-specific pagination (e.g., LIMIT/OFFSET for MySQL/PostgreSQL/SQLite,
/// OFFSET/FETCH NEXT for SQL Server).
/// </summary>
/// <example>
/// <code>
/// // Template: SELECT {{columns}} FROM {{table}} {{paginate --limit pageSize --offset offset}}
/// // SQLite:   SELECT ... LIMIT @pageSize OFFSET @offset
/// // SqlServer: SELECT ... ORDER BY (SELECT NULL) OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
/// </code>
/// </example>
public sealed class PaginatePlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>Gets the singleton instance.</summary>
    public static PaginatePlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "paginate";

    /// <inheritdoc/>
    public override PlaceholderType GetType(string options) => PlaceholderType.Static;

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var limitParam = ParseNamedOption(options, "--limit");
        var offsetParam = ParseNamedOption(options, "--offset");

        if (limitParam is null || offsetParam is null)
            return string.Empty;

        var paginateSql = context.Dialect.Paginate(
            context.Dialect.ParameterPrefix + limitParam,
            context.Dialect.ParameterPrefix + offsetParam);

        // For SQL Server, Paginate() returns OFFSET/FETCH without ORDER BY.
        // Templates don't have ORDER BY, so we add a dummy one.
        if (context.Dialect.DatabaseType == "SqlServer")
            return "ORDER BY (SELECT NULL) " + paginateSql;

        return paginateSql;
    }

    /// <inheritdoc/>
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
        => Process(context, options);

    private static string? ParseNamedOption(string options, string flag)
    {
        var idx = options.IndexOf(flag, System.StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var rest = options[(idx + flag.Length)..].TrimStart();
        var end = rest.IndexOf(' ');
        return end < 0 ? rest : rest[..end];
    }
}
