# Sqlx

<div align="center">

[![NuGet](https://img.shields.io/nuget/v/Sqlx.svg)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](License.txt)
[![Build](https://img.shields.io/github/actions/workflow/status/Cricle/Sqlx/dotnet.yml)](https://github.com/Cricle/Sqlx/actions)
[![Tests](https://img.shields.io/badge/tests-1412%2F1438-brightgreen)](PROJECT_STATUS.md)
[![Coverage](https://img.shields.io/badge/coverage-95%25-brightgreen)](PROJECT_STATUS.md)

**高性能、类型安全的 .NET 数据访问库**

使用 Source Generator 在编译时生成代码 · 零反射 · 零运行时开销 · 接近原生 ADO.NET 性能

[快速开始](#-快速开始) · [文档](docs/) · [示例](samples/) · [性能](#-性能对比)

</div>

---

## ✨ 为什么选择 Sqlx？

```csharp
// 1️⃣ 定义接口 - 写 SQL 就像写字符串一样简单
public interface IUserRepository {
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
}

// 2️⃣ 实现仓储 - Source Generator 自动生成代码
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection conn) : IUserRepository { }

// 3️⃣ 使用 - 完整的类型安全和智能感知 + 真正的异步
var user = await repo.GetByIdAsync(1);
Console.WriteLine(user?.Name);  // ✅ 编译时类型检查
```

**就是这么简单！** 无需学习复杂的 LINQ 或 ORM，直接写 SQL，获得最佳性能。

---

## 🚀 核心特性

<table>
<tr>
<td width="50%">

### ⚡ 极致性能
- **~170μs** 查询1000行（接近Dapper）
- **~2.2ms** 插入100行（优于Dapper）
- **零反射** - 编译时生成
- **低GC压力** - 栈分配优化

</td>
<td width="50%">

### 🛡️ 类型安全
- **编译时检查** - IDE即时错误提示
- **完整 Nullable** - `string?` 自动处理
- **Roslyn分析器** - SQL注入警告
- **智能感知** - 完整代码提示

</td>
</tr>
<tr>
<td width="50%">

### 🎯 简单易用
- **接口驱动** - 自动生成实现
- **纯SQL模板** - 无需学习新语法
- **占位符系统** - 40+ 动态占位符
- **批量操作** - 高效批处理

</td>
<td width="50%">

### 🗄️ 多数据库
- ✅ SQLite
- ✅ PostgreSQL
- ✅ MySQL
- ✅ SQL Server
- ✅ Oracle

</td>
</tr>
<tr>
<td colspan="2">

### ⚡ 完全异步 (v1.x+)
- **真正的异步I/O** - 使用`DbCommand`/`DbConnection`，不是`Task.FromResult`包装
- **CancellationToken支持** - 自动检测并传递到所有数据库调用
- **零阻塞操作** - 更高并发能力，支持任务取消
- **向后兼容** - 只需将`IDbConnection`改为`DbConnection`

</td>
</tr>
</table>

---

## 📦 快速开始

### 安装
```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

### 30秒示例

```csharp
// 1. 定义实体
public class User {
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// 2. 定义接口（写 SQL）
public interface IUserRepo {
    [SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);

    [SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age)")]
    Task<int> InsertAsync(string name, int age);
}

// 3. 实现仓储（自动生成）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepo))]
public partial class UserRepo(DbConnection conn) : IUserRepo { }

// 4. 使用（完全异步 + 支持取消令牌）
using DbConnection conn = new SqliteConnection("Data Source=app.db");
await conn.OpenAsync();

var repo = new UserRepo(conn);

await repo.InsertAsync("Alice", 25);
await repo.InsertAsync("Bob", 17);

var adults = await repo.GetAdultsAsync(18);  // 只返回 Alice
```

**就是这么简单！** 🎉

📖 完整教程: [QUICKSTART.md](QUICKSTART.md) | 📚 详细文档: [docs/](docs/)

---

## 📈 性能对比

我们与主流 ORM 进行了基准测试：

| 操作 | Sqlx | Dapper | EF Core | ADO.NET |
|-----|------|--------|---------|---------|
| **SELECT** (1000行) | **~170μs** | ~180μs | ~350μs | ~160μs |
| **INSERT** (100行) | **~2.2ms** | ~2.8ms | ~8.5ms | ~2.0ms |
| **内存分配** | **极低** | 低 | 中等 | 极低 |
| **GC压力** | **极低** | 低 | 高 | 极低 |

✅ **Sqlx 性能接近原生 ADO.NET，优于其他 ORM**

<details>
<summary>📊 查看详细基准测试</summary>

```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4460/23H2/2023Update/SunValley3)
Intel Core i7-12700H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 9.0.100

| Method           | Library  | Mean      | Allocated |
|----------------- |--------- |----------:|----------:|
| SelectList_1000  | Sqlx     | 169.4 μs  | 43.2 KB   |
| SelectList_1000  | Dapper   | 178.6 μs  | 45.8 KB   |
| SelectList_1000  | EFCore   | 347.2 μs  | 89.4 KB   |
| BatchInsert_100  | Sqlx     | 2.21 ms   | 8.1 KB    |
| BatchInsert_100  | Dapper   | 2.78 ms   | 12.3 KB   |
| BatchInsert_100  | EFCore   | 8.52 ms   | 45.6 KB   |
```

📊 完整报告: [tests/Sqlx.Benchmarks/](tests/Sqlx.Benchmarks/)
</details>

---

## 🎨 高级特性

<table>
<tr>
<td>

**占位符系统**
```csharp
// 动态列
[SqlTemplate("SELECT {{columns}} FROM users")]
Task<List<User>> GetAllAsync();

// 动态WHERE
[SqlTemplate("SELECT * FROM users {{where @condition}}")]
Task<List<User>> SearchAsync(string condition);

// 分页
[SqlTemplate("SELECT * FROM users {{limit @size}} {{offset @skip}}")]
Task<List<User>> GetPageAsync(int size, int skip);
```

</td>
<td>

**批量操作**
```csharp
[BatchOperation(MaxBatchSize = 1000)]
[SqlTemplate("INSERT INTO logs (msg) VALUES {{batch_values}}")]
Task<int> BatchInsertAsync(IEnumerable<Log> logs);

// 自动分批，支持大数据集
await repo.BatchInsertAsync(hugeList);  // ✅ 自动分批
```

</td>
</tr>
<tr>
<td>

**事务支持**
```csharp
using var tx = await conn.BeginTransactionAsync();
var repo = new UserRepo(conn) { Transaction = tx };

await repo.InsertAsync("User1", 20);
await repo.InsertAsync("User2", 25);

await tx.CommitAsync();  // ✅ 原子操作
```

</td>
<td>

**返回插入ID**
```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name)")]
[ReturnInsertedId]
Task<long> InsertAndGetIdAsync(string name);

// 自动返回新插入的ID
var id = await repo.InsertAndGetIdAsync("Alice");
```

</td>
</tr>
<tr>
<td colspan="2">

**CancellationToken 自动支持** ⚡
```csharp
// 在接口中添加 CancellationToken 参数
public interface IUserRepo {
    [SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge, CancellationToken ct = default);
}

// 自动传递到所有数据库调用
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try {
    var users = await repo.GetAdultsAsync(18, cts.Token);  // ✅ 支持超时
} catch (OperationCanceledException) {
    // 操作被取消
}
```

</td>
</tr>
</table>

📖 更多特性: [docs/ADVANCED_FEATURES.md](docs/ADVANCED_FEATURES.md)

---

## 🗄️ 多数据库支持

只需更改 `SqlDefine` 即可切换数据库：

```csharp
// SQLite
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepo : IUserRepo { }

// PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class UserRepo : IUserRepo { }

// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
public partial class UserRepo : IUserRepo { }

// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class UserRepo : IUserRepo { }

// Oracle
[SqlDefine(SqlDefineTypes.Oracle)]
public partial class UserRepo : IUserRepo { }
```

**SQL 模板保持不变** - Sqlx 自动处理方言差异！

📖 详细说明: [docs/MULTI_DATABASE_PLACEHOLDERS.md](docs/MULTI_DATABASE_PLACEHOLDERS.md)

---

## 📚 文档

- 📄 [START_HERE.md](START_HERE.md) - 项目入口（推荐首读）
- 🚀 [QUICKSTART.md](QUICKSTART.md) - 5分钟快速上手
- 📋 [PROJECT_AT_A_GLANCE.md](PROJECT_AT_A_GLANCE.md) - 一页纸总览
- 📖 [docs/API_REFERENCE.md](docs/API_REFERENCE.md) - 完整 API 文档
- 💡 [docs/BEST_PRACTICES.md](docs/BEST_PRACTICES.md) - 最佳实践
- 🔧 [docs/PLACEHOLDERS.md](docs/PLACEHOLDERS.md) - 占位符系统
- 📊 [PROJECT_STATUS.md](PROJECT_STATUS.md) - 项目状态

---

## 🌐 示例项目

### TodoWebApi - 完整 Web API 示例
```bash
cd samples/TodoWebApi
dotnet run
# 访问 http://localhost:5000
```

**功能演示**：
- ✅ RESTful API
- ✅ CRUD 操作
- ✅ 分页和排序
- ✅ 事务处理
- ✅ 错误处理

📂 [查看源码](samples/TodoWebApi/)

---

## 🧪 测试

**1331 个测试，100% 通过，95% 覆盖率**

```bash
# 运行所有测试
dotnet test tests/Sqlx.Tests

# 运行特定分类
dotnet test --filter "TestCategory=CRUD"
dotnet test --filter "TestCategory=Advanced"

# 性能测试
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

📊 [查看测试报告](PROJECT_STATUS.md)

---

## 🔄 异步迁移指南

如果您从旧版本升级到v1.x（完全异步版本）：

### 快速迁移步骤

1. **更新连接类型**
```diff
- using IDbConnection conn = new SqliteConnection("...");
+ using DbConnection conn = new SqliteConnection("...");
```

2. **更新仓储定义**
```diff
- public partial class UserRepo(IDbConnection conn) : IUserRepo { }
+ public partial class UserRepo(DbConnection conn) : IUserRepo { }
```

3. **添加 using 语句**
```csharp
using System.Data.Common;  // 添加这行
```

4. **可选：添加 CancellationToken 支持**
```diff
- Task<User> GetUserAsync(long id);
+ Task<User> GetUserAsync(long id, CancellationToken ct = default);
```

5. **重新编译**
```bash
dotnet clean
dotnet build
```

✅ **完成！** 所有生成的代码会自动使用真正的异步API。

📖 详细迁移文档: [ASYNC_MIGRATION_SUMMARY.md](ASYNC_MIGRATION_SUMMARY.md)

---

## 🤝 贡献

欢迎贡献！请查看：
- 📖 [贡献指南](docs/PARTIAL_METHODS_GUIDE.md)
- 🐛 [问题报告](https://github.com/Cricle/Sqlx/issues)
- 💬 [讨论区](https://github.com/Cricle/Sqlx/discussions)

---

## 📜 许可证

本项目采用 [MIT](License.txt) 许可证。

---

## ⭐ 支持项目

如果 Sqlx 对您有帮助，请给我们一个 ⭐ Star！

[![GitHub stars](https://img.shields.io/github/stars/Cricle/Sqlx?style=social)](https://github.com/Cricle/Sqlx/stargazers)

---

<div align="center">

**Sqlx - 让数据访问回归简单** 🚀

[开始使用](QUICKSTART.md) · [查看文档](docs/) · [示例项目](samples/)

</div>
