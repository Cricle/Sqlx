// -----------------------------------------------------------------------
// <copyright file="MySqlDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// MySQL database dialect provider with MySQL-specific SQL syntax.
/// </summary>
internal class MySqlDialectProvider : BaseDialectProvider
{
    /// <inheritdoc />
    public override SqlDefine SqlDefine => SqlDefine.MySql;

    /// <inheritdoc />
    public override Generator.SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    /// <inheritdoc />
    public override string GenerateLimitClause(int? limit, int? offset) =>
        (limit, offset) switch
        {
            (not null, not null) => $"LIMIT {limit} OFFSET {offset}",  // Modern syntax (MySQL 5.0+)
            (not null, null) => $"LIMIT {limit}",
            (null, not null) => throw new ArgumentException("MySQL requires LIMIT when OFFSET is specified. Use LIMIT with a very large number if you need OFFSET only."),
            _ => string.Empty
        };

    /// <inheritdoc />
    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var (wrappedTableName, wrappedColumns, parameters) = GetInsertParts(tableName, columns);
        // 性能优化：缓存重复的字符串连接结果
        var columnsJoined = string.Join(", ", wrappedColumns);
        var parametersJoined = string.Join(", ", parameters);
        return $"INSERT INTO {wrappedTableName} ({columnsJoined}) VALUES ({parametersJoined}); SELECT LAST_INSERT_ID()";
    }

    /// <inheritdoc />
    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var (wrappedTableName, wrappedColumns, parameters) = GetInsertParts(tableName, columns);
        var insertStatement = $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
                             $"VALUES ({string.Join(", ", parameters)})";

        var updateClauses = columns.Where(c => !keyColumns.Contains(c))
                                   .Select(c => $"{SqlDefine.WrapColumn(c)} = VALUES({SqlDefine.WrapColumn(c)})");

        return $"{insertStatement} ON DUPLICATE KEY UPDATE {string.Join(", ", updateClauses)}";
    }

    /// <inheritdoc />
    public override string GetDatabaseTypeName(Type dotNetType)
    {
        return dotNetType.Name switch
        {
            nameof(System.Int32) => "INT",
            nameof(System.Int64) => "BIGINT",
            nameof(System.Decimal) => "DECIMAL(18,2)",
            nameof(System.Double) => "DOUBLE",
            nameof(System.Single) => "FLOAT",
            nameof(System.String) => "VARCHAR(4000)",
            nameof(System.DateTime) => "DATETIME",
            nameof(System.Boolean) => "BOOLEAN",
            nameof(System.Byte) => "TINYINT",
            nameof(System.Guid) => "CHAR(36)",
            _ => "VARCHAR(4000)"
        };
    }

    /// <inheritdoc />
    public override string FormatDateTime(DateTime dateTime)
    {
        return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
    }

    /// <inheritdoc />
    public override string GetCurrentDateTimeSyntax()
    {
        return "NOW()";
    }

    /// <inheritdoc />
    public override string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        return $"CONCAT({string.Join(", ", expressions)})";
    }

    /// <inheritdoc />
    public override string GetReturningIdClause()
    {
        // MySQL doesn't support RETURNING clause, needs to use LAST_INSERT_ID()
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
        requiresOrderBy = false;
        return $"LIMIT {limitParam} OFFSET {offsetParam}";
    }
}

