using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;

namespace SqlxCompleteDemo;

/// <summary>
/// 简化的演示程序，直接使用 Sqlx 的 ExpressionToSql 功能
/// </summary>
public static class SimpleDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🚀 开始 Sqlx 核心功能演示...\n");
        
        // 初始化数据库
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        await SetupDatabaseAsync(connection);
        
        // 演示 Expression to SQL 功能
        await DemoExpressionToSql();
        
        // 演示多种数据库方言
        DemoMultipleDialects();
        
        // 演示动态查询构建
        DemoQueryBuilding();
        
        Console.WriteLine("\n🎉 核心功能演示完成！");
    }
    
    private static async Task SetupDatabaseAsync(DbConnection connection)
    {
        Console.WriteLine("📝 创建演示表结构...");
        
        var sql = """
            CREATE TABLE users (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                Age INTEGER,
                IsActive INTEGER DEFAULT 1,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            
            INSERT INTO users (Name, Email, Age, IsActive) VALUES
                ('张三', 'zhang@example.com', 25, 1),
                ('李四', 'li@example.com', 30, 1),
                ('王五', 'wang@example.com', 28, 0),
                ('赵六', 'zhao@example.com', 35, 1);
        """;
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("✅ 数据准备完成\n");
    }
    
    private static async Task DemoExpressionToSql()
    {
        Console.WriteLine("🎯 演示 Expression to SQL 功能");
        Console.WriteLine("=".PadRight(40, '='));
        
        try
        {
            // 基础查询
            Console.WriteLine("📋 基础查询:");
            var query1 = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive && u.Age > 25)
                .OrderBy(u => u.Name);
            
            var sql1 = query1.ToSql();
            Console.WriteLine($"   SQL: {sql1}\n");
            
            // 复杂条件查询
            Console.WriteLine("🔍 复杂条件查询:");
            var query2 = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Name.Contains("张") || u.Email.Contains("li"))
                .Where(u => u.Age >= 25 && u.Age <= 35)
                .Take(5);
            
            var sql2 = query2.ToSql();
            Console.WriteLine($"   SQL: {sql2}\n");
            
            // 选择特定列查询
            Console.WriteLine("📊 选择特定列查询:");
            var query3 = ExpressionToSql<User>.ForSqlite()
                .Select("Id", "Name", "Email", "Age")
                .Where(u => u.Age > 20)
                .OrderBy(u => u.Name);
            
            var sql3 = query3.ToSql();
            Console.WriteLine($"   SQL: {sql3}\n");
            
            // 分页查询
            Console.WriteLine("📄 分页查询:");
            var query4 = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive)
                .OrderBy(u => u.CreatedAt)
                .Skip(1)
                .Take(2);
            
            var sql4 = query4.ToSql();
            Console.WriteLine($"   SQL: {sql4}\n");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 演示异常: {ex.Message}\n");
        }
        
        await Task.CompletedTask;
    }
    
    private static void DemoMultipleDialects()
    {
        Console.WriteLine("🌍 演示多数据库方言支持");
        Console.WriteLine("=".PadRight(40, '='));
        
        var whereCondition = "u => u.IsActive && u.Age > 25";
        var orderByCondition = "u => u.CreatedAt";
        
        Console.WriteLine($"表达式条件: WHERE {whereCondition}");
        Console.WriteLine($"排序条件: ORDER BY {orderByCondition}");
        Console.WriteLine($"分页: LIMIT 10\n");
        
        try
        {
            // SQLite
            Console.WriteLine("🗃️ SQLite 方言:");
            var sqliteQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive && u.Age > 25)
                .OrderBy(u => u.CreatedAt)
                .Take(10);
            Console.WriteLine($"   {sqliteQuery.ToSql()}\n");
            
            // SQL Server
            Console.WriteLine("🏢 SQL Server 方言:");
            var sqlServerQuery = ExpressionToSql<User>.ForSqlServer()
                .Where(u => u.IsActive && u.Age > 25)
                .OrderBy(u => u.CreatedAt)
                .Take(10);
            Console.WriteLine($"   {sqlServerQuery.ToSql()}\n");
            
            // MySQL
            Console.WriteLine("🐬 MySQL 方言:");
            var mysqlQuery = ExpressionToSql<User>.ForMySql()
                .Where(u => u.IsActive && u.Age > 25)
                .OrderBy(u => u.CreatedAt)
                .Take(10);
            Console.WriteLine($"   {mysqlQuery.ToSql()}\n");
            
            // PostgreSQL
            Console.WriteLine("🐘 PostgreSQL 方言:");
            var postgresQuery = ExpressionToSql<User>.ForPostgreSQL()
                .Where(u => u.IsActive && u.Age > 25)
                .OrderBy(u => u.CreatedAt)
                .Take(10);
            Console.WriteLine($"   {postgresQuery.ToSql()}\n");
            
            // Oracle
            Console.WriteLine("🏛️ Oracle 方言:");
            var oracleQuery = ExpressionToSql<User>.ForOracle()
                .Where(u => u.IsActive && u.Age > 25)
                .OrderBy(u => u.CreatedAt)
                .Take(10);
            Console.WriteLine($"   {oracleQuery.ToSql()}\n");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 方言演示异常: {ex.Message}\n");
        }
    }
    
    private static void DemoQueryBuilding()
    {
        Console.WriteLine("🔧 演示动态查询构建");
        Console.WriteLine("=".PadRight(40, '='));
        
        try
        {
            // 动态条件构建
            Console.WriteLine("🎯 动态条件构建:");
            var baseQuery = ExpressionToSql<User>.ForSqlite();
            
            // 条件1：年龄过滤
            var queryWithAge = baseQuery.Where(u => u.Age > 25);
            Console.WriteLine($"   添加年龄条件: {queryWithAge.ToSql()}");
            
            // 条件2：活跃状态过滤
            var queryWithActive = queryWithAge.Where(u => u.IsActive);
            Console.WriteLine($"   添加活跃状态: {queryWithActive.ToSql()}");
            
            // 条件3：排序
            var queryWithOrder = queryWithActive.OrderBy(u => u.Name);
            Console.WriteLine($"   添加排序: {queryWithOrder.ToSql()}");
            
            // 条件4：分页
            var finalQuery = queryWithOrder.Skip(0).Take(10);
            Console.WriteLine($"   添加分页: {finalQuery.ToSql()}\n");
            
            // INSERT 语句演示
            Console.WriteLine("📝 INSERT 语句演示:");
            var insertQuery = ExpressionToSql<User>.ForSqlite().InsertInto();
            Console.WriteLine($"   演示 INSERT 语法: {insertQuery.GetType().Name}\n");
            
            // UPDATE 语句演示
            Console.WriteLine("✏️ UPDATE 语句演示:");
            var updateQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Id == 1);
            Console.WriteLine($"   演示 UPDATE WHERE 语法: {updateQuery.ToSql()}\n");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 查询构建异常: {ex.Message}\n");
        }
    }
}

// User 类定义已移动到 ComprehensiveSqliteDemo.cs
