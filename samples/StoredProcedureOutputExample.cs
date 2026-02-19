// <copyright file="StoredProcedureOutputExample.cs" company="Sqlx">
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
/// 演示如何在存储过程中使用输出参数（同步和异步）。
/// Demonstrates how to use output parameters with stored procedures (sync and async).
/// </summary>
public class StoredProcedureOutputExample
{
    /// <summary>
    /// 示例1: 同步方法使用 out 参数
    /// Example 1: Sync method with out parameter
    /// </summary>
    public static void Example1_SyncOutputParameter()
    {
        Console.WriteLine("=== Example 1: Sync Output Parameter ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, age INTEGER)";
        createCmd.ExecuteNonQuery();
        
        var repository = new StoredProcRepository { Connection = connection };
        
        // 使用 out 参数（同步方法）
        var result = repository.InsertUser("Alice", 25, out int userId);
        
        Console.WriteLine($"插入结果: {result} 行受影响");
        Console.WriteLine($"用户ID (out参数): {userId}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例2: 异步方法使用 OutputParameter 包装类
    /// Example 2: Async method with OutputParameter wrapper
    /// </summary>
    public static async Task Example2_AsyncOutputParameter()
    {
        Console.WriteLine("=== Example 2: Async Output Parameter ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, price REAL)";
        await createCmd.ExecuteNonQueryAsync();
        
        var repository = new StoredProcRepository { Connection = connection };
        
        // 使用 OutputParameter 包装类（异步方法）
        var productId = new OutputParameter<int>();
        var result = await repository.InsertProductAsync("Laptop", 999.99m, productId);
        
        Console.WriteLine($"插入结果: {result} 行受影响");
        Console.WriteLine($"产品ID (OutputParameter): {productId.Value}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例3: 同步方法使用 ref 参数（InputOutput 模式）
    /// Example 3: Sync method with ref parameter (InputOutput mode)
    /// </summary>
    public static void Example3_SyncRefParameter()
    {
        Console.WriteLine("=== Example 3: Sync Ref Parameter (InputOutput) ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE counters (name TEXT PRIMARY KEY, value INTEGER)";
        createCmd.ExecuteNonQuery();
        
        // 插入初始值
        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO counters (name, value) VALUES ('page_views', 100)";
        insertCmd.ExecuteNonQuery();
        
        var repository = new StoredProcRepository { Connection = connection };
        
        // 使用 ref 参数（同步方法）
        int counter = 100;
        var result = repository.IncrementCounter("page_views", ref counter);
        
        Console.WriteLine($"更新结果: {result} 行受影响");
        Console.WriteLine($"新值 (ref参数): {counter}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例4: 异步方法使用 OutputParameter 包装类（InputOutput 模式）
    /// Example 4: Async method with OutputParameter wrapper (InputOutput mode)
    /// </summary>
    public static async Task Example4_AsyncRefParameter()
    {
        Console.WriteLine("=== Example 4: Async Ref Parameter (InputOutput) ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE scores (player TEXT PRIMARY KEY, score INTEGER)";
        await createCmd.ExecuteNonQueryAsync();
        
        // 插入初始值
        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO scores (player, score) VALUES ('Player1', 50)";
        await insertCmd.ExecuteNonQueryAsync();
        
        var repository = new StoredProcRepository { Connection = connection };
        
        // 使用 OutputParameter.WithValue 创建带初始值的参数（异步方法）
        var score = OutputParameter<int>.WithValue(50);
        var result = await repository.AddScoreAsync("Player1", 10, score);
        
        Console.WriteLine($"更新结果: {result} 行受影响");
        Console.WriteLine($"新分数 (OutputParameter): {score.Value}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 示例5: 异步方法使用多个输出参数
    /// Example 5: Async method with multiple output parameters
    /// </summary>
    public static async Task Example5_AsyncMultipleOutputParameters()
    {
        Console.WriteLine("=== Example 5: Async Multiple Output Parameters ===");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        // 创建测试表
        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT, 
                customer_name TEXT, 
                total REAL,
                created_at TEXT
            )";
        await createCmd.ExecuteNonQueryAsync();
        
        var repository = new StoredProcRepository { Connection = connection };
        
        // 使用多个 OutputParameter
        var orderId = new OutputParameter<int>();
        var timestamp = new OutputParameter<string>();
        var result = await repository.CreateOrderAsync("Bob", 199.99m, orderId, timestamp);
        
        Console.WriteLine($"插入结果: {result} 行受影响");
        Console.WriteLine($"订单ID: {orderId.Value}");
        Console.WriteLine($"创建时间: {timestamp.Value}");
        Console.WriteLine();
    }
    
    /// <summary>
    /// 运行所有示例
    /// Run all examples
    /// </summary>
    public static async Task RunAllExamples()
    {
        Example1_SyncOutputParameter();
        await Example2_AsyncOutputParameter();
        Example3_SyncRefParameter();
        await Example4_AsyncRefParameter();
        await Example5_AsyncMultipleOutputParameters();
    }
}

// ===== 示例实体和仓储定义 =====

[Sqlx]
public class StoredProcUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

[Sqlx]
public class StoredProcProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

[Sqlx]
public class StoredProcOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public interface IStoredProcRepository
{
    // 同步方法 - 使用 out 参数
    [SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age); SELECT last_insert_rowid()")]
    int InsertUser(
        string name, 
        int age, 
        [OutputParameter(DbType.Int32)] out int userId);
    
    // 异步方法 - 使用 OutputParameter<T> 包装类
    [SqlTemplate("INSERT INTO products (name, price) VALUES (@name, @price); SELECT last_insert_rowid()")]
    Task<int> InsertProductAsync(
        string name, 
        decimal price, 
        [OutputParameter(DbType.Int32)] OutputParameter<int> productId);
    
    // 同步方法 - 使用 ref 参数（InputOutput 模式）
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    int IncrementCounter(
        string name,
        [OutputParameter(DbType.Int32)] ref int value);
    
    // 异步方法 - 使用 OutputParameter<T> 包装类（InputOutput 模式）
    [SqlTemplate("UPDATE scores SET score = score + @points WHERE player = @player; SELECT score FROM scores WHERE player = @player")]
    Task<int> AddScoreAsync(
        string player,
        int points,
        [OutputParameter(DbType.Int32)] OutputParameter<int> score);
    
    // 异步方法 - 多个输出参数
    [SqlTemplate(@"
        INSERT INTO orders (customer_name, total, created_at) 
        VALUES (@customerName, @total, datetime('now'));
        SELECT last_insert_rowid(), datetime('now')
    ")]
    Task<int> CreateOrderAsync(
        string customerName,
        decimal total,
        [OutputParameter(DbType.Int32)] OutputParameter<int> orderId,
        [OutputParameter(DbType.String, Size = 50)] OutputParameter<string> timestamp);
}

[RepositoryFor(typeof(IStoredProcRepository))]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
public partial class StoredProcRepository : IStoredProcRepository
{
    public DbConnection? Connection { get; set; }
}
