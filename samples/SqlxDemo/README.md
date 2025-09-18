# SqlxDemo - Sqlx 框架完整功能演示

<div align="center">

**现代 .NET ORM 框架的全方位展示**

**四大核心特性 · 现代C#语法 · 多数据库支持 · 高性能优化**

[![特性](https://img.shields.io/badge/特性-四大核心-blue)]()
[![语法](https://img.shields.io/badge/C%23-12.0%2B-green)]()
[![数据库](https://img.shields.io/badge/数据库-6种支持-orange)]()
[![性能](https://img.shields.io/badge/性能-毫秒级-red)]()

</div>

---

## 📋 项目概览

本项目全面展示了 Sqlx 源生成器的所有核心特性，通过实际可运行的代码演示每个功能点。所有数据访问代码都由 Sqlx 源生成器在编译时自动生成。

### 🎯 核心演示内容

| 特性分类 | 功能模块 | 文件位置 | 技术亮点 |
|----------|----------|----------|----------|
| **🚀 源生成器** | 四大核心特性 | `Services/` | 编译时代码生成 |
| **🏗️ 现代C#** | C# 12 语法 | `Models/` | Primary Constructor & Record |
| **🌐 多数据库** | 数据库方言 | `Services/MultiDatabase*` | 6种数据库支持 |
| **⚡ 高性能** | 模板引擎 | `Services/Advanced*` | 条件、循环、函数 |

---

## 🚀 四大核心特性演示

### 1️⃣ **[Sqlx] 特性** - 直接 SQL 支持

```csharp
// Services/UserService.cs
public partial class UserService(IDbConnection connection)
{
    // 💡 手写 SQL，自动参数映射
    [Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]
    public partial Task<IList<User>> GetActiveUsersAsync();

    // 💡 复杂查询，类型安全
    [Sqlx("SELECT * FROM [user] WHERE [age] BETWEEN @minAge AND @maxAge")]
    public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

    // 💡 存储过程调用
    [Sqlx("sp_GetUserStatistics")]
    public partial Task<UserStatistics> GetUserStatisticsAsync(int departmentId);
}
```

### 2️⃣ **[SqlExecuteType] 特性** - CRUD 操作自动生成

```csharp
// Services/ProductServices.cs
public partial class ProductService(IDbConnection connection)
{
    // 💡 自动生成 INSERT 语句
    [SqlExecuteType(SqlOperation.Insert, "product")]
    public partial Task<int> CreateProductAsync(string name, decimal price, string description);

    // 💡 自动生成 UPDATE 语句
    [SqlExecuteType(SqlOperation.Update, "product")]
    public partial Task<int> UpdateProductAsync(int id, string name, decimal price);

    // 💡 自动生成 DELETE 语句
    [SqlExecuteType(SqlOperation.Delete, "product")]
    public partial Task<int> DeleteProductAsync(int id);
}
```

### 3️⃣ **[RepositoryFor] 特性** - 仓储模式自动实现

```csharp
// Services/IUserRepository.cs - 接口定义
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<IList<User>> GetAllActiveAsync();
    Task<int> CreateAsync(User user);
    Task<int> UpdateAsync(User user);
    Task<int> DeleteAsync(int id);
}

// Services/UserRepository.cs - 自动实现
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository
{
    // 🔥 所有接口方法的实现都由源生成器自动生成！
    // 智能方法名推断：GetByIdAsync → SELECT * FROM users WHERE id = @id
    // 参数自动映射：CreateAsync(User user) → INSERT INTO users (...)
}
```

### 4️⃣ **[ExpressionToSql] 特性** - LINQ 表达式转 SQL

```csharp
// Services/AdvancedFeatureServices.cs
public partial class AdvancedFeatureService(IDbConnection connection)
{
    // 💡 类型安全的动态查询
    [Sqlx("SELECT * FROM [user] WHERE {whereCondition} ORDER BY [name]")]
    public partial Task<IList<User>> GetUsersByExpressionAsync(
        [ExpressionToSql] Expression<Func<User, bool>> whereCondition);

    // 💡 动态排序和分页
    [Sqlx("SELECT * FROM [user] WHERE {whereCondition} ORDER BY {orderBy}")]
    public partial Task<IList<User>> GetUsersWithDynamicOrderAsync(
        [ExpressionToSql] Expression<Func<User, bool>> whereCondition,
        [ExpressionToSql] Expression<Func<User, object>> orderBy);
}

// 使用示例：
var users = await service.GetUsersByExpressionAsync(u => u.Age > 18 && u.IsActive);
// 自动转换为: WHERE [age] > 18 AND [is_active] = 1

var sorted = await service.GetUsersWithDynamicOrderAsync(
    u => u.DepartmentId == 1,
    u => u.CreatedAt
);
// WHERE [department_id] = 1 ORDER BY [created_at]
```

---

## 🏗️ 现代 C# 语法展示

### Record 类型实体

```csharp
// Models/User.cs - C# 9 Record 语法
public record User(int Id, string Name, string Email)
{
    public int Age { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DepartmentId { get; set; }
}

// Models/Product.cs - 混合 Record 语法
public record Product
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

### Primary Constructor 类

```csharp
// Models/Department.cs - C# 12 Primary Constructor
public class Department(string name, decimal budget)
{
    public int Id { get; set; }
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public decimal Budget { get; } = budget;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<User> Users { get; set; } = [];
}

// Services - Primary Constructor 在服务中的应用
public partial class DepartmentService(
    IDbConnection connection,
    ILogger<DepartmentService> logger,
    IMemoryCache cache
)
{
    // 所有依赖都通过 Primary Constructor 自动注入
    [Sqlx("SELECT * FROM [department] WHERE [budget] > @minBudget")]
    public partial Task<IList<Department>> GetDepartmentsByBudgetAsync(decimal minBudget);
}
```

### 传统类（完全兼容）

```csharp
// Models/Order.cs - 传统类定义
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}
```

---

## 🌐 多数据库支持演示

### 数据库方言自动适配

```csharp
// Services/MultiDatabaseServices.cs
public class MultiDatabaseSupport
{
    // MySQL 方言
    [SqlDefine(SqlDefineTypes.MySql)]
    public partial class MySqlUserService(MySqlConnection connection)
    {
        [Sqlx("SELECT * FROM `user` WHERE `is_active` = 1")]
        public partial Task<IList<User>> GetActiveUsersAsync();
        // 生成: SELECT * FROM `user` WHERE `is_active` = 1
    }

    // SQL Server 方言  
    [SqlDefine(SqlDefineTypes.SqlServer)]
    public partial class SqlServerUserService(SqlConnection connection)
    {
        [Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]
        public partial Task<IList<User>> GetActiveUsersAsync();
        // 生成: SELECT * FROM [user] WHERE [is_active] = 1
    }

    // PostgreSQL 方言
    [SqlDefine(SqlDefineTypes.PostgreSql)]
    public partial class PostgreSqlUserService(NpgsqlConnection connection)
    {
        [Sqlx("SELECT * FROM \"user\" WHERE \"is_active\" = 1")]
        public partial Task<IList<User>> GetActiveUsersAsync();
        // 生成: SELECT * FROM "user" WHERE "is_active" = $1
    }
}
```

### ExpressionToSql 数据库适配

```csharp
// 同一个表达式，自动适配不同数据库
Expression<Func<User, bool>> condition = u => u.Name.Contains("张") && u.Age > 18;

// SQL Server: WHERE [Name] LIKE '%' + @p0 + '%' AND [Age] > @p1
// MySQL: WHERE `Name` LIKE CONCAT('%', @p0, '%') AND `Age` > @p1  
// PostgreSQL: WHERE "Name" ILIKE '%' || $1 || '%' AND "Age" > $2
```

---

## ⚡ 高性能特性演示

### SQL 模板引擎

```csharp
// Services/AdvancedSqlTemplateDemo.cs
public class SqlTemplateDemo(IDbConnection connection)
{
    // 🔥 条件逻辑模板
    public async Task<IList<User>> GetUsersWithConditionalFilterAsync(UserSearchRequest request)
    {
        var template = @"
            SELECT * FROM users WHERE 1=1
            {{if hasNameFilter}}
                AND name LIKE {{namePattern}}
            {{endif}}
            {{if hasAgeFilter}}
                AND age BETWEEN {{minAge}} AND {{maxAge}}
            {{endif}}
            {{if hasStatusFilter}}
                AND status = {{status}}
            {{endif}}";

        var sql = SqlTemplate.Render(template, new {
            hasNameFilter = !string.IsNullOrEmpty(request.NamePattern),
            namePattern = $"%{request.NamePattern}%",
            hasAgeFilter = request.MinAge.HasValue || request.MaxAge.HasValue,
            minAge = request.MinAge ?? 0,
            maxAge = request.MaxAge ?? 999,
            hasStatusFilter = !string.IsNullOrEmpty(request.Status),
            status = request.Status
        });

        return await ExecuteQueryAsync<User>(sql);
    }

    // 🔥 循环模板 - 批量插入
    public async Task<int> BulkInsertUsersAsync(IList<User> users)
    {
        var template = @"
            INSERT INTO users (name, email, age, is_active) VALUES
            {{each user in users}}
                ({{user.Name}}, {{user.Email}}, {{user.Age}}, {{user.IsActive}})
                {{if !@last}}, {{endif}}
            {{endeach}}";

        var sql = SqlTemplate.Render(template, new { users });
        return await ExecuteNonQueryAsync(sql);
    }

    // 🔥 编译模板重用 - 极致性能
    private static readonly CompiledSqlTemplate _userQueryTemplate = 
        SqlTemplate.Compile(@"
            SELECT {{columns}} FROM {{table(tableName)}}
            {{if hasConditions}}
                WHERE 1=1
                {{each condition in conditions}}
                    AND {{condition.Field}} {{condition.Operator}} {{condition.Value}}
                {{endeach}}
            {{endif}}
            ORDER BY {{orderBy}}");

    public async Task<IList<User>> GetUsersOptimizedAsync(string columns, 
        List<QueryCondition> conditions, string orderBy)
    {
        var sql = _userQueryTemplate.Execute(new {
            columns,
            tableName = "users",
            hasConditions = conditions?.Count > 0,
            conditions,
            orderBy
        });

        return await ExecuteQueryAsync<User>(sql);
    }
}

// 查询条件模型
public record QueryCondition(string Field, string Operator, object Value);
public record UserSearchRequest(
    string? NamePattern,
    int? MinAge,
    int? MaxAge,
    string? Status
);
```

### 内置函数演示

```csharp
public async Task<string> DemonstrateBuiltinFunctionsAsync()
{
    var template = @"
        SELECT 
            {{upper(firstName)}} as FirstName,
            {{lower(lastName)}} as LastName,
            {{table(tableName)}} as TableName,
            {{column(columnName)}} as ColumnName,
            {{join(',', columns)}} as JoinedColumns
        FROM {{table(tableName)}}
        WHERE {{column(statusColumn)}} = {{status}}";

    var result = SqlTemplate.Render(template, new {
        firstName = "john",
        lastName = "DOE",
        tableName = "user_profiles", 
        columnName = "full_name",
        columns = new[] { "id", "name", "email" },
        statusColumn = "status",
        status = "active"
    });

    return result.Sql;
    // 生成 SQL Server 方言:
    // SELECT 
    //     JOHN as FirstName,
    //     doe as LastName,
    //     [user_profiles] as TableName,
    //     [full_name] as ColumnName,
    //     id,name,email as JoinedColumns
    // FROM [user_profiles]
    // WHERE [status] = @p0
}
```

---

## 🚀 快速开始

### 1. 环境要求

- **.NET 8.0+** - 最新 .NET 版本
- **C# 12.0+** - 现代 C# 语法支持
- **Visual Studio 2022** 或 **VS Code** - 推荐开发环境

### 2. 构建和运行

```bash
# 克隆项目
git clone https://github.com/your-repo/sqlx.git
cd sqlx/samples/SqlxDemo

# 还原依赖
dotnet restore

# 构建项目（触发源生成器）
dotnet build

# 运行演示
dotnet run
```

### 3. 查看生成的代码

编译完成后，源生成器会在以下位置生成代码：

```
obj/Debug/net9.0/Sqlx.Generator/Sqlx.CSharpGenerator/
├── UserService.g.cs           # UserService 的生成实现
├── ProductService.g.cs        # ProductService 的生成实现
├── UserRepository.g.cs        # UserRepository 的自动实现
└── ...                        # 其他服务的生成代码
```

### 4. 核心功能测试

```csharp
// Program.cs - 完整功能演示
public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. 基础 SQL 查询
        var users = await userService.GetActiveUsersAsync();
        Console.WriteLine($"活跃用户数量: {users.Count}");

        // 2. CRUD 操作
        var newUserId = await userService.CreateUserAsync("张三", "zhang@test.com", 25);
        Console.WriteLine($"新用户ID: {newUserId}");

        // 3. Repository 模式
        var user = await userRepository.GetByIdAsync(newUserId);
        Console.WriteLine($"用户信息: {user?.Name}");

        // 4. 动态查询
        var activeAdults = await advancedService.GetUsersByExpressionAsync(
            u => u.Age >= 18 && u.IsActive);
        Console.WriteLine($"成年活跃用户: {activeAdults.Count}");

        // 5. 模板引擎
        var searchResults = await templateDemo.GetUsersWithConditionalFilterAsync(
            new UserSearchRequest("张", 20, 30, "active"));
        Console.WriteLine($"搜索结果: {searchResults.Count}");
    }
}
```

---

## 📁 项目结构

```
SqlxDemo/
├── Models/                     # 实体模型定义
│   ├── User.cs                # Record 类型实体
│   ├── Product.cs             # 混合 Record 语法
│   ├── Department.cs          # Primary Constructor 类
│   └── Order.cs               # 传统类（兼容性展示）
│
├── Services/                   # 数据服务层
│   ├── UserService.cs         # [Sqlx] 特性演示
│   ├── ProductServices.cs     # [SqlExecuteType] 特性演示
│   ├── AdvancedFeatureServices.cs  # [ExpressionToSql] 特性演示
│   ├── UserRepository.cs      # [RepositoryFor] 特性演示
│   ├── MultiDatabaseServices.cs    # 多数据库方言支持
│   ├── AdvancedSqlTemplateDemo.cs  # SQL 模板引擎演示
│   └── SimpleInterceptorDemo.cs    # 拦截器功能演示
│
├── Extensions/                 # 扩展方法和配置
│   └── DatabaseExtensions.cs  # 数据库配置扩展
│
├── Test/                       # 功能测试
│   └── SqlxDemoTests.cs       # 集成测试示例
│
├── Program.cs                  # 主程序入口
├── GlobalSuppressions.cs      # 全局抑制设置
└── SqlxDemo.csproj            # 项目配置文件
```

---

## 🎯 学习路径建议

### 🏃‍♂️ 初学者路径
1. **查看 Models/** - 了解现代 C# 实体定义
2. **学习 Services/UserService.cs** - 掌握基础 SQL 特性
3. **运行 Program.cs** - 观察实际运行效果

### 🏗️ 进阶开发者路径  
1. **深入 Services/AdvancedFeatureServices.cs** - 掌握表达式转换
2. **研究 Services/AdvancedSqlTemplateDemo.cs** - 学习模板引擎
3. **分析生成的代码** - 理解源生成器原理

### 🚀 架构师路径
1. **研究 Services/MultiDatabaseServices.cs** - 多数据库架构
2. **查看 Extensions/DatabaseExtensions.cs** - 企业级配置
3. **设计自己的数据访问层** - 应用最佳实践

---

## 🔍 故障排除

### 常见问题

| 问题 | 解决方案 |
|------|----------|
| 源生成器未执行 | 清理并重新构建项目 `dotnet clean && dotnet build` |
| 编译错误 | 检查 C# 版本是否为 12.0+，.NET 版本是否为 8.0+ |
| 找不到生成的代码 | 查看 `obj/Debug/net9.0/Sqlx.Generator/` 目录 |
| 数据库连接失败 | 检查连接字符串配置 |

### 调试技巧

```csharp
// 启用详细日志查看源生成器输出
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

---

## 🤝 贡献指南

1. **Fork 项目** - 在 GitHub 上 Fork 本项目
2. **创建分支** - 为新功能创建独立分支
3. **编写代码** - 按照项目规范编写代码
4. **添加测试** - 为新功能添加相应测试
5. **提交 PR** - 提交 Pull Request 等待审核

---

<div align="center">

**🎯 通过完整的功能演示，快速掌握 Sqlx 框架的强大能力！**

**[⬆️ 返回顶部](#sqlxdemo---sqlx-框架完整功能演示) · [🏠 回到首页](../../README.md) · [📚 文档中心](../../docs/README.md)**

</div>