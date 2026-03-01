# 设计文档：Sqlx 生态系统增强

## 概述

本设计文档描述了 Sqlx 生态系统增强的技术实现，包括依赖注入集成包、文档体系和真实示例。设计遵循 Sqlx 的核心原则：高性能、零反射、AOT 友好。

## 架构

### 整体架构

```
Sqlx 生态系统
├── Sqlx (核心库)
├── Sqlx.Generator (源生成器)
├── Sqlx.DependencyInjection (新增 - DI 集成)
│   ├── ServiceCollectionExtensions
│   ├── SqlxConnectionFactory
│   └── SqlxOptions
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
│      .NET Application (任何类型)         │
│   Web API / Console / Worker / Blazor  │
├─────────────────────────────────────────┤
│  Services → Repositories                │
│         ↓           ↓                   │
│    Sqlx.DependencyInjection             │
│  ├── DI Registration                    │
│  ├── Connection Factory                 │
│  └── Configuration                      │
│         ↓           ↓                   │
│         Sqlx Core Library               │
│  ├── SqlQuery<T>                        │
│  ├── SqlTemplate                        │
│  ├── Repository Pattern                 │
│  └── Source Generator                   │
│         ↓           ↓                   │
│         Database (SQLite/PostgreSQL/etc)│
└─────────────────────────────────────────┘
```

## 组件设计

### 1. Sqlx.DependencyInjection 包

#### 1.1 ServiceCollectionExtensions

提供流畅的 API 配置 Sqlx：

```csharp
public static class SqlxServiceCollectionExtensions
{
    // 基础注册
    public static ISqlxBuilder AddSqlx(
        this IServiceCollection services,
        Action<SqlxOptions> configure)
    {
        var options = new SqlxOptions();
        configure(options);
        
        // 验证配置
        if (string.IsNullOrEmpty(options.ConnectionString))
            throw new ArgumentException("ConnectionString is required");
        
        services.AddSingleton(options);
        services.AddSingleton<ISqlxConnectionFactory, SqlxConnectionFactory>();
        
        return new SqlxBuilder(services, options);
    }
    
    // 从 IConfiguration 注册
    public static ISqlxBuilder AddSqlx(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Sqlx")
    {
        var options = configuration.GetSection(sectionName).Get<SqlxOptions>()
            ?? throw new ArgumentException($"Configuration section '{sectionName}' not found");
        
        services.AddSingleton(options);
        services.AddSingleton<ISqlxConnectionFactory, SqlxConnectionFactory>();
        
        return new SqlxBuilder(services, options);
    }
    
    // 自动注册仓储
    public static ISqlxBuilder AddSqlxRepositories(
        this ISqlxBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }
        
        foreach (var assembly in assemblies)
        {
            // 查找所有标记了 [RepositoryFor] 的类
            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetCustomAttribute<RepositoryForAttribute>() != null);
            
            foreach (var repoType in repositoryTypes)
            {
                var attr = repoType.GetCustomAttribute<RepositoryForAttribute>();
                var interfaceType = attr.InterfaceType;
                
                // 注册仓储
                builder.Services.Add(new ServiceDescriptor(
                    interfaceType,
                    sp =>
                    {
                        var factory = sp.GetRequiredService<ISqlxConnectionFactory>();
                        var connection = factory.CreateConnection();
                        return Activator.CreateInstance(repoType, connection);
                    },
                    lifetime));
            }
        }
        
        return builder;
    }
    
    // 注册命名连接
    public static ISqlxBuilder AddNamedConnection(
        this ISqlxBuilder builder,
        string name,
        string connectionString,
        SqlDefineTypes dialect)
    {
        builder.Services.AddKeyedSingleton<ISqlxConnectionFactory>(
            name,
            (sp, key) => new SqlxConnectionFactory(connectionString, dialect));
        
        return builder;
    }
}

// Builder 接口
public interface ISqlxBuilder
{
    IServiceCollection Services { get; }
    SqlxOptions Options { get; }
}

internal class SqlxBuilder : ISqlxBuilder
{
    public SqlxBuilder(IServiceCollection services, SqlxOptions options)
    {
        Services = services;
        Options = options;
    }
    
    public IServiceCollection Services { get; }
    public SqlxOptions Options { get; }
}
```

#### 1.2 SqlxOptions

配置选项类：

```csharp
public class SqlxOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public SqlDefineTypes Dialect { get; set; } = SqlDefineTypes.SQLite;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableConnectionPooling { get; set; } = true;
    public int MinPoolSize { get; set; } = 0;
    public int MaxPoolSize { get; set; } = 100;
    
    // 验证配置
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("ConnectionString cannot be empty");
        
        if (CommandTimeout < 0)
            throw new InvalidOperationException("CommandTimeout must be non-negative");
        
        if (MinPoolSize < 0 || MaxPoolSize < MinPoolSize)
            throw new InvalidOperationException("Invalid pool size configuration");
    }
}
```

#### 1.3 SqlxConnectionFactory

连接工厂实现：

```csharp
public interface ISqlxConnectionFactory
{
    DbConnection CreateConnection();
    SqlDefineTypes Dialect { get; }
}

public class SqlxConnectionFactory : ISqlxConnectionFactory
{
    private readonly string _connectionString;
    private readonly SqlDefineTypes _dialect;
    
    public SqlxConnectionFactory(string connectionString, SqlDefineTypes dialect)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _dialect = dialect;
    }
    
    public SqlDefineTypes Dialect => _dialect;
    
    public DbConnection CreateConnection()
    {
        DbConnection connection = _dialect switch
        {
            SqlDefineTypes.SQLite => new SqliteConnection(_connectionString),
            SqlDefineTypes.PostgreSQL => new NpgsqlConnection(_connectionString),
            SqlDefineTypes.MySQL => new MySqlConnection(_connectionString),
            SqlDefineTypes.SqlServer => new SqlConnection(_connectionString),
            SqlDefineTypes.Oracle => new OracleConnection(_connectionString),
            SqlDefineTypes.DB2 => throw new NotSupportedException("DB2 connection creation not implemented"),
            _ => throw new NotSupportedException($"Dialect {_dialect} is not supported")
        };
        
        return connection;
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
dotnet add package Sqlx.DependencyInjection

// 2. 定义实体
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// 3. 定义仓储
public interface IUserRepository : ICrudRepository<User, long> { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 配置 DI（在 Program.cs 或 Startup.cs）
builder.Services.AddSqlx(options =>
{
    options.ConnectionString = "Data Source=app.db";
    options.Dialect = SqlDefineTypes.SQLite;
})
.AddSqlxRepositories(); // 自动注册当前程序集的所有仓储

// 5. 使用（通过构造函数注入）
public class UserService
{
    private readonly IUserRepository _userRepository;
    
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }
}
```

#### 2.2 性能调优文档 (docs/performance-tuning.md)

**章节**:
1. ResultReader 优化
2. 查询设计最佳实践
3. 连接池配置
4. 批量操作优化
5. 内存分配优化
6. 数据库特定优化
7. 性能监控

**关键内容**:
- 使用 SqlQuery<T> 的泛型缓存
- 避免 N+1 查询
- 使用批量操作而非循环
- 合理使用事务
- 连接池大小配置
- 异步操作最佳实践

#### 2.3 故障排除文档 (docs/troubleshooting.md)

**章节**:
1. 常见错误消息和解决方案
2. 源生成器调试技巧
3. 连接故障排除
4. 性能故障排除
5. AOT 编译问题
6. FAQ
7. 社区支持

### 3. 真实世界示例 (samples/RealWorldExample)

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

// 配置 Sqlx DI
builder.Services.AddSqlx(builder.Configuration, "Sqlx") // 从 appsettings.json 读取配置
    .AddSqlxRepositories(); // 自动注册所有仓储

// 或者手动配置
builder.Services.AddSqlx(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("Default");
    options.Dialect = SqlDefineTypes.SQLite;
    options.CommandTimeout = 30;
})
.AddSqlxRepositories(ServiceLifetime.Scoped); // 指定生命周期

// 添加控制器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

**appsettings.json**:
```json
{
  "Sqlx": {
    "ConnectionString": "Data Source=app.db",
    "Dialect": "SQLite",
    "CommandTimeout": 30,
    "EnableConnectionPooling": true,
    "MaxPoolSize": 100
  },
  "ConnectionStrings": {
    "Default": "Data Source=app.db"
  }
}
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

### SqlxOptions

```csharp
public class SqlxOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public SqlDefineTypes Dialect { get; set; } = SqlDefineTypes.SQLite;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableConnectionPooling { get; set; } = true;
    public int MinPoolSize { get; set; } = 0;
    public int MaxPoolSize { get; set; } = 100;
}
```

### ISqlxConnectionFactory

```csharp
public interface ISqlxConnectionFactory
{
    DbConnection CreateConnection();
    SqlDefineTypes Dialect { get; }
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

### 配置验证

```csharp
public class SqlxOptions
{
    // ... properties ...
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("ConnectionString cannot be empty");
        
        if (CommandTimeout < 0)
            throw new InvalidOperationException("CommandTimeout must be non-negative");
        
        if (MinPoolSize < 0 || MaxPoolSize < MinPoolSize)
            throw new InvalidOperationException("Invalid pool size configuration");
    }
}
```

### 统一异常处理（Web API 示例）

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

### DI 注册性能

- 仓储注册使用反射，但只在启动时执行一次
- 使用 `RepositoryForAttribute` 标记减少扫描范围
- 支持指定程序集避免全局扫描
- 目标注册时间 < 100ms

### 连接工厂性能

- 连接创建使用简单的 switch 表达式
- 不使用反射创建连接对象
- 支持连接池配置
- 目标连接创建开销 < 1ms

### 文档加载性能

- 使用静态 Markdown 文件
- 支持浏览器缓存
- 目标加载时间 < 2s

## 安全考虑

### 连接字符串安全

- 支持从环境变量读取连接字符串
- 支持从 User Secrets 读取（开发环境）
- 支持从 Azure Key Vault 等密钥管理服务读取
- 不在日志中输出连接字符串

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
