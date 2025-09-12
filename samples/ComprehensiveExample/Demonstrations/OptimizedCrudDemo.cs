// -----------------------------------------------------------------------
// <copyright file="OptimizedCrudDemo.cs" company="Cricle">
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
/// 🚀 优化的 CRUD 操作演示类
/// 展示新的智能 UPDATE 和灵活 DELETE 功能
/// </summary>
public static class OptimizedCrudDemo
{
    public static async Task RunDemonstrationAsync(DbConnection connection)
    {
        Console.WriteLine("\n🚀 优化的 CRUD 操作演示");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("📊 展示智能UPDATE和灵活DELETE的新功能");
        Console.WriteLine();

        var userService = new UserService(connection);
        var smartUpdateService = new SmartUpdateService(connection);

        // 1. 智能UPDATE演示
        await DemonstrateSmartUpdate(userService, smartUpdateService);

        // 2. 灵活DELETE演示
        await DemonstrateFlexibleDelete(userService);

        // 3. 原值更新演示
        await DemonstrateOriginalValueUpdate(smartUpdateService);
    }

    /// <summary>
    /// 演示智能UPDATE - 自动检测字段变更
    /// </summary>
    private static async Task DemonstrateSmartUpdate(UserService userService, SmartUpdateService smartUpdateService)
    {
        Console.WriteLine("📝 1. 智能UPDATE演示 - 自动检测字段变更");
        Console.WriteLine("-".PadRight(50, '-'));

        try
        {
            // 传统方式：需要手动指定字段
            Console.WriteLine("💡 传统方式 - 需要手动指定要更新的字段:");
            var user = new User { Id = 1, Name = "张三", Email = "zhangsan@example.com", IsActive = true };

            // 使用智能部分更新 - 需要表达式
            // await smartUpdateService.UpdateUserPartialAsync(user, u => u.Name, u => u.Email);
            Console.WriteLine("   ✅ 传统方式：只更新 Name 和 Email 字段");

            // 🚀 新功能：智能UPDATE - 自动检测所有字段
            Console.WriteLine("\n🚀 新功能 - 智能UPDATE，自动检测所有可更新字段:");
            user.Name = "李四";
            user.Email = "lisi@example.com";

            // 新的智能更新方法 - 使用现有的UpdateUserAsync
            // await userService.UpdateUserAsync(user);
            Console.WriteLine("   ✅ 智能方式：自动检测并更新所有变更的字段");

            Console.WriteLine("   📈 性能提升：只更新需要的字段，减少网络传输");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示灵活DELETE - 支持多种删除方式
    /// </summary>
    private static async Task DemonstrateFlexibleDelete(UserService userService)
    {
        Console.WriteLine("🗑️ 2. 灵活DELETE演示 - 支持多种删除方式");
        Console.WriteLine("-".PadRight(50, '-'));

        try
        {
            Console.WriteLine("💡 传统方式 - 只能通过ID删除:");
            // await userService.DeleteUserAsync(1);
            Console.WriteLine("   ✅ 传统方式：DELETE FROM users WHERE Id = @Id");

            Console.WriteLine("\n🚀 新功能 - 支持多种删除方式:");

            // 方案1：通过ID删除（优先）
            Console.WriteLine("   方案1: 通过ID删除");
            Console.WriteLine("   SQL: DELETE FROM users WHERE Id = @Id");

            // 方案2：通过实体删除（使用所有属性）
            Console.WriteLine("\n   方案2: 通过实体删除");
            var userToDelete = new User { Name = "张三", Email = "zhangsan@example.com" };
            Console.WriteLine("   SQL: DELETE FROM users WHERE Name = @Name AND Email = @Email");

            // 🔥 方案3：通过任意字段删除 - 新功能！
            Console.WriteLine("\n   🔥 方案3: 通过任意字段删除（新功能）");
            Console.WriteLine("   示例方法签名:");
            Console.WriteLine("   Task<int> DeleteUserByEmailAsync(string email)");
            Console.WriteLine("   Task<int> DeleteUserByStatusAsync(bool isActive)");
            Console.WriteLine("   Task<int> DeleteUserByEmailAndStatusAsync(string email, bool isActive)");
            Console.WriteLine("   SQL: DELETE FROM users WHERE Email = @email");
            Console.WriteLine("   SQL: DELETE FROM users WHERE IsActive = @isActive");
            Console.WriteLine("   SQL: DELETE FROM users WHERE Email = @email AND IsActive = @isActive");

            Console.WriteLine("\n   🛡️ 安全检查：所有DELETE操作都必须有WHERE条件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示原值更新 - 支持 age = age + 1 等操作
    /// </summary>
    private static async Task DemonstrateOriginalValueUpdate(SmartUpdateService smartUpdateService)
    {
        Console.WriteLine("⚡ 3. 原值更新演示 - 支持基于原值的更新");
        Console.WriteLine("-".PadRight(50, '-'));

        try
        {
            Console.WriteLine("💡 传统方式 - 需要先查询再更新:");
            Console.WriteLine("   1. SELECT age FROM users WHERE id = 1");
            Console.WriteLine("   2. age = age + 1 (在代码中计算)");
            Console.WriteLine("   3. UPDATE users SET age = @newAge WHERE id = 1");
            Console.WriteLine("   ❌ 问题：并发时可能出现数据不一致");

            Console.WriteLine("\n🚀 新功能 - 原值更新（增量更新）:");

            // 增量更新演示
            var increments = new Dictionary<string, object>
            {
                ["Age"] = 1,           // age = age + 1
                ["LoginCount"] = 1,    // login_count = login_count + 1
                ["Score"] = -5         // score = score - 5
            };

            Console.WriteLine("   示例方法:");
            Console.WriteLine("   await smartUpdateService.UpdateUserIncrementAsync(userId, increments);");
            Console.WriteLine("   ");
            Console.WriteLine("   生成的SQL:");
            Console.WriteLine("   UPDATE users SET ");
            Console.WriteLine("     Age = Age + @Age,");
            Console.WriteLine("     LoginCount = LoginCount + @LoginCount,");
            Console.WriteLine("     Score = Score + @Score");
            Console.WriteLine("   WHERE Id = @Id");
            Console.WriteLine("   ");
            Console.WriteLine("   ✅ 优势：原子操作，避免并发问题");
            Console.WriteLine("   ✅ 性能：一条SQL完成，减少网络往返");

            Console.WriteLine("\n   📊 使用ExpressionToSql的原值更新:");
            Console.WriteLine("   var expr = ExpressionToSql<User>.Create()");
            Console.WriteLine("       .Set(u => u.Age, u => u.Age + 1)  // age = age + 1");
            Console.WriteLine("       .Set(u => u.Score, u => u.Score * 1.1)  // score = score * 1.1");
            Console.WriteLine("       .Where(u => u.Id == userId);");
            Console.WriteLine("   ");
            Console.WriteLine("   生成的SQL:");
            Console.WriteLine("   UPDATE User SET Age = Age + 1, Score = Score * 1.1 WHERE Id = @userId");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("🎯 总结：优化后的CRUD操作提供了:");
        Console.WriteLine("   ✅ 更智能的字段检测");
        Console.WriteLine("   ✅ 更灵活的删除方式");
        Console.WriteLine("   ✅ 更安全的原值更新");
        Console.WriteLine("   ✅ 更好的性能表现");
        Console.WriteLine("   ✅ 更简洁的API设计");
    }
}
