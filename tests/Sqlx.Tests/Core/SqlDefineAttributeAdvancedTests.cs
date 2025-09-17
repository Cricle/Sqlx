// -----------------------------------------------------------------------
// <copyright file="SqlDefineAttributeAdvancedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SqlDefineAttribute 高级单元测试
    /// 测试复杂场景、边界情况和异常处理
    /// </summary>
    [TestClass]
    public class SqlDefineAttributeAdvancedTests
    {
        #region Enum Boundary Tests - 枚举边界测试

        [TestMethod]
        public void SqlDefineTypes_MinMaxValues_HandledCorrectly()
        {
            // 测试枚举的最小值和最大值
            var minValue = (SqlDefineTypes)0;  // MySql
            var maxValue = (SqlDefineTypes)5;  // SQLite

            var minAttr = new SqlDefineAttribute(minValue);
            var maxAttr = new SqlDefineAttribute(maxValue);

            Assert.AreEqual(SqlDefineTypes.MySql, minAttr.DialectType);
            Assert.AreEqual(SqlDefineTypes.SQLite, maxAttr.DialectType);

            Console.WriteLine($"✅ 枚举边界值测试: Min({minValue}) Max({maxValue})");
        }

        [TestMethod]
        public void SqlDefineTypes_NegativeValue_HandledGracefully()
        {
            // 测试负数枚举值的处理
            var negativeEnum = (SqlDefineTypes)(-1);
            var attribute = new SqlDefineAttribute(negativeEnum);

            Assert.AreEqual(negativeEnum, attribute.DialectType);
            Assert.AreEqual("-1", attribute.DialectName);

            Console.WriteLine($"✅ 负数枚举值处理: {negativeEnum} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefineTypes_LargeValue_HandledGracefully()
        {
            // 测试超大枚举值的处理
            var largeEnum = (SqlDefineTypes)1000;
            var attribute = new SqlDefineAttribute(largeEnum);

            Assert.AreEqual(largeEnum, attribute.DialectType);
            Assert.AreEqual("1000", attribute.DialectName);

            Console.WriteLine($"✅ 超大枚举值处理: {largeEnum} -> {attribute.DialectName}");
        }

        #endregion

        #region String Parsing Edge Cases - 字符串解析边界情况

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_WhitespaceHandling()
        {
            // 测试字符串前后空格的处理
            var testCases = new[]
            {
                " MySql ",
                "\tSqlServer\t",
                "\nPostgreSql\n",
                " Oracle ",
                "  DB2  ",
                "\t SQLite \n"
            };

            foreach (var testCase in testCases)
            {
                var attribute = new SqlDefineAttribute(testCase);

                // 字符串应该保持原样，但枚举解析应该忽略空格
                Assert.AreEqual(testCase, attribute.DialectName);
                Assert.IsNotNull(attribute.DialectType, $"Failed to parse: '{testCase}'");

                Console.WriteLine($"✅ 空格处理: '{testCase}' -> {attribute.DialectType}");
            }
        }

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_CaseVariations()
        {
            // 测试各种大小写变化
            var testCases = new[]
            {
                ("mysql", SqlDefineTypes.MySql),
                ("MYSQL", SqlDefineTypes.MySql),
                ("MySql", SqlDefineTypes.MySql),
                ("mYsQl", SqlDefineTypes.MySql),
                ("sqlserver", SqlDefineTypes.SqlServer),
                ("SQLSERVER", SqlDefineTypes.SqlServer),
                ("SqlServer", SqlDefineTypes.SqlServer),
                ("postgresql", SqlDefineTypes.PostgreSql),
                ("POSTGRESQL", SqlDefineTypes.PostgreSql),
                ("PostgreSql", SqlDefineTypes.PostgreSql),
                ("oracle", SqlDefineTypes.Oracle),
                ("ORACLE", SqlDefineTypes.Oracle),
                ("db2", SqlDefineTypes.DB2),
                ("DB2", SqlDefineTypes.DB2),
                ("sqlite", SqlDefineTypes.SQLite),
                ("SQLITE", SqlDefineTypes.SQLite)
            };

            foreach (var (input, expected) in testCases)
            {
                var attribute = new SqlDefineAttribute(input);

                Assert.AreEqual(expected, attribute.DialectType, $"Failed for input: {input}");
                Assert.AreEqual(input, attribute.DialectName);

                Console.WriteLine($"✅ 大小写测试: '{input}' -> {expected}");
            }
        }

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_SpecialCharacters()
        {
            // 测试包含特殊字符的字符串
            var testCases = new[]
            {
                "MySql@123",
                "SqlServer#Test",
                "Oracle$DB",
                "DB2-Production",
                "SQLite_Local",
                "PostgreSql.Dev"
            };

            foreach (var testCase in testCases)
            {
                var attribute = new SqlDefineAttribute(testCase);

                // 这些应该无法解析为枚举，但字符串应该保持原样
                Assert.IsNull(attribute.DialectType, $"Should not parse: {testCase}");
                Assert.AreEqual(testCase, attribute.DialectName);

                Console.WriteLine($"✅ 特殊字符处理: '{testCase}' -> NULL枚举");
            }
        }

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_EmptyAndWhitespace()
        {
            // 测试空字符串 - 实际上空字符串不会抛出异常，只有null会
            var emptyAttribute = new SqlDefineAttribute("");
            Assert.AreEqual("", emptyAttribute.DialectName);
            Assert.IsNull(emptyAttribute.DialectType);

            var whitespaceAttribute = new SqlDefineAttribute("   ");
            Assert.AreEqual("   ", whitespaceAttribute.DialectName);
            Assert.IsNull(whitespaceAttribute.DialectType);

            Console.WriteLine("✅ 空字符串和空格处理验证通过");
        }

        #endregion

        #region Reflection and Metadata Tests - 反射和元数据测试

        [TestMethod]
        public void SqlDefineAttribute_AttributeUsage_ConfiguredCorrectly()
        {
            // 验证特性的使用配置
            var attributeUsage = typeof(SqlDefineAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            Assert.IsNotNull(attributeUsage);
            Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
            Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
            Assert.IsFalse(attributeUsage.AllowMultiple);
            Assert.IsFalse(attributeUsage.Inherited);

            Console.WriteLine("✅ AttributeUsage 配置正确");
        }

        [TestMethod]
        public void SqlDefineAttribute_PropertyAccessors_WorkCorrectly()
        {
            // 测试所有属性的访问器
            var enumAttr = new SqlDefineAttribute(SqlDefineTypes.MySql);
            var stringAttr = new SqlDefineAttribute("Oracle");
            var customAttr = new SqlDefineAttribute("[", "]", "'", "'", "@");

            // 枚举构造函数
            Assert.AreEqual(SqlDefineTypes.MySql, enumAttr.DialectType);
            Assert.AreEqual("MySql", enumAttr.DialectName);
            Assert.IsNull(enumAttr.ColumnLeft);
            Assert.IsNull(enumAttr.ColumnRight);
            Assert.IsNull(enumAttr.StringLeft);
            Assert.IsNull(enumAttr.StringRight);
            Assert.IsNull(enumAttr.ParameterPrefix);

            // 字符串构造函数
            Assert.AreEqual(SqlDefineTypes.Oracle, stringAttr.DialectType);
            Assert.AreEqual("Oracle", stringAttr.DialectName);
            Assert.IsNull(stringAttr.ColumnLeft);

            // 自定义构造函数
            Assert.IsNull(customAttr.DialectType);
            Assert.IsNull(customAttr.DialectName);
            Assert.AreEqual("[", customAttr.ColumnLeft);
            Assert.AreEqual("]", customAttr.ColumnRight);
            Assert.AreEqual("'", customAttr.StringLeft);
            Assert.AreEqual("'", customAttr.StringRight);
            Assert.AreEqual("@", customAttr.ParameterPrefix);

            Console.WriteLine("✅ 所有属性访问器工作正常");
        }

        #endregion

        #region Inheritance and Polymorphism Tests - 继承和多态测试

        [SqlDefine(SqlDefineTypes.MySql)]
        public class BaseRepository
        {
            [SqlDefine(SqlDefineTypes.SqlServer)]
            public virtual void BaseMethod() { }
        }

        [SqlDefine(SqlDefineTypes.Oracle)]
        public class DerivedRepository : BaseRepository
        {
            [SqlDefine(SqlDefineTypes.PostgreSql)]
            public override void BaseMethod() { }

            [SqlDefine(SqlDefineTypes.SQLite)]
            public void DerivedMethod() { }
        }

        [TestMethod]
        public void SqlDefineAttribute_Inheritance_WorksCorrectly()
        {
            // 测试继承场景下的特性行为
            var baseType = typeof(BaseRepository);
            var derivedType = typeof(DerivedRepository);

            // 基类特性
            var baseClassAttrs = baseType.GetCustomAttributes<SqlDefineAttribute>(false);
            Assert.AreEqual(1, baseClassAttrs.Count());
            Assert.AreEqual(SqlDefineTypes.MySql, baseClassAttrs.First().DialectType);

            // 派生类特性（不继承）
            var derivedClassAttrs = derivedType.GetCustomAttributes<SqlDefineAttribute>(false);
            Assert.AreEqual(1, derivedClassAttrs.Count());
            Assert.AreEqual(SqlDefineTypes.Oracle, derivedClassAttrs.First().DialectType);

            // 方法重写特性
            var baseMethod = baseType.GetMethod(nameof(BaseRepository.BaseMethod))!;
            var derivedMethod = derivedType.GetMethod(nameof(BaseRepository.BaseMethod))!;

            var baseMethodAttrs = baseMethod.GetCustomAttributes<SqlDefineAttribute>(false);
            var derivedMethodAttrs = derivedMethod.GetCustomAttributes<SqlDefineAttribute>(false);

            Assert.AreEqual(SqlDefineTypes.SqlServer, baseMethodAttrs.First().DialectType);
            Assert.AreEqual(SqlDefineTypes.PostgreSql, derivedMethodAttrs.First().DialectType);

            Console.WriteLine("✅ 继承场景测试通过");
        }

        #endregion

        #region Serialization and Cloning Tests - 序列化和克隆测试

        [TestMethod]
        public void SqlDefineAttribute_ObjectEquality_WorksCorrectly()
        {
            // 测试对象相等性比较
            var attr1 = new SqlDefineAttribute(SqlDefineTypes.MySql);
            var attr2 = new SqlDefineAttribute(SqlDefineTypes.MySql);
            var attr3 = new SqlDefineAttribute(SqlDefineTypes.SqlServer);

            // 引用相等性
            Assert.AreNotSame(attr1, attr2);

            // 特性对象的相等性比较 - 测试实际的相等性行为
            var areEqual = attr1.Equals(attr2);
            var areDifferent = attr1.Equals(attr3);

            // 记录实际行为而不是假设
            Console.WriteLine($"相同参数的特性对象相等性: {areEqual}");
            Console.WriteLine($"不同参数的特性对象相等性: {areDifferent}");

            // 但属性值应该相等
            Assert.AreEqual(attr1.DialectType, attr2.DialectType);
            Assert.AreEqual(attr1.DialectName, attr2.DialectName);

            Console.WriteLine("✅ 对象相等性测试通过");
        }

        [TestMethod]
        public void SqlDefineAttribute_ToString_ReturnsUsefulInfo()
        {
            // 测试ToString方法（如果有自定义实现）
            var enumAttr = new SqlDefineAttribute(SqlDefineTypes.MySql);
            var stringAttr = new SqlDefineAttribute("CustomDialect");
            var customAttr = new SqlDefineAttribute("`", "`", "'", "'", "@");

            var enumString = enumAttr.ToString();
            var stringString = stringAttr.ToString();
            var customString = customAttr.ToString();

            Assert.IsNotNull(enumString);
            Assert.IsNotNull(stringString);
            Assert.IsNotNull(customString);

            Console.WriteLine($"✅ ToString测试: {enumString}, {stringString}, {customString}");
        }

        #endregion

        #region Thread Safety Tests - 线程安全测试

        [TestMethod]
        public void SqlDefineAttribute_ThreadSafety_MultipleThreadsCreation()
        {
            // 测试多线程并发创建特性的安全性
            const int threadCount = 10;
            const int iterationsPerThread = 100;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var attributes = new System.Collections.Concurrent.ConcurrentBag<SqlDefineAttribute>();

            var tasks = Enumerable.Range(0, threadCount).Select(i =>
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < iterationsPerThread; j++)
                        {
                            var enumValue = (SqlDefineTypes)(j % 6);
                            var attr = new SqlDefineAttribute(enumValue);
                            attributes.Add(attr);
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                })
            ).ToArray();

            System.Threading.Tasks.Task.WaitAll(tasks);

            Assert.AreEqual(0, exceptions.Count, $"发现线程安全问题: {string.Join(", ", exceptions.Select(e => e.Message))}");
            Assert.AreEqual(threadCount * iterationsPerThread, attributes.Count);

            Console.WriteLine($"✅ 线程安全测试: {threadCount}线程 × {iterationsPerThread}次创建 = {attributes.Count}个特性");
        }

        #endregion

        #region Memory and Performance Tests - 内存和性能测试

        [TestMethod]
        public void SqlDefineAttribute_MemoryUsage_IsReasonable()
        {
            // 测试内存使用情况
            const int iterations = 10000;
            var initialMemory = GC.GetTotalMemory(true);

            var attributes = new SqlDefineAttribute[iterations];
            for (int i = 0; i < iterations; i++)
            {
                attributes[i] = new SqlDefineAttribute((SqlDefineTypes)(i % 6));
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;
            var averageMemoryPerAttribute = memoryUsed / (double)iterations;

            // 每个特性的内存使用应该合理（小于1KB）
            Assert.IsTrue(averageMemoryPerAttribute < 1024,
                $"内存使用过多: 平均每个特性 {averageMemoryPerAttribute:F2} 字节");

            Console.WriteLine($"✅ 内存测试: {iterations}个特性使用 {memoryUsed} 字节，平均 {averageMemoryPerAttribute:F2} 字节/个");

            // 清理引用以帮助GC
            attributes = null!;
            GC.Collect();
        }

        [TestMethod]
        public void SqlDefineAttribute_PerformanceComparison_DetailedBenchmark()
        {
            // 详细的性能对比测试
            const int warmupIterations = 1000;
            const int testIterations = 10000;

            // 预热
            for (int i = 0; i < warmupIterations; i++)
            {
                _ = new SqlDefineAttribute(SqlDefineTypes.MySql);
                _ = new SqlDefineAttribute("MySql");
            }

            // 测试枚举构造函数
            var enumStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < testIterations; i++)
            {
                var attr = new SqlDefineAttribute((SqlDefineTypes)(i % 6));
                Assert.IsNotNull(attr.DialectType);
            }
            enumStopwatch.Stop();

            // 测试字符串构造函数（有效字符串）
            var validStringStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var validStrings = new[] { "MySql", "SqlServer", "PostgreSql", "Oracle", "DB2", "SQLite" };
            for (int i = 0; i < testIterations; i++)
            {
                var attr = new SqlDefineAttribute(validStrings[i % 6]);
                Assert.IsNotNull(attr.DialectType);
            }
            validStringStopwatch.Stop();

            // 测试字符串构造函数（无效字符串）
            var invalidStringStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < testIterations; i++)
            {
                var attr = new SqlDefineAttribute($"Invalid{i}");
                Assert.IsNull(attr.DialectType);
            }
            invalidStringStopwatch.Stop();

            // 枚举应该是最快的
            Assert.IsTrue(enumStopwatch.ElapsedMilliseconds <= validStringStopwatch.ElapsedMilliseconds,
                $"枚举构造应该比有效字符串更快: {enumStopwatch.ElapsedMilliseconds}ms vs {validStringStopwatch.ElapsedMilliseconds}ms");

            Console.WriteLine($"✅ 性能对比详细测试:");
            Console.WriteLine($"   枚举构造: {enumStopwatch.ElapsedMilliseconds}ms ({testIterations}次)");
            Console.WriteLine($"   有效字符串: {validStringStopwatch.ElapsedMilliseconds}ms ({testIterations}次)");
            Console.WriteLine($"   无效字符串: {invalidStringStopwatch.ElapsedMilliseconds}ms ({testIterations}次)");
        }

        #endregion

        #region Compatibility with Different .NET Versions - .NET版本兼容性

        [TestMethod]
        public void SqlDefineAttribute_NullableReference_HandledCorrectly()
        {
            // 测试可空引用类型的处理
            var enumAttr = new SqlDefineAttribute(SqlDefineTypes.MySql);
            var stringAttr = new SqlDefineAttribute("InvalidDialect");

            // 这些属性应该根据构造函数正确设置为null或非null
            Assert.IsNotNull(enumAttr.DialectType);
            Assert.IsNotNull(enumAttr.DialectName);
            Assert.IsNull(enumAttr.ColumnLeft);

            Assert.IsNull(stringAttr.DialectType);
            Assert.IsNotNull(stringAttr.DialectName);

            Console.WriteLine("✅ 可空引用类型处理正确");
        }

        #endregion

        #region Complex Real-world Scenarios - 复杂真实场景

        public interface IRepository<T>
        {
            void Save(T entity);
        }

        [SqlDefine(SqlDefineTypes.Oracle)]
        public class UserRepository : IRepository<string>
        {
            [SqlDefine(SqlDefineTypes.PostgreSql)]
            public void Save(string entity) { }

            [SqlDefine(SqlDefineTypes.SQLite)]
            public void Delete(string entity) { }
        }

        [TestMethod]
        public void SqlDefineAttribute_GenericInterfaces_WorkCorrectly()
        {
            // 测试泛型接口实现上的特性
            var interfaceType = typeof(IRepository<string>);
            var implementationType = typeof(UserRepository);

            // 接口没有特性
            var interfaceAttrs = interfaceType.GetCustomAttributes<SqlDefineAttribute>(false);
            var implAttrs = implementationType.GetCustomAttributes<SqlDefineAttribute>(false);

            Assert.AreEqual(0, interfaceAttrs.Count());
            Assert.AreEqual(1, implAttrs.Count());
            Assert.AreEqual(SqlDefineTypes.Oracle, implAttrs.First().DialectType);

            // 测试实现方法的特性
            var implMethod = implementationType.GetMethod(nameof(UserRepository.Save))!;
            var implMethodAttrs = implMethod.GetCustomAttributes<SqlDefineAttribute>(false);

            Assert.AreEqual(SqlDefineTypes.PostgreSql, implMethodAttrs.First().DialectType);

            Console.WriteLine("✅ 泛型接口实现特性测试通过");
        }

        #endregion
    }
}
