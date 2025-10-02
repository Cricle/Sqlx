# ⚡ Sqlx 快速开始指南

> **目标**：5 分钟掌握 Sqlx 核心用法
> **前置条件**：.NET 8.0+ 或 .NET Standard 2.0+
> **难度**：新手友好 🌟

---

## 🚀 第一步：安装 Sqlx

### 使用 dotnet CLI

```bash
dotnet add package Sqlx
```

### 使用 Package Manager Console

```powershell
Install-Package Sqlx
```

### 使用 PackageReference

```xml
<PackageReference Include="Sqlx" Version="3.0.0" />
```

---

## 🎯 第二步：定义数据模型

```csharp
using Sqlx.Annotations;

[TableName("users")]
public record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 📖 第三步：创建服务接口

```csharp
using Sqlx.Annotations;

public interface IUserService
{
    // 查询所有用户 - 自动生成列名
    [Sqlx("SELECT {{columns}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();

    // 根据ID查询 - 直接写WHERE条件
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);

    // 条件查询 - 直接写SQL
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive")]
    Task<List<User>> GetActiveUsersAsync(bool isActive);

    // 创建用户 - 自动生成列名和值占位符
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    Task<int> CreateAsync(User user);

    // 更新用户 - 自动生成SET子句
    [Sqlx("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    // 删除用户
    [Sqlx("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(int id);

    // 搜索用户 - OR条件
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE name LIKE @query OR email LIKE @query")]
    Task<List<User>> SearchAsync(string query);

    // 排序查询
    [Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}")]
    Task<List<User>> GetRecentUsersAsync();

    // 计数
    [Sqlx("SELECT COUNT(*) FROM {{table}} WHERE is_active = @isActive")]
    Task<int> CountActiveUsersAsync(bool isActive);
}
```

---

## 🔧 第四步：实现服务（自动生成）

```csharp
using System.Data;
using Sqlx.Annotations;

// Sqlx 自动生成所有方法的实现代码
[SqlDefine(SqlDefineTypes.SqlServer)]  // 或 MySQL, PostgreSQL, SQLite 等
[RepositoryFor(typeof(IUserService))]
public partial class UserService(IDbConnection connection) : IUserService;
```

**就这么简单！** 无需编写任何实现代码，Sqlx 在编译时自动生成。

---

## 🎓 第五步：使用服务

```csharp
using Microsoft.Data.SqlClient;

// 创建数据库连接
var connection = new SqlConnection("your-connection-string");

// 创建服务实例
var userService = new UserService(connection);

// 查询所有用户
var allUsers = await userService.GetAllAsync();

// 根据ID查询
var user = await userService.GetByIdAsync(1);

// 创建新用户
var newUser = new User
{
    Name = "张三",
    Email = "zhangsan@example.com",
    Age = 25,
    IsActive = true,
    CreatedAt = DateTime.Now
};
var created = await userService.CreateAsync(newUser);

// 更新用户
user.Age = 26;
await userService.UpdateAsync(user);

// 搜索用户
var results = await userService.SearchAsync("%zhang%");

// 计数
var count = await userService.CountActiveUsersAsync(true);

// 删除用户
await userService.DeleteAsync(1);
```

---

## 🌟 核心特性详解

### 1. 智能占位符

Sqlx 使用智能占位符自动生成复杂的 SQL 内容：

| 占位符 | 作用 | 示例 |
|--------|------|------|
| `{{table}}` | 表名 | `users` |
| `{{columns}}` | 列名列表 | `id, name, email, age, is_active, created_at` |
| `{{columns --exclude Id}}` | 排除列 | `name, email, age, is_active, created_at` |
| `{{values}}` | 值占位符 | `@Name, @Email, @Age, @IsActive, @CreatedAt` |
| `{{set}}` | SET子句 | `name=@Name, email=@Email, age=@Age, ...` |
| `{{set --exclude Id}}` | SET排除列 | `name=@Name, email=@Email, age=@Age, ...` |
| `{{orderby col --desc}}` | 排序 | `ORDER BY col DESC` |

### 2. 直接写 SQL

简单的内容直接写 SQL 更清晰：

```csharp
// WHERE 条件 - 直接写
WHERE id = @id
WHERE is_active = @isActive
WHERE age >= @minAge AND is_active = true

// 聚合函数 - 直接写
SELECT COUNT(*) FROM {{table}}
SELECT SUM(amount) FROM {{table}}
SELECT AVG(age) FROM {{table}}

// INSERT/UPDATE/DELETE - 直接写关键字
INSERT INTO {{table}} ...
UPDATE {{table}} ...
DELETE FROM {{table}} ...
```

### 3. 多数据库支持

同一份代码，支持 6 种数据库：

```csharp
[SqlDefine(SqlDefineTypes.SqlServer)]   // SQL Server
[SqlDefine(SqlDefineTypes.MySQL)]       // MySQL
[SqlDefine(SqlDefineTypes.PostgreSQL)]  // PostgreSQL
[SqlDefine(SqlDefineTypes.SQLite)]      // SQLite
[SqlDefine(SqlDefineTypes.Oracle)]      // Oracle
[SqlDefine(SqlDefineTypes.DB2)]         // DB2
```

---

## 💡 常见场景

### 场景1：复杂条件查询

```csharp
// 多条件 AND
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND age <= @maxAge AND is_active = @isActive")]
Task<List<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge, bool isActive);

// 多条件 OR
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE name = @name OR email = @email")]
Task<User?> FindByNameOrEmailAsync(string name, string email);

// 组合条件
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE (name LIKE @query OR email LIKE @query) AND is_active = true")]
Task<List<User>> SearchActiveUsersAsync(string query);
```

### 场景2：批量操作

```csharp
// 批量更新（使用JSON数组）
[Sqlx("UPDATE {{table}} SET {{set --only is_active}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> ActivateUsersAsync(string idsJson, bool isActive);

// 批量删除
[Sqlx("DELETE FROM {{table}} WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> DeleteUsersAsync(string idsJson);
```

### 场景3：分页查询

```csharp
// SQL Server / PostgreSQL
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY")]
Task<List<User>> GetPagedAsync(int skip, int take);

// MySQL
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}} LIMIT @take OFFSET @skip")]
Task<List<User>> GetPagedAsync(int skip, int take);

// SQLite
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}} LIMIT @take OFFSET @skip")]
Task<List<User>> GetPagedAsync(int skip, int take);
```

### 场景4：统计聚合

```csharp
// 计数
[Sqlx("SELECT COUNT(*) FROM {{table}}")]
Task<int> GetTotalCountAsync();

// 求和
[Sqlx("SELECT SUM(age) FROM {{table}} WHERE is_active = @isActive")]
Task<int> GetTotalAgeAsync(bool isActive);

// 平均值
[Sqlx("SELECT AVG(age) FROM {{table}}")]
Task<double> GetAverageAgeAsync();

// 最大/最小值
[Sqlx("SELECT MAX(created_at) FROM {{table}}")]
Task<DateTime> GetLatestCreatedDateAsync();
```

---

## 🎯 最佳实践

### 1. 何时使用占位符？

| 场景 | 使用占位符 | 原因 |
|------|-----------|------|
| **列名列表** | ✅ `{{columns}}` | 自动生成，类型安全 |
| **表名** | ✅ `{{table}}` | 自动转换命名规则 |
| **SET子句** | ✅ `{{set}}` | 自动生成赋值语句 |
| **值占位符** | ✅ `{{values}}` | 自动匹配列顺序 |
| **排序** | ✅ `{{orderby}}` | 支持选项 |
| **WHERE条件** | ❌ 直接写 | 更直观灵活 |
| **聚合函数** | ❌ 直接写 | 更简短清晰 |

### 2. 参数命名规范

```csharp
// ✅ 推荐：使用 @ 前缀
WHERE id = @id
WHERE name = @name

// ❌ 避免：不使用前缀
WHERE id = id  // 可能与列名冲突
```

### 3. 排除字段技巧

```csharp
// 插入时排除自增ID
{{columns --exclude Id}}

// 更新时排除不可变字段
{{set --exclude Id CreatedAt}}

// 只更新部分字段
{{set --only name email age}}
```

---

## ❓ 常见问题

### Q1：如何查看生成的代码？

**A：** 生成的代码在 `obj/Debug/net9.0/generated/` 目录：

```
obj/Debug/net9.0/generated/
└── Sqlx.Generator/
    └── Sqlx.Generator.CSharpGenerator/
        └── UserService.Repository.g.cs  ← 在这里
```

### Q2：支持事务吗？

**A：** 支持！使用标准的 ADO.NET 事务：

```csharp
using var transaction = connection.BeginTransaction();
try
{
    await userService.CreateAsync(user);
    await userService.UpdateAsync(anotherUser);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Q3：如何切换数据库？

**A：** 只需修改 `SqlDefine` 属性：

```csharp
// 从 SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]

// 改为 MySQL
[SqlDefine(SqlDefineTypes.MySQL)]
```

### Q4：支持异步操作吗？

**A：** 完全支持！所有方法都可以是异步的：

```csharp
Task<List<User>> GetAllAsync();
Task<User?> GetByIdAsync(int id);
Task<int> CreateAsync(User user);
```

### Q5：如何处理 NULL 值？

**A：** 使用可空类型和 IS NULL/IS NOT NULL：

```csharp
// 实体类
public string? Email { get; set; }  // 可空

// SQL
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE email IS NOT NULL")]
Task<List<User>> GetUsersWithEmailAsync();

[Sqlx("SELECT {{columns}} FROM {{table}} WHERE email IS NULL")]
Task<List<User>> GetUsersWithoutEmailAsync();
```

---

## 📚 下一步

### 深入学习

- 📖 [占位符完整指南](PLACEHOLDERS.md) - 详细的占位符说明
- 💡 [最佳实践](BEST_PRACTICES.md) - 推荐的使用模式
- 🌐 [多数据库支持](MULTI_DATABASE_TEMPLATE_ENGINE.md) - 跨数据库开发

### 查看示例

- 🚀 [TodoWebApi](../samples/TodoWebApi/README.md) - 完整的 RESTful API 示例
- 🧪 [SqlxDemo](../samples/SqlxDemo/README.md) - 功能演示项目

### API 参考

- 📘 [API 参考](API_REFERENCE.md) - 完整的 API 文档
- 🏗️ [高级功能](ADVANCED_FEATURES.md) - 高级特性说明

---

## 🎉 总结

**5 分钟学会 Sqlx：**

1. ✅ 定义数据模型（Record 类）
2. ✅ 创建服务接口（使用 Sqlx 属性）
3. ✅ 标记自动实现（RepositoryFor 属性）
4. ✅ 开始使用（零实现代码）

**核心优势：**

- ✅ 学习成本低（5 个核心占位符）
- ✅ 代码量少（零实现代码）
- ✅ 类型安全（编译时检查）
- ✅ 多数据库（6 种数据库）
- ✅ 高性能（无反射，AOT 友好）

---

<div align="center">

### 🚀 开始你的 Sqlx 之旅！

[📖 查看完整文档](README.md) · [🎯 占位符指南](PLACEHOLDERS.md) · [🚀 查看示例](../samples/TodoWebApi/README.md)

</div>
