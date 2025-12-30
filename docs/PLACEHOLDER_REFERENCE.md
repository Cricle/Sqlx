# 📚 Sqlx 占位符完整参考手册

> **最后更新**: 2025-11-02
> **版本**: v0.5.1
> **适用于**: .NET 8.0+ | .NET 9.0+

## 📖 目录

- [核心占位符](#-核心占位符-必会)
- [扩展占位符](#-扩展占位符)
- [方言特定占位符](#-方言特定占位符)
- [动态占位符](#-动态占位符-高级)
- [最佳实践](#-最佳实践)

---

## 🌟 核心占位符 (必会)

这7个占位符是Sqlx的核心功能，覆盖90%的使用场景。

### 1. `{{table}}` - 表名

**功能**: 自动从`TableName`特性获取表名并转换为snake_case

```csharp
[TableName("UserProfiles")]
public class User { }

[SqlTemplate("SELECT * FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 生成: SELECT * FROM user_profiles
```

**选项**: 无

**多数据库支持**: ✅ 所有数据库

---

### 2. `{{columns}}` - 列名列表

**功能**: 自动从实体类生成列名列表

```csharp
// 所有列
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();
// 生成: SELECT id, name, email, age, created_at FROM users

// 排除列
[SqlTemplate("SELECT {{columns --exclude Password Salt}} FROM {{table}}")]
Task<List<User>> GetPublicAsync();
// 生成: SELECT id, name, email, age, created_at FROM users

// 只包含指定列
[SqlTemplate("SELECT {{columns --only Id Name Email}} FROM {{table}}")]
Task<List<User>> GetBasicAsync();
// 生成: SELECT id, name, email FROM users
```

**选项**:
- `--exclude col1 col2 ...` - 排除指定列
- `--only col1 col2 ...` - 只包含指定列

**多数据库支持**: ✅ 所有数据库

---

### 3. `{{values}}` - 值占位符

**功能**: 自动生成对应的参数占位符

```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
[ReturnInsertedId]
Task<long> InsertAsync(User user);

// 生成: INSERT INTO users (name, email, age) VALUES (@Name, @Email, @Age)
```

**选项**: 自动匹配`{{columns}}`的选项

**多数据库支持**: ✅ 所有数据库

---

### 4. `{{set}}` - SET子句

**功能**: 自动生成UPDATE语句的SET子句

```csharp
// 更新所有列（排除Id）
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @Id")]
Task<int> UpdateAsync(User user);
// 生成: UPDATE users SET name=@Name, email=@Email, age=@Age WHERE id = @Id

// 只更新指定字段
[SqlTemplate("UPDATE {{table}} SET {{set --only Name Email}} WHERE id = @Id")]
Task<int> UpdatePartialAsync(User user);
// 生成: UPDATE users SET name=@Name, email=@Email WHERE id = @Id
```

**选项**:
- `--exclude col1 col2 ...` - 排除指定列
- `--only col1 col2 ...` - 只包含指定列

**多数据库支持**: ✅ 所有数据库

---

### 5. `{{where}}` - WHERE子句

**功能**: 自动生成WHERE条件（基于方法参数）

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> FindAsync(string? name = null, int? minAge = null);

// name="John", minAge=18:
// 生成: SELECT * FROM users WHERE name = @name AND age >= @minAge

// name=null, minAge=18:
// 生成: SELECT * FROM users WHERE age >= @minAge
```

**选项**: 自动根据参数生成

**多数据库支持**: ✅ 所有数据库

---

### 6. `{{orderby}}` - 排序

**功能**: 生成ORDER BY子句

```csharp
// 单列升序（默认）
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby created_at}}")]
Task<List<User>> GetAllAsync();
// 生成: ORDER BY created_at

// 单列降序
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
Task<List<User>> GetLatestAsync();
// 生成: ORDER BY created_at DESC

// 多列排序
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby priority --desc}} {{orderby created_at}}")]
Task<List<Todo>> GetSortedAsync();
// 生成: ORDER BY priority DESC, created_at
```

**选项**:
- `--desc` - 降序
- `--asc` - 升序（默认）

**多数据库支持**: ✅ 所有数据库

---

### 7. `{{limit}}` - 分页限制

**功能**: 生成LIMIT/OFFSET子句（自动适配数据库）

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit}}")]
Task<List<User>> GetPagedAsync(int? limit = null, int? offset = null);

// SQLite/MySQL/PostgreSQL: LIMIT @limit OFFSET @offset
// SQL Server: OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
```

**选项**: 自动根据参数生成

**多数据库支持**: ✅ 自动适配所有数据库

---

## 🎯 扩展占位符

### JOIN操作

#### `{{join}}` - 连接查询

```csharp
[SqlTemplate(@"
    SELECT u.*, p.title
    FROM {{table}} u
    {{join --type inner --table posts p --on u.id=p.user_id}}
")]
Task<List<UserWithPosts>> GetUsersWithPostsAsync();

// 生成: INNER JOIN posts p ON u.id = p.user_id
```

**选项**:
- `--type <inner|left|right|full>` - 连接类型
- `--table <tableName>` - 连接的表
- `--on <condition>` - 连接条件

---

### 分组和聚合

#### `{{groupby}}` - 分组

```csharp
[SqlTemplate("SELECT category, COUNT(*) FROM {{table}} {{groupby category}}")]
Task<List<CategoryCount>> GetCategoryStatsAsync();

// 生成: GROUP BY category
```

#### `{{having}}` - 分组过滤

```csharp
[SqlTemplate(@"
    SELECT category, COUNT(*) as cnt
    FROM {{table}}
    {{groupby category}}
    {{having --condition 'COUNT(*) > @minCount'}}
")]
Task<List<CategoryCount>> GetPopularCategoriesAsync(int minCount);

// 生成: HAVING COUNT(*) > @minCount
```

---

### 聚合函数

所有聚合函数都支持`--column`选项：

```csharp
// COUNT
{{count --column id}} // COUNT(id)
{{count}} // COUNT(*)

// SUM
{{sum --column amount}} // SUM(amount)

// AVG
{{avg --column price}} // AVG(price)

// MAX/MIN
{{max --column score}} // MAX(score)
{{min --column created_at}} // MIN(created_at)
```

---

### 条件操作符

#### `{{in}}` / `{{not_in}}` - IN查询

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status {{in --column status}}")]
Task<List<User>> GetByStatusAsync(List<string> status);

// 运行时展开: WHERE status IN (@status0, @status1, @status2)
```

#### `{{between}}` - 范围查询

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE created_at {{between --column created_at}}")]
Task<List<User>> GetByDateRangeAsync(DateTime start, DateTime end);

// 生成: WHERE created_at BETWEEN @start AND @end
```

#### `{{like}}` - 模糊查询

```csharp
// 包含
{{like --column name --mode contains}} // name LIKE '%' || @name || '%'

// 开始于
{{like --column name --mode startswith}} // name LIKE @name || '%'

// 结束于
{{like --column name --mode endswith}} // name LIKE '%' || @name
```

---

### 字符串函数

#### `{{concat}}` - 字符串连接

```csharp
[SqlTemplate("SELECT {{concat --columns first_name last_name --separator ' '}} AS full_name FROM {{table}}")]
Task<List<string>> GetFullNamesAsync();

// 生成: CONCAT_WS(' ', first_name, last_name)
```

#### `{{substring}}` - 子字符串

```csharp
{{substring --column email --start 1 --length 10}}
// 生成: SUBSTRING(email, 1, 10)
```

#### `{{upper}}` / `{{lower}}` / `{{trim}}` - 大小写和修剪

```csharp
{{upper --column name}} // UPPER(name)
{{lower --column email}} // LOWER(email)
{{trim --column description}} // TRIM(description)
```

#### `{{group_concat}}` - 分组连接

```csharp
[SqlTemplate(@"
    SELECT user_id, {{group_concat --column tag --separator ','}} AS tags
    FROM user_tags
    GROUP BY user_id
")]
Task<List<UserTags>> GetUserTagsAsync();

// SQL Server: STRING_AGG(tag, ',')
// MySQL: GROUP_CONCAT(tag SEPARATOR ',')
// PostgreSQL: STRING_AGG(tag, ',')
```

---

### 数学函数

```csharp
{{round --column price --precision 2}} // ROUND(price, 2)
{{abs --column balance}} // ABS(balance)
{{ceiling --column value}} // CEILING(value)
{{floor --column value}} // FLOOR(value)
{{power --column base --exponent 2}} // POWER(base, 2)
{{sqrt --column value}} // SQRT(value)
{{mod --column value --divisor 10}} // value % 10
```

---

### 日期时间函数

```csharp
{{today}} // CURRENT_DATE
{{week --column date}} // WEEK(date)
{{month --column date}} // MONTH(date)
{{year --column date}} // YEAR(date)

// 日期运算（自动适配数据库）
{{date_add --column created_at --days 7}}
// MySQL: DATE_ADD(created_at, INTERVAL 7 DAY)
// PostgreSQL: created_at + INTERVAL '7 days'
// SQL Server: DATEADD(day, 7, created_at)

{{date_diff --column1 end_date --column2 start_date --unit days}}
// 计算日期差异
```

---

### 条件表达式

#### `{{case}}` - CASE WHEN

```csharp
[SqlTemplate(@"
    SELECT id, name,
    {{case --when 'status=1' --then 'Active' --when 'status=0' --then 'Inactive' --else 'Unknown'}} AS status_text
    FROM {{table}}
")]
Task<List<User>> GetUsersWithStatusAsync();

// 生成: CASE WHEN status=1 THEN 'Active' WHEN status=0 THEN 'Inactive' ELSE 'Unknown' END
```

#### `{{coalesce}}` - 空值合并

```csharp
{{coalesce --columns email phone address --default 'N/A'}}
// 生成: COALESCE(email, phone, address, 'N/A')
```

#### `{{ifnull}}` - 空值替换

```csharp
{{ifnull --column nickname --default name}}
// MySQL: IFNULL(nickname, name)
// SQL Server: ISNULL(nickname, name)
// PostgreSQL: COALESCE(nickname, name)
```

---

### 窗口函数

```csharp
// 行号
{{row_number --orderby created_at}}
// ROW_NUMBER() OVER (ORDER BY created_at)

// 排名
{{rank --orderby score --desc}}
// RANK() OVER (ORDER BY score DESC)

// 密集排名
{{dense_rank --orderby score --desc}}
// DENSE_RANK() OVER (ORDER BY score DESC)

// LAG/LEAD
{{lag --column price --offset 1 --orderby date}}
// LAG(price, 1) OVER (ORDER BY date)

{{lead --column price --offset 1 --orderby date}}
// LEAD(price, 1) OVER (ORDER BY date)
```

---

### JSON操作

```csharp
// 提取JSON字段（自动适配数据库）
{{json_extract --column metadata --path $.userId}}
// SQL Server: JSON_VALUE(metadata, '$.userId')
// PostgreSQL: metadata->>'$.userId'
// MySQL: JSON_EXTRACT(metadata, '$.userId')

// JSON数组
{{json_array --values @ids}}
// 生成JSON数组

// JSON对象
{{json_object --pairs 'key1:value1,key2:value2'}}
// 生成JSON对象
```

---

### 类型转换

```csharp
{{cast --column id --as VARCHAR}} // CAST(id AS VARCHAR)
{{convert --column date --to DATE}} // 自动适配数据库的转换语法
```

---

### 批量操作

#### `{{batch_values}}` - 批量插入

```csharp
[SqlTemplate("INSERT INTO {{table}} (name, email) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 运行时展开:
// INSERT INTO users (name, email) VALUES
// (@Name0, @Email0), (@Name1, @Email1), ...
```

#### `{{upsert}}` - 插入或更新

```csharp
[SqlTemplate("{{upsert --conflict Id}}")]
Task<int> UpsertAsync(User user);

// PostgreSQL: INSERT ... ON CONFLICT (id) DO UPDATE SET ...
// MySQL: INSERT ... ON DUPLICATE KEY UPDATE ...
// SQLite: INSERT OR REPLACE INTO ...
// SQL Server: MERGE ... (待实现)
```

---

### 查询优化

```csharp
{{distinct --column category}} // DISTINCT category
{{top --count 10}} // SQL Server: TOP 10
{{offset --value 20}} // OFFSET 20 ROWS
{{union --type all}} // UNION ALL
```

---

### 子查询

```csharp
{{exists --query 'SELECT 1 FROM orders WHERE orders.user_id = users.id'}}
// EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)

{{subquery --query 'SELECT AVG(price) FROM products'}}
// (SELECT AVG(price) FROM products)
```

---

## 🗄️ 方言特定占位符

这些占位符会根据数据库自动生成正确的语法：

### `{{bool_true}}` / `{{bool_false}}` - 布尔值

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
Task<List<User>> GetActiveAsync();

// SQLite: 1 / 0
// SQL Server: 1 / 0
// PostgreSQL: TRUE / FALSE
// MySQL: TRUE / FALSE
```

### `{{current_timestamp}}` - 当前时间戳

```csharp
[SqlTemplate("INSERT INTO {{table}} (name, created_at) VALUES (@name, {{current_timestamp}})")]
Task<int> InsertAsync(string name);

// SQLite/PostgreSQL/MySQL: CURRENT_TIMESTAMP
// SQL Server: GETDATE()
// Oracle: SYSTIMESTAMP
```

---

## 🚨 动态占位符 (高级)

> ⚠️ **安全警告**: 动态占位符绕过参数化查询，存在SQL注入风险！

### 语法: `{{@paramName}}`

动态占位符允许运行时指定SQL片段，必须标记`[DynamicSql]`特性。

### 类型1: 标识符（表名/列名）

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{@tableName}} WHERE id = @id")]
Task<User?> GetFromTableAsync([DynamicSql] string tableName, int id);

// 验证规则：只允许字母、数字、下划线，1-128字符
```

### 类型2: SQL片段

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{@whereClause}}")]
Task<List<User>> QueryAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);

// 验证规则：禁止DDL、EXEC、注释，1-4096字符
```

### 类型3: 表名部分

```csharp
[SqlTemplate("SELECT {{columns}} FROM logs_{{@suffix}}")]
Task<List<Log>> GetLogsAsync([DynamicSql(Type = DynamicSqlType.TablePart)] string suffix);

// 验证规则：只允许字母和数字，1-64字符
```

### 最佳实践

```csharp
// ✅ 使用白名单验证
var allowedTables = new[] { "users", "admin_users" };
if (!allowedTables.Contains(tableName))
    throw new ArgumentException("Invalid table");

// ✅ 使用硬编码常量
var whereClause = "age > 18 AND status = 'active'";

// ❌ 永远不要直接使用用户输入
var whereClause = Request.Query["filter"]; // 危险！
```

**详见**: [动态占位符完整指南](PLACEHOLDERS.md#-动态占位符-前缀---高级功能)

---

## 💡 最佳实践

### 何时使用占位符？

| 场景 | 推荐方案 | 原因 |
|------|---------|------|
| **列名列表** | ✅ `{{columns}}` | 自动生成，类型安全 |
| **表名** | ✅ `{{table}}` | 自动转换snake_case |
| **SET子句** | ✅ `{{set}}` | 自动生成复杂赋值 |
| **排序** | ✅ `{{orderby}}` | 支持选项，清晰 |
| **WHERE条件** | ⚠️ 直接写SQL | 更直观，更灵活 |
| **聚合函数** | ⚠️ 直接写SQL | 比占位符更短 |
| **多数据库适配** | ✅ 使用占位符 | 自动生成正确语法 |

### 占位符 vs 直接写SQL

```csharp
// ✅ 使用占位符 - 自动生成复杂内容
[SqlTemplate("SELECT {{columns --exclude Password}} FROM {{table}} {{orderby created_at --desc}}")]

// ✅ 直接写SQL - 简单清晰的内容
[SqlTemplate("SELECT * FROM users WHERE age > @minAge AND is_active = {{bool_true}}")]

// ❌ 过度使用占位符 - 反而更复杂
[SqlTemplate("{{select}} {{from}} {{where}}")]
```

### 核心原则

1. **智能占位符**: 用于自动生成复杂内容（列名、SET子句）
2. **直接写SQL**: 用于简单清晰的内容（WHERE、聚合函数）
3. **只在必要时使用**: 不要为了用占位符而用占位符

---

## 📊 占位符分类总结

### 核心占位符 (7个) - 必会
`table` · `columns` · `values` · `set` · `where` · `orderby` · `limit`

### 常用扩展 (10个)
`join` · `groupby` · `having` · `in` · `like` · `between` · `count` · `sum` · `avg` · `max` · `min`

### 字符串操作 (8个)
`concat` · `substring` · `upper` · `lower` · `trim` · `group_concat` · `replace` · `length`

### 数学运算 (7个)
`round` · `abs` · `ceiling` · `floor` · `power` · `sqrt` · `mod`

### 日期时间 (6个)
`today` · `week` · `month` · `year` · `date_add` · `date_diff` · `current_timestamp`

### 条件表达式 (3个)
`case` · `coalesce` · `ifnull`

### 窗口函数 (5个)
`row_number` · `rank` · `dense_rank` · `lag` · `lead`

### JSON操作 (3个)
`json_extract` · `json_array` · `json_object`

### 批量操作 (3个)
`batch_values` · `batch_insert` · `upsert`

### 其他 (10个)
`distinct` · `union` · `top` · `offset` · `cast` · `convert` · `exists` · `subquery` · `page` · `pagination`

### 方言特定 (2个)
`bool_true` · `bool_false`

---

## 🔗 相关文档

- [📋 快速开始指南](QUICK_START_GUIDE.md)
- [📘 占位符详细教程](PLACEHOLDERS.md)
- [💡 最佳实践](BEST_PRACTICES.md)
- [🌐 多数据库支持](UNIFIED_DIALECT_USAGE_GUIDE.md)
- [🚀 完整示例](../samples/)

---

## 📝 更新日志

### v0.5.1 (2025-11-02)
- ✅ 新增 `{{join}}` 占位符
- ✅ 新增 `{{in}}` 自动展开支持
- ✅ 新增 `{{groupby}}` 和 `{{having}}` 占位符
- ✅ 完善多数据库方言支持
- ✅ 优化占位符参数语法（支持 `--option` 格式）

### v0.5.0 (2025-10-26)
- ✅ 初始版本发布
- ✅ 核心7个占位符
- ✅ 50+ 扩展占位符
- ✅ 支持4种数据库（SQLite, PostgreSQL, MySQL, SQL Server）

---

**💬 遇到问题？** [提交Issue](https://github.com/Cricle/Sqlx/issues) | [查看示例](../samples/) | [加入讨论](https://github.com/Cricle/Sqlx/discussions)

