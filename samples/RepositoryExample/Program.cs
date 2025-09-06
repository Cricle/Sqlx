// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using Sqlx.Annotations;

internal class Program
{
    private const string ConnectionString = "server=(localdb)\\mssqllocaldb;database=sqlx_sample;integrated security=true";

    private static async Task Main(string[] args)
    {
        Console.WriteLine("🎯 Sqlx Repository Pattern Example!");
        Console.WriteLine("=====================================\n");

        // Check for test mode arguments
        bool useRealDatabase = args.Length > 0 && args[0].ToLower() == "--real-db";
        bool useSQLiteTest = args.Length > 0 && args[0].ToLower() == "--sqlite";
        bool useAdvancedSQLiteTest = args.Length > 0 && args[0].ToLower() == "--advanced";

        if (useRealDatabase)
        {
            Console.WriteLine("🗄️ 真实数据库模式 Real Database Mode (SQL Server)");
            Console.WriteLine("===================================================");
            
            // Test database connection first
            bool connectionSuccessful = await RealDatabaseTest.TestDatabaseConnection();
            
            if (connectionSuccessful)
            {
                bool testsPassed = await RealDatabaseTest.RunRealDatabaseTests();
                Console.WriteLine($"\n🎯 真实数据库测试结果 Real Database Test Result: {(testsPassed ? "✅ 成功 SUCCESS" : "❌ 失败 FAILED")}");
            }
            else
            {
                Console.WriteLine("\n⚠️ 无法连接到数据库，显示设置说明...");
                RealDatabaseTest.ShowDatabaseSetupInstructions();
            }
            
            return;
        }

        if (useSQLiteTest)
        {
            Console.WriteLine("🗄️ SQLite 代码生成验证模式 SQLite Code Generation Verification Mode");
            Console.WriteLine("==================================================================");
            
            SQLiteTest.ShowSQLiteCapabilities();
            
            bool testsPassed = await SQLiteTest.RunSQLiteTests();
            Console.WriteLine($"\n🎯 SQLite 测试结果 SQLite Test Result: {(testsPassed ? "✅ 成功 SUCCESS" : "❌ 失败 FAILED")}");
            
            return;
        }

        if (useAdvancedSQLiteTest)
        {
            Console.WriteLine("🚀 高级SQLite企业级功能模式 Advanced SQLite Enterprise Features Mode");
            Console.WriteLine("====================================================================");
            Console.WriteLine("⚠️  高级功能演示暂时不可用 - 源生成器暂不支持接口继承");
            Console.WriteLine("⚠️  Advanced features demo temporarily unavailable - Source generator doesn't support interface inheritance yet");
            Console.WriteLine("🔄 正在切换到基本模式... Switching to basic mode...");
            
            // Fall through to basic mode
        }

        Console.WriteLine("🧪 模拟数据模式 Mock Data Mode");
        Console.WriteLine("====================================");
        Console.WriteLine("💡 可用的测试模式 Available test modes:");
        Console.WriteLine("   --real-db  : 真实 SQL Server 数据库测试");
        Console.WriteLine("   --sqlite   : SQLite 代码生成验证测试");
        Console.WriteLine("   --advanced : 高级SQLite企业级功能测试 🚀");
        Console.WriteLine("   (无参数)    : 模拟数据演示模式\n");

        // First check if attributes are available
        TestAttributes.CheckAttributes();

        using var connection = new SqlConnection(ConnectionString);
        var userService = new UserRepository(connection); // UserRepository implements IUserService

        try
        {
            // 注意：跳过数据库连接，直接演示仓储模式功能
            // Note: Skip database connection, directly demonstrate repository pattern functionality
            // connection.Open();

            Console.WriteLine("\n=== 仓储模式演示 Repository Pattern Demo ===\n");

            // 演示 GetAllUsers - 查询所有用户
            Console.WriteLine("1. 测试 GetAllUsers() - [RawSql(\"SELECT * FROM users\")]");
            var allUsers = userService.GetAllUsers();
            Console.WriteLine($"   找到 {allUsers.Count} 个用户:");
            foreach (var u in allUsers)
            {
                Console.WriteLine($"     - {u.Name} ({u.Email}) - 创建于: {u.CreatedAt:yyyy-MM-dd}");
            }

            // 演示异步方法
            Console.WriteLine("\n2. 测试 GetAllUsersAsync() - 异步版本");
            var allUsersAsync = await userService.GetAllUsersAsync();
            Console.WriteLine($"   异步查询找到 {allUsersAsync.Count} 个用户");

            // 演示根据ID查询
            Console.WriteLine("\n3. 测试 GetUserById() - [RawSql(\"SELECT * FROM users WHERE Id = @id\")]");
            var user1 = userService.GetUserById(1);
            Console.WriteLine($"   用户 ID=1: {user1?.Name ?? "未找到"}");
            
            var user2 = userService.GetUserById(2);
            Console.WriteLine($"   用户 ID=2: {user2?.Name ?? "未找到"}");
            
            var user999 = userService.GetUserById(999);
            Console.WriteLine($"   用户 ID=999: {user999?.Name ?? "未找到"}");

            // 演示创建用户
            Console.WriteLine("\n4. 测试 CreateUser() - [SqlExecuteType(SqlExecuteTypes.Insert, \"users\")]");
            var newUser = new User 
            { 
                Id = 3,
                Name = "Alice Johnson", 
                Email = "alice@example.com", 
                CreatedAt = DateTime.Now 
            };
            var insertedRows = userService.CreateUser(newUser);
            Console.WriteLine($"   创建用户结果: 影响 {insertedRows} 行");

            // 演示更新用户
            Console.WriteLine("\n5. 测试 UpdateUser() - [SqlExecuteType(SqlExecuteTypes.Update, \"users\")]");
            newUser.Name = "Alice Smith";
            newUser.Email = "alice.smith@example.com";
            var updatedRows = userService.UpdateUser(newUser);
            Console.WriteLine($"   更新用户结果: 影响 {updatedRows} 行");

            // 演示删除用户
            Console.WriteLine("\n6. 测试 DeleteUser() - [SqlExecuteType(SqlExecuteTypes.Delete, \"users\")]");
            var deletedRows = userService.DeleteUser(3);
            Console.WriteLine($"   删除用户结果: 影响 {deletedRows} 行");

            Console.WriteLine("\n=== 仓储模式核心特性展示 ===");
            Console.WriteLine("✅ RepositoryFor 特性: 正确指向服务接口 IUserService");
            Console.WriteLine("✅ TableName 特性: 自动解析表名为 'users'");  
            Console.WriteLine("✅ 自动方法生成: 所有接口方法都有对应实现");
            Console.WriteLine("✅ SQL 特性注入: RawSql 和 SqlExecuteType 特性");
            Console.WriteLine("✅ 异步支持: 完整的 Task/async 模式");
            Console.WriteLine("✅ 类型安全: 编译时类型检查");
            Console.WriteLine("✅ 依赖注入: 标准 DI 构造函数模式");

            Console.WriteLine("Repository pattern demonstration completed!");
            Console.WriteLine("Note: This demonstrates the service interface pattern with Sqlx repository generation.");

            // Run comprehensive verification tests
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("运行全面验证测试 Running Comprehensive Verification Tests");
            Console.WriteLine(new string('=', 60));

            bool allTestsPassed = await VerificationTest.RunAllVerificationTests();

            Console.WriteLine("\n" + new string('=', 60));
            if (allTestsPassed)
            {
                Console.WriteLine("🎉 所有功能验证通过！仓储模式实现完全正常！");
                Console.WriteLine("🎉 All functionality verification passed! Repository pattern implementation is fully functional!");
            }
            else
            {
                Console.WriteLine("⚠️  部分功能验证失败，请检查上述错误信息。");
                Console.WriteLine("⚠️  Some functionality verification failed, please check the error messages above.");
            }
            Console.WriteLine(new string('=', 60));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    // Implementation class for testing
    // UserImpl class is no longer needed since we use User class directly
}
