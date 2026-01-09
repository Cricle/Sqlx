// -----------------------------------------------------------------------
// <copyright file="DatabaseTestHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sqlx.Tests.Infrastructure;

/// <summary>
/// 数据库测试辅助类，提供测试数据清理和隔离支持
/// </summary>
public static class DatabaseTestHelper
{
    /// <summary>
    /// 清理指定表的所有数据
    /// </summary>
    public static async Task CleanupTableAsync(DbConnection connection, string tableName)
    {
        if (connection == null || string.IsNullOrEmpty(tableName))
            return;

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"DELETE FROM {tableName}";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to cleanup table {tableName}: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理指定表的所有数据（支持多个表）
    /// </summary>
    public static async Task CleanupTablesAsync(DbConnection connection, params string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            await CleanupTableAsync(connection, tableName);
        }
    }

    /// <summary>
    /// 删除指定表（如果存在）
    /// </summary>
    public static async Task DropTableIfExistsAsync(DbConnection connection, string tableName, SqlDefineTypes dialectType)
    {
        if (connection == null || string.IsNullOrEmpty(tableName))
            return;

        try
        {
            using var cmd = connection.CreateCommand();
            
            // 根据不同的数据库方言使用不同的语法
            cmd.CommandText = dialectType switch
            {
                SqlDefineTypes.MySql => $"DROP TABLE IF EXISTS `{tableName}`",
                SqlDefineTypes.PostgreSql => $"DROP TABLE IF EXISTS \"{tableName}\" CASCADE",
                SqlDefineTypes.SqlServer => $"IF OBJECT_ID(N'[{tableName}]', N'U') IS NOT NULL DROP TABLE [{tableName}]",
                SqlDefineTypes.SQLite => $"DROP TABLE IF EXISTS {tableName}",
                _ => $"DROP TABLE IF EXISTS {tableName}"
            };
            
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to drop table {tableName}: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查表是否存在
    /// </summary>
    public static async Task<bool> TableExistsAsync(DbConnection connection, string tableName, SqlDefineTypes dialectType)
    {
        if (connection == null || string.IsNullOrEmpty(tableName))
            return false;

        try
        {
            using var cmd = connection.CreateCommand();
            
            cmd.CommandText = dialectType switch
            {
                SqlDefineTypes.MySql => 
                    $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'",
                SqlDefineTypes.PostgreSql => 
                    $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}'",
                SqlDefineTypes.SqlServer => 
                    $"SELECT COUNT(*) FROM sys.tables WHERE name = '{tableName}'",
                SqlDefineTypes.SQLite => 
                    $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'",
                _ => 
                    $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'"
            };
            
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to check if table {tableName} exists: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取表中的记录数
    /// </summary>
    public static async Task<long> GetTableRowCountAsync(DbConnection connection, string tableName)
    {
        if (connection == null || string.IsNullOrEmpty(tableName))
            return 0;

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to get row count for table {tableName}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 截断表（保留结构，删除所有数据，重置自增ID）
    /// 注意：某些数据库不支持 TRUNCATE，会降级到 DELETE
    /// </summary>
    public static async Task TruncateTableAsync(DbConnection connection, string tableName, SqlDefineTypes dialectType)
    {
        if (connection == null || string.IsNullOrEmpty(tableName))
            return;

        try
        {
            using var cmd = connection.CreateCommand();
            
            // SQLite 不支持 TRUNCATE，使用 DELETE
            if (dialectType == SqlDefineTypes.SQLite)
            {
                cmd.CommandText = $"DELETE FROM {tableName}";
            }
            else
            {
                cmd.CommandText = dialectType switch
                {
                    SqlDefineTypes.MySql => $"TRUNCATE TABLE `{tableName}`",
                    SqlDefineTypes.PostgreSql => $"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE",
                    SqlDefineTypes.SqlServer => $"TRUNCATE TABLE [{tableName}]",
                    _ => $"TRUNCATE TABLE {tableName}"
                };
            }
            
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // 如果 TRUNCATE 失败（可能因为外键约束），尝试使用 DELETE
            try
            {
                using var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = $"DELETE FROM {tableName}";
                await deleteCmd.ExecuteNonQueryAsync();
            }
            catch (Exception deleteEx)
            {
                Console.WriteLine($"⚠️ Failed to truncate/delete table {tableName}: {ex.Message}, {deleteEx.Message}");
            }
        }
    }
}
