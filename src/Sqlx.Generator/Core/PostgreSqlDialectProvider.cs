// -----------------------------------------------------------------------
// <copyright file="PostgreSqlDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// PostgreSQL database dialect provider with PostgreSQL-specific SQL syntax.
/// </summary>
internal class PostgreSqlDialectProvider : BaseDialectProvider
{
    /// <inheritdoc />
    public override SqlDefine SqlDefine => SqlDefine.PostgreSql;

    /// <inheritdoc />
    public override Generator.SqlDefineTypes DialectType => SqlDefineTypes.PostgreSql;

    /// <inheritdoc />
    public override string GenerateLimitClause(int? limit, int? offset)
    {
        var clause = string.Empty;
        if (limit.HasValue)
        {
            clause += $"LIMIT {limit.Value}";
        }
        if (offset.HasValue)
        {
            if (!string.IsNullOrEmpty(clause))
                clause += " ";
            clause += $"OFFSET {offset.Value}";
        }
        return clause;
    }

    /// <inheritdoc />
    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var parameters = columns.Select((c, i) => $"${i + 1}").ToArray();

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES ({string.Join(", ", parameters)}) " +
               $"RETURNING {SqlDefine.WrapColumn("Id")}";
    }

    /// <inheritdoc />
    public override string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        var valuesClauses = new string[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            var parameters = columns.Select((c, j) => $"${i * columns.Length + j + 1}").ToArray();
            valuesClauses[i] = $"({string.Join(", ", parameters)})";
        }

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES {string.Join(", ", valuesClauses)}";
    }

    /// <inheritdoc />
    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var wrappedKeyColumns = keyColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var parameters = columns.Select((c, i) => $"${i + 1}").ToArray();

        var insertStatement = $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
                             $"VALUES ({string.Join(", ", parameters)})";

        var updateClauses = columns.Where(c => !keyColumns.Contains(c))
                                   .Select(c => $"{SqlDefine.WrapColumn(c)} = EXCLUDED.{SqlDefine.WrapColumn(c)}");

        var conflictColumns = string.Join(", ", wrappedKeyColumns);

        return $"{insertStatement} ON CONFLICT ({conflictColumns}) DO UPDATE SET {string.Join(", ", updateClauses)}";
    }

    /// <inheritdoc />
    public override string GetDatabaseTypeName(Type dotNetType)
    {
        return dotNetType.Name switch
        {
            nameof(System.Int16) => "SMALLINT",
            nameof(System.Int32) => "INTEGER",
            nameof(System.Int64) => "BIGINT",
            nameof(System.Decimal) => "DECIMAL(18,2)",
            nameof(System.Double) => "DOUBLE PRECISION",
            nameof(System.Single) => "REAL",
            nameof(System.String) => "VARCHAR(4000)",
            nameof(System.DateTime) => "TIMESTAMP",
            nameof(System.Boolean) => "BOOLEAN",
            nameof(System.Byte) => "SMALLINT",
            nameof(System.Guid) => "UUID",
            "Byte[]" => "BYTEA",
            _ => "VARCHAR(4000)"
        };
    }

    /// <inheritdoc />
    public override string FormatDateTime(DateTime dateTime)
    {
        return $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'::timestamp";
    }

    /// <inheritdoc />
    public override string GetCurrentDateTimeSyntax()
    {
        return "CURRENT_TIMESTAMP";
    }

    /// <inheritdoc />
    public override string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        // PostgreSQL uses || for concatenation
        return string.Join(" || ", expressions);
    }

    /// <inheritdoc />
    public override string GetReturningIdClause()
    {
        return "RETURNING id";
    }

    /// <inheritdoc />
    public override string GetBoolTrueLiteral()
    {
        return "true";
    }

    /// <inheritdoc />
    public override string GetBoolFalseLiteral()
    {
        return "false";
    }

    /// <inheritdoc />
    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = false;
        return $"LIMIT {limitParam} OFFSET {offsetParam}";
    }
}

