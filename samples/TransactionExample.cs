// <copyright file="TransactionExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;

namespace Sqlx.Samples;

/// <summary>
/// 事务管理示例
/// Transaction management examples
/// </summary>
public class TransactionExample
{
    public class Account
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sqlx 事务管理示例 ===\n");

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await CreateTableAsync(connection);
        await InsertTestDataAsync(connection);

        // 示例 1: 基本事务
        await Example1_BasicTransaction(connection);

        // 示例 2: 事务回滚
        await Example2_TransactionRollback(connection);

        // 示例 3: 事务隔离级别
        await Example3_IsolationLevel(connection);

        // 示例 4: 保存点（Savepoint）
        await Example4_Savepoint(connection);

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 示例 1: 基本事务 - 转账操作
    /// Example 1: Basic transaction - money transfer
    /// </summary>
    static async Task Example1_BasicTransaction(SqliteConnection connection)
    {
        Console.WriteLine("示例 1: 基本事务（转账）");
        Console.WriteLine("----------------------");

        // 查询初始余额
        var accounts = await SqlQuery<Account>.ForSqlite()
            .ToListAsync(connection);
        
        Console.WriteLine("转账前余额:");
        foreach (var acc in accounts)
        {
            Console.WriteLine($"  {acc.Name}: ${acc.Balance:F2}");
        }

        // 开始事务：从 Alice 转账 $100 给 Bob
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 扣除 Alice 的余额
            using var debitBuilder = new SqlBuilder(SqlDefine.SQLite);
            debitBuilder.Append($"UPDATE accounts SET balance = balance - {100} WHERE name = {"Alice"}");
            var debitTemplate = debitBuilder.Build();
            
            using var debitCmd = connection.CreateCommand();
            debitCmd.Transaction = transaction;
            debitCmd.CommandText = debitTemplate.Sql;
            foreach (var (key, value) in debitTemplate.Parameters)
            {
                var param = debitCmd.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                debitCmd.Parameters.Add(param);
            }
            await debitCmd.ExecuteNonQueryAsync();

            // 增加 Bob 的余额
            using var creditBuilder = new SqlBuilder(SqlDefine.SQLite);
            creditBuilder.Append($"UPDATE accounts SET balance = balance + {100} WHERE name = {"Bob"}");
            var creditTemplate = creditBuilder.Build();
            
            using var creditCmd = connection.CreateCommand();
            creditCmd.Transaction = transaction;
            creditCmd.CommandText = creditTemplate.Sql;
            foreach (var (key, value) in creditTemplate.Parameters)
            {
                var param = creditCmd.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                creditCmd.Parameters.Add(param);
            }
            await creditCmd.ExecuteNonQueryAsync();

            // 提交事务
            await transaction.CommitAsync();
            Console.WriteLine("\n✓ 转账成功: Alice -> Bob $100");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"\n✗ 转账失败: {ex.Message}");
        }

        // 查询最终余额
        accounts = await SqlQuery<Account>.ForSqlite()
            .ToListAsync(connection);
        
        Console.WriteLine("\n转账后余额:");
        foreach (var acc in accounts)
        {
            Console.WriteLine($"  {acc.Name}: ${acc.Balance:F2}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 2: 事务回滚 - 余额不足时回滚
    /// Example 2: Transaction rollback - insufficient balance
    /// </summary>
    static async Task Example2_TransactionRollback(SqliteConnection connection)
    {
        Console.WriteLine("示例 2: 事务回滚（余额不足）");
        Console.WriteLine("--------------------------");

        // 查询初始余额
        var bobAccount = await SqlQuery<Account>.ForSqlite()
            .Where(a => a.Name == "Bob")
            .FirstOrDefaultAsync(connection);
        
        Console.WriteLine($"Bob 当前余额: ${bobAccount?.Balance:F2}");

        // 尝试转账超过余额的金额
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            decimal transferAmount = 10000; // 远超余额

            // 检查余额
            if (bobAccount != null && bobAccount.Balance < transferAmount)
            {
                throw new InvalidOperationException($"余额不足: 需要 ${transferAmount:F2}, 但只有 ${bobAccount.Balance:F2}");
            }

            // 这里不会执行到
            using var builder = new SqlBuilder(SqlDefine.SQLite);
            builder.Append($"UPDATE accounts SET balance = balance - {transferAmount} WHERE name = {"Bob"}");
            var template = builder.Build();
            
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = template.Sql;
            foreach (var (key, value) in template.Parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"✓ 事务已回滚: {ex.Message}");
        }

        // 验证余额未改变
        bobAccount = await SqlQuery<Account>.ForSqlite()
            .Where(a => a.Name == "Bob")
            .FirstOrDefaultAsync(connection);
        
        Console.WriteLine($"Bob 最终余额: ${bobAccount?.Balance:F2} (未改变)");
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 3: 事务隔离级别
    /// Example 3: Transaction isolation levels
    /// </summary>
    static async Task Example3_IsolationLevel(SqliteConnection connection)
    {
        Console.WriteLine("示例 3: 事务隔离级别");
        Console.WriteLine("-------------------");

        // SQLite 支持的隔离级别
        Console.WriteLine("SQLite 支持的隔离级别:");
        Console.WriteLine("  - ReadUncommitted (读未提交)");
        Console.WriteLine("  - ReadCommitted (读已提交)");
        Console.WriteLine("  - RepeatableRead (可重复读)");
        Console.WriteLine("  - Serializable (串行化)");
        Console.WriteLine();

        // 使用 Serializable 隔离级别
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            Console.WriteLine("✓ 使用 Serializable 隔离级别开始事务");
            
            // 执行查询
            var accounts = await SqlQuery<Account>.ForSqlite()
                .ToListAsync(connection);
            
            Console.WriteLine($"  查询到 {accounts.Count} 个账户");

            await transaction.CommitAsync();
            Console.WriteLine("✓ 事务提交成功");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"✗ 事务失败: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 示例 4: 保存点（Savepoint）- 部分回滚
    /// Example 4: Savepoint - partial rollback
    /// </summary>
    static async Task Example4_Savepoint(SqliteConnection connection)
    {
        Console.WriteLine("示例 4: 保存点（Savepoint）");
        Console.WriteLine("-------------------------");

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 操作 1: 更新 Alice
            using var builder1 = new SqlBuilder(SqlDefine.SQLite);
            builder1.Append($"UPDATE accounts SET balance = balance + {50} WHERE name = {"Alice"}");
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
            Console.WriteLine("✓ 操作 1: Alice +$50");

            // 创建保存点
            using var savepointCmd = connection.CreateCommand();
            savepointCmd.Transaction = transaction;
            savepointCmd.CommandText = "SAVEPOINT sp1";
            await savepointCmd.ExecuteNonQueryAsync();
            Console.WriteLine("✓ 创建保存点 sp1");

            // 操作 2: 更新 Bob（这个操作会失败）
            try
            {
                using var builder2 = new SqlBuilder(SqlDefine.SQLite);
                builder2.Append($"UPDATE accounts SET balance = balance - {10000} WHERE name = {"Bob"}");
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

                // 检查余额（模拟业务逻辑检查）
                var bob = await SqlQuery<Account>.ForSqlite()
                    .Where(a => a.Name == "Bob")
                    .FirstOrDefaultAsync(connection);
                
                if (bob != null && bob.Balance < 0)
                {
                    throw new InvalidOperationException("余额不能为负数");
                }
            }
            catch (Exception ex)
            {
                // 回滚到保存点
                using var rollbackCmd = connection.CreateCommand();
                rollbackCmd.Transaction = transaction;
                rollbackCmd.CommandText = "ROLLBACK TO sp1";
                await rollbackCmd.ExecuteNonQueryAsync();
                Console.WriteLine($"✓ 回滚到保存点 sp1: {ex.Message}");
            }

            // 操作 3: 更新 Charlie
            using var builder3 = new SqlBuilder(SqlDefine.SQLite);
            builder3.Append($"UPDATE accounts SET balance = balance + {25} WHERE name = {"Charlie"}");
            var template3 = builder3.Build();
            
            using var cmd3 = connection.CreateCommand();
            cmd3.Transaction = transaction;
            cmd3.CommandText = template3.Sql;
            foreach (var (key, value) in template3.Parameters)
            {
                var param = cmd3.CreateParameter();
                param.ParameterName = "@" + key;
                param.Value = value ?? DBNull.Value;
                cmd3.Parameters.Add(param);
            }
            await cmd3.ExecuteNonQueryAsync();
            Console.WriteLine("✓ 操作 3: Charlie +$25");

            // 提交事务
            await transaction.CommitAsync();
            Console.WriteLine("✓ 事务提交成功（操作 1 和 3 生效，操作 2 已回滚）");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"✗ 事务失败: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 创建测试表
    /// </summary>
    static async Task CreateTableAsync(SqliteConnection connection)
    {
        var sql = @"
            CREATE TABLE accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                balance REAL NOT NULL DEFAULT 0
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
            INSERT INTO accounts (name, balance) VALUES
            ('Alice', 1000.00),
            ('Bob', 500.00),
            ('Charlie', 750.00)";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
