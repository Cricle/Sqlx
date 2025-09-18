# Sqlx 3.0 高级功能指南

本指南介绍Sqlx的高级功能和最佳实践。

## 🚀 AOT (Ahead-Of-Time) 优化

Sqlx 3.0 完全支持.NET的AOT编译，确保最佳性能。

### AOT 友好的设计
```csharp
// ✅ AOT友好：显式指定列
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email, u.Age })
    .Values("John", "john@example.com", 30);

// ❌ 避免在AOT中使用：依赖反射
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertIntoAll()  // 使用反射获取所有属性
    .Values("John", "john@example.com", 30, true, DateTime.Now);
```

### AOT 编译配置
```xml
<!-- 在项目文件中启用AOT -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PublishAot>true</PublishAot>
    <TrimMode>link</TrimMode>
  </PropertyGroup>
</Project>
```

### 泛型约束优化
```csharp
// Sqlx内部使用DynamicallyAccessedMembers确保AOT兼容
public class ExpressionToSql<
#if NET5_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
    T> : ExpressionToSqlBase
{
    // AOT优化的实现
}
```

---

## 🔧 数据库方言深度定制

### 自定义数据库方言
```csharp
// 创建自定义方言
var customDialect = new SqlDialect(
    columnPrefix: "[",      // 列名前缀
    columnSuffix: "]",      // 列名后缀
    stringPrefix: "'",      // 字符串前缀
    stringSuffix: "'",      // 字符串后缀
    parameterPrefix: "@"    // 参数前缀
);

var query = ExpressionToSql<User>.Create(customDialect)
    .Where(u => u.Name == "John");
```

### 方言特性对比
```csharp
// SQL Server: [Name] = @name
SqlDefine.SqlServer.WrapColumn("Name");        // [Name]
SqlDefine.SqlServer.FormatParameter("name");   // @name

// MySQL: `Name` = @name  
SqlDefine.MySql.WrapColumn("Name");            // `Name`
SqlDefine.MySql.FormatParameter("name");       // @name

// PostgreSQL: "Name" = $1
SqlDefine.PostgreSql.WrapColumn("Name");       // "Name"
SqlDefine.PostgreSql.FormatParameter("name");  // $name

// SQLite: [Name] = $name
SqlDefine.SQLite.WrapColumn("Name");           // [Name]
SqlDefine.SQLite.FormatParameter("name");      // $name
```

---

## 🎯 复杂查询构建

### 多条件动态查询
```csharp
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer);

// 基础条件
query = query.Where(u => u.IsActive);

// 动态添加条件
if (!string.IsNullOrEmpty(nameFilter))
{
    query = query.Where(u => u.Name.Contains(nameFilter));
}

if (minAge.HasValue)
{
    query = query.Where(u => u.Age >= minAge.Value);
}

if (departmentIds?.Any() == true)
{
    query = query.Where(u => departmentIds.Contains(u.DepartmentId));
}

// 添加排序和分页
query = query
    .OrderBy(u => u.Name)
    .OrderByDescending(u => u.CreatedAt)
    .Skip(pageSize * pageIndex)
    .Take(pageSize);

string sql = query.ToSql();
```

### 复杂JOIN查询
```csharp
// 虽然ExpressionToSql主要用于单表，但可以通过原始SQL处理JOIN
var joinTemplate = SqlTemplate.Parse(@"
    SELECT u.Name, u.Email, d.DepartmentName
    FROM Users u
    INNER JOIN Departments d ON u.DepartmentId = d.Id
    WHERE u.Age > @minAge
    AND d.Budget > @minBudget
    ORDER BY u.Name");

var result = joinTemplate.Execute(new 
{ 
    minAge = 25, 
    minBudget = 100000 
});
```

### 子查询支持
```csharp
// INSERT SELECT
var insertSelect = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .InsertSelect(@"
        SELECT Name, Email 
        FROM TempUsers 
        WHERE IsValid = 1 AND CreatedAt > DATEADD(day, -7, GETDATE())");

// 使用另一个查询作为子查询
var subQuery = ExpressionToSql<TempUser>.Create(SqlDefine.SqlServer)
    .Select(t => new { t.Name, t.Email })
    .Where(t => t.IsValid && t.CreatedAt > DateTime.Now.AddDays(-7));

var mainInsert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .InsertSelect(subQuery);
```

---

## 📊 GROUP BY 和聚合查询

### 基础分组查询
```csharp
var groupQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .GroupBy(u => u.DepartmentId)
    .Select(g => new
    {
        DepartmentId = g.Key,
        UserCount = g.Count(),
        AvgAge = g.Average(u => u.Age),
        MaxSalary = g.Max(u => u.Salary),
        MinSalary = g.Min(u => u.Salary),
        TotalSalary = g.Sum(u => u.Salary)
    });

string sql = groupQuery.ToSql();
```

### 复杂分组和HAVING
```csharp
var complexGroup = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)  // WHERE在GROUP BY之前
    .GroupBy(u => new { u.DepartmentId, u.JobTitle })
    .Select(g => new
    {
        Department = g.Key.DepartmentId,
        JobTitle = g.Key.JobTitle,
        Count = g.Count(),
        AvgSalary = g.Average(u => u.Salary)
    })
    .Having(g => g.Count() > 5 && g.Average(u => u.Salary) > 50000);  // HAVING在GROUP BY之后

string sql = complexGroup.ToSql();
```

---

## 🔄 模板转换和重用

### ExpressionToSql 转 SqlTemplate
```csharp
// 构建动态查询
var dynamicQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .UseParameterizedQueries()  // 启用参数化模式
    .Where(u => u.Age > 25)
    .Select(u => new { u.Name, u.Email })
    .OrderBy(u => u.Name);

// 转换为可重用模板
var template = dynamicQuery.ToTemplate();

// 重复使用模板
var result1 = template.Execute(new { /* 额外参数 */ });
var result2 = template.Execute(new { /* 不同参数 */ });
```

### 模板缓存策略
```csharp
// 全局模板缓存
public static class TemplateCache
{
    private static readonly ConcurrentDictionary<string, SqlTemplate> _cache = new();
    
    public static SqlTemplate GetOrCreate(string key, string sql)
    {
        return _cache.GetOrAdd(key, _ => SqlTemplate.Parse(sql));
    }
}

// 使用缓存
var template = TemplateCache.GetOrCreate("user_by_age", 
    "SELECT * FROM Users WHERE Age > @age");

var result = template.Execute(new { age = 18 });
```

---

## ⚡ 性能优化技巧

### 1. 模板重用
```csharp
// ✅ 好：重用模板
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user1 = template.Execute(new { id = 1 });
var user2 = template.Execute(new { id = 2 });
var user3 = template.Execute(new { id = 3 });

// ❌ 差：每次创建新实例
var user1 = ParameterizedSql.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
var user2 = ParameterizedSql.Create("SELECT * FROM Users WHERE Id = @id", new { id = 2 });
var user3 = ParameterizedSql.Create("SELECT * FROM Users WHERE Id = @id", new { id = 3 });
```

### 2. 参数化查询
```csharp
// ✅ 好：参数化查询，可被数据库缓存
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .UseParameterizedQueries()
    .Where(u => u.Status == "Active");

var template = query.ToTemplate();

// ❌ 差：内联值，无法缓存
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Status == "Active");  // 直接内联值
```

### 3. 批量操作
```csharp
// 批量插入
var batchInsert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .Values("User1", "user1@example.com")
    .AddValues("User2", "user2@example.com")
    .AddValues("User3", "user3@example.com");

string sql = batchInsert.ToSql();
// 生成: INSERT INTO [User] ([Name], [Email]) VALUES ('User1', 'user1@example.com'), ('User2', 'user2@example.com'), ('User3', 'user3@example.com')
```

### 4. 查询优化
```csharp
// 只选择需要的列
var optimizedQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Id, u.Name })  // 只选择需要的列
    .Where(u => u.IsActive)             // 尽早过滤
    .OrderBy(u => u.Id)                 // 使用索引列排序
    .Take(100);                         // 限制结果集大小
```

---

## 🔒 安全最佳实践

### 1. 参数化查询防止SQL注入
```csharp
// ✅ 安全：使用参数化查询
var safeQuery = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Name = @name", 
    new { name = userInput });

// ❌ 危险：字符串拼接
var dangerousQuery = $"SELECT * FROM Users WHERE Name = '{userInput}'";
```

### 2. 输入验证
```csharp
public static class QueryValidator
{
    public static void ValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty");
            
        if (input.Length > 255)
            throw new ArgumentException("Input too long");
            
        // 检查恶意字符
        var dangerousChars = new[] { ";", "--", "/*", "*/", "xp_", "sp_" };
        if (dangerousChars.Any(input.Contains))
            throw new ArgumentException("Input contains dangerous characters");
    }
}

// 使用验证
QueryValidator.ValidateInput(userInput);
var query = ParameterizedSql.Create("SELECT * FROM Users WHERE Name = @name", 
    new { name = userInput });
```

### 3. 权限控制
```csharp
// 在查询中添加用户权限检查
var secureQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .Where(u => u.TenantId == currentUser.TenantId)  // 多租户隔离
    .Where(u => u.CreatedBy == currentUser.Id || currentUser.IsAdmin)  // 权限检查
    .Select(u => new { u.Id, u.Name, u.Email });
```

---

## 🧪 测试和调试

### 1. SQL 生成测试
```csharp
[Test]
public void Should_Generate_Correct_SQL()
{
    var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
        .Where(u => u.Age > 18)
        .Select(u => u.Name)
        .OrderBy(u => u.Name);
    
    var sql = query.ToSql();
    
    Assert.That(sql, Is.EqualTo("SELECT [Name] FROM [User] WHERE [Age] > 18 ORDER BY [Name] ASC"));
}
```

### 2. 参数化测试
```csharp
[Test]
public void Should_Create_Parameterized_Query()
{
    var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age");
    var result = template.Execute(new { age = 18 });
    
    Assert.That(result.Sql, Is.EqualTo("SELECT * FROM Users WHERE Age > @age"));
    Assert.That(result.Parameters["age"], Is.EqualTo(18));
    
    var rendered = result.Render();
    Assert.That(rendered, Is.EqualTo("SELECT * FROM Users WHERE Age > 18"));
}
```

### 3. 调试辅助
```csharp
// 调试时查看生成的SQL
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 18);

string sql = query.ToSql();
Console.WriteLine($"Generated SQL: {sql}");

// 查看WHERE子句
string whereClause = query.ToWhereClause();
Console.WriteLine($"WHERE clause: {whereClause}");

// 查看额外子句
string additionalClause = query.ToAdditionalClause();
Console.WriteLine($"Additional clauses: {additionalClause}");
```

---

## 📈 性能监控

### 1. 查询性能分析
```csharp
public static class QueryProfiler
{
    public static void ProfileQuery(string description, Func<string> queryBuilder)
    {
        var stopwatch = Stopwatch.StartNew();
        
        string sql = queryBuilder();
        
        stopwatch.Stop();
        
        Console.WriteLine($"{description}: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"SQL: {sql}");
        Console.WriteLine($"SQL Length: {sql.Length} chars");
    }
}

// 使用性能分析
QueryProfiler.ProfileQuery("Complex Query", () =>
{
    return ExpressionToSql<User>.Create(SqlDefine.SqlServer)
        .Where(u => u.Age > 18)
        .Where(u => u.IsActive)
        .Select(u => new { u.Name, u.Email })
        .OrderBy(u => u.Name)
        .Take(100)
        .ToSql();
});
```

### 2. 内存使用监控
```csharp
// 测试模板重用的内存效率
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

// 重用模板 vs 每次创建新实例
var memoryBefore = GC.GetTotalMemory(true);

for (int i = 0; i < 10000; i++)
{
    var result = template.Execute(new { id = i });  // 重用模板
    // vs
    // var result = ParameterizedSql.Create("SELECT * FROM Users WHERE Id = @id", new { id = i });
}

var memoryAfter = GC.GetTotalMemory(true);
Console.WriteLine($"Memory used: {memoryAfter - memoryBefore} bytes");
```

---

## 🎯 生产环境最佳实践

### 1. 连接池配置
```csharp
// 配置连接字符串以优化性能
var connectionString = "Server=localhost;Database=MyApp;" +
                      "Integrated Security=true;" +
                      "Pooling=true;" +           // 启用连接池
                      "Min Pool Size=5;" +        // 最小连接数
                      "Max Pool Size=100;" +      // 最大连接数
                      "Connection Timeout=30;";   // 连接超时
```

### 2. 错误处理
```csharp
public static class SafeQueryExecutor
{
    public static async Task<T> ExecuteQueryAsync<T>(
        IDbConnection connection, 
        ParameterizedSql query, 
        Func<IDbConnection, ParameterizedSql, Task<T>> executor)
    {
        try
        {
            return await executor(connection, query);
        }
        catch (SqlException ex) when (ex.Number == 2) // Timeout
        {
            // 记录超时错误并重试
            Console.WriteLine($"Query timeout: {query.Sql}");
            throw new TimeoutException("Query execution timeout", ex);
        }
        catch (SqlException ex) when (ex.Number == 18456) // Login failed
        {
            // 记录认证错误
            Console.WriteLine("Database authentication failed");
            throw new UnauthorizedAccessException("Database access denied", ex);
        }
        catch (Exception ex)
        {
            // 记录一般错误
            Console.WriteLine($"Query failed: {query.Sql}, Error: {ex.Message}");
            throw;
        }
    }
}
```

### 3. 监控和日志
```csharp
public static class QueryLogger
{
    public static void LogQuery(ParameterizedSql query, TimeSpan duration)
    {
        var logEntry = new
        {
            Sql = query.Sql,
            Parameters = query.Parameters,
            Duration = duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        };
        
        // 记录到日志系统
        Console.WriteLine(JsonSerializer.Serialize(logEntry));
        
        // 慢查询警告
        if (duration.TotalMilliseconds > 1000)
        {
            Console.WriteLine($"SLOW QUERY DETECTED: {duration.TotalMilliseconds}ms");
        }
    }
}
```

通过这些高级功能和最佳实践，您可以充分发挥Sqlx 3.0的潜力，构建高性能、安全、可维护的数据访问层。
