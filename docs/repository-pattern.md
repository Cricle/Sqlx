# Repository 模式指南

Repository 模式是 Sqlx 的核心特性之一，它通过源代码生成自动实现您的数据访问接口，让您专注于业务逻辑而不是样板代码。

## 🎯 什么是 Repository 模式？

Repository 模式是一种设计模式，它封装了数据访问逻辑，提供了一个更面向对象的内存中域对象集合视图。在 Sqlx 中，您只需要：

1. **定义接口** - 描述您需要的数据操作
2. **标记类** - 使用 `[RepositoryFor]` 属性
3. **自动生成** - Sqlx 为您生成高性能实现

## 🏗️ 基础用法

### 定义服务接口

```csharp
public interface IUserService
{
    // 查询操作
    IList<User> GetAllUsers();
    User? GetUserById(int id);
    IList<User> GetUsersByStatus(bool isActive);
    
    // CRUD 操作
    int CreateUser(User user);
    int UpdateUser(User user);
    int DeleteUser(int id);
    
    // 异步操作
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
}
```

### 创建 Repository 实现

```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 所有方法将自动生成！
}
```

## 🎯 SQL 生成策略

Sqlx 使用智能的 SQL 生成策略，根据方法名称和参数自动推断 SQL 操作：

### 自动 SQL 推断

```csharp
public interface IProductService
{
    // 根据方法名自动生成 SQL
    IList<Product> GetAllProducts();        // SELECT * FROM products
    Product? GetProductById(int id);        // SELECT * FROM products WHERE Id = @id
    IList<Product> GetProductsByCategory(int categoryId); // SELECT * FROM products WHERE CategoryId = @categoryId
    
    int CreateProduct(Product product);     // INSERT INTO products (...)
    int UpdateProduct(Product product);     // UPDATE products SET ... WHERE Id = @id
    int DeleteProduct(int id);              // DELETE FROM products WHERE Id = @id
}
```

### 显式 SQL 指定

当自动推断不够时，可以显式指定 SQL：

```csharp
public interface IOrderService
{
    // 显式指定 SQL
    [Sqlx("SELECT * FROM orders WHERE CustomerId = @customerId ORDER BY OrderDate DESC")]
    IList<Order> GetOrdersByCustomer(int customerId);
    
    // 复杂查询
    [Sqlx(@"
        SELECT o.*, c.Name as CustomerName 
        FROM orders o 
        INNER JOIN customers c ON o.CustomerId = c.Id 
        WHERE o.OrderDate >= @startDate AND o.OrderDate <= @endDate")]
    IList<OrderWithCustomer> GetOrdersInDateRange(DateTime startDate, DateTime endDate);
    
    // 聚合查询
    [Sqlx("SELECT COUNT(*) FROM orders WHERE CustomerId = @customerId")]
    int GetOrderCountByCustomer(int customerId);
}
```

## 🔧 SqlExecuteType 属性

对于标准的 CRUD 操作，使用 `SqlExecuteType` 属性可以获得更好的控制：

```csharp
public interface IProductService
{
    [SqlExecuteType(SqlExecuteTypes.Select, "products")]
    IList<Product> GetAllProducts();
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "products")]
    int CreateProduct(Product product);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "products")]
    int UpdateProduct(Product product);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "products")]
    int DeleteProduct(int id);
}
```

### SqlExecuteTypes 详解

| 类型 | 说明 | 生成的 SQL 模式 |
|------|------|-----------------|
| `Select` | 查询操作 | `SELECT * FROM table` |
| `Insert` | 插入操作 | `INSERT INTO table (columns) VALUES (values)` |
| `Update` | 更新操作 | `UPDATE table SET columns WHERE Id = @id` |
| `Delete` | 删除操作 | `DELETE FROM table WHERE Id = @id` |

## 🌐 多数据库方言支持

Sqlx 支持多种数据库方言，通过 `SqlDefine` 属性配置：

```csharp
// MySQL Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL 方言
public partial class MySqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    public MySqlUserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL 使用反引号: SELECT * FROM `users` WHERE `Id` = @id
}

// PostgreSQL Repository  
[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)]  // PostgreSQL 方言
public partial class PgUserRepository : IUserService
{
    private readonly DbConnection connection;
    public PgUserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL 使用双引号: SELECT * FROM "users" WHERE "Id" = $id
}

// SQL Server Repository (默认)
[RepositoryFor(typeof(IUserService))]
public partial class SqlServerUserRepository : IUserService
{
    private readonly DbConnection connection;
    public SqlServerUserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL 使用方括号: SELECT * FROM [users] WHERE [Id] = @id
}
```

## 📝 TableName 属性

自定义表名映射：

```csharp
// 实体级别的表名
[TableName("user_accounts")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Repository 级别覆盖表名
[RepositoryFor(typeof(IUserService))]
[TableName("custom_users")]  // 覆盖实体的表名
public partial class CustomUserRepository : IUserService
{
    private readonly DbConnection connection;
    public CustomUserRepository(DbConnection connection) => this.connection = connection;
    
    // 使用 custom_users 表而不是 user_accounts
}
```

## ⚡ 异步支持

Sqlx 完全支持异步编程模式：

```csharp
public interface IAsyncUserService
{
    // 异步查询
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    
    // 异步 CRUD
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<int> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<int> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
    
    // 复杂异步查询
    [Sqlx("SELECT * FROM users WHERE CreatedAt >= @date")]
    Task<IList<User>> GetUsersCreatedAfterAsync(DateTime date, CancellationToken cancellationToken = default);
}
```

## 🔍 拦截器支持

Sqlx 提供了强大的拦截器机制，让您可以在 SQL 执行前后添加自定义逻辑：

```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    private readonly ILogger<UserRepository> logger;
    
    public UserRepository(DbConnection connection, ILogger<UserRepository> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }
    
    // 执行前拦截器
    partial void OnExecuting(string methodName, DbCommand command)
    {
        logger.LogInformation("执行 {Method}: {SQL}", methodName, command.CommandText);
        
        // 可以修改 command 的属性
        command.CommandTimeout = 30;
    }
    
    // 执行后拦截器
    partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
    {
        logger.LogInformation("完成 {Method}，耗时 {Elapsed}ms，结果类型: {ResultType}", 
            methodName, elapsed / 10000.0, result?.GetType().Name ?? "null");
    }
    
    // 执行失败拦截器
    partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)
    {
        logger.LogError(exception, "执行 {Method} 失败，耗时 {Elapsed}ms", 
            methodName, elapsed / 10000.0);
    }
}
```

## 🎭 ExpressionToSql 集成

Repository 可以与 ExpressionToSql 无缝集成，提供强大的动态查询能力：

```csharp
public interface IAdvancedUserService
{
    // 使用 ExpressionToSql 参数
    [Sqlx]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
    
    // 异步版本
    [Sqlx]
    Task<IList<User>> QueryUsersAsync([ExpressionToSql] ExpressionToSql<User> query, 
        CancellationToken cancellationToken = default);
}

// 使用示例
var activeUsers = userService.QueryUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.IsActive && u.CreatedAt > DateTime.Now.AddDays(-30))
        .OrderBy(u => u.Name)
        .Take(100)
);
```

## 🔧 事务支持

Repository 支持事务操作：

```csharp
public interface ITransactionalUserService
{
    // 接受事务参数的方法
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user, DbTransaction transaction);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    int UpdateUser(User user, DbTransaction transaction);
    
    // 批量操作
    [Sqlx("INSERT INTO user_logs (UserId, Action, Timestamp) VALUES (@userId, @action, @timestamp)")]
    int LogUserAction(int userId, string action, DateTime timestamp, DbTransaction transaction);
}

// 使用事务
using var transaction = connection.BeginTransaction();
try
{
    var userId = userService.CreateUser(newUser, transaction);
    userService.LogUserAction(userId, "Created", DateTime.Now, transaction);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## 📊 性能特点

Repository 模式生成的代码具有以下性能特点：

### ✅ 零反射
- 编译时生成所有代码
- 无运行时类型检查
- 完美支持 NativeAOT

### ✅ 内存优化
- 最小化对象分配
- 重用 StringBuilder
- 高效的字符串操作

### ✅ SQL 优化
- 参数化查询防止 SQL 注入
- 最优化的 SQL 生成
- 智能的类型映射

## 🚀 最佳实践

### 1. 接口设计原则

```csharp
// ✅ 好的接口设计
public interface IUserService
{
    // 清晰的方法名
    IList<User> GetActiveUsers();
    User? GetUserByEmail(string email);
    
    // 明确的参数类型
    int CreateUser(User user);
    bool UpdateUserStatus(int userId, bool isActive);
    
    // 异步方法带 CancellationToken
    Task<IList<User>> GetUsersAsync(CancellationToken cancellationToken = default);
}

// ❌ 避免的设计
public interface IBadUserService
{
    // 模糊的方法名
    List<object> Get();
    
    // 过多的参数
    User Find(string a, int b, bool c, DateTime d, string e);
    
    // 缺少异步支持
    List<User> GetExpensiveQuery();
}
```

### 2. Repository 组织

```csharp
// ✅ 按领域划分 Repository
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService { }

[RepositoryFor(typeof(IOrderService))]  
public partial class OrderRepository : IOrderService { }

[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService { }

// ✅ 使用基类共享通用逻辑
public abstract class BaseRepository
{
    protected readonly DbConnection connection;
    protected readonly ILogger logger;
    
    protected BaseRepository(DbConnection connection, ILogger logger)
    {
        this.connection = connection;
        this.logger = logger;
    }
}

[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : BaseRepository, IUserService
{
    public UserRepository(DbConnection connection, ILogger<UserRepository> logger) 
        : base(connection, logger) { }
}
```

### 3. 错误处理

```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)
    {
        // 记录详细的错误信息
        var sql = command?.CommandText ?? "Unknown SQL";
        var parameters = command?.Parameters.Cast<DbParameter>()
            .Select(p => $"{p.ParameterName}={p.Value}")
            .ToArray() ?? Array.Empty<string>();
            
        throw new DataAccessException(
            $"执行 {methodName} 失败: {exception.Message}\nSQL: {sql}\n参数: {string.Join(", ", parameters)}", 
            exception);
    }
}

public class DataAccessException : Exception
{
    public DataAccessException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

## 📚 延伸阅读

- [SqlDefine 和 TableName 属性详解](sqldefine-tablename.md)
- [ExpressionToSql 指南](expression-to-sql.md)
- [性能优化指南](OPTIMIZATION_GUIDE.md)
- [事务处理](transactions.md)
- [错误处理策略](error-handling.md)

---

Repository 模式是 Sqlx 的核心优势，它让您能够以最小的代码获得最大的性能。继续探索其他功能，打造完美的数据访问层！
