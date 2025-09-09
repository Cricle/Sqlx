# 常见问题 FAQ

本文档收集了 Sqlx 使用过程中的常见问题和解决方案。

## 🎯 目录

- [安装和配置问题](#安装和配置问题)
- [编译错误](#编译错误)
- [Repository 模式问题](#repository-模式问题)
- [SqlDefine 和 TableName 问题](#sqldefine-和-tablename-问题)
- [性能问题](#性能问题)
- [数据库连接问题](#数据库连接问题)
- [异步操作问题](#异步操作问题)

## 📦 安装和配置问题

### Q: 安装 Sqlx 包后没有代码生成

**问题**: 添加了 Sqlx NuGet 包，但是没有自动生成代码。

**解决方案**:

1. **检查包引用**:
```xml
<PackageReference Include="Sqlx" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

2. **重新构建项目**:
```bash
dotnet clean
dotnet build
```

3. **检查 IDE 设置**:
   - Visual Studio: 工具 → 选项 → 文本编辑器 → C# → 高级 → 启用源生成器
   - VS Code: 重启 OmniSharp 服务器

4. **查看生成的文件**:
   - 在解决方案资源管理器中展开 "Dependencies" → "Analyzers" → "Sqlx"
   - 生成的文件通常在 `obj/Debug/netX.X/generated/Sqlx/` 目录下

### Q: 在 .NET Framework 中使用 Sqlx

**问题**: .NET Framework 项目中 Sqlx 不工作。

**解决方案**:

Sqlx 需要 .NET Standard 2.0 或更高版本。对于 .NET Framework：

1. **最低版本要求**: .NET Framework 4.7.2 或更高
2. **启用源生成器**:
```xml
<PropertyGroup>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

## 🔨 编译错误

### Q: 编译错误 "找不到类型或命名空间"

**问题**: 
```
error CS0246: The type or namespace name 'SqlxAttribute' could not be found
```

**解决方案**:

1. **添加 using 语句**:
```csharp
using Sqlx.Annotations;
```

2. **检查包引用是否正确安装**:
```bash
dotnet list package
```

3. **清理并重新构建**:
```bash
dotnet clean
dotnet restore
dotnet build
```

### Q: 编译错误 "partial 方法没有实现"

**问题**:
```
error CS0759: No defining declaration found for implementing declaration of partial method
```

**解决方案**:

1. **确保类标记为 partial**:
```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService  // 必须是 partial
{
    // ...
}
```

2. **检查接口实现**:
```csharp
// 确保实现了接口
public partial class UserRepository : IUserService
{
    // 不需要手动实现接口方法，Sqlx 会自动生成
}
```

### Q: 编译错误 "循环依赖"

**问题**: 
```
error CS0146: Circular base class dependency
```

**解决方案**:

检查是否有循环引用：
```csharp
// ❌ 错误：循环依赖
public interface IUserService : IBaseService<UserRepository> { }
public class UserRepository : IUserService { }

// ✅ 正确：清晰的层次结构
public interface IUserService { }
public class UserRepository : IUserService { }
```

## 🏗️ Repository 模式问题

### Q: Repository 方法没有自动生成

**问题**: 标记了 `[RepositoryFor]` 但是接口方法没有自动实现。

**解决方案**:

1. **检查属性语法**:
```csharp
// ✅ 正确
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService

// ❌ 错误
[RepositoryFor(IUserService)]  // 缺少 typeof
public class UserRepository    // 缺少 partial
```

2. **检查接口方法标记**:
```csharp
public interface IUserService
{
    // 需要至少一个 Sqlx 相关属性
    [Sqlx("SELECT * FROM users")]
    IList<User> GetAllUsers();
    
    // 或者使用 SqlExecuteType
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
}
```

3. **检查方法签名**:
```csharp
// ✅ 支持的返回类型
IList<User> GetUsers();
User? GetUser();
int ExecuteCommand();
Task<IList<User>> GetUsersAsync();

// ❌ 不支持的返回类型
List<User> GetUsers();      // 使用 IList<T> 而不是 List<T>
User[] GetUsers();          // 使用 IList<T> 而不是数组
```

### Q: 拦截器方法不被调用

**问题**: 定义了拦截器方法但是没有被调用。

**解决方案**:

1. **检查方法签名**:
```csharp
// ✅ 正确的签名
partial void OnExecuting(string methodName, DbCommand command);
partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed);
partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed);

// ❌ 错误的签名
partial void OnExecuting(DbCommand command);  // 缺少 methodName 参数
void OnExecuting(string methodName, DbCommand command);  // 缺少 partial 修饰符
```

2. **确保方法是 partial**:
```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    // 必须是 partial void
    partial void OnExecuting(string methodName, DbCommand command)
    {
        // 拦截器逻辑
    }
}
```

## 🌐 SqlDefine 和 TableName 问题

### Q: SqlDefine 属性不生效

**问题**: 设置了 SqlDefine 属性但生成的 SQL 仍然使用默认格式。

**解决方案**:

1. **检查属性位置**:
```csharp
// ✅ 正确：在 Repository 类上
[RepositoryFor(typeof(IUserService))]
[SqlDefine(0)]  // MySQL 方言
public partial class MySqlUserRepository : IUserService { }

// ✅ 正确：在方法上（覆盖类级别）
public interface IUserService
{
    [SqlDefine(2)]  // PostgreSQL 方言
    [Sqlx("SELECT * FROM users")]
    IList<User> GetPostgreSqlUsers();
}
```

2. **检查参数值**:
```csharp
// ✅ 正确的预定义值
[SqlDefine(0)]  // MySQL
[SqlDefine(1)]  // SQL Server
[SqlDefine(2)]  // PostgreSQL

// ✅ 正确的自定义方言
[SqlDefine("`", "`", "'", "'", "@")]

// ❌ 错误的值
[SqlDefine(3)]  // 不存在的预定义值
[SqlDefine("`", "`")]  // 参数不完整
```

3. **确保使用最新版本**:
```bash
dotnet list package
dotnet update package Sqlx
```

### Q: TableName 属性优先级问题

**问题**: 设置了多个 TableName 属性，不知道哪个会生效。

**解决方案**:

**优先级顺序** (从高到低):
```csharp
// 1. Repository 类级别 (最高优先级)
[RepositoryFor(typeof(IUserService))]
[TableName("repository_users")]  // ← 这个会生效
public partial class UserRepository : IUserService { }

// 2. 服务接口级别
[TableName("interface_users")]   // ← 如果 Repository 没有，这个生效
public interface IUserService { }

// 3. 实体类级别
[TableName("entity_users")]      // ← 如果上面都没有，这个生效
public class User { }

// 4. 实体类名 (默认，最低优先级)
public class User { }            // ← 默认使用 "User"
```

## 🚀 性能问题

### Q: 查询性能比预期慢

**问题**: Sqlx 生成的查询性能不如预期。

**解决方案**:

1. **启用连接池**:
```csharp
// ✅ 使用连接池
services.AddScoped<DbConnection>(provider => 
    new SqlConnection(connectionString));

// ❌ 避免每次创建新连接
// new SqlConnection(connectionString)
```

2. **使用异步方法**:
```csharp
// ✅ 异步方法性能更好
Task<IList<User>> GetUsersAsync(CancellationToken cancellationToken = default);

// ❌ 同步方法会阻塞线程
IList<User> GetUsers();
```

3. **优化 SQL 查询**:
```csharp
// ✅ 添加索引和优化查询
[Sqlx("SELECT Id, Name FROM users WHERE IsActive = 1")]  // 只查询需要的列
IList<User> GetActiveUsers();

// ❌ 避免 SELECT *
[Sqlx("SELECT * FROM users")]
IList<User> GetAllUsers();
```

4. **使用拦截器监控**:
```csharp
partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
{
    var elapsedMs = elapsed / 10000.0;
    if (elapsedMs > 1000)  // 超过 1 秒的慢查询
    {
        logger.LogWarning("慢查询: {Method} - {ElapsedMs}ms - {SQL}", 
            methodName, elapsedMs, command.CommandText);
    }
}
```

### Q: 内存使用过高

**问题**: 应用程序内存使用持续增长。

**解决方案**:

1. **正确释放连接**:
```csharp
// ✅ 使用 using 语句
using var connection = new SqlConnection(connectionString);
var repo = new UserRepository(connection);

// ✅ 或者在 DI 中注册为 Scoped
services.AddScoped<DbConnection>(provider => 
    new SqlConnection(connectionString));
```

2. **避免大量数据一次性加载**:
```csharp
// ❌ 避免一次性加载大量数据
[Sqlx("SELECT * FROM huge_table")]
IList<HugeEntity> GetAllHugeEntities();

// ✅ 使用分页
[Sqlx("SELECT * FROM huge_table ORDER BY Id OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY")]
IList<HugeEntity> GetHugeEntitiesPaged(int offset, int pageSize);
```

3. **使用流式处理**:
```csharp
// ✅ 使用 ReadHandler 进行流式处理
[Sqlx("SELECT * FROM large_table")]
void ProcessLargeData([ReadHandler] Action<DbDataReader> processor);

// 使用
repo.ProcessLargeData(reader =>
{
    while (reader.Read())
    {
        // 逐行处理，不占用大量内存
        ProcessSingleRow(reader);
    }
});
```

## 🔌 数据库连接问题

### Q: 连接字符串错误

**问题**: 数据库连接失败。

**解决方案**:

1. **检查连接字符串格式**:
```csharp
// SQL Server
"Server=localhost;Database=MyDB;Integrated Security=true"
"Server=localhost;Database=MyDB;User Id=sa;Password=password"

// MySQL
"Server=localhost;Database=MyDB;Uid=root;Pwd=password"

// PostgreSQL
"Host=localhost;Database=MyDB;Username=postgres;Password=password"

// SQLite
"Data Source=mydb.db"
"Data Source=:memory:"  // 内存数据库
```

2. **测试连接**:
```csharp
using var connection = new SqlConnection(connectionString);
try
{
    connection.Open();
    Console.WriteLine("连接成功");
}
catch (Exception ex)
{
    Console.WriteLine($"连接失败: {ex.Message}");
}
```

### Q: 事务问题

**问题**: 事务操作失败或不按预期工作。

**解决方案**:

1. **正确使用事务**:
```csharp
using var connection = new SqlConnection(connectionString);
connection.Open();

using var transaction = connection.BeginTransaction();
try
{
    var repo = new UserRepository(connection);
    
    // 如果 Repository 支持事务参数
    repo.CreateUser(user1, transaction);
    repo.CreateUser(user2, transaction);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

2. **检查事务隔离级别**:
```csharp
using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
```

## ⚡ 异步操作问题

### Q: 异步方法死锁

**问题**: 在 ASP.NET 中使用异步方法导致死锁。

**解决方案**:

1. **使用 ConfigureAwait(false)**:
```csharp
// ✅ 避免死锁
var users = await repo.GetUsersAsync().ConfigureAwait(false);

// ❌ 可能导致死锁
var users = await repo.GetUsersAsync();
```

2. **一路异步到底**:
```csharp
// ✅ 控制器方法也应该是异步的
[HttpGet]
public async Task<IActionResult> GetUsers()
{
    var users = await userRepo.GetUsersAsync();
    return Ok(users);
}

// ❌ 避免混合同步和异步
[HttpGet]
public IActionResult GetUsers()
{
    var users = userRepo.GetUsersAsync().Result;  // 容易死锁
    return Ok(users);
}
```

### Q: CancellationToken 不工作

**问题**: 传递了 CancellationToken 但操作无法取消。

**解决方案**:

1. **确保方法签名正确**:
```csharp
// ✅ 正确的异步方法签名
[Sqlx("SELECT * FROM users")]
Task<IList<User>> GetUsersAsync(CancellationToken cancellationToken = default);

// ❌ 缺少 CancellationToken 参数
[Sqlx("SELECT * FROM users")]
Task<IList<User>> GetUsersAsync();
```

2. **在调用时传递 Token**:
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    var users = await repo.GetUsersAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("操作被取消");
}
```

## 🔍 调试技巧

### 查看生成的代码

1. **在项目文件中启用代码生成输出**:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

2. **查看生成的文件**:
   - 文件位置: `Generated/Sqlx/`
   - 文件名: `YourRepository.g.cs`

### 启用详细日志

```csharp
// 使用拦截器记录所有 SQL 执行
partial void OnExecuting(string methodName, DbCommand command)
{
    Console.WriteLine($"执行: {methodName}");
    Console.WriteLine($"SQL: {command.CommandText}");
    
    foreach (DbParameter param in command.Parameters)
    {
        Console.WriteLine($"参数: {param.ParameterName} = {param.Value}");
    }
}
```

## 🆘 获取帮助

如果以上解决方案都无法解决您的问题：

1. **查看文档**: [完整文档](../README.md)
2. **搜索已知问题**: [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
3. **提交问题**: [新建 Issue](https://github.com/Cricle/Sqlx/issues/new)
4. **参与讨论**: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)

提交问题时请包含：
- Sqlx 版本
- .NET 版本
- 完整的错误信息
- 最小重现代码示例
- 相关的项目文件配置

---

希望这个 FAQ 能帮助您解决常见问题。如果您发现了新的问题和解决方案，欢迎贡献到这个文档中！
