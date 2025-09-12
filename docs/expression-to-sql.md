# 🎨 ExpressionToSql 完整指南

<div align="center">

**类型安全的动态 SQL 查询构建器**

[![类型安全](https://img.shields.io/badge/类型安全-编译时检查-green?style=for-the-badge)]()
[![动态查询](https://img.shields.io/badge/动态查询-LINQ表达式-blue?style=for-the-badge)]()
[![多数据库](https://img.shields.io/badge/多数据库-方言支持-orange?style=for-the-badge)]()

**将 C# LINQ 表达式转换为优化的 SQL 查询 · 支持 6 种数据库方言**

</div>

---

## 📋 目录

- [✨ 功能概述](#-功能概述)
- [🚀 快速开始](#-快速开始)
- [🔍 WHERE 条件构建](#-where-条件构建)
- [📊 排序和分页](#-排序和分页)
- [🔄 UPDATE 操作](#-update-操作)
- [🌐 多数据库支持](#-多数据库支持)
- [🔗 Repository 集成](#-repository-集成)
- [💡 最佳实践](#-最佳实践)
- [🎯 高级用法](#-高级用法)

---

## ✨ 功能概述

### 🎯 核心特性

<table>
<tr>
<td width="50%">

#### 🛡️ 类型安全保障
- **编译时检查** - 在构建时发现错误
- **智能提示** - IDE 自动完成支持
- **重构安全** - 重命名字段自动更新查询
- **null 安全** - 完整的 nullable 支持

</td>
<td width="50%">

#### ⚡ 高性能设计
- **编译时优化** - 零运行时反射
- **SQL 缓存** - 智能查询计划缓存
- **批量优化** - 支持批量查询构建
- **内存效率** - 最小化对象分配

</td>
</tr>
<tr>
<td width="50%">

#### 🔄 动态查询构建
- **条件组合** - AND、OR、NOT 逻辑
- **运行时构建** - 根据用户输入动态添加条件
- **复杂表达式** - 支持嵌套和复合条件
- **算术运算** - 数学运算和模运算支持

</td>
<td width="50%">

#### 🌐 多数据库兼容
- **SQL Server** - 原生 T-SQL 语法
- **MySQL** - MySQL 特定语法
- **PostgreSQL** - PostgeSQL 方言
- **SQLite** - 轻量级语法
- **Oracle** - 企业级特性 (开发中)

</td>
</tr>
</table>

### 📊 支持的操作

| 操作类型 | 支持度 | 示例 |
|----------|--------|------|
| **SELECT 查询** | ✅ 完整 | `Where()`, `OrderBy()`, `Take()` |
| **UPDATE 操作** | ✅ 完整 | `Set()`, `Where()` |
| **条件筛选** | ✅ 完整 | 比较、逻辑、字符串、日期时间 |
| **排序分页** | ✅ 完整 | 单字段、多字段、升序降序 |
| **算术运算** | ✅ 完整 | `+`, `-`, `*`, `/`, `%` |
| **字符串操作** | ✅ 完整 | `Contains`, `StartsWith`, `EndsWith` |

## 🚀 基础用法

### 创建实例

```csharp
// 不同数据库方言
var query1 = ExpressionToSql<User>.ForSqlServer();
var query2 = ExpressionToSql<User>.ForMySql();
var query3 = ExpressionToSql<User>.ForPostgreSQL();
var query4 = ExpressionToSql<User>.ForSqlite();
```

### 基本查询

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 简单条件
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .Where(u => u.Age >= 18);

string sql = query.ToSql();
// 生成: SELECT * FROM [User] WHERE ([IsActive] = 1) AND ([Age] >= 18)
```

## 🔍 WHERE 条件

### 比较操作
```csharp
query.Where(u => u.Id == 1);              // 等于
query.Where(u => u.Id != 1);              // 不等于
query.Where(u => u.Age > 18);             // 大于
query.Where(u => u.Age >= 18);            // 大于等于
```

### 逻辑操作
```csharp
// AND 条件
query.Where(u => u.IsActive && u.Age >= 18);

// OR 条件  
query.Where(u => u.Age < 18 || u.Age > 65);

// NOT 条件
query.Where(u => !u.IsActive);
```

### 字符串操作
```csharp
query.Where(u => u.Name.Contains("张"));    // LIKE '%张%'
query.Where(u => u.Name.StartsWith("张")); // LIKE '张%'
query.Where(u => u.Name.EndsWith("三"));    // LIKE '%三'
```

### 🆕 算术运算

```csharp
// 基础运算
query.Where(u => u.Age + 5 > 30);           // 加法
query.Where(u => u.Price * 0.8 < 100);      // 乘法
query.Where(u => u.Id % 2 == 0);            // 🆕 模运算 (偶数ID)

// UPDATE 中使用
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Age, u => u.Age + 1)         // 年龄+1
    .Where(u => u.Id == 1);
```

### 日期时间操作

```csharp
var today = DateTime.Today;
var lastWeek = DateTime.Now.AddDays(-7);

query.Where(u => u.CreatedAt >= today);     // 日期比较
query.Where(u => u.CreatedAt > lastWeek);   // 日期范围
```

## 📊 排序和分页

```csharp
// 排序
var query = ExpressionToSql<User>.ForSqlServer()
    .OrderBy(u => u.Name)                    // 升序
    .OrderByDescending(u => u.CreatedAt);    // 降序

// 分页
var pagedQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Id)
    .Skip(20)                                // 跳过前20条
    .Take(10);                               // 获取10条
```

## 🔄 UPDATE 操作

```csharp
// 设置值
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Name, "新名称")
    .Set(u => u.IsActive, true)
    .Where(u => u.Id == 1);

// 表达式设置
var updateQuery2 = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Age, u => u.Age + 1)         // 年龄加1
    .Where(u => u.Id == 1);
```

## 🌐 多数据库方言

```csharp
var condition = u => u.IsActive && u.Age >= 18;

// SQL Server: [User] WHERE ([IsActive] = 1)
var sqlServer = ExpressionToSql<User>.ForSqlServer().Where(condition);

// MySQL: `User` WHERE (`IsActive` = 1) 
var mysql = ExpressionToSql<User>.ForMySql().Where(condition);

// PostgreSQL: "User" WHERE ("IsActive" = 1)
var pg = ExpressionToSql<User>.ForPostgreSQL().Where(condition);
```

## 🔗 Repository 集成

```csharp
public interface IUserService
{
    [Sqlx]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
    
    [Sqlx]
    int UpdateUsers([ExpressionToSql] ExpressionToSql<User> updateQuery);
}

// 使用
var activeUsers = userRepo.QueryUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.IsActive && u.Age >= 18)
        .OrderBy(u => u.Name)
);
```

## 💡 最佳实践

### 动态条件

```csharp
public IList<User> SearchUsers(string? name, int? minAge, bool? isActive)
{
    var query = ExpressionToSql<User>.ForSqlServer();
    
    if (!string.IsNullOrEmpty(name))
        query = query.Where(u => u.Name.Contains(name));
    
    if (minAge.HasValue)
        query = query.Where(u => u.Age >= minAge.Value);
    
    if (isActive.HasValue)
        query = query.Where(u => u.IsActive == isActive.Value);
    
    return userRepo.QueryUsers(query.OrderBy(u => u.Name));
}
```

### 性能建议

```csharp
// ✅ 推荐：结合其他条件
.Where(u => u.IsActive && u.Id % 2 == 0)

// ⚠️ 注意：大表上的纯模运算可能较慢
.Where(u => u.Id % 1000 == 1)  // 建议配合索引
```

---

## 🎯 高级用法

### 复杂条件组合

```csharp
// 复杂的业务逻辑查询
var complexQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => 
        (u.IsActive && u.Age >= 18) ||  // 成年活跃用户
        (u.IsVip && u.Age >= 16)        // 或VIP青少年用户
    )
    .Where(u => u.CreatedAt > DateTime.Now.AddYears(-1))  // 近一年注册
    .Where(u => u.Email.Contains("@company.com"))         // 公司邮箱
    .OrderBy(u => u.LastLoginAt)                          // 按最后登录排序
    .Take(50);                                            // 限制50条

var sql = complexQuery.ToSql();
// 生成复杂的 WHERE 子句
```

### 动态搜索功能

```csharp
public IList<User> SearchUsers(UserSearchCriteria criteria)
{
    var query = ExpressionToSql<User>.ForSqlServer();
    
    // 名称搜索
    if (!string.IsNullOrEmpty(criteria.Name))
    {
        query = query.Where(u => u.Name.Contains(criteria.Name));
    }
    
    // 年龄范围
    if (criteria.MinAge.HasValue)
    {
        query = query.Where(u => u.Age >= criteria.MinAge.Value);
    }
    
    if (criteria.MaxAge.HasValue)
    {
        query = query.Where(u => u.Age <= criteria.MaxAge.Value);
    }
    
    // 部门筛选
    if (criteria.DepartmentIds?.Any() == true)
    {
        query = query.Where(u => criteria.DepartmentIds.Contains(u.DepartmentId));
    }
    
    // 注册日期范围
    if (criteria.RegisteredAfter.HasValue)
    {
        query = query.Where(u => u.CreatedAt >= criteria.RegisteredAfter.Value);
    }
    
    // 排序
    query = criteria.SortBy switch
    {
        "name" => query.OrderBy(u => u.Name),
        "age" => query.OrderBy(u => u.Age),
        "created" => query.OrderByDescending(u => u.CreatedAt),
        _ => query.OrderBy(u => u.Id)
    };
    
    // 分页
    if (criteria.PageSize > 0)
    {
        query = query.Skip(criteria.PageIndex * criteria.PageSize)
                    .Take(criteria.PageSize);
    }
    
    return userRepository.QueryUsers(query);
}
```

### 批量查询优化

```csharp
// 批量构建多个查询
var queries = new List<ExpressionToSql<User>>();

foreach (var department in departments)
{
    var query = ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.DepartmentId == department.Id && u.IsActive)
        .OrderBy(u => u.Name);
    
    queries.Add(query);
}

// 批量执行
var results = queries.Select(q => userRepository.QueryUsers(q)).ToList();
```

---

## 💡 最佳实践

### 🔧 性能优化

```csharp
// ✅ 推荐：结合索引字段进行查询
.Where(u => u.IsActive && u.Id % 2 == 0)  // IsActive 有索引

// ✅ 推荐：避免在大表上使用纯模运算
.Where(u => u.DepartmentId == 1 && u.Id % 10 == 0)  // 先过滤部门

// ⚠️ 注意：大表慎用 Contains，考虑全文索引
.Where(u => u.Name.Contains(keyword))  // 确保有适当索引

// ✅ 推荐：使用具体的日期范围而不是函数
var lastWeek = DateTime.Now.AddDays(-7);
.Where(u => u.CreatedAt >= lastWeek)  // 而不是 DateTime.Now.AddDays(-7)
```

### 🎯 代码组织

```csharp
// ✅ 推荐：封装复杂查询为方法
public static class UserQueries
{
    public static ExpressionToSql<User> ActiveUsers()
    {
        return ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.IsActive && u.DeletedAt == null);
    }
    
    public static ExpressionToSql<User> RecentUsers(int days = 30)
    {
        var since = DateTime.Now.AddDays(-days);
        return ActiveUsers().Where(u => u.CreatedAt >= since);
    }
    
    public static ExpressionToSql<User> VipUsers()
    {
        return ActiveUsers().Where(u => u.IsVip && u.VipLevel > 0);
    }
}

// 使用
var recentVipUsers = UserQueries.VipUsers()
    .Where(u => u.CreatedAt >= DateTime.Now.AddDays(-7))
    .OrderByDescending(u => u.VipLevel);
```

### 🔍 测试策略

```csharp
[TestClass]
public class ExpressionToSqlTests
{
    [TestMethod]
    public void Where_SimpleCondition_GeneratesCorrectSql()
    {
        // Arrange
        var query = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.IsActive && u.Age >= 18);
        
        // Act
        var sql = query.ToSql();
        
        // Assert
        Assert.IsTrue(sql.Contains("[IsActive] = 1"));
        Assert.IsTrue(sql.Contains("[Age] >= 18"));
        Assert.IsTrue(sql.Contains("AND"));
    }
    
    [TestMethod]
    public void OrderBy_MultipleFields_GeneratesCorrectOrder()
    {
        // Arrange
        var query = ExpressionToSql<User>.ForSqlServer()
            .OrderBy(u => u.Name)
            .OrderByDescending(u => u.CreatedAt);
        
        // Act
        var sql = query.ToSql();
        
        // Assert
        Assert.IsTrue(sql.Contains("ORDER BY [Name], [CreatedAt] DESC"));
    }
}
```

---

## 🚨 注意事项

### ⚠️ 限制和约束

1. **不支持的表达式**
   ```csharp
   // ❌ 不支持：复杂的方法调用
   .Where(u => u.Name.Substring(0, 3) == "ABC")
   
   // ❌ 不支持：自定义方法
   .Where(u => MyCustomMethod(u.Name))
   
   // ❌ 不支持：linq 复杂操作
   .Where(u => u.Orders.Any(o => o.Total > 100))
   ```

2. **性能考虑**
   ```csharp
   // ⚠️ 大表慎用：全表扫描
   .Where(u => u.Name.Contains("keyword"))
   
   // ⚠️ 复杂计算：影响性能
   .Where(u => (u.Salary * 0.2 + u.Bonus) > 10000)
   ```

3. **数据库差异**
   ```csharp
   // ⚠️ 注意：不同数据库的日期函数差异
   // SQL Server: GETDATE()
   // MySQL: NOW()
   // 建议使用 DateTime.Now 常量
   ```

---

## 📚 参考资源

### 🔗 相关文档

- **[高级特性指南](ADVANCED_FEATURES_GUIDE.md)** - 深入了解高级功能
- **[新功能快速入门](NEW_FEATURES_QUICK_START.md)** - 最新功能介绍
- **[完整示例项目](../samples/ComprehensiveExample/)** - 实际使用案例

### 🧪 示例代码

- **[ExpressionToSql 演示](../samples/ComprehensiveExample/Demonstrations/ExpressionToSqlDemo.cs)** - 完整功能演示
- **[单元测试](../tests/Sqlx.Tests/)** - 测试用例参考
- **[性能测试](../samples/ComprehensiveExample/PerformanceTest.cs)** - 性能基准

---

<div align="center">

**🎨 掌握 ExpressionToSql，构建强大的动态查询系统**

**类型安全 · 高性能 · 多数据库支持**

**[⬆ 返回顶部](#-expressiontosql-完整指南)**

</div>