// -----------------------------------------------------------------------
// Sqlx Comprehensive Demo - 展示所有高级功能
// 这个演示展示了 Sqlx 的企业级功能和卓越性能
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;
using Sqlx.Core;

namespace Sqlx.ComprehensiveDemo;

/// <summary>
/// 用户实体 - 展示实体设计最佳实践
/// </summary>
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int Age { get; set; }
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 订单实体 - 展示关联数据处理
/// </summary>
[TableName("orders")]
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}

/// <summary>
/// 用户仓储接口 - 展示 Repository 模式设计
/// </summary>
public interface IUserRepository
{
    // 基础 CRUD 操作
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    // 高级查询操作
    Task<IList<User>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<IList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    // 批量操作
    Task<int> CreateBatchAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);
    Task<int> UpdateBatchAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);
    Task<int> DeleteBatchAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    
    // 统计操作
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetAverageSalaryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 订单仓储接口
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Order>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task<IList<Order>> GetRecentOrdersAsync(int days = 30, CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户仓储实现 - 使用 Sqlx 自动生成
/// </summary>
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    private readonly SQLiteConnection _connection;
    
    public UserRepository(SQLiteConnection connection)
    {
        _connection = connection;
    }
    
    // 拦截器实现 - 性能监控和日志记录
    partial void OnExecuting(string methodName, System.Data.Common.DbCommand command)
    {
        Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss.fff}] 执行 {methodName}");
        Console.WriteLine($"   SQL: {command.CommandText.Substring(0, Math.Min(80, command.CommandText.Length))}...");
    }
    
    partial void OnExecuted(string methodName, System.Data.Common.DbCommand command, object? result, long elapsed)
    {
        var elapsedMs = (double)elapsed / Stopwatch.Frequency * 1000;
        Console.WriteLine($"✅ [{DateTime.Now:HH:mm:ss.fff}] {methodName} 完成 - {elapsedMs:F2}ms");
        
        if (result is ICollection collection)
        {
            Console.WriteLine($"   返回: {collection.Count} 条记录");
        }
        else if (result is int count)
        {
            Console.WriteLine($"   影响: {count} 行");
        }
    }
    
    partial void OnExecuteFail(string methodName, System.Data.Common.DbCommand? command, Exception exception, long elapsed)
    {
        var elapsedMs = (double)elapsed / Stopwatch.Frequency * 1000;
        Console.WriteLine($"❌ [{DateTime.Now:HH:mm:ss.fff}] {methodName} 失败 - {elapsedMs:F2}ms");
        Console.WriteLine($"   错误: {exception.Message}");
    }
    
    // 自定义 SQL 查询示例
    [Sqlx("SELECT * FROM users WHERE Department = @department AND IsActive = 1")]
    public Task<IList<User>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        // 方法体将由 Sqlx 自动生成
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
    
    [Sqlx("SELECT * FROM users WHERE IsActive = 1 ORDER BY CreatedAt DESC")]
    public Task<IList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
    
    [Sqlx("SELECT * FROM users WHERE Email = @email LIMIT 1")]
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
    
    [Sqlx("SELECT COUNT(*) FROM users")]
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
    
    [Sqlx("SELECT AVG(Salary) FROM users WHERE IsActive = 1")]
    public Task<decimal> GetAverageSalaryAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
}

/// <summary>
/// 订单仓储实现
/// </summary>
[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository : IOrderRepository
{
    private readonly SQLiteConnection _connection;
    
    public OrderRepository(SQLiteConnection connection)
    {
        _connection = connection;
    }
    
    [Sqlx("SELECT * FROM orders WHERE UserId = @userId ORDER BY OrderDate DESC")]
    public Task<IList<Order>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
    
    [Sqlx("SELECT * FROM orders WHERE OrderDate >= date('now', '-' || @days || ' days') ORDER BY OrderDate DESC")]
    public Task<IList<Order>> GetRecentOrdersAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("This method will be implemented by Sqlx code generator");
    }
}

/// <summary>
/// 业务服务层 - 展示组合使用
/// </summary>
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    
    public UserService(IUserRepository userRepository, IOrderRepository orderRepository)
    {
        _userRepository = userRepository;
        _orderRepository = orderRepository;
    }
    
    /// <summary>
    /// 获取用户详细信息（包含订单）
    /// </summary>
    public async Task<(User? user, IList<Order> orders)> GetUserDetailsAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var orders = user != null ? await _orderRepository.GetByUserIdAsync(userId) : new List<Order>();
        
        return (user, orders);
    }
    
    /// <summary>
    /// 创建用户并生成示例订单
    /// </summary>
    public async Task<int> CreateUserWithOrderAsync(User user, Order order)
    {
        var userId = await _userRepository.CreateAsync(user);
        order.UserId = userId;
        await _orderRepository.CreateAsync(order);
        
        return userId;
    }
    
    /// <summary>
    /// 部门统计报告
    /// </summary>
    public async Task<DepartmentStats> GetDepartmentStatsAsync(string department)
    {
        var users = await _userRepository.GetByDepartmentAsync(department);
        var totalUsers = users.Count;
        var avgSalary = users.Any() ? users.Average(u => u.Salary) : 0;
        var avgAge = users.Any() ? users.Average(u => u.Age) : 0;
        
        return new DepartmentStats
        {
            Department = department,
            TotalUsers = totalUsers,
            AverageSalary = avgSalary,
            AverageAge = avgAge
        };
    }
}

/// <summary>
/// 部门统计数据
/// </summary>
public class DepartmentStats
{
    public string Department { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public decimal AverageSalary { get; set; }
    public double AverageAge { get; set; }
    
    public override string ToString() =>
        $"部门: {Department}, 人数: {TotalUsers}, 平均薪资: {AverageSalary:C}, 平均年龄: {AverageAge:F1}";
}

/// <summary>
/// 演示应用程序
/// </summary>
public class ComprehensiveDemoApp
{
    private readonly SQLiteConnection _connection;
    private readonly UserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    
    public ComprehensiveDemoApp()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        
        _userRepository = new UserRepository(_connection);
        _orderRepository = new OrderRepository(_connection);
        _userService = new UserService(_userRepository, _orderRepository);
        
        SetupDatabase().Wait();
    }
    
    private async Task SetupDatabase()
    {
        // 创建用户表
        using var userCmd = _connection.CreateCommand();
        userCmd.CommandText = @"
            CREATE TABLE users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT UNIQUE NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                Age INTEGER NOT NULL,
                Department TEXT NOT NULL,
                Salary DECIMAL(10,2) NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            )";
        await userCmd.ExecuteNonQueryAsync();
        
        // 创建订单表
        using var orderCmd = _connection.CreateCommand();
        orderCmd.CommandText = @"
            CREATE TABLE orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                Amount DECIMAL(10,2) NOT NULL,
                OrderDate TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Pending',
                FOREIGN KEY (UserId) REFERENCES users (Id)
            )";
        await orderCmd.ExecuteNonQueryAsync();
    }
    
    /// <summary>
    /// 运行完整演示
    /// </summary>
    public async Task RunDemoAsync()
    {
        Console.WriteLine("🚀 Sqlx 综合功能演示");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
        
        await DemoBasicOperations();
        await DemoBatchOperations();
        await DemoAdvancedQueries();
        await DemoCachePerformance();
        await DemoBusinessLogic();
        await DemoPerformanceMetrics();
        
        Console.WriteLine();
        Console.WriteLine("🎉 所有演示完成！Sqlx 展现了卓越的性能和功能！");
        Console.WriteLine($"📊 缓存统计: {IntelligentCacheManager.GetStatistics()}");
    }
    
    private async Task DemoBasicOperations()
    {
        Console.WriteLine("📋 1. 基础 CRUD 操作演示");
        Console.WriteLine("-".PadRight(50, '-'));
        
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
        
        // 查询用户
        var retrievedUser = await _userRepository.GetByIdAsync(userId);
        Console.WriteLine($"✅ 查询用户: {retrievedUser?.Name} ({retrievedUser?.Email})");
        
        // 更新用户
        if (retrievedUser != null)
        {
            retrievedUser.Age = 29;
            retrievedUser.Salary = 85000m;
            retrievedUser.UpdatedAt = DateTime.UtcNow;
            
            var updated = await _userRepository.UpdateAsync(retrievedUser);
            Console.WriteLine($"✅ 更新用户: {updated} 行受影响");
        }
        
        Console.WriteLine();
    }
    
    private async Task DemoBatchOperations()
    {
        Console.WriteLine("📦 2. 批量操作演示");
        Console.WriteLine("-".PadRight(50, '-'));
        
        // 生成批量数据
        var users = GenerateSampleUsers(1000);
        
        var sw = Stopwatch.StartNew();
        var affected = await _userRepository.CreateBatchAsync(users);
        sw.Stop();
        
        Console.WriteLine($"✅ 批量创建 {affected} 个用户，耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   平均每条记录: {(double)sw.ElapsedMilliseconds / affected:F2}ms");
        
        // 查询所有用户
        sw.Restart();
        var allUsers = await _userRepository.GetAllAsync();
        sw.Stop();
        
        Console.WriteLine($"✅ 查询 {allUsers.Count} 个用户，耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine();
    }
    
    private async Task DemoAdvancedQueries()
    {
        Console.WriteLine("🔍 3. 高级查询演示");
        Console.WriteLine("-".PadRight(50, '-'));
        
        // 按部门查询
        var engineeringUsers = await _userRepository.GetByDepartmentAsync("Engineering");
        Console.WriteLine($"✅ Engineering 部门用户: {engineeringUsers.Count} 人");
        
        // 查询活跃用户
        var activeUsers = await _userRepository.GetActiveUsersAsync();
        Console.WriteLine($"✅ 活跃用户总数: {activeUsers.Count} 人");
        
        // 统计查询
        var totalCount = await _userRepository.GetCountAsync();
        var avgSalary = await _userRepository.GetAverageSalaryAsync();
        
        Console.WriteLine($"✅ 用户总数: {totalCount}, 平均薪资: {avgSalary:C}");
        Console.WriteLine();
    }
    
    private async Task DemoCachePerformance()
    {
        Console.WriteLine("🧠 4. 缓存性能演示");
        Console.WriteLine("-".PadRight(50, '-'));
        
        // 第一次查询（缓存未命中）
        var sw = Stopwatch.StartNew();
        var users1 = await _userRepository.GetAllAsync();
        var firstQueryTime = sw.ElapsedMilliseconds;
        
        // 第二次查询（缓存命中）
        sw.Restart();
        var users2 = await _userRepository.GetAllAsync();
        var secondQueryTime = sw.ElapsedMilliseconds;
        
        var speedup = firstQueryTime > 0 ? (double)firstQueryTime / Math.Max(secondQueryTime, 1) : 1;
        
        Console.WriteLine($"✅ 首次查询: {firstQueryTime}ms ({users1.Count} 条记录)");
        Console.WriteLine($"✅ 缓存查询: {secondQueryTime}ms ({users2.Count} 条记录)");
        Console.WriteLine($"✅ 性能提升: {speedup:F1}x");
        Console.WriteLine();
    }
    
    private async Task DemoBusinessLogic()
    {
        Console.WriteLine("🏢 5. 业务逻辑演示");
        Console.WriteLine("-".PadRight(50, '-'));
        
        // 创建用户和订单
        var user = new User
        {
            Name = "李四",
            Email = "lisi@example.com",
            Age = 32,
            Department = "Sales",
            Salary = 75000m
        };
        
        var order = new Order
        {
            ProductName = "Laptop Pro",
            Amount = 12999.99m,
            Status = "Confirmed"
        };
        
        var userId = await _userService.CreateUserWithOrderAsync(user, order);
        Console.WriteLine($"✅ 创建用户和订单成功，用户ID: {userId}");
        
        // 获取用户详细信息
        var (userDetails, orders) = await _userService.GetUserDetailsAsync(userId);
        Console.WriteLine($"✅ 用户详情: {userDetails?.Name}, 订单数量: {orders.Count}");
        
        // 部门统计
        var stats = await _userService.GetDepartmentStatsAsync("Engineering");
        Console.WriteLine($"✅ 部门统计: {stats}");
        Console.WriteLine();
    }
    
    private async Task DemoPerformanceMetrics()
    {
        Console.WriteLine("📊 6. 性能指标总结");
        Console.WriteLine("-".PadRight(50, '-'));
        
        var cacheStats = IntelligentCacheManager.GetStatistics();
        var connectionHealth = AdvancedConnectionManager.GetConnectionHealth(_connection);
        
        Console.WriteLine($"🧠 缓存性能:");
        Console.WriteLine($"   命中次数: {cacheStats.HitCount:N0}");
        Console.WriteLine($"   未命中次数: {cacheStats.MissCount:N0}");
        Console.WriteLine($"   命中率: {cacheStats.HitRatio:P2}");
        Console.WriteLine($"   缓存条目: {cacheStats.EntryCount:N0}/{cacheStats.MaxSize:N0}");
        
        Console.WriteLine($"🔌 连接状态:");
        Console.WriteLine($"   状态: {connectionHealth.State}");
        Console.WriteLine($"   数据库: {connectionHealth.Database}");
        Console.WriteLine($"   健康状态: {(connectionHealth.IsHealthy ? "正常" : "异常")}");
        Console.WriteLine();
    }
    
    private List<User> GenerateSampleUsers(int count)
    {
        var random = new Random(42); // 固定种子确保可重现结果
        var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance" };
        var names = new[] { "张三", "李四", "王五", "赵六", "陈七", "黄八", "周九", "吴十" };
        
        return Enumerable.Range(1, count)
            .Select(i => new User
            {
                Name = $"{names[random.Next(names.Length)]}{i:D4}",
                Email = $"user{i:D4}@company.com",
                Age = 22 + random.Next(38), // 22-59岁
                Department = departments[random.Next(departments.Length)],
                Salary = 50000m + random.Next(100000), // 50k-150k
                IsActive = random.NextDouble() > 0.1 // 90%活跃用户
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
        Console.WriteLine("🌟 欢迎使用 Sqlx 综合功能演示");
        Console.WriteLine("这个演示将展示 Sqlx 的所有企业级功能和卓越性能");
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
