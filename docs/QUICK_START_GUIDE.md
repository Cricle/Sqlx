# Sqlx 3.0 快速入门指南

本指南将在5分钟内帮您掌握Sqlx的核心用法。

## 📦 安装

```bash
dotnet add package Sqlx
```

## 🎯 三种核心模式

Sqlx 3.0 专注于三种简单而强大的使用模式：

### 1️⃣ 直接执行模式 - 最简单

适用于：一次性查询，简单SQL执行

```csharp
using Sqlx;

// 创建并执行参数化SQL
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age AND IsActive = @active",
    new { age = 18, active = true });

// 渲染最终SQL
string finalSql = sql.Render();
Console.WriteLine(finalSql);
// 输出: SELECT * FROM Users WHERE Age > 18 AND IsActive = 1
```

### 2️⃣ 静态模板模式 - 可重用

适用于：需要重复执行的SQL，参数化查询

```csharp
using Sqlx;

// 定义可重用的模板
var template = SqlTemplate.Parse(
    "SELECT * FROM Users WHERE Age > @age AND IsActive = @active");

// 重复使用，绑定不同参数
var youngUsers = template.Execute(new { age = 18, active = true });
var seniorUsers = template.Execute(new { age = 65, active = true });

// 流式参数绑定
var customQuery = template.Bind()
    .Param("age", 25)
    .Param("active", true)
    .Build();

// 渲染SQL
string sql1 = youngUsers.Render();
string sql2 = customQuery.Render();
```

### 3️⃣ 动态模板模式 - 类型安全

适用于：复杂查询构建，条件动态组合

```csharp
using Sqlx;

// 定义实体类
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string Email { get; set; } = "";
}

// 构建类型安全的动态查询
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Email })
    .OrderBy(u => u.Name)
    .Take(10);

string sql = query.ToSql();
Console.WriteLine(sql);
// 输出: SELECT [Name], [Email] FROM [User] 
//       WHERE ([Age] > 25 AND [IsActive] = 1) 
//       ORDER BY [Name] ASC 
//       OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
```

## 🗄️ 数据库支持

```csharp
// 选择数据库方言
var sqlServerQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer);
var mysqlQuery = ExpressionToSql<User>.Create(SqlDefine.MySql);
var postgresQuery = ExpressionToSql<User>.Create(SqlDefine.PostgreSql);
var sqliteQuery = ExpressionToSql<User>.Create(SqlDefine.SQLite);
```

## 🔄 模式转换

三种模式可以互相转换：

```csharp
// 动态模板 → 静态模板
var dynamicQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25)
    .Select(u => u.Name);

var template = dynamicQuery.ToTemplate();  // 转换为模板

// 静态模板 → 直接执行
var execution = template.Execute(new { /* 参数 */ });
```

## 🚀 常见用法示例

### SELECT 查询
```csharp
// 简单查询
var users = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .ToSql();

// 复杂条件
var complexQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age >= 18 && u.Age <= 65)
    .Where(u => u.Name.Contains("John"))
    .OrderBy(u => u.Name)
    .OrderByDescending(u => u.Id)
    .Take(20)
    .Skip(10)
    .ToSql();
```

### INSERT 操作
```csharp
// 指定列插入（推荐，AOT友好）
var insert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email, u.Age })
    .Values("John Doe", "john@example.com", 30)
    .ToSql();

// 多行插入
var multiInsert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Age })
    .Values("John", 30)
    .AddValues("Jane", 25)
    .AddValues("Bob", 35)
    .ToSql();
```

### UPDATE 操作
```csharp
var update = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.Name, "New Name")
    .Set(u => u.Age, u => u.Age + 1)  // 表达式设置
    .Where(u => u.Id == 1)
    .ToSql();
```

### DELETE 操作
```csharp
var delete = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete(u => u.IsActive == false)
    .ToSql();
```

## 💡 最佳实践

### 1. 选择合适的模式
- **直接执行**: 简单一次性查询
- **静态模板**: 需要重复使用的查询
- **动态模板**: 复杂的条件构建

### 2. AOT 兼容性
```csharp
// ✅ 推荐：显式指定列
.InsertInto(u => new { u.Name, u.Email })

// ❌ 避免：在AOT场景中使用自动推断
.InsertIntoAll()  // 使用反射，不推荐AOT
```

### 3. 性能优化
```csharp
// ✅ 模板重用
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user1 = template.Execute(new { id = 1 });
var user2 = template.Execute(new { id = 2 });

// ✅ 参数化查询
var parameterizedQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .UseParameterizedQueries()
    .Where(u => u.Status == "Active");
```

## 🎯 下一步

- 查看 [完整API参考](API_REFERENCE.md)
- 了解 [高级功能](ADVANCED_FEATURES.md)
- 学习 [最佳实践](BEST_PRACTICES.md)

恭喜！您已经掌握了Sqlx 3.0的核心用法。三种简单模式，满足从简单到复杂的所有需求。