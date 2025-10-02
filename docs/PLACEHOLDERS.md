# 🎯 Sqlx 占位符完整指南

## 📖 什么是占位符？

占位符是 Sqlx 的核心功能，用于**自动生成复杂的 SQL 内容**，减少手写代码，提高类型安全性。

**设计理念：**
- ✅ **智能占位符**：用于自动生成复杂内容（如列名列表、SET 子句）
- ✅ **直接写 SQL**：简单的内容（如 WHERE 条件、聚合函数）直接写更清晰

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

