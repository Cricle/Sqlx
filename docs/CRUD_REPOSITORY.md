# 通用CRUD接口 - ICrudRepository<TEntity, TKey>

## 📖 概述

`ICrudRepository<TEntity, TKey>`是Sqlx提供的通用CRUD（Create, Read, Update, Delete）接口，包含8个标准数据访问方法。继承此接口后，Sqlx会在编译时自动生成高性能的SQL和ADO.NET代码，让您无需编写重复的CRUD样板代码。

###  ✨ 核心优势

- ⚡ **零样板代码** - 8个标准方法自动生成，立即可用
- 🎯 **SQL最佳实践** - 明确列名（不用`SELECT *`）、参数化查询、主键索引
- 🚀 **接近原生性能** - 编译时生成代码，零反射，零动态分发
- 🔒 **类型安全** - 编译时检查，IDE智能提示
- 🛡️ **SQL注入防护** - 全部使用参数化查询

---

## 📋 包含的8个标准方法

| 方法 | 功能 | SQL示例 |
|-----|------|--------|
| `GetByIdAsync(id)` | 根据主键查询单个实体 | `SELECT id, name FROM users WHERE id = @id` |
| `GetAllAsync(limit, offset)` | 分页查询所有实体 | `SELECT id, name FROM users ORDER BY id LIMIT @limit OFFSET @offset` |
| `InsertAsync(entity)` | 插入新实体 | `INSERT INTO users (name, email) VALUES (@name, @email)` |
| `UpdateAsync(entity)` | 更新实体 | `UPDATE users SET name = @name WHERE id = @id` |
| `DeleteAsync(id)` | 根据主键删除实体 | `DELETE FROM users WHERE id = @id` |
| `CountAsync()` | 统计总数 | `SELECT COUNT(*) FROM users` |
| `ExistsAsync(id)` | 检查是否存在 | `SELECT CASE WHEN EXISTS(SELECT 1 FROM users WHERE id = @id) THEN 1 ELSE 0 END` |
| `BatchInsertAsync(entities)` | 批量插入（高性能） | `INSERT INTO users (name) VALUES (@name_0), (@name_1), (@name_2)` |

---

## 🚀 快速开始

### 1️⃣ 定义实体

```csharp
public class User
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLogin { get; init; }  // Nullable支持
}
```

### 2️⃣ 继承ICrudRepository

```csharp
using Sqlx;
using Sqlx.Annotations;

[RepositoryFor<User>]
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepository : ICrudRepository<User, int>
{
    public UserRepository(DbConnection connection) { }
}
```

### 3️⃣ 使用（8个方法自动生成）

```csharp
var repo = new UserRepository(connection);

// ✅ 查询
var user = await repo.GetByIdAsync(1);
var all = await repo.GetAllAsync(limit: 10, offset: 0);
var count = await repo.CountAsync();
var exists = await repo.ExistsAsync(1);

// ✅ 插入
var newUser = new User { Name = "张三", Email = "zhang@example.com", CreatedAt = DateTime.Now };
await repo.InsertAsync(newUser);

// ✅ 批量插入（高性能）
var users = new List<User> { /* ... */ };
await repo.BatchInsertAsync(users);

// ✅ 更新
var updated = user with { Name = "李四" };
await repo.UpdateAsync(updated);

// ✅ 删除
await repo.DeleteAsync(1);
```

---

## 🎯 SQL最佳实践

所有生成的SQL都遵循最佳实践：

### ✅ 明确列名（不使用SELECT *）

```sql
-- ✅ 好 - 明确列名
SELECT id, name, email, created_at, last_login FROM users WHERE id = @id

-- ❌ 差 - SELECT *
SELECT * FROM users WHERE id = @id
```

**为什么？**
- 性能更好（减少网络传输）
- 避免列顺序变化导致的bug
- 更好的查询优化器提示

### ✅ 参数化查询

所有值都通过参数传递，防止SQL注入：

```sql
-- ✅ 参数化
SELECT id, name FROM users WHERE id = @id

-- ❌ 字符串拼接（危险）
SELECT id, name FROM users WHERE id = ' + userId + '
```

### ✅ 主键索引

WHERE条件使用主键，确保最快速度：

```sql
SELECT id, name FROM users WHERE id = @id  -- 使用主键索引
UPDATE users SET name = @name WHERE id = @id  -- 使用主键索引
DELETE FROM users WHERE id = @id  -- 使用主键索引
```

### ✅ 批量操作优化

`BatchInsertAsync`使用单条INSERT语句 + 多行VALUES，比循环INSERT快10-50倍：

```sql
-- ✅ 批量插入（一次SQL）
INSERT INTO users (name, email) VALUES 
  (@name_0, @email_0),
  (@name_1, @email_1),
  (@name_2, @email_2);

-- ❌ 循环插入（三次SQL）
INSERT INTO users (name, email) VALUES (@name_0, @email_0);
INSERT INTO users (name, email) VALUES (@name_1, @email_1);
INSERT INTO users (name, email) VALUES (@name_2, @email_2);
```

---

## 🔧 扩展自定义方法

您可以继承`ICrudRepository`的同时添加业务特定的方法：

```csharp
public interface IUserRepository : ICrudRepository<User, int>
{
    // ✅ 从ICrudRepository继承8个标准方法
    
    // ✅ 添加业务特定方法
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email);
    
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE last_login IS NULL")]
    Task<List<User>> GetInactiveUsersAsync();
    
    [Sqlx("UPDATE {{table}} SET last_login = @now WHERE id = @id")]
    Task<int> UpdateLastLoginAsync(int id, DateTime now);
}

[RepositoryFor<IUserRepository>]
[TableName("users")]
public partial class UserRepository : IUserRepository
{
    public UserRepository(DbConnection connection) { }
}
```

---

## 🗂️ IReadOnlyRepository - 只读接口

如果您只需要查询功能（例如报表、数据展示），可以使用`IReadOnlyRepository<TEntity, TKey>`：

```csharp
[RepositoryFor<User>]
[TableName("users")]
public partial class UserQueryRepository : IReadOnlyRepository<User, int>
{
    public UserQueryRepository(DbConnection connection) { }
}

// 只包含4个只读方法：
// - GetByIdAsync(id)
// - GetAllAsync(limit, offset)
// - CountAsync()
// - ExistsAsync(id)
```

**适用场景**：
- 报表查询
- 数据展示
- CQRS模式（查询端）
- 只读副本数据库

---

## 📌 完整方法签名

### GetByIdAsync

```csharp
Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
```

根据主键获取单个实体，不存在时返回`null`。

**生成的SQL**：
```sql
SELECT id, name, email, created_at FROM users WHERE id = @id
```

### GetAllAsync

```csharp
Task<List<TEntity>> GetAllAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
```

分页查询所有实体，默认限制100条防止一次性加载过多数据。

**生成的SQL**：
```sql
SELECT id, name, email, created_at FROM users ORDER BY id LIMIT @limit OFFSET @offset
```

### InsertAsync

```csharp
Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
```

插入新实体，返回受影响的行数（成功时为1）。

**生成的SQL**：
```sql
INSERT INTO users (name, email, created_at) VALUES (@name, @email, @created_at)
```

**注意**：自动排除`Id`列（假设是自增主键）

### UpdateAsync

```csharp
Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
```

更新实体，返回受影响的行数（成功时为1，实体不存在时为0）。

**生成的SQL**：
```sql
UPDATE users SET name = @name, email = @email WHERE id = @id
```

**注意**：`Id`列不会被更新（主键不应被更改）

### DeleteAsync

```csharp
Task<int> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
```

根据主键删除实体，返回受影响的行数（成功时为1，实体不存在时为0）。

**生成的SQL**：
```sql
DELETE FROM users WHERE id = @id
```

⚠️ **物理删除，不可恢复**。如需软删除，请添加`IsDeleted`字段并自定义方法。

### CountAsync

```csharp
Task<int> CountAsync(CancellationToken cancellationToken = default)
```

获取实体总数。

**生成的SQL**：
```sql
SELECT COUNT(*) FROM users
```

### ExistsAsync

```csharp
Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
```

检查实体是否存在。

**生成的SQL**：
```sql
SELECT CASE WHEN EXISTS(SELECT 1 FROM users WHERE id = @id) THEN 1 ELSE 0 END
```

**注意**：使用`EXISTS`比`COUNT(*)`更高效

### BatchInsertAsync

```csharp
Task<int> BatchInsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default)
```

批量插入多个实体，返回受影响的行数。

**生成的SQL**：
```sql
INSERT INTO users (name, email) VALUES 
  (@name_0, @email_0),
  (@name_1, @email_1),
  (@name_2, @email_2)
```

**性能提示**：
- 比循环`InsertAsync`快10-50倍
- 注意数据库参数数量限制（SQL Server为2100个）
- 大批量时建议分批处理

---

## 🌐 多数据库支持

所有生成的SQL自动适配不同数据库方言：

```csharp
// SQLite
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepository : ICrudRepository<User, int> { }

// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class UserRepository : ICrudRepository<User, int> { }

// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
public partial class UserRepository : ICrudRepository<User, int> { }

// PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class UserRepository : ICrudRepository<User, int> { }
```

**自动处理**：
- 参数前缀（`@` / `:` / `?`）
- 列名引用（`` `column` `` / `[column]` / `"column"`）
- 分页语法（`LIMIT/OFFSET` / `OFFSET/FETCH`）
- 自增主键语法

---

## ⚠️ 注意事项

### 1. 主键约定

`ICrudRepository`假设实体有一个名为`Id`的主键属性：

```csharp
public class User
{
    public int Id { get; init; }  // ✅ 主键
    // ...
}
```

如果主键名称不同，请使用自定义接口而不是`ICrudRepository`。

### 2. InsertAsync不返回新ID

`InsertAsync`返回受影响的行数，不返回新插入记录的ID。如需获取ID，请使用数据库特定的方法：

```csharp
// SQLite
await repo.InsertAsync(user);
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT last_insert_rowid()";
var newId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);

// SQL Server
[Sqlx("INSERT INTO users (...) VALUES (...); SELECT SCOPE_IDENTITY()")]
Task<int> InsertAndGetIdAsync(User user);
```

### 3. 软删除

`DeleteAsync`是物理删除。如需软删除，请添加自定义方法：

```csharp
public interface IUserRepository : ICrudRepository<User, int>
{
    [Sqlx("UPDATE {{table}} SET is_deleted = 1, deleted_at = @now WHERE id = @id")]
    Task<int> SoftDeleteAsync(int id, DateTime now);
    
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE is_deleted = 0")]
    Task<List<User>> GetActiveUsersAsync();
}
```

---

## 📚 更多示例

完整示例请参考：
- [samples/TodoWebApi/Services/TodoService.cs](../samples/TodoWebApi/Services/TodoService.cs) - TodoRepository实现
- [src/Sqlx/ICrudRepository.cs](../src/Sqlx/ICrudRepository.cs) - 接口定义和文档

---

## 🎯 总结

`ICrudRepository<TEntity, TKey>`是Sqlx提供的零样板代码解决方案：

✅ **8个标准方法自动生成** - GetById, GetAll, Insert, Update, Delete, Count, Exists, BatchInsert  
✅ **SQL最佳实践** - 明确列名、参数化查询、主键索引  
✅ **高性能** - 编译时代码生成，零反射  
✅ **类型安全** - 编译时检查  
✅ **可扩展** - 继承后添加自定义方法  

**下一步**：
- 查看[API参考](API_REFERENCE.md)了解更多特性
- 查看[模板占位符](PLACEHOLDERS.md)学习高级SQL模板
- 查看[批量操作](ADVANCED_FEATURES.md#批量操作)优化性能

