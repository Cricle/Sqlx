// -----------------------------------------------------------------------
// <copyright file="SimplifiedDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using ComprehensiveExample.Models;
using ComprehensiveExample.Services;

namespace ComprehensiveExample.Demonstrations;

/// <summary>
/// 简化版演示类 - 展示核心功能
/// </summary>
public static class SimplifiedDemo
{
    public static async Task RunAllDemonstrationsAsync(DbConnection connection)
    {
        Console.WriteLine("\n🚀 Sqlx 核心功能演示");
        Console.WriteLine("=".PadRight(60, '='));
        
        var userService = new UserService(connection);
        var customerService = new CustomerService(connection);
        var categoryService = new CategoryService(connection);
        var expressionService = new ExpressionToSqlService(connection);
        var batchService = new BatchOperationService(connection);
        
        // 1. 基础 CRUD 演示
        await DemonstrateBasicCrud(userService);
        
        // 2. 现代语法演示
        await DemonstrateModernSyntax(customerService);
        
        // 3. 动态查询演示
        await DemonstrateDynamicQueries(expressionService);
        
        // 4. 批量操作演示
        await DemonstrateBatchOperations(batchService);
        
        // 5. 复杂查询演示
        await DemonstrateComplexQueries(categoryService);
        
        Console.WriteLine("\n🎉 核心功能演示完成！");
    }
    
    private static async Task DemonstrateBasicCrud(IUserService userService)
    {
        Console.WriteLine("\n🎯 基础 CRUD 操作演示");
        
        // 查询活跃用户数量
        var activeCount = await userService.CountActiveUsersAsync();
        Console.WriteLine($"✅ 活跃用户数量: {activeCount}");
        
        // 查询所有用户
        var allUsers = await userService.GetAllUsersAsync();
        Console.WriteLine($"✅ 用户总数: {allUsers.Count}");
        
        // 创建测试用户
        var testUser = new User
        {
            Name = "测试用户",
            Email = $"test_{DateTime.Now.Ticks}@example.com",
            IsActive = true,
            DepartmentId = 1
        };
        
        var createResult = await userService.CreateUserAsync(testUser);
        Console.WriteLine($"✅ 创建用户: {createResult} 行受影响");
    }
    
    private static async Task DemonstrateModernSyntax(ICustomerService customerService)
    {
        Console.WriteLine("\n🏗️ 现代 C# 语法演示");
        
        // Primary Constructor 演示
        var testCustomer = new Customer(
            0,
            "测试客户",
            $"customer_{DateTime.Now.Ticks}@example.com",
            DateTime.Now.AddYears(-25)
        )
        {
            Status = CustomerStatus.Active,
            IsVip = true,
            Address = "测试地址"
        };
        
        try
        {
            var createResult = await customerService.CreateCustomerAsync(testCustomer);
            Console.WriteLine($"✅ 创建客户 (Primary Constructor): {createResult} 行受影响");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 客户创建: {ex.Message}");
        }
        
        // 查询 VIP 客户
        var vipCustomers = await customerService.GetVipCustomersAsync();
        Console.WriteLine($"✅ VIP 客户数量: {vipCustomers.Count}");
    }
    
    private static async Task DemonstrateDynamicQueries(IExpressionToSqlService expressionService)
    {
        Console.WriteLine("\n🎨 动态查询演示");
        
        // 查询活跃用户
        var activeUsers = await expressionService.QueryActiveUsersAsync(true);
        Console.WriteLine($"✅ 活跃用户查询: {activeUsers.Count} 个用户");
        
        // 按部门查询
        var deptUsers = await expressionService.QueryUsersByDepartmentAsync(1);
        Console.WriteLine($"✅ 技术部用户: {deptUsers.Count} 个用户");
        
        // 日期范围查询
        var recentUsers = await expressionService.QueryUsersByDateRangeAsync(
            DateTime.Now.AddDays(-30), 
            DateTime.Now
        );
        Console.WriteLine($"✅ 最近30天用户: {recentUsers.Count} 个用户");
        
        // 复杂条件查询
        var complexUsers = await expressionService.QueryUsersWithComplexConditionsAsync(
            true, 
            DateTime.Now.AddDays(-365), 
            10
        );
        Console.WriteLine($"✅ 复杂条件查询: {complexUsers.Count} 个用户");
    }
    
    private static async Task DemonstrateBatchOperations(IBatchOperationService batchService)
    {
        Console.WriteLine("\n⚡ 批量操作演示");
        
        // 生成测试数据
        var batchUsers = new List<User>();
        for (int i = 0; i < 100; i++)
        {
            batchUsers.Add(new User
            {
                Name = $"批量用户 {i + 1}",
                Email = $"batch{i + 1}_{DateTime.Now.Ticks}@example.com",
                IsActive = i % 2 == 0,
                DepartmentId = (i % 3) + 1
            });
        }
        
        try
        {
            var batchResult = await batchService.BatchCreateUsersAsync(batchUsers);
            Console.WriteLine($"✅ 批量创建用户: {batchResult} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 批量操作: {ex.Message}");
        }
    }
    
    private static async Task DemonstrateComplexQueries(ICategoryService categoryService)
    {
        Console.WriteLine("\n🔍 复杂查询演示");
        
        // 查询所有分类
        var allCategories = await categoryService.GetAllCategoriesAsync();
        Console.WriteLine($"✅ 分类总数: {allCategories.Count}");
        
        // 查询顶级分类
        var topCategories = await categoryService.GetTopLevelCategoriesAsync();
        Console.WriteLine($"✅ 顶级分类: {topCategories.Count} 个");
        
        // 演示层次结构
        foreach (var category in topCategories)
        {
            var subCategories = await categoryService.GetSubCategoriesAsync(category.Id);
            Console.WriteLine($"   - {category.Name}: {subCategories.Count} 个子分类");
        }
    }
}
