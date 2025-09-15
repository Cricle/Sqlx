using System;
using System.Threading.Tasks;

namespace SqlxCompleteDemo;

/// <summary>
/// Sqlx 完整功能演示程序 - 直接展示所有功能
/// 
/// 本程序将自动演示以下功能：
/// 🚀 Expression to SQL 动态查询
/// 🌍 多数据库方言支持
/// 🔧 动态查询构建
/// 💡 类型安全的数据库操作
/// ⚡ 高性能零反射执行
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        ShowWelcome();
        
        try
        {
            // 运行全面的 SQLite 功能演示
            await ComprehensiveSqliteDemo.RunAsync();
            
            ShowSummary();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 演示过程中发生错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }
        
        Console.WriteLine("\n🎉 Sqlx 完整功能演示结束！");
        Console.WriteLine("按任意键退出...");
    }
    
    static void ShowWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                🚀 Sqlx SQLite 全功能演示                     ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║  本演示将自动展示 Sqlx 的所有核心功能和特性                  ║");
        Console.WriteLine("║  💾 CRUD操作 | 🔍 高级查询 | 📊 聚合分组 | 🔤 字符串操作      ║");
        Console.WriteLine("║  🧮 数学运算 | 🔗 联表查询 | 🔧 动态构建 | 💳 事务处理      ║");
        Console.WriteLine("║  ⚡ 性能优化 | 🛡️ 错误处理 | 🌍 方言支持                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }
    
    static void ShowSectionHeader(string title, string emoji = "🔹")
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n{emoji} {title}");
        Console.WriteLine(new string('=', title.Length + 4));
        Console.ResetColor();
    }
    
    static void ShowSummary()
    {
        ShowSectionHeader("🎉 SQLite 全功能演示总结", "📋");
        
        Console.WriteLine("Sqlx SQLite 全功能演示已完成！本次演示涵盖了：\n");
        
        var features = new[]
        {
            "✅ 💾 CRUD 操作 - CREATE, READ, UPDATE, DELETE 完整示例",
            "✅ 🔍 高级查询 - 复杂条件、分页、子查询、NULL处理",
            "✅ 📊 聚合分组 - COUNT, SUM, AVG, MAX, MIN, GROUP BY, HAVING",
            "✅ 🔤 字符串操作 - Length, Contains, StartsWith, ToLower, 连接",
            "✅ 🧮 数学日期 - Math.Abs, DateTime.AddDays, 数值范围",
            "✅ 🔗 联表查询 - 多表关联查询条件构建",
            "✅ 🔧 动态构建 - 运行时条件组合和查询构建",
            "✅ 💳 事务处理 - 事务中的批量操作和数据验证",
            "✅ ⚡ 性能优化 - 索引友好、批量查询、选择性字段",
            "✅ 🛡️ 错误处理 - 空值安全、数据验证、边界情况"
        };
        
        foreach (var feature in features)
        {
            Console.WriteLine(feature);
        }
        
        Console.WriteLine("\n🚀 Sqlx SQLite 的核心优势：");
        Console.WriteLine("   💡 类型安全 - 编译时检查，运行时安全");
        Console.WriteLine("   ⚡ 高性能 - 零反射，原生 SQL 执行");
        Console.WriteLine("   🎯 智能转换 - LINQ 表达式到 SQL 的完美映射");
        Console.WriteLine("   🗃️ SQLite 优化 - 针对 SQLite 的特殊优化");
        Console.WriteLine("   🔧 易于使用 - 简洁的 API，强大的功能");
        Console.WriteLine("   📊 功能全面 - 从基础 CRUD 到高级聚合分析");
    }
}