# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx.svg)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](License.txt)
[![Build Status](https://img.shields.io/github/actions/workflow/status/Cricle/Sqlx/dotnet.yml)](https://github.com/Cricle/Sqlx/actions)

**高性能、类型安全的.NET数据访问库** —— 使用Source Generator在编译时生成代码，性能接近原生ADO.NET，零反射，零运行时开销。

## ✨ 核心特性

###  🚀 **极致性能**
- **接近原生ADO.NET** - 编译时代码生成，零反射，零动态分发
- **智能优化** - 硬编码列索引、条件化`IsDBNull`检查、对象池复用
- **低GC压力** - 栈分配、零拷贝字符串、预分配容量

###  🛡️ **类型安全**
- **编译时检查** - 在IDE中直接看到错误，不是运行时才发现
- **完整Nullable支持** - `int?`、`string?`等nullable类型自动处理
- **Roslyn分析器** - 列顺序不匹配、SQL注入风险等实时警告

###  🎯 **开发体验**
- **接口驱动** - 只需定义接口和SQL模板，代码自动生成
- **多数据库支持** - SQL Server、MySQL、PostgreSQL、SQLite、Oracle
- **丰富的模板功能** - 40+占位符、正则筛选、动态列、条件逻辑

###  📊 **生产就绪**
- **Activity集成** - 内置分布式跟踪支持（OpenTelemetry兼容）
- **Partial方法** - 自定义拦截逻辑（`OnExecuting`/`OnExecuted`/`OnExecuteFail`）
- **批量操作** - 高效的批量插入、更新、删除

## 📦 快速开始

### 1. 安装
```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

### 2. 定义实体和接口

**方式1: 使用通用CRUD接口（推荐）**
```csharp
// 实体类
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
}

// 继承通用接口，立即获得8个标准CRUD方法 ✨
[RepositoryFor<User>]
[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepository : ICrudRepository<User, int>
{
    public UserRepository(DbConnection connection) { }
    
    // ✅ 已自动生成以下方法（无需手写！）：
    // - GetByIdAsync(id)            : 根据ID查询
    // - GetAllAsync(limit, offset)  : 分页查询
    // - InsertAsync(entity)         : 插入
    // - UpdateAsync(entity)         : 更新
    // - DeleteAsync(id)             : 删除
    // - CountAsync()                : 统计总数
    // - ExistsAsync(id)             : 检查存在
    // - BatchInsertAsync(entities)  : 批量插入
}
```

**方式2: 自定义接口（完全控制）**
```csharp
// 数据访问接口
public interface IUserService
{
    [Sqlx("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<User?> GetUserByIdAsync(int id);
}

[RepositoryFor<IUserService>]
[TableName("users")]
public partial class UserService : IUserService
{
    public UserService(DbConnection connection) { }
}
```

### 3. 注册服务
```csharp
builder.Services.AddScoped<IUserService>(sp => 
    new UserService(sp.GetRequiredService<DbConnection>()));
```

### 4. 使用
```csharp
var user = await userService.GetUserByIdAsync(123);
Console.WriteLine($"User: {user?.Name}");
```

## 🆚 性能对比

| 方法            | 平均耗时 | 分配内存 | 相对速度 |
|----------------|----------|----------|----------|
| **Sqlx (无追踪)** | **51.2 μs** | **2.8 KB** | **基准** |
| Raw ADO.NET    | 49.8 μs  | 2.6 KB   | -2.8%    |
| Dapper         | 55.3 μs  | 3.1 KB   | +8.0%    |
| EF Core        | 127.5 μs | 8.4 KB   | +149%    |
| Sqlx (有追踪)  | 62.1 μs  | 3.2 KB   | +21.3%   |

> **结论**: Sqlx性能接近手写ADO.NET代码，比Dapper快7%，比EF Core快近150%。

## 🎨 高级功能

### 🗂️ 通用CRUD接口

**快速开始 —— 零样板代码**

Sqlx提供了`ICrudRepository<TEntity, TKey>`通用接口，包含8个常用数据访问方法。只需继承接口，Sqlx会在编译时自动生成高性能实现代码。

```csharp
// 1️⃣ 定义实体
public class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
}

// 2️⃣ 继承通用接口（一行代码搞定！）
[RepositoryFor<Product>]
[TableName("products")]
public partial class ProductRepository : ICrudRepository<Product, int>
{
    public ProductRepository(DbConnection connection) { }
}

// 3️⃣ 使用（8个方法全自动生成）
var product = await repo.GetByIdAsync(1);           // ✅ 根据ID查询
var all = await repo.GetAllAsync(limit: 10);        // ✅ 分页查询
await repo.InsertAsync(newProduct);                 // ✅ 插入
await repo.UpdateAsync(product);                    // ✅ 更新
await repo.DeleteAsync(1);                          // ✅ 删除
var count = await repo.CountAsync();                // ✅ 统计总数
var exists = await repo.ExistsAsync(1);             // ✅ 检查存在
await repo.BatchInsertAsync(products);              // ✅ 批量插入
```

**最佳实践 SQL**

所有生成的SQL遵循最佳实践：
- ✅ **明确列名** - 不使用`SELECT *`，性能更好
- ✅ **参数化查询** - 防止SQL注入
- ✅ **索引友好** - WHERE条件使用主键
- ✅ **批量优化** - `BatchInsertAsync`使用单条INSERT多行VALUES

```sql
-- GetByIdAsync生成的SQL
SELECT id, name, price, created_at FROM products WHERE id = @id

-- GetAllAsync生成的SQL
SELECT id, name, price, created_at FROM products 
ORDER BY id LIMIT @limit OFFSET @offset

-- BatchInsertAsync生成的SQL
INSERT INTO products (name, price, created_at) VALUES 
  (@name_0, @price_0, @created_at_0),
  (@name_1, @price_1, @created_at_1),
  (@name_2, @price_2, @created_at_2)
```

**扩展自定义方法**

混合使用通用接口和自定义方法：

```csharp
public interface IProductRepository : ICrudRepository<Product, int>
{
    // ✅ 从ICrudRepository继承8个标准方法
    // ✅ 添加业务特定方法
    
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE price <= @maxPrice")]
    Task<List<Product>> GetCheapProductsAsync(decimal maxPrice);
}
```

### 📌 正则表达式列筛选
```csharp
[Sqlx("SELECT {{columns --regex ^user_}} FROM users")]
List<User> GetUserColumnsOnly();
// 生成: SELECT user_name, user_email FROM users
```

### 📦 动态返回值
```csharp
[Sqlx("SELECT {{columns --regex @pattern}} FROM {{@tableName}}")]
List<Dictionary<string, object>> GetDynamicReport(
    [DynamicSql] string tableName, 
    string pattern);
// 适用于运行时不确定的列结构
```

### 🔄 批量操作
```csharp
[Sqlx("INSERT INTO users (name, email) VALUES {{batch_values}}")]
int BatchInsert([BatchOperation] List<User> users);
// 一次性插入多行，性能优于循环插入
```

### 🎭 模板占位符
支持40+占位符：
- **基础**: `{{table}}`, `{{columns}}`, `{{values}}`, `{{where}}`, `{{set}}`
- **动态**: `{{set @param}}`, `{{orderby @param}}`, `{{join @param}}`, `{{groupby @param}}` ⚡ 零GC
- **聚合**: `{{count}}`, `{{sum}}`, `{{avg}}`, `{{max}}`, `{{min}}`
- **高级**: `{{case}}`, `{{coalesce}}`, `{{pagination}}`, `{{upsert}}`
- **日期**: `{{today}}`, `{{date_add}}`, `{{date_diff}}`
- **字符串**: `{{upper}}`, `{{lower}}`, `{{trim}}`, `{{concat}}`

**动态占位符示例**（字符串插值优化，零 Replace 调用）：
```csharp
// 动态排序
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby @sort}}")]
Task<List<Todo>> GetSortedAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string sort);

await repo.GetSortedAsync("priority DESC, created_at DESC");
// 生成: SELECT * FROM todos ORDER BY priority DESC, created_at DESC
```

查看 [完整占位符列表](docs/PLACEHOLDERS.md)

### 🔍 Activity追踪
```csharp
// 自动生成Activity，集成OpenTelemetry
using var activity = Activity.StartActivity("DB.Query");
var user = await userService.GetUserByIdAsync(123);
// Activity自动记录: SQL、参数、耗时、异常
```

### ✂️ 自定义拦截
```csharp
public partial class UserService
{
    // 可选的Partial方法
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"执行: {operationName}, SQL: {command.CommandText}");
    }

    partial void OnExecuted(string operationName, object? result, TimeSpan elapsed)
    {
        Console.WriteLine($"完成: {operationName}, 耗时: {elapsed.TotalMilliseconds}ms");
    }

    partial void OnExecuteFail(string operationName, Exception exception)
    {
        Console.WriteLine($"失败: {operationName}, 错误: {exception.Message}");
    }
}
```

## 🗃️ 多数据库支持

```csharp
// SQL Server
[Sqlx("SELECT TOP 10 * FROM users", Dialect = SqlDefine.SqlServer)]

// MySQL
[Sqlx("SELECT * FROM users LIMIT 10", Dialect = SqlDefine.MySql)]

// PostgreSQL
[Sqlx("SELECT * FROM users LIMIT 10", Dialect = SqlDefine.PostgreSql)]

// SQLite
[Sqlx("SELECT * FROM users LIMIT 10", Dialect = SqlDefine.Sqlite)]

// Oracle
[Sqlx("SELECT * FROM users WHERE ROWNUM <= 10", Dialect = SqlDefine.Oracle)]
```

## 📚 文档

- [快速入门指南](docs/QUICK_START_GUIDE.md)
- [API参考](docs/API_REFERENCE.md)
- [最佳实践](docs/BEST_PRACTICES.md)
- [高级功能](docs/ADVANCED_FEATURES.md)
- [模板占位符](docs/PLACEHOLDERS.md)
- [迁移指南](docs/MIGRATION_GUIDE.md)
- [完整文档](docs/README.md)

## 🔧 系统要求

- .NET 6.0+ / .NET Framework 4.7.2+
- C# 11.0+（用于Source Generator）
- 支持Windows、Linux、macOS

## 🤝 贡献

欢迎贡献！请查看 [贡献指南](CONTRIBUTING.md) 了解详情。

## 📄 许可证

[MIT License](License.txt)

## 🔗 相关链接

- [GitHub仓库](https://github.com/Cricle/Sqlx)
- [NuGet包](https://www.nuget.org/packages/Sqlx/)
- [在线文档](https://cricle.github.io/Sqlx/)
- [更新日志](docs/CHANGELOG.md)

---

**为什么选择Sqlx？**

✅ **性能优先** - 接近原生ADO.NET，比Dapper更快  
✅ **类型安全** - 编译时检查，IDE智能提示  
✅ **简单易用** - 只需接口和SQL，代码自动生成  
✅ **功能丰富** - 多数据库、批量操作、追踪、分析器  
✅ **生产就绪** - 完整的测试覆盖，724+单元测试  

开始使用Sqlx，享受高性能的类型安全数据访问！🚀
