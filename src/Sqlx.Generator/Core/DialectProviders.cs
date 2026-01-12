// -----------------------------------------------------------------------
// <copyright file="DialectProviders.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// Database dialect provider interface for SQL generation.
/// </summary>
internal interface IDatabaseDialectProvider
{
    SqlDefine SqlDefine { get; }
    SqlDefineTypes DialectType { get; }
    string GenerateLimitClause(int? limit, int? offset);
    string GenerateInsertWithReturning(string tableName, string[] columns);
    string GenerateBatchInsert(string tableName, string[] columns, int batchSize);
    string GenerateUpsert(string tableName, string[] columns, string[] keyColumns);
    string GetDatabaseTypeName(Type dotNetType);
    string FormatDateTime(DateTime dateTime);
    string GetCurrentDateTimeSyntax();
    string GetConcatenationSyntax(params string[] expressions);
    string ReplacePlaceholders(string sqlTemplate, string? tableName = null, string[]? columns = null);
    string GetReturningIdClause();
    string GetBoolTrueLiteral();
    string GetBoolFalseLiteral();
    string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy);
}

/// <summary>
/// Base class for database dialect providers with shared functionality.
/// </summary>
internal abstract class BaseDialectProvider : IDatabaseDialectProvider
{
    // Shared type mappings for all dialects
    private static readonly Dictionary<(SqlDefineTypes, string), string> TypeMappings = new()
    {
        // SQL Server
        { (SqlDefineTypes.SqlServer, nameof(Int16)), "SMALLINT" },
        { (SqlDefineTypes.SqlServer, nameof(Int32)), "INT" },
        { (SqlDefineTypes.SqlServer, nameof(Int64)), "BIGINT" },
        { (SqlDefineTypes.SqlServer, nameof(Decimal)), "DECIMAL(18,2)" },
        { (SqlDefineTypes.SqlServer, nameof(Double)), "FLOAT" },
        { (SqlDefineTypes.SqlServer, nameof(Single)), "REAL" },
        { (SqlDefineTypes.SqlServer, nameof(String)), "NVARCHAR(4000)" },
        { (SqlDefineTypes.SqlServer, nameof(DateTime)), "DATETIME2" },
        { (SqlDefineTypes.SqlServer, nameof(Boolean)), "BIT" },
        { (SqlDefineTypes.SqlServer, nameof(Byte)), "TINYINT" },
        { (SqlDefineTypes.SqlServer, nameof(Guid)), "UNIQUEIDENTIFIER" },
        { (SqlDefineTypes.SqlServer, "Byte[]"), "VARBINARY(MAX)" },
        // MySQL
        { (SqlDefineTypes.MySql, nameof(Int16)), "SMALLINT" },
        { (SqlDefineTypes.MySql, nameof(Int32)), "INT" },
        { (SqlDefineTypes.MySql, nameof(Int64)), "BIGINT" },
        { (SqlDefineTypes.MySql, nameof(Decimal)), "DECIMAL(18,2)" },
        { (SqlDefineTypes.MySql, nameof(Double)), "DOUBLE" },
        { (SqlDefineTypes.MySql, nameof(Single)), "FLOAT" },
        { (SqlDefineTypes.MySql, nameof(String)), "VARCHAR(4000)" },
        { (SqlDefineTypes.MySql, nameof(DateTime)), "DATETIME" },
        { (SqlDefineTypes.MySql, nameof(Boolean)), "BOOLEAN" },
        { (SqlDefineTypes.MySql, nameof(Byte)), "TINYINT" },
        { (SqlDefineTypes.MySql, nameof(Guid)), "CHAR(36)" },
        { (SqlDefineTypes.MySql, "Byte[]"), "BLOB" },
        // PostgreSQL
        { (SqlDefineTypes.PostgreSql, nameof(Int16)), "SMALLINT" },
        { (SqlDefineTypes.PostgreSql, nameof(Int32)), "INTEGER" },
        { (SqlDefineTypes.PostgreSql, nameof(Int64)), "BIGINT" },
        { (SqlDefineTypes.PostgreSql, nameof(Decimal)), "DECIMAL(18,2)" },
        { (SqlDefineTypes.PostgreSql, nameof(Double)), "DOUBLE PRECISION" },
        { (SqlDefineTypes.PostgreSql, nameof(Single)), "REAL" },
        { (SqlDefineTypes.PostgreSql, nameof(String)), "VARCHAR(4000)" },
        { (SqlDefineTypes.PostgreSql, nameof(DateTime)), "TIMESTAMP" },
        { (SqlDefineTypes.PostgreSql, nameof(Boolean)), "BOOLEAN" },
        { (SqlDefineTypes.PostgreSql, nameof(Byte)), "SMALLINT" },
        { (SqlDefineTypes.PostgreSql, nameof(Guid)), "UUID" },
        { (SqlDefineTypes.PostgreSql, "Byte[]"), "BYTEA" },
        // SQLite
        { (SqlDefineTypes.SQLite, nameof(Int16)), "INTEGER" },
        { (SqlDefineTypes.SQLite, nameof(Int32)), "INTEGER" },
        { (SqlDefineTypes.SQLite, nameof(Int64)), "INTEGER" },
        { (SqlDefineTypes.SQLite, nameof(Decimal)), "REAL" },
        { (SqlDefineTypes.SQLite, nameof(Double)), "REAL" },
        { (SqlDefineTypes.SQLite, nameof(Single)), "REAL" },
        { (SqlDefineTypes.SQLite, nameof(String)), "TEXT" },
        { (SqlDefineTypes.SQLite, nameof(DateTime)), "TEXT" },
        { (SqlDefineTypes.SQLite, nameof(Boolean)), "INTEGER" },
        { (SqlDefineTypes.SQLite, nameof(Byte)), "INTEGER" },
        { (SqlDefineTypes.SQLite, nameof(Guid)), "TEXT" },
        { (SqlDefineTypes.SQLite, "Byte[]"), "BLOB" },
    };

    public abstract SqlDefine SqlDefine { get; }
    public abstract SqlDefineTypes DialectType { get; }
    public abstract string GenerateLimitClause(int? limit, int? offset);
    public abstract string GenerateInsertWithReturning(string tableName, string[] columns);
    public abstract string GenerateUpsert(string tableName, string[] columns, string[] keyColumns);
    public abstract string FormatDateTime(DateTime dateTime);
    public abstract string GetCurrentDateTimeSyntax();
    public abstract string GetConcatenationSyntax(params string[] expressions);
    public abstract string GetReturningIdClause();
    public abstract string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy);

    // Default implementations - override only when different
    public virtual string GetBoolTrueLiteral() => "1";
    public virtual string GetBoolFalseLiteral() => "0";

    public virtual string GetDatabaseTypeName(Type dotNetType)
    {
        var key = (DialectType, dotNetType.Name);
        return TypeMappings.TryGetValue(key, out var result) ? result : "VARCHAR(4000)";
    }

    public virtual string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var wrappedTable = SqlDefine.WrapColumn(tableName);
        var wrappedCols = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var values = new string[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            var pars = columns.Select(c => $"@{c.ToLowerInvariant()}{i}").ToArray();
            values[i] = $"({string.Join(", ", pars)})";
        }
        return $"INSERT INTO {wrappedTable} ({string.Join(", ", wrappedCols)}) VALUES {string.Join(", ", values)}";
    }

    public virtual string ReplacePlaceholders(string sqlTemplate, string? tableName = null, string[]? columns = null)
    {
        if (string.IsNullOrEmpty(sqlTemplate)) return sqlTemplate;
        var result = sqlTemplate;
        if (tableName != null && result.Contains(Core.DialectPlaceholders.Table))
            result = result.Replace(Core.DialectPlaceholders.Table, SqlDefine.WrapColumn(tableName));
        if (columns?.Length > 0 && result.Contains(Core.DialectPlaceholders.Columns))
            result = result.Replace(Core.DialectPlaceholders.Columns, string.Join(", ", columns.Select(c => SqlDefine.WrapColumn(c))));
        if (result.Contains(Core.DialectPlaceholders.ReturningId))
            result = result.Replace(Core.DialectPlaceholders.ReturningId, GetReturningIdClause());
        if (result.Contains(Core.DialectPlaceholders.BoolTrue))
            result = result.Replace(Core.DialectPlaceholders.BoolTrue, GetBoolTrueLiteral());
        if (result.Contains(Core.DialectPlaceholders.BoolFalse))
            result = result.Replace(Core.DialectPlaceholders.BoolFalse, GetBoolFalseLiteral());
        if (result.Contains(Core.DialectPlaceholders.CurrentTimestamp))
            result = result.Replace(Core.DialectPlaceholders.CurrentTimestamp, GetCurrentDateTimeSyntax());
        return result;
    }

    protected (string table, string[] cols, string[] pars) GetInsertParts(string tableName, string[] columns)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var c = columns.Select(x => SqlDefine.WrapColumn(x)).ToArray();
        var p = columns.Select(x => $"@{x.ToLowerInvariant()}").ToArray();
        return (t, c, p);
    }

    protected static string JoinCols(string[] cols) => string.Join(", ", cols);
}

/// <summary>
/// SQL Server dialect provider.
/// </summary>
internal sealed class SqlServerDialectProvider : BaseDialectProvider
{
    public override SqlDefine SqlDefine => SqlDefine.SqlServer;
    public override SqlDefineTypes DialectType => SqlDefineTypes.SqlServer;

    public override string GenerateLimitClause(int? limit, int? offset) =>
        (limit, offset) switch
        {
            (not null, not null) => $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY",
            (not null, null) => $"OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY",
            (null, not null) => $"OFFSET {offset} ROWS",
            _ => string.Empty
        };

    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var (t, c, p) = GetInsertParts(tableName, columns);
        return $"INSERT INTO {t} ({JoinCols(c)}) OUTPUT INSERTED.{SqlDefine.WrapColumn("Id")} VALUES ({JoinCols(p)})";
    }

    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var cols = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var pars = columns.Select(c => $"@{c.ToLowerInvariant()}").ToArray();
        var srcCols = columns.Select(c => $"source.{SqlDefine.WrapColumn(c)}").ToArray();
        var updates = columns.Where(c => !keyColumns.Contains(c))
            .Select(c => $"{SqlDefine.WrapColumn(c)} = source.{SqlDefine.WrapColumn(c)}");
        var onClause = keyColumns.Select(k => $"target.{SqlDefine.WrapColumn(k)} = source.{SqlDefine.WrapColumn(k)}");

        return $"MERGE {t} AS target\nUSING (\n  VALUES ({JoinCols(pars)})\n) AS source ({JoinCols(cols)})\n" +
               $"ON ({string.Join(" AND ", onClause)})\nWHEN MATCHED THEN\n  UPDATE SET {string.Join(", ", updates)}\n" +
               $"WHEN NOT MATCHED THEN\n  INSERT ({JoinCols(cols)}) VALUES ({JoinCols(srcCols)});";
    }

    public override string FormatDateTime(DateTime dateTime) => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
    public override string GetCurrentDateTimeSyntax() => "GETDATE()";
    public override string GetConcatenationSyntax(params string[] expressions) =>
        expressions.Length <= 1 ? expressions.FirstOrDefault() ?? string.Empty : string.Join(" + ", expressions);
    public override string GetReturningIdClause() => string.Empty;

    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = true;
        return $"OFFSET {offsetParam} ROWS FETCH NEXT {limitParam} ROWS ONLY";
    }
}

/// <summary>
/// MySQL dialect provider.
/// </summary>
internal sealed class MySqlDialectProvider : BaseDialectProvider
{
    public override SqlDefine SqlDefine => SqlDefine.MySql;
    public override SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    public override string GenerateLimitClause(int? limit, int? offset) =>
        (limit, offset) switch
        {
            (not null, not null) => $"LIMIT {limit} OFFSET {offset}",
            (not null, null) => $"LIMIT {limit}",
            (null, not null) => throw new ArgumentException("MySQL requires LIMIT when OFFSET is specified."),
            _ => string.Empty
        };

    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var (t, c, p) = GetInsertParts(tableName, columns);
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}); SELECT LAST_INSERT_ID()";
    }

    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var (t, c, p) = GetInsertParts(tableName, columns);
        var updates = columns.Where(col => !keyColumns.Contains(col))
            .Select(col => $"{SqlDefine.WrapColumn(col)} = VALUES({SqlDefine.WrapColumn(col)})");
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}) ON DUPLICATE KEY UPDATE {string.Join(", ", updates)}";
    }

    public override string FormatDateTime(DateTime dateTime) => $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
    public override string GetCurrentDateTimeSyntax() => "NOW()";
    public override string GetConcatenationSyntax(params string[] expressions) =>
        expressions.Length <= 1 ? expressions.FirstOrDefault() ?? string.Empty : $"CONCAT({string.Join(", ", expressions)})";
    public override string GetReturningIdClause() => string.Empty;

    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = false;
        return $"LIMIT {limitParam} OFFSET {offsetParam}";
    }
}

/// <summary>
/// PostgreSQL dialect provider.
/// </summary>
internal sealed class PostgreSqlDialectProvider : BaseDialectProvider
{
    public override SqlDefine SqlDefine => SqlDefine.PostgreSql;
    public override SqlDefineTypes DialectType => SqlDefineTypes.PostgreSql;

    public override string GenerateLimitClause(int? limit, int? offset)
    {
        var parts = new List<string>();
        if (limit.HasValue) parts.Add($"LIMIT {limit.Value}");
        if (offset.HasValue) parts.Add($"OFFSET {offset.Value}");
        return string.Join(" ", parts);
    }

    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var c = columns.Select(col => SqlDefine.WrapColumn(col)).ToArray();
        var p = columns.Select((col, i) => $"${i + 1}").ToArray();
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}) RETURNING {SqlDefine.WrapColumn("Id")}";
    }

    public override string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var c = columns.Select(col => SqlDefine.WrapColumn(col)).ToArray();
        var values = new string[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            var p = columns.Select((col, j) => $"${i * columns.Length + j + 1}").ToArray();
            values[i] = $"({JoinCols(p)})";
        }
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES {string.Join(", ", values)}";
    }

    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var c = columns.Select(col => SqlDefine.WrapColumn(col)).ToArray();
        var k = keyColumns.Select(col => SqlDefine.WrapColumn(col)).ToArray();
        var p = columns.Select((col, i) => $"${i + 1}").ToArray();
        var updates = columns.Where(col => !keyColumns.Contains(col))
            .Select(col => $"{SqlDefine.WrapColumn(col)} = EXCLUDED.{SqlDefine.WrapColumn(col)}");
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}) ON CONFLICT ({JoinCols(k)}) DO UPDATE SET {string.Join(", ", updates)}";
    }

    public override string FormatDateTime(DateTime dateTime) => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'::timestamp";
    public override string GetCurrentDateTimeSyntax() => "CURRENT_TIMESTAMP";
    public override string GetConcatenationSyntax(params string[] expressions) =>
        expressions.Length <= 1 ? expressions.FirstOrDefault() ?? string.Empty : string.Join(" || ", expressions);
    public override string GetReturningIdClause() => "RETURNING id";
    public override string GetBoolTrueLiteral() => "true";
    public override string GetBoolFalseLiteral() => "false";

    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = false;
        return $"LIMIT {limitParam} OFFSET {offsetParam}";
    }
}

/// <summary>
/// SQLite dialect provider.
/// </summary>
internal sealed class SQLiteDialectProvider : BaseDialectProvider
{
    public override SqlDefine SqlDefine => SqlDefine.SQLite;
    public override SqlDefineTypes DialectType => SqlDefineTypes.SQLite;

    public override string GenerateLimitClause(int? limit, int? offset) =>
        (limit, offset) switch
        {
            (not null, not null) => $"LIMIT {limit.Value} OFFSET {offset.Value}",
            (not null, null) => $"LIMIT {limit.Value}",
            (null, not null) => $"LIMIT -1 OFFSET {offset.Value}",
            _ => string.Empty
        };

    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var (t, c, p) = GetInsertParts(tableName, columns);
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}); SELECT last_insert_rowid()";
    }

    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var c = columns.Select(col => SqlDefine.WrapColumn(col)).ToArray();
        var k = keyColumns.Select(col => SqlDefine.WrapColumn(col)).ToArray();
        var p = columns.Select(col => $"@{col.ToLowerInvariant()}").ToArray();
        var updates = columns.Where(col => !keyColumns.Contains(col))
            .Select(col => $"{SqlDefine.WrapColumn(col)} = excluded.{SqlDefine.WrapColumn(col)}");
        return $"INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}) ON CONFLICT ({JoinCols(k)}) DO UPDATE SET {string.Join(", ", updates)}";
    }

    public override string FormatDateTime(DateTime dateTime) => $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
    public override string GetCurrentDateTimeSyntax() => "datetime('now')";
    public override string GetConcatenationSyntax(params string[] expressions) =>
        expressions.Length <= 1 ? (expressions.Length == 1 ? expressions[0] : string.Empty) : string.Join(" || ", expressions);
    public override string GetReturningIdClause() => string.Empty;

    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = false;
        return $"LIMIT {limitParam} OFFSET {offsetParam}";
    }
}


/// <summary>
/// DB2 dialect provider.
/// </summary>
internal sealed class DB2DialectProvider : BaseDialectProvider
{
    public override SqlDefine SqlDefine => SqlDefine.DB2;
    public override SqlDefineTypes DialectType => SqlDefineTypes.DB2;

    public override string GenerateLimitClause(int? limit, int? offset) =>
        (limit, offset) switch
        {
            (not null, not null) => $"OFFSET {offset.Value} ROWS FETCH FIRST {limit.Value} ROWS ONLY",
            (not null, null) => $"FETCH FIRST {limit.Value} ROWS ONLY",
            (null, not null) => $"OFFSET {offset.Value} ROWS",
            _ => string.Empty
        };

    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        var (t, c, p) = GetInsertParts(tableName, columns);
        return $"SELECT id FROM FINAL TABLE (INSERT INTO {t} ({JoinCols(c)}) VALUES ({JoinCols(p)}))";
    }

    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        var t = SqlDefine.WrapColumn(tableName);
        var cols = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var keys = keyColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var pars = columns.Select(c => $":{c.ToLowerInvariant()}").ToArray();
        var updates = columns.Where(c => !keyColumns.Contains(c))
            .Select(c => $"target.{SqlDefine.WrapColumn(c)} = source.{SqlDefine.WrapColumn(c)}");
        var onClause = keys.Select(k => $"target.{k} = source.{k}");
        var srcCols = cols.Select(c => $"source.{c}");

        return $"MERGE INTO {t} AS target USING (VALUES ({JoinCols(pars)})) AS source ({JoinCols(cols)}) " +
               $"ON {string.Join(" AND ", onClause)} WHEN MATCHED THEN UPDATE SET {string.Join(", ", updates)} " +
               $"WHEN NOT MATCHED THEN INSERT ({JoinCols(cols)}) VALUES ({string.Join(", ", srcCols)})";
    }

    public override string FormatDateTime(DateTime dateTime) => $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
    public override string GetCurrentDateTimeSyntax() => "CURRENT TIMESTAMP";
    public override string GetConcatenationSyntax(params string[] expressions) => string.Join(" || ", expressions);
    public override string GetReturningIdClause() => string.Empty;

    public override string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        requiresOrderBy = false;
        if (!string.IsNullOrEmpty(offsetParam) && !string.IsNullOrEmpty(limitParam))
            return $"OFFSET :{offsetParam} ROWS FETCH FIRST :{limitParam} ROWS ONLY";
        if (!string.IsNullOrEmpty(limitParam))
            return $"FETCH FIRST :{limitParam} ROWS ONLY";
        if (!string.IsNullOrEmpty(offsetParam))
            return $"OFFSET :{offsetParam} ROWS";
        return string.Empty;
    }
}
