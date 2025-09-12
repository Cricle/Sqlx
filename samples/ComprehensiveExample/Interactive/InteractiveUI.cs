// -----------------------------------------------------------------------
// <copyright file="InteractiveUI.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComprehensiveExample.Interactive;

/// <summary>
/// 交互式用户界面辅助类
/// 提供更好的演示体验和用户交互
/// </summary>
public static class InteractiveUI
{
    /// <summary>
    /// 显示欢迎界面
    /// </summary>
    public static void ShowWelcomeScreen()
    {
        Console.Clear();
        
        // 显示 ASCII Art Logo
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
 ██████╗  ██████╗ ██╗     ██╗  ██╗
██╔════╝ ██╔═══██╗██║     ╚██╗██╔╝
╚█████╗  ██║   ██║██║      ╚███╔╝ 
 ╚═══██╗ ██║▄▄ ██║██║      ██╔██╗ 
██████╔╝ ╚██████╔╝███████╗██╔╝ ██╗
╚═════╝   ╚══▀▀═╝ ╚══════╝╚═╝  ╚═╝
                                   ");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("🚀 Sqlx 全面功能演示程序");
        Console.ResetColor();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✨ 现代 .NET 数据访问层的完美解决方案");
        Console.ResetColor();
        
        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        
        // 显示核心特性
        var features = new[]
        {
            "⚡ 零反射高性能 - 编译时代码生成",
            "🛡️ 类型安全 - 编译时错误检查",
            "🎯 智能推断 - 自动识别 SQL 操作",
            "📊 原生 DbBatch - 10-100x 批量性能",
            "🎨 Expression to SQL - 动态查询构建",
            "🏗️ 现代 C# 语法 - Record、Primary Constructor 支持",
            "🌐 多数据库方言 - SQL Server、MySQL、PostgreSQL、SQLite",
            "✨ 零学习成本 - 无需额外配置"
        };
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("🎯 核心特性:");
        Console.ResetColor();
        
        foreach (var feature in features)
        {
            Console.WriteLine($"  {feature}");
            System.Threading.Thread.Sleep(100); // 逐行显示效果
        }
        
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();
        
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("💡 提示: 选择 '9' 可体验完整功能综合演示 (推荐)");
        Console.ResetColor();
        
        Console.WriteLine("按任意键继续...");
        Console.ReadKey();
    }
    
    /// <summary>
    /// 显示进度条
    /// </summary>
    public static void ShowProgress(string operation, int current, int total)
    {
        const int barWidth = 50;
        var progress = (double)current / total;
        var filledWidth = (int)(barWidth * progress);
        
        Console.Write($"\r{operation}: [");
        
        // 绘制进度条
        for (int i = 0; i < barWidth; i++)
        {
            if (i < filledWidth)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("█");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("░");
            }
        }
        
        Console.ResetColor();
        Console.Write($"] {progress:P0} ({current}/{total})");
        
        if (current == total)
        {
            Console.WriteLine(" ✅");
        }
    }
    
    /// <summary>
    /// 显示加载动画
    /// </summary>
    public static async Task ShowLoadingAsync(string message, Func<Task> operation)
    {
        var spinnerChars = new[] { '|', '/', '-', '\\' };
        var spinnerIndex = 0;
        var isCompleted = false;
        
        // 启动操作
        var operationTask = operation().ContinueWith(_ => isCompleted = true);
        
        // 显示加载动画
        Console.Write($"{message} ");
        while (!isCompleted)
        {
            Console.Write($"\r{message} {spinnerChars[spinnerIndex]}");
            spinnerIndex = (spinnerIndex + 1) % spinnerChars.Length;
            await Task.Delay(100);
        }
        
        Console.Write($"\r{message} ✅");
        Console.WriteLine();
        
        await operationTask;
    }
    
    /// <summary>
    /// 显示彩色标题
    /// </summary>
    public static void ShowColoredTitle(string title, ConsoleColor color = ConsoleColor.Cyan)
    {
        Console.WriteLine();
        Console.ForegroundColor = color;
        Console.WriteLine(title);
        Console.WriteLine("=".PadRight(title.Length, '='));
        Console.ResetColor();
    }
    
    /// <summary>
    /// 显示成功消息
    /// </summary>
    public static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ResetColor();
    }
    
    /// <summary>
    /// 显示警告消息
    /// </summary>
    public static void ShowWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️ {message}");
        Console.ResetColor();
    }
    
    /// <summary>
    /// 显示错误消息
    /// </summary>
    public static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {message}");
        Console.ResetColor();
    }
    
    /// <summary>
    /// 显示信息消息
    /// </summary>
    public static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"ℹ️ {message}");
        Console.ResetColor();
    }
    
    /// <summary>
    /// 显示数据表格
    /// </summary>
    public static void ShowTable<T>(IList<T> data, params (string Header, Func<T, string> ValueSelector)[] columns)
    {
        if (data.Count == 0)
        {
            ShowWarning("没有数据显示");
            return;
        }
        
        // 计算列宽
        var columnWidths = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            columnWidths[i] = Math.Max(columns[i].Header.Length, 
                data.Max(item => columns[i].ValueSelector(item).Length));
        }
        
        // 显示表头
        Console.ForegroundColor = ConsoleColor.Yellow;
        for (int i = 0; i < columns.Length; i++)
        {
            Console.Write($"| {columns[i].Header.PadRight(columnWidths[i])} ");
        }
        Console.WriteLine("|");
        Console.ResetColor();
        
        // 显示分隔线
        for (int i = 0; i < columns.Length; i++)
        {
            Console.Write($"|{new string('-', columnWidths[i] + 2)}");
        }
        Console.WriteLine("|");
        
        // 显示数据行
        foreach (var item in data)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                var value = columns[i].ValueSelector(item);
                Console.Write($"| {value.PadRight(columnWidths[i])} ");
            }
            Console.WriteLine("|");
        }
    }
    
    /// <summary>
    /// 显示性能指标
    /// </summary>
    public static void ShowPerformanceMetrics(string operation, long elapsedMs, int iterations)
    {
        var avgMs = (double)elapsedMs / iterations;
        var opsPerSec = iterations * 1000.0 / elapsedMs;
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"📊 {operation} 性能指标:");
        Console.ResetColor();
        
        Console.WriteLine($"   ⏱️  总耗时: {elapsedMs:N0} ms");
        Console.WriteLine($"   🔄 迭代次数: {iterations:N0}");
        Console.WriteLine($"   ⚡ 平均耗时: {avgMs:F3} ms/次");
        Console.WriteLine($"   🚀 吞吐量: {opsPerSec:F0} ops/sec");
        
        // 性能等级评估
        var performanceLevel = opsPerSec switch
        {
            > 10000 => ("🔥 极高", ConsoleColor.Red),
            > 5000 => ("⚡ 很高", ConsoleColor.Yellow),
            > 1000 => ("✅ 良好", ConsoleColor.Green),
            > 500 => ("📊 一般", ConsoleColor.Blue),
            _ => ("⚠️ 较低", ConsoleColor.DarkYellow)
        };
        
        Console.ForegroundColor = performanceLevel.Item2;
        Console.WriteLine($"   📈 性能等级: {performanceLevel.Item1}");
        Console.ResetColor();
    }
    
    /// <summary>
    /// 确认对话框
    /// </summary>
    public static bool Confirm(string message, bool defaultValue = true)
    {
        var prompt = defaultValue ? "[Y/n]" : "[y/N]";
        Console.Write($"{message} {prompt}: ");
        
        var input = Console.ReadLine()?.Trim().ToLower();
        
        return input switch
        {
            "y" or "yes" => true,
            "n" or "no" => false,
            "" => defaultValue,
            _ => defaultValue
        };
    }
    
    /// <summary>
    /// 选择菜单
    /// </summary>
    public static int ShowMenu(string title, params string[] options)
    {
        ShowColoredTitle(title);
        
        for (int i = 0; i < options.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {options[i]}");
        }
        
        while (true)
        {
            Console.Write($"\n请选择 (1-{options.Length}): ");
            if (int.TryParse(Console.ReadLine(), out var choice) && 
                choice >= 1 && choice <= options.Length)
            {
                return choice - 1;
            }
            
            ShowError("无效选择，请重新输入");
        }
    }
    
    /// <summary>
    /// 暂停并等待用户按键
    /// </summary>
    public static void Pause(string message = "按任意键继续...")
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.ReadKey(true);
    }
}
