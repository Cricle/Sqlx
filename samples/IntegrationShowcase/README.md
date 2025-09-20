# Sqlx Integration Showcase

This project demonstrates real-world integration patterns and best practices for using Sqlx 3.0 in production applications.

## 🎯 Purpose

This showcase goes beyond basic usage to demonstrate:

- **Real-world integration patterns**
- **Performance optimization techniques**
- **Error handling strategies**
- **Testing approaches**
- **Architecture best practices**

## 🏗️ Architecture Patterns

### Repository Pattern
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<PagedResult<User>> SearchAsync(UserSearchCriteria criteria);
    Task<int> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    
    // Pre-compiled templates for performance
    private static readonly SqlTemplate GetByIdTemplate = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
    
    public async Task<User?> GetByIdAsync(int id)
    {
        var sql = GetByIdTemplate.Execute(new { id });
        return await _connection.QueryFirstOrDefaultAsync<User>(sql.Sql, sql.Parameters);
    }
    
    // Dynamic query building for complex searches
    public async Task<PagedResult<User>> SearchAsync(UserSearchCriteria criteria)
    {
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Email, u.Age });

        // Build conditions dynamically
        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(u => u.Name.Contains(criteria.Name));
            
        if (criteria.MinAge.HasValue)
            query = query.Where(u => u.Age >= criteria.MinAge.Value);

        // Apply pagination
        query = query.OrderBy(u => u.Name)
                    .Skip(criteria.Offset)
                    .Take(criteria.PageSize);

        var sql = query.ToSql();
        var users = await _connection.QueryAsync<User>(sql);
        
        return new PagedResult<User>
        {
            Items = users.ToList(),
            TotalCount = await GetSearchCountAsync(criteria)
        };
    }
}
```

### Service Layer Pattern
```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    private readonly IMemoryCache _cache;
    
    public UserService(
        IUserRepository repository, 
        ILogger<UserService> logger,
        IMemoryCache cache)
    {
        _repository = repository;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<UserDto?> GetUserAsync(int id)
    {
        var cacheKey = $"user:{id}";
        
        if (_cache.TryGetValue(cacheKey, out UserDto? cachedUser))
        {
            _logger.LogDebug("User {UserId} retrieved from cache", id);
            return cachedUser;
        }
        
        var user = await _repository.GetByIdAsync(id);
        if (user != null)
        {
            var dto = MapToDto(user);
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
            _logger.LogDebug("User {UserId} cached for 10 minutes", id);
            return dto;
        }
        
        return null;
    }
}
```

## 🚀 Performance Optimizations

### Template Caching
```csharp
public static class QueryTemplates
{
    private static readonly ConcurrentDictionary<string, SqlTemplate> _cache = new();
    
    public static SqlTemplate GetOrCreate(string key, Func<SqlTemplate> factory)
    {
        return _cache.GetOrAdd(key, _ => factory());
    }
    
    // Common query templates
    public static SqlTemplate UserById => GetOrCreate("user_by_id", 
        () => SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id"));
        
    public static SqlTemplate ActiveUsers => GetOrCreate("active_users",
        () => SqlTemplate.Parse("SELECT * FROM Users WHERE IsActive = 1"));
}
```

### Batch Operations
```csharp
public class BatchUserService
{
    public async Task<int> CreateUsersAsync(IEnumerable<User> users)
    {
        var userList = users.ToList();
        if (!userList.Any()) return 0;
        
        // Build batch insert query
        var insertQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .InsertInto(u => new { u.Name, u.Email, u.Age, u.IsActive });
            
        // Generate parameterized values for batch insert
        var valueParams = new List<string>();
        var parameters = new Dictionary<string, object?>();
        
        for (int i = 0; i < userList.Count; i++)
        {
            var user = userList[i];
            valueParams.Add($"(@name{i}, @email{i}, @age{i}, @active{i})");
            parameters[$"name{i}"] = user.Name;
            parameters[$"email{i}"] = user.Email;
            parameters[$"age{i}"] = user.Age;
            parameters[$"active{i}"] = user.IsActive;
        }
        
        var sql = $"{insertQuery.ToSql()} VALUES {string.Join(", ", valueParams)}";
        return await _connection.ExecuteAsync(sql, parameters);
    }
}
```

## 🛡️ Error Handling

### Resilient Data Access
```csharp
public class ResilientUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<ResilientUserService> _logger;
    
    public async Task<User?> GetUserWithRetryAsync(int id, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _repository.GetByIdAsync(id);
            }
            catch (SqlException ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                _logger.LogWarning(ex, 
                    "Transient error on attempt {Attempt}/{MaxRetries} for user {UserId}. Retrying in {Delay}ms", 
                    attempt, maxRetries, id, delay.TotalMilliseconds);
                    
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user {UserId} on attempt {Attempt}", id, attempt);
                throw new DataAccessException($"Failed to retrieve user {id}", ex);
            }
        }
        
        throw new DataAccessException($"Failed to retrieve user {id} after {maxRetries} attempts");
    }
    
    private static bool IsTransientError(SqlException ex)
    {
        // Check for transient SQL error codes
        return ex.Number is 2 or 53 or 121 or 1205;
    }
}
```

## 🧪 Testing Strategies

### Repository Testing
```csharp
[TestClass]
public class UserRepositoryTests
{
    private IDbConnection _connection;
    private UserRepository _repository;
    
    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=TestDb;Integrated Security=true");
        await _connection.OpenAsync();
        _repository = new UserRepository(_connection);
        
        // Setup test data
        await SeedTestDataAsync();
    }
    
    [TestCleanup]
    public async Task Cleanup()
    {
        await CleanupTestDataAsync();
        _connection?.Dispose();
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
        Assert.AreEqual("Test User", user.Name);
    }
    
    [TestMethod]
    public async Task SearchAsync_WithNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        await CreateTestUsersAsync("John Doe", "Jane Smith", "Bob Johnson");
        var criteria = new UserSearchCriteria { Name = "John" };
        
        // Act
        var result = await _repository.SearchAsync(criteria);
        
        // Assert
        Assert.AreEqual(2, result.Items.Count);
        Assert.IsTrue(result.Items.All(u => u.Name.Contains("John")));
    }
}
```

### Integration Testing
```csharp
[TestClass]
public class UserServiceIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    
    [TestInitialize]
    public void Setup()
    {
        var builder = new WebHostBuilder()
            .UseStartup<TestStartup>()
            .ConfigureServices(services =>
            {
                services.AddDbContext<TestDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
            
        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }
    
    [TestMethod]
    public async Task GetUser_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var userId = await CreateTestUserViaApiAsync();
        
        // Act
        var response = await _client.GetAsync($"/api/users/{userId}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }
}
```

## 📊 Monitoring and Diagnostics

### Performance Monitoring
```csharp
public class MonitoringUserService : IUserService
{
    private readonly IUserService _innerService;
    private readonly IMetrics _metrics;
    private readonly ILogger<MonitoringUserService> _logger;
    
    public async Task<User?> GetByIdAsync(int id)
    {
        using var activity = Activity.StartActivity("UserService.GetById");
        activity?.SetTag("user.id", id.ToString());
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var user = await _innerService.GetByIdAsync(id);
            
            _metrics.Increment("user_service.get_by_id.success");
            _metrics.Timer("user_service.get_by_id.duration", stopwatch.Elapsed);
            
            _logger.LogDebug("Retrieved user {UserId} in {Duration}ms", 
                id, stopwatch.ElapsedMilliseconds);
                
            return user;
        }
        catch (Exception ex)
        {
            _metrics.Increment("user_service.get_by_id.error");
            _logger.LogError(ex, "Failed to get user {UserId}", id);
            throw;
        }
    }
}
```

## 🔧 Dependency Injection Setup

### ASP.NET Core Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Database connection
    services.AddScoped<IDbConnection>(provider =>
        new SqlConnection(Configuration.GetConnectionString("DefaultConnection")));
    
    // Repositories
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    
    // Services
    services.AddScoped<IUserService, UserService>();
    services.Decorate<IUserService, MonitoringUserService>();
    services.Decorate<IUserService, CachingUserService>();
    
    // Caching
    services.AddMemoryCache();
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = Configuration.GetConnectionString("Redis");
    });
    
    // Logging and monitoring
    services.AddLogging();
    services.AddMetrics();
}
```

## 🏃‍♂️ Running the Showcase

```bash
cd samples/IntegrationShowcase
dotnet restore
dotnet build
dotnet run
```

### What You'll See

1. **Startup Configuration** - Dependency injection setup
2. **Repository Pattern Demo** - CRUD operations with templates
3. **Service Layer Demo** - Business logic with caching
4. **Performance Monitoring** - Execution timing and metrics
5. **Error Handling Demo** - Resilient data access patterns
6. **Testing Examples** - Unit and integration test execution

## 📚 Key Takeaways

### Architecture Principles
- **Separation of Concerns** - Clear layer boundaries
- **Dependency Injection** - Loose coupling and testability
- **Repository Pattern** - Data access abstraction
- **Service Layer** - Business logic encapsulation

### Performance Strategies
- **Template Caching** - Reuse compiled templates
- **Connection Pooling** - Efficient database connections
- **Batch Operations** - Reduce database round trips
- **Response Caching** - Cache frequently accessed data

### Quality Practices
- **Comprehensive Testing** - Unit, integration, and performance tests
- **Error Handling** - Graceful failure management
- **Monitoring** - Observability and diagnostics
- **Documentation** - Clear code and API documentation

---

**🎯 This showcase demonstrates production-ready patterns for building robust, performant applications with Sqlx 3.0!**
