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
        {{orderby name}}
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
- `[RepositoryFor(typeof(IXxx))]` - 标注在实现类上，指定要实现的接口
- 类必须声明为 `partial class`
- 构造函数接收 `DbConnection` 参数
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
| `{{columns --include Name Age}}` | `[name], [age]` | 只包含指定列 |
| `{{values}}` | `@id, @name, @age` | 所有参数占位符（用于 INSERT） |
| `{{values --exclude Id}}` | `@name, @age` | 排除指定参数 |
| `{{set}}` | `[name] = @name, [age] = @age` | SET 子句（用于 UPDATE） |
| `{{set --exclude Id CreatedAt}}` | `[name] = @name, [age] = @age` | 排除不可更新的字段 |
| `{{arg --param name}}` | `@name` / `:name` / `$1` | 单个参数占位符（方言适配） |

### 分页与排序

| 占位符 | 输出示例 | 说明 |
|--------|---------|------|
| `{{limit --param count}}` | `LIMIT @count` (SQLite/MySQL) <br> `TOP @count` (SQL Server) | 限制返回行数 |
| `{{offset --param skip}}` | `OFFSET @skip` | 跳过行数 |
| `{{orderby col}}` | `ORDER BY [col] ASC` | 升序排序 |
| `{{orderby col --desc}}` | `ORDER BY [col] DESC` | 降序排序 |
| `{{orderby col1, col2 --desc}}` | `ORDER BY [col1] DESC, [col2] DESC` | 多列排序 |

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
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby id}} {{limit --param size}} {{offset --param skip}}")]
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

## 常见错误和正确做法

| ❌ 错误写法 | ✅ 正确写法 | 说明 |
|-----------|-----------|------|
| `INSERT INTO users ({{columns}}) VALUES ({{values}})` | `INSERT INTO users ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})` | INSERT 时需排除自增 ID |
| `UPDATE users SET {{set}}` | `UPDATE users SET {{set --exclude Id CreatedAt}}` | UPDATE 时排除不可变字段 |
| `WHERE is_active = 1` | `WHERE is_active = {{bool_true}}` | 布尔值跨数据库兼容 |
| `SELECT * FROM users` | `SELECT {{columns}} FROM {{table}}` | 使用占位符确保类型安全 |
| `List<User> GetAll()` | `Task<List<User>> GetAllAsync()` | 必须使用异步方法 |
| `public class UserRepo : IUserRepository` | `public partial class UserRepo : IUserRepository` | 必须声明为 partial |
| `[SqlTemplate] void Update(User u);` | `[SqlTemplate] Task<int> UpdateAsync(User u);` | 方法必须返回 Task |
| 忘记添加 `[Sqlx]` | `[Sqlx] public class User { }` | 实体类必须标记 [Sqlx] |
| `[TableName("users")] [RepositoryFor(...)]` | `[RepositoryFor(..., TableName = "users")]` | 表名可在任一处指定 |
| 缺少 `using Sqlx.Annotations;` | `using Sqlx.Annotations;` | 必需的命名空间 |
| `GetSingleWhereAsync(predicate)` | `GetFirstWhereAsync(predicate)` | 已移除 Single 方法，使用 First 代替 |
| `GetByIds(ids)` 在循环中调用 | 使用 `GetByIdsAsync(ids)` 一次性获取 | 批量操作避免 N+1 查询 |

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

```csharp
// 在接口中定义调试方法
public interface IUserRepository
{
    // 普通方法：执行 SQL 返回结果
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    // 调试方法：返回 SqlTemplate 查看生成的 SQL（不执行）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    SqlTemplate GetByIdSql(long id);
    
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

项目包含 **1572 个单元测试**，覆盖所有核心功能：

- ✅ 基础 CRUD 操作
- ✅ 表达式查询和转换
- ✅ SqlTemplate 占位符处理
- ✅ 多数据库方言支持
- ✅ 批量操作
- ✅ IQueryable 查询构建器
- ✅ 源生成器功能
- ✅ AOT 兼容性

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
# 启动应用
cd samples/TodoWebApi
dotnet run

# 访问 Web 界面
http://localhost:5000

# 运行 API 测试
pwsh test-api.ps1
```

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
    
    // 批量操作
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
