# SqlTemplateAttribute 使用示例

## 基本用法示例

```csharp
using Sqlx.Annotations;

public partial class UserRepository
{
    // 简单查询
    [Sqlx(UseCompileTimeTemplate = true)]
    [SqlTemplate("SELECT [id], [name], [email] FROM [user] WHERE [id] = @{userId}", 
                 Dialect = SqlDialectType.SqlServer)]
    public partial Task<User?> GetUserByIdAsync(int userId);

    // 带多个参数的查询
    [SqlTemplate(@"
        SELECT [id], [name], [email], [age] 
        FROM [user] 
        WHERE [age] >= @{minAge} AND [department_id] = @{deptId}
        ORDER BY [name]",
        Dialect = SqlDialectType.SqlServer)]
    public partial Task<List<User>> GetUsersByAgeAndDepartmentAsync(int minAge, int deptId);

    // INSERT 操作
    [SqlTemplate(@"
        INSERT INTO [user] ([name], [email], [age], [department_id]) 
        VALUES (@{name}, @{email}, @{age}, @{departmentId})",
        Operation = SqlOperation.Insert)]
    public partial Task<int> CreateUserAsync(string name, string email, int age, int departmentId);

    // UPDATE 操作
    [SqlTemplate(@"
        UPDATE [user] 
        SET [email] = @{newEmail}, [age] = @{newAge} 
        WHERE [id] = @{userId}",
        Operation = SqlOperation.Update)]
    public partial Task<int> UpdateUserEmailAsync(int userId, string newEmail, int newAge);

    // DELETE 操作
    [SqlTemplate("DELETE FROM [user] WHERE [id] = @{userId}",
                 Operation = SqlOperation.Delete)]
    public partial Task<int> DeleteUserAsync(int userId);
}
```

## 数据库方言示例

```csharp
public partial class MultiDatabaseRepository
{
    // SQL Server
    [SqlTemplate("SELECT [id], [name] FROM [user] WHERE [id] = @{userId}",
                 Dialect = SqlDialectType.SqlServer)]
    public partial Task<User?> GetUserSqlServerAsync(int userId);

    // MySQL
    [SqlTemplate("SELECT `id`, `name` FROM `user` WHERE `id` = @{userId}",
                 Dialect = SqlDialectType.MySql)]
    public partial Task<User?> GetUserMySqlAsync(int userId);

    // PostgreSQL
    [SqlTemplate("SELECT \"id\", \"name\" FROM \"user\" WHERE \"id\" = @{userId}",
                 Dialect = SqlDialectType.PostgreSql)]
    public partial Task<User?> GetUserPostgreSqlAsync(int userId);
}
```

## 安全性演示

```csharp
public partial class SafetyExamples
{
    // ✅ 安全 - 使用参数占位符
    [SqlTemplate("SELECT * FROM [user] WHERE [name] = @{userName}")]
    public partial Task<User?> SafeQueryAsync(string userName);

    // ❌ 以下示例会在编译时被拒绝：
    
    // 1. 错误的占位符格式
    // [SqlTemplate("SELECT * FROM user WHERE name = @userName")]
    
    // 2. SQL 注入风险
    // [SqlTemplate("SELECT * FROM user WHERE name = '" + userName + "'")]
    
    // 3. 危险的 SQL 操作
    // [SqlTemplate("DROP TABLE user")]
    
    // 4. 多语句执行
    // [SqlTemplate("SELECT * FROM user; DROP TABLE user;")]
}
```

## 性能对比示例

```csharp
public class PerformanceComparison
{
    // 编译时模板 - 零运行时开销
    [SqlTemplate("SELECT [id], [name] FROM [user] WHERE [id] = @{userId}")]
    public partial Task<User?> GetUserCompileTimeAsync(int userId);

    // 动态 SQL - 运行时生成（适用于复杂动态查询）
    public async Task<User?> GetUserDynamicAsync(int userId, bool includeInactive = false)
    {
        var query = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Id == userId);

        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive == true);
        }

        var sql = query.ToSql();
        // 执行查询...
        return null;
    }
}
```

## 最佳实践示例

```csharp
public partial class BestPracticeExamples
{
    // ✅ 好的做法：明确指定列名
    [SqlTemplate(@"
        SELECT 
            [id], 
            [name], 
            [email], 
            [created_at]
        FROM [user] 
        WHERE [id] = @{userId}")]
    public partial Task<User?> GetUserDetailedAsync(int userId);

    // ✅ 好的做法：复杂查询使用多行格式
    [SqlTemplate(@"
        SELECT 
            u.[id],
            u.[name],
            u.[email],
            d.[name] as department_name,
            COUNT(p.[id]) as project_count
        FROM [user] u
        INNER JOIN [department] d ON u.[department_id] = d.[id]
        LEFT JOIN [user_project] up ON u.[id] = up.[user_id]
        LEFT JOIN [project] p ON up.[project_id] = p.[id]
        WHERE u.[is_active] = @{isActive}
          AND d.[budget] >= @{minBudget}
        GROUP BY u.[id], u.[name], u.[email], d.[name]
        ORDER BY project_count DESC")]
    public partial Task<List<UserProjectStats>> GetUserProjectStatsAsync(bool isActive, decimal minBudget);

    // ✅ 好的做法：使用缓存优化
    [SqlTemplate("SELECT [id], [name] FROM [department] ORDER BY [name]",
                 EnableCaching = true,
                 TemplateCacheKey = "all_departments")]
    public partial Task<List<Department>> GetAllDepartmentsAsync();
}
```

## SqlTemplate 的正确使用方式

```csharp
// 编译时模板方式（推荐用于静态查询）
[SqlTemplate("SELECT [id], [name], [email] FROM [user] WHERE [id] = @{userId}")]
public partial Task<User?> GetUserCompileTimeAsync(int userId);

// 运行时模板方式（适用于需要参数化的查询）
public async Task<User?> GetUserRuntimeAsync(int userId)
{
    var template = SqlTemplate.Parse("SELECT [id], [name], [email] FROM [user] WHERE [id] = @userId")
        .Execute(new { userId });
    // 执行查询...
    return null;
}

// 动态 SQL 方式（适用于复杂动态查询）
public async Task<List<User>> GetUsersDynamicAsync(int? minAge = null, string? namePattern = null)
{
    var query = ExpressionToSql<User>.ForSqlServer();
    
    if (minAge.HasValue)
        query = query.Where(u => u.Age >= minAge.Value);
        
    if (!string.IsNullOrEmpty(namePattern))
        query = query.Where(u => u.Name != null && u.Name.Contains(namePattern));
        
    var sql = query.ToSql();
    // 执行查询...
    return new List<User>();
}
```

## 总结

编译时 SQL 模板提供了：

1. **最高性能**: 编译时生成，零运行时开销
2. **最高安全性**: 编译时 SQL 注入检查
3. **类型安全**: 参数类型编译时验证
4. **简洁语法**: 声明式编程风格

适合固定查询逻辑的场景，与 ExpressionToSql 互补，提供完整的静态到动态 SQL 解决方案。
