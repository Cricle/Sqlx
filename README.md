# Sqlx - 高性能 .NET 数据访问库

<div align="center">

[![NuGet](https://img.shields.io/nuget/v/Sqlx.svg)](https://www.nuget.org/packages/Sqlx/)
[![Tests](https://img.shields.io/badge/tests-1412%20passed-brightgreen)](tests/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](#)

**极致性能 · 类型安全 · 完全异步 · 零配置**

[快速开始](#-快速开始) · [特性](#-核心特性) · [性能对比](#-性能对比) · [文档](docs/) · [示例](samples/)

</div>

---

## 💡 什么是 Sqlx？

Sqlx 是一个**高性能、类型安全的 .NET 数据访问库**，通过**源代码生成器**在编译时生成数据访问代码，提供接近原生 ADO.NET 的性能，同时保持极简的 API 设计。

### 为什么选择 Sqlx？

| 特性 | Sqlx | Dapper | EF Core |
|-----|------|--------|---------|
| 性能 | ⚡⚡⚡⚡⚡ 接近ADO.NET | ⚡⚡⚡⚡ 优秀 | ⚡⚡⚡ 良好 |
| 类型安全 | ✅ 编译时 | ⚠️ 运行时 | ✅ 编译时 |
| 学习曲线 | 📈 极低 | 📈 低 | 📈📈 中等 |
| SQL控制 | ✅ 完全控制 | ✅ 完全控制 | ⚠️ 有限 |
| 代码生成 | ✅ 编译时 | ❌ 无 | ✅ 运行时 |
| AOT支持 | ✅ 完整 | ✅ 完整 | ⚠️ 有限 |
| GC压力 | ⚡ 极低 | ⚡ 低 | ⚡⚡ 中等 |
| 多数据库 | ✅ 5+ | ✅ 多种 | ✅ 多种 |

---

## ⚡ 快速开始

### 安装

```bash
dotnet add package Sqlx
```

### 3步开始使用

#### 1️⃣ 定义实体

```csharp
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public decimal Balance { get; set; }
}
```

#### 2️⃣ 定义仓储接口

```csharp
using Sqlx;
using Sqlx.Annotations;

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository
{
    // 使用 {{占位符}} 实现跨数据库SQL
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    
    [SqlTemplate("INSERT INTO users (name, age, balance) VALUES (@name, @age, @balance)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, decimal balance, CancellationToken ct = default);
    
    [SqlTemplate("SELECT {{columns}} FROM users WHERE age >= @minAge {{limit}}")]
    Task<List<User>> GetAdultsAsync(int minAge = 18, int? limit = null, CancellationToken ct = default);
    
    // 批量插入，自动处理参数限制
    [SqlTemplate("INSERT INTO users (name, age, balance) VALUES {{batch_values}}")]
    [BatchOperation(MaxBatchSize = 500)]
    Task<int> BatchInsertAsync(IEnumerable<User> users, CancellationToken ct = default);
}

// 部分实现类 - 源生成器会生成实际代码
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

#### 3️⃣ 使用仓储

```csharp
using System.Data.Common;
using Microsoft.Data.Sqlite;

await using DbConnection conn = new SqliteConnection("Data Source=app.db");
await conn.OpenAsync();

var repo = new UserRepository(conn);

// 插入用户
long userId = await repo.InsertAsync("Alice", 25, 1000.50m);

// 查询用户
var user = await repo.GetByIdAsync(userId);
Console.WriteLine($"{user.Name}, {user.Age}岁, 余额: ${user.Balance}");

// 批量操作
var users = new[] {
    new User { Name = "Bob", Age = 30, Balance = 2000m },
    new User { Name = "Carol", Age = 28, Balance = 1500m }
};
await repo.BatchInsertAsync(users);

// 条件查询
var adults = await repo.GetAdultsAsync(minAge: 18, limit: 10);
```

---

## 🎯 核心特性

### 1. ⚡ 极致性能

通过**编译时源代码生成**，Sqlx 生成的代码接近手写 ADO.NET 的性能：

```
BenchmarkDotNet=v0.13.12, OS=Windows 11
Intel Core i7-12700H, 1 CPU, 20 logical cores

| Method              | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated |
|-------------------- |----------:|--------:|---------:|------:|-------:|----------:|
| ADO.NET (baseline)  | 162.0 μs  | 2.1 μs  | 1.9 μs   | 1.00  | 2.44   | 10.1 KB   |
| Sqlx                | 170.2 μs  | 1.8 μs  | 1.6 μs   | 1.05  | 2.44   | 10.2 KB   |
| Dapper              | 182.5 μs  | 2.3 μs  | 2.0 μs   | 1.13  | 2.68   | 11.3 KB   |
| EF Core             | 245.8 μs  | 3.2 μs  | 2.8 μs   | 1.52  | 4.88   | 20.6 KB   |
```

### 2. 🛡️ 类型安全

**编译时验证**，发现问题更早：

```csharp
// ✅ 编译时检查参数类型
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);  // ✅ 正确

// ❌ 编译错误：找不到参数
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetByIdAsync(long id);  // ❌ 编译器会报错

// ✅ Nullable支持
Task<User?> GetByIdAsync(long id);  // 返回值可能为null
```

### 3. 🚀 完全异步

真正的异步I/O，不是`Task.FromResult`包装：

```csharp
public partial class UserRepository(DbConnection connection) : IUserRepository 
{
    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, age FROM users WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        
        // 真正的异步I/O
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new User 
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2)
            };
        }
        return null;
    }
}
```

**自动支持 CancellationToken**：

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var users = await repo.GetUsersAsync(cancellationToken: cts.Token);
```

### 4. 📝 强大的占位符系统

跨数据库SQL模板，一次编写，多数据库运行：

| 占位符 | 说明 | SQLite | MySQL | PostgreSQL | SQL Server |
|--------|------|--------|-------|------------|------------|
| `{{columns}}` | 自动列选择 | `id, name, age` | `id, name, age` | `id, name, age` | `id, name, age` |
| `{{table}}` | 表名 | `users` | `users` | `users` | `users` |
| `{{values}}` | VALUES子句 | `(@id, @name)` | `(@id, @name)` | `(@id, @name)` | `(@id, @name)` |
| `{{where}}` | WHERE条件 | 表达式树 | 表达式树 | 表达式树 | 表达式树 |
| `{{limit}}` | 分页限制 | `LIMIT @limit` | `LIMIT @limit` | `LIMIT @limit` | `TOP (@limit)` |
| `{{offset}}` | 分页偏移 | `OFFSET @offset` | `OFFSET @offset` | `OFFSET @offset` | `OFFSET @offset ROWS` |
| `{{orderby}}` | 排序 | `ORDER BY created_at DESC` | `ORDER BY created_at DESC` | `ORDER BY created_at DESC` | `ORDER BY created_at DESC` |
| `{{batch_values}}` | 批量插入 | 自动生成 | 自动生成 | 自动生成 | 自动生成 |

**示例**：

```csharp
// 同一个SQL模板
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge {{orderby age}} {{limit}} {{offset}}")]
Task<List<User>> GetUsersAsync(int minAge, int? limit = null, int? offset = null);

// SQLite: SELECT id, name, age FROM users WHERE age >= @minAge ORDER BY age LIMIT @limit OFFSET @offset
// MySQL:  SELECT id, name, age FROM users WHERE age >= @minAge ORDER BY age LIMIT @limit OFFSET @offset  
// SQL Server: SELECT TOP (@limit) id, name, age FROM users WHERE age >= @minAge ORDER BY age OFFSET @offset ROWS
```

### 5. 🌳 表达式树支持

使用C#表达式代替SQL WHERE子句：

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 使用
var users = await repo.QueryAsync(u => u.Age >= 18 && u.Balance > 1000);
// 生成: SELECT id, name, age, balance FROM users WHERE age >= 18 AND balance > 1000
```

**支持的表达式**：
- 比较: `==`, `!=`, `>`, `>=`, `<`, `<=`
- 逻辑: `&&`, `||`, `!`
- 字符串: `Contains`, `StartsWith`, `EndsWith`
- 集合: `Any`, `All`
- NULL检查: `== null`, `!= null`

### 6. 🔄 智能批量操作

自动处理数据库参数限制，智能分批：

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]  // 自动分批，每批最多500条
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 插入10000条数据 - 自动分为20批
await repo.BatchInsertAsync(GenerateUsers(10000));
```

### 7. 🗄️ 多数据库支持

一套API，支持5大主流数据库：

```csharp
// SQLite
[SqlDefine(SqlDefineTypes.SQLite)]
public interface ISqliteRepo : IUserRepository { }

// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
public interface IMySqlRepo : IUserRepository { }

// PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSql)]
public interface IPostgreSqlRepo : IUserRepository { }

// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]
public interface ISqlServerRepo : IUserRepository { }

// Oracle
[SqlDefine(SqlDefineTypes.Oracle)]
public interface IOracleRepo : IUserRepository { }
```

### 8. 🎯 CRUD接口

开箱即用的通用CRUD操作：

```csharp
// 实现 ICrudRepository 接口，自动获得8个标准方法
public interface IUserRepository : ICrudRepository<User, long> { }

// 自动生成的方法：
// - GetByIdAsync(id)
// - GetAllAsync(limit, offset)
// - InsertAsync(entity)
// - UpdateAsync(entity)
// - DeleteAsync(id)
// - CountAsync()
// - ExistsAsync(id)
// - BatchInsertAsync(entities)
```

### 9. 🔍 返回插入的ID或实体

```csharp
// 返回自增ID
[SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
[ReturnInsertedId]
Task<long> InsertAsync(string name, int age);

// 返回完整实体（包含自增ID和默认值）
[SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
[ReturnInsertedEntity]
Task<User> InsertAndReturnAsync(string name, int age);
```

### 10. 🔐 事务支持

```csharp
await using var tx = await connection.BeginTransactionAsync();
repo.Transaction = tx;

try
{
    await repo.InsertAsync("Alice", 25, 1000m);
    await repo.UpdateBalanceAsync(userId, 2000m);
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

### 11. 🎣 拦截器

监控和自定义SQL执行：

```csharp
public partial class UserRepository
{
    // SQL执行前
    partial void OnExecuting(string operationName, DbCommand command)
    {
        _logger.LogDebug("[{Op}] SQL: {Sql}", operationName, command.CommandText);
    }
    
    // SQL执行后
    partial void OnExecuted(string operationName, DbCommand command, long elapsedMilliseconds)
    {
        _logger.LogInformation("[{Op}] 完成，耗时: {Ms}ms", operationName, elapsedMilliseconds);
    }
    
    // SQL执行失败
    partial void OnExecuteFail(string operationName, DbCommand command, Exception exception)
    {
        _logger.LogError(exception, "[{Op}] 失败", operationName);
    }
}
```

---

## 📊 性能对比

### SELECT 1000行

```
| Method      | Mean      | Ratio | Allocated |
|------------ |----------:|------:|----------:|
| ADO.NET     | 162.0 μs  | 1.00  | 10.1 KB   |
| Sqlx        | 170.2 μs  | 1.05  | 10.2 KB   | ⭐
| Dapper      | 182.5 μs  | 1.13  | 11.3 KB   |
| EF Core     | 245.8 μs  | 1.52  | 20.6 KB   |
```

### INSERT 100行

```
| Method      | Mean      | Ratio | Allocated |
|------------ |----------:|------:|----------:|
| ADO.NET     | 2.01 ms   | 1.00  | 8.5 KB    |
| Sqlx        | 2.18 ms   | 1.08  | 9.2 KB    | ⭐
| Dapper      | 2.35 ms   | 1.17  | 12.1 KB   |
| EF Core     | 3.82 ms   | 1.90  | 28.4 KB   |
```

### 批量插入1000行

```
| Method         | Mean      | Allocated |
|--------------- |----------:|----------:|
| Sqlx Batch     | 58.2 ms   | 45.2 KB   | ⭐ 最快
| Dapper Loop    | 225.8 ms  | 125.8 KB  |
| EF Core Bulk   | 185.6 ms  | 248.5 KB  |
```

**结论**：Sqlx 在所有场景下都接近原生 ADO.NET 性能，远超传统 ORM。

---

## 📚 文档

- [快速开始指南](docs/QUICK_START_GUIDE.md) - 5分钟上手
- [API参考](docs/API_REFERENCE.md) - 完整API文档
- [占位符指南](docs/PLACEHOLDERS.md) - 占位符详解
- [表达式树](docs/EXPRESSION_TO_SQL.md) - 表达式转SQL
- [高级特性](docs/ADVANCED_FEATURES.md) - SoftDelete、AuditFields等
- [最佳实践](docs/BEST_PRACTICES.md) - 推荐用法
- [性能优化](docs/PERFORMANCE_OPTIMIZATION.md) - 性能调优
- [多数据库支持](docs/MULTI_DATABASE_SUPPORT.md) - 数据库方言

---

## 🎯 示例项目

### [FullFeatureDemo](samples/FullFeatureDemo/)

完整演示所有Sqlx功能：
- ✅ 基础CRUD操作
- ✅ 批量操作
- ✅ 事务支持
- ✅ 占位符使用
- ✅ 表达式树查询
- ✅ 高级SQL（JOIN、聚合、分页）

```bash
cd samples/FullFeatureDemo
dotnet run
```

### [TodoWebApi](samples/TodoWebApi/)

真实Web API示例：
- ✅ ASP.NET Core集成
- ✅ RESTful API设计
- ✅ 搜索和过滤
- ✅ 批量更新

```bash
cd samples/TodoWebApi
dotnet run
# 访问 http://localhost:5000
```

---

## 🏗️ 高级特性

### SoftDelete（软删除）

```csharp
[SoftDelete(FlagColumn = "is_deleted")]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; }
}

// 删除操作会设置标志而非真删除
await repo.DeleteAsync(productId);
// UPDATE products SET is_deleted = 1 WHERE id = @id

// 默认查询会过滤已删除数据
var products = await repo.GetAllAsync();
// SELECT * FROM products WHERE is_deleted = 0

// 如需包含已删除数据
[IncludeDeleted]
Task<List<Product>> GetAllIncludingDeletedAsync();
```

### AuditFields（审计字段）

```csharp
[AuditFields(
    CreatedAtColumn = "created_at",
    UpdatedAtColumn = "updated_at",
    CreatedByColumn = "created_by",
    UpdatedByColumn = "updated_by")]
public class Order
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
}

// 插入和更新时自动设置审计字段
await repo.InsertAsync(order);
// created_at, created_by 自动设置

await repo.UpdateAsync(order);
// updated_at, updated_by 自动设置
```

### ConcurrencyCheck（乐观锁）

```csharp
public class Account
{
    public long Id { get; set; }
    public decimal Balance { get; set; }
    
    [ConcurrencyCheck]
    public long Version { get; set; }
}

// 更新时会自动检查版本号
await repo.UpdateAsync(account);
// UPDATE accounts SET balance = @balance, version = version + 1 
// WHERE id = @id AND version = @version
```

---

## 🔧 配置

### 基础配置

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}

// Startup.cs / Program.cs
services.AddScoped<DbConnection>(sp => 
{
    var conn = new SqliteConnection(Configuration.GetConnectionString("DefaultConnection"));
    conn.Open();
    return conn;
});

services.AddScoped<IUserRepository, UserRepository>();
```

### 依赖注入

```csharp
public class UserService
{
    private readonly IUserRepository _userRepo;
    
    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }
    
    public async Task<User?> GetUserAsync(long id)
    {
        return await _userRepo.GetByIdAsync(id);
    }
}
```

---

## ⚙️ 支持的.NET版本

- ✅ .NET 8.0
- ✅ .NET 9.0
- ✅ .NET Standard 2.0 (库)

---

## 🗄️ 支持的数据库

| 数据库 | 版本 | 状态 | 特性支持 |
|--------|------|------|----------|
| SQLite | 3.x | ✅ 完全支持 | 全部特性 |
| MySQL | 5.7+ / 8.0+ | ✅ 完全支持 | 全部特性 |
| PostgreSQL | 12+ | ✅ 完全支持 | 全部特性 |
| SQL Server | 2016+ | ✅ 完全支持 | 全部特性 |
| Oracle | 12c+ | ✅ 完全支持 | 全部特性 |

---

## 🤝 贡献

欢迎贡献！请查看 [贡献指南](CONTRIBUTING.md)。

---

## 📄 许可证

[MIT License](LICENSE.txt)

---

## 🌟 Star History

如果Sqlx对您有帮助，请给个Star⭐！

---

## 📞 联系方式

- 🐛 问题反馈: [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
- 💬 讨论交流: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)
- 📧 邮件: [项目联系邮箱]

---

<div align="center">

**Sqlx - 让数据访问回归简单，让性能接近极致！** 🚀

Made with ❤️ by the Sqlx Team

</div>
