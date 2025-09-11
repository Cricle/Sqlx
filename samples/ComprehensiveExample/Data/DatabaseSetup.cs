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
            )",
            
            // 客户表 (Primary Constructor 演示)
            @"CREATE TABLE customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT UNIQUE NOT NULL,
                birth_date DATETIME NOT NULL,
                status INTEGER DEFAULT 1,
                total_spent DECIMAL(12,2) DEFAULT 0,
                referred_by INTEGER,
                address TEXT DEFAULT '',
                phone TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                last_login_at DATETIME,
                is_vip BOOLEAN DEFAULT 0,
                FOREIGN KEY (referred_by) REFERENCES customers(id)
            )",
            
            // 审计日志表 (Primary Constructor + Record 演示)
            @"CREATE TABLE audit_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                action TEXT NOT NULL,
                entity_type TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                old_values TEXT,
                new_values TEXT,
                ip_address TEXT,
                user_agent TEXT
            )",
            
            // 产品分类表
            @"CREATE TABLE categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                description TEXT DEFAULT '',
                parent_id INTEGER,
                sort_order INTEGER DEFAULT 0,
                is_active BOOLEAN DEFAULT 1,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (parent_id) REFERENCES categories(id)
            )",
            
            // 库存表 (Record 演示)
            @"CREATE TABLE inventory (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_id INTEGER NOT NULL UNIQUE,
                quantity INTEGER NOT NULL DEFAULT 0,
                reorder_level DECIMAL(10,2) NOT NULL DEFAULT 0,
                last_updated DATETIME DEFAULT CURRENT_TIMESTAMP,
                warehouse_location TEXT,
                reserved_quantity DECIMAL(10,2) DEFAULT 0,
                FOREIGN KEY (product_id) REFERENCES products(id)
            )",
            
            // 订单统计视图
            @"CREATE VIEW IF NOT EXISTS order_summary_view AS
                SELECT 
                    c.id AS customer_id,
                    c.name AS customer_name,
                    COUNT(o.id) AS total_orders,
                    COALESCE(SUM(o.total_amount), 0) AS total_amount,
                    COALESCE(AVG(o.total_amount), 0) AS average_order_value,
                    MIN(o.order_date) AS first_order_date,
                    MAX(o.order_date) AS last_order_date
                FROM customers c
                LEFT JOIN orders o ON c.id = o.customer_id
                GROUP BY c.id, c.name"
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
            "INSERT INTO departments (name, description) VALUES ('销售部', '负责产品销售和客户关系维护')",
            "INSERT INTO departments (name, description) VALUES ('市场部', '负责市场推广和品牌建设')",
            
            // 插入产品分类数据
            "INSERT INTO categories (name, description, sort_order) VALUES ('电子产品', '各类电子设备和配件', 1)",
            "INSERT INTO categories (name, description, sort_order) VALUES ('家电', '家用电器产品', 2)",
            "INSERT INTO categories (name, description, sort_order) VALUES ('服装', '服装和配饰产品', 3)",
            "INSERT INTO categories (name, description, sort_order) VALUES ('图书', '各类图书和电子书', 4)",
            "INSERT INTO categories (name, description, parent_id, sort_order) VALUES ('手机', '智能手机产品', 1, 1)",
            "INSERT INTO categories (name, description, parent_id, sort_order) VALUES ('电脑', '台式机和笔记本电脑', 1, 2)",
            
            // 插入客户数据 (Primary Constructor 演示)
            "INSERT INTO customers (name, email, birth_date, status, address, phone, is_vip) VALUES " +
            "('张三', 'zhangsan@example.com', '1990-05-15', 1, '北京市朝阳区', '13800138001', 1), " +
            "('李四', 'lisi@example.com', '1985-08-20', 1, '上海市浦东新区', '13800138002', 0), " +
            "('王五', 'wangwu@example.com', '1992-12-10', 1, '广州市天河区', '13800138003', 1), " +
            "('赵六', 'zhaoliu@example.com', '1988-03-25', 2, '深圳市南山区', '13800138004', 0), " +
            "('钱七', 'qianqi@example.com', '1995-07-08', 1, '杭州市西湖区', '13800138005', 0)",
            
            // 更新产品数据，关联分类
            "UPDATE products SET category_id = 5 WHERE name LIKE '%iPhone%' OR name LIKE '%手机%'",
            "UPDATE products SET category_id = 6 WHERE name LIKE '%MacBook%' OR name LIKE '%电脑%'",
            "UPDATE products SET category_id = 1 WHERE name LIKE '%iPad%'",
        };
        
        foreach (var sql in sampleData)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // 忽略已存在的数据错误，继续执行
                Console.WriteLine($"Warning: {ex.Message}");
            }
        }
    }
}