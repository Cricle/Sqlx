# Sqlx 3.0 最佳实践指南

本指南提供使用Sqlx 3.0的最佳实践和推荐模式。

## 🎯 选择合适的使用模式

Sqlx 3.0提供三种核心使用模式，选择合适的模式是成功的关键。

### 决策流程图
```
需要执行SQL查询？
├── 是一次性简单查询？
│   └── 使用 ParameterizedSql.Create() [直接执行模式]
├── 需要重复使用相同SQL？
│   └── 使用 SqlTemplate.Parse() [静态模板模式]
└── 需要动态构建查询条件？
    └── 使用 ExpressionToSql<T>.Create() [动态模板模式]
```

### 具体场景对照

| 场景 | 推荐模式 | 示例 |
|------|---------|------|
| 简单查询，一次性使用 | 直接执行 | 根据ID查询用户 |
| 固定SQL，多次使用 | 静态模板 | 分页查询、报表查询 |
| 搜索功能，条件动态 | 动态模板 | 用户搜索、高级筛选 |
| 复杂业务逻辑 | 静态模板 | 存储过程调用、复杂JOIN |
| 批量操作 | 动态模板 | 批量插入、批量更新 |

---

## 🚀 性能最佳实践

### 1. 模板重用策略

```csharp
// ✅ 最佳实践：全局模板缓存
public static class QueryTemplates
{
    // 常用查询模板
    public static readonly SqlTemplate GetUserById = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
    
    public static readonly SqlTemplate GetActiveUsers = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE IsActive = @active ORDER BY Name");
    
    public static readonly SqlTemplate SearchUsers = 
        SqlTemplate.Parse(@"
            SELECT * FROM Users 
            WHERE (@name IS NULL OR Name LIKE @name)
            AND (@minAge IS NULL OR Age >= @minAge)
            ORDER BY Name 
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
}

// 使用缓存的模板
public class UserService
{
    public async Task<User> GetByIdAsync(int id)
    {
        var query = QueryTemplates.GetUserById.Execute(new { id });
        return await connection.QuerySingleAsync<User>(query.Render());
    }
    
    public async Task<List<User>> SearchAsync(string name, int? minAge, int page, int pageSize)
    {
        var query = QueryTemplates.SearchUsers.Execute(new 
        { 
            name = string.IsNullOrEmpty(name) ? null : $"%{name}%",
            minAge,
            offset = page * pageSize,
            pageSize
        });
        return (await connection.QueryAsync<User>(query.Render())).ToList();
    }
}
```

### 2. 参数化查询优化

```csharp
// ✅ 最佳实践：参数化查询，利用数据库查询计划缓存
public class OptimizedUserService
{
    private readonly SqlTemplate _searchTemplate;
    
    public OptimizedUserService()
    {
        // 构造函数中初始化模板
        _searchTemplate = SqlTemplate.Parse(@"
            SELECT Id, Name, Email, Age 
            FROM Users 
            WHERE IsActive = 1
            AND (@departmentId IS NULL OR DepartmentId = @departmentId)
            AND (@keyword IS NULL OR Name LIKE @keyword OR Email LIKE @keyword)
            ORDER BY Name
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
    }
    
    public async Task<PagedResult<User>> SearchAsync(SearchCriteria criteria)
    {
        var query = _searchTemplate.Execute(new
        {
            departmentId = criteria.DepartmentId,
            keyword = string.IsNullOrEmpty(criteria.Keyword) ? null : $"%{criteria.Keyword}%",
            offset = criteria.Page * criteria.PageSize,
            pageSize = criteria.PageSize
        });
        
        // 使用原始SQL执行，让数据库缓存查询计划
        var users = await connection.QueryAsync<User>(query.Render());
        var total = await GetTotalCountAsync(criteria);
        
        return new PagedResult<User>(users.ToList(), total, criteria.Page, criteria.PageSize);
    }
}
```

### 3. 批量操作优化

```csharp
// ✅ 最佳实践：批量操作减少数据库往返
public class BatchOperationService
{
    public async Task<int> BulkInsertUsersAsync(List<User> users)
    {
        if (!users.Any()) return 0;
        
        // 使用动态模板构建批量插入
        var batchInsert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .InsertInto(u => new { u.Name, u.Email, u.Age, u.IsActive });
        
        // 添加所有用户数据
        foreach (var user in users)
        {
            if (batchInsert.HasValues())
                batchInsert.AddValues(user.Name, user.Email, user.Age, user.IsActive);
            else
                batchInsert.Values(user.Name, user.Email, user.Age, user.IsActive);
        }
        
        string sql = batchInsert.ToSql();
        return await connection.ExecuteAsync(sql);
    }
    
    public async Task<int> BulkUpdateStatusAsync(List<int> userIds, bool isActive)
    {
        if (!userIds.Any()) return 0;
        
        // 使用IN子句批量更新
        var template = SqlTemplate.Parse(@"
            UPDATE Users 
            SET IsActive = @isActive, ModifiedAt = GETDATE()
            WHERE Id IN @userIds");
        
        var query = template.Execute(new { isActive, userIds });
        return await connection.ExecuteAsync(query.Render());
    }
}
```

---

## 🛡️ 安全最佳实践

### 1. 防止SQL注入

```csharp
// ✅ 最佳实践：始终使用参数化查询
public class SecureUserService
{
    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        // ✅ 安全：参数化查询
        var query = ParameterizedSql.Create(
            "SELECT * FROM Users WHERE Name LIKE @searchTerm",
            new { searchTerm = $"%{searchTerm}%" });
        
        return (await connection.QueryAsync<User>(query.Render())).ToList();
    }
    
    // ❌ 危险：永远不要这样做
    public async Task<List<User>> UnsafeSearchUsersAsync(string searchTerm)
    {
        // 这会导致SQL注入漏洞
        var sql = $"SELECT * FROM Users WHERE Name LIKE '%{searchTerm}%'";
        return (await connection.QueryAsync<User>(sql)).ToList();
    }
}
```

### 2. 输入验证和清理

```csharp
public static class InputValidator
{
    public static string ValidateAndSanitizeString(string input, int maxLength = 255)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        // 长度限制
        if (input.Length > maxLength)
            input = input.Substring(0, maxLength);
        
        // 移除危险字符（根据业务需求调整）
        input = input.Replace("'", "''");  // SQL转义
        
        return input.Trim();
    }
    
    public static void ValidateId(int id)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be positive", nameof(id));
    }
    
    public static void ValidatePageParameters(int page, int pageSize)
    {
        if (page < 0)
            throw new ArgumentException("Page must be non-negative", nameof(page));
        
        if (pageSize <= 0 || pageSize > 1000)
            throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));
    }
}

// 使用验证器
public class ValidatedUserService
{
    public async Task<List<User>> SearchUsersAsync(string name, int page, int pageSize)
    {
        // 验证输入
        InputValidator.ValidatePageParameters(page, pageSize);
        name = InputValidator.ValidateAndSanitizeString(name);
        
        var template = SqlTemplate.Parse(@"
            SELECT * FROM Users 
            WHERE (@name = '' OR Name LIKE @name)
            ORDER BY Name
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
        
        var query = template.Execute(new 
        { 
            name = string.IsNullOrEmpty(name) ? "" : $"%{name}%",
            offset = page * pageSize,
            pageSize 
        });
        
        return (await connection.QueryAsync<User>(query.Render())).ToList();
    }
}
```

### 3. 权限和多租户

```csharp
public class TenantAwareUserService
{
    private readonly ICurrentUser _currentUser;
    
    public TenantAwareUserService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }
    
    public async Task<List<User>> GetUsersAsync()
    {
        // 自动添加租户隔离
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Where(u => u.TenantId == _currentUser.TenantId)  // 租户隔离
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Name, u.Email });  // 只返回必要字段
        
        return (await connection.QueryAsync<User>(query.ToSql())).ToList();
    }
    
    public async Task<User> GetUserByIdAsync(int id)
    {
        InputValidator.ValidateId(id);
        
        var template = SqlTemplate.Parse(@"
            SELECT Id, Name, Email, CreatedAt 
            FROM Users 
            WHERE Id = @id 
            AND TenantId = @tenantId 
            AND (CreatedBy = @userId OR @isAdmin = 1)");  // 权限检查
        
        var query = template.Execute(new 
        { 
            id, 
            tenantId = _currentUser.TenantId,
            userId = _currentUser.Id,
            isAdmin = _currentUser.IsAdmin 
        });
        
        return await connection.QuerySingleOrDefaultAsync<User>(query.Render());
    }
}
```

---

## 📊 数据库设计最佳实践

### 1. 索引优化

```csharp
// ✅ 最佳实践：设计查询时考虑索引
public class IndexOptimizedService
{
    public async Task<List<User>> GetUsersByStatusAsync(string status)
    {
        // 假设 (Status, CreatedAt) 有复合索引
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Where(u => u.Status == status)         // 使用索引的第一列
            .OrderBy(u => u.CreatedAt)              // 使用索引的第二列排序
            .Take(100);                             // 限制结果集
        
        return (await connection.QueryAsync<User>(query.ToSql())).ToList();
    }
    
    public async Task<List<User>> SearchWithCoveringIndexAsync(string department, bool isActive)
    {
        // 假设有覆盖索引: (Department, IsActive) INCLUDE (Id, Name, Email)
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Email })  // 只选择索引覆盖的列
            .Where(u => u.Department == department)      // 索引条件1
            .Where(u => u.IsActive == isActive);         // 索引条件2
        
        return (await connection.QueryAsync<User>(query.ToSql())).ToList();
    }
}
```

### 2. 分页最佳实践

```csharp
public class PaginationService
{
    // ✅ 最佳实践：使用OFFSET/FETCH或游标分页
    public async Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize)
    {
        InputValidator.ValidatePageParameters(page, pageSize);
        
        var template = SqlTemplate.Parse(@"
            SELECT Id, Name, Email, CreatedAt
            FROM Users
            WHERE IsActive = 1
            ORDER BY Id  -- 使用主键排序确保稳定性
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY");
        
        var query = template.Execute(new 
        { 
            offset = page * pageSize, 
            pageSize 
        });
        
        var users = await connection.QueryAsync<User>(query.Render());
        var total = await GetTotalActiveUsersAsync();
        
        return new PagedResult<User>(users.ToList(), total, page, pageSize);
    }
    
    // ✅ 更好的实践：游标分页（适用于大数据集）
    public async Task<CursorPagedResult<User>> GetUsersWithCursorAsync(int? afterId, int pageSize)
    {
        var template = SqlTemplate.Parse(@"
            SELECT TOP(@pageSize) Id, Name, Email, CreatedAt
            FROM Users
            WHERE IsActive = 1
            AND (@afterId IS NULL OR Id > @afterId)
            ORDER BY Id");
        
        var query = template.Execute(new { afterId, pageSize });
        var users = (await connection.QueryAsync<User>(query.Render())).ToList();
        
        return new CursorPagedResult<User>
        {
            Items = users,
            HasMore = users.Count == pageSize,
            NextCursor = users.LastOrDefault()?.Id
        };
    }
}
```

---

## 🔧 错误处理和日志

### 1. 结构化错误处理

```csharp
public class RobustUserService
{
    private readonly ILogger<RobustUserService> _logger;
    private readonly IDbConnection _connection;
    
    public RobustUserService(ILogger<RobustUserService> logger, IDbConnection connection)
    {
        _logger = logger;
        _connection = connection;
    }
    
    public async Task<Result<User>> GetUserAsync(int id)
    {
        try
        {
            InputValidator.ValidateId(id);
            
            var template = QueryTemplates.GetUserById;
            var query = template.Execute(new { id });
            
            _logger.LogDebug("Executing query: {Sql}", query.Sql);
            
            var user = await _connection.QuerySingleOrDefaultAsync<User>(query.Render());
            
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                return Result<User>.NotFound($"User with ID {id} not found");
            }
            
            return Result<User>.Success(user);
        }
        catch (SqlException ex) when (ex.Number == -2) // Timeout
        {
            _logger.LogError(ex, "Database timeout while getting user {UserId}", id);
            return Result<User>.Error("Database timeout occurred");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error while getting user {UserId}", id);
            return Result<User>.Error("Database error occurred");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting user {UserId}", id);
            return Result<User>.Error("An unexpected error occurred");
        }
    }
}

// 结果类型
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public int? ErrorCode { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Error(string error, int? code = null) => new() { Error = error, ErrorCode = code };
    public static Result<T> NotFound(string error) => new() { Error = error, ErrorCode = 404 };
}
```

### 2. 查询性能监控

```csharp
public class PerformanceMonitoredService
{
    private readonly ILogger _logger;
    private readonly IMetrics _metrics;
    
    public async Task<List<User>> GetUsersWithMonitoringAsync(SearchCriteria criteria)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationName = "GetUsers";
        
        try
        {
            var query = BuildSearchQuery(criteria);
            var sql = query.Render();
            
            _logger.LogDebug("Executing query: {Sql} with parameters: {@Parameters}", 
                query.Sql, query.Parameters);
            
            var users = await connection.QueryAsync<User>(sql);
            var result = users.ToList();
            
            stopwatch.Stop();
            
            // 记录性能指标
            _metrics.RecordQueryDuration(operationName, stopwatch.ElapsedMilliseconds);
            _metrics.RecordQueryResultCount(operationName, result.Count);
            
            _logger.LogInformation("Query {Operation} completed in {Duration}ms, returned {Count} results",
                operationName, stopwatch.ElapsedMilliseconds, result.Count);
            
            // 慢查询警告
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Slow query detected: {Operation} took {Duration}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordQueryError(operationName);
            _logger.LogError(ex, "Query {Operation} failed after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## 🏗️ 架构模式

### 1. 仓储模式实现

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetByDepartmentAsync(string department);
    Task<PagedResult<User>> SearchAsync(UserSearchCriteria criteria);
    Task<int> CreateAsync(User user);
    Task<int> UpdateAsync(User user);
    Task<int> DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<UserRepository> _logger;
    
    // 预编译的查询模板
    private static readonly SqlTemplate GetByIdTemplate = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
    
    private static readonly SqlTemplate GetByDepartmentTemplate = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE Department = @department AND IsActive = 1");
    
    public UserRepository(IDbConnection connection, ILogger<UserRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }
    
    public async Task<User?> GetByIdAsync(int id)
    {
        var query = GetByIdTemplate.Execute(new { id });
        return await _connection.QuerySingleOrDefaultAsync<User>(query.Render());
    }
    
    public async Task<List<User>> GetByDepartmentAsync(string department)
    {
        var query = GetByDepartmentTemplate.Execute(new { department });
        return (await _connection.QueryAsync<User>(query.Render())).ToList();
    }
    
    public async Task<PagedResult<User>> SearchAsync(UserSearchCriteria criteria)
    {
        var queryBuilder = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Where(u => u.IsActive);
        
        // 动态添加搜索条件
        if (!string.IsNullOrEmpty(criteria.Name))
            queryBuilder = queryBuilder.Where(u => u.Name.Contains(criteria.Name));
        
        if (!string.IsNullOrEmpty(criteria.Department))
            queryBuilder = queryBuilder.Where(u => u.Department == criteria.Department);
        
        if (criteria.MinAge.HasValue)
            queryBuilder = queryBuilder.Where(u => u.Age >= criteria.MinAge.Value);
        
        // 添加排序和分页
        var query = queryBuilder
            .OrderBy(u => u.Name)
            .Skip(criteria.Page * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToSql();
        
        var users = await _connection.QueryAsync<User>(query);
        var total = await GetSearchCountAsync(criteria);
        
        return new PagedResult<User>(users.ToList(), total, criteria.Page, criteria.PageSize);
    }
    
    public async Task<int> CreateAsync(User user)
    {
        var insertQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .InsertInto(u => new { u.Name, u.Email, u.Department, u.Age, u.IsActive })
            .Values(user.Name, user.Email, user.Department, user.Age, user.IsActive);
        
        return await _connection.ExecuteAsync(insertQuery.ToSql());
    }
    
    public async Task<int> UpdateAsync(User user)
    {
        var updateQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Update()
            .Set(u => u.Name, user.Name)
            .Set(u => u.Email, user.Email)
            .Set(u => u.Department, user.Department)
            .Set(u => u.Age, user.Age)
            .Set(u => u.ModifiedAt, DateTime.UtcNow)
            .Where(u => u.Id == user.Id);
        
        return await _connection.ExecuteAsync(updateQuery.ToSql());
    }
    
    public async Task<int> DeleteAsync(int id)
    {
        var deleteQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Update()  // 软删除
            .Set(u => u.IsActive, false)
            .Set(u => u.ModifiedAt, DateTime.UtcNow)
            .Where(u => u.Id == id);
        
        return await _connection.ExecuteAsync(deleteQuery.ToSql());
    }
}
```

### 2. 服务层模式

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IValidator<User> _validator;
    
    public UserService(
        IUserRepository userRepository, 
        ILogger<UserService> logger,
        IValidator<User> validator)
    {
        _userRepository = userRepository;
        _logger = logger;
        _validator = validator;
    }
    
    public async Task<Result<User>> GetUserAsync(int id)
    {
        if (id <= 0)
            return Result<User>.Error("Invalid user ID");
        
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null 
                ? Result<User>.Success(user)
                : Result<User>.NotFound($"User {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return Result<User>.Error("Failed to retrieve user");
        }
    }
    
    public async Task<Result<int>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Department = request.Department,
                Age = request.Age,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            var validationResult = await _validator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                return Result<int>.Error($"Validation failed: {string.Join(", ", validationResult.Errors)}");
            }
            
            var id = await _userRepository.CreateAsync(user);
            _logger.LogInformation("User created with ID {UserId}", id);
            
            return Result<int>.Success(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return Result<int>.Error("Failed to create user");
        }
    }
}
```

---

## 📈 监控和维护

### 1. 健康检查

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnection _connection;
    
    public DatabaseHealthCheck(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用简单查询测试数据库连接
            var testQuery = ParameterizedSql.Create("SELECT 1 as TestValue", null);
            var result = await _connection.QuerySingleAsync<int>(testQuery.Render());
            
            return result == 1 
                ? HealthCheckResult.Healthy("Database connection is healthy")
                : HealthCheckResult.Unhealthy("Database returned unexpected result");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}
```

### 2. 性能计数器

```csharp
public class QueryMetrics
{
    private readonly IMetricsCollector _metrics;
    
    public void RecordQueryExecution(string queryType, long durationMs, int resultCount)
    {
        _metrics.Histogram("sqlx_query_duration_ms")
            .WithTag("query_type", queryType)
            .Record(durationMs);
            
        _metrics.Counter("sqlx_query_executions_total")
            .WithTag("query_type", queryType)
            .Increment();
            
        _metrics.Histogram("sqlx_query_result_count")
            .WithTag("query_type", queryType)
            .Record(resultCount);
    }
    
    public void RecordQueryError(string queryType, string errorType)
    {
        _metrics.Counter("sqlx_query_errors_total")
            .WithTag("query_type", queryType)
            .WithTag("error_type", errorType)
            .Increment();
    }
}
```

---

## 🎯 总结

遵循这些最佳实践将帮助您：

1. **选择合适的模式** - 根据场景选择最适合的使用模式
2. **优化性能** - 通过模板重用和参数化查询提升性能
3. **确保安全** - 防止SQL注入和实现适当的权限控制
4. **提高可维护性** - 使用清晰的架构模式和错误处理
5. **监控生产环境** - 实现适当的日志记录和性能监控

记住，最佳实践是指导原则，需要根据具体项目需求进行调整。Sqlx 3.0的设计理念是简单而强大，正确使用这些模式将让您的数据访问层既高效又可维护。
