// -----------------------------------------------------------------------
// <copyright file="MySqlDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// MySQL database dialect provider with MySQL-specific SQL syntax.
/// </summary>
internal class MySqlDialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public SqlDefine SqlDefine => SqlDefine.MySql;

    /// <inheritdoc />
    public SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    /// <inheritdoc />
    public string GenerateLimitClause(int? limit, int? offset)
    {
        if (limit.HasValue && offset.HasValue)
        {
            return $"LIMIT {offset.Value}, {limit.Value}";
        }
        else if (limit.HasValue)
        {
            return $"LIMIT {limit.Value}";
        }
        else if (offset.HasValue)
        {
            return $"LIMIT {offset.Value}, 18446744073709551615"; // MySQL max value for unlimited
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
               $"VALUES ({string.Join(", ", parameters)}); SELECT LAST_INSERT_ID()";
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
        var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}").ToArray();

        var insertStatement = $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
                             $"VALUES ({string.Join(", ", parameters)})";

        var updateClauses = columns.Where(c => !keyColumns.Contains(c))
                                   .Select(c => $"{SqlDefine.WrapColumn(c)} = VALUES({SqlDefine.WrapColumn(c)})");

        return $"{insertStatement} ON DUPLICATE KEY UPDATE {string.Join(", ", updateClauses)}";
    }

    /// <inheritdoc />
    public string GetDatabaseTypeName(Type dotNetType)
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
    public string FormatDateTime(System.DateTime dateTime)
    {
        return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
    }

    /// <inheritdoc />
    public string GetCurrentDateTimeSyntax()
    {
        return "NOW()";
    }

    /// <inheritdoc />
    public string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        return $"CONCAT({string.Join(", ", expressions)})";
    }
}

