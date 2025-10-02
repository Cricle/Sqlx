# 📚 Sqlx 文档中心

欢迎来到 Sqlx 文档中心！这里提供完整的使用指南和 API 参考。

---

## 🚀 快速导航

### 📖 新手入门

| 文档 | 描述 | 预计时间 |
|------|------|----------|
| [📋 项目概览](../README.md) | Sqlx 是什么？核心特性介绍 | 3 分钟 |
| [⚡ 快速开始](QUICK_START_GUIDE.md) | 5 分钟快速上手指南 | 5 分钟 |
| [🎯 占位符指南](PLACEHOLDERS.md) | **必读**：核心占位符完整说明 | 10 分钟 |
| [🚀 TodoWebApi 示例](../samples/TodoWebApi/README.md) | 完整的 RESTful API 示例 | 15 分钟 |

### 📚 深入学习

| 文档 | 描述 | 适合人群 |
|------|------|----------|
| [📘 API 参考](API_REFERENCE.md) | 完整的 API 文档 | 开发者 |
| [💡 最佳实践](BEST_PRACTICES.md) | 推荐的使用模式和技巧 | 进阶用户 |
| [🌐 多数据库支持](MULTI_DATABASE_TEMPLATE_ENGINE.md) | 如何支持多种数据库 | 架构师 |
| [🏗️ 高级功能](ADVANCED_FEATURES.md) | 高级特性和扩展功能 | 高级用户 |
| [📦 迁移指南](MIGRATION_GUIDE.md) | 从其他 ORM 迁移到 Sqlx | 迁移用户 |

---

## 🎯 三分钟了解 Sqlx

### 什么是 Sqlx？

Sqlx 是一个轻量级、高性能的 .NET ORM 库，专注于**简单、类型安全、多数据库支持**。

### 核心特性

```csharp
// 1️⃣ 零手写列名 - 自动从实体类生成
[Sqlx("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 2️⃣ 智能占位符 - 自动生成复杂 SQL
[Sqlx("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
Task<int> UpdateAsync(User user);

// 3️⃣ 直接写 SQL - 简单清晰
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE age >= @min AND is_active = @active")]
Task<List<User>> SearchAsync(int min, bool active);

// 4️⃣ 自动实现 - 只需一行代码
[RepositoryFor(typeof(IUserService))]
public partial class UserService : IUserService;
```

### 为什么选择 Sqlx？

| 对比项 | EF Core | Dapper | Sqlx |
|--------|---------|--------|------|
| **学习曲线** | 陡峭 | 中等 | 平缓 ✅ |
| **性能** | 中等 | 高 | 高 ✅ |
| **类型安全** | ✅ | ❌ | ✅ |
| **手写 SQL** | 多 | 多 | 少 ✅ |
| **多数据库** | ✅ | ❌ | ✅ |
| **AOT 支持** | ❌ | ✅ | ✅ |

---

## 📖 核心概念

### 1. 占位符系统

Sqlx 使用**智能占位符**自动生成复杂的 SQL 内容：

| 占位符 | 作用 | 示例 |
|--------|------|------|
| `{{table}}` | 表名 | `todos` |
| `{{columns}}` | 列名列表 | `id, title, description, ...` |
| `{{values}}` | 值占位符 | `@Title, @Description, ...` |
| `{{set}}` | SET 子句 | `title=@Title, description=@Description, ...` |
| `{{orderby col --desc}}` | 排序 | `ORDER BY col DESC` |

**核心理念：**
- ✅ **智能占位符** - 用于自动生成复杂内容
- ✅ **直接写 SQL** - 用于简单清晰的内容（WHERE、COUNT 等）

👉 **详细说明**：[占位符指南](PLACEHOLDERS.md)

### 2. 多数据库支持

同一份代码，支持 6 种数据库：

```csharp
// 只需修改 SqlDefine 属性即可切换数据库
[SqlDefine(SqlDefineTypes.SQLite)]    // SQLite
[SqlDefine(SqlDefineTypes.SqlServer)]  // SQL Server
[SqlDefine(SqlDefineTypes.MySQL)]      // MySQL
[SqlDefine(SqlDefineTypes.PostgreSQL)] // PostgreSQL
[SqlDefine(SqlDefineTypes.Oracle)]     // Oracle
[SqlDefine(SqlDefineTypes.DB2)]        // DB2
```

👉 **详细说明**：[多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md)

### 3. 源代码生成

Sqlx 使用 C# 源代码生成器，在**编译时**自动生成实现代码：

```csharp
// 你写的代码（接口）
public interface IUserService
{
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
}

// Sqlx 自动生成的代码（实现）
[RepositoryFor(typeof(IUserService))]
public partial class UserService : IUserService
{
    public async Task<User?> GetByIdAsync(int id)
    {
        // 自动生成的实现代码
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age, is_active FROM users WHERE id = @id";
        cmd.Parameters.Add(new SqlParameter("@id", id));
        // ...
    }
}
```

**优势：**
- ✅ 编译时生成，零运行时开销
- ✅ 类型安全，编译时检查
- ✅ 可调试，生成的代码可见
- ✅ 支持 AOT 编译

---

## 🎓 学习路线

### 新手路线（推荐）

1. ✅ **阅读项目概览** - 了解 Sqlx 是什么
2. ✅ **快速开始** - 5 分钟上手
3. ✅ **占位符指南** - 掌握核心概念
4. ✅ **TodoWebApi 示例** - 看完整的实战项目
5. ✅ **最佳实践** - 学习推荐的使用模式

### 进阶路线

1. ✅ **API 参考** - 深入了解所有 API
2. ✅ **多数据库支持** - 学习跨数据库开发
3. ✅ **高级功能** - 探索高级特性
4. ✅ **性能优化** - 极致性能调优

---

## 📂 文档列表

### 核心文档

- [📋 README](../README.md) - 项目主页
- [⚡ 快速开始](QUICK_START_GUIDE.md) - 5 分钟快速上手
- [🎯 占位符指南](PLACEHOLDERS.md) - 核心占位符完整说明
- [📘 API 参考](API_REFERENCE.md) - 完整的 API 文档
- [💡 最佳实践](BEST_PRACTICES.md) - 推荐的使用模式

### 功能文档

- [🌐 多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) - 跨数据库支持
- [🏗️ 高级功能](ADVANCED_FEATURES.md) - 高级特性说明
- [📦 迁移指南](MIGRATION_GUIDE.md) - 从其他 ORM 迁移

### 示例项目

- [🚀 TodoWebApi](../samples/TodoWebApi/README.md) - 完整的 RESTful API 示例
- [🧪 SqlxDemo](../samples/SqlxDemo/README.md) - 功能演示项目

---

## 💡 常见问题

### Q1：Sqlx 和 Dapper 有什么区别？

| 特性 | Dapper | Sqlx |
|------|--------|------|
| **手写 SQL** | 需要手写所有列名 | 自动生成列名 ✅ |
| **类型安全** | 运行时检查 | 编译时检查 ✅ |
| **多数据库** | 需要手动适配 | 自动适配 ✅ |
| **学习成本** | 低 | 更低 ✅ |
| **性能** | 高 | 同样高 ✅ |

### Q2：Sqlx 和 EF Core 有什么区别？

| 特性 | EF Core | Sqlx |
|------|---------|------|
| **学习曲线** | 陡峭 | 平缓 ✅ |
| **性能** | 中等 | 高 ✅ |
| **控制力** | 低（黑盒） | 高（透明） ✅ |
| **SQL 可见性** | 隐藏 | 清晰可见 ✅ |
| **AOT 支持** | 有限 | 完整 ✅ |
| **复杂查询** | 简单 | 灵活 ✅ |

### Q3：需要学习多少占位符？

**只需记住 5 个核心占位符：**
- `{{table}}` - 表名
- `{{columns}}` - 列名列表
- `{{values}}` - 值占位符
- `{{set}}` - SET 子句
- `{{orderby}}` - 排序

其余内容（WHERE、COUNT 等）直接写 SQL 即可！

### Q4：支持哪些数据库？

- ✅ SQL Server
- ✅ MySQL
- ✅ PostgreSQL
- ✅ SQLite
- ✅ Oracle
- ✅ DB2

### Q5：支持 .NET 版本？

- ✅ .NET 9.0
- ✅ .NET 8.0
- ✅ .NET Standard 2.0（支持 .NET Framework 4.6.1+）

---

## 🔗 相关链接

- [📘 GitHub 仓库](https://github.com/your-org/Sqlx)
- [📦 NuGet 包](https://www.nuget.org/packages/Sqlx)
- [🐛 问题反馈](https://github.com/your-org/Sqlx/issues)
- [💬 讨论区](https://github.com/your-org/Sqlx/discussions)

---

## 🎉 开始使用

准备好了吗？让我们开始吧！

1. **安装 Sqlx**
   ```bash
   dotnet add package Sqlx
   ```

2. **阅读快速开始**
   👉 [5 分钟快速上手](QUICK_START_GUIDE.md)

3. **查看完整示例**
   👉 [TodoWebApi 示例](../samples/TodoWebApi/README.md)

---

<div align="center">

### 🚀 开始你的 Sqlx 之旅！

[📖 快速开始](QUICK_START_GUIDE.md) · [🎯 占位符指南](PLACEHOLDERS.md) · [🚀 查看示例](../samples/TodoWebApi/README.md)

</div>
