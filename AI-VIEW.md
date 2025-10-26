# Sqlx AI 助手使用指南

> **版本**: 0.4.0  
> **目标**: 帮助AI助手快速掌握Sqlx库的完整功能和使用方式  
> **最后更新**: 2025-10-26

---

## 📋 目录

1. [快速参考](#快速参考)
2. [核心概念](#核心概念)
3. [完整特性列表](#完整特性列表)
4. [代码模式](#代码模式)
5. [重要注意事项](#重要注意事项)
6. [常见错误](#常见错误)
7. [性能优化](#性能优化)
8. [故障排查](#故障排查)

---

## 快速参考

### 最小示例（3步）

```csharp
// 1. 定义实体
public class User 
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 2. 定义仓储接口
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
}

// 3. 实现仓储
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

### 关键命名空间

```csharp
using Sqlx;                    // 核心类型
using Sqlx.Annotations;        // 特性标记
using System.Data.Common;      // DbConnection (必须)
using System.Linq.Expressions; // 表达式树
```

---

## 核心概念

### 1. 源代码生成器机制

**工作原理**：
```
用户编写接口 + [SqlTemplate]
    ↓
编译时 Roslyn 源生成器分析
    ↓
自动生成 partial 类实现
    ↓
编译器编译生成的代码
    ↓
零运行时开销，纯ADO.NET代码
```

**关键点**：
- ✅ 所有代码在**编译时**生成，无反射、无动态
- ✅ 生成的代码直接使用 `DbCommand`、`DbDataReader`
- ✅ 支持 AOT (Ahead-of-Time) 编译
- ✅ 性能接近手写 ADO.NET

### 2. 必需的特性标记

| 特性 | 位置 | 用途 | 必需性 |
|------|------|------|--------|
| `[SqlDefine]` | 接口/类 | 指定数据库方言 | ✅ 必需 |
| `[RepositoryFor]` | 类 | 标记实体类型 | ✅ 必需 |
| `[SqlTemplate]` | 方法 | 定义SQL模板 | ✅ 必需（对自定义方法） |
| `[TableName]` | 实体类 | 指定表名 | ⚠️ 可选 |

### 3. 占位符系统

Sqlx 的核心功能，提供跨数据库SQL模板：

| 占位符 | 生成内容 | 适用场景 |
|--------|----------|----------|
| `{{columns}}` | `id, name, age, balance` | SELECT 列列表 |
| `{{table}}` | `users` | 表名引用 |
| `{{values}}` | `(@id, @name, @age)` | INSERT VALUES |
| `{{where}}` | `WHERE age >= 18 AND ...` | 表达式树条件 |
| `{{limit}}` | `LIMIT @limit` / `TOP (@limit)` | 分页限制 |
| `{{offset}}` | `OFFSET @offset` / `OFFSET @offset ROWS` | 分页偏移 |
| `{{orderby column [--desc]}}` | `ORDER BY column [DESC]` | 排序 |
| `{{set}}` | `name=@name, age=@age` | UPDATE SET |
| `{{batch_values}}` | `(@v1, @v2), (@v3, @v4)...` | 批量插入 |

---

## 完整特性列表

### ✅ 已实现特性

#### 1. 基础数据访问

```csharp
// SELECT 单条记录（可空）
[SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// SELECT 多条记录
[SqlTemplate("SELECT {{columns}} FROM users WHERE age >= @minAge")]
Task<List<User>> GetAdultsAsync(int minAge);

// INSERT 返回影响行数
[SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
Task<int> InsertAsync(string name, int age);

// UPDATE
[SqlTemplate("UPDATE users SET name = @name WHERE id = @id")]
Task<int> UpdateAsync(long id, string name);

// DELETE
[SqlTemplate("DELETE FROM users WHERE id = @id")]
Task<int> DeleteAsync(long id);

// COUNT
[SqlTemplate("SELECT COUNT(*) FROM users")]
Task<long> CountAsync();

// EXISTS
[SqlTemplate("SELECT COUNT(1) FROM users WHERE id = @id")]
Task<bool> ExistsAsync(long id);
```

#### 2. 返回类型支持

| 返回类型 | 用途 | 示例 |
|---------|------|------|
| `Task<T?>` | 单个实体（可能为null） | `Task<User?>` |
| `Task<List<T>>` | 实体列表 | `Task<List<User>>` |
| `Task<int>` | 影响行数 | INSERT/UPDATE/DELETE |
| `Task<long>` | 计数/ID | COUNT/自增ID |
| `Task<bool>` | 布尔结果 | EXISTS |
| `Task<Dictionary<string, object?>>` | 动态单行 | 复杂查询 |
| `Task<List<Dictionary<string, object?>>>` | 动态多行 | 复杂查询 |

#### 3. 占位符详解

##### `{{columns}}` - 自动列选择

```csharp
public class User 
{
    public long Id { get; set; }        // → id
    public string Name { get; set; }    // → name
    public int Age { get; set; }        // → age
    public decimal Balance { get; set; }// → balance
}

// {{columns}} → id, name, age, balance
[SqlTemplate("SELECT {{columns}} FROM users")]
Task<List<User>> GetAllAsync();
```

**命名规则**：
- PascalCase → snake_case
- `Id` → `id`
- `UserName` → `user_name`
- `CreatedAt` → `created_at`

##### `{{table}}` - 表名

```csharp
// 使用 [TableName] 指定
[TableName("app_users")]
public class User { }

// {{table}} → app_users
[SqlTemplate("SELECT * FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 不指定则使用类名小写
public class Product { }  // → product
```

##### `{{where}}` - 表达式树条件

```csharp
[SqlTemplate("SELECT {{columns}} FROM users {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 使用
await repo.QueryAsync(u => u.Age >= 18 && u.Balance > 1000);
// 生成: SELECT id, name, age, balance FROM users WHERE age >= 18 AND balance > 1000
```

**支持的表达式**：
```csharp
// 比较运算符
u => u.Age == 18        // age = 18
u => u.Age != 18        // age != 18
u => u.Age > 18         // age > 18
u => u.Age >= 18        // age >= 18
u => u.Age < 18         // age < 18
u => u.Age <= 18        // age <= 18

// 逻辑运算符
u => u.Age >= 18 && u.Balance > 1000    // age >= 18 AND balance > 1000
u => u.Age < 18 || u.IsVip              // age < 18 OR is_vip = 1
u => !u.IsDeleted                        // is_deleted = 0

// NULL 检查
u => u.Email == null     // email IS NULL
u => u.Email != null     // email IS NOT NULL

// 字符串方法
u => u.Name.Contains("Alice")    // name LIKE '%Alice%'
u => u.Name.StartsWith("A")      // name LIKE 'A%'
u => u.Name.EndsWith("e")        // name LIKE '%e'
```

**不支持的表达式**：
```csharp
// ❌ 方法调用（除字符串方法）
u => u.Age.ToString()

// ❌ 复杂Lambda
u => Calculate(u.Age, u.Balance)

// ❌ 本地变量引用
var minAge = 18;
u => u.Age >= minAge  // ❌ 应该用参数代替

// ❌ 集合操作
u => u.Tags.Any(t => t == "VIP")
```

##### `{{limit}}` 和 `{{offset}}` - 跨数据库分页

```csharp
[SqlTemplate("SELECT {{columns}} FROM users {{orderby id}} {{limit}} {{offset}}")]
Task<List<User>> GetPagedAsync(int? limit = null, int? offset = null);

// SQLite/MySQL/PostgreSQL:
// SELECT id, name FROM users ORDER BY id LIMIT @limit OFFSET @offset

// SQL Server:
// SELECT id, name FROM users ORDER BY id 
// OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
```

**注意事项**：
- ✅ 参数类型必须是 `int?`（可选）
- ✅ 如果参数为 `null`，占位符会被移除
- ⚠️ SQL Server 的 `OFFSET` 需要 `ORDER BY`

##### `{{orderby}}` - 排序

```csharp
// 基本排序
[SqlTemplate("SELECT {{columns}} FROM users {{orderby created_at}}")]
Task<List<User>> GetUsersAsync();
// → ORDER BY created_at ASC

// 降序
[SqlTemplate("SELECT {{columns}} FROM users {{orderby created_at --desc}}")]
Task<List<User>> GetRecentUsersAsync();
// → ORDER BY created_at DESC

// 多列排序
[SqlTemplate("SELECT {{columns}} FROM users {{orderby age --desc, name}}")]
Task<List<User>> GetUsersAsync();
// → ORDER BY age DESC, name ASC
```

##### `{{set}}` - UPDATE SET子句

```csharp
// 自动生成 SET 子句（排除主键）
[SqlTemplate("UPDATE users {{set}} WHERE id = @id")]
Task<int> UpdateAsync(User user);
// → UPDATE users SET name=@name, age=@age, balance=@balance WHERE id = @id
```

##### `{{batch_values}}` - 批量插入

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 自动生成: 
// INSERT INTO users (name, age) VALUES 
// (@name_0, @age_0), (@name_1, @age_1), (@name_2, @age_2)...

// 自动分批处理（考虑数据库参数限制）
await repo.BatchInsertAsync(GenerateUsers(10000));
// 自动分为 20 批（每批 500 条）
```

#### 4. 特殊功能特性

##### 返回插入的ID

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
[ReturnInsertedId]
Task<long> InsertAsync(string name, int age);

// 自动添加数据库特定的返回ID逻辑：
// SQLite:      SELECT last_insert_rowid()
// MySQL:       SELECT LAST_INSERT_ID()
// PostgreSQL:  RETURNING id
// SQL Server:  OUTPUT INSERTED.id
// Oracle:      RETURNING id INTO :id
```

##### 返回插入的实体

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
[ReturnInsertedEntity]
Task<User> InsertAndReturnAsync(string name, int age);

// 执行插入后自动查询完整实体（包括默认值）
```

##### 批量操作

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]  // 每批最多500条
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 特点：
// - 自动处理数据库参数限制（SQL Server: 2100参数）
// - 自动分批（如果数据超过 MaxBatchSize）
// - 返回总影响行数
```

##### 表达式转SQL

```csharp
[SqlTemplate("SELECT {{columns}} FROM users {{where}}")]
Task<List<User>> FindAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 使用
var users = await repo.FindAsync(u => u.Age >= 18 && u.IsActive);
```

#### 5. 事务支持

```csharp
// 仓储自动支持事务
public partial class UserRepository(DbConnection connection) : IUserRepository 
{
    public DbTransaction? Transaction { get; set; }  // 自动生成
}

// 使用
await using var tx = await connection.BeginTransactionAsync();
repo.Transaction = tx;

try 
{
    await repo.InsertAsync(user);
    await repo.UpdateBalanceAsync(userId, 1000m);
    await tx.CommitAsync();
}
catch 
{
    await tx.RollbackAsync();
    throw;
}
```

#### 6. 拦截器（Partial Methods）

```csharp
public partial class UserRepository 
{
    // SQL 执行前
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[{operationName}] SQL: {command.CommandText}");
        // 可以修改 command
        // 可以记录开始时间
    }
    
    // SQL 执行后
    partial void OnExecuted(string operationName, DbCommand command, long elapsedMilliseconds)
    {
        Console.WriteLine($"[{operationName}] 完成，耗时: {elapsedMilliseconds}ms");
        // 可以记录性能指标
    }
    
    // SQL 执行失败
    partial void OnExecuteFail(string operationName, DbCommand command, Exception exception)
    {
        Console.WriteLine($"[{operationName}] 失败: {exception.Message}");
        // 可以记录错误
        // 可以发送告警
    }
}
```

#### 7. ICrudRepository 接口

自动实现8个标准CRUD方法：

```csharp
public interface ICrudRepository<TEntity, TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<List<TEntity>> GetAllAsync(int? limit = null, int? offset = null, CancellationToken ct = default);
    Task<long> InsertAsync(TEntity entity, CancellationToken ct = default);
    Task<int> UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task<int> DeleteAsync(TKey id, CancellationToken ct = default);
    Task<long> CountAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);
    Task<int> BatchInsertAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
}

// 使用
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository : ICrudRepository<User, long> 
{
    // 自动获得8个方法
    // 可以添加自定义方法
}
```

#### 8. 高级特性（部分实现）

##### SoftDelete（软删除）

```csharp
[SoftDelete(
    FlagColumn = "is_deleted",           // 删除标志列
    TimestampColumn = "deleted_at",      // 删除时间列（可选）
    DeletedByColumn = "deleted_by")]     // 删除人列（可选）
public class Product 
{
    public long Id { get; set; }
    public string Name { get; set; }
    // 不需要显式定义软删除列
}

// DELETE 操作会转换为 UPDATE
await repo.DeleteAsync(productId);
// UPDATE products SET is_deleted = 1, deleted_at = NOW() WHERE id = @id

// 查询自动过滤已删除数据
var products = await repo.GetAllAsync();
// SELECT * FROM products WHERE is_deleted = 0

// 包含已删除数据
[SqlTemplate("SELECT {{columns}} FROM products")]
[IncludeDeleted]
Task<List<Product>> GetAllIncludingDeletedAsync();
```

##### AuditFields（审计字段）

```csharp
[AuditFields(
    CreatedAtColumn = "created_at",
    CreatedByColumn = "created_by",
    UpdatedAtColumn = "updated_at",
    UpdatedByColumn = "updated_by")]
public class Order 
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
}

// INSERT 时自动设置 created_at, created_by
// UPDATE 时自动设置 updated_at, updated_by
```

##### ConcurrencyCheck（乐观锁）

```csharp
public class Account 
{
    public long Id { get; set; }
    public decimal Balance { get; set; }
    
    [ConcurrencyCheck]
    public long Version { get; set; }
}

// UPDATE 时自动检查版本
// UPDATE accounts SET balance=@balance, version=version+1 
// WHERE id=@id AND version=@version

// 如果版本不匹配，返回 0（无行受影响）
```

#### 9. 多数据库支持

```csharp
// 使用 SqlDefine 指定数据库方言
[SqlDefine(SqlDefineTypes.SQLite)]      // SQLite
[SqlDefine(SqlDefineTypes.MySql)]       // MySQL
[SqlDefine(SqlDefineTypes.PostgreSql)]  // PostgreSQL
[SqlDefine(SqlDefineTypes.SqlServer)]   // SQL Server
[SqlDefine(SqlDefineTypes.Oracle)]      // Oracle

// 占位符会根据方言生成不同SQL
```

**方言差异示例**：

| 功能 | SQLite | SQL Server | PostgreSQL |
|------|--------|------------|------------|
| LIMIT | `LIMIT @n` | `TOP (@n)` | `LIMIT @n` |
| 返回ID | `SELECT last_insert_rowid()` | `OUTPUT INSERTED.id` | `RETURNING id` |
| 布尔值 | `1/0` | `1/0` | `TRUE/FALSE` |

---

## 代码模式

### 模式1: 基础CRUD仓储

```csharp
// 1. 定义实体
[TableName("users")]
public class User 
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }  // Nullable
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2. 定义接口（继承 ICrudRepository）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository : ICrudRepository<User, long> 
{
    // 自动获得8个方法，无需定义
}

// 3. 实现类
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 使用
await using var conn = new SqliteConnection("Data Source=app.db");
await conn.OpenAsync();
var repo = new UserRepository(conn);

var user = new User { Name = "Alice", Age = 25, IsActive = true, CreatedAt = DateTime.UtcNow };
long userId = await repo.InsertAsync(user);
var found = await repo.GetByIdAsync(userId);
```

### 模式2: 自定义查询方法

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository : ICrudRepository<User, long>
{
    // 简单条件查询
    [SqlTemplate("SELECT {{columns}} FROM users WHERE age >= @minAge AND is_active = @isActive")]
    Task<List<User>> GetActiveUsersAsync(int minAge, bool isActive = true);
    
    // 使用 LIKE 搜索
    [SqlTemplate("SELECT {{columns}} FROM users WHERE name LIKE @pattern")]
    Task<List<User>> SearchByNameAsync(string pattern);
    
    // 分页查询
    [SqlTemplate("SELECT {{columns}} FROM users {{orderby created_at --desc}} {{limit}} {{offset}}")]
    Task<List<User>> GetPagedAsync(int? limit = 20, int? offset = 0);
    
    // 聚合查询
    [SqlTemplate("SELECT COUNT(*) FROM users WHERE age >= @minAge")]
    Task<long> CountAdultsAsync(int minAge = 18);
    
    // 复杂查询返回动态字典
    [SqlTemplate(@"
        SELECT u.id, u.name, COUNT(o.id) as order_count
        FROM users u
        LEFT JOIN orders o ON o.user_id = u.id
        GROUP BY u.id, u.name
        HAVING COUNT(o.id) > @minOrders
    ")]
    Task<List<Dictionary<string, object?>>> GetActiveUsersWithOrdersAsync(int minOrders = 5);
}
```

### 模式3: 表达式树查询

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository 
{
    // 使用表达式树
    [SqlTemplate("SELECT {{columns}} FROM users {{where}}")]
    Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
    
    // 表达式树 + 分页
    [SqlTemplate("SELECT {{columns}} FROM users {{where}} {{orderby id}} {{limit}} {{offset}}")]
    Task<List<User>> QueryPagedAsync(
        [ExpressionToSql] Expression<Func<User, bool>> predicate,
        int? limit = null,
        int? offset = null);
}

// 使用
var adults = await repo.QueryAsync(u => u.Age >= 18 && u.IsActive);
var richUsers = await repo.QueryAsync(u => u.Balance > 10000 && !u.IsDeleted);
var searchResults = await repo.QueryAsync(u => u.Name.Contains("Alice"));
```

### 模式4: 批量操作

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository 
{
    // 批量插入
    [SqlTemplate("INSERT INTO users (name, age, is_active) VALUES {{batch_values}}")]
    [BatchOperation(MaxBatchSize = 500)]
    Task<int> BatchInsertAsync(IEnumerable<User> users);
    
    // 批量更新（通过临时表，高级用法）
    [SqlTemplate(@"
        UPDATE users 
        SET is_active = 0 
        WHERE id IN (SELECT value FROM json_each(@ids))
    ")]
    Task<int> BatchDeactivateAsync(string ids);  // JSON array string
}

// 使用
var users = Enumerable.Range(1, 10000)
    .Select(i => new User { Name = $"User{i}", Age = 20 + i % 50 })
    .ToList();

await repo.BatchInsertAsync(users);  // 自动分批
```

### 模式5: 事务处理

```csharp
public class UserService 
{
    private readonly IUserRepository _userRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly DbConnection _connection;
    
    public async Task<long> CreateUserWithOrderAsync(User user, Order order) 
    {
        await using var tx = await _connection.BeginTransactionAsync();
        _userRepo.Transaction = tx;
        _orderRepo.Transaction = tx;
        
        try 
        {
            // 插入用户
            long userId = await _userRepo.InsertAsync(user);
            
            // 插入订单
            order.UserId = userId;
            await _orderRepo.InsertAsync(order);
            
            // 提交事务
            await tx.CommitAsync();
            return userId;
        }
        catch 
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
```

### 模式6: 依赖注入集成

```csharp
// Program.cs / Startup.cs
var builder = WebApplication.CreateBuilder(args);

// 注册数据库连接
builder.Services.AddScoped<DbConnection>(sp => 
{
    var conn = new SqliteConnection("Data Source=app.db");
    conn.Open();  // 立即打开
    return conn;
});

// 注册仓储
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 使用
public class UserController : ControllerBase 
{
    private readonly IUserRepository _userRepo;
    
    public UserController(IUserRepository userRepo) 
    {
        _userRepo = userRepo;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(long id) 
    {
        var user = await _userRepo.GetByIdAsync(id);
        return user is not null ? Ok(user) : NotFound();
    }
}
```

### 模式7: 拦截器监控

```csharp
public partial class UserRepository 
{
    private readonly ILogger<UserRepository> _logger;
    
    // 构造函数注入日志
    public UserRepository(DbConnection connection, ILogger<UserRepository> logger)
        : this(connection) 
    {
        _logger = logger;
    }
    
    partial void OnExecuting(string operationName, DbCommand command)
    {
        _logger.LogDebug("[{Op}] SQL: {Sql}", operationName, command.CommandText);
    }
    
    partial void OnExecuted(string operationName, DbCommand command, long elapsedMs)
    {
        if (elapsedMs > 1000) 
        {
            _logger.LogWarning(
                "[{Op}] 慢查询检测: {Ms}ms\nSQL: {Sql}",
                operationName, elapsedMs, command.CommandText);
        }
    }
    
    partial void OnExecuteFail(string operationName, DbCommand command, Exception exception)
    {
        _logger.LogError(exception, "[{Op}] 执行失败", operationName);
    }
}
```

---

## 重要注意事项

### ⚠️ 关键限制

#### 1. 接口和实现必须分文件

```csharp
// ❌ 错误：同一文件
// UserRepository.cs
public interface IUserRepository { }
public partial class UserRepository : IUserRepository { }  // ❌ 不会生成代码

// ✅ 正确：分开文件
// IUserRepository.cs
public interface IUserRepository { }

// UserRepository.cs
public partial class UserRepository : IUserRepository { }  // ✅ 会生成代码
```

**原因**：源生成器在编译时运行，无法看到正在编译的同一文件中的定义。

#### 2. 必须使用 DbConnection（不是 IDbConnection）

```csharp
// ❌ 错误：IDbConnection 不支持异步
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

// ✅ 正确：DbConnection 支持异步
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

**原因**：`IDbConnection` 是旧接口，不支持真正的异步I/O。

#### 3. CancellationToken 参数命名

```csharp
// ✅ 正确：参数名必须包含 "cancellation" 或 "token"
Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

// ❌ 错误：不会被识别为 CancellationToken
Task<User?> GetByIdAsync(long id, CancellationToken c = default);
```

#### 4. SQL 参数必须与方法参数匹配

```csharp
// ✅ 正确：参数名匹配（不区分大小写）
[SqlTemplate("SELECT * FROM users WHERE id = @id AND age = @age")]
Task<User?> GetUserAsync(long id, int age);

// ❌ 错误：SQL中的参数找不到
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetUserAsync(long id);  // 参数名不匹配
```

#### 5. 实体类必须使用公共属性

```csharp
// ✅ 正确：公共属性
public class User 
{
    public long Id { get; set; }
    public string Name { get; set; }
}

// ❌ 错误：字段、私有属性不会被识别
public class User 
{
    public long Id;  // ❌ 字段
    private string Name { get; set; }  // ❌ 私有
}
```

#### 6. Nullable 引用类型支持

```csharp
#nullable enable

public class User 
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;  // 不可空
    public string? Email { get; set; }  // 可空
    public int? Age { get; set; }  // 可空值类型
}

// 生成的代码会正确处理 null 检查
if (!reader.IsDBNull(emailOrdinal))
{
    entity.Email = reader.GetString(emailOrdinal);
}
```

### 🔒 安全注意事项

#### 1. 防止SQL注入

```csharp
// ✅ 安全：使用参数化查询
[SqlTemplate("SELECT * FROM users WHERE name = @name")]
Task<List<User>> FindByNameAsync(string name);

// ❌ 危险：不要使用字符串拼接（Sqlx不支持这种方式）
// 所有 @参数 都会自动参数化，天然防SQL注入
```

#### 2. 连接管理

```csharp
// ✅ 正确：使用 using 或 await using
await using DbConnection conn = new SqliteConnection("...");
await conn.OpenAsync();
// 自动关闭和释放

// ❌ 错误：不释放连接
DbConnection conn = new SqliteConnection("...");
await conn.OpenAsync();
// 可能导致连接泄漏
```

#### 3. 事务管理

```csharp
// ✅ 正确：使用 try-catch-finally
await using var tx = await conn.BeginTransactionAsync();
try 
{
    await repo.InsertAsync(user);
    await tx.CommitAsync();
}
catch 
{
    await tx.RollbackAsync();
    throw;
}

// ❌ 错误：忘记 Rollback
await using var tx = await conn.BeginTransactionAsync();
await repo.InsertAsync(user);
await tx.CommitAsync();  // 如果出错怎么办？
```

### 🎯 命名约定

#### 属性名 → 列名转换

```csharp
// 自动转换规则（PascalCase → snake_case）
Id           → id
Name         → name
UserName     → user_name
CreatedAt    → created_at
IsActive     → is_active
EmailAddress → email_address
```

#### 类名 → 表名转换

```csharp
// 默认：类名小写
User    → user
Product → product
OrderItem → orderitem  // 注意：不是 order_item

// 推荐：使用 [TableName] 明确指定
[TableName("users")]
public class User { }

[TableName("order_items")]
public class OrderItem { }
```

---

## 常见错误

### 错误1: 生成的代码找不到

**症状**：
```
error CS0535: 'UserRepository' does not implement interface member 'IUserRepository.GetByIdAsync(long)'
```

**原因**：
1. 接口和实现在同一文件
2. 缺少 `[SqlDefine]` 或 `[RepositoryFor]`
3. 项目未重新编译

**解决方案**：
```csharp
// 1. 确保接口和实现分文件
// 2. 确保标记了必需特性
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository { }

public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 3. 重新编译
dotnet clean
dotnet build
```

### 错误2: SQL参数找不到

**症状**：
```
error: SQL template contains parameter @userId but method does not have matching parameter
```

**原因**：SQL中的参数名与方法参数不匹配

**解决方案**：
```csharp
// ❌ 错误
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetByIdAsync(long id);

// ✅ 正确
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

### 错误3: 异步方法不支持

**症状**：
```
error: Cannot use IDbConnection with async methods
```

**原因**：使用了 `IDbConnection` 而不是 `DbConnection`

**解决方案**：
```csharp
// ❌ 错误
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

// ✅ 正确
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

### 错误4: 返回类型不匹配

**症状**：
```
error: Return type mismatch for method GetByIdAsync
```

**原因**：返回类型与SQL语句不匹配

**解决方案**：
```csharp
// ✅ SELECT 单行 → Task<T?>
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// ✅ SELECT 多行 → Task<List<T>>
[SqlTemplate("SELECT * FROM users")]
Task<List<User>> GetAllAsync();

// ✅ INSERT/UPDATE/DELETE → Task<int>
[SqlTemplate("DELETE FROM users WHERE id = @id")]
Task<int> DeleteAsync(long id);

// ✅ COUNT → Task<long>
[SqlTemplate("SELECT COUNT(*) FROM users")]
Task<long> CountAsync();
```

### 错误5: 表达式树不支持的操作

**症状**：
```
error: Expression type 'MethodCall' is not supported in SQL conversion
```

**原因**：使用了不支持的表达式操作

**解决方案**：
```csharp
// ❌ 错误：不支持的方法调用
u => u.Age.ToString() == "18"

// ✅ 正确：使用支持的操作
u => u.Age == 18

// ❌ 错误：本地变量
var minAge = 18;
u => u.Age >= minAge

// ✅ 正确：使用参数
[SqlTemplate("SELECT {{columns}} FROM users {{where}}")]
Task<List<User>> QueryAsync(
    [ExpressionToSql] Expression<Func<User, bool>> predicate,
    int minAge = 18);
```

---

## 性能优化

### 1. 使用批量操作

```csharp
// ❌ 慢：循环插入（N次数据库往返）
foreach (var user in users) 
{
    await repo.InsertAsync(user);
}

// ✅ 快：批量插入（1-2次数据库往返）
await repo.BatchInsertAsync(users);
```

### 2. 使用 LIMIT 限制结果集

```csharp
// ❌ 慢：查询所有数据
[SqlTemplate("SELECT {{columns}} FROM users")]
Task<List<User>> GetAllAsync();

// ✅ 快：限制结果数量
[SqlTemplate("SELECT {{columns}} FROM users {{limit}}")]
Task<List<User>> GetAllAsync(int? limit = 100);
```

### 3. 只查询需要的列

```csharp
// ❌ 慢：SELECT *
[SqlTemplate("SELECT * FROM users")]
Task<List<User>> GetAllAsync();

// ✅ 快：只查询需要的列
[SqlTemplate("SELECT id, name FROM users")]
Task<List<Dictionary<string, object?>>> GetNamesAsync();
```

### 4. 使用连接池

```csharp
// 连接字符串中配置
// SQLite
"Data Source=app.db;Cache=Shared;Pooling=true"

// MySQL
"Server=localhost;Database=app;Pooling=true;Min Pool Size=5;Max Pool Size=100"

// PostgreSQL
"Host=localhost;Database=app;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100"
```

### 5. 使用索引

```sql
-- 为常用查询字段创建索引
CREATE INDEX idx_user_age ON users(age);
CREATE INDEX idx_user_email ON users(email);
CREATE INDEX idx_user_created_at ON users(created_at);

-- 组合索引
CREATE INDEX idx_user_age_balance ON users(age, balance);
```

### 6. 避免 N+1 查询

```csharp
// ❌ 慢：N+1 查询
var users = await userRepo.GetAllAsync();
foreach (var user in users) 
{
    var orders = await orderRepo.GetByUserIdAsync(user.Id);  // N 次查询
}

// ✅ 快：使用 JOIN
[SqlTemplate(@"
    SELECT u.id, u.name, o.id as order_id, o.amount
    FROM users u
    LEFT JOIN orders o ON o.user_id = u.id
")]
Task<List<Dictionary<string, object?>>> GetUsersWithOrdersAsync();
```

---

## 故障排查

### 查看生成的代码

生成的代码位置：
```
项目目录/obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.Generator.CSharpGenerator/
    └── UserRepository.g.cs
```

或在IDE中：
- **Visual Studio**: Solution Explorer → Dependencies → Analyzers → Sqlx.Generator
- **Rider**: 类似位置

### 启用生成器日志

```xml
<!-- .csproj -->
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### 常见问题检查清单

- [ ] 接口和实现是否在不同文件？
- [ ] 是否标记了 `[SqlDefine]`？
- [ ] 是否标记了 `[RepositoryFor]`？
- [ ] 是否使用 `DbConnection`（不是 `IDbConnection`）？
- [ ] SQL参数名是否与方法参数匹配？
- [ ] 返回类型是否正确？
- [ ] 是否重新编译了项目？

---

## 完整示例总结

### 最小完整示例

```csharp
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

// 1. 实体
[TableName("users")]
public class User 
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

// 2. 接口
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository 
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age);
}

// 3. 实现
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 使用
class Program 
{
    static async Task Main() 
    {
        await using var conn = new SqliteConnection("Data Source=:memory:");
        await conn.OpenAsync();
        
        // 创建表
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL
            )";
        await cmd.ExecuteNonQueryAsync();
        
        // 使用仓储
        var repo = new UserRepository(conn);
        long id = await repo.InsertAsync("Alice", 25);
        var user = await repo.GetByIdAsync(id);
        
        Console.WriteLine($"User: {user?.Name}, Age: {user?.Age}");
    }
}
```

---

## 总结

### Sqlx 的核心优势

1. ✅ **极致性能** - 接近原生 ADO.NET（1.05-1.13x）
2. ✅ **类型安全** - 编译时验证，零运行时错误
3. ✅ **零反射** - 所有代码编译时生成
4. ✅ **完全异步** - 真正的异步I/O
5. ✅ **简单易用** - 学习曲线极低
6. ✅ **跨数据库** - 一套代码，5种数据库

### Sqlx 的适用场景

✅ **推荐使用**：
- 性能要求高的应用
- 需要完全控制SQL的场景
- 微服务架构
- AOT部署
- CRUD为主的应用

❌ **不推荐使用**：
- 需要复杂ORM功能（导航属性、延迟加载）
- 团队不熟悉SQL
- 需要频繁更改数据模型

### 快速参考卡片

```csharp
// 基础模板
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(Entity))]
public interface IRepo {
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Entity?> GetAsync(long id);
}
public partial class Repo(DbConnection conn) : IRepo { }

// 常用占位符
{{columns}}      // 列列表
{{table}}        // 表名
{{where}}        // WHERE子句（表达式树）
{{limit}}        // LIMIT
{{offset}}       // OFFSET
{{orderby col}}  // ORDER BY
{{set}}          // UPDATE SET
{{batch_values}} // 批量VALUES

// 常用特性
[ReturnInsertedId]    // 返回自增ID
[BatchOperation]      // 批量操作
[ExpressionToSql]     // 表达式参数
[IncludeDeleted]      // 包含软删除

// 拦截器
partial void OnExecuting(string op, DbCommand cmd);
partial void OnExecuted(string op, DbCommand cmd, long ms);
partial void OnExecuteFail(string op, DbCommand cmd, Exception ex);
```

---

**文档版本**: 0.4.0  
**最后更新**: 2025-10-26  
**维护**: Sqlx Team

