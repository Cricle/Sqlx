// -----------------------------------------------------------------------
// <copyright file="SqlDefineGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Generator.Core;

namespace Sqlx.Tests.Generator
{
    /// <summary>
    /// SqlDefine 代码生成器测试
    /// 验证生成器正确处理 SqlDefine 枚举特性
    /// </summary>
    [TestClass]
    public class SqlDefineGeneratorTests
    {
        #region Enum to SqlDefine Mapping Tests - 枚举到SqlDefine映射测试

        [TestMethod]
        public void SqlDefineEnum_ToInternalSqlDefine_MapsCorrectly()
        {
            // 验证枚举值到内部SqlDefine的映射关系
            var testCases = new[]
            {
                (Sqlx.Annotations.SqlDefineTypes.MySql, 0, Sqlx.Generator.Core.SqlDefine.MySql),
                (Sqlx.Annotations.SqlDefineTypes.SqlServer, 1, Sqlx.Generator.Core.SqlDefine.SqlServer),
                (Sqlx.Annotations.SqlDefineTypes.PostgreSql, 2, Sqlx.Generator.Core.SqlDefine.PgSql),
                (Sqlx.Annotations.SqlDefineTypes.Oracle, 3, Sqlx.Generator.Core.SqlDefine.Oracle),
                (Sqlx.Annotations.SqlDefineTypes.DB2, 4, Sqlx.Generator.Core.SqlDefine.DB2),
                (Sqlx.Annotations.SqlDefineTypes.SQLite, 5, Sqlx.Generator.Core.SqlDefine.SQLite)
            };

            foreach (var (enumValue, expectedInt, expectedSqlDefine) in testCases)
            {
                // 验证枚举的整数值
                Assert.AreEqual(expectedInt, (int)enumValue, 
                    $"枚举 {enumValue} 的整数值应该是 {expectedInt}");

                // 验证映射到内部SqlDefine
                var actualSqlDefine = MapEnumToSqlDefine((int)enumValue);
                Assert.AreEqual(expectedSqlDefine.ColumnLeft, actualSqlDefine.ColumnLeft,
                    $"{enumValue} 的 ColumnLeft 映射错误");
                Assert.AreEqual(expectedSqlDefine.ColumnRight, actualSqlDefine.ColumnRight,
                    $"{enumValue} 的 ColumnRight 映射错误");
                Assert.AreEqual(expectedSqlDefine.ParameterPrefix, actualSqlDefine.ParameterPrefix,
                    $"{enumValue} 的 ParameterPrefix 映射错误");

                Console.WriteLine($"✅ 映射验证: {enumValue} ({expectedInt}) -> {actualSqlDefine.ColumnLeft}col{actualSqlDefine.ColumnRight}");
            }
        }

        private Sqlx.Generator.Core.SqlDefine MapEnumToSqlDefine(int enumValue)
        {
            // 模拟生成器中的映射逻辑
            return enumValue switch
            {
                0 => Sqlx.Generator.Core.SqlDefine.MySql,
                1 => Sqlx.Generator.Core.SqlDefine.SqlServer,
                2 => Sqlx.Generator.Core.SqlDefine.PgSql,
                3 => Sqlx.Generator.Core.SqlDefine.Oracle,
                4 => Sqlx.Generator.Core.SqlDefine.DB2,
                5 => Sqlx.Generator.Core.SqlDefine.SQLite,
                _ => Sqlx.Generator.Core.SqlDefine.SqlServer, // Default fallback
            };
        }

        #endregion

        #region SQL Dialect Configuration Tests - SQL方言配置测试

        [TestMethod]
        public void SqlDefine_DialectConfigurations_AreCorrect()
        {
            // 验证每个方言的配置是否正确
            var configurations = new[]
            {
                (Sqlx.Generator.Core.SqlDefine.MySql, "`", "`", "'", "'", "@", "MySQL"),
                (Sqlx.Generator.Core.SqlDefine.SqlServer, "[", "]", "'", "'", "@", "SQL Server"),
                (Sqlx.Generator.Core.SqlDefine.PgSql, "\"", "\"", "'", "'", "$", "PostgreSQL"),
                (Sqlx.Generator.Core.SqlDefine.Oracle, "\"", "\"", "'", "'", ":", "Oracle"),
                (Sqlx.Generator.Core.SqlDefine.DB2, "\"", "\"", "'", "'", "?", "DB2"),
                (Sqlx.Generator.Core.SqlDefine.SQLite, "[", "]", "'", "'", "$", "SQLite")
            };

            foreach (var (sqlDefine, expectedColLeft, expectedColRight, expectedStrLeft, expectedStrRight, expectedParamPrefix, dialectName) in configurations)
            {
                Assert.AreEqual(expectedColLeft, sqlDefine.ColumnLeft, $"{dialectName} ColumnLeft 错误");
                Assert.AreEqual(expectedColRight, sqlDefine.ColumnRight, $"{dialectName} ColumnRight 错误");
                Assert.AreEqual(expectedStrLeft, sqlDefine.StringLeft, $"{dialectName} StringLeft 错误");
                Assert.AreEqual(expectedStrRight, sqlDefine.StringRight, $"{dialectName} StringRight 错误");
                Assert.AreEqual(expectedParamPrefix, sqlDefine.ParameterPrefix, $"{dialectName} ParameterPrefix 错误");

                Console.WriteLine($"✅ {dialectName} 配置: {sqlDefine.ColumnLeft}col{sqlDefine.ColumnRight} {sqlDefine.StringLeft}str{sqlDefine.StringRight} {sqlDefine.ParameterPrefix}param");
            }
        }

        [TestMethod]
        public void SqlDefine_WrapMethods_WorkCorrectly()
        {
            // 测试包装方法的正确性
            var testColumn = "TestColumn";
            var testString = "TestString";

            var testCases = new[]
            {
                (Sqlx.Generator.Core.SqlDefine.MySql, "`TestColumn`", "'TestString'"),
                (Sqlx.Generator.Core.SqlDefine.SqlServer, "[TestColumn]", "'TestString'"),
                (Sqlx.Generator.Core.SqlDefine.PgSql, "\"TestColumn\"", "'TestString'"),
                (Sqlx.Generator.Core.SqlDefine.Oracle, "\"TestColumn\"", "'TestString'"),
                (Sqlx.Generator.Core.SqlDefine.DB2, "\"TestColumn\"", "'TestString'"),
                (Sqlx.Generator.Core.SqlDefine.SQLite, "[TestColumn]", "'TestString'")
            };

            foreach (var (sqlDefine, expectedColumn, expectedString) in testCases)
            {
                var wrappedColumn = sqlDefine.WrapColumn(testColumn);
                var wrappedString = sqlDefine.WrapString(testString);

                Assert.AreEqual(expectedColumn, wrappedColumn, 
                    $"列包装错误: {sqlDefine.GetType().Name}");
                Assert.AreEqual(expectedString, wrappedString, 
                    $"字符串包装错误: {sqlDefine.GetType().Name}");

                Console.WriteLine($"✅ 包装测试: {wrappedColumn}, {wrappedString}");
            }
        }

        #endregion

        #region Generator Integration Simulation Tests - 生成器集成模拟测试

        [TestMethod]
        public void SqlDefine_GeneratorIntegration_SimulatedAttributeParsing()
        {
            // 模拟生成器解析SqlDefine特性的过程
            var testScenarios = new[]
            {
                // 模拟不同的特性使用场景
                (Sqlx.Annotations.SqlDefineTypes.MySql, "MySql", "`", "`"),
                (Sqlx.Annotations.SqlDefineTypes.SqlServer, "SqlServer", "[", "]"),
                (Sqlx.Annotations.SqlDefineTypes.PostgreSql, "PostgreSql", "\"", "\""),
                (Sqlx.Annotations.SqlDefineTypes.Oracle, "Oracle", "\"", "\""),
                (Sqlx.Annotations.SqlDefineTypes.DB2, "DB2", "\"", "\""),
                (Sqlx.Annotations.SqlDefineTypes.SQLite, "SQLite", "[", "]")
            };

            foreach (var (enumValue, dialectName, expectedLeft, expectedRight) in testScenarios)
            {
                // 模拟特性创建
                var attribute = new SqlDefineAttribute(enumValue);
                
                // 模拟生成器读取特性
                var parsedDialectType = attribute.DialectType;
                var parsedDialectName = attribute.DialectName;
                
                // 模拟生成器转换为内部SqlDefine
                var sqlDefine = MapEnumToSqlDefine((int)parsedDialectType!);
                
                // 验证结果
                Assert.AreEqual(enumValue, parsedDialectType);
                Assert.AreEqual(dialectName, parsedDialectName);
                Assert.AreEqual(expectedLeft, sqlDefine.ColumnLeft);
                Assert.AreEqual(expectedRight, sqlDefine.ColumnRight);
                
                Console.WriteLine($"✅ 生成器集成模拟: {enumValue} -> {sqlDefine.ColumnLeft}column{sqlDefine.ColumnRight}");
            }
        }

        [TestMethod]
        public void SqlDefine_GeneratorFallback_HandlesUnknownValues()
        {
            // 测试生成器对未知枚举值的回退处理
            var unknownValues = new[] { -1, 999, int.MaxValue, int.MinValue };
            
            foreach (var unknownValue in unknownValues)
            {
                var fallbackSqlDefine = MapEnumToSqlDefine(unknownValue);
                
                // 应该回退到 SqlServer 默认值
                Assert.AreEqual(Sqlx.Generator.Core.SqlDefine.SqlServer.ColumnLeft, fallbackSqlDefine.ColumnLeft);
                Assert.AreEqual(Sqlx.Generator.Core.SqlDefine.SqlServer.ColumnRight, fallbackSqlDefine.ColumnRight);
                Assert.AreEqual(Sqlx.Generator.Core.SqlDefine.SqlServer.ParameterPrefix, fallbackSqlDefine.ParameterPrefix);
                
                Console.WriteLine($"✅ 未知值回退: {unknownValue} -> SQL Server 默认配置");
            }
        }

        #endregion

        #region SQL Generation Tests - SQL生成测试

        [TestMethod]
        public void SqlDefine_SQLGeneration_ProducesCorrectSyntax()
        {
            // 测试不同方言生成正确的SQL语法
            var tableName = "Users";
            var columnName = "Name";
            var parameterName = "nameParam";

            var testCases = new[]
            {
                (Sqlx.Generator.Core.SqlDefine.MySql, "`Users`", "`Name`", "@nameParam"),
                (Sqlx.Generator.Core.SqlDefine.SqlServer, "[Users]", "[Name]", "@nameParam"),
                (Sqlx.Generator.Core.SqlDefine.PgSql, "\"Users\"", "\"Name\"", "$nameParam"),
                (Sqlx.Generator.Core.SqlDefine.Oracle, "\"Users\"", "\"Name\"", ":nameParam"),
                (Sqlx.Generator.Core.SqlDefine.DB2, "\"Users\"", "\"Name\"", "?"),  // DB2 使用位置参数
                (Sqlx.Generator.Core.SqlDefine.SQLite, "[Users]", "[Name]", "$nameParam")
            };

            foreach (var (sqlDefine, expectedTable, expectedColumn, expectedParam) in testCases)
            {
                var wrappedTable = sqlDefine.WrapColumn(tableName);
                var wrappedColumn = sqlDefine.WrapColumn(columnName);
                var formattedParam = sqlDefine.ParameterPrefix + (sqlDefine.ParameterPrefix == "?" ? "" : parameterName);

                Assert.AreEqual(expectedTable, wrappedTable);
                Assert.AreEqual(expectedColumn, wrappedColumn);
                
                if (sqlDefine.ParameterPrefix != "?")
                {
                    Assert.AreEqual(expectedParam, formattedParam);
                }

                // 模拟生成完整的SELECT语句
                var selectSql = $"SELECT {wrappedColumn} FROM {wrappedTable} WHERE {wrappedColumn} = {formattedParam}";
                
                Console.WriteLine($"✅ SQL生成: {selectSql}");
            }
        }

        #endregion

        #region Performance Tests for Generator - 生成器性能测试

        [TestMethod]
        public void SqlDefine_GeneratorPerformance_EnumMappingIsEfficient()
        {
            // 测试枚举映射的性能
            const int iterations = 100000;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var enumValue = i % 6; // 循环使用0-5
                var sqlDefine = MapEnumToSqlDefine(enumValue);
                
                // 执行一些基本操作来模拟实际使用
                _ = sqlDefine.WrapColumn("TestColumn");
                _ = sqlDefine.WrapString("TestString");
            }
            
            stopwatch.Stop();
            
            // 应该在合理时间内完成（比如100ms内）
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"枚举映射性能不佳: {iterations}次操作耗时 {stopwatch.ElapsedMilliseconds}ms");
            
            Console.WriteLine($"✅ 生成器性能: {iterations}次枚举映射耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Error Handling Tests - 错误处理测试

        [TestMethod]
        public void SqlDefine_ErrorHandling_InvalidInputs()
        {
            // 测试各种无效输入的处理
            var testCases = new[]
            {
                ("", false),
                (null, false),
                ("   ", false),
                ("InvalidDialect", false),
                ("MySql123", false),
                ("mysql", true),  // 小写应该能解析
                ("MYSQL", true),  // 大写应该能解析
                ("MySql", true)   // 正确格式应该能解析
            };

            foreach (var (input, shouldParse) in testCases)
            {
                try
                {
                    if (input == null)
                    {
                        Assert.ThrowsException<ArgumentNullException>(() => 
                            new SqlDefineAttribute(input!));
                        Console.WriteLine("✅ NULL输入正确抛出异常");
                        continue;
                    }

                    if (input == "")
                    {
                        // 空字符串实际上不会抛出异常
                        var emptyAttr = new SqlDefineAttribute(input);
                        Assert.AreEqual("", emptyAttr.DialectName);
                        Assert.IsNull(emptyAttr.DialectType);
                        Console.WriteLine("✅ 空字符串处理正常");
                        continue;
                    }

                    var attribute = new SqlDefineAttribute(input);
                    var hasDialectType = attribute.DialectType.HasValue;
                    
                    Assert.AreEqual(shouldParse, hasDialectType, 
                        $"解析结果不符合预期: '{input}' 应该 {(shouldParse ? "能" : "不能")} 解析");
                    
                    Console.WriteLine($"✅ 错误处理: '{input}' -> {(hasDialectType ? "成功解析" : "解析失败")}");
                }
                catch (ArgumentNullException) when (input == null || input == "")
                {
                    // 预期的异常
                    Console.WriteLine($"✅ 预期异常: '{input ?? "NULL"}' 正确抛出 ArgumentNullException");
                }
            }
        }

        #endregion

        #region Compatibility Tests - 兼容性测试

        [TestMethod]
        public void SqlDefine_BackwardCompatibility_StringToEnumConsistency()
        {
            // 确保字符串和枚举方式产生一致的结果
            var dialectPairs = new[]
            {
                ("MySql", Sqlx.Annotations.SqlDefineTypes.MySql),
                ("SqlServer", Sqlx.Annotations.SqlDefineTypes.SqlServer),
                ("PostgreSql", Sqlx.Annotations.SqlDefineTypes.PostgreSql),
                ("Oracle", Sqlx.Annotations.SqlDefineTypes.Oracle),
                ("DB2", Sqlx.Annotations.SqlDefineTypes.DB2),
                ("SQLite", Sqlx.Annotations.SqlDefineTypes.SQLite)
            };

            foreach (var (dialectString, dialectEnum) in dialectPairs)
            {
                var stringAttr = new SqlDefineAttribute(dialectString);
                var enumAttr = new SqlDefineAttribute(dialectEnum);

                // 两种方式应该产生相同的结果
                Assert.AreEqual(enumAttr.DialectType, stringAttr.DialectType,
                    $"字符串 '{dialectString}' 和枚举 {dialectEnum} 应该产生相同的 DialectType");

                // 生成器映射也应该一致
                var stringMappedSqlDefine = MapEnumToSqlDefine((int)stringAttr.DialectType!);
                var enumMappedSqlDefine = MapEnumToSqlDefine((int)enumAttr.DialectType!);

                Assert.AreEqual(enumMappedSqlDefine.ColumnLeft, stringMappedSqlDefine.ColumnLeft);
                Assert.AreEqual(enumMappedSqlDefine.ColumnRight, stringMappedSqlDefine.ColumnRight);
                Assert.AreEqual(enumMappedSqlDefine.ParameterPrefix, stringMappedSqlDefine.ParameterPrefix);

                Console.WriteLine($"✅ 兼容性验证: '{dialectString}' 和 {dialectEnum} 产生一致结果");
            }
        }

        #endregion
    }
}
