// -----------------------------------------------------------------------
// <copyright file="SqlServerDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// SQL Server database dialect provider with SQL Server-specific SQL syntax.
/// </summary>
internal class SqlServerDialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public SqlDefine SqlDefine => SqlDefine.SqlServer;

    /// <inheritdoc />
    public SqlDefineTypes DialectType => SqlDefineTypes.SqlServer;

    /// <inheritdoc />
    public string GenerateLimitClause(int? limit, int? offset)
    {
        if (limit.HasValue && offset.HasValue)
        {
            return $"OFFSET {offset.Value} ROWS FETCH NEXT {limit.Value} ROWS ONLY";
        }
        else if (limit.HasValue)
        {
            return $"OFFSET 0 ROWS FETCH NEXT {limit.Value} ROWS ONLY";
        }
        else if (offset.HasValue)
        {
            return $"OFFSET {offset.Value} ROWS";
        }
        return string.Empty;
    }

    /// <inheritdoc />
    public string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}").ToArray();

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"OUTPUT INSERTED.{SqlDefine.WrapColumn("Id")} " +
               $"VALUES ({string.Join(", ", parameters)})";
    }

    /// <inheritdoc />
    public string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
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
    public string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var wrappedKeyColumns = keyColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        // SQL Server MERGE statement
        var mergeStatement = $"MERGE {wrappedTableName} AS target\n";
        mergeStatement += "USING (\n";
        mergeStatement += $"  VALUES ({string.Join(", ", columns.Select(c => $"@{c.ToLowerInvariant()}"))})\n";
        mergeStatement += $") AS source ({string.Join(", ", wrappedColumns)})\n";
        mergeStatement += $"ON ({string.Join(" AND ", keyColumns.Select(k => $"target.{SqlDefine.WrapColumn(k)} = source.{SqlDefine.WrapColumn(k)}"))})";
        mergeStatement += "\nWHEN MATCHED THEN\n";
        mergeStatement += $"  UPDATE SET {string.Join(", ", columns.Where(c => !keyColumns.Contains(c)).Select(c => $"{SqlDefine.WrapColumn(c)} = source.{SqlDefine.WrapColumn(c)}"))}\n";
        mergeStatement += "WHEN NOT MATCHED THEN\n";
        mergeStatement += $"  INSERT ({string.Join(", ", wrappedColumns)}) VALUES ({string.Join(", ", columns.Select(c => $"source.{SqlDefine.WrapColumn(c)}"))});";

        return mergeStatement;
    }

    /// <inheritdoc />
    public string GetDatabaseTypeName(Type dotNetType)
    {
        return dotNetType.Name switch
        {
            nameof(System.Int32) => "INT",
            nameof(System.Int64) => "BIGINT",
            nameof(System.Decimal) => "DECIMAL(18,2)",
            nameof(System.Double) => "FLOAT",
            nameof(System.Single) => "REAL",
            nameof(System.String) => "NVARCHAR(4000)",
            nameof(System.DateTime) => "DATETIME2",
            nameof(System.Boolean) => "BIT",
            nameof(System.Byte) => "TINYINT",
            nameof(System.Guid) => "UNIQUEIDENTIFIER",
            _ => "NVARCHAR(4000)"
        };
    }

    /// <inheritdoc />
    public string FormatDateTime(DateTime dateTime)
    {
        return $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
    }

    /// <inheritdoc />
    public string GetCurrentDateTimeSyntax()
    {
        return "GETDATE()";
    }

    /// <inheritdoc />
    public string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        return string.Join(" + ", expressions);
    }
}

