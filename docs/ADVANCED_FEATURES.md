# Sqlx 3.0 Advanced Features Guide

This guide covers advanced features and best practices for Sqlx.

## 🚀 AOT (Ahead-Of-Time) Optimization

Sqlx 3.0 fully supports .NET AOT compilation for optimal performance.

### AOT-Friendly Design
```csharp
// ✅ AOT-friendly: Explicit column specification
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email, u.Age })
    .Values("John", "john@example.com", 30);

// ❌ Avoid in AOT: Relies on reflection
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertIntoAll()  // Uses reflection to get all properties
    .Values("John", "john@example.com", 30, true, DateTime.Now);
```

### AOT Compilation Configuration
```xml
<!-- Enable AOT in project file -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PublishAot>true</PublishAot>
    <TrimMode>link</TrimMode>
  </PropertyGroup>
</Project>
```

### AOT Performance Benefits
- **Zero reflection overhead** at runtime
- **Faster startup times** with pre-compiled code
- **Smaller deployment size** with trimmed dependencies
- **Predictable performance** without JIT compilation delays

---

## 🎯 Type-Safe Query Building

### Complex Expression Support
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public Department Department { get; set; } = null!;
    public List<Order> Orders { get; set; } = new();
}

// Complex conditions with type safety
var advancedQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.BirthDate > DateTime.Now.AddYears(-30))
    .Where(u => u.Department.Name == "Engineering")
    .Where(u => u.Orders.Any(o => o.Amount > 1000))
    .Select(u => new { 
        u.Id, 
        u.Name, 
        Age = DateTime.Now.Year - u.BirthDate.Year,
        DepartmentName = u.Department.Name
    })
    .OrderByDescending(u => u.BirthDate)
    .Take(50);
```

### Method Call Translation
```csharp
// String methods
var nameQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Name.StartsWith("John"))
    .Where(u => u.Email.Contains("@company.com"))
    .Where(u => u.Name.Length > 5);

// Date methods
var dateQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.CreatedAt.Year == 2024)
    .Where(u => u.CreatedAt.Month >= 6)
    .Where(u => u.UpdatedAt.Date == DateTime.Today);

// Math methods
var mathQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => Math.Abs(u.Balance) > 1000)
    .Where(u => Math.Round(u.Score, 2) >= 85.5);
```

---

## 🔄 Template Conversion and Optimization

### Dynamic to Static Template Conversion
```csharp
public class QueryOptimizer
{
    // Convert complex dynamic query to reusable template
    public static SqlTemplate CreateUserSearchTemplate()
    {
        var baseQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Email, u.Department })
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name);
            
        return baseQuery.ToTemplate();
    }
    
    // Use parameterized queries for better caching
    public static string CreateOptimizedSearch()
    {
        return ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .UseParameterizedQueries()
            .Where(u => u.Department == "IT")
            .Select(u => new { u.Name, u.Email })
            .ToSql();
    }
}
```

### Template Caching Strategy
```csharp
public static class TemplateCache
{
    private static readonly ConcurrentDictionary<string, SqlTemplate> _cache = new();
    
    public static SqlTemplate GetOrCreate(string key, Func<SqlTemplate> factory)
    {
        return _cache.GetOrAdd(key, _ => factory());
    }
    
    // Usage
    public static SqlTemplate GetUserSearchTemplate()
    {
        return GetOrCreate("user_search", () => 
            SqlTemplate.Parse(@"SELECT Id, Name, Email FROM Users 
                               WHERE IsActive = 1 
                               AND (@department IS NULL OR Department = @department)
                               ORDER BY Name"));
    }
}
```

---

## 🌐 Multi-Database Support

### Database-Specific Features
```csharp
// SQL Server specific features
var sqlServerQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Id, u.Name })
    .Take(10);  // Uses OFFSET/FETCH

// MySQL specific features  
var mysqlQuery = ExpressionToSql<User>.Create(SqlDefine.MySql)
    .Select(u => new { u.Id, u.Name })
    .Take(10);  // Uses LIMIT

// PostgreSQL specific features
var postgresQuery = ExpressionToSql<User>.Create(SqlDefine.PostgreSql)
    .Select(u => new { u.Id, u.Name })
    .Take(10);  // Uses LIMIT with proper parameter handling
```

### Custom Dialect Creation
```csharp
public static class CustomDialects
{
    // Create custom dialect for specific database
    public static SqlDefine CreateCustomDialect()
    {
        return new SqlDefine
        {
            ColumnWrapper = "`",      // MySQL-style column wrapping
            ParameterPrefix = "@",    // SQL Server-style parameters
            TableWrapper = "`",       // MySQL-style table wrapping
            StringQuote = "'",        // Standard string quotes
            // Additional custom configurations...
        };
    }
    
    // Oracle-optimized queries
    public static string CreateOracleOptimizedQuery<T>() where T : class
    {
        return ExpressionToSql<T>.Create(SqlDefine.Oracle)
            .Select()  // Oracle-specific SELECT optimization
            .ToSql();
    }
}
```

---

## 📊 Advanced GROUP BY and Aggregations

### Complex Aggregation Queries
```csharp
public class AdvancedReporting
{
    public static string CreateSalesReport()
    {
        return ExpressionToSql<Order>.Create(SqlDefine.SqlServer)
            .GroupBy(o => new { o.CustomerId, Year = o.OrderDate.Year })
            .Select(g => new 
            {
                CustomerId = g.Key.CustomerId,
                Year = g.Key.Year,
                TotalOrders = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                AverageAmount = g.Average(o => o.Amount),
                MaxAmount = g.Max(o => o.Amount),
                MinAmount = g.Min(o => o.Amount)
            })
            .Having(g => g.Sum(o => o.Amount) > 10000)
            .OrderByDescending(g => g.Sum(o => o.Amount))
            .ToSql();
    }
    
    public static string CreateDepartmentStats()
    {
        return ExpressionToSql<Employee>.Create(SqlDefine.SqlServer)
            .GroupBy(e => e.Department)
            .Select(g => new
            {
                Department = g.Key,
                EmployeeCount = g.Count(),
                AverageSalary = g.Average(e => e.Salary),
                TotalSalary = g.Sum(e => e.Salary),
                SeniorCount = g.Count(e => e.YearsOfService > 5)
            })
            .ToSql();
    }
}
```

### Window Functions (SQL Server/PostgreSQL)
```csharp
// Advanced window function support
public static string CreateRankingQuery()
{
    return ExpressionToSql<Employee>.Create(SqlDefine.SqlServer)
        .Select(e => new
        {
            e.Name,
            e.Salary,
            e.Department,
            // Window functions can be handled through raw SQL when needed
            Rank = "ROW_NUMBER() OVER (PARTITION BY Department ORDER BY Salary DESC)",
            SalaryPercentile = "PERCENT_RANK() OVER (ORDER BY Salary)"
        })
        .ToSql();
}
```

---

## 🔧 Advanced Parameter Handling

### Complex Parameter Types
```csharp
public class AdvancedParameterHandling
{
    // Handle arrays and lists
    public static string CreateInClauseQuery(List<int> userIds)
    {
        var sql = ParameterizedSql.Create(
            "SELECT * FROM Users WHERE Id IN @userIds",
            new { userIds });
        return sql.Render();
    }
    
    // Handle JSON parameters (modern databases)
    public static string CreateJsonQuery()
    {
        var jsonData = new { name = "John", age = 30 };
        return ParameterizedSql.Create(
            "SELECT * FROM Users WHERE JsonData = @jsonData",
            new { jsonData = JsonSerializer.Serialize(jsonData) }).Render();
    }
    
    // Handle complex object parameters
    public static string CreateComplexParameterQuery()
    {
        var criteria = new SearchCriteria
        {
            Names = new[] { "John", "Jane" },
            AgeRange = new { Min = 18, Max = 65 },
            Departments = new[] { "IT", "Sales" }
        };
        
        var template = SqlTemplate.Parse(@"
            SELECT * FROM Users 
            WHERE Name IN @names 
            AND Age BETWEEN @minAge AND @maxAge 
            AND Department IN @departments");
            
        return template.Execute(new {
            names = criteria.Names,
            minAge = criteria.AgeRange.Min,
            maxAge = criteria.AgeRange.Max,
            departments = criteria.Departments
        }).Render();
    }
}
```

---

## 🚄 Performance Optimization Techniques

### Query Plan Optimization
```csharp
public class QueryOptimization
{
    // Optimize for covering indexes
    public static string CreateIndexOptimizedQuery()
    {
        return ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Email })  // Only select needed columns
            .Where(u => u.IsActive && u.Department == "IT")  // Use indexed columns first
            .OrderBy(u => u.Id)  // Order by clustered index when possible
            .ToSql();
    }
    
    // Batch operations for better performance
    public static string CreateBatchInsert(List<User> users)
    {
        var values = users.Select((u, i) => 
            $"(@name{i}, @email{i}, @age{i})").ToArray();
            
        var parameters = users.SelectMany((u, i) => new[]
        {
            new KeyValuePair<string, object?>($"name{i}", u.Name),
            new KeyValuePair<string, object?>($"email{i}", u.Email),
            new KeyValuePair<string, object?>($"age{i}", u.Age)
        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        return ParameterizedSql.CreateWithDictionary(
            $"INSERT INTO Users (Name, Email, Age) VALUES {string.Join(", ", values)}",
            parameters).Render();
    }
}
```

### Memory-Efficient Patterns
```csharp
public class MemoryOptimization
{
    // Use readonly structs for better memory efficiency
    public readonly struct QueryResult
    {
        public QueryResult(string sql, IReadOnlyDictionary<string, object?> parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }
        
        public string Sql { get; }
        public IReadOnlyDictionary<string, object?> Parameters { get; }
    }
    
    // Optimize string building for large queries
    public static string CreateLargeQuery(IEnumerable<SearchFilter> filters)
    {
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Email });
            
        foreach (var filter in filters)
        {
            switch (filter.Type)
            {
                case FilterType.Name:
                    query = query.Where(u => u.Name.Contains(filter.Value));
                    break;
                case FilterType.Age:
                    query = query.Where(u => u.Age >= int.Parse(filter.Value));
                    break;
                case FilterType.Department:
                    query = query.Where(u => u.Department == filter.Value);
                    break;
            }
        }
        
        return query.ToSql();
    }
}
```

---

## 🔒 Security and Validation

### SQL Injection Prevention
```csharp
public class SecurityPatterns
{
    // Always validate input parameters
    public static string SafeUserSearch(string searchTerm, int maxResults)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term cannot be empty");
            
        if (maxResults <= 0 || maxResults > 1000)
            throw new ArgumentException("Max results must be between 1 and 1000");
        
        // Safe parameterized query
        return ParameterizedSql.Create(
            "SELECT TOP (@maxResults) * FROM Users WHERE Name LIKE @searchTerm",
            new { searchTerm = $"%{searchTerm}%", maxResults }).Render();
    }
    
    // Whitelist approach for dynamic sorting
    public static string SafeDynamicSort(string sortColumn, bool ascending = true)
    {
        var allowedColumns = new[] { "Id", "Name", "Email", "CreatedAt" };
        
        if (!allowedColumns.Contains(sortColumn))
            throw new ArgumentException($"Invalid sort column: {sortColumn}");
            
        var direction = ascending ? "ASC" : "DESC";
        
        return $"SELECT * FROM Users ORDER BY [{sortColumn}] {direction}";
    }
}
```

---

## 📈 Monitoring and Diagnostics

### Query Performance Tracking
```csharp
public class QueryDiagnostics
{
    public static QueryStats TrackQueryExecution(string queryName, Func<string> queryBuilder)
    {
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        
        try
        {
            var sql = queryBuilder();
            stopwatch.Stop();
            
            var memoryAfter = GC.GetTotalMemory(false);
            
            return new QueryStats
            {
                QueryName = queryName,
                GenerationTime = stopwatch.Elapsed,
                MemoryUsed = memoryAfter - memoryBefore,
                SqlLength = sql.Length,
                Success = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new QueryStats
            {
                QueryName = queryName,
                GenerationTime = stopwatch.Elapsed,
                Success = false,
                Error = ex.Message
            };
        }
    }
}

public class QueryStats
{
    public string QueryName { get; set; } = string.Empty;
    public TimeSpan GenerationTime { get; set; }
    public long MemoryUsed { get; set; }
    public int SqlLength { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
```

---

## 🎯 Integration Patterns

### Entity Framework Core Integration
```csharp
public class EfCoreIntegration
{
    public static string CreateEfCompatibleQuery<T>(DbContext context) where T : class
    {
        // Use Sqlx for complex query building, then integrate with EF Core
        var sqlxQuery = ExpressionToSql<T>.Create(SqlDefine.SqlServer)
            .Where(entity => EF.Property<bool>(entity, "IsActive"))
            .Select(entity => new { 
                Id = EF.Property<int>(entity, "Id"),
                Name = EF.Property<string>(entity, "Name")
            })
            .ToSql();
            
        return sqlxQuery;
    }
    
    // Execute raw SQL through EF Core
    public static async Task<List<T>> ExecuteSqlxQuery<T>(DbContext context, string sql) where T : class
    {
        return await context.Set<T>().FromSqlRaw(sql).ToListAsync();
    }
}
```

### Dapper Integration
```csharp
public class DapperIntegration
{
    private readonly IDbConnection _connection;
    
    public DapperIntegration(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<IEnumerable<T>> QueryAsync<T>(SqlTemplate template, object? parameters = null)
    {
        var sql = template.Execute(parameters);
        return await _connection.QueryAsync<T>(sql.Sql, sql.Parameters);
    }
    
    public async Task<T?> QueryFirstOrDefaultAsync<T>(SqlTemplate template, object? parameters = null)
    {
        var sql = template.Execute(parameters);
        return await _connection.QueryFirstOrDefaultAsync<T>(sql.Sql, sql.Parameters);
    }
}
```

---

These advanced features enable you to build sophisticated, high-performance applications with Sqlx 3.0 while maintaining type safety and optimal performance characteristics.