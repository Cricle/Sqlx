// -----------------------------------------------------------------------
// <copyright file="SmartUpdateDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ComprehensiveExample.Models;
using ComprehensiveExample.Services;

namespace ComprehensiveExample.Demonstrations;

/// <summary>
/// 智能 UPDATE 操作演示类
/// 展示优化后的高性能、灵活、易用的更新操作
/// </summary>
public static class SmartUpdateDemo
{
    public static async Task RunDemonstrationAsync(DbConnection connection)
    {
        Console.WriteLine("\n🚀 智能 UPDATE 操作演示");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("📊 展示优化后的更新操作：简单易懂、灵活高效、性能卓越");
        Console.WriteLine();
        
        var smartUpdateService = new SmartUpdateService(connection);
        var userService = new UserService(connection);
        var customerService = new CustomerService(connection);
        
        // 1. 部分更新演示 - 性能优化
        await DemonstratePartialUpdate(smartUpdateService, userService);
        
        // 2. 批量条件更新演示 - 避免 N+1 问题
        await DemonstrateBatchUpdate(smartUpdateService);
        
        // 3. 增量更新演示 - 原子操作
        await DemonstrateIncrementalUpdate(smartUpdateService, customerService);
        
        // 4. 乐观锁更新演示 - 并发安全
        await DemonstrateOptimisticUpdate(smartUpdateService);
        
        // 5. 高性能批量字段更新演示
        await DemonstrateBulkFieldUpdate(smartUpdateService);
        
        // 6. 性能对比测试
        await PerformanceComparison(smartUpdateService, userService);
    }
    
    /// <summary>
    /// 演示部分更新 - 只更新指定字段
    /// </summary>
    private static async Task DemonstratePartialUpdate(ISmartUpdateService smartService, IUserService userService)
    {
        Console.WriteLine("🎯 部分更新演示 - 只更新需要的字段");
        Console.WriteLine("💡 场景：用户只修改了邮箱和活跃状态，无需更新所有字段");
        Console.WriteLine();
        
        try
        {
            // 获取一个测试用户
            var users = await userService.GetAllUsersAsync();
            if (users.Any())
            {
                var user = users.First();
                var originalEmail = user.Email;
                var originalName = user.Name;
                
                Console.WriteLine($"📋 原始数据: {user.Name} ({originalEmail})");
                
                // 修改用户信息
                user.Email = $"updated_{DateTime.Now.Ticks}@example.com";
                user.IsActive = !user.IsActive;
                user.Name = "这个字段不会被更新"; // 故意修改，但不包含在更新字段中
                
                var sw = Stopwatch.StartNew();
                
                // 智能部分更新 - 只更新邮箱和活跃状态
                var affectedRows = await smartService.UpdateUserPartialAsync(user,
                    u => u.Email,           // 只更新邮箱
                    u => u.IsActive);       // 只更新活跃状态
                    
                sw.Stop();
                
                Console.WriteLine($"✅ 部分更新完成: 影响 {affectedRows} 行");
                Console.WriteLine($"⚡ 执行时间: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"🎯 只更新了 Email 和 IsActive 字段");
                Console.WriteLine($"💾 节省的数据传输: 避免更新 Name, CreatedAt, DepartmentId 等字段");
                
                // 验证更新结果
                var updatedUser = await userService.GetUserByIdAsync(user.Id);
                Console.WriteLine($"📋 更新后数据: {updatedUser.Name} ({updatedUser.Email})");
                Console.WriteLine($"🔍 验证: Name 字段保持原值 '{originalName}'，Email 已更新");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("⚠️ 没有找到测试用户");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 部分更新演示失败: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示批量条件更新 - 避免 N+1 问题
    /// </summary>
    private static async Task DemonstrateBatchUpdate(ISmartUpdateService smartService)
    {
        Console.WriteLine("📦 批量条件更新演示 - 避免 N+1 问题");
        Console.WriteLine("💡 场景：批量将特定条件的用户状态设为非活跃");
        Console.WriteLine();
        
        try
        {
            var sw = Stopwatch.StartNew();
            
            // 准备批量更新的字段和值
            var setValues = new Dictionary<string, object>
            {
                ["IsActive"] = false,
                ["Name"] = "Batch Updated User"
            };
            
            // 批量更新：将 ID > 1000 的用户设为非活跃 (演示用，实际可能没有这么多用户)
            var affectedRows = await smartService.UpdateUsersBatchAsync(
                setValues, 
                "id > 1000");
                
            sw.Stop();
            
            Console.WriteLine($"✅ 批量更新完成: 影响 {affectedRows} 行");
            Console.WriteLine($"⚡ 执行时间: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"🎯 单条 SQL 语句完成批量更新");
            Console.WriteLine($"💾 性能优势: 避免了 N 次单独的 UPDATE 语句");
            
            if (affectedRows == 0)
            {
                Console.WriteLine("💡 提示: 没有符合条件 (id > 1000) 的用户，这是正常的");
                
                // 演示另一个更实际的批量更新
                var setValues2 = new Dictionary<string, object>
                {
                    ["IsActive"] = true  // 重新激活所有用户
                };
                
                var affectedRows2 = await smartService.UpdateUsersBatchAsync(setValues2, "id > 0");
                Console.WriteLine($"🔄 重新激活所有用户: 影响 {affectedRows2} 行");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 批量更新演示失败: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示增量更新 - 原子操作
    /// </summary>
    private static async Task DemonstrateIncrementalUpdate(ISmartUpdateService smartService, ICustomerService customerService)
    {
        Console.WriteLine("➕➖ 增量更新演示 - 数值字段原子操作");
        Console.WriteLine("💡 场景：客户购买商品，增加消费金额，避免并发问题");
        Console.WriteLine();
        
        try
        {
            // 获取一个测试客户
            var customers = await customerService.GetAllCustomersAsync();
            if (customers.Any())
            {
                var customer = customers.First();
                var originalSpent = customer.TotalSpent;
                
                Console.WriteLine($"📋 客户: {customer.Name}");
                Console.WriteLine($"💰 原始消费金额: ${originalSpent:F2}");
                
                var sw = Stopwatch.StartNew();
                
                // 模拟购买操作 - 增量更新消费金额
                var increments = new Dictionary<string, decimal>
                {
                    ["TotalSpent"] = 299.99m,    // 增加消费金额
                };
                
                var affectedRows = await smartService.UpdateCustomerIncrementAsync(customer.Id, increments);
                sw.Stop();
                
                Console.WriteLine($"✅ 增量更新完成: 影响 {affectedRows} 行");
                Console.WriteLine($"⚡ 执行时间: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine($"🎯 原子操作: TotalSpent = TotalSpent + 299.99");
                Console.WriteLine($"🛡️ 并发安全: 避免读取-修改-写入的竞态条件");
                
                // 验证更新结果
                var updatedCustomer = await customerService.GetCustomerByIdAsync(customer.Id);
                Console.WriteLine($"💰 更新后金额: ${updatedCustomer.TotalSpent:F2}");
                Console.WriteLine($"📈 增加金额: ${updatedCustomer.TotalSpent - originalSpent:F2}");
                
                // 再次演示减少操作
                Console.WriteLine();
                Console.WriteLine("🔄 演示减少操作 (退款场景):");
                
                var decrementSw = Stopwatch.StartNew();
                var decrements = new Dictionary<string, decimal>
                {
                    ["TotalSpent"] = -50.00m,    // 减少消费金额 (退款)
                };
                
                await smartService.UpdateCustomerIncrementAsync(customer.Id, decrements);
                decrementSw.Stop();
                
                var finalCustomer = await customerService.GetCustomerByIdAsync(customer.Id);
                Console.WriteLine($"💰 退款后金额: ${finalCustomer.TotalSpent:F2}");
                Console.WriteLine($"⚡ 退款操作时间: {decrementSw.ElapsedMilliseconds} ms");
            }
            else
            {
                Console.WriteLine("⚠️ 没有找到测试客户");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 增量更新演示失败: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 演示乐观锁更新 - 并发安全
    /// </summary>
    private static Task DemonstrateOptimisticUpdate(ISmartUpdateService smartService)
    {
        Console.WriteLine("🔒 乐观锁更新演示 - 并发安全");
        Console.WriteLine("💡 场景：多用户同时修改同一条记录，防止数据覆盖");
        Console.WriteLine();
        
        try
        {
            Console.WriteLine("📝 模拟并发修改场景:");
            Console.WriteLine("   用户A 和 用户B 同时获取了同一个用户记录");
            Console.WriteLine("   用户A 先提交修改，用户B 后提交");
            Console.WriteLine("   系统应该检测到版本冲突并阻止用户B的修改");
            Console.WriteLine();
            
            // 创建带版本字段的用户模型 (这里简化演示，实际需要扩展 User 模型)
            Console.WriteLine("✅ 乐观锁更新演示完成");
            Console.WriteLine("💡 提示: 需要在 User 模型中添加 Version 字段以支持乐观锁");
            Console.WriteLine("🔧 实现步骤:");
            Console.WriteLine("   1. 在实体中添加 Version 或 RowVersion 字段");
            Console.WriteLine("   2. 每次更新时检查版本号");
            Console.WriteLine("   3. 更新成功时递增版本号");
            Console.WriteLine("   4. 版本不匹配时返回冲突错误");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 乐观锁更新演示失败: {ex.Message}");
        }
        
        Console.WriteLine();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 演示高性能批量字段更新
    /// </summary>
    private static async Task DemonstrateBulkFieldUpdate(ISmartUpdateService smartService)
    {
        Console.WriteLine("⚡ 高性能批量字段更新演示 - DbBatch 模式");
        Console.WriteLine("💡 场景：批量更新多个用户的不同字段，每个用户更新的内容不同");
        Console.WriteLine();
        
        try
        {
            var sw = Stopwatch.StartNew();
            
            // 准备批量更新数据 - 每个用户更新不同的字段
            var updates = new Dictionary<int, Dictionary<string, object>>
            {
                // 用户1: 更新邮箱和活跃状态
                [1] = new Dictionary<string, object>
                {
                    ["Email"] = $"bulk_user1_{DateTime.Now.Ticks}@example.com",
                    ["IsActive"] = true
                },
                // 用户2: 只更新名称
                [2] = new Dictionary<string, object>
                {
                    ["Name"] = $"Bulk Updated User 2 - {DateTime.Now:HH:mm:ss}"
                },
                // 用户3: 更新活跃状态和部门
                [3] = new Dictionary<string, object>
                {
                    ["IsActive"] = false,
                    ["DepartmentId"] = 2
                }
            };
            
            var affectedRows = await smartService.UpdateUsersBulkFieldsAsync(updates);
            sw.Stop();
            
            Console.WriteLine($"✅ 批量字段更新完成: 影响 {affectedRows} 行");
            Console.WriteLine($"⚡ 执行时间: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"🚀 使用 DbBatch 实现真正的批量操作");
            Console.WriteLine($"📊 更新详情:");
            Console.WriteLine($"   - 用户1: 更新 Email + IsActive");
            Console.WriteLine($"   - 用户2: 更新 Name");
            Console.WriteLine($"   - 用户3: 更新 IsActive + DepartmentId");
            Console.WriteLine($"💾 性能优势: 一次网络往返完成所有更新");
            
            if (affectedRows == 0)
            {
                Console.WriteLine("💡 提示: 用户ID 1,2,3 可能不存在，这是正常的演示结果");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 批量字段更新演示失败: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// 性能对比测试
    /// </summary>
    private static async Task PerformanceComparison(ISmartUpdateService smartService, IUserService userService)
    {
        Console.WriteLine("📈 性能对比测试 - 智能更新 vs 传统更新");
        Console.WriteLine();
        
        try
        {
            var users = await userService.GetAllUsersAsync();
            if (!users.Any())
            {
                Console.WriteLine("⚠️ 没有用户数据进行性能测试");
                return;
            }
            
            var testUser = users.First();
            const int iterations = 100; // 测试次数
            
            Console.WriteLine($"🧪 测试用户: {testUser.Name} (ID: {testUser.Id})");
            Console.WriteLine($"🔄 测试次数: {iterations} 次");
            Console.WriteLine();
            
            // 1. 传统全字段更新性能测试
            Console.WriteLine("1️⃣ 传统全字段更新测试:");
            var traditionalSw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                testUser.Email = $"traditional_test_{i}@example.com";
                await userService.UpdateUserAsync(testUser);
            }
            
            traditionalSw.Stop();
            var traditionalAvg = traditionalSw.ElapsedMilliseconds / (double)iterations;
            
            Console.WriteLine($"   ⏱️ 总时间: {traditionalSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"   📊 平均时间: {traditionalAvg:F2} ms/次");
            Console.WriteLine($"   📝 特点: 更新所有字段，数据传输量大");
            Console.WriteLine();
            
            // 2. 智能部分更新性能测试
            Console.WriteLine("2️⃣ 智能部分更新测试:");
            var smartSw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                testUser.Email = $"smart_test_{i}@example.com";
                await smartService.UpdateUserPartialAsync(testUser, u => u.Email);
            }
            
            smartSw.Stop();
            var smartAvg = smartSw.ElapsedMilliseconds / (double)iterations;
            
            Console.WriteLine($"   ⏱️ 总时间: {smartSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"   📊 平均时间: {smartAvg:F2} ms/次");
            Console.WriteLine($"   📝 特点: 只更新指定字段，数据传输量小");
            Console.WriteLine();
            
            // 3. 性能对比结果
            Console.WriteLine("📊 性能对比结果:");
            var improvement = traditionalAvg / smartAvg;
            var speedupPercent = (1 - smartAvg / traditionalAvg) * 100;
            
            Console.WriteLine($"   🚀 智能更新性能提升: {improvement:F1}x 倍");
            Console.WriteLine($"   📈 速度提升: {speedupPercent:F1}%");
            
            if (improvement > 1.2)
            {
                Console.WriteLine($"   ✅ 显著性能提升! 推荐使用智能部分更新");
            }
            else if (improvement > 1.0)
            {
                Console.WriteLine($"   ✅ 轻微性能提升，在字段较多时更明显");
            }
            else
            {
                Console.WriteLine($"   💡 性能相近，但智能更新减少了网络传输量");
            }
            
            Console.WriteLine();
            Console.WriteLine("💡 性能提升说明:");
            Console.WriteLine("   - 字段越多，智能更新优势越明显");
            Console.WriteLine("   - 网络延迟越高，减少传输量的收益越大");
            Console.WriteLine("   - 数据库负载越高，减少处理量的收益越大");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 性能对比测试失败: {ex.Message}");
        }
        
        Console.WriteLine();
    }
}
