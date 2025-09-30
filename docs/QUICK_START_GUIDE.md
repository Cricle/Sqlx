# ⚡ Sqlx 快速开始指南

> **目标**: 5分钟内掌握Sqlx的核心用法
> **前置条件**: .NET 8.0+ 或 .NET Standard 2.0+
> **难度**: 新手友好 🌟

---

## 🚀 第一步：安装Sqlx

### NuGet包管理器
```bash
dotnet add package Sqlx
```

### Package Manager Console
```powershell
Install-Package Sqlx
```

### PackageReference
```xml
<PackageReference Include="Sqlx" Version="3.0.0" />
```

---

## 🎯 第二步：选择使用模式

Sqlx提供三种使用模式，选择最适合您需求的方式：

### 🔥 推荐：多数据库模板引擎（最强大）
适合：需要支持多种数据库的项目

```csharp
using Sqlx.Annotations;

public interface IUserRepository
{
    // 一个模板，适配所有数据库
    [Sqlx("SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}}")]
    Task<User?> GetUserByIdAsync(int id);

    [Sqlx("SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:auto}} {{orderby:name}}")]
    Task<List<User>> GetUsersByAgeAsync(int age);
}

// 自动生成实现，支持6种数据库
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 由源生成器自动生成实现代码
}
```

### 📝 简单：直接执行（最简单）
适合：简单查询和一次性SQL

```csharp
using Sqlx;

// 参数化SQL执行
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age AND IsActive = @active",
    new { age = 25, active = true });

Console.WriteLine(sql.Render());
// 输出：SELECT * FROM Users WHERE Age > 25 AND IsActive = 1
```

### 🔄 灵活：静态模板（可重用）
适合：需要重复执行的SQL

```csharp
using Sqlx;

// 解析一次，重复使用
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age");

var young = template.Execute(new { age = 18 });
var middle = template.Execute(new { age = 35 });
var senior = template.Execute(new { age = 65 });
```

### 🛡️ 高级：类型安全动态查询（最安全）
适合：复杂查询和动态条件

```csharp
using Sqlx;

// LINQ表达式转SQL
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Email })
    .OrderBy(u => u.Name)
    .Take(10);

string sql = query.ToSql();
// 生成类型安全的SQL
```

---

## 🌟 第三步：体验多数据库支持

### 数据库方言定义
```csharp
// 支持的数据库
SqlDefine.SqlServer  // SQL Server: [column], @param
SqlDefine.MySql      // MySQL: `column`, @param
SqlDefine.PostgreSql // PostgreSQL: "column", $1
SqlDefine.SQLite     // SQLite: [column], $param
SqlDefine.Oracle     // Oracle: "column", :param
SqlDefine.DB2        // DB2: "column", ?param
```

### 同一模板，多种数据库
```csharp
// 定义模板
var template = "SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}}";

// 模板引擎
var engine = new SqlTemplateEngine();

// 不同数据库的输出
var sqlServer = engine.ProcessTemplate(template, method, entityType, "User", SqlDefine.SqlServer);
// SQL Server: SELECT [Id], [Name], [Email] FROM [User] WHERE [Id] = @id

var mysql = engine.ProcessTemplate(template, method, entityType, "User", SqlDefine.MySql);
// MySQL: SELECT `Id`, `Name`, `Email` FROM `User` WHERE `Id` = @id

var postgres = engine.ProcessTemplate(template, method, entityType, "User", SqlDefine.PostgreSql);
// PostgreSQL: SELECT "Id", "Name", "Email" FROM "User" WHERE "Id" = $1
```

---

## 💡 第四步：使用智能占位符

### 核心占位符（7个必备）
```csharp
// 基础CRUD模板
var selectTemplate = "SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}}";
var insertTemplate = "INSERT INTO {{table:quoted}} ({{columns:auto}}) VALUES ({{values}})";
var updateTemplate = "UPDATE {{table:quoted}} SET {{set:auto}} WHERE {{where:id}}";
var deleteTemplate = "DELETE FROM {{table:quoted}} WHERE {{where:id}}";

// 排序和分页
var pagedTemplate = "SELECT {{columns:auto}} FROM {{table:quoted}} {{orderby:name}} {{limit:mysql|default=20}}";
```

### 扩展占位符（15个高级功能）
```csharp
// 复杂查询模板
var complexTemplate = @"
    {{select:distinct}} {{columns:auto|exclude=Password}}
    FROM {{table:quoted}} u
    {{join:inner|table=Department d|on=u.DeptId = d.Id}}
    WHERE {{where:auto}}
    {{groupby:department}}
    {{having:count}}
    {{orderby:salary|desc}}
    {{limit:mysql|default=20}}";

// 聚合查询
var aggregateTemplate = @"
    SELECT
        {{count:all}} as TotalUsers,
        {{avg:salary}} as AvgSalary,
        {{max:age}} as MaxAge
    FROM {{table:quoted}}
    WHERE {{where:auto}}";
```

---

## 🛡️ 第五步：安全特性

### SQL注入防护
```csharp
// ✅ 安全：使用参数化查询
var safeSql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Name = @name",
    new { name = userInput });

// ❌ 危险：字符串拼接（自动检测）
var dangerousSql = $"SELECT * FROM Users WHERE Name = '{userInput}'";
// Sqlx会在编译时检测并警告
```

### 自动安全检查
```csharp
var engine = new SqlTemplateEngine();

// SQL注入检测
var maliciousTemplate = "SELECT * FROM users; DROP TABLE users; --";
var result = engine.ProcessTemplate(maliciousTemplate, null, null, "users");

if (result.Errors.Any())
{
    Console.WriteLine($"安全检查失败: {result.Errors.First()}");
    // 输出：Template contains potential SQL injection patterns
}
```

---

## 📊 第六步：性能优化技巧

### ✅ 推荐做法
```csharp
// 1. 模板重用
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age");
var result1 = template.Execute(new { age = 18 });
var result2 = template.Execute(new { age = 65 });

// 2. 使用静态缓存
private static readonly SqlTemplate UserByAgeTemplate =
    SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age");

// 3. 指定具体列（避免 SELECT *）
[Sqlx("SELECT {{columns:auto|exclude=Password}} FROM {{table:quoted}}")]
Task<List<User>> GetUsersAsync();

// 4. 使用参数化查询
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > age)  // 自动参数化
    .ToSql();
```

### ❌ 避免做法
```csharp
// 1. 每次创建新模板
var template1 = SqlTemplate.Parse(sql);  // ❌ 重复解析
var template2 = SqlTemplate.Parse(sql);  // ❌ 重复解析

// 2. 字符串拼接
var sql = $"SELECT * FROM Users WHERE Name = '{name}'";  // ❌ SQL注入风险

// 3. 在AOT中使用反射
.InsertIntoAll()  // ❌ 使用反射，AOT不兼容

// 4. 不必要的复杂查询
.Where(u => true)  // ❌ 无意义的条件
```

---

## 🎯 第七步：完整示例

### 用户管理示例
```csharp
using Sqlx;
using Sqlx.Annotations;

// 1. 定义实体
public record User(int Id, string Name, string Email, int Age, bool IsActive);

// 2. 定义仓储接口
public interface IUserRepository
{
    [Sqlx("SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}}")]
    Task<User?> GetByIdAsync(int id);

    [Sqlx("SELECT {{columns:auto}} FROM {{table:quoted}} WHERE [IsActive] = 1 {{orderby:name}}")]
    Task<List<User>> GetActiveUsersAsync();

    [Sqlx("INSERT INTO {{table:quoted}} ({{columns:auto|exclude=Id}}) VALUES ({{values|exclude=Id}})")]
    Task<int> CreateAsync(User user);

    [Sqlx("UPDATE {{table:quoted}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}")]
    Task<int> UpdateAsync(User user);
}

// 3. 实现类（由源生成器自动生成）
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    // GetByIdAsync, GetActiveUsersAsync, CreateAsync, UpdateAsync
    // 方法实现由源生成器自动生成
}

// 4. 使用示例
public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _userRepository.GetActiveUsersAsync();
    }
}
```

### 依赖注入配置
```csharp
// Program.cs 或 Startup.cs
services.AddScoped<IDbConnection>(provider =>
    new SqlConnection(connectionString));
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<UserService>();
```

---

## 🎉 第八步：运行和测试

### 编译项目
```bash
dotnet build
# 源生成器会自动生成仓储实现代码
```

### 查看生成的代码
生成的代码位置：`obj/Debug/net8.0/generated/Sqlx.Generator/`

### 运行示例
```csharp
// 创建用户
var user = new User(0, "张三", "zhang@example.com", 25, true);
var userId = await userService.CreateAsync(user);

// 查询用户
var foundUser = await userService.GetUserAsync(userId);
Console.WriteLine($"找到用户：{foundUser?.Name}");

// 获取活跃用户
var activeUsers = await userService.GetActiveUsersAsync();
Console.WriteLine($"活跃用户数：{activeUsers.Count}");
```

---

## 🔍 下一步学习

### 深入学习推荐
1. **[多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md)** - 了解核心创新功能
2. **[扩展占位符指南](EXTENDED_PLACEHOLDERS_GUIDE.md)** - 掌握22个智能占位符
3. **[API参考](API_REFERENCE.md)** - 查看完整API文档
4. **[最佳实践](BEST_PRACTICES.md)** - 学习使用技巧和优化方法

### 示例项目
查看 [SqlxDemo](../samples/SqlxDemo/) 项目，包含：
- 完整的CRUD操作示例
- 多数据库演示
- 性能测试
- AOT编译示例

```bash
cd samples/SqlxDemo
dotnet run
```

### 社区资源
- **GitHub仓库**: [https://github.com/your-repo/sqlx](https://github.com/your-repo/sqlx)
- **问题反馈**: [GitHub Issues](https://github.com/your-repo/sqlx/issues)
- **讨论交流**: [GitHub Discussions](https://github.com/your-repo/sqlx/discussions)

---

## ❓ 常见问题

### Q: Sqlx与EF Core有什么区别？
**A**: Sqlx专注于性能和AOT支持，零反射设计，性能比EF Core快3-27倍，更适合云原生和高性能场景。

### Q: 多数据库支持是如何实现的？
**A**: 通过SqlDefine方言系统和智能模板引擎，同一个模板自动适配不同数据库的语法差异。

### Q: 是否支持.NET Framework？
**A**: 支持.NET Standard 2.0+，兼容.NET Framework 4.6.1+。

### Q: 如何处理复杂查询？
**A**: 使用22个智能占位符或ExpressionToSql类型安全查询构建器。

### Q: 性能有多大提升？
**A**: 相比EF Core，简单查询快3.2倍，批量操作快26.7倍，AOT启动快26.7倍。

---

<div align="center">

**🎉 恭喜！您已完成Sqlx快速入门！**

**现在您可以开始使用Sqlx构建高性能的数据访问层了！**

**[📚 文档中心](README.md) · [🌐 多数据库引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) · [💡 最佳实践](BEST_PRACTICES.md)**

</div>
