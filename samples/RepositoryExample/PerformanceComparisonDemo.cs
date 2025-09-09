// -----------------------------------------------------------------------
// <copyright file="PerformanceComparisonDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sqlx.RepositoryExample;

/// <summary>
/// 演示 Sqlx 优化前后的性能对比
/// </summary>
public static class PerformanceComparisonDemo
{
    /// <summary>
    /// 运行性能对比演示
    /// </summary>
    public static async Task RunComparisonAsync()
    {
        Console.WriteLine("=== Sqlx 性能优化对比演示 ===");
        Console.WriteLine();

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建测试表和数据
        await SetupTestDataAsync(connection);

        Console.WriteLine("🔄 准备测试数据...");
        Console.WriteLine();

        // 运行性能测试
        await RunPerformanceTestsAsync(connection);
    }

    private static async Task SetupTestDataAsync(SqliteConnection connection)
    {
        // 创建表
        var createTableSql = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                Age INTEGER,
                IsActive BOOLEAN,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = createTableSql;
        await cmd.ExecuteNonQueryAsync();

        // 插入测试数据
        const int recordCount = 10000;
        cmd.CommandText = @"
            INSERT INTO Users (Name, Email, Age, IsActive) 
            VALUES (@name, @email, @age, @isActive)";
        
        cmd.Parameters.Add("@name", Microsoft.Data.Sqlite.SqliteType.Text);
        cmd.Parameters.Add("@email", Microsoft.Data.Sqlite.SqliteType.Text);
        cmd.Parameters.Add("@age", Microsoft.Data.Sqlite.SqliteType.Integer);
        cmd.Parameters.Add("@isActive", Microsoft.Data.Sqlite.SqliteType.Integer);

        for (int i = 1; i <= recordCount; i++)
        {
            cmd.Parameters["@name"].Value = $"User{i}";
            cmd.Parameters["@email"].Value = $"user{i}@example.com";
            cmd.Parameters["@age"].Value = 20 + (i % 50);
            cmd.Parameters["@isActive"].Value = i % 2 == 0;
            await cmd.ExecuteNonQueryAsync();
        }

        Console.WriteLine($"✅ 已创建 {recordCount} 条测试数据");
    }

    private static async Task RunPerformanceTestsAsync(SqliteConnection connection)
    {
        const int iterations = 100;
        
        Console.WriteLine($"🚀 开始性能测试 (执行 {iterations} 次)...");
        Console.WriteLine();

        // 测试 1: 传统装箱方式 vs Sqlx 优化方式
        await CompareDataReadingPerformanceAsync(connection, iterations);
        
        Console.WriteLine();
        
        // 测试 2: GetOrdinal 缓存效果
        await CompareGetOrdinalPerformanceAsync(connection, iterations);
        
        Console.WriteLine();
        
        // 测试 3: 内存分配对比
        await CompareMemoryAllocationAsync(connection, iterations);
    }

    private static async Task CompareDataReadingPerformanceAsync(SqliteConnection connection, int iterations)
    {
        Console.WriteLine("📊 数据读取性能对比:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // 传统装箱方式
        var boxingTime = await MeasureBoxingApproachAsync(connection, iterations);
        
        // Sqlx 优化方式
        var optimizedTime = await MeasureOptimizedApproachAsync(connection, iterations);
        
        // 计算性能提升
        var improvement = (boxingTime - optimizedTime) / boxingTime * 100;
        
        Console.WriteLine($"📈 传统装箱方式:     {boxingTime:F2} ms");
        Console.WriteLine($"⚡ Sqlx 优化方式:     {optimizedTime:F2} ms");
        Console.WriteLine($"🎯 性能提升:          {improvement:F1}%");
    }

    private static async Task<double> MeasureBoxingApproachAsync(SqliteConnection connection, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Email, Age, IsActive FROM Users LIMIT 100";
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // 传统装箱方式 - 每次都进行装箱拆箱
                var user = new
                {
                    Id = reader["Id"] is DBNull ? 0 : Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"] is DBNull ? string.Empty : reader["Name"].ToString(),
                    Email = reader["Email"] is DBNull ? string.Empty : reader["Email"].ToString(),
                    Age = reader["Age"] is DBNull ? 0 : Convert.ToInt32(reader["Age"]),
                    IsActive = reader["IsActive"] is DBNull ? false : Convert.ToBoolean(reader["IsActive"])
                };
            }
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static async Task<double> MeasureOptimizedApproachAsync(SqliteConnection connection, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Email, Age, IsActive FROM Users LIMIT 100";
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            // Sqlx 优化方式 - 缓存 GetOrdinal 并使用强类型方法
            int ordinalId = reader.GetOrdinal("Id");
            int ordinalName = reader.GetOrdinal("Name");
            int ordinalEmail = reader.GetOrdinal("Email");
            int ordinalAge = reader.GetOrdinal("Age");
            int ordinalIsActive = reader.GetOrdinal("IsActive");
            
            while (await reader.ReadAsync())
            {
                var user = new
                {
                    Id = reader.IsDBNull(ordinalId) ? 0 : reader.GetInt32(ordinalId),
                    Name = reader.IsDBNull(ordinalName) ? string.Empty : reader.GetString(ordinalName),
                    Email = reader.IsDBNull(ordinalEmail) ? string.Empty : reader.GetString(ordinalEmail),
                    Age = reader.IsDBNull(ordinalAge) ? 0 : reader.GetInt32(ordinalAge),
                    IsActive = reader.IsDBNull(ordinalIsActive) ? false : reader.GetBoolean(ordinalIsActive)
                };
            }
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static async Task CompareGetOrdinalPerformanceAsync(SqliteConnection connection, int iterations)
    {
        Console.WriteLine("🔍 GetOrdinal 缓存效果对比:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // 每次调用 GetOrdinal
        var uncachedTime = await MeasureUncachedGetOrdinalAsync(connection, iterations);
        
        // 缓存 GetOrdinal
        var cachedTime = await MeasureCachedGetOrdinalAsync(connection, iterations);
        
        var improvement = (uncachedTime - cachedTime) / uncachedTime * 100;
        
        Console.WriteLine($"🐌 未缓存 GetOrdinal:  {uncachedTime:F2} ms");
        Console.WriteLine($"⚡ 缓存 GetOrdinal:    {cachedTime:F2} ms");
        Console.WriteLine($"🎯 缓存优化提升:      {improvement:F1}%");
    }

    private static async Task<double> MeasureUncachedGetOrdinalAsync(SqliteConnection connection, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Email FROM Users LIMIT 100";
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // 每次都调用 GetOrdinal - 性能较差
                var id = reader.GetInt32(reader.GetOrdinal("Id"));
                var name = reader.GetString(reader.GetOrdinal("Name"));
                var email = reader.GetString(reader.GetOrdinal("Email"));
            }
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static async Task<double> MeasureCachedGetOrdinalAsync(SqliteConnection connection, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Email FROM Users LIMIT 100";
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            // 缓存 GetOrdinal - Sqlx 的优化方式
            int ordinalId = reader.GetOrdinal("Id");
            int ordinalName = reader.GetOrdinal("Name");
            int ordinalEmail = reader.GetOrdinal("Email");
            
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(ordinalId);
                var name = reader.GetString(ordinalName);
                var email = reader.GetString(ordinalEmail);
            }
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static async Task CompareMemoryAllocationAsync(SqliteConnection connection, int iterations)
    {
        Console.WriteLine("💾 内存分配对比:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // 强制垃圾回收以获得准确的内存测量
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);
        
        // 装箱方式的内存使用
        await MeasureBoxingApproachAsync(connection, iterations / 10);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var boxingMemory = GC.GetTotalMemory(false) - initialMemory;
        
        // 重置内存测量
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        initialMemory = GC.GetTotalMemory(false);
        
        // 优化方式的内存使用
        await MeasureOptimizedApproachAsync(connection, iterations / 10);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var optimizedMemory = GC.GetTotalMemory(false) - initialMemory;
        
        var memoryReduction = boxingMemory > 0 ? (boxingMemory - optimizedMemory) / (double)boxingMemory * 100 : 0;
        
        Console.WriteLine($"📦 装箱方式内存使用:  {boxingMemory / 1024.0:F1} KB");
        Console.WriteLine($"⚡ 优化方式内存使用:  {optimizedMemory / 1024.0:F1} KB");
        Console.WriteLine($"🎯 内存使用减少:      {memoryReduction:F1}%");
        
        Console.WriteLine();
        Console.WriteLine("🎉 性能优化总结:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("✅ 消除装箱拆箱，提升执行速度");
        Console.WriteLine("✅ GetOrdinal 缓存，减少字符串查找");
        Console.WriteLine("✅ 强类型访问，提高类型安全");
        Console.WriteLine("✅ 修复内存泄漏，提升内存安全");
        Console.WriteLine("✅ 生成代码可读性更好，维护更容易");
    }
}
