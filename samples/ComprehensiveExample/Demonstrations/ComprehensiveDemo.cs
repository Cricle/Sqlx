// -----------------------------------------------------------------------
// <copyright file="ComprehensiveDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using ComprehensiveExample.Models;
using ComprehensiveExample.Services;
using Sqlx;

namespace ComprehensiveExample.Demonstrations;

/// <summary>
/// 🚀 Sqlx 全面功能综合演示类
/// 展示所有核心功能的完整使用场景
/// </summary>
public static class ComprehensiveDemo
{
    public static async Task RunFullDemonstrationAsync(DbConnection connection)
    {
        Console.WriteLine("\n🚀 Sqlx 全面功能综合演示");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("🎯 本演示将展示 Sqlx 的所有核心功能和最佳实践");
        Console.WriteLine("⏱️ 预计用时：5-8 分钟");
        Console.WriteLine("=".PadRight(80, '='));

        try
        {
            // 1. 基础 CRUD 操作演示
            await DemonstrateBasicCrudOperations(connection);

            // 2. 现代 C# 语法支持演示
            await DemonstrateModernCSharpSyntax(connection);

            // 3. 智能更新操作演示
            await DemonstrateSmartUpdateOperations(connection);

            // 4. Expression to SQL 动态查询演示
            await DemonstrateExpressionToSql(connection);

            // 5. 批量操作性能演示
            await DemonstrateBatchOperations(connection);

            // 6. 复杂查询和分析演示
            await DemonstrateComplexQueriesAndAnalytics(connection);

            // 7. 多数据库方言支持演示
            await DemonstrateMultiDatabaseSupport(connection);

            // 8. 高级特性演示
            await DemonstrateAdvancedFeatures(connection);

            // 9. 性能对比演示
            await DemonstratePerformanceComparison(connection);

            Console.WriteLine("\n🎉 全面功能演示完成！");
            Console.WriteLine("✨ 感谢体验 Sqlx 的强大功能");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 演示过程中发生错误: {ex.Message}");
            Console.WriteLine($"📋 详细信息: {ex}");
        }
    }

    /// <summary>
    /// 1. 基础 CRUD 操作演示
    /// </summary>
    private static async Task DemonstrateBasicCrudOperations(DbConnection connection)
    {
        Console.WriteLine("\n1️⃣ 基础 CRUD 操作演示");
        Console.WriteLine("-".PadRight(60, '-'));

        var userService = new UserService(connection);
        var departmentService = new DepartmentService(connection);

        // 📋 查询操作演示
        Console.WriteLine("📋 查询操作演示:");
        var allUsers = await userService.GetAllUsersAsync();
        Console.WriteLine($"   - 查询到 {allUsers.Count} 个用户");

        var allDepartments = await departmentService.GetAllDepartmentsAsync();
        Console.WriteLine($"   - 查询到 {allDepartments.Count} 个部门");

        // 🔍 条件查询演示
        Console.WriteLine("\n🔍 条件查询演示:");
        if (allUsers.Count > 0)
        {
            var firstUser = await userService.GetUserByIdAsync(allUsers[0].Id);
            Console.WriteLine($"   - 按 ID 查询用户: {firstUser?.Name ?? "未找到"}");

            var userByEmail = await userService.GetUserByEmailAsync(allUsers[0].Email);
            Console.WriteLine($"   - 按邮箱查询用户: {userByEmail?.Name ?? "未找到"}");
        }

        // ➕ 创建操作演示
        Console.WriteLine("\n➕ 创建操作演示:");
        var newUser = new User
        {
            Name = "演示用户",
            Email = $"demo_{DateTime.Now:yyyyMMddHHmmss}@example.com",
            DepartmentId = allDepartments.Count > 0 ? allDepartments[0].Id : null,
            CreatedAt = DateTime.Now
        };

        var createResult = await userService.CreateUserAsync(newUser);
        Console.WriteLine($"   - 创建用户结果: {createResult} 行受影响");

        // ✏️ 更新操作演示
        Console.WriteLine("\n✏️ 更新操作演示:");
        if (createResult > 0)
        {
            // 查询刚创建的用户
            var createdUser = await userService.GetUserByEmailAsync(newUser.Email);
            if (createdUser != null)
            {
                createdUser.Name = "更新后的演示用户";
                var updateResult = await userService.UpdateUserAsync(createdUser);
                Console.WriteLine($"   - 更新用户结果: {updateResult} 行受影响");

                // 📊 统计查询演示
                var activeCount = await userService.CountActiveUsersAsync();
                Console.WriteLine($"   - 活跃用户统计: {activeCount} 个");
            }
        }

        Console.WriteLine("✅ 基础 CRUD 操作演示完成");
    }

    /// <summary>
    /// 2. 现代 C# 语法支持演示
    /// </summary>
    private static async Task DemonstrateModernCSharpSyntax(DbConnection connection)
    {
        Console.WriteLine("\n2️⃣ 现代 C# 语法支持演示");
        Console.WriteLine("-".PadRight(60, '-'));

        var modernService = new ModernSyntaxService(connection);
        var customerService = new CustomerService(connection);
        var inventoryService = new InventoryService(connection);
        var auditLogService = new AuditLogService(connection);

        // 📦 Record 类型演示
        Console.WriteLine("📦 Record 类型演示 (C# 9+):");
        var products = await modernService.GetAllProductsAsync();
        Console.WriteLine($"   - 查询到 {products.Count} 个产品 (Record 类型)");

        if (products.Count > 0)
        {
            var product = products[0];
            Console.WriteLine($"   - 产品示例: {product.Name} - ¥{product.Price:N2}");
            Console.WriteLine($"   - Record 特性: 不可变主键 ID={product.Id}");
        }

        // 🔧 Primary Constructor 演示
        Console.WriteLine("\n🔧 Primary Constructor 演示 (C# 12+):");
        var customers = await customerService.GetAllCustomersAsync();
        Console.WriteLine($"   - 查询到 {customers.Count} 个客户 (Primary Constructor)");

        if (customers.Count > 0)
        {
            var customer = customers[0];
            Console.WriteLine($"   - 客户示例: {customer.Name} ({customer.Email})");
            Console.WriteLine($"   - Primary Constructor 特性: 只读属性 ID={customer.Id}");
            Console.WriteLine($"   - 可变属性: Status={customer.Status}, IsVip={customer.IsVip}");
        }

        // 📋 Record + Primary Constructor 组合演示
        Console.WriteLine("\n📋 Record + Primary Constructor 组合演示:");
        var auditLogs = await auditLogService.GetUserAuditLogsAsync("admin");
        Console.WriteLine($"   - 查询到 {auditLogs.Count} 条审计日志 (Record + Primary Constructor)");

        foreach (var log in auditLogs.Take(3))
        {
            Console.WriteLine($"   - {log.Action} {log.EntityType}:{log.EntityId} by {log.UserId}");
        }

        // 🏗️ 混合语法演示
        Console.WriteLine("\n🏗️ 混合语法演示:");
        var inventory = await inventoryService.GetAllInventoryAsync();
        Console.WriteLine($"   - 查询到 {inventory.Count} 个库存项 (Record 类型)");

        if (inventory.Count > 0)
        {
            var item = inventory[0];
            Console.WriteLine($"   - 库存示例: 产品ID={item.ProductId}, 数量={item.Quantity}");
            Console.WriteLine($"   - 计算属性: 可用数量={item.AvailableQuantity}");
        }

        Console.WriteLine("✅ 现代 C# 语法支持演示完成");
    }

    /// <summary>
    /// 3. 智能更新操作演示
    /// </summary>
    private static async Task DemonstrateSmartUpdateOperations(DbConnection connection)
    {
        Console.WriteLine("\n3️⃣ 智能更新操作演示");
        Console.WriteLine("-".PadRight(60, '-'));

        await SmartUpdateDemo.RunDemonstrationAsync(connection);

        Console.WriteLine("✅ 智能更新操作演示完成");
    }

    /// <summary>
    /// 4. Expression to SQL 动态查询演示
    /// </summary>
    private static async Task DemonstrateExpressionToSql(DbConnection connection)
    {
        Console.WriteLine("\n4️⃣ Expression to SQL 动态查询演示");
        Console.WriteLine("-".PadRight(60, '-'));

        await ExpressionToSqlDemo.RunDemonstrationAsync(connection);

        Console.WriteLine("✅ Expression to SQL 动态查询演示完成");
    }

    /// <summary>
    /// 5. 批量操作性能演示
    /// </summary>
    private static async Task DemonstrateBatchOperations(DbConnection connection)
    {
        Console.WriteLine("\n5️⃣ 批量操作性能演示");
        Console.WriteLine("-".PadRight(60, '-'));

        await BatchOperationDemo.RunDemonstrationAsync(connection);

        Console.WriteLine("✅ 批量操作性能演示完成");
    }

    /// <summary>
    /// 6. 复杂查询和分析演示
    /// </summary>
    private static async Task DemonstrateComplexQueriesAndAnalytics(DbConnection connection)
    {
        Console.WriteLine("\n6️⃣ 复杂查询和分析演示");
        Console.WriteLine("-".PadRight(60, '-'));

        var customerService = new CustomerService(connection);
        var categoryService = new CategoryService(connection);
        var inventoryService = new InventoryService(connection);
        var auditLogService = new AuditLogService(connection);

        // 📊 客户分析
        Console.WriteLine("📊 客户价值分析:");
        var vipCustomers = await customerService.GetVipCustomersAsync();
        Console.WriteLine($"   - VIP 客户总数: {vipCustomers.Count}");

        if (vipCustomers.Count > 0)
        {
            var topVip = vipCustomers.OrderByDescending(c => c.TotalSpent).First();
            Console.WriteLine($"   - 最高价值客户: {topVip.Name} (消费: ¥{topVip.TotalSpent:N2})");
        }

        var activeCustomers = await customerService.CountCustomersByStatusAsync(CustomerStatus.Active);
        Console.WriteLine($"   - 活跃客户数: {activeCustomers}");

        // 📂 分类层次分析
        Console.WriteLine("\n📂 分类层次结构分析:");
        var topCategories = await categoryService.GetTopLevelCategoriesAsync();
        Console.WriteLine($"   - 顶级分类数: {topCategories.Count}");

        foreach (var category in topCategories.Take(3))
        {
            var subCategories = await categoryService.GetSubCategoriesAsync(category.Id);
            Console.WriteLine($"   - {category.Name}: {subCategories.Count} 个子分类");
        }

        // 📦 库存分析
        Console.WriteLine("\n📦 库存状况分析:");
        var lowStockItems = await inventoryService.GetLowStockItemsAsync();
        Console.WriteLine($"   - 低库存商品数: {lowStockItems.Count}");

        if (lowStockItems.Count > 0)
        {
            var criticalItem = lowStockItems.OrderBy(i => i.Quantity).First();
            Console.WriteLine($"   - 最紧急补货: 产品ID={criticalItem.ProductId}, 库存={criticalItem.Quantity}");
        }

        // 📝 审计日志分析
        Console.WriteLine("\n📝 系统操作审计:");
        var systemLogs = await auditLogService.GetEntityAuditHistoryAsync("Product", "1");
        Console.WriteLine($"   - 产品操作历史: {systemLogs.Count} 条记录");

        Console.WriteLine("✅ 复杂查询和分析演示完成");
    }

    /// <summary>
    /// 7. 多数据库方言支持演示
    /// </summary>
    private static async Task DemonstrateMultiDatabaseSupport(DbConnection connection)
    {
        Console.WriteLine("\n7️⃣ 多数据库方言支持演示");
        Console.WriteLine("-".PadRight(60, '-'));

        await MultiDatabaseDemo.RunDemonstrationAsync(connection);

        Console.WriteLine("✅ 多数据库方言支持演示完成");
    }

    /// <summary>
    /// 8. 高级特性演示
    /// </summary>
    private static async Task DemonstrateAdvancedFeatures(DbConnection connection)
    {
        Console.WriteLine("\n8️⃣ 高级特性演示");
        Console.WriteLine("-".PadRight(60, '-'));

        var userService = new UserService(connection);

        // 🔍 高级搜索演示
        Console.WriteLine("🔍 高级搜索功能:");
        var searchResults = await userService.SearchUsersAsync("%演示%");
        Console.WriteLine($"   - 搜索包含'演示'的用户: {searchResults.Count} 个结果");

        // 📈 时间范围查询
        Console.WriteLine("\n📈 时间范围查询:");
        var recentUsers = await userService.GetRecentUsersAsync(DateTime.Now.AddDays(-30));
        Console.WriteLine($"   - 最近30天创建的用户: {recentUsers.Count} 个");

        // 🔗 关联查询演示
        Console.WriteLine("\n🔗 部门关联查询:");
        var departmentService = new DepartmentService(connection);
        var departments = await departmentService.GetAllDepartmentsAsync();

        foreach (var dept in departments.Take(3))
        {
            var deptUsers = await userService.GetUsersByDepartmentAsync(dept.Id);
            var userCount = await departmentService.CountUsersByDepartmentAsync(dept.Id);
            Console.WriteLine($"   - {dept.Name}: {userCount} 个用户");
        }

        Console.WriteLine("✅ 高级特性演示完成");
    }

    /// <summary>
    /// 9. 性能对比演示
    /// </summary>
    private static async Task DemonstratePerformanceComparison(DbConnection connection)
    {
        Console.WriteLine("\n9️⃣ 性能对比演示");
        Console.WriteLine("-".PadRight(60, '-'));

        // 运行性能测试
        await PerformanceTest.RunPerformanceTestAsync();

        Console.WriteLine("✅ 性能对比演示完成");
    }

    /// <summary>
    /// 显示功能特性总结
    /// </summary>
    public static void ShowFeatureSummary()
    {
        Console.WriteLine("\n🎯 Sqlx 核心特性总结");
        Console.WriteLine("=".PadRight(80, '='));

        var features = new[]
        {
            "✅ 零反射高性能 - 编译时代码生成",
            "✅ 类型安全 - 编译时错误检查",
            "✅ 智能推断 - 自动识别 SQL 操作",
            "✅ 现代 C# 语法 - Record、Primary Constructor 完美支持",
            "✅ Expression to SQL - 类型安全的动态查询",
            "✅ DbBatch 批量操作 - 10-100x 性能提升",
            "✅ 多数据库支持 - SQL Server、MySQL、PostgreSQL、SQLite 等",
            "✅ 智能更新操作 - 部分更新、批量更新、增量更新",
            "✅ 复杂查询支持 - 关联查询、聚合统计、分页排序",
            "✅ 零配置启动 - 无需复杂映射配置"
        };

        foreach (var feature in features)
        {
            Console.WriteLine($"  {feature}");
        }

        Console.WriteLine("\n🚀 性能优势:");
        Console.WriteLine("  📈 查询性能: 8,000+ ops/sec");
        Console.WriteLine("  ⚡ 批量插入: 6,000+ 条/秒 (比单条快 10-100 倍)");
        Console.WriteLine("  🗑️ 内存友好: 平均每查询 < 5 bytes");
        Console.WriteLine("  🔄 零 GC 压力: Gen 2 回收几乎为 0");

        Console.WriteLine("\n💡 适用场景:");
        Console.WriteLine("  🏢 企业级应用 - 高性能、高可靠性要求");
        Console.WriteLine("  🚀 微服务架构 - 轻量级、高性能数据访问");
        Console.WriteLine("  📊 数据分析平台 - 大量查询和批量处理");
        Console.WriteLine("  🎮 实时应用 - 低延迟、高吞吐量需求");

        Console.WriteLine("=".PadRight(80, '='));
    }
}
