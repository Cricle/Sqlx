# SqlTemplate 返回类型 - SQL 调试功能

## 概述

SqlTemplate 返回类型功能允许你通过简单地改变方法返回类型来获取生成的 SQL 和参数，而不执行数据库查询。这是一个强大的调试和测试工具，让你可以检查 Sqlx 生成的 SQL 语句。

## 核心概念

### 基于返回类型的行为切换

Sqlx 根据方法的返回类型决定是生成 SQL 还是执行查询：

- **返回 `SqlTemplate`**: 只生成 SQL 和参数，不执行查询
- **返回其他类型**: 正常执行数据库查询

```csharp
public interface IUserRepository
{
    // 调试模式 - 返回 SqlTemplate
    [Sqlx("SELECT * FROM users WHERE id = @id")]
    SqlTemplate GetUserByIdSql(int id);
    
    // 执行模式 - 返回实体
    [Sqlx("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetUserByIdAsync(int id);
}
```

### SqlTemplate 结构

```csharp
public readonly record struct SqlTemplate(
    string Sql,                                    // 生成的 SQL 字符串
    IReadOnlyDictionary<string, object?> Parameters // 参数字典
)
{
    // 渲染为可执行的 SQL（参数值内联）
    public ParameterizedSql Execute(IReadOnlyDictionary<string, object?>? parameters = null);
    
    // 创建参数绑定构建器
    public SqlTemplateBuilder Bind();
}
```

## 快速开始

### 1. 定义 SqlTemplate 返回方法

```csharp
[RepositoryFor<User>]
public partial interface IUserRepository
{
    // 简单查询
    [Sqlx("SELECT * FROM users WHERE age >= @minAge")]
    SqlTemplate GetAdultUsersSql(int minAge);
    
    // 带多个参数
    [Sqlx("SELECT * FROM users WHERE age >= @minAge AND city = @city")]
    SqlTemplate GetUsersByCitySql(int minAge, string city);
    
    // 异步版本
    [Sqlx("SELECT * FROM users WHERE id = @id")]
    Task<SqlTemplate> GetUserByIdSqlAsync(int id);
}
```

### 2. 使用 SqlTemplate

```csharp
var repo = new UserRepository(connection);

// 获取 SQL 模板
var template = repo.GetAdultUsersSql(18);

// 检查生成的 SQL
Console.WriteLine(template.Sql);
// 输出: SELECT * FROM users WHERE age >= @minAge

// 检查参数
Console.WriteLine(template.Parameters["@minAge"]);
// 输出: 18

// 渲染为可执行 SQL（用于日志记录）
var rendered = template.Execute().Render();
Console.WriteLine(rendered);
// 输出: SELECT * FROM users WHERE age >= 18
```

## 使用场景

### 1. SQL 调试

在开发过程中检查生成的 SQL：

```csharp
// 开发时使用 SqlTemplate 版本
var template = repo.GetComplexQuerySql(param1, param2, param3);
Console.WriteLine($"SQL: {template.Sql}");
Console.WriteLine($"Parameters: {string.Join(", ", template.Parameters)}");

// 生产环境使用执行版本
var results = await repo.GetComplexQueryAsync(param1, param2, param3);
```

### 2. 单元测试

验证 SQL 生成逻辑：

```csharp
[Test]
public void GetUsersByCity_GeneratesCorrectSql()
{
    var repo = new UserRepository(connection);
    var template = repo.GetUsersByCitySql(18, "Beijing");
    
    // 验证 SQL
    Assert.That(template.Sql, Does.Contain("WHERE age >= @minAge"));
    Assert.That(template.Sql, Does.Contain("AND city = @city"));
    
    // 验证参数
    Assert.That(template.Parameters["@minAge"], Is.EqualTo(18));
    Assert.That(template.Parameters["@city"], Is.EqualTo("Beijing"));
}
```

### 3. 日志记录

记录实际执行的 SQL（用于审计）：

```csharp
var template = repo.GetUserByIdSql(userId);
logger.LogInformation("Executing SQL: {Sql}", template.Execute().Render());

// 然后执行实际查询
var user = await repo.GetUserByIdAsync(userId);
```

### 4. SQL 性能分析

将生成的 SQL 复制到数据库工具进行性能分析：

```csharp
var template = repo.GetComplexReportSql(startDate, endDate);
File.WriteAllText("query.sql", template.Execute().Render());
// 然后在 SQL Server Management Studio 或其他工具中分析
```

## 高级功能

### 1. 复杂对象参数

SqlTemplate 自动展开复杂对象的属性：

```csharp
public class UserFilter
{
    public int MinAge { get; set; }
    public string? City { get; set; }
}

[Sqlx("SELECT * FROM users WHERE age >= @MinAge AND city = @City")]
SqlTemplate FilterUsersSql(UserFilter filter);

// 使用
var template = repo.FilterUsersSql(new UserFilter { MinAge = 18, City = "Beijing" });
// Parameters: { "@MinAge": 18, "@City": "Beijing" }
```

### 2. 批量操作

SqlTemplate 支持批量插入的 SQL 生成：

```csharp
[Sqlx("INSERT INTO users (name, age) VALUES {{batch_values}}")]
SqlTemplate BatchInsertUsersSql(List<User> users);

// 使用
var users = new List<User>
{
    new User { Name = "Alice", Age = 25 },
    new User { Name = "Bob", Age = 30 }
};

var template = repo.BatchInsertUsersSql(users);
Console.WriteLine(template.Sql);
// 输出: INSERT INTO users (name, age) VALUES (@Name_0, @Age_0), (@Name_1, @Age_1)

Console.WriteLine(string.Join(", ", template.Parameters.Keys));
// 输出: @Name_0, @Age_0, @Name_1, @Age_1
```

### 3. 多数据库方言支持

SqlTemplate 自动使用正确的数据库方言：

```csharp
// PostgreSQL - 使用 $ 参数前缀
[RepositoryFor<User>(Dialect = "PostgreSql")]
public partial class PostgreSqlUserRepository : IUserRepository { }

var pgRepo = new PostgreSqlUserRepository(pgConnection);
var template = pgRepo.GetUserByIdSql(123);
Console.WriteLine(template.Sql);
// 输出: SELECT * FROM users WHERE id = $id

// SQL Server - 使用 @ 参数前缀
[RepositoryFor<User>(Dialect = "SqlServer")]
public partial class SqlServerUserRepository : IUserRepository { }

var sqlRepo = new SqlServerUserRepository(sqlConnection);
var template2 = sqlRepo.GetUserByIdSql(123);
Console.WriteLine(template2.Sql);
// 输出: SELECT * FROM users WHERE id = @id
```

## 最佳实践

### 1. 命名约定

为 SqlTemplate 返回方法添加 `Sql` 后缀：

```csharp
// ✅ 推荐
SqlTemplate GetUserByIdSql(int id);
Task<User?> GetUserByIdAsync(int id);

// ❌ 不推荐（容易混淆）
SqlTemplate GetUserById(int id);
Task<User?> GetUserById(int id);
```

### 2. 保持方法签名一致

SqlTemplate 版本和执行版本应该有相同的参数：

```csharp
// ✅ 推荐 - 参数一致
SqlTemplate GetUsersSql(int minAge, string city);
Task<List<User>> GetUsersAsync(int minAge, string city);

// ❌ 不推荐 - 参数不一致
SqlTemplate GetUsersSql(int minAge);
Task<List<User>> GetUsersAsync(int minAge, string city, bool includeInactive);
```

### 3. 使用异步版本

对于可能在异步上下文中使用的方法，提供异步版本：

```csharp
// 同步版本（用于简单场景）
SqlTemplate GetUserByIdSql(int id);

// 异步版本（用于异步上下文）
Task<SqlTemplate> GetUserByIdSqlAsync(int id);
```

### 4. 不要在生产代码中过度使用

SqlTemplate 主要用于调试和测试，不要在生产代码中大量使用：

```csharp
// ✅ 推荐 - 在测试中使用
[Test]
public void VerifySqlGeneration()
{
    var template = repo.GetUsersSql(18);
    Assert.That(template.Sql, Does.Contain("age >= @minAge"));
}

// ❌ 不推荐 - 在生产代码中使用
public async Task<List<User>> GetUsers(int minAge)
{
    var template = repo.GetUsersSql(minAge);
    // 为什么要先生成 SqlTemplate 再执行？直接调用执行方法即可
    return await ExecuteSomehow(template);
}
```

## 性能考虑

### 零运行时开销

SqlTemplate 生成是在编译时完成的，运行时只是简单的字符串和字典操作：

```csharp
// 生成的代码（简化版）
public SqlTemplate GetUserByIdSql(int id)
{
    var sql = "SELECT * FROM users WHERE id = @id";
    var parameters = new Dictionary<string, object?>
    {
        ["@id"] = id
    };
    return new SqlTemplate(sql, parameters);
}
```

### 内存占用

SqlTemplate 使用只读字典，内存占用极小：

- SQL 字符串：共享常量
- 参数字典：只包含实际参数
- 无额外分配

### 与执行模式对比

| 特性 | SqlTemplate 模式 | 执行模式 |
|------|-----------------|---------|
| 数据库连接 | ❌ 不需要 | ✅ 需要 |
| 网络 I/O | ❌ 无 | ✅ 有 |
| 内存占用 | 🟢 极低 | 🟡 中等 |
| 执行时间 | ⚡ 微秒级 | 🐌 毫秒级 |
| 用途 | 调试、测试 | 生产查询 |

## 限制和注意事项

### 1. 不执行数据库操作

SqlTemplate 方法不会打开数据库连接或执行查询：

```csharp
// ✅ 正确理解
var template = repo.GetUserByIdSql(123);
// 此时没有任何数据库操作，只是生成了 SQL 字符串和参数

// ❌ 错误理解
var template = repo.GetUserByIdSql(123);
// 期望 template 包含查询结果 - 这是错误的！
```

### 2. 参数值是快照

SqlTemplate 捕获调用时的参数值：

```csharp
var filter = new UserFilter { MinAge = 18 };
var template = repo.FilterUsersSql(filter);

// 修改原对象不影响 template
filter.MinAge = 25;
Console.WriteLine(template.Parameters["@MinAge"]); // 仍然是 18
```

### 3. 不支持流式查询

SqlTemplate 不支持 `IAsyncEnumerable` 等流式返回类型：

```csharp
// ❌ 不支持
SqlTemplate GetUsersStreamSql();  // 对应 IAsyncEnumerable<User>

// ✅ 支持
SqlTemplate GetUsersSql();  // 对应 List<User> 或 Task<List<User>>
```

## 与其他功能的集成

### 1. 占位符系统

SqlTemplate 完全支持所有占位符：

```csharp
[Sqlx(@"
    SELECT {{columns --exclude Password}}
    FROM {{table}}
    WHERE age >= @minAge
    {{orderby created_at --desc}}
    {{limit}}
")]
SqlTemplate QueryUsersSql(int minAge, int? limit = null);

var template = repo.QueryUsersSql(18, 10);
// SQL 包含展开的列名、表名、ORDER BY 和 LIMIT 子句
```

### 2. 表达式树

SqlTemplate 支持表达式树参数：

```csharp
[Sqlx("SELECT {{columns}} FROM {{table}} {{where}}")]
SqlTemplate QuerySql([ExpressionToSql] Expression<Func<User, bool>> predicate);

var template = repo.QuerySql(u => u.Age >= 18 && u.City == "Beijing");
Console.WriteLine(template.Sql);
// 输出: SELECT * FROM users WHERE age >= 18 AND city = 'Beijing'
```

### 3. 批量操作

SqlTemplate 支持批量操作占位符：

```csharp
[Sqlx("INSERT INTO users (name, age) VALUES {{batch_values}}")]
SqlTemplate BatchInsertSql(List<User> users);

[Sqlx("UPDATE users SET age = @age WHERE id IN {{in_clause}}")]
SqlTemplate BatchUpdateSql(int age, List<int> ids);
```

## 故障排除

### 问题 1: 参数名称不匹配

**症状**: 生成的 SQL 中参数名称与预期不符

**原因**: 不同数据库使用不同的参数前缀

**解决方案**: 检查 Repository 的方言配置

```csharp
// PostgreSQL 使用 $ 前缀
[RepositoryFor<User>(Dialect = "PostgreSql")]
public partial class PgRepo : IUserRepository { }

var template = pgRepo.GetUserByIdSql(123);
// Parameters: { "$id": 123 }  // 注意是 $ 而不是 @
```

### 问题 2: 复杂对象参数未展开

**症状**: 参数字典中只有对象本身，没有属性

**原因**: 对象类型被识别为标量类型

**解决方案**: 确保对象类型不在标量类型列表中

```csharp
// ✅ 正确 - 自定义类会被展开
public class UserFilter
{
    public int MinAge { get; set; }
    public string City { get; set; }
}

// ❌ 错误 - string 是标量类型，不会展开
[Sqlx("SELECT * FROM users WHERE name = @Name")]
SqlTemplate GetUserSql(string filter);  // Parameters: { "@filter": "..." }
```

### 问题 3: 批量操作 SQL 不正确

**症状**: 批量插入的 SQL 缺少 VALUES 子句

**原因**: 缺少 `{{batch_values}}` 占位符

**解决方案**: 在 SQL 中添加占位符

```csharp
// ❌ 错误
[Sqlx("INSERT INTO users (name, age)")]
SqlTemplate BatchInsertSql(List<User> users);

// ✅ 正确
[Sqlx("INSERT INTO users (name, age) VALUES {{batch_values}}")]
SqlTemplate BatchInsertSql(List<User> users);
```

## 示例代码

完整示例请参考：
- [TodoWebApi 示例](../samples/TodoWebApi/) - 包含 SqlTemplate 返回类型演示
- [SqlTemplate 单元测试](../tests/Sqlx.Tests/SqlTemplateGeneration/)
- [集成测试示例](../tests/Sqlx.Tests/SqlTemplateGeneration/EndToEndTests.cs)

## 相关文档

- [API 参考](API_REFERENCE.md) - 完整 API 文档
- [占位符指南](PLACEHOLDERS.md) - 占位符系统详解
- [最佳实践](BEST_PRACTICES.md) - 推荐用法
- [快速开始](QUICK_START_GUIDE.md) - 入门指南
