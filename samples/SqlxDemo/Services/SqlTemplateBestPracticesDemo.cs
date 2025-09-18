// -----------------------------------------------------------------------
// <copyright file="SqlTemplateBestPracticesDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx;
using SqlxDemo.Models;

namespace SqlxDemo.Services
{
    /// <summary>
    /// SqlTemplate最佳实践演示 - 展示正确的API使用方式
    /// 遵循"模板是模板，参数是参数"的设计原则
    /// </summary>
    public class SqlTemplateBestPracticesDemo
    {
        /// <summary>
        /// ✅ 正确示例1：纯模板定义和重用
        /// </summary>
        public void CorrectExample_PureTemplateReuse()
        {
            Console.WriteLine("=== ✅ 正确示例1：纯模板定义和重用 ===");

            // ✅ 正确：创建纯模板定义
            var userQueryTemplate = SqlTemplate.Parse(@"
                SELECT Id, Name, Email, IsActive 
                FROM Users 
                WHERE IsActive = @isActive 
                AND Age > @minAge");

            Console.WriteLine($"模板定义: {userQueryTemplate.Sql}");
            Console.WriteLine($"是否为纯模板: {userQueryTemplate.IsPureTemplate}");

            // ✅ 正确：重复使用同一模板，绑定不同参数
            var activeUsers = userQueryTemplate.Execute(new { isActive = true, minAge = 18 });
            var seniorUsers = userQueryTemplate.Execute(new { isActive = true, minAge = 65 });
            var inactiveUsers = userQueryTemplate.Execute(new { isActive = false, minAge = 0 });

            Console.WriteLine($"活跃用户查询: {activeUsers.Render()}");
            Console.WriteLine($"老年用户查询: {seniorUsers.Render()}");
            Console.WriteLine($"非活跃用户查询: {inactiveUsers.Render()}");
        }

        /// <summary>
        /// ❌ 错误示例：混合模板定义和参数值
        /// </summary>
        public void IncorrectExample_MixedTemplateAndParameters()
        {
            Console.WriteLine("\n=== ❌ 错误示例：混合模板定义和参数值 ===");

            #pragma warning disable CS0618 // 忽略过时警告，演示目的
            // ❌ 错误：每次都创建新模板，混合了模板定义和参数值
            var template1 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
            var template2 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 2 });
            var template3 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 3 });
            #pragma warning restore CS0618

            Console.WriteLine("❌ 问题：创建了3个重复的模板对象");
            Console.WriteLine($"  模板1: {template1}");
            Console.WriteLine($"  模板2: {template2}");
            Console.WriteLine($"  模板3: {template3}");
            Console.WriteLine("❌ 浪费内存、降低性能、概念混乱");
        }

        /// <summary>
        /// ✅ 正确示例2：流式参数绑定
        /// </summary>
        public void CorrectExample_FluentParameterBinding()
        {
            Console.WriteLine("\n=== ✅ 正确示例2：流式参数绑定 ===");

            var complexQueryTemplate = SqlTemplate.Parse(@"
                SELECT u.*, d.Name as DepartmentName
                FROM Users u
                INNER JOIN Departments d ON u.DepartmentId = d.Id
                WHERE u.IsActive = @isActive
                AND u.Age BETWEEN @minAge AND @maxAge
                AND u.Salary >= @minSalary
                AND u.HireDate >= @hireDate
                ORDER BY u.Salary DESC");

            // ✅ 正确：使用流式API绑定多个参数
            var execution = complexQueryTemplate.Bind()
                .Param("isActive", true)
                .Param("minAge", 25)
                .Param("maxAge", 65)
                .Param("minSalary", 5000m)
                .Param("hireDate", DateTime.Now.AddYears(-5))
                .Build();

            Console.WriteLine($"复杂查询: {execution.Render()}");
        }

        /// <summary>
        /// ✅ 正确示例3：模板缓存和高性能重用
        /// </summary>
        public void CorrectExample_TemplateCaching()
        {
            Console.WriteLine("\n=== ✅ 正确示例3：模板缓存和高性能重用 ===");

            // ✅ 正确：模拟生产环境的模板缓存
            var templateCache = new Dictionary<string, SqlTemplate>();

            SqlTemplate GetCachedTemplate(string key, string sql)
            {
                if (!templateCache.ContainsKey(key))
                {
                    templateCache[key] = SqlTemplate.Parse(sql);
                    Console.WriteLine($"  ✅ 缓存新模板: {key}");
                }
                else
                {
                    Console.WriteLine($"  ✅ 使用缓存模板: {key}");
                }
                return templateCache[key];
            }

            // 模拟多次请求同一模板
            for (int i = 0; i < 5; i++)
            {
                var template = GetCachedTemplate("user_by_status", 
                    "SELECT * FROM Users WHERE IsActive = @isActive AND DepartmentId = @deptId");
                
                var execution = template.Execute(new { isActive = true, deptId = i % 3 + 1 });
                Console.WriteLine($"    请求{i + 1}: {execution.Render()}");
            }

            Console.WriteLine($"✅ 性能统计: 只创建了 {templateCache.Count} 个模板，处理了 5 次请求");
        }

        /// <summary>
        /// ✅ 正确示例4：类型安全的参数绑定
        /// </summary>
        public void CorrectExample_TypeSafeParameters()
        {
            Console.WriteLine("\n=== ✅ 正确示例4：类型安全的参数绑定 ===");

            var template = SqlTemplate.Parse(@"
                SELECT * FROM Orders 
                WHERE CustomerId = @customerId 
                AND OrderDate >= @startDate 
                AND TotalAmount >= @minAmount 
                AND Status = @status");

            // ✅ 正确：使用强类型参数对象
            var searchCriteria = new
            {
                customerId = 12345,
                startDate = DateTime.Now.AddMonths(-6),
                minAmount = 100.00m,
                status = "Completed"
            };

            var execution = template.Execute(searchCriteria);
            Console.WriteLine($"订单查询: {execution.Render()}");

            // ✅ 正确：使用字典参数（AOT友好）
            var dictParams = new Dictionary<string, object>
            {
                ["customerId"] = 67890,
                ["startDate"] = DateTime.Now.AddMonths(-3),
                ["minAmount"] = 200.00m,
                ["status"] = "Pending"
            };

            var execution2 = template.Execute((Dictionary<string, object>)dictParams);
            Console.WriteLine($"订单查询2: {execution2.Render()}");
        }

        /// <summary>
        /// ✅ 正确示例5：与ParameterizedSql的配合使用
        /// </summary>
        public void CorrectExample_ParameterizedSqlIntegration()
        {
            Console.WriteLine("\n=== ✅ 正确示例5：与ParameterizedSql的配合使用 ===");

            // 场景1：直接创建ParameterizedSql（一次性使用）
            var oneTimeQuery = ParameterizedSql.Create(
                "SELECT COUNT(*) FROM Users WHERE CreatedAt >= @date",
                new { date = DateTime.Now.AddMonths(-1) });

            Console.WriteLine($"一次性查询: {oneTimeQuery.Render()}");

            // 场景2：从SqlTemplate创建ParameterizedSql（可重用）
            var reusableTemplate = SqlTemplate.Parse("SELECT * FROM Products WHERE CategoryId = @categoryId");
            
            var electronics = reusableTemplate.Execute(new { categoryId = 1 });
            var books = reusableTemplate.Execute(new { categoryId = 2 });
            var clothing = reusableTemplate.Execute(new { categoryId = 3 });

            Console.WriteLine($"电子产品: {electronics.Render()}");
            Console.WriteLine($"图书: {books.Render()}");
            Console.WriteLine($"服装: {clothing.Render()}");
        }

        /// <summary>
        /// ✅ 正确示例6：复杂业务场景的模板设计
        /// </summary>
        public void CorrectExample_ComplexBusinessScenario()
        {
            Console.WriteLine("\n=== ✅ 正确示例6：复杂业务场景的模板设计 ===");

            // 定义业务查询模板
            var reportTemplate = SqlTemplate.Parse(@"
                WITH UserStats AS (
                    SELECT 
                        u.Id,
                        u.Name,
                        u.DepartmentId,
                        COUNT(DISTINCT o.Id) as OrderCount,
                        SUM(o.TotalAmount) as TotalRevenue,
                        AVG(o.TotalAmount) as AvgOrderValue
                    FROM Users u
                    LEFT JOIN Orders o ON u.Id = o.UserId
                    WHERE u.IsActive = @isActive
                    AND u.HireDate >= @startDate
                    GROUP BY u.Id, u.Name, u.DepartmentId
                ),
                DepartmentStats AS (
                    SELECT 
                        d.Id as DepartmentId,
                        d.Name as DepartmentName,
                        COUNT(us.Id) as UserCount,
                        SUM(us.TotalRevenue) as DeptRevenue
                    FROM Departments d
                    LEFT JOIN UserStats us ON d.Id = us.DepartmentId
                    GROUP BY d.Id, d.Name
                )
                SELECT *
                FROM DepartmentStats
                WHERE DeptRevenue >= @minRevenue
                ORDER BY DeptRevenue DESC");

            // 不同的报表需求使用同一模板
            var yearlyReport = reportTemplate.Execute(new 
            { 
                isActive = true,
                startDate = DateTime.Now.AddYears(-1),
                minRevenue = 50000m
            });

            var quarterlyReport = reportTemplate.Execute(new 
            { 
                isActive = true,
                startDate = DateTime.Now.AddMonths(-3),
                minRevenue = 10000m
            });

            Console.WriteLine("年度报表查询:");
            Console.WriteLine(yearlyReport.Render());
            
            Console.WriteLine("\n季度报表查询:");
            Console.WriteLine(quarterlyReport.Render());
        }

        /// <summary>
        /// 运行所有最佳实践演示
        /// </summary>
        public void RunAllExamples()
        {
            try
            {
                CorrectExample_PureTemplateReuse();
                IncorrectExample_MixedTemplateAndParameters();
                CorrectExample_FluentParameterBinding();
                CorrectExample_TemplateCaching();
                CorrectExample_TypeSafeParameters();
                CorrectExample_ParameterizedSqlIntegration();
                CorrectExample_ComplexBusinessScenario();

                Console.WriteLine("\n=== 📋 最佳实践总结 ===");
                Console.WriteLine("✅ DO: 使用 SqlTemplate.Parse() 创建纯模板定义");
                Console.WriteLine("✅ DO: 使用 template.Execute() 绑定参数创建执行实例");
                Console.WriteLine("✅ DO: 重复使用同一模板，提高性能");
                Console.WriteLine("✅ DO: 使用模板缓存在生产环境中");
                Console.WriteLine("✅ DO: 使用流式API进行复杂参数绑定");
                Console.WriteLine("✅ DO: 优先使用Dictionary<string, object?>以获得AOT兼容性");
                Console.WriteLine("");
                Console.WriteLine("❌ DON'T: 使用 SqlTemplate.Create() 混合模板和参数");
                Console.WriteLine("❌ DON'T: 为相同SQL重复创建模板实例");
                Console.WriteLine("❌ DON'T: 在模板定义中包含具体参数值");
                Console.WriteLine("");
                Console.WriteLine("🎯 核心原则: SqlTemplate = 纯模板定义, ParameterizedSql = 执行实例");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示运行出错: {ex.Message}");
            }
        }
    }
}
