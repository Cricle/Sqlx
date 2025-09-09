# 快速入门指南

欢迎使用 Sqlx！这个指南将帮助您在 3 分钟内上手 Sqlx，体验高性能、类型安全的数据库访问。

## 🎯 什么是 Sqlx？

Sqlx 是一个为现代 .NET 应用设计的高性能微型 ORM，具有以下特点：

- ⚡ **零反射** - 源代码生成，编译时确定所有类型
- 🔥 **极致性能** - 接近手写 ADO.NET 的速度
- 🎯 **类型安全** - 编译时检查，告别运行时错误
- 🌐 **NativeAOT 友好** - 完美支持原生编译
- 🏗️ **Repository 模式** - 自动实现接口，无需手写代码

## 📦 安装

### 使用 NuGet Package Manager

```bash
dotnet add package Sqlx
```

### 使用 Package Manager Console

```powershell
Install-Package Sqlx
```

### 使用 PackageReference

```xml
<PackageReference Include="Sqlx" Version="1.0.0" />
```

## 🚀 3分钟快速上手

### 步骤 1: 定义数据模型

```csharp
using Sqlx.Annotations;

[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 步骤 2: 定义服务接口

```csharp
using Sqlx.Annotations;

public interface IUserService
{
    // 🎯 查询操作 - 自动生成 SELECT
    [Sqlx("SELECT * FROM users")]
    IList<User> GetAllUsers();
    
    [Sqlx("SELECT * FROM users WHERE Id = @id")]
    User? GetUserById(int id);
    
    // 🎯 CRUD 操作 - 自动生成 INSERT/UPDATE/DELETE
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]  
    int UpdateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "users")]
    int DeleteUser(int id);
    
    // 🎯 异步支持
    [Sqlx("SELECT * FROM users")]
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
}
```

### 步骤 3: 创建 Repository 实现

```csharp
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 🎯 所有接口方法将自动生成高性能实现！
    // ✅ 参数化查询，防止 SQL 注入
    // ✅ 类型安全的对象映射
    // ✅ 自动连接管理
    // ✅ 异常处理和资源释放
}
```

### 步骤 4: 使用 Repository

```csharp
using Microsoft.Data.Sqlite;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建数据库连接
        using var connection = new SqliteConnection("Data Source=app.db");
        
        // 创建 Repository
        var userRepo = new UserRepository(connection);
        
        // 🚀 高性能查询，零反射！
        var users = userRepo.GetAllUsers();
        Console.WriteLine($"找到 {users.Count} 个用户");
        
        // 🚀 创建新用户
        var newUser = new User 
        { 
            Name = "张三", 
            Email = "zhangsan@example.com", 
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        
        int rowsAffected = userRepo.CreateUser(newUser);
        Console.WriteLine($"插入了 {rowsAffected} 行数据");
        
        // 🚀 异步操作
        var usersAsync = await userRepo.GetAllUsersAsync();
        Console.WriteLine($"异步查询到 {usersAsync.Count} 个用户");
        
        // 🚀 查询单个用户
        var user = userRepo.GetUserById(1);
        if (user != null)
        {
            Console.WriteLine($"用户: {user.Name} - {user.Email}");
            
            // 🚀 更新用户
            user.Email = "newemail@example.com";
            userRepo.UpdateUser(user);
            Console.WriteLine("用户信息已更新");
        }
    }
}
```

## 🎉 恭喜！

您已经成功创建了第一个 Sqlx 应用！只用了几行代码，您就获得了：

- ✅ 高性能的数据库访问
- ✅ 类型安全的查询
- ✅ 自动的 SQL 生成
- ✅ 完整的 CRUD 操作
- ✅ 异步支持

## 🔧 生成的代码是什么样的？

Sqlx 会为您的 `UserRepository` 生成类似这样的高性能代码：

```csharp
// 自动生成的代码示例
public partial class UserRepository : IUserService
{
    public IList<User> GetAllUsers()
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM users";
        
        using var reader = command.ExecuteReader();
        var results = new List<User>();
        
        while (reader.Read())
        {
            var user = new User();
            user.Id = reader.GetInt32("Id");
            user.Name = reader.GetString("Name");
            user.Email = reader.GetString("Email");
            user.CreatedAt = reader.GetDateTime("CreatedAt");
            user.IsActive = reader.GetBoolean("IsActive");
            results.Add(user);
        }
        
        return results;
    }
    
    // 其他方法的高性能实现...
}
```

## 🌟 接下来做什么？

现在您已经掌握了 Sqlx 的基础用法，可以继续探索更多功能：

### 🏗️ 进阶功能
- [Repository 模式详解](repository-pattern.md) - 深入了解 Repository 模式
- [多数据库支持](sqldefine-tablename.md) - 支持 MySQL、PostgreSQL、SQL Server
- [ExpressionToSql](expression-to-sql.md) - LINQ 表达式转 SQL

### 💡 实用示例
- [基础示例](examples/basic-examples.md) - 更多基础用法
- [高级示例](examples/advanced-examples.md) - 复杂场景应用
- [最佳实践](examples/best-practices.md) - 生产环境建议

### 🔧 配置和优化
- [性能优化](OPTIMIZATION_GUIDE.md) - 获得最佳性能
- [错误处理](error-handling.md) - 优雅的错误处理
- [事务处理](transactions.md) - 事务管理

## ❓ 需要帮助？

- 📖 [完整文档](README.md)
- 🐛 [问题报告](https://github.com/Cricle/Sqlx/issues)
- 💬 [讨论区](https://github.com/Cricle/Sqlx/discussions)
- 📧 [联系我们](mailto:support@sqlx.dev)

---

**准备好体验极致性能了吗？** 让我们继续深入探索 Sqlx 的强大功能！
