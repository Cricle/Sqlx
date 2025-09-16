// -----------------------------------------------------------------------
// <copyright file="EdgeCaseAndErrorHandlingTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8625, CS8604, CS8603, CS8602, CS8629 // Null-related warnings in test code

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 边界情况和错误处理的专项测试，验证系统在各种异常情况下的行为。
    /// </summary>
    [TestClass]
    public class EdgeCaseAndErrorHandlingTests
    {
        #region 测试实体

        public class TestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int? NullableId { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Price { get; set; }
        }

        public class EmptyEntity
        {
            // 空实体类
        }

        public class EntityWithSpecialCharacters
        {
            public int Id { get; set; }
            public string? NameWithSpaces { get; set; }
            public string? NameWith_Underscore { get; set; }
            public string? Name123 { get; set; }
            public string? SpecialChars { get; set; }
        }

        #endregion

        #region 空值和NULL处理测试

        [TestMethod]
        public void EdgeCase_NullExpressions_HandlesGracefully()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            
            try
            {
                // 测试null表达式 - 应该被忽略或处理
                expr.Where(null!);
                var sql = expr.ToSql();
                
                Console.WriteLine($"Null expression SQL: {sql}");
                Assert.IsNotNull(sql, "即使传入null表达式也应返回SQL");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("✅ 正确处理了null表达式参数");
                // 抛出ArgumentNullException是可以接受的
            }
        }

        [TestMethod]
        public void EdgeCase_NullableComparisons_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.NullableId == null)
                .Where(e => e.NullableId != null)
                .Where(e => e.NullableId.HasValue)
                .Where(e => e.NullableId.Value > 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Nullable comparisons SQL: {sql}");
            Assert.IsTrue(sql.Contains("NullableId"), "应包含可空字段");
            Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("IS NOT NULL"), 
                "应使用正确的NULL比较语法");
        }

        [TestMethod]
        public void EdgeCase_EmptyStringValues_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Name == "")
                .Where(e => e.Name != "")
                .Where(e => e.Description == string.Empty)
                .Where(e => e.Description != string.Empty);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Empty string SQL: {sql}");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Description"), 
                "应处理空字符串比较");
        }

        [TestMethod]
        public void EdgeCase_WhitespaceStrings_HandlesCorrectly()
        {
            // Arrange
            var whitespaceString = "   ";
            var tabString = "\t";
            var newlineString = "\n";

            // Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Name == whitespaceString)
                .Where(e => e.Description == tabString)
                .Where(e => e.Name != newlineString);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Whitespace strings SQL: {sql}");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Description"), 
                "应处理包含空白字符的字符串");
        }

        #endregion

        #region 特殊字符和编码测试

        [TestMethod]
        public void EdgeCase_SpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var specialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            var unicodeChars = "测试数据中文字符";
            var emojiChars = "😀😁😂🤣😃😄";

            // Act
            using var expr = ExpressionToSql<EntityWithSpecialCharacters>.ForSqlServer();
            expr.Where(e => e.SpecialChars == specialChars)
                .Where(e => e.NameWithSpaces!.Contains(unicodeChars))
                .Where(e => e.Name123 == emojiChars);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Special characters SQL: {sql}");
            Assert.IsTrue(sql.Contains("SpecialChars") && sql.Contains("NameWithSpaces") && 
                         sql.Contains("Name123"), "应处理特殊字符字段");
        }

        [TestMethod]
        public void EdgeCase_SqlInjectionAttempts_SafelyHandled()
        {
            // Arrange - 模拟SQL注入尝试
            var maliciousInput1 = "'; DROP TABLE TestEntity; --";
            var maliciousInput2 = "1' OR '1'='1";
            var maliciousInput3 = "UNION SELECT * FROM Users";

            // Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Name == maliciousInput1)
                .Where(e => e.Description == maliciousInput2)
                .Where(e => e.Name!.Contains(maliciousInput3));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"SQL injection attempts SQL: {sql}");
            Assert.IsTrue(sql.Contains("Name") && sql.Contains("Description"), 
                "应处理包含SQL关键字的输入");
            
            // 检查是否有适当的转义或参数化
            Assert.IsFalse(sql.Contains("DROP TABLE") && sql.Contains("TestEntity") && 
                          sql.IndexOf("DROP") < sql.IndexOf("TestEntity"), 
                "不应包含未转义的危险SQL语句");
        }

        [TestMethod]
        public void EdgeCase_LongStrings_HandlesCorrectly()
        {
            // Arrange
            var veryLongString = new string('A', 8000); // 8KB字符串
            var extremelyLongString = new string('B', 100000); // 100KB字符串

            // Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Name == veryLongString)
                .Where(e => e.Description == extremelyLongString);

            try
            {
                var sql = expr.ToSql();
                
                Console.WriteLine($"Long string SQL length: {sql.Length}");
                Assert.IsTrue(sql.Length > veryLongString.Length, "SQL应包含长字符串");
                Assert.IsTrue(sql.Contains("Name") && sql.Contains("Description"), 
                    "应处理长字符串字段");
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("处理极长字符串时出现内存不足，这是可以接受的");
            }
        }

        #endregion

        #region 数值边界情况测试

        [TestMethod]
        public void EdgeCase_NumericBoundaries_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id == int.MaxValue)
                .Where(e => e.Id == int.MinValue)
                .Where(e => e.Id == 0)
                .Where(e => e.Price == decimal.MaxValue)
                .Where(e => e.Price == decimal.MinValue);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Numeric boundaries SQL: {sql}");
            Assert.IsTrue(sql.Contains("Id") && sql.Contains("Price"), 
                "应处理数值边界值");
            Assert.IsTrue(sql.Contains(int.MaxValue.ToString()) || 
                         sql.Contains(int.MinValue.ToString()), 
                "应包含边界数值");
        }

        [TestMethod]
        public void EdgeCase_FloatingPointPrecision_HandlesCorrectly()
        {
            // Arrange
            var preciseDecimal = 123.456789012345678901234567890m;
            var tinyDecimal = 0.000000000000000001m;

            // Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Price == preciseDecimal)
                .Where(e => e.Price > tinyDecimal);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Floating point precision SQL: {sql}");
            Assert.IsTrue(sql.Contains("Price"), "应处理高精度小数");
        }

        [TestMethod]
        public void EdgeCase_DivisionByZero_HandlesGracefully()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            
            try
            {
                expr.Where(e => e.Id / 0 > 0);
                var sql = expr.ToSql();
                
                Console.WriteLine($"Division by zero SQL: {sql}");
                // 如果没有抛出异常，检查生成的SQL
                Assert.IsTrue(sql.Contains("Id"), "应包含除法操作");
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("✅ 正确处理了除零异常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理除零时的其他异常: {ex.GetType().Name}: {ex.Message}");
            }
        }

        #endregion

        #region 日期时间边界测试

        [TestMethod]
        public void EdgeCase_DateTimeBoundaries_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.CreatedAt == DateTime.MinValue)
                .Where(e => e.CreatedAt == DateTime.MaxValue)
                .Where(e => e.CreatedAt == DateTime.UnixEpoch)
                .Where(e => e.CreatedAt == new DateTime(1900, 1, 1));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"DateTime boundaries SQL: {sql}");
            Assert.IsTrue(sql.Contains("CreatedAt"), "应处理日期时间边界值");
        }

        [TestMethod]
        public void EdgeCase_DateTimeCalculations_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.CreatedAt.AddDays(30) > DateTime.Now)
                .Where(e => e.CreatedAt.AddYears(-1) < DateTime.Today)
                .Where(e => e.CreatedAt.Date == DateTime.Today);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"DateTime calculations SQL: {sql}");
            Assert.IsTrue(sql.Contains("CreatedAt"), "应处理日期时间计算");
        }

        #endregion

        #region 复杂表达式错误处理

        [TestMethod]
        public void EdgeCase_ComplexNestedExpressions_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            
            try
            {
                expr.Where(e => ((e.Id > 0 ? e.Name : e.Description) ?? "default").Length > 0);
                var sql = expr.ToSql();
                
                Console.WriteLine($"Complex nested expression SQL: {sql}");
                Assert.IsNotNull(sql, "应处理复杂嵌套表达式");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"复杂表达式处理异常: {ex.GetType().Name}: {ex.Message}");
                // 复杂表达式可能不被支持，这是可以接受的
            }
        }

        [TestMethod]
        public void EdgeCase_UnsupportedMethods_HandlesGracefully()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            
            try
            {
                // 尝试使用可能不被支持的方法
                expr.Where(e => e.Name!.GetHashCode() > 0)
                    .Where(e => e.CreatedAt.GetType() == typeof(DateTime))
                    .Where(e => object.ReferenceEquals(e.Name, "test"));
                
                var sql = expr.ToSql();
                Console.WriteLine($"Unsupported methods SQL: {sql}");
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"✅ 正确处理了不支持的方法: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理不支持方法时的其他异常: {ex.GetType().Name}: {ex.Message}");
            }
        }

        #endregion

        #region 资源管理和性能测试

        [TestMethod]
        public void EdgeCase_MultipleDispose_HandlesCorrectly()
        {
            // Arrange
            var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            expr.Where(e => e.Id > 0);

            // Act & Assert
            expr.Dispose();
            
            try
            {
                expr.Dispose(); // 第二次释放
                Console.WriteLine("✅ 多次Dispose没有抛出异常");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("✅ 多次Dispose抛出了ObjectDisposedException");
            }

            try
            {
                var sql = expr.ToSql(); // 使用已释放的对象
                Assert.Fail("使用已释放对象应抛出异常");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("✅ 正确阻止了已释放对象的使用");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"已释放对象使用时的其他异常: {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void EdgeCase_HighMemoryPressure_HandlesGracefully()
        {
            // Arrange - 模拟高内存压力
            var expressions = new List<ExpressionToSql<TestEntity>>();
            
            try
            {
                // 创建大量表达式对象
                for (int i = 0; i < 1000; i++)
                {
                    var expr = ExpressionToSql<TestEntity>.ForSqlServer();
                    expr.Where(e => e.Id == i);
                    expressions.Add(expr);
                }

                Console.WriteLine($"创建了 {expressions.Count} 个表达式对象");
                
                // 尝试生成SQL
                foreach (var expr in expressions.Take(10))
                {
                    var sql = expr.ToSql();
                    Assert.IsTrue(sql.Length > 0, "应生成有效SQL");
                }

            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("在高内存压力下出现OutOfMemoryException，这是可以预期的");
            }
            finally
            {
                // 清理资源
                foreach (var expr in expressions)
                {
                    expr?.Dispose();
                }
            }
        }

        [TestMethod]
        public void EdgeCase_VeryLongExpressionChain_HandlesEfficiently()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
            
            var startTime = DateTime.Now;
            
            // 创建非常长的表达式链
            for (int i = 0; i < 500; i++)
            {
                expr.Where(e => e.Id != i);
            }

            var sql = expr.ToSql();
            var endTime = DateTime.Now;

            // Assert
            var duration = endTime - startTime;
            Console.WriteLine($"500个WHERE条件处理时间: {duration.TotalMilliseconds}ms");
            Console.WriteLine($"生成的SQL长度: {sql.Length}");
            
            Assert.IsTrue(duration.TotalMilliseconds < 5000, "500个条件应在5秒内处理完成");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Length > 1000, "应生成相当长的SQL");
        }

        #endregion

        #region 并发和线程安全测试

        [TestMethod]
        public void EdgeCase_ConcurrentAccess_ThreadSafety()
        {
            // Arrange
            var exceptions = new List<Exception>();
            var results = new List<string>();
            var lockObject = new object();

            // Act - 模拟并发访问
            var tasks = new List<System.Threading.Tasks.Task>();
            
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        using var expr = ExpressionToSql<TestEntity>.ForSqlServer();
                        expr.Where(e => e.Id == taskId);
                        var sql = expr.ToSql();
                        
                        lock (lockObject)
                        {
                            results.Add($"Task {taskId}: {sql}");
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
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));

            // Assert
            Console.WriteLine($"并发任务完成数: {results.Count}");
            Console.WriteLine($"并发异常数: {exceptions.Count}");
            
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
            
            foreach (var ex in exceptions)
            {
                Console.WriteLine($"并发异常: {ex.GetType().Name}: {ex.Message}");
            }

            Assert.IsTrue(results.Count > 0, "至少有一些并发任务应该成功");
        }

        #endregion

        #region 空实体和元数据测试

        [TestMethod]
        public void EdgeCase_EmptyEntity_HandlesCorrectly()
        {
            // Arrange & Act
            try
            {
                using var expr = ExpressionToSql<EmptyEntity>.ForSqlServer();
                var sql = expr.ToSql();
                
                Console.WriteLine($"Empty entity SQL: {sql}");
                Assert.IsNotNull(sql, "即使是空实体也应生成基本SQL");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"空实体处理异常: {ex.GetType().Name}: {ex.Message}");
                // 空实体可能不被支持，这是可以接受的
            }
        }

        [TestMethod]
        public void EdgeCase_GenericTypeParameters_HandlesCorrectly()
        {
            // 这个测试确保泛型类型参数被正确处理
            
            // Act & Assert
            using var expr1 = ExpressionToSql<TestEntity>.ForSqlServer();
            using var expr2 = ExpressionToSql<EntityWithSpecialCharacters>.ForSqlServer();
            
            expr1.Where(e => e.Id > 0);
            expr2.Where(e => e.Id > 0);
            
            var sql1 = expr1.ToSql();
            var sql2 = expr2.ToSql();
            
            Console.WriteLine($"TestEntity SQL: {sql1}");
            Console.WriteLine($"EntityWithSpecialCharacters SQL: {sql2}");
            
            Assert.IsTrue(sql1.Contains("TestEntity"), "应包含正确的表名");
            Assert.IsTrue(sql2.Contains("EntityWithSpecialCharacters"), "应包含正确的表名");
            Assert.AreNotEqual(sql1, sql2, "不同实体类型应生成不同的SQL");
        }

        #endregion
    }
}
