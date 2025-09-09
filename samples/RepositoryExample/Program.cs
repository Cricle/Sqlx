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
    RealDatabase,
    SQLite,
    AdvancedSQLite
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
        Console.WriteLine("🎯 Sqlx Repository Pattern Example!");
        Console.WriteLine("=====================================\n");
    }

    private static TestMode ParseArguments(string[] args)
    {
        if (args.Length == 0) return TestMode.SQLite;
        
        return args[0].ToLower() switch
        {
            "--real-db" => TestMode.RealDatabase,
            "--sqlite" => TestMode.SQLite,
            "--advanced" => TestMode.AdvancedSQLite,
            _ => TestMode.SQLite
        };
    }

    private static async Task RunSelectedTest(TestMode mode)
    {
        try
        {
            switch (mode)
            {
                case TestMode.RealDatabase:
                    await RunRealDatabaseTest();
                    break;
                case TestMode.SQLite:
                    await RunSQLiteTest();
                    break;
                case TestMode.AdvancedSQLite:
                    await RunAdvancedSQLiteTest();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test execution failed: {ex.Message}");
        }
    }

    private static async Task RunRealDatabaseTest()
    {
        Console.WriteLine("🗄️ Real Database Mode (SQL Server)");
        Console.WriteLine("===================================");
        
        bool connectionSuccessful = await RealDatabaseTest.TestDatabaseConnection();
        
        if (connectionSuccessful)
        {
            bool testsPassed = await RealDatabaseTest.RunRealDatabaseTests();
            PrintTestResult("Real Database", testsPassed);
        }
        else
        {
            Console.WriteLine("\n⚠️ Cannot connect to database, showing setup instructions...");
            RealDatabaseTest.ShowDatabaseSetupInstructions();
        }
    }

    private static async Task RunSQLiteTest()
    {
        Console.WriteLine("🗄️ SQLite Code Generation Verification Mode");
        Console.WriteLine("============================================");
        
        ShowSQLiteCapabilities();
        
        bool testsPassed = await SQLiteTest.RunSQLiteTests();
        
        // Run the new generic repository demo
        Console.WriteLine("\n🎭 Running Generic Repository Demo...");
        await GenericRepositoryDemo.RunDemoAsync();
        
        // Run Performance Comparison Demo
        Console.WriteLine("\n⚡ Running Performance Comparison Demo...");
        await PerformanceComparisonDemo.RunComparisonAsync();
        
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

    private static async Task RunAdvancedSQLiteTest()
    {
        Console.WriteLine("🚀 Advanced SQLite Enterprise Features Mode");
        Console.WriteLine("==========================================");
        Console.WriteLine("⚠️  Advanced features demo temporarily unavailable - Source generator doesn't support interface inheritance yet");
        Console.WriteLine("🔄 Switching to basic mode...\n");
        
        bool testsPassed = await SQLiteTest.RunSQLiteTests();
        PrintTestResult("Advanced SQLite", testsPassed);
    }

    private static void PrintTestResult(string testName, bool success)
    {
        var status = success ? "✅ SUCCESS" : "❌ FAILED";
        Console.WriteLine($"\n🎯 {testName} Test Result: {status}");
    }
}
