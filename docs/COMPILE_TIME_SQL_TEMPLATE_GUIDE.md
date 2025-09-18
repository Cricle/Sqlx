# 编译时 SQL 模板指南

## 概述

Sqlx 现在支持编译时 SQL 模板功能，通过 `SqlTemplateAttribute` 特性提供安全、高性能的 SQL 生成方式。与动态 SQL 不同，编译时模板在编译阶段就生成最终的 SQL 代码，提供零运行时开销的最佳性能。

## 核心概念

### 设计原则

- **编译时安全**: 所有 SQL 模板在编译时进行安全验证，防止 SQL 注入
- **零运行时开销**: SQL 在编译时生成，运行时无需解析或拼接
- **类型安全**: 参数类型在编译时验证，避免运行时类型错误
- **明确分工**: 
  - `SqlTemplateAttribute`: 编译时静态 SQL 模板
  - `ExpressionToSql`: 运行时动态 SQL 生成

## 基本用法

### 1. 简单查询示例

```csharp
public partial class UserService
{
    [Sqlx(UseCompileTimeTemplate = true)]
    [SqlTemplate("SELECT [id], [name], [email] FROM [user] WHERE [id] = @{userId}", 
                 Dialect = SqlDialectType.SqlServer)]
    public partial Task<User?> GetUserByIdAsync(int userId);
}
```

### 2. 复杂查询示例

```csharp
[Sqlx(UseCompileTimeTemplate = true)]
[SqlTemplate(@"
    SELECT 
        u.[id],
        u.[name],
        u.[email],
        d.[name] as department_name
    FROM [user] u
    INNER JOIN [department] d ON u.[department_id] = d.[id]
    WHERE u.[age] >= @{minAge} 
      AND u.[department_id] = @{departmentId}
      AND u.[is_active] = @{isActive}
    ORDER BY u.[name]", 
    Dialect = SqlDialectType.SqlServer,
    Operation = SqlOperation.Select)]
public partial Task<List<UserWithDepartment>> GetUsersByDepartmentAsync(
    int minAge, int departmentId, bool isActive);
```

### 3. INSERT/UPDATE/DELETE 操作

```csharp
// INSERT 示例
[SqlTemplate(@"
    INSERT INTO [user] ([name], [email], [age], [department_id])
    VALUES (@{name}, @{email}, @{age}, @{departmentId})",
    Operation = SqlOperation.Insert)]
public partial Task<int> CreateUserAsync(string name, string email, int age, int departmentId);

// UPDATE 示例
[SqlTemplate(@"
    UPDATE [user] 
    SET [email] = @{newEmail}, [age] = @{newAge}
    WHERE [id] = @{userId}",
    Operation = SqlOperation.Update)]
public partial Task<int> UpdateUserAsync(int userId, string newEmail, int newAge);

// DELETE 示例
[SqlTemplate("DELETE FROM [user] WHERE [id] = @{userId}",
             Operation = SqlOperation.Delete)]
public partial Task<int> DeleteUserAsync(int userId);
```

## 参数占位符

### 占位符语法

使用 `@{参数名}` 格式的占位符：

```csharp
[SqlTemplate("SELECT * FROM [user] WHERE [name] = @{userName} AND [age] > @{minAge}")]
public partial Task<List<User>> FindUsersAsync(string userName, int minAge);
```

### 支持的参数类型

- 基本类型：`int`, `long`, `string`, `bool`, `DateTime`, `decimal` 等
- 可空类型：`int?`, `DateTime?` 等
- 枚举类型
- 自定义值类型

## 安全验证

### 编译时安全检查

编译时会自动检查以下安全问题：

1. **SQL 注入风险**
   ```csharp
   // ❌ 错误 - 会被编译时拒绝
   [SqlTemplate("SELECT * FROM user WHERE name = '" + userName + "'")]
   
   // ✅ 正确 - 使用参数占位符
   [SqlTemplate("SELECT * FROM user WHERE name = @{userName}")]
   ```

2. **危险 SQL 模式**
   ```csharp
   // ❌ 错误 - 包含危险关键字
   [SqlTemplate("SELECT * FROM user; DROP TABLE user;")]
   
   // ❌ 错误 - 动态 SQL 执行
   [SqlTemplate("EXEC sp_executesql @{sql}")]
   ```

3. **参数占位符格式**
   ```csharp
   // ❌ 错误 - 错误的占位符格式
   [SqlTemplate("SELECT * FROM user WHERE id = @userId")]
   
   // ✅ 正确 - 使用大括号包围参数名
   [SqlTemplate("SELECT * FROM user WHERE id = @{userId}")]
   ```

### 安全模式配置

```csharp
[SqlTemplate("SELECT * FROM [user] WHERE [id] = @{userId}", 
             SafeMode = true,           // 启用安全模式（默认）
             ValidateParameters = true)] // 启用参数验证（默认）
public partial Task<User?> GetUserAsync(int userId);
```

## 数据库方言支持

### 支持的数据库

```csharp
// SQL Server
[SqlTemplate("SELECT [id], [name] FROM [user]", Dialect = SqlDialectType.SqlServer)]

// MySQL
[SqlTemplate("SELECT `id`, `name` FROM `user`", Dialect = SqlDialectType.MySql)]

// PostgreSQL
[SqlTemplate("SELECT \"id\", \"name\" FROM \"user\"", Dialect = SqlDialectType.PostgreSql)]

// SQLite
[SqlTemplate("SELECT [id], [name] FROM [user]", Dialect = SqlDialectType.SQLite)]

// Oracle
[SqlTemplate("SELECT \"id\", \"name\" FROM \"user\"", Dialect = SqlDialectType.Oracle)]

// DB2
[SqlTemplate("SELECT \"id\", \"name\" FROM \"user\"", Dialect = SqlDialectType.DB2)]
```

### 自动方言适配

编译时会根据指定的方言自动转换参数标记：

- SQL Server: `@parameter`
- MySQL: `@parameter`  
- PostgreSQL: `$parameter`
- SQLite: `$parameter`
- Oracle: `:parameter`
- DB2: `?`

## 性能优势

### 编译时 vs 动态 SQL 性能对比

| 特性 | 编译时模板 | 动态 SQL (ExpressionToSql) |
|------|------------|---------------------------|
| SQL 生成时机 | 编译时 | 运行时 |
| 运行时开销 | 零开销 | 表达式树解析 + SQL 生成 |
| 内存占用 | 极低 | 表达式树缓存 |
| 性能 | 最高 | 较高 |
| 灵活性 | 静态 | 动态 |

### 性能测试示例

```csharp
// 编译时模板 - 零运行时开销
[SqlTemplate("SELECT * FROM [user] WHERE [id] = @{userId}")]
public partial Task<User?> GetUserCompileTimeAsync(int userId);

// 动态 SQL - 运行时生成
public async Task<User?> GetUserDynamicAsync(int userId)
{
    var sql = ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Id == userId)
        .ToSql();
    // 执行查询...
}
```

## 最佳实践

### 1. 使用具体列名而不是 SELECT *

```csharp
// ❌ 避免
[SqlTemplate("SELECT * FROM [user] WHERE [id] = @{userId}")]

// ✅ 推荐
[SqlTemplate("SELECT [id], [name], [email] FROM [user] WHERE [id] = @{userId}")]
```

### 2. 复杂查询使用多行格式

```csharp
[SqlTemplate(@"
    SELECT 
        u.[id],
        u.[name],
        u.[email],
        d.[name] as department_name
    FROM [user] u
    INNER JOIN [department] d ON u.[department_id] = d.[id]
    WHERE u.[is_active] = @{isActive}
    ORDER BY u.[name]")]
```

### 3. 合理使用缓存

```csharp
[SqlTemplate("SELECT * FROM [user] WHERE [id] = @{userId}",
             EnableCaching = true,        // 启用缓存（默认）
             TemplateCacheKey = "user_by_id")] // 自定义缓存键
```

### 4. 明确指定操作类型

```csharp
[SqlTemplate("INSERT INTO [user] ([name]) VALUES (@{name})",
             Operation = SqlOperation.Insert)] // 有助于代码生成优化
```

## 与 ExpressionToSql 的对比

### 何时使用编译时模板

✅ **适合编译时模板的场景：**
- 固定的查询逻辑
- 对性能要求极高
- SQL 相对简单或中等复杂度
- 需要最大安全性

### 何时使用 ExpressionToSql

✅ **适合动态 SQL 的场景：**
- 需要根据条件动态构建查询
- 复杂的查询逻辑组合
- 需要运行时灵活性
- 查询条件经常变化

### 混合使用示例

```csharp
public partial class UserService
{
    // 编译时模板 - 固定查询
    [SqlTemplate("SELECT [id], [name] FROM [user] WHERE [id] = @{userId}")]
    public partial Task<User?> GetUserByIdAsync(int userId);
    
    // 动态 SQL - 灵活查询
    public async Task<List<User>> SearchUsersAsync(UserSearchCriteria criteria)
    {
        var query = ExpressionToSql<User>.ForSqlServer();
        
        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(u => u.Name.Contains(criteria.Name));
            
        if (criteria.MinAge.HasValue)
            query = query.Where(u => u.Age >= criteria.MinAge.Value);
            
        if (criteria.DepartmentIds?.Any() == true)
            query = query.Where(u => criteria.DepartmentIds.Contains(u.DepartmentId));
            
        return await ExecuteQueryAsync<User>(query.ToSql());
    }
}
```

## 错误处理

### 常见编译错误

1. **无效的占位符格式**
   ```
   错误: 请使用 @{参数名} 格式的参数占位符
   解决: 将 @userName 改为 @{userName}
   ```

2. **检测到 SQL 注入风险**
   ```
   错误: 不安全的 SQL 模板
   解决: 使用参数占位符而不是字符串拼接
   ```

3. **不支持的 SQL 操作**
   ```
   错误: SQL 模板必须以 SELECT、INSERT、UPDATE 或 DELETE 开头
   解决: 确保 SQL 以合法的操作开头
   ```

### 调试技巧

1. 使用编译时验证：
   ```csharp
   var validation = SqlTemplateSafetyValidator.ValidateSqlTemplate(sqlTemplate);
   if (!validation.IsValid)
   {
       Console.WriteLine($"SQL 验证失败: {validation.ErrorMessage}");
   }
   ```

2. 检查生成的代码：
   ```csharp
   var parseResult = CompileTimeSqlTemplate.ParseTemplate(sqlTemplate);
   var generatedCode = CompileTimeSqlTemplate.GenerateCompileTimeCode(
       parseResult, SqlDialectType.SqlServer);
   Console.WriteLine(generatedCode);
   ```

## 总结

编译时 SQL 模板为 Sqlx 带来了新的性能和安全性维度：

- **安全**: 编译时验证，防止 SQL 注入
- **性能**: 零运行时开销，最佳执行效率  
- **简洁**: 声明式语法，代码简洁明了
- **类型安全**: 编译时参数类型检查

结合 ExpressionToSql 的动态能力，Sqlx 现在提供了从静态到动态的完整 SQL 解决方案，开发者可以根据具体需求选择最合适的方式。
