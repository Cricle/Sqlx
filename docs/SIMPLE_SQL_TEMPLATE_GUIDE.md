# 极简SQL模板指南

> **设计哲学**：简单、易记、通用、高效 - 让SQL模板回归本质

## 🎯 设计目标

### ❌ 旧设计的问题
- **概念太多**：`SqlTemplate`、`SqlTemplateBuilder`、`SqlTemplateOptions`...
- **API冗余**：`Execute()` vs `Render()`、`Param()` vs `Params()`
- **步骤繁琐**：`Parse()` → `Bind()` → `Param()` → `Build()` → `Render()`
- **语法复杂**：`@{parameterName}` vs `@parameterName`

### ✅ 新设计的优势
- **一个入口**：`Sql` 静态类解决所有问题
- **统一语法**：`{参数名}` 占位符，简单易记
- **链式调用**：流畅的API设计
- **零学习成本**：直观的方法命名

---

## 🚀 快速开始

### 1. 最简单的用法 - 一行搞定

```csharp
// 直接执行，无需中间步骤
var sql = Sql.Execute("SELECT * FROM Users WHERE Id = {id}", new { id = 123 });
Console.WriteLine(sql);
// 输出: SELECT * FROM Users WHERE Id = 123
```

### 2. 模板复用 - 创建一次，多次使用

```csharp
// 创建可复用模板
var userQuery = Sql.Template("SELECT * FROM Users WHERE Age > {age} AND City = {city}");

// 多次使用不同参数
var youngUsers = userQuery.With(new { age = 18, city = "北京" });
var adultUsers = userQuery.With(new { age = 30, city = "上海" });

Console.WriteLine(youngUsers); // 隐式转换为字符串
Console.WriteLine(adultUsers.ToSql()); // 显式转换
```

### 3. 链式参数设置 - 灵活组合

```csharp
var sql = Sql.Template("SELECT * FROM Products WHERE Price > {price} AND Category = {category}")
    .Set("price", 100)
    .Set("category", "电子产品")
    .ToSql();
```

---

## 📚 完整API参考

### `Sql` 静态类 - 统一入口

```csharp
// 创建模板
public static SqlTemplate Template(string template)

// 直接执行
public static string Execute(string sql, object? parameters = null)

// 批量处理
public static IEnumerable<string> Batch(string template, IEnumerable<object> parametersList)
```

### `SqlTemplate` 结构体 - 核心功能

```csharp
// 设置参数（支持匿名对象、字典）
public SqlTemplate With(object? parameters)

// 设置单个参数（链式调用）
public SqlTemplate Set(string name, object? value)

// 生成SQL字符串
public string ToSql()

// 转换为参数化SQL（用于ORM集成）
public ParameterizedSql ToParameterized()

// 隐式转换为字符串
public static implicit operator string(SqlTemplate template)
```

### 字符串扩展方法 - 更自然的语法

```csharp
// 转换为模板
public static SqlTemplate AsSqlTemplate(this string template)

// 直接执行
public static string SqlWith(this string template, object? parameters = null)
```

---

## 💡 使用场景和最佳实践

### 场景1：简单查询 - 用 `Sql.Execute()`

```csharp
// ✅ 推荐：一次性查询
var userSql = Sql.Execute("SELECT * FROM Users WHERE Email = {email}", new { email = "user@example.com" });

// ❌ 不推荐：为简单查询创建模板
var template = Sql.Template("SELECT * FROM Users WHERE Email = {email}");
var userSql2 = template.With(new { email = "user@example.com" });
```

### 场景2：模板复用 - 用 `Sql.Template()`

```csharp
// ✅ 推荐：需要多次使用的模板
var searchTemplate = Sql.Template("SELECT * FROM Products WHERE Name LIKE '%{keyword}%' AND Price > {minPrice}");

var laptops = searchTemplate.With(new { keyword = "笔记本", minPrice = 3000 });
var phones = searchTemplate.With(new { keyword = "手机", minPrice = 1000 });
var tablets = searchTemplate.With(new { keyword = "平板", minPrice = 2000 });
```

### 场景3：批量操作 - 用 `Sql.Batch()`

```csharp
// ✅ 推荐：批量插入/更新
var users = GetUsersFromFile();
var insertSqls = Sql.Batch("INSERT INTO Users (Name, Email, Age) VALUES ({name}, {email}, {age})", users);

foreach (var sql in insertSqls)
{
    ExecuteNonQuery(sql);
}
```

### 场景4：复杂查询 - 组合使用

```csharp
// ✅ 推荐：复杂查询的构建
var reportQuery = Sql.Template(@"
    SELECT 
        u.Name,
        u.Email,
        COUNT(o.Id) as OrderCount,
        SUM(o.Amount) as TotalAmount
    FROM Users u
    LEFT JOIN Orders o ON u.Id = o.UserId
    WHERE u.CreatedDate >= {startDate}
      AND u.CreatedDate <= {endDate}
      AND u.Status = {status}
    GROUP BY u.Id, u.Name, u.Email
    HAVING COUNT(o.Id) >= {minOrders}
    ORDER BY TotalAmount DESC
    LIMIT {limit}");

var monthlySales = reportQuery.With(new 
{
    startDate = DateTime.Now.AddMonths(-1),
    endDate = DateTime.Now,
    status = "Active",
    minOrders = 5,
    limit = 100
});
```

---

## 🔧 高级功能

### 1. 类型安全的参数处理

```csharp
var template = Sql.Template("SELECT * FROM Orders WHERE Amount > {amount} AND Date = {date}");

// 支持各种.NET类型
var sql = template.With(new 
{
    amount = 199.99m,           // decimal
    date = DateTime.Today,      // DateTime
    isActive = true,            // bool → 1/0
    userId = (int?)null,        // nullable → NULL
    guid = Guid.NewGuid(),      // Guid → 'xxx-xxx-xxx'
    description = "It's great!" // string → 'It''s great!' (自动转义)
});
```

### 2. 与现有系统集成

```csharp
// 转换为ParameterizedSql，用于ORM集成
var template = Sql.Template("SELECT * FROM Users WHERE Age > {age}");
var parameterizedSql = template.With(new { age = 18 }).ToParameterized();

// 现在可以用于Dapper、EF Core等
using var connection = new SqlConnection(connectionString);
var users = connection.Query<User>(parameterizedSql.Sql, parameterizedSql.Parameters);
```

### 3. 字符串扩展的便捷用法

```csharp
// 直接在字符串字面量上使用
var sql1 = "SELECT * FROM Products WHERE Category = {category}".SqlWith(new { category = "Books" });

// 从配置文件读取的SQL模板
var templateFromConfig = Configuration["SqlTemplates:UserSearch"];
var sql2 = templateFromConfig.AsSqlTemplate().With(searchParams);
```

---

## 📊 性能对比

### API复杂度对比

| 功能 | 旧API | 新API | 简化程度 |
|------|-------|-------|----------|
| 简单查询 | 7行代码 | 1行代码 | **85%** ↓ |
| 模板复用 | 5个概念 | 2个概念 | **60%** ↓ |
| 参数绑定 | 3步操作 | 1步操作 | **67%** ↓ |
| 学习成本 | 8个API | 3个API | **62%** ↓ |

### 旧API示例（复杂）
```csharp
// 需要记住：SqlTemplate.Parse, Bind, Param, Build, Render
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var builder = template.Bind();
builder.Param("id", 123);
var paramSql = builder.Build();
var result = paramSql.Render();
```

### 新API示例（简单）
```csharp
// 只需要记住：Sql.Execute
var result = Sql.Execute("SELECT * FROM Users WHERE Id = {id}", new { id = 123 });
```

---

## ✅ 最佳实践总结

### DO（推荐做法）
- ✅ 使用 `{参数名}` 占位符语法
- ✅ 简单查询用 `Sql.Execute()`
- ✅ 复用场景用 `Sql.Template()`
- ✅ 批量操作用 `Sql.Batch()`
- ✅ 利用隐式字符串转换
- ✅ 使用字符串扩展方法

### DON'T（避免做法）
- ❌ 不要为一次性查询创建模板
- ❌ 不要混用旧的复杂API
- ❌ 不要在模板中硬编码值
- ❌ 不要忽略SQL注入防护

### 记忆技巧
1. **一个入口**：`Sql` 类解决一切
2. **三个方法**：`Execute`（执行）、`Template`（模板）、`Batch`（批量）
3. **一个语法**：`{参数名}` 统一占位符
4. **零概念**：不需要记住复杂的类层次结构

---

## 🎯 迁移指南

如果您正在使用旧的SQL模板API，可以按以下方式快速迁移：

```csharp
// 旧代码
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var result = template.Execute(new { id = 123 });
var sql = result.Render();

// 新代码（一行搞定）
var sql = Sql.Execute("SELECT * FROM Users WHERE Id = {id}", new { id = 123 });
```

**迁移建议**：
1. 新项目直接使用新API
2. 旧项目可以渐进式迁移
3. 两套API可以并存，不冲突
4. 优先迁移使用频率高的代码

---

> **设计理念**：最好的API是让用户感觉不到API的存在，就像在写普通的字符串操作一样自然。
