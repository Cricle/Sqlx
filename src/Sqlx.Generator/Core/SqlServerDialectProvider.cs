// -----------------------------------------------------------------------
// <copyright file="SqlServerDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// SQL Server database dialect provider with SQL Server-specific SQL syntax.
/// </summary>
internal class SqlServerDialectProvider : BaseDialectProvider
{
    /// <inheritdoc />
    public override SqlDefine SqlDefine => SqlDefine.SqlServer;

    /// <inheritdoc />
    public override SqlDefineTypes DialectType => SqlDefineTypes.SqlServer;

    /// <inheritdoc />
    public override string GenerateLimitClause(int? limit, int? offset) =>
        (limit, offset) switch
        {
            (not null, not null) => $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY",
            (not null, null) => $"OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY",
            (null, not null) => $"OFFSET {offset} ROWS",
            _ => string.Empty
        };

    /// <inheritdoc />
    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var (wrappedTableName, wrappedColumns, parameters) = GetInsertParts(tableName, columns);
        // 性能优化：缓存重复的字符串连接结果
        var columnsJoined = string.Join(", ", wrappedColumns);
        var parametersJoined = string.Join(", ", parameters);
        return $"INSERT INTO {wrappedTableName} ({columnsJoined}) OUTPUT INSERTED.{SqlDefine.WrapColumn("Id")} VALUES ({parametersJoined})";
    }

    /// <inheritdoc />
    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
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
        // 性能优化：缓存重复的字符串连接和计算结果
        var columnsJoined = string.Join(", ", wrappedColumns);
        var sourceColumnsJoined = string.Join(", ", columns.Select(c => $"source.{SqlDefine.WrapColumn(c)}"));
        mergeStatement += $"  INSERT ({columnsJoined}) VALUES ({sourceColumnsJoined});";

        return mergeStatement;
    }

    /// <inheritdoc />
    public override string GetDatabaseTypeName(Type dotNetType)
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
    public override string FormatDateTime(DateTime dateTime)
    {
        return $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
    }

    /// <inheritdoc />
    public override string GetCurrentDateTimeSyntax()
    {
        return "GETDATE()";
    }

    /// <inheritdoc />
    public override string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        return string.Join(" + ", expressions);
    }

    /// <inheritdoc />
    public override string GetReturningIdClause()
    {
        // SQL Server doesn't support RETURNING clause, needs to use SCOPE_IDENTITY()
        return string.Empty;
    }

    /// <inheritdoc />
    public override string GetBoolTrueLiteral()
    {
        return "1";
    }

    /// <inheritdoc />
    public override string GetBoolFalseLiteral()
    {
        return "0";
    }

    /// <inheritdoc />
    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = true; // SQL Server requires ORDER BY for OFFSET/FETCH
        return $"OFFSET {offsetParam} ROWS FETCH NEXT {limitParam} ROWS ONLY";
    }
}

