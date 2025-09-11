// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlDemo.cs" company="Cricle">
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
/// Expression to SQL 功能演示类
/// 展示动态查询构建和类型安全的数据库操作
/// </summary>
public static class ExpressionToSqlDemo
{
    public static async Task RunDemonstrationAsync(DbConnection connection)
    {
        Console.WriteLine("\n🎨 Expression to SQL 动态查询演示");
        Console.WriteLine("=".PadRight(60, '='));
        
        var expressionService = new ExpressionToSqlService(connection);
        
        // 演示基础动态查询
        await DemonstrateBasicDynamicQueries(expressionService);
        
        // 演示复杂条件组合
        await DemonstrateComplexConditions(expressionService);
        
        // 演示排序和分页
        await DemonstrateSortingAndPaging(expressionService);
        
        // 演示更新操作
        await DemonstrateUpdateOperations(expressionService);
        
        // 演示多表类型支持
        await DemonstrateMultipleEntityTypes(expressionService);
        
        // 演示动态搜索
        await DemonstrateDynamicSearch(expressionService);
    }
    
    private static async Task DemonstrateBasicDynamicQueries(IExpressionToSqlService service)
    {
        Console.WriteLine("\n📋 基础动态查询演示");
        
        // 1. 简单条件查询
        var activeUsers = await service.QueryActiveUsersAsync(true);
        Console.WriteLine($"✅ 活跃用户查询: 找到 {activeUsers.Count} 个活跃用户");
        
        // 2. 数值比较查询
        var techUsers = await service.QueryUsersByDepartmentAsync(1);
        Console.WriteLine($"✅ 部门查询: 技术部有 {techUsers.Count} 个用户");
        
        // 3. 字符串模糊查询
        var nameResults = await service.SearchUsersByNameAsync("%Alice%");
        Console.WriteLine($"✅ 模糊查询: 找到 {nameResults.Count} 个包含'Alice'的用户");
    }
    
    private static async Task DemonstrateComplexConditions(IExpressionToSqlService service)
    {
        Console.WriteLine("\n🔍 复杂条件组合演示");
        
        // 1. AND 条件组合
        var complexResults1 = await service.QueryUsersAsync("WHERE is_active = 1 AND department_id IS NOT NULL AND created_at > datetime('now', '-30 days')");
        Console.WriteLine($"✅ AND 条件: 找到 {complexResults1.Count} 个符合条件的用户");
        
        // 2. OR 条件组合
        var complexResults2 = await service.QueryUsersAsync("WHERE department_id = 1 OR department_id = 2");
        Console.WriteLine($"✅ OR 条件: 找到 {complexResults2.Count} 个技术部或人事部用户");
        
        // 3. NOT 条件
        var complexResults3 = await service.QueryUsersAsync("WHERE is_active = 0");
        Console.WriteLine($"✅ NOT 条件: 找到 {complexResults3.Count} 个非活跃用户");
    }
    
    private static async Task DemonstrateSortingAndPaging(IExpressionToSqlService service)
    {
        Console.WriteLine("\n📄 排序和分页演示");
        
        // 1. 排序查询
        var sortedUsers = await service.QueryUsersAsync("WHERE is_active = 1 ORDER BY name ASC, created_at DESC");
        Console.WriteLine($"✅ 排序查询: 按姓名和创建时间排序，共 {sortedUsers.Count} 条记录");
        
        // 2. 分页查询
        var pagedUsers = await service.GetPagedUsersAsync("WHERE is_active = 1", 5, 0);
        Console.WriteLine($"✅ 分页查询: 获取前5条记录，实际返回 {pagedUsers.Count} 条");
        
        // 显示分页结果
        foreach (var user in pagedUsers)
        {
            Console.WriteLine($"   - {user.Name} ({user.Email})");
        }
    }
    
    private static async Task DemonstrateUpdateOperations(IExpressionToSqlService service)
    {
        Console.WriteLine("\n✏️ 动态更新操作演示");
        
        // 1. 条件更新
        var updateCount1 = await service.UpdateUsersAsync("SET is_active = 0", "WHERE id = 999"); // 使用不存在的ID避免影响演示数据
        Console.WriteLine($"✅ 条件更新: 更新了 {updateCount1} 条记录");
        
        // 2. 表达式更新 (模拟名称更新)
        var updateCount2 = await service.UpdateUsersAsync("SET name = name || ' (Updated)'", "WHERE id = 999"); // 使用不存在的ID
        Console.WriteLine($"✅ 表达式更新: 更新了 {updateCount2} 条记录");
    }
    
    private static async Task DemonstrateMultipleEntityTypes(IExpressionToSqlService service)
    {
        Console.WriteLine("\n🏗️ 多实体类型支持演示");
        
        // 1. 客户查询 (Primary Constructor)
        var vipCustomers = await service.QueryCustomersAsync("WHERE is_vip = 1 ORDER BY total_spent DESC");
        Console.WriteLine($"✅ Primary Constructor: 找到 {vipCustomers.Count} 个VIP客户");
        
        // 2. 产品查询 (Record)
        var expensiveProducts = await service.QueryProductsAsync("WHERE price > 500 AND is_active = 1 ORDER BY name");
        Console.WriteLine($"✅ Record 类型: 找到 {expensiveProducts.Count} 个高价产品");
        
        // 3. 库存查询 (Record)
        var lowStockItems = await service.QueryInventoryAsync("WHERE quantity < reorder_level");
        Console.WriteLine($"✅ 库存查询: 找到 {lowStockItems.Count} 个低库存商品");
    }
    
    private static async Task DemonstrateDynamicSearch(IExpressionToSqlService service)
    {
        Console.WriteLine("\n🔍 动态搜索演示");
        
        // 模拟用户搜索输入
        var searchCriteria = new
        {
            Name = "Alice",
            IsActive = (bool?)true,
            DepartmentId = (int?)null,
            MinCreatedDate = (DateTime?)DateTime.Now.AddDays(-365)
        };
        
        // 根据条件动态构建查询
        var whereConditions = new List<string>();
        
        if (!string.IsNullOrEmpty(searchCriteria.Name))
        {
            whereConditions.Add($"name LIKE '%{searchCriteria.Name}%'");
            Console.WriteLine($"📝 添加姓名过滤: 包含 '{searchCriteria.Name}'");
        }
        
        if (searchCriteria.IsActive.HasValue)
        {
            var isVip = searchCriteria.IsActive.Value;
            whereConditions.Add($"is_vip = {(isVip ? "1" : "0")}");
            Console.WriteLine($"📝 添加VIP过滤: {(isVip ? "是" : "否")}");
        }
        
        if (searchCriteria.MinCreatedDate.HasValue)
        {
            var minDate = searchCriteria.MinCreatedDate.Value;
            whereConditions.Add($"created_at >= '{minDate:yyyy-MM-dd HH:mm:ss}'");
            Console.WriteLine($"📝 添加日期过滤: 大于等于 {minDate:yyyy-MM-dd}");
        }
        
        var whereClause = whereConditions.Count > 0 ? "WHERE " + string.Join(" AND ", whereConditions) + " ORDER BY name LIMIT 10" : "ORDER BY name LIMIT 10";
        
        var searchResults = await service.SearchCustomersAsync(whereClause);
        Console.WriteLine($"✅ 动态搜索结果: 找到 {searchResults.Count} 个匹配客户");
        
        // 显示搜索结果
        foreach (var customer in searchResults)
        {
            Console.WriteLine($"   - {customer.Name} ({customer.Email}) - VIP: {(customer.IsVip ? "是" : "否")}");
        }
        
        // 演示计数查询
        var userCount = await service.CountUsersAsync("WHERE is_active = 1 AND department_id IS NOT NULL");
        Console.WriteLine($"✅ 计数查询: 符合条件的用户总数为 {userCount}");
    }
}
