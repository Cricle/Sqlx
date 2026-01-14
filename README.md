# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](#)

编译时源生成器，构建类型安全、高性能的 .NET 数据库访问层。零运行时反射，完全 AOT 兼容。

## 核心特性

- **编译时生成** - 零反射，接近原生 ADO.NET 性能
- **类型安全** - 编译时捕获 SQL 错误
- **多数据库** - SQLite、PostgreSQL、MySQL、SQL Server、Oracle、DB2
- **智能模板** - 占位符自动适配不同数据库方言
- **AOT 兼容** - 完全支持 Native AOT

## 快速开始

```bash
dotnet add package Sqlx
```

```csharp
// 1. 定义实体
[SqlxEntity, SqlxParameter, TableName("users")]
public class User
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

## 表达式查询

```csharp
// 使用 LINQ 表达式构建类型安全的动态查询
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

| 场景 | Sqlx vs Dapper.AOT | Sqlx vs FreeSql |
|------|-------------------|-----------------|
| 单条查询 | **快 20%** | **快 7.5x** |
| 更新操作 | **快 14%** | **快 4.2x** |
| 删除操作 | **快 7%** | **快 5.3x** |
| 计数操作 | 持平 | **快 49x** |

> 运行命令：`dotnet run -c Release --project tests/Sqlx.Benchmarks`

## 支持的数据库

| 数据库 | 方言枚举 |
|--------|---------|
| SQLite | `SqlDefineTypes.SQLite` |
| PostgreSQL | `SqlDefineTypes.PostgreSql` |
| MySQL | `SqlDefineTypes.MySql` |
| SQL Server | `SqlDefineTypes.SqlServer` |
| Oracle | `SqlDefineTypes.Oracle` |
| IBM DB2 | `SqlDefineTypes.DB2` |

## 更多文档

- [AI 助手完全指南](AI-VIEW.md) - 完整功能清单和代码模式
- [示例项目](samples/TodoWebApi/) - 完整 Web API 示例

## 许可证

MIT License - 详见 [LICENSE.txt](LICENSE.txt)
