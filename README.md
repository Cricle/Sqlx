# Sqlx - 让数据库操作变简单

<div align="center">

**🎯 5分钟上手 · 📝 不用写SQL列名 · ⚡ 性能极致 · 🌐 支持6种数据库**

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Sqlx.svg)](https://www.nuget.org/packages/Sqlx/)

</div>

---

## 🤔 这是什么？

Sqlx 是一个让你**不用手写 SQL 列名**的数据库工具。你只需要定义好你的数据类型，Sqlx 会自动帮你生成所有的数据库操作代码。

**简单来说：**
- ❌ 不用写 `INSERT INTO users (id, name, email, age) VALUES ...`
- ✅ Sqlx 方式：`INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})`
- 🎉 添加/删除字段时，代码自动更新，不用改 SQL！

### ⚡ 一分钟速查

| 你想做什么 | Sqlx 写法 | 生成的 SQL |
|-----------|-----------|-----------|
| **查所有** | `SELECT {{columns}} FROM {{table}}` | `SELECT id, name, email FROM users` |
| **按ID查** | `WHERE id = @id` | `WHERE id = @id` |
| **插入** | `INSERT INTO {{table}} ({{columns --exclude Id}})` | `INSERT INTO users (name, email)` |
| **更新** | `UPDATE {{table}} SET {{set --exclude Id}}` | `UPDATE users SET name=@Name, email=@Email` |
| **删除** | `DELETE FROM {{table}} WHERE id = @id` | `DELETE FROM users WHERE id = @id` |
| **排序** | `{{orderby name --desc}}` | `ORDER BY name DESC` |

---

## 🚀 快速开始

### 安装
```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

### 示例代码

```csharp
// 1️⃣ 定义数据模型
public class Todo
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2️⃣ 定义接口
public interface ITodoRepository
{
    // 使用占位符自动生成列名
    [Sqlx("SELECT {{columns}} FROM {{table}}")]
    Task<List<Todo>> GetAllAsync();

    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Todo?> GetByIdAsync(long id);

    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}}) RETURNING id")]
    Task<long> CreateAsync(Todo todo);

    [Sqlx("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
    Task<int> UpdateAsync(Todo todo);

    [Sqlx("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}

// 3️⃣ 实现类（自动生成）
[RepositoryFor(typeof(ITodoRepository))]
[SqlDefine(
    Dialect = SqlDialect.Sqlite,
    TableName = "todos"
)]
public partial class TodoRepository(IDbConnection connection) : ITodoRepository;

// 4️⃣ 使用
var repo = new TodoRepository(connection);
var todos = await repo.GetAllAsync();
```

---

## ✨ 核心特性

### 1️⃣ 智能占位符

**核心占位符：**
- `{{table}}` - 表名（从 SqlDefine 或 TableName 特性获取）
- `{{columns}}` - 列名列表（从实体类属性自动生成）
- `{{values}}` - 参数占位符（@param1, @param2...）
- `{{set}}` - SET 子句（name=@Name, email=@Email...）
- `{{orderby}}` - ORDER BY 子句

**命令行风格选项：**
```csharp
{{columns --exclude Id}}           // 排除 Id 字段
{{columns --only Name Email}}      // 只包含指定字段
{{orderby created_at --desc}}      // 降序排序
```

### 2️⃣ 多数据库支持

一份代码，支持 6 种数据库：

```csharp
// 只需改一个枚举值
[SqlDefine(Dialect = SqlDialect.Sqlite)]     // SQLite
[SqlDefine(Dialect = SqlDialect.SqlServer)]  // SQL Server
[SqlDefine(Dialect = SqlDialect.MySql)]      // MySQL
[SqlDefine(Dialect = SqlDialect.PostgreSql)] // PostgreSQL
[SqlDefine(Dialect = SqlDialect.Oracle)]     // Oracle
[SqlDefine(Dialect = SqlDialect.DB2)]        // DB2

// 占位符自动生成适配的SQL
```

### 3️⃣ 类型安全

```csharp
// ✅ 编译时检查
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(int id);  // 参数类型匹配

// ❌ 编译错误
Task<User?> GetByIdAsync(string id);  // 编译器会报错
```

---

## ⚡ 性能优化

### 核心技术

**1. 直接序号访问**
```csharp
// ✅ Sqlx 生成
Id = reader.GetInt32(0)           // 直接数组访问，O(1)

// ❌ 传统方式
var ordinal = reader.GetOrdinal("id");  // 字符串哈希查找
Id = reader.GetInt32(ordinal);
```

**2. 零反射设计**
- 所有代码在**编译时**生成
- 运行时无反射、无IL.Emit
- 类型安全，性能接近手写代码

**3. 内存优化**
- GetOrdinal 优化减少 53% 内存分配
- 序号访问避免字符串查找开销

### 性能数据

**单行查询基准测试**（AMD Ryzen 7 5800H，.NET 8.0）：

| 方案 | 延迟 | 内存分配 | 相对速度 |
|------|------|----------|----------|
| **Raw ADO.NET** | 6.434 μs | 1.17 KB | 1.00x ⚡ |
| **Sqlx** | **7.371 μs** | **1.21 KB** | **1.15x** ✅ **（比Dapper快20%）** |
| **Dapper** | 9.241 μs | 2.25 KB | 1.44x |

**关键优势**：
- ✅ 比 Dapper **快 20%**，内存少 **46%**
- ✅ 零反射，编译时代码生成
- ✅ 类型安全，避免运行时错误
- ✅ 完整 Activity 追踪和性能指标（性能影响<0.1μs）

### 自定义拦截

使用 partial 方法扩展生成的代码：

```csharp
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository : ITodoRepository
{
    // 在SQL执行前拦截
    partial void OnExecuting(string operation, IDbCommand command)
    {
        Console.WriteLine($"执行: {command.CommandText}");
    }

    // 在SQL执行后拦截
    partial void OnExecuted(string operation, IDbCommand command, 
                            object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"完成 {operation}，耗时 {ms:F2}ms");
    }

    // 在SQL执行失败时拦截
    partial void OnExecuteFail(string operation, IDbCommand command, 
                               Exception ex, long elapsedTicks)
    {
        Console.Error.WriteLine($"失败: {ex.Message}");
    }
}
```

**特性**：
- ✅ **零开销** - 未实现时编译器自动移除
- ✅ **完全控制** - 在自己的代码中实现
- ✅ **类型安全** - 编译时检查

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

## 📚 完整示例

查看 [TodoWebApi 示例](samples/TodoWebApi/)，这是一个完整的 Todo API 应用：

- ✅ CRUD 完整实现
- ✅ SQLite 数据库
- ✅ RESTful API
- ✅ 错误处理
- ✅ Activity 追踪
- ✅ 可直接运行

```bash
cd samples/TodoWebApi
dotnet run
```

访问 `http://localhost:5000` 查看 Web UI。

---

## 📖 文档

### 快速导航
- **[文档中心](docs/)** - 所有文档的入口
- **[快速参考](docs/QUICK_REFERENCE.md)** - 一页纸速查表
- **[性能优化总结](FORCED_TRACING_SUMMARY.md)** - 详细的性能测试报告和优化历程
- **[Partial 方法指南](docs/PARTIAL_METHODS_GUIDE.md)** - 自定义拦截详解

### 核心文档
- [占位符参考](docs/PLACEHOLDERS.md)
- [最佳实践](docs/BEST_PRACTICES.md)
- [框架兼容性](docs/FRAMEWORK_COMPATIBILITY.md)
- [多数据库支持](docs/MULTI_DATABASE_TEMPLATE_ENGINE.md)
- [迁移指南](docs/MIGRATION_GUIDE.md)

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
1. 定义数据类型（普通的 C# 类）
2. 定义接口方法（用占位符代替列名）
3. 添加特性（`[RepositoryFor]`）
4. 完成！

### Q3：性能怎么样？
**A：** Sqlx 是**最快的 ORM 框架**：
- 🚀 比 Dapper **快 20%**
- ⚡ 比 Dapper 少 **46%** 内存分配
- 💾 零反射，零 IL.Emit
- 📦 编译时代码生成
- 仅比手写 ADO.NET 慢 **15%**

### Q4：可以和现有项目集成吗？
**A：** 完全可以！Sqlx 不会影响现有代码：
- 在新功能中使用 Sqlx
- 逐步迁移旧代码
- 与 Dapper、EF Core 共存

---

## 🚀 快速开始

```bash
# 创建新项目
dotnet new webapi -n MyApi
cd MyApi

# 安装 Sqlx
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 开始编码！
```

---

## 📊 运行性能测试

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

**测试覆盖**：
- 查询操作（单行、多行、全表、参数化）
- CRUD 操作（增删改查）
- 复杂查询（JOIN、聚合、排序）

**性能报告**：
- [性能优化总结](FORCED_TRACING_SUMMARY.md) - 完整的性能测试结果和优化历程

---

## 💬 获取帮助

- 📖 [文档中心](docs/) - 完整的文档导航
- 📋 [快速参考](docs/QUICK_REFERENCE.md) - 一页纸速查表
- 💡 [示例代码](samples/TodoWebApi/) - 实际使用示例
- 🐛 [问题反馈](https://github.com/Cricle/Sqlx/issues) - 提交 Bug 和建议
- ⚡ [性能测试](tests/Sqlx.Benchmarks/) - 运行 Benchmark

---

## 📄 开源协议

本项目采用 [MIT 协议](License.txt) 开源，可自由用于商业项目。

---

<div align="center">

**⭐ 如果觉得有用，请给个 Star ⭐**

[GitHub](https://github.com/Cricle/Sqlx) · [NuGet](https://www.nuget.org/packages/Sqlx/) · [文档](docs/)

Made with ❤️ by Cricle

</div>
