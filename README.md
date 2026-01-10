# Sqlx - 高性能 .NET 数据访问库

<div align="center">

[![NuGet](https://img.shields.io/badge/nuget-v0.5.1-blue)](https://www.nuget.org/packages/Sqlx/)
[![Tests](https://img.shields.io/badge/tests-2700%2B%20passed-brightgreen)](tests/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](#)

**极致性能 · 类型安全 · 完全异步 · 零配置**

[快速开始](#-快速开始) · [核心特性](#-核心特性) · [文档](#-文档) · [示例](samples/)

</div>

---

## 💡 什么是 Sqlx？

Sqlx 是一个**高性能、类型安全的 .NET 数据访问库**，通过**源代码生成器**在编译时生成数据访问代码。

### 核心优势

| 特性 | Sqlx | Dapper | EF Core |
|-----|------|--------|---------|
| 性能 | ⚡⚡⚡⚡⚡ | ⚡⚡⚡⚡ | ⚡⚡⚡ |
| 类型安全 | ✅ 编译时 | ⚠️ 运行时 | ✅ 编译时 |
| SQL控制 | ✅ 完全 | ✅ 完全 | ⚠️ 有限 |
| 学习曲线 | 📈 极低 | 📈 低 | 📈📈 中等 |
| AOT支持 | ✅ 完整 | ✅ 完整 | ⚠️ 有限 |

---

## ⚡ 快速开始

### 方式一：使用预定义接口（推荐 - 3 行代码）⭐

Sqlx 提供了完善的预定义 CRUD 接口，包含 **50+ 个常用方法**，无需手写任何 SQL！

```csharp
// 1. 定义实体
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 2. 使用预定义接口（自动获得 15+ 个 CRUD 方法）
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class UserRepository(DbConnection connection) 
    : ICrudRepository<User, long> { }

// 3. 开始使用 - 已自动拥有所有 CRUD 方法！
await using var conn = new SqliteConnection("Data Source=app.db");
var repo = new UserRepository(conn);

var userId = await repo.InsertAndGetIdAsync(new User { Name = "Alice", Age = 25 });
var user = await repo.GetByIdAsync(userId);
var allUsers = await repo.GetAllAsync();
await repo.UpdateAsync(user);
await repo.DeleteAsync(userId);
// ... 还有 10+ 个方法可用！
```

**预定义接口包含的方法**：
- `ICrudRepository<TEntity, TKey>` - 15+ 个基础 CRUD 方法
- `IRepository<TEntity, TKey>` - 50+ 个完整方法（查询、命令、批量、聚合、高级）
- `IReadOnlyRepository<TEntity, TKey>` - 26 个只读方法（适合报表、CQRS 查询端）
- `IBulkRepository<TEntity, TKey>` - 17 个批量操作方法（高性能场景）

📖 **[查看完整的预定义接口指南](docs/PREDEFINED_INTERFACES_GUIDE.md)**

---

### 方式二：手写 SQL 模板（完全控制）

如果需要完全控制 SQL 或自定义查询，可以手写方法：

```csharp
// 仓储接口
[SqlDefine(SqlDefineTypes.SQLite)]
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("INSERT INTO users ({{columns --exclude Id}}) VALUES ({{values}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(User user);
}

// 实现类（源生成器自动生成方法）
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

**推荐做法**：结合使用预定义接口和自定义方法

```csharp
// 继承预定义接口获得基础 CRUD，再添加自定义方法
public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge = 18);
}

[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

---

## 🎯 核心特性

### 1. 编译时代码生成
- 零运行时开销
- 类型安全验证
- 接近 ADO.NET 性能

### 2. 70+ 占位符系统
跨数据库 SQL 模板：

```csharp
[SqlTemplate(@"
    SELECT {{columns --exclude Password}}
    FROM {{table}}
    WHERE age >= @minAge
    {{orderby created_at --desc}}
    {{limit}}
")]
```

### 3. 多数据库支持
一套代码，4个数据库：

```csharp
// 统一接口
public partial interface IUnifiedRepo
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
```

### 4. 批量操作
自动分批处理：

```csharp
[SqlTemplate("INSERT INTO users (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);
```

### 5. 表达式树查询

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 使用
var users = await repo.QueryAsync(u => u.Age >= 18 && u.Balance > 1000);
```

---

## 📚 文档

### 快速入门
- **[5分钟快速开始](docs/QUICK_START_GUIDE.md)** ⭐ - 新手必读
- **[预定义接口指南](docs/PREDEFINED_INTERFACES_GUIDE.md)** ⭐ - 50+ 个预定义方法，无需手写 SQL
- **[AI 助手指南](AI-VIEW.md)** ⭐ - 让 AI 学会 Sqlx（完整功能清单）
- **[文档索引](docs/INDEX.md)** - 按主题、角色、功能分类的完整文档列表

### 核心文档
- [API 参考](docs/API_REFERENCE.md) - 完整 API 文档
- [占位符指南](docs/PLACEHOLDERS.md) - 70+ 占位符详解
- [占位符参考](docs/PLACEHOLDER_REFERENCE.md) - 占位符速查表
- [最佳实践](docs/BEST_PRACTICES.md) - 推荐用法

### 高级特性
- [高级特性](docs/ADVANCED_FEATURES.md) - AOT、性能优化
- [统一方言指南](docs/UNIFIED_DIALECT_USAGE_GUIDE.md) - 多数据库支持
- [当前功能状态](docs/CURRENT_CAPABILITIES.md) - 实现进度

### 示例
- [TodoWebApi](samples/TodoWebApi/) - 完整 Web API 示例
- [集成测试](tests/Sqlx.Tests/Integration/) - 所有功能演示

---

## 🗄️ 支持的数据库

| 数据库 | 状态 | 测试覆盖 |
|--------|------|---------|
| SQLite | ✅ 生产就绪 | 100% |
| PostgreSQL | ✅ 生产就绪 | 100% |
| MySQL | ✅ 生产就绪 | 100% |
| SQL Server | ✅ 生产就绪 | 100% |

---

## 📊 性能对比

```
| Method      | Mean      | Ratio | Allocated |
|------------ |----------:|------:|----------:|
| ADO.NET     | 162.0 μs  | 1.00  | 10.1 KB   |
| Sqlx        | 170.2 μs  | 1.05  | 10.2 KB   | ⭐
| Dapper      | 182.5 μs  | 1.13  | 11.3 KB   |
| EF Core     | 245.8 μs  | 1.52  | 20.6 KB   |
```

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

</div>
