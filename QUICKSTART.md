# 🚀 Sqlx v2.0.0 - 快速开始

欢迎使用 Sqlx！这是一个5分钟快速上手指南。

---

## 📦 安装

### NuGet Package Manager
```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

### Package Manager Console
```powershell
Install-Package Sqlx
Install-Package Sqlx.Generator
```

---

## 🎯 第一个例子

### 1. 定义模型

```csharp
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string? Email { get; set; }
}
```

### 2. 定义仓储接口

```csharp
using Sqlx.Annotations;

public interface IUserRepository
{
    // SELECT查询
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    // 列表查询
    [SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
    Task<List<User>> GetByAgeAsync(int minAge);

    // INSERT操作
    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    Task<int> InsertAsync(string name, int age, string? email);

    // UPDATE操作
    [SqlTemplate("UPDATE users SET name = @name, age = @age WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name, int age);

    // DELETE操作
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteAsync(long id);
}
```

### 3. 实现仓储

```csharp
using Sqlx.Annotations;
using System.Data;

[SqlDefine(SqlDefineTypes.SQLite)]  // 或 PostgreSql, MySql, SqlServer, Oracle
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository
{
    // Sqlx会自动生成实现代码
}
```

### 4. 使用仓储

```csharp
using Microsoft.Data.Sqlite;

// 创建连接
using var connection = new SqliteConnection("Data Source=app.db");
connection.Open();

// 创建仓储实例
var userRepo = new UserRepository(connection);

// INSERT
await userRepo.InsertAsync("Alice", 25, "alice@example.com");
await userRepo.InsertAsync("Bob", 30, "bob@example.com");

// SELECT
var user = await userRepo.GetByIdAsync(1);
Console.WriteLine($"User: {user?.Name}, Age: {user?.Age}");

// LIST
var adults = await userRepo.GetByAgeAsync(18);
Console.WriteLine($"Found {adults.Count} adults");

// UPDATE
await userRepo.UpdateAsync(1, "Alice Smith", 26);

// DELETE
await userRepo.DeleteAsync(2);
```

---

## 🎨 核心特性示例

### 占位符系统

```csharp
public interface IProductRepository
{
    // {{columns}} - 动态列
    [SqlTemplate("SELECT {{columns}} FROM products WHERE category = @category")]
    Task<List<Product>> GetByCategoryAsync(string category);

    // {{where}} - 动态WHERE条件
    [SqlTemplate("SELECT * FROM products {{where}} ORDER BY price")]
    Task<List<Product>> SearchAsync(string whereClause);

    // {{limit}} {{offset}} - 分页
    [SqlTemplate("SELECT * FROM products {{limit @limit}} {{offset @offset}}")]
    Task<List<Product>> GetPagedAsync(int limit, int offset);

    // {{set}} - 动态UPDATE
    [SqlTemplate("UPDATE products {{set}} WHERE id = @id")]
    Task<int> UpdateFieldsAsync(long id, string setClause);
}
```

### 批量操作

```csharp
public interface IBatchRepository
{
    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate("INSERT INTO logs (message, level) VALUES {{batch_values}}")]
    Task<int> BatchInsertLogsAsync(IEnumerable<LogEntry> logs);
}
```

### 事务支持

```csharp
using var transaction = connection.BeginTransaction();
var repo = new UserRepository(connection) { Transaction = transaction };

try
{
    await repo.InsertAsync("User1", 20, null);
    await repo.InsertAsync("User2", 25, null);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### 返回插入ID

```csharp
public interface IUserRepository
{
    [SqlTemplate("INSERT INTO users (name) VALUES (@name)")]
    [ReturnInsertedId]
    Task<long> InsertAndGetIdAsync(string name);
}
```

---

## 🗄️ 多数据库支持

### SQLite
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepository : IUserRepository { }
```

### PostgreSQL
```csharp
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class UserRepository : IUserRepository { }
```

### MySQL
```csharp
[SqlDefine(SqlDefineTypes.MySql)]
public partial class UserRepository : IUserRepository { }
```

### SQL Server
```csharp
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class UserRepository : IUserRepository { }
```

### Oracle
```csharp
[SqlDefine(SqlDefineTypes.Oracle)]
public partial class UserRepository : IUserRepository { }
```

---

## 📖 深入学习

### 完整文档
- 📘 [API参考](docs/API_REFERENCE.md)
- 📗 [高级特性](docs/ADVANCED_FEATURES.md)
- 📙 [最佳实践](docs/BEST_PRACTICES.md)
- 📕 [占位符系统](docs/PLACEHOLDERS.md)
- 📓 [多数据库支持](docs/MULTI_DATABASE_PLACEHOLDERS.md)

### 示例项目
- 🌐 [TodoWebApi](samples/TodoWebApi/) - 完整Web API示例
- 📦 [SqlxDemo](samples/SqlxDemo/) - 基础使用示例

---

## 🎯 常见场景

### 分页查询
```csharp
[SqlTemplate(@"
    SELECT * FROM products
    WHERE category = @category
    ORDER BY created_at DESC
    LIMIT @pageSize OFFSET @offset
")]
Task<List<Product>> GetPageAsync(string category, int pageSize, int offset);
```

### 模糊搜索
```csharp
[SqlTemplate("SELECT * FROM users WHERE name LIKE @pattern")]
Task<List<User>> SearchByNameAsync(string pattern);

// 使用: await repo.SearchByNameAsync("%alice%");
```

### 聚合查询
```csharp
[SqlTemplate("SELECT COUNT(*) FROM users WHERE age >= @minAge")]
Task<int> CountAdultsAsync(int minAge);

[SqlTemplate("SELECT AVG(price) FROM products")]
Task<double> GetAveragePriceAsync();
```

### JOIN查询
```csharp
[SqlTemplate(@"
    SELECT u.id, u.name, o.order_id, o.amount
    FROM users u
    INNER JOIN orders o ON u.id = o.user_id
    WHERE u.id = @userId
")]
Task<List<UserOrder>> GetUserOrdersAsync(long userId);
```

---

## ⚡ 性能对比

Sqlx性能接近原生ADO.NET，优于其他ORM：

| 操作 | Sqlx | Dapper | EF Core |
|-----|------|--------|---------|
| SELECT (1000行) | ~170μs | ~180μs | ~350μs |
| INSERT (100行) | ~2.2ms | ~2.8ms | ~8.5ms |
| 内存分配 | 极低 | 低 | 中等 |

---

## 🛡️ 类型安全

Sqlx在编译时生成代码，提供完整的类型安全：

```csharp
// ✅ 编译时检查
var user = await repo.GetByIdAsync(1);  // 类型安全
user?.Name.ToUpper();  // 智能感知

// ❌ 编译错误
await repo.GetByIdAsync("invalid");  // 类型不匹配
await repo.NonExistentMethod();      // 方法不存在
```

---

## 💡 提示

### 1. 使用参数化查询防止SQL注入
```csharp
// ✅ 正确 - 参数化
[SqlTemplate("SELECT * FROM users WHERE name = @name")]

// ❌ 错误 - 字符串拼接
[SqlTemplate("SELECT * FROM users WHERE name = '{name}'")]
```

### 2. 异步操作
```csharp
// ✅ 推荐 - 异步
Task<User?> GetByIdAsync(long id);

// ⚠️ 同步 - 仅在必要时使用
User? GetById(long id);
```

### 3. 使用using管理连接
```csharp
using var connection = new SqliteConnection("...");
// 连接会自动关闭和释放
```

---

## 🆘 常见问题

### Q: 如何调试生成的代码？
A: 在项目文件中添加：
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
```

### Q: 支持存储过程吗？
A: 支持，使用 `[SqlTemplate("EXEC sp_name @param1, @param2")]`

### Q: 可以用原始SQL吗？
A: 完全可以，Sqlx不限制SQL的复杂度

### Q: 支持.NET Framework吗？
A: 主要支持.NET 6.0+，部分功能支持.NET Standard 2.0

---

## 🎉 开始使用

1. **安装** NuGet包
2. **定义** 接口和模型
3. **实现** 仓储类
4. **使用** 仓储进行数据访问

就这么简单！

---

## 📞 获取帮助

- 📖 [完整文档](docs/README.md)
- 🐛 [问题报告](https://github.com/Cricle/Sqlx/issues)
- 💬 [讨论区](https://github.com/Cricle/Sqlx/discussions)
- 📧 联系维护者

---

**享受使用 Sqlx！** 🚀

如果觉得有用，请给我们一个 ⭐ Star！

