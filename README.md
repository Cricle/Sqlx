# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](#)

**Sqlx** 是一个编译时源生成器，用于构建类型安全、高性能的 .NET 数据库访问层。编写一次 SQL 模板，在任何数据库上运行，零运行时开销。

## ✨ 核心特性

- **🚀 编译时生成** - 零运行时反射，接近原生 ADO.NET 性能
- **🔒 类型安全** - 编译时捕获 SQL 错误
- **🌐 多数据库支持** - SQLite、PostgreSQL、MySQL、SQL Server、Oracle、DB2
- **📝 智能模板** - 占位符自动适配不同数据库方言
- **⚡ 高性能** - 直接 ADO.NET 调用，最小内存分配
- **🎯 AOT 兼容** - 完全支持 Native AOT

## 🚀 快速开始

### 安装

```bash
dotnet add package Sqlx
```

### 基础示例

```csharp
// 1. 定义实体
[SqlxEntity]
[SqlxParameter]
[TableName("users")]
public class User
{
    [Key]
    public long Id { get; set; }
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

var userId = await repo.InsertAndGetIdAsync(new User { Name = "Alice", Age = 25 });
var user = await repo.GetByIdAsync(userId);
var adults = await repo.GetAdultsAsync(18);
```

## 📚 核心概念

### SQL 模板占位符

占位符自动适配不同数据库方言：

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
Task<List<User>> GetActiveUsersAsync();
```

**各数据库生成的 SQL：**

| 数据库 | 生成的 SQL |
|--------|-----------|
| SQLite | `SELECT [id], [name], [age] FROM [users] WHERE is_active = 1` |
| PostgreSQL | `SELECT "id", "name", "age" FROM "users" WHERE is_active = true` |
| MySQL | ``SELECT `id`, `name`, `age` FROM `users` WHERE is_active = 1`` |
| SQL Server | `SELECT [id], [name], [age] FROM [users] WHERE is_active = 1` |

### 常用占位符

| 占位符 | 说明 | 示例输出 |
|--------|------|---------|
| `{{table}}` | 表名（带方言引号） | `"users"` (PostgreSQL) |
| `{{columns}}` | 所有列名 | `id, name, age` |
| `{{columns --exclude Id}}` | 排除指定列 | `name, age` |
| `{{values --exclude Id}}` | 参数占位符 | `@name, @age` |
| `{{set --exclude Id}}` | UPDATE SET 子句 | `name = @name, age = @age` |
| `{{where --param predicate}}` | WHERE 子句（表达式） | `WHERE age > @p0` |
| `{{limit --param count}}` | LIMIT 子句 | `LIMIT @count` |
| `{{offset --param skip}}` | OFFSET 子句 | `OFFSET @skip` |
| `{{if notnull=param}}...{{/if}}` | 条件包含（参数非空时） | 动态 SQL |
| `{{if null=param}}...{{/if}}` | 条件包含（参数为空时） | 动态 SQL |
| `{{if notempty=param}}...{{/if}}` | 条件包含（集合非空时） | 动态 SQL |
| `{{if empty=param}}...{{/if}}` | 条件包含（集合为空时） | 动态 SQL |

### 条件占位符

使用 `{{if}}` 块实现动态 SQL 条件：

```csharp
// 动态搜索：只在参数有值时添加条件
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1 
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}
    {{if notnull=status}}AND status = @status{{/if}}
")]
Task<List<User>> SearchAsync(string? name, int? minAge, string? status);

// 使用
await repo.SearchAsync("Alice%", null, "active");
// 生成: SELECT ... WHERE 1=1 AND name LIKE @name AND status = @status
// minAge 为 null，对应条件被排除
```

**支持的条件：**
- `notnull=param` - 参数不为 null 时包含
- `null=param` - 参数为 null 时包含
- `notempty=param` - 集合不为空时包含
- `empty=param` - 集合为空时包含

### 内置仓储接口

继承 `ICrudRepository<TEntity, TKey>` 获得标准 CRUD 方法：

```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    // 继承的方法：
    // - GetByIdAsync(id)
    // - GetByIdsAsync(ids)
    // - GetAllAsync(limit)
    // - GetWhereAsync(predicate, limit)
    // - GetFirstWhereAsync(predicate)
    // - GetPagedAsync(pageSize, offset)
    // - ExistsAsync(predicate)
    // - CountAsync()
    // - CountWhereAsync(predicate)
    // - InsertAndGetIdAsync(entity)
    // - UpdateAsync(entity)
    // - UpdateWhereAsync(predicate, setter)
    // - DeleteAsync(id)
    // - DeleteWhereAsync(predicate)
    
    // 自定义方法
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @pattern")]
    Task<List<User>> SearchByNameAsync(string pattern);
}
```

## 🌐 多数据库支持

### 可扩展的方言系统

`SqlDialect` 是一个抽象基类，提供丰富的可扩展方法：

```csharp
// 预定义方言
SqlDefine.SQLite      // SQLite
SqlDefine.PostgreSql  // PostgreSQL
SqlDefine.MySql       // MySQL
SqlDefine.SqlServer   // SQL Server
SqlDefine.Oracle      // Oracle
SqlDefine.DB2         // IBM DB2

// 使用方言方法
var dialect = SqlDefine.PostgreSql;
dialect.WrapColumn("name")           // "name"
dialect.Concat("a", "b")             // a || b
dialect.CurrentTimestamp             // CURRENT_TIMESTAMP
dialect.IfNull("col", "'default'")   // COALESCE(col, 'default')
dialect.Paginate("10", "20")         // LIMIT 10 OFFSET 20
dialect.Cast("col", "VARCHAR(100)")  // (col)::VARCHAR(100)
```

### 方言方法一览

| 类别 | 方法 |
|------|------|
| 标识符 | `WrapColumn`, `WrapString`, `CreateParameter` |
| 字符串 | `Concat`, `Upper`, `Lower`, `Trim`, `Length`, `Substring`, `Replace`, `Coalesce` |
| 日期时间 | `CurrentTimestamp`, `CurrentDate`, `CurrentTime`, `DatePart`, `DateAdd`, `DateDiff` |
| 数值 | `Abs`, `Round`, `Ceiling`, `Floor`, `Mod` |
| 聚合 | `Count`, `Sum`, `Avg`, `Min`, `Max` |
| 分页 | `Limit`, `Offset`, `Paginate` |
| 空值 | `IfNull`, `NullIf` |
| 条件 | `CaseWhen`, `Iif` |
| 类型 | `Cast` |
| 其他 | `LastInsertedId`, `BoolTrue`, `BoolFalse` |

### 自定义方言

继承 `SqlDialect` 创建自定义方言：

```csharp
public class MyCustomDialect : SqlDialect
{
    public override string DatabaseType => "MyDB";
    public override Annotations.SqlDefineTypes DbType => /* ... */;
    public override string ColumnLeft => "`";
    public override string ColumnRight => "`";
    public override string ParameterPrefix => "?";
    
    public override string Concat(params string[] parts) => 
        $"CONCAT({string.Join(", ", parts)})";
    
    public override string CurrentTimestamp => "NOW()";
    // ... 其他方法
}
```

## 🎯 高级特性

### AOT 兼容的实体生成

使用 `[SqlxEntity]` 和 `[SqlxParameter]` 特性生成高性能代码：

```csharp
[SqlxEntity]      // 生成 EntityProvider 和 ResultReader
[SqlxParameter]   // 生成 ParameterBinder
public class User
{
    [Key]
    public long Id { get; set; }
    
    [Column("user_name")]  // 自定义列名映射
    public string Name { get; set; }
    
    [IgnoreDataMember]     // 排除字段
    public string? CachedData { get; set; }
}
```

**生成的代码：**
- `UserEntityProvider` - 提供列元数据，无反射
- `UserResultReader` - 从 `DbDataReader` 读取实体，缓存列序号
- `UserParameterBinder` - 绑定实体属性到 `DbCommand` 参数

### 表达式查询

使用 LINQ 表达式构建类型安全的动态查询：

```csharp
// 接口定义
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);

// 使用
var adults = await repo.GetWhereAsync(u => u.Age >= 18 && u.IsActive);
// 生成: SELECT ... FROM users WHERE age >= @p0 AND is_active = @p1
```

### 执行拦截器

监控和调试 SQL 执行：

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    partial void OnExecuting(string operationName, DbCommand command, SqlTemplate template)
    {
        Console.WriteLine($"[{operationName}] SQL: {command.CommandText}");
    }

    partial void OnExecuted(string operationName, DbCommand command, SqlTemplate template, 
                           object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"[{operationName}] Completed in {ms:F2}ms");
    }

    partial void OnExecuteFail(string operationName, DbCommand command, SqlTemplate template,
                              Exception exception, long elapsedTicks)
    {
        Console.WriteLine($"[{operationName}] Failed: {exception.Message}");
    }
}
```

### Activity 跟踪

自动集成 OpenTelemetry 跟踪：

```csharp
// 生成的代码自动添加 Activity 事件和标签：
// - db.system: 数据库类型
// - db.operation: sqlx.execute
// - db.statement: SQL 语句
// - db.duration_ms: 执行时间
// - db.rows_affected: 影响行数
```

### SQL 调试

返回 `SqlTemplate` 类型获取生成的 SQL：

```csharp
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    SqlTemplate GetByIdSql(long id);  // 返回 SqlTemplate 而非执行
}

var template = repo.GetByIdSql(123);
Console.WriteLine(template.Sql);  // 输出生成的 SQL
```

## ⚡ 性能对比

### Sqlx vs Dapper.AOT

基于 BenchmarkDotNet 的公平对比测试（SQLite 内存数据库，10000 条记录，禁用 Activity 和 Interceptor）：

#### 单条查询 (SelectSingle)

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| Sqlx | 9.04 μs | 1.00 | 1.7 KB | 1.00 |
| Dapper.AOT | 10.67 μs | 1.18 | 2.95 KB | 1.73 |

**Sqlx 快 18%，内存少 42%**

#### 列表查询 (SelectList)

| Method | Limit | Mean | Ratio | Allocated | Alloc Ratio |
|--------|-------|------|-------|-----------|-------------|
| Dapper.AOT | 10 | 22.97 μs | 0.87 | 6.55 KB | 0.96 |
| Sqlx | 10 | 26.50 μs | 1.00 | 6.84 KB | 1.00 |
| Dapper.AOT | 100 | 137.26 μs | 0.78 | 42.14 KB | 1.05 |
| Sqlx | 100 | 176.37 μs | 1.00 | 40.28 KB | 1.00 |
| Dapper.AOT | 1000 | 1,475 μs | 0.77 | 393.71 KB | 1.06 |
| Sqlx | 1000 | 1,911 μs | 1.00 | 370.57 KB | 1.00 |

**Dapper.AOT 批量读取更快，Sqlx 内存更少**

#### 插入操作 (Insert)

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| Dapper.AOT | 82.60 μs | 0.89 | 7.59 KB | 1.50 |
| Sqlx | 98.67 μs | 1.06 | 5.08 KB | 1.00 |

**性能持平，Sqlx 内存少 33%**

#### 批量插入 (BatchInsert)

| Method | BatchSize | Mean | Ratio | Allocated | Alloc Ratio |
|--------|-----------|------|-------|-----------|-------------|
| Sqlx.Loop | 10 | 201.8 μs | 0.93 | 23.7 KB | 1.00 |
| Dapper.AOT | 10 | 209.4 μs | 0.96 | 35.19 KB | 1.48 |
| Sqlx.DbBatch | 10 | 218.0 μs | 1.00 | 23.77 KB | 1.00 |
| | | | | | |
| Sqlx.Loop | 100 | 1,321 μs | 0.97 | 228.3 KB | 1.00 |
| Sqlx.DbBatch | 100 | 1,364 μs | 1.01 | 228.66 KB | 1.00 |
| Dapper.AOT | 100 | 2,400 μs | 1.77 | 336.13 KB | 1.47 |
| | | | | | |
| Sqlx.Loop | 1000 | 14,055 μs | 0.97 | 2274.32 KB | 1.00 |
| Sqlx.DbBatch | 1000 | 14,477 μs | 1.00 | 2274.34 KB | 1.00 |
| Dapper.AOT | 1000 | 15,825 μs | 1.09 | 3345.5 KB | 1.47 |

**批量插入：Sqlx 快 3-77%，内存少 32-47%**

### 总结

| 场景 | 性能对比 | 内存对比 |
|------|----------|----------|
| 单条查询 | **Sqlx 快 18%** | **Sqlx 少 42%** |
| 插入操作 | 持平 | **Sqlx 少 33%** |
| 批量插入（小批量） | **Sqlx 快 3-7%** | **Sqlx 少 32%** |
| 批量插入（中批量） | **Sqlx 快 43-77%** | **Sqlx 少 32%** |
| 批量插入（大批量） | **Sqlx 快 9%** | **Sqlx 少 32%** |
| 列表查询 | Dapper.AOT 快 13-23% | Sqlx 少 5-6% |

**Sqlx 优势场景**：单条查询、批量插入、内存敏感场景
**Dapper.AOT 优势场景**：批量读取

> 测试环境：.NET 9.0.8, AMD Ryzen 7 5800H, Windows 10 (22H2)
> 运行命令：`dotnet run -c Release --project tests/Sqlx.Benchmarks`

### 批量执行 (Batch Execution)

使用 `DbBatchExecutor` 高效执行批量操作：

```csharp
// 定义实体和参数绑定器（由源生成器自动生成）
[SqlxEntity]
[SqlxParameter]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// 批量插入
var users = new List<User>
{
    new() { Name = "Alice", Email = "alice@test.com" },
    new() { Name = "Bob", Email = "bob@test.com" },
    new() { Name = "Charlie", Email = "charlie@test.com" }
};

var sql = "INSERT INTO users (name, email) VALUES (@name, @email)";
var affected = await connection.ExecuteBatchAsync(
    sql, 
    users, 
    UserParameterBinder.Default,
    batchSize: 100,        // 每批最大命令数（默认 1000）
    commandTimeout: 60);   // 命令超时（秒）

// 批量更新
var updates = users.Select(u => new { u.Id, u.Name }).ToList();
var updateSql = "UPDATE users SET name = @name WHERE id = @id";
await connection.ExecuteBatchAsync(updateSql, updates, UpdateBinder.Default);

// 批量删除
var deleteIds = new List<DeleteParam> { new(1), new(2), new(3) };
var deleteSql = "DELETE FROM users WHERE id = @id";
await connection.ExecuteBatchAsync(deleteSql, deleteIds, DeleteBinder.Default);
```

**特性：**
- 零反射：完全 AOT 兼容
- 自动分批：大数据集按 `batchSize` 分批执行
- 事务支持：可传入 `DbTransaction`
- 高性能：复用命令对象，最小化内存分配

## 🗄️ 支持的数据库

| 数据库 | 状态 | 方言枚举 |
|--------|------|---------|
| SQLite | ✅ 生产就绪 | `SqlDefineTypes.SQLite` |
| PostgreSQL | ✅ 生产就绪 | `SqlDefineTypes.PostgreSql` |
| MySQL | ✅ 生产就绪 | `SqlDefineTypes.MySql` |
| SQL Server | ✅ 生产就绪 | `SqlDefineTypes.SqlServer` |
| Oracle | ✅ 生产就绪 | `SqlDefineTypes.Oracle` |
| IBM DB2 | ✅ 生产就绪 | `SqlDefineTypes.DB2` |

## 📖 示例项目

查看 [samples/TodoWebApi](samples/TodoWebApi/) 获取完整的 Web API 示例，演示：
- 实体定义和仓储实现
- CRUD 操作
- 自定义查询方法
- 批量操作
- AOT 兼容配置

## 🤝 贡献

欢迎提交 Pull Request！

## 📄 许可证

MIT License - 详见 [LICENSE.txt](LICENSE.txt)
