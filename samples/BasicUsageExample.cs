// <copyright file="BasicUsageExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;

namespace Sqlx.Samples;

/// <summary>
/// Sqlx 基础用法示例
/// Basic usage examples for Sqlx
/// </summary>
public class BasicUsageExample
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sqlx 基础用法示例 ===\n");

        // 创建内存数据库
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建测试表
        await CreateTableAsync(connection);

        // 示例 1: 基本查询
        await Example1_BasicQuery(connection);

        // 示例 2: 参数化查询
        await Example2_ParameterizedQuery(connection);

        // 示例 3: CRUD 操作
        await Example3_CrudOperations(connection);

        // 示例 4: LINQ 查询
        await Example4_LinqQuery(connection);

        // 示例 5: 事务处理
        await Example5_Transaction(connection);

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 示例 1: 基本查询 - 使用 SqlQuery 查询数据
    /// Example 1: Basic query using SqlQuery
    /// </summary>
    static async Task Example1_BasicQuery(SqliteConnection connection)
    {
        Console.WriteLine("示例 1: 基本查询");
        Console.WriteLine("----------------");

        // 插入测试数据
        await InsertTestDataAsync(connection);

        // 使用 SqlQuery 查询所有用户
        var users = await SqlQuery<User>.ForSqlite()
            .WithConnection(connection)
            .ToListAsync();

        Console.WriteLine($"查询到 {users.Count} 个用户:");
        foreach (var user in users)
        {
            Console.WriteLine($"  ID={user.Id}, Name={user.Name}, Age={user.Age}, Email={user.Email}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 2: 参数化查询 - 使用 Where 条件过滤
    /// Example 2: Parameterized query with Where conditions
    /// </summary>
    static async Task Example2_ParameterizedQuery(SqliteConnection connection)
    {
        Console.WriteLine("示例 2: 参数化查询");
        Console.WriteLine("------------------");

        // 查询年龄大于 25 的用户
        var users = await SqlQuery<User>.ForSqlite()
            .Where(u => u.Age > 25)
            .OrderBy(u => u.Name)
            .WithConnection(connection)
            .ToListAsync();

        Console.WriteLine($"年龄 > 25 的用户 ({users.Count} 个):");
        foreach (var user in users)
        {
            Console.WriteLine($"  {user.Name} - {user.Age} 岁");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 3: CRUD 操作 - 增删改查
    /// Example 3: CRUD operations
    /// </summary>
    static async Task Example3_CrudOperations(SqliteConnection connection)
    {
        Console.WriteLine("示例 3: CRUD 操作");
        Console.WriteLine("----------------");

        // Create - 插入新用户
        var newUser = new User
        {
            Name = "Frank",
            Age = 35,
            Email = "frank@example.com"
        };

        using var insertBuilder = new SqlBuilder(SqlDefine.SQLite);
        insertBuilder.Append($"INSERT INTO users (name, age, email) VALUES ({newUser.Name}, {newUser.Age}, {newUser.Email})");
        var insertTemplate = insertBuilder.Build();
        
        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = insertTemplate.Sql;
        foreach (var (key, value) in insertTemplate.Parameters)
        {
            var param = insertCmd.CreateParameter();
            param.ParameterName = "@" + key;
            param.Value = value ?? DBNull.Value;
            insertCmd.Parameters.Add(param);
        }
        await insertCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"✓ 插入用户: {newUser.Name}");

        // Read - 查询用户
        var user = await SqlQuery<User>.ForSqlite()
            .Where(u => u.Name == "Frank")
            .WithConnection(connection)
            .FirstOrDefaultAsync();
        Console.WriteLine($"✓ 查询用户: {user?.Name} ({user?.Email})");

        // Update - 更新用户
        if (user != null)
        {
            using var updateBuilder = new SqlBuilder(SqlDefine.SQLite);
            updateBuilder.Append($"UPDATE users SET age = {40} WHERE name = {"Frank"}");
            var updateTemplate = updateBuilder.Build();
            
            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = updateTemplate.Sql;
            foreach (var (key, value) in updateTemplate.Parameters)
            {
                var param = updateCmd.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                updateCmd.Parameters.Add(param);
            }
            await updateCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✓ 更新用户年龄: {user.Name} -> 40 岁");
        }

        // Delete - 删除用户
        using var deleteBuilder = new SqlBuilder(SqlDefine.SQLite);
        deleteBuilder.Append($"DELETE FROM users WHERE name = {"Frank"}");
        var deleteTemplate = deleteBuilder.Build();
        
        using var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = deleteTemplate.Sql;
        foreach (var (key, value) in deleteTemplate.Parameters)
        {
            var param = deleteCmd.CreateParameter();
            param.ParameterName = "@" + key;
            param.Value = value ?? DBNull.Value;
            deleteCmd.Parameters.Add(param);
        }
        await deleteCmd.ExecuteNonQueryAsync();
        Console.WriteLine($"✓ 删除用户: Frank");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 4: LINQ 查询 - 使用 LINQ 表达式
    /// Example 4: LINQ query with expressions
    /// </summary>
    static async Task Example4_LinqQuery(SqliteConnection connection)
    {
        Console.WriteLine("示例 4: LINQ 查询");
        Console.WriteLine("----------------");

        // 复杂的 LINQ 查询
        var users = await SqlQuery<User>.ForSqlite()
            .Where(u => u.Age >= 25 && u.Age <= 35)
            .Where(u => u.Email.Contains("@example.com"))
            .OrderByDescending(u => u.Age)
            .ThenBy(u => u.Name)
            .Take(3)
            .WithConnection(connection)
            .ToListAsync();

        Console.WriteLine("年龄在 25-35 之间，邮箱包含 @example.com 的用户（前3个）:");
        foreach (var user in users)
        {
            Console.WriteLine($"  {user.Name} - {user.Age} 岁 - {user.Email}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 5: 事务处理 - 确保数据一致性
    /// Example 5: Transaction handling for data consistency
    /// </summary>
    static async Task Example5_Transaction(SqliteConnection connection)
    {
        Console.WriteLine("示例 5: 事务处理");
        Console.WriteLine("----------------");

        // 开始事务
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 操作 1: 插入用户
            using var builder1 = new SqlBuilder(SqlDefine.SQLite);
            builder1.Append($"INSERT INTO users (name, age, email) VALUES ({"Grace"}, {28}, {"grace@example.com"})");
            var template1 = builder1.Build();
            
            using var cmd1 = connection.CreateCommand();
            cmd1.Transaction = transaction;
            cmd1.CommandText = template1.Sql;
            foreach (var (key, value) in template1.Parameters)
            {
                var param = cmd1.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                cmd1.Parameters.Add(param);
            }
            await cmd1.ExecuteNonQueryAsync();
            Console.WriteLine("✓ 事务操作 1: 插入用户 Grace");

            // 操作 2: 更新另一个用户
            using var builder2 = new SqlBuilder(SqlDefine.SQLite);
            builder2.Append($"UPDATE users SET age = age + 1 WHERE name = {"Alice"}");
            var template2 = builder2.Build();
            
            using var cmd2 = connection.CreateCommand();
            cmd2.Transaction = transaction;
            cmd2.CommandText = template2.Sql;
            foreach (var (key, value) in template2.Parameters)
            {
                var param = cmd2.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                cmd2.Parameters.Add(param);
            }
            await cmd2.ExecuteNonQueryAsync();
            Console.WriteLine("✓ 事务操作 2: 更新用户 Alice 年龄");

            // 提交事务
            await transaction.CommitAsync();
            Console.WriteLine("✓ 事务提交成功");
        }
        catch (Exception ex)
        {
            // 回滚事务
            await transaction.RollbackAsync();
            Console.WriteLine($"✗ 事务回滚: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 创建测试表
    /// </summary>
    static async Task CreateTableAsync(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                email TEXT NOT NULL
            )";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 插入测试数据
    /// </summary>
    static async Task InsertTestDataAsync(SqliteConnection connection)
    {
        var sql = @"
            INSERT INTO users (name, age, email) VALUES
            ('Alice', 30, 'alice@example.com'),
            ('Bob', 25, 'bob@example.com'),
            ('Charlie', 35, 'charlie@example.com'),
            ('David', 28, 'david@example.com'),
            ('Eve', 32, 'eve@example.com')";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
