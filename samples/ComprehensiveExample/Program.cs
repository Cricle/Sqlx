// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using ComprehensiveExample.Models;
using ComprehensiveExample.Services;
using ComprehensiveExample.Data;

namespace ComprehensiveExample;

/// <summary>
/// 🚀 Sqlx 全面功能演示
/// 
/// 这个示例展示了 Sqlx 的所有核心功能：
/// ✨ Repository 模式自动生成
/// 🎯 智能 SQL 推断 
/// 💡 类型安全的数据库操作
/// ⚡ 高性能零反射执行
/// 📋 完整的 CRUD 操作演示
/// 🔍 自定义 SQL 查询
/// 📦 Record 类型支持
/// 🔗 部门关联演示
/// 📊 聚合统计
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Sqlx 全面功能演示");
        Console.WriteLine("=".PadRight(60, '='));
        
        // 🔧 设置 SQLite 数据库
        using var connection = DatabaseSetup.CreateConnection();
        
        // 📋 创建表结构
        await DatabaseSetup.InitializeDatabaseAsync(connection);
        
        try
        {
            // 🎯 创建 Repository (自动生成实现)
            var userService = new UserService(connection);
            var departmentService = new DepartmentService(connection);
            var modernService = new ModernSyntaxService(connection);
            
            // ✨ 演示基础 CRUD 操作
            await DemonstrateCrudOperations(userService);
            
            // 🧪 演示高级功能
            await DemonstrateAdvancedFeatures(userService);
            
            // 🏢 演示部门管理
            await DemonstrateDepartmentFeatures(departmentService, userService);
            
            // 🏗️ 演示现代 C# 语法支持
            await DemonstrateModernSyntaxSupport(modernService);
            
            // 🚀 性能测试
            await PerformanceTest.RunPerformanceTestAsync();
            
            Console.WriteLine("\n🎉 所有演示完成！按任意键退出...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }
        
        Console.ReadKey();
    }
    
    /// <summary>
    /// 演示基础 CRUD 操作
    /// </summary>
    static async Task DemonstrateCrudOperations(IUserService userService)
    {
        Console.WriteLine("\n🎯 演示基础 CRUD 操作...");
        
        // ➕ 创建用户 (自动推断为 INSERT)
        var newUsers = new[]
        {
            new User { Name = "Alice Johnson", Email = "alice@example.com", DepartmentId = 1 },
            new User { Name = "Bob Smith", Email = "bob@example.com", DepartmentId = 2 },
            new User { Name = "Charlie Brown", Email = "charlie@example.com", DepartmentId = 1 }
        };
        
        foreach (var user in newUsers)
        {
            user.CreatedAt = DateTime.Now;
            var createResult = await userService.CreateUserAsync(user);
            Console.WriteLine($"✅ 创建用户 {user.Name}: {createResult} 行受影响");
        }
        
        // 📋 查询所有用户 (自动推断为 SELECT)
        var allUsers = await userService.GetAllUsersAsync();
        Console.WriteLine($"📋 查询到 {allUsers.Count} 个用户:");
        foreach (var user in allUsers)
        {
            Console.WriteLine($"   - {user.Name} ({user.Email}) - 部门ID: {user.DepartmentId} - {(user.IsActive ? "活跃" : "非活跃")}");
        }
        
        // 🔍 按 ID 查询用户 (自动推断为 SELECT WHERE)
        var firstUser = await userService.GetUserByIdAsync(1);
        if (firstUser != null)
        {
            Console.WriteLine($"🔍 按 ID 查询: {firstUser.Name} ({firstUser.Email})");
            
            // ✏️ 更新用户 (自动推断为 UPDATE)
            firstUser.Name = "Alice Johnson-Smith";
            firstUser.Email = "alice.johnson.smith@example.com";
            var updateResult = await userService.UpdateUserAsync(firstUser);
            Console.WriteLine($"✏️ 更新用户: {updateResult} 行受影响");
        }
        
        // ❌ 删除用户 (自动推断为 DELETE)
        var deleteResult = await userService.DeleteUserAsync(3);
        Console.WriteLine($"❌ 删除用户 ID 3: {deleteResult} 行受影响");
    }
    
    /// <summary>
    /// 演示高级功能
    /// </summary>
    static async Task DemonstrateAdvancedFeatures(IUserService userService)
    {
        Console.WriteLine("\n🧪 演示高级功能...");
        
        // 🎯 自定义 SQL 查询
        var userByEmail = await userService.GetUserByEmailAsync("alice.johnson.smith@example.com");
        if (userByEmail != null)
        {
            Console.WriteLine($"🎯 按邮箱查询: {userByEmail.Name}");
        }
        
        // 📊 标量查询
        var activeCount = await userService.CountActiveUsersAsync();
        Console.WriteLine($"📊 活跃用户数量: {activeCount}");
        
        // 📈 复杂查询
        var recentUsers = await userService.GetRecentUsersAsync(DateTime.Now.AddDays(-1));
        Console.WriteLine($"📈 最近用户数量: {recentUsers.Count}");
        
        // 🔍 搜索功能演示
        var searchResults = await userService.SearchUsersAsync("%Johnson%");
        Console.WriteLine($"🔍 搜索包含'Johnson'的用户: {searchResults.Count} 个结果");
        foreach (var user in searchResults)
        {
            Console.WriteLine($"   - {user.Name} ({user.Email})");
        }
    }
    
    /// <summary>
    /// 演示部门管理功能
    /// </summary>
    static async Task DemonstrateDepartmentFeatures(IDepartmentService departmentService, IUserService userService)
    {
        Console.WriteLine("\n🏢 演示部门管理功能...");
        
        // 📋 查询所有部门
        var departments = await departmentService.GetAllDepartmentsAsync();
        Console.WriteLine($"📋 查询到 {departments.Count} 个部门:");
        foreach (var dept in departments)
        {
            Console.WriteLine($"   - {dept.Name}: {dept.Description}");
        }
        
        // 🔍 按 ID 查询部门
        var techDept = await departmentService.GetDepartmentByIdAsync(1);
        if (techDept != null)
        {
            Console.WriteLine($"🔍 技术部详情: {techDept.Name} - {techDept.Description}");
        }
        
        // ➕ 创建新部门
        var newDept = new Department 
        { 
            Name = "市场部", 
            Description = "负责市场推广和品牌建设",
            CreatedAt = DateTime.Now
        };
        var createResult = await departmentService.CreateDepartmentAsync(newDept);
        Console.WriteLine($"✅ 创建新部门: {createResult} 行受影响");
        
        // 🔗 演示部门关联查询
        Console.WriteLine("\n🔗 演示部门关联查询...");
        
        // 查询技术部的用户
        var techUsers = await userService.GetUsersByDepartmentAsync(1);
        Console.WriteLine($"📋 技术部用户 ({techUsers.Count} 人):");
        foreach (var user in techUsers)
        {
            Console.WriteLine($"   - {user.Name} ({user.Email})");
        }
        
        // 统计各部门用户数量
        Console.WriteLine("\n📊 部门用户统计:");
        foreach (var dept in departments)
        {
            var userCount = await departmentService.CountUsersByDepartmentAsync(dept.Id);
            Console.WriteLine($"   - {dept.Name}: {userCount} 用户");
        }
    }
    
    /// <summary>
    /// 演示现代 C# 语法支持 (Record)
    /// </summary>
    static async Task DemonstrateModernSyntaxSupport(IModernSyntaxService modernService)
    {
        Console.WriteLine("\n🏗️ 演示现代 C# 语法支持...");
        
        // Record 类型演示
        var products = new[]
        {
            new Product(0, "iPhone 15", 999.99m, 1) { CreatedAt = DateTime.Now, IsActive = true },
            new Product(0, "MacBook Pro", 2999.99m, 1) { CreatedAt = DateTime.Now, IsActive = true },
            new Product(0, "iPad Air", 599.99m, 1) { CreatedAt = DateTime.Now, IsActive = true }
        };
        
        foreach (var product in products)
        {
            await modernService.AddProductAsync(product);
            Console.WriteLine($"✅ 添加产品 (Record): {product.Name} - ${product.Price}");
        }
        
        var allProducts = await modernService.GetAllProductsAsync();
        Console.WriteLine($"📦 查询到 {allProducts.Count} 个产品 (Record 类型):");
        foreach (var product in allProducts)
        {
            Console.WriteLine($"   - {product.Name}: ${product.Price} (类别: {product.CategoryId})");
        }
        
        // 订单演示
        var orders = new[]
        {
            new Order { CustomerName = "张三", OrderDate = DateTime.Now, TotalAmount = 999.99m },
            new Order { CustomerName = "李四", OrderDate = DateTime.Now, TotalAmount = 2999.99m },
            new Order { CustomerName = "王五", OrderDate = DateTime.Now, TotalAmount = 599.99m }
        };
        
        foreach (var order in orders)
        {
            await modernService.AddOrderAsync(order);
            Console.WriteLine($"✅ 添加订单: 客户 {order.CustomerName} - ${order.TotalAmount}");
        }
        
        var allOrders = await modernService.GetAllOrdersAsync();
        Console.WriteLine($"🛒 查询到 {allOrders.Count} 个订单:");
        foreach (var order in allOrders)
        {
            Console.WriteLine($"   - 订单 #{order.Id}: {order.CustomerName} - ${order.TotalAmount}");
        }
        
        // 按客户查询订单
        var customerOrders = await modernService.GetOrdersByCustomerAsync("%张%");
        Console.WriteLine($"🔍 客户姓名包含'张'的订单: {customerOrders.Count} 个");
        foreach (var order in customerOrders)
        {
            Console.WriteLine($"   - {order.CustomerName}: ${order.TotalAmount}");
        }
    }
}