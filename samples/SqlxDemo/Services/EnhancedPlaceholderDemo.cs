// -----------------------------------------------------------------------
// <copyright file="EnhancedPlaceholderDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using SqlxDemo.Models;
using System.Data;

namespace SqlxDemo.Services;

/// <summary>
/// 增强占位符功能演示 - 展示22个扩展占位符的实际应用
/// </summary>
public interface IEnhancedPlaceholderDemo
{
    // 🔍 条件查询占位符
    
    /// <summary>范围查询 - BETWEEN</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}}")]
    Task<List<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

    /// <summary>模糊查询 - LIKE</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{like:name|pattern=@namePattern}}")]
    Task<List<User>> GetUsersByNamePatternAsync(string namePattern);

    /// <summary>IN查询 - 多值匹配</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{in:department_id|values=@deptIds}}")]
    Task<List<User>> GetUsersByDepartmentsAsync(List<int> deptIds);

    /// <summary>NULL检查 - IS NULL</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{isnull:bonus}}")]
    Task<List<User>> GetUsersWithoutBonusAsync();

    /// <summary>非NULL检查 - IS NOT NULL</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{notnull:performance_rating}}")]
    Task<List<User>> GetUsersWithPerformanceRatingAsync();

    // 📅 日期时间函数占位符

    /// <summary>今天创建的用户</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{today:hire_date}}")]
    Task<List<User>> GetTodayHiredUsersAsync();

    /// <summary>本周创建的用户</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{week:hire_date}}")]
    Task<List<User>> GetWeekHiredUsersAsync();

    /// <summary>本月创建的用户</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{month:hire_date}}")]
    Task<List<User>> GetMonthHiredUsersAsync();

    /// <summary>今年创建的用户</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{year:hire_date}}")]
    Task<List<User>> GetYearHiredUsersAsync();

    // 🔤 字符串函数占位符

    /// <summary>包含文本查询</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{contains:email|text=@searchText}}")]
    Task<List<User>> GetUsersByEmailContainsAsync(string searchText);

    /// <summary>以文本开始</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{startswith:name|prefix=@namePrefix}}")]
    Task<List<User>> GetUsersByNameStartsWithAsync(string namePrefix);

    /// <summary>以文本结束</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{endswith:email|suffix=@emailSuffix}}")]
    Task<List<User>> GetUsersByEmailEndsWithAsync(string emailSuffix);

    // 📊 数学和聚合函数占位符

    /// <summary>薪资四舍五入查询</summary>
    [Sqlx("SELECT {{columns:auto}}, {{round:salary|decimals=2}} as rounded_salary FROM {{table}}")]
    Task<List<User>> GetUsersWithRoundedSalaryAsync();

    /// <summary>绩效评分绝对值</summary>
    [Sqlx("SELECT {{columns:auto}}, {{abs:performance_rating}} as abs_rating FROM {{table}}")]
    Task<List<User>> GetUsersWithAbsPerformanceAsync();

    // 🔗 高级操作占位符

    /// <summary>批量插入值</summary>
    [Sqlx("INSERT INTO {{table}} ({{columns:auto|exclude=Id}}) {{batch_values:auto|size=@batchSize}}")]
    Task<int> BatchInsertUsersAsync(List<User> users, int batchSize = 100);

    /// <summary>UPSERT操作 - 插入或更新</summary>
    [Sqlx("{{upsert:auto|conflict=email|update=name,age,salary}}")]
    Task<int> UpsertUserAsync(User user);

    /// <summary>EXISTS子查询</summary>
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} u WHERE {{exists:department|table=departments|condition=d.id = u.department_id AND d.is_active = 1}}")]
    Task<List<User>> GetUsersInActiveDepartmentsAsync();

    // 📈 分析和统计占位符

    /// <summary>年龄分组统计</summary>
    [Sqlx("SELECT {{groupby:age|range=10}} as age_group, COUNT(*) as user_count FROM {{table}} GROUP BY {{groupby:age|range=10}}")]
    Task<List<dynamic>> GetUserAgeGroupStatsAsync();

    /// <summary>部门薪资统计</summary>
    [Sqlx("SELECT department_id, {{sum:salary}} as total_salary, {{avg:salary}} as avg_salary, {{max:salary}} as max_salary FROM {{table}} {{groupby:department_id}} {{having:count|min=5}}")]
    Task<List<dynamic>> GetDepartmentSalaryStatsAsync();
}

/// <summary>
/// 增强占位符演示实现
/// </summary>
[TableName("user")]
[RepositoryFor(typeof(IEnhancedPlaceholderDemo))]
public partial class EnhancedPlaceholderDemo(IDbConnection connection) : IEnhancedPlaceholderDemo
{
    /// <summary>
    /// 演示模板处理过程的监控
    /// </summary>
    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        Console.WriteLine($"🚀 [增强占位符] {operationName}");
        Console.WriteLine($"   生成的SQL: {command.CommandText}");
        Console.WriteLine($"   参数数量: {command.Parameters.Count}");
    }

    /// <summary>
    /// 演示执行结果监控
    /// </summary>
    partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks / (decimal)TimeSpan.TicksPerMillisecond;
        Console.WriteLine($"✅ [执行完成] {operationName} - {elapsedMs:F2}ms");
        if (result is List<User> users)
        {
            Console.WriteLine($"   📊 返回 {users.Count} 条用户记录");
        }
        else if (result is List<dynamic> dynamics)
        {
            Console.WriteLine($"   📊 返回 {dynamics.Count} 条统计记录");
        }
        else if (result is int affectedRows)
        {
            Console.WriteLine($"   📊 影响 {affectedRows} 行记录");
        }
    }
}

/// <summary>
/// 增强占位符演示程序
/// </summary>
public static class EnhancedPlaceholderDemoRunner
{
    /// <summary>
    /// 运行增强占位符演示
    /// </summary>
    public static async Task RunEnhancedPlaceholderDemoAsync(IDbConnection connection)
    {
        Console.WriteLine("🚀 === 增强占位符功能演示 ===");
        Console.WriteLine("展示22个扩展占位符的强大功能");
        Console.WriteLine();

        var demo = new EnhancedPlaceholderDemo(connection);

        try
        {
            // 1. 条件查询占位符演示
            Console.WriteLine("🔍 === 条件查询占位符演示 ===");
            
            Console.WriteLine("📋 1. 年龄范围查询 (BETWEEN)");
            await SafeExecuteAsync(() => demo.GetUsersByAgeRangeAsync(25, 35));
            
            Console.WriteLine("🔤 2. 姓名模糊查询 (LIKE)");
            await SafeExecuteAsync(() => demo.GetUsersByNamePatternAsync("%张%"));
            
            Console.WriteLine("📑 3. 部门多选查询 (IN)");
            await SafeExecuteAsync(() => demo.GetUsersByDepartmentsAsync(new List<int> { 1, 2, 3 }));
            
            Console.WriteLine("❌ 4. 无奖金用户 (IS NULL)");
            await SafeExecuteAsync(() => demo.GetUsersWithoutBonusAsync());
            
            Console.WriteLine("✅ 5. 有绩效评分用户 (IS NOT NULL)");
            await SafeExecuteAsync(() => demo.GetUsersWithPerformanceRatingAsync());
            
            Console.WriteLine();

            // 2. 日期时间函数演示
            Console.WriteLine("📅 === 日期时间函数占位符演示 ===");
            
            Console.WriteLine("📅 1. 今天入职的用户");
            await SafeExecuteAsync(() => demo.GetTodayHiredUsersAsync());
            
            Console.WriteLine("📅 2. 本周入职的用户");
            await SafeExecuteAsync(() => demo.GetWeekHiredUsersAsync());
            
            Console.WriteLine("📅 3. 本月入职的用户");
            await SafeExecuteAsync(() => demo.GetMonthHiredUsersAsync());
            
            Console.WriteLine("📅 4. 今年入职的用户");
            await SafeExecuteAsync(() => demo.GetYearHiredUsersAsync());
            
            Console.WriteLine();

            // 3. 字符串函数演示
            Console.WriteLine("🔤 === 字符串函数占位符演示 ===");
            
            Console.WriteLine("🔍 1. 邮箱包含文本查询");
            await SafeExecuteAsync(() => demo.GetUsersByEmailContainsAsync("gmail"));
            
            Console.WriteLine("📝 2. 姓名以指定文本开始");
            await SafeExecuteAsync(() => demo.GetUsersByNameStartsWithAsync("李"));
            
            Console.WriteLine("📧 3. 邮箱以指定后缀结束");
            await SafeExecuteAsync(() => demo.GetUsersByEmailEndsWithAsync(".com"));
            
            Console.WriteLine();

            // 4. 数学函数演示
            Console.WriteLine("📊 === 数学函数占位符演示 ===");
            
            Console.WriteLine("💰 1. 薪资四舍五入查询");
            await SafeExecuteAsync(() => demo.GetUsersWithRoundedSalaryAsync());
            
            Console.WriteLine("📈 2. 绩效评分绝对值");
            await SafeExecuteAsync(() => demo.GetUsersWithAbsPerformanceAsync());
            
            Console.WriteLine();

            // 5. 高级操作演示
            Console.WriteLine("🔗 === 高级操作占位符演示 ===");
            
            Console.WriteLine("📦 1. 批量操作演示 (需要测试数据)");
            Console.WriteLine("   ⚠️ 批量插入和UPSERT需要准备测试数据");
            
            Console.WriteLine("🔍 2. EXISTS子查询演示");
            await SafeExecuteAsync(() => demo.GetUsersInActiveDepartmentsAsync());
            
            Console.WriteLine();

            // 6. 统计分析演示
            Console.WriteLine("📈 === 分析统计占位符演示 ===");
            
            Console.WriteLine("📊 1. 年龄分组统计 (需要聚合支持)");
            Console.WriteLine("   ⚠️ 聚合查询需要代码生成器支持动态类型");
            
            Console.WriteLine("💼 2. 部门薪资统计 (需要聚合支持)");
            Console.WriteLine("   ⚠️ 聚合查询需要代码生成器支持动态类型");
            
            Console.WriteLine();

            ShowPlaceholderSummary();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 增强占位符演示错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 安全执行异步操作
    /// </summary>
    private static async Task SafeExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            Console.WriteLine($"   ✅ 操作成功完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 操作跳过: {ex.Message}");
        }
    }

    /// <summary>
    /// 展示占位符功能总结
    /// </summary>
    private static void ShowPlaceholderSummary()
    {
        Console.WriteLine("📋 === 增强占位符功能总结 ===");
        Console.WriteLine();

        var placeholderGroups = new Dictionary<string, List<string>>
        {
            ["🔍 条件查询"] = new List<string>
            {
                "{{between:field|min=@min|max=@max}} - 范围查询",
                "{{like:field|pattern=@pattern}} - 模糊查询",
                "{{in:field|values=@values}} - 多值匹配",
                "{{not_in:field|values=@values}} - 排除多值",
                "{{isnull:field}} - NULL检查",
                "{{notnull:field}} - 非NULL检查"
            },
            ["📅 日期时间"] = new List<string>
            {
                "{{today:field}} - 今天的记录",
                "{{week:field}} - 本周的记录",
                "{{month:field}} - 本月的记录", 
                "{{year:field}} - 今年的记录",
                "{{date_add:field|interval=@days}} - 日期加法",
                "{{date_diff:field1|field2=@date}} - 日期差值"
            },
            ["🔤 字符串函数"] = new List<string>
            {
                "{{contains:field|text=@text}} - 包含文本",
                "{{startswith:field|prefix=@prefix}} - 前缀匹配",
                "{{endswith:field|suffix=@suffix}} - 后缀匹配",
                "{{upper:field}} - 转大写",
                "{{lower:field}} - 转小写",
                "{{trim:field}} - 去除空格"
            },
            ["📊 数学统计"] = new List<string>
            {
                "{{round:field|decimals=2}} - 四舍五入",
                "{{abs:field}} - 绝对值",
                "{{ceiling:field}} - 向上取整",
                "{{floor:field}} - 向下取整",
                "{{sum:field}} - 求和",
                "{{avg:field}} - 平均值",
                "{{max:field}} - 最大值",
                "{{min:field}} - 最小值"
            },
            ["🔗 高级操作"] = new List<string>
            {
                "{{batch_values:auto|size=@size}} - 批量插入",
                "{{upsert:auto|conflict=@field}} - 插入或更新",
                "{{exists:table|condition=@cond}} - 存在性检查",
                "{{subquery:select|table=@table}} - 子查询",
                "{{distinct:field}} - 去重",
                "{{union:query}} - 联合查询"
            }
        };

        foreach (var group in placeholderGroups)
        {
            Console.WriteLine($"{group.Key}:");
            foreach (var placeholder in group.Value)
            {
                Console.WriteLine($"   • {placeholder}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("🎯 === 设计特点 ===");
        Console.WriteLine("• 📝 写一次 (Write Once): 同一模板支持多种数据库");
        Console.WriteLine("• 🛡️ 安全 (Safety): 全面的SQL注入防护和参数验证");
        Console.WriteLine("• ⚡ 高效 (Efficiency): 智能缓存和编译时优化");
        Console.WriteLine("• 💡 友好 (User-friendly): 清晰的错误提示和智能建议");
        Console.WriteLine("• 🔧 多库可使用 (Multi-database): 通过SqlDefine支持所有主流数据库");
        Console.WriteLine();

        Console.WriteLine("✨ === 使用建议 ===");
        Console.WriteLine("• 🎯 优先使用基础7个占位符覆盖日常需求");
        Console.WriteLine("• 📊 复杂查询可组合多个占位符使用");
        Console.WriteLine("• 🔒 始终使用参数化查询防止SQL注入");
        Console.WriteLine("• 📈 大数据集查询务必添加LIMIT限制");
        Console.WriteLine("• 🧪 新功能建议先在测试环境验证");
    }
}
