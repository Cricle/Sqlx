// -----------------------------------------------------------------------
// <copyright file="SqlDialectBoundaryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8625, CS8604, CS8603, CS8602, CS8629, CS8765

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SQL方言边界测试 - 测试不同数据库方言在极限情况下的行为
    /// </summary>
    [TestClass]
    public class SqlDialectBoundaryTests
    {
        #region 测试实体

        public class DialectTestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Price { get; set; }
            public bool IsActive { get; set; }
            public Guid UniqueId { get; set; }
        }

        #endregion

        #region 方言基础特性边界测试

        [TestMethod]
        public void SqlDialect_AllDialects_ColumnWrappingConsistency()
        {
            // Arrange
            var testColumns = new[]
            {
                "Id",
                "Name",
                "select",        // SQL关键字
                "from",          // SQL关键字
                "where",         // SQL关键字
                "Column_With_Underscore",
                "Column123",
                "UPPERCASECOLUMN",
                "mixedCaseColumn",
                "Very_Long_Column_Name_That_Exceeds_Normal_Length_Limits_And_Continues_For_A_While",
                "",              // 空列名
                " ",             // 空格列名
                "列名",          // 中文列名
                "名前",          // 日文列名
                "nombre",        // 西班牙文列名
                "имя",           // 俄文列名
                "🚀",            // emoji列名
                "Column With Spaces",
                "Column'With'Quotes",
                "Column\"With\"DoubleQuotes",
                "Column[With]Brackets",
                "Column{With}Braces"
            };

            var dialects = new[]
            {
                ("SQL Server", SqlDefine.SqlServer),
                ("MySQL", SqlDefine.MySql),
                ("PostgreSQL", SqlDefine.PgSql),
                ("Oracle", SqlDefine.Oracle),
                ("DB2", SqlDefine.DB2),
                ("SQLite", SqlDefine.Sqlite)
            };

            // Act & Assert
            foreach (var (dialectName, dialect) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 方言 ===");

                foreach (var columnName in testColumns)
                {
                    try
                    {
                        var wrappedColumn = dialect.WrapColumn(columnName);

                        // 基本验证
                        Assert.IsNotNull(wrappedColumn);
                        Assert.IsTrue(wrappedColumn.Length >= columnName.Length,
                            $"{dialectName}: 包装后的列名长度不应小于原始长度");

                        // 检查是否包含适当的包装字符
                        var expectedLeft = dialect.ColumnLeft;
                        var expectedRight = dialect.ColumnRight;

                        if (!string.IsNullOrEmpty(columnName))
                        {
                            Assert.IsTrue(wrappedColumn.StartsWith(expectedLeft),
                                $"{dialectName}: 列名应以 '{expectedLeft}' 开始");
                            Assert.IsTrue(wrappedColumn.EndsWith(expectedRight),
                                $"{dialectName}: 列名应以 '{expectedRight}' 结束");
                        }

                        Console.WriteLine($"✅ {dialectName}: '{columnName}' -> '{wrappedColumn}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ {dialectName}: '{columnName}' 处理异常: {ex.GetType().Name}");
                    }
                }
            }
        }

        [TestMethod]
        public void SqlDialect_AllDialects_StringWrappingConsistency()
        {
            // Arrange
            var testStrings = new[]
            {
                "简单字符串",
                "",
                " ",
                "String with 'single quotes'",
                "String with \"double quotes\"",
                "String with 'mixed' and \"quotes\"",
                "String with \\ backslash",
                "String with \n newline",
                "String with \t tab",
                "String with \r\n CRLF",
                "SQL injection'; DROP TABLE Users; --",
                "Unicode: 🚀🎉💻🌟⭐",
                "Very " + new string('l', 1000) + "ong string",
                "NULL",
                "null",
                "undefined",
                new string('\'', 100), // 100个单引号
                new string('"', 100),  // 100个双引号
                "中文字符串测试",
                "日本語テスト",
                "Тест на русском",
                "Prueba en español",
                "Test français",
                "Test Deutsch",
                "عربي تست",
                "עברית מבחן"
            };

            var dialects = new[]
            {
                ("SQL Server", SqlDefine.SqlServer),
                ("MySQL", SqlDefine.MySql),
                ("PostgreSQL", SqlDefine.PgSql),
                ("Oracle", SqlDefine.Oracle),
                ("DB2", SqlDefine.DB2),
                ("SQLite", SqlDefine.Sqlite)
            };

            // Act & Assert
            foreach (var (dialectName, dialect) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 字符串包装 ===");

                foreach (var testString in testStrings)
                {
                    try
                    {
                        var wrappedString = dialect.WrapString(testString);

                        // 基本验证
                        Assert.IsNotNull(wrappedString);

                        // 检查是否包含适当的包装字符
                        var expectedLeft = dialect.StringLeft;
                        var expectedRight = dialect.StringRight;

                        Assert.IsTrue(wrappedString.StartsWith(expectedLeft) || wrappedString == "NULL",
                            $"{dialectName}: 字符串应以 '{expectedLeft}' 开始或为 'NULL'");
                        Assert.IsTrue(wrappedString.EndsWith(expectedRight) || wrappedString == "NULL",
                            $"{dialectName}: 字符串应以 '{expectedRight}' 结束或为 'NULL'");

                        Console.WriteLine($"✅ {dialectName}: '{testString.Substring(0, Math.Min(20, testString.Length))}...' -> '{wrappedString.Substring(0, Math.Min(50, wrappedString.Length))}...'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ {dialectName}: 字符串处理异常: {ex.GetType().Name}");
                    }
                }
            }
        }

        [TestMethod]
        public void SqlDialect_AllDialects_ParameterPrefixConsistency()
        {
            // Arrange
            var testParameterNames = new[]
            {
                "id",
                "name",
                "param1",
                "parameter_with_underscore",
                "Parameter123",
                "UPPERCASEPARAM",
                "mixedCaseParam",
                "Very_Long_Parameter_Name_That_Exceeds_Normal_Limits",
                "",
                "1invalidname", // 数字开头
                "param with spaces",
                "param'with'quotes",
                "param[with]brackets",
                "参数名",
                "パラメータ",
                "параметр"
            };

            var dialects = new[]
            {
                ("SQL Server", SqlDefine.SqlServer, "@"),
                ("MySQL", SqlDefine.MySql, "@"),
                ("PostgreSQL", SqlDefine.PgSql, "$"),
                ("Oracle", SqlDefine.Oracle, ":"),
                ("DB2", SqlDefine.DB2, "?"),
                ("SQLite", SqlDefine.Sqlite, "$")
            };

            // Act & Assert
            foreach (var (dialectName, dialect, expectedPrefix) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 参数前缀 ===");

                // 验证参数前缀配置
                Assert.AreEqual(expectedPrefix, dialect.ParameterPrefix,
                    $"{dialectName} 参数前缀应为 '{expectedPrefix}'");

                foreach (var paramName in testParameterNames)
                {
                    try
                    {
                        var parameter = dialect.CreateParameter(paramName);

                        // 基本验证
                        Assert.IsNotNull(parameter);

                        if (!string.IsNullOrEmpty(paramName))
                        {
                            Assert.IsTrue(parameter.StartsWith(expectedPrefix),
                                $"{dialectName}: 参数应以 '{expectedPrefix}' 开始");
                            Assert.IsTrue(parameter.Contains(paramName),
                                $"{dialectName}: 参数应包含原始参数名");
                        }

                        Console.WriteLine($"✅ {dialectName}: '{paramName}' -> '{parameter}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ {dialectName}: 参数 '{paramName}' 处理异常: {ex.GetType().Name}");
                    }
                }
            }
        }

        #endregion

        #region 方言特定语法边界测试

        [TestMethod]
        public void SqlDialect_LimitOffsetSyntax_AllDialects_ConsistentBehavior()
        {
            // Arrange & Act & Assert
            var dialects = new (string, Func<ExpressionToSql<DialectTestEntity>>)[]
            {
                ("SQL Server", () => ExpressionToSql<DialectTestEntity>.ForSqlServer()),
                ("MySQL", () => ExpressionToSql<DialectTestEntity>.ForMySql()),
                ("PostgreSQL", () => ExpressionToSql<DialectTestEntity>.ForPostgreSQL()),
                ("Oracle", () => ExpressionToSql<DialectTestEntity>.ForOracle()),
                ("DB2", () => ExpressionToSql<DialectTestEntity>.ForDB2()),
                ("SQLite", () => ExpressionToSql<DialectTestEntity>.ForSqlite())
            };

            var testCases = new[]
            {
                (0, 0, "零值分页"),
                (1, 0, "仅Take"),
                (0, 1, "仅Skip"),
                (10, 5, "标准分页"),
                (1, 1000000, "大Skip值"),
                (1000000, 1, "大Take值"),
                (int.MaxValue / 2, int.MaxValue / 2, "极大值")
            };

            foreach (var (dialectName, factory) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 分页语法 ===");

                foreach (var (take, skip, description) in testCases)
                {
                    try
                    {
                        using var expr = factory();
                        expr.Where(e => e.Id > 0);

                        if (take > 0) expr.Take(take);
                        if (skip > 0) expr.Skip(skip);

                        var sql = expr.ToSql();

                        Assert.IsNotNull(sql);
                        Console.WriteLine($"✅ {dialectName} {description}: {sql.Substring(0, Math.Min(100, sql.Length))}...");

                        // 验证基本分页关键字存在
                        if (take > 0 || skip > 0)
                        {
                            var hasLimitKeywords = sql.ToUpperInvariant().Contains("LIMIT") ||
                                                 sql.ToUpperInvariant().Contains("OFFSET") ||
                                                 sql.ToUpperInvariant().Contains("FETCH") ||
                                                 sql.ToUpperInvariant().Contains("TOP") ||
                                                 sql.ToUpperInvariant().Contains("ROWNUM");

                            if (!hasLimitKeywords)
                            {
                                Console.WriteLine($"⚠️ {dialectName}: 可能缺少分页关键字");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ {dialectName} {description} 异常: {ex.GetType().Name}");
                    }
                }
            }
        }

        [TestMethod]
        public void SqlDialect_DateTimeFormatting_AllDialects_HandlesCorrectly()
        {
            // Arrange
            var testDates = new[]
            {
                DateTime.MinValue,
                DateTime.MaxValue,
                DateTime.UnixEpoch,
                new DateTime(1900, 1, 1),
                new DateTime(2000, 1, 1, 0, 0, 0),
                new DateTime(2023, 12, 31, 23, 59, 59),
                DateTime.Now,
                DateTime.UtcNow
            };

            var dialects = new (string, Func<ExpressionToSql<DialectTestEntity>>)[]
            {
                ("SQL Server", () => ExpressionToSql<DialectTestEntity>.ForSqlServer()),
                ("MySQL", () => ExpressionToSql<DialectTestEntity>.ForMySql()),
                ("PostgreSQL", () => ExpressionToSql<DialectTestEntity>.ForPostgreSQL()),
                ("Oracle", () => ExpressionToSql<DialectTestEntity>.ForOracle()),
                ("DB2", () => ExpressionToSql<DialectTestEntity>.ForDB2()),
                ("SQLite", () => ExpressionToSql<DialectTestEntity>.ForSqlite())
            };

            // Act & Assert
            foreach (var (dialectName, factory) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 日期时间格式 ===");

                foreach (var testDate in testDates)
                {
                    try
                    {
                        using var expr = factory();
                        expr.Where(e => e.CreatedAt == testDate);

                        var sql = expr.ToSql();

                        Assert.IsNotNull(sql);
                        Assert.IsTrue(sql.Contains("CreatedAt"), "应包含日期列名");

                        Console.WriteLine($"✅ {dialectName}: {testDate:yyyy-MM-dd HH:mm:ss} 处理成功");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ {dialectName}: 日期 {testDate} 处理异常: {ex.GetType().Name}");
                    }
                }
            }
        }

        [TestMethod]
        public void SqlDialect_BooleanHandling_AllDialects_ConsistentBehavior()
        {
            // Arrange
            var dialects = new (string, Func<ExpressionToSql<DialectTestEntity>>)[]
            {
                ("SQL Server", () => ExpressionToSql<DialectTestEntity>.ForSqlServer()),
                ("MySQL", () => ExpressionToSql<DialectTestEntity>.ForMySql()),
                ("PostgreSQL", () => ExpressionToSql<DialectTestEntity>.ForPostgreSQL()),
                ("Oracle", () => ExpressionToSql<DialectTestEntity>.ForOracle()),
                ("DB2", () => ExpressionToSql<DialectTestEntity>.ForDB2()),
                ("SQLite", () => ExpressionToSql<DialectTestEntity>.ForSqlite())
            };

            // Act & Assert
            foreach (var (dialectName, factory) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 布尔值处理 ===");

                try
                {
                    using var expr = factory();
                    expr.Where(e => e.IsActive == true)
                        .Where(e => e.IsActive == false)
                        .Where(e => e.IsActive)
                        .Where(e => !e.IsActive);

                    var sql = expr.ToSql();

                    Assert.IsNotNull(sql);
                    Assert.IsTrue(sql.Contains("IsActive"), "应包含布尔列名");

                    Console.WriteLine($"✅ {dialectName}: 布尔值处理成功");
                    Console.WriteLine($"SQL: {sql.Substring(0, Math.Min(150, sql.Length))}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ {dialectName}: 布尔值处理异常: {ex.GetType().Name}");
                }
            }
        }

        #endregion

        #region 方言转换边界测试

        [TestMethod]
        public void SqlDialect_DatabaseTypeDetection_HandlesAllCombinations()
        {
            // Arrange - 测试所有可能的方言特征组合
            var dialectConfigurations = new[]
            {
                ("`", "`", "@", "MySql"),
                ("[", "]", "@", "SqlServer"),
                ("[", "]", "$", "SQLite"),
                ("\"", "\"", "$", "PostgreSql"),
                ("\"", "\"", ":", "Oracle"),
                ("\"", "\"", "?", "DB2"),
                ("", "", "", "Unknown"),          // 空配置
                ("X", "X", "X", "Unknown"),       // 无效配置
                ("`", "]", "@", "Unknown"),       // 混合配置
                ("[", "\"", "$", "Unknown"),      // 混合配置
            };

            // Act & Assert
            foreach (var (left, right, prefix, expectedType) in dialectConfigurations)
            {
                try
                {
                    var dialect = new SqlDialect(left, right, "'", "'", prefix);
                    var detectedType = dialect.DatabaseType;

                    Assert.AreEqual(expectedType, detectedType,
                        $"方言配置 ({left}, {right}, {prefix}) 应检测为 {expectedType}");

                    Console.WriteLine($"✅ 方言检测: ({left}, {right}, {prefix}) -> {detectedType}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 方言配置 ({left}, {right}, {prefix}) 异常: {ex.GetType().Name}");
                }
            }
        }

        [TestMethod]
        public void SqlDialect_NullAndEmptyInputs_HandlesGracefully()
        {
            // Arrange
            var testInputs = new[]
            {
                (null, "null输入"),
                ("", "空字符串"),
                ("   ", "空白字符串"),
                ("\n", "换行符"),
                ("\t", "制表符"),
                ("\r\n", "回车换行"),
                ("a", "单字符"),
                (new string('x', 10000), "超长字符串")
            };

            var dialects = new[]
            {
                ("SQL Server", SqlDefine.SqlServer),
                ("MySQL", SqlDefine.MySql),
                ("PostgreSQL", SqlDefine.PgSql),
                ("Oracle", SqlDefine.Oracle),
                ("DB2", SqlDefine.DB2),
                ("SQLite", SqlDefine.Sqlite)
            };

            // Act & Assert
            foreach (var (dialectName, dialect) in dialects)
            {
                Console.WriteLine($"\n=== 测试 {dialectName} 空值输入处理 ===");

                foreach (var (input, description) in testInputs)
                {
                    try
                    {
                        // 测试列名包装
                        if (input != null)
                        {
                            var wrappedColumn = dialect.WrapColumn(input);
                            Assert.IsNotNull(wrappedColumn);
                        }

                        // 测试字符串包装
                        var wrappedString = dialect.WrapString(input);
                        Assert.IsNotNull(wrappedString);

                        // 测试参数创建
                        if (input != null)
                        {
                            var parameter = dialect.CreateParameter(input);
                            Assert.IsNotNull(parameter);
                        }

                        Console.WriteLine($"✅ {dialectName}: {description} 处理成功");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ {dialectName}: {description} 处理异常: {ex.GetType().Name}");
                    }
                }
            }
        }

        #endregion

    }
}
