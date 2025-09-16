# Sqlx 功能演示

简洁的 Sqlx 功能演示项目，展示所有核心特性。

## 🎯 演示内容

- **🔧 源生成器**: `[Sqlx]` 和 `[RepositoryFor]` 自动生成代码
- **🗄️ 多数据库方言**: MySQL、SQL Server、PostgreSQL 支持
- **🔤 扩展方法**: DbConnection 扩展方法源生成
- **🎯 Expression to SQL**: LINQ 表达式转 SQL
- **⚡ 高性能**: 编译时优化，零反射执行

## 🏗️ 项目结构

```
SqlxDemo/
├── Models/                  # 数据模型
│   ├── User.cs
│   └── Department.cs
├── Services/               # 服务层 (源生成)
│   ├── IUserService.cs     # 用户服务接口
│   ├── UserService.cs      # 用户服务实现 (partial)
│   ├── IDepartmentService.cs
│   ├── DepartmentService.cs
│   └── MultiDatabaseServices.cs
├── Extensions/             # 扩展方法 (源生成)
│   └── DatabaseExtensions.cs
├── Program.cs              # 演示程序
├── SqlxDemo.csproj         # 项目文件
└── README.md               # 本文档
```

## 🔧 源生成工作原理

### 步骤 1: 定义接口
```csharp
public interface IUserService
{
    [Sqlx("SELECT * FROM [User] WHERE [IsActive] = 1")]
    Task<IList<User>> GetActiveUsersAsync();
}
```

### 步骤 2: 使用主构造函数标记实现类
```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserService(SqliteConnection connection) : IUserService
{
    // 主构造函数参数 connection 会被源生成器自动识别
    // 源生成器自动生成接口方法的实现
}
```

### 步骤 3: 多数据库方言支持
```csharp
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserService))]
public partial class PostgreSqlUserService(SqliteConnection connection) : IUserService
{
    // 生成PostgreSQL语法：使用 "column" 和 $param
}
```

### 步骤 4: 源生成器自动生成
编译时，Sqlx 源生成器会分析代码并生成实际的实现

## 🚀 运行演示

```bash
cd SqlxDemo
dotnet build
dotnet run
```

演示程序将展示：
1. ✅ 基本源生成服务的使用
2. ✅ 多数据库方言的差异
3. ✅ 扩展方法的源生成
4. ✅ Expression to SQL 转换
5. ✅ 源生成代码的性能表现

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
obj/Debug/net9.0/Sqlx.Generator/Sqlx.CSharpGenerator/
├── UserService.Repository.g.cs
├── DepartmentService.Repository.g.cs
├── MySqlUserService.Repository.g.cs
├── SqlServerUserService.Repository.g.cs
├── PostgreSqlUserService.Repository.g.cs
└── SqlxDemo_Extensions_DatabaseExtensions.Sql.g.cs
```

这些文件包含了源生成器自动创建的完整实现代码。

## 🔗 相关文档

- [Sqlx 官方文档](../docs/README.md)
- [源生成器指南](../docs/ADVANCED_FEATURES_GUIDE.md)
- [多数据库支持](../docs/databases/)
- [Expression to SQL 指南](../docs/expression-to-sql.md)
