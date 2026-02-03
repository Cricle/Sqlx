# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](#)
[![LTS](https://img.shields.io/badge/LTS-.NET%2010-green.svg)](#)
[![Tests](https://img.shields.io/badge/tests-2076%20passing-brightgreen.svg)](#)
[![AOT](https://img.shields.io/badge/AOT-ready-blue.svg)](#)

高性能、AOT 友好的 .NET 数据库访问库。使用源生成器在编译时生成代码，零运行时反射，完全支持 Native AOT。

## 核心特性

- **🚀 高性能** - 比 Dapper.AOT 快 1.5-2.9%，最低 GC 压力（Gen1 GC 是 FreeSql 的 1/13）
- **⚡ 零反射** - 编译时源生成，运行时无反射开销
- **🎯 类型安全** - 编译时验证 SQL 模板和表达式
- **🌐 多数据库** - SQLite、PostgreSQL、MySQL、SQL Server、Oracle、DB2
- **📦 AOT 就绪** - 完全支持 Native AOT，通过 2076 个单元测试
- **🔧 LINQ 支持** - IQueryable 接口，支持 Where/Select/OrderBy/Join 等
- **💾 智能缓存** - SqlQuery\<T\> 泛型缓存，自动注册 EntityProvider
- **🔍 自动发现** - 源生成器自动发现 SqlQuery\<T\> 和 SqlTemplate 中的实体类型

## 快速开始

```bash
dotnet add package Sqlx
```

```csharp
// 1. 定义实体（支持 class、record、struct）
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 也支持 record 类型
[Sqlx, TableName("users")]
public record UserRecord(long Id, string Name, int Age);

// 也支持 struct 类型
[Sqlx, TableName("users")]
public struct UserStruct
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 2. 定义仓储接口
public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
}

// 3. 实现仓储（代码自动生成）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 使用
await using var conn = new SqliteConnection("Data Source=app.db");
var repo = new UserRepository(conn);
var adults = await repo.GetAdultsAsync(18);
```

**重要说明：** Sqlx 中有两个不同的 `SqlTemplate`：
- **`[SqlTemplate]` 特性** (`Sqlx.Annotations`) - 用于标注接口方法，定义 SQL 模板
- **`SqlTemplate` 类** (`Sqlx`) - 运行时类，用于调试查看生成的 SQL

```csharp
using Sqlx;                    // SqlTemplate 类
using Sqlx.Annotations;        // [SqlTemplate] 特性

public interface IUserRepository
{
    // [SqlTemplate] 特性 - 标注方法执行查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    // SqlTemplate 类 - 返回类型用于调试（不执行查询）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    SqlTemplate GetByIdSql(long id);
}

// 调试使用
var template = repo.GetByIdSql(123);
Console.WriteLine($"SQL: {template.Sql}");
```

## SQL 模板占位符

占位符自动适配不同数据库方言：

| 占位符 | 说明 | 示例输出 |
|--------|------|---------|
| `{{table}}` | 表名（带方言引号） | `"users"` (PostgreSQL) |
| `{{columns}}` | 所有列名 | `id, name, age` |
| `{{columns --exclude Id}}` | 排除指定列 | `name, age` |
| `{{values --exclude Id}}` | 参数占位符 | `@name, @age` |
| `{{values --inline CreatedAt=CURRENT_TIMESTAMP}}` | 内联表达式（INSERT 默认值） | `@name, @age, CURRENT_TIMESTAMP` |
| `{{set --exclude Id}}` | UPDATE SET 子句 | `name = @name` |
| `{{set --inline Version=Version+1}}` | 内联表达式（UPDATE 计算字段） | `name = @name, version = version+1` |
| `{{where --object filter}}` | 对象条件查询 | `(name = @name AND age = @age)` |
| `{{if notnull=param}}...{{/if}}` | 条件包含 | 动态 SQL |

### 内联表达式（Inline Expressions）

内联表达式允许在 SQL 中使用表达式、函数和字面量：

```csharp
// UPDATE 示例：自动递增版本号
[SqlTemplate(@"
    UPDATE {{table}} 
    SET {{set --exclude Id,Version --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
    WHERE id = @id
")]
Task<int> UpdateAsync(long id, string name, string email);
// 生成: UPDATE [users] SET [name] = @name, [email] = @email, 
//       [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id

// INSERT 示例：设置默认值
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns --exclude Id}}) 
    VALUES ({{values --exclude Id --inline Status='pending',CreatedAt=CURRENT_TIMESTAMP}})
")]
Task<int> CreateAsync(string name, string description);
// 生成: INSERT INTO [tasks] ([name], [description], [status], [created_at]) 
//       VALUES (@name, @description, 'pending', CURRENT_TIMESTAMP)
```

**支持的表达式：**
- 算术运算：`Version=Version+1`, `Total=@quantity*@unitPrice`
- SQL 函数：`CreatedAt=CURRENT_TIMESTAMP`, `Email=LOWER(TRIM(Email))`
- 字面量：`Status='pending'`, `Priority=0`, `IsActive=1`
- 复杂表达式：`Result=COALESCE(NULLIF(Value,''),Default)`

**关键特性：**
- ✅ 使用属性名（PascalCase），自动转换为列名
- ✅ 函数内的逗号被正确处理（如 `COALESCE(Status,'pending')`）
- ✅ 支持嵌套函数和深度括号
- ✅ 跨数据库方言自动适配
- ✅ 编译时解析，零运行时开销

**各数据库生成的 SQL：**

| 数据库 | 生成的 SQL |
|--------|-----------|
| SQLite | `SELECT [id], [name] FROM [users] WHERE is_active = 1` |
| PostgreSQL | `SELECT "id", "name" FROM "users" WHERE is_active = true` |
| MySQL | ``SELECT `id`, `name` FROM `users` WHERE is_active = 1`` |

## 内置仓储接口

继承 `ICrudRepository<TEntity, TKey>` 获得 46 个标准方法（26 个查询 + 20 个命令）：

**查询方法（26 个）**：
- 单实体查询：`GetByIdAsync/GetById`, `GetFirstWhereAsync/GetFirstWhere`
- 列表查询：`GetByIdsAsync/GetByIds`, `GetAllAsync/GetAll`, `GetWhereAsync/GetWhere`
- 分页查询：`GetPagedAsync/GetPaged`, `GetPagedWhereAsync/GetPagedWhere`
- 存在性检查：`ExistsByIdAsync/ExistsById`, `ExistsAsync/Exists`
- 计数：`CountAsync/Count`, `CountWhereAsync/CountWhere`
- IQueryable：`AsQueryable()` - 返回 LINQ 查询构建器

**命令方法（20 个）**：
- 插入：`InsertAndGetIdAsync/InsertAndGetId`, `InsertAsync/Insert`, `BatchInsertAsync/BatchInsert`
- 更新：`UpdateAsync/Update`, `UpdateWhereAsync/UpdateWhere`, `BatchUpdateAsync/BatchUpdate`
- **动态更新**：`DynamicUpdateAsync/DynamicUpdate`, `DynamicUpdateWhereAsync/DynamicUpdateWhere`
- 删除：`DeleteAsync/Delete`, `DeleteByIdsAsync/DeleteByIds`, `DeleteWhereAsync/DeleteWhere`, `DeleteAllAsync/DeleteAll`

```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    // 继承 46 个标准方法，无需自定义即可使用
    
    // 自定义方法（仅在需要复杂查询时）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @pattern")]
    Task<List<User>> SearchByNameAsync(string pattern);
}
```

### 动态更新（DynamicUpdate）

使用表达式树动态更新指定字段，无需定义自定义方法：

```csharp
// 更新单个字段
await repo.DynamicUpdateAsync(userId, u => new User { Name = "John" });

// 更新多个字段
await repo.DynamicUpdateAsync(userId, u => new User 
{ 
    Name = "John",
    Age = 30,
    UpdatedAt = DateTime.UtcNow
});

// 使用表达式（递增、计算）
await repo.DynamicUpdateAsync(userId, u => new User 
{ 
    Age = u.Age + 1,
    Score = u.Score * 1.1
});

// 批量更新（带条件）
await repo.DynamicUpdateWhereAsync(
    u => new User { IsActive = false, UpdatedAt = DateTime.UtcNow },
    u => u.LastLoginDate < DateTime.UtcNow.AddDays(-30)
);
```

**优势**：
- ✅ 类型安全 - 编译时验证字段名和类型
- ✅ 灵活 - 支持任意字段组合
- ✅ 高性能 - 编译时生成代码，零反射
- ✅ 表达式支持 - 支持算术运算、函数调用

## 条件占位符

```csharp
// 动态搜索：只在参数有值时添加条件
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} WHERE 1=1 
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}
")]
Task<List<User>> SearchAsync(string? name, int? minAge);
```

## 对象条件查询

使用 `{{where --object}}` 从字典自动生成 WHERE 条件（AOT 兼容）：

```csharp
// 定义查询方法
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> FilterAsync(IReadOnlyDictionary<string, object?> filter);

// 使用：只有非空值会生成条件
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",      // 生成: [name] = @name
    ["Age"] = 25,           // 生成: [age] = @age
    ["Email"] = null        // 忽略（null 值）
};
var users = await repo.FilterAsync(filter);
// 生成: SELECT ... WHERE ([name] = @name AND [age] = @age)

// 空字典返回 1=1（查询所有）
var all = await repo.FilterAsync(new Dictionary<string, object?>());
// 生成: SELECT ... WHERE 1=1
```

## IQueryable 查询构建器

使用标准 LINQ 语法构建类型安全的 SQL 查询：

```csharp
// 基本查询
var query = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10);

var sql = query.ToSql();
// SELECT [id], [name], [age], [is_active] FROM [User] 
// WHERE ([age] >= 18 AND [is_active] = 1) 
// ORDER BY [name] ASC LIMIT 10

// 投影查询（匿名类型，完全 AOT 兼容）
var results = await SqlQuery<User>.ForPostgreSQL()
    .Where(u => u.Name.Contains("test"))
    .Select(u => new { u.Id, u.Name })
    .WithConnection(connection)
    .ToListAsync();

// JOIN 查询
var query = SqlQuery<User>.ForSqlite()
    .Join(SqlQuery<Order>.ForSqlite(),
        u => u.Id,
        o => o.UserId,
        (u, o) => new { u.Name, o.Total })
    .Where(x => x.Total > 100);

// 聚合函数
var maxAge = await SqlQuery<User>.ForSqlite()
    .WithConnection(connection)
    .WithReader(UserResultReader.Default)
    .MaxAsync(u => u.Age);
```

**支持的 LINQ 方法：**
- `Where`, `Select`, `OrderBy`, `ThenBy`, `Take`, `Skip`
- `GroupBy`, `Distinct`, `Join`, `GroupJoin`
- `Count`, `Min`, `Max`, `Sum`, `Average`
- `First`, `FirstOrDefault`, `Any`

**支持的函数：**
- String: `Contains`, `StartsWith`, `EndsWith`, `ToUpper`, `ToLower`, `Trim`, `Substring`, `Replace`, `Length`
- Math: `Abs`, `Round`, `Floor`, `Ceiling`, `Sqrt`, `Pow`, `Min`, `Max`

## 表达式查询（仓储模式）

```csharp
// 在仓储中使用 LINQ 表达式
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);

// 使用
var adults = await repo.GetWhereAsync(u => u.Age >= 18 && u.IsActive);
```

## 表达式占位符（Any Placeholder）

使用 `Any.Value<T>()` 创建可重用的表达式模板，在运行时填充参数：

```csharp
// 定义可重用的表达式模板
Expression<Func<User, bool>> ageRangeTemplate = u => 
    u.Age >= Any.Value<int>("minAge") && 
    u.Age <= Any.Value<int>("maxAge");

// 场景 1: 查询年轻用户（18-30岁）
var youngUsers = ExpressionBlockResult.Parse(ageRangeTemplate.Body, SqlDefine.SQLite)
    .WithParameter("minAge", 18)
    .WithParameter("maxAge", 30);
// SQL: ([age] >= @minAge AND [age] <= @maxAge)
// 参数: @minAge=18, @maxAge=30

// 场景 2: 查询中年用户（30-50岁）- 重用同一模板
var middleAgedUsers = ExpressionBlockResult.Parse(ageRangeTemplate.Body, SqlDefine.SQLite)
    .WithParameter("minAge", 30)
    .WithParameter("maxAge", 50);
// SQL: ([age] >= @minAge AND [age] <= @maxAge)
// 参数: @minAge=30, @maxAge=50

// UPDATE 表达式模板
Expression<Func<User, User>> updateTemplate = u => new User
{
    Name = Any.Value<string>("newName"),
    Age = u.Age + Any.Value<int>("ageIncrement")
};

var result = ExpressionBlockResult.ParseUpdate(updateTemplate, SqlDefine.SQLite)
    .WithParameter("newName", "John")
    .WithParameter("ageIncrement", 1);
// SQL: [name] = @newName, [age] = ([age] + @ageIncrement)
```

**使用场景**：
- ✅ 查询模板库 - 预定义常用查询模板
- ✅ 动态查询构建 - 运行时决定参数值
- ✅ 多租户应用 - 不同租户使用相同模板
- ✅ 配置驱动查询 - 从配置文件加载参数

**API 方法**：
- `Any.Value<T>(name)` - 定义占位符
- `WithParameter(name, value)` - 填充单个占位符
- `WithParameters(dictionary)` - 批量填充占位符
- `GetPlaceholderNames()` - 获取所有占位符名称
- `AreAllPlaceholdersFilled()` - 检查是否所有占位符都已填充

## 批量执行

```csharp
var users = new List<User> { new() { Name = "Alice" }, new() { Name = "Bob" } };
var sql = "INSERT INTO users (name) VALUES (@name)";
await connection.ExecuteBatchAsync(sql, users, UserParameterBinder.Default);
```

## 连接和事务管理

### 连接获取优先级

源生成器按以下优先级查找 DbConnection：

**方法参数 > 字段 > 属性 > 主构造函数**

```csharp
// 方式 1: 显式字段（推荐，优先级最高）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    private readonly SqliteConnection _connection;
    public DbTransaction? Transaction { get; set; }
    
    public UserRepository(SqliteConnection connection)
    {
        _connection = connection;
    }
}

// 方式 2: 属性（适合需要外部访问）
public partial class UserRepository : IUserRepository
{
    public SqliteConnection Connection { get; }
    public DbTransaction? Transaction { get; set; }
    
    public UserRepository(SqliteConnection connection)
    {
        Connection = connection;
    }
}

// 方式 3: 主构造函数（最简洁，自动生成）
public partial class UserRepository(SqliteConnection connection) : IUserRepository
{
    // 生成器自动生成：
    // private readonly SqliteConnection _connection = connection;
    // public DbTransaction? Transaction { get; set; }
}

// 方式 4: 方法参数（最灵活，优先级最高）
public interface IUserRepository
{
    // 使用类级别连接
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    // 使用方法参数连接（覆盖类级别连接）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdWithConnectionAsync(DbConnection connection, long id);
}
```

### 事务支持

```csharp
var repo = new UserRepository(connection);

using var transaction = connection.BeginTransaction();
repo.Transaction = transaction;

try
{
    await repo.InsertAsync(user1);
    await repo.UpdateAsync(user2);
    await repo.DeleteAsync(user3);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

**自动生成规则**：
- 如果用户未定义 `Transaction` 属性，生成器会自动生成
- 如果用户未定义连接字段/属性，生成器会从主构造函数参数自动生成字段

## 性能对比

基于 BenchmarkDotNet 测试（.NET 10 LTS，SQLite 内存数据库）：

### 小数据集性能（10-100条）- Web API 主要场景

| 数据量 | Sqlx | Dapper.AOT | FreeSql | Sqlx 优势 |
|--------|------|------------|---------|-----------|
| **10条** | **42.19 μs** | 43.42 μs | 49.64 μs | 🥇 快 2.9% / 17.7% |
| **100条** | **230.35 μs** | 233.76 μs | 237.38 μs | 🥇 快 1.5% / 3.1% |
| **1000条** | **2,165.87 μs** | 2,172.08 μs | 1,625.41 μs | 🥇 快 0.3% |

### 内存效率

| 数据量 | Sqlx | Dapper.AOT | FreeSql | Sqlx 优势 |
|--------|------|------------|---------|-----------|
| **10条** | **4.68 KB** | 6.55 KB | 8.67 KB | 🥇 少 40% / 85% |
| **100条** | **37 KB** | 45.66 KB | 37.23 KB | 🥇 少 23% |
| **1000条** | **360.24 KB** | 432.38 KB | 318.6 KB | 🥇 少 20% |

### GC 压力（关键指标）

| 数据量 | Sqlx Gen1 | Dapper.AOT Gen1 | FreeSql Gen1 | Sqlx 优势 |
|--------|-----------|-----------------|--------------|-----------|
| **1000条** | **1.95** | 3.91 | **25.39** | 🥇 最低（FreeSql 的 1/13） |

**关键洞察**：
- ✅ Sqlx 在小数据集（10-100条）上性能最优，这是 Web API 的主要场景
- ✅ Sqlx 的 GC 压力最小，更适合长时间运行的应用
- ✅ Sqlx 在所有场景下都比 Dapper.AOT 快，且内存效率更高
- ⚠️ FreeSql 在大数据集（1000+条）上更快，但 Gen1 GC 是 Sqlx 的 13倍

> 详细数据见 [性能基准测试](docs/benchmarks.md) 和 [AOT 性能测试](AOT_PERFORMANCE_RESULTS.md)

## 支持的数据库

| 数据库 | 方言枚举 | 状态 |
|--------|---------|------|
| SQLite | `SqlDefineTypes.SQLite` | ✅ 完全支持 |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | ✅ 完全支持 |
| MySQL | `SqlDefineTypes.MySql` | ✅ 完全支持 |
| SQL Server | `SqlDefineTypes.SqlServer` | ✅ 完全支持 |
| Oracle | `SqlDefineTypes.Oracle` | ✅ 完全支持 |
| IBM DB2 | `SqlDefineTypes.DB2` | ✅ 完全支持 |

**推荐：** .NET 10 (LTS) - 支持到 2028 年 11 月，性能最佳

## 高级类型支持

Sqlx 支持多种 C# 类型，自动生成最优代码：

### 支持的类型

| 类型 | 示例 | 生成策略 |
|------|------|---------|
| **Class** | `public class User { }` | 对象初始化器 |
| **Record** | `public record User(long Id, string Name);` | 构造函数 |
| **Mixed Record** | `public record User(long Id, string Name) { public string Email { get; set; } }` | 构造函数 + 对象初始化器 |
| **Struct** | `public struct User { }` | 对象初始化器 |
| **Struct Record** | `public readonly record struct User(long Id, string Name);` | 构造函数 |

### 特性

- ✅ **自动检测类型** - 源生成器自动识别类型并生成最优代码
- ✅ **只读属性过滤** - 自动忽略没有 setter 的属性
- ✅ **混合 Record 支持** - 主构造函数参数 + 额外属性
- ✅ **完全类型安全** - 编译时验证，零运行时开销

### 示例

```csharp
// 纯 Record - 使用构造函数
[Sqlx, TableName("users")]
public record User(long Id, string Name, int Age);

// 混合 Record - 构造函数 + 对象初始化器
[Sqlx, TableName("users")]
public record MixedUser(long Id, string Name)
{
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

// 只读属性自动忽略
[Sqlx, TableName("users")]
public class UserWithComputed
{
    [Key] public long Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    
    // 只读属性 - 自动忽略
    public string FullName => $"{FirstName} {LastName}";
}

// Struct Record
[Sqlx, TableName("points")]
public readonly record struct Point(int X, int Y);
```

## 更多文档

- [快速开始](docs/getting-started.md)
- [SQL 模板](docs/sql-templates.md)
- [数据库方言](docs/dialects.md)
- [源生成器](docs/source-generators.md)
- [性能基准测试](docs/benchmarks.md)
- [API 参考](docs/api-reference.md)
- [AI 助手指南](AI-VIEW.md)
- [示例项目](samples/TodoWebApi/)

## 许可证

MIT License - 详见 [LICENSE.txt](LICENSE.txt)
