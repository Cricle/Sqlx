# Sqlx TodoWebApi - 完整功能展示

本示例展示了 Sqlx 的所有核心功能，包括最新的高级类型支持。

## 🎯 核心功能

### 1. 高级类型支持

Sqlx 自动识别并优化不同的 C# 类型：

#### Class（标准类）
```csharp
[Sqlx, TableName("todos")]
public class Todo
{
    [Key] public long Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
}
// 生成: new Todo { Id = id, Title = title, IsCompleted = isCompleted }
```

#### Pure Record（纯 Record）
```csharp
[Sqlx, TableName("todo_snapshots")]
public record TodoSnapshot(
    long Id,
    string Title,
    bool IsCompleted,
    int Priority,
    DateTime CreatedAt
);
// 生成: new TodoSnapshot(id, title, isCompleted, priority, createdAt)
```

#### Mixed Record（混合 Record）
```csharp
[Sqlx, TableName("todo_summaries")]
public record TodoSummary(long Id, string Title)
{
    public bool IsCompleted { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    
    // 只读属性 - 自动忽略
    public string Status => IsCompleted ? "完成" : "进行中";
}
// 生成: new TodoSummary(id, title) { IsCompleted = isCompleted, Priority = priority, DueDate = dueDate }
```

#### Struct（结构体）
```csharp
[Sqlx, TableName("coordinates")]
public struct Coordinate
{
    public int X { get; set; }
    public int Y { get; set; }
    
    // 只读属性 - 自动忽略
    public double DistanceFromOrigin => Math.Sqrt(X * X + Y * Y);
}
// 生成: new Coordinate { X = x, Y = y }
```

#### Struct Record（结构体 Record）
```csharp
[Sqlx, TableName("points")]
public readonly record struct Point(int X, int Y)
{
    // 只读属性 - 自动忽略
    public double Distance => Math.Sqrt(X * X + Y * Y);
}
// 生成: new Point(x, y)
```

#### 只读属性自动过滤
```csharp
[Sqlx, TableName("todo_details")]
public class TodoDetail
{
    [Key] public long Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    
    // 这些只读属性不会生成到 SQL 中
    public string DisplayTitle => $"[{(IsCompleted ? "✓" : " ")}] {Title}";
    public int DaysOld => (DateTime.UtcNow - CreatedAt).Days;
    public bool IsRecent => DaysOld < 7;
}
```

### 2. 三种查询方式

#### 方式 1: SqlTemplate（直接 SQL）
```csharp
// 定义
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query")]
Task<List<Todo>> SearchAsync(string query);

// 使用
var results = await repo.SearchAsync("%keyword%");
```

#### 方式 2: LINQ Expression（类型安全谓词）
```csharp
// 定义
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> predicate);

// 使用
var highPriority = await repo.GetWhereAsync(t => 
    t.Priority >= 3 && 
    !t.IsCompleted &&
    t.DueDate < DateTime.Now.AddDays(7)
);
```

#### 方式 3: IQueryable（完整 LINQ 查询构建器）
```csharp
// 定义（从 ICrudRepository 继承）
IQueryable<Todo> AsQueryable();

// 使用
var query = repo.AsQueryable()
    .Where(t => t.Priority >= 3)
    .Where(t => !t.IsCompleted)
    .OrderByDescending(t => t.Priority)
    .ThenBy(t => t.DueDate)
    .Skip(10)
    .Take(10);

var todos = await query.ToListAsync();

// 调试：查看生成的 SQL
var sql = query.ToSql();
Console.WriteLine(sql);
```

#### 方式 4: ExpressionBlockResult（统一表达式解析）⚡ 新功能

**性能优势：** 比传统方式快 2 倍，一次遍历同时获取 SQL 和参数。

```csharp
// 定义
Task<int> DynamicUpdateWhereAsync(
    Expression<Func<Todo, Todo>> updateExpression,
    Expression<Func<Todo, bool>> whereExpression);

// 使用 - 类型安全的动态更新
await repo.DynamicUpdateWhereAsync(
    // UPDATE 表达式
    t => new Todo 
    { 
        Priority = 5,
        UpdatedAt = DateTime.UtcNow
    },
    // WHERE 表达式
    t => t.IsCompleted == false && t.Priority < 3
);

// 复杂示例 - 字符串函数 + 增量更新
await repo.DynamicUpdateWhereAsync(
    t => new Todo 
    { 
        Title = t.Title.Trim().ToLower(),
        Priority = t.Priority + 1,
        Version = t.Version + 1
    },
    t => t.Priority >= 3 && t.DueDate < DateTime.Now
);
```

**性能对比：**

| 方法 | 表达式遍历次数 | 相对性能 |
|------|--------------|---------|
| 传统方式 | 4 次 | 基准 |
| ExpressionBlockResult | 2 次 | **快 2 倍** ⚡ |

**实现原理：**
```csharp
// 传统方式 - 4 次遍历
var updateSql = expr.ToSetClause();           // 遍历 1
var updateParams = expr.GetSetParameters();   // 遍历 2
var whereSql = predicate.ToWhereClause();     // 遍历 3
var whereParams = predicate.GetParameters();  // 遍历 4

// ExpressionBlockResult - 2 次遍历
var updateResult = ExpressionBlockResult.ParseUpdate(expr, dialect);      // 遍历 1
var whereResult = ExpressionBlockResult.Parse(predicate.Body, dialect);   // 遍历 2
```

**特性：**
- ✅ 一次解析同时获取 SQL 和参数
- ✅ 零反射，纯表达式树解析
- ✅ 完全支持 Native AOT
- ✅ 线程安全，无共享状态
- ✅ 支持所有数据库方言

### 3. ICrudRepository 内置方法（46个）

继承 `ICrudRepository<Todo, long>` 自动获得：

#### 查询方法（24个）

（保持不变）

#### 命令方法（22个）
```csharp
// 单实体查询
var todo = await repo.GetByIdAsync(1);
var first = await repo.GetFirstWhereAsync(t => t.IsCompleted);

// 列表查询
var all = await repo.GetAllAsync(limit: 100);
var byIds = await repo.GetByIdsAsync(new[] { 1L, 2L, 3L });
var filtered = await repo.GetWhereAsync(t => t.Priority >= 3);

// 分页查询
var page1 = await repo.GetPagedAsync(pageSize: 20, offset: 0);
var page2 = await repo.GetPagedWhereAsync(
    predicate: t => t.IsCompleted,
    pageSize: 20,
    offset: 20
);

// 存在性和计数
var exists = await repo.ExistsByIdAsync(1);
var hasActive = await repo.ExistsAsync(t => !t.IsCompleted);
var total = await repo.CountAsync();
var activeCount = await repo.CountWhereAsync(t => !t.IsCompleted);
```

```csharp
// 插入（6个）
var newId = await repo.InsertAndGetIdAsync(todo);
await repo.InsertAsync(todo);
await repo.BatchInsertAsync(todos);

// 更新（10个）
await repo.UpdateAsync(todo);
await repo.UpdateWhereAsync(todo, t => t.Id == todo.Id);
await repo.BatchUpdateAsync(todos);

// 动态更新 - 类型安全的部分字段更新 ⚡ 新功能
await repo.DynamicUpdateAsync(todoId, t => new Todo 
{ 
    Priority = 5,
    UpdatedAt = DateTime.UtcNow
});

// 动态批量更新 - 使用表达式条件 ⚡ 新功能
await repo.DynamicUpdateWhereAsync(
    t => new Todo { IsCompleted = true, CompletedAt = DateTime.UtcNow },
    t => t.Priority >= 3 && t.DueDate < DateTime.Now
);

// 删除（6个）
await repo.DeleteAsync(1);
await repo.DeleteByIdsAsync(new[] { 1L, 2L, 3L });
await repo.DeleteWhereAsync(t => t.IsCompleted);
await repo.DeleteAllAsync();
```

### 4. 动态更新（DynamicUpdate）⚡ 新功能

使用 `DynamicUpdateAsync` 和 `DynamicUpdateWhereAsync` 实现类型安全的动态字段更新：

```csharp
// 单条记录动态更新 - 只更新指定字段
await repo.DynamicUpdateAsync(todoId, t => new Todo 
{ 
    Priority = 5,
    UpdatedAt = DateTime.UtcNow
});
// 生成: UPDATE [todos] SET [priority] = @p0, [updated_at] = @p1 WHERE [id] = @id

// 批量动态更新 - 使用 WHERE 表达式
await repo.DynamicUpdateWhereAsync(
    t => new Todo { IsCompleted = true, CompletedAt = DateTime.UtcNow },
    t => t.Priority >= 3 && !t.IsCompleted
);
// 生成: UPDATE [todos] SET [is_completed] = @p0, [completed_at] = @p1 
//       WHERE [priority] >= @p2 AND [is_completed] = @p3

// 增量更新 - 字段自引用
await repo.DynamicUpdateAsync(todoId, t => new Todo 
{ 
    Priority = t.Priority + 1,
    Version = t.Version + 1
});
// 生成: UPDATE [todos] SET [priority] = [priority] + @p0, [version] = [version] + @p1 
//       WHERE [id] = @id

// 字符串函数
await repo.DynamicUpdateAsync(todoId, t => new Todo 
{ 
    Title = t.Title.Trim().ToUpper()
});
// 生成: UPDATE [todos] SET [title] = UPPER(TRIM([title])) WHERE [id] = @id
```

**优势：**
- ✅ 类型安全 - 编译时检查字段名和类型
- ✅ IDE 支持 - 智能提示和重构
- ✅ 灵活性 - 任意字段组合
- ✅ 表达式支持 - 增量、函数、计算
- ✅ 防注入 - 自动参数化

### 5. 表达式占位符（Any Placeholder）⚡ 新功能

使用 `Any.Value<T>()` 创建可重用的表达式模板：

```csharp
// 定义可重用的增量表达式模板
var incrementTemplate = ExpressionBlockResult.ParseUpdate<Todo>(
    t => new Todo 
    { 
        Priority = t.Priority + Any.Value<int>("increment"),
        Version = t.Version + 1
    },
    SqlDefine.SQLite
);

// 使用不同的增量值
var result1 = incrementTemplate
    .WithParameter("increment", 1)
    .WithParameter("version_increment", 1);
// SQL: [priority] = [priority] + @increment, [version] = [version] + 1

var result2 = incrementTemplate
    .WithParameter("increment", 5)
    .WithParameter("version_increment", 1);
// SQL: [priority] = [priority] + @increment, [version] = [version] + 1
// 参数: increment=5

// 批量操作模板
var batchUpdateTemplate = ExpressionBlockResult.ParseUpdate<Todo>(
    t => new Todo 
    { 
        Priority = Any.Value<int>("newPriority"),
        UpdatedAt = DateTime.UtcNow
    },
    SqlDefine.SQLite
);

// 为不同的批次填充不同的值
foreach (var batch in batches)
{
    var result = batchUpdateTemplate.WithParameter("newPriority", batch.Priority);
    // 执行更新...
}
```

**使用场景：**
- ✅ 模板重用 - 一次定义，多次使用
- ✅ 批量操作 - 相同结构，不同参数
- ✅ 动态表单 - 运行时填充值
- ✅ 条件更新 - 根据条件选择参数

### 6. 批量操作

使用内置的批量方法或 DynamicUpdateWhereAsync：

```csharp
// 方式1：使用 DynamicUpdateWhereAsync（推荐）
await repo.DynamicUpdateWhereAsync(
    t => new Todo { Priority = 5, UpdatedAt = DateTime.UtcNow },
    t => ids.Contains(t.Id)
);

// 方式2：使用 BatchUpdateAsync
var todos = await repo.GetByIdsAsync(ids);
foreach (var todo in todos)
{
    todo.Priority = 5;
    todo.UpdatedAt = DateTime.UtcNow;
}
await repo.BatchUpdateAsync(todos);
```

### 7. 内联表达式

```csharp
// INSERT 时设置默认值
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns --exclude Id}}) 
    VALUES ({{values --exclude Id --inline 
        IsCompleted=0,
        Priority=2,
        CreatedAt=CURRENT_TIMESTAMP,
        UpdatedAt=CURRENT_TIMESTAMP
    }})
")]
[ReturnInsertedId]
Task<long> CreateWithDefaultsAsync(string title, string? description);

// UPDATE 时自动更新时间戳
[SqlTemplate(@"
    UPDATE {{table}} 
    SET {{set --exclude Id,CreatedAt,UpdatedAt --inline UpdatedAt=CURRENT_TIMESTAMP}} 
    WHERE id = @id
")]
Task<int> UpdateWithTimestampAsync(Todo todo);

// 计数器递增
[SqlTemplate(@"
    UPDATE {{table}} 
    SET {{set --exclude Id --inline ViewCount=ViewCount+1}} 
    WHERE id = @id
")]
Task<int> IncrementViewCountAsync(long id);
```

### 8. 动态 SET 表达式（`{{set --param}}`）

使用 `{{set --param}}` 配合 `Expression<Func<T, T>>` 可以实现类型安全的动态更新：

```csharp
// 定义动态更新方法
[SqlTemplate("UPDATE {{table}} SET {{set --param updates}} WHERE id = @id")]
Task<int> DynamicUpdateAsync(long id, string updates);

// 使用示例 1: 更新单个字段（类型安全）
Expression<Func<Todo, Todo>> expr = t => new Todo { Priority = 5 };
var setClause = expr.ToSetClause(); // "[priority] = @p0"
await repo.DynamicUpdateAsync(todoId, setClause);

// 使用示例 2: 递增表达式
Expression<Func<Todo, Todo>> expr = t => new Todo { Version = t.Version + 1 };
var setClause = expr.ToSetClause(); // "[version] = ([version] + @p0)"
await repo.DynamicUpdateAsync(todoId, setClause);

// 使用示例 3: 多字段更新
Expression<Func<Todo, Todo>> expr = t => new Todo 
{ 
    Title = "新标题",
    Priority = 5,
    Version = t.Version + 1
};
var setClause = expr.ToSetClause(); // "[title] = @p0, [priority] = @p1, [version] = ([version] + @p2)"
await repo.DynamicUpdateAsync(todoId, setClause);

// 使用示例 4: 条件构建（动态表单）
Expression<Func<Todo, Todo>>? updateExpr = null;
if (updatePriority && updateTitle)
{
    updateExpr = t => new Todo { Title = newTitle, Priority = newPriority };
}
else if (updatePriority)
{
    updateExpr = t => new Todo { Priority = newPriority };
}
else if (updateTitle)
{
    updateExpr = t => new Todo { Title = newTitle };
}

if (updateExpr != null)
{
    var setClause = updateExpr.ToSetClause();
    await repo.DynamicUpdateAsync(todoId, setClause);
}

// 使用示例 5: 字符串函数
Expression<Func<Todo, Todo>> expr = t => new Todo 
{ 
    Title = t.Title.Trim().ToUpper(),
    Description = t.Description + " (已更新)"
};
var setClause = expr.ToSetClause();
// 生成: "[title] = UPPER(TRIM([title])), [description] = [description] || @p0"

// 使用示例 6: 数学函数
Expression<Func<Todo, Todo>> expr = t => new Todo 
{ 
    Priority = Math.Abs(t.Priority),
    ActualMinutes = Math.Max(t.ActualMinutes, 0)
};
var setClause = expr.ToSetClause();
// 生成: "[priority] = ABS([priority]), [actual_minutes] = GREATEST([actual_minutes], @p0)"
```

**支持的函数：**

| 类别 | 函数 | SQL 输出 |
|------|------|---------|
| 字符串 | `ToLower()` | `LOWER(column)` |
| 字符串 | `ToUpper()` | `UPPER(column)` |
| 字符串 | `Trim()` | `TRIM(column)` |
| 字符串 | `Substring(start, length)` | `SUBSTR(column, start, length)` |
| 字符串 | `Replace(old, new)` | `REPLACE(column, old, new)` |
| 字符串 | `+ (连接)` | `column \|\| value` (SQLite) |
| 数学 | `Math.Abs()` | `ABS(column)` |
| 数学 | `Math.Round()` | `ROUND(column)` |
| 数学 | `Math.Ceiling()` | `CEIL(column)` |
| 数学 | `Math.Floor()` | `FLOOR(column)` |
| 数学 | `Math.Pow()` | `POWER(column, exp)` |
| 数学 | `Math.Sqrt()` | `SQRT(column)` |
| 数学 | `Math.Max()` | `GREATEST(a, b)` (SQLite) |
| 数学 | `Math.Min()` | `LEAST(a, b)` (SQLite) |

**对比静态 SET 和动态 SET：**

| 特性 | 静态 `{{set}}` | 动态 `{{set --param}}` + 表达式树 |
|------|---------------|--------------------------------|
| 编译时确定 | ✅ 是 | ❌ 否 |
| 性能 | 🚀 最快（预编译） | ⚡ 快（运行时渲染） |
| 灵活性 | ⚠️ 固定字段 | ✅ 任意字段组合 |
| 类型安全 | ✅ 完全类型安全 | ✅ 完全类型安全（表达式树） |
| IDE 支持 | ✅ 智能提示 | ✅ 智能提示 + 重构 |
| 使用场景 | 标准 CRUD | 动态表单、部分更新、条件更新 |

**类型安全的优势：**
- ✅ 编译时检查字段名和类型
- ✅ IDE 智能提示和重构支持
- ✅ 自动参数化，防止 SQL 注入
- ✅ 支持复杂表达式（递增、计算等）

### 9. 条件占位符

```csharp
// 动态搜索
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1
    {{if notnull=title}}AND title LIKE @title{{/if}}
    {{if notnull=minPriority}}AND priority >= @minPriority{{/if}}
    {{if notnull=isCompleted}}AND is_completed = @isCompleted{{/if}}
    ORDER BY created_at DESC
")]
Task<List<Todo>> SearchAsync(
    string? title, 
    int? minPriority, 
    bool? isCompleted
);

// 使用
var results = await repo.SearchAsync(
    title: "%urgent%",
    minPriority: 3,
    isCompleted: null  // 忽略此条件
);
```

### 10. 调试方法

```csharp
// 返回 SqlTemplate 用于调试
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
SqlTemplate GetByIdSql(long id);

// 使用
var template = repo.GetByIdSql(123);
Console.WriteLine($"Prepared SQL: {template.Sql}");
Console.WriteLine($"Has dynamic placeholders: {template.HasDynamicPlaceholders}");
```

## 📊 API 端点

### 基础 CRUD
- `GET /api/todos` - 获取所有 TODO
- `GET /api/todos/{id}` - 获取单个 TODO
- `POST /api/todos` - 创建 TODO
- `PUT /api/todos/{id}` - 更新 TODO
- `DELETE /api/todos/{id}` - 删除 TODO

### 查询与过滤
- `GET /api/todos/search?q={keyword}` - 搜索 TODO
- `GET /api/todos/completed` - 获取已完成的 TODO
- `GET /api/todos/high-priority` - 获取高优先级 TODO
- `GET /api/todos/due-soon` - 获取即将到期的 TODO
- `GET /api/todos/overdue` - 获取已逾期的 TODO
- `GET /api/todos/priority/{priority}` - 按优先级获取

### 统计与聚合
- `GET /api/todos/count` - 总数
- `GET /api/todos/count/pending` - 待办数
- `GET /api/todos/queryable/stats` - 完整统计信息

### 批量操作
- `PUT /api/todos/batch/priority` - 批量更新优先级
- `PUT /api/todos/batch/complete` - 批量完成
- `DELETE /api/todos/batch` - 批量删除
- `DELETE /api/todos/completed` - 删除所有已完成

### 分页与存在性
- `GET /api/todos/paged?page={page}&pageSize={size}` - 分页查询
- `GET /api/todos/{id}/exists` - 检查是否存在
- `POST /api/todos/by-ids` - 批量获取

### LINQ 示例
- `GET /api/todos/linq/high-priority-pending` - LINQ 表达式查询
- `GET /api/todos/linq/count-overdue` - LINQ 计数
- `GET /api/todos/queryable/priority-paged` - IQueryable 分页
- `GET /api/todos/queryable/titles` - IQueryable 投影
- `GET /api/todos/queryable/search-advanced` - IQueryable 高级搜索

## 🎨 前端功能

### 基础功能
- ✅ 添加新任务
- ✅ 标记完成/未完成
- ✅ 删除任务
- ✅ 实时统计（总数、活动、完成、完成率）

### 过滤功能
- ✅ 全部任务
- ✅ 活动任务
- ✅ 已完成任务

### UI 特性
- ✅ 玻璃态设计（Glassmorphism）
- ✅ 流畅动画效果
- ✅ 完全响应式
- ✅ 优先级标签（高/中/低）
- ✅ 相对时间显示
- ✅ 空状态提示

## 🚀 性能特性

### 编译时优化
- ✅ 零反射 - 所有代码在编译时生成
- ✅ 静态 SQL 模板 - 预解析和缓存
- ✅ 优化的列序数 - 使用 struct 避免数组访问
- ✅ 类型特定代码生成 - 为每种类型生成最优代码

### 运行时优化
- ✅ 连接池 - 单例连接管理
- ✅ 参数化查询 - 防止 SQL 注入
- ✅ 批量操作 - 减少数据库往返
- ✅ 容量提示 - List 预分配避免扩容

### AOT 支持
- ✅ 完全 Native AOT 兼容
- ✅ 使用 `CreateSlimBuilder` 减少启动时间
- ✅ JSON 源生成器 - 零反射序列化
- ✅ 最小化依赖 - 只包含必需的功能

## 📝 测试

运行 API 测试：
```bash
pwsh test-api.ps1
```

测试覆盖：
- ✅ 39 个 API 端点
- ✅ 所有 CRUD 操作
- ✅ 批量操作
- ✅ LINQ 查询
- ✅ IQueryable 查询
- ✅ 错误处理

## 🎓 学习资源

### 关键概念
1. **源生成器** - 编译时代码生成，零运行时开销
2. **占位符系统** - 智能 SQL 模板，跨数据库兼容
3. **类型检测** - 自动识别 class/record/struct 并生成最优代码
4. **只读属性过滤** - 自动忽略计算属性
5. **IQueryable 支持** - 完整的 LINQ 查询构建器

### 最佳实践
1. **选择合适的类型**
   - 不可变数据：使用 `record` 或 `readonly record struct`
   - 可变数据：使用 `class` 或 `struct`
   - 小型值类型：使用 `struct` 或 `record struct`

2. **选择合适的查询方式**
   - 简单查询：使用 SqlTemplate
   - 动态条件：使用 LINQ Expression
   - 复杂查询：使用 IQueryable

3. **性能优化**
   - 使用批量操作减少往返
   - 使用分页避免大结果集
   - 使用 IQueryable 的 Take/Skip 进行服务器端分页
   - 使用内联表达式减少代码重复

4. **调试技巧**
   - 使用 SqlTemplate 返回类型查看生成的 SQL
   - 使用 IQueryable.ToSql() 查看 LINQ 生成的 SQL
   - 启用拦截器记录所有 SQL 执行

## 🔗 相关链接

- [Sqlx 主仓库](../../README.md)
- [API 参考](../../docs/api-reference.md)
- [SQL 模板文档](../../docs/sql-templates.md)
- [源生成器文档](../../docs/source-generators.md)
