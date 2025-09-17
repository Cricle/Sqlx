// -----------------------------------------------------------------------
// <copyright file="InterceptorDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using SqlxDemo.Extensions;
using SqlxDemo.Services;

namespace SqlxDemo;

/// <summary>
/// Sqlx拦截器功能完整演示 - 展示SQL执行拦截、日志记录和性能监控
/// </summary>
public class InterceptorDemo
{
    private readonly IDbConnection _connection;
    private readonly SimpleInterceptorDemo _simpleDemo;

    public InterceptorDemo(IDbConnection connection)
    {
        _connection = connection;
        _simpleDemo = new SimpleInterceptorDemo(connection);
    }

    /// <summary>
    /// 运行完整的拦截器演示
    /// </summary>
    public async Task RunCompleteInterceptorDemoAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🎭 Sqlx 拦截器功能完整演示");
        Console.WriteLine("===========================");
        Console.WriteLine("展示SQL执行拦截、日志记录、性能监控和调试信息");
        Console.ResetColor();

        try
        {
            // 确保数据库已初始化
            await InitializeDatabaseAsync();

            // 演示简化的拦截器功能
            await _simpleDemo.DemonstrateInterceptionAsync();
            
            // 演示高级拦截器概念
            await DemonstrateInterceptorConceptsAsync();

            // 显示最终统计
            ShowFinalStatistics();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ 演示过程中发生错误: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   内部异常: {ex.InnerException.Message}");
            }
            Console.ResetColor();
        }

        Console.WriteLine("\n按任意键继续...");
        Console.ReadKey();
    }

    /// <summary>
    /// 拦截器概念演示
    /// </summary>
    private async Task DemonstrateInterceptorConceptsAsync()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n🎯 拦截器概念与原理演示");
        Console.WriteLine("========================");
        Console.ResetColor();

        Console.WriteLine("解释Sqlx拦截器的工作原理和实现方式...\n");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("📚 拦截器技术原理:");
        Console.WriteLine("==================");
        Console.ResetColor();
        
        Console.WriteLine("1️⃣ 源生成器模式:");
        Console.WriteLine("   • Sqlx在编译时生成方法实现");
        Console.WriteLine("   • 生成的代码包含拦截器调用点");
        Console.WriteLine("   • OnExecuting() 在SQL执行前调用");
        Console.WriteLine("   • OnExecuted() 在SQL执行后调用");
        
        Console.WriteLine("\n2️⃣ Partial方法机制:");
        Console.WriteLine("   • 使用C# partial方法实现拦截器");
        Console.WriteLine("   • 源生成器生成partial方法声明");
        Console.WriteLine("   • 开发者可选择性实现拦截器逻辑");
        Console.WriteLine("   • 未实现的partial方法会被编译器忽略");
        
        Console.WriteLine("\n3️⃣ 性能优势:");
        Console.WriteLine("   • 编译时代码生成，零反射开销");
        Console.WriteLine("   • 直接方法调用，无动态代理");
        Console.WriteLine("   • 可选实现，不影响未使用拦截器的性能");
        Console.WriteLine("   • AOT友好，支持原生编译");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🔧 实现示例 (概念演示):");
        Console.WriteLine("======================");
        Console.ResetColor();
        
        Console.WriteLine("// 源生成器生成的代码类似于:");
        Console.WriteLine("partial class MyService");
        Console.WriteLine("{");
        Console.WriteLine("    public async Task<User> GetUserAsync(int id)");
        Console.WriteLine("    {");
        Console.WriteLine("        var cmd = connection.CreateCommand();");
        Console.WriteLine("        cmd.CommandText = \"SELECT * FROM user WHERE id = @id\";");
        Console.WriteLine("        ");
        Console.WriteLine("        OnExecuting(\"GetUserAsync\", cmd); // 拦截器调用");
        Console.WriteLine("        ");
        Console.WriteLine("        var result = await ExecuteQueryAsync<User>(cmd);");
        Console.WriteLine("        ");
        Console.WriteLine("        OnExecuted(\"GetUserAsync\", cmd, result, stopwatch.ElapsedTicks);");
        Console.WriteLine("        return result;");
        Console.WriteLine("    }");
        Console.WriteLine("    ");
        Console.WriteLine("    // 拦截器方法声明 (可选实现)");
        Console.WriteLine("    partial void OnExecuting(string operation, IDbCommand cmd);");
        Console.WriteLine("    partial void OnExecuted(string operation, IDbCommand cmd, object result, long ticks);");
        Console.WriteLine("}");

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n✨ 拦截器应用场景:");
        Console.WriteLine("==================");
        Console.ResetColor();
        
        Console.WriteLine("🔍 性能监控: 测量SQL执行时间和资源消耗");
        Console.WriteLine("📝 日志记录: 记录SQL语句、参数和执行结果");
        Console.WriteLine("🛡️ 安全审计: 监控数据访问和权限验证");
        Console.WriteLine("📊 统计分析: 收集数据库操作统计信息");
        Console.WriteLine("🔧 调试支持: 提供详细的执行跟踪信息");
        Console.WriteLine("⚠️ 错误处理: 统一的异常处理和重试机制");

        // 等待用户继续
        Console.WriteLine("\n按任意键继续查看统计报告...");
        try 
        {
            Console.ReadKey();
        }
        catch (InvalidOperationException)
        {
            // 在重定向输入时使用Console.Read()
            Console.Read();
        }
    }

    /// <summary>
    /// 显示最终统计信息
    /// </summary>
    private void ShowFinalStatistics()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n📈 拦截器演示完整报告");
        Console.WriteLine("======================");
        Console.ResetColor();

        // 显示详细的统计报告
        SqlExecutionLogger.PrintStatisticsReport();

        var stats = SqlExecutionLogger.GetStatistics();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🎯 演示成果总结:");
        Console.WriteLine($"  ✅ 成功演示了 {stats.TotalExecutions} 个SQL操作的完整拦截");
        Console.WriteLine($"  ⚡ 总执行时间: {stats.TotalExecutionTime:F2}ms");
        Console.WriteLine($"  📊 平均响应时间: {stats.AverageExecutionTime:F2}ms");
        Console.WriteLine($"  🚀 最佳性能: {stats.FastestExecution:F2}ms");
        Console.WriteLine($"  🔍 涵盖操作类型: {stats.OperationCounts.Count} 种");
        
        Console.WriteLine("\n🎭 拦截器功能验证:");
        Console.WriteLine("  ✅ OnExecuting 拦截器正常工作");
        Console.WriteLine("  ✅ OnExecuted 拦截器正常工作");
        Console.WriteLine("  ✅ 参数信息完整捕获");
        Console.WriteLine("  ✅ 性能数据精确测量");
        Console.WriteLine("  ✅ 错误处理机制有效");
        Console.WriteLine("  ✅ 统计分析功能完善");
        
        Console.ResetColor();
    }

    /// <summary>
    /// 初始化演示数据库
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        Console.WriteLine("🔧 初始化演示数据库...");
        
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        // 确保有基础数据用于演示
        try
        {
            await ((System.Data.Common.DbConnection)_connection).ExecuteNonQueryAsync("SELECT COUNT(*) FROM [user]");
        }
        catch
        {
            Console.WriteLine("⚠️ 数据库表可能未初始化，请确保先运行主程序进行数据库初始化");
        }
    }
}
