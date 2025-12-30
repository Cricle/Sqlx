// -----------------------------------------------------------------------
// <copyright file="BaseDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// Base class for database dialect providers with common functionality.
/// </summary>
internal abstract class BaseDialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public abstract SqlDefine SqlDefine { get; }

    /// <inheritdoc />
    public abstract SqlDefineTypes DialectType { get; }

    /// <inheritdoc />
    public abstract string GenerateLimitClause(int? limit, int? offset);

    /// <inheritdoc />
    public abstract string GenerateInsertWithReturning(string tableName, string[] columns);

    /// <inheritdoc />
    public virtual string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        var valuesClauses = new string[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}{i}").ToArray();
            valuesClauses[i] = $"({string.Join(", ", parameters)})";
        }

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES {string.Join(", ", valuesClauses)}";
    }

    /// <inheritdoc />
    public abstract string GenerateUpsert(string tableName, string[] columns, string[] keyColumns);

    /// <inheritdoc />
    public abstract string GetDatabaseTypeName(Type dotNetType);

    /// <inheritdoc />
    public abstract string FormatDateTime(DateTime dateTime);

    /// <inheritdoc />
    public abstract string GetCurrentDateTimeSyntax();

    /// <inheritdoc />
    public abstract string GetConcatenationSyntax(params string[] expressions);

    /// <inheritdoc />
    public virtual string ReplacePlaceholders(string sqlTemplate, string? tableName = null, string[]? columns = null)
    {
        if (string.IsNullOrEmpty(sqlTemplate))
            return sqlTemplate;

        var result = sqlTemplate;

        // Replace {{table}}
        if (tableName != null && result.Contains(Core.DialectPlaceholders.Table))
        {
            result = result.Replace(Core.DialectPlaceholders.Table, SqlDefine.WrapColumn(tableName));
        }

        // Replace {{columns}}
        if (columns != null && columns.Length > 0 && result.Contains(Core.DialectPlaceholders.Columns))
        {
            var wrappedColumns = string.Join(", ", columns.Select(c => SqlDefine.WrapColumn(c)));
            result = result.Replace(Core.DialectPlaceholders.Columns, wrappedColumns);
        }

        // Replace {{returning_id}}
        if (result.Contains(Core.DialectPlaceholders.ReturningId))
        {
            result = result.Replace(Core.DialectPlaceholders.ReturningId, GetReturningIdClause());
        }

        // Replace {{bool_true}}
        if (result.Contains(Core.DialectPlaceholders.BoolTrue))
        {
            result = result.Replace(Core.DialectPlaceholders.BoolTrue, GetBoolTrueLiteral());
        }

        // Replace {{bool_false}}
        if (result.Contains(Core.DialectPlaceholders.BoolFalse))
        {
            result = result.Replace(Core.DialectPlaceholders.BoolFalse, GetBoolFalseLiteral());
        }

        // Replace {{current_timestamp}}
        if (result.Contains(Core.DialectPlaceholders.CurrentTimestamp))
        {
            result = result.Replace(Core.DialectPlaceholders.CurrentTimestamp, GetCurrentDateTimeSyntax());
        }

        return result;
    }

    /// <inheritdoc />
    public abstract string GetReturningIdClause();

    /// <inheritdoc />
    public abstract string GetBoolTrueLiteral();

    /// <inheritdoc />
    public abstract string GetBoolFalseLiteral();

    /// <inheritdoc />
    public abstract string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy);

    /// <summary>
    /// Helper method to generate basic insert statement parts.
    /// </summary>
    protected (string tableName, string[] wrappedColumns, string[] parameters) GetInsertParts(string tableName, string[] columns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}").ToArray();
        return (wrappedTableName, wrappedColumns, parameters);
    }
}


