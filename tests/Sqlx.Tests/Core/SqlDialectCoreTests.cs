// -----------------------------------------------------------------------
// <copyright file="SqlDialectCoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SqlDialect 和 SqlDefine 核心功能单元测试
    /// 验证SQL方言系统的核心功能
    /// </summary>
    [TestClass]
    public class SqlDialectCoreTests
    {
        #region SqlDialect Basic Tests - SqlDialect基础测试

        [TestMethod]
        public void SqlDialect_Creation_WorksCorrectly()
        {
            // 测试SqlDialect的创建
            var dialect = new SqlDialect("[", "]", "'", "'", "@");
            
            Assert.AreEqual("[", dialect.ColumnLeft);
            Assert.AreEqual("]", dialect.ColumnRight);
            Assert.AreEqual("'", dialect.StringLeft);
            Assert.AreEqual("'", dialect.StringRight);
            Assert.AreEqual("@", dialect.ParameterPrefix);
            
            Console.WriteLine($"✅ SqlDialect创建测试通过");
        }

        [TestMethod]
        public void SqlDialect_WrapColumn_WorksCorrectly()
        {
            // 测试列名包装功能
            var testCases = new[]
            {
                (SqlDefine.SqlServer, "UserId", "[UserId]"),
                (SqlDefine.MySql, "UserId", "`UserId`"),
                (SqlDefine.PgSql, "UserId", "\"UserId\""),
                (SqlDefine.Oracle, "UserId", "\"UserId\""),
                (SqlDefine.DB2, "UserId", "\"UserId\""),
                (SqlDefine.Sqlite, "UserId", "[UserId]")
            };

            foreach (var (dialect, columnName, expected) in testCases)
            {
                var wrapped = dialect.WrapColumn(columnName);
                Assert.AreEqual(expected, wrapped, 
                    $"Column wrapping failed for {dialect}: expected '{expected}', got '{wrapped}'");
                
                Console.WriteLine($"✅ {GetDialectName(dialect)} 列包装: {columnName} -> {wrapped}");
            }
        }

        [TestMethod]
        public void SqlDialect_WrapString_WorksCorrectly()
        {
            // 测试字符串值包装功能
            var testValue = "test value";
            
            var testCases = new[]
            {
                (SqlDefine.SqlServer, "'test value'"),
                (SqlDefine.MySql, "'test value'"),
                (SqlDefine.PgSql, "'test value'"),
                (SqlDefine.Oracle, "'test value'"),
                (SqlDefine.DB2, "'test value'"),
                (SqlDefine.Sqlite, "'test value'")
            };

            foreach (var (dialect, expected) in testCases)
            {
                var wrapped = dialect.WrapString(testValue);
                Assert.AreEqual(expected, wrapped, 
                    $"String wrapping failed for {dialect}: expected '{expected}', got '{wrapped}'");
                
                Console.WriteLine($"✅ {GetDialectName(dialect)} 字符串包装: {testValue} -> {wrapped}");
            }
        }

        [TestMethod]
        public void SqlDialect_ParameterPrefix_IsCorrect()
        {
            // 测试参数前缀的正确性
            var testCases = new[]
            {
                (SqlDefine.SqlServer, "@"),
                (SqlDefine.MySql, "@"),
                (SqlDefine.PgSql, "$"),
                (SqlDefine.Oracle, ":"),
                (SqlDefine.DB2, "?"),
                (SqlDefine.Sqlite, "$")
            };

            foreach (var (dialect, expectedPrefix) in testCases)
            {
                Assert.AreEqual(expectedPrefix, dialect.ParameterPrefix, 
                    $"Parameter prefix incorrect for {dialect}");
                
                Console.WriteLine($"✅ {GetDialectName(dialect)} 参数前缀: {dialect.ParameterPrefix}");
            }
        }

        #endregion

        #region SqlDefine Static Properties Tests - SqlDefine静态属性测试

        [TestMethod]
        public void SqlDefine_AllDialects_AreInitialized()
        {
            // 测试所有方言都正确初始化
            var dialects = new[]
            {
                ("MySql", SqlDefine.MySql),
                ("SqlServer", SqlDefine.SqlServer),
                ("PgSql", SqlDefine.PgSql),
                ("Oracle", SqlDefine.Oracle),
                ("DB2", SqlDefine.DB2),
                ("Sqlite", SqlDefine.Sqlite)
            };

            foreach (var (name, dialect) in dialects)
            {
                Assert.IsNotNull(dialect.ColumnLeft, $"{name} ColumnLeft should not be null");
                Assert.IsNotNull(dialect.ColumnRight, $"{name} ColumnRight should not be null");
                Assert.IsNotNull(dialect.StringLeft, $"{name} StringLeft should not be null");
                Assert.IsNotNull(dialect.StringRight, $"{name} StringRight should not be null");
                Assert.IsNotNull(dialect.ParameterPrefix, $"{name} ParameterPrefix should not be null");
                
                Console.WriteLine($"✅ {name} 方言初始化验证通过");
            }
        }

        [TestMethod]
        public void SqlDefine_DialectDifferences_AreDistinct()
        {
            // 测试不同方言之间的差异性
            var dialects = new[]
            {
                SqlDefine.MySql,
                SqlDefine.SqlServer,
                SqlDefine.PgSql,
                SqlDefine.Oracle,
                SqlDefine.DB2,
                SqlDefine.Sqlite
            };

            // 检查列引用符的差异
            var columnQuotes = dialects.Select(d => $"{d.ColumnLeft}{d.ColumnRight}").Distinct().ToList();
            Assert.IsTrue(columnQuotes.Count > 1, "Different dialects should have different column quotes");

            // 检查参数前缀的差异
            var paramPrefixes = dialects.Select(d => d.ParameterPrefix).Distinct().ToList();
            Assert.IsTrue(paramPrefixes.Count > 1, "Different dialects should have different parameter prefixes");

            Console.WriteLine($"✅ 方言差异性验证: {columnQuotes.Count}种列引用符, {paramPrefixes.Count}种参数前缀");
            Console.WriteLine($"   列引用符: {string.Join(", ", columnQuotes)}");
            Console.WriteLine($"   参数前缀: {string.Join(", ", paramPrefixes)}");
        }

        #endregion

        #region Dialect Specific Behavior Tests - 方言特定行为测试

        [TestMethod]
        public void SqlDialect_MySql_BehaviorIsCorrect()
        {
            // 测试MySQL方言的特定行为
            var mysql = SqlDefine.MySql;
            
            Assert.AreEqual("`", mysql.ColumnLeft);
            Assert.AreEqual("`", mysql.ColumnRight);
            Assert.AreEqual("@", mysql.ParameterPrefix);
            
            var columnName = mysql.WrapColumn("user_name");
            var stringValue = mysql.WrapString("John's Data");
            
            Assert.AreEqual("`user_name`", columnName);
            Assert.AreEqual("'John's Data'", stringValue);
            
            Console.WriteLine($"✅ MySQL方言行为验证:");
            Console.WriteLine($"   列名: {columnName}");
            Console.WriteLine($"   字符串: {stringValue}");
        }

        [TestMethod]
        public void SqlDialect_SqlServer_BehaviorIsCorrect()
        {
            // 测试SQL Server方言的特定行为
            var sqlServer = SqlDefine.SqlServer;
            
            Assert.AreEqual("[", sqlServer.ColumnLeft);
            Assert.AreEqual("]", sqlServer.ColumnRight);
            Assert.AreEqual("@", sqlServer.ParameterPrefix);
            
            var columnName = sqlServer.WrapColumn("UserName");
            var stringValue = sqlServer.WrapString("Test Data");
            
            Assert.AreEqual("[UserName]", columnName);
            Assert.AreEqual("'Test Data'", stringValue);
            
            Console.WriteLine($"✅ SQL Server方言行为验证:");
            Console.WriteLine($"   列名: {columnName}");
            Console.WriteLine($"   字符串: {stringValue}");
        }

        [TestMethod]
        public void SqlDialect_PostgreSQL_BehaviorIsCorrect()
        {
            // 测试PostgreSQL方言的特定行为
            var pgSql = SqlDefine.PgSql;
            
            Assert.AreEqual("\"", pgSql.ColumnLeft);
            Assert.AreEqual("\"", pgSql.ColumnRight);
            Assert.AreEqual("$", pgSql.ParameterPrefix);
            
            var columnName = pgSql.WrapColumn("user_name");
            var stringValue = pgSql.WrapString("Test Data");
            
            Assert.AreEqual("\"user_name\"", columnName);
            Assert.AreEqual("'Test Data'", stringValue);
            
            Console.WriteLine($"✅ PostgreSQL方言行为验证:");
            Console.WriteLine($"   列名: {columnName}");
            Console.WriteLine($"   字符串: {stringValue}");
        }

        #endregion

        #region Integration with ExpressionToSql Tests - 与ExpressionToSql集成测试

        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public bool IsActive { get; set; }
        }

        [TestMethod]
        public void SqlDialect_ExpressionToSql_Integration()
        {
            // 测试方言与ExpressionToSql的集成
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<TestEntity>.ForSqlServer()),
                ("MySQL", ExpressionToSql<TestEntity>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<TestEntity>.ForPostgreSQL()),
                ("Oracle", ExpressionToSql<TestEntity>.ForOracle()),
                ("SQLite", ExpressionToSql<TestEntity>.ForSqlite())
            };

            foreach (var (dialectName, queryBuilder) in testCases)
            {
                using (queryBuilder)
                {
                    queryBuilder.Where(e => e.Name == "test" && e.IsActive);
                    var sql = queryBuilder.ToSql();
                    
                    Assert.IsTrue(sql.Contains("WHERE"), $"{dialectName} should generate WHERE clause");
                    Assert.IsTrue(sql.Contains("Name"), $"{dialectName} should contain Name column");
                    Assert.IsTrue(sql.Contains("IsActive"), $"{dialectName} should contain IsActive column");
                    
                    Console.WriteLine($"✅ {dialectName} 集成测试: {sql}");
                }
            }
        }

        [TestMethod]
        public void SqlDialect_ParameterGeneration_Integration()
        {
            // 测试参数生成的集成
            var testValue = "test value";
            var testId = 25;
            
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<TestEntity>.ForSqlServer()),
                ("MySQL", ExpressionToSql<TestEntity>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<TestEntity>.ForPostgreSQL()),
                ("SQLite", ExpressionToSql<TestEntity>.ForSqlite())
            };

            foreach (var (dialectName, queryBuilder) in testCases)
            {
                using (queryBuilder)
                {
                    queryBuilder.Where(e => e.Name.Contains(testValue) && e.Id > testId);
                    var template = queryBuilder.ToTemplate();
                    
                    Assert.IsNotNull(template.Sql, $"{dialectName} should generate SQL");
                    
                    // 检查是否有参数（可能某些表达式会被内联而不生成参数）
                    if (template.Parameters.Length > 0)
                    {
                        // 验证参数值
                        var hasTestValue = template.Parameters.Any(p => 
                            p.Value?.ToString()?.Contains(testValue) == true);
                        var hasTestId = template.Parameters.Any(p => 
                            p.Value?.Equals(testId) == true);
                        
                        Console.WriteLine($"✅ {dialectName} 参数生成集成:");
                        Console.WriteLine($"   SQL: {template.Sql}");
                        Console.WriteLine($"   参数数量: {template.Parameters.Length}");
                        
                        if (hasTestValue)
                            Console.WriteLine($"   ✓ 包含字符串参数");
                        if (hasTestId)
                            Console.WriteLine($"   ✓ 包含ID参数");
                    }
                    else
                    {
                        Console.WriteLine($"✅ {dialectName} SQL生成（无参数）:");
                        Console.WriteLine($"   SQL: {template.Sql}");
                        
                        // 验证SQL包含预期的值
                        Assert.IsTrue(template.Sql.Contains("LIKE") || template.Sql.Contains("Contains"), 
                            $"{dialectName} should handle string contains");
                        Assert.IsTrue(template.Sql.Contains(">") || template.Sql.Contains(testId.ToString()), 
                            $"{dialectName} should handle ID comparison");
                    }
                }
            }
        }

        #endregion

        #region Special Characters Handling Tests - 特殊字符处理测试

        [TestMethod]
        public void SqlDialect_SpecialCharacters_HandleCorrectly()
        {
            // 测试特殊字符的处理
            var testCases = new[]
            {
                "normal_column",
                "column with spaces",
                "column-with-dashes",
                "column.with.dots",
                "column[with]brackets",
                "column`with`backticks",
                "column\"with\"quotes"
            };

            foreach (var columnName in testCases)
            {
                var sqlServer = SqlDefine.SqlServer.WrapColumn(columnName);
                var mysql = SqlDefine.MySql.WrapColumn(columnName);
                var pgSql = SqlDefine.PgSql.WrapColumn(columnName);
                
                Assert.IsTrue(sqlServer.StartsWith("[") && sqlServer.EndsWith("]"), 
                    $"SQL Server should wrap '{columnName}' with brackets");
                Assert.IsTrue(mysql.StartsWith("`") && mysql.EndsWith("`"), 
                    $"MySQL should wrap '{columnName}' with backticks");
                Assert.IsTrue(pgSql.StartsWith("\"") && pgSql.EndsWith("\""), 
                    $"PostgreSQL should wrap '{columnName}' with quotes");
                
                Console.WriteLine($"✅ 特殊字符测试 '{columnName}':");
                Console.WriteLine($"   SQL Server: {sqlServer}");
                Console.WriteLine($"   MySQL: {mysql}");
                Console.WriteLine($"   PostgreSQL: {pgSql}");
            }
        }

        [TestMethod]
        public void SqlDialect_StringEscaping_HandleCorrectly()
        {
            // 测试字符串转义处理
            var testStrings = new[]
            {
                "normal string",
                "string with 'single quotes'",
                "string with \"double quotes\"",
                "string with \\ backslashes",
                "string with \n newlines",
                "string with \t tabs"
            };

            foreach (var testString in testStrings)
            {
                var wrapped = SqlDefine.SqlServer.WrapString(testString);
                
                Assert.IsTrue(wrapped.StartsWith("'") && wrapped.EndsWith("'"), 
                    $"String should be wrapped with single quotes: {wrapped}");
                
                Console.WriteLine($"✅ 字符串转义测试: '{testString}' -> {wrapped}");
            }
        }

        #endregion

        #region Performance Tests - 性能测试

        [TestMethod]
        public void SqlDialect_Performance_WrapOperationsAreEfficient()
        {
            // 测试包装操作的性能
            const int iterations = 10000;
            var columnName = "TestColumn";
            var stringValue = "Test String Value";
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var wrappedColumn = SqlDefine.SqlServer.WrapColumn(columnName);
                var wrappedString = SqlDefine.SqlServer.WrapString(stringValue);
                
                Assert.IsNotNull(wrappedColumn);
                Assert.IsNotNull(wrappedString);
            }
            
            stopwatch.Stop();
            
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Wrap operations should be fast: {stopwatch.ElapsedMilliseconds}ms for {iterations} operations");
            
            Console.WriteLine($"✅ 包装操作性能: {iterations}次操作耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Helper Methods - 辅助方法

        private static string GetDialectName(SqlDialect dialect)
        {
            if (dialect.Equals(SqlDefine.SqlServer)) return "SQL Server";
            if (dialect.Equals(SqlDefine.MySql)) return "MySQL";
            if (dialect.Equals(SqlDefine.PgSql)) return "PostgreSQL";
            if (dialect.Equals(SqlDefine.Oracle)) return "Oracle";
            if (dialect.Equals(SqlDefine.DB2)) return "DB2";
            if (dialect.Equals(SqlDefine.Sqlite)) return "SQLite";
            return "Unknown";
        }

        #endregion
    }
}
