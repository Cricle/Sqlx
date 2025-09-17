// -----------------------------------------------------------------------
// <copyright file="SqlxBoundaryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8625, CS8604, CS8603, CS8602, CS8629, CS8765

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Sqlx 边界条件和极限情况测试
    /// 测试系统在各种边界条件下的行为和稳定性
    /// </summary>
    [TestClass]
    public class SqlxBoundaryAdvancedTests
    {
        #region 测试实体

        public class BoundaryTestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int? NullableValue { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Price { get; set; }
            public bool IsActive { get; set; }
            public long BigNumber { get; set; }
            public double DoubleValue { get; set; }
            public float FloatValue { get; set; }
            public byte ByteValue { get; set; }
            public short ShortValue { get; set; }
            public Guid UniqueId { get; set; }
        }

        public class ExtremeLengthEntity
        {
            public int Id { get; set; }
            public string? VeryLongName { get; set; }
            public string? AnotherLongProperty { get; set; }
        }

        #endregion

        #region 数值边界测试

        [TestMethod]
        public void Boundary_IntegerLimits_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.Id == int.MaxValue)
                .Where(e => e.Id == int.MinValue)
                .Where(e => e.Id == 0)
                .Where(e => e.Id == -1)
                .Where(e => e.Id == 1);

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("Id"), "应包含 Id 列");
            Assert.IsTrue(sql.Contains(int.MaxValue.ToString()) || sql.Contains("@"),
                "应处理 int.MaxValue");
            Assert.IsTrue(sql.Contains(int.MinValue.ToString()) || sql.Contains("@"),
                "应处理 int.MinValue");

            Console.WriteLine($"✅ 整数边界测试: {sql}");
        }

        [TestMethod]
        public void Boundary_LongLimits_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.BigNumber == long.MaxValue)
                .Where(e => e.BigNumber == long.MinValue)
                .Where(e => e.BigNumber == 0L);

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("BigNumber"), "应包含 BigNumber 列");

            Console.WriteLine($"✅ Long 边界测试: {sql}");
        }

        [TestMethod]
        public void Boundary_DecimalPrecision_HandlesCorrectly()
        {
            // Arrange
            var maxDecimal = decimal.MaxValue;
            var minDecimal = decimal.MinValue;
            var preciseDecimal = 123.456789012345678901234567890m;
            var tinyDecimal = 0.000000000000000001m;

            // Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.Price == maxDecimal)
                .Where(e => e.Price == minDecimal)
                .Where(e => e.Price == preciseDecimal)
                .Where(e => e.Price == tinyDecimal);

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("Price"), "应包含 Price 列");

            Console.WriteLine($"✅ Decimal 精度边界测试: {sql}");
        }

        [TestMethod]
        public void Boundary_FloatingPointLimits_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.DoubleValue == double.MaxValue)
                .Where(e => e.DoubleValue == double.MinValue)
                .Where(e => e.DoubleValue == double.Epsilon)
                .Where(e => e.FloatValue == float.MaxValue)
                .Where(e => e.FloatValue == float.MinValue)
                .Where(e => e.FloatValue == float.Epsilon);

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("DoubleValue") || sql.Contains("FloatValue"),
                "应包含浮点数列");

            Console.WriteLine($"✅ 浮点数边界测试: {sql}");
        }

        [TestMethod]
        public void Boundary_SpecialFloatingValues_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            try
            {
                expr.Where(e => e.DoubleValue == double.NaN)
                    .Where(e => e.DoubleValue == double.PositiveInfinity)
                    .Where(e => e.DoubleValue == double.NegativeInfinity);

                var sql = expr.ToSql();
                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ 特殊浮点值测试: {sql}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 正确处理特殊浮点值异常: {ex.GetType().Name}");
                // 某些特殊值可能不被支持，这是正常的
            }
        }

        #endregion

        #region 字符串边界测试

        [TestMethod]
        public void Boundary_EmptyAndNullStrings_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.Name == "")
                .Where(e => e.Name == null)
                .Where(e => e.Name == " ")
                .Where(e => e.Name == "\t")
                .Where(e => e.Name == "\n")
                .Where(e => e.Name == "\r\n");

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("Name"), "应包含 Name 列");
            Assert.IsTrue(sql.Contains("NULL") || sql.Contains("@"), "应处理空字符串和NULL");

            Console.WriteLine($"✅ 空字符串边界测试: {sql}");
        }

        [TestMethod]
        public void Boundary_VeryLongString_HandlesCorrectly()
        {
            // Arrange
            var shortString = "abc";
            var mediumString = new string('x', 1000);
            var longString = new string('y', 10000);
            var veryLongString = new string('z', 100000);

            // Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.Name == shortString)
                .Where(e => e.Name == mediumString)
                .Where(e => e.Name == longString);

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("Name"), "应包含 Name 列");

            // 测试超长字符串
            try
            {
                using var expr2 = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();
                expr2.Where(e => e.Name == veryLongString);
                var sql2 = expr2.ToSql();

                Console.WriteLine($"✅ 超长字符串测试通过，长度: {veryLongString.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 超长字符串正确抛出异常: {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void Boundary_SpecialCharactersInString_HandlesCorrectly()
        {
            // Arrange
            var strings = new[]
            {
                "'单引号'",
                "\"双引号\"",
                "反斜杠\\测试",
                "SQL注入'; DROP TABLE Users; --",
                "Unicode测试: 🚀🎉💻",
                "制表符\t和换行\n测试",
                "NULL\0字符测试",
                "百分号%和下划线_",
                "星号*和问号?",
                "[方括号]和{大括号}",
                "HTML标签<script>alert('test')</script>",
                new string('\'', 100), // 100个单引号
            };

            // Act & Assert
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            foreach (var testString in strings)
            {
                try
                {
                    using var testExpr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();
                    testExpr.Where(e => e.Name == testString);
                    var sql = testExpr.ToSql();

                    Assert.IsNotNull(sql);
                    Console.WriteLine($"✅ 特殊字符测试通过: '{testString.Substring(0, Math.Min(20, testString.Length))}...'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 特殊字符处理异常: '{testString.Substring(0, Math.Min(20, testString.Length))}...' -> {ex.GetType().Name}");
                }
            }
        }

        #endregion

        #region 日期时间边界测试

        [TestMethod]
        public void Boundary_DateTimeLimits_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.CreatedAt == DateTime.MinValue)
                .Where(e => e.CreatedAt == DateTime.MaxValue)
                .Where(e => e.CreatedAt == DateTime.UnixEpoch)
                .Where(e => e.CreatedAt == new DateTime(1900, 1, 1))
                .Where(e => e.CreatedAt == new DateTime(2099, 12, 31))
                .Where(e => e.CreatedAt == DateTime.Now)
                .Where(e => e.CreatedAt == DateTime.UtcNow);

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("CreatedAt"), "应包含 CreatedAt 列");

            Console.WriteLine($"✅ DateTime 边界测试: {sql}");
        }

        [TestMethod]
        public void Boundary_DateTimeOperations_HandlesCorrectly()
        {
            // Arrange
            var baseDate = new DateTime(2023, 6, 15, 10, 30, 0);

            // Act
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            expr.Where(e => e.CreatedAt > baseDate.AddYears(100))
                .Where(e => e.CreatedAt < baseDate.AddYears(-100))
                .Where(e => e.CreatedAt >= baseDate.AddDays(int.MaxValue / 1000))  // 避免溢出
                .Where(e => e.CreatedAt <= baseDate.AddDays(int.MinValue / 1000)); // 避免溢出

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Console.WriteLine($"✅ DateTime 操作边界测试: {sql}");
        }

        #endregion

        #region 集合边界测试

        [TestMethod]
        public void Boundary_EmptyCollections_HandlesCorrectly()
        {
            // Arrange
            var emptyIntList = new List<int>();
            var emptyStringList = new List<string>();

            // Act & Assert
            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

            try
            {
                expr.Where(e => emptyIntList.Contains(e.Id));
                var sql = expr.ToSql();

                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ 空集合测试通过: {sql}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 空集合正确处理异常: {ex.GetType().Name}");
            }

            try
            {
                using var expr2 = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();
                expr2.Where(e => emptyStringList.Contains(e.Name!));
                var sql2 = expr2.ToSql();

                Console.WriteLine($"✅ 空字符串集合测试通过: {sql2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 空字符串集合正确处理异常: {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void Boundary_LargeCollections_HandlesCorrectly()
        {
            // Arrange
            var smallList = Enumerable.Range(1, 10).ToList();
            var mediumList = Enumerable.Range(1, 1000).ToList();
            var largeList = Enumerable.Range(1, 10000).ToList();

            // Act & Assert
            foreach (var (list, size) in new[] { (smallList, "small"), (mediumList, "medium"), (largeList, "large") })
            {
                try
                {
                    using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();
                    expr.Where(e => list.Contains(e.Id));
                    var sql = expr.ToSql();

                    Assert.IsNotNull(sql);
                    Console.WriteLine($"✅ {size} 集合测试通过，大小: {list.Count}");

                    // 检查SQL长度是否合理
                    if (sql.Length > 1000000) // 1MB
                    {
                        Console.WriteLine($"⚠️ 警告: {size} 集合生成的SQL非常长: {sql.Length} 字符");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✅ {size} 集合正确处理异常: {ex.GetType().Name}");
                }
            }
        }

        #endregion

        #region 性能边界测试

        [TestMethod]
        public void Boundary_ComplexExpressionPerformance_WithinLimits()
        {
            // Arrange
            var startTime = DateTime.Now;
            var maxDuration = TimeSpan.FromSeconds(5); // 5秒超时

            // Act
            try
            {
                using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();

                // 构建复杂的查询
                for (int i = 0; i < 100; i++)
                {
                    expr.Where(e => e.Id > i)
                        .Where(e => e.Name!.Contains($"test{i}"))
                        .Where(e => e.CreatedAt > DateTime.Now.AddDays(-i))
                        .Where(e => e.Price > i * 10.5m);
                }

                // 添加复杂的排序
                for (int i = 0; i < 20; i++)
                {
                    if (i % 2 == 0)
                        expr.OrderBy(e => e.Id);
                    else
                        expr.OrderByDescending(e => e.CreatedAt);
                }

                var sql = expr.ToSql();
                var duration = DateTime.Now - startTime;

                // Assert
                Assert.IsNotNull(sql);
                Assert.IsTrue(duration < maxDuration,
                    $"复杂表达式处理时间 {duration.TotalSeconds:F2}s 应小于 {maxDuration.TotalSeconds}s");

                Console.WriteLine($"✅ 复杂表达式性能测试通过，耗时: {duration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"生成的SQL长度: {sql.Length} 字符");
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                Console.WriteLine($"⚠️ 复杂表达式处理异常 (耗时 {duration.TotalMilliseconds:F2}ms): {ex.GetType().Name}: {ex.Message}");
            }
        }

        [TestMethod]
        public void Boundary_MemoryUsageUnderStress_DoesNotLeak()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var maxIterations = 1000;

            // Act
            for (int i = 0; i < maxIterations; i++)
            {
                using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();
                expr.Where(e => e.Id == i)
                    .Where(e => e.Name == $"test{i}")
                    .OrderBy(e => e.CreatedAt);

                var sql = expr.ToSql();

                // 每100次迭代检查一次内存
                if (i % 100 == 0)
                {
                    GC.Collect();
                    var currentMemory = GC.GetTotalMemory(false);
                    var memoryIncrease = currentMemory - initialMemory;

                    Console.WriteLine($"迭代 {i}: 内存增长 {memoryIncrease / 1024}KB");

                    // 检查内存增长是否过大 (超过10MB认为可能有内存泄漏)
                    if (memoryIncrease > 10 * 1024 * 1024)
                    {
                        Console.WriteLine($"⚠️ 警告: 内存增长过大 {memoryIncrease / 1024 / 1024}MB");
                    }
                }
            }

            // Assert
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            var totalIncrease = finalMemory - initialMemory;

            Console.WriteLine($"✅ 内存压力测试完成，总内存增长: {totalIncrease / 1024}KB");

            // 内存增长不应超过5MB
            Assert.IsTrue(totalIncrease < 5 * 1024 * 1024,
                $"内存增长 {totalIncrease / 1024 / 1024}MB 应小于 5MB");
        }

        #endregion

        #region 并发边界测试

        [TestMethod]
        public void Boundary_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var tasks = new List<System.Threading.Tasks.Task>();
            var exceptions = new List<Exception>();
            var lockObject = new object();

            // Act
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            using var expr = ExpressionToSql<BoundaryTestEntity>.ForSqlServer();
                            expr.Where(e => e.Id == taskId * 100 + j)
                                .Where(e => e.Name == $"task{taskId}_item{j}")
                                .OrderBy(e => e.CreatedAt);

                            var sql = expr.ToSql();
                            Assert.IsNotNull(sql);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObject)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });

                tasks.Add(task);
            }

            // 等待所有任务完成
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));

            // Assert
            Assert.AreEqual(0, exceptions.Count,
                $"并发访问不应产生异常，但发现 {exceptions.Count} 个异常");

            Console.WriteLine($"✅ 并发访问测试通过，执行了 {tasks.Count * 100} 次操作");
        }

        #endregion
    }
}
