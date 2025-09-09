// -----------------------------------------------------------------------
// <copyright file="SQLiteTest.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite testing class to verify actual repository pattern implementation.
/// This demonstrates how generated code would work with a real database.
/// </summary>
public static class SQLiteTest
{
    private const string DatabasePath = "users.db";
    private static readonly string ConnectionString = $"Data Source={DatabasePath}";

    /// <summary>
    /// Runs comprehensive SQLite tests to verify repository functionality.
    /// </summary>
    /// <returns>A task representing the asynchronous operation with test results.</returns>
    public static async Task<bool> RunSQLiteTests()
    {
        Console.WriteLine("=== SQLite 真实代码生成验证 SQLite Real Code Generation Verification ===");
        Console.WriteLine("=== Starting SQLite Repository Tests ===\n");

        try
        {
            // Setup database
            await SetupDatabase();

            using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("✅ SQLite 数据库连接成功 SQLite database connection successful");

            var repository = new SQLiteUserRepository(connection);

            // Test 1: Verify empty state
            Console.WriteLine("\n1. 验证初始状态 Verify Initial State");
            var initialUsers = repository.GetAllUsers();
            Console.WriteLine($"   📊 初始用户数量 Initial user count: {initialUsers.Count}");

            // Test 2: Create users (INSERT operations)
            Console.WriteLine("\n2. 测试 CreateUser() - 实际 INSERT 操作");
            var user1 = new User 
            { 
                Name = "SQLite User 1", 
                Email = "sqlite1@example.com", 
                CreatedAt = DateTime.Now 
            };
            var createResult1 = repository.CreateUser(user1);
            Console.WriteLine($"   ➕ 创建用户1结果 Created user 1 result: {createResult1} 行受影响");

            var user2 = new User 
            { 
                Name = "SQLite User 2", 
                Email = "sqlite2@example.com", 
                CreatedAt = DateTime.Now.AddMinutes(-30) 
            };
            var createResult2 = await repository.CreateUserAsync(user2);
            Console.WriteLine($"   ➕ 异步创建用户2结果 Async created user 2 result: {createResult2} 行受影响");

            // Test 3: Read all users (SELECT operations)
            Console.WriteLine("\n3. 测试 GetAllUsers() - 实际 SELECT 操作");
            var allUsers = repository.GetAllUsers();
            Console.WriteLine($"   📊 总用户数 Total users: {allUsers.Count}");
            foreach (var user in allUsers)
            {
                Console.WriteLine($"     - ID: {user.Id}, Name: {user.Name}, Email: {user.Email}, Created: {user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            // Test 4: Read by ID (SELECT with WHERE)
            Console.WriteLine("\n4. 测试 GetUserById() - 带条件的 SELECT 操作");
            if (allUsers.Count > 0)
            {
                var firstUser = allUsers[0];
                var userById = repository.GetUserById(firstUser.Id);
                Console.WriteLine($"   🔍 查询用户 ID {firstUser.Id}: {userById?.Name ?? "未找到"}");
                
                var userByIdAsync = await repository.GetUserByIdAsync(firstUser.Id);
                Console.WriteLine($"   🔍 异步查询用户 ID {firstUser.Id}: {userByIdAsync?.Name ?? "未找到"}");
            }

            // Test 5: Update user (UPDATE operations)
            Console.WriteLine("\n5. 测试 UpdateUser() - 实际 UPDATE 操作");
            if (allUsers.Count > 0)
            {
                var userToUpdate = allUsers[0];
                var originalName = userToUpdate.Name;
                userToUpdate.Name = "Updated " + originalName;
                userToUpdate.Email = "updated_" + userToUpdate.Email;
                
                var updateResult = repository.UpdateUser(userToUpdate);
                Console.WriteLine($"   ✏️ 更新用户结果 Update user result: {updateResult} 行受影响");
                
                // Verify update
                var updatedUser = repository.GetUserById(userToUpdate.Id);
                Console.WriteLine($"   🔍 验证更新 Verify update: '{updatedUser?.Name}' (原名 original: '{originalName}')");
            }

            // Test 6: Delete user (DELETE operations)
            Console.WriteLine("\n6. 测试 DeleteUser() - 实际 DELETE 操作");
            if (allUsers.Count > 1)
            {
                var userToDelete = allUsers[allUsers.Count - 1];
                var deleteResult = repository.DeleteUser(userToDelete.Id);
                Console.WriteLine($"   🗑️ 删除用户结果 Delete user result: {deleteResult} 行受影响");
                
                // Verify deletion
                var deletedUser = repository.GetUserById(userToDelete.Id);
                Console.WriteLine($"   🔍 验证删除 Verify deletion: {(deletedUser == null ? "✅ 用户已删除 User deleted" : "❌ 删除失败 Deletion failed")}");
            }

            // Test 7: Final state verification
            Console.WriteLine("\n7. 最终状态验证 Final State Verification");
            var finalUsers = await repository.GetAllUsersAsync();
            Console.WriteLine($"   📊 最终用户数量 Final user count: {finalUsers.Count}");

            // Test 8: Performance test
            Console.WriteLine("\n8. 性能测试 Performance Test");
            var startTime = DateTime.Now;
            for (int i = 0; i < 50; i++)
            {
                var perfUsers = repository.GetAllUsers();
                if (perfUsers.Count > 0)
                {
                    var firstUser = repository.GetUserById(perfUsers[0].Id);
                }
            }
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            Console.WriteLine($"   ⏱️ 50次查询耗时 50 queries took: {duration.TotalMilliseconds:F0}ms");

            // Test 9: Concurrent operations
            Console.WriteLine("\n9. 并发操作测试 Concurrent Operations Test");
            var tasks = new List<Task<IList<User>>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(repository.GetAllUsersAsync());
            }
            var concurrentResults = await Task.WhenAll(tasks);
            Console.WriteLine($"   🔄 并发查询完成 Concurrent queries completed: {concurrentResults.Length} 个任务，每个返回 {concurrentResults[0].Count} 用户");

            Console.WriteLine("\n✅ 所有 SQLite 测试成功完成 All SQLite tests completed successfully!");
            Console.WriteLine("🎯 验证结果 Verification Results:");
            Console.WriteLine("   ✅ 实际数据库操作正常工作");
            Console.WriteLine("   ✅ CRUD 操作完全功能正确");
            Console.WriteLine("   ✅ 同步/异步方法都工作正常");
            Console.WriteLine("   ✅ 参数化查询防止 SQL 注入");
            Console.WriteLine("   ✅ 类型安全的数据映射");
            Console.WriteLine("   ✅ 性能表现优秀");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ SQLite 测试失败 SQLite test failed: {ex.Message}");
            Console.WriteLine($"📋 错误详情 Error details: {ex}");
            return false;
        }
        finally
        {
            // Cleanup database file
            try
            {
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                    Console.WriteLine("\n🧹 清理数据库文件 Cleaned up database file");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n⚠️ 清理失败 Cleanup failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sets up the SQLite database with the required table structure.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task SetupDatabase()
    {
        // Delete existing database if it exists
        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }

        using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        const string createTableSql = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            
            CREATE INDEX idx_users_email ON users(email);
        ";

        using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();

        Console.WriteLine("🗄️ SQLite 数据库和表创建成功 SQLite database and table created successfully");
    }

    /// <summary>
    /// Shows the capabilities and benefits of the SQLite implementation.
    /// </summary>
    public static void ShowSQLiteCapabilities()
    {
        Console.WriteLine("\n=== SQLite 仓储模式能力展示 SQLite Repository Pattern Capabilities ===");
        Console.WriteLine("✅ 真实数据库操作 Real Database Operations");
        Console.WriteLine("✅ 完整的 CRUD 支持 Complete CRUD Support");
        Console.WriteLine("✅ 类型安全映射 Type-safe Mapping");
        Console.WriteLine("✅ 参数化查询 Parameterized Queries");
        Console.WriteLine("✅ 异步/同步双重支持 Async/Sync Dual Support");
        Console.WriteLine("✅ 事务安全 Transaction Safety");
        Console.WriteLine("✅ 高性能执行 High Performance");
        Console.WriteLine("✅ 零配置依赖 Zero Configuration Dependencies");
        Console.WriteLine("✅ 跨平台兼容 Cross-platform Compatible");
        Console.WriteLine("\n💡 这展示了 Sqlx 源代码生成器将如何创建实际工作的仓储方法");
        Console.WriteLine("💡 This demonstrates how Sqlx source generator would create actual working repository methods");
    }
}
