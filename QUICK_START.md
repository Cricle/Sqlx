# Sqlx 快速入门指南 🚀

**5分钟快速上手Sqlx** —— 从零开始构建第一个数据访问层

---

## 📦 步骤1：安装包

```bash
# 安装核心包和Source Generator
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 安装数据库驱动（根据需要选择）
dotnet add package Microsoft.Data.Sqlite          # SQLite
# 或
dotnet add package Npgsql                         # PostgreSQL
# 或
dotnet add package Microsoft.Data.SqlClient       # SQL Server
# 或
dotnet add package MySqlConnector                 # MySQL
# 或
dotnet add package Oracle.ManagedDataAccess.Core  # Oracle
```

---

## 🎯 步骤2A：使用通用CRUD接口（推荐 - 零样板代码）

### 1. 定义实体

```csharp
using System;

namespace MyApp.Models;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. 创建Repository（一行代码！）

```csharp
using Sqlx;
using Sqlx.Attributes;
using System.Data.Common;

namespace MyApp.Data;

[RepositoryFor<User>]
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]  // 根据数据库选择
public partial class UserRepository : ICrudRepository<User, long>
{
    private readonly DbConnection _connection;

    public UserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}
```

### 3. 使用Repository

```csharp
using Microsoft.Data.Sqlite;
using MyApp.Data;
using MyApp.Models;

// 创建数据库连接
using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

// 创建Repository
var userRepo = new UserRepository(connection);

// ✅ 插入用户
var newUser = new User
{
    Name = "张三",
    Email = "zhangsan@example.com",
    Age = 25,
    IsActive = true,
    CreatedAt = DateTime.Now
};
await userRepo.InsertAsync(newUser);

// ✅ 查询单个用户
var user = await userRepo.GetByIdAsync(1);
Console.WriteLine($"用户: {user?.Name}, 邮箱: {user?.Email}");

// ✅ 查询所有用户（分页）
var users = await userRepo.GetAllAsync(limit: 10, offset: 0);
foreach (var u in users)
{
    Console.WriteLine($"- {u.Name} ({u.Email})");
}

// ✅ 更新用户
user!.Age = 26;
await userRepo.UpdateAsync(user);

// ✅ 统计用户数
var count = await userRepo.CountAsync();
Console.WriteLine($"总用户数: {count}");

// ✅ 检查用户是否存在
var exists = await userRepo.ExistsAsync(1);
Console.WriteLine($"用户1存在: {exists}");

// ✅ 批量插入
var newUsers = new[]
{
    new User { Name = "李四", Email = "lisi@example.com", Age = 30, IsActive = true, CreatedAt = DateTime.Now },
    new User { Name = "王五", Email = "wangwu@example.com", Age = 28, IsActive = true, CreatedAt = DateTime.Now }
};
await userRepo.BatchInsertAsync(newUsers);

// ✅ 删除用户
await userRepo.DeleteAsync(1);
```

**就这么简单！** 🎉 你已经拥有了一个完整的、高性能的数据访问层！

---

## 🎨 步骤2B：自定义接口（完全控制）

如果你需要更多自定义查询，可以使用接口方式：

### 1. 定义接口

```csharp
using Sqlx;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MyApp.Data;

public interface IUserRepository
{
    // 基础查询
    [SqlTemplate("SELECT id, name, email, age, is_active, created_at FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    // 条件查询
    [SqlTemplate("SELECT id, name, email, age, is_active, created_at FROM users WHERE age > @minAge")]
    Task<List<User>> GetUsersByAgeAsync(int minAge);

    // 表达式查询（推荐！）
    [SqlTemplate("SELECT id, name, email, age, is_active, created_at FROM users WHERE {{where @predicate}}")]
    [ExpressionToSql(typeof(User))]
    Task<List<User>> FindAsync(Expression<Func<User, bool>> predicate);

    // 插入
    [SqlTemplate("INSERT INTO users (name, email, age, is_active, created_at) VALUES (@name, @email, @age, @is_active, @created_at)")]
    Task<int> InsertAsync(string name, string email, int age, bool is_active, DateTime created_at);

    // 批量插入
    [SqlTemplate("INSERT INTO users (name, email, age, is_active, created_at) VALUES {{values @users}}")]
    [BatchOperation(MaxBatchSize = 100)]
    Task<int> BatchInsertAsync(IEnumerable<User> users);

    // 更新
    [SqlTemplate("UPDATE users SET name = @name, email = @email, age = @age WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name, string email, int age);

    // 删除
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteByIdAsync(long id);

    // 聚合查询
    [SqlTemplate("SELECT COUNT(*) FROM users")]
    Task<int> GetCountAsync();

    [SqlTemplate("SELECT AVG(age) FROM users")]
    Task<double> GetAverageAgeAsync();
}
```

### 2. 实现Repository

```csharp
using Sqlx.Attributes;

namespace MyApp.Data;

[RepositoryFor<IUserRepository>]
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepository : IUserRepository
{
    private readonly DbConnection _connection;

    public UserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}
```

### 3. 使用自定义Repository

```csharp
var userRepo = new UserRepository(connection);

// 表达式查询（类型安全！）
var activeUsers = await userRepo.FindAsync(u => u.IsActive && u.Age >= 18);
var youngUsers = await userRepo.FindAsync(u => u.Age < 30);
var users = await userRepo.FindAsync(u => u.Name.StartsWith("张"));

// 聚合查询
var avgAge = await userRepo.GetAverageAgeAsync();
Console.WriteLine($"平均年龄: {avgAge:F1}");
```

---

## 🗄️ 步骤3：数据库初始化

### SQLite示例

```csharp
using Microsoft.Data.Sqlite;

public class DatabaseSetup
{
    public static async Task InitializeDatabaseAsync(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL
            )";
        await command.ExecuteNonQueryAsync();
    }
}

// 在应用启动时调用
await DatabaseSetup.InitializeDatabaseAsync("Data Source=app.db");
```

### PostgreSQL示例

```csharp
using Npgsql;

public class DatabaseSetup
{
    public static async Task InitializeDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                age INT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )";
        await command.ExecuteNonQueryAsync();
    }
}
```

---

## 🏗️ 步骤4：ASP.NET Core集成

### 1. 配置服务

```csharp
// Program.cs
using Microsoft.Data.Sqlite;
using MyApp.Data;

var builder = WebApplication.CreateBuilder(args);

// 注册数据库连接（Scoped，每个请求一个连接）
builder.Services.AddScoped<DbConnection>(sp => 
{
    var connection = new SqliteConnection("Data Source=app.db");
    connection.Open();
    return connection;
});

// 注册Repository
builder.Services.AddScoped<UserRepository>();
// 或使用接口方式
// builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// 初始化数据库
await DatabaseSetup.InitializeDatabaseAsync("Data Source=app.db");

app.MapControllers();
app.Run();
```

### 2. 在Controller中使用

```csharp
using Microsoft.AspNetCore.Mvc;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepo;

    public UsersController(UserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers([FromQuery] int limit = 10, [FromQuery] int offset = 0)
    {
        var users = await _userRepo.GetAllAsync(limit, offset);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        user.CreatedAt = DateTime.Now;
        await _userRepo.InsertAsync(user);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser(long id, [FromBody] User user)
    {
        user.Id = id;
        await _userRepo.UpdateAsync(user);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(long id)
    {
        await _userRepo.DeleteAsync(id);
        return NoContent();
    }
}
```

---

## 🎯 步骤5：多数据库支持

根据不同数据库修改`[SqlDefine]`特性：

```csharp
// SQLite
[SqlDefine(SqlDefineTypes.SQLite)]

// PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSQL)]

// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]

// MySQL
[SqlDefine(SqlDefineTypes.MySQL)]

// Oracle
[SqlDefine(SqlDefineTypes.Oracle)]
```

**示例：切换到PostgreSQL**

```csharp
using Npgsql;

[RepositoryFor<User>]
[TableName("users")]
[SqlDefine(SqlDefineTypes.PostgreSQL)]  // ← 改这里
public partial class UserRepository : ICrudRepository<User, long>
{
    public UserRepository(DbConnection connection) { }
}

// 依赖注入时改为PostgreSQL连接
builder.Services.AddScoped<DbConnection>(sp => 
{
    var connection = new NpgsqlConnection("Host=localhost;Database=mydb;Username=postgres;Password=password");
    connection.Open();
    return connection;
});
```

---

## 🚀 进阶功能

### 1. INSERT并返回ID

```csharp
[SqlTemplate("INSERT INTO users (name, email, age, is_active, created_at) VALUES (@name, @email, @age, @is_active, @created_at)")]
[ReturnInsertedId]  // ← 自动返回插入的ID
Task<long> InsertReturnIdAsync(string name, string email, int age, bool is_active, DateTime created_at);

// 使用
var newId = await userRepo.InsertReturnIdAsync("张三", "zhangsan@example.com", 25, true, DateTime.Now);
Console.WriteLine($"新用户ID: {newId}");
```

### 2. INSERT并返回完整实体

```csharp
[SqlTemplate("INSERT INTO users (name, email, age, is_active, created_at) VALUES (@name, @email, @age, @is_active, @created_at)")]
[ReturnInsertedEntity]  // ← 自动返回完整实体（包括自增ID）
Task<User> InsertReturnEntityAsync(string name, string email, int age, bool is_active, DateTime created_at);

// 使用
var newUser = await userRepo.InsertReturnEntityAsync("李四", "lisi@example.com", 30, true, DateTime.Now);
Console.WriteLine($"新用户: {newUser.Id} - {newUser.Name}");
```

### 3. 分页查询

```csharp
[SqlTemplate("SELECT {{columns}} FROM users ORDER BY created_at DESC LIMIT @limit OFFSET @offset")]
Task<List<User>> GetUsersPageAsync(int limit, int offset);

// 使用
var page1 = await userRepo.GetUsersPageAsync(10, 0);   // 第1页
var page2 = await userRepo.GetUsersPageAsync(10, 10);  // 第2页
var page3 = await userRepo.GetUsersPageAsync(10, 20);  // 第3页
```

### 4. 表达式查询（推荐！）

```csharp
// 接口定义
[SqlTemplate("SELECT {{columns}} FROM users WHERE {{where @predicate}}")]
[ExpressionToSql(typeof(User))]
Task<List<User>> FindAsync(Expression<Func<User, bool>> predicate);

// 使用 - 类型安全，编译时检查！
var users1 = await userRepo.FindAsync(u => u.Age > 20);
var users2 = await userRepo.FindAsync(u => u.IsActive && u.Age >= 18 && u.Age < 60);
var users3 = await userRepo.FindAsync(u => u.Name.Contains("张") || u.Name.Contains("李"));
var users4 = await userRepo.FindAsync(u => u.CreatedAt >= DateTime.Today);
```

---

## 🔍 常见问题

### Q1: 生成的代码在哪里？
**A**: 生成的代码在`obj/Debug/net9.0/generated/`目录中，但你不需要关心它们。Sqlx会在编译时自动生成和使用这些代码。

### Q2: 如何调试生成的代码？
**A**: 
1. 在项目文件中添加：
   ```xml
   <PropertyGroup>
     <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   </PropertyGroup>
   ```
2. 重新构建项目
3. 在`obj/Debug/net9.0/generated/Sqlx.Generator/`目录查看生成的代码

### Q3: 支持异步吗？
**A**: 完全支持！所有查询方法都可以返回`Task<T>`或`Task<List<T>>`。

### Q4: 如何处理NULL值？
**A**: 使用C#的nullable类型：
```csharp
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;  // NOT NULL
    public string? MiddleName { get; set; }            // NULLABLE
    public int Age { get; set; }                       // NOT NULL
    public DateTime? LastLogin { get; set; }           // NULLABLE
}
```

### Q5: 性能如何？
**A**: Sqlx在关键场景中性能优于Dapper：
- SelectSingle: **+5% faster**
- BatchInsert: **+47% faster**，内存**-50%**
- SelectList: -8% (可接受)

### Q6: 支持事务吗？
**A**: 支持！使用ADO.NET的标准事务API：
```csharp
using var transaction = connection.BeginTransaction();
try
{
    await userRepo.InsertAsync(user1);
    await userRepo.InsertAsync(user2);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## 📚 下一步

- 📖 [完整文档](README.md)
- 🎨 [高级功能](README.md#高级功能)
- 🏆 [性能对比](README.md#性能对比)
- 📊 [最佳实践](PROGRESS.md)
- 🔧 [GitHub仓库](https://github.com/Cricle/Sqlx)

---

## 🎊 恭喜！

你已经掌握了Sqlx的核心用法！现在可以开始构建高性能、类型安全的数据访问层了。🚀

**记住：**
- ✅ 使用`ICrudRepository<TEntity, TKey>`获得零样板代码
- ✅ 使用`Expression<Func<T, bool>>`获得类型安全查询
- ✅ 使用`[BatchOperation]`获得高性能批量操作
- ✅ 关注业务逻辑，让Sqlx处理SQL生成和优化

Happy coding! 🎉

