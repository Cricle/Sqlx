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
```csharp
// 实体类
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }  // Nullable支持
}

// 数据访问接口
[Sqlx("SELECT * FROM users WHERE id = @id")]
public partial interface IUserService
{
    Task<User?> GetUserByIdAsync(int id);
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
- **聚合**: `{{count}}`, `{{sum}}`, `{{avg}}`, `{{max}}`, `{{min}}`
- **高级**: `{{case}}`, `{{coalesce}}`, `{{pagination}}`, `{{upsert}}`
- **日期**: `{{today}}`, `{{date_add}}`, `{{date_diff}}`
- **字符串**: `{{upper}}`, `{{lower}}`, `{{trim}}`, `{{concat}}`

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
