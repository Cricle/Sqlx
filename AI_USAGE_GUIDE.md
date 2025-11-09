# Sqlx AI 使用指南 - 为 AI 助手准备的完整文档

> **版本**: 2.0
> **更新日期**: 2025-11-08
> **目标**: 帮助 AI 助手快速理解 Sqlx 并正确使用其功能

---

## 📋 目录

1. [项目概览](#项目概览)
2. [核心概念](#核心概念)
3. [完整功能列表](#完整功能列表)
4. [代码示例](#代码示例)
5. [重要注意事项](#重要注意事项)
6. [常见错误](#常见错误)
7. [故障排查](#故障排查)

---

## 项目概览

### 是什么？

**Sqlx** 是一个高性能、类型安全的 .NET 数据访问库，通过**编译时源代码生成**提供接近原生 ADO.NET 的性能。

### 核心优势

| 特性 | 说明 | 优势 |
|------|------|------|
| **编译时生成** | 使用 Roslyn 源生成器在编译时生成代码 | 零运行时开销、零反射 |
| **类型安全** | 编译时验证 SQL 参数和类型 | 提前发现错误 |
| **跨数据库** | 统一 API 支持 4 种数据库 | 写一次，到处运行 |
| **70+ 占位符** | 强大的 SQL 模板系统 | 减少重复代码 |
| **高性能** | 接近 ADO.NET，比 EF Core 快 52% | 生产就绪 |

### 支持的数据库

- ✅ **SQLite** - 生产就绪
- ✅ **PostgreSQL** - 生产就绪
- ✅ **MySQL** - 生产就绪
- ✅ **SQL Server** - 生产就绪

---

## 核心概念

### 1. 源代码生成器

```
用户定义接口 + [SqlTemplate]
         ↓
编译时 Roslyn 源生成器
         ↓
自动生成 partial 类实现
         ↓
编译为高性能的 ADO.NET 代码
```

**关键点**：
- 所有代码在编译时生成
- 无反射、无动态代码
- 支持 AOT (Ahead-of-Time) 编译

### 2. 必需的特性标记

| 特性 | 位置 | 用途 | 示例 |
|------|------|------|------|
| `[SqlDefine]` | 接口/类 | 指定数据库方言 | `[SqlDefine(SqlDefineTypes.SQLite)]` |
| `[RepositoryFor]` | 类 | 标记实体类型 | `[RepositoryFor(typeof(User))]` |
| `[SqlTemplate]` | 方法 | 定义 SQL 模板 | `[SqlTemplate("SELECT {{columns}} FROM {{table}}")]` |
| `[TableName]` | 实体类 | 指定表名 | `[TableName("users")]` |

### 3. 占位符系统

**基础占位符**（7个核心）:

```csharp
{{columns}}    // 自动列名列表: id, name, age
{{table}}      // 表名: users
{{values}}     // INSERT 值: (@name, @age)
{{set}}        // UPDATE SET: name=@name, age=@age
{{where}}      // WHERE 子句（表达式树）
{{orderby}}    // 排序: ORDER BY created_at DESC
{{limit}}      // 分页限制（跨数据库）
{{offset}}     // 分页偏移（跨数据库）
```

**方言占位符**（跨数据库兼容）:

```csharp
{{bool_true}}          // SQLite: 1, PostgreSQL: true
{{bool_false}}         // SQLite: 0, PostgreSQL: false
{{current_timestamp}}  // 跨数据库当前时间
```

**聚合函数**（5个）:

```csharp
{{count}}           // COUNT(*)
{{sum balance}}     // SUM(balance)
{{avg age}}         // AVG(age)
{{max balance}}     // MAX(balance)
{{min age}}         // MIN(age)
```

**字符串函数**（8个）:

```csharp
{{like @pattern}}              // LIKE 查询
{{in @ids}}                    // IN 查询
{{between @min, @max}}         // BETWEEN 查询
{{coalesce email, 'default'}}  // COALESCE
{{distinct age}}               // DISTINCT
{{concat name, email}}         // 字符串拼接
{{upper name}}                 // 转大写
{{lower name}}                 // 转小写
```

**复杂查询**（10+）:

```csharp
{{join --type inner --table orders --on user_id = users.id}}  // JOIN
{{groupby category}}                                           // GROUP BY
{{having --condition 'COUNT(*) > 10'}}                        // HAVING
{{case --when ... --then ... --else ...}}                     // CASE
{{union}}                                                      // UNION
{{row_number --partition_by cat --order_by price}}           // 窗口函数
```

---

## 完整功能列表

### ✅ 基础功能

#### 1. CRUD 操作

```csharp
// 查询单个实体
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// 查询多个实体
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 插入（返回自增 ID）
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age)")]
[ReturnInsertedId]
Task<long> InsertAsync(string name, int age);

// 更新
[SqlTemplate("UPDATE {{table}} SET name = @name WHERE id = @id")]
Task<int> UpdateAsync(long id, string name);

// 删除
[SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
Task<int> DeleteAsync(long id);
```

#### 2. 返回类型支持

| 返回类型 | 用途 | 示例 |
|---------|------|------|
| `Task<T?>` | 单个实体（可能为 null） | `Task<User?>` |
| `Task<List<T>>` | 实体列表 | `Task<List<User>>` |
| `Task<int>` | 影响行数 | INSERT/UPDATE/DELETE |
| `Task<long>` | 计数/ID | COUNT/自增 ID |
| `Task<bool>` | 布尔结果 | EXISTS |
| `Task<Dictionary<string, object?>>` | 动态单行 | 复杂查询 |
| `Task<List<Dictionary<string, object?>>>` | 动态多行 | 复杂查询 |

### ✅ 高级功能

#### 1. 表达式树查询

```csharp
// 使用 C# Lambda 表达式代替 SQL WHERE
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 使用示例
await repo.QueryAsync(u => u.Age >= 18 && u.Balance > 5000);
// 生成: WHERE age >= 18 AND balance > 5000

await repo.QueryAsync(u => u.Name.Contains("张"));
// 生成: WHERE name LIKE '%张%'
```

**支持的表达式**:
- ✅ 比较: `==`, `!=`, `>`, `>=`, `<`, `<=`
- ✅ 逻辑: `&&`, `||`, `!`
- ✅ NULL: `== null`, `!= null`
- ✅ 字符串: `Contains`, `StartsWith`, `EndsWith`
- ❌ 方法调用（除字符串方法）
- ❌ 本地变量引用

#### 2. 批量操作

```csharp
// 批量插入（自动分批）
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 1000)]
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 使用
var users = Enumerable.Range(1, 10000)
    .Select(i => new User { Name = $"User{i}", Age = 20 + i })
    .ToList();
await repo.BatchInsertAsync(users);
// 自动分为 10 批，每批 1000 条
```

**特点**:
- 自动处理数据库参数限制
- 自动分批（超过 MaxBatchSize）
- 返回总影响行数
- 性能提升 25 倍

#### 3. 软删除

```csharp
// 实体类标记
[TableName("products")]
[SoftDelete(FlagColumn = "is_deleted")]
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; }
}

// 仓储方法
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}}")]
Task<List<Product>> GetAllAsync();

[SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_true}} WHERE id = @id")]
Task<int> SoftDeleteAsync(long id);

// 包含已删除的查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
[IncludeDeleted]
Task<Product?> GetByIdIncludingDeletedAsync(long id);
```

#### 4. 审计字段

```csharp
// 实体类标记
[TableName("orders")]
[AuditFields(
    CreatedAtColumn = "created_at",
    CreatedByColumn = "created_by",
    UpdatedAtColumn = "updated_at",
    UpdatedByColumn = "updated_by")]
public class Order
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// INSERT 和 UPDATE 时自动设置审计字段
```

#### 5. 乐观锁

```csharp
// 实体类标记
[TableName("accounts")]
public class Account
{
    public long Id { get; set; }
    public decimal Balance { get; set; }

    [ConcurrencyCheck]
    public long Version { get; set; }
}

// 更新时自动检查版本号
[SqlTemplate("UPDATE {{table}} SET balance = @balance, version = version + 1 WHERE id = @id AND version = @version")]
Task<int> UpdateBalanceAsync(long id, decimal balance, long version);
```

#### 6. 事务支持

```csharp
// 仓储自动支持事务
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

#### 7. 拦截器（Partial Methods）

```csharp
public partial class UserRepository
{
    // SQL 执行前
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[{operationName}] SQL: {command.CommandText}");
    }

    // SQL 执行后
    partial void OnExecuted(string operationName, DbCommand command, long elapsedMs)
    {
        Console.WriteLine($"[{operationName}] 完成，耗时: {elapsedMs}ms");
    }

    // SQL 执行失败
    partial void OnExecuteFail(string operationName, DbCommand command, Exception ex)
    {
        Console.WriteLine($"[{operationName}] 失败: {ex.Message}");
    }
}
```

---

## 代码示例

### 示例 1: 最小完整示例

```csharp
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

// 1. 定义实体
[TableName("users")]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// 2. 定义接口（在单独的文件中）
// IUserRepository.cs
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

// 3. 实现类（在单独的文件中）
// UserRepository.cs
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 使用
await using var conn = new SqliteConnection("Data Source=app.db");
await conn.OpenAsync();

var repo = new UserRepository(conn);
long id = await repo.InsertAsync("Alice", 25);
var user = await repo.GetByIdAsync(id);

Console.WriteLine($"{user?.Name}, {user?.Age}岁");
```

### 示例 2: 使用占位符

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository
{
    // 基础占位符
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}} {{limit}}")]
    Task<List<User>> GetRecentUsersAsync(int? limit = 10);

    // 聚合函数占位符
    [SqlTemplate("SELECT {{count}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<long> CountActiveUsersAsync();

    // 表达式树
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
    Task<List<User>> FindUsersAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

    // 批量操作
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
    [BatchOperation(MaxBatchSize = 500)]
    Task<int> BatchInsertAsync(IEnumerable<User> users);
}
```

### 示例 3: 跨数据库（统一方言）

```csharp
// 定义统一接口（只写一次）
public partial interface IUnifiedUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveUsersAsync();
}

// SQLite 实现
[RepositoryFor(typeof(IUnifiedUserRepository), Dialect = "SQLite", TableName = "users")]
public partial class SQLiteUserRepository(DbConnection connection) : IUnifiedUserRepository { }

// PostgreSQL 实现（完全相同的代码）
[RepositoryFor(typeof(IUnifiedUserRepository), Dialect = "PostgreSql", TableName = "users")]
public partial class PostgreSQLUserRepository(DbConnection connection) : IUnifiedUserRepository { }

// MySQL 实现
[RepositoryFor(typeof(IUnifiedUserRepository), Dialect = "MySql", TableName = "users")]
public partial class MySQLUserRepository(DbConnection connection) : IUnifiedUserRepository { }

// SQL Server 实现
[RepositoryFor(typeof(IUnifiedUserRepository), Dialect = "SqlServer", TableName = "users")]
public partial class SqlServerUserRepository(DbConnection connection) : IUnifiedUserRepository { }
```

**生成的 SQL 自动适配**:

| 占位符 | SQLite | PostgreSQL | MySQL | SQL Server |
|--------|--------|-----------|-------|------------|
| `{{table}}` | `[users]` | `"users"` | `` `users` `` | `[users]` |
| `{{bool_true}}` | `1` | `true` | `1` | `1` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `GETDATE()` |

---

## 重要注意事项

### ⚠️ 关键限制

#### 1. 接口和实现必须分文件

```csharp
// ❌ 错误：同一文件
// UserRepository.cs
public interface IUserRepository { }
public partial class UserRepository : IUserRepository { }  // 不会生成代码

// ✅ 正确：分开文件
// IUserRepository.cs
public interface IUserRepository { }

// UserRepository.cs
public partial class UserRepository : IUserRepository { }  // ✅ 会生成代码
```

**原因**: 源生成器在编译时运行，无法看到正在编译的同一文件中的定义。

#### 2. 必须使用 DbConnection（不是 IDbConnection）

```csharp
// ❌ 错误：IDbConnection 不支持异步
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

// ✅ 正确：DbConnection 支持异步
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

#### 3. SQL 参数必须与方法参数匹配

```csharp
// ✅ 正确：参数名匹配
[SqlTemplate("SELECT * FROM users WHERE id = @id AND age = @age")]
Task<User?> GetUserAsync(long id, int age);

// ❌ 错误：SQL 中的参数找不到
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetUserAsync(long id);  // 参数名不匹配
```

#### 4. 实体类必须使用公共属性

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

#### 5. CancellationToken 参数命名

```csharp
// ✅ 正确：参数名必须包含 "cancellation" 或 "token"
Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

// ❌ 错误：不会被识别为 CancellationToken
Task<User?> GetByIdAsync(long id, CancellationToken c = default);
```

### 🔒 安全注意事项

#### 1. 防止 SQL 注入

```csharp
// ✅ 安全：使用参数化查询
[SqlTemplate("SELECT * FROM users WHERE name = @name")]
Task<List<User>> FindByNameAsync(string name);

// ✨ Sqlx 所有 @参数 都会自动参数化，天然防 SQL 注入
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

// 推荐：使用 [TableName] 明确指定
[TableName("users")]
public class User { }
```

---

## 常见错误

### 错误 1: 生成的代码找不到

**症状**:
```
error CS0535: 'UserRepository' does not implement interface member 'IUserRepository.GetByIdAsync(long)'
```

**原因**:
1. 接口和实现在同一文件
2. 缺少 `[SqlDefine]` 或 `[RepositoryFor]`
3. 项目未重新编译

**解决方案**:
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

### 错误 2: SQL 参数找不到

**症状**:
```
error: SQL template contains parameter @userId but method does not have matching parameter
```

**解决方案**:
```csharp
// ❌ 错误
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetByIdAsync(long id);

// ✅ 正确
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

### 错误 3: 异步方法不支持

**症状**:
```
error: Cannot use IDbConnection with async methods
```

**解决方案**:
```csharp
// ❌ 错误
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

// ✅ 正确
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

---

## 故障排查

### 查看生成的代码

生成的代码位置：
```
项目目录/obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.CSharpGenerator/
    └── UserRepository.g.cs
```

或在 IDE 中：
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
- [ ] SQL 参数名是否与方法参数匹配？
- [ ] 返回类型是否正确？
- [ ] 是否重新编译了项目？

---

## 性能最佳实践

### 1. 使用批量操作

```csharp
// ❌ 慢：循环插入（N 次数据库往返）
foreach (var user in users)
{
    await repo.InsertAsync(user);
}

// ✅ 快：批量插入（1-2 次数据库往返）
await repo.BatchInsertAsync(users);
```

### 2. 使用 LIMIT 限制结果集

```csharp
// ❌ 慢：查询所有数据
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// ✅ 快：限制结果数量
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit}}")]
Task<List<User>> GetAllAsync(int? limit = 100);
```

### 3. 只查询需要的列

```csharp
// ❌ 慢：SELECT *
[SqlTemplate("SELECT * FROM users")]
Task<List<User>> GetAllAsync();

// ✅ 快：只查询需要的列
[SqlTemplate("SELECT {{columns --include Id, Name}} FROM {{table}}")]
Task<List<User>> GetNamesAsync();
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

---

## 总结

### Sqlx 的核心价值

1. ✅ **极致性能** - 接近原生 ADO.NET（1.05-1.13x）
2. ✅ **类型安全** - 编译时验证，零运行时错误
3. ✅ **零反射** - 所有代码编译时生成
4. ✅ **完全异步** - 真正的异步 I/O
5. ✅ **简单易用** - 学习曲线极低
6. ✅ **跨数据库** - 一套代码，4 种数据库

### 适用场景

✅ **推荐使用**:
- 性能要求高的应用
- 需要完全控制 SQL 的场景
- 微服务架构
- AOT 部署
- CRUD 为主的应用

❌ **不推荐使用**:
- 需要复杂 ORM 功能（导航属性、延迟加载）
- 团队不熟悉 SQL
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
{{where}}        // WHERE 子句（表达式树）
{{limit}}        // LIMIT
{{offset}}       // OFFSET
{{orderby col}}  // ORDER BY
{{set}}          // UPDATE SET
{{batch_values}} // 批量 VALUES

// 常用特性
[ReturnInsertedId]    // 返回自增 ID
[BatchOperation]      // 批量操作
[ExpressionToSql]     // 表达式参数
[IncludeDeleted]      // 包含软删除

// 拦截器
partial void OnExecuting(string op, DbCommand cmd);
partial void OnExecuted(string op, DbCommand cmd, long ms);
partial void OnExecuteFail(string op, DbCommand cmd, Exception ex);
```

---

**开始使用 Sqlx，让数据访问回归简单！** 🚀

**相关资源**:
- [GitHub 仓库](https://github.com/Cricle/Sqlx)
- [完整教程](TUTORIAL.md)
- [API 参考](docs/API_REFERENCE.md)
- [占位符参考](docs/PLACEHOLDER_REFERENCE.md)
- [最佳实践](docs/BEST_PRACTICES.md)


