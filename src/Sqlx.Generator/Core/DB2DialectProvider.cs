// -----------------------------------------------------------------------
// <copyright file="DB2DialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// DB2 database dialect provider.
/// DB2 uses :param format for parameters.
/// </summary>
internal class DB2DialectProvider : BaseDialectProvider
{
    /// <inheritdoc />
    public override SqlDefine SqlDefine => SqlDefine.DB2;

    /// <inheritdoc />
    public override Generator.SqlDefineTypes DialectType => SqlDefineTypes.DB2;

    /// <inheritdoc />
    public override string GenerateLimitClause(int? limit, int? offset)
    {
        // DB2 uses FETCH FIRST n ROWS ONLY
        if (limit.HasValue && offset.HasValue)
        {
            return $"OFFSET {offset.Value} ROWS FETCH FIRST {limit.Value} ROWS ONLY";
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
    public override string GenerateInsertWithReturning(string tableName, string[] columns)
    {
        // DB2 doesn't support RETURNING clause like PostgreSQL
        // Use SELECT FROM FINAL TABLE instead
        var (wrappedTableName, wrappedColumns, parameters) = GetInsertParts(tableName, columns);
        
        return $"SELECT id FROM FINAL TABLE (" +
               $"INSERT INTO {wrappedTableName} ({string.Join(", ", wrappedColumns)}) " +
               $"VALUES ({string.Join(", ", parameters)}))";
    }

    /// <inheritdoc />
    public override string GenerateUpsert(string tableName, string[] columns, string[] keyColumns)
    {
        // DB2 uses MERGE statement for upsert
        var wrappedTableName = SqlDefine.WrapColumn(tableName);
        var wrappedColumns = columns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        var wrappedKeyColumns = keyColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        
        var updateColumns = columns.Except(keyColumns).ToArray();
        var wrappedUpdateColumns = updateColumns.Select(c => SqlDefine.WrapColumn(c)).ToArray();
        
        var matchConditions = string.Join(" AND ", 
            wrappedKeyColumns.Select(k => $"target.{k} = source.{k}"));
        
        var updateSet = string.Join(", ", 
            wrappedUpdateColumns.Select(c => $"target.{c} = source.{c}"));
        
        var insertColumns = string.Join(", ", wrappedColumns);
        var insertValues = string.Join(", ", wrappedColumns.Select(c => $"source.{c}"));
        
        return $"MERGE INTO {wrappedTableName} AS target " +
               $"USING (VALUES ({string.Join(", ", columns.Select(c => $":{c.ToLowerInvariant()}"))})) " +
               $"AS source ({insertColumns}) " +
               $"ON {matchConditions} " +
               $"WHEN MATCHED THEN UPDATE SET {updateSet} " +
               $"WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues})";
    }

    /// <inheritdoc />
    public override string GetDatabaseTypeName(Type dotNetType)
    {
        return dotNetType.Name switch
        {
            "Int32" => "INTEGER",
            "Int64" => "BIGINT",
            "String" => "VARCHAR(255)",
            "Boolean" => "SMALLINT",
            "DateTime" => "TIMESTAMP",
            "Decimal" => "DECIMAL(18,2)",
            "Double" => "DOUBLE",
            "Float" => "REAL",
            "Guid" => "CHAR(36)",
            _ => "VARCHAR(255)"
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
        return "CURRENT TIMESTAMP";
    }

    /// <inheritdoc />
    public override string GetConcatenationSyntax(params string[] expressions)
    {
        // DB2 uses || for concatenation
        return string.Join(" || ", expressions);
    }

    /// <inheritdoc />
    public override string GetReturningIdClause()
    {
        // DB2 doesn't support RETURNING clause
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
        
        if (!string.IsNullOrEmpty(offsetParam) && !string.IsNullOrEmpty(limitParam))
        {
            return $"OFFSET :{offsetParam} ROWS FETCH FIRST :{limitParam} ROWS ONLY";
        }
        else if (!string.IsNullOrEmpty(limitParam))
        {
            return $"FETCH FIRST :{limitParam} ROWS ONLY";
        }
        else if (!string.IsNullOrEmpty(offsetParam))
        {
            return $"OFFSET :{offsetParam} ROWS";
        }

        return string.Empty;
    }
}
