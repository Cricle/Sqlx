#nullable disable
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using SqlxDemo.Models;

namespace SqlxDemo.Services;

/// <summary>
/// SQL 占位符功能演示
/// 展示 {{columns}}, {{table}}, {{where}} 等占位符的强大功能
/// </summary>
public class SqlPlaceholderDemo
{
    private readonly SqliteConnection _connection;

    public SqlPlaceholderDemo(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// 运行完整的 SQL 占位符演示
    /// </summary>
    public async Task RunCompleteDemo()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("🎯 SQL 占位符功能完整演示");
        Console.WriteLine("============================");
        Console.ResetColor();
        Console.WriteLine();

        await SetupTestData();

        await Demo1_ColumnsPlaceholder();
        await Demo2_TablePlaceholder();
        await Demo3_CombinedPlaceholders();
        await Demo4_RepositoryForWithPlaceholders();
        await Demo5_AdvancedPlaceholders();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ SQL 占位符演示完成！");
        Console.WriteLine("💡 这个功能让 RepositoryFor 可以适应不同的仓储模式");
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

        // 创建部门表
        var createDeptSql = @"
            CREATE TABLE IF NOT EXISTS department (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                manager_id INTEGER,
                budget DECIMAL NOT NULL DEFAULT 0
            )";

        using var createDeptCommand = _connection.CreateCommand();
        createDeptCommand.CommandText = createDeptSql;
        await createDeptCommand.ExecuteNonQueryAsync();

        // 清空并重新插入测试数据
        using var deleteCommand = _connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM user; DELETE FROM department;";
        await deleteCommand.ExecuteNonQueryAsync();

        var insertUserSql = @"
            INSERT INTO user (name, email, age, is_active, department_id, salary, hire_date) VALUES
            ('张三', 'zhangsan@example.com', 28, 1, 1, 8000.00, '2020-01-15'),
            ('李四', 'lisi@example.com', 32, 1, 2, 12000.00, '2019-03-10'),
            ('王五', 'wangwu@example.com', 25, 0, 1, 6000.00, '2021-06-20'),
            ('赵六', 'zhaoliu@example.com', 35, 1, 3, 15000.00, '2018-12-05')";

        var insertDeptSql = @"
            INSERT INTO department (name, manager_id, budget) VALUES
            ('开发部', 1, 500000.00),
            ('销售部', 2, 300000.00),
            ('人事部', 4, 200000.00)";

        using var insertUserCommand = _connection.CreateCommand();
        insertUserCommand.CommandText = insertUserSql;
        await insertUserCommand.ExecuteNonQueryAsync();

        using var insertDeptCommand = _connection.CreateCommand();
        insertDeptCommand.CommandText = insertDeptSql;
        await insertDeptCommand.ExecuteNonQueryAsync();

        Console.WriteLine("✅ 测试数据准备完成");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示1：{{columns}} 占位符
    /// </summary>
    private async Task Demo1_ColumnsPlaceholder()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("1️⃣ {{columns}} 占位符演示");
        Console.WriteLine("========================");
        Console.ResetColor();

        Console.WriteLine("📝 占位符用法说明:");
        Console.WriteLine("   {{columns}}                    - 所有列");
        Console.WriteLine("   {{columns:exclude=id,email}}   - 排除指定列");
        Console.WriteLine("   {{columns:include=name,age}}    - 只包含指定列");
        Console.WriteLine();

        // 模拟不同的 SQL 模板（实际中这些会在编译时被替换）
        var sqlTemplates = new Dictionary<string, string>
        {
            ["所有列"] = "SELECT {{columns}} FROM {{table}}",
            ["排除敏感信息"] = "SELECT {{columns:exclude=email,salary}} FROM {{table}}",
            ["只要基本信息"] = "SELECT {{columns:include=name,age}} FROM {{table}}"
        };

        foreach (var template in sqlTemplates)
        {
            Console.WriteLine($"🔧 {template.Key}:");
            Console.WriteLine($"   模板: {template.Value}");
            
            // 根据不同的示例手动构建 SQL 来演示占位符的效果
            // 在实际项目中，这些替换会在编译时由源生成器自动完成
            string processedSql = template.Key switch
            {
                "所有列" => "SELECT [id], [name], [email], [age], [is_active], [department_id], [salary], [hire_date] FROM [user]",
                "排除敏感信息" => "SELECT [id], [name], [age], [is_active], [department_id], [hire_date] FROM [user]",
                "只要基本信息" => "SELECT [name], [age] FROM [user]",
                _ => "SELECT * FROM [user]"
            };

            Console.WriteLine($"   生成: {processedSql}");

            using var command = _connection.CreateCommand();
            command.CommandText = processedSql;
            using var reader = await command.ExecuteReaderAsync();
            
            var count = 0;
            while (await reader.ReadAsync() && count < 2)
            {
                var values = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    values.Add($"{reader.GetName(i)}={reader.GetValue(i)}");
                }
                Console.WriteLine($"      记录{count + 1}: {string.Join(", ", values)}");
                count++;
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 演示2：{{table}} 占位符
    /// </summary>
    private async Task Demo2_TablePlaceholder()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("2️⃣ {{table}} 占位符演示");
        Console.WriteLine("======================");
        Console.ResetColor();

        Console.WriteLine("📝 占位符用法说明:");
        Console.WriteLine("   {{table}}            - 表名");
        Console.WriteLine("   {{table:alias=u}}    - 表名 + 别名");
        Console.WriteLine();

        // 模拟不同的表别名使用场景
        var scenarios = new[]
        {
            new { 
                Description = "简单查询", 
                Template = "SELECT * FROM {{table}} WHERE is_active = 1",
                ProcessedSql = "SELECT * FROM [user] WHERE is_active = 1"
            },
            new { 
                Description = "带别名查询", 
                Template = "SELECT u.name, u.age FROM {{table:alias=u}} WHERE u.age > 30",
                ProcessedSql = "SELECT u.name, u.age FROM [user] u WHERE u.age > 30"
            }
        };

        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"🔧 {scenario.Description}:");
            Console.WriteLine($"   模板: {scenario.Template}");
            Console.WriteLine($"   生成: {scenario.ProcessedSql}");

            using var command = _connection.CreateCommand();
            command.CommandText = scenario.ProcessedSql;
            using var reader = await command.ExecuteReaderAsync();
            
            var count = 0;
            while (await reader.ReadAsync() && count < 2)
            {
                var values = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    values.Add($"{reader.GetValue(i)}");
                }
                Console.WriteLine($"      结果{count + 1}: {string.Join(", ", values)}");
                count++;
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 演示3：组合占位符
    /// </summary>
    private async Task Demo3_CombinedPlaceholders()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("3️⃣ 组合占位符演示");
        Console.WriteLine("==================");
        Console.ResetColor();

        Console.WriteLine("📝 复杂场景的占位符组合:");
        Console.WriteLine();

        var complexScenarios = new[]
        {
            new {
                Description = "统计查询",
                Template = "SELECT {{count}}, AVG(salary) FROM {{table}} WHERE {{where:default=is_active=1}}",
                ProcessedSql = "SELECT COUNT(*), AVG(salary) FROM [user] WHERE is_active=1"
            },
            new {
                Description = "JOIN 查询",
                Template = "{{select:exclude=salary}} FROM {{table:alias=u}} {{joins:type=INNER,table=department,on=u.department_id=d.id,alias=d}}",
                ProcessedSql = "SELECT [id], [name], [email], [age], [is_active], [department_id], [hire_date] FROM [user] u INNER JOIN [department] d ON u.department_id=d.id"
            },
            new {
                Description = "排序查询",
                Template = "{{select}} FROM {{table}} WHERE age > 25 {{orderby:default=salary DESC}}",
                ProcessedSql = "SELECT [id], [name], [email], [age], [is_active], [department_id], [salary], [hire_date] FROM [user] WHERE age > 25 ORDER BY salary DESC"
            }
        };

        foreach (var scenario in complexScenarios)
        {
            Console.WriteLine($"🔧 {scenario.Description}:");
            Console.WriteLine($"   模板: {scenario.Template}");
            Console.WriteLine($"   生成: {scenario.ProcessedSql}");

            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = scenario.ProcessedSql;
                using var reader = await command.ExecuteReaderAsync();
                
                var count = 0;
                while (await reader.ReadAsync() && count < 2)
                {
                    var values = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        values.Add($"{reader.GetValue(i)}");
                    }
                    Console.WriteLine($"      结果{count + 1}: {string.Join(", ", values)}");
                    count++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ⚠️ SQL 执行错误: {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 演示4：RepositoryFor 中使用占位符
    /// </summary>
    private async Task Demo4_RepositoryForWithPlaceholders()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("4️⃣ RepositoryFor 中使用占位符");
        Console.WriteLine("=============================");
        Console.ResetColor();

        Console.WriteLine("💡 传统 RepositoryFor 的问题:");
        Console.WriteLine("   - SQL 写死，不能适应不同的仓储需求");
        Console.WriteLine("   - 难以实现通用的查询模板");
        Console.WriteLine("   - 每个实体都要写重复的 SQL");
        Console.WriteLine();

        Console.WriteLine("✅ 使用占位符的 RepositoryFor:");
        Console.WriteLine();

        // 模拟传统方式
        Console.WriteLine("❌ 传统方式:");
        Console.WriteLine(@"   public interface IUserRepository
   {
       [Sqlx(""SELECT id, name, email, age, is_active, department_id, salary, hire_date FROM user WHERE is_active = 1"")]
       Task<IList<User>> GetActiveUsersAsync();
       
       [Sqlx(""SELECT id, name, email, age, is_active, department_id, salary, hire_date FROM user WHERE age > @age"")]
       Task<IList<User>> GetUsersByAgeAsync(int age);
   }");
        Console.WriteLine();

        // 模拟占位符方式
        Console.WriteLine("✅ 占位符方式:");
        Console.WriteLine(@"   public interface IUserRepository
   {
       [Sqlx(""{{select}} FROM {{table}} WHERE {{where:default=is_active=1}}"")]
       Task<IList<User>> GetActiveUsersAsync();
       
       [Sqlx(""{{select}} FROM {{table}} WHERE age > @age"")]
       Task<IList<User>> GetUsersByAgeAsync(int age);
       
       [Sqlx(""{{select:exclude=salary,email}} FROM {{table}} WHERE department_id = @deptId"")]
       Task<IList<User>> GetPublicUserInfoAsync(int deptId);
   }");
        Console.WriteLine();

        Console.WriteLine("🎯 占位符的优势:");
        Console.WriteLine("   ✨ 自动适配实体类型: {{columns}} 会根据实体自动生成列");
        Console.WriteLine("   🔧 灵活的列控制: exclude/include 参数控制返回列");
        Console.WriteLine("   🗃️ 自动表名推断: {{table}} 从实体类型推断表名");
        Console.WriteLine("   🔄 可重用模板: 一套模板适应多种实体");
        Console.WriteLine("   🛡️ 类型安全: 编译时生成，运行时无反射");
        Console.WriteLine();

        // 展示不同仓储的复用
        Console.WriteLine("🔄 模板复用示例:");
        var entities = new[] { "User", "Product", "Order" };
        foreach (var entity in entities)
        {
            Console.WriteLine($"   {entity}Repository:");
            Console.WriteLine($"     {{{{select}}}} FROM {{{{table}}}} → SELECT * FROM [{entity}]");
            Console.WriteLine($"     {{{{insert}}}} VALUES {{{{values}}}} → INSERT INTO [{entity}](...) VALUES (...)");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 演示5：高级占位符功能
    /// </summary>
    private async Task Demo5_AdvancedPlaceholders()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("5️⃣ 高级占位符功能");
        Console.WriteLine("==================");
        Console.ResetColor();

        Console.WriteLine("🚀 高级占位符用法:");
        Console.WriteLine();

        var advancedExamples = new Dictionary<string, string>
        {
            ["条件插入"] = "{{insert}} VALUES {{values}} ON CONFLICT (email) DO UPDATE SET {{update:exclude=id,email}}",
            ["分页查询"] = "{{select}} FROM {{table}} WHERE {{where}} {{orderby}} LIMIT @limit OFFSET @offset",
            ["聚合统计"] = "SELECT department_id, {{count:column=id}}, AVG(salary) FROM {{table}} GROUP BY department_id",
            ["复杂联查"] = @"{{select:exclude=salary}} FROM {{table:alias=u}} 
                           {{joins:type=LEFT,table=department,on=u.department_id=d.id,alias=d}}
                           WHERE u.is_active = 1 {{orderby:default=u.name}}"
        };

        foreach (var example in advancedExamples)
        {
            Console.WriteLine($"🔧 {example.Key}:");
            Console.WriteLine($"   模板: {example.Value}");
            
            // 模拟编译时处理结果
            var processed = example.Value
                .Replace("{{insert}}", "INSERT INTO [user] ([name], [email], [age], [is_active], [department_id], [salary], [hire_date])")
                .Replace("{{values}}", "VALUES (@name, @email, @age, @isActive, @deptId, @salary, @hireDate)")
                .Replace("{{update:exclude=id,email}}", "SET [name]=excluded.[name], [age]=excluded.[age], [is_active]=excluded.[is_active], [department_id]=excluded.[department_id], [salary]=excluded.[salary], [hire_date]=excluded.[hire_date]")
                .Replace("{{select}}", "SELECT [id], [name], [email], [age], [is_active], [department_id], [salary], [hire_date]")
                .Replace("{{select:exclude=salary}}", "SELECT [id], [name], [email], [age], [is_active], [department_id], [hire_date]")
                .Replace("{{table}}", "[user]")
                .Replace("{{table:alias=u}}", "[user] u")
                .Replace("{{where}}", "1=1")
                .Replace("{{orderby}}", "ORDER BY [id]")
                .Replace("{{orderby:default=u.name}}", "ORDER BY u.name")
                .Replace("{{count:column=id}}", "COUNT([id])")
                .Replace("{{joins:type=LEFT,table=department,on=u.department_id=d.id,alias=d}}", "LEFT JOIN [department] d ON u.department_id=d.id");

            Console.WriteLine($"   生成: {processed.Replace("\n", " ").Replace("  ", " ").Trim()}");
            Console.WriteLine();
        }

        Console.WriteLine("💡 占位符系统的核心价值:");
        Console.WriteLine("   🎯 提高开发效率: 减少重复的 SQL 编写");
        Console.WriteLine("   🔧 增强灵活性: 一套模板适应多种场景");
        Console.WriteLine("   🛡️ 保持类型安全: 编译时处理，零运行时开销");
        Console.WriteLine("   🎨 简化维护: 修改实体结构自动更新相关 SQL");
        Console.WriteLine("   🔄 促进重用: 标准化的查询模式可以跨项目复用");
        Console.WriteLine();
    }
}

/// <summary>
/// 演示用的用户仓储接口 - 展示占位符的使用
/// </summary>
public interface IFlexibleUserRepository
{
    // 基础查询 - 使用占位符自动适配
    [Sqlx("{{select}} FROM {{table}} WHERE {{where:default=is_active=1}}")]
    Task<IList<User>> GetAllActiveUsersAsync();

    // 排除敏感信息的查询
    [Sqlx("{{select:exclude=salary,email}} FROM {{table}} WHERE department_id = @deptId")]
    Task<IList<User>> GetPublicUserInfoAsync(int deptId);

    // 只获取基本信息
    [Sqlx("{{select:include=name,age,department_id}} FROM {{table}} WHERE age > @minAge {{orderby:default=age}}")]
    Task<IList<User>> GetUserBasicInfoAsync(int minAge);

    // 统计查询
    [Sqlx("SELECT department_id, {{count}}, AVG(age) FROM {{table}} WHERE {{where:default=is_active=1}} GROUP BY department_id")]
    Task<IList<object>> GetDepartmentStatsAsync();

    // 插入操作
    [Sqlx("{{insert}} VALUES {{values}}")]
    Task<int> CreateUserAsync(User user);

    // 更新操作
    [Sqlx("{{update}} {{columns:include=name,email,age}} WHERE id = @id")]
    Task<int> UpdateUserBasicInfoAsync(User user);
}

/// <summary>
/// 占位符仓储实现 - 这些方法将由源生成器自动实现
/// </summary>
[RepositoryFor(typeof(IFlexibleUserRepository))]
public partial class FlexibleUserRepository : IFlexibleUserRepository
{
    private readonly SqliteConnection _connection;

    public FlexibleUserRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    // 所有方法实现将由 Sqlx 源生成器自动生成
    // 占位符将在编译时被替换为实际的 SQL
}
