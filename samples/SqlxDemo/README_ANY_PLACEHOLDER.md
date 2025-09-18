# 🎯 Any占位符功能演示

## 🚀 功能概述

Sqlx的Any占位符功能让您可以用更自然的方式构建参数化SqlTemplate。SqlTemplate现在使用简洁的Dictionary<string, object?>参数形式，类似于EF Core的FromSql，但更轻量级。

## 💡 基本用法

### 1. 自动生成参数名

```csharp
using var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age > Any.Int() && u.Name == Any.String() && u.IsActive == Any.Bool());

var template = query.ToTemplate();
// 生成: SELECT * FROM [User] WHERE ([Age] > @p0 AND [Name] = @p1 AND [IsActive] = @p2)
// 参数: Dictionary<string, object?> { "@p0" => 0, "@p1" => null, "@p2" => false }
```

### 2. 自定义参数名

```csharp
using var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age > Any.Int("minAge") && u.Name == Any.String("userName"));

var template = query.ToTemplate();
// 生成: SELECT * FROM [User] WHERE ([Age] > @minAge AND [Name] = @userName)  
// 参数: Dictionary<string, object?> { "@minAge" => 0, "@userName" => null }
```

## 🔧 支持的数据类型

| Any方法 | 对应类型 | 示例 |
|---------|---------|------|
| `Any.Int()` | int | `u.Age > Any.Int()` |
| `Any.String()` | string | `u.Name == Any.String()` |
| `Any.Bool()` | bool | `u.IsActive == Any.Bool()` |
| `Any.DateTime()` | DateTime | `u.CreatedAt > Any.DateTime()` |
| `Any.Guid()` | Guid | `u.Id == Any.Guid()` |
| `Any.Value<T>()` | 泛型T | `u.Salary > Any.Value<decimal>()` |

每个方法都有两个重载：
- 无参数版本：自动生成参数名（如 @p0, @p1）
- 带参数版本：使用自定义参数名（如 Any.Int("userAge")）

## 🎨 实际应用场景

### 复杂查询条件

```csharp
using var complexQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => (u.Age >= Any.Int("minAge") && u.Age <= Any.Int("maxAge")) &&
               (u.Salary > Any.Value<decimal>("baseSalary") || u.Bonus > Any.Value<decimal>("minBonus")) &&
               u.DepartmentId == Any.Int("deptId") &&
               u.IsActive == Any.Bool("activeStatus"))
    .OrderBy(u => u.Salary)
    .Take(Any.Int("pageSize"));

var template = complexQuery.ToTemplate();
```

### SqlTemplate重用

```csharp
// 创建可重用的模板
using var reusableQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age > Any.Int("minAge") && u.DepartmentId == Any.Int("deptId"));

var template = reusableQuery.ToTemplate();

// 可以在不同场景中重用这个模板，只需要设置不同的参数值
```

## ✅ 优势对比

### ❌ 传统方式
```csharp
var template = SqlTemplate.Create(
    "SELECT * FROM [User] WHERE age > @age AND is_active = @isActive",
    new Dictionary<string, object?> {
        { "@age", 25 },
        { "@isActive", 1 }
    }
);
```

### ✅ Any占位符方式
```csharp
using var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age > Any.Int() && u.IsActive);

var template = query.ToTemplate();
// 自动生成参数化查询和参数字典
```

## 📖 SqlTemplate 新设计

### 数据库无关设计
```csharp
// SqlTemplate 现在使用简洁的 Dictionary<string, object?> 参数
public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)

// 创建方式1：使用字典
var template1 = SqlTemplate.Create(
    "SELECT * FROM Users WHERE Age > @age",
    new Dictionary<string, object?> { { "@age", 25 } }
);

// 创建方式2：使用匿名对象（自动转换为字典）
var template2 = SqlTemplate.Create(
    "SELECT * FROM Users WHERE Name = @name AND Age > @age",
    new { name = "John", age = 25 }
);
```

### 与EF Core FromSql类似的体验
```csharp
// EF Core 风格
context.Users.FromSql($"SELECT * FROM Users WHERE Age > {minAge}");

// Sqlx SqlTemplate 风格（类型安全 + 可重用）
var template = SqlTemplate.Create(
    "SELECT * FROM Users WHERE Age > @minAge",
    new { minAge }
);
```

## 🚀 核心优势

- **✨ 开发效率**: 减少80%的样板代码
- **🛡️ 类型安全**: 编译时验证，运行时无错
- **🚀 高性能**: 零反射，最优化执行
- **🎨 直观语法**: 自然的LINQ表达式体验
- **🔒 安全可靠**: 自动防止SQL注入

## 🎯 如何运行演示

在SqlxDemo项目中选择选项3即可体验Any占位符的完整功能演示！

```bash
dotnet run --project samples/SqlxDemo/SqlxDemo.csproj
# 选择 "3" - Any占位符演示
```
