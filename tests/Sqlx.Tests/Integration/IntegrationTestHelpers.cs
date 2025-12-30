// -----------------------------------------------------------------------
// <copyright file="IntegrationTestHelpers.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Tests.TestModels;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 集成测试辅助方法
/// </summary>
public static class IntegrationTestHelpers
{
    /// <summary>
    /// 生成测试用户数据
    /// </summary>
    public static List<User> GenerateTestUsers(int count = 5)
    {
        var users = new List<User>();
        var names = new[] { "张三", "李四", "王五", "赵六", "钱七", "孙八", "周九", "吴十" };
        var random = new Random(42); // 固定种子以确保可重复性

        for (int i = 0; i < count; i++)
        {
            users.Add(new User
            {
                Name = names[i % names.Length],
                Email = $"user{i + 1}@example.com",
                Age = 20 + random.Next(30),
                Balance = random.Next(1000, 20000),
                CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                IsActive = random.Next(2) == 0
            });
        }

        return users;
    }

    /// <summary>
    /// 生成测试产品数据
    /// </summary>
    public static List<Product> GenerateTestProducts(int count = 3)
    {
        var products = new List<Product>
        {
            new Product { Name = "iPhone 15", Category = "Electronics", Price = 999m, Stock = 100, IsDeleted = false },
            new Product { Name = "MacBook Pro", Category = "Electronics", Price = 2499m, Stock = 50, IsDeleted = false },
            new Product { Name = "Magic Mouse", Category = "Electronics", Price = 99m, Stock = 200, IsDeleted = false }
        };

        return products.Take(count).ToList();
    }

    /// <summary>
    /// 生成测试日志数据
    /// </summary>
    public static List<Log> GenerateTestLogs(int count = 100)
    {
        var logs = new List<Log>();
        var levels = new[] { "INFO", "WARN", "ERROR" };
        var random = new Random(42);

        for (int i = 1; i <= count; i++)
        {
            logs.Add(new Log
            {
                Level = levels[i % 3],
                Message = $"日志消息 #{i}",
                Timestamp = DateTime.Now.AddSeconds(-i)
            });
        }

        return logs;
    }

    /// <summary>
    /// 验证两个用户列表是否相等
    /// </summary>
    public static bool AreUsersEqual(User user1, User user2)
    {
        return user1.Name == user2.Name &&
               user1.Email == user2.Email &&
               user1.Age == user2.Age &&
               Math.Abs(user1.Balance - user2.Balance) < 0.01m &&
               user1.IsActive == user2.IsActive;
    }

    /// <summary>
    /// 验证两个产品列表是否相等
    /// </summary>
    public static bool AreProductsEqual(Product p1, Product p2)
    {
        return p1.Name == p2.Name &&
               p1.Category == p2.Category &&
               Math.Abs(p1.Price - p2.Price) < 0.01m &&
               p1.Stock == p2.Stock;
    }
}
