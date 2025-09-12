// -----------------------------------------------------------------------
// <copyright file="BatchOperationDemo.cs" company="Cricle">
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
/// DbBatch 批量操作演示类
/// 展示高性能批量数据处理能力
/// </summary>
public static class BatchOperationDemo
{
    public static async Task RunDemonstrationAsync(DbConnection connection)
    {
        Console.WriteLine("\n🚀 DbBatch 批量操作演示");
        Console.WriteLine("=".PadRight(60, '='));

        var batchService = new BatchOperationService(connection);
        var userService = new UserService(connection);
        var customerService = new CustomerService(connection);
        var modernService = new ModernSyntaxService(connection);
        var inventoryService = new InventoryService(connection);

        // 演示批量插入性能
        await DemonstrateBatchInsertPerformance(batchService, userService);

        // 演示不同类型的批量操作
        await DemonstrateDifferentEntityTypes(batchService, customerService, modernService, inventoryService);

        // 演示批量更新和删除
        await DemonstrateBatchUpdateAndDelete(batchService);

        // 演示原生 DbBatch 命令
        await DemonstrateNativeBatchCommands(batchService);

        // 性能对比测试
        await PerformanceComparison(batchService, userService);
    }

    private static async Task DemonstrateBatchInsertPerformance(IBatchOperationService batchService, IUserService userService)
    {
        Console.WriteLine("\n📊 批量插入性能演示");

        // 准备测试数据
        var batchUsers = GenerateTestUsers(1000);
        Console.WriteLine($"📝 准备批量插入 {batchUsers.Count} 个用户...");

        // 批量插入
        var sw = Stopwatch.StartNew();
        var batchResult = await batchService.BatchCreateUsersAsync(batchUsers);
        sw.Stop();

        Console.WriteLine($"✅ 批量插入完成:");
        Console.WriteLine($"   - 插入数量: {batchResult} 条记录");
        Console.WriteLine($"   - 耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - 平均: {sw.ElapsedMilliseconds / (double)batchUsers.Count:F3} ms/条");
        Console.WriteLine($"   - 吞吐量: {batchUsers.Count / sw.Elapsed.TotalSeconds:F0} 条/秒");

        // 验证插入结果
        var allUsers = await userService.GetAllUsersAsync();
        Console.WriteLine($"📈 数据库中现有用户总数: {allUsers.Count}");
    }

    private static async Task DemonstrateDifferentEntityTypes(
        IBatchOperationService batchService,
        ICustomerService customerService,
        IModernSyntaxService modernService,
        IInventoryService inventoryService)
    {
        Console.WriteLine("\n🏗️ 不同实体类型批量操作演示");

        // 1. 批量创建客户 (Primary Constructor)
        var customers = GenerateTestCustomers(100);
        var customerResult = await batchService.BatchCreateCustomersAsync(customers);
        Console.WriteLine($"✅ 批量创建客户 (Primary Constructor): {customerResult} 条");

        // 验证客户数据
        var allCustomers = await customerService.GetAllCustomersAsync();
        Console.WriteLine($"   - 数据库中客户总数: {allCustomers.Count}");

        // 2. 批量创建产品 (Record)
        var products = GenerateTestProducts(50);
        var productResult = await batchService.BatchCreateProductsAsync(products);
        Console.WriteLine($"✅ 批量创建产品 (Record): {productResult} 条");

        // 验证产品数据
        var allProducts = await modernService.GetAllProductsAsync();
        Console.WriteLine($"   - 数据库中产品总数: {allProducts.Count}");

        // 3. 批量创建库存 (Record)
        var inventoryItems = GenerateTestInventory(allProducts.Take(20).ToList());
        if (inventoryItems.Count > 0)
        {
            var inventoryResult = await batchService.BatchCreateInventoryAsync(inventoryItems);
            Console.WriteLine($"✅ 批量创建库存 (Record): {inventoryResult} 条");

            // 验证库存数据
            var allInventory = await inventoryService.GetAllInventoryAsync();
            Console.WriteLine($"   - 数据库中库存总数: {allInventory.Count}");
        }
    }

    private static async Task DemonstrateBatchUpdateAndDelete(IBatchOperationService batchService)
    {
        Console.WriteLine("\n✏️ 批量更新和删除演示");

        try
        {
            // 准备更新数据 - 获取前10个用户进行更新
            var usersToUpdate = GenerateTestUsers(10);
            for (int i = 0; i < usersToUpdate.Count; i++)
            {
                usersToUpdate[i].Id = i + 1000; // 使用较大的ID避免冲突
                usersToUpdate[i].Name = $"Updated User {i + 1}";
                usersToUpdate[i].IsActive = false;
            }

            // 批量更新
            var updateResult = await batchService.BatchUpdateUsersAsync(usersToUpdate);
            Console.WriteLine($"✅ 批量更新: {updateResult} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 批量更新演示: {ex.Message}");
        }

        try
        {
            // 准备删除数据
            var idsToDelete = Enumerable.Range(2000, 10).ToList(); // 使用不存在的ID

            // 批量删除
            var deleteResult = await batchService.BatchDeleteUsersAsync(idsToDelete);
            Console.WriteLine($"✅ 批量删除: {deleteResult} 条记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 批量删除演示: {ex.Message}");
        }
    }

    private static async Task DemonstrateNativeBatchCommands(IBatchOperationService batchService)
    {
        Console.WriteLine("\n⚡ 原生 DbBatch 命令演示");

        var batchCommands = new List<string>
        {
            "UPDATE users SET is_active = 1 WHERE id > 0",
            "UPDATE departments SET description = description || ' (Updated)' WHERE id = 1",
            "INSERT INTO audit_logs (action, entity_type, entity_id, user_id) VALUES ('BATCH_DEMO', 'System', '0', 'admin')"
        };

        try
        {
            var commandResult = await batchService.ExecuteBatchCommandAsync(batchCommands);
            Console.WriteLine($"✅ 原生批量命令: 执行了 {commandResult} 个命令");
            Console.WriteLine($"   - 用户状态更新");
            Console.WriteLine($"   - 部门描述更新");
            Console.WriteLine($"   - 审计日志记录");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 原生批量命令演示: {ex.Message}");
        }
    }

    private static async Task PerformanceComparison(IBatchOperationService batchService, IUserService userService)
    {
        Console.WriteLine("\n📈 性能对比测试");

        const int testCount = 100;
        var testUsers = GenerateTestUsers(testCount);

        // 1. 单条插入性能测试
        Console.WriteLine($"🔄 测试单条插入 {testCount} 条记录...");
        var sw1 = Stopwatch.StartNew();

        for (int i = 0; i < Math.Min(10, testCount); i++) // 限制单条测试数量避免过长时间
        {
            var user = testUsers[i];
            user.Name = $"Single Insert User {i}";
            user.Email = $"single{i}@test.com";
            await userService.CreateUserAsync(user);
        }

        sw1.Stop();

        // 2. 批量插入性能测试
        Console.WriteLine($"⚡ 测试批量插入 {testCount} 条记录...");
        var batchTestUsers = GenerateTestUsers(testCount);
        for (int i = 0; i < batchTestUsers.Count; i++)
        {
            batchTestUsers[i].Name = $"Batch Insert User {i}";
            batchTestUsers[i].Email = $"batch{i}@test.com";
        }

        var sw2 = Stopwatch.StartNew();
        await batchService.BatchCreateUsersAsync(batchTestUsers);
        sw2.Stop();

        // 性能对比结果
        var singleRecordsPerSecond = 10 / sw1.Elapsed.TotalSeconds;
        var batchRecordsPerSecond = testCount / sw2.Elapsed.TotalSeconds;
        var performanceImprovement = batchRecordsPerSecond / singleRecordsPerSecond;

        Console.WriteLine($"\n📊 性能对比结果:");
        Console.WriteLine($"   单条插入:");
        Console.WriteLine($"     - 10 条记录耗时: {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"     - 吞吐量: {singleRecordsPerSecond:F0} 条/秒");
        Console.WriteLine($"   批量插入:");
        Console.WriteLine($"     - {testCount} 条记录耗时: {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"     - 吞吐量: {batchRecordsPerSecond:F0} 条/秒");
        Console.WriteLine($"   🚀 性能提升: {performanceImprovement:F1}x 倍!");
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
                Name = $"Test User {i + 1}",
                Email = $"testuser{i + 1}@example.com",
                CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                IsActive = random.Next(2) == 1,
                DepartmentId = random.Next(1, 6) // 1-5 部门
            });
        }

        return users;
    }

    // 辅助方法：生成测试客户数据
    private static List<Customer> GenerateTestCustomers(int count)
    {
        var customers = new List<Customer>();
        var random = new Random();
        var cities = new[] { "北京", "上海", "广州", "深圳", "杭州", "南京", "武汉", "成都" };

        for (int i = 0; i < count; i++)
        {
            var birthDate = DateTime.Now.AddYears(-random.Next(18, 65));
            customers.Add(new Customer(
                0, // ID will be auto-generated
                $"Test Customer {i + 1}",
                $"customer{i + 1}@test.com",
                birthDate
            )
            {
                Status = (CustomerStatus)(random.Next(1, 5)),
                TotalSpent = random.Next(0, 50000),
                Address = $"{cities[random.Next(cities.Length)]}市测试区",
                Phone = $"138{random.Next(10000000, 99999999)}",
                IsVip = random.Next(10) < 3 // 30% VIP
            });
        }

        return customers;
    }

    // 辅助方法：生成测试产品数据
    private static List<Product> GenerateTestProducts(int count)
    {
        var products = new List<Product>();
        var categories = new[] { "手机", "电脑", "平板", "耳机", "音响", "相机", "手表", "配件" };
        var brands = new[] { "Apple", "Samsung", "华为", "小米", "OPPO", "vivo", "一加", "魅族" };
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var category = categories[random.Next(categories.Length)];
            var brand = brands[random.Next(brands.Length)];
            var price = random.Next(100, 10000);

            products.Add(new Product(
                0, // ID will be auto-generated
                $"{brand} {category} {i + 1}",
                price,
                random.Next(1, 7) // 1-6 分类
            ));
        }

        return products;
    }

    // 辅助方法：生成测试库存数据
    private static List<InventoryItem> GenerateTestInventory(List<Product> products)
    {
        var inventory = new List<InventoryItem>();
        var random = new Random();
        var locations = new[] { "A区", "B区", "C区", "D区" };

        foreach (var product in products)
        {
            var quantity = random.Next(0, 1000);
            var reorderLevel = random.Next(10, 100);

            inventory.Add(new InventoryItem(
                product.Id,
                quantity,
                reorderLevel
            )
            {
                WarehouseLocation = $"{locations[random.Next(locations.Length)]}-{random.Next(1, 100):D3}",
                ReservedQuantity = random.Next(0, Math.Min(quantity, 50))
            });
        }

        return inventory;
    }
}
