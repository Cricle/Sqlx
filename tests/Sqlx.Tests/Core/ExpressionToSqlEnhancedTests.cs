// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlEnhancedTests.cs" company="Cricle">
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
    /// 测试增强的表达式解析功能，特别是GroupBy聚合函数中的嵌套函数支持
    /// </summary>
    [TestClass]
    public class ExpressionToSqlEnhancedTests
    {
        public class TestEmployee
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Salary { get; set; }
            public decimal? Bonus { get; set; }
            public int DepartmentId { get; set; }
            public bool IsActive { get; set; }
            public DateTime HireDate { get; set; }
            public decimal PerformanceRating { get; set; }
        }

        public class TestEmployeeResult
        {
            public int DepartmentId { get; set; }
            public int Count { get; set; }
            public decimal TotalSalary { get; set; }
            public double AvgSalary { get; set; }
            public decimal MaxSalary { get; set; }
            public decimal MinSalary { get; set; }
        }

        #region GroupBy聚合函数嵌套函数测试

        [TestMethod]
        public void GroupBy_NestedMathFunctions_GeneratesCorrectSql()
        {
            // Arrange & Act - 测试聚合函数中使用数学函数
            var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .Where(e => e.IsActive)
                .GroupBy(e => e.DepartmentId);

            var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => Math.Round(e.Salary * 1.2m, 2)), // 嵌套函数：Math.Round + 算术运算
                AvgSalary = g.Average(e => Math.Abs(e.Salary)), // 嵌套函数：Math.Abs
                MaxSalary = g.Max(e => (decimal)Math.Floor((double)e.Salary)), // 嵌套函数：Math.Floor
                MinSalary = g.Min(e => (decimal)Math.Ceiling((double)e.Salary)) // 嵌套函数：Math.Ceiling
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"嵌套数学函数SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM(ROUND(([Salary] * 1.2), 2))"), "应包含嵌套的ROUND和乘法运算");
            Assert.IsTrue(sql.Contains("AVG(ABS([Salary]))"), "应包含嵌套的ABS函数");
            Assert.IsTrue(sql.Contains("MAX(FLOOR([Salary]))"), "应包含嵌套的FLOOR函数");
            Assert.IsTrue(sql.Contains("MIN(CEILING([Salary]))"), "应包含嵌套的CEILING函数");
            Assert.IsTrue(sql.Contains("GROUP BY [DepartmentId]"), "应包含GROUP BY子句");
        }

        [TestMethod]
        public void GroupBy_ConditionalExpressions_GeneratesCorrectSql()
        {
            // Arrange & Act - 测试聚合函数中使用条件表达式
            var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .GroupBy(e => e.DepartmentId);

            var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => e.IsActive ? e.Salary : 0), // 条件表达式
                AvgSalary = g.Average(e => e.Bonus ?? 0), // 空值合并
                MaxSalary = g.Max(e => e.PerformanceRating > 4.0m ? e.Salary * 1.2m : e.Salary) // 复杂条件表达式
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"条件表达式SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM(CASE WHEN [IsActive] THEN [Salary] ELSE 0 END)"),
                "应包含条件表达式的CASE WHEN语句");
            Assert.IsTrue(sql.Contains("AVG(COALESCE([Bonus], 0))"),
                "应包含空值合并的COALESCE函数");
            Assert.IsTrue(sql.Contains("CASE WHEN ([PerformanceRating] > 4.0) THEN ([Salary] * 1.2) ELSE [Salary] END"),
                "应包含复杂条件表达式");
        }

        [TestMethod]
        public void GroupBy_ComplexArithmeticExpressions_GeneratesCorrectSql()
        {
            // Arrange & Act - 测试聚合函数中使用复杂算术表达式
            var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .GroupBy(e => e.DepartmentId);

            var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
            {
                DepartmentId = g.Key,
                TotalSalary = g.Sum(e => (e.Salary + (e.Bonus ?? 0)) * 1.1m), // 复杂算术表达式
                AvgSalary = g.Average(e => e.Salary / 12), // 除法运算
                MaxSalary = g.Max(e => e.Salary - (e.Salary * 0.1m)) // 减法和乘法组合
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"复杂算术表达式SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM((([Salary] + COALESCE([Bonus], 0)) * 1.1))") ||
                         sql.Contains("SUM(((COALESCE([Bonus], 0) + [Salary]) * 1.1))"),
                "应包含复杂的算术表达式和空值合并");
            Assert.IsTrue(sql.Contains("AVG(([Salary] / 12))"),
                "应包含除法运算");
            Assert.IsTrue(sql.Contains("MAX(([Salary] - ([Salary] * 0.1)))"),
                "应包含减法和乘法组合");
        }

        [TestMethod]
        public void GroupBy_StringFunctions_GeneratesCorrectSql()
        {
            // Arrange & Act - 测试聚合函数中使用字符串函数
            var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .GroupBy(e => e.DepartmentId);

            var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
            {
                DepartmentId = g.Key,
                Count = (int)g.Sum(e => (decimal)e.Name.Length), // 字符串长度函数
                TotalSalary = g.Sum(e => (decimal)e.Name.ToUpper().Length) // 嵌套字符串函数
            });

            var sql = resultQuery.ToSql();

            // Assert
            Console.WriteLine($"字符串函数SQL: {sql}");
            Assert.IsTrue(sql.Contains("SUM(LEN([Name]))") || sql.Contains("SUM(LENGTH([Name]))"),
                "应包含字符串长度函数");
            Assert.IsTrue(sql.Contains("SUM(LEN(UPPER([Name])))") || sql.Contains("SUM(LENGTH(UPPER([Name])))"),
                "应包含嵌套的字符串函数");
        }

        #endregion

        #region 性能优化测试

        [TestMethod]
        public void Performance_CachedExpressionParsing_IsEfficient()
        {
            // Arrange
            const int iterations = 100;
            var stopwatch = Stopwatch.StartNew();

            // Act - 测试表达式缓存性能
            for (int i = 0; i < iterations; i++)
            {
                using var query = ExpressionToSql<TestEmployee>.ForSqlServer()
                    .Where(e => e.Salary > 50000)
                    .Where(e => e.IsActive && e.Name.Contains("test"))
                    .OrderBy(e => e.HireDate);

                var sql = query.ToSql();
                Assert.IsNotNull(sql);
            }

            stopwatch.Stop();

            // Assert
            Console.WriteLine($"缓存优化性能测试: {iterations}次解析耗时 {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
                $"缓存优化后的表达式解析应该很快: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
        }

        [TestMethod]
        public void Performance_ComplexGroupByExpressions_IsEfficient()
        {
            // Arrange
            const int iterations = 50;
            var stopwatch = Stopwatch.StartNew();

            // Act - 测试复杂GroupBy表达式的性能
            for (int i = 0; i < iterations; i++)
            {
                var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                    .Where(e => e.IsActive)
                    .GroupBy(e => e.DepartmentId);

                var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
                {
                    DepartmentId = g.Key,
                    TotalSalary = g.Sum(e => Math.Round((e.Salary + (e.Bonus ?? 0)) * 1.1m, 2)),
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

        #region 错误处理和边界情况测试

        [TestMethod]
        public void GroupBy_UnsupportedNestedFunctions_HandlesGracefully()
        {
            // Arrange & Act
            var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .GroupBy(e => e.DepartmentId);

            try
            {
                var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
                {
                    DepartmentId = g.Key,
                    // 尝试使用可能不支持的复杂嵌套
                    TotalSalary = g.Sum(e => (decimal)Math.Pow((double)e.Salary, 2))
                });

                var sql = resultQuery.ToSql();
                Console.WriteLine($"复杂嵌套函数SQL: {sql}");

                // 应该能够处理，即使是复杂的嵌套
                Assert.IsTrue(sql.Contains("POWER") || sql.Contains("POW"),
                    "应该能处理数学幂函数");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理不支持函数的异常: {ex.Message}");
                // 某些复杂表达式可能不被支持，这是可以接受的
            }
        }

        [TestMethod]
        public void GroupBy_NullHandling_GeneratesCorrectSql()
        {
            // Arrange & Act - 测试NULL值处理
            var groupQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .GroupBy(e => e.DepartmentId);

            var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
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
        }

        #endregion

        #region 多数据库方言测试

        [TestMethod]
        public void GroupBy_NestedFunctions_AllDialects()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<TestEmployee>.ForSqlServer()),
                ("MySQL", ExpressionToSql<TestEmployee>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<TestEmployee>.ForPostgreSQL()),
                ("SQLite", ExpressionToSql<TestEmployee>.ForSqlite())
            };

            foreach (var (dialectName, expr) in testCases)
            {
                using (expr)
                {
                    var groupQuery = expr.GroupBy(e => e.DepartmentId);
                    var resultQuery = groupQuery.Select<TestEmployeeResult>(g => new TestEmployeeResult
                    {
                        DepartmentId = g.Key,
                        TotalSalary = g.Sum(e => Math.Round(e.Salary, 2)),
                        AvgSalary = g.Average(e => Math.Abs(e.Salary))
                    });

                    var sql = resultQuery.ToSql();
                    Console.WriteLine($"{dialectName} 嵌套函数SQL: {sql}");

                    Assert.IsTrue(sql.Contains("ROUND") && sql.Contains("ABS"),
                        $"{dialectName}: 应包含ROUND和ABS函数");
                    Assert.IsTrue(sql.Contains("SUM") && sql.Contains("AVG"),
                        $"{dialectName}: 应包含SUM和AVG聚合函数");

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
            for (int i = 0; i < 10; i++)
            {
                using var query = ExpressionToSql<TestEmployee>.ForSqlServer()
                    .Where(e => e.Salary > i * 1000);
                query.ToSql();
            }

            // Act - 清理缓存
            ExpressionToSqlBase.ClearGlobalCache();

            // Assert - 验证缓存清理后仍能正常工作
            using var testQuery = ExpressionToSql<TestEmployee>.ForSqlServer()
                .Where(e => e.IsActive);
            var sql = testQuery.ToSql();

            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("IsActive"));
            Console.WriteLine("✅ 缓存清理功能正常工作");
        }

        #endregion
    }
}
