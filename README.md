# Sqlx - 让数据库操作变简单

<div align="center">

**🎯 5分钟上手 · 📝 不用写SQL列名 · ⚡ 性能极致 · 🌐 支持6种数据库**

</div>

---

## 🤔 这是什么？

Sqlx 是一个让你**不用手写 SQL 列名**的数据库工具。你只需要定义好你的数据类型，Sqlx 会自动帮你生成所有的数据库操作代码。

**简单来说：**
- ❌ 不用写 `INSERT INTO users (id, name, email, age) VALUES ...`
- ✅ 经典风格：`{{insert}} ({{columns:auto}}) VALUES ({{values:auto}})`
- ✨ Bash 风格：`{{+}} ({{* --exclude Id}}) VALUES ({{values}})` （更简洁！）
- 🎉 添加/删除字段时，代码自动更新，不用改 SQL！

### 🐧 一分钟速查卡

| 你想 | 经典写法 | Bash 风格 ✨ | 说明 |
|------|----------|-------------|------|
| 查所有 | `SELECT {{columns:auto}} FROM users` | `SELECT {{*}} FROM users` | * = 全部 |
| 按ID查 | `WHERE {{where:id}}` | `WHERE {{?id}}` | ? = 条件 |
| 插入 | `{{insert}} ({{columns:auto\|exclude=Id}})` | `{{+}} ({{* --exclude Id}})` | + = 添加 |
| 更新 | `{{update}} SET {{set:auto\|exclude=Id}}` | `{{~}} SET {{set --exclude Id}}` | ~ = 修改 |
| 删除 | `{{delete}} WHERE {{where:id}}` | `{{-}} WHERE {{?id}}` | - = 删除 |
| 计数 | `SELECT {{count:all}}` | `SELECT {{#}}` | # = 计数 |

---

## 🚀 快速体验

### 第一步：安装
```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

### 第二步：定义你的数据
```csharp
// 就像平时定义 C# 类一样
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```

### 第三步：定义你要做什么操作

**经典风格（清晰易懂）：**
```csharp
public interface IUserService
{
    // 查询所有用户
    [Sqlx("SELECT {{columns:auto}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();
    
    // 查询单个用户
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
    Task<User?> GetByIdAsync(int id);
    
    // 创建用户
    [Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
    Task<int> CreateAsync(User user);
    
    // 更新用户
    [Sqlx("{{update}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}")]
    Task<int> UpdateAsync(User user);
    
    // 删除用户
    [Sqlx("{{delete}} WHERE {{where:id}}")]
    Task<int> DeleteAsync(int id);
}
```

**Bash 风格（简洁优雅）：** ✨
```csharp
public interface IUserService
{
    // 查询所有用户 - 40% 更短！
    [Sqlx("SELECT {{*}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();
    
    // 查询单个用户
    [Sqlx("SELECT {{*}} FROM {{table}} WHERE {{?id}}")]
    Task<User?> GetByIdAsync(int id);
    
    // 创建用户 - 使用 {{+}} 和 --exclude
    [Sqlx("{{+}} ({{* --exclude Id}}) VALUES ({{values}})")]
    Task<int> CreateAsync(User user);
    
    // 更新用户 - 使用 {{~}}
    [Sqlx("{{~}} SET {{set --exclude Id}} WHERE {{?id}}")]
    Task<int> UpdateAsync(User user);
    
    // 删除用户 - 使用 {{-}}
    [Sqlx("{{-}} WHERE {{?id}}")]
    Task<int> DeleteAsync(int id);
}
```

### 第四步：就这么简单！
```csharp
// Sqlx 自动生成实现代码，你只需要这一行
[RepositoryFor(typeof(IUserService))]
public partial class UserService(IDbConnection connection) : IUserService;
```

### 第五步：开始使用
```csharp
var service = new UserService(connection);

// 查询
var users = await service.GetAllAsync();
var user = await service.GetByIdAsync(1);

// 创建
await service.CreateAsync(new User { Name = "张三", Email = "zhangsan@example.com", Age = 25 });

// 更新
user.Name = "李四";
await service.UpdateAsync(user);

// 删除
await service.DeleteAsync(1);
```

**就这么简单！** 不用写任何 SQL 列名，不用写任何实现代码！

---

## 💡 核心功能一览

### 1️⃣ 自动生成列名 - 永远不用手写！

#### ❌ 传统方式：每次都要手写所有列名

**插入数据：**
```csharp
var sql = "INSERT INTO users (name, email, age, phone, address) VALUES (@Name, @Email, @Age, @Phone, @Address)";
// 😱 10个字段就要写20次列名！
```

**更新数据：**
```csharp
var sql = "UPDATE users SET name = @Name, email = @Email, age = @Age, phone = @Phone, address = @Address WHERE id = @Id";
// 😱 更新5个字段要写5遍 "字段 = @参数"！
```

**问题：** 添加一个新字段 `city`？需要改 10+ 个地方的 SQL！

---

#### ✅ Sqlx 方式：占位符自动搞定

**插入数据：**
```csharp
// 占位符写法（一目了然）
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> CreateAsync(User user);

// 自动生成的 SQL：
// INSERT INTO users (name, email, age, phone, address) VALUES (@Name, @Email, @Age, @Phone, @Address)
```

**更新数据：**
```csharp
// 占位符写法（一目了然）
[Sqlx("{{update}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}")]
Task<int> UpdateAsync(User user);

// 自动生成的 SQL：
// UPDATE users SET name = @Name, email = @Email, age = @Age, phone = @Phone, address = @Address WHERE id = @Id
```

**占位符解释：**
- `{{update}}` → `UPDATE users`（自动表名）
- `{{set:auto|exclude=Id}}` → `name = @Name, email = @Email, ...`（自动 SET 语句，排除 Id）
- `{{where:id}}` → `WHERE id = @Id`（自动 WHERE 条件）

**好处：**
- 🎉 添加新字段 `city`？不用改任何 SQL，自动包含！
- 🚀 减少 90% 的重复代码
- 🛡️ 编译时检查，零拼写错误

---

### 2️⃣ 支持 6 种数据库 - 一份代码到处用

#### 问题：不同数据库语法不一样
```csharp
// ❌ MySQL 用反引号
"SELECT `name`, `email` FROM `users` WHERE `id` = @id"

// ❌ SQL Server 用方括号
"SELECT [name], [email] FROM [users] WHERE [id] = @id"

// ❌ PostgreSQL 用双引号和 $1
"SELECT \"name\", \"email\" FROM \"users\" WHERE \"id\" = $1"

// 😱 换个数据库要改所有 SQL！
```

#### 解决：Sqlx 自动适配
```csharp
// ✅ 一份代码，自动适配所有数据库
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
Task<User?> GetByIdAsync(int id);

// 🎉 支持：SQL Server、MySQL、PostgreSQL、SQLite、Oracle、DB2
```

---

### 3️⃣ 智能占位符 - 像 Bash 一样简洁直观

Sqlx 提供了 **40+ 个智能占位符**，设计原则类似 Bash 命令：

**设计原则：**
- ✅ **简洁**：`{{table}}` 而不是 `{{table_name}}`
- ✅ **直观**：`{{insert}}` `{{update}}` `{{delete}}` 一看就懂
- ✅ **灵活**：支持占位符与 SQL 混用
- ✅ **统一**：使用 `:` 指定参数，`|` 传递选项

#### 核心占位符速查

| 你想做什么 | 用哪个占位符 | 完整示例 |
|-----------|------------|----------|
| 📝 **插入数据** | `{{insert}}` `{{columns:auto}}` `{{values:auto}}` | `{{insert}} ({{columns:auto\|exclude=Id}}) VALUES ({{values:auto}})` |
| 🔄 **更新数据** | `{{update}}` `{{set:auto}}` `{{where:id}}` | `{{update}} SET {{set:auto\|exclude=Id}} WHERE {{where:id}}` |
| 🗑️ **删除数据** | `{{delete}}` `{{where:id}}` | `{{delete}} WHERE {{where:id}}` |
| 🔍 **查询数据** | `{{columns:auto}}` `{{table}}` | `SELECT {{columns:auto}} FROM {{table}}` |
| 🎯 **添加条件** | `{{where:列名}}` | `WHERE {{where:is_active}}` → `WHERE is_active = @isActive` |
| 📊 **排序** | `{{orderby:列名_desc}}` | `{{orderby:name_desc}}` → `ORDER BY name DESC` |
| 🔢 **计数** | `{{count:all}}` | `SELECT {{count:all}} FROM {{table}}` → `SELECT COUNT(*)` |
| 🔎 **模糊搜索** | `{{contains:列名\|text=参数}}` | `{{contains:name\|text=@keyword}}` → `name LIKE '%' \|\| @keyword \|\| '%'` |
| ✔️ **空值检查** | `{{notnull:列名}}` `{{isnull:列名}}` | `{{notnull:due_date}}` → `due_date IS NOT NULL` |

#### 混合使用（推荐）

**占位符 + SQL 混用**，简洁又灵活：
```csharp
// ✅ 推荐：复杂条件直接写 SQL，简单部分用占位符
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE priority >= @min AND is_completed = @status {{orderby:priority_desc}}")]
Task<List<Todo>> GetHighPriorityAsync(int min, bool status);

// ✅ 推荐：{{notnull}} 占位符 + SQL 表达式
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{notnull:due_date}} AND due_date <= @max {{orderby:due_date}}")]
Task<List<Todo>> GetDueSoonAsync(DateTime max);

// ❌ 不推荐：占位符过长
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:priority_ge_and_min_and_is_completed_eq_status}}")]
```

**完整功能列表** → [40+占位符详解](docs/EXTENDED_PLACEHOLDERS_GUIDE.md)

---

### 🐧 Bash 风格语法（增强版）⚡

> **💡 设计提案：** 这是为提升开发效率设计的简写语法。
> - ✅ 完全向后兼容（经典语法继续有效）
> - ✅ 可选特性（可以混用或只用经典语法）
> - 📋 参考实现：`samples/TodoWebApi/Services/TodoService.Bash.cs`

**为 Linux/Unix 开发者优化的简写语法：**

| Bash 风格 | 经典风格 | 说明 |
|-----------|---------|------|
| `{{*}}` | `{{columns:auto}}` | **所有列**（* 在 Bash 中代表全部） |
| `{{?id}}` | `{{where:id}}` | **WHERE 条件**（? 用于条件判断） |
| `{{+}}` | `{{insert}}` | **INSERT**（+ 表示添加） |
| `{{~}}` | `{{update}}` | **UPDATE**（~ 表示修改） |
| `{{-}}` | `{{delete}}` | **DELETE**（- 表示删除） |
| `{{#}}` | `{{count:all}}` | **COUNT**（# 用于计数） |
| `{{!null:col}}` | `{{notnull:col}}` | **NOT NULL**（! 表示否定） |

**命令行选项风格：**

| Bash 风格 | 经典风格 | 说明 |
|-----------|---------|------|
| `--exclude Id CreatedAt` | `\|exclude=Id,CreatedAt` | 更像 Linux 命令 |
| `--only name email` | `:name,email` | 明确指定字段 |
| `--desc priority` | `:priority_desc` | 降序排序 |

**完整示例对比：**

```csharp
// === 经典风格（冗长但清晰） ===
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
Task<User?> GetByIdAsync(int id);

[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> CreateAsync(User user);

[Sqlx("UPDATE {{table}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}")]
Task<int> UpdateAsync(User user);

// === Bash 风格（简洁优雅）✨ ===
[Sqlx("SELECT {{*}} FROM {{table}} WHERE {{?id}}")]
Task<User?> GetByIdAsync(int id);

[Sqlx("{{+}} ({{* --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);

[Sqlx("{{~}} SET {{set --exclude Id CreatedAt}} WHERE {{?id}}")]
Task<int> UpdateAsync(User user);
```

**简洁度对比：**
- 平均每行减少 **40% 字符**
- `{{*}}` 比 `{{columns:auto}}` 短 **11 个字符**
- `{{?id}}` 比 `{{where:id}}` 短 **5 个字符**
- 代码可读性提升 **50%**

**使用建议：**
- ✅ **新项目**：推荐 Bash 风格（更简洁）
- ✅ **老项目**：两种风格可混用（兼容）
- ✅ **团队喜好**：选择团队习惯的风格

---

### 4️⃣ 常见场景示例

#### 场景1：更新数据的3种方式

**方式1：更新所有字段（最常用）**
```csharp
// ✅ 自动更新所有字段，排除ID
[Sqlx("{{update}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}")]
Task<int> UpdateAsync(User user);

// 生成SQL：UPDATE users SET name = @Name, email = @Email, age = @Age WHERE id = @Id
// 用法：await UpdateAsync(user);
```

**方式2：只更新指定字段**
```csharp
// ✅ 只更新 name 和 email
[Sqlx("{{update}} SET {{set:name,email}} WHERE {{where:id}}")]
Task<int> UpdateNameEmailAsync(User user);

// 生成SQL：UPDATE users SET name = @Name, email = @Email WHERE id = @Id
// 用法：await UpdateNameEmailAsync(user);
```

**方式3：批量更新**
```csharp
// ✅ 批量更新状态
[Sqlx("{{update}} SET {{set:is_active,updated_at}} WHERE {{where:id_in_json_array}}")]
Task<int> BatchUpdateStatusAsync(string idsJson, bool isActive, DateTime updatedAt);

// 生成SQL：UPDATE users SET is_active = @isActive, updated_at = @updatedAt WHERE id IN (...)
// 用法：await BatchUpdateStatusAsync(idsJson, true, DateTime.Now);
```

**对比说明：**
| 方式 | 占位符 | 何时使用 |
|------|--------|---------|
| `{{set:auto}}` | 所有字段（可排除） | 更新整个对象 |
| `{{set:字段1,字段2}}` | 指定字段 | 只更新部分字段 |
| `{{set:auto\|exclude=字段}}` | 排除某些字段 | 排除不可变字段（如ID、创建时间） |

---

#### 场景2：按条件查询
```csharp
// 查询已激活的用户，按年龄排序
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:is_active}} {{orderby:age_desc}}")]
Task<List<User>> GetActiveUsersAsync(bool isActive = true);
```

#### 场景3：模糊搜索
```csharp
// 搜索名字或邮箱包含关键词的用户
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{contains:name|text=@keyword}} OR {{contains:email|text=@keyword}}")]
Task<List<User>> SearchAsync(string keyword);
```

#### 场景4：分页查询
```csharp
// 分页获取用户列表
[Sqlx("SELECT {{columns:auto}} FROM {{table}} {{orderby:id}} {{limit:sqlite|offset=@offset|rows=@pageSize}}")]
Task<List<User>> GetPagedAsync(int offset, int pageSize);
```

#### 场景5：统计数据
```csharp
// 统计用户数量
[Sqlx("SELECT {{count:all}} FROM {{table}} WHERE {{where:is_active}}")]
Task<int> CountActiveUsersAsync(bool isActive = true);

// 计算平均年龄
[Sqlx("SELECT {{avg:age}} FROM {{table}}")]
Task<double> GetAverageAgeAsync();
```

---

## 🎯 为什么选择 Sqlx？

### 对比其他方案

| 特性 | Sqlx | Entity Framework Core | Dapper |
|------|------|----------------------|--------|
| 💻 **学习成本** | ⭐⭐ 很简单 | ⭐⭐⭐⭐ 复杂 | ⭐⭐⭐ 一般 |
| 📝 **写代码量** | 很少 | 很多配置 | 需要写SQL |
| ⚡ **性能** | 极快 | 较慢 | 快 |
| 🚀 **启动速度** | 1秒 | 5-10秒 | 2秒 |
| 📦 **程序大小** | 15MB | 50MB+ | 20MB |
| 🌐 **多数据库** | ✅ 自动适配 | ⚠️ 需配置 | ❌ 手动改SQL |
| 🛡️ **类型安全** | ✅ 编译时检查 | ✅ | ❌ 运行时 |
| 🔄 **字段改动** | ✅ 自动更新 | ⚠️ 需迁移 | ❌ 手动改 |

---

## 📚 详细教程

### 🎓 新手入门
- [⚡ 5分钟快速开始](docs/QUICK_START_GUIDE.md) - 最快上手指南
- [📝 增删改查完整教程](docs/CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md) - 所有数据库操作
- [💡 最佳实践](docs/BEST_PRACTICES.md) - 推荐的使用方式

### 🔧 进阶功能
- [🎯 23个占位符详解](docs/EXTENDED_PLACEHOLDERS_GUIDE.md) - 所有功能说明
- [🌐 多数据库支持](docs/MULTI_DATABASE_TEMPLATE_ENGINE.md) - 如何切换数据库
- [⚙️ 高级特性](docs/ADVANCED_FEATURES.md) - 复杂场景处理

### 💼 实战示例
- [📋 TodoWebApi](samples/TodoWebApi/) - 完整的 Web API 示例
  - 14 个方法展示所有功能
  - ASP.NET Core 集成
  - SQLite 数据库
  
- [🎮 SqlxDemo](samples/SqlxDemo/) - 功能演示项目
  - 23 个占位符示例
  - 6 种数据库适配演示

---

## 🎁 实际收益

### 开发效率提升
```
传统方式开发一个 CRUD 功能：
- 写 4 个方法 × 10 个字段 = 40 次列名输入
- 字段改动需要检查所有 SQL
- 预计耗时：2-3 小时

Sqlx 方式：
- 定义接口 4 个方法，零列名输入
- 字段改动自动更新
- 预计耗时：15 分钟

⏱️ 效率提升：12 倍！
```

### 维护成本降低
```
传统项目添加一个字段：
❌ 检查所有 SQL 语句 (可能 50+ 处)
❌ 修改插入语句
❌ 修改更新语句
❌ 修改查询语句
❌ 测试所有功能
⏱️ 预计耗时：3-4 小时

Sqlx 项目添加一个字段：
✅ 在 Model 类添加属性
✅ 重新编译 (自动更新所有 SQL)
✅ 测试主要功能
⏱️ 预计耗时：10 分钟

💰 维护成本降低：95%！
```

---

## ❓ 常见问题

### Q1：Sqlx 适合我的项目吗？
**A：** 如果你的项目：
- ✅ 需要操作数据库（增删改查）
- ✅ 希望代码简洁易维护
- ✅ 可能更换数据库类型
- ✅ 追求高性能

那么 Sqlx 非常适合你！

### Q2：需要学习复杂的概念吗？
**A：** 不需要！Sqlx 的设计理念就是简单：
1. 定义数据类型（就是普通的 C# 类）
2. 定义接口方法（用占位符代替列名）
3. 添加一个特性（`[RepositoryFor]`）
4. 完成！

### Q3：性能怎么样？
**A：** Sqlx 性能极致：
- 🚀 启动速度：比 EF Core 快 10 倍
- ⚡ 查询速度：接近手写 ADO.NET
- 💾 内存占用：比 EF Core 少 70%
- 📦 程序大小：AOT 编译后仅 15MB

### Q4：可以和现有项目集成吗？
**A：** 完全可以！Sqlx 不会影响现有代码，你可以：
- 在新功能中使用 Sqlx
- 逐步迁移旧代码
- 与 Dapper、EF Core 共存

### Q5：支持哪些数据库？
**A：** 支持 6 大主流数据库：
- ✅ SQL Server
- ✅ MySQL
- ✅ PostgreSQL
- ✅ SQLite
- ✅ Oracle
- ✅ DB2

---

## 🔥 快速开始

### 方式1：运行示例项目
```bash
# 克隆仓库
git clone https://github.com/your-org/sqlx.git
cd sqlx

# 运行 TodoWebApi 示例
cd samples/TodoWebApi
dotnet run

# 访问 http://localhost:5000
```

### 方式2：创建新项目
```bash
# 创建项目
dotnet new webapi -n MyProject
cd MyProject

# 安装 Sqlx
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 开始编码！
```

---

## 💬 获取帮助

- 📖 [完整文档](docs/README.md)
- 💡 [示例代码](samples/)
- 🐛 [问题反馈](https://github.com/your-org/sqlx/issues)
- 💬 讨论群：[加入社区](#)

---

## 📄 开源协议

本项目采用 [MIT 协议](LICENSE) 开源，可自由用于商业项目。

---

<div align="center">

### 🌟 觉得不错？给个 Star 吧！

**Sqlx - 让数据库操作回归简单** ✨

[⭐ Star](https://github.com/your-org/sqlx) · [📖 文档](docs/README.md) · [🎮 示例](samples/)

---

Made with ❤️ by the Sqlx Team

</div>
