# 基础示例

本文档提供了 Sqlx 的基础用法示例，帮助您快速上手并掌握核心功能。

## 🎯 目录

- [简单的 CRUD 操作](#简单的-crud-操作)
- [Repository 模式基础](#repository-模式基础)
- [异步操作](#异步操作)
- [多数据库方言](#多数据库方言)
- [自定义表名](#自定义表名)
- [参数化查询](#参数化查询)
- [拦截器使用](#拦截器使用)

## 🚀 简单的 CRUD 操作

### 定义实体类

```csharp
using Sqlx.Annotations;

[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 定义服务接口

```csharp
using Sqlx.Annotations;

public interface IUserService
{
    // 查询操作
    [SqlExecuteType(SqlExecuteTypes.Select, "users")]
    IList<User> GetAllUsers();
    
    [Sqlx("SELECT * FROM users WHERE Id = @id")]
    User? GetUserById(int id);
    
    [Sqlx("SELECT * FROM users WHERE IsActive = @isActive")]
    IList<User> GetUsersByStatus(bool isActive);
    
    // CRUD 操作
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    int UpdateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "users")]
    int DeleteUser(int id);
    
    // 聚合查询
    [Sqlx("SELECT COUNT(*) FROM users WHERE IsActive = 1")]
    int GetActiveUserCount();
}
```

### 创建 Repository

```csharp
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 所有接口方法将自动生成实现
}
```

### 使用 Repository

```csharp
using Microsoft.Data.Sqlite;

class Program
{
    static void Main(string[] args)
    {
        // 创建数据库连接
        using var connection = new SqliteConnection("Data Source=users.db");
        
        // 创建 Repository
        var userRepo = new UserRepository(connection);
        
        // 创建用户
        var newUser = new User
        {
            Name = "张三",
            Email = "zhangsan@example.com",
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        
        int userId = userRepo.CreateUser(newUser);
        Console.WriteLine($"创建用户，ID: {userId}");
        
        // 查询所有用户
        var allUsers = userRepo.GetAllUsers();
        Console.WriteLine($"总用户数: {allUsers.Count}");
        
        // 查询单个用户
        var user = userRepo.GetUserById(1);
        if (user != null)
        {
            Console.WriteLine($"用户: {user.Name} - {user.Email}");
        }
        
        // 查询活跃用户
        var activeUsers = userRepo.GetUsersByStatus(true);
        Console.WriteLine($"活跃用户数: {activeUsers.Count}");
        
        // 更新用户
        if (user != null)
        {
            user.Email = "newemail@example.com";
            int rowsAffected = userRepo.UpdateUser(user);
            Console.WriteLine($"更新了 {rowsAffected} 行");
        }
        
        // 获取活跃用户数量
        int activeCount = userRepo.GetActiveUserCount();
        Console.WriteLine($"活跃用户总数: {activeCount}");
    }
}
```

## 🏗️ Repository 模式基础

### 产品管理示例

```csharp
// 产品实体
[TableName("products")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 产品服务接口
public interface IProductService
{
    // 基础 CRUD
    IList<Product> GetAllProducts();
    Product? GetProductById(int id);
    IList<Product> GetProductsByCategory(int categoryId);
    IList<Product> GetProductsInStock();
    
    int CreateProduct(Product product);
    int UpdateProduct(Product product);
    int DeleteProduct(int id);
    
    // 业务查询
    [Sqlx("SELECT * FROM products WHERE Price BETWEEN @minPrice AND @maxPrice")]
    IList<Product> GetProductsByPriceRange(decimal minPrice, decimal maxPrice);
    
    [Sqlx("SELECT * FROM products WHERE Stock < @threshold ORDER BY Stock ASC")]
    IList<Product> GetLowStockProducts(int threshold);
    
    [Sqlx("UPDATE products SET Stock = Stock - @quantity WHERE Id = @productId")]
    int ReduceStock(int productId, int quantity);
}

// Repository 实现
[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection connection;
    
    public ProductRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}

// 使用示例
static void ProductExample()
{
    using var connection = new SqliteConnection("Data Source=shop.db");
    var productRepo = new ProductRepository(connection);
    
    // 创建产品
    var product = new Product
    {
        Name = "智能手机",
        Description = "最新款智能手机",
        Price = 2999.99m,
        CategoryId = 1,
        Stock = 50,
        CreatedAt = DateTime.Now
    };
    
    int productId = productRepo.CreateProduct(product);
    Console.WriteLine($"创建产品，ID: {productId}");
    
    // 查询价格范围内的产品
    var affordableProducts = productRepo.GetProductsByPriceRange(1000, 3000);
    Console.WriteLine($"价格在 1000-3000 的产品: {affordableProducts.Count} 个");
    
    // 查询库存不足的产品
    var lowStockProducts = productRepo.GetLowStockProducts(10);
    Console.WriteLine($"库存不足的产品: {lowStockProducts.Count} 个");
    
    // 减少库存
    int stockReduced = productRepo.ReduceStock(productId, 5);
    Console.WriteLine($"减少库存，影响行数: {stockReduced}");
}
```

## ⚡ 异步操作

```csharp
// 异步服务接口
public interface IAsyncUserService
{
    // 异步查询
    [Sqlx("SELECT * FROM users")]
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    [Sqlx("SELECT * FROM users WHERE Id = @id")]
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    
    // 异步 CRUD
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    Task<int> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "users")]
    Task<int> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
    
    // 复杂异步查询
    [Sqlx(@"
        SELECT u.*, COUNT(o.Id) as OrderCount 
        FROM users u 
        LEFT JOIN orders o ON u.Id = o.UserId 
        WHERE u.CreatedAt >= @startDate 
        GROUP BY u.Id 
        ORDER BY OrderCount DESC")]
    Task<IList<UserWithOrderCount>> GetUsersWithOrderCountAsync(DateTime startDate, CancellationToken cancellationToken = default);
}

// 用户订单统计实体
public class UserWithOrderCount : User
{
    public int OrderCount { get; set; }
}

// 异步 Repository
[RepositoryFor(typeof(IAsyncUserService))]
public partial class AsyncUserRepository : IAsyncUserService
{
    private readonly DbConnection connection;
    
    public AsyncUserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}

// 异步使用示例
static async Task AsyncExample()
{
    using var connection = new SqliteConnection("Data Source=users.db");
    var userRepo = new AsyncUserRepository(connection);
    
    // 创建取消令牌
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    
    try
    {
        // 异步查询所有用户
        var users = await userRepo.GetAllUsersAsync(cts.Token);
        Console.WriteLine($"查询到 {users.Count} 个用户");
        
        // 异步创建用户
        var newUser = new User
        {
            Name = "李四",
            Email = "lisi@example.com",
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        
        int userId = await userRepo.CreateUserAsync(newUser, cts.Token);
        Console.WriteLine($"异步创建用户，ID: {userId}");
        
        // 异步查询单个用户
        var user = await userRepo.GetUserByIdAsync(userId, cts.Token);
        if (user != null)
        {
            Console.WriteLine($"异步查询用户: {user.Name}");
        }
        
        // 复杂异步查询
        var usersWithOrderCount = await userRepo.GetUsersWithOrderCountAsync(
            DateTime.Now.AddDays(-30), cts.Token);
        Console.WriteLine($"最近30天有订单的用户: {usersWithOrderCount.Count} 个");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("操作被取消");
    }
}
```

## 🌐 多数据库方言

```csharp
// 共同的服务接口
public interface IUserService
{
    IList<User> GetAllUsers();
    User? GetUserById(int id);
    int CreateUser(User user);
}

// MySQL Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL 方言
public partial class MySqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public MySqlUserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 生成的 SQL 使用反引号:
    // SELECT * FROM `users`
    // SELECT * FROM `users` WHERE `Id` = @id
    // INSERT INTO `users` (`Name`, `Email`) VALUES (@Name, @Email)
}

// PostgreSQL Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)]  // PostgreSQL 方言
public partial class PostgreSqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public PostgreSqlUserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 生成的 SQL 使用双引号和 $ 参数:
    // SELECT * FROM "users"
    // SELECT * FROM "users" WHERE "Id" = $id
    // INSERT INTO "users" ("Name", "Email") VALUES ($Name, $Email)
}

// SQL Server Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(1)]  // SQL Server 方言 (或省略，默认为 SQL Server)
public partial class SqlServerUserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public SqlServerUserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 生成的 SQL 使用方括号:
    // SELECT * FROM [users]
    // SELECT * FROM [users] WHERE [Id] = @id
    // INSERT INTO [users] ([Name], [Email]) VALUES (@Name, @Email)
}

// 使用示例
static void MultiDatabaseExample()
{
    // MySQL 使用
    using var mysqlConnection = new MySqlConnection("Server=localhost;Database=test;Uid=root;Pwd=password;");
    var mysqlRepo = new MySqlUserRepository(mysqlConnection);
    var mysqlUsers = mysqlRepo.GetAllUsers();
    
    // PostgreSQL 使用
    using var pgConnection = new NpgsqlConnection("Host=localhost;Database=test;Username=postgres;Password=password");
    var pgRepo = new PostgreSqlUserRepository(pgConnection);
    var pgUsers = pgRepo.GetAllUsers();
    
    // SQL Server 使用
    using var sqlConnection = new SqlConnection("Server=localhost;Database=test;Integrated Security=true");
    var sqlRepo = new SqlServerUserRepository(sqlConnection);
    var sqlUsers = sqlRepo.GetAllUsers();
}
```

## 📝 自定义表名

```csharp
// 实体定义表名
[TableName("user_accounts")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 服务接口定义表名
[TableName("service_users")]
public interface IUserService
{
    IList<User> GetAllUsers();
    User? GetUserById(int id);
}

// Repository 级别覆盖表名
[RepositoryFor(typeof(IUserService))]
[TableName("custom_users")]  // 最高优先级，覆盖其他表名
public partial class CustomUserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public CustomUserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 最终使用 custom_users 表
}

// 多租户示例
[RepositoryFor(typeof(IUserService))]
[TableName("tenant_a_users")]
public partial class TenantAUserRepository : IUserService
{
    private readonly DbConnection connection;
    public TenantAUserRepository(DbConnection connection) => this.connection = connection;
}

[RepositoryFor(typeof(IUserService))]
[TableName("tenant_b_users")]
public partial class TenantBUserRepository : IUserService
{
    private readonly DbConnection connection;
    public TenantBUserRepository(DbConnection connection) => this.connection = connection;
}

// 使用示例
static void CustomTableNameExample()
{
    using var connection = new SqliteConnection("Data Source=multi_tenant.db");
    
    // 租户 A 的用户操作
    var tenantARepo = new TenantAUserRepository(connection);
    var tenantAUsers = tenantARepo.GetAllUsers();  // 查询 tenant_a_users 表
    
    // 租户 B 的用户操作
    var tenantBRepo = new TenantBUserRepository(connection);
    var tenantBUsers = tenantBRepo.GetAllUsers();  // 查询 tenant_b_users 表
}
```

## 🔍 参数化查询

```csharp
public interface IAdvancedUserService
{
    // 单个参数
    [Sqlx("SELECT * FROM users WHERE Name = @name")]
    User? GetUserByName(string name);
    
    // 多个参数
    [Sqlx("SELECT * FROM users WHERE Age >= @minAge AND Age <= @maxAge")]
    IList<User> GetUsersByAgeRange(int minAge, int maxAge);
    
    // 复杂参数类型
    [Sqlx("SELECT * FROM users WHERE CreatedAt >= @startDate AND IsActive = @isActive")]
    IList<User> GetUsersCreatedAfter(DateTime startDate, bool isActive);
    
    // 可空参数
    [Sqlx(@"
        SELECT * FROM users 
        WHERE (@name IS NULL OR Name LIKE '%' + @name + '%')
        AND (@email IS NULL OR Email = @email)")]
    IList<User> SearchUsers(string? name, string? email);
    
    // 数组参数 (需要特殊处理)
    [Sqlx("SELECT * FROM users WHERE Id IN @ids")]
    IList<User> GetUsersByIds(int[] ids);
    
    // 输出参数
    [Sqlx("SELECT @totalCount = COUNT(*) FROM users WHERE IsActive = @isActive; SELECT * FROM users WHERE IsActive = @isActive")]
    IList<User> GetUsersWithCount(bool isActive, out int totalCount);
}

[RepositoryFor(typeof(IAdvancedUserService))]
public partial class AdvancedUserRepository : IAdvancedUserService
{
    private readonly DbConnection connection;
    
    public AdvancedUserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}

// 使用示例
static void ParameterizedQueryExample()
{
    using var connection = new SqliteConnection("Data Source=users.db");
    var userRepo = new AdvancedUserRepository(connection);
    
    // 按名称查询
    var userByName = userRepo.GetUserByName("张三");
    
    // 按年龄范围查询
    var usersByAge = userRepo.GetUsersByAgeRange(18, 65);
    
    // 按创建时间和状态查询
    var recentActiveUsers = userRepo.GetUsersCreatedAfter(
        DateTime.Now.AddDays(-30), true);
    
    // 搜索用户（可空参数）
    var searchResults = userRepo.SearchUsers("张", null);  // 只按名称搜索
    var searchResults2 = userRepo.SearchUsers(null, "test@example.com");  // 只按邮箱搜索
    var searchResults3 = userRepo.SearchUsers("李", "li@example.com");  // 同时按名称和邮箱搜索
    
    // 按 ID 列表查询
    var selectedUsers = userRepo.GetUsersByIds(new[] { 1, 2, 3, 4, 5 });
}
```

## 🔧 拦截器使用

```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepositoryWithInterceptors : IUserService
{
    private readonly DbConnection connection;
    private readonly ILogger<UserRepositoryWithInterceptors> logger;
    
    public UserRepositoryWithInterceptors(
        DbConnection connection, 
        ILogger<UserRepositoryWithInterceptors> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }
    
    // 执行前拦截器
    partial void OnExecuting(string methodName, DbCommand command)
    {
        logger.LogInformation("开始执行 {MethodName}", methodName);
        logger.LogDebug("SQL: {SQL}", command.CommandText);
        
        // 记录参数
        foreach (DbParameter parameter in command.Parameters)
        {
            logger.LogDebug("参数 {Name} = {Value}", 
                parameter.ParameterName, parameter.Value);
        }
        
        // 设置超时时间
        command.CommandTimeout = 30;
        
        // 记录开始时间
        command.Transaction?.GetType().GetProperty("StartTime")?.SetValue(
            command.Transaction, DateTime.UtcNow);
    }
    
    // 执行成功拦截器
    partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
    {
        var elapsedMs = elapsed / 10000.0; // 转换为毫秒
        
        logger.LogInformation("成功执行 {MethodName}，耗时 {ElapsedMs:F2}ms", 
            methodName, elapsedMs);
        
        // 记录结果信息
        if (result is IList list)
        {
            logger.LogInformation("返回 {Count} 条记录", list.Count);
        }
        else if (result is int affectedRows)
        {
            logger.LogInformation("影响 {AffectedRows} 行", affectedRows);
        }
        
        // 性能警告
        if (elapsedMs > 1000)
        {
            logger.LogWarning("慢查询警告: {MethodName} 耗时 {ElapsedMs:F2}ms", 
                methodName, elapsedMs);
        }
    }
    
    // 执行失败拦截器
    partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)
    {
        var elapsedMs = elapsed / 10000.0;
        
        logger.LogError(exception, 
            "执行 {MethodName} 失败，耗时 {ElapsedMs:F2}ms", methodName, elapsedMs);
        
        if (command != null)
        {
            logger.LogError("失败的 SQL: {SQL}", command.CommandText);
            
            foreach (DbParameter parameter in command.Parameters)
            {
                logger.LogError("参数 {Name} = {Value}", 
                    parameter.ParameterName, parameter.Value);
            }
        }
        
        // 根据异常类型进行特殊处理
        switch (exception)
        {
            case SqlException sqlEx when sqlEx.Number == 2: // Timeout
                logger.LogError("数据库超时，考虑优化查询或增加超时时间");
                break;
                
            case SqlException sqlEx when sqlEx.Number == 18456: // Login failed
                logger.LogError("数据库连接失败，检查连接字符串和权限");
                break;
                
            default:
                logger.LogError("未知数据库错误: {ErrorMessage}", exception.Message);
                break;
        }
    }
}

// 使用带拦截器的 Repository
static void InterceptorExample()
{
    // 配置日志
    using var loggerFactory = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    
    var logger = loggerFactory.CreateLogger<UserRepositoryWithInterceptors>();
    
    using var connection = new SqliteConnection("Data Source=users.db");
    var userRepo = new UserRepositoryWithInterceptors(connection, logger);
    
    try
    {
        // 这些操作将触发拦截器
        var users = userRepo.GetAllUsers();
        var user = userRepo.GetUserById(1);
        
        if (user != null)
        {
            user.Email = "updated@example.com";
            userRepo.UpdateUser(user);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"操作失败: {ex.Message}");
        // 失败信息已经通过 OnExecuteFail 拦截器记录
    }
}
```

## 📚 下一步

这些基础示例展示了 Sqlx 的核心功能。继续探索更高级的功能：

- [高级示例](advanced-examples.md) - 复杂场景和高级用法
- [ExpressionToSql 指南](../expression-to-sql.md) - 动态查询构建
- [性能优化指南](../OPTIMIZATION_GUIDE.md) - 性能调优技巧
- [最佳实践](best-practices.md) - 生产环境建议

---

通过这些基础示例，您应该已经掌握了 Sqlx 的核心用法。现在可以开始在您的项目中使用 Sqlx 构建高性能的数据访问层了！
