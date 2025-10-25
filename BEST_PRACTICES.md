# Sqlx 最佳实践指南 🎯

本指南提供了使用Sqlx的最佳实践和性能优化建议，帮助你构建高性能、可维护的数据访问层。

---

## 📋 目录

1. [代码组织](#代码组织)
2. [性能优化](#性能优化)
3. [类型安全](#类型安全)
4. [错误处理](#错误处理)
5. [数据库设计](#数据库设计)
6. [测试策略](#测试策略)
7. [生产环境](#生产环境)

---

## 🗂️ 代码组织

### ✅ 推荐：分层架构

```
MyApp/
├── Models/              # 实体类
│   ├── User.cs
│   ├── Product.cs
│   └── Order.cs
├── Data/                # 数据访问层
│   ├── IUserRepository.cs
│   ├── UserRepository.cs
│   ├── IProductRepository.cs
│   └── ProductRepository.cs
├── Services/            # 业务逻辑层
│   ├── UserService.cs
│   └── ProductService.cs
└── Controllers/         # API控制器
    ├── UsersController.cs
    └── ProductsController.cs
```

### ✅ 推荐：使用ICrudRepository

对于标准CRUD操作，优先使用通用接口：

```csharp
// ✅ 好 - 零样板代码，8个方法自动生成
[RepositoryFor<User>]
[TableName("users")]
public partial class UserRepository : ICrudRepository<User, long>
{
    public UserRepository(DbConnection connection) { }
}
```

```csharp
// ❌ 避免 - 手写重复的CRUD代码
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<List<User>> GetAllAsync(int limit, int offset);
    Task InsertAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(long id);
    // ... 重复的样板代码
}
```

### ✅ 推荐：接口 + 扩展方法

混合使用通用接口和自定义查询：

```csharp
// 定义扩展接口
public interface IUserRepository : ICrudRepository<User, long>
{
    // 添加业务特定查询
    [SqlTemplate("SELECT {{columns}} FROM users WHERE age > @minAge")]
    Task<List<User>> GetAdultUsersAsync(int minAge = 18);
    
    [SqlTemplate("SELECT {{columns}} FROM users WHERE {{where @predicate}}")]
    [ExpressionToSql(typeof(User))]
    Task<List<User>> FindAsync(Expression<Func<User, bool>> predicate);
}

// 实现
[RepositoryFor<IUserRepository>]
[TableName("users")]
public partial class UserRepository : IUserRepository
{
    public UserRepository(DbConnection connection) { }
}
```

---

## ⚡ 性能优化

### 1️⃣ 批量操作 - 性能提升47%

```csharp
// ✅ 好 - 批量插入（快47%，内存-50%）
[SqlTemplate("INSERT INTO users (name, email, age) VALUES {{values @users}}")]
[BatchOperation(MaxBatchSize = 100)]
Task<int> BatchInsertAsync(IEnumerable<User> users);

await userRepo.BatchInsertAsync(users);  // 一次数据库往返
```

```csharp
// ❌ 避免 - 循环插入（慢47%，内存+100%）
foreach (var user in users)
{
    await userRepo.InsertAsync(user);  // N次数据库往返
}
```

**性能数据：**
- 批量插入10行：Sqlx 92.23μs vs Dapper 174.85μs ⚡ **快47%**
- 批量插入100行：内存使用减少50%（126 KB vs 252 KB）💚

### 2️⃣ 使用`{{columns}}`占位符

```csharp
// ✅ 好 - 明确列名，利用索引优化
[SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
// 生成: SELECT id, name, email, age FROM users WHERE id = @id
```

```csharp
// ❌ 避免 - SELECT * 性能差
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

**好处：**
- ✅ 只读取需要的列，减少网络传输
- ✅ 更好的索引覆盖
- ✅ 编译时验证列名

### 3️⃣ 分页查询 - 使用LIMIT/OFFSET

```csharp
// ✅ 好 - 服务器端分页
[SqlTemplate("SELECT {{columns}} FROM users ORDER BY created_at DESC LIMIT @limit OFFSET @offset")]
Task<List<User>> GetUsersPageAsync(int limit, int offset);

var page1 = await repo.GetUsersPageAsync(20, 0);   // 第1页
var page2 = await repo.GetUsersPageAsync(20, 20);  // 第2页
```

```csharp
// ❌ 避免 - 客户端分页（读取所有数据）
var allUsers = await repo.GetAllUsersAsync();
var page1 = allUsers.Skip(0).Take(20).ToList();
var page2 = allUsers.Skip(20).Take(20).ToList();
```

### 4️⃣ 预分配集合容量

Sqlx自动为`List<T>`预分配容量，你也可以手动优化：

```csharp
// ✅ Sqlx自动优化
// 生成的代码会预分配List容量，减少重新分配
```

### 5️⃣ 避免N+1查询

```csharp
// ❌ 避免 - N+1查询问题
var users = await userRepo.GetAllAsync(100, 0);
foreach (var user in users)
{
    var orders = await orderRepo.GetByUserIdAsync(user.Id);  // N次查询
}
```

```csharp
// ✅ 好 - 一次查询 + 内存分组
var users = await userRepo.GetAllAsync(100, 0);
var userIds = users.Select(u => u.Id).ToList();
var allOrders = await orderRepo.GetByUserIdsAsync(userIds);  // 1次查询
var ordersByUser = allOrders.GroupBy(o => o.UserId).ToDictionary(g => g.Key, g => g.ToList());
```

### 6️⃣ 连接池管理

```csharp
// ✅ 好 - 在ASP.NET Core中使用Scoped连接
builder.Services.AddScoped<DbConnection>(sp =>
{
    var connection = new SqliteConnection(connectionString);
    connection.Open();
    return connection;
});

builder.Services.AddScoped<UserRepository>();
```

```csharp
// ❌ 避免 - 每次创建新连接（性能差）
public async Task<User?> GetUserAsync(long id)
{
    using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();
    var repo = new UserRepository(connection);
    return await repo.GetByIdAsync(id);
}
```

---

## 🛡️ 类型安全

### 1️⃣ 使用Expression查询（强类型）

```csharp
// ✅ 好 - 编译时类型检查
[SqlTemplate("SELECT {{columns}} FROM users WHERE {{where @predicate}}")]
[ExpressionToSql(typeof(User))]
Task<List<User>> FindAsync(Expression<Func<User, bool>> predicate);

var users = await repo.FindAsync(u => u.Age > 18 && u.IsActive);
//                                    ↑ 编译时检查属性是否存在
```

```csharp
// ❌ 避免 - 字符串拼接（易出错，SQL注入风险）
[SqlTemplate("SELECT {{columns}} FROM users WHERE age > " + minAge)]  // 危险！
Task<List<User>> GetUsersAsync(int minAge);
```

### 2️⃣ 使用Nullable类型

```csharp
// ✅ 好 - 明确NULL语义
public class User
{
    public long Id { get; set; }               // NOT NULL
    public string Name { get; set; } = "";     // NOT NULL
    public string? MiddleName { get; set; }    // NULLABLE
    public DateTime? LastLogin { get; set; }   // NULLABLE
}
```

```csharp
// ❌ 避免 - 隐式NULL（容易NullReferenceException）
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }      // 可能是NULL，但类型不表达
    public string MiddleName { get; set; } // 可能是NULL，但类型不表达
}
```

### 3️⃣ 参数化查询

```csharp
// ✅ 好 - 参数化（防止SQL注入）
[SqlTemplate("SELECT {{columns}} FROM users WHERE name = @name")]
Task<List<User>> GetByNameAsync(string name);

await repo.GetByNameAsync("Robert'); DROP TABLE users; --");  // 安全
```

```csharp
// ❌ 绝对不要 - 字符串拼接（SQL注入风险）
[SqlTemplate("SELECT {{columns}} FROM users WHERE name = '" + name + "'")]  // 危险！
Task<List<User>> GetByNameAsync(string name);
```

---

## 🚨 错误处理

### 1️⃣ 处理NULL返回值

```csharp
// ✅ 好 - 明确处理NULL
var user = await userRepo.GetByIdAsync(id);
if (user == null)
{
    return NotFound($"User {id} not found");
}
return Ok(user);
```

```csharp
// ❌ 避免 - 假设永远不为NULL
var user = await userRepo.GetByIdAsync(id);
return Ok(user.Name);  // NullReferenceException!
```

### 2️⃣ 处理数据库异常

```csharp
// ✅ 好 - 捕获特定异常
try
{
    await userRepo.InsertAsync(user);
}
catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint
{
    return BadRequest($"Email {user.Email} already exists");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to insert user");
    return StatusCode(500, "Internal server error");
}
```

### 3️⃣ 事务处理

```csharp
// ✅ 好 - 正确使用事务
using var transaction = connection.BeginTransaction();
try
{
    await userRepo.InsertAsync(user);
    await orderRepo.InsertAsync(order);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## 🗄️ 数据库设计

### 1️⃣ 主键设计

```sql
-- ✅ 好 - 使用自增主键
CREATE TABLE users (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,  -- 或 SERIAL, AUTOINCREMENT
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

```sql
-- ⚠️ 谨慎 - GUID/UUID（性能稍差，但有分布式优势）
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE
);
```

### 2️⃣ 索引优化

```sql
-- ✅ 好 - 为常查询列添加索引
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_created_at ON users(created_at);
CREATE INDEX idx_orders_user_id ON orders(user_id);

-- 复合索引（按查询频率和选择性）
CREATE INDEX idx_users_active_age ON users(is_active, age);
```

### 3️⃣ 约束定义

```sql
-- ✅ 好 - 明确约束
CREATE TABLE users (
    id BIGINT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    age INT NOT NULL CHECK (age >= 0 AND age <= 150),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

---

## 🧪 测试策略

### 1️⃣ 单元测试

```csharp
[TestClass]
public class UserRepositoryTests
{
    private SqliteConnection _connection = null!;
    private UserRepository _repo = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // 使用内存数据库
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        
        // 创建表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                age INTEGER NOT NULL
            )";
        await cmd.ExecuteNonQueryAsync();
        
        _repo = new UserRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User { Name = "Test", Email = "test@example.com", Age = 25 };
        await _repo.InsertAsync(user);
        
        // Act
        var result = await _repo.GetByIdAsync(1);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Name);
        Assert.AreEqual("test@example.com", result.Email);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByIdAsync(999);
        
        // Assert
        Assert.IsNull(result);
    }
}
```

### 2️⃣ 集成测试

```csharp
[TestClass]
public class UserServiceIntegrationTests
{
    [TestMethod]
    public async Task CreateUser_ValidData_Success()
    {
        // 使用真实数据库连接
        using var connection = new SqliteConnection("Data Source=test.db");
        await connection.OpenAsync();
        
        var repo = new UserRepository(connection);
        var service = new UserService(repo);
        
        // Test
        var user = await service.CreateUserAsync("John", "john@example.com", 30);
        
        Assert.IsNotNull(user);
        Assert.IsTrue(user.Id > 0);
    }
}
```

---

## 🚀 生产环境

### 1️⃣ 日志记录

```csharp
public partial class UserRepository
{
    private readonly ILogger<UserRepository> _logger;

    partial void OnExecuting(string operationName, DbCommand command)
    {
        _logger.LogDebug("Executing {Operation}: {SQL}", operationName, command.CommandText);
    }

    partial void OnExecuted(string operationName, object? result, TimeSpan elapsed)
    {
        _logger.LogInformation("{Operation} completed in {Elapsed}ms", operationName, elapsed.TotalMilliseconds);
    }

    partial void OnExecuteFail(string operationName, Exception exception)
    {
        _logger.LogError(exception, "{Operation} failed", operationName);
    }
}
```

### 2️⃣ 监控和追踪

```csharp
// Sqlx自动生成Activity，集成OpenTelemetry
public partial class UserRepository
{
    // Activity自动包含:
    // - SQL命令文本
    // - 参数信息
    // - 执行时间
    // - 异常信息（如果有）
}
```

### 3️⃣ 连接字符串管理

```csharp
// ✅ 好 - 使用配置文件
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=mydb;User=sa;Password=***"
  }
}

// Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<DbConnection>(sp =>
{
    var connection = new SqlConnection(connectionString);
    connection.Open();
    return connection;
});
```

```csharp
// ❌ 避免 - 硬编码连接字符串
var connection = new SqlConnection("Server=localhost;User=sa;Password=MyPassword123");  // 危险！
```

### 4️⃣ 健康检查

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("Database", () =>
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    });

app.MapHealthChecks("/health");
```

---

## 📊 性能基准

根据实际Benchmark测试（.NET 9.0, SQLite）：

| 场景 | 推荐做法 | 性能 |
|------|---------|------|
| 单行查询 | `GetByIdAsync(id)` | 7.32μs (快5%) ✅ |
| 列表查询 | `GetAllAsync(10, 0)` | 17.13μs (慢8%) ✅ |
| 批量插入10行 | `BatchInsertAsync(users)` | 92.23μs (快47%!) ⚡⚡⚡ |
| 批量插入100行 | `BatchInsertAsync(users)` | 1,284μs, 内存-50% 💚💚 |

---

## ✅ 检查清单

在部署到生产环境前，检查以下项目：

- [ ] 所有查询都使用参数化（防止SQL注入）
- [ ] 使用`{{columns}}`而非`SELECT *`
- [ ] 批量操作使用`[BatchOperation]`
- [ ] 正确处理NULL返回值
- [ ] 数据库连接使用连接池
- [ ] 添加适当的索引
- [ ] 实现错误处理和日志记录
- [ ] 编写单元测试和集成测试
- [ ] 配置健康检查
- [ ] 使用配置文件管理连接字符串
- [ ] 启用Activity追踪（OpenTelemetry）
- [ ] 测试高并发场景
- [ ] 验证事务正确性

---

## 🎯 关键要点

1. **性能优先**：使用批量操作，避免N+1查询
2. **类型安全**：使用Expression查询和Nullable类型
3. **代码组织**：分层架构，接口驱动设计
4. **错误处理**：捕获特定异常，正确使用事务
5. **生产就绪**：日志、监控、健康检查、测试

遵循这些最佳实践，你的Sqlx应用将既高性能又易于维护！🚀

