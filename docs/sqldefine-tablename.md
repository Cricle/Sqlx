# SqlDefine 和 TableName 属性指南

本指南详细介绍 Sqlx 中的 `SqlDefine` 和 `TableName` 属性，这两个属性是实现多数据库支持和自定义表名映射的核心功能。

## 🎯 概述

- **SqlDefine 属性**: 定义数据库方言，控制 SQL 生成的格式（列包装符、参数前缀等）
- **TableName 属性**: 指定数据库表名，支持自定义表名映射

这两个属性在 **RepositoryFor** 模式中已经完全修复并正常工作！

## 🌐 SqlDefine 属性

### 支持的数据库方言

| 数据库 | SqlDefine 值 | 列包装符 | 参数前缀 | 示例 SQL |
|--------|--------------|----------|----------|----------|
| **MySQL** | `0` | \`column\` | `@` | `SELECT * FROM \`users\` WHERE \`Id\` = @id` |
| **SQL Server** | `1` (默认) | [column] | `@` | `SELECT * FROM [users] WHERE [Id] = @id` |
| **PostgreSQL** | `2` | "column" | `$` | `SELECT * FROM "users" WHERE "Id" = $id` |
| **SQLite** | `1` (同 SQL Server) | [column] | `@` | `SELECT * FROM [users] WHERE [Id] = @id` |
| **自定义** | 5个参数构造函数 | 自定义 | 自定义 | 完全自定义的 SQL 格式 |

### 基础用法

#### 1. 使用预定义方言

```csharp
// MySQL Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL 方言
public partial class MySqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    public MySqlUserRepository(DbConnection connection) => this.connection = connection;
}

// PostgreSQL Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)]  // PostgreSQL 方言
public partial class PostgreSqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    public PostgreSqlUserRepository(DbConnection connection) => this.connection = connection;
}

// SQL Server Repository (默认，可省略)
[RepositoryFor(typeof(IUserService))]
[SqlDefine(1)]  // SQL Server 方言（可省略）
public partial class SqlServerUserRepository : IUserService
{
    private readonly DbConnection connection;
    public SqlServerUserRepository(DbConnection connection) => this.connection = connection;
}
```

#### 2. 自定义方言

```csharp
// 完全自定义的数据库方言
[RepositoryFor(typeof(IUserService))]
[SqlDefine("`", "`", "'", "'", ":")]  // 列左右包装符、字符串左右包装符、参数前缀
public partial class CustomDialectRepository : IUserService
{
    private readonly DbConnection connection;
    public CustomDialectRepository(DbConnection connection) => this.connection = connection;
}
```

**参数说明**：
- 参数1: `columnLeft` - 列名左包装符（如 `[`、`` ` ``、`"`）
- 参数2: `columnRight` - 列名右包装符（如 `]`、`` ` ``、`"`）
- 参数3: `stringLeft` - 字符串左包装符（通常是 `'`）
- 参数4: `stringRight` - 字符串右包装符（通常是 `'`）
- 参数5: `parameterPrefix` - 参数前缀（如 `@`、`$`、`:`）

### 生成的 SQL 示例

假设有一个简单的查询方法：

```csharp
public interface IUserService
{
    [SqlExecuteType(SqlExecuteTypes.Select, "users")]
    IList<User> GetAllUsers();
    
    [Sqlx("SELECT * FROM users WHERE Id = @id")]
    User? GetUserById(int id);
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
}
```

不同方言生成的 SQL：

```sql
-- MySQL (SqlDefine(0))
SELECT * FROM `users`
SELECT * FROM `users` WHERE `Id` = @id
INSERT INTO `users` (`Name`, `Email`) VALUES (@Name, @Email)

-- SQL Server (SqlDefine(1) 或默认)
SELECT * FROM [users]
SELECT * FROM [users] WHERE [Id] = @id
INSERT INTO [users] ([Name], [Email]) VALUES (@Name, @Email)

-- PostgreSQL (SqlDefine(2))
SELECT * FROM "users"
SELECT * FROM "users" WHERE "Id" = $id
INSERT INTO "users" ("Name", "Email") VALUES ($Name, $Email)

-- 自定义方言 (SqlDefine("`", "`", "'", "'", ":"))
SELECT * FROM `users`
SELECT * FROM `users` WHERE `Id` = :id
INSERT INTO `users` (`Name`, `Email`) VALUES (:Name, :Email)
```

### 方法级别的 SqlDefine 覆盖

```csharp
public interface IMultiDatabaseService
{
    // 使用类级别的默认方言
    IList<User> GetAllUsers();
    
    // 方法级别覆盖为 MySQL 方言
    [SqlDefine(0)]
    IList<User> GetMySqlUsers();
    
    // 方法级别覆盖为 PostgreSQL 方言
    [SqlDefine(2)]
    IList<User> GetPostgreSqlUsers();
}

[RepositoryFor(typeof(IMultiDatabaseService))]
[SqlDefine(1)]  // 类级别默认：SQL Server
public partial class MultiDatabaseRepository : IMultiDatabaseService
{
    private readonly DbConnection connection;
    public MultiDatabaseRepository(DbConnection connection) => this.connection = connection;
    
    // GetAllUsers() 生成: SELECT * FROM [users]
    // GetMySqlUsers() 生成: SELECT * FROM `users`
    // GetPostgreSqlUsers() 生成: SELECT * FROM "users"
}
```

## 📝 TableName 属性

### 基础用法

#### 1. 实体级别的表名

```csharp
[TableName("user_accounts")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 使用实体的表名
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    public UserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL 将使用 user_accounts 表
}
```

#### 2. Repository 级别覆盖表名

```csharp
// Repository 级别的表名会覆盖实体级别的表名
[RepositoryFor(typeof(IUserService))]
[TableName("custom_users")]  // 覆盖实体的 user_accounts
public partial class CustomUserRepository : IUserService
{
    private readonly DbConnection connection;
    public CustomUserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL 将使用 custom_users 表而不是 user_accounts
}
```

#### 3. 服务接口级别的表名

```csharp
[TableName("service_users")]
public interface IUserService
{
    IList<User> GetAllUsers();
    User? GetUserById(int id);
}

[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    public UserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL 将使用 service_users 表
}
```

### 表名优先级

表名的解析遵循以下优先级（从高到低）：

1. **Repository 类级别的 TableName 属性** - 最高优先级
2. **服务接口的 TableName 属性** - 中等优先级  
3. **实体类的 TableName 属性** - 较低优先级
4. **实体类名** - 默认值（最低优先级）

```csharp
[TableName("entity_users")]          // 优先级 3
public class User { ... }

[TableName("interface_users")]       // 优先级 2
public interface IUserService { ... }

[RepositoryFor(typeof(IUserService))]
[TableName("repository_users")]      // 优先级 1 (最高)
public partial class UserRepository : IUserService
{
    // 最终使用 repository_users 表名
}
```

## 🔄 组合使用 SqlDefine 和 TableName

```csharp
[TableName("custom_users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public interface IUserService
{
    IList<User> GetAllUsers();
    User? GetUserById(int id);
    int CreateUser(User user);
}

// MySQL Repository with custom table name
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]                    // MySQL 方言
[TableName("mysql_users")]        // 自定义表名
public partial class MySqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    public MySqlUserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL:
    // SELECT * FROM `mysql_users`
    // SELECT * FROM `mysql_users` WHERE `Id` = @id
    // INSERT INTO `mysql_users` (`Name`, `Email`) VALUES (@Name, @Email)
}

// PostgreSQL Repository with different table name
[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)]                    // PostgreSQL 方言
[TableName("pg_users")]           // 不同的表名
public partial class PostgreSqlUserRepository : IUserService
{
    private readonly DbConnection connection;
    public PostgreSqlUserRepository(DbConnection connection) => this.connection = connection;
    
    // 生成的 SQL:
    // SELECT * FROM "pg_users"
    // SELECT * FROM "pg_users" WHERE "Id" = $id
    // INSERT INTO "pg_users" ("Name", "Email") VALUES ($Name, $Email)
}
```

## 🚀 实际应用场景

### 1. 多数据库支持的微服务

```csharp
// 共同的服务接口
public interface IProductService
{
    IList<Product> GetAllProducts();
    Product? GetProductById(int id);
    int CreateProduct(Product product);
}

// MySQL 实现
[RepositoryFor(typeof(IProductService))]
[SqlDefine(0)]
[TableName("products")]
public partial class MySqlProductRepository : IProductService
{
    private readonly MySqlConnection connection;
    public MySqlProductRepository(MySqlConnection connection) => this.connection = connection;
}

// PostgreSQL 实现
[RepositoryFor(typeof(IProductService))]
[SqlDefine(2)]
[TableName("products")]
public partial class PostgreSqlProductRepository : IProductService
{
    private readonly NpgsqlConnection connection;
    public PostgreSqlProductRepository(NpgsqlConnection connection) => this.connection = connection;
}

// SQL Server 实现
[RepositoryFor(typeof(IProductService))]
[SqlDefine(1)]
[TableName("Products")]  // SQL Server 通常使用 PascalCase
public partial class SqlServerProductRepository : IProductService
{
    private readonly SqlConnection connection;
    public SqlServerProductRepository(SqlConnection connection) => this.connection = connection;
}
```

### 2. 多租户系统

```csharp
public interface ITenantUserService
{
    IList<User> GetAllUsers();
    User? GetUserById(int id);
}

// 租户 A 的用户 Repository
[RepositoryFor(typeof(ITenantUserService))]
[TableName("tenant_a_users")]
public partial class TenantAUserRepository : ITenantUserService
{
    private readonly DbConnection connection;
    public TenantAUserRepository(DbConnection connection) => this.connection = connection;
}

// 租户 B 的用户 Repository
[RepositoryFor(typeof(ITenantUserService))]
[TableName("tenant_b_users")]
public partial class TenantBUserRepository : ITenantUserService
{
    private readonly DbConnection connection;
    public TenantBUserRepository(DbConnection connection) => this.connection = connection;
}
```

### 3. 历史数据和实时数据分离

```csharp
public interface IOrderService
{
    IList<Order> GetAllOrders();
    IList<Order> GetOrdersByDate(DateTime date);
}

// 实时订单 Repository
[RepositoryFor(typeof(IOrderService))]
[TableName("orders_current")]
public partial class CurrentOrderRepository : IOrderService
{
    private readonly DbConnection connection;
    public CurrentOrderRepository(DbConnection connection) => this.connection = connection;
}

// 历史订单 Repository
[RepositoryFor(typeof(IOrderService))]
[TableName("orders_history")]
public partial class HistoryOrderRepository : IOrderService
{
    private readonly DbConnection connection;
    public HistoryOrderRepository(DbConnection connection) => this.connection = connection;
}
```

## 🔧 配置和依赖注入

### ASP.NET Core 中的配置

```csharp
// Startup.cs 或 Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // 根据配置选择不同的数据库实现
    var databaseType = Configuration.GetValue<string>("DatabaseType");
    
    switch (databaseType?.ToLower())
    {
        case "mysql":
            services.AddScoped<DbConnection>(provider => 
                new MySqlConnection(Configuration.GetConnectionString("MySQL")));
            services.AddScoped<IUserService, MySqlUserRepository>();
            break;
            
        case "postgresql":
            services.AddScoped<DbConnection>(provider => 
                new NpgsqlConnection(Configuration.GetConnectionString("PostgreSQL")));
            services.AddScoped<IUserService, PostgreSqlUserRepository>();
            break;
            
        case "sqlserver":
        default:
            services.AddScoped<DbConnection>(provider => 
                new SqlConnection(Configuration.GetConnectionString("SqlServer")));
            services.AddScoped<IUserService, SqlServerUserRepository>();
            break;
    }
}
```

### 工厂模式

```csharp
public interface IRepositoryFactory
{
    T CreateRepository<T>(string databaseType) where T : class;
}

public class RepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider serviceProvider;
    
    public RepositoryFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    
    public T CreateRepository<T>(string databaseType) where T : class
    {
        return databaseType.ToLower() switch
        {
            "mysql" => serviceProvider.GetRequiredService<MySqlUserRepository>() as T,
            "postgresql" => serviceProvider.GetRequiredService<PostgreSqlUserRepository>() as T,
            "sqlserver" => serviceProvider.GetRequiredService<SqlServerUserRepository>() as T,
            _ => throw new NotSupportedException($"数据库类型 {databaseType} 不支持")
        };
    }
}
```

## ⚠️ 注意事项和最佳实践

### 1. 属性应用范围

```csharp
// ✅ 正确：SqlDefine 可以应用在类和方法上
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // 类级别
public partial class UserRepository : IUserService
{
    [SqlDefine(2)]  // 方法级别覆盖
    public IList<User> GetSpecialUsers() { /* 自动生成 */ }
}

// ✅ 正确：TableName 可以应用在类、接口和实体上
[TableName("users")]
public class User { }

[TableName("user_service")]
public interface IUserService { }

[RepositoryFor(typeof(IUserService))]
[TableName("repository_users")]
public partial class UserRepository : IUserService { }

// ❌ 错误：TableName 不能应用在方法上
public interface IUserService
{
    [TableName("method_table")]  // 编译错误！
    IList<User> GetUsers();
}
```

### 2. 参数前缀一致性

```csharp
// ✅ 确保 SQL 中的参数前缀与 SqlDefine 一致
[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)]  // PostgreSQL 使用 $ 前缀
public partial class PgUserRepository : IUserService
{
    // ✅ 正确：使用 $ 前缀
    [Sqlx("SELECT * FROM users WHERE age > $minAge")]
    IList<User> GetUsersByMinAge(int minAge);
    
    // ❌ 错误：使用了 @ 前缀，但 PostgreSQL 应该用 $
    [Sqlx("SELECT * FROM users WHERE age > @minAge")]
    IList<User> GetUsersByMinAgeWrong(int minAge);
}
```

### 3. 表名命名规范

```csharp
// ✅ 推荐：使用数据库的命名规范
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL
[TableName("user_accounts")]  // MySQL 通常使用 snake_case
public partial class MySqlUserRepository : IUserService { }

[RepositoryFor(typeof(IUserService))]
[SqlDefine(1)]  // SQL Server
[TableName("UserAccounts")]   // SQL Server 通常使用 PascalCase
public partial class SqlServerUserRepository : IUserService { }

[RepositoryFor(typeof(IUserService))]
[SqlDefine(2)]  // PostgreSQL
[TableName("user_accounts")]  // PostgreSQL 通常使用 snake_case
public partial class PostgreSqlUserRepository : IUserService { }
```

## 🐛 故障排除

### 常见问题

1. **SqlDefine 属性不生效**
   - 确保使用的是最新版本的 Sqlx
   - 检查属性是否正确应用在 Repository 类上
   - 验证参数值是否正确（0=MySQL, 1=SqlServer, 2=PostgreSQL）

2. **TableName 属性不生效**
   - 检查属性的应用位置和优先级
   - 确保表名字符串不为空
   - 验证生成的代码中是否使用了正确的表名

3. **生成的 SQL 格式不正确**
   - 检查 SqlDefine 的参数顺序
   - 确保自定义方言的 5 个参数都正确设置
   - 验证参数前缀与 SQL 中使用的一致

### 调试技巧

```csharp
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL
public partial class DebugUserRepository : IUserService
{
    private readonly DbConnection connection;
    private readonly ILogger<DebugUserRepository> logger;
    
    public DebugUserRepository(DbConnection connection, ILogger<DebugUserRepository> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }
    
    // 使用拦截器调试生成的 SQL
    partial void OnExecuting(string methodName, DbCommand command)
    {
        logger.LogInformation("执行方法: {Method}", methodName);
        logger.LogInformation("生成的 SQL: {SQL}", command.CommandText);
        
        foreach (DbParameter param in command.Parameters)
        {
            logger.LogInformation("参数: {Name} = {Value}", param.ParameterName, param.Value);
        }
    }
}
```

## 📚 延伸阅读

- [Repository 模式指南](repository-pattern.md)
- [ExpressionToSql 指南](expression-to-sql.md)
- [多数据库示例](examples/multi-database-examples.md)
- [性能优化指南](OPTIMIZATION_GUIDE.md)

---

SqlDefine 和 TableName 属性是 Sqlx 实现多数据库支持的核心功能。通过合理使用这两个属性，您可以轻松构建支持多种数据库的应用程序！
