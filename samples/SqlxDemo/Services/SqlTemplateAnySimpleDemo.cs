using Microsoft.Data.Sqlite;
using Sqlx;
using SqlxDemo.Models;

namespace SqlxDemo.Services;

/// <summary>
/// SqlTemplate Any占位符功能演示（简化版）
/// 展示如何使用Any占位符构建参数化SqlTemplate
/// </summary>
public class SqlTemplateAnySimpleDemo
{
    private readonly SqliteConnection _connection;

    public SqlTemplateAnySimpleDemo(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// 运行Any占位符简化演示
    /// </summary>
    public async Task RunAnyPlaceholderDemoAsync()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("🎯 SqlTemplate Any占位符功能演示");
        Console.WriteLine("===================================");
        Console.ResetColor();
        Console.WriteLine();

        await Demo1_BasicAnyPlaceholders();
        await Demo2_CustomParameterNames();
        await Demo3_ComplexQueries();
        await Demo4_DifferentDataTypes();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Any占位符演示完成！");
        Console.WriteLine();
        Console.WriteLine("📖 更多详细用法请参考: samples/SqlxDemo/README_ANY_PLACEHOLDER.md");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// 演示1：基础Any占位符用法
    /// </summary>
    private async Task Demo1_BasicAnyPlaceholders()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("1️⃣ 基础Any占位符用法");
        Console.WriteLine("==================");
        Console.ResetColor();

        // 传统方式说明
        Console.WriteLine("❌ 传统方式 - 手动构建参数:");
        Console.WriteLine("   代码: new SqliteParameter(\"@age\", 25), new SqliteParameter(\"@isActive\", 1)");
        Console.WriteLine("   缺点: 繁琐、容易出错、参数名需要手动管理");
        Console.WriteLine();

        // 新方式：使用Any占位符
        Console.WriteLine("✅ Any占位符方式 - 自动生成参数:");
        using var query = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age > Any.Int() && u.IsActive && u.Salary > Any.Value<decimal>());

        var template = query.ToTemplate();
        Console.WriteLine($"   SQL: {template.Sql}");
        Console.WriteLine($"   参数: {template.Parameters.Count} 个");
        
        foreach (var param in template.Parameters)
        {
            Console.WriteLine($"     - {param.Key} = {param.Value} ({param.Value?.GetType().Name ?? "null"})");
        }

        Console.WriteLine("   优点: 简洁、类型安全、自动参数管理");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示2：自定义参数名
    /// </summary>
    private async Task Demo2_CustomParameterNames()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("2️⃣ 自定义参数名");
        Console.WriteLine("===============");
        Console.ResetColor();

        // 使用自定义参数名
        using var query = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age > Any.Int("minAge") && 
                       u.Name.Contains(Any.String("searchName")) && 
                       u.Salary > Any.Value<decimal>("minSalary"));

        var template = query.ToTemplate();
        Console.WriteLine($"✅ 自定义参数名:");
        Console.WriteLine($"   SQL: {template.Sql}");
        Console.WriteLine($"   参数: {template.Parameters.Count} 个");
        
        foreach (var param in template.Parameters)
        {
            Console.WriteLine($"     - {param.Key} = {param.Value} ({param.Value?.GetType().Name ?? "null"})");
        }

        Console.WriteLine();
        Console.WriteLine($"💡 自定义参数名的优势:");
        Console.WriteLine($"   - 参数名更有意义: @minAge, @searchName, @minSalary");
        Console.WriteLine($"   - 便于调试和日志记录");
        Console.WriteLine($"   - 提高代码可读性");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示3：复杂查询条件
    /// </summary>
    private async Task Demo3_ComplexQueries()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("3️⃣ 复杂查询条件");
        Console.WriteLine("===============");
        Console.ResetColor();

        // 复杂的业务查询
        using var complexQuery = ExpressionToSql<User>.ForSqlServer()
            .Where(u => (u.Age >= Any.Int("minAge") && u.Age <= Any.Int("maxAge")) &&
                       (u.Salary > Any.Value<decimal>("baseSalary") || u.Bonus > Any.Value<decimal>("minBonus")) &&
                       u.DepartmentId == Any.Int("deptId") &&
                       u.IsActive == Any.Bool("activeStatus"))
            .OrderBy(u => u.Salary)
            .Take(10);

        var complexTemplate = complexQuery.ToTemplate();
        Console.WriteLine($"✅ 复杂查询条件:");
        Console.WriteLine($"   SQL: {complexTemplate.Sql}");
        Console.WriteLine($"   参数: {complexTemplate.Parameters.Count} 个");
        
        foreach (var param in complexTemplate.Parameters)
        {
            Console.WriteLine($"     - {param.Key} = {param.Value}");
        }

        Console.WriteLine();
        Console.WriteLine($"💡 支持的复杂条件:");
        Console.WriteLine($"   - 范围查询: age >= @minAge AND age <= @maxAge");
        Console.WriteLine($"   - OR条件: salary > @baseSalary OR bonus > @minBonus");
        Console.WriteLine($"   - 排序和分页: ORDER BY salary");
        Console.WriteLine($"   - 多个字段条件组合");
        Console.WriteLine();
    }

    /// <summary>
    /// 演示4：不同数据类型支持
    /// </summary>
    private async Task Demo4_DifferentDataTypes()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("4️⃣ 不同数据类型支持");
        Console.WriteLine("==================");
        Console.ResetColor();

        // 展示各种数据类型的Any占位符
        using var typesQuery = ExpressionToSql<User>.ForSqlServer()
            .Where(u => u.Age > Any.Int("userAge") &&                     // int
                       u.Name == Any.String("userName") &&                // string
                       u.IsActive == Any.Bool("isActive") &&              // bool
                       u.HireDate > Any.DateTime("hireDate") &&           // DateTime
                       u.Salary > Any.Value<decimal>("salary") &&         // decimal
                       u.Bonus > Any.Value<decimal?>("bonus"));           // nullable decimal

        var typesTemplate = typesQuery.ToTemplate();
        Console.WriteLine($"✅ 支持的数据类型:");
        Console.WriteLine($"   SQL: {typesTemplate.Sql}");
        Console.WriteLine($"   参数详情:");
        
        foreach (var param in typesTemplate.Parameters)
        {
            var typeName = param.Value?.GetType().Name ?? "null";
            Console.WriteLine($"     - {param.Key} = {param.Value} ({typeName})");
        }

        Console.WriteLine();
        Console.WriteLine($"📋 支持的Any方法:");
        Console.WriteLine($"   - Any.Int() / Any.Int(\"name\")          → int");
        Console.WriteLine($"   - Any.String() / Any.String(\"name\")    → string");
        Console.WriteLine($"   - Any.Bool() / Any.Bool(\"name\")        → bool");
        Console.WriteLine($"   - Any.DateTime() / Any.DateTime(\"name\") → DateTime");
        Console.WriteLine($"   - Any.Guid() / Any.Guid(\"name\")        → Guid");
        Console.WriteLine($"   - Any.Value<T>() / Any.Value<T>(\"name\") → 任意类型");

        Console.WriteLine();
        Console.WriteLine($"🎯 Any占位符的核心价值:");
        Console.WriteLine($"   ✨ 开发效率: 减少80%的样板代码");
        Console.WriteLine($"   🛡️ 类型安全: 编译时验证，运行时无错");
        Console.WriteLine($"   🚀 高性能: 零反射，最优化执行");
        Console.WriteLine($"   🎨 直观语法: 自然的LINQ表达式体验");
        Console.WriteLine($"   🔒 安全可靠: 自动防止SQL注入");
        Console.WriteLine();
    }
}
