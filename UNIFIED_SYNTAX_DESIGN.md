# Sqlx 统一友好语法设计

## 🎯 设计原则

**核心理念：**
1. ✅ **只有一种格式** - 避免选择困扰
2. ✅ **学习成本低** - 看名字就懂，不用查文档
3. ✅ **表达能力强** - 能表达各种复杂场景
4. ✅ **友好易懂** - 像说话一样自然

---

## ❌ 问题分析

### 当前问题

**1. 经典风格太冗长**
```csharp
{{columns:auto|exclude=Id,CreatedAt}}  // 36字符，不够友好
{{where:id}}                           // 需要记住冒号语法
```

**2. Bash风格符号难记**
```csharp
{{*}}      // * 代表什么？需要查文档
{{?id}}    // ? 为什么是where？不直观
{{+}}      // + 是insert吗？不确定
```

**3. 两种格式造成困扰**
- 用哪种？
- 能混用吗？
- 团队标准是什么？

---

## ✅ 统一方案：清晰 + 简洁

### 设计思路

**保留清晰的命名 + 友好的选项语法**

| 原则 | 说明 | 示例 |
|------|------|------|
| **清晰命名** | 用完整单词，不用符号 | `columns` `where` `insert` |
| **默认简写** | 常用参数作为默认值 | `{{columns}}` = `{{columns:auto}}` |
| **空格分隔** | 用空格代替冒号 | `{{where id}}` 而不是 `{{where:id}}` |
| **-- 选项** | 像命令行参数 | `--exclude` `--only` |
| **自然语序** | 从左到右，符合阅读习惯 | `{{columns --exclude Id}}` |

---

## 📝 统一语法规范

### 核心占位符

| 统一语法 | 说明 | 生成SQL |
|---------|------|---------|
| `{{table}}` | 表名 | `users` |
| `{{columns}}` | 所有列（默认auto） | `id, name, email, age` |
| `{{values}}` | 所有参数（默认auto） | `@Id, @Name, @Email, @Age` |
| `{{where id=@id}}` | WHERE条件（表达式） | `id = @id` |
| `{{where is_active=true}}` | WHERE条件（常量） | `is_active = 1` |
| `{{where age>=@min}}` | WHERE条件（比较） | `age >= @min` |
| `{{set}}` | SET子句（默认auto） | `name = @Name, email = @Email` |
| `{{orderby name}}` | 排序 | `ORDER BY name` |
| `{{limit 10}}` | 限制行数 | `LIMIT 10` |

### 选项语法

| 统一语法 | 说明 | 生成SQL |
|---------|------|---------|
| `{{columns --exclude Id}}` | 排除字段 | `name, email, age` |
| `{{columns --only name email}}` | 只包含字段 | `name, email` |
| `{{set --exclude Id CreatedAt}}` | SET排除字段 | `name = @Name, ...` |
| `{{orderby name --desc}}` | 降序 | `ORDER BY name DESC` |
| `{{limit 10 --offset 20}}` | 分页 | `LIMIT 10 OFFSET 20` |

### CRUD 简写（保持清晰）

| 统一语法 | 说明 |
|---------|------|
| `{{insert into}}` | INSERT INTO table |
| `{{update}}` | UPDATE table |
| `{{delete from}}` | DELETE FROM table |

---

## 🎨 完整示例

### 基础 CRUD

```csharp
// === 查询所有 ===
[Sqlx("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// === 按ID查询 ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where id=@id}}")]
Task<User?> GetByIdAsync(int id);

// === 插入 ===
[Sqlx("{{insert into}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);

// === 更新 ===
[Sqlx("{{update}} SET {{set --exclude Id CreatedAt}} WHERE {{where id=@id}}")]
Task<int> UpdateAsync(User user);

// === 删除 ===
[Sqlx("{{delete from}} WHERE {{where id=@id}}")]
Task<int> DeleteAsync(int id);
```

### 增强的 WHERE 语法 ⚡

**支持表达式和组合：**

```csharp
// === 单个条件（表达式） ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_active=@isActive}}")]
Task<List<User>> GetActiveUsersAsync(bool isActive);
// 生成：WHERE is_active = @isActive

// === 比较运算符 ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where age>=@minAge}}")]
Task<List<User>> GetAdultsAsync(int minAge = 18);
// 生成：WHERE age >= @minAge

[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where salary>@min AND salary<@max}}")]
Task<List<User>> GetSalaryRangeAsync(decimal min, decimal max);
// 生成：WHERE salary > @min AND salary < @max

// === 多个 WHERE 组合（AND） ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_active=@active}} AND {{where age>=@minAge}}")]
Task<List<User>> SearchAsync(bool active, int minAge);
// 生成：WHERE is_active = @active AND age >= @minAge

// === 多个 WHERE 组合（OR） ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where name=@name}} OR {{where email=@email}}")]
Task<User?> FindByNameOrEmailAsync(string name, string email);
// 生成：WHERE name = @name OR email = @email

// === 复杂条件组合 ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE ({{where name=@name}} OR {{where email=@email}}) AND {{where is_active=true}}")]
Task<User?> FindActiveUserAsync(string name, string email);
// 生成：WHERE (name = @name OR email = @email) AND is_active = 1

// === 常量值支持 ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where status='pending'}}")]
Task<List<User>> GetPendingUsersAsync();
// 生成：WHERE status = 'pending'

[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where is_deleted=false}}")]
Task<List<User>> GetNonDeletedAsync();
// 生成：WHERE is_deleted = 0

// === NULL 检查 ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where email IS NOT NULL}}")]
Task<List<User>> GetUsersWithEmailAsync();
// 生成：WHERE email IS NOT NULL

// === LIKE 查询 ===
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where name LIKE @pattern}}")]
Task<List<User>> SearchByNameAsync(string pattern);
// 生成：WHERE name LIKE @pattern
```

### 其他高级查询

```csharp
// 排序
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
Task<List<User>> GetAllOrderedAsync();

// 分页
[Sqlx("SELECT {{columns}} FROM {{table}} {{limit 10 --offset @skip}}")]
Task<List<User>> GetPagedAsync(int skip);

// 部分字段
[Sqlx("SELECT {{columns --only name email}} FROM {{table}}")]
Task<List<User>> GetNamesAsync();

// 聚合
[Sqlx("SELECT {{count}} FROM {{table}} WHERE {{where is_active=true}}")]
Task<int> GetActiveCountAsync();
```

---

## 📊 对比分析

### 三种方案对比

| 方案 | 示例 | 字符数 | 学习成本 | 直观度 |
|------|------|--------|---------|--------|
| **旧经典** | `{{columns:auto\|exclude=Id,CreatedAt}}` | 38 | 中 | 中 |
| **Bash符号** | `{{* --exclude Id CreatedAt}}` | 29 | 高 | 低 |
| **统一方案** | `{{columns --exclude Id CreatedAt}}` | 36 | **低** | **高** |

### 为什么选择统一方案？

| 对比项 | 旧经典 | Bash符号 | 统一方案 ✅ |
|--------|--------|----------|------------|
| **清晰度** | ✅ 好 | ❌ 符号难记 | ✅ 最好 |
| **简洁度** | ❌ 冗长 | ✅ 最短 | ⚠️ 适中 |
| **学习成本** | ⚠️ 中等 | ❌ 需记符号 | ✅ 最低 |
| **表达能力** | ✅ 强 | ⚠️ 有限 | ✅ 最强 |
| **新手友好** | ⚠️ 一般 | ❌ 不友好 | ✅ 最友好 |

**结论：** 统一方案平衡了清晰度和简洁度，最适合！

---

## 🔧 具体改进

### 改进1：默认值简化

```csharp
// ❌ 旧语法：需要显式指定 :auto
{{columns:auto}}
{{values:auto}}
{{set:auto}}

// ✅ 新语法：auto 是默认值，不用写
{{columns}}
{{values}}
{{set}}

// 💡 只在需要指定时才写参数
{{columns --only name email}}  // 明确指定
```

### 改进2：空格替代冒号

```csharp
// ❌ 旧语法：冒号不够友好
{{where:id}}
{{orderby:name_desc}}
{{count:all}}

// ✅ 新语法：空格更自然
{{where id}}
{{orderby name --desc}}
{{count}}  // all 是默认值

// 💡 读起来像英语句子
WHERE id
ORDER BY name DESC
COUNT
```

### 改进3：-- 选项语法

```csharp
// ❌ 旧语法：管道符和等号不统一
{{columns:auto|exclude=Id,CreatedAt}}
{{orderby:name_desc,created_at_desc}}

// ✅ 新语法：统一的 -- 选项
{{columns --exclude Id CreatedAt}}
{{orderby name created_at --desc}}

// 💡 像 Linux 命令行
ls --exclude *.tmp
sort --reverse
```

### 改进4：CRUD 语义化

```csharp
// ❌ 符号版本：需要记忆
{{+}}  {{~}}  {{-}}

// ✅ 单词版本：一目了然
{{insert into}}
{{update}}
{{delete from}}

// 💡 完整的SQL语义
INSERT INTO users
UPDATE users
DELETE FROM users
```

---

## 📚 迁移指南

### 从旧语法迁移

| 旧语法 | 新语法 |
|--------|--------|
| `{{columns:auto}}` | `{{columns}}` |
| `{{values:auto}}` | `{{values}}` |
| `{{set:auto}}` | `{{set}}` |
| `{{where:id}}` | `{{where id}}` |
| `{{orderby:name_desc}}` | `{{orderby name --desc}}` |
| `{{columns:auto\|exclude=Id}}` | `{{columns --exclude Id}}` |
| `{{insert}}` | `{{insert into}}` |
| `{{count:all}}` | `{{count}}` |

### 迁移步骤

1. **全局查找替换**
   ```
   {{columns:auto}} → {{columns}}
   {{values:auto}} → {{values}}
   {{where:(\w+)}} → {{where $1}}
   ```

2. **选项语法转换**
   ```
   |exclude=(\w+),(\w+) → --exclude $1 $2
   |only=(\w+),(\w+) → --only $1 $2
   ```

3. **测试验证**
   ```bash
   dotnet build
   dotnet test
   ```

---

## ✅ 最终方案

### 完整示例（TodoService）

```csharp
public interface ITodoService
{
    // 查询所有
    [Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<Todo>> GetAllAsync();
    
    // 按ID查询
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where id}}")]
    Task<Todo?> GetByIdAsync(long id);
    
    // 创建
    [Sqlx("{{insert into}} ({{columns --exclude Id}}) VALUES ({{values}}); SELECT last_insert_rowid()")]
    Task<long> CreateAsync(Todo todo);
    
    // 更新
    [Sqlx("{{update}} SET {{set --exclude Id CreatedAt}} WHERE {{where id}}")]
    Task<int> UpdateAsync(Todo todo);
    
    // 删除
    [Sqlx("{{delete from}} WHERE {{where id}}")]
    Task<int> DeleteAsync(long id);
    
    // 搜索（混合SQL）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query OR description LIKE @query")]
    Task<List<Todo>> SearchAsync(string query);
    
    // 统计
    [Sqlx("SELECT {{count}} FROM {{table}}")]
    Task<int> GetTotalCountAsync();
}
```

### 优势总结

| 特性 | 说明 |
|------|------|
| ✅ **统一** | 只有一种格式，无选择困扰 |
| ✅ **清晰** | 用完整单词，不用符号 |
| ✅ **简洁** | 默认值简化，平均减少30%字符 |
| ✅ **友好** | 空格分隔，像英语句子 |
| ✅ **强大** | -- 选项语法，表达能力强 |
| ✅ **易学** | 符合直觉，无需查文档 |

---

## 🎯 总结

**统一语法核心特点：**
1. **清晰命名** - `columns` `where` `insert` 一看就懂
2. **默认简化** - `{{columns}}` 而不是 `{{columns:auto}}`
3. **空格分隔** - `{{where id}}` 而不是 `{{where:id}}`
4. **-- 选项** - `--exclude` `--only` `--desc` 像命令行
5. **语义完整** - `{{insert into}}` `{{delete from}}` 符合SQL

**学习成本：** ⭐⭐ (5分钟上手)
**表达能力：** ⭐⭐⭐⭐⭐ (完全覆盖)
**友好程度：** ⭐⭐⭐⭐⭐ (像说话一样)

**这是最好的平衡！** 🎉

