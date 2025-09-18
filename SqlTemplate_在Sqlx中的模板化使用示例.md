# 🎯 SqlTemplate 在 Sqlx 部分方法中的模板化使用

## 📋 概述

SqlTemplate 确实可以与 Sqlx 的部分方法（partial methods）结合使用，实现强大的 SQL 模板化功能。这种方式结合了编译时类型安全和运行时灵活性。

## 🚀 使用方式

### 1. 基础 SqlTemplate 与部分方法结合

```csharp
using Sqlx;
using Sqlx.Annotations;

// 服务接口定义
public interface IUserTemplateService
{
    // 使用 SqlTemplate 作为参数的方法
    Task<IList<User>> QueryUsersAsync(SqlTemplate template);
    Task<User?> GetUserByTemplateAsync(SqlTemplate template);
    Task<int> ExecuteTemplateAsync(SqlTemplate template);
}

// 服务实现
[RepositoryFor(typeof(IUserTemplateService))]
public partial class UserTemplateService : IUserTemplateService
{
    private readonly DbConnection _connection;

    public UserTemplateService(DbConnection connection)
    {
        _connection = connection;
    }

    // 生成器会自动实现这些方法，支持 SqlTemplate 参数
    public partial Task<IList<User>> QueryUsersAsync(SqlTemplate template);
    public partial Task<User?> GetUserByTemplateAsync(SqlTemplate template);
    public partial Task<int> ExecuteTemplateAsync(SqlTemplate template);
}
```

### 2. 创建和使用 SqlTemplate

```csharp
public class UserService
{
    private readonly UserTemplateService _templateService;

    public UserService(UserTemplateService templateService)
    {
        _templateService = templateService;
    }

    // 方式1：使用 ExpressionToSql 生成 SqlTemplate
    public async Task<IList<User>> GetActiveUsersByAgeAsync(int minAge)
    {
        using var query = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age > Any.Int("minAge") && u.IsActive == Any.Bool("isActive"))
            .OrderBy(u => u.Name);

        var template = query.ToTemplate();
        
        // 设置实际参数值
        var parameters = new Dictionary<string, object?>
        {
            ["@minAge"] = minAge,
            ["@isActive"] = true
        };
        
        var actualTemplate = new SqlTemplate(template.Sql, parameters);
        return await _templateService.QueryUsersAsync(actualTemplate);
    }

    // 方式2：直接创建 SqlTemplate
    public async Task<IList<User>> GetUsersByDepartmentAsync(int departmentId, bool includeInactive = false)
    {
        var sql = @"
            SELECT u.*, d.Name as DepartmentName 
            FROM Users u 
            INNER JOIN Departments d ON u.DepartmentId = d.Id 
            WHERE u.DepartmentId = @deptId
            AND (@includeInactive = 1 OR u.IsActive = 1)
            ORDER BY u.Name";

        var template = SqlTemplate.Create(sql, new {
            deptId = departmentId,
            includeInactive = includeInactive
        });

        return await _templateService.QueryUsersAsync(template);
    }

    // 方式3：复杂条件的动态 SqlTemplate 构建
    public async Task<IList<User>> SearchUsersAsync(UserSearchCriteria criteria)
    {
        var queryBuilder = ExpressionToSql<User>.ForSqlServer();

        // 根据条件动态添加 WHERE 子句
        if (criteria.MinAge.HasValue)
        {
            queryBuilder = queryBuilder.Where(u => u.Age >= Any.Int("minAge"));
        }

        if (!string.IsNullOrEmpty(criteria.NamePattern))
        {
            queryBuilder = queryBuilder.Where(u => u.Name.Contains(Any.String("namePattern")));
        }

        if (criteria.DepartmentIds?.Any() == true)
        {
            // 对于 IN 子句，可以动态生成 SQL
            var inClause = string.Join(",", criteria.DepartmentIds.Select((_, i) => $"@dept{i}"));
            var customSql = $@"
                SELECT * FROM Users 
                WHERE DepartmentId IN ({inClause})
                AND Age >= @minAge 
                ORDER BY Name";

            var parameters = new Dictionary<string, object?> { ["@minAge"] = criteria.MinAge ?? 0 };
            for (int i = 0; i < criteria.DepartmentIds.Count; i++)
            {
                parameters[$"@dept{i}"] = criteria.DepartmentIds[i];
            }

            var template = new SqlTemplate(customSql, parameters);
            return await _templateService.QueryUsersAsync(template);
        }

        // 使用动态构建的查询
        var dynamicTemplate = queryBuilder.ToTemplate();
        var actualParameters = new Dictionary<string, object?>();

        if (criteria.MinAge.HasValue)
            actualParameters["@minAge"] = criteria.MinAge.Value;
        if (!string.IsNullOrEmpty(criteria.NamePattern))
            actualParameters["@namePattern"] = criteria.NamePattern;

        var finalTemplate = new SqlTemplate(dynamicTemplate.Sql, actualParameters);
        return await _templateService.QueryUsersAsync(finalTemplate);
    }
}

// 搜索条件类
public class UserSearchCriteria
{
    public int? MinAge { get; set; }
    public string? NamePattern { get; set; }
    public List<int>? DepartmentIds { get; set; }
    public bool? IsActive { get; set; }
}
```

### 3. 高级模板化使用场景

```csharp
public class AdvancedTemplateService
{
    private readonly UserTemplateService _templateService;

    public AdvancedTemplateService(UserTemplateService templateService)
    {
        _templateService = templateService;
    }

    // 可重用的 SQL 模板
    private static readonly Dictionary<string, string> SqlTemplates = new()
    {
        ["GetActiveUsers"] = "SELECT * FROM Users WHERE IsActive = @isActive ORDER BY Name",
        ["GetUsersByAge"] = "SELECT * FROM Users WHERE Age BETWEEN @minAge AND @maxAge",
        ["GetUsersByDepartment"] = @"
            SELECT u.*, d.Name as DepartmentName 
            FROM Users u 
            INNER JOIN Departments d ON u.DepartmentId = d.Id 
            WHERE d.Id = @deptId",
        ["ComplexUserQuery"] = @"
            SELECT u.*, d.Name as DepartmentName,
                   COUNT(p.Id) as ProjectCount,
                   AVG(p.Budget) as AvgProjectBudget
            FROM Users u 
            INNER JOIN Departments d ON u.DepartmentId = d.Id 
            LEFT JOIN Projects p ON u.Id = p.ManagerId
            WHERE u.IsActive = @isActive 
            AND u.HireDate >= @hireDate
            GROUP BY u.Id, u.Name, u.Email, d.Name
            HAVING COUNT(p.Id) >= @minProjects
            ORDER BY AvgProjectBudget DESC"
    };

    // 使用预定义模板
    public async Task<IList<User>> GetActiveUsersAsync()
    {
        var template = SqlTemplate.Create(
            SqlTemplates["GetActiveUsers"], 
            new { isActive = true }
        );
        return await _templateService.QueryUsersAsync(template);
    }

    // 复杂查询模板
    public async Task<IList<User>> GetManagersWithProjectsAsync(DateTime fromDate, int minProjects)
    {
        var template = SqlTemplate.Create(
            SqlTemplates["ComplexUserQuery"],
            new { 
                isActive = true, 
                hireDate = fromDate, 
                minProjects = minProjects 
            }
        );
        return await _templateService.QueryUsersAsync(template);
    }

    // 动态 SQL 模板构建
    public async Task<IList<User>> BuildDynamicQueryAsync(
        bool includeInactive = false,
        int? minAge = null,
        int? maxAge = null,
        List<int>? departmentIds = null)
    {
        var sqlBuilder = new StringBuilder("SELECT * FROM Users WHERE 1=1");
        var parameters = new Dictionary<string, object?>();

        if (!includeInactive)
        {
            sqlBuilder.Append(" AND IsActive = @isActive");
            parameters["@isActive"] = true;
        }

        if (minAge.HasValue)
        {
            sqlBuilder.Append(" AND Age >= @minAge");
            parameters["@minAge"] = minAge.Value;
        }

        if (maxAge.HasValue)
        {
            sqlBuilder.Append(" AND Age <= @maxAge");
            parameters["@maxAge"] = maxAge.Value;
        }

        if (departmentIds?.Any() == true)
        {
            var placeholders = string.Join(",", 
                departmentIds.Select((_, i) => $"@dept{i}"));
            sqlBuilder.Append($" AND DepartmentId IN ({placeholders})");
            
            for (int i = 0; i < departmentIds.Count; i++)
            {
                parameters[$"@dept{i}"] = departmentIds[i];
            }
        }

        sqlBuilder.Append(" ORDER BY Name");

        var template = new SqlTemplate(sqlBuilder.ToString(), parameters);
        return await _templateService.QueryUsersAsync(template);
    }
}
```

### 4. 与现有 Sqlx 特性结合

```csharp
public partial class UserAdvancedService
{
    private readonly DbConnection _connection;

    public UserAdvancedService(DbConnection connection)
    {
        _connection = connection;
    }

    // 传统的 Sqlx 方法
    [Sqlx("SELECT * FROM Users WHERE Age > @age")]
    public partial Task<IList<User>> GetUsersByAgeAsync(int age);

    // SqlTemplate 方法
    public async Task<IList<User>> GetUsersByTemplateAsync(SqlTemplate template)
    {
        // 这里可以手动实现，或者通过生成器自动生成
        using var command = _connection.CreateCommand();
        command.CommandText = template.Sql;
        
        foreach (var param in template.Parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = param.Key;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }

        var results = new List<User>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            results.Add(new User
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name"),
                Email = reader.GetString("Email"),
                // ... 其他属性映射
            });
        }
        
        return results;
    }

    // 混合使用：ExpressionToSql + SqlTemplate
    public async Task<IList<User>> GetUsersWithComplexLogicAsync(int minAge, string namePattern)
    {
        // 使用 ExpressionToSql 构建基础查询
        using var baseQuery = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age >= Any.Int("minAge") && u.IsActive)
            .OrderBy(u => u.Name);

        var baseTemplate = baseQuery.ToTemplate();

        // 添加额外的复杂逻辑
        var enhancedSql = $@"
            WITH FilteredUsers AS (
                {baseTemplate.Sql}
            )
            SELECT fu.*, d.Name as DepartmentName
            FROM FilteredUsers fu
            INNER JOIN Departments d ON fu.DepartmentId = d.Id
            WHERE fu.Name LIKE @namePattern";

        var enhancedParameters = new Dictionary<string, object?>(baseTemplate.Parameters)
        {
            ["@minAge"] = minAge,
            ["@namePattern"] = $"%{namePattern}%"
        };

        var finalTemplate = new SqlTemplate(enhancedSql, enhancedParameters);
        return await GetUsersByTemplateAsync(finalTemplate);
    }
}
```

## 🎯 优势总结

### 1. 类型安全性
- **编译时验证**: SqlTemplate 的结构在编译时验证
- **参数类型安全**: 强类型的参数传递
- **SQL 语法检查**: 结合 IDE 插件可以验证 SQL 语法

### 2. 灵活性
- **动态 SQL 构建**: 根据条件动态生成 SQL
- **模板重用**: 预定义的 SQL 模板可以重复使用
- **参数化查询**: 自动防止 SQL 注入

### 3. 性能优势
- **零反射**: 编译时生成代码，运行时无反射开销
- **参数缓存**: 数据库可以缓存执行计划
- **类型优化**: 直接的类型转换，无装箱拆箱

### 4. 开发效率
- **IntelliSense 支持**: 完整的代码提示
- **调试友好**: SQL 和参数清晰可见
- **维护性**: 模板化的 SQL 易于维护和修改

## 📝 最佳实践

1. **模板组织**: 将常用的 SQL 模板集中管理
2. **参数命名**: 使用有意义的参数名称
3. **错误处理**: 妥善处理 SQL 执行异常
4. **性能监控**: 记录 SQL 执行时间和性能指标
5. **单元测试**: 为模板化查询编写完整的单元测试

这种方式将 Sqlx 的编译时优势与 SqlTemplate 的运行时灵活性完美结合，为复杂的数据访问场景提供了强大而灵活的解决方案。
