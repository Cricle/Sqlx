# 属性参考

Sqlx 提供了丰富的属性来控制代码生成和 SQL 行为。本文档详细介绍了所有可用的属性及其用法。

## 📋 属性概览

| 属性 | 适用范围 | 功能 | 状态 |
|------|----------|------|------|
| `[RepositoryFor]` | 类 | 标记 Repository 实现类 | ✅ 完全支持 |
| `[SqlDefine]` | 类、方法 | 定义数据库方言 | ✅ 完全支持 |
| `[TableName]` | 类、接口、实体 | 指定表名 | ✅ 完全支持 |
| `[Sqlx]` | 方法 | 指定 SQL 语句 | ✅ 完全支持 |
| `[SqlExecuteType]` | 方法 | 指定 CRUD 操作类型 | ✅ 完全支持 |
| `[RawSql]` | 方法、参数 | 原始 SQL 处理 | ✅ 完全支持 |
| `[ExpressionToSql]` | 参数 | LINQ 表达式转 SQL | ✅ 完全支持 |
| `[DbColumn]` | 参数、属性 | 数据库列映射 | ✅ 完全支持 |
| `[Timeout]` | 方法、类 | 设置超时时间 | ✅ 完全支持 |
| `[ReadHandler]` | 参数 | 自定义数据读取 | ✅ 完全支持 |

## 🏗️ Repository 相关属性

### RepositoryForAttribute

**作用**: 标记一个类作为指定服务接口的 Repository 实现

**适用范围**: 类

**语法**:
```csharp
[RepositoryFor(typeof(IServiceInterface))]
public partial class MyRepository : IServiceInterface
{
    // Repository 实现
}
```

**参数**:
- `serviceType` (Type): 要实现的服务接口类型

**示例**:
```csharp
public interface IUserService
{
    IList<User> GetAllUsers();
    User? GetUserById(int id);
}

[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 所有接口方法将自动生成
}
```

## 🌐 数据库方言属性

### SqlDefineAttribute

**作用**: 定义数据库方言，控制 SQL 生成格式

**适用范围**: 类、方法

**语法**:
```csharp
// 使用预定义方言
[SqlDefine(dialectType)]

// 使用自定义方言
[SqlDefine(columnLeft, columnRight, stringLeft, stringRight, parameterPrefix)]
```

**预定义方言**:
- `0`: MySQL - 使用反引号 `` `column` `` 和 `@` 参数前缀
- `1`: SQL Server - 使用方括号 `[column]` 和 `@` 参数前缀
- `2`: PostgreSQL - 使用双引号 `"column"` 和 `$` 参数前缀

**自定义方言参数**:
- `columnLeft` (string): 列名左包装符
- `columnRight` (string): 列名右包装符
- `stringLeft` (string): 字符串左包装符
- `stringRight` (string): 字符串右包装符
- `parameterPrefix` (string): 参数前缀

**示例**:
```csharp
// MySQL Repository
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL 方言
public partial class MySqlUserRepository : IUserService { }

// 自定义方言
[RepositoryFor(typeof(IUserService))]
[SqlDefine("`", "`", "'", "'", ":")]  // 自定义方言
public partial class CustomRepository : IUserService { }

// 方法级别覆盖
public interface IMultiDbService
{
    IList<User> GetUsers();          // 使用类级别方言
    
    [SqlDefine(0)]                   // 方法级别覆盖为 MySQL
    IList<User> GetMySqlUsers();
}
```

### TableNameAttribute

**作用**: 指定数据库表名

**适用范围**: 类、接口、实体类

**语法**:
```csharp
[TableName("table_name")]
```

**参数**:
- `tableName` (string): 数据库表名

**优先级** (从高到低):
1. Repository 类级别的 TableName
2. 服务接口的 TableName
3. 实体类的 TableName
4. 实体类名

**示例**:
```csharp
// 实体级别
[TableName("user_accounts")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// 接口级别
[TableName("service_users")]
public interface IUserService
{
    IList<User> GetAllUsers();
}

// Repository 级别 (最高优先级)
[RepositoryFor(typeof(IUserService))]
[TableName("repository_users")]  // 最终使用这个表名
public partial class UserRepository : IUserService { }
```

## 🎯 SQL 定义属性

### SqlxAttribute

**作用**: 指定方法的 SQL 语句

**适用范围**: 方法

**语法**:
```csharp
[Sqlx("SQL statement")]
[Sqlx]  // 用于 ExpressionToSql 参数
```

**参数**:
- `sql` (string, 可选): SQL 语句

**示例**:
```csharp
public interface IUserService
{
    // 简单查询
    [Sqlx("SELECT * FROM users WHERE IsActive = 1")]
    IList<User> GetActiveUsers();
    
    // 参数化查询
    [Sqlx("SELECT * FROM users WHERE Age >= @minAge AND City = @city")]
    IList<User> GetUsersByAgeAndCity(int minAge, string city);
    
    // 复杂查询
    [Sqlx(@"
        SELECT u.*, p.ProfileData 
        FROM users u 
        LEFT JOIN profiles p ON u.Id = p.UserId 
        WHERE u.CreatedAt > @since
        ORDER BY u.CreatedAt DESC")]
    IList<UserWithProfile> GetUsersWithProfiles(DateTime since);
    
    // 与 ExpressionToSql 配合使用
    [Sqlx]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
}
```

### SqlExecuteTypeAttribute

**作用**: 指定 CRUD 操作类型和目标表

**适用范围**: 方法

**语法**:
```csharp
[SqlExecuteType(SqlExecuteTypes.OperationType, "tableName")]
```

**参数**:
- `executeType` (SqlExecuteTypes): 操作类型
- `tableName` (string): 目标表名

**SqlExecuteTypes 枚举**:
- `Select`: 查询操作
- `Insert`: 插入操作
- `Update`: 更新操作
- `Delete`: 删除操作

**示例**:
```csharp
public interface IProductService
{
    [SqlExecuteType(SqlExecuteTypes.Select, "products")]
    IList<Product> GetAllProducts();
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "products")]
    int CreateProduct(Product product);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "products")]
    int UpdateProduct(Product product);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "products")]
    int DeleteProduct(int id);
}
```

### RawSqlAttribute

**作用**: 标记原始 SQL 处理

**适用范围**: 方法、参数

**语法**:
```csharp
[RawSql]
[RawSql("default SQL")]
```

**参数**:
- `sql` (string, 可选): 默认 SQL 语句

**示例**:
```csharp
public interface IAdvancedService
{
    // 方法级别
    [RawSql("SELECT COUNT(*) FROM users")]
    int GetUserCount();
    
    // 参数级别
    IList<User> ExecuteQuery([RawSql] string sql, params object[] parameters);
}
```

## 🔄 表达式和参数属性

### ExpressionToSqlAttribute

**作用**: 标记参数为 LINQ 表达式转 SQL

**适用范围**: 参数

**语法**:
```csharp
[ExpressionToSql]
```

**示例**:
```csharp
public interface IQueryService
{
    [Sqlx]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
    
    [Sqlx]
    Task<IList<User>> QueryUsersAsync(
        [ExpressionToSql] ExpressionToSql<User> query,
        CancellationToken cancellationToken = default);
}

// 使用示例
var users = queryService.QueryUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.IsActive && u.Age >= 18)
        .OrderBy(u => u.Name)
        .Take(100)
);
```

## 🗂️ 列映射属性

### DbColumnAttribute

**作用**: 自定义数据库列映射

**适用范围**: 参数、属性

**语法**:
```csharp
[DbColumn]
[DbColumn("column_name")]
[DbColumn("column_name", Precision = precision, Scale = scale, Size = size, Direction = direction)]
```

**参数**:
- `name` (string, 可选): 列名
- `Precision` (byte): 精度
- `Scale` (byte): 小数位数
- `Size` (byte): 大小
- `Direction` (ParameterDirection): 参数方向

**示例**:
```csharp
public class User
{
    public int Id { get; set; }
    
    [DbColumn("user_name")]
    public string Name { get; set; } = string.Empty;
    
    [DbColumn("email_address")]
    public string Email { get; set; } = string.Empty;
    
    [DbColumn("created_date")]
    public DateTime CreatedAt { get; set; }
}

public interface IUserService
{
    // 参数级别的列映射
    [Sqlx("SELECT * FROM users WHERE user_name = @userName")]
    User? GetUserByName([DbColumn("user_name")] string userName);
}
```

## ⏱️ 性能和配置属性

### TimeoutAttribute

**作用**: 设置命令超时时间

**适用范围**: 方法、类、字段、属性、参数

**语法**:
```csharp
[Timeout]
[Timeout(timeoutInSeconds)]
```

**参数**:
- `timeout` (int, 可选): 超时时间（秒）

**示例**:
```csharp
public interface ILongRunningService
{
    // 方法级别超时
    [Timeout(300)]  // 5分钟超时
    [Sqlx("EXEC LongRunningStoredProcedure")]
    void ExecuteLongRunningProcedure();
    
    // 使用默认超时
    [Timeout]
    [Sqlx("SELECT * FROM large_table")]
    IList<LargeEntity> GetAllLargeEntities();
}

// 类级别超时
[RepositoryFor(typeof(ILongRunningService))]
[Timeout(120)]  // 所有方法默认2分钟超时
public partial class LongRunningRepository : ILongRunningService { }
```

### ReadHandlerAttribute

**作用**: 标记参数为自定义数据读取处理器

**适用范围**: 参数

**语法**:
```csharp
[ReadHandler]
```

**示例**:
```csharp
public interface ICustomReaderService
{
    [Sqlx("SELECT * FROM complex_data")]
    void ProcessComplexData([ReadHandler] Func<DbDataReader, Task> readerHandler);
    
    [Sqlx("SELECT * FROM users")]
    void ProcessUsers([ReadHandler] Action<DbDataReader> readerAction);
}

// 使用示例
customReaderService.ProcessComplexData(async reader =>
{
    while (await reader.ReadAsync())
    {
        // 自定义数据处理逻辑
        var id = reader.GetInt32("Id");
        var data = reader.GetString("Data");
        await ProcessDataAsync(id, data);
    }
});
```

## 🔧 属性组合使用

### 复杂示例

```csharp
[TableName("user_accounts")]
public class User
{
    public int Id { get; set; }
    
    [DbColumn("full_name")]
    public string Name { get; set; } = string.Empty;
    
    [DbColumn("email_address")]
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

[TableName("user_service")]
public interface IUserService
{
    // 基础查询
    [SqlExecuteType(SqlExecuteTypes.Select, "user_accounts")]
    IList<User> GetAllUsers();
    
    // 自定义 SQL 查询
    [Sqlx("SELECT * FROM user_accounts WHERE is_active = 1")]
    [Timeout(30)]
    IList<User> GetActiveUsers();
    
    // 多数据库方言支持
    [SqlDefine(0)]  // MySQL 方言
    [Sqlx("SELECT * FROM user_accounts WHERE full_name LIKE @pattern")]
    IList<User> SearchUsersByName(string pattern);
    
    // ExpressionToSql 支持
    [Sqlx]
    [Timeout(60)]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
    
    // CRUD 操作
    [SqlExecuteType(SqlExecuteTypes.Insert, "user_accounts")]
    int CreateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Update, "user_accounts")]
    int UpdateUser(User user);
    
    [SqlExecuteType(SqlExecuteTypes.Delete, "user_accounts")]
    int DeleteUser(int id);
    
    // 自定义读取处理
    [Sqlx("SELECT * FROM user_accounts")]
    void ProcessAllUsers([ReadHandler] Action<DbDataReader> processor);
}

[RepositoryFor(typeof(IUserService))]
[SqlDefine(1)]  // SQL Server 方言（类级别默认）
[TableName("custom_users")]  // 覆盖其他级别的表名
[Timeout(45)]  // 默认超时时间
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    private readonly ILogger<UserRepository> logger;
    
    public UserRepository(DbConnection connection, ILogger<UserRepository> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }
    
    // 拦截器
    partial void OnExecuting(string methodName, DbCommand command)
    {
        logger.LogInformation("执行 {Method}: {SQL}", methodName, command.CommandText);
    }
    
    partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
    {
        logger.LogInformation("完成 {Method}，耗时 {Elapsed}ms", methodName, elapsed / 10000.0);
    }
    
    partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)
    {
        logger.LogError(exception, "执行 {Method} 失败", methodName);
    }
}
```

## 📚 延伸阅读

- [Repository 模式指南](../repository-pattern.md)
- [SqlDefine 和 TableName 详解](../sqldefine-tablename.md)
- [ExpressionToSql 指南](../expression-to-sql.md)
- [类型映射参考](type-mapping.md)

---

这些属性为 Sqlx 提供了强大的灵活性和控制能力。通过合理组合使用这些属性，您可以构建高性能、类型安全的数据访问层。
