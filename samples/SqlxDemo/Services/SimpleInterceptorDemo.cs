// -----------------------------------------------------------------------
// <copyright file="SimpleInterceptorDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Diagnostics;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 简化的拦截器演示 - 使用现有的TestUserService展示自定义拦截器功能
/// </summary>
public class SimpleInterceptorDemo
{
    private readonly TestUserService _userService;
    private readonly IDbConnection _connection;

    public SimpleInterceptorDemo(IDbConnection connection)
    {
        _connection = connection;
        _userService = new TestUserService((System.Data.Common.DbConnection)connection);
    }

    /// <summary>
    /// 演示SQL执行监控和拦截
    /// </summary>
    public async Task DemonstrateInterceptionAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🎭 简化拦截器功能演示");
        Console.WriteLine("======================");
        Console.WriteLine("通过包装现有服务实现SQL执行监控");
        Console.ResetColor();

        // 清除之前的日志
        SqlExecutionLogger.ClearLogs();

        try
        {
            Console.WriteLine("\n1️⃣ 监控SELECT操作:");
            await ExecuteWithMonitoring("GetActiveUsersAsync",
                () => _userService.GetActiveUsersAsync());

            Console.WriteLine("\n2️⃣ 监控参数化查询:");
            await ExecuteWithMonitoring("GetUserByIdAsync",
                () => _userService.GetUserByIdAsync(1));

            Console.WriteLine("\n3️⃣ 监控范围查询:");
            await ExecuteWithMonitoring("GetUsersByAgeRangeAsync",
                () => _userService.GetUsersByAgeRangeAsync(25, 40));

            // 显示统计报告
            SqlExecutionLogger.PrintStatisticsReport();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ 拦截器演示过程中发生错误: {ex.Message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 带监控的执行包装器
    /// </summary>
    private async Task<T> ExecuteWithMonitoring<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();

        // 模拟拦截器 - 执行前
        var command = CreateMockCommand(operationName);
        LogExecutionStart(operationName, command);

        try
        {
            // 执行实际操作
            var result = await operation();

            stopwatch.Stop();

            // 模拟拦截器 - 执行后
            LogExecutionEnd(operationName, command, result, stopwatch.ElapsedTicks);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogExecutionEnd(operationName, command, ex, stopwatch.ElapsedTicks);
            throw;
        }
    }

    /// <summary>
    /// 创建模拟的数据库命令对象
    /// </summary>
    private IDbCommand CreateMockCommand(string operationName)
    {
        var command = _connection.CreateCommand();

        // 根据操作名称设置相应的SQL
        command.CommandText = operationName switch
        {
            "GetActiveUsersAsync" => "SELECT * FROM [user] WHERE [is_active] = 1",
            "GetUserByIdAsync" => "SELECT * FROM [user] WHERE [id] = @id",
            "GetUsersByAgeRangeAsync" => "SELECT * FROM [user] WHERE [age] BETWEEN @min_age AND @max_age",
            _ => $"-- Mock SQL for {operationName}"
        };

        return command;
    }

    /// <summary>
    /// 记录SQL执行开始
    /// </summary>
    private void LogExecutionStart(string operationName, IDbCommand command)
    {
        SqlExecutionLogger.LogExecutionStart(operationName, command);

        // 输出额外的调试信息
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"🎯 [自定义拦截器] 操作 {operationName} 即将执行");
        Console.WriteLine($"   🔍 连接状态: {_connection.State}");
        Console.WriteLine($"   🗄️  数据库: {_connection.Database ?? "N/A"}");
        Console.WriteLine($"   🏷️  命令类型: {command.CommandType}");
        Console.ResetColor();
    }

    /// <summary>
    /// 记录SQL执行完成
    /// </summary>
    private void LogExecutionEnd(string operationName, IDbCommand command, object? result, long elapsedTicks)
    {
        SqlExecutionLogger.LogExecutionEnd(operationName, command, result, elapsedTicks);

        // 计算执行时间
        var elapsedMs = (double)elapsedTicks / Stopwatch.Frequency * 1000;

        // 输出性能分析信息
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"📊 [性能分析] {operationName}");
        Console.WriteLine($"   ⚡ 执行时间: {elapsedMs:F3}ms");
        Console.WriteLine($"   🎯 性能等级: {GetPerformanceRating(elapsedMs)}");
        Console.WriteLine($"   💾 结果类型: {result?.GetType().Name ?? "null"}");

        // 分析结果
        if (result != null)
        {
            var resultInfo = AnalyzeResult(result);
            if (!string.IsNullOrEmpty(resultInfo))
            {
                Console.WriteLine($"   📈 结果详情: {resultInfo}");
            }
        }

        Console.ResetColor();
        Console.WriteLine(); // 空行分隔
    }

    private static string GetPerformanceRating(double elapsedMs)
    {
        return elapsedMs switch
        {
            < 1 => "🚀 极快 (<1ms)",
            < 10 => "⚡ 很快 (<10ms)",
            < 50 => "✅ 良好 (<50ms)",
            < 100 => "⚠️ 一般 (<100ms)",
            < 500 => "🐌 较慢 (<500ms)",
            _ => "🔥 需要优化 (>500ms)"
        };
    }

    private static string AnalyzeResult(object result)
    {
        return result switch
        {
            IEnumerable<object> enumerable => $"集合，{enumerable.Count()} 项",
            System.Collections.ICollection collection => $"集合，{collection.Count} 项",
            int intResult => $"整数值: {intResult}",
            decimal decimalResult => $"decimal值: {decimalResult}",
            string stringResult => $"字符串: \"{stringResult}\"",
            bool boolResult => $"布尔值: {boolResult}",
            User user => $"用户对象: {user.Name}",
            Exception ex => $"异常: {ex.Message}",
            _ => result.ToString() ?? "null"
        };
    }
}
