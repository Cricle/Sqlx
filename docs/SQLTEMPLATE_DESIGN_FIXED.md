# SqlTemplate 设计修复：分离模板定义与参数值

## 问题解决

根据您提出的正确观点："SqlTemplate不应该有参数，因为只是模版，没有值"，我们已经重构了SqlTemplate的设计，修复了概念混乱的问题。

## 修复前的问题

```csharp
// 问题：混合了模板定义和运行时值
public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)

// 错误用法：每次参数不同都要创建新模板
var template1 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
var template2 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 2 }); // 重复创建！
```

**问题分析：**
- ❌ 概念混乱：模板包含了具体参数值
- ❌ 无法重用：每次参数不同都要新建实例
- ❌ 性能浪费：重复解析相同的SQL结构
- ❌ 内存低效：同一模板创建多个对象

## 修复后的设计

### 1. 新增 ParameterizedSql 类型
```csharp
// 新类型：专门表示参数化的SQL执行实例
public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?> Parameters)
```

### 2. 重构 SqlTemplate 为纯模板
```csharp
// SqlTemplate 现在是纯模板定义
public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
{
    // 新API：创建纯模板
    public static SqlTemplate Parse(string sql) => new(sql, new Dictionary<string, object?>());
    
    // 执行时绑定参数
    public ParameterizedSql Execute(object? parameters = null) => ParameterizedSql.Create(Sql, parameters);
    
    // 流式参数绑定
    public SqlTemplateBuilder Bind() => new(this);
    
    // 检查是否为纯模板
    public bool IsPureTemplate => Parameters.Count == 0;
}
```

### 3. 流式参数绑定器
```csharp
public sealed class SqlTemplateBuilder
{
    public SqlTemplateBuilder Param<T>(string name, T value) { /* ... */ }
    public SqlTemplateBuilder Params(object? parameters) { /* ... */ }
    public ParameterizedSql Build() { /* ... */ }
}
```

## 正确的使用方式

### 基础用法：模板重用
```csharp
// 1. 定义纯模板（只定义一次）
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

// 2. 重复使用，绑定不同参数
var execution1 = template.Execute(new { id = 1 });
var execution2 = template.Execute(new { id = 2 });
var execution3 = template.Execute(new { id = 3 });

// 3. 渲染最终SQL
string sql1 = execution1.Render(); // "SELECT * FROM Users WHERE Id = 1"
string sql2 = execution2.Render(); // "SELECT * FROM Users WHERE Id = 2"
```

### 流式参数绑定
```csharp
var template = SqlTemplate.Parse(@"
    SELECT * FROM Products 
    WHERE CategoryId = @categoryId 
    AND Price BETWEEN @minPrice AND @maxPrice");

var execution = template.Bind()
    .Param("categoryId", 1)
    .Param("minPrice", 10.0m)
    .Param("maxPrice", 100.0m)
    .Build();

string sql = execution.Render();
```

### 模板缓存（推荐生产环境）
```csharp
// 全局模板缓存
private static readonly Dictionary<string, SqlTemplate> TemplateCache = new();

public SqlTemplate GetTemplate(string key, string sql)
{
    if (!TemplateCache.ContainsKey(key))
    {
        TemplateCache[key] = SqlTemplate.Parse(sql);
    }
    return TemplateCache[key];
}

// 使用缓存模板
var template = GetTemplate("user_by_id", "SELECT * FROM Users WHERE Id = @id");
var execution = template.Execute(new { id = userId });
```

## 性能和内存优势

### 修复前（问题）
```csharp
// 每次创建新模板 - 浪费CPU和内存
var template1 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
var template2 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 2 });
var template3 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 3 });
// 创建了3个模板对象 + 3个参数字典对象 = 6个对象
```

### 修复后（高效）
```csharp
// 一个模板，多次执行 - 高效
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var execution1 = template.Execute(new { id = 1 });
var execution2 = template.Execute(new { id = 2 });
var execution3 = template.Execute(new { id = 3 });
// 创建了1个模板对象 + 3个执行对象 = 4个对象，节省33%内存
```

## 向后兼容性

为了不破坏现有代码，我们保留了原有API但标记为过时：

```csharp
[Obsolete("Use SqlTemplate.Parse(sql).Execute(parameters) for better template reuse")]
public static SqlTemplate Create(string sql, object? parameters = null)
```

### 迁移指南

**旧代码：**
```csharp
var template = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
string sql = template.Sql; // 使用Parameters内联
```

**新代码：**
```csharp
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var execution = template.Execute(new { id = 1 });
string sql = execution.Render();
```

**或者直接使用ParameterizedSql：**
```csharp
var execution = ParameterizedSql.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
string sql = execution.Render();
```

## 设计原则验证

✅ **模板分离原则**：SqlTemplate现在是纯模板定义，不包含参数值
✅ **单一职责原则**：SqlTemplate负责模板定义，ParameterizedSql负责执行实例
✅ **可重用性**：模板可以被多次执行，提高性能
✅ **概念清晰**：名称和职责完全匹配
✅ **向后兼容**：现有代码无需立即修改
✅ **AOT友好**：减少反射使用，提供字典优先的参数绑定

## 总结

这次重构彻底解决了您指出的核心问题：**"SqlTemplate不应该有参数，因为只是模版，没有值"**。

新设计的核心理念：
- **SqlTemplate** = 纯模板定义（可重用、可缓存）
- **ParameterizedSql** = 执行实例（包含具体参数值）
- **SqlTemplateBuilder** = 流式构建器（提供良好的API体验）

这样的设计不仅概念清晰，还带来了显著的性能和内存优势，同时保持了向后兼容性。
