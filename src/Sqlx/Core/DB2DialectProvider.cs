// -----------------------------------------------------------------------
// <copyright file="DB2DialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// IBM DB2 database dialect provider with DB2-specific SQL syntax.
/// </summary>
internal class DB2DialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public SqlDefine SqlDefine => SqlDefine.DB2;

    /// <inheritdoc />
    public SqlDefineTypes DialectType => SqlDefineTypes.DB2;

    /// <inheritdoc />
    public string GenerateLimitClause(int? limit, int? offset)
    {
        // DB2 uses LIMIT and OFFSET
        var clause = string.Empty;
        if (offset.HasValue)
        {
            clause += $"OFFSET {offset.Value}";
        }
        if (limit.HasValue)
        {
            if (!string.IsNullOrEmpty(clause))
                clause += " ";
            clause += $"LIMIT {limit.Value}";
        }
        return clause;
    }

    /// <inheritdoc />
    public string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var parameters = Enumerable.Range(1, columns.Length).Select(i => "?").ToArray();

        // DB2 doesn't have RETURNING clause in the same way, use SELECT after INSERT
        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES ({string.Join(", ", parameters)})";
    }

    /// <inheritdoc />
    public string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        // DB2 supports VALUES clause for multiple rows
        var valuesClauses = new string[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            var parameters = Enumerable.Range(i * columns.Length + 1, columns.Length).Select(p => "?").ToArray();
            valuesClauses[i] = $"({string.Join(", ", parameters)})";
        }

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES {string.Join(", ", valuesClauses)}";
    }

    /// <inheritdoc />
    public string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var wrappedKeyColumns = keyColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        // DB2 MERGE statement
        var mergeStatement = $"MERGE INTO {wrappedTableName} AS target\n";
        mergeStatement += "USING (\n";
        mergeStatement += $"  VALUES ({string.Join(", ", Enumerable.Range(1, columns.Length).Select(i => "?"))})\n";
        mergeStatement += $") AS source ({string.Join(", ", wrappedColumns)})\n";
        mergeStatement += $"ON ({string.Join(" AND ", keyColumns.Select(k => $"target.{SqlDefine.WrapColumn(k)} = source.{SqlDefine.WrapColumn(k)}"))})";
        mergeStatement += "\nWHEN MATCHED THEN\n";
        mergeStatement += $"  UPDATE SET {string.Join(", ", columns.Where(c => !keyColumns.Contains(c)).Select(c => $"{SqlDefine.WrapColumn(c)} = source.{SqlDefine.WrapColumn(c)}"))}\n";
        mergeStatement += "WHEN NOT MATCHED THEN\n";
        mergeStatement += $"  INSERT ({string.Join(", ", wrappedColumns)}) VALUES ({string.Join(", ", columns.Select(c => $"source.{SqlDefine.WrapColumn(c)}"))})";

        return mergeStatement;
    }

    /// <inheritdoc />
    public string GetDatabaseTypeName(Type dotNetType)
    {
        return dotNetType.Name switch
        {
            nameof(System.Int32) => "INTEGER",
            nameof(System.Int64) => "BIGINT",
            nameof(System.Decimal) => "DECIMAL(18,2)",
            nameof(System.Double) => "DOUBLE",
            nameof(System.Single) => "REAL",
            nameof(System.String) => "VARCHAR(4000)",
            nameof(System.DateTime) => "TIMESTAMP",
            nameof(System.Boolean) => "BOOLEAN",
            nameof(System.Byte) => "SMALLINT",
            nameof(System.Guid) => "CHAR(36)",
            _ => "VARCHAR(4000)"
        };
    }

    /// <inheritdoc />
    public string FormatDateTime(DateTime dateTime)
    {
        return $"TIMESTAMP('{dateTime:yyyy-MM-dd HH:mm:ss.fff}')";
    }

    /// <inheritdoc />
    public string GetCurrentDateTimeSyntax()
    {
        return "CURRENT_TIMESTAMP";
    }

    /// <inheritdoc />
    public string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        // DB2 uses CONCAT function or || operator
        return string.Join(" || ", expressions);
    }
}

