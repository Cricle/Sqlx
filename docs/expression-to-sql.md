# ExpressionToSql 指南

ExpressionToSql 是 Sqlx 的核心功能之一，它允许您使用 LINQ 表达式构建类型安全的动态 SQL 查询。

## 🎯 什么是 ExpressionToSql？

ExpressionToSql 将 C# LINQ 表达式转换为 SQL 语句，提供：

- 🎯 **类型安全** - 编译时检查，避免 SQL 语法错误
- 🔄 **动态查询** - 根据条件动态构建复杂查询
- 🌐 **多数据库支持** - 支持不同数据库方言
- ⚡ **高性能** - 编译时优化，运行时高效

## 🚀 基础用法

### 创建 ExpressionToSql 实例

```csharp
// SQL Server (默认)
var query = ExpressionToSql<User>.Create();
var query = ExpressionToSql<User>.ForSqlServer();

// MySQL
var query = ExpressionToSql<User>.ForMySql();

// PostgreSQL  
var query = ExpressionToSql<User>.ForPostgreSQL();

// SQLite
var query = ExpressionToSql<User>.ForSqlite();
```

### 基本查询构建

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 简单条件查询
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .Where(u => u.Age >= 18);

string sql = query.ToSql();
// 生成: SELECT * FROM [User] WHERE ([IsActive] = 1) AND ([Age] >= 18)

// 排序和分页
var pagedQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Skip(20)
    .Take(10);

string pagedSql = pagedQuery.ToSql();
// 生成: SELECT * FROM [User] WHERE ([IsActive] = 1) ORDER BY [Name] ASC OFFSET 20 LIMIT 10
```

## 🔍 WHERE 条件

### 比较操作

```csharp
var query = ExpressionToSql<User>.ForSqlServer();

// 等于
query.Where(u => u.Id == 1);              // [Id] = 1
query.Where(u => u.Name == "张三");        // [Name] = '张三'

// 不等于
query.Where(u => u.Id != 1);              // [Id] <> 1
query.Where(u => u.Name != "张三");        // [Name] <> '张三'

// 大于/小于
query.Where(u => u.Age > 18);             // [Age] > 18
query.Where(u => u.Age >= 18);            // [Age] >= 18
query.Where(u => u.Age < 65);             // [Age] < 65
query.Where(u => u.Age <= 65);            // [Age] <= 65
```

### 逻辑操作

```csharp
// AND 条件
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive && u.Age >= 18);
// 生成: ([IsActive] = 1) AND ([Age] >= 18)

// OR 条件
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age < 18 || u.Age > 65);
// 生成: ([Age] < 18) OR ([Age] > 65)

// NOT 条件
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => !u.IsActive);
// 生成: NOT ([IsActive] = 1)

// 复杂逻辑组合
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => (u.Age >= 18 && u.Age <= 65) && u.IsActive);
// 生成: (([Age] >= 18) AND ([Age] <= 65)) AND ([IsActive] = 1)
```

### 字符串操作

```csharp
// Contains (LIKE)
query.Where(u => u.Name.Contains("张"));    // [Name] LIKE '%张%'

// StartsWith
query.Where(u => u.Name.StartsWith("张")); // [Name] LIKE '张%'

// EndsWith
query.Where(u => u.Name.EndsWith("三"));    // [Name] LIKE '%三'

// 组合字符串条件
var searchQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Name.Contains("张") || u.Name.Contains("李"))
    .Where(u => u.IsActive);
```

### 日期时间操作

```csharp
var today = DateTime.Today;
var lastWeek = DateTime.Now.AddDays(-7);

// 日期比较
query.Where(u => u.CreatedAt >= today);           // [CreatedAt] >= '2024-01-01'
query.Where(u => u.CreatedAt > lastWeek);         // [CreatedAt] > '2023-12-25'

// 日期范围
var startDate = new DateTime(2024, 1, 1);
var endDate = new DateTime(2024, 12, 31);

query.Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate);
// 生成: ([CreatedAt] >= '2024-01-01') AND ([CreatedAt] <= '2024-12-31')
```

## 📊 排序 (ORDER BY)

### 单列排序

```csharp
// 升序排序
var query = ExpressionToSql<User>.ForSqlServer()
    .OrderBy(u => u.Name);
// 生成: ORDER BY [Name] ASC

// 降序排序  
var query = ExpressionToSql<User>.ForSqlServer()
    .OrderByDescending(u => u.CreatedAt);
// 生成: ORDER BY [CreatedAt] DESC
```

### 多列排序

```csharp
var query = ExpressionToSql<User>.ForSqlServer()
    .OrderBy(u => u.Age)
    .OrderBy(u => u.Name);
// 生成: ORDER BY [Age] ASC, [Name] ASC

// 混合升序降序
var query = ExpressionToSql<User>.ForSqlServer()
    .OrderByDescending(u => u.CreatedAt)
    .OrderBy(u => u.Name);
// 生成: ORDER BY [CreatedAt] DESC, [Name] ASC
```

## 📄 分页 (LIMIT/OFFSET)

### Take 和 Skip

```csharp
// 获取前 10 条记录
var query = ExpressionToSql<User>.ForSqlServer()
    .Take(10);
// 生成: LIMIT 10

// 跳过前 20 条，获取接下来的 10 条
var query = ExpressionToSql<User>.ForSqlServer()
    .Skip(20)
    .Take(10);
// 生成: OFFSET 20 LIMIT 10

// 分页查询示例
int pageNumber = 2;  // 第2页
int pageSize = 20;   // 每页20条

var pagedQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Id)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize);
```

## 🔄 UPDATE 操作

### Set 方法

```csharp
// 设置常量值
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Name, "新名称")
    .Set(u => u.IsActive, true)
    .Where(u => u.Id == 1);

string updateSql = updateQuery.ToSql();
// 生成: UPDATE [User] SET [Name] = '新名称', [IsActive] = 1 WHERE ([Id] = 1)

// 设置表达式值
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Age, u => u.Age + 1)  // 年龄加1
    .Where(u => u.Id == 1);

string updateSql = updateQuery.ToSql();
// 生成: UPDATE [User] SET [Age] = [Age] + 1 WHERE ([Id] = 1)
```

## 🌐 多数据库方言

### 不同数据库的 SQL 差异

```csharp
var condition = u => u.IsActive && u.Age >= 18;

// SQL Server
var sqlServerQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(condition)
    .Take(10);
// 生成: SELECT * FROM [User] WHERE ([IsActive] = 1) AND ([Age] >= 18) OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY

// MySQL
var mysqlQuery = ExpressionToSql<User>.ForMySql()
    .Where(condition)
    .Take(10);
// 生成: SELECT * FROM `User` WHERE (`IsActive` = 1) AND (`Age` >= 18) LIMIT 10

// PostgreSQL
var pgQuery = ExpressionToSql<User>.ForPostgreSQL()
    .Where(condition)
    .Take(10);
// 生成: SELECT * FROM "User" WHERE ("IsActive" = 1) AND ("Age" >= 18) LIMIT 10
```

### 参数前缀差异

```csharp
var name = "张三";

// SQL Server / MySQL (@ 前缀)
var query1 = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Name == name);
// 参数: @p0 = "张三"

// PostgreSQL ($ 前缀)  
var query2 = ExpressionToSql<User>.ForPostgreSQL()
    .Where(u => u.Name == name);
// 参数: $p0 = "张三"
```

## 🔗 与 Repository 集成

### 在 Repository 中使用

```csharp
public interface IUserService
{
    // 使用 ExpressionToSql 参数
    [Sqlx]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
    
    [Sqlx] 
    Task<IList<User>> QueryUsersAsync([ExpressionToSql] ExpressionToSql<User> query, 
        CancellationToken cancellationToken = default);
    
    // 更新操作
    [Sqlx]
    int UpdateUsers([ExpressionToSql] ExpressionToSql<User> updateQuery);
}

[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}
```

### 使用示例

```csharp
var userRepo = new UserRepository(connection);

// 动态查询
var activeAdults = userRepo.QueryUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.IsActive && u.Age >= 18)
        .OrderBy(u => u.Name)
        .Take(50)
);

// 搜索查询
string searchTerm = "张";
var searchResults = userRepo.QueryUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm))
        .OrderByDescending(u => u.CreatedAt)
);

// 异步查询
var recentUsers = await userRepo.QueryUsersAsync(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.CreatedAt >= DateTime.Now.AddDays(-30))
        .OrderByDescending(u => u.CreatedAt)
        .Take(100)
);

// 批量更新
int affectedRows = userRepo.UpdateUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Set(u => u.IsActive, false)
        .Where(u => u.CreatedAt < DateTime.Now.AddYears(-1))
);
```

## 🔧 高级用法

### 动态条件构建

```csharp
public IList<User> SearchUsers(string? name, int? minAge, int? maxAge, bool? isActive)
{
    var query = ExpressionToSql<User>.ForSqlServer();
    
    // 动态添加条件
    if (!string.IsNullOrEmpty(name))
    {
        query = query.Where(u => u.Name.Contains(name));
    }
    
    if (minAge.HasValue)
    {
        query = query.Where(u => u.Age >= minAge.Value);
    }
    
    if (maxAge.HasValue)
    {
        query = query.Where(u => u.Age <= maxAge.Value);
    }
    
    if (isActive.HasValue)
    {
        query = query.Where(u => u.IsActive == isActive.Value);
    }
    
    // 默认排序
    query = query.OrderBy(u => u.Name);
    
    return userRepo.QueryUsers(query);
}
```

### 复杂查询构建器

```csharp
public class UserQueryBuilder
{
    private ExpressionToSql<User> query;
    
    public UserQueryBuilder(string dialect = "sqlserver")
    {
        query = dialect.ToLower() switch
        {
            "mysql" => ExpressionToSql<User>.ForMySql(),
            "postgresql" => ExpressionToSql<User>.ForPostgreSQL(),
            "sqlite" => ExpressionToSql<User>.ForSqlite(),
            _ => ExpressionToSql<User>.ForSqlServer()
        };
    }
    
    public UserQueryBuilder WhereActive(bool isActive = true)
    {
        query = query.Where(u => u.IsActive == isActive);
        return this;
    }
    
    public UserQueryBuilder WhereAgeRange(int minAge, int maxAge)
    {
        query = query.Where(u => u.Age >= minAge && u.Age <= maxAge);
        return this;
    }
    
    public UserQueryBuilder WhereNameContains(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(u => u.Name.Contains(name));
        }
        return this;
    }
    
    public UserQueryBuilder WhereCreatedAfter(DateTime date)
    {
        query = query.Where(u => u.CreatedAt >= date);
        return this;
    }
    
    public UserQueryBuilder OrderByName(bool descending = false)
    {
        query = descending 
            ? query.OrderByDescending(u => u.Name)
            : query.OrderBy(u => u.Name);
        return this;
    }
    
    public UserQueryBuilder OrderByCreatedAt(bool descending = true)
    {
        query = descending
            ? query.OrderByDescending(u => u.CreatedAt)
            : query.OrderBy(u => u.CreatedAt);
        return this;
    }
    
    public UserQueryBuilder Paginate(int pageNumber, int pageSize)
    {
        query = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
        return this;
    }
    
    public ExpressionToSql<User> Build() => query;
    
    public string ToSql() => query.ToSql();
    
    public SqlTemplate ToTemplate() => query.ToTemplate();
}

// 使用查询构建器
var searchQuery = new UserQueryBuilder("sqlserver")
    .WhereActive(true)
    .WhereAgeRange(18, 65)
    .WhereNameContains("张")
    .WhereCreatedAfter(DateTime.Now.AddDays(-30))
    .OrderByCreatedAt(true)
    .Paginate(1, 20)
    .Build();

var users = userRepo.QueryUsers(searchQuery);
```

## 📊 性能优化

### 查询缓存

```csharp
public class CachedUserService
{
    private readonly IUserService userService;
    private readonly IMemoryCache cache;
    
    public CachedUserService(IUserService userService, IMemoryCache cache)
    {
        this.userService = userService;
        this.cache = cache;
    }
    
    public IList<User> GetActiveUsers()
    {
        // 缓存查询结果
        return cache.GetOrCreate("active_users", factory =>
        {
            factory.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            
            return userService.QueryUsers(
                ExpressionToSql<User>.ForSqlServer()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
            );
        });
    }
}
```

### 预编译查询

```csharp
public static class PrecompiledQueries
{
    // 预编译常用查询
    public static readonly ExpressionToSql<User> ActiveUsersQuery = 
        ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name);
    
    public static readonly ExpressionToSql<User> RecentUsersQuery =
        ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.CreatedAt >= DateTime.Now.AddDays(-7))
            .OrderByDescending(u => u.CreatedAt);
    
    public static ExpressionToSql<User> UsersByAgeRange(int minAge, int maxAge) =>
        ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age >= minAge && u.Age <= maxAge)
            .OrderBy(u => u.Age);
}

// 使用预编译查询
var activeUsers = userRepo.QueryUsers(PrecompiledQueries.ActiveUsersQuery);
var recentUsers = userRepo.QueryUsers(PrecompiledQueries.RecentUsersQuery);
var adultsQuery = PrecompiledQueries.UsersByAgeRange(18, 65);
var adults = userRepo.QueryUsers(adultsQuery);
```

## 🐛 调试和故障排除

### 查看生成的 SQL

```csharp
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive && u.Age >= 18)
    .OrderBy(u => u.Name)
    .Take(10);

// 查看生成的 SQL
string sql = query.ToSql();
Console.WriteLine($"生成的 SQL: {sql}");

// 查看 SQL 模板和参数
var template = query.ToTemplate();
Console.WriteLine($"SQL: {template.Sql}");
foreach (var param in template.Parameters)
{
    Console.WriteLine($"参数: {param.ParameterName} = {param.Value}");
}
```

### 常见问题

1. **不支持的表达式**:
```csharp
// ❌ 不支持复杂的方法调用
query.Where(u => u.Name.Substring(0, 1) == "A");

// ✅ 使用支持的字符串方法
query.Where(u => u.Name.StartsWith("A"));
```

2. **类型转换问题**:
```csharp
// ❌ 避免复杂的类型转换
query.Where(u => Convert.ToInt32(u.Name) > 100);

// ✅ 使用正确的属性类型
query.Where(u => u.Age > 100);
```

## 📚 延伸阅读

- [Repository 模式指南](repository-pattern.md)
- [SqlDefine 和 TableName 属性](sqldefine-tablename.md)
- [高级示例](examples/advanced-examples.md)
- [性能优化指南](OPTIMIZATION_GUIDE.md)

---

ExpressionToSql 是构建动态、类型安全查询的强大工具。通过合理使用，您可以构建灵活且高性能的数据访问层！
