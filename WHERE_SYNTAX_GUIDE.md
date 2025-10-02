# Sqlx 增强 WHERE 语法完整指南

## 🎯 设计目标

将 WHERE 占位符从简单的字段匹配升级为**支持表达式和组合**的强大工具，让你像写普通 SQL 一样直观和灵活。

---

## ❌ 旧语法的问题

### 问题 1：不够直观
```csharp
[Sqlx("WHERE {{where:id}}")]  
// 🤔 生成什么？WHERE id = @id 还是 WHERE id = @param？
```

### 问题 2：不够灵活
```csharp
[Sqlx("WHERE {{where:is_active}}")]  
// ❌ 只能处理等值匹配 (=)
// ❌ 不支持比较运算符 (>, <, >=, <=)
// ❌ 不支持 LIKE、IS NULL 等
```

### 问题 3：多条件复杂
```csharp
[Sqlx("WHERE {{where:is_active_and_age_ge_18}}")]  
// ❌ 占位符名太长
// ❌ 需要手动定义组合规则
```

---

## ✅ 新语法的优势

### 核心特点

| 特点 | 说明 | 示例 |
|------|------|------|
| **表达式支持** | 直接写条件表达式 | `{{where is_active=@active}}` |
| **运算符支持** | `=` `>` `<` `>=` `<=` `!=` `LIKE` `IS NULL` 等 | `{{where age>=@min}}` |
| **常量支持** | 字符串、数字、布尔 | `{{where status='pending'}}` |
| **组合支持** | AND/OR 连接 | `{{where A}} AND {{where B}}` |
| **括号支持** | 控制优先级 | `({{where A}} OR {{where B}}) AND {{where C}}` |
| **零学习成本** | 就像写普通 SQL | 一眼看懂 |

---

## 📚 完整语法详解

### 1️⃣ 单个条件（等值查询）

**语法：** `{{where column=@param}}`

```csharp
// ✅ 布尔字段
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_active=@active}}")]
Task<List<User>> GetActiveUsersAsync(bool active);
// 生成：WHERE is_active = @active

// ✅ 字符串字段
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where name=@name}}")]
Task<User?> FindByNameAsync(string name);
// 生成：WHERE name = @name

// ✅ 数字字段
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where id=@id}}")]
Task<User?> GetByIdAsync(int id);
// 生成：WHERE id = @id
```

---

### 2️⃣ 比较运算符

**支持的运算符：** `=` `>` `<` `>=` `<=` `!=` `<>`

```csharp
// ✅ 大于等于
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where age>=@minAge}}")]
Task<List<User>> GetAdultsAsync(int minAge = 18);
// 生成：WHERE age >= @minAge

// ✅ 小于
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where salary<@max}}")]
Task<List<User>> GetLowSalaryUsersAsync(decimal max);
// 生成：WHERE salary < @max

// ✅ 不等于
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where status!=@status}}")]
Task<List<Order>> GetNonStatusOrdersAsync(string status);
// 生成：WHERE status != @status

// ✅ 范围查询（两个条件组合）
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where salary>@min}} AND {{where salary<@max}}")]
Task<List<User>> GetSalaryRangeAsync(decimal min, decimal max);
// 生成：WHERE salary > @min AND salary < @max
```

---

### 3️⃣ 多个条件（AND）

**语法：** `{{where A}} AND {{where B}}`

```csharp
// ✅ 两个条件
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_active=@active}} AND {{where age>=@minAge}}")]
Task<List<User>> SearchAsync(bool active, int minAge);
// 生成：WHERE is_active = @active AND age >= @minAge

// ✅ 三个条件
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_active=true}} AND {{where age>=18}} AND {{where email IS NOT NULL}}")]
Task<List<User>> GetAdultActiveUsersWithEmailAsync();
// 生成：WHERE is_active = 1 AND age >= 18 AND email IS NOT NULL

// ✅ 混合常量和参数
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_deleted=false}} AND {{where created_at>=@startDate}}")]
Task<List<User>> GetNonDeletedAfterDateAsync(DateTime startDate);
// 生成：WHERE is_deleted = 0 AND created_at >= @startDate
```

---

### 4️⃣ 多个条件（OR）

**语法：** `{{where A}} OR {{where B}}`

```csharp
// ✅ 两个条件（OR）
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where name=@name}} OR {{where email=@email}}")]
Task<User?> FindByNameOrEmailAsync(string name, string email);
// 生成：WHERE name = @name OR email = @email

// ✅ 三个条件（OR）
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where phone=@value}} OR {{where email=@value}} OR {{where username=@value}}")]
Task<User?> FindByContactAsync(string value);
// 生成：WHERE phone = @value OR email = @value OR username = @value
```

---

### 5️⃣ 复杂条件组合（AND + OR + 括号）

**语法：** `({{where A}} OR {{where B}}) AND {{where C}}`

```csharp
// ✅ (A OR B) AND C
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE ({{where name=@name}} OR {{where email=@email}}) AND {{where is_active=true}}")]
Task<User?> FindActiveUserAsync(string name, string email);
// 生成：WHERE (name = @name OR email = @email) AND is_active = 1

// ✅ (A AND B) OR (C AND D)
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE ({{where role='admin'}} AND {{where is_active=true}}) OR ({{where role='owner'}} AND {{where is_deleted=false}})")]
Task<List<User>> GetPrivilegedUsersAsync();
// 生成：WHERE (role = 'admin' AND is_active = 1) OR (role = 'owner' AND is_deleted = 0)
```

---

### 6️⃣ 常量值支持

**支持的类型：** 字符串、数字、布尔、NULL

```csharp
// ✅ 字符串常量
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where status='pending'}}")]
Task<List<Order>> GetPendingOrdersAsync();
// 生成：WHERE status = 'pending'

// ✅ 数字常量
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where priority>3}}")]
Task<List<Todo>> GetHighPriorityTodosAsync();
// 生成：WHERE priority > 3

// ✅ 布尔常量
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_deleted=false}}")]
Task<List<User>> GetNonDeletedUsersAsync();
// 生成：WHERE is_deleted = 0  （SQLite）
// 生成：WHERE is_deleted = false  （PostgreSQL）

// ✅ NULL 常量
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where deleted_at IS NULL}}")]
Task<List<User>> GetActiveUsersAsync();
// 生成：WHERE deleted_at IS NULL
```

---

### 7️⃣ NULL 检查

**语法：** `{{where column IS NULL}}` / `{{where column IS NOT NULL}}`

```csharp
// ✅ IS NOT NULL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where email IS NOT NULL}}")]
Task<List<User>> GetUsersWithEmailAsync();
// 生成：WHERE email IS NOT NULL

// ✅ IS NULL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where deleted_at IS NULL}}")]
Task<List<User>> GetActiveUsersAsync();
// 生成：WHERE deleted_at IS NULL

// ✅ 组合使用
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where email IS NOT NULL}} AND {{where phone IS NOT NULL}}")]
Task<List<User>> GetUsersWithFullContactAsync();
// 生成：WHERE email IS NOT NULL AND phone IS NOT NULL
```

---

### 8️⃣ LIKE 模糊查询

**语法：** `{{where column LIKE @pattern}}`

```csharp
// ✅ LIKE 查询
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where name LIKE @pattern}}")]
Task<List<User>> SearchByNameAsync(string pattern);
// 调用：SearchByNameAsync("%john%")
// 生成：WHERE name LIKE @pattern

// ✅ 组合 LIKE（OR）
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where name LIKE @query}} OR {{where email LIKE @query}}")]
Task<List<User>> SearchAsync(string query);
// 调用：SearchAsync("%@example.com%")
// 生成：WHERE name LIKE @query OR email LIKE @query
```

---

## 🎨 完整示例（TodoWebApi）

### 完整的 ITodoService 接口

```csharp
public interface ITodoService
{
    // 1. 查询所有TODO - 自动生成列名和排序
    [Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<Todo>> GetAllAsync();

    // 2. 根据ID获取TODO - WHERE 表达式（等值）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where id=@id}}")]
    Task<Todo?> GetByIdAsync(long id);

    // 3. 创建新TODO
    [Sqlx("{{insert into}} ({{columns --exclude Id}}) VALUES ({{values}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);

    // 4. 更新TODO
    [Sqlx("{{update}} SET {{set --exclude Id CreatedAt}} WHERE {{where id=@id}}")]
    Task<int> UpdateAsync(Todo todo);

    // 5. 删除TODO
    [Sqlx("{{delete from}} WHERE {{where id=@id}}")]
    Task<int> DeleteAsync(long id);

    // 6. 搜索TODO - WHERE 表达式组合（OR）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where title LIKE @query}} OR {{where description LIKE @query}} {{orderby updated_at --desc}}")]
    Task<List<Todo>> SearchAsync(string query);

    // 7. 获取已完成的TODO - WHERE 表达式（等值查询）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_completed=@isCompleted}} {{orderby completed_at --desc}}")]
    Task<List<Todo>> GetCompletedAsync(bool isCompleted = true);

    // 8. 获取高优先级TODO - WHERE 表达式（多条件 AND）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where priority>=@minPriority}} AND {{where is_completed=@isCompleted}} {{orderby priority --desc}}")]
    Task<List<Todo>> GetHighPriorityAsync(int minPriority = 3, bool isCompleted = false);

    // 9. 获取即将到期的TODO - WHERE 表达式（NULL 检查 + 比较）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where due_date IS NOT NULL}} AND {{where due_date<=@maxDueDate}} AND {{where is_completed=@isCompleted}} {{orderby due_date}}")]
    Task<List<Todo>> GetDueSoonAsync(DateTime maxDueDate, bool isCompleted = false);

    // 10. 获取任务总数
    [Sqlx("SELECT {{count}} FROM {{table}}")]
    Task<int> GetTotalCountAsync();

    // 11. 批量更新优先级
    [Sqlx("{{update}} SET {{set --only priority updated_at}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
    Task<int> UpdatePriorityBatchAsync(string idsJson, int newPriority, DateTime updatedAt);

    // 12. 归档过期任务 - WHERE 表达式（多条件）
    [Sqlx("{{update}} SET {{set --only is_completed completed_at updated_at}} WHERE {{where due_date<@maxDueDate}} AND {{where is_completed=@isCompleted}}")]
    Task<int> ArchiveExpiredTasksAsync(DateTime maxDueDate, bool isCompleted, DateTime completedAt, DateTime updatedAt);
}
```

---

## 📊 与旧语法对比

| 场景 | 旧语法（冗长/不灵活） | 新语法（简洁/强大） |
|------|---------------------|-------------------|
| **等值查询** | `{{where:id}}` | `{{where id=@id}}` ✅ 更清晰 |
| **比较查询** | ❌ 不支持，需手写SQL | `{{where age>=@min}}` ✅ 支持 |
| **多条件AND** | `{{where:is_active_and_age_ge_18}}` ❌ 太长 | `{{where is_active=@active}} AND {{where age>=18}}` ✅ 灵活 |
| **多条件OR** | ❌ 不支持 | `{{where A}} OR {{where B}}` ✅ 支持 |
| **常量值** | ❌ 不支持 | `{{where status='pending'}}` ✅ 支持 |
| **NULL检查** | `{{notnull:column}}` ⚠️ 需记占位符 | `{{where email IS NOT NULL}}` ✅ 就像SQL |
| **LIKE查询** | ❌ 不支持 | `{{where name LIKE @pattern}}` ✅ 支持 |
| **复杂组合** | ❌ 不支持 | `({{where A}} OR {{where B}}) AND {{where C}}` ✅ 支持 |

---

## ✅ 核心优势总结

| 优势 | 说明 |
|------|------|
| 🎯 **更直观** | `is_active=@active` 一眼看懂，不用猜 |
| 🎯 **更灵活** | 支持任意表达式和运算符 |
| 🎯 **更强大** | AND/OR 组合 + 括号优先级 |
| 🎯 **零学习成本** | 就像写普通 SQL |
| 🎯 **完全兼容** | 可以混用占位符和 SQL |
| 🎯 **类型安全** | 参数绑定，防止 SQL 注入 |
| 🎯 **多数据库** | 自动适配 6 种数据库方言 |

---

## 🚀 迁移指南

### 旧语法 → 新语法

| 旧写法 | 新写法 |
|--------|--------|
| `{{where:id}}` | `{{where id=@id}}` |
| `{{where:is_active}}` | `{{where is_active=@isActive}}` |
| `{{where:status_eq_pending}}` | `{{where status='pending'}}` |
| `{{notnull:email}}` | `{{where email IS NOT NULL}}` |
| 手写SQL: `priority >= @min AND ...` | `{{where priority>=@min}} AND ...` |

### 迁移步骤

1. **全局搜索 `{{where:` 替换为 `{{where `**
2. **手动检查每个 WHERE 占位符，改为表达式形式**
3. **测试验证**

---

## 🎉 总结

**Sqlx 增强 WHERE 语法 = 简洁 + 强大 + 零学习成本**

- ✅ 从 `{{where:id}}` 到 `{{where id=@id}}`（清晰）
- ✅ 支持 `=` `>` `<` `>=` `<=` `!=` `LIKE` `IS NULL`（灵活）
- ✅ 支持 AND/OR 组合 + 括号优先级（强大）
- ✅ 就像写普通 SQL（零学习成本）

**现在开始使用增强的 WHERE 语法吧！** 🚀

