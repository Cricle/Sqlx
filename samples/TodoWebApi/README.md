# 📋 TodoWebApi - Sqlx 完整功能演示

这是一个**真实可用**的待办事项管理 API，展示了 Sqlx 的核心功能和最佳实践。

---

## 🎯 这个示例展示什么？

### ✅ 完整的 RESTful API
- **CRUD 操作**：创建、查询、更新、删除待办事项
- **条件查询**：搜索、筛选、排序
- **批量操作**：批量更新优先级、归档过期任务
- **统计功能**：任务计数

### ✅ Sqlx 核心特性
- **零手写列名**：自动从实体类生成所有列名
- **智能占位符**：`{{table}}` `{{columns}}` `{{values}}` `{{set}}` `{{orderby}}`
- **直接写 SQL**：WHERE 条件直接写，清晰直观
- **100% 类型安全**：编译时检查
- **多数据库支持**：可切换到 SQL Server / MySQL / PostgreSQL

---

## 🚀 快速运行

```bash
# 进入项目目录
cd samples/TodoWebApi

# 运行项目
dotnet run

# 浏览器打开 http://localhost:5000
```

---

## 📝 API 端点一览

### 基础 CRUD

| 功能 | 方法 | 地址 | 说明 |
|------|------|------|------|
| 📋 获取所有 | GET | `/api/todos` | 返回所有待办，按创建时间排序 |
| 🔍 获取单个 | GET | `/api/todos/{id}` | 根据 ID 查询 |
| ➕ 创建 | POST | `/api/todos` | 创建新待办 |
| ✏️ 更新 | PUT | `/api/todos/{id}` | 更新待办信息 |
| 🗑️ 删除 | DELETE | `/api/todos/{id}` | 删除待办 |

### 高级查询

| 功能 | 方法 | 地址 | 说明 |
|------|------|------|------|
| 🔎 搜索 | GET | `/api/todos/search?q=关键词` | 搜索标题或描述 |
| ✅ 已完成 | GET | `/api/todos/completed` | 获取已完成的任务 |
| ⚡ 高优先级 | GET | `/api/todos/high-priority` | 获取高优先级任务（≥3） |
| ⏰ 即将到期 | GET | `/api/todos/due-soon` | 获取7天内到期的任务 |
| 📊 统计 | GET | `/api/todos/count` | 获取任务总数 |

### 批量操作

| 功能 | 方法 | 地址 | 说明 |
|------|------|------|------|
| 🔄 批量更新优先级 | PUT | `/api/todos/batch/priority` | 批量修改优先级 |
| 📦 归档过期任务 | POST | `/api/todos/archive-expired` | 自动归档过期未完成的任务 |

---

## 💡 核心代码讲解

### 1. 数据模型（普通的 C# Record）

```csharp
[TableName("todos")]
public record Todo
{
    public long Id { get; set; }              // 主键ID
    public string Title { get; set; }          // 标题
    public string? Description { get; set; }   // 描述
    public bool IsCompleted { get; set; }      // 是否完成
    public int Priority { get; set; }          // 优先级 1-5
    public DateTime? DueDate { get; set; }     // 到期日期
    public DateTime CreatedAt { get; set; }    // 创建时间
    public DateTime UpdatedAt { get; set; }    // 更新时间
    public DateTime? CompletedAt { get; set; } // 完成时间
    public string? Tags { get; set; }          // 标签
    public int? EstimatedMinutes { get; set; } // 预计耗时
    public int? ActualMinutes { get; set; }    // 实际耗时
}
```

### 2. 服务接口（使用 Sqlx 占位符）

```csharp
public interface ITodoService
{
    /// <summary>获取所有TODO - 自动生成列名和排序</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<Todo>> GetAllAsync();

    /// <summary>根据ID获取TODO - 直接写 SQL</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Todo?> GetByIdAsync(long id);

    /// <summary>创建新TODO - 自动生成列名和值占位符</summary>
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);

    /// <summary>更新TODO - 自动生成 SET 子句</summary>
    [Sqlx("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(Todo todo);

    /// <summary>删除TODO - 简单直接</summary>
    [Sqlx("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    /// <summary>搜索TODO - 直接写 SQL（OR 组合）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query {{orderby updated_at --desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    /// <summary>获取已完成的TODO - 直接写 SQL（等值查询）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted {{orderby completed_at --desc}}")]
    Task<List<Todo>> GetCompletedAsync(bool isCompleted = true);

    /// <summary>获取高优先级TODO - 直接写 SQL（多条件 AND）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE priority >= @minPriority AND is_completed = @isCompleted {{orderby priority --desc}}")]
    Task<List<Todo>> GetHighPriorityAsync(int minPriority = 3, bool isCompleted = false);

    /// <summary>获取即将到期的TODO - 直接写 SQL（NULL 检查 + 比较）</summary>
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE due_date IS NOT NULL AND due_date <= @maxDueDate AND is_completed = @isCompleted {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime maxDueDate, bool isCompleted = false);

    /// <summary>获取任务总数 - 简单计数</summary>
    [Sqlx("SELECT COUNT(*) FROM {{table}}")]
    Task<int> GetTotalCountAsync();

    /// <summary>批量更新优先级 - 自动生成 SET 子句</summary>
    [Sqlx("UPDATE {{table}} SET {{set --only priority updated_at}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdatePriorityBatchAsync(string idsJson, int newPriority, DateTime updatedAt);

    /// <summary>归档过期任务 - 自动生成 SET 子句</summary>
    [Sqlx("UPDATE {{table}} SET {{set --only is_completed completed_at updated_at}} WHERE due_date < @maxDueDate AND is_completed = @isCompleted")]
    Task<int> ArchiveExpiredTasksAsync(DateTime maxDueDate, bool isCompleted, DateTime completedAt, DateTime updatedAt);
}
```

### 3. 服务实现（只需一行！）

```csharp
// Sqlx 自动生成所有方法的实现代码
[TableName("todos")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoService))]
public partial class TodoService(SqliteConnection connection) : ITodoService;
```

**就这么简单！**
- ✅ **零列名**：不用写任何列名（12个字段 × 14个方法 = 168个列名，全部自动生成）
- ✅ **零实现**：不用写实现代码（Sqlx 在编译时自动生成）
- ✅ **零维护**：添加字段自动更新（修改 Todo 类即可）

---

## 🎨 Sqlx 占位符展示

### 智能占位符（自动生成）

| 占位符 | 作用 | 生成示例 |
|--------|------|----------|
| `{{table}}` | 表名 | `todos` |
| `{{columns}}` | 所有列名 | `id, title, description, is_completed, ...` |
| `{{columns --exclude Id}}` | 排除指定列 | `title, description, is_completed, ...` |
| `{{values}}` | 参数占位符 | `@Title, @Description, @IsCompleted, ...` |
| `{{set}}` | SET 子句 | `title=@Title, description=@Description, ...` |
| `{{set --exclude Id CreatedAt}}` | SET 排除列 | `title=@Title, description=@Description, is_completed=@IsCompleted, ...` |
| `{{set --only priority updated_at}}` | SET 仅包含列 | `priority=@Priority, updated_at=@UpdatedAt` |
| `{{orderby created_at --desc}}` | 排序 | `ORDER BY created_at DESC` |

### 直接写 SQL（简单清晰）

```csharp
// ✅ WHERE 条件 - 直接写，清晰直观
WHERE id = @id
WHERE is_completed = @isCompleted
WHERE priority >= @minPriority AND is_completed = @isCompleted
WHERE due_date IS NOT NULL AND due_date <= @maxDueDate

// ✅ LIKE 查询 - 直接写
WHERE title LIKE @query OR description LIKE @query

// ✅ IN 查询 - 配合 SQLite 的 json_each
WHERE id IN (SELECT value FROM json_each(@idsJson))

// ✅ 聚合函数 - 直接写
SELECT COUNT(*) FROM {{table}}
```

---

## 💪 为什么这个示例很强大？

### 1️⃣ 零手写列名

```
传统方式：
- 每个方法手写 12 个列名
- 14 个方法 × 12 列 = 168 次列名输入
- 添加字段需要改 14 个方法
- 容易拼错列名

Sqlx 方式：
- ✅ 0 次手写列名
- ✅ 添加字段自动更新
- ✅ 编译时类型检查
- ✅ 不可能拼错
```

### 2️⃣ 100% 类型安全

```csharp
// ❌ 传统方式：字符串拼接，运行时才知道错误
"SELECT id, title FROM todos WHERE id = " + id  // SQL 注入风险！

// ✅ Sqlx 方式：编译时检查，运行时安全
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<Todo?> GetByIdAsync(long id);
```

### 3️⃣ 语法简洁友好

```csharp
// ✅ 直接写 SQL，一目了然
WHERE is_completed = @isCompleted
WHERE priority >= @minPriority AND is_completed = @isCompleted

// ✅ 智能占位符，自动生成复杂内容
{{columns}}  // 自动生成 id, title, description, is_completed, ...
{{set --exclude Id CreatedAt}}  // 自动生成 SET 子句，排除不可变字段
{{orderby created_at --desc}}  // ORDER BY created_at DESC
```

### 4️⃣ 多数据库支持

同一份代码，不改任何东西，可以切换到：
- ✅ SQL Server
- ✅ MySQL
- ✅ PostgreSQL
- ✅ SQLite（当前使用）
- ✅ Oracle
- ✅ DB2

---

## 📊 生成的 SQL 示例

### GetAllAsync
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
ORDER BY created_at DESC
```

### CreateAsync
```sql
INSERT INTO todos (title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes)
VALUES (@Title, @Description, @IsCompleted, @Priority, @DueDate, @CreatedAt, @UpdatedAt, @CompletedAt, @Tags, @EstimatedMinutes, @ActualMinutes);
SELECT last_insert_rowid()
```

### UpdateAsync
```sql
UPDATE todos
SET title=@Title, description=@Description, is_completed=@IsCompleted, priority=@Priority, due_date=@DueDate, updated_at=@UpdatedAt, completed_at=@CompletedAt, tags=@Tags, estimated_minutes=@EstimatedMinutes, actual_minutes=@ActualMinutes
WHERE id = @id
```

### SearchAsync
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
WHERE title LIKE @query OR description LIKE @query
ORDER BY updated_at DESC
```

---

## 🎓 学习建议

### 新手路线
1. ✅ 先看 `TodoService.cs` 的接口定义
2. ✅ 对照注释理解每个占位符的作用
3. ✅ 运行项目，用浏览器或 Postman 测试 API
4. ✅ 尝试添加一个新字段（如 `Status`）
5. ✅ 观察生成的 SQL 代码（在 `obj/Debug/net9.0/generated/` 目录）

### 进阶练习
1. 添加一个 `SearchByTag` 方法
2. 实现分页查询（使用 `LIMIT` 和 `OFFSET`）
3. 添加用户系统（多表关联）
4. 切换到 SQL Server 或 PostgreSQL 数据库
5. 添加事务支持

---

## 🔧 项目结构

```
TodoWebApi/
├── Models/
│   └── Todo.cs              # 数据模型
├── Services/
│   ├── TodoService.cs       # Sqlx 接口和自动实现
│   └── DatabaseService.cs   # 数据库初始化
├── Json/
│   └── TodoJsonContext.cs   # JSON 序列化（AOT 支持）
├── Program.cs               # 主程序和 API 路由
└── README.md                # 本文档
```

---

## 💡 实用技巧

### 技巧1：排除自增字段
```csharp
// 插入时自动排除 ID（自增字段）
{{columns --exclude Id}}
```

### 技巧2：排除不可变字段
```csharp
// 更新时排除 Id 和 CreatedAt
{{set --exclude Id CreatedAt}}
```

### 技巧3：只更新部分字段
```csharp
// 只更新 priority 和 updated_at
{{set --only priority updated_at}}
```

### 技巧4：多列排序
```csharp
// 先按优先级降序，再按创建时间降序（使用两个 orderby）
{{orderby priority --desc}} {{orderby created_at --desc}}
```

### 技巧5：查看生成的 SQL
生成的代码在 `obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.Generator.CSharpGenerator/` 目录下，
文件名类似 `TodoService.Repository.g.cs`

---

## ❓ 常见问题

### Q1：如何添加一个新字段？
**A：** 非常简单：
1. 在 `Todo` Record 中添加属性
2. 在 `DatabaseService.cs` 的建表语句中添加列
3. 重新编译 - 完成！所有 SQL 自动更新

### Q2：如何切换到其他数据库？
**A：** 修改三处即可：
1. 替换 NuGet 包（如改为 `Npgsql`）
2. 修改连接字符串
3. 修改 `[SqlDefine]` 特性（如 `SqlDefineTypes.PostgreSql`）

### Q3：生成的 SQL 在哪里？
**A：** 在 `obj/Debug/net9.0/generated/` 目录下，搜索 `TodoService.Repository.g.cs`

### Q4：支持事务吗？
**A：** 支持！在 `IDbConnection` 上使用标准的 `BeginTransaction()`

### Q5：如何调试生成的代码？
**A：** 生成的代码是标准 C# 代码，可以像普通代码一样打断点调试

---

## 📚 相关文档

- [📘 Sqlx 主文档](../../README.md)
- [🎯 占位符完整指南](../../docs/EXTENDED_PLACEHOLDERS_GUIDE.md)
- [📝 CRUD 操作指南](../../docs/CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md)
- [💡 最佳实践](../../docs/BEST_PRACTICES.md)

---

## 🎉 总结

**这个示例展示了：**
- ✅ 14 个方法，0 次手写列名
- ✅ 智能占位符 + 直接写 SQL 的完美结合
- ✅ 完整的 RESTful API
- ✅ 100% 类型安全
- ✅ 真实可用的项目结构

**适合：**
- 🎓 学习 Sqlx 的最佳示例
- 🚀 快速启动新项目的模板
- 📚 占位符功能的参考手册
- 💼 企业级项目的参考架构

---

<div align="center">

### 开始你的 Sqlx 之旅吧！🚀

[📖 查看完整文档](../../docs/README.md) · [💡 了解最佳实践](../../docs/BEST_PRACTICES.md)

</div>
