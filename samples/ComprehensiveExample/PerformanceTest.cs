// -----------------------------------------------------------------------
// <copyright file="PerformanceTest.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ComprehensiveExample.Services;
using ComprehensiveExample.Data;

namespace ComprehensiveExample;

/// <summary>
/// 性能测试，验证装箱优化的效果
/// </summary>
public static class PerformanceTest
{
    public static async Task RunPerformanceTestAsync()
    {
        Console.WriteLine("\n🚀 性能测试开始...");
        Console.WriteLine("=".PadRight(60, '='));
        
        using var connection = DatabaseSetup.CreateConnection();
        await DatabaseSetup.InitializeDatabaseAsync(connection);
        
        var userService = new UserService(connection);
        
        // 预热
        await userService.CountActiveUsersAsync();
        
        const int iterations = 10000;
        
        // 测试标量查询性能（之前可能有装箱，现在应该没有）
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            await userService.CountActiveUsersAsync();
        }
        
        sw.Stop();
        
        Console.WriteLine($"📊 标量查询性能测试结果:");
        Console.WriteLine($"   - 迭代次数: {iterations:N0}");
        Console.WriteLine($"   - 总耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 平均耗时: {sw.ElapsedMilliseconds / (double)iterations:F3} ms/次");
        Console.WriteLine($"   - 吞吐量: {iterations / sw.Elapsed.TotalSeconds:F0} ops/sec");
        
        // 测试对象分配
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            await userService.CountActiveUsersAsync();
        }
        sw.Stop();
        
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);
        
        Console.WriteLine($"\n🗑️ 垃圾回收统计:");
        Console.WriteLine($"   - Gen 0 回收: {gen0After - gen0Before}");
        Console.WriteLine($"   - Gen 1 回收: {gen1After - gen1Before}");
        Console.WriteLine($"   - Gen 2 回收: {gen2After - gen2Before}");
        
        Console.WriteLine($"\n✅ 性能测试完成!");
        
        // 额外测试：大量实体查询
        Console.WriteLine("\n📋 实体查询性能测试...");
        
        sw.Restart();
        for (int i = 0; i < 1000; i++)
        {
            var users = await userService.GetAllUsersAsync();
        }
        sw.Stop();
        
        Console.WriteLine($"   - 实体查询 1000 次耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 平均耗时: {sw.ElapsedMilliseconds / 1000.0:F3} ms/次");
    }
}
