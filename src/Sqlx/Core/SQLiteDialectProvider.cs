// -----------------------------------------------------------------------
// <copyright file="SQLiteDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// SQLite database dialect provider with SQLite-specific SQL syntax.
/// </summary>
internal class SQLiteDialectProvider : IDatabaseDialectProvider
{
    /// <inheritdoc />
    public SqlDefine SqlDefine => new SqlDefine("[", "]", "'", "'", "@"); // Use @ for actual parameter generation

    /// <inheritdoc />
    public SqlDefineTypes DialectType => SqlDefineTypes.SQLite;

    /// <inheritdoc />
    public string GenerateLimitClause(int? limit, int? offset)
    {
        if (limit.HasValue && offset.HasValue)
        {
            return $"LIMIT {limit.Value} OFFSET {offset.Value}";
        }
        else if (limit.HasValue)
        {
            return $"LIMIT {limit.Value}";
        }
        else if (offset.HasValue)
        {
            return $"LIMIT -1 OFFSET {offset.Value}";
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
               $"VALUES ({string.Join(", ", parameters)}); SELECT last_insert_rowid()";
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
        var parameters = columns.Select(c => $"@{c.ToLowerInvariant()}").ToArray();

        var insertStatement = $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
                             $"VALUES ({string.Join(", ", parameters)})";

        var updateClauses = columns.Where(c => !keyColumns.Contains(c))
                                   .Select(c => $"{SqlDefine.WrapColumn(c)} = excluded.{SqlDefine.WrapColumn(c)}");

        var conflictColumns = string.Join(", ", wrappedKeyColumns);

        return $"{insertStatement} ON CONFLICT ({conflictColumns}) DO UPDATE SET {string.Join(", ", updateClauses)}";
    }

    /// <inheritdoc />
    public string GetDatabaseTypeName(Type dotNetType)
    {
        return dotNetType.Name switch
        {
            nameof(System.Int32) => "INTEGER",
            nameof(System.Int64) => "INTEGER",
            nameof(System.Decimal) => "REAL",
            nameof(System.Double) => "REAL",
            nameof(System.Single) => "REAL",
            nameof(System.String) => "TEXT",
            nameof(System.DateTime) => "TEXT", // SQLite stores dates as text
            nameof(System.Boolean) => "INTEGER",
            nameof(System.Byte) => "INTEGER",
            nameof(System.Guid) => "TEXT",
            _ => "TEXT"
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
        return "datetime('now')";
    }

    /// <inheritdoc />
    public string GetConcatenationSyntax(params string[] expressions)
    {
        if (expressions.Length <= 1)
            return expressions.FirstOrDefault() ?? string.Empty;

        // SQLite uses || for concatenation
        return string.Join(" || ", expressions);
    }
}

