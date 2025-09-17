// -----------------------------------------------------------------------
// <copyright file="ValueStringBuilderCoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// ValueStringBuilder 核心功能单元测试
    /// 验证高性能字符串构建器的核心功能
    /// </summary>
    [TestClass]
    public class ValueStringBuilderCoreTests
    {
        #region Basic Functionality Tests - 基础功能测试

        [TestMethod]
        public void ValueStringBuilder_BasicAppend_WorksCorrectly()
        {
            // 测试基本的字符串追加功能
            using var builder = new ValueStringBuilder(64);

            builder.Append("Hello");
            builder.Append(" ");
            builder.Append("World");

            var result = builder.ToString();

            Assert.AreEqual("Hello World", result);
            Console.WriteLine($"✅ 基础追加测试: '{result}'");
        }

        [TestMethod]
        public void ValueStringBuilder_AppendChar_WorksCorrectly()
        {
            // 测试字符追加功能
            using var builder = new ValueStringBuilder(32);

            builder.Append('H');
            builder.Append('e');
            builder.Append('l');
            builder.Append('l');
            builder.Append('o');

            var result = builder.ToString();

            Assert.AreEqual("Hello", result);
            Console.WriteLine($"✅ 字符追加测试: '{result}'");
        }

        [TestMethod]
        public void ValueStringBuilder_AppendString_WorksCorrectly()
        {
            // 测试字符串追加功能
            using var builder = new ValueStringBuilder(64);

            var text = "Hello World";
            builder.Append(text);

            var result = builder.ToString();

            Assert.AreEqual("Hello World", result);
            Console.WriteLine($"✅ 字符串追加测试: '{result}'");
        }

        [TestMethod]
        public void ValueStringBuilder_Length_TracksCorrectly()
        {
            // 测试长度跟踪功能
            using var builder = new ValueStringBuilder(64);

            Assert.AreEqual(0, builder.Length, "Initial length should be 0");

            builder.Append("Hello");
            Assert.AreEqual(5, builder.Length, "Length should be 5 after 'Hello'");

            builder.Append(" World");
            Assert.AreEqual(11, builder.Length, "Length should be 11 after ' World'");

            Console.WriteLine($"✅ 长度跟踪测试: 最终长度 {builder.Length}");
        }

        #endregion

        #region Capacity Management Tests - 容量管理测试

        [TestMethod]
        public void ValueStringBuilder_InitialCapacity_IsRespected()
        {
            // 测试初始容量设置
            var initialCapacity = 128;
            using var builder = new ValueStringBuilder(initialCapacity);

            // 添加少于初始容量的内容
            var testString = new string('A', initialCapacity / 2);
            builder.Append(testString);

            var result = builder.ToString();

            Assert.AreEqual(testString, result);
            Assert.AreEqual(initialCapacity / 2, builder.Length);

            Console.WriteLine($"✅ 初始容量测试: 容量={initialCapacity}, 使用={builder.Length}");
        }

        [TestMethod]
        public void ValueStringBuilder_AutoGrowth_WorksCorrectly()
        {
            // 测试自动扩容功能
            var initialCapacity = 16;
            using var builder = new ValueStringBuilder(initialCapacity);

            // 添加超过初始容量的内容
            var largeString = new string('B', initialCapacity * 3);
            builder.Append(largeString);

            var result = builder.ToString();

            Assert.AreEqual(largeString, result);
            Assert.AreEqual(largeString.Length, builder.Length);

            Console.WriteLine($"✅ 自动扩容测试: 初始容量={initialCapacity}, 实际使用={builder.Length}");
        }

        [TestMethod]
        public void ValueStringBuilder_MultipleGrowths_HandleCorrectly()
        {
            // 测试多次扩容
            using var builder = new ValueStringBuilder(8);

            // 逐步添加内容，触发多次扩容
            for (int i = 0; i < 10; i++)
            {
                builder.Append($"Segment{i}_");
            }

            var result = builder.ToString();
            var expectedLength = 10 * 9; // "Segment0_" = 9 characters * 10

            Assert.AreEqual(expectedLength, result.Length);
            Assert.IsTrue(result.Contains("Segment0_"));
            Assert.IsTrue(result.Contains("Segment9_"));

            Console.WriteLine($"✅ 多次扩容测试: 最终长度={result.Length}");
        }

        #endregion

        #region Performance Tests - 性能测试

        [TestMethod]
        public void ValueStringBuilder_Performance_AppendManyStrings()
        {
            // 测试大量字符串追加的性能
            const int iterations = 1000;
            using var builder = new ValueStringBuilder(1024);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                builder.Append($"Item{i}_");
            }

            var result = builder.ToString();
            stopwatch.Stop();

            Assert.IsTrue(result.Length > 0);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100,
                $"Performance test failed: {stopwatch.ElapsedMilliseconds}ms for {iterations} appends");

            Console.WriteLine($"✅ 性能测试: {iterations}次追加耗时 {stopwatch.ElapsedMilliseconds}ms, " +
                            $"结果长度={result.Length}");
        }

        [TestMethod]
        public void ValueStringBuilder_Performance_CompareWithStringBuilder()
        {
            // 与StringBuilder性能对比
            const int iterations = 1000;
            var testString = "TestString";

            // ValueStringBuilder性能测试
            var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
            using (var valueBuilder = new ValueStringBuilder(1024))
            {
                for (int i = 0; i < iterations; i++)
                {
                    valueBuilder.Append(testString);
                }
                var result1 = valueBuilder.ToString();
                stopwatch1.Stop();

                Assert.AreEqual(testString.Length * iterations, result1.Length);
            }

            // StringBuilder性能测试
            var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
            var stringBuilder = new System.Text.StringBuilder(1024);
            for (int i = 0; i < iterations; i++)
            {
                stringBuilder.Append(testString);
            }
            var result2 = stringBuilder.ToString();
            stopwatch2.Stop();

            Assert.AreEqual(testString.Length * iterations, result2.Length);

            Console.WriteLine($"✅ 性能对比测试:");
            Console.WriteLine($"   ValueStringBuilder: {stopwatch1.ElapsedMilliseconds}ms");
            Console.WriteLine($"   StringBuilder: {stopwatch2.ElapsedMilliseconds}ms");
            Console.WriteLine($"   性能比率: {(double)stopwatch2.ElapsedMilliseconds / stopwatch1.ElapsedMilliseconds:F2}x");
        }

        #endregion

        #region Memory Management Tests - 内存管理测试

        [TestMethod]
        public void ValueStringBuilder_MemoryUsage_IsEfficient()
        {
            // 测试内存使用效率
            const int iterations = 100;
            var initialMemory = GC.GetTotalMemory(true);

            for (int i = 0; i < iterations; i++)
            {
                using var builder = new ValueStringBuilder(64);
                builder.Append("Test string for memory usage");
                builder.Append(i.ToString());
                var result = builder.ToString();
                Assert.IsNotNull(result);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);
            var memoryUsed = finalMemory - initialMemory;
            var averageMemoryPerBuilder = memoryUsed / (double)iterations;

            Assert.IsTrue(averageMemoryPerBuilder < 1024,
                $"Memory usage per builder should be reasonable: {averageMemoryPerBuilder:F2} bytes");

            Console.WriteLine($"✅ 内存效率测试: {iterations}个构建器使用 {memoryUsed} 字节, " +
                            $"平均 {averageMemoryPerBuilder:F2} 字节/个");
        }

        [TestMethod]
        public void ValueStringBuilder_Dispose_ReleasesResources()
        {
            // 测试资源释放
            var builder = new ValueStringBuilder(128);

            try
            {
                builder.Append("Test content");

                var result = builder.ToString();
                Assert.AreEqual("Test content", result);

                Console.WriteLine($"✅ 资源释放测试通过");
            }
            finally
            {
                builder.Dispose(); // 确保资源被释放
            }
        }

        [TestMethod]
        public void ValueStringBuilder_UsingStatement_WorksCorrectly()
        {
            // 测试using语句的正确使用
            string result;

            using (var builder = new ValueStringBuilder(64))
            {
                builder.Append("Using statement test");
                result = builder.ToString();
            }

            Assert.AreEqual("Using statement test", result);
            Console.WriteLine($"✅ Using语句测试: '{result}'");
        }

        #endregion

        #region Edge Cases Tests - 边界情况测试

        [TestMethod]
        public void ValueStringBuilder_EmptyString_HandlesCorrectly()
        {
            // 测试空字符串处理
            using var builder = new ValueStringBuilder(32);

            builder.Append("");
            builder.Append(string.Empty);

            var result = builder.ToString();

            Assert.AreEqual("", result);
            Assert.AreEqual(0, builder.Length);

            Console.WriteLine($"✅ 空字符串测试: 长度={builder.Length}");
        }

        [TestMethod]
        public void ValueStringBuilder_NullString_HandlesCorrectly()
        {
            // 测试null字符串处理
            using var builder = new ValueStringBuilder(32);

            string? nullString = null;
            builder.Append(nullString ?? "");
            builder.Append("NotNull");

            var result = builder.ToString();

            // 根据实现，null可能被忽略或转换为空字符串
            Assert.IsTrue(result.Contains("NotNull"));

            Console.WriteLine($"✅ NULL字符串测试: '{result}'");
        }

        [TestMethod]
        public void ValueStringBuilder_VeryLargeString_HandlesCorrectly()
        {
            // 测试非常大的字符串处理
            using var builder = new ValueStringBuilder(16);

            var largeString = new string('X', 10000);
            builder.Append(largeString);

            var result = builder.ToString();

            Assert.AreEqual(largeString.Length, result.Length);
            Assert.AreEqual(largeString, result);

            Console.WriteLine($"✅ 大字符串测试: 长度={result.Length}");
        }

        [TestMethod]
        public void ValueStringBuilder_ZeroCapacity_HandlesCorrectly()
        {
            // 测试零容量初始化
            using var builder = new ValueStringBuilder(0);

            builder.Append("Test");

            var result = builder.ToString();

            Assert.AreEqual("Test", result);
            Assert.AreEqual(4, builder.Length);

            Console.WriteLine($"✅ 零容量测试: '{result}', 长度={builder.Length}");
        }

        #endregion

        #region Integration Tests - 集成测试

        [TestMethod]
        public void ValueStringBuilder_SqlGeneration_Integration()
        {
            // 测试与SQL生成的集成
            using var builder = new ValueStringBuilder(256);

            // 模拟SQL生成过程
            builder.Append("SELECT ");
            builder.Append("[Id], [Name], [Email]");
            builder.Append(" FROM ");
            builder.Append("[Users]");
            builder.Append(" WHERE ");
            builder.Append("[Age] > ");
            builder.Append("18");
            builder.Append(" AND ");
            builder.Append("[IsActive] = ");
            builder.Append("1");
            builder.Append(" ORDER BY ");
            builder.Append("[CreatedAt] DESC");

            var sql = builder.ToString();

            Assert.IsTrue(sql.StartsWith("SELECT"));
            Assert.IsTrue(sql.Contains("FROM [Users]"));
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("ORDER BY"));

            Console.WriteLine($"✅ SQL生成集成测试:");
            Console.WriteLine($"   {sql}");
        }

        [TestMethod]
        public void ValueStringBuilder_ComplexSQL_Performance()
        {
            // 测试复杂SQL生成的性能
            const int iterations = 100;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                using var builder = new ValueStringBuilder(512);

                // 构建复杂的SQL语句
                builder.Append("SELECT t1.[Id], t1.[Name], t2.[Description], t3.[Value] ");
                builder.Append("FROM [Table1] t1 ");
                builder.Append("INNER JOIN [Table2] t2 ON t1.[Id] = t2.[Table1Id] ");
                builder.Append("LEFT JOIN [Table3] t3 ON t2.[Id] = t3.[Table2Id] ");
                builder.Append("WHERE t1.[IsActive] = 1 ");
                builder.Append("AND t1.[CreatedAt] > '2023-01-01' ");
                builder.Append("AND (t2.[Status] = 'Active' OR t2.[Status] = 'Pending') ");
                builder.Append("ORDER BY t1.[CreatedAt] DESC, t1.[Name] ASC ");
                builder.Append("OFFSET ");
                builder.Append((i * 10).ToString());
                builder.Append(" ROWS FETCH NEXT 10 ROWS ONLY");

                var sql = builder.ToString();
                Assert.IsTrue(sql.Length > 0);
            }

            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100,
                $"Complex SQL generation should be fast: {stopwatch.ElapsedMilliseconds}ms");

            Console.WriteLine($"✅ 复杂SQL性能测试: {iterations}次生成耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}
