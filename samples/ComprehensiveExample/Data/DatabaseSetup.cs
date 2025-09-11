// -----------------------------------------------------------------------
// <copyright file="DatabaseSetup.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
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
        return new SqliteConnection(connectionString);
    }
    
    /// <summary>
    /// 初始化数据库表结构和示例数据
    /// </summary>
    public static async Task InitializeDatabaseAsync(DbConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        
        Console.WriteLine("📋 初始化数据库表结构...");
        
        // 创建表结构
        await CreateTablesAsync(connection);
        
        // 插入示例数据
        await InsertSampleDataAsync(connection);
        
        Console.WriteLine("✅ 数据库初始化完成");
    }
    
    /// <summary>
    /// 创建所有表结构
    /// </summary>
    private static async Task CreateTablesAsync(DbConnection connection)
    {
        var tables = new[]
        {
            // 部门表
            @"CREATE TABLE departments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                description TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )",
            
            // 用户表
            @"CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT UNIQUE NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN DEFAULT 1,
                department_id INTEGER,
                FOREIGN KEY (department_id) REFERENCES departments(id)
            )",
            
            // 产品表 (Record 类型演示)
            @"CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price DECIMAL(10,2) NOT NULL,
                category_id INTEGER NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN DEFAULT 1
            )",
            
            // 订单表 (Primary Constructor 演示)
            @"CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                customer_name TEXT NOT NULL,
                order_date DATETIME DEFAULT CURRENT_TIMESTAMP,
                total_amount DECIMAL(10,2) DEFAULT 0
            )",
            
            // 订单项表 (Primary Constructor Record 演示)
            @"CREATE TABLE order_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                order_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                unit_price DECIMAL(10,2) NOT NULL,
                FOREIGN KEY (order_id) REFERENCES orders(id),
                FOREIGN KEY (product_id) REFERENCES products(id)
            )"
        };
        
        foreach (var sql in tables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }
    
    /// <summary>
    /// 插入示例数据
    /// </summary>
    private static async Task InsertSampleDataAsync(DbConnection connection)
    {
        var sampleData = new[]
        {
            // 插入部门数据
            "INSERT INTO departments (name, description) VALUES ('技术部', '负责产品开发和技术支持')",
            "INSERT INTO departments (name, description) VALUES ('人事部', '负责人力资源管理和招聘')",
            "INSERT INTO departments (name, description) VALUES ('财务部', '负责财务管理和会计核算')",
            
            // 插入产品分类数据（示例）
            // 注意：这里假设 category_id 1 表示电子产品类别
        };
        
        foreach (var sql in sampleData)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }
}