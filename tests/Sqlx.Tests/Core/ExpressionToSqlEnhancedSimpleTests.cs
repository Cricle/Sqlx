// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlEnhancedSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 简化的表达式解析增强功能测试
    /// </summary>
    [TestClass]
    public class ExpressionToSqlEnhancedSimpleTests
    {
        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Salary { get; set; }
            public decimal? Bonus { get; set; }
            public int DepartmentId { get; set; }
            public bool IsActive { get; set; }
            public DateTime HireDate { get; set; }
        }

        public class TestUserResult
        {
            public int DepartmentId { get; set; }
            public int Count { get; set; }
            public decimal TotalSalary { get; set; }
            public double AvgSalary { get; set; }
        }

        #region GroupBy聚合函数嵌套函数测试

        [TestMethod]
        public void GroupBy_NestedFunctions_BasicTest()
        {
            // Arrange & Act - 测试聚合函数中使用算术运算
            var groupQuery = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(u => u.IsActive)
                .GroupBy(u => u.DepartmentId);

            var resultQuery = groupQuery.Select<TestUserResult>(g => new TestUserResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => e.Salary * 1.2m), // 算术运算
                AvgSalary = g.Average(e => e.Salary + (e.Bonus ?? 0)) // 空值合并 + 算术运算
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"嵌套函数基础测试SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM(([Salary] * 1.2))"), "应包含算术运算");
            Assert.IsTrue(sql.Contains("AVG(([Salary] + COALESCE([Bonus], 0)))"), "应包含空值合并和算术运算");
            Assert.IsTrue(sql.Contains("GROUP BY [DepartmentId]"), "应包含GROUP BY子句");
            
            resultQuery.Dispose();
        }

        [TestMethod]
        public void GroupBy_ConditionalExpressions_BasicTest()
        {
            // Arrange & Act - 测试聚合函数中使用条件表达式
            var groupQuery = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(u => u.DepartmentId);

            var resultQuery = groupQuery.Select<TestUserResult>(g => new TestUserResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => e.IsActive ? e.Salary : 0), // 条件表达式
                Count = g.Count()
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"条件表达式基础测试SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM(CASE WHEN [IsActive] THEN [Salary] ELSE 0 END)"), 
                "应包含条件表达式的CASE WHEN语句");
            Assert.IsTrue(sql.Contains("COUNT(*)"), "应包含COUNT聚合函数");
            
            resultQuery.Dispose();
        }

        [TestMethod]
        public void GroupBy_ComplexArithmeticExpressions_BasicTest()
        {
            // Arrange & Act - 测试聚合函数中使用复杂算术表达式
            var groupQuery = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(u => u.DepartmentId);

            var resultQuery = groupQuery.Select<TestUserResult>(g => new TestUserResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => (e.Salary + (e.Bonus ?? 0)) * 1.1m), // 复杂算术表达式
                AvgSalary = g.Average(e => e.Salary / 12) // 除法运算
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"复杂算术表达式基础测试SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM((([Salary] + COALESCE([Bonus], 0)) * 1.1))"), 
                "应包含复杂的算术表达式和空值合并");
            Assert.IsTrue(sql.Contains("AVG(([Salary] / 12))"), 
                "应包含除法运算");
                
            resultQuery.Dispose();
        }

        #endregion

        #region 性能优化测试

        [TestMethod]
        public void Performance_CachedExpressionParsing_IsEfficient()
        {
            // Arrange
            const int iterations = 50;
            var stopwatch = Stopwatch.StartNew();

            // Act - 测试表达式缓存性能
            for (int i = 0; i < iterations; i++)
            {
                using var query = ExpressionToSql<TestUser>.ForSqlServer()
                    .Where(e => e.Salary > 50000)
                    .Where(e => e.IsActive && e.Name.Contains("test"))
                    .OrderBy(e => e.HireDate);
                
                var sql = query.ToSql();
                Assert.IsNotNull(sql);
            }

            stopwatch.Stop();

            // Assert
            Console.WriteLine($"缓存优化性能测试: {iterations}次解析耗时 {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"缓存优化后的表达式解析应该很快: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        }

        [TestMethod]
        public void Performance_ComplexGroupByExpressions_IsEfficient()
        {
            // Arrange
            const int iterations = 20;
            var stopwatch = Stopwatch.StartNew();

            // Act - 测试复杂GroupBy表达式的性能
            for (int i = 0; i < iterations; i++)
            {
                var groupQuery = ExpressionToSql<TestUser>.ForSqlServer()
                    .Where(e => e.IsActive)
                    .GroupBy(e => e.DepartmentId);

                var resultQuery = groupQuery.Select<TestUserResult>(g => new TestUserResult
                {
                    DepartmentId = g.Key,
                    TotalSalary = g.Sum(e => (e.Salary + (e.Bonus ?? 0)) * 1.1m),
                    AvgSalary = g.Average(e => e.IsActive ? e.Salary : 0),
                    Count = g.Count()
                });

                var sql = resultQuery.ToSql();
                Assert.IsNotNull(sql);
                resultQuery.Dispose();
            }

            stopwatch.Stop();

            // Assert
            Console.WriteLine($"复杂GroupBy表达式性能测试: {iterations}次解析耗时 {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"复杂GroupBy表达式解析应该保持高效: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        }

        #endregion

        #region 多数据库方言测试

        [TestMethod]
        public void GroupBy_NestedFunctions_AllDialects()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<TestUser>.ForSqlServer()),
                ("MySQL", ExpressionToSql<TestUser>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<TestUser>.ForPostgreSQL()),
                ("SQLite", ExpressionToSql<TestUser>.ForSqlite())
            };

            foreach (var (dialectName, expr) in testCases)
            {
                using (expr)
                {
                    var groupQuery = expr.GroupBy(e => e.DepartmentId);
                    var resultQuery = groupQuery.Select<TestUserResult>(g => new TestUserResult
                    {
                        DepartmentId = g.Key,
                        TotalSalary = g.Sum(e => e.Salary * 1.2m),
                        AvgSalary = g.Average(e => e.Bonus ?? 0)
                    });

                    var sql = resultQuery.ToSql();
                    Console.WriteLine($"{dialectName} 嵌套函数SQL: {sql}");
                    
                    Assert.IsTrue(sql.Contains("SUM") && sql.Contains("AVG"), 
                        $"{dialectName}: 应包含SUM和AVG聚合函数");
                    Assert.IsTrue(sql.Contains("COALESCE") && sql.Contains("Bonus") && sql.Contains("0"), 
                        $"{dialectName}: 应包含空值合并处理");
                    
                    resultQuery.Dispose();
                }
            }
        }

        #endregion

        #region 缓存清理测试

        [TestMethod]
        public void Cache_ClearGlobalCache_WorksCorrectly()
        {
            // Arrange - 先创建一些缓存项
            for (int i = 0; i < 5; i++)
            {
                using var query = ExpressionToSql<TestUser>.ForSqlServer()
                    .Where(e => e.Salary > i * 1000);
                query.ToSql();
            }

            // Act - 清理缓存
            ExpressionToSqlBase.ClearGlobalCache();

            // Assert - 验证缓存清理后仍能正常工作
            using var testQuery = ExpressionToSql<TestUser>.ForSqlServer()
                .Where(e => e.IsActive);
            var sql = testQuery.ToSql();
            
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("IsActive"));
            Console.WriteLine("✅ 缓存清理功能正常工作");
        }

        #endregion

        #region 错误处理测试

        [TestMethod]
        public void GroupBy_NullHandling_GeneratesCorrectSql()
        {
            // Arrange & Act - 测试NULL值处理
            var groupQuery = ExpressionToSql<TestUser>.ForSqlServer()
                .GroupBy(e => e.DepartmentId);

            var resultQuery = groupQuery.Select<TestUserResult>(g => new TestUserResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => e.Bonus ?? 0), // NULL合并
                AvgSalary = g.Average(e => e.Bonus ?? e.Salary), // 复杂NULL合并
                Count = g.Count()
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"NULL处理SQL: {sql}");
            Assert.IsTrue(sql.Contains("COALESCE([Bonus], 0)"), 
                "应包含简单的NULL合并");
            Assert.IsTrue(sql.Contains("COALESCE([Bonus], [Salary])"), 
                "应包含复杂的NULL合并");
                
            resultQuery.Dispose();
        }

        #endregion
    }
}
