// -----------------------------------------------------------------------
// Sqlx Comprehensive Demo - 展示核心功能
// 这个演示展示了 Sqlx 的核心功能和性能特性
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Core;

namespace Sqlx.ComprehensiveDemo;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Age { get; set; }
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 简化的用户仓储
/// </summary>
public class UserRepository
{
    private readonly SQLiteConnection _connection;
    
    public UserRepository(SQLiteConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<int> CreateAsync(User user)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO users (Name, Email, CreatedAt, Age, Department, Salary, IsActive) 
                           VALUES (@name, @email, @createdAt, @age, @department, @salary, @isActive);
                           SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", user.Name);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@createdAt", user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@age", user.Age);
        cmd.Parameters.AddWithValue("@department", user.Department);
        cmd.Parameters.AddWithValue("@salary", user.Salary);
        cmd.Parameters.AddWithValue("@isActive", user.IsActive ? 1 : 0);
        
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
    
    public async Task<List<User>> GetAllAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM users";
        
        var users = new List<User>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                Age = reader.GetInt32(4),
                Department = reader.GetString(5),
                Salary = reader.GetDecimal(6),
                IsActive = reader.GetInt32(7) == 1
            });
        }
        return users;
    }
    
    public async Task<int> GetCountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}

/// <summary>
/// 演示应用程序
/// </summary>
public class ComprehensiveDemoApp
{
    private readonly SQLiteConnection _connection;
    private readonly UserRepository _userRepository;
    
    public ComprehensiveDemoApp()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        _userRepository = new UserRepository(_connection);
        
        SetupDatabase().Wait();
    }
    
    private async Task SetupDatabase()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT UNIQUE NOT NULL,
                CreatedAt TEXT NOT NULL,
                Age INTEGER NOT NULL,
                Department TEXT NOT NULL,
                Salary DECIMAL(10,2) NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            )";
        await cmd.ExecuteNonQueryAsync();
    }
    
    public async Task RunDemoAsync()
    {
        Console.WriteLine("🚀 Sqlx 核心功能演示");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine();
        
        await DemoBasicOperations();
        await DemoCachePerformance();
        await DemoConnectionManagement();
        
        Console.WriteLine();
        Console.WriteLine("🎉 演示完成！");
    }
    
    private async Task DemoBasicOperations()
    {
        Console.WriteLine("📋 1. 基础操作演示");
        Console.WriteLine("-".PadRight(30, '-'));
        
        // 创建用户
        var user = new User
        {
            Name = "张三",
            Email = "zhangsan@example.com",
            Age = 28,
            Department = "Engineering",
            Salary = 80000m
        };
        
        var userId = await _userRepository.CreateAsync(user);
        Console.WriteLine($"✅ 创建用户成功，ID: {userId}");
        
        // 批量创建
        var users = GenerateSampleUsers(100);
        var sw = Stopwatch.StartNew();
        
        foreach (var u in users)
        {
            await _userRepository.CreateAsync(u);
        }
        
        sw.Stop();
        Console.WriteLine($"✅ 批量创建 {users.Count} 个用户，耗时: {sw.ElapsedMilliseconds}ms");
        
        var count = await _userRepository.GetCountAsync();
        Console.WriteLine($"✅ 总用户数: {count}");
        Console.WriteLine();
    }
    
    private async Task DemoCachePerformance()
    {
        Console.WriteLine("🧠 2. 缓存性能演示");
        Console.WriteLine("-".PadRight(30, '-'));
        
        // 测试缓存
        var cacheKey = "user_count";
        
        var sw = Stopwatch.StartNew();
        var count1 = IntelligentCacheManager.GetOrAdd(cacheKey, () =>
        {
            return _userRepository.GetCountAsync().Result;
        });
        var firstTime = sw.ElapsedMilliseconds;
        
        sw.Restart();
        var count2 = IntelligentCacheManager.GetOrAdd(cacheKey, () =>
        {
            return _userRepository.GetCountAsync().Result;
        });
        var secondTime = sw.ElapsedMilliseconds;
        
        Console.WriteLine($"🆕 首次查询: {firstTime}ms");
        Console.WriteLine($"⚡ 缓存查询: {secondTime}ms");
        
        var stats = IntelligentCacheManager.GetStatistics();
        Console.WriteLine($"📊 缓存命中率: {stats.HitRatio:P2}");
        
        // 添加一个 await 操作来满足 async 要求
        await Task.Delay(1);
        Console.WriteLine();
    }
    
    private async Task DemoConnectionManagement()
    {
        Console.WriteLine("🔌 3. 连接管理演示");
        Console.WriteLine("-".PadRight(30, '-'));
        
        var health = AdvancedConnectionManager.GetConnectionHealth(_connection);
        Console.WriteLine($"🏥 连接健康状况: {health.IsHealthy}");
        Console.WriteLine($"📊 连接状态: {health.State}");
        
        await AdvancedConnectionManager.EnsureConnectionOpenAsync(_connection);
        Console.WriteLine("✅ 连接确保完成");
        Console.WriteLine();
    }
    
    private List<User> GenerateSampleUsers(int count)
    {
        var random = new Random(42);
        var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance" };
        
        return Enumerable.Range(1, count)
            .Select(i => new User
            {
                Name = $"User{i:D3}",
                Email = $"user{i:D3}@company.com",
                Age = 22 + random.Next(38),
                Department = departments[random.Next(departments.Length)],
                Salary = 50000m + random.Next(100000),
                IsActive = random.NextDouble() > 0.1
            })
            .ToList();
    }
}

/// <summary>
/// 程序入口点
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🌟 欢迎使用 Sqlx 演示");
        Console.WriteLine();
        
        try
        {
            var demo = new ComprehensiveDemoApp();
            await demo.RunDemoAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 演示过程中发生错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}
