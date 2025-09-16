# Sqlx 诊断指导系统

Sqlx 源生成器现在包含了一个强大的诊断指导系统，可以在编译时分析您的代码，并提供全面的使用指导、SQL质量检查和性能优化建议。

## 📊 诊断类别

### 🔍 SQL 质量检查 (SQLX3002)

自动检测并提供SQL质量改进建议：

#### 1. SELECT * 检查
```csharp
[Sqlx("SELECT * FROM [User] WHERE [IsActive] = 1")]
public partial Task<IList<User>> GetActiveUsersAsync();
```
**警告**: `避免使用 SELECT *，明确指定需要的列可以提高性能和维护性`

**建议**: 
```csharp
[Sqlx("SELECT [Id], [Name], [Email] FROM [User] WHERE [IsActive] = 1")]
public partial Task<IList<User>> GetActiveUsersAsync();
```

#### 2. 缺少 WHERE 子句检查
```csharp
[Sqlx("DELETE FROM [User]")]
public partial Task<int> DeleteAllUsersAsync();
```
**警告**: `UPDATE/DELETE 语句缺少 WHERE 子句，可能会影响所有记录`

#### 3. SQL 注入风险检查
```csharp
[Sqlx("SELECT * FROM [User] WHERE [Name] = 'hardcoded'")]
public partial Task<User?> GetUserAsync();
```
**警告**: `检测到硬编码字符串值，可能存在SQL注入风险，建议使用参数化查询`

#### 4. 无分页大结果集检查
```csharp
[Sqlx("SELECT * FROM [User] ORDER BY [Name]")]
public partial Task<IList<User>> GetAllUsersAsync();
```
**建议**: `ORDER BY 查询建议添加 LIMIT/TOP 限制返回行数，避免大结果集性能问题`

#### 5. 复杂 JOIN 检查
```csharp
[Sqlx("SELECT * FROM [User] u JOIN [Department] d ON u.DepartmentId = d.Id JOIN [Manager] m ON d.ManagerId = m.Id JOIN [Location] l ON d.LocationId = l.Id")]
public partial Task<IList<User>> GetUsersWithDetailsAsync();
```
**建议**: `检测到 3 个 JOIN 操作，考虑优化查询或添加适当的索引`

### 🎯 使用方式指导 (SQLX3001)

帮助您遵循最佳的使用模式：

#### 1. 异步方法命名约定
```csharp
[Sqlx("SELECT * FROM [User]")]
public partial Task<IList<User>> GetUsers(); // ❌
```
**建议**: `异步方法建议以 'Async' 结尾`

**正确写法**:
```csharp
[Sqlx("SELECT * FROM [User]")]
public partial Task<IList<User>> GetUsersAsync(); // ✅
```

#### 2. 方法命名语义检查
```csharp
[Sqlx("INSERT INTO [User] (Name, Email) VALUES (@name, @email)")]
public partial Task<int> ProcessUserAsync(string name, string email); // ❌
```
**建议**: `INSERT 操作方法命名不够清晰，建议使用 Create/Add/Insert 前缀`

**正确写法**:
```csharp
[Sqlx("INSERT INTO [User] (Name, Email) VALUES (@name, @email)")]
public partial Task<int> CreateUserAsync(string name, string email); // ✅
```

#### 3. 返回类型约定检查
```csharp
[Sqlx("SELECT COUNT(*) FROM [User]")]
public partial Task GetUserCountAsync(); // ❌
```
**建议**: `SELECT 查询应该有返回值`

#### 4. 参数过多检查
```csharp
public partial Task<User> CreateUserAsync(string name, string email, int age, string phone, string address, string city, string state, string zip, string country); // ❌
```
**建议**: `方法有 9 个参数，可能过多。考虑使用对象参数或分解为多个更简单的方法`

#### 5. 未使用参数检查
```csharp
[Sqlx("SELECT * FROM [User] WHERE [Id] = @id")]
public partial Task<User?> GetUserAsync(int id, string unused); // ❌
```
**建议**: `参数 'unused' 在SQL中未使用`

### 🚀 性能优化建议 (SQLX2001)

提供具体的性能优化机会：

#### 1. 批量操作建议
```csharp
[Sqlx("INSERT INTO [User] (Name, Email) VALUES (@name, @email)")]
public partial Task<int> CreateUsersAsync(IList<User> users); // 检测到集合参数
```
**建议**: `检测到集合参数，考虑使用批量操作以提高性能`

#### 2. 分页查询建议
```csharp
[Sqlx("SELECT * FROM [User]")]
public partial Task<IList<User>> GetUsersAsync(); // 返回集合但无分页
```
**建议**: `返回集合的查询建议添加分页支持（LIMIT/OFFSET 或 TOP）`

#### 3. 大实体优化建议
```csharp
public class LargeEntity
{
    // 15+ 属性
    public int Id { get; set; }
    public string Property1 { get; set; }
    // ... 更多属性
}

[Sqlx("SELECT * FROM [LargeEntity]")]
public partial Task<IList<LargeEntity>> GetEntitiesAsync();
```
**建议**: `实体 LargeEntity 有 15+ 个属性，考虑使用投影查询只选择需要的字段`

#### 4. 字符串属性优化
```csharp
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public string Description { get; set; }
    public string Notes { get; set; } // 6+ 字符串属性
}
```
**建议**: `实体包含 6 个字符串属性，考虑字符串池优化或分离大文本字段`

#### 5. 同步方法建议
```csharp
[Sqlx("SELECT * FROM [User]")]
public partial IList<User> GetUsers(); // 同步数据库查询
```
**建议**: `数据库查询建议使用异步方法以避免阻塞线程`

### 🛡️ 最佳实践建议 (SQLX3004)

引导您遵循行业最佳实践：

#### 1. CancellationToken 支持
```csharp
[Sqlx("SELECT * FROM [User]")]
public partial Task<IList<User>> GetUsersAsync(); // 缺少 CancellationToken
```
**建议**: `异步方法建议添加 CancellationToken 参数以支持取消操作`

**正确写法**:
```csharp
[Sqlx("SELECT * FROM [User]")]
public partial Task<IList<User>> GetUsersAsync(CancellationToken cancellationToken = default); // ✅
```

#### 2. 数据库方言配置
```csharp
public partial class UserService : IUserService // 未指定方言
{
    [Sqlx("SELECT * FROM [User]")]
    public partial Task<IList<User>> GetUsersAsync();
}
```
**建议**: `未指定数据库方言，将使用默认方言。建议显式指定 [SqlDefine] 特性`

**正确写法**:
```csharp
[SqlDefine(SqlDefineTypes.SqlServer)] // ✅
public partial class UserService : IUserService
{
    [Sqlx("SELECT * FROM [User]")]
    public partial Task<IList<User>> GetUsersAsync();
}
```

#### 3. 实体主键建议
```csharp
public class User // 没有 Id 属性
{
    public string Name { get; set; }
    public string Email { get; set; }
}
```
**建议**: `实体类型缺少Id属性，建议添加主键属性`

#### 4. Record 类型使用建议
```csharp
public record User(int Id, string Name);

[Sqlx("UPDATE [User] SET [Name] = @name WHERE [Id] = @id")]
public partial Task<int> UpdateUserAsync(User user); // Record 用于修改操作
```
**建议**: `Record类型通常用于不可变数据，考虑在数据修改操作中使用普通类`

#### 5. 事务支持建议
```csharp
[Sqlx("INSERT INTO [User] (Name, Email) VALUES (@name, @email)")]
public partial Task<int> CreateUserAsync(string name, string email); // 缺少事务参数
```
**建议**: `数据修改操作建议支持事务参数，以确保数据一致性`

#### 6. 乐观并发控制
```csharp
[Sqlx("UPDATE [User] SET [Name] = @name WHERE [Id] = @id")]
public partial Task<int> UpdateUserAsync(int id, string name); // 缺少版本控制
```
**建议**: `UPDATE操作建议包含版本字段或时间戳以支持乐观并发控制`

### 🔒 安全警告 (SQLX3003)

识别潜在的安全风险：

#### 1. 无条件删除/更新
```csharp
[Sqlx("DELETE FROM [User]")]
public partial Task<int> ClearUsersAsync();
```
**警告**: `UPDATE/DELETE 语句缺少 WHERE 子句，可能会影响所有记录`

#### 2. SQL 注入风险
```csharp
[Sqlx("SELECT * FROM [User] WHERE [Name] = 'admin'")]
public partial Task<User?> GetAdminAsync();
```
**警告**: `检测到硬编码字符串值，可能存在SQL注入风险，建议使用参数化查询`

## 📈 正面反馈系统

当您的代码遵循最佳实践时，系统会给予正面反馈：

```
良好实践检测: ✅ 异步方法命名规范, ✅ 支持取消令牌, ✅ 使用参数化查询, ✅ 使用分页限制
```

## 📊 诊断摘要报告

构建完成后，您会看到总体摘要：

```
📊 Sqlx 代码生成摘要:
  • 已分析 24 个方法
  • 异步方法: 20
  • 查询方法: 18
  • 建议查看诊断信息以获取优化建议
```

## 🛠️ 如何使用

1. **自动触发**: 诊断功能在编译时自动运行，无需额外配置
2. **IDE 集成**: 诊断信息在 Visual Studio、VS Code 等IDE中显示为警告或信息
3. **CI/CD 集成**: 在构建管道中显示为构建警告，不会阻止构建

## 🎯 诊断 ID 参考

| ID | 类型 | 描述 |
|---|---|---|
| SQLX2001 | 性能建议 | 性能优化机会 |
| SQLX3001 | 使用指导 | 使用方式改进建议 |
| SQLX3002 | SQL质量 | SQL代码质量问题 |
| SQLX3003 | 安全警告 | 安全相关问题 |
| SQLX3004 | 最佳实践 | 行业最佳实践建议 |

## 💡 快速开始

新手用户会收到快速入门指导：

```
💡 Sqlx快速提示:
  • 使用 [Sqlx("SQL语句")] 标记方法
  • 异步方法建议添加 CancellationToken 参数
  • 使用 @param、$param 或 :param 进行参数化查询
  • 为大结果集添加 LIMIT/TOP 分页
  • 考虑为修改操作返回受影响行数 (int)
```

通过这个强大的诊断系统，Sqlx 不仅生成高质量的代码，还能指导您编写更好、更安全、更高性能的数据访问代码！
