# 设计文档：Sqlx 生态系统增强

## 概述

本设计文档描述了 Sqlx 生态系统增强的技术实现，包括 ASP.NET Core 集成包、文档体系、真实示例、性能调优和日志功能。设计遵循 Sqlx 的核心原则：高性能、零反射、AOT 友好。

## 架构

### 整体架构

```
Sqlx 生态系统
├── Sqlx (核心库)
├── Sqlx.Generator (源生成器)
├── Sqlx.AspNetCore (新增 - ASP.NET Core 集成)
│   ├── ServiceCollectionExtensions
│   └── SqlLoggingMiddleware
├── docs/ (增强文档)
│   ├── getting-started.md
│   ├── performance-tuning.md
│   └── troubleshooting.md
└── samples/RealWorldExample/ (新增 - 完整示例)
    ├── API Project
    ├── Tests
    └── Docker Support
```

### 组件关系

```
┌─────────────────────────────────────────┐
│         ASP.NET Core Application        │
├─────────────────────────────────────────┤
│  Controllers → Services → Repositories  │
│         ↓           ↓           ↓       │
│    Sqlx.AspNetCore Extensions           │
│  ├── DI Registration                    │
│  └── SQL Logging Middleware             │
│         ↓           ↓           ↓       │
│         Sqlx Core Library               │
│  ├── SqlQuery<T>                        │
│  ├── SqlTemplate                        │
│  ├── Repository Pattern                 │
│  └── Source Generator                   │
│         ↓           ↓           ↓       │
│         Database (SQLite/PostgreSQL/etc)│
└─────────────────────────────────────────┘
```

## 组件设计

### 1. Sqlx.AspNetCore 包

#### 1.1 ServiceCollectionExtensions

提供流畅的 API 配置 Sqlx：

```csharp
public static class SqlxServiceCollectionExtensions
{
    public static ISqlxBuilder AddSqlx(
        this IServiceCollection services,
        Action<SqlxOptions> configure)
    {
        var options = new SqlxOptions();
        configure(options);
        
        services.AddSingleton(options);
        services.AddSingleton<ISqlxConnectionFactory, SqlxConnectionFactory>();
        
        return new SqlxBuilder(services, options);
    }
    
    public static ISqlxBuilder AddSqlxRepositories(
        this ISqlxBuilder builder,
        params Assembly[] assemblies)
    {
        // 自动扫描并注册所有仓储
        foreach (var assembly in assemblies)
        {
            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(ICrudRepository<,>)));
            
            foreach (var repoType in repositoryTypes)
            {
                var interfaces = repoType.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    builder.Services.AddScoped(iface, repoType);
                }
            }
        }
        
        return builder;
    }
}

public class SqlxOptions
{
    public string ConnectionString { get; set; }
    public SqlDefineTypes Dialect { get; set; }
    public bool EnableLogging { get; set; } = true;
    public int SlowQueryThresholdMs { get; set; } = 1000;
}
```

#### 1.2 SqlLoggingMiddleware

记录所有 SQL 执行和慢查询检测：

```csharp
public class SqlLoggingMiddleware
{
    private readonly ILogger<SqlLoggingMiddleware> _logger;
    private readonly SqlxOptions _options;
    
    public SqlLoggingMiddleware(
        ILogger<SqlLoggingMiddleware> logger,
        SqlxOptions options)
    {
        _logger = logger;
        _options = options;
    }
    
    public async Task InvokeAsync(
        HttpContext context,
        ISqlExecutionInterceptor interceptor)
    {
        interceptor.OnBeforeExecute += LogSqlExecution;
        interceptor.OnAfterExecute += LogSqlCompletion;
        interceptor.OnError += LogSqlError;
        
        await _next(context);
    }
    
    private void LogSqlExecution(SqlExecutionContext ctx)
    {
        if (_options.EnableLogging)
        {
            _logger.LogDebug(
                "Executing SQL: {Sql} | Parameters: {Parameters} | Repository: {Repository}",
                ctx.Sql,
                MaskSensitiveData(ctx.Parameters),
                ctx.RepositoryName);
        }
    }
    
    private void LogSqlCompletion(SqlExecutionContext ctx)
    {
        var duration = ctx.Duration.TotalMilliseconds;
        
        if (duration > _options.SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "Slow query detected ({Duration}ms): {Sql} | Repository: {Repository}.{Method}",
                duration,
                ctx.Sql,
                ctx.RepositoryName,
                ctx.MethodName);
        }
        else if (_options.EnableLogging)
        {
            _logger.LogInformation(
                "SQL completed ({Duration}ms): {Repository}.{Method}",
                duration,
                ctx.RepositoryName,
                ctx.MethodName);
        }
    }
    
    private Dictionary<string, object> MaskSensitiveData(
        Dictionary<string, object> parameters)
    {
        var masked = new Dictionary<string, object>();
        var sensitiveKeys = new[] { "password", "token", "secret", "key" };
        
        foreach (var (key, value) in parameters)
        {
            if (sensitiveKeys.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                masked[key] = "***MASKED***";
            }
            else
            {
                masked[key] = value;
            }
        }
        
        return masked;
    }
}
```

### 2. 文档结构

#### 2.1 快速开始文档 (docs/getting-started.md)

**结构**:
1. 安装 (< 1 分钟)
2. 第一个示例 (< 3 分钟)
3. 核心概念 (< 1 分钟)
4. 下一步

**示例代码**:
```csharp
// 1. 安装
dotnet add package Sqlx

// 2. 定义实体
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
}

// 3. 定义仓储
public interface IUserRepository : ICrudRepository<User, long> { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 使用
var conn = new SqliteConnection("Data Source=:memory:");
var repo = new UserRepository(conn);
var users = await repo.GetAllAsync();
```

#### 2.2 性能调优文档 (docs/performance-tuning.md)

#### 3.1 项目结构

```
RealWorldExample/
├── src/
│   ├── RealWorldExample.Api/          # Web API 项目
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Program.cs
│   ├── RealWorldExample.Core/         # 核心业务逻辑
│   │   ├── Entities/
│   │   ├── Repositories/
│   │   └── Services/
│   └── RealWorldExample.Infrastructure/ # 基础设施
│       ├── Data/
│       └── Repositories/
├── tests/
│   ├── RealWorldExample.Tests/        # 单元测试
│   └── RealWorldExample.IntegrationTests/ # 集成测试
├── docker-compose.yml
├── Dockerfile
└── README.md
```

#### 3.2 核心功能

**实现的 API**:
- `GET /api/users` - 获取用户列表（分页、过滤、排序）
- `GET /api/users/{id}` - 获取单个用户
- `POST /api/users` - 创建用户
- `PUT /api/users/{id}` - 更新用户
- `DELETE /api/users/{id}` - 删除用户
- `GET /api/users/{id}/orders` - 获取用户订单
- `POST /api/orders` - 创建订单（事务示例）

**技术栈**:
- ASP.NET Core 10.0
- Sqlx + Sqlx.AspNetCore
- SQLite (开发) / PostgreSQL (生产)
- Swagger/OpenAPI
- xUnit + FluentAssertions
- Docker + Docker Compose

#### 3.3 关键代码示例

**Program.cs**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// 配置 Sqlx
builder.Services.AddSqlx(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default");
    options.Dialect = SqlDefineTypes.SQLite;
    options.EnableLogging = true;
    options.SlowQueryThresholdMs = 500;
})
.AddSqlxRepositories(typeof(Program).Assembly);

var app = builder.Build();

// 使用 SQL 日志中间件
app.UseSqlLogging();

app.Run();
```

**UserController.cs**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(
        IUserRepository userRepository,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<User>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = SqlQuery<User>.ForSqlite();
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Name.Contains(search));
        }
        
        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(_userRepository.Connection);
        
        var total = await query.CountAsync(_userRepository.Connection);
        
        return Ok(new PagedResult<User>
        {
            Items = users,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }
    
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email
        };
        
        var id = await _userRepository.InsertAndGetIdAsync(user);
        user.Id = id;
        
        return CreatedAtAction(nameof(GetUser), new { id }, user);
    }
}
```

## 数据模型

### SqlExecutionContext

```csharp
public class SqlExecutionContext
{
    public string Sql { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public string RepositoryName { get; set; }
    public string MethodName { get; set; }
    public TimeSpan Duration { get; set; }
    public Exception? Error { get; set; }
    public string CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### PagedResult<T>

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

## 错误处理

### 统一异常处理

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");
        
        var problemDetails = exception switch
        {
            SqlxException sqlEx => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Database Error",
                Detail = sqlEx.Message,
                Extensions =
                {
                    ["sql"] = sqlEx.Sql,
                    ["repository"] = sqlEx.RepositoryType,
                    ["method"] = sqlEx.MethodName
                }
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = exception.Message
            }
        };
        
        context.Response.StatusCode = problemDetails.Status.Value;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        return true;
    }
}
```

## 测试策略

### 单元测试

- 测试仓储方法的正确性
- 测试业务逻辑
- 使用内存数据库（SQLite :memory:）

### 集成测试

- 测试完整的 API 端点
- 测试事务行为
- 使用 TestContainers 运行真实数据库

### 性能测试

- 使用 BenchmarkDotNet
- 测试查询性能
- 测试批量操作性能

## 部署

### Docker 支持

**Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RealWorldExample.Api/RealWorldExample.Api.csproj", "RealWorldExample.Api/"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RealWorldExample.Api.dll"]
```

**docker-compose.yml**:
```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__Default=Host=db;Database=realworld;Username=postgres;Password=postgres
    depends_on:
      - db
  
  db:
    image: postgres:16
    environment:
      - POSTGRES_DB=realworld
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## 性能考虑

### SQL 日志中间件性能

- 使用异步日志避免阻塞
- 参数掩码使用缓存的正则表达式
- 慢查询统计使用高效的数据结构
- 目标开销 < 1ms per query

### 文档加载性能

- 使用静态 Markdown 文件
- 支持浏览器缓存
- 目标加载时间 < 2s

## 安全考虑

### 敏感数据掩码

- 自动掩码密码、令牌、密钥等参数
- 可配置的敏感字段列表
- 日志中不包含完整的 SQL 参数值

### SQL 注入防护

- 所有示例使用参数化查询
- 文档强调参数化的重要性
- 提供 SQL 注入防护最佳实践

## 可维护性

### 代码组织

- 遵循 Sqlx 编码规范
- 使用 StyleCop 和 EditorConfig
- 完整的 XML 文档注释

### 文档维护

- Markdown 格式易于维护
- 版本控制跟踪变更
- 定期审查和更新

### 测试覆盖

- 目标 80%+ 代码覆盖率
- 关键路径 100% 覆盖
- 集成测试覆盖主要场景
