// -----------------------------------------------------------------------
// <copyright file="BatchOperationHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Sqlx.SqlGen;
using System;

namespace Sqlx.Core;

/// <summary>
/// Helper class for batch operations to avoid static/instance method conflicts.
/// </summary>
internal static class BatchOperationHelper
{
    /// <summary>
    /// Handles batch INSERT operations.
    /// </summary>
    public static string HandleBatchInsertOperation(string tableName)
    {
        return $"BATCH_INSERT:{tableName}";
    }

    /// <summary>
    /// Handles batch UPDATE operations.
    /// </summary>
    public static string HandleBatchUpdateOperation(string tableName)
    {
        return $"BATCH_UPDATE:{tableName}";
    }

    /// <summary>
    /// Handles batch DELETE operations.
    /// </summary>
    public static string HandleBatchDeleteOperation(string tableName)
    {
        return $"BATCH_DELETE:{tableName}";
    }

    /// <summary>
    /// Checks if a SQL operation is a batch operation.
    /// </summary>
    public static bool IsBatchOperation(string sql)
    {
        return sql != null && sql.StartsWith("BATCH_");
    }

    /// <summary>
    /// Gets the SQL for a specific SQL execute type including batch operations.
    /// </summary>
    public static string GetBatchSql(SqlExecuteTypes type, string tableName)
    {
        return type switch
        {
            SqlExecuteTypes.BatchInsert => HandleBatchInsertOperation(tableName),
            SqlExecuteTypes.BatchUpdate => HandleBatchUpdateOperation(tableName),
            SqlExecuteTypes.BatchDelete => HandleBatchDeleteOperation(tableName),
            _ => string.Empty
        };
    }
}

