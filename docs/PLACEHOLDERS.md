# 🎯 Sqlx 占位符完整指南

## 📖 什么是占位符？

占位符是 Sqlx 的核心功能，用于**自动生成复杂的 SQL 内容**，减少手写代码，提高类型安全性。

**设计理念：**
- ✅ **智能占位符**：用于自动生成复杂内容（如列名列表、SET 子句）
- ✅ **直接写 SQL**：简单的内容（如 WHERE 条件、聚合函数）直接写更清晰

---

## 🚨 动态占位符（@ 前缀）- 高级功能

### 什么是动态占位符？

动态占位符使用 `{{@paramName}}` 语法，允许在运行时动态指定表名、列名或 SQL 片段。主要用于：
- 🏢 多租户系统（每个租户独立的表）
- 🗂️ 分库分表（动态表后缀）
- 🔍 动态查询（运行时构建 WHERE 子句）

### ⚠️ 安全警告

**动态占位符会绕过参数化查询，存在 SQL 注入风险！**

**使用前必须：**
- ✅ 显式标记 `[DynamicSql]` 特性（否则编译错误）
- ✅ 在调用前进行严格验证（白名单）
- ✅ 不要在公共 API 中暴露
- ✅ 生成的代码会包含内联验证

---

### 动态占位符类型

#### 1. `[DynamicSql]` - 标识符（表名/列名）

**验证规则（最严格）：**
- 只允许字母、数字、下划线
- 必须以字母或下划线开头
- 长度限制：1-128 字符
- 不能包含 SQL 关键字

```csharp
// ✅ 多租户表名
public interface IUserRepository
{
    [Sqlx("SELECT {{columns}} FROM {{@tableName}} WHERE id = @id")]
    Task<User?> GetFromTableAsync([DynamicSql] string tableName, int id);
}

// 调用前验证
var allowedTables = new[] { "users", "admin_users", "guest_users" };
if (!allowedTables.Contains(tableName))
    throw new ArgumentException("Invalid table name");

var user = await repo.GetFromTableAsync("users", userId);
// 生成 SQL: SELECT id, name, email FROM users WHERE id = @id
```

---

#### 2. `[DynamicSql(Type = DynamicSqlType.Fragment)]` - SQL 片段

**验证规则（中等）：**
- 禁止 DDL 操作（DROP、TRUNCATE、ALTER、CREATE）
- 禁止危险函数（EXEC、EXECUTE、xp_、sp_executesql）
- 禁止注释符号（--, /*）
- 长度限制：1-4096 字符

```csharp
// ✅ 动态 WHERE 子句
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{@whereClause}}")]
Task<List<User>> QueryAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);

// 调用
var where = "age > 18 AND status = 'active'";
var users = await repo.QueryAsync(where);
// 生成 SQL: SELECT id, name, email FROM users WHERE age > 18 AND status = 'active'
```

---

#### 3. `[DynamicSql(Type = DynamicSqlType.TablePart)]` - 表名部分

**验证规则（严格）：**
- 只允许字母和数字
- 不允许下划线、空格等特殊字符
- 长度限制：1-64 字符

```csharp
// ✅ 分表后缀
[Sqlx("SELECT {{columns}} FROM logs_{{@suffix}} WHERE created_at > @date")]
Task<List<Log>> GetLogsAsync([DynamicSql(Type = DynamicSqlType.TablePart)] string suffix, DateTime date);

// 调用
var logs = await repo.GetLogsAsync("202410", DateTime.Today);
// 生成 SQL: SELECT id, level, message FROM logs_202410 WHERE created_at > @date
```

---

### 完整示例：多租户系统

```csharp
// 1. 定义接口
public interface ITenantRepository
{
    [Sqlx("SELECT {{columns}} FROM {{@tenantTable}} WHERE id = @id")]
    Task<User?> GetUserAsync([DynamicSql] string tenantTable, int id);

    [Sqlx("SELECT {{columns}} FROM {{@tenantTable}} WHERE {{@condition}}")]
    Task<List<User>> QueryUsersAsync(
        [DynamicSql] string tenantTable,
        [DynamicSql(Type = DynamicSqlType.Fragment)] string condition);
}

// 2. 使用（带验证）
public class TenantService
{
    private readonly ITenantRepository _repo;
    private static readonly HashSet<string> AllowedTenants = new()
    {
        "tenant1_users", "tenant2_users", "tenant3_users"
    };

    public async Task<User?> GetTenantUserAsync(string tenantId, int userId)
    {
        // ✅ 白名单验证
        var tableName = $"{tenantId}_users";
        if (!AllowedTenants.Contains(tableName))
            throw new ArgumentException($"Invalid tenant: {tenantId}");

        return await _repo.GetUserAsync(tableName, userId);
    }

    public async Task<List<User>> QueryActiveUsers(string tenantId)
    {
        var tableName = $"{tenantId}_users";
        if (!AllowedTenants.Contains(tableName))
            throw new ArgumentException($"Invalid tenant: {tenantId}");

        // ✅ 硬编码的安全条件
        var condition = "is_active = 1 AND deleted_at IS NULL";

        return await _repo.QueryUsersAsync(tableName, condition);
    }
}
```

---

#### 4. 高级动态占位符（JOIN、SET、ORDERBY、GROUPBY）

**适用场景：** 在运行时动态构建复杂 SQL 子句

| 占位符 | 用法 | 说明 |
|--------|------|------|
| `{{set @param}}` | 动态 SET 子句 | 运行时指定更新字段 |
| `{{orderby @param}}` | 动态 ORDER BY | 运行时指定排序规则 |
| `{{join @param}}` | 动态 JOIN | 运行时指定 JOIN 子句 |
| `{{groupby @param}}` | 动态 GROUP BY | 运行时指定分组列 |

**性能优化：**
- ✅ 使用字符串插值（零 `Replace` 调用）
- ✅ 编译时拆分静态/动态部分
- ✅ 内联 SQL 验证（~50ns）
- ✅ 零 GC 开销（基于 `ReadOnlySpan<char>`）

```csharp
// ✅ 动态 SET - 支持部分字段更新
public interface IUserRepository
{
    [Sqlx("UPDATE {{table}} SET {{set @updates}} WHERE id = @id")]
    Task<int> UpdatePartialAsync(
        int id, 
        [DynamicSql(Type = DynamicSqlType.Fragment)] string updates);
}

// 调用示例
await repo.UpdatePartialAsync(1, "name = @name, email = @email");
// 生成: UPDATE users SET name = @name, email = @email WHERE id = 1

// ✅ 动态 ORDER BY - 支持多列排序
public interface ITodoRepository
{
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE status = @status {{orderby @sort}}")]
    Task<List<Todo>> GetByStatusAsync(
        string status,
        [DynamicSql(Type = DynamicSqlType.Fragment)] string sort);
}

// 调用示例
var todos = await repo.GetByStatusAsync("active", "priority DESC, created_at DESC");
// 生成: SELECT * FROM todos WHERE status = @status ORDER BY priority DESC, created_at DESC

// ✅ 动态 JOIN - 支持复杂查询
public interface IOrderRepository
{
    [Sqlx("SELECT o.*, c.name FROM orders o {{join @joins}} WHERE o.id = @id")]
    Task<Order?> GetWithDetailsAsync(
        int id,
        [DynamicSql(Type = DynamicSqlType.Fragment)] string joins);
}

// 调用示例
var order = await repo.GetWithDetailsAsync(1, "INNER JOIN customers c ON o.customer_id = c.id");
// 生成: SELECT o.*, c.name FROM orders o INNER JOIN customers c ON o.customer_id = c.id WHERE o.id = @id

// ✅ 动态 GROUP BY - 支持聚合查询
public interface IReportRepository
{
    [Sqlx("SELECT {{groupby @groupCols}}, COUNT(*) as cnt FROM {{table}} {{groupby @groupCols}}")]
    Task<List<Dictionary<string, object>>> GetAggregatedAsync(
        [DynamicSql(Type = DynamicSqlType.Fragment)] string groupCols);
}

// 调用示例
var report = await repo.GetAggregatedAsync("category, status");
// 生成: SELECT category, status, COUNT(*) as cnt FROM items GROUP BY category, status
```

**安全验证：**
- ✅ 所有动态占位符都必须标记 `[DynamicSql(Type = DynamicSqlType.Fragment)]`
- ✅ 生成代码自动包含 `SqlValidator.IsValidFragment()` 检查
- ✅ 拒绝 DDL、EXEC、注释等危险操作

**最佳实践：**
1. 使用预定义的常量字符串（而非用户输入）
2. 在调用前进行白名单验证
3. 优先使用静态占位符（如 `{{orderby created_at}}`）
4. 仅在确实需要动态性时使用

---

### 生成的代码示例

```csharp
// Sqlx 生成的方法（包含内联验证）
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // ✅ 内联验证代码（编译器完全优化）
    if (tableName.Length == 0 || tableName.Length > 128)
        throw new ArgumentException("Invalid table name length", nameof(tableName));

    if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
        throw new ArgumentException("Table name must start with letter or underscore", nameof(tableName));

    if (tableName.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
        tableName.Contains("--") ||
        tableName.Contains("/*"))
        throw new ArgumentException("Invalid table name", nameof(tableName));

    // ✅ 使用 C# 字符串插值（高性能）
    var sql = $"SELECT id, name, email FROM {tableName} WHERE id = @id";

    // ... 执行 SQL
}

// Sqlx 生成的动态 ORDER BY 方法（字符串插值优化）
public async Task<List<Todo>> GetByStatusAsync(string status, string sort)
{
    // ✅ 验证 SQL 片段（~50ns）
    if (!SqlValidator.IsValidFragment(sort.AsSpan()))
        throw new ArgumentException($"Invalid SQL fragment: {sort}. Contains dangerous keywords.", nameof(sort));

    var __orderByClause_0__ = sort;

    // ✅ 字符串插值（零 Replace 调用，零 GC）
    var sql = $@"SELECT id, title, status FROM todos WHERE status = @status ORDER BY {__orderByClause_0__}";

    // ... 执行 SQL
}
```

---

### 最佳实践

#### ✅ 推荐做法
1. 使用白名单验证所有动态参数
2. 在内部服务层使用，不暴露给公共 API
3. 使用硬编码的常量作为动态参数
4. 为动态查询方法编写充分的单元测试

#### ❌ 禁止做法
1. 不要直接使用用户输入作为动态参数
2. 不要在 Web API 控制器中直接使用
3. 不要禁用或跳过验证逻辑
4. 不要在动态片段中使用 DDL 操作

---

### Roslyn 分析器支持

Sqlx 提供 10 个诊断规则来检测不安全的使用：

- **SQLX2001** (Error): 使用 `{{@}}` 但参数未标记 `[DynamicSql]`
- **SQLX2002** (Warning): 动态参数来自不安全来源（用户输入）
- **SQLX2003** (Warning): 调用前缺少验证
- **SQLX2004** (Info): 建议使用白名单验证
- **SQLX2005** (Warning): 在公共 API 中暴露动态参数
- **SQLX2006** (Error): 动态参数类型不是 string
- **SQLX2007** (Warning): SQL 模板包含危险操作
- **SQLX2008** (Info): 建议添加单元测试
- **SQLX2009** (Warning): 缺少长度限制检查
- **SQLX2010** (Error): `[DynamicSql]` 特性使用错误

详见：[分析器设计文档](../ANALYZER_DESIGN.md)

---

## 🌟 核心占位符（必会）

### 1. `{{table}}` - 表名

**作用**：自动从 `TableName` 特性获取表名并转换为 snake_case

```csharp
[TableName("TodoItems")]
public record Todo { ... }

[Sqlx("SELECT * FROM {{table}}")]
Task<List<Todo>> GetAllAsync();

// 生成: SELECT * FROM todo_items
```

**选项**：
- 无需选项，自动处理

---

### 2. `{{columns}}` - 列名列表

**作用**：自动从实体类生成所有列名

```csharp
// 基本用法 - 生成所有列名
[Sqlx("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();
// 生成: SELECT id, name, email, age, is_active FROM users

// 排除指定列
[Sqlx("SELECT {{columns --exclude Password Salt}} FROM {{table}}")]
Task<List<User>> GetPublicInfoAsync();
// 生成: SELECT id, name, email, age, is_active FROM users

// 只包含指定列
[Sqlx("SELECT {{columns --only id name email}} FROM {{table}}")]
Task<List<User>> GetBasicInfoAsync();
// 生成: SELECT id, name, email FROM users
```

**选项**：
- `--exclude col1 col2` - 排除指定列
- `--only col1 col2` - 只包含指定列

---

### 3. `{{values}}` - 值占位符

**作用**：自动生成对应的参数占位符（@param1, @param2, ...）

```csharp
[Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);

// 生成: INSERT INTO users (name, email, age, is_active)
//       VALUES (@Name, @Email, @Age, @IsActive)
```

**说明**：
- 自动匹配 `{{columns}}` 的列顺序
- 自动处理 `--exclude` 和 `--only` 选项
- 参数名与属性名一致（PascalCase）

---

### 4. `{{set}}` - SET 子句

**作用**：自动生成 UPDATE 语句的 SET 子句

```csharp
// 更新所有列（排除Id）
[Sqlx("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
Task<int> UpdateAsync(User user);
// 生成: UPDATE users SET name=@Name, email=@Email, age=@Age, is_active=@IsActive WHERE id = @id

// 排除不可变字段
[Sqlx("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(Todo todo);
// 生成: UPDATE todos SET title=@Title, description=@Description, updated_at=@UpdatedAt WHERE id = @id

// 只更新指定字段
[Sqlx("UPDATE {{table}} SET {{set --only priority updated_at}} WHERE id = @id")]
Task<int> UpdatePriorityAsync(Todo todo);
// 生成: UPDATE todos SET priority=@Priority, updated_at=@UpdatedAt WHERE id = @id
```

**选项**：
- `--exclude col1 col2` - 排除指定列
- `--only col1 col2` - 只包含指定列

---

### 5. `{{orderby}}` - 排序

**作用**：自动生成 ORDER BY 子句

```csharp
// 升序（默认）
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at}}")]
Task<List<Todo>> GetAllAsync();
// 生成: SELECT ... FROM todos ORDER BY created_at

// 降序
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
Task<List<Todo>> GetAllDescAsync();
// 生成: SELECT ... FROM todos ORDER BY created_at DESC

// 升序（显式指定）
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby name --asc}}")]
Task<List<User>> GetAllAscAsync();
// 生成: SELECT ... FROM users ORDER BY name ASC

// 多列排序（使用多个 orderby）
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby priority --desc}} {{orderby created_at --desc}}")]
Task<List<Todo>> GetAllSortedAsync();
// 生成: SELECT ... FROM todos ORDER BY priority DESC, created_at DESC
```

**选项**：
- `--desc` - 降序（DESC）
- `--asc` - 升序（ASC，默认）

---

## ✍️ 直接写 SQL（推荐）

以下内容直接写 SQL 更清晰，无需占位符：

### WHERE 条件

```csharp
// ✅ 直接写 - 清晰直观
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(int id);

[Sqlx("SELECT {{columns}} FROM {{table}} WHERE is_active = @active")]
Task<List<User>> GetActiveUsersAsync(bool active);

[Sqlx("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND is_active = @active")]
Task<List<User>> SearchAsync(int minAge, bool active);

// OR 组合
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE name LIKE @query OR email LIKE @query")]
Task<List<User>> SearchByNameOrEmailAsync(string query);

// NULL 检查
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE email IS NOT NULL")]
Task<List<User>> GetUsersWithEmailAsync();

// IN 查询（SQLite）
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<List<User>> GetByIdsAsync(string idsJson);
```

### INSERT / UPDATE / DELETE

```csharp
// ✅ 直接写 - 一目了然
[Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);

[Sqlx("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

[Sqlx("DELETE FROM {{table}} WHERE id = @id")]
Task<int> DeleteAsync(int id);
```

### 聚合函数

```csharp
// ✅ 直接写 - 比占位符更短
[Sqlx("SELECT COUNT(*) FROM {{table}}")]
Task<int> GetTotalCountAsync();

[Sqlx("SELECT COUNT(*) FROM {{table}} WHERE is_active = @active")]
Task<int> GetActiveCountAsync(bool active);

[Sqlx("SELECT SUM(amount) FROM {{table}} WHERE user_id = @userId")]
Task<decimal> GetTotalAmountAsync(int userId);

[Sqlx("SELECT AVG(age) FROM {{table}}")]
Task<double> GetAverageAgeAsync();

[Sqlx("SELECT MAX(created_at) FROM {{table}}")]
Task<DateTime> GetLatestDateAsync();
```

---

## 🎓 高级占位符（可选）

以下占位符用于特定场景，可以简化复杂查询，但大部分情况下**直接写SQL更清晰**。

### 分页占位符

#### `{{page}}` - 智能分页
自动计算 OFFSET 和 LIMIT（适用于标准分页场景）

```csharp
// 自动计算偏移量: OFFSET = (page - 1) * pageSize
[Sqlx("SELECT {{columns}} FROM {{table}} {{page}}")]
Task<List<User>> GetPagedAsync(int page, int pageSize);

// MySQL/PostgreSQL 生成: LIMIT @pageSize OFFSET ((@page - 1) * @pageSize)
// SQL Server 生成: OFFSET ... ROWS FETCH NEXT ... ROWS ONLY
```

### 条件表达式

#### `{{coalesce}}` - NULL 合并
返回第一个非NULL值

```csharp
// 多列合并
[Sqlx("SELECT id, {{coalesce|columns=email,phone,address|default='N/A'}} AS contact FROM {{table}}")]
Task<List<Contact>> GetContactsAsync();

// 生成: SELECT id, COALESCE(email, phone, address, 'N/A') AS contact FROM users
```

#### `{{case}}` - 条件表达式
生成 CASE WHEN 语句

```csharp
[Sqlx("SELECT id, name, {{case --when status=1 --then 'Active' --else 'Inactive'}} AS status_text FROM {{table}}")]
Task<List<User>> GetUsersWithStatusAsync();
```

### 窗口函数

#### `{{row_number}}` - 行号
为查询结果添加行号

```csharp
[Sqlx("SELECT {{row_number|orderby=created_at}} AS row_num, {{columns}} FROM {{table}}")]
Task<List<User>> GetUsersWithRowNumberAsync();

// 生成: SELECT ROW_NUMBER() OVER (ORDER BY created_at) AS row_num, ... FROM users
```

#### `{{rank}}` / `{{dense_rank}}` - 排名
为查询结果添加排名

```csharp
[Sqlx("SELECT {{rank|orderby=score --desc}} AS rank, name, score FROM {{table}}")]
Task<List<Player>> GetLeaderboardAsync();

// 生成: SELECT RANK() OVER (ORDER BY score DESC) AS rank, name, score FROM players
```

### JSON 操作

#### `{{json_extract}}` - 提取 JSON 字段
从 JSON 列中提取值

```csharp
[Sqlx("SELECT id, {{json_extract|column=metadata|path=$.userId}} AS user_id FROM {{table}}")]
Task<List<Event>> GetEventsAsync();

// SQL Server: JSON_VALUE(metadata, '$.userId')
// PostgreSQL: metadata->'$.userId'
// MySQL: JSON_EXTRACT(metadata, '$.userId')
```

### 字符串函数

#### `{{group_concat}}` - 分组字符串聚合
将分组结果连接成字符串

```csharp
[Sqlx("SELECT user_id, {{group_concat|column=tag|separator=,}} AS tags FROM user_tags GROUP BY user_id")]
Task<List<UserTags>> GetUserTagsAsync();

// SQL Server: STRING_AGG(tag, ',')
// MySQL: GROUP_CONCAT(tag SEPARATOR ',')
// PostgreSQL: STRING_AGG(tag, ',')
```

#### `{{concat}}` - 字符串连接
连接多个列

```csharp
[Sqlx("SELECT {{concat|columns=first_name,last_name|separator= }} AS full_name FROM {{table}}")]
Task<List<User>> GetFullNamesAsync();

// 生成: SELECT CONCAT_WS(' ', first_name, last_name) AS full_name FROM users
```

#### `{{substring}}` - 子字符串
提取字符串的一部分

```csharp
[Sqlx("SELECT {{substring|column=email|start=1|length=10}} AS email_prefix FROM {{table}}")]
Task<List<string>> GetEmailPrefixesAsync();

// 生成: SELECT SUBSTRING(email, 1, 10) AS email_prefix FROM users
```

### 数学函数

#### `{{round}}` / `{{power}}` / `{{sqrt}}` - 数学运算
常用数学函数

```csharp
[Sqlx("SELECT {{round|column=price|precision=2}} AS rounded_price FROM {{table}}")]
Task<List<Product>> GetProductsAsync();

// 生成: SELECT ROUND(price, 2) AS rounded_price FROM products
```

### 类型转换

#### `{{cast}}` - 类型转换
转换列的数据类型

```csharp
[Sqlx("SELECT {{cast|column=id|as=VARCHAR}} AS id_string FROM {{table}}")]
Task<List<string>> GetIdsAsStringsAsync();

// 生成: SELECT CAST(id AS VARCHAR) AS id_string FROM users
```

### 批量操作

#### `{{upsert}}` - 插入或更新
自动生成 UPSERT 语句（根据数据库方言）

```csharp
[Sqlx("{{upsert|conflict=id}}")]
Task<int> UpsertAsync(User user);

// PostgreSQL: INSERT ... ON CONFLICT (id) DO UPDATE SET ...
// MySQL: INSERT ... ON DUPLICATE KEY UPDATE ...
// SQLite: INSERT OR REPLACE INTO ...
```

---

## 💡 占位符选择建议

| 场景 | 推荐方案 | 原因 |
|------|---------|------|
| **简单查询** | ❌ 不用高级占位符<br>✅ 直接写 SQL | 更清晰、更灵活 |
| **标准分页** | ✅ `{{page}}`<br>⚠️ 或直接写 LIMIT/OFFSET | 占位符自动适配数据库 |
| **窗口函数** | ✅ `{{row_number}}`、`{{rank}}`<br>⚠️ 或直接写 | 占位符简化语法 |
| **JSON 查询** | ✅ `{{json_extract}}`<br>⚠️ 必须适配多数据库时 | 自动适配不同数据库语法 |
| **字符串聚合** | ✅ `{{group_concat}}`<br>⚠️ 必须适配多数据库时 | 自动适配不同数据库语法 |
| **UPSERT** | ✅ `{{upsert}}`<br>⚠️ 必须适配多数据库时 | 不同数据库语法差异大 |
| **简单数学函数** | ❌ 不用占位符<br>✅ 直接写 `ROUND(price, 2)` | 占位符反而更复杂 |
| **WHERE 条件** | ❌ 不用占位符<br>✅ 直接写 SQL | 直接写更直观 |

**核心原则：**
- ✅ 使用核心占位符（`{{table}}`, `{{columns}}`, `{{values}}`, `{{set}}`, `{{orderby}}`）
- ⚠️ 高级占位符仅在**多数据库适配**或**复杂场景**下使用
- ❌ 简单场景下，直接写 SQL 永远是最佳选择

---

## 📚 完整示例：TodoWebApi

### 数据模型

```csharp
[TableName("todos")]
public record Todo
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

### 服务接口

```csharp
public interface ITodoService
{
    // 1. 查询所有 - {{columns}} + {{table}} + {{orderby}}
    [Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<Todo>> GetAllAsync();

    // 2. 根据ID查询 - {{columns}} + {{table}} + WHERE
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Todo?> GetByIdAsync(long id);

    // 3. 创建 - INSERT INTO + {{table}} + {{columns}} + {{values}}
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);

    // 4. 更新 - UPDATE + {{table}} + {{set}} + WHERE
    [Sqlx("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(Todo todo);

    // 5. 删除 - DELETE FROM + {{table}} + WHERE
    [Sqlx("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    // 6. 搜索 - WHERE ... OR ...
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query {{orderby updated_at --desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    // 7. 条件查询 - WHERE ... AND ...
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted {{orderby priority --desc}}")]
    Task<List<Todo>> GetHighPriorityAsync(int minPriority, bool isCompleted);

    // 8. NULL 检查 - WHERE ... IS NOT NULL
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE due_date IS NOT NULL AND due_date <= @maxDueDate {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime maxDueDate);

    // 9. 计数 - COUNT(*)
    [Sqlx("SELECT COUNT(*) FROM {{table}}")]
    Task<int> GetTotalCountAsync();

    // 10. 批量更新 - {{set --only}}
    [Sqlx("UPDATE {{table}} SET {{set --only priority updated_at}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdatePriorityBatchAsync(string idsJson, int newPriority, DateTime updatedAt);
}
```

### 服务实现

```csharp
[TableName("todos")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoService))]
public partial class TodoService(SqliteConnection connection) : ITodoService;
```

**就这么简单！** Sqlx 自动生成所有方法的实现代码。

---

## 🎯 最佳实践

### 1. 何时使用占位符？

| 场景 | 使用占位符 | 原因 |
|------|-----------|------|
| **列名列表** | ✅ `{{columns}}` | 自动生成，类型安全，维护方便 |
| **表名** | ✅ `{{table}}` | 自动转换 snake_case，统一管理 |
| **SET 子句** | ✅ `{{set}}` | 自动生成复杂的赋值语句 |
| **值占位符** | ✅ `{{values}}` | 自动匹配列顺序 |
| **排序** | ✅ `{{orderby}}` | 支持选项，清晰 |
| **WHERE 条件** | ❌ 直接写 | 更直观，更灵活 |
| **INSERT/UPDATE/DELETE** | ❌ 直接写 | 更清晰，无需记忆 |
| **COUNT/SUM/AVG** | ❌ 直接写 | 比占位符更短 |

### 2. 排除字段的技巧

```csharp
// ✅ 插入时排除自增ID
{{columns --exclude Id}}

// ✅ 更新时排除不可变字段
{{set --exclude Id CreatedAt}}

// ✅ 只更新部分字段
{{set --only priority updated_at}}
```

### 3. 多列排序

```csharp
// ✅ 方法1：使用多个 orderby
{{orderby priority --desc}} {{orderby created_at --desc}}

// ✅ 方法2：直接写 SQL
ORDER BY priority DESC, created_at DESC
```

### 4. 参数化查询

```csharp
// ✅ 正确：使用参数
WHERE id = @id
WHERE age >= @minAge AND is_active = @active

// ❌ 错误：字符串拼接（SQL 注入风险）
WHERE id = " + id + "
```

---

## 🔍 常见问题

### Q1：如何查看生成的 SQL？

**A：** 生成的代码在 `obj/Debug/net9.0/generated/` 目录下：

```
obj/Debug/net9.0/generated/
└── Sqlx.Generator/
    └── Sqlx.Generator.CSharpGenerator/
        └── YourService.Repository.g.cs  ← 查看这个文件
```

### Q2：为什么不用 `{{where}}` 占位符？

**A：** 直接写 WHERE 条件更清晰、更灵活、更直观：

```csharp
// ❌ 使用占位符（过度抽象）
{{where id=@id}}

// ✅ 直接写 SQL（清晰直观）
WHERE id = @id
```

### Q3：为什么不用 `{{count}}` 占位符？

**A：** 直接写 `COUNT(*)` 更标准、更短、更清晰：

```csharp
// ❌ 使用占位符（多余）
SELECT {{count}} FROM {{table}}

// ✅ 直接写（标准 SQL）
SELECT COUNT(*) FROM {{table}}
```

### Q4：如何实现复杂的 WHERE 条件？

**A：** 直接写 SQL 表达式：

```csharp
// AND 组合
WHERE age >= @minAge AND is_active = @active AND country = @country

// OR 组合
WHERE name LIKE @query OR email LIKE @query OR phone LIKE @query

// 括号组合
WHERE (name = @name OR email = @email) AND is_active = true

// NULL 检查
WHERE email IS NOT NULL AND phone IS NOT NULL

// IN 查询
WHERE status IN ('pending', 'active', 'completed')
```

### Q5：添加新字段后需要做什么？

**A：** 什么都不用做！

1. 在实体类中添加属性
2. 在数据库建表语句中添加列
3. 重新编译

**所有使用占位符的 SQL 自动更新！** 这就是 Sqlx 的强大之处。

---

## 📖 相关文档

- [📋 快速开始](QUICK_START_GUIDE.md)
- [💡 最佳实践](BEST_PRACTICES.md)
- [🌐 多数据库支持](MULTI_DATABASE_TEMPLATE_ENGINE.md)
- [🚀 TodoWebApi 示例](../samples/TodoWebApi/README.md)

---

## 🎉 总结

### Sqlx 占位符设计理念

1. **智能占位符**：用于自动生成复杂内容
   - `{{table}}` - 表名转换
   - `{{columns}}` - 列名列表
   - `{{values}}` - 值占位符
   - `{{set}}` - SET 子句
   - `{{orderby}}` - 排序

2. **直接写 SQL**：用于简单清晰的内容
   - WHERE 条件
   - INSERT / UPDATE / DELETE 关键字
   - COUNT / SUM / AVG / MAX / MIN 聚合函数

**核心优势：**
- ✅ 学习成本低（只需记住 5 个核心占位符）
- ✅ 代码更清晰（直接写 SQL 更直观）
- ✅ 类型安全（编译时检查）
- ✅ 自动维护（添加字段自动更新）

**开始你的 Sqlx 之旅吧！** 🚀

