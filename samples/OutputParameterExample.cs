// <copyright file="OutputParameterExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Samples;

/// <summary>
/// 演示如何使用输出参数功能。
/// Demonstrates how to use output parameter functionality.
/// </summary>
public class OutputParameterExample
{
    /// <summary>
    /// 示例1: 使用SqlTemplate手动添加输出参数
    /// Example 1: Manually add output parameters using SqlTemplate
    /// </summary>
    public static async Task Example1_ManualOutputParameter()
    {
        Console.WriteLine("=== Example 1: Manual Output Parameter ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT)";
        await createCmd.ExecuteNonQueryAsync();
        
        // 使用SqlBuilder构建SQL并添加输出参数
        var builder = new SqlBuilder();
        builder.Append($"INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid()");
        builder.AddParameter("name", "张三");
        
        var template = builder.Build();
        template.AddOutputParameter("userId", DbType.Int32);
        
        // 执行SQL
        using var cmd = connection.CreateCommand();
        cmd.CommandText = template.Sql;
        
        // 绑定输入参数
        foreach (var param in template.Parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = "@" + param.Key;
            p.Value = param.Value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        
        // 绑定输出参数
        foreach (var outParam in template.OutputParameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = "@" + outParam.Key;
            p.DbType = outParam.Value;
            p.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(p);
        }
        
        await cmd.ExecuteNonQueryAsync();
        
        // 获取输出参数值
        var userId = cmd.Parameters["@userId"].Value;
        Console.WriteLine($"插入的用户ID: {userId}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例2: 使用源生成器自动处理输出参数（同步方法）
    /// Example 2: Use source generator to automatically handle output parameters (sync method)
    /// </summary>
    public static void Example2_GeneratedOutputParameter()
    {
        Console.WriteLine("=== Example 2: Generated Output Parameter ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, price REAL)";
        createCmd.ExecuteNonQuery();
        
        var repository = new ProductRepository { Connection = connection };
        
        // 调用带输出参数的方法（同步）
        var result = repository.InsertProduct("笔记本电脑", 5999.99m, out int productId);
        
        Console.WriteLine($"插入结果: {result} 行受影响");
        Console.WriteLine($"产品ID (输出参数): {productId}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例3: 多个输出参数（同步方法）
    /// Example 3: Multiple output parameters (sync method)
    /// </summary>
    public static void Example3_MultipleOutputParameters()
    {
        Console.WriteLine("=== Example 3: Multiple Output Parameters ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT, 
                customer_name TEXT, 
                total_amount REAL,
                created_at TEXT
            )";
        createCmd.ExecuteNonQuery();
        
        var repository = new OrderRepository { Connection = connection };
        
        // 调用带多个输出参数的方法（同步）
        var result = repository.CreateOrder(
            "李四", 
            1299.99m, 
            out int orderId, 
            out string createdAt);
        
        Console.WriteLine($"插入结果: {result} 行受影响");
        Console.WriteLine($"订单ID: {orderId}");
        Console.WriteLine($"创建时间: {createdAt}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例4: 使用ref参数实现InputOutput模式（同步方法）
    /// Example 4: Use ref parameters for InputOutput mode (sync method)
    /// </summary>
    public static void Example4_RefParameterInputOutput()
    {
        Console.WriteLine("=== Example 4: Ref Parameter (InputOutput Mode) ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE counters (
                name TEXT PRIMARY KEY, 
                value INTEGER
            )";
        createCmd.ExecuteNonQuery();
        
        // 插入初始计数器值
        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO counters (name, value) VALUES ('page_views', 100)";
        insertCmd.ExecuteNonQuery();
        
        var repository = new CounterRepository { Connection = connection };
        
        // 使用ref参数：传入当前值，返回更新后的值（同步）
        int currentValue = 100;
        var result = repository.IncrementCounter("page_views", ref currentValue);
        
        Console.WriteLine($"更新结果: {result} 行受影响");
        Console.WriteLine($"更新后的值 (ref参数): {currentValue}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 运行所有示例
    /// Run all examples
    /// </summary>
    public static async Task RunAllExamples()
    {
        await Example1_ManualOutputParameter();
        Example2_GeneratedOutputParameter();
        Example3_MultipleOutputParameters();
        Example4_RefParameterInputOutput();
    }
}

// ===== 示例实体和仓储定义 =====

[Sqlx]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public interface IProductRepository
{
    [SqlTemplate("INSERT INTO products (name, price) VALUES (@name, @price)")]
    int InsertProduct(
        string name, 
        decimal price, 
        [OutputParameter(DbType.Int32)] out int productId);
}

[RepositoryFor(typeof(IProductRepository))]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("products")]
public partial class ProductRepository : IProductRepository
{
    public DbConnection? Connection { get; set; }
}

[Sqlx]
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public interface IOrderRepository
{
    [SqlTemplate("INSERT INTO orders (customer_name, total_amount, created_at) VALUES (@customerName, @totalAmount, datetime('now'))")]
    int CreateOrder(
        string customerName,
        decimal totalAmount,
        [OutputParameter(DbType.Int32)] out int orderId,
        [OutputParameter(DbType.String, Size = 50)] out string createdAt);
}

[RepositoryFor(typeof(IOrderRepository))]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("orders")]
public partial class OrderRepository : IOrderRepository
{
    public DbConnection? Connection { get; set; }
}

[Sqlx]
public class Counter
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public interface ICounterRepository
{
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name")]
    int IncrementCounter(
        string name,
        [OutputParameter(DbType.Int32)] ref int value);
}

[RepositoryFor(typeof(ICounterRepository))]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("counters")]
public partial class CounterRepository : ICounterRepository
{
    public DbConnection? Connection { get; set; }
}
