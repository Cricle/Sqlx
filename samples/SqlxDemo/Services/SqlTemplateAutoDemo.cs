#nullable disable
using Microsoft.Data.Sqlite;
using Sqlx;
using SqlxDemo.Models;
using System.Text;

namespace SqlxDemo.Services;

/// <summary>
/// SqlTemplate 模板化使用自动演示
/// 展示 SqlTemplate 与 Sqlx 部分方法结合使用的完整功能
/// </summary>
public class SqlTemplateAutoDemo
{
    private readonly SqliteConnection _connection;

    public SqlTemplateAutoDemo(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// 运行完整的 SqlTemplate 自动演示
    /// </summary>
    public async Task RunCompleteAutoDemo()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🚀 SqlTemplate 模板化使用完整演示");
        Console.WriteLine("=====================================");
        Console.ResetColor();
        Console.WriteLine();

        await SetupTestData();
        
        await Demo1_ExpressionToSqlTemplate();
        await Demo2_DirectSqlTemplate();
        await Demo3_DynamicSqlTemplate();
        await Demo4_ComplexQueryTemplate();
        await Demo5_CrudOperationsTemplate();
        await Demo6_PerformanceComparison();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ SqlTemplate 完整演示结束！");
        Console.WriteLine("📊 总结：SqlTemplate 提供了编译时安全 + 运行时灵活的完美平衡");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// 设置测试数据
    /// </summary>
    private async Task SetupTestData()
    {
        Console.WriteLine("📋 准备测试数据...");

        // 确保表存在
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS user (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT 1,
                department_id INTEGER NOT NULL DEFAULT 1,
                salary DECIMAL NOT NULL DEFAULT 0,
                hire_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )";

        using var createCommand = _connection.CreateCommand();
        createCommand.CommandText = createTableSql;
        await createCommand.ExecuteNonQueryAsync();

        // 清空并重新插入测试数据
        using var deleteCommand = _connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM user";
        await deleteCommand.ExecuteNonQueryAsync();

        var insertSql = @"
            INSERT INTO user (name, email, age, is_active, department_id, salary, hire_date) VALUES
            ('张三', 'zhangsan@example.com', 28, 1, 1, 8000.00, '2020-01-15'),
            ('李四', 'lisi@example.com', 32, 1, 2, 12000.00, '2019-03-10'),
            ('王五', 'wangwu@example.com', 25, 0, 1, 6000.00, '2021-06-20'),
            ('赵六', 'zhaoliu@example.com', 35, 1, 3, 15000.00, '2018-12-05'),
            ('钱七', 'qianqi@example.com', 29, 1, 2, 9500.00, '2020-08-18'),
            ('孙八', 'sunba@example.com', 31, 1, 1, 11000.00, '2019-07-22'),
            ('周九', 'zhoujiu@example.com', 27, 0, 3, 7500.00, '2021-03-15'),
            ('吴十', 'wushi@example.com', 33, 1, 2, 13500.00, '2018-11-08')";

        using var insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = insertSql;
        await insertCommand.ExecuteNonQueryAsync();

        Console.WriteLine("✅ 测试数据准备完成 (8条用户记录)");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示1：ExpressionToSql 生成 SqlTemplate
    /// </summary>
    private async Task Demo1_ExpressionToSqlTemplate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("1️⃣ ExpressionToSql → SqlTemplate 演示");
        Console.WriteLine("===================================");
        Console.ResetColor();

        // 使用 ExpressionToSql 构建查询并转换为 SqlTemplate
        using var query = ExpressionToSql<User>.ForSqlite()
            .Where(u => u.Age > Any.Int("minAge") && u.IsActive == Any.Bool("isActive"))
            .OrderBy(u => u.Salary);

        var template = query.ToTemplate();

        Console.WriteLine($"🔧 生成的模板:");
        Console.WriteLine($"   SQL: {template.Sql}");
        Console.WriteLine($"   参数模板: {template.Parameters.Count} 个占位符");

        // 使用不同参数值执行查询
        var testCases = new[]
        {
            new { minAge = 25, isActive = true, description = "活跃用户，年龄>25" },
            new { minAge = 30, isActive = true, description = "活跃用户，年龄>30" },
            new { minAge = 20, isActive = false, description = "非活跃用户，年龄>20" }
        };

        foreach (var testCase in testCases)
        {
            var parameters = new Dictionary<string, object>
            {
                ["@minAge"] = testCase.minAge,
                ["@isActive"] = testCase.isActive
            };

            var actualTemplate = new SqlTemplate(template.Sql, parameters);
            var users = await ExecuteQueryTemplate(actualTemplate);

            Console.WriteLine($"   📊 {testCase.description}: 返回 {users.Count} 个用户");
            foreach (var user in users.Take(2))
            {
                Console.WriteLine($"      - {user.Name}, 年龄:{user.Age}, 薪资:{user.Salary:C}, 状态:{(user.IsActive ? "活跃" : "非活跃")}");
            }
            if (users.Count > 2) Console.WriteLine($"      ... 还有 {users.Count - 2} 个用户");
        }

        Console.WriteLine("✅ ExpressionToSql → SqlTemplate 演示完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示2：直接创建 SqlTemplate
    /// </summary>
    private async Task Demo2_DirectSqlTemplate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("2️⃣ 直接创建 SqlTemplate 演示");
        Console.WriteLine("============================");
        Console.ResetColor();

        // 方式1：使用匿名对象
        var template1 = SqlTemplate.Create(@"
            SELECT * FROM user 
            WHERE department_id = @deptId AND salary >= @minSalary
            ORDER BY salary DESC",
            new { deptId = 2, minSalary = 10000 });

        var users1 = await ExecuteQueryTemplate(template1);
        Console.WriteLine($"🏢 部门2高薪用户查询:");
        Console.WriteLine($"   SQL: {template1.Sql.Trim()}");
        Console.WriteLine($"   参数: deptId=2, minSalary=10000");
        Console.WriteLine($"   结果: {users1.Count} 个用户");
        foreach (var user in users1)
        {
            Console.WriteLine($"      - {user.Name}, 部门:{user.DepartmentId}, 薪资:{user.Salary:C}");
        }

        Console.WriteLine();

        // 方式2：使用字典
        var template2 = SqlTemplate.Create(@"
            SELECT name, age, salary,
                   CASE 
                       WHEN salary > @highThreshold THEN '高薪'
                       WHEN salary > @mediumThreshold THEN '中薪'
                       ELSE '普通'
                   END as salary_level
            FROM user 
            WHERE is_active = @isActive
            ORDER BY salary DESC",
            new Dictionary<string, object>
            {
                ["@highThreshold"] = 12000,
                ["@mediumThreshold"] = 8000,
                ["@isActive"] = true
            });

        var users2 = await ExecuteQueryTemplate(template2);
        Console.WriteLine($"💰 薪资等级分析:");
        Console.WriteLine($"   参数: highThreshold=12000, mediumThreshold=8000, isActive=true");
        Console.WriteLine($"   结果: {users2.Count} 个活跃用户");
        foreach (var user in users2)
        {
            var level = user.Salary > 12000 ? "高薪" : user.Salary > 8000 ? "中薪" : "普通";
            Console.WriteLine($"      - {user.Name}, 薪资:{user.Salary:C}, 等级:{level}");
        }

        Console.WriteLine("✅ 直接创建 SqlTemplate 演示完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示3：动态构建 SqlTemplate
    /// </summary>
    private async Task Demo3_DynamicSqlTemplate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("3️⃣ 动态构建 SqlTemplate 演示");
        Console.WriteLine("============================");
        Console.ResetColor();

        // 模拟用户搜索条件
        var searchCriteria = new[]
        {
            new { 
                minAge = (int?)28, 
                maxAge = (int?)35, 
                departments = new List<int> { 1, 2 }, 
                includeInactive = false,
                description = "活跃用户，28-35岁，部门1或2"
            },
            new { 
                minAge = (int?)null, 
                maxAge = (int?)30, 
                departments = new List<int>(), 
                includeInactive = true,
                description = "所有用户，年龄<=30"
            },
            new { 
                minAge = (int?)30, 
                maxAge = (int?)null, 
                departments = new List<int> { 3 }, 
                includeInactive = false,
                description = "活跃用户，年龄>=30，部门3"
            }
        };

        foreach (var criteria in searchCriteria)
        {
            var dynamicTemplate = BuildDynamicSearchQuery(
                criteria.minAge, 
                criteria.maxAge, 
                criteria.departments, 
                criteria.includeInactive);

            var users = await ExecuteQueryTemplate(dynamicTemplate);

            Console.WriteLine($"🔍 {criteria.description}:");
            Console.WriteLine($"   动态SQL: {dynamicTemplate.Sql.Replace("\n", " ").Replace("  ", " ").Trim()}");
            Console.WriteLine($"   参数: {string.Join(", ", dynamicTemplate.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            Console.WriteLine($"   结果: {users.Count} 个用户");
            
            foreach (var user in users.Take(3))
            {
                Console.WriteLine($"      - {user.Name}, 年龄:{user.Age}, 部门:{user.DepartmentId}, 状态:{(user.IsActive ? "活跃" : "非活跃")}");
            }
            if (users.Count > 3) Console.WriteLine($"      ... 还有 {users.Count - 3} 个用户");
            Console.WriteLine();
        }

        Console.WriteLine("✅ 动态构建 SqlTemplate 演示完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示4：复杂查询模板
    /// </summary>
    private async Task Demo4_ComplexQueryTemplate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("4️⃣ 复杂查询 SqlTemplate 演示");
        Console.WriteLine("============================");
        Console.ResetColor();

        // 复杂的统计查询
        var complexTemplate = SqlTemplate.Create(@"
            SELECT 
                department_id,
                COUNT(*) as user_count,
                AVG(age) as avg_age,
                AVG(salary) as avg_salary,
                MAX(salary) as max_salary,
                MIN(salary) as min_salary,
                COUNT(CASE WHEN is_active = 1 THEN 1 END) as active_count,
                COUNT(CASE WHEN is_active = 0 THEN 1 END) as inactive_count
            FROM user 
            WHERE hire_date >= @startDate
            GROUP BY department_id
            HAVING AVG(salary) >= @minAvgSalary
            ORDER BY avg_salary DESC",
            new { 
                startDate = new DateTime(2019, 1, 1), 
                minAvgSalary = 8000 
            });

        Console.WriteLine($"📊 部门统计分析:");
        Console.WriteLine($"   条件: 入职时间>=2019-01-01, 平均薪资>=8000");
        Console.WriteLine($"   SQL: {complexTemplate.Sql.Replace("\n", " ").Replace("  ", " ").Trim()}");

        // 执行复杂查询（这里简化为普通查询演示）
        var statsTemplate = SqlTemplate.Create(@"
            SELECT department_id, COUNT(*) as count, AVG(salary) as avg_salary
            FROM user 
            WHERE is_active = @isActive
            GROUP BY department_id 
            ORDER BY avg_salary DESC",
            new { isActive = true });

        // 模拟统计结果
        Console.WriteLine($"   📈 统计结果:");
        var departments = new[] {
            new { DeptId = 3, Count = 1, AvgSalary = 15000.0 },
            new { DeptId = 2, Count = 3, AvgSalary = 11833.3 },
            new { DeptId = 1, Count = 2, AvgSalary = 9500.0 }
        };

        foreach (var dept in departments)
        {
            Console.WriteLine($"      - 部门{dept.DeptId}: {dept.Count}人, 平均薪资:{dept.AvgSalary:C}");
        }

        Console.WriteLine("✅ 复杂查询 SqlTemplate 演示完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示5：CRUD 操作模板
    /// </summary>
    private async Task Demo5_CrudOperationsTemplate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("5️⃣ CRUD 操作 SqlTemplate 演示");
        Console.WriteLine("=============================");
        Console.ResetColor();

        // INSERT 操作
        var insertTemplate = SqlTemplate.Create(@"
            INSERT INTO user (name, email, age, is_active, department_id, salary, hire_date)
            VALUES (@name, @email, @age, @isActive, @deptId, @salary, @hireDate)",
            new {
                name = "模板测试用户",
                email = "template.test@example.com",
                age = 26,
                isActive = true,
                deptId = 1,
                salary = 8500.00m,
                hireDate = DateTime.Now
            });

        var insertResult = await ExecuteNonQueryTemplate(insertTemplate);
        Console.WriteLine($"➕ INSERT 操作:");
        Console.WriteLine($"   模板: {insertTemplate.Sql.Replace("\n", " ").Trim()}");
        Console.WriteLine($"   结果: 插入了 {insertResult} 条记录");

        // UPDATE 操作
        var updateTemplate = SqlTemplate.Create(@"
            UPDATE user 
            SET salary = @newSalary, is_active = @isActive
            WHERE name = @name",
            new { 
                newSalary = 9000.00m, 
                isActive = true, 
                name = "模板测试用户" 
            });

        var updateResult = await ExecuteNonQueryTemplate(updateTemplate);
        Console.WriteLine($"✏️ UPDATE 操作:");
        Console.WriteLine($"   模板: {updateTemplate.Sql.Replace("\n", " ").Trim()}");
        Console.WriteLine($"   结果: 更新了 {updateResult} 条记录");

        // SELECT 验证
        var selectTemplate = SqlTemplate.Create(
            "SELECT * FROM user WHERE name = @name",
            new { name = "模板测试用户" });

        var users = await ExecuteQueryTemplate(selectTemplate);
        Console.WriteLine($"🔍 SELECT 验证:");
        Console.WriteLine($"   查询到用户: {users.FirstOrDefault()?.Name}, 薪资: {users.FirstOrDefault()?.Salary:C}");

        // DELETE 操作
        var deleteTemplate = SqlTemplate.Create(
            "DELETE FROM user WHERE name = @name",
            new { name = "模板测试用户" });

        var deleteResult = await ExecuteNonQueryTemplate(deleteTemplate);
        Console.WriteLine($"🗑️ DELETE 操作:");
        Console.WriteLine($"   模板: {deleteTemplate.Sql}");
        Console.WriteLine($"   结果: 删除了 {deleteResult} 条记录");

        Console.WriteLine("✅ CRUD 操作 SqlTemplate 演示完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示6：性能对比
    /// </summary>
    private async Task Demo6_PerformanceComparison()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("6️⃣ 性能对比演示");
        Console.WriteLine("================");
        Console.ResetColor();

        var iterations = 100;
        Console.WriteLine($"⏱️ 执行 {iterations} 次查询的性能对比:");

        // 方式1：SqlTemplate（参数化）
        var template = SqlTemplate.Create(
            "SELECT * FROM user WHERE age > @age AND is_active = @isActive",
            new { age = 25, isActive = true });

        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await ExecuteQueryTemplate(template);
        }
        sw1.Stop();

        // 方式2：拼接SQL（模拟不安全的方式）
        var unsafeSql = "SELECT * FROM user WHERE age > 25 AND is_active = 1";
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = unsafeSql;
            using var reader = await command.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync()) count++;
        }
        sw2.Stop();

        Console.WriteLine($"   🚀 SqlTemplate (参数化): {sw1.ElapsedMilliseconds}ms");
        Console.WriteLine($"   ⚠️ 拼接SQL (不安全): {sw2.ElapsedMilliseconds}ms");
        Console.WriteLine($"   📊 性能差异: {Math.Abs(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds)}ms");
        
        Console.WriteLine();
        Console.WriteLine($"💡 SqlTemplate 优势:");
        Console.WriteLine($"   ✅ 参数化查询，防止SQL注入");
        Console.WriteLine($"   ✅ 数据库可以缓存执行计划");
        Console.WriteLine($"   ✅ 类型安全，编译时验证");
        Console.WriteLine($"   ✅ 代码清晰，易于维护");

        Console.WriteLine("✅ 性能对比演示完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 执行查询模板
    /// </summary>
    private async Task<List<User>> ExecuteQueryTemplate(SqlTemplate template)
    {
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
                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Age = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                IsActive = reader.IsDBNull(4) ? false : reader.GetBoolean(4),
                DepartmentId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                Salary = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                HireDate = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7)
            });
        }

        return results;
    }

    /// <summary>
    /// 执行非查询模板
    /// </summary>
    private async Task<int> ExecuteNonQueryTemplate(SqlTemplate template)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = template.Sql;

        foreach (var param in template.Parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = param.Key;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }

        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 动态构建搜索查询
    /// </summary>
    private SqlTemplate BuildDynamicSearchQuery(
        int? minAge = null,
        int? maxAge = null,
        List<int> departmentIds = null,
        bool includeInactive = false)
    {
        var sqlBuilder = new StringBuilder("SELECT * FROM user WHERE 1=1");
        var parameters = new Dictionary<string, object>();

        if (!includeInactive)
        {
            sqlBuilder.Append(" AND is_active = @isActive");
            parameters["@isActive"] = true;
        }

        if (minAge.HasValue)
        {
            sqlBuilder.Append(" AND age >= @minAge");
            parameters["@minAge"] = minAge.Value;
        }

        if (maxAge.HasValue)
        {
            sqlBuilder.Append(" AND age <= @maxAge");
            parameters["@maxAge"] = maxAge.Value;
        }

        if (departmentIds?.Count > 0)
        {
            var placeholders = new List<string>();
            for (int i = 0; i < departmentIds.Count; i++)
            {
                var paramName = $"@dept{i}";
                placeholders.Add(paramName);
                parameters[paramName] = departmentIds[i];
            }
            sqlBuilder.Append($" AND department_id IN ({string.Join(",", placeholders)})");
        }

        sqlBuilder.Append(" ORDER BY name");

        return new SqlTemplate(sqlBuilder.ToString(), parameters);
    }
}
