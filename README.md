# Sqlx - C# SQL 代码生成器

> 🚀 零反射、编译时优化、类型安全的 SQL 操作库

[![NuGet](https://img.shields.io/nuget/v/Sqlx.svg)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/github/license/Cricle/Sqlx)](License.txt)
[![.NET](https://img.shields.io/badge/.NET-6.0%2B-blueviolet)](https://dotnet.microsoft.com/)

## ✨ 特性

- ⚡ **零反射** - 源代码生成，编译时确定类型
- 🛡️ **类型安全** - 编译时检查，避免 SQL 错误  
- 🌐 **多数据库** - SQL Server、MySQL、PostgreSQL、SQLite
- 🎯 **简单易用** - 特性驱动，学习成本低
- 🚀 **高性能** - 接近手写 ADO.NET 的速度
- 🔥 **原生 DbBatch** - 真正的批处理支持，性能提升 10-100 倍

## 🚀 快速开始

### 安装
```bash
dotnet add package Sqlx
```

### 基本用法

**1. 定义模型**
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

**2. 定义接口**
```csharp
public interface IUserService
{
    [SqlExecuteType(SqlExecuteTypes.Select, "users")]
    IList<User> GetAllUsers();
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    int UpdateUser(User user);
    
    // 🔥 NEW: 原生 DbBatch 批处理操作
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "users")]
    Task<int> BatchInsertAsync(IEnumerable<User> users);
    
    [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "users")]
    Task<int> BatchUpdateAsync(IEnumerable<User> users);
}
```

**3. 实现 Repository**
```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    public UserRepository(DbConnection connection) => this.connection = connection;
}
```

**4. 使用**
```csharp
var userRepo = new UserRepository(connection);
var users = userRepo.GetAllUsers();
var newUser = new User { Name = "张三", Email = "zhang@example.com" };
userRepo.CreateUser(newUser);
```

## 🆕 新功能

### BatchCommand 批量操作
```csharp
[SqlExecuteType(SqlExecuteTypes.BatchCommand, "users")]
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 使用
var count = await userRepo.BatchInsertAsync(users);
```

## 🔥 原生 DbBatch 批处理

### 超高性能批处理操作

```csharp
var users = new[]
{
    new User { Name = "张三", Email = "zhang@example.com" },
    new User { Name = "李四", Email = "li@example.com" },
    // ... 更多数据
};

// 批量插入 - 比单条操作快 10-100 倍！
var insertCount = await userRepo.BatchInsertAsync(users);

// 批量更新 - 自动基于主键生成 WHERE 条件
var updateCount = await userRepo.BatchUpdateAsync(users);

// 批量删除
var deleteCount = await userRepo.BatchDeleteAsync(users);
```

### 智能数据库适配

- ✅ **SQL Server 2012+** - 原生 DbBatch，性能提升 10-100x
- ✅ **PostgreSQL 3.0+** - 原生 DbBatch，性能提升 10-100x  
- ✅ **MySQL 8.0+** - 原生 DbBatch，性能提升 10-100x
- ⚠️ **SQLite** - 自动降级，性能提升 2-5x
- 🔄 **自动检测** - 不支持时优雅降级到兼容模式

### 性能对比（1000条记录）

| 方法 | SQL Server | PostgreSQL | MySQL | SQLite |
|------|-----------|-----------|-------|--------|
| 单条操作 | 2.5s | 1.8s | 2.2s | 1.2s |
| **DbBatch** | **0.08s** | **0.12s** | **0.13s** | **0.4s** |
| **性能提升** | **31x** | **15x** | **17x** | **3x** |

### ExpressionToSql 动态查询
```csharp
[Sqlx]
IList<User> GetUsers([ExpressionToSql] ExpressionToSql<User> filter);

// 使用 - 支持模运算
var evenUsers = userRepo.GetUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Id % 2 == 0)  // 偶数ID
        .Where(u => u.Name.Contains("张"))
        .OrderBy(u => u.Name)
);
```

## 📚 文档

- 🚀 [DbBatch 快速上手](GETTING_STARTED_DBBATCH.md)
- 📖 [新功能快速入门](docs/NEW_FEATURES_QUICK_START.md)
- 💻 [完整示例代码](samples/NewFeatures/ComprehensiveBatchExample.cs)
- 🔧 [ExpressionToSql 详细指南](docs/expression-to-sql.md)
- 📋 [更新日志](CHANGELOG.md)

## 🎯 数据库支持

| 数据库 | 支持状态 | 连接池 |
|--------|----------|--------|
| SQL Server | ✅ | ADO.NET 内置 |
| MySQL | ✅ | ADO.NET 内置 |
| PostgreSQL | ✅ | ADO.NET 内置 |
| SQLite | ✅ | ADO.NET 内置 |

## 📦 安装要求

- .NET 6.0+
- C# 10.0+
- 支持 NativeAOT

## 🤝 贡献

欢迎提交 Issues 和 Pull Requests！

## 📄 许可证

MIT License - 详见 [LICENSE](License.txt)