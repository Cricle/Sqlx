// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.GettingStarted;

/// <summary>
/// 🚀 Sqlx 快速入门示例
/// 
/// 这个示例展示了 Sqlx 的核心功能：
/// ✨ Repository 模式自动生成
/// 🎯 智能 SQL 推断
/// 💡 类型安全的数据库操作
/// ⚡ 高性能零反射执行
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Sqlx 快速入门示例");
        Console.WriteLine("=".PadRight(50, '='));
        
        // 🔧 设置 SQLite 数据库
        var connectionString = "Data Source=:memory:";
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        
        // 📋 创建表结构
        await SetupDatabase(connection);
        
        // 🎯 创建 Repository (自动生成实现)
        var userService = new UserRepository(connection);
        
        // ✨ 演示 CRUD 操作
        await DemonstrateCrudOperations(userService);
        
        // 🧪 演示高级功能
        await DemonstrateAdvancedFeatures(userService);
        
        Console.WriteLine("\n🎉 示例完成！按任意键退出...");
        Console.ReadKey();
    }
    
    /// <summary>
    /// 设置数据库表结构
    /// </summary>
    static async Task SetupDatabase(DbConnection connection)
    {
        Console.WriteLine("\n📋 设置数据库...");
        
        var createTable = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT UNIQUE NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN DEFAULT 1
            )";
            
        using var command = connection.CreateCommand();
        command.CommandText = createTable;
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("✅ 数据库表创建成功");
    }
    
    /// <summary>
    /// 演示基础 CRUD 操作
    /// </summary>
    static async Task DemonstrateCrudOperations(IUserService userService)
    {
        Console.WriteLine("\n🎯 演示 CRUD 操作...");
        
        // ➕ 创建用户 (自动推断为 INSERT)
        var newUsers = new[]
        {
            new User { Name = "Alice", Email = "alice@example.com" },
            new User { Name = "Bob", Email = "bob@example.com" },
            new User { Name = "Charlie", Email = "charlie@example.com" }
        };
        
        foreach (var user in newUsers)
        {
            user.CreatedAt = DateTime.Now;
            var affected = await userService.CreateUserAsync(user);
            Console.WriteLine($"✅ 创建用户 {user.Name}: {affected} 行受影响");
        }
        
        // 📋 查询所有用户 (自动推断为 SELECT)
        var allUsers = await userService.GetAllUsersAsync();
        Console.WriteLine($"📋 查询到 {allUsers.Count} 个用户:");
        foreach (var user in allUsers)
        {
            Console.WriteLine($"   - {user.Name} ({user.Email}) - {(user.IsActive ? "活跃" : "非活跃")}");
        }
        
        // 🔍 按 ID 查询用户 (自动推断为 SELECT WHERE)
        var firstUser = await userService.GetUserByIdAsync(1);
        if (firstUser != null)
        {
            Console.WriteLine($"🔍 按 ID 查询: {firstUser.Name} ({firstUser.Email})");
        }
        
        // ✏️ 更新用户 (自动推断为 UPDATE)
        if (firstUser != null)
        {
            firstUser.Name = "Alice Smith";
            firstUser.Email = "alice.smith@example.com";
            var affected = await userService.UpdateUserAsync(firstUser);
            Console.WriteLine($"✏️ 更新用户: {affected} 行受影响");
        }
        
        // ❌ 删除用户 (自动推断为 DELETE)
        var affected2 = await userService.DeleteUserAsync(3);
        Console.WriteLine($"❌ 删除用户 ID 3: {affected2} 行受影响");
    }
    
    /// <summary>
    /// 演示高级功能
    /// </summary>
    static async Task DemonstrateAdvancedFeatures(IUserService userService)
    {
        Console.WriteLine("\n🧪 演示高级功能...");
        
        // 🎯 自定义 SQL 查询
        var userByEmail = await userService.GetUserByEmailAsync("alice.smith@example.com");
        if (userByEmail != null)
        {
            Console.WriteLine($"🎯 按邮箱查询: {userByEmail.Name}");
        }
        
        // 📊 标量查询
        var activeCount = await userService.CountActiveUsersAsync();
        Console.WriteLine($"📊 活跃用户数量: {activeCount}");
        
        // 📈 复杂查询
        var recentUsers = await userService.GetRecentUsersAsync(DateTime.Now.AddDays(-1));
        Console.WriteLine($"📈 最近用户数量: {recentUsers.Count}");
    }
}

/// <summary>
/// 用户实体类
/// </summary>
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 用户服务接口 - 定义所有数据库操作
/// </summary>
public interface IUserService
{
    // 🎯 基础 CRUD 操作 (自动推断 SQL)
    Task<IList<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<int> CreateUserAsync(User user);
    Task<int> UpdateUserAsync(User user);
    Task<int> DeleteUserAsync(int id);
    
    // 🎯 自定义 SQL 查询
    [Sqlx("SELECT * FROM users WHERE email = @email")]
    Task<User?> GetUserByEmailAsync(string email);
    
    // 📊 标量查询
    [Sqlx("SELECT COUNT(*) FROM users WHERE is_active = 1")]
    Task<int> CountActiveUsersAsync();
    
    // 📈 复杂查询
    [Sqlx("SELECT * FROM users WHERE created_at > @since ORDER BY created_at DESC")]
    Task<IList<User>> GetRecentUsersAsync(DateTime since);
}

/// <summary>
/// 用户 Repository 实现
/// 🚀 使用 [RepositoryFor] 特性自动生成所有方法实现
/// ✨ 零样板代码，编译时生成高性能实现
/// </summary>
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    /// <summary>
    /// 构造函数 - 这是您需要写的唯一代码！
    /// </summary>
    /// <param name="connection">数据库连接</param>
    public UserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 🎉 所有 IUserService 接口方法都会被自动生成！
    // ✨ 包括：
    // - SQL 语句生成 (基于方法名推断或自定义 SQL)
    // - 参数绑定 (防止 SQL 注入)
    // - 结果映射 (高性能强类型读取)
    // - 异常处理 (友好的错误信息)
    // - 资源管理 (自动释放资源)
}

