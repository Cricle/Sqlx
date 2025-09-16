# Sqlx - 高性能 .NET ORM 源生成器

Sqlx 是一个现代化的 .NET ORM 库，专注于编译时代码生成，提供高性能的数据库访问能力。通过源生成器技术，Sqlx 在编译时生成优化的数据访问代码，避免了运行时反射的性能开销。

## ✨ 核心特性

### 🚀 编译时源生成
- **零运行时反射**: 所有代码在编译时生成，运行时性能最优
- **类型安全**: 编译时验证SQL语法和参数类型
- **智能补全**: IDE 完整支持智能提示和错误检查

### 🎯 多数据库支持
- **MySQL**: 使用 `` `column` `` 语法和 `@param` 参数
- **SQL Server**: 使用 `[column]` 语法和 `@param` 参数  
- **PostgreSQL**: 使用 `"column"` 语法和 `$param` 参数
- **SQLite**: 使用 `[column]` 语法和 `$param` 参数
- **Oracle**: 使用 `"column"` 语法和 `:param` 参数

### 🏗️ 现代 C# 特性支持
- **主构造函数 (C# 12)**: 简洁的构造函数语法
- **可空引用类型**: 完整的空安全支持
- **异步/等待**: 原生异步操作支持

## 📁 项目结构

```
Sqlx/
├── src/
│   ├── Sqlx/                    # 核心库
│   │   ├── Annotations/         # 属性注解
│   │   ├── ExpressionToSql.cs   # LINQ 表达式转 SQL
│   │   └── SqlDefine.cs         # 数据库方言定义
│   └── Sqlx.Generator/          # 源生成器
│       ├── Core/                # 核心生成逻辑
│       └── CSharpGenerator.cs   # C# 代码生成器
├── samples/                     # 示例项目
│   └── SqlxDemo/                # 完整功能演示
│   ├── Models/                  # 数据模型
│   │   ├── User.cs
│   │   └── Department.cs
│   ├── Services/               # 服务层 (主构造函数)
│   │   ├── IUserService.cs
│   │   ├── UserService.cs
│   │   ├── IDepartmentService.cs
│   │   ├── DepartmentService.cs
│   │   └── MultiDatabaseServices.cs
│   ├── Extensions/             # 扩展方法
│   │   └── DatabaseExtensions.cs
│   └── Program.cs              # 演示程序
└── tests/                      # 单元测试
    └── Sqlx.Tests/
```

## 🚀 快速开始

### 1. 安装包

```xml
<PackageReference Include="Sqlx" Version="1.0.0" />
<PackageReference Include="Sqlx.Generator" Version="1.0.0" />
```

### 2. 定义数据模型

```csharp
public record User(int Id, string Name, string Email, bool IsActive, int DepartmentId);
public record Department(int Id, string Name, decimal Budget);
```

### 3. 定义服务接口

```csharp
public interface IUserService
{
    [Sqlx("SELECT * FROM [User] WHERE [IsActive] = 1")]
    Task<IList<User>> GetActiveUsersAsync();
    
    [Sqlx("SELECT COUNT(*) FROM [User] WHERE [DepartmentId] = @departmentId")]
    Task<int> GetUserCountByDepartmentAsync(int departmentId);
}
```

### 4. 实现服务 (使用主构造函数)

```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserService(SqliteConnection connection) : IUserService
{
    // 源生成器会自动生成所有接口方法的实现
}
```

### 5. 多数据库方言支持

```csharp
// MySQL 方言
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IUserService))]
public partial class MySqlUserService(MySqlConnection connection) : IUserService
{
    // 生成MySQL语法：使用 `column` 和 @param
}

// PostgreSQL 方言
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserService))]
public partial class PostgreSqlUserService(NpgsqlConnection connection) : IUserService
{
    // 生成PostgreSQL语法：使用 "column" 和 $param
}
```

### 6. 扩展方法支持

```csharp
public static partial class DatabaseExtensions
{
    [Sqlx("SELECT COUNT(*) FROM [User] WHERE [IsActive] = 1")]
    public static partial Task<int> GetActiveUserCountAsync(this DbConnection connection);
    
    [Sqlx("SELECT AVG([Salary]) FROM [User] WHERE [IsActive] = 1")]
    public static partial Task<decimal> GetAverageSalaryAsync(this DbConnection connection);
}
```

## 🚀 运行演示

```bash
cd samples/SqlxDemo
dotnet run
```

演示程序将展示：
1. ✅ 基本源生成服务的使用
2. ✅ 多数据库方言的差异
3. ✅ 扩展方法的源生成
4. ✅ 源生成代码的性能表现

## 💡 核心优势

- **🚀 编译时生成**: 零运行时开销，无反射调用
- **🛡️ 类型安全**: 编译时检查，避免运行时错误
- **⚡ 高性能**: 直接SQL执行，无抽象层损耗
- **🔧 自动化**: 减少90%的样板代码
- **🗄️ 多数据库**: 支持多种数据库方言
- **🔒 安全**: 自动参数化查询，防止SQL注入

## 📝 生成的代码位置

编译后，可以在以下位置查看生成的代码：
```
samples/SqlxDemo/obj/Debug/net9.0/Sqlx.Generator/Sqlx.CSharpGenerator/
├── UserService.Repository.g.cs
├── DepartmentService.Repository.g.cs
├── MySqlUserService.Repository.g.cs
├── SqlServerUserService.Repository.g.cs
├── PostgreSqlUserService.Repository.g.cs
└── SqlxDemo_DatabaseExtensions.Sql.g.cs
```

这些文件包含了源生成器自动创建的完整实现代码。

## 🧪 测试

运行完整的单元测试套件：

```bash
dotnet test
```

测试覆盖了：
- ✅ 源生成器核心功能
- ✅ 多数据库方言支持
- ✅ 类型转换和映射
- ✅ 异步操作
- ✅ 错误处理
- ✅ 性能基准测试

## 📄 许可证

本项目采用 MIT 许可证 - 详情请参阅 [License.txt](License.txt) 文件。