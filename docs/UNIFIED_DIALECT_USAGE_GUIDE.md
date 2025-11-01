# 统一方言使用指南

## 📋 概述

本指南展示如何使用统一接口定义来支持多数据库方言，实现"写一次，多数据库运行"的目标。

## 🎯 核心概念

### 1. 占位符系统

使用`{{}}`占位符在SQL模板中标记需要方言适配的部分：

| 占位符 | PostgreSQL | MySQL | SQL Server | SQLite |
|--------|-----------|-------|------------|--------|
| `{{table}}` | `"users"` | `` `users` `` | `[users]` | `"users"` |
| `{{returning_id}}` | `RETURNING id` | (empty) | (empty) | (empty) |
| `{{bool_true}}` | `true` | `1` | `1` | `1` |
| `{{bool_false}}` | `false` | `0` | `0` | `0` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `NOW()` | `GETDATE()` | `datetime('now')` |

### 2. 统一接口定义

只需定义一次基础接口，使用占位符编写SQL：

```csharp
using Sqlx.Annotations;
using System.Threading;
using System.Threading.Tasks;

public interface IUserRepositoryBase
{
    [SqlTemplate(@"SELECT * FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    [SqlTemplate(@"
        INSERT INTO {{table}} (username, email, age, created_at) 
        VALUES (@username, @email, @age, {{current_timestamp}}) 
        {{returning_id}}")]
    Task<int> InsertAsync(User user, CancellationToken ct = default);

    [SqlTemplate(@"UPDATE {{table}} SET active = {{bool_true}} WHERE id = @id")]
    Task ActivateAsync(int id, CancellationToken ct = default);

    [SqlTemplate(@"SELECT * FROM {{table}} ORDER BY id LIMIT @limit OFFSET @offset")]
    Task<List<User>> GetPagedAsync(int limit, int offset, CancellationToken ct = default);
}
```

### 3. 方言实现

为每个数据库创建一个简单的实现类，指定方言和表名：

```csharp
// PostgreSQL
[RepositoryFor(typeof(IUserRepositoryBase), 
    Dialect = SqlDefineTypes.PostgreSql, 
    TableName = "users")]
public partial class PostgreSQLUserRepository : IUserRepositoryBase
{
    private readonly DbConnection _connection;
    
    public PostgreSQLUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

// MySQL
[RepositoryFor(typeof(IUserRepositoryBase), 
    Dialect = SqlDefineTypes.MySql, 
    TableName = "users")]
public partial class MySQLUserRepository : IUserRepositoryBase
{
    private readonly DbConnection _connection;
    
    public MySQLUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

// SQL Server
[RepositoryFor(typeof(IUserRepositoryBase), 
    Dialect = SqlDefineTypes.SqlServer, 
    TableName = "users")]
public partial class SqlServerUserRepository : IUserRepositoryBase
{
    private readonly DbConnection _connection;
    
    public SqlServerUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

// SQLite
[RepositoryFor(typeof(IUserRepositoryBase), 
    Dialect = SqlDefineTypes.SQLite, 
    TableName = "users")]
public partial class SQLiteUserRepository : IUserRepositoryBase
{
    private readonly DbConnection _connection;
    
    public SQLiteUserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}
```

## 🔧 源生成器处理流程

源生成器会为每个实现类生成适配的代码：

### PostgreSQL 生成结果示例

```csharp
public partial class PostgreSQLUserRepository
{
    // GetByIdAsync
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var __sql__ = @"SELECT * FROM ""users"" WHERE id = @id";
        // ... 执行逻辑
    }

    // InsertAsync
    public async Task<int> InsertAsync(User user, CancellationToken ct = default)
    {
        var __sql__ = @"
            INSERT INTO ""users"" (username, email, age, created_at) 
            VALUES (@username, @email, @age, CURRENT_TIMESTAMP) 
            RETURNING id";
        // ... 执行逻辑，使用 RETURNING 获取ID
    }

    // ActivateAsync
    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var __sql__ = @"UPDATE ""users"" SET active = true WHERE id = @id";
        // ... 执行逻辑
    }
}
```

### MySQL 生成结果示例

```csharp
public partial class MySQLUserRepository
{
    // GetByIdAsync
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var __sql__ = @"SELECT * FROM `users` WHERE id = @id";
        // ... 执行逻辑
    }

    // InsertAsync
    public async Task<int> InsertAsync(User user, CancellationToken ct = default)
    {
        var __sql__ = @"
            INSERT INTO `users` (username, email, age, created_at) 
            VALUES (@username, @email, @age, NOW()) 
            ";
        // ... 执行逻辑，使用 LAST_INSERT_ID() 获取ID
    }

    // ActivateAsync
    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var __sql__ = @"UPDATE `users` SET active = 1 WHERE id = @id";
        // ... 执行逻辑
    }
}
```

## 📝 完整示例

### User实体

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 使用示例

```csharp
// 在应用程序中使用
public class UserService
{
    private readonly IUserRepositoryBase _repository;

    public UserService(IUserRepositoryBase repository)
    {
        _repository = repository;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<int> CreateUserAsync(User user)
    {
        return await _repository.InsertAsync(user);
    }

    public async Task ActivateUserAsync(int id)
    {
        await _repository.ActivateAsync(id);
    }

    public async Task<List<User>> GetUsersPageAsync(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        return await _repository.GetPagedAsync(pageSize, offset);
    }
}
```

### 依赖注入配置

```csharp
// Startup.cs or Program.cs

// PostgreSQL
services.AddScoped<IUserRepositoryBase>(sp =>
{
    var connection = new NpgsqlConnection(Configuration.GetConnectionString("PostgreSQL"));
    return new PostgreSQLUserRepository(connection);
});

// 或 MySQL
services.AddScoped<IUserRepositoryBase>(sp =>
{
    var connection = new MySqlConnection(Configuration.GetConnectionString("MySQL"));
    return new MySQLUserRepository(connection);
});

// 或 SQL Server
services.AddScoped<IUserRepositoryBase>(sp =>
{
    var connection = new SqlConnection(Configuration.GetConnectionString("SqlServer"));
    return new SqlServerUserRepository(connection);
});

// 或 SQLite
services.AddScoped<IUserRepositoryBase>(sp =>
{
    var connection = new SqliteConnection(Configuration.GetConnectionString("SQLite"));
    return new SQLiteUserRepository(connection);
});
```

## 🎨 高级用法

### 1. 表名推断优先级

```csharp
// 优先级1: RepositoryFor.TableName (最高)
[RepositoryFor(typeof(IUserRepositoryBase), TableName = "custom_users")]
public partial class UserRepository : IUserRepositoryBase { }

// 优先级2: TableNameAttribute
[TableName("custom_users")]
public class User { }

// 优先级3: 从实体类型名推断 (最低)
public class User { } // 推断为 "user"
```

### 2. 多占位符组合

```csharp
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns}}, created_at, is_active) 
    VALUES (@id, @username, @email, {{current_timestamp}}, {{bool_true}})
    {{returning_id}}")]
Task<int> InsertAsync(User user, CancellationToken ct = default);
```

### 3. 条件查询

```csharp
[SqlTemplate(@"
    SELECT * FROM {{table}} 
    WHERE (@username IS NULL OR username = @username)
      AND (@minAge IS NULL OR age >= @minAge)
      AND active = {{bool_true}}
    ORDER BY created_at DESC
    LIMIT @limit")]
Task<List<User>> SearchAsync(
    string? username, 
    int? minAge, 
    int limit, 
    CancellationToken ct = default);
```

## ✅ 最佳实践

### 1. 接口命名
- 基础接口使用`I{Entity}RepositoryBase`命名
- 实现类使用`{Dialect}{Entity}Repository`命名

### 2. 占位符使用
- 总是使用`{{table}}`而不是硬编码表名
- 使用`{{bool_true}}`/`{{bool_false}}`而不是硬编码`1`/`0`
- 使用`{{current_timestamp}}`而不是方言特定的函数

### 3. SQL格式
- 使用@前缀参数化查询
- 使用verbatim字符串(@"...")保持SQL可读性
- 适当的缩进和换行

### 4. 错误处理
```csharp
public async Task<User?> GetUserSafeAsync(int id)
{
    try
    {
        return await _repository.GetByIdAsync(id);
    }
    catch (DbException ex)
    {
        _logger.LogError(ex, "Failed to get user {UserId}", id);
        return null;
    }
}
```

## 🚀 性能优化

### 1. 连接管理
```csharp
// 使用连接池
services.AddScoped<IUserRepositoryBase>(sp =>
{
    var connection = sp.GetRequiredService<DbConnection>();
    return new PostgreSQLUserRepository(connection);
});
```

### 2. 批量操作
```csharp
[SqlTemplate(@"
    INSERT INTO {{table}} (username, email) 
    VALUES (@username0, @email0), (@username1, @email1), (@username2, @email2)
    {{returning_id}}")]
Task<List<int>> BatchInsertAsync(List<User> users, CancellationToken ct = default);
```

### 3. 异步操作
- 所有数据库操作使用`async`/`await`
- 传递`CancellationToken`支持取消

## 📊 测试策略

### 单元测试

```csharp
[TestClass]
public class UserRepositoryTests
{
    [TestMethod]
    public async Task GetByIdAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var repository = new SQLiteUserRepository(connection);
        
        // Act
        var user = await repository.GetByIdAsync(1);
        
        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("test@example.com", user.Email);
    }
}
```

### 集成测试

```csharp
[TestClass]
public class MultiDialectIntegrationTests
{
    [TestMethod]
    public async Task AllDialects_ShouldWorkConsistently()
    {
        var dialects = new[]
        {
            (IUserRepositoryBase)new PostgreSQLUserRepository(pgConnection),
            new MySQLUserRepository(mysqlConnection),
            new SqlServerUserRepository(sqlServerConnection),
            new SQLiteUserRepository(sqliteConnection)
        };

        foreach (var repo in dialects)
        {
            var user = new User { Username = "test", Email = "test@example.com" };
            var id = await repo.InsertAsync(user);
            var retrieved = await repo.GetByIdAsync(id);
            
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("test@example.com", retrieved.Email);
        }
    }
}
```

## 🎯 总结

使用统一方言架构的优势：

1. **编写一次，多处运行** - 一个接口定义支持所有数据库
2. **类型安全** - 编译时验证
3. **零运行时开销** - 编译时生成代码
4. **易于维护** - SQL集中定义
5. **可测试性强** - 接口抽象便于mock
6. **方言透明** - 应用代码不感知数据库类型

---

*最后更新: 2025-11-01*

