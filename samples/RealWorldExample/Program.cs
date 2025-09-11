using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using Sqlx.Annotations;

namespace RealWorldExample;

/// <summary>
/// 真实世界的电商系统示例
/// 展示 Sqlx v2.0 的现代 C# 特性支持
/// </summary>
class Program
{
    private static ILogger<Program> _logger = null!;

    static void Main(string[] args)
    {
        // 设置日志
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<Program>();

        _logger.LogInformation("🚀 Sqlx v2.0 真实世界示例 - 现代电商系统");
        
        // 创建内存数据库
        using var connection = new SQLiteConnection("Data Source=:memory:");
        connection.Open();
        
        // 初始化数据库
        InitializeDatabase(connection);
        
        // 创建服务实例
        var userService = new UserRepository(connection, _logger);
        var productService = new ProductRepository(connection, _logger);
        var orderService = new OrderRepository(connection, _logger);
        
        try
        {
            // 演示各种现代 C# 特性
            DemoTraditionalClasses(userService);
            DemoRecordTypes(productService);
            DemoPrimaryConstructors(orderService);
            DemoComplexQueries(userService, productService, orderService);
            
            _logger.LogInformation("✅ 所有示例执行成功！");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 示例执行出错");
        }
    }

    /// <summary>
    /// 演示传统类的使用
    /// </summary>
    static async Task DemoTraditionalClassesAsync(IUserService userService)
    {
        _logger.LogInformation("\n📁 1. 传统类示例 (User)");
        
        // 创建用户
        var user = new User
        {
            Email = "john.doe@example.com",
            Username = "johndoe",
            FullName = "John Doe",
            IsActive = true
        };
        
        var userId = await userService.CreateUserAsync(user);
        _logger.LogInformation($"创建用户，ID: {userId}");
        
        // 查询用户
        var users = await userService.GetActiveUsersAsync();
        _logger.LogInformation($"活跃用户数: {users.Count}");
        
        foreach (var u in users)
        {
            _logger.LogInformation($"  - {u.FullName} ({u.Email}) - 注册于 {u.CreatedAt:yyyy-MM-dd}");
        }
    }

    /// <summary>
    /// 演示 Record 类型的使用
    /// </summary>
    static async Task DemoRecordTypesAsync(IProductService productService)
    {
        _logger.LogInformation("\n📦 2. Record 类型示例 (Product)");
        
        // 创建产品 - 展示 Record 的不可变性
        var products = new[]
        {
            new Product(0, "iPhone 15 Pro", 999.99m, "Electronics") { Description = "最新的苹果手机" },
            new Product(0, "MacBook Air", 1299.99m, "Electronics") { Description = "轻薄笔记本电脑" },
            new Product(0, "AirPods Pro", 249.99m, "Electronics") { Description = "无线降噪耳机" }
        };
        
        foreach (var product in products)
        {
            var productId = await productService.CreateProductAsync(product);
            _logger.LogInformation($"创建产品: {product.Name} (ID: {productId})");
        }
        
        // 查询产品
        var allProducts = await productService.GetProductsByCategoryAsync("Electronics");
        _logger.LogInformation($"电子产品数量: {allProducts.Count}");
        
        foreach (var p in allProducts)
        {
            _logger.LogInformation($"  - {p.Name}: ${p.Price} ({p.Category})");
            
            // 展示 Record 的 with 表达式
            var discountedProduct = p with { Price = p.Price * 0.9m };
            _logger.LogInformation($"    折扣价: ${discountedProduct.Price:F2}");
        }
    }

    /// <summary>
    /// 演示主构造函数的使用
    /// </summary>
    static async Task DemoPrimaryConstructorsAsync(IOrderService orderService)
    {
        _logger.LogInformation("\n🛒 3. 主构造函数示例 (Order)");
        
        // 创建订单 - 展示主构造函数的优雅语法
        var order = new Order(0, "customer123", "John Doe");
        order.AddItem(new OrderItem("iPhone 15 Pro", 999.99m, 1));
        order.AddItem(new OrderItem("AirPods Pro", 249.99m, 1));
        
        _logger.LogInformation($"订单总金额: ${order.CalculateTotal():F2}");
        
        var orderId = await orderService.CreateOrderAsync(order);
        _logger.LogInformation($"创建订单，ID: {orderId}");
        
        // 查询订单
        var recentOrders = await orderService.GetRecentOrdersAsync(7);
        _logger.LogInformation($"最近7天订单数: {recentOrders.Count}");
        
        foreach (var o in recentOrders)
        {
            _logger.LogInformation($"  - 订单 {o.OrderId}: {o.CustomerName} - ${o.TotalAmount:F2} ({o.Status})");
        }
    }

    /// <summary>
    /// 演示复杂查询和混合实体类型
    /// </summary>
    static async Task DemoComplexQueriesAsync(IUserService userService, IProductService productService, IOrderService orderService)
    {
        _logger.LogInformation("\n🔍 4. 复杂查询示例");
        
        // 统计信息
        var stats = await orderService.GetOrderStatisticsAsync();
        _logger.LogInformation($"订单统计: 总数 {stats.TotalOrders}, 总金额 ${stats.TotalRevenue:F2}");
        
        // 热门产品
        var topProducts = await productService.GetTopProductsAsync(3);
        _logger.LogInformation("热门产品:");
        foreach (var product in topProducts)
        {
            _logger.LogInformation($"  - {product.Name}: ${product.Price}");
        }
        
        // 用户订单历史
        var userOrders = await orderService.GetUserOrderHistoryAsync("customer123");
        _logger.LogInformation($"用户 customer123 的订单历史: {userOrders.Count} 个订单");
    }

    /// <summary>
    /// 初始化数据库结构
    /// </summary>
    static async Task InitializeDatabaseAsync(DbConnection connection)
    {
        var sql = @"
            -- 用户表 (传统类)
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL UNIQUE,
                Username TEXT NOT NULL,
                FullName TEXT NOT NULL,
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- 产品表 (Record 类型)
            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price DECIMAL(10,2) NOT NULL,
                Category TEXT NOT NULL,
                Description TEXT,
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- 订单表 (主构造函数)
            CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId TEXT NOT NULL,
                CustomerId TEXT NOT NULL,
                CustomerName TEXT NOT NULL,
                TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
                Status TEXT NOT NULL DEFAULT 'Pending',
                OrderDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- 订单项表
            CREATE TABLE OrderItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                Price DECIMAL(10,2) NOT NULL,
                Quantity INTEGER NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders (Id)
            );

            -- 插入示例数据
            INSERT INTO Users (Email, Username, FullName) VALUES 
                ('admin@example.com', 'admin', 'System Admin'),
                ('user@example.com', 'user', 'Test User');

            INSERT INTO Products (Name, Price, Category, Description) VALUES 
                ('Sample Product', 99.99, 'Electronics', 'A sample product for testing');
        ";

        var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}

#region 实体定义

/// <summary>
/// 传统类 - 用户实体
/// 展示传统的属性定义方式，完全向后兼容
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Record 类型 - 产品实体
/// 展示不可变数据的最佳实践
/// </summary>
public record Product(int Id, string Name, decimal Price, string Category)
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 主构造函数类 - 订单实体
/// 展示现代 C# 12+ 语法的优雅性
/// </summary>
public class Order(int id, string customerId, string customerName)
{
    public int Id { get; set; } = id;
    public string OrderId { get; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public string CustomerId { get; } = customerId;
    public string CustomerName { get; } = customerName;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    // 业务逻辑
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    
    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        TotalAmount = CalculateTotal();
    }
    
    public decimal CalculateTotal() => _items.Sum(item => item.Price * item.Quantity);
}

/// <summary>
/// 订单项 - 简单的数据类
/// </summary>
public record OrderItem(string ProductName, decimal Price, int Quantity);

/// <summary>
/// 订单状态枚举
/// </summary>
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// 订单统计 DTO
/// </summary>
public record OrderStatistics(int TotalOrders, decimal TotalRevenue, DateTime LastOrderDate);

#endregion

#region 服务接口定义

public interface IUserService
{
    Task<int> CreateUserAsync(User user);
    Task<IList<User>> GetActiveUsersAsync();
    Task<User?> GetUserByEmailAsync(string email);
}

public interface IProductService
{
    Task<int> CreateProductAsync(Product product);
    Task<IList<Product>> GetProductsByCategoryAsync(string category);
    Task<IList<Product>> GetTopProductsAsync(int count);
}

public interface IOrderService
{
    Task<int> CreateOrderAsync(Order order);
    Task<IList<Order>> GetRecentOrdersAsync(int days);
    Task<IList<Order>> GetUserOrderHistoryAsync(string customerId);
    Task<OrderStatistics> GetOrderStatisticsAsync();
}

#endregion

#region Repository 实现

/// <summary>
/// 用户 Repository - 传统类支持
/// </summary>
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection _connection;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(DbConnection connection, ILogger<UserRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    [Sqlx("INSERT INTO Users (Email, Username, FullName, IsActive) VALUES (@Email, @Username, @FullName, @IsActive); SELECT last_insert_rowid();")]
    public partial Task<int> CreateUserAsync(User user);

    [Sqlx("SELECT * FROM Users WHERE IsActive = 1 ORDER BY CreatedAt DESC")]
    public partial Task<IList<User>> GetActiveUsersAsync();

    [Sqlx("SELECT * FROM Users WHERE Email = @email LIMIT 1")]
    public partial Task<User?> GetUserByEmailAsync(string email);

    // 性能监控拦截器
    partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
    {
        _logger.LogInformation("执行 {Method}: {ElapsedMs}ms", methodName, elapsed);
        if (elapsed > 1000)
        {
            _logger.LogWarning("慢查询警告 {Method}: {ElapsedMs}ms - {Sql}", 
                methodName, elapsed, command.CommandText);
        }
    }

    partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)
    {
        _logger.LogError(exception, "查询失败 {Method} after {ElapsedMs}ms", methodName, elapsed);
    }
}

/// <summary>
/// 产品 Repository - Record 类型支持
/// </summary>
[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection _connection;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(DbConnection connection, ILogger<ProductRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    [Sqlx("INSERT INTO Products (Name, Price, Category, Description) VALUES (@Name, @Price, @Category, @Description); SELECT last_insert_rowid();")]
    public partial Task<int> CreateProductAsync(Product product);

    [Sqlx("SELECT * FROM Products WHERE Category = @category AND IsActive = 1 ORDER BY Name")]
    public partial Task<IList<Product>> GetProductsByCategoryAsync(string category);

    [Sqlx("SELECT * FROM Products WHERE IsActive = 1 ORDER BY Price DESC LIMIT @count")]
    public partial Task<IList<Product>> GetTopProductsAsync(int count);
}

/// <summary>
/// 订单 Repository - 主构造函数支持
/// </summary>
[RepositoryFor(typeof(IOrderService))]
public partial class OrderRepository : IOrderService
{
    private readonly DbConnection _connection;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(DbConnection connection, ILogger<OrderRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    [Sqlx("INSERT INTO Orders (OrderId, CustomerId, CustomerName, TotalAmount, Status) VALUES (@OrderId, @CustomerId, @CustomerName, @TotalAmount, @Status); SELECT last_insert_rowid();")]
    public partial Task<int> CreateOrderAsync(Order order);

    [Sqlx("SELECT * FROM Orders WHERE OrderDate >= date('now', '-' || @days || ' days') ORDER BY OrderDate DESC")]
    public partial Task<IList<Order>> GetRecentOrdersAsync(int days);

    [Sqlx("SELECT * FROM Orders WHERE CustomerId = @customerId ORDER BY OrderDate DESC")]
    public partial Task<IList<Order>> GetUserOrderHistoryAsync(string customerId);

    [Sqlx("SELECT COUNT(*) as TotalOrders, COALESCE(SUM(TotalAmount), 0) as TotalRevenue, MAX(OrderDate) as LastOrderDate FROM Orders")]
    public partial Task<OrderStatistics> GetOrderStatisticsAsync();
}

#endregion
