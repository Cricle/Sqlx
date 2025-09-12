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
            "INSERT INTO categories (name, description, parent_id, sort_order) VALUES ('平板', '平板电脑产品', 1, 3)",
            "INSERT INTO categories (name, description, parent_id, sort_order) VALUES ('智能家电', '智能家用电器', 2, 1)",
            
            // 插入客户数据 (Primary Constructor 演示)
            "INSERT INTO customers (name, email, birth_date, status, total_spent, address, phone, is_vip) VALUES " +
            "('张三', 'zhangsan@example.com', '1990-05-15', 1, 15000.00, '北京市朝阳区', '13800138001', 1), " +
            "('李四', 'lisi@example.com', '1985-08-20', 1, 5200.50, '上海市浦东新区', '13800138002', 0), " +
            "('王五', 'wangwu@example.com', '1992-12-10', 1, 12800.75, '广州市天河区', '13800138003', 1), " +
            "('赵六', 'zhaoliu@example.com', '1988-03-25', 2, 800.00, '深圳市南山区', '13800138004', 0), " +
            "('钱七', 'qianqi@example.com', '1995-07-08', 1, 3200.25, '杭州市西湖区', '13800138005', 0), " +
            "('孙八', 'sunba@example.com', '1993-11-30', 1, 18500.00, '成都市高新区', '13800138006', 1), " +
            "('周九', 'zhoujiu@example.com', '1987-02-14', 1, 6700.80, '武汉市洪山区', '13800138007', 0), " +
            "('吴十', 'wushi@example.com', '1991-09-22', 1, 9900.90, '西安市雁塔区', '13800138008', 0)",
            
            // 插入产品数据 (Record 类型演示)
            "INSERT INTO products (name, price, category_id) VALUES " +
            "('iPhone 15 Pro', 9999.00, 5), " +
            "('iPhone 15', 6999.00, 5), " +
            "('MacBook Pro 16\"', 19999.00, 6), " +
            "('MacBook Air 13\"', 8999.00, 6), " +
            "('iPad Pro 12.9\"', 8799.00, 7), " +
            "('iPad Air', 4399.00, 7), " +
            "('小米智能电视55\"', 2999.00, 8), " +
            "('华为MateBook X Pro', 9999.00, 6), " +
            "('三星Galaxy S24', 5999.00, 5), " +
            "('戴尔XPS 13', 7999.00, 6)",
            
            // 插入订单数据
            "INSERT INTO orders (customer_name, total_amount, order_date) VALUES " +
            "('张三', 9999.00, '2024-01-15 10:30:00'), " +
            "('李四', 2999.00, '2024-01-20 14:20:00'), " +
            "('王五', 8999.00, '2024-02-01 09:15:00'), " +
            "('张三', 4399.00, '2024-02-10 16:45:00'), " +
            "('孙八', 19999.00, '2024-02-15 11:00:00'), " +
            "('周九', 5999.00, '2024-03-01 13:30:00'), " +
            "('王五', 7999.00, '2024-03-05 10:20:00')",
            
            // 插入库存数据 (Record 类型演示)
            "INSERT INTO inventory (product_id, quantity, reorder_level, warehouse_location) VALUES " +
            "(1, 50, 10, 'A区-01'), " +
            "(2, 120, 20, 'A区-02'), " +
            "(3, 25, 5, 'B区-01'), " +
            "(4, 80, 15, 'B区-02'), " +
            "(5, 40, 8, 'C区-01'), " +
            "(6, 90, 18, 'C区-02'), " +
            "(7, 15, 5, 'D区-01'), " +
            "(8, 30, 6, 'B区-03'), " +
            "(9, 60, 12, 'A区-03'), " +
            "(10, 35, 7, 'B区-04')",
            
            // 插入审计日志示例数据
            "INSERT INTO audit_logs (action, entity_type, entity_id, user_id, old_values, new_values, ip_address, user_agent) VALUES " +
            "('CREATE', 'Product', '1', 'admin', '', '{\"name\":\"iPhone 15 Pro\",\"price\":9999.00}', '192.168.1.100', 'Sqlx Demo App'), " +
            "('UPDATE', 'Customer', '1', 'system', '{\"total_spent\":14000.00}', '{\"total_spent\":15000.00}', '127.0.0.1', 'System Process'), " +
            "('CREATE', 'Order', '1', 'user_001', '', '{\"customer_name\":\"张三\",\"total_amount\":9999.00}', '192.168.1.101', 'Mobile App'), " +
            "('UPDATE', 'Inventory', '1', 'warehouse', '{\"quantity\":55}', '{\"quantity\":50}', '192.168.1.102', 'Warehouse System'), " +
            "('DELETE', 'User', '999', 'admin', '{\"name\":\"Test User\",\"email\":\"test@example.com\"}', '', '192.168.1.100', 'Admin Panel')",
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
                Console.WriteLine($"⚠️ 数据插入警告: {ex.Message}");
            }
        }
        
        Console.WriteLine("📊 示例数据插入完成：");
        Console.WriteLine("   - 5 个部门");
        Console.WriteLine("   - 8 个客户 (Primary Constructor)");
        Console.WriteLine("   - 10 个产品 (Record 类型)");
        Console.WriteLine("   - 8 个产品分类 (层次结构)");
        Console.WriteLine("   - 7 个订单");
        Console.WriteLine("   - 10 个库存项 (Record 类型)");
        Console.WriteLine("   - 5 条审计日志 (Primary Constructor + Record)");
    }
}