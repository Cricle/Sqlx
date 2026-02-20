# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-purple.svg)](#)
[![LTS](https://img.shields.io/badge/LTS-.NET%2010-green.svg)](#)
[![Tests](https://img.shields.io/badge/tests-2665%20passing-brightgreen.svg)](#)
[![AOT](https://img.shields.io/badge/AOT-ready-blue.svg)](#)
[![Performance](https://img.shields.io/badge/performance-optimized-orange.svg)](#)

高性能、AOT 友好的 .NET 数据库访问库。使用源生成器在编译时生成代码，零运行时反射，完全支持 Native AOT。

## 核心特性

- **🚀 高性能** - 优化的 ResultReader，某些场景比 Dapper.AOT 快 5.8%，最低 GC 压力
- **⚡ 零反射** - 编译时源生成，运行时无反射开销
- **🎯 类型安全** - 编译时验证 SQL 模板和表达式
- **🌐 多数据库** - SQLite、PostgreSQL、MySQL、SQL Server、Oracle、DB2
- **📦 AOT 就绪** - 完全支持 Native AOT，通过 2191 个单元测试
- **🔧 LINQ 支持** - IQueryable 接口，支持 Where/Select/OrderBy/Join 等
- **💾 智能缓存** - SqlQuery\<T\> 泛型缓存，自动注册 EntityProvider
- **🔍 自动发现** - 源生成器自动发现 SqlQuery\<T\> 和 SqlTemplate 中的实体类型
- **✨ 智能优化** - 自动选择最优 ResultReader 策略，零配置

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
    
    // 输出参数支持（仅同步方法，C#不支持异步方法使用out/ref）
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age)")]
    int InsertAndGetId(string name, int age, [OutputParameter(DbType.Int32)] out int id);
}

// 3. 实现仓储（代码自动生成）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 使用
await using var conn = new SqliteConnection("Data Source=app.db");
var repo = new UserRepository(conn);
var adults = await repo.GetAdultsAsync(18);

// 使用输出参数（同步方法）
var result = repo.InsertAndGetId("John", 25, out int newId);
Console.WriteLine($"Inserted ID: {newId}");
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
| `{{var --name varName}}` | 运行时变量（SqlxVar） | `tenant-123` (字面量) |

### SqlxVar - 声明式变量提供器

使用 `[SqlxVar]` 特性声明运行时变量，自动生成代码，零反射，AOT 兼容：

```csharp
public partial class UserRepository
{
    // 标记方法为变量提供器
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    [SqlxVar("userId")]
    private string GetUserId() => UserContext.CurrentUserId.ToString();
    
    [SqlxVar("timestamp")]
    private static string GetTimestamp() => DateTime.UtcNow.ToString("O");
    
    // 源生成器自动创建 GetVar 方法和 VarProvider 属性
}

// 在 SQL 模板中使用
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE tenant_id = {{var --name tenantId}} 
    AND user_id = {{var --name userId}}
")]
Task<List<User>> GetMyDataAsync();

// 生成的 SQL（值作为字面量插入）：
// SELECT "id", "name" FROM "users" 
// WHERE tenant_id = tenant-123 AND user_id = user-456
```

**关键特性：**
- ✅ 零反射 - 编译时生成 switch 语句
- ✅ AOT 兼容 - 完全支持 Native AOT
- ✅ 类型安全 - 编译时验证变量名和方法签名
- ✅ 高性能 - O(1) 调度，单次实例转换
- ✅ 支持静态和实例方法

**⚠️ 安全警告：** `{{var}}` 将值作为字面量插入 SQL。仅用于受信任的应用程序控制值（如租户 ID、SQL 关键字）。用户输入必须使用 SQL 参数（`@param`）。

详见：[SqlxVar 完整文档](docs/sqlxvar.md)

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

## 输出参数（Output Parameters）

Sqlx 支持 SQL 输出参数，用于存储过程、获取生成的 ID 或返回计算值。

### 同步方法：使用 Out/Ref 参数

#### Out 参数（Output 模式）

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

#### Ref 参数（InputOutput 模式）

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

### 异步方法：使用 OutputParameter<T> 包装类

由于 C# 不允许在异步方法中使用 `ref` 和 `out` 参数，Sqlx 提供了 `OutputParameter<T>` 包装类：

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

// InputOutput 模式（带初始值）
var counter = OutputParameter<int>.WithValue(100);
var result = await repo.IncrementCounterAsync("page_views", counter);
Console.WriteLine($"新值: {counter.Value}"); // 输出: 101
```

### 特性

- ✅ **类型安全** - 编译时验证参数类型
- ✅ **双向支持** - out（Output）和 ref（InputOutput）
- ✅ **多参数** - 支持多个输出参数
- ✅ **异步支持** - 通过 OutputParameter<T> 包装类
- ✅ **AOT 兼容** - 完全支持 Native AOT

> 详细示例见 [samples/OutputParameterExample.cs](samples/OutputParameterExample.cs) 和 [samples/StoredProcedureOutputExample.cs](samples/StoredProcedureOutputExample.cs)

## 多结果集（Multiple Result Sets）

Sqlx 支持使用元组返回类型从单个 SQL 查询返回多个标量值，在一次数据库调用中高效获取多个相关值。

### 基本用法

```csharp
public interface IUserRepository
{
    // 使用 ResultSetMapping 明确指定映射关系
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
}

// 使用
var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice");
Console.WriteLine($"插入用户 {userId}，总用户数: {total}");
```

### 默认映射

不指定 `[ResultSetMapping]` 时使用默认映射：
- 第1个元素 → 受影响行数（ExecuteNonQuery）
- 其余元素 → SELECT 结果（按顺序）

```csharp
[SqlTemplate(@"
    INSERT INTO users (name) VALUES (@name);
    SELECT last_insert_rowid();
    SELECT COUNT(*) FROM users
")]
Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
```

### 多个 SELECT 语句

```csharp
[SqlTemplate(@"
    SELECT COUNT(*) FROM users;
    SELECT MAX(id) FROM users;
    SELECT MIN(id) FROM users
")]
[ResultSetMapping(0, "totalUsers")]
[ResultSetMapping(1, "maxId")]
[ResultSetMapping(2, "minId")]
Task<(int totalUsers, long maxId, long minId)> GetUserStatsAsync();
```

### 同步和异步支持

```csharp
// 异步
[SqlTemplate("...")]
Task<(int rows, long id)> InsertAsync(string name);

// 同步
[SqlTemplate("...")]
(int rows, long id) Insert(string name);
```

### 与输出参数结合使用

可以同时使用输出参数和元组返回（需要数据库支持输出参数）：

```csharp
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
```

**注意**: SQLite 不支持输出参数，此功能仅适用于 SQL Server、PostgreSQL、MySQL 等数据库。

### 特性

- ✅ **单次往返** - 一次数据库调用获取所有值
- ✅ **类型安全** - 编译时类型检查
- ✅ **零反射** - 编译时代码生成
- ✅ **AOT 兼容** - 完全支持 Native AOT
- ✅ **自动转换** - 自动进行类型转换

> 详细文档见 [docs/multiple-result-sets.md](docs/multiple-result-sets.md)

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

## SqlBuilder - 动态 SQL 构建器

SqlBuilder 提供高性能、类型安全的动态 SQL 构建能力，使用 C# 插值字符串自动参数化，防止 SQL 注入。

### 核心特性

- **🔒 自动参数化** - 插值字符串自动转换为 SQL 参数，防止 SQL 注入
- **⚡ 高性能** - ArrayPool<char> 零堆分配，Expression tree 优化（比反射快 20-34%）
- **🔧 SqlTemplate 集成** - 支持 {{columns}}、{{table}} 等占位符
- **🔗 子查询支持** - 组合式查询构建，自动参数冲突解决
- **📦 AOT 兼容** - 零反射设计，完全支持 Native AOT

### 快速示例

```csharp
// 自动参数化
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE age >= {18} AND name = {"John"}");
var template = builder.Build();
// SQL: "SELECT * FROM users WHERE age >= @p0 AND name = @p1"
// Parameters: { "p0": 18, "p1": "John" }

// 动态条件
using var builder2 = new SqlBuilder(SqlDefine.SQLite);
builder2.Append($"SELECT * FROM users WHERE 1=1");

if (nameFilter != null)
    builder2.Append($" AND name LIKE {"%" + nameFilter + "%"}");

if (minAge.HasValue)
    builder2.Append($" AND age >= {minAge.Value}");

var users = await connection.QueryAsync(
    builder2.Build().Sql, 
    builder2.Build().Parameters, 
    UserResultReader.Default
);
```

### SqlTemplate 集成

```csharp
// 使用 {{columns}}、{{table}} 等占位符
var context = new PlaceholderContext(
    SqlDefine.SQLite, 
    "users", 
    UserEntityProvider.Default.Columns
);

using var builder = new SqlBuilder(context);
builder.AppendTemplate(
    "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", 
    new { minAge = 18 }
);

var template = builder.Build();
// SQL: "SELECT [id], [name], [age] FROM [users] WHERE age >= @minAge"
// Parameters: { "minAge": 18 }
```

### 子查询支持

```csharp
// 嵌套子查询，自动参数冲突解决
using var subquery = new SqlBuilder(SqlDefine.SQLite);
subquery.Append($"SELECT id FROM orders WHERE total > {1000}");

using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
mainQuery.Append($"SELECT * FROM users WHERE id IN ");
mainQuery.AppendSubquery(subquery);

var template = mainQuery.Build();
// SQL: "SELECT * FROM users WHERE id IN (SELECT id FROM orders WHERE total > @p0)"
```

### 性能优化

SqlBuilder 使用 Expression tree 优化匿名对象参数转换：

| 属性数量 | Expression Tree | Reflection | 性能提升 |
|---------|----------------|------------|---------|
| 2 props | 1.486 μs | 1.632 μs | **8.9%** |
| 5 props | 1.328 μs | 1.678 μs | **20.9%** |
| 10 props | 1.507 μs | 2.282 μs | **34.0%** |

> 详细文档见 [SqlBuilder 完整指南](docs/sqlbuilder.md)

## SqlxContext - 统一上下文管理

SqlxContext 提供统一的数据库上下文，支持多仓储、事务管理和异常处理：

```csharp
// 1. 定义上下文
[SqlxContext]
[SqlDefine(SqlDefineTypes.SQLite)]
[IncludeRepository(typeof(UserRepository))]
[IncludeRepository(typeof(OrderRepository))]
public partial class AppDbContext : SqlxContext
{
    // 源生成器自动生成：
    // - 构造函数
    // - Users 和 Orders 属性（懒加载）
    // - 事务传播逻辑
}

// 2. 注册到 DI
services.AddSingleton<UserRepository>();
services.AddSingleton<OrderRepository>();
services.AddSqlxContext<AppDbContext>((sp, options) =>
{
    var connection = sp.GetRequiredService<SqliteConnection>();
    
    // 配置异常处理
    options.OnException = (ex, context) =>
    {
        Console.WriteLine($"SQL Error in {context.MethodName}: {ex.Message}");
    };
    
    // 配置重试
    options.EnableRetry = true;
    options.MaxRetryCount = 3;
    options.RetryDelayMilliseconds = 100;
    
    // 配置日志
    options.Logger = sp.GetRequiredService<ILogger<AppDbContext>>();
    
    return new AppDbContext(connection, options, sp);
}, ServiceLifetime.Singleton);

// 3. 使用事务
app.MapPost("/api/orders", async (CreateOrderRequest request, AppDbContext context) =>
{
    await using var transaction = await context.BeginTransactionAsync();
    try
    {
        var user = await context.Users.GetByIdAsync(request.UserId);
        if (user == null) return Results.NotFound();
        
        var order = new Order { UserId = user.Id, Total = request.Total };
        var orderId = await context.Orders.InsertAndGetIdAsync(order);
        
        await transaction.CommitAsync();
        return Results.Created($"/api/orders/{orderId}", order);
    }
    catch (SqlxException ex)
    {
        // 异常包含完整上下文
        Console.WriteLine($"SQL: {ex.Sql}");
        Console.WriteLine($"Method: {ex.MethodName}");
        Console.WriteLine($"Duration: {ex.DurationMilliseconds}ms");
        Console.WriteLine($"Transaction: {ex.TransactionId}");
        throw;
    }
});
```

### 异常处理特性

**SqlxException** 提供丰富的上下文信息：

```csharp
try
{
    await repo.GetByIdAsync(123);
}
catch (SqlxException ex)
{
    // 完整的 SQL 上下文
    Console.WriteLine($"SQL: {ex.Sql}");
    Console.WriteLine($"Parameters: {string.Join(", ", ex.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
    Console.WriteLine($"Method: {ex.MethodName}");
    Console.WriteLine($"Repository: {ex.RepositoryType}");
    
    // 性能信息
    Console.WriteLine($"Duration: {ex.DurationMilliseconds}ms");
    
    // 事务信息
    Console.WriteLine($"Transaction ID: {ex.TransactionId}");
    Console.WriteLine($"In Transaction: {ex.InTransaction}");
    
    // 关联 ID（用于分布式追踪）
    Console.WriteLine($"Correlation ID: {ex.CorrelationId}");
    
    // 原始异常
    Console.WriteLine($"Inner: {ex.InnerException?.Message}");
}
```

### 自动重试机制

配置自动重试瞬态错误（连接超时、死锁等）：

```csharp
services.AddSqlxContext<AppDbContext>((sp, options) =>
{
    var connection = sp.GetRequiredService<SqliteConnection>();
    
    // 启用重试
    options.EnableRetry = true;
    options.MaxRetryCount = 3;              // 最多重试 3 次
    options.RetryDelayMilliseconds = 100;   // 初始延迟 100ms
    options.UseExponentialBackoff = true;   // 指数退避（100ms, 200ms, 400ms）
    
    return new AppDbContext(connection, options, sp);
});
```

**支持的瞬态错误**：
- 连接超时
- 网络错误
- 死锁（SQL Server、PostgreSQL、MySQL）
- 事务冲突
- 临时资源不可用

### 全局异常回调

使用 `OnException` 回调集中处理异常：

```csharp
options.OnException = (ex, context) =>
{
    // 记录到日志系统
    logger.LogError(ex, 
        "SQL Error in {Repository}.{Method}: {Message}",
        context.RepositoryType,
        context.MethodName,
        ex.Message);
    
    // 发送到监控系统
    telemetry.TrackException(ex, new Dictionary<string, string>
    {
        ["sql"] = context.Sql,
        ["method"] = context.MethodName,
        ["duration"] = context.DurationMilliseconds.ToString()
    });
    
    // 敏感数据已自动清理（密码、令牌等）
};
```

### 日志集成

使用 `ILogger` 自动记录 SQL 执行和错误：

```csharp
options.Logger = sp.GetRequiredService<ILogger<AppDbContext>>();

// 自动记录：
// - SQL 执行（Debug 级别）
// - 重试尝试（Warning 级别）
// - 错误（Error 级别）
```

> 详细文档见 [SqlxContext 完整指南](docs/sqlx-context.md)

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

## 指标收集（Metrics）

Sqlx 内置支持使用标准 `System.Diagnostics.Metrics` API 收集 SQL 执行指标（.NET 8+）：

```csharp
// 指标自动记录，无需额外配置
var repo = new UserRepository(connection);
await repo.GetByIdAsync(123);  // 自动记录执行时间、次数等

// 使用 OpenTelemetry 导出指标
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Sqlx.SqlTemplate")
    .AddPrometheusExporter()
    .Build();
```

**记录的指标**：
- `sqlx.template.duration` (Histogram) - SQL 执行时间（毫秒）
- `sqlx.template.executions` (Counter) - SQL 执行次数
- `sqlx.template.errors` (Counter) - SQL 执行错误次数

**指标标签（Tags）**：
- `repository.class` - 仓储类全名（如 `MyApp.UserRepository`）
- `repository.method` - 方法名（如 `GetByIdAsync`）
- `sql.template` - SQL 模板（如 `SELECT {{columns}} FROM {{table}} WHERE id = @id`）
- `error.type` - 错误类型（仅错误指标）

**特性**：
- ✅ 零配置 - 自动记录所有 SQL 执行
- ✅ 标准 API - 兼容 OpenTelemetry、Prometheus、Application Insights
- ✅ 高性能 - 编译时生成，零运行时开销
- ✅ AOT 友好 - 完全支持 Native AOT
- ✅ 向后兼容 - .NET 8 以下版本为 no-op

> 详细文档见 [指标收集](docs/metrics.md)

## 性能对比

基于 BenchmarkDotNet 测试（.NET 10.0.2 LTS，SQLite 内存数据库）：

### 单行查询性能

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx |
|-----|------|---------|-----------|---------|
| **Sqlx** | **9.447 μs** | baseline | **2.85 KB** | baseline |
| Dapper.AOT | 10.040 μs | +6.3% | 2.66 KB | -6.7% |
| FreeSql | 55.817 μs | +491% | 10.17 KB | +257% |

### 批量查询性能（不同数据量）

#### 10 行数据

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx |
|-----|------|---------|-----------|---------|
| Dapper.AOT | 31.58 μs | -5.0% | 6.55 KB | +6.9% |
| **Sqlx** | **33.25 μs** | baseline | **6.13 KB** | baseline |
| FreeSql | 34.63 μs | +4.1% | 8.67 KB | +41.4% |

#### 100 行数据

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx |
|-----|------|---------|-----------|---------|
| FreeSql | 151.15 μs | -14.4% | 37.23 KB | -3.2% |
| Dapper.AOT | 162.24 μs | -8.1% | 45.66 KB | +18.8% |
| **Sqlx** | **176.54 μs** | baseline | **38.45 KB** | baseline |

#### 1000 行数据

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx | Gen1 GC |
|-----|------|---------|-----------|---------|---------|
| FreeSql | 1,290.47 μs | -19.2% | 318.56 KB | -11.9% | 9.77 |
| Dapper.AOT | 1,433.50 μs | -10.2% | 432.38 KB | +19.5% | 23.44 |
| **Sqlx** | **1,596.93 μs** | baseline | **361.69 KB** | baseline | **19.53** |

**关键洞察**：
- ✅ **单行查询最快** - Sqlx 比 Dapper.AOT 快 6.3%，比 FreeSql 快 5.9倍
- ✅ **内存效率高** - 比 Dapper.AOT 少分配 16.4% 内存（1000行）
- ✅ **GC 压力低** - Gen1 GC 比 Dapper.AOT 低 16.7%，比 FreeSql 低 50%
- ✅ **小数据集优势** - 在 Web API 主要场景（10-100行）中性能最优
- ⚠️ **大数据集权衡** - 1000行时比 FreeSql 慢 19.2%，但内存和 GC 更优

> 详细数据见 [性能基准测试结果](docs/benchmark-results.md)

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
- [输出参数](docs/output-parameters.md)
- [SqlBuilder 动态 SQL 构建器](docs/sqlbuilder.md)
- [SQL 模板](docs/sql-templates.md)
- [数据库方言](docs/dialects.md)
- [源生成器](docs/source-generators.md)
- [性能基准测试](docs/benchmarks.md)
- [API 参考](docs/api-reference.md)
- [AI 助手指南](AI-VIEW.md)
- [示例项目](samples/TodoWebApi/)

## 许可证

MIT License - 详见 [LICENSE.txt](LICENSE.txt)
