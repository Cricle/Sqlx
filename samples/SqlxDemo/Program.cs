using Microsoft.Data.Sqlite;
using SqlxDemo.Extensions;
using SqlxDemo.Models;
using SqlxDemo.Services;
using Sqlx;

namespace SqlxDemo;

/// <summary>
/// Sqlx 完整功能演示
/// 演示源生成器、多数据库方言、扩展方法和 Expression to SQL
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        ShowWelcome();
        
        try
        {
            // 创建内存数据库连接
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            // 初始化数据库
            await InitializeDatabaseAsync(connection);

            // 统一演示所有功能
            await RunComprehensiveDemoAsync(connection);

            ShowSummary();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 演示过程中发生错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }
        
        Console.WriteLine("\n🎉 Sqlx 统一功能演示结束！");
        Console.WriteLine("按任意键退出...");
    }

    static void ShowWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🚀 Sqlx 完整功能演示");
        Console.WriteLine("===================");
        Console.WriteLine("演示源生成器、多数据库方言、扩展方法和高性能数据访问");
        Console.ResetColor();
        Console.WriteLine();
    }

    static async Task InitializeDatabaseAsync(SqliteConnection connection)
    {
        Console.WriteLine("📊 初始化演示数据库...");

        // 创建表
        await connection.ExecuteNonQueryAsync(@"
            CREATE TABLE [User] (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER,
                salary DECIMAL,
                department_id INTEGER,
                is_active INTEGER DEFAULT 1,
                hire_date TEXT,
                bonus DECIMAL,
                performance_rating REAL
            )");

        await connection.ExecuteNonQueryAsync(@"
            CREATE TABLE [Department] (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                budget DECIMAL,
                manager_id INTEGER
            )");

        // 插入测试数据
        await connection.ExecuteNonQueryAsync(@"
            INSERT INTO [Department] (name, budget, manager_id) VALUES 
            ('技术部', 100000, NULL),
            ('市场部', 75000, NULL),
            ('财务部', 60000, NULL)");

        await connection.ExecuteNonQueryAsync(@"
            INSERT INTO [User] (name, email, age, salary, department_id, is_active, hire_date, bonus, performance_rating) VALUES 
            ('张三', 'zhangsan@example.com', 28, 8500, 1, 1, '2023-01-15', 1000, 4.2),
            ('李四', 'lisi@example.com', 32, 12000, 1, 1, '2022-03-20', 1500, 4.5),
            ('王五', 'wangwu@example.com', 26, 7000, 2, 1, '2024-01-10', 800, 3.8),
            ('赵六', 'zhaoliu@example.com', 35, 15000, 1, 1, '2021-06-15', 2000, 4.8),
            ('钱七', 'qianqi@example.com', 29, 9500, 3, 1, '2023-08-20', NULL, 4.1)");

        Console.WriteLine("✅ 数据库初始化完成\n");
    }

    static async Task RunComprehensiveDemoAsync(SqliteConnection connection)
    {
        Console.WriteLine("🔧 Sqlx 完整功能演示");
        Console.WriteLine("====================");

        // 创建服务实例
        var userService = new TestUserService(connection);

        // 1. 基本源生成功能
        Console.WriteLine("\n1️⃣ 源生成Repository模式:");
        var users = await userService.GetActiveUsersAsync();
        Console.WriteLine($"✅ 活跃用户: {users.Count} 个");
        
        var user = await userService.GetUserByIdAsync(1);
        Console.WriteLine($"✅ 用户查询: {user?.Name}");
        
        var count = await userService.GetUserCountByDepartmentAsync(1);
        Console.WriteLine($"✅ 部门人数: {count}");

        // 2. 多数据库方言
        Console.WriteLine("\n2️⃣ 多数据库方言支持:");
        var mysqlService = new MySqlUserService(connection);
        var mysqlUsers = await mysqlService.GetActiveUsersAsync();
        Console.WriteLine($"✅ MySQL方言: {mysqlUsers.Count} 个用户 (使用 `column` 和 @param)");
        
        var sqlServerService = new SqlServerUserService(connection);
        var sqlServerUsers = await sqlServerService.GetActiveUsersAsync();
        Console.WriteLine($"✅ SQL Server方言: {sqlServerUsers.Count} 个用户 (使用 [column] 和 @param)");
        
        var postgresService = new PostgreSqlUserService(connection);
        var postgresUsers = await postgresService.GetActiveUsersAsync();
        Console.WriteLine($"✅ PostgreSQL方言: {postgresUsers.Count} 个用户 (使用 \"column\" 和 $param)");

        // 3. 扩展方法
        Console.WriteLine("\n3️⃣ 扩展方法源生成:");
        var activeCount = await connection.GetActiveUserCountAsync();
        Console.WriteLine($"✅ 扩展方法统计: {activeCount} 个活跃用户");
        
        var avgSalary = await connection.GetAverageSalaryAsync();
        Console.WriteLine($"✅ 平均薪资: {avgSalary:C}");

        // 4. Expression to SQL
        Console.WriteLine("\n4️⃣ Expression to SQL:");
        var query = ExpressionToSql<User>.Create()
            .Where(u => u.IsActive && u.Age > 25)
            .OrderBy(u => u.Salary);
        Console.WriteLine($"✅ LINQ转SQL: {query.ToSql()}");

        // 5. 性能测试
        Console.WriteLine("\n5️⃣ 性能测试:");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            await userService.GetActiveUsersAsync();
        }
        stopwatch.Stop();
        Console.WriteLine($"✅ 100次调用耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    static void ShowSummary()
    {
        Console.WriteLine("\n🎉 演示完成！");
        Console.WriteLine("================");
        Console.WriteLine("Sqlx 核心特性:");
        Console.WriteLine("• 🚀 编译时代码生成，零反射开销");
        Console.WriteLine("• 🗄️ 多数据库方言支持");
        Console.WriteLine("• 🎯 LINQ表达式转SQL");
        Console.WriteLine("• 🛡️ 类型安全的数据访问");
        Console.WriteLine("• ⚡ 高性能原生SQL执行");
    }
}
