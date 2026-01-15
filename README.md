# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](#)
[![LTS](https://img.shields.io/badge/LTS-.NET%2010-green.svg)](#)
[![Tests](https://img.shields.io/badge/tests-974%20passing-brightgreen.svg)](#)
[![AOT](https://img.shields.io/badge/AOT-ready-blue.svg)](#)

高性能、AOT 友好的 .NET 数据库访问库。使用源生成器在编译时生成代码，零运行时反射，完全支持 Native AOT。

## 核心特性

- **🚀 高性能** - 比 Dapper.AOT 快 15%，比 FreeSql 快 7 倍（单条查询）
- **⚡ 零反射** - 编译时源生成，运行时无反射开销
- **🎯 类型安全** - 编译时验证 SQL 模板和表达式
- **🌐 多数据库** - SQLite、PostgreSQL、MySQL、SQL Server、Oracle、DB2
- **📦 AOT 就绪** - 完全支持 Native AOT，通过 974 个单元测试
- **🔧 LINQ 支持** - IQueryable 接口，支持 Where/Select/OrderBy/Join 等
- **💾 智能缓存** - SqlQuery\<T\> 泛型缓存，自动注册 EntityProvider

## 快速开始

```bash
dotnet add package Sqlx
```

```csharp
// 1. 定义实体（标记为 partial 以启用自动注册）
[SqlxEntity, SqlxParameter, TableName("users")]
public partial class User
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

## SQL 模板占位符

占位符自动适配不同数据库方言：

| 占位符 | 说明 | 示例输出 |
|--------|------|---------|
| `{{table}}` | 表名（带方言引号） | `"users"` (PostgreSQL) |
| `{{columns}}` | 所有列名 | `id, name, age` |
| `{{columns --exclude Id}}` | 排除指定列 | `name, age` |
| `{{values --exclude Id}}` | 参数占位符 | `@name, @age` |
| `{{set --exclude Id}}` | UPDATE SET 子句 | `name = @name` |
| `{{if notnull=param}}...{{/if}}` | 条件包含 | 动态 SQL |

**各数据库生成的 SQL：**

| 数据库 | 生成的 SQL |
|--------|-----------|
| SQLite | `SELECT [id], [name] FROM [users] WHERE is_active = 1` |
| PostgreSQL | `SELECT "id", "name" FROM "users" WHERE is_active = true` |
| MySQL | ``SELECT `id`, `name` FROM `users` WHERE is_active = 1`` |

## 内置仓储接口

继承 `ICrudRepository<TEntity, TKey>` 获得标准 CRUD 方法：

```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    // 继承方法: GetByIdAsync, GetAllAsync, InsertAndGetIdAsync, 
    // UpdateAsync, DeleteAsync, CountAsync, ExistsAsync...
    
    // 自定义方法
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @pattern")]
    Task<List<User>> SearchByNameAsync(string pattern);
}
```

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

## 批量执行

```csharp
var users = new List<User> { new() { Name = "Alice" }, new() { Name = "Bob" } };
var sql = "INSERT INTO users (name) VALUES (@name)";
await connection.ExecuteBatchAsync(sql, users, UserParameterBinder.Default);
```

## 性能对比

基于 BenchmarkDotNet 测试（.NET 10 LTS，SQLite 内存数据库）：

| 操作 | Sqlx | Dapper.AOT | FreeSql | Sqlx 优势 |
|------|------|------------|---------|-----------|
| 单条查询 | **9.08 μs** | 10.43 μs | 64.54 μs | 快 15% / 7.1x |
| 内存分配 | **1.79 KB** | 2.96 KB | 11.55 KB | 少 65% / 546% |
| 插入操作 | **81.76 μs** | 85.03 μs | 165.69 μs | 持平 / 快 2.0x |
| 更新操作 | **15.82 μs** | 17.20 μs | 65.63 μs | 快 9% / 4.2x |
| 计数操作 | **3.91 μs** | 3.98 μs | 195.30 μs | 持平 / 快 50x |

> 详细数据见 [性能基准测试](docs/benchmarks.md)

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
