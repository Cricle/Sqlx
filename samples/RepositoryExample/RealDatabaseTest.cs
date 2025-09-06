// -----------------------------------------------------------------------
// <copyright file="RealDatabaseTest.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

/// <summary>
/// Real database testing class for the repository pattern.
/// </summary>
public static class RealDatabaseTest
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=SqlxRepositoryDemo;Integrated Security=true;TrustServerCertificate=true;";

    /// <summary>
    /// Runs comprehensive tests against a real database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation with test results.</returns>
    public static async Task<bool> RunRealDatabaseTests()
    {
        Console.WriteLine("=== 真实数据库测试 Real Database Tests ===");
        Console.WriteLine("=== Starting Real Database Integration Tests ===\n");

        try
        {
            using var connection = new SqlConnection(ConnectionString);
            
            // Test connection
            Console.WriteLine("1. 测试数据库连接 Testing Database Connection...");
            await connection.OpenAsync();
            Console.WriteLine("   ✅ 数据库连接成功 Database connection successful");

            var repository = new RealDatabaseUserRepository(connection);

            // Test GetAllUsers
            Console.WriteLine("\n2. 测试 GetAllUsers() - 真实SQL查询");
            var allUsers = repository.GetAllUsers();
            Console.WriteLine($"   📊 找到 {allUsers.Count} 个用户 Found {allUsers.Count} users:");
            foreach (var user in allUsers)
            {
                Console.WriteLine($"     - ID: {user.Id}, Name: {user.Name}, Email: {user.Email}");
            }

            // Test GetAllUsersAsync
            Console.WriteLine("\n3. 测试 GetAllUsersAsync() - 异步SQL查询");
            var allUsersAsync = await repository.GetAllUsersAsync();
            Console.WriteLine($"   📊 异步查询找到 {allUsersAsync.Count} 个用户 Async query found {allUsersAsync.Count} users");

            // Test GetUserById
            Console.WriteLine("\n4. 测试 GetUserById() - 带参数的SQL查询");
            if (allUsers.Count > 0)
            {
                var firstUser = allUsers[0];
                var userById = repository.GetUserById(firstUser.Id);
                Console.WriteLine($"   🔍 查询用户 ID {firstUser.Id}: {userById?.Name ?? "未找到"}");
            }

            // Test CreateUser
            Console.WriteLine("\n5. 测试 CreateUser() - INSERT操作");
            var newUser = new User
            {
                Name = "Test User " + DateTime.Now.Ticks,
                Email = $"test{DateTime.Now.Ticks}@example.com",
                CreatedAt = DateTime.Now
            };
            var createResult = repository.CreateUser(newUser);
            Console.WriteLine($"   ➕ 创建用户结果 Create user result: {createResult} 行受影响");

            // Test async create
            Console.WriteLine("\n6. 测试 CreateUserAsync() - 异步INSERT操作");
            var newUserAsync = new User
            {
                Name = "Async Test User " + DateTime.Now.Ticks,
                Email = $"asynctest{DateTime.Now.Ticks}@example.com",
                CreatedAt = DateTime.Now
            };
            var createResultAsync = await repository.CreateUserAsync(newUserAsync);
            Console.WriteLine($"   ➕ 异步创建用户结果 Async create result: {createResultAsync} 行受影响");

            // Get updated count
            Console.WriteLine("\n7. 验证插入结果 Verify Insert Results");
            var updatedUsers = repository.GetAllUsers();
            Console.WriteLine($"   📊 更新后的用户总数 Updated user count: {updatedUsers.Count}");

            // Test UpdateUser (if we have users)
            if (updatedUsers.Count > 0)
            {
                Console.WriteLine("\n8. 测试 UpdateUser() - UPDATE操作");
                var userToUpdate = updatedUsers[updatedUsers.Count - 1]; // Get last user
                var originalName = userToUpdate.Name;
                userToUpdate.Name = "Updated " + originalName;
                userToUpdate.Email = "updated_" + userToUpdate.Email;
                
                var updateResult = repository.UpdateUser(userToUpdate);
                Console.WriteLine($"   ✏️ 更新用户结果 Update user result: {updateResult} 行受影响");
                
                // Verify update
                var updatedUser = repository.GetUserById(userToUpdate.Id);
                Console.WriteLine($"   🔍 验证更新 Verify update: {updatedUser?.Name} (原名: {originalName})");
            }

            // Test DeleteUser (clean up test data)
            Console.WriteLine("\n9. 测试 DeleteUser() - DELETE操作");
            if (updatedUsers.Count > 5) // Only delete if we have more than 5 users (keep original sample data)
            {
                var userToDelete = updatedUsers[updatedUsers.Count - 1]; // Delete last user
                var deleteResult = repository.DeleteUser(userToDelete.Id);
                Console.WriteLine($"   🗑️ 删除用户结果 Delete user result: {deleteResult} 行受影响");
                
                // Verify deletion
                var deletedUser = repository.GetUserById(userToDelete.Id);
                Console.WriteLine($"   🔍 验证删除 Verify deletion: {(deletedUser == null ? "用户已删除 User deleted" : "删除失败 Deletion failed")}");
            }

            // Performance test
            Console.WriteLine("\n10. 性能测试 Performance Test");
            var startTime = DateTime.Now;
            for (int i = 0; i < 10; i++)
            {
                var perfUsers = repository.GetAllUsers();
                var firstUser = perfUsers.Count > 0 ? repository.GetUserById(perfUsers[0].Id) : null;
            }
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            Console.WriteLine($"   ⏱️ 10次查询耗时 10 queries took: {duration.TotalMilliseconds:F0}ms");

            Console.WriteLine("\n✅ 所有真实数据库测试完成 All real database tests completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 数据库测试失败 Database test failed: {ex.Message}");
            Console.WriteLine($"📋 错误详情 Error details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Tests database connection and setup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation with connection test results.</returns>
    public static async Task<bool> TestDatabaseConnection()
    {
        try
        {
            Console.WriteLine("🔍 测试数据库连接 Testing database connection...");
            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            
            // Test basic query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM users";
            var userCount = await command.ExecuteScalarAsync();
            
            Console.WriteLine($"✅ 数据库连接成功 Database connection successful");
            Console.WriteLine($"📊 当前用户数量 Current user count: {userCount}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 数据库连接失败 Database connection failed: {ex.Message}");
            Console.WriteLine("\n💡 解决方案建议 Suggested solutions:");
            Console.WriteLine("   1. 确保 SQL Server LocalDB 已安装 Ensure SQL Server LocalDB is installed");
            Console.WriteLine("   2. 运行 DatabaseSetup.sql 脚本创建数据库 Run DatabaseSetup.sql script to create database");
            Console.WriteLine("   3. 检查连接字符串配置 Check connection string configuration");
            return false;
        }
    }

    /// <summary>
    /// Provides instructions for setting up the database.
    /// </summary>
    public static void ShowDatabaseSetupInstructions()
    {
        Console.WriteLine("\n=== 数据库设置说明 Database Setup Instructions ===");
        Console.WriteLine("1. 安装 SQL Server LocalDB Install SQL Server LocalDB");
        Console.WriteLine("   - 下载: https://www.microsoft.com/sql-server/sql-server-downloads");
        Console.WriteLine("   - 或通过 Visual Studio Installer 安装");
        
        Console.WriteLine("\n2. 运行数据库设置脚本 Run database setup script");
        Console.WriteLine("   sqlcmd -S \"(localdb)\\MSSQLLocalDB\" -i DatabaseSetup.sql");
        
        Console.WriteLine("\n3. 或手动创建数据库 Or manually create database");
        Console.WriteLine("   - 打开 SQL Server Management Studio");
        Console.WriteLine("   - 连接到 (localdb)\\MSSQLLocalDB");
        Console.WriteLine("   - 执行 DatabaseSetup.sql 中的脚本");
        
        Console.WriteLine("\n4. 验证设置 Verify setup");
        Console.WriteLine("   - 数据库名称: SqlxRepositoryDemo");
        Console.WriteLine("   - 表名称: users");
        Console.WriteLine("   - 示例数据: 5 个用户记录");
    }
}
