# 输出参数（Output Parameters）

Sqlx 支持 SQL 输出参数，允许从数据库获取返回值。这对于存储过程、获取生成的 ID 或返回计算值非常有用。

## 概述

Sqlx 提供两种方式来处理输出参数：

1. **同步方法 + Out/Ref 参数** - 使用 C# 原生的 `out` 和 `ref` 关键字
2. **异步方法 + OutputParameter<T> 包装类** - 使用包装类绕过 C# 语言限制

## 方案 1: 同步方法使用 Out/Ref 参数

### Out 参数（Output 模式）

用于从数据库获取单向输出值：

```csharp
public interface IUserRepository
{
    // 单个输出参数（同步方法）
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age); SELECT last_insert_rowid()")]
    int InsertAndGetId(
        string name, 
        int age, 
        [OutputParameter(DbType.Int32)] out int id);
}

// 使用
var result = repo.InsertAndGetId("John", 25, out int newId);
Console.WriteLine($"Inserted ID: {newId}");
```

### Ref 参数（InputOutput 模式）

用于传入初始值并接收更新后的值：

```csharp
public interface ICounterRepository
{
    // ref 参数：传入当前值，返回更新后的值（同步方法）
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    int IncrementCounter(
        string name,
        [OutputParameter(DbType.Int32)] ref int currentValue);
}

// 使用
int counter = 100;
var result = repo.IncrementCounter("page_views", ref counter);
Console.WriteLine($"New counter value: {counter}"); // 输出: 101
```

## 方案 2: 异步方法使用 OutputParameter<T> 包装类

由于 C# 不允许在异步方法中使用 `ref` 和 `out` 参数，Sqlx 提供了 `OutputParameter<T>` 包装类来解决这个问题。

### 基本用法（Output 模式）

```csharp
public interface IUserRepository
{
    // 使用 OutputParameter<T> 包装类（异步方法）
    [SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age); SELECT last_insert_rowid()")]
    Task<int> InsertUserAsync(
        string name, 
        int age, 
        [OutputParameter(DbType.Int32)] OutputParameter<int> userId);
}

// 使用
var userId = new OutputParameter<int>();
var result = await repo.InsertUserAsync("Alice", 25, userId);
Console.WriteLine($"插入了 {result} 行，ID: {userId.Value}");
```

### InputOutput 模式

```csharp
public interface ICounterRepository
{
    // 使用 OutputParameter.WithValue 创建带初始值的参数
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    Task<int> IncrementCounterAsync(
        string name,
        [OutputParameter(DbType.Int32)] OutputParameter<int> value);
}

// 使用
var counter = OutputParameter<int>.WithValue(100);  // 设置初始值
var result = await repo.IncrementCounterAsync("page_views", counter);
Console.WriteLine($"新值: {counter.Value}"); // 输出: 101
```

### 多个输出参数

```csharp
public interface IOrderRepository
{
    [SqlTemplate(@"
        INSERT INTO orders (customer_name, total, created_at) 
        VALUES (@customerName, @total, datetime('now'));
        SELECT last_insert_rowid(), datetime('now')
    ")]
    Task<int> CreateOrderAsync(
        string customerName,
        decimal total,
        [OutputParameter(DbType.Int32)] OutputParameter<int> orderId,
        [OutputParameter(DbType.String, Size = 50)] OutputParameter<string> timestamp);
}

// 使用
var orderId = new OutputParameter<int>();
var timestamp = new OutputParameter<string>();
var result = await repo.CreateOrderAsync("Bob", 99.99m, orderId, timestamp);
Console.WriteLine($"订单 {orderId.Value} 创建于 {timestamp.Value}");
```

### OutputParameter<T> API

```csharp
// 创建输出参数（Output 模式）
var param = new OutputParameter<int>();

// 创建带初始值的参数（InputOutput 模式）
var param = new OutputParameter<int>(100);
var param = OutputParameter<int>.WithValue(100);

// 读取值
int value = param.Value;
bool hasValue = param.HasValue;

// 隐式转换
int value = param;  // 自动转换为 T
```

## 手动使用（SqlBuilder）

也可以在 `SqlBuilder` 中手动添加输出参数：

```csharp
var builder = new SqlBuilder();
builder.Append($"INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid()");
builder.AddParameter("name", "John");

var template = builder.Build();
template.AddOutputParameter("userId", DbType.Int32);

// 执行并获取输出参数
using var cmd = connection.CreateCommand();
cmd.CommandText = template.Sql;

// 绑定输入参数
foreach (var param in template.Parameters)
{
    var p = cmd.CreateParameter();
    p.ParameterName = "@" + param.Key;
    p.Value = param.Value ?? DBNull.Value;
    cmd.Parameters.Add(p);
}

// 绑定输出参数
foreach (var outParam in template.OutputParameters)
{
    var p = cmd.CreateParameter();
    p.ParameterName = "@" + outParam.Key;
    p.DbType = outParam.Value;
    p.Direction = ParameterDirection.Output;
    cmd.Parameters.Add(p);
}

await cmd.ExecuteNonQueryAsync();
var userId = cmd.Parameters["@userId"].Value;
Console.WriteLine($"User ID: {userId}");
```

## 事务支持

输出参数在事务中正常工作：

```csharp
using var transaction = connection.BeginTransaction();
repo.Transaction = transaction;

try
{
    // 同步方法
    repo.InsertAndGetId("User1", 25, out int id1);
    repo.InsertAndGetId("User2", 30, out int id2);
    
    // 异步方法
    var id3 = new OutputParameter<int>();
    await repo.InsertUserAsync("User3", 35, id3);
    
    Console.WriteLine($"Created users: {id1}, {id2}, {id3.Value}");
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## 数据库方言支持

输出参数在所有支持的数据库方言中工作：

### SQLite

```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid()")]
int Insert(string name, [OutputParameter(DbType.Int32)] out int id);
```

### SQL Server

```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name); SELECT SCOPE_IDENTITY()")]
int Insert(string name, [OutputParameter(DbType.Int32)] out int id);
```

### PostgreSQL

```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name) RETURNING id")]
int Insert(string name, [OutputParameter(DbType.Int32)] out int id);
```

### MySQL

```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name); SELECT LAST_INSERT_ID()")]
int Insert(string name, [OutputParameter(DbType.Int32)] out int id);
```

## 支持的 DbType

`[OutputParameter]` 特性支持所有 `System.Data.DbType` 枚举值：

- `DbType.Int32`, `DbType.Int64`, `DbType.Int16`
- `DbType.String`, `DbType.AnsiString`
- `DbType.Decimal`, `DbType.Double`, `DbType.Single`
- `DbType.DateTime`, `DbType.DateTime2`, `DbType.Date`, `DbType.Time`
- `DbType.Boolean`
- `DbType.Guid`
- `DbType.Binary`
- 等等...

## 性能特性

- ✅ **零反射** - 编译时生成所有代码
- ✅ **类型安全** - 编译时验证参数类型
- ✅ **AOT 兼容** - 完全支持 Native AOT
- ✅ **高性能** - 与手写代码性能相同
- ✅ **零开销** - 无运行时开销

## 限制和注意事项

1. **C# 语言限制** - 异步方法不能使用 `out`/`ref` 参数，需使用 `OutputParameter<T>` 包装类
2. **参数名称** - 输出参数名称必须与 SQL 中的参数名称匹配
3. **执行顺序** - 输出参数值在 SQL 执行完成后才可用
4. **Nullable 类型** - 支持 nullable 类型（如 `out int?`）
5. **Size 属性** - 仅对字符串和二进制类型有意义

## 完整示例

查看以下示例获取完整的可运行代码：
- [samples/OutputParameterExample.cs](../samples/OutputParameterExample.cs) - 同步方法示例
- [samples/StoredProcedureOutputExample.cs](../samples/StoredProcedureOutputExample.cs) - 存储过程示例（同步+异步）

## 相关文档

- [API 参考](api-reference.md#outputparameter)
- [源生成器](source-generators.md)
- [SQL 模板](sql-templates.md)


## 与多结果集结合使用

输出参数可以与多结果集（tuple 返回值）结合使用，在一次数据库调用中获取多种类型的数据：

```csharp
public interface IUserRepository
{
    // 同时使用输出参数和元组返回
    [SqlTemplate(@"
        INSERT INTO users (name, created_at) VALUES (@name, @createdAt);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(
        string name,
        [OutputParameter(DbType.DateTime)] ref DateTime createdAt);
}

// 使用
DateTime created = DateTime.UtcNow;
var (rows, userId, total) = repo.InsertAndGetStats("Bob", ref created);
Console.WriteLine($"插入了 {rows} 行，用户 ID: {userId}，总用户数: {total}，创建时间: {created}");
```

**注意**: 此功能需要数据库支持输出参数（如 SQL Server、PostgreSQL、MySQL）。SQLite 不支持输出参数。

详细信息请参阅 [多结果集文档](multiple-result-sets.md)。
