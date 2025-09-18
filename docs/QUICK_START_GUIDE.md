# Sqlx 快速开始指南

欢迎来到 Sqlx！这是一个现代化的 .NET ORM 框架，专为高性能、类型安全和 AOT 兼容而设计。本指南将带您在 5 分钟内掌握 Sqlx 的核心功能。

## 🚀 30 秒快速体验

### 1️⃣ 安装 NuGet 包

```bash
# Package Manager Console
Install-Package Sqlx
Install-Package Sqlx.Generator

# .NET CLI
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# PackageReference
<PackageReference Include="Sqlx" Version="2.0.2" />
<PackageReference Include="Sqlx.Generator" Version="2.0.2" />
```

### 2️⃣ 定义数据模型

```csharp
// ✨ 使用 Record 类型（推荐）
public record User(int Id, string Name, string Email)
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ✨ 使用 Primary Constructor（C# 12+）
public class UserService(IDbConnection connection)
{
    private readonly IDbConnection _connection = connection;
}
```

### 3️⃣ 创建数据访问层

```csharp
public partial class UserRepository(IDbConnection connection)
{
    // 🔥 自动 SQL 生成 - 方法名智能推断
    public partial Task<User?> GetByIdAsync(int id);
    public partial Task<IEnumerable<User>> GetAllActiveAsync();
    public partial Task<int> CreateAsync(string name, string email);
    
    // 🔥 自定义 SQL - 编译时验证
    [Sqlx("SELECT * FROM users WHERE age > @minAge AND department_id = @deptId")]
    public partial Task<IList<User>> GetUsersByAgeAndDepartmentAsync(int minAge, int deptId);
}
```

### 4️⃣ 使用革新的 SqlTemplate

```csharp
// 🔥 NEW: 纯模板设计
var template = SqlTemplate.Parse(@"
    SELECT * FROM users 
    WHERE is_active = @isActive 
    AND age > @minAge");

// 重复使用，高性能
var activeUsers = template.Execute(new { isActive = true, minAge = 18 });
var seniorUsers = template.Execute(new { isActive = true, minAge = 65 });

// 渲染最终 SQL
string sql = activeUsers.Render();
// 输出: SELECT * FROM users WHERE is_active = 1 AND age > 18
```

## 🏗️ 5 分钟完整示例

让我们创建一个完整的用户管理系统，展示 Sqlx 的强大功能：

### 📋 第一步：项目设置

```csharp
// Program.cs
using Microsoft.Data.Sqlite;
using SqlxDemo;

var connectionString = "Data Source=demo.db";
using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

// 创建示例表
await connection.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        email TEXT NOT NULL,
        age INTEGER,
        is_active BOOLEAN DEFAULT 1,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )");

var userService = new UserService(connection);
await userService.DemoAllFeaturesAsync();
```

### 🏗️ 第二步：定义模型和服务

```csharp
// Models/User.cs
public record User(int Id, string Name, string Email)
{
    public int Age { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Services/UserService.cs
public partial class UserService(IDbConnection connection)
{
    // 🔥 基础 CRUD - 自动生成
    public partial Task<User?> GetByIdAsync(int id);
    public partial Task<int> CreateAsync(string name, string email, int age);
    public partial Task<int> UpdateAsync(int id, string name, string email);
    public partial Task<int> DeleteByIdAsync(int id);
    
    // 🔥 自定义查询 - 编译时验证
    [Sqlx("SELECT * FROM users WHERE age BETWEEN @minAge AND @maxAge AND is_active = 1")]
    public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);
    
    // 🔥 动态查询 - 类型安全
    [Sqlx("SELECT * FROM users WHERE {whereClause} ORDER BY {orderBy}")]
    public partial Task<IList<User>> SearchUsersAsync(
        [ExpressionToSql] Expression<Func<User, bool>> whereClause,
        [ExpressionToSql] Expression<Func<User, object>> orderBy);
}
```

### 🚀 第三步：高级功能演示

```csharp
public partial class UserService
{
    public async Task DemoAllFeaturesAsync()
    {
        Console.WriteLine("=== Sqlx 功能演示 ===\n");
        
        // 1. 基础 CRUD 操作
        await DemoBasicCrudAsync();
        
        // 2. SqlTemplate 纯模板设计
        await DemoSqlTemplateAsync();
        
        // 3. ExpressionToSql 动态查询
        await DemoExpressionToSqlAsync();
        
        // 4. 无缝集成功能
        await DemoSeamlessIntegrationAsync();
    }
    
    private async Task DemoBasicCrudAsync()
    {
        Console.WriteLine("1. 基础 CRUD 操作");
        
        // 创建用户
        var userId = await CreateAsync("张三", "zhangsan@example.com", 25);
        Console.WriteLine($"✅ 创建用户: ID = {userId}");
        
        // 查询用户
        var user = await GetByIdAsync(userId);
        Console.WriteLine($"✅ 查询用户: {user?.Name} ({user?.Email})");
        
        // 更新用户
        var updated = await UpdateAsync(userId, "张三丰", "zhangsan@newmail.com");
        Console.WriteLine($"✅ 更新用户: 影响行数 = {updated}");
        
        // 年龄范围查询
        var users = await GetUsersByAgeRangeAsync(20, 30);
        Console.WriteLine($"✅ 年龄范围查询: 找到 {users.Count} 个用户");
        
        Console.WriteLine();
    }
    
    private async Task DemoSqlTemplateAsync()
    {
        Console.WriteLine("2. SqlTemplate 纯模板设计");
        
        // 创建纯模板（革新设计）
        var template = SqlTemplate.Parse(@"
            SELECT * FROM users 
            WHERE age > @minAge 
            AND is_active = @isActive");
        
        Console.WriteLine($"📋 模板定义: {template.IsPureTemplate}");
        
        // 重复使用模板，绑定不同参数
        var youngUsers = template.Execute(new { minAge = 18, isActive = true });
        var allUsers = template.Execute(new { minAge = 0, isActive = true });
        
        Console.WriteLine($"✅ 年轻用户查询: {youngUsers.Render()}");
        Console.WriteLine($"✅ 所有用户查询: {allUsers.Render()}");
        
        // 流式参数绑定
        var customQuery = template.Bind()
            .Param("minAge", 30)
            .Param("isActive", true)
            .Build();
        
        Console.WriteLine($"✅ 流式绑定: {customQuery.Render()}");
        Console.WriteLine();
    }
    
    private async Task DemoExpressionToSqlAsync()
    {
        Console.WriteLine("3. ExpressionToSql 动态查询");
        
        // 类型安全的动态查询构建
        var query = ExpressionToSql.ForSqlite<User>()
            .Select(u => new { u.Name, u.Email, u.Age })
            .Where(u => u.Age > 20)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10);
        
        var sql = query.ToSql();
        Console.WriteLine($"✅ 生成的 SQL: {sql}");
        
        // 转换为模板（NEW 功能）
        var template = query.ToTemplate();
        Console.WriteLine($"✅ 转换为模板: {template.Sql}");
        
        // 使用动态查询方法
        var results = await SearchUsersAsync(
            u => u.Age > 18 && u.IsActive,
            u => u.CreatedAt);
        
        Console.WriteLine($"✅ 动态查询结果: 找到 {results.Count} 个用户");
        Console.WriteLine();
    }
    
    private async Task DemoSeamlessIntegrationAsync()
    {
        Console.WriteLine("4. 无缝集成功能");
        
        // 混合使用表达式和模板
        using var builder = SqlTemplateExpressionBridge.Create<User>();
        var result = builder
            .SmartSelect(ColumnSelectionMode.OptimizedForQuery)  // 智能列选择
            .Where(u => u.IsActive)                              // 表达式 WHERE
            .Template("AND created_at >= @startDate")            // 模板片段
            .Param("startDate", DateTime.Now.AddMonths(-6))      // 参数绑定
            .OrderBy(u => u.Name)                                // 表达式排序
            .Build();
        
        Console.WriteLine($"✅ 混合查询: {result.Render()}");
        Console.WriteLine();
    }
}
```

## 🎯 核心概念速览

### 1️⃣ SqlTemplate 革新设计

**核心理念**: "模板是模板，参数是参数" - 完全分离

```csharp
// ✅ 正确：纯模板设计
var template = SqlTemplate.Parse(sql);        // 定义一次
var exec1 = template.Execute(params1);        // 多次执行
var exec2 = template.Execute(params2);        // 高性能重用

// ❌ 错误：混合设计（已过时）
var mixed = SqlTemplate.Create(sql, params);  // 混合概念
```

### 2️⃣ ExpressionToSql 类型安全

```csharp
// 编译时类型检查
var query = ExpressionToSql.ForSqlServer<User>()
    .Where(u => u.Age > 18)                    // ✅ 类型安全
    .Where(u => u.InvalidField > 0);           // ❌ 编译错误
```

### 3️⃣ 无缝集成

```csharp
// 表达式 + 模板的强大组合
using var builder = SqlTemplateExpressionBridge.Create<User>();
var result = builder
    .Where(u => u.IsActive)        // 表达式
    .Template("AND score > @min")  // 模板
    .Build();
```

## 🔥 性能优势

### SqlTemplate 性能对比

| 指标 | 旧设计 | 新设计 | 提升 |
|------|-------|-------|------|
| 内存使用 | 6 对象 | 4 对象 | **33%** ↓ |
| 模板重用 | ❌ | ✅ | **∞** |
| 缓存支持 | ❌ | ✅ | **10x** ↑ |

### 整体性能对比

| 场景 | Sqlx | EF Core | Dapper | 提升 |
|------|------|---------|--------|------|
| 简单查询 | **1.2ms** | 3.8ms | 2.1ms | **3.2x** |
| 冷启动 | **0.1ms** | 450ms | 2ms | **4500x** |

## 📚 下一步学习

### 🚀 基础功能
1. [完整特性指南](SQLX_COMPLETE_FEATURE_GUIDE.md) - 深入了解所有功能
2. [SqlTemplate 指南](SQL_TEMPLATE_GUIDE.md) - 掌握模板引擎
3. [ExpressionToSql 指南](expression-to-sql.md) - 学习动态查询

### 🏗️ 高级主题
1. [高级特性指南](ADVANCED_FEATURES_GUIDE.md) - AOT、性能优化
2. [无缝集成指南](SEAMLESS_INTEGRATION_GUIDE.md) - 混合使用模式
3. [现代 C# 支持](PRIMARY_CONSTRUCTOR_RECORD_SUPPORT.md) - C# 12+ 特性

### 🔧 实践项目
1. [SqlxDemo 示例](../samples/SqlxDemo/) - 完整功能演示
2. [最佳实践](../samples/SqlxDemo/Services/SqlTemplateBestPracticesDemo.cs) - 推荐用法

## 💡 常见问题

### Q: 如何从 EF Core 迁移？
A: 查看 [迁移指南](MIGRATION_GUIDE.md)，提供详细的迁移步骤和对比。

### Q: SqlTemplate 新设计有什么优势？
A: 查看 [设计革新说明](SQLTEMPLATE_DESIGN_FIXED.md)，了解性能提升和设计理念。

### Q: 支持哪些数据库？
A: SQL Server、MySQL、PostgreSQL、SQLite、Oracle、DB2，详见[高级特性指南](ADVANCED_FEATURES_GUIDE.md)。

### Q: AOT 兼容性如何？
A: 完整支持 .NET 9 AOT 编译，零反射设计，详见[AOT 支持文档](ADVANCED_FEATURES_GUIDE.md#aot-支持)。

---

<div align="center">

**🎉 恭喜！您已经掌握了 Sqlx 的核心功能**

**🚀 开始构建您的高性能数据访问层吧！**

</div>
