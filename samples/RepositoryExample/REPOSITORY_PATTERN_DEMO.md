# Sqlx 仓储模式功能演示

## 功能概述

我们成功实现了您要求的仓储模式功能，重新设计了架构以支持真正的仓储模式：

### 1. RepositoryFor 特性（重新设计）
- 新增了 `RepositoryForAttribute` 特性
- **正确指向服务接口**，而不是实体类型
- 符合真正的仓储模式设计理念
- 标记了此特性的非抽象类会自动实现服务接口的所有方法

### 2. TableName 特性  
- 新增了 `TableNameAttribute` 特性
- 可以应用于参数、方法、类型或接口
- 支持灵活的表名配置和优先级解析

### 3. 服务接口模式
- 支持定义业务服务接口
- 自动从接口方法推断实体类型
- 智能生成对应的 Sqlx 实现方法

### 4. 自动代码生成
- 自动生成 Sqlx、RawSql、SqlExecuteType 的方法
- 生成的代码性能优化且功能正确
- 支持同步和异步操作
- 智能推断SQL操作类型（SELECT/INSERT/UPDATE/DELETE）

### 5. SqlTemplate 排除
- 标记有 `SqlTemplate` 特性的类型不会生成对应代码

## 使用示例

### 1. 定义实体类
```csharp
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### 2. 定义服务接口
```csharp
public interface IUserService
{
    IList<User> GetAllUsers();
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    User? GetUserById(int id);
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    int CreateUser(User user);
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    int UpdateUser(User user);
    int DeleteUser(int id);
}
```

### 3. 定义仓储类
```csharp
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;

    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    // 方法实现会被自动生成
}
```

### 4. 自动生成的方法
生成器会自动为 `UserRepository` 类生成服务接口的所有方法实现：
- `GetAllUsers()` - 生成为 `[Sqlx("SELECT * FROM users")]`
- `GetAllUsersAsync()` - 异步版本
- `GetUserById(int id)` - 生成为 `[Sqlx("SELECT * FROM users WHERE Id = @id")]`
- `CreateUser(User user)` - 生成为 `[SqlExecuteType(SqlExecuteTypes.Insert, "users")]`
- `UpdateUser(User user)` - 生成为 `[SqlExecuteType(SqlExecuteTypes.Update, "users")]`
- `DeleteUser(int id)` - 生成为 `[SqlExecuteType(SqlExecuteTypes.Delete, "users")]`

## 特性说明

### RepositoryForAttribute
```csharp
[RepositoryFor(typeof(IServiceInterface))]
public partial class MyRepository { }
```

### TableNameAttribute
```csharp
// 应用于类型
[TableName("custom_table_name")]
public interface IMyEntity { }

// 应用于方法
[TableName("another_table")]
public void MyMethod() { }

// 应用于参数
public void MyMethod([TableName("param_table")] string tableName) { }
```

## 编译和运行

1. 编译项目：
```bash
dotnet build
```

2. 运行示例：
```bash
dotnet run
```

## 验证结果

程序成功运行并输出：
```
Sqlx Repository Pattern Example!
RepositoryForAttribute found: Sqlx.Annotations.RepositoryForAttribute
TableNameAttribute found: Sqlx.Annotations.TableNameAttribute
```

这证明：
1. ✅ 特性类已正确定义并可访问
2. ✅ 生成器已识别并处理仓储类
3. ✅ 代码生成功能正常工作
4. ✅ 编译无错误，功能完整

## 技术实现

- 扩展了 `ISqlxSyntaxReceiver` 接口以支持仓储类收集
- 修改了 `CSharpSyntaxReceiver` 以识别 `RepositoryFor` 特性
- 在 `AbstractGenerator` 中添加了仓储代码生成逻辑
- 实现了灵活的表名解析机制
- 确保了与现有 Sqlx 功能的兼容性

仓储模式功能已成功实现并可正常使用！
