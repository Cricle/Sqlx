# 设计文档：Sqlx 生态系统增强

## 概述

本设计文档描述了 Sqlx 生态系统增强的技术实现，包括通过源生成器实现的 DI 集成、文档体系和真实示例。设计遵循 Sqlx 的核心原则：高性能、零反射、AOT 友好。

## 核心设计理念

1. **零反射**: 所有 DI 注册代码通过源生成器在编译时生成
2. **AOT 友好**: 生成的代码完全兼容 Native AOT
3. **无额外依赖**: DI 扩展直接集成在 Sqlx 核心库中
4. **编译时安全**: 源生成器在编译时验证配置

## 架构

### 整体架构

```
Sqlx 生态系统
├── Sqlx (核心库 + DI 扩展)
│   ├── Core (现有功能)
│   └── DependencyInjection (新增)
│       ├── Attributes (标记特性)
│       └── Extensions (手写扩展方法)
├── Sqlx.Generator (源生成器 - 增强)
│   ├── Repository Generator (现有)
│   └── DI Registration Generator (新增)
├── docs/ (增强文档)
│   ├── getting-started.md
│   ├── dependency-injection.md
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
│    Generated DI Registration Code       │
│  (源生成器生成，零反射，AOT 友好)        │
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

### 1. DI 特性标记（Sqlx 核心库）

#### 1.1 SqlxContextAttribute

标记 SqlxContext 类，源生成器将为其生成 DI 注册代码：

```csharp
namespace Sqlx;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SqlxContextAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}

public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}
```

#### 1.2 用户代码示例

```csharp
// 用户定义 SqlxContext
[SqlxContext(Lifetime = ServiceLifetime.Scoped)]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class AppDbContext : SqlxContext
{
    public AppDbContext(string connectionString) : base(connectionString)
    {
    }
    
    // 仓储属性
    public IUserRepository Users => GetRepository<IUserRepository>();
    public IOrderRepository Orders => GetRepository<IOrderRepository>();
}

// 仓储定义
public interface IUserRepository : ICrudRepository<User, long> { }

[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

### 2. 源生成器增强（Sqlx.Generator）

#### 2.1 DI 注册代码生成器

源生成器扫描标记了 `[SqlxContext]` 的类，自动生成扩展方法：

**生成的代码示例**:

```csharp
// 文件: AppDbContext.g.cs
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace Sqlx.Generated;

public static class AppDbContextServiceCollectionExtensions
{
    public static IServiceCollection AddAppDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        // 注册 DbConnection 工厂
        services.AddScoped<DbConnection>(sp =>
        {
            var connection = new SqliteConnection(connectionString);
            return connection;
        });
        
        // 注册 Context
        services.AddScoped<AppDbContext>(sp =>
        {
            var connection = sp.GetRequiredService<DbConnection>();
            return new AppDbContext(connection.ConnectionString);
        });
        
        // 注册所有仓储
        services.AddScoped<IUserRepository>(sp =>
        {
            var connection = sp.GetRequiredService<DbConnection>();
            return new UserRepository(connection);
        });
        
        services.AddScoped<IOrderRepository>(sp =>
        {
            var connection = sp.GetRequiredService<DbConnection>();
            return new OrderRepository(connection);
        });
        
        return services;
    }
    
    // 重载：从 IConfiguration 读取
    public static IServiceCollection AddAppDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringKey = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionStringKey)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringKey}' not found");
        
        return services.AddAppDbContext(connectionString);
    }
}
```

#### 2.2 生成器实现逻辑

```csharp
[Generator]
public class DependencyInjectionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 查找所有标记了 [SqlxContext] 的类
        var contextClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => GetContextClass(ctx))
            .Where(c => c is not null);
        
        // 2. 为每个 Context 生成 DI 扩展方法
        context.RegisterSourceOutput(contextClasses, (spc, contextClass) =>
        {
            var source = GenerateDIExtensions(contextClass);
            spc.AddSource($"{contextClass.Name}.g.cs", source);
        });
    }
    
    private string GenerateDIExtensions(ContextClassInfo contextClass)
    {
        var sb = new StringBuilder();
        
        // 生成命名空间和类
        sb.AppendLine($"namespace Sqlx.Generated;");
        sb.AppendLine();
        sb.AppendLine($"public static class {contextClass.Name}ServiceCollectionExtensions");
        sb.AppendLine("{");
        
        // 生成 Add{ContextName} 方法
        sb.AppendLine($"    public static IServiceCollection Add{contextClass.Name}(");
        sb.AppendLine($"        this IServiceCollection services,");
        sb.AppendLine($"        string connectionString)");
        sb.AppendLine("    {");
        
        // 注册 DbConnection
        sb.AppendLine($"        services.Add{GetLifetimeMethod(contextClass.Lifetime)}<DbConnection>(sp =>");
        sb.AppendLine("        {");
        sb.AppendLine($"            return new {GetConnectionType(contextClass.Dialect)}(connectionString);");
        sb.AppendLine("        });");
        sb.AppendLine();
        
        // 注册 Context
        sb.AppendLine($"        services.Add{GetLifetimeMethod(contextClass.Lifetime)}<{contextClass.Name}>(sp =>");
        sb.AppendLine("        {");
        sb.AppendLine("            var connection = sp.GetRequiredService<DbConnection>();");
        sb.AppendLine($"            return new {contextClass.Name}(connection.ConnectionString);");
        sb.AppendLine("        });");
        sb.AppendLine();
        
        // 注册所有仓储
        foreach (var repo in contextClass.Repositories)
        {
            sb.AppendLine($"        services.Add{GetLifetimeMethod(contextClass.Lifetime)}<{repo.InterfaceType}>(sp =>");
            sb.AppendLine("        {");
            sb.AppendLine("            var connection = sp.GetRequiredService<DbConnection>();");
            sb.AppendLine($"            return new {repo.ImplementationType}(connection);");
            sb.AppendLine("        });");
            sb.AppendLine();
        }
        
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
}
```

### 3. 手写扩展方法（Sqlx 核心库）

为了支持更灵活的场景，提供一些手写的扩展方法：

```csharp
namespace Sqlx.DependencyInjection;

public static class SqlxServiceCollectionExtensions
{
    // 手动注册单个仓储
    public static IServiceCollection AddSqlxRepository<TInterface, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.Add(new ServiceDescriptor(
            typeof(TInterface),
            sp =>
            {
                var connection = sp.GetRequiredService<DbConnection>();
                return Activator.CreateInstance(typeof(TImplementation), connection)!;
            },
            lifetime));
        
        return services;
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
    public string Email { get; set; }
}

// 3. 定义仓储
public interface IUserRepository : ICrudRepository<User, long> { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. 定义 SqlxContext（新增）
[SqlxContext(Lifetime = ServiceLifetime.Scoped)]
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class AppDbContext : SqlxContext
{
    public AppDbContext(string connectionString) : base(connectionString) { }
    
    public IUserRepository Users => GetRepository<IUserRepository>();
}

// 5. 配置 DI（在 Program.cs）
// 源生成器会自动生成 AddAppDbContext 扩展方法
builder.Services.AddAppDbContext("Data Source=app.db");

// 或从配置读取
builder.Services.AddAppDbContext(builder.Configuration);

// 6. 使用（通过构造函数注入）
public class UserService
{
    private readonly AppDbContext _db;
    
    public UserService(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _db.Users.GetAllAsync();
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

// 配置 Sqlx DI（源生成器自动生成的扩展方法）
builder.Services.AddAppDbContext(builder.Configuration); // 从 appsettings.json 读取

// 或手动指定连接字符串
builder.Services.AddAppDbContext("Data Source=app.db");

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
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
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

### SqlxContext 基类

```csharp
public abstract class SqlxContext : IDisposable
{
    protected DbConnection Connection { get; }
    private readonly Dictionary<Type, object> _repositories = new();
    
    protected SqlxContext(string connectionString)
    {
        Connection = CreateConnection(connectionString);
    }
    
    protected abstract DbConnection CreateConnection(string connectionString);
    
    protected TRepository GetRepository<TRepository>() where TRepository : class
    {
        var type = typeof(TRepository);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            repo = CreateRepository<TRepository>();
            _repositories[type] = repo;
        }
        return (TRepository)repo;
    }
    
    protected abstract TRepository CreateRepository<TRepository>() where TRepository : class;
    
    public void Dispose()
    {
        Connection?.Dispose();
    }
}
```

### SqlxContextAttribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class SqlxContextAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}

public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
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

### 编译时验证

源生成器在编译时验证：
- SqlxContext 类必须是 partial
- 必须有正确的构造函数签名
- 仓储属性必须有对应的实现类
- 连接字符串配置必须存在

### 运行时验证（Web API 示例）

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

### 源生成器性能

- 代码生成在编译时完成，零运行时开销
- 生成的代码直接调用构造函数，无反射
- 完全 AOT 兼容
- 目标生成时间 < 1s

### DI 注册性能

- 生成的注册代码与手写代码性能相同
- 无反射，无动态类型创建
- 连接创建使用简单的 new 表达式
- 目标注册开销 < 1ms

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
