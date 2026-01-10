# Sqlx AI 助手完全指南

> **目标读者**: AI 助手（如 GitHub Copilot、ChatGPT、Claude 等）
> **目的**: 让 AI 完全理解 Sqlx 的所有功能、使用方式、注意事项和最佳实践

---

## 📋 目录

1. [核心概念](#核心概念)
2. [三大核心组件](#三大核心组件)
3. [完整功能清单](#完整功能清单)
4. [代码模式](#代码模式)
5. [重要注意事项](#重要注意事项)
6. [完整示例](#完整示例)
7. [性能优化](#性能优化)
8. [调试技巧](#调试技巧)

---

## 🎯 核心概念

### Sqlx 是什么？

Sqlx 是一个**编译时源代码生成器**，用于生成高性能、类型安全的数据访问代码。

**核心特点**:
- ✅ **编译时生成** - 零运行时开销，接近 ADO.NET 性能
- ✅ **类型安全** - 编译时验证 SQL 和参数
- ✅ **占位符系统** - 70+ 占位符自动生成复杂 SQL
- ✅ **多数据库** - 一套代码支持 4 种数据库（SQLite、PostgreSQL、MySQL、SQL Server）
- ✅ **零配置** - 无需 DbContext、无需映射配置

### 工作原理

```
用户代码 (接口 + 属性)
    ↓
源代码生成器 (编译时)
    ↓
生成的实现代码 (partial class)
    ↓
编译到程序集
```


---

## 🏗️ 三大核心组件

### 1. 特性 (Attributes)

#### `[SqlDefine]` - 定义数据库类型

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]    // SQLite
[SqlDefine(SqlDefineTypes.PostgreSql)] // PostgreSQL
[SqlDefine(SqlDefineTypes.MySql)]      // MySQL
[SqlDefine(SqlDefineTypes.SqlServer)]  // SQL Server
```

#### `[SqlTemplate]` - 定义 SQL 模板

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

#### `[RepositoryFor]` - 标记仓储实现

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 或指定方言和表名（统一方言架构）
[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "users")]
public partial class PostgreSQLUserRepository(DbConnection connection) : IUserRepository { }
```

#### `[TableName]` - 指定表名

```csharp
[TableName("users")]
public class User { ... }
```

#### `[ReturnInsertedId]` - 返回插入的 ID

```csharp
[SqlTemplate("INSERT INTO {{table}} (name) VALUES (@name)")]
[ReturnInsertedId]
Task<long> InsertAsync(string name);
```


### 2. 占位符系统

#### 核心占位符（必会）

| 占位符 | 作用 | 示例 |
|--------|------|------|
| `{{table}}` | 表名 | `users` → `"users"` (PostgreSQL) / `` `users` `` (MySQL) |
| `{{columns}}` | 列名列表 | `id, name, email, age` |
| `{{columns --exclude Id}}` | 排除列 | `name, email, age` |
| `{{columns --only Id Name}}` | 只包含列 | `id, name` |
| `{{values}}` | 值占位符 | `@Name, @Email, @Age` |
| `{{set}}` | SET 子句 | `name=@Name, email=@Email` |
| `{{set --exclude Id}}` | SET 排除列 | `name=@Name, email=@Email` |
| `{{orderby col}}` | 排序 | `ORDER BY col` |
| `{{orderby col --desc}}` | 降序 | `ORDER BY col DESC` |
| `{{orderby col --asc}}` | 升序 | `ORDER BY col ASC` |

#### 数据库方言占位符

| 占位符 | SQLite | PostgreSQL | MySQL | SQL Server |
|--------|--------|-----------|-------|------------|
| `{{bool_true}}` | `1` | `true` | `1` | `1` |
| `{{bool_false}}` | `0` | `false` | `0` | `0` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `NOW()` | `GETDATE()` |
| `{{returning_id}}` | (empty) | `RETURNING id` | (empty) | `OUTPUT INSERTED.id` |

#### 分页占位符

| 占位符 | 作用 | 示例 |
|--------|------|------|
| `{{limit}}` | LIMIT 子句 | `LIMIT @limit` |
| `{{offset}}` | OFFSET 子句 | `OFFSET @offset` |
| `{{limit --param pageSize}}` | 动态参数 | `LIMIT @pageSize` |
| `{{offset --param skip}}` | 动态参数 | `OFFSET @skip` |


### 3. 实体类

```csharp
// 推荐：使用 Record 类型
[TableName("users")]
public record User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }  // 可空字段
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 📚 完整功能清单

### CRUD 操作

#### 查询（SELECT）

```csharp
// 1. 查询所有
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 2. 根据 ID 查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// 3. 条件查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND is_active = @isActive")]
Task<List<User>> SearchAsync(int minAge, bool isActive);

// 4. 排序查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
Task<List<User>> GetRecentAsync();

// 5. 分页查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby id}} {{limit --param pageSize}} {{offset --param skip}}")]
Task<List<User>> GetPagedAsync(int pageSize, int skip);

// 6. 只查询部分列
[SqlTemplate("SELECT {{columns --only Id Name Email}} FROM {{table}}")]
Task<List<User>> GetBasicInfoAsync();

// 7. 排除敏感列
[SqlTemplate("SELECT {{columns --exclude Password Salt}} FROM {{table}}")]
Task<List<User>> GetPublicInfoAsync();
```


#### 插入（INSERT）

```csharp
// 1. 基本插入
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> InsertAsync(User user);

// 2. 插入并返回 ID
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
[ReturnInsertedId]
Task<long> InsertAndGetIdAsync(User user);

// 3. 插入指定字段
[SqlTemplate("INSERT INTO {{table}} (name, email) VALUES (@name, @email)")]
[ReturnInsertedId]
Task<long> InsertBasicAsync(string name, string email);

// 4. 插入带默认值
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id CreatedAt}}) VALUES ({{values}})")]
[ReturnInsertedId]
Task<long> InsertWithDefaultsAsync(User user);
```

#### 更新（UPDATE）

```csharp
// 1. 更新所有字段（排除 ID）
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// 2. 更新所有字段（排除 ID 和 CreatedAt）
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// 3. 只更新指定字段
[SqlTemplate("UPDATE {{table}} SET {{set --only Name Email}} WHERE id = @id")]
Task<int> UpdateBasicInfoAsync(User user);

// 4. 更新单个字段
[SqlTemplate("UPDATE {{table}} SET is_active = @isActive WHERE id = @id")]
Task<int> UpdateStatusAsync(long id, bool isActive);

// 5. 批量更新
[SqlTemplate("UPDATE {{table}} SET is_active = @isActive WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> UpdateStatusBatchAsync(string idsJson, bool isActive);
```


#### 删除（DELETE）

```csharp
// 1. 根据 ID 删除
[SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
Task<int> DeleteAsync(long id);

// 2. 条件删除
[SqlTemplate("DELETE FROM {{table}} WHERE is_active = @isActive")]
Task<int> DeleteInactiveAsync(bool isActive);

// 3. 批量删除
[SqlTemplate("DELETE FROM {{table}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> DeleteBatchAsync(string idsJson);
```

#### 聚合查询

```csharp
// 1. 计数
[SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
Task<long> CountAsync();

// 2. 条件计数
[SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_active = @isActive")]
Task<long> CountActiveAsync(bool isActive);

// 3. 求和
[SqlTemplate("SELECT SUM(amount) FROM {{table}} WHERE user_id = @userId")]
Task<decimal> GetTotalAmountAsync(long userId);

// 4. 平均值
[SqlTemplate("SELECT AVG(age) FROM {{table}}")]
Task<double> GetAverageAgeAsync();

// 5. 最大/最小值
[SqlTemplate("SELECT MAX(created_at) FROM {{table}}")]
Task<DateTime> GetLatestDateAsync();

// 6. 存在性检查
[SqlTemplate("SELECT EXISTS(SELECT 1 FROM {{table}} WHERE email = @email)")]
Task<bool> ExistsAsync(string email);
```


---

## 🎨 代码模式

### 模式 1: 基础 CRUD 仓储

```csharp
// 1. 定义实体
[TableName("users")]
public record User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2. 定义接口
[SqlDefine(SqlDefineTypes.SQLite)]
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(User user);

    [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}

// 3. 实现类（源生成器自动生成方法）
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```


### 模式 2: 统一方言架构（多数据库）

```csharp
// 1. 定义统一接口（使用占位符）
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}}) {{returning_id}}")]
    Task<long> InsertAsync(User user);

    [SqlTemplate("UPDATE {{table}} SET is_active = {{bool_true}} WHERE id = @id")]
    Task<int> ActivateAsync(long id);
}

// 2. SQLite 实现
[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.SQLite, TableName = "users")]
public partial class SQLiteUserRepository(DbConnection connection) : IUserRepository { }

// 3. PostgreSQL 实现
[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "users")]
public partial class PostgreSQLUserRepository(DbConnection connection) : IUserRepository { }

// 4. MySQL 实现
[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.MySql, TableName = "users")]
public partial class MySQLUserRepository(DbConnection connection) : IUserRepository { }

// 5. SQL Server 实现
[RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.SqlServer, TableName = "users")]
public partial class SqlServerUserRepository(DbConnection connection) : IUserRepository { }
```

**生成的 SQL 对比**:

| 数据库 | `{{table}}` | `{{bool_true}}` | `{{returning_id}}` |
|--------|------------|----------------|-------------------|
| SQLite | `"users"` | `1` | (empty) |
| PostgreSQL | `"users"` | `true` | `RETURNING id` |
| MySQL | `` `users` `` | `1` | (empty) |
| SQL Server | `[users]` | `1` | `OUTPUT INSERTED.id` |


### 模式 3: 复杂查询

```csharp
public interface IUserRepository
{
    // 多条件查询
    [SqlTemplate(@"
        SELECT {{columns}}
        FROM {{table}}
        WHERE age >= @minAge
          AND age <= @maxAge
          AND is_active = @isActive
        {{orderby created_at --desc}}
        {{limit --param pageSize}}
        {{offset --param skip}}
    ")]
    Task<List<User>> SearchAsync(int minAge, int maxAge, bool isActive, int pageSize, int skip);

    // OR 条件
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @query OR email LIKE @query")]
    Task<List<User>> SearchByNameOrEmailAsync(string query);

    // NULL 检查
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email IS NOT NULL")]
    Task<List<User>> GetUsersWithEmailAsync();

    // IN 查询（SQLite）
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<List<User>> GetByIdsAsync(string idsJson);

    // JOIN 查询
    [SqlTemplate(@"
        SELECT u.{{columns}}, o.id as order_id, o.amount
        FROM {{table}} u
        INNER JOIN orders o ON u.id = o.user_id
        WHERE u.id = @userId
    ")]
    Task<List<UserWithOrders>> GetUserWithOrdersAsync(long userId);
}
```


### 模式 4: 批量操作

```csharp
public interface IUserRepository
{
    // 批量插入（循环调用）
    async Task<int> InsertManyAsync(List<User> users)
    {
        var count = 0;
        foreach (var user in users)
        {
            await InsertAsync(user);
            count++;
        }
        return count;
    }

    // 批量更新（使用 JSON 数组）
    [SqlTemplate("UPDATE {{table}} SET is_active = @isActive WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdateStatusBatchAsync(string idsJson, bool isActive);

    // 批量删除
    [SqlTemplate("DELETE FROM {{table}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> DeleteBatchAsync(string idsJson);
}

// 使用示例
var ids = new[] { 1L, 2L, 3L };
var idsJson = JsonSerializer.Serialize(ids);
await repo.UpdateStatusBatchAsync(idsJson, true);
```

### 模式 5: 事务支持

```csharp
// 使用标准 ADO.NET 事务
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

await using var transaction = await connection.BeginTransactionAsync();
try
{
    var repo = new UserRepository(connection);

    var userId = await repo.InsertAsync(new User { Name = "Alice" });
    await repo.UpdateAsync(new User { Id = userId, Name = "Alice Updated" });

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```


---

## ⚠️ 重要注意事项

### ✅ 正确做法

#### 1. 使用占位符生成复杂内容

```csharp
// ✅ 正确：使用 {{columns}} 自动生成列名
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// ✅ 正确：使用 {{set}} 自动生成 SET 子句
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// ✅ 正确：使用 {{values}} 自动生成值占位符
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> InsertAsync(User user);
```

#### 2. 直接写简单的 SQL

```csharp
// ✅ 正确：WHERE 条件直接写
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// ✅ 正确：聚合函数直接写
[SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
Task<long> CountAsync();

// ✅ 正确：INSERT/UPDATE/DELETE 关键字直接写
[SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
Task<int> DeleteAsync(long id);
```

#### 3. 参数化查询

```csharp
// ✅ 正确：使用 @参数
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
Task<User?> GetByNameAsync(string name);

// ✅ 正确：多个参数
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND age <= @maxAge")]
Task<List<User>> GetByAgeRangeAsync(int minAge, int maxAge);
```


#### 4. 排除字段

```csharp
// ✅ 正确：插入时排除自增 ID
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> InsertAsync(User user);

// ✅ 正确：更新时排除不可变字段
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// ✅ 正确：只更新指定字段
[SqlTemplate("UPDATE {{table}} SET {{set --only Name Email}} WHERE id = @id")]
Task<int> UpdateBasicInfoAsync(User user);
```

#### 5. 使用 Record 类型

```csharp
// ✅ 正确：使用 Record
[TableName("users")]
public record User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}
```

#### 6. 异步方法

```csharp
// ✅ 正确：所有方法都是异步的
Task<List<User>> GetAllAsync();
Task<User?> GetByIdAsync(long id);
Task<int> InsertAsync(User user);
```


### ❌ 错误做法

#### 1. 过度使用占位符

```csharp
// ❌ 错误：WHERE 条件不需要占位符
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where id=@id}}")]
Task<User?> GetByIdAsync(long id);

// ✅ 正确：直接写 WHERE
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

#### 2. 字符串拼接（SQL 注入风险）

```csharp
// ❌ 错误：字符串拼接
[SqlTemplate($"SELECT * FROM users WHERE name = '{name}'")]
Task<User?> GetByNameAsync(string name);

// ✅ 正确：参数化查询
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
Task<User?> GetByNameAsync(string name);
```

#### 3. 忘记排除自增 ID

```csharp
// ❌ 错误：包含 Id 字段
[SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})")]
Task<int> InsertAsync(User user);

// ✅ 正确：排除 Id
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> InsertAsync(User user);
```

#### 4. 更新不可变字段

```csharp
// ❌ 错误：会更新 CreatedAt
[SqlTemplate("UPDATE {{table}} SET {{set}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// ✅ 正确：排除不可变字段
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
Task<int> UpdateAsync(User user);
```


#### 5. 硬编码表名

```csharp
// ❌ 错误：硬编码表名
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// ✅ 正确：使用 {{table}} 占位符
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

#### 6. 硬编码布尔值

```csharp
// ❌ 错误：硬编码 1（不同数据库可能不同）
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = 1")]
Task<List<User>> GetActiveAsync();

// ✅ 正确：使用 {{bool_true}} 占位符
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
Task<List<User>> GetActiveAsync();
```

#### 7. 同步方法

```csharp
// ❌ 错误：同步方法
List<User> GetAll();

// ✅ 正确：异步方法
Task<List<User>> GetAllAsync();
```


---

## 📖 完整示例

### 示例 1: 用户管理系统

```csharp
// 1. 实体类
[TableName("users")]
public record User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// 2. 仓储接口
[SqlDefine(SqlDefineTypes.SQLite)]
public interface IUserRepository
{
    // 查询所有用户
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<User>> GetAllAsync();

    // 根据 ID 查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    // 根据邮箱查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email);

    // 搜索用户（排除密码）
    [SqlTemplate("SELECT {{columns --exclude Password}} FROM {{table}} WHERE name LIKE @query OR email LIKE @query")]
    Task<List<User>> SearchAsync(string query);

    // 分页查询活跃用户
    [SqlTemplate(@"
        SELECT {{columns --exclude Password}}
        FROM {{table}}
        WHERE is_active = {{bool_true}}
        {{orderby created_at --desc}}
        {{limit --param pageSize}}
        {{offset --param skip}}
    ")]
    Task<List<User>> GetActivePagedAsync(int pageSize, int skip);

    // 创建用户
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(User user);

    // 更新用户（排除 Id 和 CreatedAt）
    [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    // 激活用户
    [SqlTemplate("UPDATE {{table}} SET is_active = {{bool_true}}, updated_at = {{current_timestamp}} WHERE id = @id")]
    Task<int> ActivateAsync(long id);

    // 删除用户
    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    // 统计
    [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
    Task<long> CountAsync();

    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<long> CountActiveAsync();

    // 检查邮箱是否存在
    [SqlTemplate("SELECT EXISTS(SELECT 1 FROM {{table}} WHERE email = @email)")]
    Task<bool> EmailExistsAsync(string email);
}

// 3. 实现类
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```


### 示例 2: 电商订单系统

```csharp
// 1. 实体类
[TableName("orders")]
public record Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// 2. 仓储接口
[SqlDefine(SqlDefineTypes.SQLite)]
public interface IOrderRepository
{
    // 查询用户的所有订单
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId {{orderby created_at --desc}}")]
    Task<List<Order>> GetByUserIdAsync(long userId);

    // 查询待处理订单
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status = @status {{orderby created_at}}")]
    Task<List<Order>> GetByStatusAsync(string status);

    // 查询用户的订单总额
    [SqlTemplate("SELECT SUM(amount) FROM {{table}} WHERE user_id = @userId AND status = @status")]
    Task<decimal> GetTotalAmountAsync(long userId, string status);

    // 创建订单
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id CompletedAt}}) VALUES ({{values}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(Order order);

    // 更新订单状态
    [SqlTemplate("UPDATE {{table}} SET status = @status, completed_at = @completedAt WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, DateTime? completedAt);

    // 取消订单
    [SqlTemplate("UPDATE {{table}} SET status = 'cancelled' WHERE id = @id AND status = 'pending'")]
    Task<int> CancelAsync(long id);

    // 统计用户订单数
    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE user_id = @userId")]
    Task<long> CountByUserAsync(long userId);
}

// 3. 实现类
[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository(DbConnection connection) : IOrderRepository { }
```


### 示例 3: 博客系统（多数据库）

```csharp
// 1. 实体类
[TableName("posts")]
public record Post
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long AuthorId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

// 2. 统一接口（使用占位符）
public interface IPostRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Post?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_published = {{bool_true}} {{orderby published_at --desc}}")]
    Task<List<Post>> GetPublishedAsync();

    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id PublishedAt}}) VALUES ({{values}}) {{returning_id}}")]
    Task<long> InsertAsync(Post post);

    [SqlTemplate("UPDATE {{table}} SET is_published = {{bool_true}}, published_at = {{current_timestamp}} WHERE id = @id")]
    Task<int> PublishAsync(long id);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}

// 3. SQLite 实现
[RepositoryFor(typeof(IPostRepository), Dialect = SqlDefineTypes.SQLite, TableName = "posts")]
public partial class SQLitePostRepository(DbConnection connection) : IPostRepository { }

// 4. PostgreSQL 实现
[RepositoryFor(typeof(IPostRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "posts")]
public partial class PostgreSQLPostRepository(DbConnection connection) : IPostRepository { }

// 5. MySQL 实现
[RepositoryFor(typeof(IPostRepository), Dialect = SqlDefineTypes.MySql, TableName = "posts")]
public partial class MySQLPostRepository(DbConnection connection) : IPostRepository { }

// 6. SQL Server 实现
[RepositoryFor(typeof(IPostRepository), Dialect = SqlDefineTypes.SqlServer, TableName = "posts")]
public partial class SqlServerPostRepository(DbConnection connection) : IPostRepository { }
```

**生成的 SQL 对比**:

```sql
-- SQLite
INSERT INTO "posts" (...) VALUES (...)
-- (使用 last_insert_rowid() 获取 ID)

-- PostgreSQL
INSERT INTO "posts" (...) VALUES (...) RETURNING id

-- MySQL
INSERT INTO `posts` (...) VALUES (...)
-- (使用 LAST_INSERT_ID() 获取 ID)

-- SQL Server
INSERT INTO [posts] (...) OUTPUT INSERTED.id VALUES (...)
```


---

## 🚀 性能优化

### 1. 只查询需要的列

```csharp
// ❌ 不好：查询所有列
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// ✅ 更好：只查询需要的列
[SqlTemplate("SELECT {{columns --only Id Name Email}} FROM {{table}}")]
Task<List<User>> GetBasicInfoAsync();
```

### 2. 使用索引列进行查询

```csharp
// ✅ 好：使用索引列（id, email）
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
Task<User?> GetByEmailAsync(string email);
```

### 3. 批量操作

```csharp
// ❌ 不好：循环单条插入
foreach (var user in users)
{
    await repo.InsertAsync(user);
}

// ✅ 更好：使用批量操作
var idsJson = JsonSerializer.Serialize(ids);
await repo.UpdateStatusBatchAsync(idsJson, true);
```

### 4. 连接管理

```csharp
// ✅ 好：使用 using 自动释放连接
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

var repo = new UserRepository(connection);
var users = await repo.GetAllAsync();
```


---

## 🔍 调试技巧

### 1. 查看生成的代码

生成的代码位于：`obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.Generator.CSharpGenerator/`

```
obj/Debug/net9.0/generated/
└── Sqlx.Generator/
    └── Sqlx.Generator.CSharpGenerator/
        └── UserRepository.Repository.g.cs  ← 查看这个文件
```

### 2. 查看生成的 SQL

在生成的代码中，可以看到实际的 SQL：

```csharp
public async Task<User?> GetByIdAsync(long id)
{
    var __sql__ = @"SELECT id, name, email, age, is_active, created_at FROM ""users"" WHERE id = @id";
    // ... 执行逻辑
}
```

### 3. 编译错误

如果遇到编译错误，检查：
- ✅ 是否标记了 `[RepositoryFor]` 特性
- ✅ 是否使用了 `partial` 关键字
- ✅ SQL 模板是否正确
- ✅ 参数名是否匹配

### 4. 运行时错误

如果遇到运行时错误，检查：
- ✅ 数据库连接是否正确
- ✅ 表名是否存在
- ✅ 列名是否匹配
- ✅ 参数类型是否正确


---

## 📋 快速参考

### 核心特性

| 特性 | 用途 | 示例 |
|------|------|------|
| `[SqlDefine]` | 定义数据库类型 | `[SqlDefine(SqlDefineTypes.SQLite)]` |
| `[SqlTemplate]` | 定义 SQL 模板 | `[SqlTemplate("SELECT * FROM {{table}}")]` |
| `[RepositoryFor]` | 标记仓储实现 | `[RepositoryFor(typeof(IUserRepository))]` |
| `[TableName]` | 指定表名 | `[TableName("users")]` |
| `[ReturnInsertedId]` | 返回插入的 ID | `[ReturnInsertedId]` |

### 核心占位符

| 占位符 | 作用 |
|--------|------|
| `{{table}}` | 表名 |
| `{{columns}}` | 列名列表 |
| `{{columns --exclude Id}}` | 排除列 |
| `{{columns --only Id Name}}` | 只包含列 |
| `{{values}}` | 值占位符 |
| `{{set}}` | SET 子句 |
| `{{set --exclude Id}}` | SET 排除列 |
| `{{orderby col}}` | 排序 |
| `{{orderby col --desc}}` | 降序 |
| `{{limit}}` | LIMIT 子句 |
| `{{offset}}` | OFFSET 子句 |
| `{{bool_true}}` | 布尔 true |
| `{{bool_false}}` | 布尔 false |
| `{{current_timestamp}}` | 当前时间戳 |
| `{{returning_id}}` | RETURNING/OUTPUT 子句 |

### 数据库类型

| 类型 | 枚举值 |
|------|--------|
| SQLite | `SqlDefineTypes.SQLite` |
| PostgreSQL | `SqlDefineTypes.PostgreSql` |
| MySQL | `SqlDefineTypes.MySql` |
| SQL Server | `SqlDefineTypes.SqlServer` |


---

## 🎓 学习路径

### 第 1 步：基础 CRUD（5 分钟）

1. 定义实体类（使用 `[TableName]`）
2. 定义接口（使用 `[SqlTemplate]`）
3. 创建实现类（使用 `[RepositoryFor]`）
4. 使用仓储

### 第 2 步：占位符系统（10 分钟）

1. 学习核心占位符：`{{table}}`, `{{columns}}`, `{{values}}`, `{{set}}`, `{{orderby}}`
2. 学习排除选项：`--exclude`, `--only`
3. 学习方言占位符：`{{bool_true}}`, `{{current_timestamp}}`, `{{returning_id}}`

### 第 3 步：复杂查询（15 分钟）

1. 多条件查询（AND, OR）
2. 分页查询（`{{limit}}`, `{{offset}}`）
3. 聚合查询（COUNT, SUM, AVG）
4. JOIN 查询

### 第 4 步：多数据库支持（10 分钟）

1. 理解统一方言架构
2. 使用 `RepositoryFor` 的 `Dialect` 和 `TableName` 参数
3. 为每个数据库创建实现类

### 第 5 步：最佳实践（10 分钟）

1. 何时使用占位符，何时直接写 SQL
2. 参数化查询避免 SQL 注入
3. 排除字段的技巧
4. 性能优化建议

---

## 📚 相关文档

- [快速开始指南](docs/QUICK_START_GUIDE.md) - 5 分钟上手
- [占位符完整指南](docs/PLACEHOLDERS.md) - 70+ 占位符详解
- [API 参考](docs/API_REFERENCE.md) - 完整 API 文档
- [最佳实践](docs/BEST_PRACTICES.md) - 推荐用法
- [统一方言指南](docs/UNIFIED_DIALECT_USAGE_GUIDE.md) - 多数据库支持
- [TodoWebApi 示例](samples/TodoWebApi/) - 完整 Web API 示例

---

## 🎯 总结

### Sqlx 的核心优势

1. **编译时生成** - 零运行时开销，接近 ADO.NET 性能
2. **类型安全** - 编译时验证，减少运行时错误
3. **占位符系统** - 自动生成复杂 SQL，减少手写代码
4. **多数据库** - 一套代码支持 4 种数据库
5. **零配置** - 无需 DbContext、无需映射配置
6. **易学易用** - 5 个核心占位符即可上手

### 设计理念

- ✅ **智能占位符** - 用于自动生成复杂内容（列名、SET 子句等）
- ✅ **直接写 SQL** - 简单的内容（WHERE、聚合函数）直接写更清晰
- ✅ **类型安全** - 编译时验证，发现问题更早
- ✅ **性能优先** - 零运行时开销，接近原生性能

### 开始使用

1. 安装 NuGet 包：`dotnet add package Sqlx`
2. 定义实体和接口
3. 标记实现类
4. 开始使用！

---

<div align="center">

**Sqlx - 让数据访问回归简单，让性能接近极致！** 🚀

Made with ❤️ by the Sqlx Team

</div>
