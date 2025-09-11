// -----------------------------------------------------------------------
// <copyright file="PerformanceTest.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ComprehensiveExample.Services;
using ComprehensiveExample.Data;
using ComprehensiveExample.Models;

namespace ComprehensiveExample;

/// <summary>
/// 性能测试，验证装箱优化的效果
/// </summary>
public static class PerformanceTest
{
    public static async Task RunPerformanceTestAsync()
    {
        Console.WriteLine("\n🚀 综合性能测试套件");
        Console.WriteLine("=".PadRight(60, '='));
        
        using var connection = DatabaseSetup.CreateConnection();
        await DatabaseSetup.InitializeDatabaseAsync(connection);
        
        var userService = new UserService(connection);
        var batchService = new BatchOperationService(connection);
        var expressionService = new ExpressionToSqlService(connection);
        var customerService = new CustomerService(connection);
        
        // 预热
        await userService.CountActiveUsersAsync();
        
        // 1. 标量查询性能测试
        await TestScalarQueryPerformance(userService);
        
        // 2. 实体查询性能测试  
        await TestEntityQueryPerformance(userService);
        
        // 3. 批量操作性能测试
        await TestBatchOperationPerformance(batchService, userService);
        
        // 4. Expression to SQL 性能测试
        await TestExpressionToSqlPerformance(expressionService);
        
        // 5. 复杂查询性能测试
        await TestComplexQueryPerformance(customerService);
        
        // 6. 内存使用测试
        await TestMemoryUsage(userService);
        
        // 7. 并发性能测试
        await TestConcurrentPerformance(userService);
        
        Console.WriteLine($"\n🎉 全部性能测试完成!");
    }
    
    private static async Task TestScalarQueryPerformance(IUserService userService)
    {
        Console.WriteLine("\n📊 标量查询性能测试");
        
        const int iterations = 10000;
        
        // 预热
        await userService.CountActiveUsersAsync();
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await userService.CountActiveUsersAsync();
        }
        sw.Stop();
        
        Console.WriteLine($"   - 迭代次数: {iterations:N0}");
        Console.WriteLine($"   - 总耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 平均耗时: {sw.ElapsedMilliseconds / (double)iterations:F3} ms/次");
        Console.WriteLine($"   - 吞吐量: {iterations / sw.Elapsed.TotalSeconds:F0} ops/sec");
    }
    
    private static async Task TestEntityQueryPerformance(IUserService userService)
    {
        Console.WriteLine("\n📋 实体查询性能测试");
        
        const int iterations = 1000;
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var users = await userService.GetAllUsersAsync();
        }
        sw.Stop();
        
        // 获取一次以检查记录数
        var sampleUsers = await userService.GetAllUsersAsync();
        
        Console.WriteLine($"   - 查询次数: {iterations:N0}");
        Console.WriteLine($"   - 每次返回: {sampleUsers.Count} 条记录");
        Console.WriteLine($"   - 总耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 平均耗时: {sw.ElapsedMilliseconds / (double)iterations:F3} ms/次");
        Console.WriteLine($"   - 记录处理速度: {(sampleUsers.Count * iterations) / sw.Elapsed.TotalSeconds:F0} 条/秒");
    }
    
    private static async Task TestBatchOperationPerformance(IBatchOperationService batchService, IUserService userService)
    {
        Console.WriteLine("\n⚡ 批量操作性能测试");
        
        // 生成测试数据
        var testUsers = GenerateTestUsers(1000);
        
        // 批量插入测试
        var sw = Stopwatch.StartNew();
        var batchResult = await batchService.BatchCreateUsersAsync(testUsers);
        sw.Stop();
        
        Console.WriteLine($"   - 批量插入 {testUsers.Count} 条记录");
        Console.WriteLine($"   - 耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - 平均: {sw.ElapsedMilliseconds / (double)testUsers.Count:F3} ms/条");
        Console.WriteLine($"   - 吞吐量: {testUsers.Count / sw.Elapsed.TotalSeconds:F0} 条/秒");
        
        // 单条插入对比测试 (小样本)
        var singleTestUsers = GenerateTestUsers(100);
        sw.Restart();
        
        foreach (var user in singleTestUsers)
        {
            user.Email = $"single_{Guid.NewGuid()}@test.com"; // 避免重复
            await userService.CreateUserAsync(user);
        }
        sw.Stop();
        
        var singleInsertSpeed = singleTestUsers.Count / sw.Elapsed.TotalSeconds;
        // 重新获取批量操作的时间
        sw.Restart();
        var tempBatchUsers = GenerateTestUsers(100);
        for (int i = 0; i < tempBatchUsers.Count; i++)
        {
            tempBatchUsers[i].Email = $"batch_temp_{i}_{DateTime.Now.Ticks}@test.com";
        }
        await batchService.BatchCreateUsersAsync(tempBatchUsers);
        sw.Stop();
        
        var batchInsertSpeed = tempBatchUsers.Count / sw.Elapsed.TotalSeconds;
        var improvement = batchInsertSpeed / singleInsertSpeed;
        
        Console.WriteLine($"   - 单条插入速度: {singleInsertSpeed:F0} 条/秒");
        Console.WriteLine($"   - 🚀 性能提升: {improvement:F1}x 倍");
    }
    
    private static async Task TestExpressionToSqlPerformance(IExpressionToSqlService expressionService)
    {
        Console.WriteLine("\n🎨 Expression to SQL 性能测试");
        
        const int iterations = 1000;
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var results = await expressionService.QueryActiveUsersAsync(true);
        }
        sw.Stop();
        
        // 获取一次样本
        var sampleResults = await expressionService.QueryActiveUsersAsync(true);
        
        Console.WriteLine($"   - 复杂动态查询执行 {iterations} 次");
        Console.WriteLine($"   - 每次返回: {sampleResults.Count} 条记录"); 
        Console.WriteLine($"   - 总耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 平均耗时: {sw.ElapsedMilliseconds / (double)iterations:F3} ms/次");
    }
    
    private static async Task TestComplexQueryPerformance(ICustomerService customerService)
    {
        Console.WriteLine("\n🔍 复杂查询性能测试");
        
        const int iterations = 500;
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var vipCustomers = await customerService.GetVipCustomersAsync();
        }
        sw.Stop();
        
        var sampleVips = await customerService.GetVipCustomersAsync();
        
        Console.WriteLine($"   - VIP客户查询执行 {iterations} 次");
        Console.WriteLine($"   - 每次返回: {sampleVips.Count} 条记录");
        Console.WriteLine($"   - 总耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 平均耗时: {sw.ElapsedMilliseconds / (double)iterations:F3} ms/次");
    }
    
    private static async Task TestMemoryUsage(IUserService userService)
    {
        Console.WriteLine("\n🗑️ 内存使用测试");
        
        const int iterations = 5000;
        
        // 收集垃圾回收前的状态
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1); 
        var gen2Before = GC.CollectionCount(2);
        var memoryBefore = GC.GetTotalMemory(false);
        
        // 执行大量查询
        for (int i = 0; i < iterations; i++)
        {
            await userService.CountActiveUsersAsync();
            
            if (i % 1000 == 0)
            {
                // 定期释放临时对象
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }
        
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);
        var memoryAfter = GC.GetTotalMemory(false);
        
        Console.WriteLine($"   - 执行了 {iterations:N0} 次查询");
        Console.WriteLine($"   - Gen 0 回收: {gen0After - gen0Before}");
        Console.WriteLine($"   - Gen 1 回收: {gen1After - gen1Before}");
        Console.WriteLine($"   - Gen 2 回收: {gen2After - gen2Before}");
        Console.WriteLine($"   - 内存变化: {(memoryAfter - memoryBefore) / 1024.0:F1} KB");
        Console.WriteLine($"   - 平均内存/查询: {(memoryAfter - memoryBefore) / (double)iterations:F1} bytes");
    }
    
    private static async Task TestConcurrentPerformance(IUserService userService)
    {
        Console.WriteLine("\n🔄 并发性能测试");
        
        const int concurrentTasks = 10;
        const int iterationsPerTask = 100;
        
        var tasks = new Task[concurrentTasks];
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < concurrentTasks; i++)
        {
            int taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < iterationsPerTask; j++)
                {
                    await userService.CountActiveUsersAsync();
                }
            });
        }
        
        await Task.WhenAll(tasks);
        sw.Stop();
        
        var totalOperations = concurrentTasks * iterationsPerTask;
        
        Console.WriteLine($"   - 并发任务数: {concurrentTasks}");
        Console.WriteLine($"   - 每任务查询数: {iterationsPerTask}");
        Console.WriteLine($"   - 总查询数: {totalOperations:N0}");
        Console.WriteLine($"   - 总耗时: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"   - 并发吞吐量: {totalOperations / sw.Elapsed.TotalSeconds:F0} ops/sec");
    }
    
    // 辅助方法：生成测试用户数据
    private static List<User> GenerateTestUsers(int count)
    {
        var users = new List<User>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            users.Add(new User
            {
                Name = $"Perf Test User {i + 1}",
                Email = $"perftest{i + 1}_{Guid.NewGuid():N}@example.com", // 确保唯一性
                CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                IsActive = random.Next(2) == 1,
                DepartmentId = random.Next(1, 6)
            });
        }
        
        return users;
    }
}
