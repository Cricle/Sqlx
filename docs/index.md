# Sqlx - 高性能 .NET 数据访问库

<div align="center">

[![NuGet](https://img.shields.io/badge/nuget-v0.5.0-blue)](https://www.nuget.org/packages/Sqlx/)
[![Tests](https://img.shields.io/badge/tests-1647%20passed-brightgreen)](https://github.com/Cricle/Sqlx/tree/main/tests)
[![Coverage](https://img.shields.io/badge/coverage-59.6%25-yellow)](#)
[![Production Ready](https://img.shields.io/badge/status-production%20ready-success)](#)

**极致性能 · 类型安全 · 完全异步 · 零配置**

[快速开始](#快速开始) · [核心特性](#核心特性) · [API文档](API_REFERENCE.md) · [GitHub](https://github.com/Cricle/Sqlx)

</div>

---

## 🚀 快速开始

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

// 仓储接口
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(User))]
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age);
}

// 实现类
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

### 3. 使用

```csharp
await using var conn = new SqliteConnection("Data Source=:memory:");
await conn.OpenAsync();

var repo = new UserRepository(conn);
var userId = await repo.InsertAsync("Alice", 25);
var user = await repo.GetByIdAsync(userId);
```

---

## ✨ 核心特性

### 🌐 统一方言架构

**写一次，多数据库运行** - 真正的跨数据库统一接口

```csharp
// 1个接口定义
public partial interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveUsersAsync();
}

// 4个数据库实现（只需1行配置）
[RepositoryFor(typeof(User), Dialect = "SQLite", TableName = "users")]
public partial class SQLiteUserRepository(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(User), Dialect = "PostgreSql", TableName = "users")]
public partial class PostgreSQLUserRepository(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(User), Dialect = "MySql", TableName = "users")]
public partial class MySQLUserRepository(DbConnection conn) : IUserRepository { }

[RepositoryFor(typeof(User), Dialect = "SqlServer", TableName = "users")]
public partial class SqlServerUserRepository(DbConnection conn) : IUserRepository { }
```

**测试验证**: 248个测试用例（62个测试 × 4个数据库）| **通过率**: 100% ✅

### ⚡ 极致性能

通过编译时源代码生成，接近原生 ADO.NET 性能：

| 操作 | ADO.NET | Sqlx | Dapper | EF Core |
|------|---------|------|--------|---------|
| SELECT 1000行 | 162 μs | 170 μs (1.05x) | 182 μs (1.13x) | 246 μs (1.52x) |
| INSERT 100行 | 2.01 ms | 2.18 ms (1.08x) | 2.35 ms (1.17x) | 3.82 ms (1.90x) |
| 批量插入 1000行 | - | **58.2 ms** | 225.8 ms | 185.6 ms |

### 🛡️ 类型安全

编译时验证，发现问题更早：

```csharp
// ✅ 编译时检查参数
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(long id);

// ❌ 编译错误：参数不匹配
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetByIdAsync(long id);  // 编译器报错
```

### 📝 强大的占位符系统

跨数据库SQL模板，自动适配：

| 占位符 | SQLite | PostgreSQL | MySQL | SQL Server |
|--------|--------|-----------|-------|------------|
| `{{table}}` | `[users]` | `"users"` | `` `users` `` | `[users]` |
| `{{columns}}` | `id, name` | `id, name` | `id, name` | `id, name` |
| `{{bool_true}}` | `1` | `true` | `1` | `1` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `GETDATE()` |

---

## 📚 文档导航

### 🚀 快速上手
- [快速开始指南](QUICK_START_GUIDE.md) - 5分钟上手
- [完整教程](../TUTORIAL.md) - 从入门到精通
- [快速参考](../QUICK_REFERENCE.md) - 一页纸速查

### 📖 核心文档
- [API参考](API_REFERENCE.md) - 完整API文档
- [**占位符完整参考**](PLACEHOLDER_REFERENCE.md) - **70+ 占位符速查手册** ⭐
- [占位符详细教程](PLACEHOLDERS.md) - 占位符详解
- [最佳实践](BEST_PRACTICES.md) - 推荐用法
- [高级特性](ADVANCED_FEATURES.md) - SoftDelete、AuditFields等

### 🌐 统一方言
- [统一方言使用指南](UNIFIED_DIALECT_USAGE_GUIDE.md) - 详细用法
- [统一方言状态报告](../UNIFIED_DIALECT_STATUS.md) - 实现状态
- [测试改进报告](../TEST_IMPROVEMENT_REPORT.md) - 测试覆盖

### 🔄 迁移与对比
- [迁移指南](../MIGRATION_GUIDE.md) - 从其他ORM迁移
- [性能基准测试](../PERFORMANCE.md) - 详细性能数据

### 🆘 帮助与支持
- [FAQ](../FAQ.md) - 常见问题解答
- [故障排除](../TROUBLESHOOTING.md) - 问题解决
- [贡献指南](../CONTRIBUTING.md) - 参与贡献

---

## 🎯 示例项目

### [FullFeatureDemo](https://github.com/Cricle/Sqlx/tree/main/samples/FullFeatureDemo)
完整演示所有Sqlx功能

### [TodoWebApi](https://github.com/Cricle/Sqlx/tree/main/samples/TodoWebApi)
真实Web API示例

### [UnifiedDialectDemo](https://github.com/Cricle/Sqlx/tree/main/samples/UnifiedDialectDemo)
统一方言架构演示

---

## 🗄️ 支持的数据库

| 数据库 | 状态 | 测试数 | 通过率 |
|--------|------|--------|--------|
| **SQLite** | ✅ 生产就绪 | 62 | 100% |
| **PostgreSQL** | ✅ 生产就绪 | 62 | 100% |
| **MySQL** | ✅ 生产就绪 | 62 | 100% |
| **SQL Server** | ✅ 生产就绪 | 62 | 100% |

**总计**: 248个测试用例 | **通过率**: 100% ✅

---

## 📊 项目状态

| 指标 | 值 |
|------|---|
| **测试总数** | 1647 |
| **测试通过** | 1647 (100%) |
| **代码覆盖率** | 59.6% |
| **NuGet版本** | v0.5.0 |
| **状态** | ✅ 生产就绪 |

---

## 🤝 参与贡献

欢迎贡献！请查看 [贡献指南](../CONTRIBUTING.md)。

---

## 📄 许可证

[MIT License](../LICENSE.txt)

---

## 📞 联系方式

- 🐛 问题反馈: [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
- 💬 讨论交流: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)

---

<div align="center">

**Sqlx - 让数据访问回归简单，让性能接近极致！** 🚀

Made with ❤️ by the Sqlx Team

[返回GitHub](https://github.com/Cricle/Sqlx)

</div>

