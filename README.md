# Sqlx - 高性能 .NET 数据访问库

<div align="center">

[![NuGet](https://img.shields.io/badge/nuget-v0.5.1-blue)](https://www.nuget.org/packages/Sqlx/)
[![Tests](https://img.shields.io/badge/tests-3738%20passed-brightgreen)](tests/)
[![Coverage](https://img.shields.io/badge/coverage-88.6%25-brightgreen)](FINAL_COVERAGE_REPORT.md)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](#)

**极致性能 · 类型安全 · 完全异步 · 零配置**

[快速开始](#-快速开始) · [核心特性](#-核心特性) · [性能对比](#-性能对比) · [文档](#-文档)

</div>

---

## 💡 什么是 Sqlx？

Sqlx 是一个**高性能、类型安全的 .NET 数据访问库**，通过**源代码生成器**在编译时生成数据访问代码，实现接近 ADO.NET 的性能，同时提供优雅的 API 和强大的功能。

### 为什么选择 Sqlx？

| 特性 | Sqlx | Dapper | EF Core |
|-----|------|--------|---------|
| **性能** | ⚡⚡⚡⚡⚡ | ⚡⚡⚡⚡ | ⚡⚡⚡ |
| **内存占用** | 🟢 极低 | 🟡 低 | 🔴 高 |
| **类型安全** | ✅ 编译时 | ⚠️ 运行时 | ✅ 编译时 |
| **SQL控制** | ✅ 完全 | ✅ 完全 | ⚠️ 有限 |
| **学习曲线** | 📈 极低 | 📈 低 | 📈📈 中等 |
| **AOT支持** | ✅ 完整 | ✅ 完整 | ⚠️ 有限 |
| **批量操作** | ✅ 自动优化 | ⚠️ 手动 | ✅ 支持 |

---

## ⚡ 快速开始

### 1. 安装

```bash
dotnet add package Sqlx
```

### 2. 定义实体和仓储

```csharp
// 实体
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 仓储接口和实现
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age);
    
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<User> users);
}
```

### 3. 使用

```csharp
await using var conn = new SqliteConnection("Data Source=app.db");
await conn.OpenAsync();

var repo = new UserRepository(conn);

// 单条插入
var userId = await repo.InsertAsync("Alice", 25);
var user = await repo.GetByIdAsync(userId);

// 批量插入（自动优化）
var users = Enumerable.Range(1, 100).Select(i => new User 
{ 
    Name = $"User{i}", 
    Age = 20 + i 
});
await repo.BatchInsertAsync(users);
```

---

## 📊 性能对比

### 真实 Benchmark 数据

基于 BenchmarkDotNet 在 .NET 9.0 上的测试结果：

#### 批量插入 10 行

| 方法 | 平均耗时 | 内存分配 | 相对性能 |
|------|---------|---------|---------|
| **Sqlx (Batch)** | **118.0 μs** | **14.05 KB** | **基准** |
| Dapper (Individual) | 188.5 μs | 26.78 KB | 慢 60% |

**Sqlx 优势**: 速度快 **37%**，内存少 **48%**

#### 批量插入 100 行

| 方法 | 平均耗时 | 内存分配 | 相对性能 |
|------|---------|---------|---------|
| **Sqlx (Batch)** | **1.351 ms** | **126.31 KB** | **基准** |
| Dapper (Individual) | 1.332 ms | 251.5 KB | 相当 |

**Sqlx 优势**: 速度相当，内存少 **50%**

### 性能特点

- ✅ **编译时代码生成** - 零运行时反射开销
- ✅ **批量操作优化** - 自动合并 SQL 语句
- ✅ **内存高效** - 比 Dapper 节省 48-50% 内存
- ✅ **AOT 友好** - 完全支持 Native AOT

> 💡 **测试环境**: AMD Ryzen 7 5800H, .NET 9.0.8, Windows 10  
> 📊 **完整报告**: 查看 `tests/Sqlx.Benchmarks/` 目录

---

## 🎯 核心特性

### 1. 编译时代码生成

```csharp
// 你写的代码
[SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
Task<List<User>> GetAdultUsersAsync(int minAge);

// 生成器自动生成高性能实现
// - 零反射
// - 零动态代码
// - 完全类型安全
```

### 2. 70+ 占位符系统

跨数据库 SQL 模板，一次编写，多处运行：

```csharp
[SqlTemplate(@"
    SELECT {{columns --exclude Password}}
    FROM {{table}}
    WHERE age >= @minAge
    {{orderby created_at --desc}}
    {{limit}}
")]
Task<List<User>> QueryUsersAsync(int minAge, int? limit = null);
```

**支持的占位符**:
- `{{columns}}` - 自动列名
- `{{table}}` - 表名
- `{{where}}` - WHERE 子句
- `{{orderby}}` - ORDER BY 子句
- `{{limit}}` / `{{offset}}` - 分页
- `{{batch_values}}` - 批量插入
- `{{bool_true}}` / `{{bool_false}}` - 布尔值
- 还有 60+ 个占位符...

### 3. 多数据库支持

一套代码，4个数据库：

```csharp
// 统一接口
public interface IUnifiedRepo
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveAsync();
}

// SQLite 实现
[RepositoryFor(typeof(IUnifiedRepo), Dialect = "SQLite", TableName = "users")]
public partial class SQLiteRepo(DbConnection conn) : IUnifiedRepo { }

// PostgreSQL 实现
[RepositoryFor(typeof(IUnifiedRepo), Dialect = "PostgreSql", TableName = "users")]
public partial class PostgreSQLRepo(DbConnection conn) : IUnifiedRepo { }

// MySQL 实现
[RepositoryFor(typeof(IUnifiedRepo), Dialect = "MySql", TableName = "users")]
public partial class MySQLRepo(DbConnection conn) : IUnifiedRepo { }

// SQL Server 实现
[RepositoryFor(typeof(IUnifiedRepo), Dialect = "SqlServer", TableName = "users")]
public partial class SqlServerRepo(DbConnection conn) : IUnifiedRepo { }
```

### 4. 批量操作

自动分批处理，性能优化：

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 自动处理：
// - 1000 条数据 → 自动分成 2 批（500 + 500）
// - 自动事务管理
// - 内存优化
```

### 5. 表达式树查询

类型安全的动态查询：

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 使用
var users = await repo.QueryAsync(u => u.Age >= 18 && u.Balance > 1000);
// 生成: SELECT * FROM users WHERE age >= 18 AND balance > 1000
```

### 6. SQL 调试功能

通过返回类型获取生成的 SQL，无需执行查询：

```csharp
// 调试模式 - 返回 SqlTemplate
[SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
SqlTemplate GetAdultUsersSql(int minAge);

// 执行模式 - 返回数据
[SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
Task<List<User>> GetAdultUsersAsync(int minAge);

// 使用
var template = repo.GetAdultUsersSql(18);
Console.WriteLine(template.Sql);        // SELECT * FROM users WHERE age >= @minAge
Console.WriteLine(template.Parameters["@minAge"]);  // 18
Console.WriteLine(template.Execute().Render());     // SELECT * FROM users WHERE age >= 18
```

---

## 🗄️ 支持的数据库

| 数据库 | 状态 | 测试覆盖 | 特性支持 |
|--------|------|---------|---------|
| **SQLite** | ✅ 生产就绪 | 100% | 完整 |
| **PostgreSQL** | ✅ 生产就绪 | 100% | 完整 |
| **MySQL** | ✅ 生产就绪 | 100% | 完整 |
| **SQL Server** | ✅ 生产就绪 | 100% | 完整 |
| Oracle | 🚧 实验性 | 80% | 基础 |
| DB2 | 🚧 实验性 | 60% | 基础 |

---

## 📚 文档

### 快速入门
- **[5分钟快速开始](docs/QUICK_START_GUIDE.md)** ⭐ - 新手必读
- **[AI 助手指南](AI-VIEW.md)** ⭐ - 让 AI 学会 Sqlx（完整功能清单）
- **[文档索引](docs/index.md)** - 按主题、角色、功能分类的完整文档列表

### 核心文档
- [API 参考](docs/API_REFERENCE.md) - 完整 API 文档
- [SqlTemplate 返回类型](docs/SQL_TEMPLATE_RETURN_TYPE.md) - SQL 调试功能 ⭐
- [占位符指南](docs/PLACEHOLDERS.md) - 70+ 占位符详解
- [占位符参考](docs/PLACEHOLDER_REFERENCE.md) - 占位符速查表
- [最佳实践](docs/BEST_PRACTICES.md) - 推荐用法

### 高级特性
- [高级特性](docs/ADVANCED_FEATURES.md) - AOT、性能优化
- [统一方言指南](docs/UNIFIED_DIALECT_USAGE_GUIDE.md) - 多数据库支持
- [当前功能状态](docs/CURRENT_CAPABILITIES.md) - 实现进度

### 示例

- [TodoWebApi](samples/TodoWebApi/) - 完整 Web API 示例（包含 SqlTemplate 演示）
- [FullDemo](samples/FullDemo/) - 完整功能演示
- [集成测试](tests/Sqlx.Tests/Integration/) - 所有功能演示

---

## 🧪 测试覆盖率

Sqlx 拥有**生产级别的测试覆盖率**：

- **总测试数**: 3,738 个测试
- **核心库覆盖率**: 88.6%
- **测试通过率**: 100%
- **16 个类达到 100% 覆盖率**

详细报告: [FINAL_COVERAGE_REPORT.md](FINAL_COVERAGE_REPORT.md)

---

## 🚀 生产就绪

Sqlx 已经在多个生产环境中使用：

- ✅ **高性能**: 接近 ADO.NET 的性能
- ✅ **低内存**: 比 Dapper 节省 48-50% 内存
- ✅ **类型安全**: 编译时验证，零运行时错误
- ✅ **AOT 支持**: 完全支持 Native AOT
- ✅ **测试完善**: 3,738 个测试，88.6% 覆盖率
- ✅ **多数据库**: 支持 4 种主流数据库

---

## 🤝 贡献

欢迎贡献！请查看 [贡献指南](CONTRIBUTING.md)。

---

## 📄 许可证

[MIT License](LICENSE.txt)

---

<div align="center">

**Sqlx - 让数据访问回归简单，让性能接近极致！** 🚀

Made with ❤️ by the Sqlx Team

[GitHub](https://github.com/Cricle/Sqlx) · [NuGet](https://www.nuget.org/packages/Sqlx/) · [文档](docs/)

</div>
