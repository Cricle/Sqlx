# 🎮 SqlxDemo - 23个占位符功能展示

这是一个**纯演示项目**，展示 Sqlx 的**所有 23 个占位符**如何使用。

---

## 🎯 这个示例是什么？

### 一个"占位符百科全书"
- 📚 展示所有 23 个占位符
- 💡 每个占位符都有实际代码示例
- 🎓 适合学习和查阅

### 不是实际项目
- ⚠️ 这不是一个可用的应用
- ⚠️ 只是功能演示
- ✅ 想看实际项目 → [TodoWebApi](../TodoWebApi/)

---

## 🚀 快速运行

```bash
# 进入目录
cd samples/SqlxDemo

# 运行程序
dotnet run

# 看到所有占位符的演示输出
```

**运行结果：** 控制台会打印每个占位符生成的 SQL 语句

---

## 📚 占位符清单

### 🔵 核心占位符（7个）- 基础中的基础

| 占位符 | 功能 | 代码示例 |
|--------|------|---------|
| `{{table}}` | 表名 | `FROM {{table}}` |
| `{{columns:auto}}` | 所有列名 | `SELECT {{columns:auto}}` |
| `{{values:auto}}` | 所有参数 | `VALUES ({{values:auto}})` |
| `{{where}}` | WHERE 条件 | `WHERE {{where:id}}` |
| `{{set:auto}}` | SET 语句 | `SET {{set:auto}}` |
| `{{orderby}}` | 排序 | `{{orderby:name_desc}}` |
| `{{limit}}` | 分页 | `{{limit:10}}` |

### 🟢 CRUD 占位符（3个）- 简化增删改

| 占位符 | 功能 | 代码示例 |
|--------|------|---------|
| `{{insert}}` | INSERT INTO | `{{insert}} ({{columns:auto}})` |
| `{{update}}` | UPDATE | `{{update}} SET {{set:auto}}` |
| `{{delete}}` | DELETE FROM | `{{delete}} WHERE {{where:id}}` |

### 🟡 聚合函数占位符（6个）- 统计数据

| 占位符 | 功能 | 代码示例 |
|--------|------|---------|
| `{{count}}` | 计数 | `SELECT {{count:all}}` |
| `{{sum}}` | 求和 | `SELECT {{sum:salary}}` |
| `{{avg}}` | 平均值 | `SELECT {{avg:age}}` |
| `{{max}}` | 最大值 | `SELECT {{max:salary}}` |
| `{{min}}` | 最小值 | `SELECT {{min:age}}` |
| `{{distinct}}` | 去重 | `SELECT {{distinct:city}}` |

### 🟠 条件查询占位符（7个）- 复杂查询

| 占位符 | 功能 | 代码示例 |
|--------|------|---------|
| `{{like}}` | 模糊匹配 | `{{like:name|pattern=@pattern}}` |
| `{{contains}}` | 包含 | `{{contains:name|text=@keyword}}` |
| `{{startswith}}` | 开始于 | `{{startswith:name|value=@prefix}}` |
| `{{endswith}}` | 结束于 | `{{endswith:name|value=@suffix}}` |
| `{{between}}` | 范围查询 | `{{between:age|min=@min|max=@max}}` |
| `{{in}}` | IN 查询 | `{{in:id|values=@ids}}` |
| `{{not_in}}` | NOT IN | `{{not_in:id|values=@ids}}` |

### 🟣 其他实用占位符 - 特殊场景

还有 `{{join}}`、`{{groupby}}`、`{{having}}`、`{{exists}}` 等更多占位符！

---

## 💡 看懂示例代码

### 示例1：基础查询
```csharp
// 定义接口
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
Task<User?> GetByIdAsync(int id);

// 生成的 SQL（SQLite）：
// SELECT id, name, email, age FROM user WHERE id = @id
```

**解释：**
- `{{columns:auto}}` → 自动变成 `id, name, email, age`
- `{{table}}` → 自动变成 `user`（表名）
- `{{where:id}}` → 自动变成 `id = @id`

### 示例2：插入数据
```csharp
// 定义接口
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> CreateUserAsync(User user);

// 生成的 SQL：
// INSERT INTO user (name, email, age) VALUES (@Name, @Email, @Age)
```

**解释：**
- `{{insert}}` → 变成 `INSERT INTO user`
- `{{columns:auto|exclude=Id}}` → 排除 Id，生成 `name, email, age`
- `{{values:auto}}` → 生成 `@Name, @Email, @Age`

### 示例3：模糊搜索
```csharp
// 定义接口
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{contains:name|text=@keyword}}")]
Task<List<User>> SearchAsync(string keyword);

// 生成的 SQL（SQLite）：
// SELECT id, name, email, age FROM user WHERE name LIKE '%' || @keyword || '%'
```

**解释：**
- `{{contains:name|text=@keyword}}` → 自动生成 LIKE 语句
- 不同数据库语法不同，Sqlx 自动适配

---

## 🎓 学习路线

### 新手（5分钟）
1. ✅ 运行项目，看输出结果
2. ✅ 打开 `SimpleTemplateDemo.cs`
3. ✅ 看懂前 5 个方法

### 进阶（15分钟）
1. ✅ 看 `EnhancedPlaceholderDemo.cs`
2. ✅ 理解聚合函数占位符
3. ✅ 理解条件查询占位符

### 高级（30分钟）
1. ✅ 对比不同数据库的 SQL 生成结果
2. ✅ 尝试修改代码添加新方法
3. ✅ 查看生成的代码文件

---

## 📂 项目结构

```
SqlxDemo/
├── Models/
│   ├── User.cs              # 用户模型
│   └── DemoUser.cs          # 演示模型
├── Services/
│   ├── SimpleTemplateDemo.cs       # 🔵 7个核心占位符
│   ├── EnhancedPlaceholderDemo.cs  # 🟢 16个扩展占位符
│   └── DemoUserRepository.cs       # 仓储模式示例
├── Program.cs               # 主程序，运行所有演示
└── README.md                # 本文档
```

---

## 🎯 每个文件讲什么？

### SimpleTemplateDemo.cs
**展示：** 7 个核心占位符
- ✅ 增删改查的基本操作
- ✅ 条件查询
- ✅ 排序和分页

**适合：** 刚开始学习 Sqlx

### EnhancedPlaceholderDemo.cs
**展示：** 16 个扩展占位符
- ✅ 聚合函数（COUNT、SUM、AVG 等）
- ✅ 模糊搜索（LIKE、CONTAINS 等）
- ✅ 日期函数
- ✅ 字符串函数

**适合：** 进阶学习

### DemoUserRepository.cs
**展示：** 仓储模式
- ✅ 如何组织代码
- ✅ 接口定义和实现

**适合：** 实际项目参考

---

## 💪 这个示例的价值

### 1️⃣ 完整的功能清单
```
✅ 23 个占位符全覆盖
✅ 6 种数据库支持演示
✅ 每个功能都有注释说明
```

### 2️⃣ 可复制的代码片段
```
需要模糊搜索？复制这段代码：
[Sqlx("... WHERE {{contains:name|text=@keyword}}")]

需要分页？复制这段代码：
[Sqlx("... {{limit:sqlite|offset=@offset|rows=@rows}}")]
```

### 3️⃣ 学习占位符的最佳方式
```
❌ 不要：死记硬背占位符列表
✅ 推荐：运行项目，看实际效果
✅ 推荐：修改代码，试试不同参数
✅ 推荐：对比不同数据库的SQL
```

---

## 🔍 占位符速查

### 我想做...应该用哪个占位符？

| 需求 | 占位符 |
|------|--------|
| 💾 **插入数据** | `{{insert}} ({{columns:auto}}) VALUES ({{values:auto}})` |
| 🔄 **更新数据** | `{{update}} SET {{set:auto}} WHERE {{where:id}}` |
| 🗑️ **删除数据** | `{{delete}} WHERE {{where:id}}` |
| 🔍 **查询数据** | `SELECT {{columns:auto}} FROM {{table}}` |
| 🔎 **模糊搜索** | `WHERE {{contains:name|text=@keyword}}` |
| 📊 **统计数量** | `SELECT {{count:all}} FROM {{table}}` |
| 📈 **计算平均** | `SELECT {{avg:salary}} FROM {{table}}` |
| 🎯 **条件查询** | `WHERE {{where:is_active}}` |
| 📑 **排序** | `{{orderby:name_desc}}` |
| 📄 **分页** | `{{limit:10}}` 或 `{{limit:sqlite|offset=@skip|rows=@take}}` |

---

## ❓ 常见问题

### Q1：为什么有些占位符看起来很复杂？
**A：** 复杂的占位符功能更强大：
```csharp
// 简单：只指定列名
{{contains:name}}

// 复杂：指定列名和参数名
{{contains:name|text=@keyword}}

// 更复杂：指定列名、参数名和模式
{{like:name|pattern=@pattern|mode=starts}}
```

### Q2：`|exclude=Id` 是什么意思？
**A：** 这是**选项参数**，用来控制占位符行为：
```csharp
{{columns:auto}}              // 所有列
{{columns:auto|exclude=Id}}   // 排除 Id 列
{{columns:auto|exclude=Id,CreatedAt}}  // 排除多列
```

### Q3：如何知道支持哪些选项？
**A：** 看文档或者看示例代码的注释！
- [📚 完整占位符文档](../../docs/EXTENDED_PLACEHOLDERS_GUIDE.md)
- 💡 每个方法都有注释说明

### Q4：可以混用多个占位符吗？
**A：** 完全可以！
```csharp
// 同时使用 4 个占位符
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{contains:name|text=@keyword}} {{orderby:name}}")]
```

---

## 🎯 实用技巧

### 技巧1：先运行，再理解
```bash
dotnet run  # 看看输出的 SQL 是什么样的
```

### 技巧2：修改代码试试
```csharp
// 试试改改这里：
{{orderby:name}}           →  {{orderby:name_desc}}
{{columns:auto}}          →  {{columns:auto|exclude=Email}}
{{contains:name}}         →  {{contains:email}}
```

### 技巧3：对比不同数据库
```csharp
// 看看同一个占位符在不同数据库的生成结果
// SQLite:   name LIKE '%' || @keyword || '%'
// MySQL:    name LIKE CONCAT('%', @keyword, '%')
// SQL Server: name LIKE '%' + @keyword + '%'
```

---

## 📚 相关资源

### 文档
- [📘 Sqlx 主文档](../../README.md)
- [🎯 占位符完整指南](../../docs/EXTENDED_PLACEHOLDERS_GUIDE.md)
- [📝 CRUD 操作指南](../../docs/CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md)

### 示例
- [📋 TodoWebApi](../TodoWebApi/) - **推荐！** 实际项目示例
- [🎮 SqlxDemo](.) - 你现在在这里

---

## 🎉 总结

**SqlxDemo 适合：**
- 🎓 **学习者** - 看所有占位符怎么用
- 📖 **查阅者** - 忘记语法时快速查找
- 🔧 **开发者** - 复制代码片段到自己项目

**不适合：**
- ❌ 作为实际项目的模板（请看 [TodoWebApi](../TodoWebApi/)）
- ❌ 深入学习 Sqlx 架构（这是功能演示）

---

<div align="center">

### 📚 23 个占位符，助你高效开发！

[⭐ Star 项目](https://github.com/your-org/sqlx) · [📖 查看文档](../../docs/README.md) · [💡 实际示例](../TodoWebApi/)

**愿你早日掌握所有占位符！** 🚀

</div>
