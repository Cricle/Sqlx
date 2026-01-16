# Sqlx AI 助手指南

> 面向 AI 助手的 Sqlx 使用指南，帮助快速理解和生成正确代码。

## 概述

Sqlx 是编译时源生成器，生成高性能数据访问代码。核心流程：

```
接口定义 + [SqlTemplate] → 源生成器 → partial class 实现
```

## 核心特性

| 特性 | 用途 |
|------|------|
| `[SqlDefine(SqlDefineTypes.XXX)]` | 定义数据库类型 |
| `[SqlTemplate("SQL")]` | 定义 SQL 模板 |
| `[RepositoryFor(typeof(IXxx))]` | 标记仓储实现类 |
| `[TableName("xxx")]` | 指定表名 |
| `[Sqlx]` | 生成 EntityProvider/ResultReader/ParameterBinder |
| `[ReturnInsertedId]` | 返回插入 ID |
| `[Key]` | 标记主键 |

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

| 占位符 | 输出示例 |
|--------|---------|
| `{{table}}` | `[users]` / `"users"` / `` `users` `` |
| `{{columns}}` | `[id], [name], [age]` |
| `{{columns --exclude Id}}` | `[name], [age]` |
| `{{values}}` | `@id, @name, @age` |
| `{{values --exclude Id}}` | `@name, @age` |
| `{{set}}` | `[name] = @name, [age] = @age` |
| `{{set --exclude Id}}` | `[name] = @name, [age] = @age` |

### 分页与排序

| 占位符 | 输出示例 |
|--------|---------|
| `{{limit --param count}}` | `LIMIT @count` |
| `{{offset --param skip}}` | `OFFSET @skip` |
| `{{orderby col}}` | `ORDER BY col ASC` |
| `{{orderby col --desc}}` | `ORDER BY col DESC` |

### 方言占位符

| 占位符 | SQLite | PostgreSQL | MySQL | SqlServer |
|--------|--------|------------|-------|-----------|
| `{{bool_true}}` | `1` | `true` | `1` | `1` |
| `{{bool_false}}` | `0` | `false` | `0` | `0` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `NOW()` | `GETDATE()` |

### 条件占位符

```sql
-- 参数非空时包含
{{if notnull=name}}AND name = @name{{/if}}

-- 参数为空时包含
{{if null=status}}AND status IS NULL{{/if}}

-- 集合非空时包含
{{if notempty=ids}}AND id IN @ids{{/if}}
```

### WHERE 占位符

```sql
-- 表达式模式：使用 Expression<Func<T, bool>>
{{where --param predicate}}

-- 字典模式：使用 IReadOnlyDictionary<string, object?>
{{where --object filter}}
```

**`--object` 模式说明：**
- 字典中非 null 值生成 `column = @column` 条件
- 多个条件用 AND 连接并加括号
- 空字典或全 null 值返回 `1=1`
- 支持按 PropertyName 或 ColumnName 匹配（不区分大小写）

```csharp
// 示例
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> SearchAsync(IReadOnlyDictionary<string, object?> filter);

// 调用
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",    // 生成: [name] = @name
    ["Age"] = 25,         // 生成: [age] = @age
    ["Email"] = null      // 忽略 null 值
};
var users = await repo.SearchAsync(filter);
// 生成 SQL: SELECT ... WHERE ([name] = @name AND [age] = @age)
```

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

继承 `ICrudRepository<TEntity, TKey>` 自动获得：

| 方法 | 说明 |
|------|------|
| `GetByIdAsync(id)` | 按 ID 查询 |
| `GetByIdsAsync(ids)` | 批量 ID 查询 |
| `GetAllAsync(limit)` | 查询全部 |
| `GetWhereAsync(predicate, limit)` | 条件查询 |
| `GetFirstWhereAsync(predicate)` | 条件查询首条 |
| `GetPagedAsync(pageSize, offset)` | 分页查询 |
| `ExistsAsync(predicate)` | 存在性检查 |
| `CountAsync()` | 计数 |
| `CountWhereAsync(predicate)` | 条件计数 |
| `InsertAndGetIdAsync(entity)` | 插入返回 ID |
| `UpdateAsync(entity)` | 更新 |
| `UpdateWhereAsync(predicate, setter)` | 条件更新 |
| `DeleteAsync(id)` | 删除 |
| `DeleteWhereAsync(predicate)` | 条件删除 |

## 多数据库支持

```csharp
// 统一接口
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveAsync();
}

// 各数据库实现
[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.SQLite, TableName = "users")]
public partial class SqliteUserRepo(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "users")]
public partial class PgUserRepo(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.MySql, TableName = "users")]
public partial class MySqlUserRepo(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.SqlServer, TableName = "users")]
public partial class SqlServerUserRepo(DbConnection conn) : IUserRepository { }
```

## 批量操作

```csharp
// 使用 DbBatchExecutor
var users = new List<User> { ... };
var sql = "INSERT INTO users (name, age) VALUES (@name, @age)";
await connection.ExecuteBatchAsync(sql, users, UserParameterBinder.Default, batchSize: 100);
```

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| `INSERT ... ({{columns}})` | `INSERT ... ({{columns --exclude Id}})` |
| `UPDATE ... SET {{set}}` | `UPDATE ... SET {{set --exclude Id}}` |
| `WHERE is_active = 1` | `WHERE is_active = {{bool_true}}` |
| `SELECT * FROM users` | `SELECT {{columns}} FROM {{table}}` |
| 同步方法 `List<User> GetAll()` | 异步方法 `Task<List<User>> GetAllAsync()` |

## 支持的数据库

| 数据库 | 枚举值 | 标识符引号 | 参数前缀 |
|--------|--------|-----------|---------|
| SQLite | `SqlDefineTypes.SQLite` | `[col]` | `@` |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | `"col"` | `@` |
| MySQL | `SqlDefineTypes.MySql` | `` `col` `` | `@` |
| SQL Server | `SqlDefineTypes.SqlServer` | `[col]` | `@` |
| Oracle | `SqlDefineTypes.Oracle` | `"col"` | `:` |
| DB2 | `SqlDefineTypes.DB2` | `"col"` | `?` |

## 调试

生成代码位置：`obj/Debug/net9.0/generated/Sqlx.Generator/`

返回 `SqlTemplate` 类型可查看生成的 SQL：

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
SqlTemplate GetByIdSql(long id);  // 返回模板而非执行

var template = repo.GetByIdSql(123);
Console.WriteLine(template.Sql);  // 查看 SQL
```

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

项目包含 1344 个单元测试，覆盖所有核心功能。
