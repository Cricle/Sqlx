# 💡 Sqlx 最佳实践指南

本指南提供使用 Sqlx 的最佳实践和推荐模式。

---

## 🎯 核心原则

### 1. 智能占位符 vs 直接写 SQL

**核心理念**：复杂的用占位符，简单的直接写

| 场景 | 推荐方式 | 原因 |
|------|---------|------|
| **列名列表** | `{{columns}}` | 自动生成，类型安全，维护方便 |
| **表名** | `{{table}}` | 自动转换命名规则 |
| **SET 子句** | `{{set}}` | 自动生成复杂赋值语句 |
| **值占位符** | `{{values}}` | 自动匹配列顺序 |
| **WHERE 条件** | 直接写 SQL | 更清晰，更灵活 |
| **聚合函数** | 直接写 SQL | 更简短，更标准 |
| **INSERT/UPDATE/DELETE** | 直接写关键字 | 一目了然 |

**示例：**

```csharp
// ✅ 推荐：智能占位符 + 直接 SQL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND is_active = @isActive {{orderby created_at --desc}}")]
Task<List<User>> SearchAsync(int minAge, bool isActive);

// ❌ 避免：过度使用占位符
[Sqlx("{{select}} {{columns}} {{from}} {{table}} {{where age>=@minAge}} {{and is_active=@isActive}} {{orderby created_at --desc}}")]
```

---

## 🏗️ 项目结构最佳实践

### 推荐的文件组织

```
YourProject/
├── Models/                    # 数据模型
│   ├── User.cs
│   ├── Order.cs
│   └── Product.cs
├── Services/                  # 服务层（Repository）
│   ├── IUserService.cs       # 接口（包含 Sqlx 属性）
│   ├── UserService.cs        # 实现（标记 RepositoryFor）
│   ├── IOrderService.cs
│   └── OrderService.cs
├── Queries/                   # 复杂查询（可选）
│   ├── UserQueries.cs
│   └── ReportQueries.cs
└── Data/                      # 数据库上下文
    └── DatabaseService.cs
```

### 数据模型规范

```csharp
using Sqlx.Annotations;

// ✅ 推荐：使用 Record 和 TableName
[TableName("users")]
public record User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }  // 可空字段
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }  // 可空字段
}

// ❌ 避免：过多的业务逻辑在模型中
public class User
{
    // ... 属性 ...

    // ❌ 不要在模型中写业务逻辑
    public void Activate() { IsActive = true; }
    public bool CanEdit(User editor) { /* ... */ }
}
```

### 服务层规范

```csharp
// ✅ 推荐：接口定义 + 自动实现
public interface IUserService
{
    // 清晰的方法名和注释
    /// <summary>获取所有活跃用户</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive {{orderby created_at --desc}}")]
    Task<List<User>> GetActiveUsersAsync(bool isActive = true);

    /// <summary>根据ID获取用户</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);

    /// <summary>创建用户</summary>
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    Task<int> CreateAsync(User user);
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IUserService))]
public partial class UserService(IDbConnection connection) : IUserService;
```

---

## 🎯 SQL 编写最佳实践

### 1. WHERE 条件

```csharp
// ✅ 推荐：直接写清晰的 SQL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND age <= @maxAge")]
Task<List<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

// ✅ 推荐：使用参数避免 SQL 注入
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE name LIKE @query")]
Task<List<User>> SearchByNameAsync(string query);  // 调用时: SearchByNameAsync("%john%")

// ❌ 避免：字符串拼接（SQL 注入风险）
// 不要这样做：
// [Sqlx($"SELECT * FROM users WHERE name LIKE '{query}'")]
```

### 2. 排除字段策略

```csharp
// ✅ 插入时排除自增 ID
[Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);

// ✅ 更新时排除不可变字段
[Sqlx("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// ✅ 只更新特定字段
[Sqlx("UPDATE {{table}} SET {{set --only Name Email Age}} WHERE id = @id")]
Task<int> UpdateBasicInfoAsync(User user);
```

### 3. 排序和分页

```csharp
// ✅ 推荐：使用 orderby 占位符
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
Task<List<User>> GetRecentUsersAsync();

// ✅ 多列排序
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby priority --desc}} {{orderby created_at --desc}}")]
Task<List<Todo>> GetSortedTodosAsync();

// ✅ 分页（SQL Server / PostgreSQL）
[Sqlx(@"SELECT {{columns}} FROM {{table}}
        {{orderby id}}
        OFFSET @skip ROWS
        FETCH NEXT @take ROWS ONLY")]
Task<List<User>> GetPagedAsync(int skip, int take);

// ✅ 分页（MySQL / SQLite）
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby id}} LIMIT @take OFFSET @skip")]
Task<List<User>> GetPagedAsync(int skip, int take);
```

### 4. 批量操作

```csharp
// ✅ 推荐：使用 JSON 数组（SQLite / PostgreSQL / SQL Server）
[Sqlx("UPDATE {{table}} SET is_active = @isActive WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> ActivateUsersAsync(string idsJson, bool isActive);

// 调用示例：
var ids = new[] { 1, 2, 3 };
var idsJson = JsonSerializer.Serialize(ids);
await userService.ActivateUsersAsync(idsJson, true);

// ✅ 批量插入（多次调用单条插入）
public async Task<int> CreateManyAsync(List<User> users)
{
    var count = 0;
    foreach (var user in users)
    {
        count += await CreateAsync(user);
    }
    return count;
}
```

---

## 🔒 安全最佳实践

### 1. 参数化查询（必须）

```csharp
// ✅ 正确：使用参数
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
Task<User?> GetByNameAsync(string name);

// ❌ 错误：字符串插值（SQL 注入风险）
// 永远不要这样做：
// [Sqlx($"SELECT * FROM users WHERE name = '{name}'")]
```

### 2. 输入验证

```csharp
public async Task<List<User>> SearchAsync(string query)
{
    // ✅ 推荐：验证输入
    if (string.IsNullOrWhiteSpace(query))
        throw new ArgumentException("查询条件不能为空", nameof(query));

    if (query.Length > 100)
        throw new ArgumentException("查询条件过长", nameof(query));

    // 调用服务
    return await _userService.SearchAsync($"%{query}%");
}
```

### 3. 权限检查

```csharp
public async Task<int> DeleteAsync(int id, int currentUserId)
{
    // ✅ 推荐：检查权限
    var user = await GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("用户不存在");

    if (user.Id != currentUserId && !await IsAdminAsync(currentUserId))
        throw new UnauthorizedAccessException("无权删除此用户");

    return await _userService.DeleteAsync(id);
}
```

---

## 🚀 性能优化最佳实践

### 1. 只查询需要的列

```csharp
// ✅ 推荐：只查询需要的列
[Sqlx("SELECT {{columns --only Id Name Email}} FROM {{table}}")]
Task<List<User>> GetBasicInfoAsync();

// ❌ 避免：查询所有列（如果不需要）
[Sqlx("SELECT {{columns}} FROM {{table}}")]  // 包括所有列，可能很大
Task<List<User>> GetBasicInfoAsync();
```

### 2. 使用异步方法

```csharp
// ✅ 推荐：使用异步方法
Task<List<User>> GetAllAsync();
Task<User?> GetByIdAsync(int id);
Task<int> CreateAsync(User user);

// ❌ 避免：同步方法（阻塞线程）
List<User> GetAll();
User? GetById(int id);
```

### 3. 连接管理

```csharp
// ✅ 推荐：使用依赖注入管理连接
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }
}

// 在 Startup.cs / Program.cs 中注册
services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(Configuration.GetConnectionString("Default")));
services.AddScoped<IUserService, UserService>();
```

### 4. 缓存策略

```csharp
public class CachedUserService : IUserService
{
    private readonly IUserService _innerService;
    private readonly IMemoryCache _cache;

    public CachedUserService(IUserService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        var cacheKey = $"user_{id}";

        // ✅ 推荐：缓存不常变化的数据
        if (_cache.TryGetValue(cacheKey, out User? user))
            return user;

        user = await _innerService.GetByIdAsync(id);
        if (user != null)
        {
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
        }

        return user;
    }
}
```

---

## 🧪 测试最佳实践

### 1. 单元测试

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建表
        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT,
                age INTEGER,
                is_active INTEGER
            )");

        // 插入测试数据
        await connection.ExecuteAsync(
            "INSERT INTO users (id, name, email, age, is_active) VALUES (1, 'Test', 'test@example.com', 25, 1)");

        var service = new UserService(connection);

        // Act
        var user = await service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
        Assert.Equal("test@example.com", user.Email);
    }
}
```

### 2. 集成测试

```csharp
public class UserServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_ValidUser_InsertsUser()
    {
        // Arrange
        var service = new UserService(_fixture.Connection);
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await service.CreateAsync(user);

        // Assert
        Assert.True(result > 0);

        // Verify
        var created = await service.GetByIdAsync(result);
        Assert.NotNull(created);
        Assert.Equal(user.Name, created.Name);
    }
}
```

---

## 📊 监控和调试

### 1. 日志记录

```csharp
public class LoggingUserService : IUserService
{
    private readonly IUserService _innerService;
    private readonly ILogger<LoggingUserService> _logger;

    public LoggingUserService(IUserService innerService, ILogger<LoggingUserService> logger)
    {
        _innerService = innerService;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Getting user by ID: {UserId}", id);

        try
        {
            var user = await _innerService.GetByIdAsync(id);

            if (user == null)
                _logger.LogWarning("User not found: {UserId}", id);
            else
                _logger.LogInformation("User found: {UserId}, Name: {UserName}", id, user.Name);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            throw;
        }
    }
}
```

### 2. 查看生成的 SQL

生成的代码在 `obj/Debug/net9.0/generated/` 目录：

```
obj/Debug/net9.0/generated/
└── Sqlx.Generator/
    └── Sqlx.Generator.CSharpGenerator/
        └── UserService.Repository.g.cs  ← 查看这个文件
```

---

## 🌐 多数据库最佳实践

### 1. 使用配置切换数据库

```csharp
public class DatabaseConfig
{
    public static SqlDefineTypes GetDialect()
    {
        var dbType = Environment.GetEnvironmentVariable("DB_TYPE") ?? "SqlServer";

        return dbType.ToLower() switch
        {
            "sqlserver" => SqlDefineTypes.SqlServer,
            "mysql" => SqlDefineTypes.MySQL,
            "postgresql" => SqlDefineTypes.PostgreSQL,
            "sqlite" => SqlDefineTypes.SQLite,
            _ => SqlDefineTypes.SqlServer
        };
    }
}

// 在服务中使用
[SqlDefine(SqlDefineTypes.SqlServer)]  // 默认，可以被配置覆盖
[RepositoryFor(typeof(IUserService))]
public partial class UserService(IDbConnection connection) : IUserService;
```

### 2. 避免数据库特定的语法

```csharp
// ✅ 推荐：使用通用 SQL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE created_at >= @startDate")]
Task<List<User>> GetRecentUsersAsync(DateTime startDate);

// ❌ 避免：数据库特定语法
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE created_at >= DATEADD(day, -7, GETDATE())")]  // SQL Server 特定
Task<List<User>> GetRecentUsersAsync();
```

---

## 🎯 常见陷阱和解决方案

### 1. 忘记排除自增 ID

```csharp
// ❌ 错误：包含 Id 字段
[Sqlx("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);  // 插入时包含 Id，会报错

// ✅ 正确：排除 Id
[Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);
```

### 2. 更新时修改不可变字段

```csharp
// ❌ 错误：更新 CreatedAt
[Sqlx("UPDATE {{table}} SET {{set}} WHERE id = @id")]
Task<int> UpdateAsync(User user);  // 会更新 CreatedAt

// ✅ 正确：排除不可变字段
[Sqlx("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(User user);
```

### 3. 忘记处理 NULL 值

```csharp
// ✅ 推荐：明确处理 NULL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE email IS NOT NULL AND email = @email")]
Task<User?> GetByEmailAsync(string email);

// 或在调用前检查
public async Task<User?> GetByEmailAsync(string? email)
{
    if (string.IsNullOrEmpty(email))
        return null;

    return await _userService.GetByEmailAsync(email);
}
```

---

## 📚 相关文档

- [📖 快速开始](QUICK_START_GUIDE.md)
- [🎯 占位符指南](PLACEHOLDERS.md)
- [🌐 多数据库支持](MULTI_DATABASE_TEMPLATE_ENGINE.md)
- [🚀 TodoWebApi 示例](../samples/TodoWebApi/README.md)

---

## 🎉 总结

**Sqlx 最佳实践核心：**

1. ✅ **简单清晰** - 智能占位符 + 直接 SQL
2. ✅ **类型安全** - 编译时检查，运行时安全
3. ✅ **性能优先** - 异步方法，合理缓存
4. ✅ **安全第一** - 参数化查询，输入验证
5. ✅ **可维护性** - 清晰结构，完整测试

遵循这些最佳实践，您将构建出高质量、高性能、易维护的数据访问层！
