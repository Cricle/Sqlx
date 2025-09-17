// -----------------------------------------------------------------------
// <copyright file="SqlExecutionLogger.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Diagnostics;
using System.Text;

namespace SqlxDemo.Services;

/// <summary>
/// SQL执行日志记录器 - 用于拦截器功能演示
/// </summary>
public static class SqlExecutionLogger
{
    private static readonly List<SqlExecutionLog> _executionLogs = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 记录SQL执行开始
    /// </summary>
    public static void LogExecutionStart(string operationName, IDbCommand command)
    {
        lock (_lock)
        {
            var log = new SqlExecutionLog
            {
                Id = Guid.NewGuid(),
                OperationName = operationName,
                SqlCommand = command.CommandText,
                Parameters = ExtractParameters(command),
                StartTime = DateTime.Now,
                StartTicks = Stopwatch.GetTimestamp()
            };
            
            _executionLogs.Add(log);
            
            // 输出开始执行信息
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"🚀 [SQL执行开始] {operationName}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"   📝 SQL: {FormatSql(command.CommandText)}");
            
            if (log.Parameters.Any())
            {
                Console.WriteLine($"   🔧 参数: {string.Join(", ", log.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            }
            
            Console.WriteLine($"   ⏰ 开始时间: {log.StartTime:HH:mm:ss.fff}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 记录SQL执行完成
    /// </summary>
    public static void LogExecutionEnd(string operationName, IDbCommand command, object? result, long elapsedTicks)
    {
        lock (_lock)
        {
            var log = _executionLogs.LastOrDefault(l => l.OperationName == operationName && l.SqlCommand == command.CommandText);
            if (log != null)
            {
                log.EndTime = DateTime.Now;
                log.EndTicks = Stopwatch.GetTimestamp();
                log.ElapsedTicks = elapsedTicks;
                log.ElapsedMilliseconds = (double)elapsedTicks / Stopwatch.Frequency * 1000;
                log.Result = result;
                log.IsCompleted = true;
            }
            
            // 输出执行完成信息
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ [SQL执行完成] {operationName}");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            
            if (log != null)
            {
                Console.WriteLine($"   ⚡ 执行时间: {log.ElapsedMilliseconds:F2}ms");
                Console.WriteLine($"   📊 结果类型: {result?.GetType().Name ?? "void"}");
                
                // 分析结果
                if (result != null)
                {
                    var resultInfo = AnalyzeResult(result);
                    if (!string.IsNullOrEmpty(resultInfo))
                    {
                        Console.WriteLine($"   📈 结果详情: {resultInfo}");
                    }
                }
            }
            
            Console.ResetColor();
            Console.WriteLine(); // 空行分隔
        }
    }

    /// <summary>
    /// 获取执行统计信息
    /// </summary>
    public static SqlExecutionStatistics GetStatistics()
    {
        lock (_lock)
        {
            var completedLogs = _executionLogs.Where(l => l.IsCompleted).ToList();
            
            return new SqlExecutionStatistics
            {
                TotalExecutions = completedLogs.Count,
                TotalExecutionTime = completedLogs.Sum(l => l.ElapsedMilliseconds),
                AverageExecutionTime = completedLogs.Any() ? completedLogs.Average(l => l.ElapsedMilliseconds) : 0,
                FastestExecution = completedLogs.Any() ? completedLogs.Min(l => l.ElapsedMilliseconds) : 0,
                SlowestExecution = completedLogs.Any() ? completedLogs.Max(l => l.ElapsedMilliseconds) : 0,
                OperationCounts = completedLogs.GroupBy(l => l.OperationName)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }

    /// <summary>
    /// 清除执行日志
    /// </summary>
    public static void ClearLogs()
    {
        lock (_lock)
        {
            _executionLogs.Clear();
        }
    }

    /// <summary>
    /// 输出执行统计报告
    /// </summary>
    public static void PrintStatisticsReport()
    {
        var stats = GetStatistics();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n📊 SQL执行统计报告");
        Console.WriteLine("==================");
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine($"🔢 总执行次数: {stats.TotalExecutions}");
        Console.WriteLine($"⏱️  总执行时间: {stats.TotalExecutionTime:F2}ms");
        Console.WriteLine($"📈 平均执行时间: {stats.AverageExecutionTime:F2}ms");
        Console.WriteLine($"🚀 最快执行: {stats.FastestExecution:F2}ms");
        Console.WriteLine($"🐌 最慢执行: {stats.SlowestExecution:F2}ms");
        
        if (stats.OperationCounts.Any())
        {
            Console.WriteLine("\n📋 操作统计:");
            foreach (var operation in stats.OperationCounts.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"   • {operation.Key}: {operation.Value} 次");
            }
        }
        
        Console.ResetColor();
    }

    private static Dictionary<string, object?> ExtractParameters(IDbCommand command)
    {
        var parameters = new Dictionary<string, object?>();
        
        foreach (IDbDataParameter param in command.Parameters)
        {
            parameters[param.ParameterName] = param.Value;
        }
        
        return parameters;
    }

    private static string FormatSql(string sql)
    {
        // 简单的SQL格式化，移除多余的空白字符
        return sql.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ")
            .Trim().Replace("  ", " ");
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
            _ => result.ToString() ?? "null"
        };
    }
}

/// <summary>
/// SQL执行日志记录
/// </summary>
public class SqlExecutionLog
{
    public Guid Id { get; set; }
    public string OperationName { get; set; } = "";
    public string SqlCommand { get; set; } = "";
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long StartTicks { get; set; }
    public long EndTicks { get; set; }
    public long ElapsedTicks { get; set; }
    public double ElapsedMilliseconds { get; set; }
    public object? Result { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// SQL执行统计信息
/// </summary>
public class SqlExecutionStatistics
{
    public int TotalExecutions { get; set; }
    public double TotalExecutionTime { get; set; }
    public double AverageExecutionTime { get; set; }
    public double FastestExecution { get; set; }
    public double SlowestExecution { get; set; }
    public Dictionary<string, int> OperationCounts { get; set; } = new();
}
