// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Threading.Tasks;

/// <summary>
/// Test execution modes for the repository example.
/// </summary>
internal enum TestMode
{
    SQLite
}

/// <summary>
/// Main program demonstrating Sqlx Repository pattern with optimized structure.
/// </summary>
internal class Program
{
    private const string ConnectionString = "server=(localdb)\\mssqllocaldb;database=sqlx_sample;integrated security=true";

    private static async Task Main(string[] args)
    {
        PrintWelcomeMessage();

        var testMode = ParseArguments(args);
        await RunSelectedTest(testMode);
    }

    private static void PrintWelcomeMessage()
    {
        Console.WriteLine("🚀 Sqlx Repository Pattern 最佳实践演示");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();

        Console.WriteLine("本示例展示了 Sqlx 最新功能：");
        Console.WriteLine("✨ Repository Pattern 自动生成");
        Console.WriteLine("🔥 智能 SQL 推断");
        Console.WriteLine("💡 编译时类型安全");
        Console.WriteLine("⚡ 高性能代码生成");
        Console.WriteLine("🎯 零反射运行时");
        Console.WriteLine();
    }

    private static TestMode ParseArguments(string[] args)
    {
        if (args.Length == 0) return TestMode.SQLite;
        return TestMode.SQLite;
    }

    private static async Task RunSelectedTest(TestMode mode)
    {
        try
        {
            await RunSQLiteTest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test execution failed: {ex.Message}");
        }
    }

    private static async Task RunSQLiteTest()
    {
        Console.WriteLine("🗄️ SQLite Code Generation Verification Mode");
        Console.WriteLine("============================================");

        ShowSQLiteCapabilities();

        bool testsPassed = await SQLiteTest.RunSQLiteTests();
        PrintTestResult("SQLite", testsPassed);
    }

    private static void ShowSQLiteCapabilities()
    {
        Console.WriteLine("\n=== SQLite Repository Pattern Capabilities ===");
        var capabilities = new[]
        {
            "✅ Real Database Operations",
            "✅ Complete CRUD Support",
            "✅ Type-safe Mapping",
            "✅ Parameterized Queries",
            "✅ Async/Sync Dual Support",
            "✅ Transaction Safety",
            "✅ High Performance",
            "✅ Zero Configuration Dependencies",
            "✅ Cross-platform Compatible"
        };

        foreach (var capability in capabilities)
        {
            Console.WriteLine(capability);
        }

        Console.WriteLine("\n💡 This demonstrates how Sqlx source generator creates actual working repository methods");
        Console.WriteLine("=== SQLite Real Code Generation Verification ===\n");
    }

    // Advanced and Real DB modes removed to simplify the sample

    private static void PrintTestResult(string testName, bool success)
    {
        var status = success ? "✅ SUCCESS" : "❌ FAILED";
        Console.WriteLine($"\n🎯 {testName} Test Result: {status}");
    }
}
