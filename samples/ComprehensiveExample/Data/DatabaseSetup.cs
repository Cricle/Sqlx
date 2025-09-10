// -----------------------------------------------------------------------
// <copyright file="DatabaseSetup.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ComprehensiveExample.Data;

/// <summary>
/// 数据库设置和初始化
/// </summary>
public static class DatabaseSetup
{
    /// <summary>
    /// 创建数据库连接
    /// </summary>
    public static DbConnection CreateConnection()
    {
        var connectionString = "Data Source=:memory:";
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// 初始化数据库架构
    /// </summary>
    public static async Task InitializeDatabaseAsync(DbConnection connection)
    {
        // 创建部门表
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS departments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )");

        // 创建用户表
        await ExecuteSqlAsync(connection, @"
            CREATE TABLE IF NOT EXISTS users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                DepartmentId INTEGER,
                FOREIGN KEY (DepartmentId) REFERENCES departments(Id)
            )");

        // 插入示例数据
        await InsertSampleDataAsync(connection);
    }

    /// <summary>
    /// 插入示例数据
    /// </summary>
    private static async Task InsertSampleDataAsync(DbConnection connection)
    {
        // 插入部门数据
        await ExecuteSqlAsync(connection, @"
            INSERT OR IGNORE INTO departments (Id, Name, Description) VALUES 
            (1, '技术部', '负责产品开发和技术支持'),
            (2, '销售部', '负责产品销售和客户关系'),
            (3, '人事部', '负责人员招聘和管理')");

        // 插入用户数据
        await ExecuteSqlAsync(connection, @"
            INSERT OR IGNORE INTO users (Id, Name, Email, IsActive, DepartmentId) VALUES 
            (1, '张三', 'zhangsan@example.com', 1, 1),
            (2, '李四', 'lisi@example.com', 1, 1),
            (3, '王五', 'wangwu@example.com', 0, 2),
            (4, '赵六', 'zhaoliu@example.com', 1, 2),
            (5, '钱七', 'qianqi@example.com', 1, 3)");
    }

    /// <summary>
    /// 执行 SQL 命令
    /// </summary>
    private static async Task ExecuteSqlAsync(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
