// -----------------------------------------------------------------------
// <copyright file="OracleDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// Oracle database dialect provider with Oracle-specific SQL syntax.
/// </summary>
internal class OracleDialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public SqlDefine SqlDefine => SqlDefine.Oracle;

    /// <inheritdoc />
    public SqlDefineTypes DialectType => SqlDefineTypes.Oracle;

    /// <inheritdoc />
    public string GenerateLimitClause(int? limit, int? offset)
    {
        // Oracle uses ROWNUM or OFFSET/FETCH (12c+)
        if (limit.HasValue && offset.HasValue)
        {
            return $"OFFSET {offset.Value} ROWS FETCH NEXT {limit.Value} ROWS ONLY";
        }
        else if (limit.HasValue)
        {
            return $"FETCH FIRST {limit.Value} ROWS ONLY";
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
        var parameters = columns.Select(c => $":{c.ToLowerInvariant()}").ToArray();

        return $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES ({string.Join(", ", parameters)}) " +
               $"RETURNING {SqlDefine.WrapColumn("Id")} INTO :id";
    }

    /// <inheritdoc />
    public string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        // Oracle supports INSERT ALL for batch inserts
        var insertAll = "INSERT ALL\n";
        for (int i = 0; i < batchSize; i++)
        {
            var parameters = columns.Select(c => $":{c.ToLowerInvariant()}{i}").ToArray();
            insertAll += $"  INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) VALUES ({string.Join(", ", parameters)})\n";
        }
        insertAll += "SELECT 1 FROM DUAL";

        return insertAll;
    }

    /// <inheritdoc />
    public string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var wrappedKeyColumns = keyColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();

        // Oracle MERGE statement
        var mergeStatement = $"MERGE INTO {wrappedTableName} target\n";
        mergeStatement += "USING (\n";
        mergeStatement += $"  SELECT {string.Join(", ", columns.Select(c => $":{c.ToLowerInvariant()} AS {SqlDefine.WrapColumn(c)}"))}\n";
        mergeStatement += "  FROM DUAL\n";
        mergeStatement += ") source\n";
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
            nameof(System.Int32) => "NUMBER(10)",
            nameof(System.Int64) => "NUMBER(19)",
            nameof(System.Decimal) => "NUMBER(18,2)",
            nameof(System.Double) => "BINARY_DOUBLE",
            nameof(System.Single) => "BINARY_FLOAT",
            nameof(System.String) => "NVARCHAR2(4000)",
            nameof(System.DateTime) => "TIMESTAMP",
            nameof(System.Boolean) => "NUMBER(1)",
            nameof(System.Byte) => "NUMBER(3)",
            nameof(System.Guid) => "RAW(16)",
            _ => "NVARCHAR2(4000)"
        };
    }

    /// <inheritdoc />
    public string FormatDateTime(DateTime dateTime)
    {
        return $"TIMESTAMP '{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
    }

    /// <inheritdoc />
    public string GetCurrentDateTimeSyntax()
    {
        return "SYSTIMESTAMP";
    }

    /// <inheritdoc />
    public string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        // Oracle uses || for concatenation
        return string.Join(" || ", expressions);
    }
}

