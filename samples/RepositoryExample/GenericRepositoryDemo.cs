// -----------------------------------------------------------------------
// <copyright file="GenericRepositoryDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx.Core;

namespace Sqlx.RepositoryExample;

/// <summary>
/// Demonstrates the new generic repository functionality and GetOrdinal caching optimization.
/// </summary>
public static class GenericRepositoryDemo
{
    /// <summary>
    /// Runs a comprehensive demo of generic repository features.
    /// </summary>
    public static async Task RunDemoAsync()
    {
        Console.WriteLine("\n🎭 Generic Repository Demonstration");
        Console.WriteLine("===================================");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        await SetupDatabaseAsync(connection);
        
        // Test the generic repository
        await TestGenericRepositoryAsync(connection);
        
        // Test performance monitoring
        TestPerformanceMonitoring();
        
        // Demo GetOrdinal caching benefits
        await DemoGetOrdinalCachingAsync(connection);
        
        Console.WriteLine("\n✅ Generic repository demo completed successfully!");
    }
    
    private static async Task SetupDatabaseAsync(SqliteConnection connection)
    {
        Console.WriteLine("\n📋 Setting up test database...");
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
        await cmd.ExecuteNonQueryAsync();
        
        // Insert test data
        for (int i = 1; i <= 1000; i++)
        {
            cmd.CommandText = "INSERT INTO Users (Name, Email) VALUES (@name, @email)";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@name", $"GenericUser{i}");
            cmd.Parameters.AddWithValue("@email", $"generic{i}@example.com");
            await cmd.ExecuteNonQueryAsync();
        }
        
        Console.WriteLine("✅ Database setup completed with 1000 test records");
    }
    
    private static Task TestGenericRepositoryAsync(SqliteConnection connection)
    {
        Console.WriteLine("\n🎯 Testing Generic Repository Implementation");
        Console.WriteLine("============================================");
        
        // Test the simple generic repository
        var simpleRepo = new TestGenericRepository(connection);
        
        // Test GetAll method
        using (var monitor = PerformanceMonitor.StartMonitoring("GenericRepository.GetAll"))
        {
            var allUsers = simpleRepo.GetAll();
            Console.WriteLine($"📊 Retrieved {allUsers.Count} users using generic repository");
            Console.WriteLine($"   First user: {allUsers[0].Name} ({allUsers[0].Email})");
            Console.WriteLine($"   Last user: {allUsers[^1].Name} ({allUsers[^1].Email})");
        }
        
        // Test Create method
        var newUser = new User
        {
            Name = "Generic Test User",
            Email = "generictest@example.com",
            CreatedAt = DateTime.Now
        };
        
        using (var monitor = PerformanceMonitor.StartMonitoring("GenericRepository.Create"))
        {
            int rowsAffected = simpleRepo.Create(newUser);
            Console.WriteLine($"✅ Created new user: {rowsAffected} rows affected");
        }
        
        // Test the advanced generic repository  
        var advancedRepo = new GenericUserRepository(connection);
        
        // Test GetById
        User? user = null;
        using (var monitor = PerformanceMonitor.StartMonitoring("GenericRepository.GetById"))
        {
            user = advancedRepo.GetById(1);
            Console.WriteLine($"🔍 Retrieved user by ID: {user?.Name ?? "Not found"}");
        }
        
        // Test Update method
        if (user != null)
        {
            using (var monitor = PerformanceMonitor.StartMonitoring("GenericRepository.Update"))
            {
                user.Name = "Updated " + user.Name;
                int updateResult = advancedRepo.Update(user);
                Console.WriteLine($"✏️ Updated user: {updateResult} rows affected");
            }
        }
        
        // Test Delete method
        using (var monitor = PerformanceMonitor.StartMonitoring("GenericRepository.Delete"))
        {
            int deleteResult = advancedRepo.Delete(999); // Delete a high ID user
            Console.WriteLine($"🗑️ Deleted user: {deleteResult} rows affected");
        }
        
        return Task.CompletedTask;
    }
    
    private static void TestPerformanceMonitoring()
    {
        Console.WriteLine("\n📊 Performance Monitoring Results");
        Console.WriteLine("=================================");
        
        var stats = PerformanceMonitor.GetStatistics();
        
        Console.WriteLine($"Total operations monitored: {stats.TotalOperations}");
        Console.WriteLine($"Average execution time: {stats.AverageExecutionTimeMs:F2}ms");
        
        foreach (var methodStat in stats.MethodStatistics)
        {
            Console.WriteLine($"  📈 {methodStat.Key}:");
            Console.WriteLine($"     Executions: {methodStat.Value.TotalExecutions}");
            Console.WriteLine($"     Avg time: {methodStat.Value.AverageExecutionTimeMs:F2}ms");
            Console.WriteLine($"     Success rate: {methodStat.Value.SuccessRate:P2}");
        }
    }
    
    private static async Task DemoGetOrdinalCachingAsync(SqliteConnection connection)
    {
        Console.WriteLine("\n⚡ GetOrdinal Caching Performance Demo");
        Console.WriteLine("=====================================");
        
        Console.WriteLine("🔬 This demonstrates the GetOrdinal caching optimization:");
        Console.WriteLine("   Traditional approach: GetOrdinal called for every row");
        Console.WriteLine("   Sqlx approach: GetOrdinal cached once, reused for all rows");
        
        // Simulate traditional approach timing
        var traditionalTime = await MeasureTraditionalApproachAsync(connection);
        Console.WriteLine($"📊 Traditional approach: {traditionalTime}ms");
        
        // Measure Sqlx optimized approach
        var optimizedTime = await MeasureSqlxOptimizedApproachAsync(connection);
        Console.WriteLine($"🚀 Sqlx optimized approach: {optimizedTime}ms");
        
        var improvement = ((traditionalTime - optimizedTime) / traditionalTime) * 100;
        Console.WriteLine($"🎯 Performance improvement: {improvement:F1}% faster");
        
        Console.WriteLine("\n💡 The optimization is especially effective with:");
        Console.WriteLine("   • Large result sets (1000+ rows)");
        Console.WriteLine("   • Wide tables (many columns)");
        Console.WriteLine("   • Repeated queries with same schema");
    }
    
    private static async Task<long> MeasureTraditionalApproachAsync(SqliteConnection connection)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Email, CreatedAt FROM Users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var users = new List<User>();
        
        while (await reader.ReadAsync())
        {
            // Simulate traditional approach - GetOrdinal called every row
            var user = new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
            users.Add(user);
        }
        
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
    
    private static async Task<long> MeasureSqlxOptimizedApproachAsync(SqliteConnection connection)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Email, CreatedAt FROM Users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var users = new List<User>();
        
        // Simulate Sqlx approach - GetOrdinal cached once
        int ordinalId = reader.GetOrdinal("Id");
        int ordinalName = reader.GetOrdinal("Name");
        int ordinalEmail = reader.GetOrdinal("Email");
        int ordinalCreatedAt = reader.GetOrdinal("CreatedAt");
        
        while (await reader.ReadAsync())
        {
            var user = new User
            {
                Id = reader.GetInt32(ordinalId),
                Name = reader.GetString(ordinalName),
                Email = reader.GetString(ordinalEmail),
                CreatedAt = reader.GetDateTime(ordinalCreatedAt)
            };
            users.Add(user);
        }
        
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}
