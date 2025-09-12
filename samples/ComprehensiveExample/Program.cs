// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ComprehensiveExample.Data;
using ComprehensiveExample.Demonstrations;
using ComprehensiveExample.Interactive;
using ComprehensiveExample.Models;
using ComprehensiveExample.Services;
using System;
using System.Data.Common;
using System.Threading.Tasks;

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
        // 显示欢迎界面
        InteractiveUI.ShowWelcomeScreen();

        InteractiveUI.ShowColoredTitle("🚀 Sqlx 全面功能演示", ConsoleColor.Cyan);

        // 🔧 设置 SQLite 数据库
        using var connection = DatabaseSetup.CreateConnection();

        // 📋 创建表结构
        await InteractiveUI.ShowLoadingAsync("正在初始化数据库",
            async () => await DatabaseSetup.InitializeDatabaseAsync(connection));

        try
        {
            // 🎯 创建所有服务实例 (自动生成实现)
            var userService = new UserService(connection);
            var departmentService = new DepartmentService(connection);
            var modernService = new ModernSyntaxService(connection);
            var customerService = new CustomerService(connection);
            var categoryService = new CategoryService(connection);
            var inventoryService = new InventoryService(connection);
            var auditLogService = new AuditLogService(connection);

            // 显示演示菜单
            await ShowDemoMenu(connection);

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
    /// 显示演示菜单
    /// </summary>
    static async Task ShowDemoMenu(DbConnection connection)
    {
        Console.WriteLine("\n🎯 Sqlx 全面功能演示菜单");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("1️⃣  基础 CRUD 操作演示");
        Console.WriteLine("2️⃣  🆕 智能 UPDATE 操作演示 (优化体验)");
        Console.WriteLine("3️⃣  Expression to SQL 动态查询演示");
        Console.WriteLine("4️⃣  DbBatch 批量操作演示");
        Console.WriteLine("5️⃣  多数据库方言支持演示");
        Console.WriteLine("6️⃣  现代 C# 语法支持演示");
        Console.WriteLine("7️⃣  复杂查询和分析演示");
        Console.WriteLine("8️⃣  性能基准测试对比");
        Console.WriteLine("9️⃣  🚀 完整功能综合演示 (推荐)");
        Console.WriteLine("A️⃣  全部单项演示 (详细版)");
        Console.WriteLine("0️⃣  退出演示");
        Console.WriteLine("=".PadRight(60, '='));

        while (true)
        {
            Console.Write("\n请选择演示项目 (0-9, A): ");
            var input = Console.ReadLine()?.ToUpper();

            switch (input)
            {
                case "1":
                    await DemonstrateCrudOperations(new UserService(connection));
                    break;
                case "2":
                    await SmartUpdateDemo.RunDemonstrationAsync(connection);
                    break;
                case "3":
                    await ExpressionToSqlDemo.RunDemonstrationAsync(connection);
                    break;
                case "4":
                    await BatchOperationDemo.RunDemonstrationAsync(connection);
                    break;
                case "5":
                    await MultiDatabaseDemo.RunDemonstrationAsync(connection);
                    break;
                case "6":
                    await DemonstrateModernSyntaxSupport(new ModernSyntaxService(connection));
                    break;
                case "7":
                    await DemonstrateComplexQueries(connection);
                    break;
                case "8":
                    await PerformanceTest.RunPerformanceTestAsync();
                    break;
                case "9":
                    Console.WriteLine("\n🚀 开始完整功能综合演示...");
                    await ComprehensiveDemo.RunFullDemonstrationAsync(connection);
                    ComprehensiveDemo.ShowFeatureSummary();
                    break;
                case "A":
                    await RunAllDemonstrations(connection);
                    break;
                case "0":
                    Console.WriteLine("👋 感谢使用 Sqlx 演示程序！");
                    return;
                default:
                    Console.WriteLine("❌ 无效选择，请输入 0-9 或 A");
                    continue;
            }

            Console.WriteLine("\n按任意键继续...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// 运行所有演示
    /// </summary>
    static async Task RunAllDemonstrations(DbConnection connection)
    {
        Console.WriteLine("\n🚀 开始全面演示 Sqlx 所有功能");
        Console.WriteLine("=".PadRight(60, '='));

        var userService = new UserService(connection);
        var departmentService = new DepartmentService(connection);
        var modernService = new ModernSyntaxService(connection);

        // 1. 基础 CRUD 操作
        await DemonstrateCrudOperations(userService);

        // 2. 高级功能
        await DemonstrateAdvancedFeatures(userService);

        // 3. 部门管理
        await DemonstrateDepartmentFeatures(departmentService, userService);

        // 4. 现代 C# 语法支持
        await DemonstrateModernSyntaxSupport(modernService);

        // 5. 智能 UPDATE 演示 (🆕 新功能)
        await SmartUpdateDemo.RunDemonstrationAsync(connection);

        // 6. Expression to SQL 动态查询
        await ExpressionToSqlDemo.RunDemonstrationAsync(connection);

        // 7. 批量操作演示
        await BatchOperationDemo.RunDemonstrationAsync(connection);

        // 8. 多数据库方言支持
        await MultiDatabaseDemo.RunDemonstrationAsync(connection);

        // 9. 复杂查询
        await DemonstrateComplexQueries(connection);

        // 10. 性能测试
        await PerformanceTest.RunPerformanceTestAsync();

        Console.WriteLine("\n🎉 全面演示完成！");
    }

    /// <summary>
    /// 演示复杂查询和分析功能
    /// </summary>
    static async Task DemonstrateComplexQueries(DbConnection connection)
    {
        Console.WriteLine("\n🔍 复杂查询和分析演示");
        Console.WriteLine("=".PadRight(60, '='));

        var customerService = new CustomerService(connection);
        var categoryService = new CategoryService(connection);
        var auditLogService = new AuditLogService(connection);

        try
        {

            // VIP 客户统计
            var vipCustomers = await customerService.GetVipCustomersAsync();
            Console.WriteLine($"⭐ VIP 客户总数: {vipCustomers.Count}");

            // 分类层次结构
            var topCategories = await categoryService.GetTopLevelCategoriesAsync();
            Console.WriteLine($"📂 顶级分类: {topCategories.Count} 个");

            foreach (var category in topCategories)
            {
                var subCategories = await categoryService.GetSubCategoriesAsync(category.Id);
                Console.WriteLine($"   - {category.Name}: {subCategories.Count} 个子分类");
            }

            // 审计日志演示
            var auditLog = new AuditLog("DEMO", "System", "ComplexQuery", "admin")
            {
                IpAddress = "127.0.0.1",
                UserAgent = "Sqlx Demo Application"
            };

            await auditLogService.CreateAuditLogAsync(auditLog);
            Console.WriteLine("📝 创建了演示审计日志");

            // 查询系统操作历史
            var systemLogs = await auditLogService.GetEntityAuditHistoryAsync("System", "ComplexQuery");
            Console.WriteLine($"📋 系统操作历史: {systemLogs.Count} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 复杂查询演示中的某些功能可能需要更多数据: {ex.Message}");
        }
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