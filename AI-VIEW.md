# Sqlx AI 参考手册

高性能 .NET 数据库访问库。源生成器编译时生成代码，热路径零运行时反射，完全 AOT 兼容。

支持：SQLite · PostgreSQL · MySQL · SQL Server（完整）；Oracle · DB2（兼容入口，不承诺持续验证）

```bash
dotnet add package Sqlx
```

---

## 实体定义

```csharp
[Sqlx, TableName("users")]          // 两个都必须
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; } = "";
    [Column("email_address")] public string Email { get; set; } = ""; // 自定义列名
    [IgnoreDataMember] public string Tag => $"u-{Id}";  // 排除此属性
    public string ReadOnly { get; }                      // 无 setter 自动排除
}
```

属性名自动转 snake_case（`UserName` → `user_name`）。支持 class / record / struct / record struct，源生成器自动选最优映射策略。

**DataAnnotations 验证**：仓储方法自动校验带 `ValidationAttribute` 的参数和实体字段，校验失败抛 `ValidationException`。

---

## 仓储模式

```csharp
// 1. 接口 — 继承 ICrudRepository 获得 46 个内置方法
public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
}

// 2. 实现 — partial class，代码自动生成，方言运行时传入
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection, SqlDialect dialect) : IUserRepository { }

// 3. 使用
var repo = new UserRepository(conn, SqlDefine.SQLite);  // 或 PostgreSql / MySql / SqlServer
```

### 内置 CRUD（46 个）

```csharp
// 查询
await repo.GetByIdAsync(id);
await repo.GetByIdsAsync(ids);
await repo.GetAllAsync();
await repo.GetWhereAsync(u => u.IsActive);
await repo.GetFirstWhereAsync(u => u.Age > 18);
await repo.GetPagedAsync(pageNumber: 1, pageSize: 20);
await repo.GetPagedWhereAsync(u => u.IsActive, 1, 20);
await repo.ExistsByIdAsync(id);
await repo.ExistsAsync(u => u.Name == "John");
await repo.CountAsync();
await repo.CountWhereAsync(u => u.Age > 18);
repo.AsQueryable();

// 命令
await repo.InsertAndGetIdAsync(user);
await repo.InsertAsync(user);
await repo.BatchInsertAsync(users);          // ← 多条插入必须用这个
await repo.UpdateAsync(user);
await repo.BatchUpdateAsync(users);
await repo.DynamicUpdateAsync(id, u => new User { Name = "John" });           // ← 部分字段更新
await repo.DynamicUpdateWhereAsync(u => new User { IsActive = false }, pred); // ← 带条件的批量部分更新
await repo.DeleteAsync(id);
await repo.DeleteByIdsAsync(ids);
await repo.DeleteWhereAsync(u => u.IsActive == false);
await repo.DeleteAllAsync();
```

### 事务

```csharp
using var tx = connection.BeginTransaction();
repo.Transaction = tx;
try { await repo.InsertAsync(u); tx.Commit(); }
catch { tx.Rollback(); throw; }
```

### 轻量模式（不写接口/仓储类）

```csharp
var adults = await conn.SqlxQueryAsync<User>(
    "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge",
    SqlDefine.SQLite, new { minAge = 18 });

var total = await conn.SqlxScalarAsync<long, User>("SELECT COUNT(*) FROM {{table}}", SqlDefine.SQLite);

await conn.SqlxExecuteAsync<User>(
    "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
    SqlDefine.SQLite, new User { Id = 1, Name = "Updated", Age = 20 });
```

### 调试 SQL

返回类型用 `SqlTemplate` 类（不执行查询，只返回生成的 SQL 和参数）：

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
SqlTemplate GetByIdSql(long id);   // 调试用

var t = repo.GetByIdSql(123);
Console.WriteLine(t.Sql);          // 查看生成的 SQL
```

---

## SQL 模板占位符

| 占位符 | 用途 |
|--------|------|
| `{{table}}` | 表名（带方言引号） |
| `{{columns}}` | 所有可写列 |
| `{{columns --exclude Id,Age}}` | 排除指定列 |
| `{{values --exclude Id}}` | 参数占位符，排除指定列 |
| `{{values --inline Status='x',CreatedAt=CURRENT_TIMESTAMP}}` | 含内联表达式的参数列表 |
| `{{set --exclude Id}}` | UPDATE SET 子句 |
| `{{set --inline Version=Version+1}}` | SET 含内联表达式 |
| `{{where --object filter}}` | 字典生成 WHERE 条件 |
| `{{where --param predicate}}` | `Expression<Func<T,bool>>` 生成 WHERE |
| `{{var --name varName}}` | 运行时变量（字面量插入，非参数） |
| `{{if notnull=param}}...{{/if}}` | 参数非 null 时包含该片段 |

**内联表达式**用于 INSERT 默认值或 UPDATE 计算字段，用属性名（PascalCase），自动转列名：

```csharp
// INSERT 设置默认值
[SqlTemplate(@"INSERT INTO {{table}} ({{columns --exclude Id}})
    VALUES ({{values --exclude Id --inline Status='pending',CreatedAt=CURRENT_TIMESTAMP}})")]
Task<int> CreateAsync(string name);

// UPDATE 递增版本号
[SqlTemplate(@"UPDATE {{table}}
    SET {{set --exclude Id,Version --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}}
    WHERE id = @id")]
Task<int> UpdateAsync(long id, string name);
```

---

## 动态 WHERE

**三种方式，按场景选择：**

```csharp
// 1. 可选参数 — 参数可能为 null 时用 {{if}}
[SqlTemplate(@"SELECT {{columns}} FROM {{table}} WHERE 1=1
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}")]
Task<List<User>> SearchAsync(string? name, int? minAge);

// 2. 字典条件 — 运行时动态决定过滤字段时用
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> FilterAsync(IReadOnlyDictionary<string, object?> filter);
// null 值 → IS NULL；空字典 → WHERE 1=1

// 3. 表达式条件 — 类型安全的 LINQ 表达式时用
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
await repo.GetWhereAsync(u => u.Age >= 18 && u.IsActive);
```

---

## SqlxVar — 运行时变量

将应用程序控制的值作为**字面量**插入 SQL（适合租户 ID、枚举值等固定上下文值）：

```csharp
public partial class UserRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;  // 来自 auth token，非用户输入
}

[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}")]
Task<List<User>> GetAllAsync();
```

⚠️ `{{var}}` 是字面量，**绝不能用于用户输入**（SQL 注入风险）。用户输入必须用 `@param`。

---

## 输出参数

```csharp
// 同步 — 用 out（Output）或 ref（InputOutput）
int InsertAndGetId(string name, int age, out int id);
int IncrementCounter(string name, ref int currentValue);

// 异步 — C# 不允许 async 用 out/ref，改用 OutputParameter<T>
Task<int> InsertUserAsync(string name, int age, OutputParameter<int> userId);

var userId = new OutputParameter<int>();
await repo.InsertUserAsync("Alice", 25, userId);
Console.WriteLine(userId.Value);

// InputOutput 模式（带初始值）
var counter = OutputParameter<int>.WithValue(100);
await repo.IncrementCounterAsync("views", counter);
```

`[OutputParameter(DbType.X)]` 特性可选，大多数情况自动推断。

---

## 多结果集

单次往返返回多个值，用元组返回类型：

```csharp
[SqlTemplate(@"INSERT INTO users (name) VALUES (@name);
    SELECT last_insert_rowid();
    SELECT COUNT(*) FROM users")]
[ResultSetMapping(0, "rowsAffected")]
[ResultSetMapping(1, "userId")]
[ResultSetMapping(2, "totalUsers")]
Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
```

不加 `[ResultSetMapping]` 时：第1个元素 → 受影响行数，其余 → SELECT 结果按顺序。

---

## IQueryable & LINQ

适合需要动态组合查询条件、不想写 SQL 字符串的场景：

```csharp
var users = await SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10)
    .WithConnection(connection)
    .ToListAsync();

// 投影
var results = await SqlQuery<User>.ForPostgreSQL()
    .Select(u => new { u.Id, u.Name })
    .WithConnection(connection)
    .ToListAsync();

// 聚合
var count = await SqlQuery<User>.ForSqlite().Where(u => u.Age >= 18).WithConnection(conn).CountAsync();
var first = await SqlQuery<User>.ForSqlite().Where(u => u.Age >= 18).WithConnection(conn).FirstOrDefaultAsync();
```

支持：`Where` `Select` `OrderBy` `ThenBy` `Take` `Skip` `GroupBy` `Distinct` `Join` `Count` `Min` `Max` `Sum` `Average` `First` `FirstOrDefault` `Any`

### 可复用表达式模板（Any.Value）

```csharp
Expression<Func<User, bool>> tmpl = u =>
    u.Age >= Any.Value<int>("minAge") && u.Age <= Any.Value<int>("maxAge");

var sql = ExpressionBlockResult.Parse(tmpl.Body, SqlDefine.SQLite)
    .WithParameter("minAge", 18)
    .WithParameter("maxAge", 30);
// → ([age] >= @minAge AND [age] <= @maxAge)
```

---

## SqlBuilder — 动态 SQL

需要完全动态拼接 SQL 时用，插值字符串自动参数化：

```csharp
using var b = new SqlBuilder(SqlDefine.SQLite);
b.Append($"SELECT * FROM users WHERE 1=1");
if (nameFilter != null) b.Append($" AND name LIKE {"%" + nameFilter + "%"}");
if (minAge.HasValue)    b.Append($" AND age >= {minAge.Value}");
var t = b.Build();
// SQL: SELECT * FROM users WHERE 1=1 AND name LIKE @p0 AND age >= @p1

// 集成 {{columns}}/{{table}}
var ctx = PlaceholderContext.Create<User>(SqlDefine.SQLite);
using var b2 = new SqlBuilder(ctx);
b2.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });

// 子查询
using var sub = new SqlBuilder(SqlDefine.SQLite);
sub.Append($"SELECT id FROM orders WHERE total > {1000}");
using var main = new SqlBuilder(SqlDefine.SQLite);
main.Append($"SELECT * FROM users WHERE id IN ");
main.AppendSubquery(sub);
```

⚠️ `AppendRaw` 不参数化，只用于 SQL 关键字等固定字符串，**绝不传入外部输入**。

---

## SqlxContext — 多仓储上下文

管理多个仓储、统一事务和异常处理：

```csharp
[SqlxContext]
[IncludeRepository(typeof(UserRepository))]
[IncludeRepository(typeof(OrderRepository))]
public partial class AppDbContext : SqlxContext { }

services.AddSqlxContext<AppDbContext>((sp, options) =>
{
    options.OnException = (ex, ctx) => logger.LogError(ex, "{Method}", ctx.MethodName);
    options.EnableRetry = true;
    options.MaxRetryCount = 3;
    options.UseExponentialBackoff = true;
    return new AppDbContext(sp.GetRequiredService<SqliteConnection>(), options, sp);
});

// 使用
await using var tx = await context.BeginTransactionAsync();
await context.Users.InsertAsync(user);
await context.Orders.InsertAsync(order);
await tx.CommitAsync();
```

`SqlxException` 包含：`Sql`、`MethodName`、`DurationMilliseconds`、`TransactionId`、`CorrelationId`。

---

## 何时用哪种方式

| 场景 | 推荐方式 |
|------|---------|
| 标准 CRUD | `ICrudRepository` 内置方法 |
| 固定 SQL + 参数 | `[SqlTemplate]` 方法 |
| 部分字段更新 | `DynamicUpdateAsync` |
| 可选过滤条件 | `{{if notnull=param}}` |
| 运行时动态字段过滤 | `{{where --object filter}}` 字典 |
| 类型安全条件 | `{{where --param predicate}}` 表达式 |
| 完全动态拼接 | `SqlBuilder` |
| LINQ 风格查询 | `SqlQuery<T>` |
| 不想写接口/仓储 | `conn.SqlxQueryAsync<T>` 轻量 API |
| 多仓储 + 统一事务 | `SqlxContext` |
| 单次往返多个返回值 | 元组返回 + `[ResultSetMapping]` |
| 应用上下文值注入 SQL | `[SqlxVar]` + `{{var}}` |

---

## 陷阱速查

| ❌ 错误 | ✅ 正确 |
|--------|--------|
| `foreach` 循环 `InsertAsync` | `BatchInsertAsync(list)` |
| 硬编码列名 `SELECT id, name` | `SELECT {{columns}}` |
| `{{var}}` 用于用户输入 | 用 `@param` 参数 |
| async 方法用 `out int id` | `OutputParameter<int>` |
| 字典 WHERE 期望 null 被忽略 | null → `IS NULL`，不想过滤就不加该 key |
| `.Result` / `.Wait()` 阻塞 | `await` |
| 改了特性但生成代码没变 | `dotnet clean && dotnet build` |
| 多步操作不加事务 | `repo.Transaction = tx` |
