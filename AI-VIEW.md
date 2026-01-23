# Sqlx AI 助手指南

> 面向 AI 助手的 Sqlx 使用指南，帮助快速理解和生成正确代码。

## 概述

Sqlx 是编译时源生成器，生成高性能数据访问代码。核心流程：

```
接口定义 + [SqlTemplate] → 源生成器 → partial class 实现
```

## 完整示例（从零开始）

### 1. 安装和引用

```bash
# 安装 NuGet 包
dotnet add package Sqlx
```

```csharp
// 文件顶部必需的 using 命名空间
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sqlx;                          // 核心命名空间
using Sqlx.Annotations;              // 特性标记
```

### 2. 定义实体类

```csharp
using Sqlx.Annotations;

namespace MyApp.Models;

// [Sqlx] 标记告诉源生成器生成支持代码
[Sqlx]
// [TableName] 指定数据库表名（标注在实体类上）
[TableName("users")]
public class User
{
    // [Key] 标记主键（通常是自增 ID）
    [Key]
    public long Id { get; set; }
    
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // 可空属性（string? 或 int?）
    public string? Email { get; set; }
    public int? Score { get; set; }
}
```

**关键说明：**
- `[Sqlx]` - 标注在实体类上，生成 EntityProvider/ResultReader/ParameterBinder
- `[TableName("table_name")]` - 标注在实体类上，指定数据库表名
- `[Key]` - 标注在主键属性上，INSERT/UPDATE 时自动排除

### 3. 定义仓储接口

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace MyApp.Repositories;

// 继承 ICrudRepository<TEntity, TKey> 获得内置的 CRUD 方法
// TEntity: 实体类型, TKey: 主键类型
public interface IUserRepository : ICrudRepository<User, long>
{
    // [SqlTemplate] 定义 SQL 模板（标注在接口方法上）
    
    // 简单查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge, CancellationToken cancellationToken = default);
    
    // 条件查询（使用 {{if}} 占位符）
    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table}} 
        WHERE is_active = {{bool_true}}
        {{if notnull=name}}AND name LIKE @name{{/if}}
        {{if notnull=minAge}}AND age >= @minAge{{/if}}
        ORDER BY name ASC
    ")]
    Task<List<User>> SearchAsync(string? name, int? minAge, CancellationToken cancellationToken = default);
    
    // 字典条件查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
    Task<List<User>> SearchByFilterAsync(IReadOnlyDictionary<string, object?> filter);
    
    // 表达式查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
    
    // 插入并返回自增 ID
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]  // 标注返回插入的 ID
    Task<long> InsertAndGetIdAsync(User user, CancellationToken cancellationToken = default);
    
    // 更新（排除 Id 和 CreatedAt）
    [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default);
    
    // 调试方法：返回 SqlTemplate 类型查看生成的 SQL
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    SqlTemplate GetByIdSql(long id);
}
```

**关键说明：**
- `[SqlTemplate("SQL")]` - 标注在接口方法上，定义 SQL 模板
- `[ReturnInsertedId]` - 标注在 INSERT 方法上，返回自增 ID
- 所有方法必须返回 `Task<T>` 或 `SqlTemplate`（调试用）
- `CancellationToken` 参数是可选的，建议添加

### 4. 实现仓储类

```csharp
using System.Data.Common;
using Sqlx.Annotations;

namespace MyApp.Repositories;

// [SqlDefine] 指定数据库方言（标注在实现类上）
[SqlDefine(SqlDefineTypes.SQLite)]
// [RepositoryFor] 指定要实现的接口（标注在实现类上）
[RepositoryFor(typeof(IUserRepository))]
// partial class - 源生成器会生成另一半实现
public partial class UserRepository(DbConnection connection) : IUserRepository
{
    // 连接获取优先级：方法参数 > 字段 > 属性 > 主构造函数
    
    // 方式1：显式声明字段（推荐，优先级最高）
    private readonly DbConnection _connection = connection;
    public DbTransaction? Transaction { get; set; }
    
    // 方式2：使用属性（优先级次之）
    // public DbConnection Connection { get; } = connection;
    // public DbTransaction? Transaction { get; set; }
    
    // 方式3：不声明字段/属性，使用主构造函数（最简洁，优先级最低）
    // 源生成器会自动生成：
    // private readonly DbConnection _connection = connection;
    // public DbTransaction? Transaction { get; set; }
    
    // 不需要写任何方法实现！
    // 源生成器会自动生成所有接口方法的实现
    
    // 可选：重写拦截器方法
#if !SQLX_DISABLE_INTERCEPTOR
    partial void OnExecuting(string operationName, DbCommand command, SqlTemplate template)
    {
        // 执行前拦截，可记录日志
        Console.WriteLine($"Executing: {operationName}");
        Console.WriteLine($"SQL: {command.CommandText}");
    }
    
    partial void OnExecuted(string operationName, DbCommand command, SqlTemplate template, object? result, long elapsedTicks)
    {
        // 执行后拦截
        var ms = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"Executed: {operationName} in {ms:F2}ms");
    }
    
    partial void OnExecuteFail(string operationName, DbCommand command, SqlTemplate template, Exception exception, long elapsedTicks)
    {
        // 执行失败拦截
        Console.WriteLine($"Failed: {operationName} - {exception.Message}");
    }
#endif
}
```

**关键说明：**
- `[SqlDefine(SqlDefineTypes.XXX)]` - 标注在实现类上，指定数据库方言
- **连接优先级**：方法参数 > 字段 > 属性 > 主构造函数
  - 方法参数：最灵活，可为特定方法指定不同连接
  - 字段：推荐方式，明确且易于调试
  - 属性：适合需要外部访问连接的场景
  - 主构造函数：最简洁，自动生成字段和 Transaction 属性
- `[RepositoryFor(typeof(IXxx))]` - 标注在实现类上，指定要实现的接口
- 类必须声明为 `partial class`
- 构造函数接收 `DbConnection` 参数
- **两种字段声明方式都支持**：
  - 显式声明：`private readonly DbConnection _connection = connection;`（推荐）
  - 隐式使用：直接使用主构造函数参数 `connection`（也可以）
- 不需要手写任何方法实现，源生成器自动生成

**[RepositoryFor] 的高级用法：**

```csharp
// 方式1：使用单独的特性
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection conn) : IUserRepository { }

// 方式2：在 [RepositoryFor] 中指定所有配置
[RepositoryFor(typeof(IUserRepository), 
    Dialect = SqlDefineTypes.PostgreSql,   // 数据库方言
    TableName = "app_users")]              // 表名（覆盖实体类的 [TableName]）
public partial class PgUserRepository(DbConnection conn) : IUserRepository { }
```

### 5. 使用仓储

```csharp
using Microsoft.Data.Sqlite;
using MyApp.Models;
using MyApp.Repositories;

// 创建数据库连接
using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

// 创建仓储实例
var userRepo = new UserRepository(connection);

// 使用 ICrudRepository 内置方法
var user = await userRepo.GetByIdAsync(1);
var allUsers = await userRepo.GetAllAsync(limit: 100);
var activeUsers = await userRepo.GetWhereAsync(u => u.IsActive && u.Age >= 18);
var count = await userRepo.CountAsync();

// 使用自定义方法
var adults = await userRepo.GetAdultsAsync(minAge: 18);

var searchResults = await userRepo.SearchAsync(
    name: "%John%",   // LIKE 模糊查询
    minAge: 25
);

// 字典条件查询
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",
    ["IsActive"] = true,
    ["Age"] = 25
};
var users = await userRepo.SearchByFilterAsync(filter);

// 插入
var newUser = new User
{
    Name = "Alice",
    Age = 30,
    IsActive = true,
    CreatedAt = DateTime.Now
};
long insertedId = await userRepo.InsertAndGetIdAsync(newUser);
newUser.Id = insertedId;

// 更新
newUser.Age = 31;
await userRepo.UpdateAsync(newUser);

// 删除
await userRepo.DeleteAsync(newUser.Id);

// 调试：查看生成的 SQL
var sqlTemplate = userRepo.GetByIdSql(123);
Console.WriteLine($"SQL: {sqlTemplate.Sql}");
Console.WriteLine($"Has dynamic placeholders: {sqlTemplate.HasDynamicPlaceholders}");
```

## 核心特性速查表

### 标注位置说明

| 特性 | 标注位置 | 用途 | 示例 |
|------|---------|------|------|
| `[Sqlx]` | **实体类** | 生成 EntityProvider/ResultReader/ParameterBinder | `[Sqlx] public class User { }` |
| `[TableName("xxx")]` | **实体类** 或 **仓储类** | 指定数据库表名 | `[TableName("users")] public class User { }` |
| `[Key]` | **实体属性** | 标记主键（INSERT/UPDATE 自动排除） | `[Key] public long Id { get; set; }` |
| `[SqlDefine(XXX)]` | **仓储类** | 指定数据库方言 | `[SqlDefine(SqlDefineTypes.SQLite)] public partial class Repo { }` |
| `[RepositoryFor(typeof(I))]` | **仓储类** | 指定要实现的接口 | `[RepositoryFor(typeof(IUserRepo))] public partial class UserRepo { }` |
| `[SqlTemplate("SQL")]` | **接口方法** | 定义 SQL 模板 | `[SqlTemplate("SELECT ...")] Task<List<User>> GetAllAsync();` |
| `[ReturnInsertedId]` | **接口方法（INSERT）** | 返回自增 ID | `[SqlTemplate("INSERT ...")] [ReturnInsertedId] Task<long> InsertAsync(User u);` |
| `[Column("col_name")]` | **实体属性** | 指定列名映射 | `[Column("user_name")] public string Name { get; set; }` |

### SqlTemplate 的两种形式

**重要说明：** Sqlx 中有两个不同的 `SqlTemplate`，用途完全不同：

| 类型 | 命名空间 | 用途 | 使用场景 |
|------|---------|------|---------|
| **`[SqlTemplate]` 特性** | `Sqlx.Annotations` | 编译时标注接口方法，定义 SQL 模板 | 在接口方法上使用，源生成器会生成实现代码 |
| **`SqlTemplate` 类** | `Sqlx` | 运行时类，表示已解析的 SQL 模板对象 | 调试时返回此类型查看生成的 SQL |

**使用示例：**

```csharp
using Sqlx;                    // SqlTemplate 类
using Sqlx.Annotations;        // [SqlTemplate] 特性

public interface IUserRepository
{
    // 1. [SqlTemplate] 特性 - 标注方法，定义 SQL 模板
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    // 2. SqlTemplate 类 - 返回类型，用于调试查看生成的 SQL
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    SqlTemplate GetByIdSql(long id);  // 不执行查询，只返回 SQL 模板对象
}

// 使用调试方法
var sqlTemplate = repo.GetByIdSql(123);
Console.WriteLine($"Prepared SQL: {sqlTemplate.Sql}");
Console.WriteLine($"Has dynamic placeholders: {sqlTemplate.HasDynamicPlaceholders}");
```

**关键区别：**
- **`[SqlTemplate]` 特性**：用 `[]` 包裹，标注在方法上，告诉源生成器生成代码
- **`SqlTemplate` 类**：作为返回类型，用于调试和查看生成的 SQL

### 必需的 using 命名空间

```csharp
// 实体类文件
using Sqlx.Annotations;        // [Sqlx], [TableName], [Key], [Column]

// 接口文件  
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sqlx;                    // ICrudRepository<TEntity, TKey>, SqlTemplate
using Sqlx.Annotations;        // [SqlTemplate], [ReturnInsertedId]

// 仓储实现文件
using System.Data.Common;      // DbConnection
using Sqlx.Annotations;        // [SqlDefine], [RepositoryFor], [TableName]

// 使用文件
using System.Data;
using Microsoft.Data.Sqlite;  // 或其他数据库的连接类
using Sqlx;                    // 扩展方法

## 源生成器自动发现

源生成器会自动发现并生成以下类型的 EntityProvider/ResultReader/ParameterBinder：

1. **`[Sqlx]` 标记的类** - 显式标记
2. **`SqlQuery<T>` 泛型参数** - 使用 SqlQuery 构建器时自动发现
3. **`[SqlTemplate]` 方法返回值** - 支持 `Task<T>`, `Task<List<T>>`, `Task<T?>` 等
4. **`[SqlTemplate]` 方法参数** - 非基元类型参数自动发现

```csharp
// 1. 显式标记
[Sqlx]
public class User { ... }

// 2. SqlQuery<T> 自动发现
var query = SqlQuery<Order>.ForSqlite();  // Order 自动生成

// 3. SqlTemplate 返回值自动发现
[SqlTemplate("SELECT ...")]
Task<List<Product>> GetProductsAsync();  // Product 自动生成

// 4. SqlTemplate 参数自动发现
[SqlTemplate("INSERT ...")]
Task<int> InsertAsync(Customer customer);  // Customer 自动生成
```

## 占位符速查

### 基础占位符

| 占位符 | 输出示例 | 说明 |
|--------|---------|------|
| `{{table}}` | `[users]` / `"users"` / `` `users` `` | 实体表名（自动加引号） |
| `{{table --param tableName}}` | 动态表名（从参数获取） | 运行时动态表名 |
| `{{columns}}` | `[id], [name], [age]` | 所有列名（逗号分隔） |
| `{{columns --exclude Id}}` | `[name], [age]` | 排除指定列 |
| `{{values}}` | `@id, @name, @age` | 所有参数占位符（用于 INSERT） |
| `{{values --exclude Id}}` | `@name, @age` | 排除指定参数 |
| `{{values --inline CreatedAt=CURRENT_TIMESTAMP}}` | `@id, @name, CURRENT_TIMESTAMP` | 内联表达式（用于 INSERT 默认值） |
| `{{set}}` | `[name] = @name, [age] = @age` | SET 子句（用于 UPDATE） |
| `{{set --exclude Id CreatedAt}}` | `[name] = @name, [age] = @age` | 排除不可更新的字段 |
| `{{set --inline Version=Version+1}}` | `[name] = @name, [version] = [version]+1` | 内联表达式（用于 UPDATE 计算字段） |
| `{{arg --param name}}` | `@name` / `:name` / `$1` | 单个参数占位符（方言适配） |
| `{{where --param predicate}}` | 动态 WHERE 子句（从表达式生成） | 表达式查询 |
| `{{where --object filter}}` | 动态 WHERE 子句（从字典生成） | 字典查询 |

### 内联表达式占位符（新功能）

内联表达式允许在 SQL 中使用表达式、函数和字面量，而不仅仅是参数占位符。

| 占位符 | 输出示例 | 说明 |
|--------|---------|------|
| `{{set --inline Version=Version+1}}` | `[version] = [version]+1` | UPDATE 时自动递增 |
| `{{set --inline UpdatedAt=CURRENT_TIMESTAMP}}` | `[updated_at] = CURRENT_TIMESTAMP` | UPDATE 时自动更新时间戳 |
| `{{values --inline CreatedAt=CURRENT_TIMESTAMP}}` | `CURRENT_TIMESTAMP` | INSERT 时自动生成时间戳 |
| `{{values --inline Status='pending'}}` | `'pending'` | INSERT 时设置默认值 |
| `{{values --inline Version=1}}` | `1` | INSERT 时初始化版本号 |
| `{{values --inline Total=@quantity*@unitPrice}}` | `@quantity*@unitPrice` | INSERT 时计算字段 |

**内联表达式规则：**
- 使用 C# 属性名（PascalCase），自动转换为列名
- 支持 SQL 函数、算术运算、字面量
- 参数占位符（@param）会被保留
- 多个表达式用逗号分隔

**使用示例：**

```csharp
// UPDATE 示例：自动递增版本号
[SqlTemplate(@"
    UPDATE {{table}} 
    SET {{set --exclude Id,Version --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
    WHERE id = @id
")]
Task<int> UpdateWithVersionAsync(long id, string name, string email);
// 生成: UPDATE [users] SET [name] = @name, [email] = @email, [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id

// INSERT 示例：设置默认值和时间戳
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns --exclude Id}}) 
    VALUES ({{values --exclude Id --inline Status='pending',Priority=0,CreatedAt=CURRENT_TIMESTAMP}})
")]
Task<int> CreateTaskAsync(string name, string description);
// 生成: INSERT INTO [tasks] ([name], [description], [status], [priority], [created_at]) VALUES (@name, @description, 'pending', 0, CURRENT_TIMESTAMP)

// INSERT 示例：计算字段
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns}}) 
    VALUES ({{values --inline Total=@quantity*@unitPrice}})
")]
Task<int> CreateOrderItemAsync(long id, int quantity, decimal unitPrice);
// 生成: INSERT INTO [order_items] ([id], [quantity], [unit_price], [total]) VALUES (@id, @quantity, @unit_price, @quantity*@unitPrice)
```

### 分页与排序

| 占位符 | 输出示例 | 说明 |
|--------|---------|------|
| `{{limit --param count}}` | `LIMIT @count` (SQLite/MySQL) <br> `TOP @count` (SQL Server) | 限制返回行数 |
| `{{limit --count 10}}` | `LIMIT 10` | 静态限制行数 |
| `{{offset --param skip}}` | `OFFSET @skip` | 跳过行数 |
| `{{offset --count 20}}` | `OFFSET 20` | 静态跳过行数 |

**注意：** Sqlx 不提供 `{{orderby}}` 占位符。排序应该直接在 SQL 中编写或使用 IQueryable 的 `OrderBy()` 方法。

```csharp
// ✅ 正确：直接在 SQL 中写 ORDER BY
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge ORDER BY name ASC")]
Task<List<User>> GetAdultsAsync(int minAge);

// ✅ 正确：使用 IQueryable
var users = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .ToList();

// ❌ 错误：不存在 {{orderby}} 占位符
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby name}}")]  // 错误！
```

### 方言占位符（跨数据库兼容）

| 占位符 | SQLite | PostgreSQL | MySQL | SqlServer | Oracle | DB2 |
|--------|--------|------------|-------|-----------|--------|-----|
| `{{bool_true}}` | `1` | `true` | `1` | `1` | `1` | `1` |
| `{{bool_false}}` | `0` | `false` | `0` | `0` | `0` | `0` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `NOW()` | `GETDATE()` | `SYSTIMESTAMP` | `CURRENT TIMESTAMP` |
| `{{current_date}}` | `CURRENT_DATE` | `CURRENT_DATE` | `CURDATE()` | `CAST(GETDATE() AS DATE)` | `CURRENT_DATE` | `CURRENT DATE` |
| `{{current_time}}` | `CURRENT_TIME` | `CURRENT_TIME` | `CURTIME()` | `CAST(GETDATE() AS TIME)` | `CURRENT_TIMESTAMP` | `CURRENT TIME` |

**方言占位符的优势：** 在 SQL 模板中使用 `{{current_timestamp}}` 而非硬编码 `NOW()` 或 `GETDATE()`，可以让同一份接口在不同数据库实现中正常工作。

### 条件占位符（动态 SQL）

条件占位符允许根据参数是否为 null/empty 动态包含或排除 SQL 片段：

| 条件 | 说明 | 示例 |
|------|------|------|
| `{{if notnull=param}}...{{/if}}` | 参数非 null 时包含 | `{{if notnull=name}}AND name = @name{{/if}}` |
| `{{if null=param}}...{{/if}}` | 参数为 null 时包含 | `{{if null=status}}AND status IS NULL{{/if}}` |
| `{{if notempty=param}}...{{/if}}` | 集合非空时包含 | `{{if notempty=ids}}AND id IN @ids{{/if}}` |
| `{{if empty=param}}...{{/if}}` | 集合为空时包含 | `{{if empty=roles}}AND 1=0{{/if}}` |

**使用示例：**

```csharp
// 灵活搜索：参数为 null 时忽略该条件
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}
    {{if notnull=maxAge}}AND age <= @maxAge{{/if}}
    {{if notempty=roles}}AND role IN @roles{{/if}}
")]
Task<List<User>> SearchAsync(string? name, int? minAge, int? maxAge, List<string>? roles);

// 调用示例
await repo.SearchAsync(name: "%John%", minAge: 18, maxAge: null, roles: null);
// 生成: SELECT ... WHERE 1=1 AND name LIKE @name AND age >= @minAge

await repo.SearchAsync(name: null, minAge: null, maxAge: 65, roles: new() { "admin", "user" });
// 生成: SELECT ... WHERE 1=1 AND age <= @maxAge AND role IN @roles
```

### WHERE 占位符（高级查询）

WHERE 占位符支持两种模式：表达式模式和字典模式。

#### 1. 表达式模式（类型安全）

使用 `Expression<Func<T, bool>>` 构建类型安全的 WHERE 子句：

```csharp
// 接口定义
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);

// 使用示例
var users = await repo.GetWhereAsync(u => 
    u.Age >= 18 && 
    u.Age <= 65 && 
    u.IsActive && 
    u.Name.Contains("John"));
// 生成: WHERE age >= 18 AND age <= 65 AND is_active = 1 AND name LIKE '%John%'

// 支持的表达式运算符
await repo.GetWhereAsync(u => u.Age > 18);           // age > 18
await repo.GetWhereAsync(u => u.Age >= 18);          // age >= 18
await repo.GetWhereAsync(u => u.Age == 18);          // age = 18
await repo.GetWhereAsync(u => u.Age != 18);          // age != 18
await repo.GetWhereAsync(u => u.Name == "John");     // name = 'John'
await repo.GetWhereAsync(u => u.Name.StartsWith("J")); // name LIKE 'J%'
await repo.GetWhereAsync(u => u.Name.EndsWith("n"));   // name LIKE '%n'
await repo.GetWhereAsync(u => u.Name.Contains("oh"));  // name LIKE '%oh%'
```

#### 2. 字典模式（动态条件）

使用 `IReadOnlyDictionary<string, object?>` 构建动态 WHERE 子句：

```csharp
// 接口定义
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> SearchAsync(IReadOnlyDictionary<string, object?> filter);

// 使用示例
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",       // 生成: [name] = @name
    ["Age"] = 25,            // 生成: [age] = @age
    ["IsActive"] = true,     // 生成: [is_active] = @isActive
    ["Email"] = null         // 忽略 null 值
};
var users = await repo.SearchAsync(filter);
// 生成 SQL: WHERE ([name] = @name AND [age] = @age AND [is_active] = @isActive)
```

**字典模式规则：**
- 字典键匹配属性名或列名（不区分大小写）
- null 值会被忽略
- 多个条件用 AND 连接并加括号
- 空字典或全 null 返回 `1=1`

**对比选择：**
- **表达式模式**: 编译时类型检查，智能提示，重构友好
- **字典模式**: 运行时动态构建，适合搜索表单等场景

## 代码模板

### 实体定义

```csharp
[Sqlx]
[TableName("users")]
public class User
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Email { get; set; }  // 可空
}
```

### 仓储接口

```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    // 自定义查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
    
    // 条件查询
    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table}} WHERE 1=1
        {{if notnull=name}}AND name LIKE @name{{/if}}
        {{if notnull=minAge}}AND age >= @minAge{{/if}}
    ")]
    Task<List<User>> SearchAsync(string? name, int? minAge);
    
    // 字典条件查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
    Task<List<User>> SearchByFilterAsync(IReadOnlyDictionary<string, object?> filter);
}
```

### 仓储实现

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

## CRUD 模板

### SELECT

```csharp
// 查询全部
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 按 ID 查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// 分页查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id ASC {{limit --param size}} {{offset --param skip}}")]
Task<List<User>> GetPagedAsync(int size, int skip);

// 表达式查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);

// 字典条件查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> GetByFilterAsync(IReadOnlyDictionary<string, object?> filter);
```

### INSERT

```csharp
// 插入并返回 ID
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
[ReturnInsertedId]
Task<long> InsertAndGetIdAsync(User user);

// 普通插入
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
Task<int> InsertAsync(User user);
```

### UPDATE

```csharp
// 更新（排除 Id 和 CreatedAt）
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// 条件更新
[SqlTemplate("UPDATE {{table}} SET is_active = @isActive WHERE id = @id")]
Task<int> UpdateStatusAsync(long id, bool isActive);
```

### DELETE

```csharp
// 按 ID 删除
[SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
Task<int> DeleteAsync(long id);

// 条件删除
[SqlTemplate("DELETE FROM {{table}} WHERE {{where --param predicate}}")]
Task<int> DeleteWhereAsync(Expression<Func<User, bool>> predicate);
```

### 聚合

```csharp
[SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
Task<long> CountAsync();

[SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE {{where --param predicate}}")]
Task<long> CountWhereAsync(Expression<Func<User, bool>> predicate);

[SqlTemplate("SELECT EXISTS(SELECT 1 FROM {{table}} WHERE {{where --param predicate}})")]
Task<bool> ExistsAsync(Expression<Func<User, bool>> predicate);
```

## ICrudRepository 内置方法

继承 `ICrudRepository<TEntity, TKey>` 自动获得 **42 个标准方法**（24 个查询 + 18 个命令）：

### 查询方法（24 个）

#### 单实体查询（4 个）
| 方法 | 说明 |
|------|------|
| `GetByIdAsync(id)` / `GetById(id)` | 按 ID 查询单个实体 |
| `GetFirstWhereAsync(predicate)` / `GetFirstWhere(predicate)` | 条件查询首条记录 |

#### 列表查询（6 个）
| 方法 | 说明 |
|------|------|
| `GetByIdsAsync(ids)` / `GetByIds(ids)` | 批量 ID 查询 |
| `GetAllAsync(limit)` / `GetAll(limit)` | 查询全部（默认限制 1000 条）|
| `GetWhereAsync(predicate, limit)` / `GetWhere(predicate, limit)` | 条件查询（支持 LINQ 表达式）|

#### 分页查询（4 个）
| 方法 | 说明 |
|------|------|
| `GetPagedAsync(pageSize, offset)` / `GetPaged(pageSize, offset)` | 分页查询 |
| `GetPagedWhereAsync(predicate, pageSize, offset)` / `GetPagedWhere(predicate, pageSize, offset)` | 条件分页查询 |

#### 存在性与计数（10 个）
| 方法 | 说明 |
|------|------|
| `ExistsByIdAsync(id)` / `ExistsById(id)` | 检查 ID 是否存在 |
| `ExistsAsync(predicate)` / `Exists(predicate)` | 条件存在性检查 |
| `CountAsync()` / `Count()` | 计数全部 |
| `CountWhereAsync(predicate)` / `CountWhere(predicate)` | 条件计数 |

### 命令方法（18 个）

#### 插入操作（6 个）
| 方法 | 说明 |
|------|------|
| `InsertAndGetIdAsync(entity)` / `InsertAndGetId(entity)` | 插入并返回自增 ID |
| `InsertAsync(entity)` / `Insert(entity)` | 插入实体 |
| `BatchInsertAsync(entities)` / `BatchInsert(entities)` | 批量插入 |

#### 更新操作（6 个）
| 方法 | 说明 |
|------|------|
| `UpdateAsync(entity)` / `Update(entity)` | 更新实体 |
| `UpdateWhereAsync(entity, predicate)` / `UpdateWhere(entity, predicate)` | 条件更新 |
| `BatchUpdateAsync(entities)` / `BatchUpdate(entities)` | 批量更新 |

#### 删除操作（6 个）
| 方法 | 说明 |
|------|------|
| `DeleteAsync(id)` / `Delete(id)` | 按 ID 删除 |
| `DeleteByIdsAsync(ids)` / `DeleteByIds(ids)` | 批量 ID 删除 |
| `DeleteWhereAsync(predicate)` / `DeleteWhere(predicate)` | 条件删除 |
| `DeleteAllAsync()` / `DeleteAll()` | 删除全部 |

**使用示例：**

```csharp
// 继承 ICrudRepository 即可使用所有方法
public interface IUserRepository : ICrudRepository<User, long>
{
    // 无需定义任何方法，已包含 42 个标准方法
    
    // 仅在需要自定义查询时添加
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @pattern")]
    Task<List<User>> SearchByNameAsync(string pattern);
}

// 使用内置方法
var user = await repo.GetByIdAsync(1);
var users = await repo.GetWhereAsync(u => u.Age >= 18 && u.IsActive);
var count = await repo.CountWhereAsync(u => !u.IsActive);
var exists = await repo.ExistsByIdAsync(123);

// 分页查询
var page1 = await repo.GetPagedAsync(pageSize: 20, offset: 0);
var page2 = await repo.GetPagedWhereAsync(
    predicate: u => u.Age >= 18, 
    pageSize: 20, 
    offset: 20
);

// 插入
var newUser = new User { Name = "Alice", Age = 25 };
long id = await repo.InsertAndGetIdAsync(newUser);

// 批量插入
var users = new List<User> { /* ... */ };
await repo.BatchInsertAsync(users);

// 更新
user.Age = 26;
await repo.UpdateAsync(user);

// 删除
await repo.DeleteAsync(id);
await repo.DeleteWhereAsync(u => u.Age < 18);
```

## 多数据库支持

### 方式1：接口 + 多个实现类（推荐）

```csharp
// 1. 定义通用实体（不指定表名）
using Sqlx.Annotations;

[Sqlx]
public class User
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// 2. 定义通用接口
using Sqlx;
using Sqlx.Annotations;

public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveAsync();
}

// 3. 各数据库实现（在 [RepositoryFor] 中指定方言和表名）
using System.Data.Common;
using Sqlx.Annotations;

[RepositoryFor(typeof(IUserRepository), 
    Dialect = SqlDefineTypes.SQLite,     // 指定方言
    TableName = "users")]                // 指定表名
public partial class SqliteUserRepo(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(IUserRepository), 
    Dialect = SqlDefineTypes.PostgreSql, 
    TableName = "users")]
public partial class PgUserRepo(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(IUserRepository), 
    Dialect = SqlDefineTypes.MySql, 
    TableName = "users")]
public partial class MySqlUserRepo(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(IUserRepository), 
    Dialect = SqlDefineTypes.SqlServer, 
    TableName = "users")]
public partial class SqlServerUserRepo(DbConnection conn) : IUserRepository { }
```

### 方式2：使用单独特性

```csharp
// SQLite 实现
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SqliteUserRepo(DbConnection conn) : IUserRepository { }

// PostgreSQL 实现
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class PgUserRepo(DbConnection conn) : IUserRepository { }
```

### 支持的数据库详情

### 支持的数据库详情

| 数据库 | 枚举值 | 标识符引号 | 参数前缀 | NuGet 包 |
|--------|--------|-----------|---------|---------|
| SQLite | `SqlDefineTypes.SQLite` | `[col]` | `@` | Microsoft.Data.Sqlite |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | `"col"` | `@` | Npgsql |
| MySQL | `SqlDefineTypes.MySql` | `` `col` `` | `@` | MySql.Data |
| SQL Server | `SqlDefineTypes.SqlServer` | `[col]` | `@` | Microsoft.Data.SqlClient |
| Oracle | `SqlDefineTypes.Oracle` | `"col"` | `:` | Oracle.ManagedDataAccess |
| DB2 | `SqlDefineTypes.DB2` | `"col"` | `?` | IBM.Data.DB2 |

## 批量操作

```csharp
using Sqlx;
using MyApp.Models;

// 批量插入
var users = new List<User>
{
    new() { Name = "Alice", Age = 25 },
    new() { Name = "Bob", Age = 30 },
    // ... 1000+ 条记录
};

var sql = "INSERT INTO users (name, age, created_at) VALUES (@name, @age, @createdAt)";

// 使用 DbBatchExecutor，自动分批执行
await connection.ExecuteBatchAsync(
    sql, 
    users, 
    UserParameterBinder.Default,  // 源生成器自动生成
    batchSize: 100                // 每批 100 条
);

// 批量更新
var updateSql = "UPDATE users SET age = @age WHERE id = @id";
await connection.ExecuteBatchAsync(updateSql, users, UserParameterBinder.Default);
```

## 内联表达式（Inline Expressions）

内联表达式是 Sqlx 的强大功能，允许在 SQL 模板中使用表达式、函数和字面量，而不仅仅是参数占位符。

### 基本概念

**什么是内联表达式？**

内联表达式允许你在 `{{set}}` 和 `{{values}}` 占位符中使用 SQL 表达式，例如：
- 算术运算：`Version=Version+1`
- SQL 函数：`CreatedAt=CURRENT_TIMESTAMP`
- 字面量：`Status='pending'`, `Priority=0`
- 计算字段：`Total=@quantity*@unitPrice`

### 使用场景

#### 1. 版本控制（自动递增版本号）

```csharp
// 实体定义
[Sqlx, TableName("documents")]
public class Document
{
    [Key] public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// 仓储接口
public interface IDocumentRepository : ICrudRepository<Document, long>
{
    // INSERT 时初始化版本为 1
    [SqlTemplate(@"
        INSERT INTO {{table}} ({{columns --exclude Id}}) 
        VALUES ({{values --exclude Id --inline Version=1,UpdatedAt=CURRENT_TIMESTAMP}})
    ")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string title, string content);
    
    // UPDATE 时自动递增版本号（乐观锁）
    [SqlTemplate(@"
        UPDATE {{table}} 
        SET {{set --exclude Id,Version,UpdatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
        WHERE id = @id AND version = @version
    ")]
    Task<int> UpdateAsync(long id, string title, string content, int version);
}

// 使用示例
var doc = new Document { Title = "Test", Content = "Content" };
long id = await repo.CreateAsync(doc.Title, doc.Content);
// 生成: INSERT INTO [documents] ([title], [content], [version], [updated_at]) 
//       VALUES (@title, @content, 1, CURRENT_TIMESTAMP)

// 更新时自动递增版本
int affected = await repo.UpdateAsync(id, "New Title", "New Content", version: 1);
// 生成: UPDATE [documents] 
//       SET [title] = @title, [content] = @content, [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP 
//       WHERE id = @id AND version = @version
```

#### 2. 审计跟踪（自动时间戳）

```csharp
[Sqlx, TableName("audit_logs")]
public class AuditLog
{
    [Key] public long Id { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public long EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
}

public interface IAuditLogRepository : ICrudRepository<AuditLog, long>
{
    // 自动记录创建时间
    [SqlTemplate(@"
        INSERT INTO {{table}} ({{columns --exclude Id}}) 
        VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP}})
    ")]
    Task<int> LogAsync(string action, string entityType, long entityId, string createdBy);
}

// 使用
await auditRepo.LogAsync("UPDATE", "User", userId, currentUser);
// 生成: INSERT INTO [audit_logs] ([action], [entity_type], [entity_id], [created_at], [created_by]) 
//       VALUES (@action, @entityType, @entityId, CURRENT_TIMESTAMP, @createdBy)
```

#### 3. 计数器和统计

```csharp
[Sqlx, TableName("posts")]
public class Post
{
    [Key] public long Id { get; set; }
    public string Title { get; set; } = "";
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
}

public interface IPostRepository : ICrudRepository<Post, long>
{
    // 递增浏览次数
    [SqlTemplate(@"
        UPDATE {{table}} 
        SET {{set --exclude Id,Title,ViewCount,LikeCount --inline ViewCount=ViewCount+1}} 
        WHERE id = @id
    ")]
    Task<int> IncrementViewCountAsync(long id);
    
    // 按指定数量递增
    [SqlTemplate(@"
        UPDATE {{table}} 
        SET {{set --exclude Id,Title,ViewCount,LikeCount --inline ViewCount=ViewCount+@increment}} 
        WHERE id = @id
    ")]
    Task<int> IncrementViewCountByAsync(long id, int increment);
}

// 使用
await postRepo.IncrementViewCountAsync(postId);
// 生成: UPDATE [posts] SET [view_count] = [view_count]+1 WHERE id = @id

await postRepo.IncrementViewCountByAsync(postId, 10);
// 生成: UPDATE [posts] SET [view_count] = [view_count]+@increment WHERE id = @id
```

#### 4. 默认值和初始化

```csharp
[Sqlx, TableName("tasks")]
public class Task
{
    [Key] public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface ITaskRepository : ICrudRepository<Task, long>
{
    // 设置默认状态和优先级
    [SqlTemplate(@"
        INSERT INTO {{table}} ({{columns --exclude Id}}) 
        VALUES ({{values --exclude Id --inline Status='pending',Priority=0,IsActive=1,CreatedAt=CURRENT_TIMESTAMP}})
    ")]
    Task<int> CreateAsync(string name);
}

// 使用
await taskRepo.CreateAsync("New Task");
// 生成: INSERT INTO [tasks] ([name], [status], [priority], [is_active], [created_at]) 
//       VALUES (@name, 'pending', 0, 1, CURRENT_TIMESTAMP)
```

#### 5. 计算字段

```csharp
[Sqlx, TableName("order_items")]
public class OrderItem
{
    [Key] public long Id { get; set; }
    public long OrderId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

public interface IOrderItemRepository : ICrudRepository<OrderItem, long>
{
    // INSERT 时自动计算总价
    [SqlTemplate(@"
        INSERT INTO {{table}} ({{columns --exclude Id}}) 
        VALUES ({{values --exclude Id --inline Total=@quantity*@unitPrice}})
    ")]
    Task<int> CreateAsync(long orderId, int quantity, decimal unitPrice);
    
    // UPDATE 时重新计算总价
    [SqlTemplate(@"
        UPDATE {{table}} 
        SET {{set --exclude Id --inline Total=@quantity*@unitPrice}} 
        WHERE id = @id
    ")]
    Task<int> UpdateAsync(long id, long orderId, int quantity, decimal unitPrice);
}

// 使用
await orderItemRepo.CreateAsync(orderId: 1, quantity: 5, unitPrice: 19.99m);
// 生成: INSERT INTO [order_items] ([order_id], [quantity], [unit_price], [total]) 
//       VALUES (@orderId, @quantity, @unitPrice, @quantity*@unitPrice)
```

#### 6. 数据规范化

```csharp
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public interface IUserRepository : ICrudRepository<User, long>
{
    // 邮箱自动转小写并去除空格
    [SqlTemplate(@"
        INSERT INTO {{table}} ({{columns --exclude Id}}) 
        VALUES ({{values --exclude Id --inline Email=LOWER(TRIM(@email))}})
    ")]
    Task<int> CreateAsync(string name, string email);
}

// 使用
await userRepo.CreateAsync("John", "  JOHN@EXAMPLE.COM  ");
// 生成: INSERT INTO [users] ([name], [email]) 
//       VALUES (@name, LOWER(TRIM(@email)))
// 实际存储: john@example.com
```

### 内联表达式规则

1. **使用属性名（PascalCase）**
   ```csharp
   // ✅ 正确：使用属性名
   {{set --inline Version=Version+1}}
   
   // ❌ 错误：使用列名
   {{set --inline version=version+1}}
   ```

2. **属性名自动转换为列名**
   ```csharp
   // 属性名: CreatedAt (PascalCase)
   // 列名: created_at (snake_case)
   {{values --inline CreatedAt=CURRENT_TIMESTAMP}}
   // 生成: CURRENT_TIMESTAMP (不是 @created_at)
   ```

3. **参数占位符会被保留**
   ```csharp
   {{set --inline Counter=Counter+@increment}}
   // 生成: [counter] = [counter]+@increment
   //       ^^^^^^^ 列名    ^^^^^^^^^ 参数保留
   ```

4. **支持的表达式类型**
   - 算术运算：`+`, `-`, `*`, `/`, `%`
   - SQL 函数：`CURRENT_TIMESTAMP`, `UPPER()`, `LOWER()`, `TRIM()`, `COALESCE()`, `SUBSTRING()`, `CASE WHEN` 等
   - 字面量：字符串 `'value'`、数字 `123`、布尔 `1`/`0`、`NULL`
   - 复杂表达式：`(Price-Discount)*(1+TaxRate)`
   - 嵌套函数：`LOWER(TRIM(Email))`, `COALESCE(NULLIF(Value,''),Default)`

5. **多个表达式用逗号分隔**
   ```csharp
   {{set --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}}
   {{values --inline Status='pending',Priority=0,CreatedAt=CURRENT_TIMESTAMP}}
   ```

6. **函数内的逗号会被正确处理**
   ```csharp
   // ✅ 正确：函数内的逗号不会被误认为分隔符
   {{set --inline Status=COALESCE(Status,'pending')}}
   {{set --inline Value=SUBSTRING(Value,1,10)}}
   {{set --inline Result=COALESCE(NULLIF(Result,''),SUBSTRING('default',1,7))}}
   
   // 生成的 SQL 保留完整的函数调用
   // [status] = COALESCE([status],'pending')
   // [value] = SUBSTRING([value],1,10)
   // [result] = COALESCE(NULLIF([result],''),SUBSTRING('default',1,7))
   ```

7. **支持深度嵌套的括号和引号**
   ```csharp
   // 嵌套括号
   {{set --inline Result=((([result]+@a)*@b)/@c)+@d}}
   // 生成: [result] = ((([result]+@a)*@b)/@c)+@d
   
   // 字符串中的引号
   {{set --inline Message='It''s working'}}
   // 生成: [message] = 'It''s working'
   
   // JSON 字符串
   {{set --inline JsonData='{\"key\":\"value\"}'}}
   // 生成: [json_data] = '{"key":"value"}'
   ```

8. **跨方言自动适配**
   ```csharp
   // SQLite
   {{set --inline Version=Version+1}}
   // 生成: [version] = [version]+1
   
   // PostgreSQL
   {{set --inline Version=Version+1}}
   // 生成: "version" = "version"+1
   
   // MySQL
   {{set --inline Version=Version+1}}
   // 生成: `version` = `version`+1
   ```

### 内联表达式的实现细节

**智能逗号处理：**

Sqlx 使用智能解析器处理内联表达式，能够正确识别：
- 顶层逗号（用于分隔多个表达式）
- 函数内的逗号（保留为函数参数）
- 括号嵌套（跟踪深度）
- 字符串内的逗号（单引号和双引号）

```csharp
// 示例：多个复杂表达式
{{set --inline 
    Email=LOWER(TRIM(Email)),
    Status=COALESCE(Status,'active'),
    Priority=CASE WHEN Priority>10 THEN 10 ELSE Priority END,
    UpdatedAt=CURRENT_TIMESTAMP
}}

// 解析结果（4个独立表达式）：
// 1. Email=LOWER(TRIM(Email))
// 2. Status=COALESCE(Status,'active')
// 3. Priority=CASE WHEN Priority>10 THEN 10 ELSE Priority END
// 4. UpdatedAt=CURRENT_TIMESTAMP

// 生成的 SQL：
// [email] = LOWER(TRIM([email])), 
// [status] = COALESCE([status],'active'), 
// [priority] = CASE WHEN [priority]>10 THEN 10 ELSE [priority] END, 
// [updated_at] = CURRENT_TIMESTAMP
```

**性能说明：**
- 表达式解析在编译时完成（`SqlTemplate.Prepare()`）
- 运行时无额外开销
- 与标准占位符性能完全相同

### 限制和注意事项

1. **完全向后兼容**
   - 不使用 `--inline` 时行为不变
   - 所有现有代码无需修改

2. **表达式在编译时处理**
   - 表达式解析在 `SqlTemplate.Prepare()` 时完成
   - 运行时无额外开销
   - 与标准占位符性能完全相同

3. **测试覆盖**
   - ✅ 1842 个单元测试全部通过
   - ✅ 包含 56 个专门的内联表达式测试
   - ✅ 覆盖所有边界情况和复杂场景
   - ✅ 验证所有 6 种数据库方言

## 常见错误和正确做法

| ❌ 错误写法 | ✅ 正确写法 | 说明 |
|-----------|-----------|------|
| `INSERT INTO users ({{columns}}) VALUES ({{values}})` | `INSERT INTO users ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})` | INSERT 时需排除自增 ID |
| `UPDATE users SET {{set}}` | `UPDATE users SET {{set --exclude Id CreatedAt}}` | UPDATE 时排除不可变字段 |
| `WHERE is_active = 1` | `WHERE is_active = {{bool_true}}` | 布尔值跨数据库兼容 |
| `SELECT * FROM users` | `SELECT {{columns}} FROM {{table}}` | 使用占位符确保类型安全 |
| `SELECT ... ORDER BY {{orderby name}}` | `SELECT ... ORDER BY name ASC` | 不存在 {{orderby}} 占位符，直接写 SQL |
| `List<User> GetAll()` | `Task<List<User>> GetAllAsync()` | 必须使用异步方法 |
| `public class UserRepo : IUserRepository` | `public partial class UserRepo : IUserRepository` | 必须声明为 partial |
| `[SqlTemplate] void Update(User u);` | `[SqlTemplate] Task<int> UpdateAsync(User u);` | 方法必须返回 Task |
| 忘记添加 `[Sqlx]` | `[Sqlx] public class User { }` | 实体类必须标记 [Sqlx] |
| `[TableName("users")] [RepositoryFor(...)]` | `[RepositoryFor(..., TableName = "users")]` | 表名可在任一处指定 |
| 缺少 `using Sqlx.Annotations;` | `using Sqlx.Annotations;` | 必需的命名空间 |
| `GetSingleWhereAsync(predicate)` | `GetFirstWhereAsync(predicate)` | 已移除 Single 方法，使用 First 代替 |
| `GetByIds(ids)` 在循环中调用 | 使用 `GetByIdsAsync(ids)` 一次性获取 | 批量操作避免 N+1 查询 |
| `{{set --inline version=version+1}}` | `{{set --inline Version=Version+1}}` | 内联表达式必须使用属性名（PascalCase） |
| `{{values --inline created_at=NOW()}}` | `{{values --inline CreatedAt=NOW()}}` | 内联表达式必须使用属性名（PascalCase） |

## 生成代码位置和调试

### 查看生成的代码

源生成器生成的代码位于项目的 `obj` 目录：

```
obj/
  Debug/
    net9.0/
      generated/
        Sqlx.Generator/
          Sqlx.Generator.RepositoryGenerator/
            UserRepository.Repository.g.cs      # 仓储实现
          Sqlx.Generator.SqlxGenerator/
            User.EntityProvider.g.cs            # EntityProvider
            User.ResultReader.g.cs              # ResultReader
            User.ParameterBinder.g.cs           # ParameterBinder
```

### 调试方法1：返回 SqlTemplate

**重要：** 这里的 `SqlTemplate` 是 `Sqlx` 命名空间下的**类**（不是 `Sqlx.Annotations` 下的特性）。

```csharp
using Sqlx;                    // SqlTemplate 类
using Sqlx.Annotations;        // [SqlTemplate] 特性

// 在接口中定义调试方法
public interface IUserRepository
{
    // 普通方法：执行 SQL 返回结果
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    // 调试方法：返回 SqlTemplate 类查看生成的 SQL（不执行）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    SqlTemplate GetByIdSql(long id);  // 返回类型是 Sqlx.SqlTemplate 类
    
    // 带动态参数的调试
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
    SqlTemplate GetWhereSql(Expression<Func<User, bool>> predicate);
}

// 使用
var sqlTemplate = userRepo.GetByIdSql(123);
Console.WriteLine($"Prepared SQL: {sqlTemplate.Sql}");
Console.WriteLine($"Has dynamic placeholders: {sqlTemplate.HasDynamicPlaceholders}");

var whereSql = userRepo.GetWhereSql(u => u.Age > 18 && u.IsActive);
Console.WriteLine($"WHERE SQL: {whereSql.Render("predicate", "age > 18 AND is_active = 1")}");
```

**说明：**
- `[SqlTemplate]` 是**特性**（Attribute），用于标注方法
- `SqlTemplate` 是**类**（Class），用作返回类型查看 SQL
- 两者命名相同但用途完全不同，注意区分

### 调试方法2：使用拦截器

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository
{
#if !SQLX_DISABLE_INTERCEPTOR
    partial void OnExecuting(string operationName, DbCommand command, SqlTemplate template)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Executing: {operationName}");
        Console.WriteLine($"  Prepared SQL: {template.Sql}");
        Console.WriteLine($"  Final SQL: {command.CommandText}");
        
        foreach (DbParameter param in command.Parameters)
        {
            Console.WriteLine($"  @{param.ParameterName} = {param.Value}");
        }
    }
    
    partial void OnExecuted(string operationName, DbCommand command, SqlTemplate template, object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Executed: {operationName} in {ms:F2}ms");
        Console.WriteLine($"  Result: {result}");
    }
    
    partial void OnExecuteFail(string operationName, DbCommand command, SqlTemplate template, Exception exception, long elapsedTicks)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Failed: {operationName}");
        Console.WriteLine($"  Error: {exception.Message}");
        Console.WriteLine($"  SQL: {command.CommandText}");
    }
#endif
}
```

### 调试方法3：启用 Activity 跟踪

```csharp
using System.Diagnostics;

// 创建 ActivitySource
var activitySource = new ActivitySource("MyApp.Database");

using var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStopped = activity =>
    {
        Console.WriteLine($"Activity: {activity.DisplayName}");
        foreach (var tag in activity.Tags)
        {
            Console.WriteLine($"  {tag.Key} = {tag.Value}");
        }
    }
};

ActivitySource.AddActivityListener(listener);

// 执行查询（会自动记录到 Activity）
using (var activity = activitySource.StartActivity("GetUsers"))
{
    var users = await userRepo.GetAllAsync();
    // Activity 标签包含：db.system, db.operation, db.statement, db.duration_ms 等
}
```

## 支持的数据库

## IQueryable 查询构建器

使用标准 LINQ 语法构建 SQL：

```csharp
using Sqlx;

// 基本查询
var sql = SqlQuery.ForSqlite<User>()
    .Where(u => u.Age >= 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToSql();

// 参数化查询
var (sql, parameters) = SqlQuery.ForSqlServer<User>()
    .Where(u => u.Age > 18)
    .ToSqlWithParameters();
```

**入口方法：**
- `SqlQuery.ForSqlite<T>()`
- `SqlQuery.ForSqlServer<T>()`
- `SqlQuery.ForMySql<T>()`
- `SqlQuery.ForPostgreSQL<T>()`
- `SqlQuery.ForOracle<T>()`
- `SqlQuery.ForDB2<T>()`
- `SqlQuery.For<T>(SqlDialect dialect)`

**支持的 LINQ 方法：**
- `Where` - 条件过滤
- `Select` - 投影
- `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending` - 排序
- `Take` / `Skip` - 分页
- `GroupBy` - 分组
- `Distinct` - 去重

**支持的函数：**
- String: `Contains`, `StartsWith`, `EndsWith`, `ToUpper`, `ToLower`, `Trim`, `Substring`, `Replace`, `Length`
- Math: `Abs`, `Round`, `Floor`, `Ceiling`, `Sqrt`, `Pow`, `Min`, `Max`

## 测试覆盖

项目包含 **1575 个单元测试**，覆盖所有核心功能：

- ✅ 基础 CRUD 操作
- ✅ 表达式查询和转换
- ✅ SqlTemplate 占位符处理
- ✅ 多数据库方言支持
- ✅ 批量操作
- ✅ IQueryable 查询构建器
- ✅ 源生成器功能
- ✅ AOT 兼容性
- ✅ 主构造函数支持

## 示例项目

### TodoWebApi - 完整的 Web 应用示例

位置：`samples/TodoWebApi/`

**功能特性：**
- ✅ **39 个 API 端点** - 展示完整的 CRUD 和高级查询
- ✅ **现代化 UI** - 玻璃态设计 + 流畅动画
- ✅ **三种查询方式** - SqlTemplate、LINQ 表达式、IQueryable
- ✅ **批量操作** - 批量更新、批量删除、批量完成
- ✅ **完全 AOT 兼容** - Native AOT 编译支持
- ✅ **100% 测试覆盖** - 39 个自动化测试用例

**API 端点分类：**

1. **基础 CRUD** (10 个)
   - 创建、读取、更新、删除任务
   - 标记完成、更新工作时间

2. **查询与过滤** (10 个)
   - 搜索、按优先级过滤、逾期任务
   - 分页查询、存在性检查、批量获取

3. **统计与聚合** (4 个)
   - 总数、待办数、完成数、完成率
   - LINQ 表达式统计

4. **批量操作** (4 个)
   - 批量更新优先级、批量完成
   - 批量删除、删除已完成

5. **LINQ 示例** (4 个)
   - LINQ 表达式查询
   - IQueryable 分页和投影
   - 高级搜索

6. **错误处理** (5 个)
   - 404 响应验证
   - 不存在资源的操作

**运行示例：**

```bash
# 启动应用（使用 CreateSlimBuilder，仅支持 HTTP）
cd samples/TodoWebApi
dotnet run

# 访问 Web 界面
http://localhost:5000

# 运行 API 测试
pwsh test-api.ps1
```

**重要说明：**
- 本示例使用 `WebApplication.CreateSlimBuilder()` 以支持 Native AOT 编译
- CreateSlimBuilder 默认不加载完整配置系统，**不支持 HTTPS 端点配置**
- `appsettings.json` 中只配置了 HTTP 端点（`http://localhost:5000`）
- 如需 HTTPS 支持，请改用 `WebApplication.CreateBuilder(args)`（失去 AOT 支持）

**代码示例：**

```csharp
// 1. 定义实体
[Sqlx, TableName("todos")]
public class Todo
{
    [Key] public long Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
}

// 2. 定义仓储接口（继承 ICrudRepository 获得 42 个标准方法）
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    // SqlTemplate 方式
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query")]
    Task<List<Todo>> SearchAsync(string query);
    
    // LINQ 表达式方式
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
    Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> predicate);
    
    // 批量操作 - 使用 json_each() 处理 ID 列表（SQLite）
    // 注意：需要手动构建 JSON 数组字符串，如 "[1,2,3]"
    [SqlTemplate("UPDATE {{table}} SET priority = @priority WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> BatchUpdatePriorityAsync(string idsJson, int priority, DateTime updatedAt);
}

// 3. 实现仓储（自动生成）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository(SqliteConnection connection) : ITodoRepository { }

// 4. 在 API 中使用
app.MapGet("/api/todos/search", async (string q, ITodoRepository repo) =>
    Results.Json(await repo.SearchAsync($"%{q}%")));

app.MapGet("/api/todos/high-priority", async (ITodoRepository repo) =>
    Results.Json(await repo.GetWhereAsync(t => t.Priority >= 3 && !t.IsCompleted)));

app.MapPut("/api/todos/batch/priority", async (BatchRequest req, ITodoRepository repo) =>
{
    var idsJson = $"[{string.Join(",", req.Ids)}]";
    var result = await repo.BatchUpdatePriorityAsync(idsJson, req.Priority, DateTime.UtcNow);
    return Results.Json(new { updatedCount = result });
});
```

**UI 特性：**
- 🎨 玻璃态设计（Glassmorphism）
- ✨ 流畅的动画效果
- 📱 完全响应式设计
- 🎯 三种过滤模式（全部/活动/已完成）
- 📊 实时统计（总数、活动、完成、完成率）
- 🏷️ 优先级标签（高/中/低）
- ⏰ 相对时间显示
- ✅ 自定义复选框

详见：[TodoWebApi README](samples/TodoWebApi/README.md)


## 占位符生成质量保证

### 验证测试覆盖

Sqlx 包含全面的占位符生成验证测试，确保生成的 SQL 没有语法错误和逻辑问题。

**测试统计：**
- ✅ **1842 个单元测试** - 100% 通过率
- ✅ **56 个内联表达式专项测试** - 覆盖所有边界情况
- ✅ **18 个占位符生成验证测试** - 验证语法正确性
- ✅ **15 个边界情况测试** - 验证复杂场景

### 验证的关键点

#### 1. 语法正确性验证

**验证项目：**
- ✅ 没有连续的逗号 (`,,`)
- ✅ SET 子句末尾没有多余逗号 (`, WHERE`)
- ✅ 括号匹配正确
- ✅ 列数和值数匹配（INSERT 语句）
- ✅ 没有空格格式错误

**测试示例：**
```csharp
[TestMethod]
public void Set_BasicGeneration_NoSyntaxErrors()
{
    var template = SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", 
        context);
    
    var sql = template.Sql;
    
    // 验证基本结构
    Assert.IsTrue(sql.StartsWith("UPDATE [users] SET"));
    Assert.IsTrue(sql.Contains("WHERE id = @id"));
    
    // 验证没有语法错误
    Assert.IsFalse(sql.Contains(",,"));
    Assert.IsFalse(sql.Contains(", WHERE"));
    Assert.IsFalse(sql.Contains("SET ,"));
}
```

#### 2. 内联表达式处理验证

**验证项目：**
- ✅ 函数内的逗号被正确保留（如 `COALESCE(Status,'pending')`）
- ✅ 嵌套函数正确处理（如 `LOWER(TRIM(Email))`）
- ✅ 多个表达式正确分隔
- ✅ CASE 表达式完整保留
- ✅ 括号嵌套正确处理
- ✅ 字符串引号正确处理

**测试示例：**
```csharp
[TestMethod]
public void InlineExpression_MultipleCommasInFunction_ParsesCorrectly()
{
    var template = SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id --inline Value=SUBSTRING(Value,1,10)}} WHERE id = @id",
        context);
    
    var sql = template.Sql;
    
    // 验证 SUBSTRING 函数完整，包含所有逗号
    Assert.IsTrue(sql.Contains("SUBSTRING([value],1,10)"));
    Assert.IsFalse(sql.Contains(",,"));
}

[TestMethod]
public void InlineExpression_NestedFunctionsWithCommas_ParsesCorrectly()
{
    var template = SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id --inline Result=COALESCE(NULLIF(Result,''),SUBSTRING('default',1,7))}} WHERE id = @id",
        context);
    
    var sql = template.Sql;
    
    // 验证嵌套函数完整
    Assert.IsTrue(sql.Contains("COALESCE(NULLIF([result],''),SUBSTRING('default',1,7))"));
}
```

#### 3. 复杂场景验证

**验证的复杂场景：**
- ✅ 多参数函数：`SUBSTRING(Value,1,10)`
- ✅ 嵌套函数：`COALESCE(NULLIF(Result,''),SUBSTRING('default',1,7))`
- ✅ 深度嵌套括号：`((([result]+@a)*@b)/@c)+@d`
- ✅ 多条件 CASE：`CASE WHEN ... THEN ... WHEN ... THEN ... ELSE ... END`
- ✅ 混合表达式：`Email=LOWER(TRIM(Email)),Status=COALESCE(Status,'active'),UpdatedAt=CURRENT_TIMESTAMP`

**测试示例：**
```csharp
[TestMethod]
public void RealIssue_MultipleInlineExpressionsWithFunctions_WorksCorrectly()
{
    var template = SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id --inline Email=LOWER(TRIM(Email)),Status=COALESCE(Status,'active'),UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
        context);
    
    var sql = template.Sql;
    
    // 验证所有表达式都正确
    Assert.IsTrue(sql.Contains("LOWER(TRIM([email]))"));
    Assert.IsTrue(sql.Contains("COALESCE([status],'active')"));
    Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
}
```

#### 4. 方言一致性验证

**验证所有 6 种数据库方言：**
- ✅ SQLite: `[column]` + `@param`
- ✅ PostgreSQL: `"column"` + `$param`
- ✅ MySQL: `` `column` `` + `@param`
- ✅ SQL Server: `[column]` + `@param`
- ✅ Oracle: `"column"` + `:param`
- ✅ DB2: `"column"` + `?`

**测试示例：**
```csharp
[TestMethod]
public void InlineExpression_AllDialects_ConsistentBehavior()
{
    var dialects = new[] { 
        SqlDefine.SQLite, SqlDefine.PostgreSql, SqlDefine.MySql, 
        SqlDefine.SqlServer, SqlDefine.Oracle, SqlDefine.DB2 
    };
    
    foreach (var dialect in dialects)
    {
        var context = new PlaceholderContext(dialect, "test", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Value=COALESCE(Value,'default')}} WHERE id = @id",
            context);
        
        var sql = template.Sql;
        
        // 验证基本结构
        Assert.IsTrue(sql.Contains("UPDATE"));
        Assert.IsTrue(sql.Contains("SET"));
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("COALESCE"));
        
        // 验证没有语法错误
        Assert.IsFalse(sql.Contains(",,"));
        Assert.IsFalse(sql.Contains(", WHERE"));
    }
}
```

#### 5. 实际使用场景验证

**验证的实际场景：**
- ✅ 用户注册（INSERT with defaults）
- ✅ 订单更新（UPDATE with version check）
- ✅ 库存扣减（UPDATE with calculations）
- ✅ 优化锁定（version increment）
- ✅ 审计跟踪（auto timestamps）

**测试示例：**
```csharp
[TestMethod]
public void RealWorld_UserRegistration_GeneratesValidSQL()
{
    var template = SqlTemplate.Prepare(
        "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status='active',CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})",
        context);
    
    var sql = template.Sql;
    
    // 验证完整性
    Assert.IsTrue(sql.Contains("INSERT INTO"));
    Assert.IsTrue(sql.Contains("VALUES"));
    Assert.IsTrue(sql.Contains("@username"));
    Assert.IsTrue(sql.Contains("@email"));
    Assert.IsTrue(sql.Contains("'active'"));
    Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
    
    // 验证没有语法错误
    Assert.IsFalse(sql.Contains(",,"));
    Assert.IsFalse(sql.Contains("(,"));
    Assert.IsFalse(sql.Contains(",)"));
}
```

### 已修复的Bug

#### Bug #1: 函数内逗号被错误分割

**问题描述：**
`ParseInlineExpressions()` 方法简单地按逗号分割，导致函数内的逗号被错误处理。

**示例：**
```csharp
// 错误的解析
{{set --inline Status=COALESCE(Status,'pending')}}
// 被错误分割为两个表达式：
// 1. Status=COALESCE(Status
// 2. 'pending')
```

**修复方案：**
实现了 `SplitRespectingParentheses()` 方法，智能处理：
- 括号嵌套（跟踪深度）
- 单引号字符串
- 双引号字符串
- 只在顶层逗号处分割

**修复后：**
```csharp
// 正确的解析
{{set --inline Status=COALESCE(Status,'pending')}}
// 识别为单个表达式：
// Status=COALESCE(Status,'pending')

// 生成正确的 SQL：
// [status] = COALESCE([status],'pending')
```

**测试验证：**
```csharp
[TestMethod]
public void RealIssue_CoalesceWithComma_WorksCorrectly()
{
    var template = SqlTemplate.Prepare(
        "UPDATE {{table}} SET {{set --exclude Id --inline Status=COALESCE(Status,'pending')}} WHERE id = @id",
        context);
    
    var sql = template.Sql;
    
    // 验证 COALESCE 函数完整，包含逗号
    Assert.AreEqual(
        "UPDATE [orders] SET [status] = COALESCE([status],'pending') WHERE id = @id", 
        sql
    );
}
```

### 质量保证流程

1. **编译时验证**
   - 源生成器在编译时生成代码
   - 编译器验证生成的代码语法正确

2. **单元测试验证**
   - 1842 个单元测试覆盖所有功能
   - 每次提交自动运行测试
   - 100% 通过率要求

3. **边界情况测试**
   - 专门的边界情况测试套件
   - 覆盖极端场景和复杂组合
   - 验证错误处理

4. **实际场景测试**
   - 基于真实使用场景的测试
   - 验证常见模式和最佳实践
   - 确保实用性

5. **多方言测试**
   - 所有测试在 6 种数据库方言上运行
   - 确保跨数据库一致性
   - 验证方言特定功能

### 测试文件位置

```
tests/Sqlx.Tests/
├── SetPlaceholderStrictTests.cs              # 35 个严格测试
├── SetPlaceholderUpdateScenarioTests.cs      # 21 个场景测试
├── PlaceholderGenerationValidationTests.cs   # 18 个验证测试
├── PlaceholderEdgeCaseTests.cs               # 15 个边界测试
├── ParseInlineExpressionsTests.cs            # 内联表达式解析测试
├── SetPlaceholderInlineTests.cs              # SET 内联表达式测试
├── SetPlaceholderInlineEdgeCaseTests.cs      # SET 边界情况测试
├── SetPlaceholderInlineDialectTests.cs       # SET 方言测试
├── SetPlaceholderInlineIntegrationTests.cs   # SET 集成测试
├── ValuesPlaceholderInlineTests.cs           # VALUES 内联表达式测试
├── ValuesPlaceholderInlineEdgeCaseTests.cs   # VALUES 边界情况测试
└── ValuesPlaceholderInlineIntegrationTests.cs # VALUES 集成测试
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~PlaceholderGenerationValidationTests"

# 运行内联表达式相关测试
dotnet test --filter "FullyQualifiedName~Inline"

# 查看详细输出
dotnet test --logger "console;verbosity=detailed"
```

### 测试结果示例

```
测试运行成功。
测试总数: 1842
     通过数: 1842
     失败数: 0
     跳过数: 0
总时间: 7.5 秒

测试摘要: 总计: 1842, 失败: 0, 成功: 1842, 已跳过: 0
```

### 持续改进

Sqlx 团队持续改进占位符生成质量：
- ✅ 定期审查测试覆盖率
- ✅ 添加新的边界情况测试
- ✅ 修复发现的任何问题
- ✅ 优化性能和代码质量
- ✅ 更新文档和示例

**如果发现问题，请提交 Issue 并附上：**
1. 使用的 SQL 模板
2. 实体类定义
3. 期望的 SQL 输出
4. 实际的 SQL 输出
5. 数据库方言

我们会尽快修复并添加相应的测试用例。
