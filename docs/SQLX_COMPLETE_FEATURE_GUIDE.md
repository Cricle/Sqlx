# Sqlx 源生成器完整功能指南

## 📋 概述

Sqlx 是一个强大的 C# 源生成器，专为简化数据库操作而设计。它通过编译时代码生成提供类型安全的 SQL 操作，无运行时性能损失。

## 🚀 核心功能架构

### 1. **RawSql 特性** (已合并到 Sqlx 特性)

**功能说明**: 允许开发者手写原始 SQL 语句，源生成器会自动生成执行代码。

**特性语法**:
```csharp
[Sqlx("SELECT * FROM users WHERE age > @minAge")]
public partial Task<IEnumerable<User>> GetUsersByAgeAsync(int minAge);
```

**核心优势**:
- ✅ 完全控制 SQL 语句
- ✅ 编译时 SQL 语法验证
- ✅ 自动参数映射
- ✅ 强类型结果映射

**代码示例**:
```csharp
public partial class UserService
{
    private readonly IDbConnection _connection;
    public IDbConnection dbContext => _connection;

    // 简单查询
    [Sqlx("SELECT COUNT(*) FROM users WHERE is_active = 1")]
    public partial Task<int> GetActiveUserCountAsync();

    // 参数化查询
    [Sqlx("SELECT * FROM users WHERE age > @minAge AND department_id = @deptId")]
    public partial Task<IEnumerable<User>> GetUsersByAgeAndDepartmentAsync(int minAge, int deptId);
}
```

### 2. **Sqlx 特性** - 存储过程支持

**功能说明**: 支持调用数据库存储过程，自动处理参数映射和结果集。

**特性语法**:
```csharp
[Sqlx("sp_GetUsersByDepartment")]
public partial Task<IEnumerable<User>> CallStoredProcedureAsync(int departmentId);
```

**核心优势**:
- ✅ 存储过程调用
- ✅ 输入/输出参数支持
- ✅ 复杂结果集映射
- ✅ 事务支持

**代码示例**:
```csharp
public partial class UserService
{
    // 存储过程调用
    [Sqlx("sp_GetUsersByDepartment")]
    public partial Task<IEnumerable<User>> GetDepartmentUsersAsync(int departmentId);

    // 复杂返回类型
    [Sqlx("sp_GetDepartmentStats")]
    public partial Task<(int UserCount, decimal AvgSalary, decimal TotalBudget)> GetStatsAsync(int deptId);
}
```

### 3. **SqlExecuteType 特性** - CRUD 生成和批量操作

**功能说明**: 根据指定的操作类型和表名自动生成标准 CRUD 操作。

**支持的操作类型**:
- `SqlOperation.Select` - 查询操作
- `SqlOperation.Insert` - 插入操作  
- `SqlOperation.Update` - 更新操作
- `SqlOperation.Delete` - 删除操作

**批量操作支持**:
```csharp
public static class SqlExecuteTypeValues
{
    public const int BatchInsert = 4;
    public const int BatchUpdate = 5;
    public const int BatchDelete = 6;
    public const int BatchCommand = 7;
}
```

**特性语法**:
```csharp
[SqlExecuteType(SqlOperation.Select, "users")]
public partial Task<IEnumerable<User>> SelectUsersAsync();

[SqlExecuteType(SqlOperation.Insert, "users")]
public partial Task<int> InsertUserAsync(User user);
```

**核心优势**:
- ✅ 自动 CRUD 生成
- ✅ 批量操作支持
- ✅ 表名自动推断
- ✅ 实体属性映射

**代码示例**:
```csharp
public partial class UserService
{
    // 自动生成查询
    [SqlExecuteType(SqlOperation.Select, "users")]
    public partial Task<IEnumerable<User>> GetAllUsersAsync();

    // 自动生成插入
    [SqlExecuteType(SqlOperation.Insert, "users")]
    public partial Task<int> CreateUserAsync(User user);

    // 自动生成更新
    [SqlExecuteType(SqlOperation.Update, "users")]
    public partial Task<int> UpdateUserAsync(User user);

    // 自动生成删除
    [SqlExecuteType(SqlOperation.Delete, "users")]
    public partial Task<int> DeleteUserAsync(int id);

    // 批量操作示例
    [Sqlx("BATCH_INSERT_USERS")]
    public partial Task<int> BatchInsertUsersAsync(IEnumerable<User> users);
}
```

### 4. **RepositoryFor 特性** - 自动仓储模式生成

**功能说明**: 通过接口定义自动生成完整的仓储实现，实现标准的仓储模式。

**特性语法**:
```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    // 源生成器会自动实现接口的所有方法
}
```

**核心优势**:
- ✅ 自动接口实现
- ✅ 方法名推断 SQL 操作
- ✅ 标准仓储模式
- ✅ 依赖注入友好

**代码示例**:
```csharp
// 1. 定义服务接口
public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
}

// 2. 使用 RepositoryFor 自动实现
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly IDbConnection _connection;
    
    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    // 源生成器会自动生成以下方法的实现:
    // - GetAllUsersAsync() -> SELECT * FROM users
    // - GetUserByIdAsync(int id) -> SELECT * FROM users WHERE id = @id
    // - CreateUserAsync(User user) -> INSERT INTO users ...
    // - UpdateUserAsync(User user) -> UPDATE users SET ... WHERE id = @id
    // - DeleteUserAsync(int id) -> DELETE FROM users WHERE id = @id
    // - SearchUsersAsync(string searchTerm) -> SELECT * FROM users WHERE name LIKE @searchTerm
}
```

## 🌐 多数据库方言支持

Sqlx 支持多种数据库方言，通过 `SqlDefine` 特性指定：

```csharp
// MySQL 方言
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySqlUserService
{
    [Sqlx("SELECT * FROM `users` WHERE `is_active` = 1")]
    public partial Task<IEnumerable<User>> GetActiveUsersAsync();
}

// PostgreSQL 方言
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class PostgreSqlUserService
{
    [Sqlx("SELECT * FROM \"users\" WHERE \"is_active\" = true")]
    public partial Task<IEnumerable<User>> GetActiveUsersAsync();
}

// SQLite 方言
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class SqliteUserService
{
    [Sqlx("SELECT * FROM [users] WHERE [is_active] = 1")]
    public partial Task<IEnumerable<User>> GetActiveUsersAsync();
}

// SQL Server 方言
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class SqlServerUserService
{
    [Sqlx("SELECT * FROM [users] WHERE [is_active] = 1")]
    public partial Task<IEnumerable<User>> GetActiveUsersAsync();
}
```

**支持的数据库**:
- ✅ MySQL
- ✅ PostgreSQL  
- ✅ SQLite
- ✅ SQL Server
- ✅ Oracle
- ✅ DB2

## 🔧 高级功能

### 1. LINQ 表达式转 SQL 支持

```csharp
public partial class UserService
{
    [Sqlx("SELECT * FROM users")]
    public partial Task<IEnumerable<User>> GetUsersAsync(
        [ExpressionToSql] Expression<Func<User, bool>> predicate);
        
    [Sqlx("SELECT * FROM users")]
    public partial Task<IEnumerable<User>> GetUsersSortedAsync(
        [ExpressionToSql] Expression<Func<User, object>> orderBy);
}

// 使用示例:
var activeUsers = await userService.GetUsersAsync(u => u.IsActive && u.Age > 18);
var sortedUsers = await userService.GetUsersSortedAsync(u => u.Name);
```

### 2. 批量操作支持

```csharp
public partial class UserService
{
    // 批量插入
    [Sqlx("BATCH_INSERT")]
    public partial Task<int> BatchInsertUsersAsync(IEnumerable<User> users);
    
    // 批量更新
    [Sqlx("BATCH_UPDATE")]
    public partial Task<int> BatchUpdateUsersAsync(IEnumerable<User> users);
    
    // 批量删除
    [Sqlx("BATCH_DELETE")]
    public partial Task<int> BatchDeleteUsersAsync(IEnumerable<int> userIds);
}
```

### 3. 事务支持

```csharp
public partial class UserService
{
    [Sqlx("INSERT INTO users (name, email) VALUES (@name, @email)")]
    public partial Task<int> CreateUserAsync(string name, string email, IDbTransaction transaction);
}
```

### 4. 取消令牌支持

```csharp
public partial class UserService
{
    [Sqlx("SELECT * FROM users")]
    public partial Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default);
}
```

## 📊 性能特性

### 编译时优化
- ✅ **零运行时反射** - 所有映射在编译时生成
- ✅ **内联 SQL** - SQL 直接嵌入生成代码
- ✅ **优化的参数绑定** - 最小的装箱/拆箱
- ✅ **连接池友好** - 高效的连接使用

### 内存效率
- ✅ **流式处理** - 支持 `IEnumerable<T>` 和 `IAsyncEnumerable<T>`
- ✅ **最小分配** - 复用对象和缓冲区
- ✅ **批量处理** - 减少数据库往返

## 🛠️ 配置和集成

### 1. 项目配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Sqlx" Version="latest" />
    <PackageReference Include="Sqlx.Generator" Version="latest" />
  </ItemGroup>
</Project>
```

### 2. 依赖注入集成

```csharp
// Program.cs
services.AddScoped<IDbConnection>(provider => 
    new SqliteConnection(connectionString));
services.AddScoped<IUserService, UserRepository>();
```

### 3. Entity Framework 集成

Sqlx 主要设计为与 Entity Framework 集成使用：

```csharp
public partial class UserService
{
    private readonly DbContext _context;
    public DbContext dbContext => _context;  // EF 集成
    
    [Sqlx("SELECT * FROM users WHERE is_active = 1")]
    public partial Task<IEnumerable<User>> GetActiveUsersAsync();
}
```

## 🎯 最佳实践

### 1. 命名约定
- 使用 `Async` 后缀表示异步方法
- 使用清晰的方法名描述操作意图
- 参数名与 SQL 参数名保持一致

### 2. 错误处理
```csharp
public partial class UserService
{
    [Sqlx("SELECT * FROM users WHERE id = @id")]
    public partial Task<User?> GetUserByIdAsync(int id);  // 使用可空类型处理未找到情况
}
```

### 3. 性能优化
- 优先使用异步方法
- 对于大数据集使用 `IAsyncEnumerable<T>`
- 批量操作优于循环单条操作
- 明确指定需要的列而不是使用 `SELECT *`

### 4. SQL 质量
Sqlx 提供内置的 SQL 质量检查：
- `SQLX3002`: 避免使用 `SELECT *`
- `SQLX3003`: SQL 注入风险检测
- `SQLX9999`: 通用源生成错误

## 🔍 调试和诊断

### 1. 生成代码查看
生成的代码位于：
```
obj/Debug/net9.0/Sqlx.Generator/Sqlx.CSharpGenerator/
```

### 2. 诊断输出
```csharp
#if DEBUG
    System.Diagnostics.Debug.WriteLine("Sqlx 生成器调试信息");
#endif
```

### 3. 单元测试支持
```csharp
[TestMethod]
public void TestSqlxGeneration()
{
    var generator = new CSharpGenerator();
    var result = RunGenerator(generator, sourceCode);
    Assert.IsTrue(result.GeneratedSources.Any());
}
```

## 📈 项目状态

- ✅ **1200个单元测试全部通过**
- ✅ **核心功能稳定**
- ✅ **支持 .NET 9.0**
- ✅ **Roslyn 4.11.0 兼容**
- ✅ **生产环境就绪**

## 🎉 总结

Sqlx 源生成器提供了一个强大而灵活的数据访问解决方案，通过编译时代码生成实现：

1. **类型安全** - 编译时验证 SQL 和类型映射
2. **高性能** - 零运行时反射，最优化的执行路径  
3. **开发效率** - 自动化样板代码生成
4. **多数据库支持** - 统一的 API，多种数据库方言
5. **模式支持** - 原始 SQL、存储过程、CRUD 生成、仓储模式

无论是简单的数据访问还是复杂的企业级应用，Sqlx 都能提供合适的解决方案。

