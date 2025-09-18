// -----------------------------------------------------------------------
// <copyright file="SqlTemplateRefactoredDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Sqlx;
using SqlxDemo.Models;

namespace SqlxDemo.Services
{
    /// <summary>
    /// 演示重构后的SqlTemplate设计：分离模板定义与参数值
    /// 展示纯模板设计的优势：可重用、高性能、概念清晰
    /// </summary>
    public class SqlTemplateRefactoredDemo
    {
        /// <summary>
        /// 演示1：纯模板定义的重用性
        /// </summary>
        public void DemoPureTemplateReuse()
        {
            Console.WriteLine("=== 演示1：纯模板定义的重用性 ===");

            // 1. 定义纯模板（只定义一次）
            var userQueryTemplate = SqlTemplate.Parse(@"
                SELECT Id, Name, Email, IsActive 
                FROM Users 
                WHERE IsActive = @isActive 
                AND Age > @minAge");

            Console.WriteLine($"模板定义: {userQueryTemplate.Sql}");
            Console.WriteLine($"是否为纯模板: {userQueryTemplate.IsPureTemplate}");

            // 2. 重复使用同一模板，绑定不同参数
            var execution1 = userQueryTemplate.Execute(new { isActive = true, minAge = 18 });
            var execution2 = userQueryTemplate.Execute(new { isActive = false, minAge = 65 });
            var execution3 = userQueryTemplate.Execute(new { isActive = true, minAge = 25 });

            Console.WriteLine($"\n执行1: {execution1.Render()}");
            Console.WriteLine($"执行2: {execution2.Render()}");
            Console.WriteLine($"执行3: {execution3.Render()}");
        }

        /// <summary>
        /// 演示2：流式参数绑定
        /// </summary>
        public void DemoFluentParameterBinding()
        {
            Console.WriteLine("\n=== 演示2：流式参数绑定 ===");

            var template = SqlTemplate.Parse(@"
                SELECT * FROM Products 
                WHERE CategoryId = @categoryId 
                AND Price BETWEEN @minPrice AND @maxPrice 
                AND Name LIKE @namePattern");

            // 流式绑定参数
            var execution = template.Bind()
                .Param("categoryId", 1)
                .Param("minPrice", 10.0m)
                .Param("maxPrice", 100.0m)
                .Param("namePattern", "%laptop%")
                .Build();

            Console.WriteLine($"绑定后的SQL: {execution.Render()}");
        }

        /// <summary>
        /// 演示3：与旧设计对比
        /// </summary>
        public void DemoOldVsNewDesign()
        {
            Console.WriteLine("\n=== 演示3：旧设计 vs 新设计对比 ===");

            // 旧设计（有问题）：每次都创建新模板
            Console.WriteLine("旧设计（不推荐）：");
            #pragma warning disable CS0618 // 忽略过时警告
            var oldTemplate1 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
            var oldTemplate2 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 2 });
            var oldTemplate3 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 3 });
            #pragma warning restore CS0618

            Console.WriteLine($"  创建了3个模板实例（浪费内存）");
            Console.WriteLine($"  模板1: {oldTemplate1}");
            Console.WriteLine($"  模板2: {oldTemplate2}");
            Console.WriteLine($"  模板3: {oldTemplate3}");

            // 新设计（推荐）：一个模板，多次执行
            Console.WriteLine("\n新设计（推荐）：");
            var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
            var execution1 = template.Execute(new { id = 1 });
            var execution2 = template.Execute(new { id = 2 });
            var execution3 = template.Execute(new { id = 3 });

            Console.WriteLine($"  只创建1个模板实例（高效）");
            Console.WriteLine($"  模板: {template}");
            Console.WriteLine($"  执行1: {execution1}");
            Console.WriteLine($"  执行2: {execution2}");
            Console.WriteLine($"  执行3: {execution3}");
        }

        /// <summary>
        /// 演示4：复杂查询的模板化
        /// </summary>
        public void DemoComplexQueryTemplating()
        {
            Console.WriteLine("\n=== 演示4：复杂查询的模板化 ===");

            var complexTemplate = SqlTemplate.Parse(@"
                WITH UserStats AS (
                    SELECT 
                        u.Id,
                        u.Name,
                        u.Email,
                        COUNT(o.Id) as OrderCount,
                        SUM(o.TotalAmount) as TotalSpent
                    FROM Users u
                    LEFT JOIN Orders o ON u.Id = o.UserId
                    WHERE u.IsActive = @isActive
                    AND u.CreatedAt >= @startDate
                    GROUP BY u.Id, u.Name, u.Email
                )
                SELECT *
                FROM UserStats
                WHERE OrderCount >= @minOrders
                AND TotalSpent >= @minSpent
                ORDER BY TotalSpent DESC");

            // 不同的分析场景使用同一模板
            var highValueCustomers = complexTemplate.Execute(new 
            { 
                isActive = true,
                startDate = DateTime.Now.AddYears(-1),
                minOrders = 5,
                minSpent = 1000m
            });

            var recentActiveUsers = complexTemplate.Execute(new 
            { 
                isActive = true,
                startDate = DateTime.Now.AddMonths(-3),
                minOrders = 1,
                minSpent = 0m
            });

            Console.WriteLine("高价值客户查询:");
            Console.WriteLine(highValueCustomers.Render());
            
            Console.WriteLine("\n近期活跃用户查询:");
            Console.WriteLine(recentActiveUsers.Render());
        }

        /// <summary>
        /// 演示5：模板缓存和性能优势
        /// </summary>
        public void DemoTemplateCachingPerformance()
        {
            Console.WriteLine("\n=== 演示5：模板缓存和性能优势 ===");

            // 模拟模板缓存
            var templateCache = new Dictionary<string, SqlTemplate>();

            string GetTemplate(string key, string sql)
            {
                if (!templateCache.ContainsKey(key))
                {
                    templateCache[key] = SqlTemplate.Parse(sql);
                    Console.WriteLine($"  缓存新模板: {key}");
                }
                return $"使用缓存模板: {key}";
            }

            // 多次请求相同的模板
            for (int i = 0; i < 5; i++)
            {
                var info = GetTemplate("user_by_id", "SELECT * FROM Users WHERE Id = @id");
                Console.WriteLine($"  请求{i + 1}: {info}");
            }

            Console.WriteLine($"\n缓存统计: 总共缓存了 {templateCache.Count} 个模板");
            
            // 展示重用
            var template = templateCache["user_by_id"];
            for (int i = 1; i <= 3; i++)
            {
                var execution = template.Execute(new { id = i });
                Console.WriteLine($"  执行{i}: {execution.Render()}");
            }
        }

        /// <summary>
        /// 运行所有演示
        /// </summary>
        public void RunAllDemos()
        {
            try
            {
                DemoPureTemplateReuse();
                DemoFluentParameterBinding();
                DemoOldVsNewDesign();
                DemoComplexQueryTemplating();
                DemoTemplateCachingPerformance();

                Console.WriteLine("\n=== 总结 ===");
                Console.WriteLine("新的SqlTemplate设计优势：");
                Console.WriteLine("1. 概念清晰：模板是模板，参数是参数");
                Console.WriteLine("2. 高性能：模板可缓存，避免重复解析");
                Console.WriteLine("3. 内存效率：同一模板可重用，减少对象创建");
                Console.WriteLine("4. 类型安全：编译时检查，AOT友好");
                Console.WriteLine("5. 流式API：直观的参数绑定体验");
                Console.WriteLine("6. 向后兼容：旧代码仍可工作，渐进式升级");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示运行出错: {ex.Message}");
            }
        }
    }
}
