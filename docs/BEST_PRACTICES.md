# Sqlx 最佳实践指南

> 🎯 构建高质量、可维护的数据库访问层

## 项目结构最佳实践

### 1. 推荐的项目组织结构

```
YourProject/
├── src/
│   ├── YourProject.Core/
│   │   ├── Entities/           # 实体类
│   │   │   ├── User.cs
│   │   │   └── Product.cs
│   │   ├── Interfaces/         # 服务接口
│   │   │   ├── IUserService.cs
│   │   │   └── IProductService.cs
│   │   └── Models/             # DTO 和 ViewModels
│   │       ├── UserDto.cs
│   │       └── CreateUserRequest.cs
│   ├── YourProject.Data/
│   │   ├── Repositories/       # Repository 实现
│   │   │   ├── UserRepository.cs
│   │   │   └── ProductRepository.cs
│   │   ├── Configurations/     # 数据库配置
│   │   │   └── DatabaseConfig.cs
│   │   └── Migrations/         # 数据库迁移
│   └── YourProject.Api/
│       ├── Controllers/
│       └── Program.cs
└── tests/
    ├── YourProject.Tests/
    └── YourProject.IntegrationTests/
```

### 2. 实体类设计最佳实践

```csharp
[TableName("users")]
public class User
{
    /// <summary>
    /// 主键 - 使用明确的数据类型
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 业务字段 - 使用非空注解
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 自定义列映射
    /// </summary>
    [DbColumn("email_address")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 审计字段 - 标准化命名
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 导航属性 - 使用懒加载
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

## Repository 模式最佳实践

### 1. 接口设计原则

```csharp
/// <summary>
/// 用户服务接口 - 遵循单一职责原则
/// </summary>
public interface IUserService
{
    // 基础 CRUD 操作
    [SqlExecuteType(SqlExecuteTypes.Select, "users")]
    Task<IList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    
    [SqlExecuteType(SqlExecuteTypes.Select, "users")]
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "users")]
    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    // 业务特定查询
    [Sqlx("SELECT * FROM users WHERE IsActive = 1")]
    Task<IList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    
    [Sqlx("SELECT * FROM users WHERE Email = @email")]
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    // 复杂查询使用 ExpressionToSql
    [Sqlx]
    Task<IList<User>> SearchAsync([ExpressionToSql] ExpressionToSql<User> filter, CancellationToken cancellationToken = default);
    
    // 聚合查询
    [Sqlx("SELECT COUNT(*) FROM users WHERE IsActive = 1")]
    Task<int> GetActiveUserCountAsync(CancellationToken cancellationToken = default);
}
```

### 2. Repository 实现最佳实践

```csharp
/// <summary>
/// 用户 Repository - 遵循依赖注入原则
/// </summary>
[RepositoryFor(typeof(IUserService))]
[SqlDefine(1)] // SQL Server 方言
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    private readonly ILogger<UserRepository> logger;
    
    public UserRepository(DbConnection connection, ILogger<UserRepository> logger)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // 所有方法实现由 Sqlx 自动生成
    // 支持：
    // - 异步操作
    // - 参数化查询
    // - 异常处理
    // - 性能监控
    // - 取消令牌支持
}
```

### 3. 多数据库支持策略

```csharp
// 定义通用接口
public interface IUserService
{
    Task<IList<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
}

// SQL Server 实现
[RepositoryFor(typeof(IUserService))]
[SqlDefine(1)] // SQL Server 方言
public partial class SqlServerUserRepository : IUserService
{
    private readonly SqlConnection connection;
    public SqlServerUserRepository(SqlConnection connection) => this.connection = connection;
}

// MySQL 实现
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)] // MySQL 方言
public partial class MySqlUserRepository : IUserService
{
    private readonly MySqlConnection connection;
    public MySqlUserRepository(MySqlConnection connection) => this.connection = connection;
}

// PostgreSQL 实现
[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)] // PostgreSQL 方言
public partial class PostgreSqlUserRepository : IUserService
{
    private readonly NpgsqlConnection connection;
    public PostgreSqlUserRepository(NpgsqlConnection connection) => this.connection = connection;
}
```

## 查询优化最佳实践

### 1. ExpressionToSql 使用模式

```csharp
public class UserQueryService
{
    private readonly IUserService userService;
    
    public UserQueryService(IUserService userService)
    {
        this.userService = userService;
    }
    
    /// <summary>
    /// 动态查询示例 - 构建可重用的查询表达式
    /// </summary>
    public async Task<IList<User>> SearchUsersAsync(UserSearchCriteria criteria)
    {
        var query = ExpressionToSql<User>.ForSqlServer();
        
        // 条件构建
        if (!string.IsNullOrEmpty(criteria.Name))
        {
            query = query.Where(u => u.Name.Contains(criteria.Name));
        }
        
        if (!string.IsNullOrEmpty(criteria.Email))
        {
            query = query.Where(u => u.Email == criteria.Email);
        }
        
        if (criteria.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == criteria.IsActive.Value);
        }
        
        if (criteria.CreatedAfter.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= criteria.CreatedAfter.Value);
        }
        
        // 排序和分页
        query = query.OrderBy(u => u.Name);
        
        if (criteria.PageSize > 0)
        {
            query = query.Skip(criteria.PageIndex * criteria.PageSize)
                         .Take(criteria.PageSize);
        }
        
        return await userService.SearchAsync(query);
    }
}

public class UserSearchCriteria
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}
```

### 2. 批量操作模式

```csharp
/// <summary>
/// 批量操作服务 - 优化大数据量处理
/// </summary>
public class BulkUserService
{
    private readonly IUserService userService;
    private readonly DbConnection connection;
    
    public BulkUserService(IUserService userService, DbConnection connection)
    {
        this.userService = userService;
        this.connection = connection;
    }
    
    /// <summary>
    /// 批量创建用户 - 使用事务保证一致性
    /// </summary>
    public async Task<int> BulkCreateUsersAsync(IList<User> users, CancellationToken cancellationToken = default)
    {
        if (!users.Any()) return 0;
        
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            int totalCreated = 0;
            
            // 分批处理，避免过大的事务
            foreach (var batch in users.Chunk(1000))
            {
                foreach (var user in batch)
                {
                    totalCreated += await userService.CreateAsync(user, cancellationToken);
                }
            }
            
            await transaction.CommitAsync(cancellationToken);
            return totalCreated;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    /// <summary>
    /// 批量更新用户状态 - 使用原生 SQL 提高性能
    /// </summary>
    [Sqlx("UPDATE users SET IsActive = @isActive WHERE Id IN @userIds")]
    public partial Task<int> BulkUpdateUserStatusAsync(bool isActive, IList<int> userIds, CancellationToken cancellationToken = default);
}
```

## 错误处理最佳实践

### 1. 异常处理策略

```csharp
/// <summary>
/// 带错误处理的用户服务包装器
/// </summary>
public class SafeUserService
{
    private readonly IUserService userService;
    private readonly ILogger<SafeUserService> logger;
    
    public SafeUserService(IUserService userService, ILogger<SafeUserService> logger)
    {
        this.userService = userService;
        this.logger = logger;
    }
    
    public async Task<Result<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id <= 0)
            {
                return Result<User>.Failure("Invalid user ID");
            }
            
            var user = await userService.GetByIdAsync(id, cancellationToken);
            
            if (user == null)
            {
                return Result<User>.Failure($"User with ID {id} not found");
            }
            
            return Result<User>.Success(user);
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Database error occurred while getting user {UserId}", id);
            return Result<User>.Failure("Database error occurred");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation was cancelled while getting user {UserId}", id);
            return Result<User>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while getting user {UserId}", id);
            return Result<User>.Failure("An unexpected error occurred");
        }
    }
}

/// <summary>
/// 通用结果类型 - 函数式错误处理
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

### 2. 重试机制

```csharp
/// <summary>
/// 带重试机制的数据库操作
/// </summary>
public class ResilientUserService
{
    private readonly IUserService userService;
    private readonly ILogger<ResilientUserService> logger;
    
    public ResilientUserService(IUserService userService, ILogger<ResilientUserService> logger)
    {
        this.userService = userService;
        this.logger = logger;
    }
    
    public async Task<User?> GetUserByIdWithRetryAsync(int id, CancellationToken cancellationToken = default)
    {
        var retryPolicy = Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning("Retry {RetryCount} for GetUserById({UserId}) after {Delay}ms", 
                        retryCount, id, timespan.TotalMilliseconds);
                });
        
        return await retryPolicy.ExecuteAsync(async () => 
        {
            return await userService.GetByIdAsync(id, cancellationToken);
        });
    }
}
```

## 依赖注入配置

### 1. .NET Core 依赖注入设置

```csharp
// Program.cs
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // 数据库连接配置
        builder.Services.AddScoped<DbConnection>(provider =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        });
        
        // Repository 注册
        builder.Services.AddScoped<IUserService, UserRepository>();
        builder.Services.AddScoped<IProductService, ProductRepository>();
        
        // 服务层注册
        builder.Services.AddScoped<UserQueryService>();
        builder.Services.AddScoped<BulkUserService>();
        builder.Services.AddScoped<SafeUserService>();
        
        // 日志配置
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });
        
        var app = builder.Build();
        
        // 中间件配置
        app.UseExceptionHandler("/error");
        app.UseHttpsRedirection();
        app.MapControllers();
        
        app.Run();
    }
}
```

### 2. 多数据库配置

```csharp
// 数据库配置
public class DatabaseConfiguration
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 主数据库 (SQL Server)
        services.AddScoped<SqlConnection>(provider =>
        {
            var connectionString = configuration.GetConnectionString("SqlServer");
            return new SqlConnection(connectionString);
        });
        
        // 只读数据库 (MySQL)
        services.AddScoped<MySqlConnection>(provider =>
        {
            var connectionString = configuration.GetConnectionString("MySqlReadOnly");
            return new MySqlConnection(connectionString);
        });
        
        // 缓存数据库 (Redis)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        
        // Repository 策略模式
        services.AddScoped<IUserService>(provider =>
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            var sqlConnection = provider.GetRequiredService<SqlConnection>();
            var mysqlConnection = provider.GetRequiredService<MySqlConnection>();
            
            return environment.IsProduction() 
                ? new SqlServerUserRepository(sqlConnection)
                : new MySqlUserRepository(mysqlConnection);
        });
    }
}
```

## 测试最佳实践

### 1. 单元测试模式

```csharp
/// <summary>
/// Repository 单元测试
/// </summary>
[TestClass]
public class UserRepositoryTests
{
    private UserRepository repository;
    private SqlConnection connection;
    
    [TestInitialize]
    public async Task SetupAsync()
    {
        // 使用内存数据库进行测试
        connection = new SqlConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        // 创建测试表
        await CreateTestTableAsync();
        
        repository = new UserRepository(connection);
    }
    
    [TestCleanup]
    public async Task CleanupAsync()
    {
        await connection.DisposeAsync();
    }
    
    [TestMethod]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var testUser = await CreateTestUserAsync();
        
        // Act
        var result = await repository.GetByIdAsync(testUser.Id);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testUser.Name, result.Name);
        Assert.AreEqual(testUser.Email, result.Email);
    }
    
    [TestMethod]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await repository.GetByIdAsync(999);
        
        // Assert
        Assert.IsNull(result);
    }
    
    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        var id = await repository.CreateAsync(user);
        user.Id = id;
        return user;
    }
}
```

### 2. 集成测试模式

```csharp
/// <summary>
/// 端到端集成测试
/// </summary>
[TestClass]
public class UserServiceIntegrationTests
{
    private WebApplicationFactory<Program> factory;
    private HttpClient client;
    
    [TestInitialize]
    public void Setup()
    {
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 替换为测试数据库
                    services.RemoveAll<DbConnection>();
                    services.AddSingleton<DbConnection>(provider =>
                    {
                        var connection = new SqliteConnection("Data Source=:memory:");
                        connection.Open();
                        return connection;
                    });
                });
            });
        
        client = factory.CreateClient();
    }
    
    [TestMethod]
    public async Task GetUser_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = await CreateTestUserAsync();
        
        // Act
        var response = await client.GetAsync($"/api/users/{userId}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(content);
        
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }
}
```

## 安全最佳实践

### 1. SQL 注入防护

```csharp
// ✅ 正确：使用参数化查询（Sqlx 自动处理）
[Sqlx("SELECT * FROM users WHERE Email = @email")]
Task<User?> GetByEmailAsync(string email);

// ✅ 正确：ExpressionToSql 自动参数化
[Sqlx]
Task<IList<User>> SearchAsync([ExpressionToSql] ExpressionToSql<User> filter);

// ❌ 错误：字符串拼接（永远不要这样做）
// [Sqlx($"SELECT * FROM users WHERE Email = '{email}'")]
```

### 2. 数据验证

```csharp
/// <summary>
/// 带验证的用户服务
/// </summary>
public class ValidatedUserService
{
    private readonly IUserService userService;
    private readonly IValidator<User> validator;
    
    public ValidatedUserService(IUserService userService, IValidator<User> validator)
    {
        this.userService = userService;
        this.validator = validator;
    }
    
    public async Task<Result<int>> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        // 输入验证
        var validationResult = await validator.ValidateAsync(user, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<int>.Failure($"Validation failed: {validationResult.Errors}");
        }
        
        // 业务规则验证
        var existingUser = await userService.GetByEmailAsync(user.Email, cancellationToken);
        if (existingUser != null)
        {
            return Result<int>.Failure("User with this email already exists");
        }
        
        // 创建用户
        var userId = await userService.CreateAsync(user, cancellationToken);
        return Result<int>.Success(userId);
    }
}

/// <summary>
/// 用户验证规则
/// </summary>
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
            
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters");
    }
}
```

## 总结

遵循这些最佳实践将帮助您构建：

1. **高性能** - 利用 Sqlx 的零反射和缓存机制
2. **可维护** - 清晰的代码结构和错误处理
3. **可测试** - 依赖注入和模块化设计
4. **安全** - 参数化查询和输入验证
5. **可扩展** - 多数据库支持和灵活配置

🎯 **核心原则：** 
- 保持简单，优先使用 Sqlx 的自动生成功能
- 遵循 SOLID 原则进行设计
- 使用异步编程提高性能
- 始终考虑错误处理和边界情况
- 编写测试确保代码质量
