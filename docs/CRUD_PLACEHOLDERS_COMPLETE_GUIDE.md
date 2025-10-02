# 🎯 Sqlx CRUD 操作完整占位符指南

## 📋 目录
- [CREATE - 创建操作](#create---创建操作)
- [READ - 查询操作](#read---查询操作)
- [UPDATE - 更新操作](#update---更新操作)
- [DELETE - 删除操作](#delete---删除操作)
- [高级CRUD场景](#高级crud场景)
- [完整示例](#完整示例)

---

## CREATE - 创建操作

### 1. 基础插入

```csharp
// 使用 {{insert}} + {{columns:auto}} + {{values:auto}}
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> CreateAsync(Todo todo);

// 生成SQL:
// INSERT INTO todos (title, description, is_completed, ...) 
// VALUES (@Title, @Description, @IsCompleted, ...)
```

### 2. 插入并返回ID（SQLite）

```csharp
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}}); SELECT last_insert_rowid()")]
Task<long> CreateAndReturnIdAsync(Todo todo);

// 生成SQL:
// INSERT INTO todos (...) VALUES (...); SELECT last_insert_rowid()
```

### 3. 插入并返回完整记录（PostgreSQL）

```csharp
[SqlTemplate(@"INSERT INTO todos (title, description, is_completed) 
               VALUES (@Title, @Description, @IsCompleted) 
               RETURNING *", 
             Dialect = SqlDefineTypes.PostgreSQL)]
Task<Todo> CreateAndReturnAsync(Todo todo);
```

### 4. 批量插入（使用参数）

```csharp
// 多次调用单条插入（推荐）
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> CreateAsync(Todo todo);

// 在服务层循环调用
public async Task<int> CreateBatchAsync(List<Todo> todos)
{
    int count = 0;
    foreach (var todo in todos)
        count += await CreateAsync(todo);
    return count;
}
```

### 5. 忽略重复（INSERT IGNORE - MySQL）

```csharp
[SqlTemplate("INSERT IGNORE INTO todos ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})",
             Dialect = SqlDefineTypes.MySQL)]
Task<int> CreateIgnoreDuplicateAsync(Todo todo);
```

---

## READ - 查询操作

### 1. 查询所有记录

```csharp
// 使用 {{columns:auto}} + {{table}}
[Sqlx("SELECT {{columns:auto}} FROM {{table}}")]
Task<List<Todo>> GetAllAsync();

// 生成SQL:
// SELECT id, title, description, ... FROM todos
```

### 2. 根据ID查询

```csharp
// 使用 {{where:id}}
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
Task<Todo?> GetByIdAsync(long id);

// 生成SQL:
// SELECT id, title, ... FROM todos WHERE id = @id
```

### 3. 条件查询

```csharp
// 使用 {{where:auto}} 自动推断
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:auto}}")]
Task<List<Todo>> GetByStatusAsync(bool isCompleted);

// 生成SQL:
// SELECT id, title, ... FROM todos WHERE is_completed = @isCompleted
```

### 4. 复杂条件查询

```csharp
// 手写条件
[SqlTemplate(@"SELECT id, title, description, is_completed 
               FROM todos 
               WHERE is_completed = @isCompleted 
                 AND priority >= @minPriority 
               ORDER BY created_at DESC",
             Dialect = SqlDefineTypes.SQLite)]
Task<List<Todo>> SearchAsync(bool isCompleted, int minPriority);
```

### 5. 分页查询

```csharp
// 使用 {{limit}} 占位符
[Sqlx("SELECT {{columns:auto}} FROM {{table}} {{orderby:id_desc}} {{limit:sqlite|offset=0|rows=20}}")]
Task<List<Todo>> GetPagedAsync();

// 使用 OFFSET 和 LIMIT（SQLite）
[SqlTemplate("SELECT * FROM todos ORDER BY id LIMIT @pageSize OFFSET @offset",
             Dialect = SqlDefineTypes.SQLite)]
Task<List<Todo>> GetPagedAsync(int offset, int pageSize);
```

### 6. 聚合查询

```csharp
// 使用 {{count:all}}
[Sqlx("SELECT {{count:all}} FROM {{table}}")]
Task<int> GetTotalCountAsync();

// 使用 {{count:all}} + WHERE
[Sqlx("SELECT {{count:all}} FROM {{table}} WHERE {{where:auto}}")]
Task<int> GetCountByStatusAsync(bool isCompleted);

// 使用其他聚合函数
[Sqlx("SELECT {{avg:priority}}, {{max:priority}}, {{min:priority}} FROM {{table}}")]
Task<Statistics> GetStatisticsAsync();
```

### 7. DISTINCT查询

```csharp
// 使用 {{distinct}}
[Sqlx("SELECT {{distinct:priority}} FROM {{table}}")]
Task<List<int>> GetDistinctPrioritiesAsync();
```

### 8. 分组查询

```csharp
// 使用 {{groupby}}
[SqlTemplate(@"SELECT priority, COUNT(*) as count 
               FROM todos 
               GROUP BY priority 
               ORDER BY priority",
             Dialect = SqlDefineTypes.SQLite)]
Task<List<PriorityCount>> GetCountByPriorityAsync();
```

---

## UPDATE - 更新操作

### 1. 基础更新

```csharp
// 使用 {{update}} + {{set:auto}} + {{where:id}}
[Sqlx("{{update}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}")]
Task<int> UpdateAsync(Todo todo);

// 生成SQL:
// UPDATE todos SET title = @Title, description = @Description, ... WHERE id = @Id
```

### 2. 部分字段更新

```csharp
// 明确指定要更新的字段
[SqlTemplate("UPDATE todos SET title = @title, updated_at = @updatedAt WHERE id = @id",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Update)]
Task<int> UpdateTitleAsync(long id, string title, DateTime updatedAt);
```

### 3. 条件批量更新

```csharp
// 使用 WHERE 条件
[SqlTemplate(@"UPDATE todos 
               SET priority = @newPriority, updated_at = datetime('now') 
               WHERE priority = @oldPriority AND is_completed = 0",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Update)]
Task<int> UpdatePriorityBatchAsync(int oldPriority, int newPriority);
```

### 4. 递增/递减操作

```csharp
[SqlTemplate(@"UPDATE todos 
               SET view_count = view_count + 1, updated_at = datetime('now') 
               WHERE id = @id",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Update)]
Task<int> IncrementViewCountAsync(long id);
```

### 5. 使用子查询更新

```csharp
[SqlTemplate(@"UPDATE todos 
               SET priority = (SELECT AVG(priority) FROM todos WHERE is_completed = 1)
               WHERE id = @id",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Update)]
Task<int> UpdateToAvgPriorityAsync(long id);
```

---

## DELETE - 删除操作

### 1. 根据ID删除

```csharp
// 使用 {{delete}} + {{where:id}}
[Sqlx("{{delete}} WHERE {{where:id}}")]
Task<int> DeleteAsync(long id);

// 生成SQL:
// DELETE FROM todos WHERE id = @id
```

### 2. 条件删除

```csharp
// 使用 {{where:auto}}
[Sqlx("{{delete}} WHERE {{where:auto}}")]
Task<int> DeleteByStatusAsync(bool isCompleted);

// 生成SQL:
// DELETE FROM todos WHERE is_completed = @isCompleted
```

### 3. 批量删除

```csharp
// 使用 IN 子句
[SqlTemplate("DELETE FROM todos WHERE id IN (SELECT value FROM json_each(@ids))",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Delete)]
Task<int> DeleteBatchAsync(string ids);
```

### 4. 软删除（标记为删除）

```csharp
// 使用 UPDATE 实现软删除
[SqlTemplate(@"UPDATE todos 
               SET is_deleted = 1, deleted_at = datetime('now') 
               WHERE id = @id",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Update)]
Task<int> SoftDeleteAsync(long id);
```

### 5. 清空表（谨慎使用）

```csharp
[SqlTemplate("DELETE FROM todos", 
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Delete)]
Task<int> TruncateAsync();
```

---

## 高级CRUD场景

### 1. UPSERT（插入或更新）

#### SQLite - ON CONFLICT

```csharp
[SqlTemplate(@"INSERT INTO todos (id, title, description, updated_at) 
               VALUES (@Id, @Title, @Description, @UpdatedAt)
               ON CONFLICT(id) 
               DO UPDATE SET title = @Title, 
                            description = @Description, 
                            updated_at = @UpdatedAt",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Insert)]
Task<int> UpsertAsync(Todo todo);
```

#### MySQL - ON DUPLICATE KEY UPDATE

```csharp
[SqlTemplate(@"INSERT INTO todos (id, title, description) 
               VALUES (@Id, @Title, @Description)
               ON DUPLICATE KEY UPDATE 
                   title = VALUES(title),
                   description = VALUES(description)",
             Dialect = SqlDefineTypes.MySQL,
             Operation = SqlOperation.Insert)]
Task<int> UpsertAsync(Todo todo);
```

#### PostgreSQL - INSERT ... ON CONFLICT

```csharp
[SqlTemplate(@"INSERT INTO todos (id, title, description) 
               VALUES (@Id, @Title, @Description)
               ON CONFLICT (id) 
               DO UPDATE SET title = EXCLUDED.title,
                            description = EXCLUDED.description
               RETURNING *",
             Dialect = SqlDefineTypes.PostgreSQL,
             Operation = SqlOperation.Insert)]
Task<Todo> UpsertAndReturnAsync(Todo todo);
```

### 2. 事务操作（在服务层实现）

```csharp
public class TodoService : ITodoService
{
    private readonly IDbConnection _connection;

    public async Task<long> CreateWithHistoryAsync(Todo todo)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            // 1. 插入主记录
            var id = await CreateAsync(todo);
            
            // 2. 插入历史记录
            await CreateHistoryAsync(new TodoHistory 
            { 
                TodoId = id, 
                Action = "Created",
                Timestamp = DateTime.UtcNow 
            });
            
            transaction.Commit();
            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### 3. 乐观锁（版本控制）

```csharp
// 添加 version 字段到模型
public record Todo
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Version { get; set; }  // 版本号
    // ...
}

// 更新时检查版本号
[SqlTemplate(@"UPDATE todos 
               SET title = @Title, 
                   version = version + 1,
                   updated_at = datetime('now')
               WHERE id = @Id AND version = @Version",
             Dialect = SqlDefineTypes.SQLite,
             Operation = SqlOperation.Update)]
Task<int> UpdateWithVersionAsync(Todo todo);

// 使用示例
public async Task<bool> TryUpdateAsync(Todo todo)
{
    var affectedRows = await UpdateWithVersionAsync(todo);
    return affectedRows > 0; // 返回 false 表示版本冲突
}
```

### 4. 级联操作

```csharp
// 删除主记录及关联记录
public async Task<int> DeleteWithRelatedAsync(long todoId)
{
    using var transaction = _connection.BeginTransaction();
    try
    {
        // 1. 删除关联的评论
        await DeleteCommentsByTodoIdAsync(todoId);
        
        // 2. 删除关联的标签
        await DeleteTagsByTodoIdAsync(todoId);
        
        // 3. 删除主记录
        var result = await DeleteAsync(todoId);
        
        transaction.Commit();
        return result;
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

---

## 完整示例

### 完整的 CRUD 接口示例

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace MyApp.Services;

/// <summary>
/// TODO 数据访问接口 - 完整的CRUD操作示例
/// </summary>
public interface ITodoService
{
    // ==================== CREATE ====================
    
    /// <summary>创建 - 基础插入</summary>
    [Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
    Task<int> CreateAsync(Todo todo);
    
    /// <summary>创建 - 返回ID</summary>
    [Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}}); SELECT last_insert_rowid()")]
    Task<long> CreateAndReturnIdAsync(Todo todo);
    
    // ==================== READ ====================
    
    /// <summary>查询 - 所有记录</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} {{orderby:created_at_desc}}")]
    Task<List<Todo>> GetAllAsync();
    
    /// <summary>查询 - 根据ID</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
    Task<Todo?> GetByIdAsync(long id);
    
    /// <summary>查询 - 条件查询</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:auto}}")]
    Task<List<Todo>> GetByStatusAsync(bool isCompleted);
    
    /// <summary>查询 - 分页</summary>
    [SqlTemplate("SELECT * FROM todos ORDER BY id DESC LIMIT @pageSize OFFSET @offset",
                 Dialect = SqlDefineTypes.SQLite)]
    Task<List<Todo>> GetPagedAsync(int offset, int pageSize);
    
    /// <summary>查询 - 计数</summary>
    [Sqlx("SELECT {{count:all}} FROM {{table}}")]
    Task<int> GetTotalCountAsync();
    
    /// <summary>查询 - 条件计数</summary>
    [Sqlx("SELECT {{count:all}} FROM {{table}} WHERE {{where:auto}}")]
    Task<int> GetCountByStatusAsync(bool isCompleted);
    
    // ==================== UPDATE ====================
    
    /// <summary>更新 - 完整更新</summary>
    [Sqlx("{{update}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}")]
    Task<int> UpdateAsync(Todo todo);
    
    /// <summary>更新 - 部分字段</summary>
    [SqlTemplate("UPDATE todos SET title = @title, updated_at = @updatedAt WHERE id = @id",
                 Dialect = SqlDefineTypes.SQLite,
                 Operation = SqlOperation.Update)]
    Task<int> UpdateTitleAsync(long id, string title, DateTime updatedAt);
    
    /// <summary>更新 - 批量更新</summary>
    [SqlTemplate("UPDATE todos SET priority = @newPriority WHERE id IN (SELECT value FROM json_each(@ids))",
                 Dialect = SqlDefineTypes.SQLite,
                 Operation = SqlOperation.Update)]
    Task<int> UpdatePriorityBatchAsync(string ids, int newPriority);
    
    // ==================== DELETE ====================
    
    /// <summary>删除 - 根据ID</summary>
    [Sqlx("{{delete}} WHERE {{where:id}}")]
    Task<int> DeleteAsync(long id);
    
    /// <summary>删除 - 条件删除</summary>
    [Sqlx("{{delete}} WHERE {{where:auto}}")]
    Task<int> DeleteByStatusAsync(bool isCompleted);
    
    /// <summary>删除 - 批量删除</summary>
    [SqlTemplate("DELETE FROM todos WHERE id IN (SELECT value FROM json_each(@ids))",
                 Dialect = SqlDefineTypes.SQLite,
                 Operation = SqlOperation.Delete)]
    Task<int> DeleteBatchAsync(string ids);
    
    /// <summary>软删除</summary>
    [SqlTemplate("UPDATE todos SET is_deleted = 1, deleted_at = datetime('now') WHERE id = @id",
                 Dialect = SqlDefineTypes.SQLite,
                 Operation = SqlOperation.Update)]
    Task<int> SoftDeleteAsync(long id);
}

/// <summary>
/// TODO 数据访问实现 - Sqlx 自动生成
/// </summary>
[TableName("todos")]
[RepositoryFor(typeof(ITodoService))]
public partial class TodoService(IDbConnection connection) : ITodoService
{
    // 所有方法由 Sqlx 源代码生成器自动实现
}
```

---

## 📊 占位符快速参考

### CRUD 核心占位符

| 占位符 | 用途 | 示例 |
|--------|------|------|
| `{{insert}}` | INSERT INTO | `{{insert}} (columns) VALUES (values)` |
| `{{update}}` | UPDATE table | `{{update}} SET ... WHERE ...` |
| `{{delete}}` | DELETE FROM | `{{delete}} WHERE ...` |
| `{{table}}` | 表名 | `FROM {{table}}` |
| `{{columns:auto}}` | 自动列名 | `SELECT {{columns:auto}}` |
| `{{values:auto}}` | 自动参数 | `VALUES ({{values:auto}})` |
| `{{where:id}}` | WHERE id = @id | `WHERE {{where:id}}` |
| `{{where:auto}}` | 自动WHERE | `WHERE {{where:auto}}` |
| `{{set:auto}}` | SET 子句 | `SET {{set:auto}}` |
| `{{orderby:field}}` | 排序 | `{{orderby:created_at_desc}}` |
| `{{count:all}}` | COUNT(*) | `SELECT {{count:all}}` |

---

## ✅ 最佳实践

### 1. 优先使用占位符
```csharp
// ✅ 推荐：使用占位符
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]

// ❌ 不推荐：手写列名
[Sqlx("INSERT INTO todos (title, description, ...) VALUES (@Title, @Description, ...)")]
```

### 2. 排除不需要的字段
```csharp
// 插入时排除自增ID和时间戳
[Sqlx("{{insert}} ({{columns:auto|exclude=Id,CreatedAt,UpdatedAt}}) VALUES ({{values:auto}})")]

// 更新时排除ID和创建时间
[Sqlx("{{update}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}")]
```

### 3. 使用类型安全的参数
```csharp
// ✅ 推荐：使用实体对象
Task<int> CreateAsync(Todo todo);

// ❌ 不推荐：大量零散参数
Task<int> CreateAsync(string title, string desc, bool completed, int priority, ...);
```

### 4. 选择合适的返回类型
```csharp
// 返回影响行数
Task<int> UpdateAsync(Todo todo);

// 返回新记录ID
Task<long> CreateAsync(Todo todo);

// 返回单个对象
Task<Todo?> GetByIdAsync(long id);

// 返回列表
Task<List<Todo>> GetAllAsync();
```

---

## 🎯 总结

Sqlx 提供了完整的 CRUD 占位符支持：

✅ **CREATE** - `{{insert}}` + `{{columns:auto}}` + `{{values:auto}}`  
✅ **READ** - `{{columns:auto}}` + `{{where}}` + `{{orderby}}` + `{{count}}`  
✅ **UPDATE** - `{{update}}` + `{{set:auto}}` + `{{where}}`  
✅ **DELETE** - `{{delete}}` + `{{where}}`  

**核心优势**：
- 🚀 代码简洁，自动生成 SQL
- 🔄 字段变更时自动适配
- 🛡️ 类型安全，编译时检查
- 📝 易于维护，清晰直观

