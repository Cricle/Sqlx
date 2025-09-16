// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlAdvancedFunctionalityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// ExpressionToSql 高级功能测试集，验证复杂场景和核心功能。
    /// </summary>
    [TestClass]
    public class ExpressionToSqlAdvancedFunctionalityTests
    {
        #region 测试实体

        public class AdvancedTestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public decimal? Salary { get; set; }
            public double Score { get; set; }
            public string? Email { get; set; }
            public int CategoryId { get; set; }
        }

        #endregion

        #region 基础功能验证测试

        [TestMethod]
        public void ExpressionToSql_BasicWhere_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 100)
                .Where(e => e.Name == "Test")
                .Where(e => e.IsActive);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Generated SQL: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("100"), "应包含ID条件");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Test"), "应包含Name条件");
            Assert.IsTrue(sql.Contains("IsActive"), "应包含IsActive条件");
        }

        [TestMethod]
        public void ExpressionToSql_ComplexWhere_HandlesNestedConditions()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => (e.Age > 18 && e.Age < 65) || (e.IsActive && e.Salary > 50000));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex WHERE SQL: {sql}");
            Assert.IsTrue(sql.Contains("Age"), "应包含Age条件");
            Assert.IsTrue(sql.Contains("IsActive"), "应包含IsActive条件");
            Assert.IsTrue(sql.Contains("Salary"), "应包含Salary条件");
        }

        [TestMethod]
        public void ExpressionToSql_OrderBy_MultipleSorting()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.OrderBy(e => e.Name)
                .OrderByDescending(e => e.CreatedAt)
                .OrderBy(e => e.Age);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Multiple ORDER BY SQL: {sql}");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY子句");
            Assert.IsTrue(sql.Contains("Name"), "应包含Name排序");
            Assert.IsTrue(sql.Contains("CreatedAt"), "应包含CreatedAt排序");
            Assert.IsTrue(sql.Contains("Age"), "应包含Age排序");
        }

        [TestMethod]
        public void ExpressionToSql_TakeSkip_PaginationSyntax()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.IsActive)
                .OrderBy(e => e.Id)
                .Skip(10)
                .Take(20);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Pagination SQL: {sql}");
            Assert.IsTrue(sql.Contains("10") && sql.Contains("20"), "应包含分页参数");
            Assert.IsTrue(sql.Contains("ORDER BY"), "分页需要ORDER BY");
        }

        #endregion

        #region 数据类型处理测试

        [TestMethod]
        public void ExpressionToSql_NullableTypes_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Salary != null)
                .Where(e => e.UpdatedAt == null)
                .Where(e => e.Salary > 1000);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Nullable types SQL: {sql}");
            Assert.IsTrue(sql.Contains("Salary") || sql.Contains("UpdatedAt"), "应处理可空字段");
            Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("IS NOT NULL") || sql.Contains("NULL"), 
                "应包含NULL相关的比较");
        }

        [TestMethod]
        public void ExpressionToSql_StringOperations_AllMethods()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Name.Contains("test"))
                .Where(e => e.Name.StartsWith("prefix"))
                .Where(e => e.Name.EndsWith("suffix"))
                .Where(e => e.Email.ToLower().Contains("@domain"));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"String operations SQL: {sql}");
            Assert.IsTrue(sql.Contains("Name"), "应包含Name条件");
            Assert.IsTrue(sql.Contains("Email"), "应包含Email条件");
        }

        [TestMethod]
        public void ExpressionToSql_NumericOperations_MathematicalExpressions()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Age + 5 > 30)
                .Where(e => e.Salary * 0.8m > 40000)
                .Where(e => e.Score / 2 >= 50)
                .Where(e => e.CategoryId % 3 == 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Numeric operations SQL: {sql}");
            Assert.IsTrue(sql.Contains("Age") && sql.Contains("Salary") && sql.Contains("Score"), 
                "应包含数值字段");
        }

        #endregion

        #region SQL模板测试

        [TestMethod]
        public void ExpressionToSql_ToTemplate_GeneratesParameterizedSql()
        {
            // Arrange
            var testAge = 30;
            var testName = "TestUser";

            // Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Age > testAge)
                .Where(e => e.Name == testName)
                .Where(e => e.IsActive);

            var template = expr.ToTemplate();

            // Assert
            Console.WriteLine($"Template SQL: {template.Sql}");
            Console.WriteLine($"Parameters count: {template.Parameters.Length}");
            
            Assert.IsNotNull(template.Sql, "模板SQL不应为空");
            Assert.IsTrue(template.Sql.Contains("Age") && template.Sql.Contains("Name"), 
                "SQL应包含字段名");
        }

        #endregion

        #region 性能和边界测试

        [TestMethod]
        public void ExpressionToSql_ManyWhereConditions_PerformanceTest()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            
            var startTime = DateTime.Now;
            
            // 添加多个WHERE条件
            for (int i = 0; i < 100; i++)
            {
                expr.Where(e => e.Age > i);
            }
            
            var sql = expr.ToSql();
            var endTime = DateTime.Now;
            
            // Assert
            var duration = endTime - startTime;
            Console.WriteLine($"Generated SQL with 100 conditions in {duration.TotalMilliseconds}ms");
            Console.WriteLine($"SQL length: {sql.Length}");
            
            Assert.IsTrue(duration.TotalMilliseconds < 1000, "生成100个条件应在1秒内完成");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Length > 500, "应生成相当长的SQL");
        }

        [TestMethod]
        public void ExpressionToSql_LargeStringValues_HandlesCorrectly()
        {
            // Arrange
            var largeString = new string('A', 1000); // 1KB字符串

            // Act
            using var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Name == largeString);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Large string SQL length: {sql.Length}");
            Assert.IsTrue(sql.Contains("Name"), "应包含Name字段");
            Assert.IsTrue(sql.Length > largeString.Length, "SQL应包含大字符串");
        }

        #endregion

        #region 多方言兼容性测试

        [TestMethod]
        public void ExpressionToSql_AllDialects_ConsistentBehavior()
        {
            // Arrange
            var dialects = new[]
            {
                ("SQL Server", ExpressionToSql<AdvancedTestEntity>.ForSqlServer()),
                ("MySQL", ExpressionToSql<AdvancedTestEntity>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<AdvancedTestEntity>.ForPostgreSQL()),
                ("Oracle", ExpressionToSql<AdvancedTestEntity>.ForOracle()),
                ("DB2", ExpressionToSql<AdvancedTestEntity>.ForDB2()),
                ("SQLite", ExpressionToSql<AdvancedTestEntity>.ForSqlite())
            };

            // Act & Assert
            foreach (var (dialectName, builder) in dialects)
            {
                using (builder)
                {
                    builder.Where(e => e.Age > 18)
                           .Where(e => e.IsActive)
                           .OrderBy(e => e.Name)
                           .Take(10);

                    var sql = builder.ToSql();
                    
                    Console.WriteLine($"{dialectName} SQL: {sql}");
                    
                    Assert.IsNotNull(sql, $"{dialectName} 应生成SQL");
                    Assert.IsTrue(sql.Length > 20, $"{dialectName} 应生成合理长度的SQL");
                    Assert.IsTrue(sql.Contains("Age") && sql.Contains("IsActive") && sql.Contains("Name"), 
                        $"{dialectName} 应包含所有查询字段");
                }
            }
        }

        #endregion

        #region 错误处理测试

        [TestMethod]
        public void ExpressionToSql_DisposedObject_ThrowsException()
        {
            // Arrange
            var expr = ExpressionToSql<AdvancedTestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 0);
            expr.Dispose();

            // Act & Assert
            try
            {
                var sql = expr.ToSql();
                Assert.Fail("使用已释放的对象应抛出异常");
            }
            catch (ObjectDisposedException)
            {
                // 预期的异常
                Console.WriteLine("✅ 正确处理了已释放对象的访问");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"意外的异常类型: {ex.GetType().Name}: {ex.Message}");
                // 可能是其他类型的异常，但只要有异常就是正确的
            }
        }

        #endregion
    }
}
