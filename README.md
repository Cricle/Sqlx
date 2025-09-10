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

- 📖 [新功能快速入门](docs/NEW_FEATURES_QUICK_START.md)
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