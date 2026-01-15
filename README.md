# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](#)
[![LTS](https://img.shields.io/badge/LTS-.NET%2010-green.svg)](#)

编译时源生成器，构建类型安全、高性能的 .NET 数据库访问层。零运行时反射，完全 AOT 兼容。

## 核心特性

- **编译时生成** - 零反射，接近原生 ADO.NET 性能
- **类型安全** - 编译时捕获 SQL 错误
- **多数据库** - SQLite、PostgreSQL、MySQL、SQL Server、Oracle、DB2
- **智能模板** - 占位符自动适配不同数据库方言
- **完全 AOT 兼容** - 通过 817 个单元测试，支持 Native AOT 部署
- **泛型缓存** - SqlQuery<T> 自动缓存实体元信息，无反射开销
- **动态投影** - Select 支持匿名类型，自动生成高性能 ResultReader

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
using Sqlx;

// 基本查询
var sql = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToSql();
// SELECT * FROM [User] WHERE ([age] >= 18 AND [is_active] = 1) ORDER BY [name] ASC LIMIT 10

// 投影查询（匿名类型）
var sql = SqlQuery<User>.ForPostgreSQL()
    .Where(u => u.Name.Contains("test"))
    .Select(u => new { u.Id, u.Name })
    .ToSql();
// SELECT "id", "name" FROM "User" WHERE "name" LIKE '%' || 'test' || '%'

// 投影查询执行（自动使用 DynamicResultReader）
var results = await SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .Select(u => new { u.Id, u.Name, u.Email })
    .WithConnection(connection)
    .ToListAsync();
// 返回匿名类型列表，完全 AOT 兼容

// 参数化查询
var (sql, parameters) = SqlQuery<User>.ForSqlServer()
    .Where(u => u.Age > 18)
    .ToSqlWithParameters();
// SQL: SELECT * FROM [User] WHERE [age] > @p0
// Parameters: { "@p0": 18 }

// 聚合函数（使用 ColumnMeta 自动映射列名）
var count = await SqlQuery<User>.ForSqlite()
    .Where(u => u.IsActive)
    .WithConnection(connection)
    .WithReader(UserResultReader.Default)
    .CountAsync();

var maxAge = await SqlQuery<User>.ForSqlite()
    .WithConnection(connection)
    .WithReader(UserResultReader.Default)
    .MaxAsync(u => u.Age);
```

**支持的 LINQ 方法：**
- `Where` - 条件过滤（支持 String/Math 方法、null 合并、条件表达式）
- `Select` - 投影（支持匿名类型、函数调用）
- `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending` - 排序
- `Take` / `Skip` - 分页
- `GroupBy` - 分组
- `Distinct` - 去重
- `Count` / `LongCount` - 计数
- `Min` / `Max` - 极值
- `Sum` / `Average` - 求和/平均值
- `First` / `FirstOrDefault` - 获取第一条

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

基于 BenchmarkDotNet 测试（SQLite 内存数据库，10000 条记录）：

### .NET 10 性能（最新 LTS）

| 场景 | Sqlx vs Dapper.AOT | Sqlx vs FreeSql |
|------|-------------------|-----------------|
| 单条查询 | **快 15%** | **快 7.1x** |
| 插入操作 | 持平 | **快 2.0x** |
| 更新操作 | **快 9%** | **快 4.2x** |
| 删除操作 | 持平 | **快 5.4x** |
| 计数操作 | 持平 | **快 50x** |

**测试环境：** .NET 10.0.2 (LTS), BenchmarkDotNet 0.15.7, AMD Ryzen 7 5800H

**AOT 兼容性：** ✅ 通过 842 个单元测试，完全支持 Native AOT

**最新优化：**
- 移除 SQL 生成中的反射
- 支持 JOIN 查询
- 永远不生成 SELECT *

> 详细数据见 [性能基准测试](docs/benchmarks.md)

## 支持的数据库

| 数据库 | 方言枚举 | .NET 版本 |
|--------|---------|----------|
| SQLite | `SqlDefineTypes.SQLite` | .NET 8.0+ |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | .NET 8.0+ |
| MySQL | `SqlDefineTypes.MySql` | .NET 8.0+ |
| SQL Server | `SqlDefineTypes.SqlServer` | .NET 8.0+ |
| Oracle | `SqlDefineTypes.Oracle` | .NET 8.0+ |
| IBM DB2 | `SqlDefineTypes.DB2` | .NET 8.0+ |

**推荐：** .NET 10 (LTS) - 支持到 2028 年 11 月

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
