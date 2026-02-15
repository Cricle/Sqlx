# SqlBuilder - 动态 SQL 构建器

SqlBuilder 提供高性能、类型安全的动态 SQL 构建能力，使用 C# 插值字符串自动参数化，防止 SQL 注入。

## 核心特性

- **🔒 自动参数化** - 插值字符串自动转换为 SQL 参数，防止 SQL 注入
- **⚡ 高性能** - 使用 ArrayPool<char> 零堆分配，Expression tree 优化参数转换
- **🔧 SqlTemplate 集成** - 支持 {{columns}}、{{table}} 等占位符
- **🔗 子查询支持** - 组合式查询构建，自动参数冲突解决
- **📦 AOT 兼容** - 零反射设计，完全支持 Native AOT
- **🎯 类型安全** - 编译时验证，运行时零开销

## 快速开始

### 基本用法

```csharp
using Sqlx;

// 自动参数化
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE age >= {18} AND name = {"John"}");
var template = builder.Build();

// 生成的 SQL
Console.WriteLine(template.Sql);
// SELECT * FROM users WHERE age >= @p0 AND name = @p1

// 参数
foreach (var (key, value) in template.Parameters)
{
    Console.WriteLine($"{key} = {value}");
}
// p0 = 18
// p1 = John

// 直接执行查询
var users = await connection.QueryAsync(
    template.Sql, 
    template.Parameters, 
    UserResultReader.Default
);
```

### 动态条件构建

```csharp
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE 1=1");

// 根据条件动态添加 WHERE 子句
if (!string.IsNullOrEmpty(nameFilter))
{
    builder.Append($" AND name LIKE {"%" + nameFilter + "%"}");
}

if (minAge.HasValue)
{
    builder.Append($" AND age >= {minAge.Value}");
}

if (isActive.HasValue)
{
    builder.Append($" AND is_active = {isActive.Value}");
}

var template = builder.Build();
var users = await connection.QueryAsync(template.Sql, template.Parameters, UserResultReader.Default);
```

## SqlTemplate 集成

SqlBuilder 完全集成 Sqlx 的 SqlTemplate 系统，支持所有占位符：

```csharp
// 创建带 PlaceholderContext 的 builder
var context = new PlaceholderContext(
    SqlDefine.SQLite, 
    "users", 
    UserEntityProvider.Default.Columns
);

using var builder = new SqlBuilder(context);

// 使用 {{columns}} 和 {{table}} 占位符
builder.AppendTemplate(
    "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", 
    new { minAge = 18 }
);

var template = builder.Build();
// SQL: SELECT [id], [name], [age], [email] FROM [users] WHERE age >= @minAge
// Parameters: { "minAge": 18 }
```

### 支持的占位符

| 占位符 | 说明 | 示例 |
|--------|------|------|
| `{{table}}` | 表名（带方言引号） | `[users]` (SQLite) |
| `{{columns}}` | 所有列名 | `[id], [name], [age]` |
| `{{columns --exclude Id}}` | 排除指定列 | `[name], [age]` |
| `{{values}}` | 参数占位符 | `@name, @age` |
| `{{set}}` | UPDATE SET 子句 | `[name] = @name` |

### 参数传递方式

SqlBuilder 支持三种参数传递方式，性能从高到低：

```csharp
// 1. Dictionary<string, object?> - 最快（直接使用）
var dict = new Dictionary<string, object?> 
{ 
    { "minAge", 18 },
    { "status", "active" }
};
builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", dict);

// 2. 匿名对象 - 推荐（Expression tree 优化）
builder.AppendTemplate(
    "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status",
    new { minAge = 18, status = "active" }
);

// 3. IReadOnlyDictionary<string, object?> - 兼容性
IReadOnlyDictionary<string, object?> readOnlyDict = dict;
builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", readOnlyDict);
```

## 子查询支持

SqlBuilder 支持嵌套子查询，自动处理参数冲突：

```csharp
// 创建子查询
using var subquery = new SqlBuilder(SqlDefine.SQLite);
subquery.Append($"SELECT id FROM orders WHERE total > {1000}");

// 创建主查询
using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
mainQuery.Append($"SELECT * FROM users WHERE id IN ");
mainQuery.AppendSubquery(subquery);

var template = mainQuery.Build();
// SQL: SELECT * FROM users WHERE id IN (SELECT id FROM orders WHERE total > @p0)
// Parameters: { "p0": 1000 }
```

### 参数冲突自动解决

```csharp
// 主查询和子查询都有参数
using var subquery = new SqlBuilder(SqlDefine.SQLite);
subquery.Append($"SELECT user_id FROM orders WHERE total > {1000}");

using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
mainQuery.Append($"SELECT * FROM users WHERE age >= {18} AND id IN ");
mainQuery.AppendSubquery(subquery);

var template = mainQuery.Build();
// SQL: SELECT * FROM users WHERE age >= @p0 AND id IN (SELECT user_id FROM orders WHERE total > @p1)
// Parameters: { "p0": 18, "p1": 1000 }
// 参数名自动重命名，避免冲突
```

## 方法链式调用

SqlBuilder 支持流畅的方法链式调用：

```csharp
var template = new SqlBuilder(SqlDefine.SQLite)
    .Append($"SELECT * FROM users")
    .Append($" WHERE age >= {18}")
    .Append($" AND status = {"active"}")
    .Append($" ORDER BY name")
    .Build();
```

## AppendRaw - 原始 SQL

对于不需要参数化的 SQL 片段，使用 `AppendRaw`：

```csharp
using var builder = new SqlBuilder(SqlDefine.SQLite);

// 安全：使用方言引号包装标识符
var tableName = SqlDefine.SQLite.WrapColumn("users");
builder.AppendRaw($"SELECT * FROM {tableName}");

// 安全：SQL 关键字和固定字符串
builder.AppendRaw(" WHERE 1=1");
builder.Append($" AND age >= {18}");

var template = builder.Build();
// SQL: SELECT * FROM [users] WHERE 1=1 AND age >= @p0
```

### ⚠️ 安全警告

`AppendRaw` 不提供 SQL 注入保护，仅用于：
- ✅ SQL 关键字（SELECT, WHERE, ORDER BY 等）
- ✅ 使用方言引号包装的标识符
- ✅ 应用程序控制的固定字符串

**永远不要**将用户输入传递给 `AppendRaw`：

```csharp
// ❌ 危险！SQL 注入风险
builder.AppendRaw($"SELECT * FROM {userInput}");

// ✅ 安全：使用参数化
builder.Append($"SELECT * FROM users WHERE name = {userInput}");
```

## 性能优化

SqlBuilder 使用多项技术优化性能：

### 1. ArrayPool<char> 零堆分配

```csharp
// 内部使用 ArrayPool<char> 租用缓冲区
using var builder = new SqlBuilder(SqlDefine.SQLite, initialCapacity: 1024);
// 缓冲区自动增长，无需手动管理
builder.Append($"SELECT * FROM users WHERE id = {123}");
// Dispose 时自动归还缓冲区到池
```

### 2. Expression Tree 参数转换

匿名对象参数使用编译的 Expression tree 转换，比反射快 8.9%-34%：

```csharp
// 第一次调用：编译 Expression tree（一次性开销）
builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", new { age = 18 });

// 后续调用：使用缓存的 Expression tree（零反射）
builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", new { age = 30 });
```

**性能对比**（基于 BenchmarkDotNet，.NET 10.0.3）：

| 属性数量 | Expression Tree | Reflection | 性能提升 | 内存节省 |
|---------|----------------|------------|---------|---------|
| 2 props | 1.486 μs | 1.632 μs | **8.9%** | 7.3% |
| 5 props | 1.328 μs | 1.678 μs | **20.9%** | 9.9% |
| 10 props | 1.507 μs | 2.282 μs | **34.0%** | 14.8% |
| 多次调用 | 12.130 μs | 13.358 μs | **9.2%** | 5.0% |

> 详细性能测试结果见 [SqlBuilder 性能基准测试](sqlbuilder-benchmark-results.md)

### 3. 直接字典索引赋值

Expression tree 直接使用字典索引器赋值，避免临时对象：

```csharp
// 生成的代码（简化）
dict["prop1"] = obj.prop1;
dict["prop2"] = obj.prop2;
// 无临时 Dictionary 分配
```

### 4. GC 压力优化

Expression tree 版本的 GC 压力显著低于反射版本：

- 2 props: 减少 **7.1%** GC 压力
- 5 props: 减少 **10.1%** GC 压力
- 10 props: 减少 **14.9%** GC 压力

## 完整示例

### 复杂查询构建

```csharp
public async Task<List<User>> SearchUsersAsync(
    string? nameFilter,
    int? minAge,
    int? maxAge,
    bool? isActive,
    string? orderBy,
    int pageSize,
    int pageNumber)
{
    var context = new PlaceholderContext(
        SqlDefine.SQLite,
        "users",
        UserEntityProvider.Default.Columns
    );

    using var builder = new SqlBuilder(context);
    
    // 基础查询
    builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE 1=1");

    // 动态条件
    if (!string.IsNullOrEmpty(nameFilter))
    {
        builder.Append($" AND name LIKE {"%" + nameFilter + "%"}");
    }

    if (minAge.HasValue)
    {
        builder.Append($" AND age >= {minAge.Value}");
    }

    if (maxAge.HasValue)
    {
        builder.Append($" AND age <= {maxAge.Value}");
    }

    if (isActive.HasValue)
    {
        builder.Append($" AND is_active = {isActive.Value}");
    }

    // 动态排序
    if (!string.IsNullOrEmpty(orderBy))
    {
        var column = SqlDefine.SQLite.WrapColumn(orderBy);
        builder.AppendRaw($" ORDER BY {column}");
    }

    // 分页
    var offset = (pageNumber - 1) * pageSize;
    builder.Append($" LIMIT {pageSize} OFFSET {offset}");

    var template = builder.Build();
    return await connection.QueryAsync(
        template.Sql,
        template.Parameters,
        UserResultReader.Default
    );
}
```

### 批量插入

```csharp
public async Task<int> BulkInsertUsersAsync(List<User> users)
{
    var context = new PlaceholderContext(
        SqlDefine.SQLite,
        "users",
        UserEntityProvider.Default.Columns
    );

    using var builder = new SqlBuilder(context);
    builder.AppendTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ");

    for (int i = 0; i < users.Count; i++)
    {
        if (i > 0) builder.AppendRaw(", ");
        
        var user = users[i];
        builder.Append($"({user.Name}, {user.Age}, {user.Email})");
    }

    var template = builder.Build();
    return await connection.ExecuteAsync(template.Sql, template.Parameters);
}
```

### 动态 JOIN

```csharp
public async Task<List<UserWithOrders>> GetUsersWithOrdersAsync(decimal minTotal)
{
    using var builder = new SqlBuilder(SqlDefine.SQLite);
    
    builder.Append($@"
        SELECT u.id, u.name, o.id as order_id, o.total
        FROM users u
        INNER JOIN orders o ON u.id = o.user_id
        WHERE o.total >= {minTotal}
    ");

    var template = builder.Build();
    return await connection.QueryAsync(
        template.Sql,
        template.Parameters,
        UserWithOrdersResultReader.Default
    );
}
```

## API 参考

### 构造函数

```csharp
// 使用 SqlDialect
public SqlBuilder(SqlDialect dialect, int initialCapacity = 1024)

// 使用 PlaceholderContext（支持 SqlTemplate 占位符）
public SqlBuilder(PlaceholderContext context, int initialCapacity = 1024)
```

### 方法

```csharp
// 自动参数化插值字符串
public SqlBuilder Append(FormattableString formattable)

// 追加原始 SQL（不参数化）
public SqlBuilder AppendRaw(string sql)

// 追加 SqlTemplate（支持占位符）
public SqlBuilder AppendTemplate<TParameters>(string template, TParameters? parameters = default)
    where TParameters : class

// 追加子查询
public SqlBuilder AppendSubquery(SqlBuilder subquery)

// 构建最终 SQL
public SqlTemplate Build()

// 释放资源
public void Dispose()
```

### SqlTemplate 返回值

```csharp
public class SqlTemplate
{
    public string Sql { get; }                              // 生成的 SQL
    public Dictionary<string, object?> Parameters { get; }  // 参数字典
    public string TemplateSql { get; }                      // 原始模板
    public bool HasDynamicPlaceholders { get; }             // 是否有动态占位符
}
```

## 最佳实践

### 1. 使用 using 语句

```csharp
// ✅ 推荐：自动释放资源
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE id = {123}");
var template = builder.Build();

// ❌ 不推荐：手动释放
var builder = new SqlBuilder(SqlDefine.SQLite);
try
{
    builder.Append($"SELECT * FROM users WHERE id = {123}");
    var template = builder.Build();
}
finally
{
    builder.Dispose();
}
```

### 2. 合理设置初始容量

```csharp
// 小查询（默认 1024 字符）
using var builder = new SqlBuilder(SqlDefine.SQLite);

// 大查询（预估 4096 字符）
using var builder = new SqlBuilder(SqlDefine.SQLite, initialCapacity: 4096);
```

### 3. 优先使用匿名对象

```csharp
// ✅ 推荐：Expression tree 优化
builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", new { age = 18 });

// ⚠️ 可用但较慢：需要手动创建字典
var dict = new Dictionary<string, object?> { { "age", 18 } };
builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", dict);
```

### 4. 避免过度使用 AppendRaw

```csharp
// ❌ 不推荐：容易出错
builder.AppendRaw($"SELECT * FROM users WHERE age >= {age}");

// ✅ 推荐：自动参数化
builder.Append($"SELECT * FROM users WHERE age >= {age}");
```

### 5. 复用 PlaceholderContext

```csharp
// ✅ 推荐：复用 context
var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns);

using var builder1 = new SqlBuilder(context);
builder1.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id", new { id = 1 });

using var builder2 = new SqlBuilder(context);
builder2.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @age", new { age = 18 });
```

## 常见问题

### Q: SqlBuilder 是否线程安全？

A: 否。每个 SqlBuilder 实例应在单个线程中使用。如需并发，为每个线程创建独立实例。

### Q: 为什么要使用 SqlBuilder 而不是字符串拼接？

A: SqlBuilder 提供：
- 自动参数化（防止 SQL 注入）
- 类型安全（编译时验证）
- 高性能（ArrayPool 零堆分配）
- SqlTemplate 集成（占位符支持）

### Q: SqlBuilder 与 Dapper 的 SqlBuilder 有什么区别？

A: Sqlx SqlBuilder：
- 使用 C# 插值字符串（更简洁）
- 集成 SqlTemplate 系统
- 使用 ArrayPool（更高性能）
- 完全 AOT 兼容

### Q: 可以在 SqlBuilder 中使用存储过程吗？

A: 可以，但不推荐。SqlBuilder 主要用于动态 SQL 构建。存储过程建议直接使用 `connection.ExecuteAsync`。

### Q: 如何调试生成的 SQL？

A: 使用 `Build()` 返回的 `SqlTemplate`：

```csharp
var template = builder.Build();
Console.WriteLine($"SQL: {template.Sql}");
foreach (var (key, value) in template.Parameters)
{
    Console.WriteLine($"{key} = {value}");
}
```

## 相关文档

- [SqlBuilder 性能基准测试](sqlbuilder-benchmark-results.md)
- [SQL 模板](sql-templates.md)
- [数据库方言](dialects.md)
- [性能基准测试](benchmarks.md)
- [快速开始](getting-started.md)
