# Sqlx Best Practices Guide

This guide provides best practices and recommended patterns for using Sqlx.

## 🎯 Choosing the Right Usage Pattern

Sqlx offers three core usage patterns. Choosing the right pattern is key to success.

### Decision Flow Chart
```
Need to execute SQL query?
├── Is it a one-time simple query?
│   └── Use ParameterizedSql.Create() [Direct Execution Pattern]
├── Need to reuse the same SQL?
│   └── Use SqlTemplate.Parse() [Static Template Pattern]
└── Need to build query conditions dynamically?
    └── Use ExpressionToSql<T>.Create() [Dynamic Template Pattern]
```

### Scenario Mapping

| Scenario | Recommended Pattern | Example |
|----------|-------------------|---------|
| Simple query, one-time use | Direct Execution | Get user by ID |
| Fixed SQL, multiple uses | Static Template | Pagination, reports |
| Search functionality, dynamic conditions | Dynamic Template | User search, advanced filtering |
| Complex business logic | Static Template | Stored procedure calls, complex JOINs |
| Batch operations | Dynamic Template | Bulk insert, bulk update |

---

## 🚀 Performance Best Practices

### 1. Template Reuse Strategy

```csharp
// ✅ Best Practice: Global template cache
public static class QueryTemplates
{
    // Common query templates
    public static readonly SqlTemplate GetUserById =
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

    public static readonly SqlTemplate GetActiveUsers =
        SqlTemplate.Parse("SELECT * FROM Users WHERE IsActive = @active");

    public static readonly SqlTemplate SearchUsers =
        SqlTemplate.Parse(@"SELECT * FROM Users
                           WHERE (@name IS NULL OR Name LIKE @name)
                           AND (@minAge IS NULL OR Age >= @minAge)
                           ORDER BY Name");
}

// Usage
public class UserService
{
    public User? GetUser(int id)
    {
        var sql = QueryTemplates.GetUserById.Execute(new { id });
        // Execute with your data access method
    }

    public List<User> SearchUsers(string? name = null, int? minAge = null)
    {
        var sql = QueryTemplates.SearchUsers.Execute(new {
            name = name != null ? $"%{name}%" : null,
            minAge
        });
        // Execute with your data access method
    }
}
```

### 2. Dynamic Query Optimization

```csharp
// ✅ Build base query once, reuse with different conditions
public class UserSearchService
{
    private readonly SqlTemplate _baseTemplate;

    public UserSearchService()
    {
        // Convert dynamic query to reusable template
        var baseQuery = ExpressionToSql<User>.ForSqlServer()
            .Select(u => new { u.Id, u.Name, u.Email, u.Age })
            .Where(u => u.IsActive);

        _baseTemplate = baseQuery.ToTemplate();
    }

    public List<User> GetActiveUsers()
    {
        var sql = _baseTemplate.Execute();
        // Execute query
    }
}
```

### 3. Parameterized Query Patterns

```csharp
// ✅ Use parameterized mode for better performance
public class HighPerformanceQueries
{
    public string BuildOptimizedSearch(string department, int minAge)
    {
        return ExpressionToSql<User>.ForSqlServer()
            .UseParameterizedQueries()  // Enable parameterized mode
            .Where(u => u.Department == department && u.Age >= minAge)
            .Select(u => new { u.Name, u.Email })
            .ToSql();
    }
}
```

---

## 🛡️ AOT Compatibility Guidelines

### 1. Prefer Explicit Column Specification

```csharp
// ✅ AOT-Friendly: Explicit column specification
var insertQuery = ExpressionToSql<User>.ForSqlServer()
    .Insert(u => new { u.Name, u.Email, u.Age })
    .Values("John", "john@example.com", 30);

// ⚠️ Avoid in AOT: Uses reflection
var autoInsert = ExpressionToSql<User>.ForSqlServer()
    .InsertAll()  // Reflection-based - avoid in AOT scenarios
    .Values("John", "john@example.com", 30, true, DateTime.Now);
```

### 2. Type-Safe Expression Building

```csharp
// ✅ Type-safe and AOT-compatible
public static class TypeSafeQueries
{
    public static string GetUsersByDepartment<T>(string department)
        where T : class
    {
        return ExpressionToSql<T>.ForSqlServer()
            .Where(u => EF.Property<string>(u, "Department") == department)
            .Select(u => new {
                Id = EF.Property<int>(u, "Id"),
                Name = EF.Property<string>(u, "Name")
            })
            .ToSql();
    }
}
```

---

## 🎨 Code Organization Patterns

### 1. Repository Pattern with Sqlx

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> SearchAsync(UserSearchCriteria criteria);
    Task<int> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    // Pre-compiled templates for better performance
    private static readonly SqlTemplate GetByIdTemplate =
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

    private static readonly SqlTemplate CreateTemplate =
        SqlTemplate.Parse(@"INSERT INTO Users (Name, Email, Age, IsActive)
                           VALUES (@name, @email, @age, @isActive)");

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        var sql = GetByIdTemplate.Execute(new Dictionary<string, object?> { ["@id"] = id });
        return await _connection.QueryFirstOrDefaultAsync<User>(sql.Sql, sql.Parameters);
    }

    public async Task<List<User>> SearchAsync(UserSearchCriteria criteria)
    {
        var query = ExpressionToSql<User>.ForSqlServer()
            .Select(u => new { u.Id, u.Name, u.Email, u.Age });

        // Build dynamic conditions
        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(u => u.Name.Contains(criteria.Name));

        if (criteria.MinAge.HasValue)
            query = query.Where(u => u.Age >= criteria.MinAge.Value);

        if (criteria.Department != null)
            query = query.Where(u => u.Department == criteria.Department);

        query = query.OrderBy(u => u.Name).Take(criteria.PageSize).Skip(criteria.Offset);

        var sql = query.ToSql();
        return await _connection.QueryAsync<User>(sql).ToListAsync();
    }

    public async Task<int> CreateAsync(User user)
    {
        var sql = CreateTemplate.Execute(new Dictionary<string, object?>
        {
            ["@name"] = user.Name,
            ["@email"] = user.Email,
            ["@age"] = user.Age,
            ["@isActive"] = user.IsActive
        });
        return await _connection.ExecuteAsync(sql.Sql, sql.Parameters);
    }
}
```

### 2. Service Layer Pattern

```csharp
public class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto?> GetUserAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<PagedResult<UserDto>> SearchUsersAsync(UserSearchRequest request)
    {
        var criteria = new UserSearchCriteria
        {
            Name = request.Name,
            MinAge = request.MinAge,
            Department = request.Department,
            PageSize = request.PageSize,
            Offset = request.Page * request.PageSize
        };

        var users = await _repository.SearchAsync(criteria);
        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = await _repository.GetSearchCountAsync(criteria)
        };
    }
}
```

---

## 🔍 Error Handling and Validation

### 1. Input Validation

```csharp
public class SafeUserService
{
    public async Task<User?> GetUserAsync(int id)
    {
        // Validate input
        if (id <= 0)
            throw new ArgumentException("User ID must be positive", nameof(id));

        try
        {
            var sql = ParameterizedSql.Create(
                "SELECT * FROM Users WHERE Id = @id",
                new { id });

            return await ExecuteQueryAsync<User>(sql);
        }
        catch (SqlException ex)
        {
            // Log and handle SQL errors
            _logger.LogError(ex, "Failed to get user {UserId}", id);
            throw new DataAccessException($"Failed to retrieve user {id}", ex);
        }
    }
}
```

### 2. SQL Injection Prevention

```csharp
// ✅ Safe: Always use parameterized queries
public string SafeSearch(string userInput)
{
    return ParameterizedSql.Create(
        "SELECT * FROM Users WHERE Name LIKE @search",
        new { search = $"%{userInput}%" }).Render();
}

// ❌ Dangerous: Never concatenate user input directly
public string UnsafeSearch(string userInput)
{
    return $"SELECT * FROM Users WHERE Name LIKE '%{userInput}%'";  // SQL Injection risk!
}
```

---

## 📊 Testing Strategies

### 1. Unit Testing Templates

```csharp
[TestClass]
public class UserQueryTests
{
    [TestMethod]
    public void GetUserById_GeneratesCorrectSQL()
    {
        // Arrange
        var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

        // Act
        var sql = template.Execute(new { id = 123 });

        // Assert
        Assert.AreEqual("SELECT * FROM Users WHERE Id = 123", sql.Render());
    }

    [TestMethod]
    public void SearchUsers_WithMultipleConditions_GeneratesCorrectSQL()
    {
        // Arrange
        var query = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age > 18 && u.IsActive && u.Name.Contains("John"))
            .Select(u => new { u.Id, u.Name });

        // Act
        var sql = query.ToSql();

        // Assert
        StringAssert.Contains(sql, "WHERE ([Age] > 18 AND [IsActive] = 1");
        StringAssert.Contains(sql, "SELECT [Id], [Name]");
    }
}
```

### 2. Integration Testing

```csharp
[TestClass]
public class UserRepositoryIntegrationTests
{
    private IDbConnection _connection;
    private UserRepository _repository;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqlConnection(TestConnectionString);
        _repository = new UserRepository(_connection);
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = await CreateTestUserAsync();

        // Act
        var user = await _repository.GetByIdAsync(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }
}
```

---

## 🔧 Configuration and Setup

### 1. Dependency Injection Setup

```csharp
// Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register database connection
    services.AddScoped<IDbConnection>(provider =>
        new SqlConnection(Configuration.GetConnectionString("DefaultConnection")));

    // Register repositories and services
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<UserService>();

    // Configure default dialect (if needed)
    services.AddSingleton<SqlDialect>(SqlDefine.SqlServer);
}
```

### 2. Logging Integration

```csharp
public class LoggingUserRepository : IUserRepository
{
    private readonly IUserRepository _repository;
    private readonly ILogger<LoggingUserRepository> _logger;

    public LoggingUserRepository(IUserRepository repository, ILogger<LoggingUserRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Getting user by ID: {UserId}", id);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var user = await _repository.GetByIdAsync(id);
            _logger.LogDebug("Retrieved user {UserId} in {ElapsedMs}ms", id, stopwatch.ElapsedMilliseconds);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", id);
            throw;
        }
    }
}
```

---

## 📈 Performance Monitoring

### 1. Query Performance Tracking

```csharp
public class PerformanceTrackingService
{
    private readonly ILogger _logger;

    public async Task<T> ExecuteWithTracking<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            _logger.LogInformation(
                "Operation {OperationName} completed in {ElapsedMs}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Operation {OperationName} failed after {ElapsedMs}ms",
                operationName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 2. Cache-Friendly Patterns

```csharp
public class CachedUserService
{
    private readonly IMemoryCache _cache;
    private readonly IUserRepository _repository;

    public async Task<User?> GetUserAsync(int id)
    {
        var cacheKey = $"user:{id}";

        if (_cache.TryGetValue(cacheKey, out User? cachedUser))
            return cachedUser;

        var user = await _repository.GetByIdAsync(id);
        if (user != null)
        {
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(10));
        }

        return user;
    }
}
```

---

## 🎯 Common Patterns Summary

### When to Use Each Pattern

1. **Direct Execution (`ParameterizedSql`)**
   - Simple, one-time queries
   - Quick prototyping
   - Simple CRUD operations

2. **Static Templates (`SqlTemplate`)**
   - Reusable queries with parameters
   - Report queries
   - Stored procedure calls
   - Performance-critical paths

3. **Dynamic Templates (`ExpressionToSql<T>`)**
   - Search functionality
   - Complex filtering
   - Conditional query building
   - Type-safe query construction

### Performance Guidelines

- **Cache templates** for frequently used queries
- **Use parameterized mode** for dynamic queries
- **Prefer explicit columns** over reflection-based approaches
- **Convert dynamic queries to templates** when possible
- **Monitor query performance** and optimize hot paths

### Security Best Practices

- **Always use parameterized queries** to prevent SQL injection
- **Validate inputs** before query execution
- **Use type-safe expressions** when building dynamic queries
- **Log and monitor** for security events

---

Following these best practices will help you build robust, performant, and maintainable applications with Sqlx.
