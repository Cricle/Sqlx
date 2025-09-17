# Sqlx - 现代 .NET 源生成 ORM

<div align="center">

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](License.txt)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%2B-purple.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-1200%2F1200-brightgreen.svg)](#)

**零反射 · 编译时生成 · 类型安全 · 高性能**

</div>

## ✨ 为什么选择 Sqlx？

- 🚀 **零反射开销** - 编译时生成，运行时最优性能
- 🛡️ **编译时验证** - SQL 语法和类型错误在编译期发现
- 🌐 **多数据库支持** - MySQL、SQL Server、PostgreSQL、SQLite、Oracle
- 📝 **简洁语法** - 最少的样板代码，最大的生产力

## 🏃‍♂️ 5分钟快速开始

### 1. 安装包

```xml
<PackageReference Include="Sqlx" Version="2.0.2" />
<PackageReference Include="Sqlx.Generator" Version="2.0.2" />
```

### 2. 定义模型和服务

```csharp
// 数据模型
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; }
}

// 服务接口
public interface IUserService
{
    Task<IList<User>> GetActiveUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<int> CreateUserAsync(string name, string email);
}
```

### 3. 实现服务（自动生成）

```csharp
using Sqlx.Annotations;

public partial class UserService : IUserService
{
    private readonly DbConnection _connection;
    
    public UserService(DbConnection connection) => _connection = connection;

    [Sqlx("SELECT * FROM [User] WHERE [IsActive] = 1")]
    public partial Task<IList<User>> GetActiveUsersAsync();

    [Sqlx("SELECT * FROM [User] WHERE [Id] = @id")]
    public partial Task<User?> GetUserByIdAsync(int id);

    [Sqlx("INSERT INTO [User] (Name, Email) VALUES (@name, @email)")]
    public partial Task<int> CreateUserAsync(string name, string email);
}
```

### 4. 使用

```csharp
using var connection = new SqliteConnection("Data Source=app.db");
var userService = new UserService(connection);

var users = await userService.GetActiveUsersAsync();
var user = await userService.GetUserByIdAsync(1);
await userService.CreateUserAsync("张三", "zhang@example.com");
```

## 🌐 多数据库支持

不同数据库使用不同的 SQL 方言，Sqlx 自动适配：

```csharp
// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySqlUserService : IUserService
{
    // 生成: SELECT * FROM `User` WHERE `IsActive` = @p0
}

// SQL Server  
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class SqlServerUserService : IUserService
{
    // 生成: SELECT * FROM [User] WHERE [IsActive] = @p0
}

// PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class PostgreSqlUserService : IUserService
{
    // 生成: SELECT * FROM "User" WHERE "IsActive" = $1
}
```

## 🔧 高级特性

### 仓储模式

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository
{
    private readonly IDbConnection _connection;
    public UserRepository(IDbConnection connection) => _connection = connection;
    
    // 源生成器会自动实现 IUserRepository 的所有方法
}
```

### 扩展方法

```csharp
public static partial class DatabaseExtensions
{
    [Sqlx("SELECT COUNT(*) FROM [User] WHERE [IsActive] = 1")]
    public static partial Task<int> GetActiveUserCountAsync(this DbConnection connection);
}

// 使用
var count = await connection.GetActiveUserCountAsync();
```

### 动态查询构建

```csharp
var whereClause = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive && u.Age > 18)
    .Where(u => u.Name.Contains("张"))
    .ToWhereClause();

[Sqlx($"SELECT * FROM [User] WHERE {whereClause}")]
public partial Task<IList<User>> GetFilteredUsersAsync();
```

## 📊 性能对比

| 特性 | Sqlx | Entity Framework | Dapper |
|------|------|------------------|---------|
| 反射开销 | ❌ 无 | ⚠️ 重度 | ✅ 最小 |
| 编译时验证 | ✅ 完整 | ⚠️ 部分 | ❌ 无 |
| 类型安全 | ✅ 强类型 | ✅ 强类型 | ⚠️ 弱类型 |
| 性能 | 🚀 最优 | 🐌 中等 | ⚡ 高 |

基准测试（.NET 8.0）：
```
|              Method |    Mean |
|-------------------- |--------:|
|         SqlxQuery   |  45.2 μs|
|       DapperQuery   |  48.1 μs|
| EntityFrameworkQuery| 125.7 μs|
```

## 🎯 运行演示

```bash
git clone <repository-url>
cd Sqlx/samples/SqlxDemo
dotnet run
```

演示项目展示了完整的 CRUD 操作、多数据库支持和高级特性。

## 🧪 测试

项目有完整的测试覆盖：

```bash
dotnet test  # 运行所有 1200+ 测试用例
```

## 🛠️ 环境要求

- **.NET Standard 2.0+** (支持 .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+)
- **C# 10.0+** (推荐最新版本)

## 📚 文档

- [📖 项目结构](docs/PROJECT_STRUCTURE.md)
- [🚀 高级特性](docs/ADVANCED_FEATURES_GUIDE.md)
- [🔄 迁移指南](docs/MIGRATION_GUIDE.md)
- [🎨 ExpressionToSql](docs/expression-to-sql.md)

## 🤝 贡献

欢迎贡献代码！请查看 [CONTRIBUTING.md](CONTRIBUTING.md)。

```bash
git clone <repository-url>
cd Sqlx
dotnet build
dotnet test
```

## 📄 许可证

MIT 许可证 - 详见 [License.txt](License.txt)

---

<div align="center">

**🚀 开始使用 Sqlx，体验现代 .NET 数据访问！**

[快速开始](#-5分钟快速开始) · [查看演示](#-运行演示) · [阅读文档](docs/)

</div>