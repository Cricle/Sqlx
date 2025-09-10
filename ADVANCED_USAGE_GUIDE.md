# 🚀 Sqlx 高级使用指南

## 📖 目录
1. [快速开始](#快速开始)
2. [核心功能](#核心功能)
3. [企业级特性](#企业级特性)
4. [性能优化](#性能优化)
5. [最佳实践](#最佳实践)
6. [故障排除](#故障排除)

## 快速开始

### 基础Repository模式

```csharp
// 1. 定义实体
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

// 2. 定义服务接口
public interface IUserService
{
    IList<User> GetAllUsers();
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    User? GetUserById(int id);
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    int CreateUser(User user);
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    int UpdateUser(User user);
    int DeleteUser(int id);
}

// 3. 实现Repository (自动生成)
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;

    public UserRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 所有方法将自动实现！
}
```

### 使用示例

```csharp
// 创建连接
using var connection = new SqliteConnection("Data Source=mydb.db");
await connection.OpenAsync();

// 创建Repository
var userRepository = new UserRepository(connection);

// 使用Repository
var users = await userRepository.GetAllUsersAsync();
var user = await userRepository.GetUserByIdAsync(1);

var newUser = new User 
{ 
    Name = "John Doe", 
    Email = "john@example.com", 
    CreatedAt = DateTime.Now 
};

var affectedRows = await userRepository.CreateUserAsync(newUser);
```

## 核心功能

### 1. 🎯 自动SQL生成

```csharp
// 方法名称自动推断SQL
public interface IProductService
{
    // 自动生成: SELECT * FROM products
    IList<Product> GetAllProducts();
    
    // 自动生成: SELECT * FROM products WHERE Id = @id
    Product? GetProductById(int id);
    
    // 自动生成: INSERT INTO products (Name, Price) VALUES (@name, @price)
    int CreateProduct(Product product);
    
    // 自动生成: UPDATE products SET Name = @name, Price = @price WHERE Id = @id
    int UpdateProduct(Product product);
    
    // 自动生成: DELETE FROM products WHERE Id = @id
    int DeleteProduct(int id);
}
```

### 2. 🏷️ 属性驱动的SQL

```csharp
public interface IAdvancedUserService
{
    // 使用SqlExecuteType属性
    [SqlExecuteType(SqlExecuteTypes.Select, "users")]
    IList<User> GetAllUsers();
    
    // 使用RawSql属性
    [RawSql("SELECT * FROM users WHERE Email = @email")]
    User? GetUserByEmail(string email);
    
    // 复杂查询
    [RawSql(@"
        SELECT u.*, COUNT(o.Id) as OrderCount 
        FROM users u 
        LEFT JOIN orders o ON u.Id = o.UserId 
        WHERE u.IsActive = 1 
        GROUP BY u.Id
    ")]
    IList<UserWithOrderCount> GetActiveUsersWithOrderCount();
}
```

### 3. 🌐 多数据库支持

```csharp
// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class SqlServerRepository
{
    // 生成: SELECT [Name], [Email] FROM [users]
}

// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySqlRepository
{
    // 生成: SELECT `Name`, `Email` FROM `users`
}

// PostgreSQL
[SqlDefine(SqlDefineTypes.Postgresql)]
public partial class PostgreSqlRepository
{
    // 生成: SELECT "Name", "Email" FROM "users"
}
```

## 企业级特性

### 1. 💡 智能缓存系统

```csharp
// 基础缓存操作
IntelligentCacheManager.Set("user:123", user, TimeSpan.FromMinutes(30));
var cachedUser = IntelligentCacheManager.Get<User>("user:123");

// 缓存统计
var stats = IntelligentCacheManager.GetStatistics();
Console.WriteLine($"Cache Hit Rate: {stats.HitRate:P2}");
Console.WriteLine($"Memory Usage: {stats.TotalMemoryUsage:N0} bytes");

// 自动过期和LRU清理
IntelligentCacheManager.Set("temp_data", data, TimeSpan.FromSeconds(10));

// 高级缓存模式
var expensiveData = IntelligentCacheManager.GetOrAdd("expensive_key", () => {
    // 这里执行昂贵的计算或数据库查询
    return ComputeExpensiveData();
});
```

### 2. 🔌 高级连接管理

```csharp
// 连接健康检查
var health = AdvancedConnectionManager.GetConnectionHealth(connection);
Console.WriteLine($"Connection Health: {health.IsHealthy}");
Console.WriteLine($"Response Time: {health.ResponseTime.TotalMilliseconds}ms");

// 智能重试机制
var result = await AdvancedConnectionManager.ExecuteWithRetryAsync(async () =>
{
    // 可能失败的数据库操作
    return await connection.QueryAsync<User>("SELECT * FROM users");
}, maxAttempts: 3);

// 熔断器模式
var circuitBreaker = new CircuitBreaker();
var data = await AdvancedConnectionManager.ExecuteWithCircuitBreakerAsync(async () =>
{
    return await SomeUnreliableOperation();
}, circuitBreaker);
```

### 3. 📦 高性能批量操作

```csharp
// 批量插入
var users = GenerateUsers(10000);
var result = await BatchOperations.CreateBatchAsync(
    connection, 
    "users", 
    users, 
    batchSize: 1000
);

Console.WriteLine($"Inserted {result.AffectedRows} users in {result.ExecutionTime}");

// 批量更新
var updates = users.Select(u => new { u.Id, IsActive = false });
var updateResult = await BatchOperations.UpdateBatchAsync(
    connection, 
    "users", 
    updates, 
    keyColumn: "Id",
    batchSize: 500
);

// 批量删除
var idsToDelete = users.Select(u => u.Id);
var deleteResult = await BatchOperations.DeleteBatchAsync(
    connection, 
    "users", 
    idsToDelete, 
    keyColumn: "Id"
);
```

## 性能优化

### 1. 🚀 缓存策略

```csharp
public class OptimizedUserService
{
    private readonly IUserRepository _repository;
    private readonly string _cachePrefix = "user:";

    public async Task<User?> GetUserByIdAsync(int id)
    {
        var cacheKey = $"{_cachePrefix}{id}";
        
        // 先检查缓存
        var cachedUser = IntelligentCacheManager.Get<User>(cacheKey);
        if (cachedUser != null)
        {
            return cachedUser;
        }

        // 缓存未命中，查询数据库
        var user = await _repository.GetUserByIdAsync(id);
        if (user != null)
        {
            // 缓存结果，30分钟过期
            IntelligentCacheManager.Set(cacheKey, user, TimeSpan.FromMinutes(30));
        }

        return user;
    }
}
```

### 2. ⚡ 批量操作优化

```csharp
public class HighPerformanceDataService
{
    public async Task<BatchOperationResult> BulkImportUsersAsync(
        IEnumerable<User> users, 
        int batchSize = 1000)
    {
        // 自动优化批次大小
        return await BatchOperations.CreateBatchAsync(
            connection, 
            "users", 
            users,
            batchSize: batchSize,
            autoOptimizeBatchSize: true,
            continueOnError: false
        );
    }

    public async Task ProcessLargeDatasetAsync(IEnumerable<User> users)
    {
        // 分批处理大数据集
        var batches = users.Chunk(5000);
        
        foreach (var batch in batches)
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                await BatchOperations.CreateBatchAsync(
                    connection, 
                    "users", 
                    batch,
                    transaction: transaction
                );
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
```

### 3. 📊 性能监控

```csharp
public class MonitoredRepository : IUserService
{
    private readonly UserRepository _repository;

    // 实现拦截器
    partial void OnExecuting(string methodName, DbCommand command)
    {
        Console.WriteLine($"Executing {methodName}: {command.CommandText}");
    }

    partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
    {
        var ms = elapsed / 10000.0; // Convert ticks to milliseconds
        Console.WriteLine($"Completed {methodName} in {ms:F2}ms");
        
        // 性能警告
        if (ms > 1000)
        {
            Console.WriteLine($"⚠️  Slow query detected: {methodName} took {ms:F2}ms");
        }
    }

    partial void OnExecuteFail(string methodName, DbCommand command, Exception exception, long elapsed)
    {
        Console.WriteLine($"❌ Failed {methodName}: {exception.Message}");
    }
}
```

## 最佳实践

### 1. 🏗️ 项目结构

```
MyProject/
├── Data/
│   ├── Entities/
│   │   ├── User.cs
│   │   └── Product.cs
│   ├── Interfaces/
│   │   ├── IUserService.cs
│   │   └── IProductService.cs
│   └── Repositories/
│       ├── UserRepository.cs
│       └── ProductRepository.cs
├── Services/
│   └── BusinessLogicServices.cs
└── Program.cs
```

### 2. 🔧 依赖注入配置

```csharp
// Program.cs 或 Startup.cs
services.AddScoped<DbConnection>(provider =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    return new SqliteConnection(connectionString);
});

services.AddScoped<IUserService, UserRepository>();
services.AddScoped<IProductService, ProductRepository>();

// 注册企业级服务
services.AddSingleton<IntelligentCacheManager>();
services.AddSingleton<AdvancedConnectionManager>();
```

### 3. 🛡️ 错误处理

```csharp
public class RobustUserService
{
    private readonly IUserService _userService;
    private readonly ILogger<RobustUserService> _logger;

    public async Task<User?> GetUserSafelyAsync(int id)
    {
        try
        {
            return await AdvancedConnectionManager.ExecuteWithRetryAsync(
                async () => await _userService.GetUserByIdAsync(id),
                maxAttempts: 3
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", id);
            return null;
        }
    }
}
```

### 4. 🧪 测试策略

```csharp
[TestClass]
public class UserRepositoryTests
{
    private SqliteConnection _connection = null!;
    private UserRepository _repository = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        
        // 创建表结构
        await CreateTableAsync();
        
        _repository = new UserRepository(_connection);
    }

    [TestMethod]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _repository.GetUserByIdAsync(user.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(user.Name, result.Name);
    }
}
```

## 故障排除

### 常见问题

#### 1. "No connection" 错误
```csharp
// ❌ 错误
private readonly DbConnection connection;

// ✅ 正确
private readonly System.Data.Common.DbConnection connection;
```

#### 2. 类型转换错误
```csharp
// 确保实体属性类型与数据库列类型匹配
public class User
{
    public int Id { get; set; }           // INTEGER
    public string Name { get; set; }      // TEXT
    public DateTime CreatedAt { get; set; } // TEXT (ISO format)
    public bool IsActive { get; set; }    // INTEGER (0/1)
}
```

#### 3. 性能问题
```csharp
// 使用批量操作替代循环插入
// ❌ 慢
foreach (var user in users)
{
    await repository.CreateUserAsync(user);
}

// ✅ 快
await BatchOperations.CreateBatchAsync(connection, "users", users);
```

### 调试技巧

1. **启用SQL日志**:
   ```csharp
   partial void OnExecuting(string methodName, DbCommand command)
   {
       Debug.WriteLine($"SQL: {command.CommandText}");
   }
   ```

2. **监控缓存命中率**:
   ```csharp
   var stats = IntelligentCacheManager.GetStatistics();
   if (stats.HitRate < 0.8)
   {
       Console.WriteLine("⚠️ Low cache hit rate, consider adjusting cache strategy");
   }
   ```

3. **性能分析**:
   ```csharp
   var stopwatch = Stopwatch.StartNew();
   var result = await operation();
   stopwatch.Stop();
   
   if (stopwatch.ElapsedMilliseconds > 100)
   {
       Console.WriteLine($"Slow operation: {stopwatch.ElapsedMilliseconds}ms");
   }
   ```

---

## 🎯 总结

Sqlx提供了一个完整的企业级ORM解决方案，结合了：

- 🚀 **高性能**: 零反射，NativeAOT支持
- 🛡️ **类型安全**: 编译时验证
- 🔧 **易于使用**: 自动代码生成
- 📈 **企业级**: 缓存、连接管理、批量操作
- 🧪 **可测试**: 完整的测试支持

通过遵循本指南中的最佳实践，您可以构建高性能、可维护的数据访问层。




