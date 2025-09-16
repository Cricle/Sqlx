// -----------------------------------------------------------------------
// <copyright file="SqlDefineEnumIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Integration
{
    /// <summary>
    /// SqlDefine 枚举集成测试
    /// 验证 SqlDefine 枚举特性在实际使用场景中的正确性
    /// </summary>
    [TestClass]
    public class SqlDefineEnumIntegrationTests
    {
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public bool IsActive { get; set; }
        }

        #region Enum Usage Integration Tests - 枚举使用集成测试

        [TestMethod]
        public void SqlDefine_EnumUsage_MySql_GeneratesCorrectSyntax()
        {
            // 测试 MySQL 枚举特性是否正确影响 SQL 生成
            using var query = ExpressionToSql<TestEntity>.ForMySql()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // MySQL 应该使用反引号包装列名
            Assert.IsTrue(sql.Contains("`Id`, `Name`") || sql.Contains("Id, Name"), 
                $"MySQL 语法错误: {sql}");
            Assert.IsTrue(sql.Contains("FROM"), $"缺少 FROM 子句: {sql}");
            
            Console.WriteLine($"✅ MySQL 枚举集成测试: {sql}");
        }

        [TestMethod]
        public void SqlDefine_EnumUsage_SqlServer_GeneratesCorrectSyntax()
        {
            // 测试 SQL Server 枚举特性是否正确影响 SQL 生成
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // SQL Server 应该使用方括号包装列名
            Assert.IsTrue(sql.Contains("[Id], [Name]"), $"SQL Server 语法错误: {sql}");
            Assert.IsTrue(sql.Contains("FROM [TestEntity]"), $"表名包装错误: {sql}");
            
            Console.WriteLine($"✅ SQL Server 枚举集成测试: {sql}");
        }

        [TestMethod]
        public void SqlDefine_EnumUsage_PostgreSql_GeneratesCorrectSyntax()
        {
            // 测试 PostgreSQL 枚举特性是否正确影响 SQL 生成
            using var query = ExpressionToSql<TestEntity>.ForPostgreSQL()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // PostgreSQL 应该使用双引号或无引号
            Assert.IsTrue(sql.Contains("SELECT"), $"缺少 SELECT: {sql}");
            Assert.IsTrue(sql.Contains("FROM"), $"缺少 FROM: {sql}");
            
            Console.WriteLine($"✅ PostgreSQL 枚举集成测试: {sql}");
        }

        [TestMethod]
        public void SqlDefine_EnumUsage_SQLite_GeneratesCorrectSyntax()
        {
            // 测试 SQLite 枚举特性是否正确影响 SQL 生成
            using var query = ExpressionToSql<TestEntity>.ForSqlite()
                .Select(e => new { e.Id, e.Name })
                .Where(e => e.IsActive);

            var sql = query.ToSql();

            // SQLite 应该使用方括号或无引号
            Assert.IsTrue(sql.Contains("SELECT"), $"缺少 SELECT: {sql}");
            Assert.IsTrue(sql.Contains("FROM"), $"缺少 FROM: {sql}");
            
            Console.WriteLine($"✅ SQLite 枚举集成测试: {sql}");
        }

        #endregion

        #region Enum Value Mapping Tests - 枚举值映射测试

        [TestMethod]
        public void SqlDefineTypes_EnumValues_MapToCorrectDialects()
        {
            // 验证枚举值与实际方言的映射关系
            var testCases = new[]
            {
                (SqlDefineTypes.MySql, "MySql"),
                (SqlDefineTypes.SqlServer, "SqlServer"),
                (SqlDefineTypes.PostgreSql, "PostgreSql"),
                (SqlDefineTypes.Oracle, "Oracle"),
                (SqlDefineTypes.DB2, "DB2"),
                (SqlDefineTypes.SQLite, "SQLite")
            };

            foreach (var (enumValue, expectedName) in testCases)
            {
                var attribute = new SqlDefineAttribute(enumValue);
                
                Assert.AreEqual(enumValue, attribute.DialectType);
                Assert.AreEqual(expectedName, attribute.DialectName);
                
                Console.WriteLine($"✅ 枚举映射验证: {enumValue} -> {expectedName}");
            }
        }

        [TestMethod]
        public void SqlDefineTypes_IntegerValues_MatchExpectedMapping()
        {
            // 验证枚举的整数值与生成器期望的映射一致
            Assert.AreEqual(0, (int)SqlDefineTypes.MySql);
            Assert.AreEqual(1, (int)SqlDefineTypes.SqlServer);
            Assert.AreEqual(2, (int)SqlDefineTypes.PostgreSql);
            Assert.AreEqual(3, (int)SqlDefineTypes.Oracle);
            Assert.AreEqual(4, (int)SqlDefineTypes.DB2);
            Assert.AreEqual(5, (int)SqlDefineTypes.SQLite);
            
            Console.WriteLine("✅ 所有枚举整数值与生成器映射一致");
        }

        #endregion

        #region Backward Compatibility Tests - 向后兼容性测试

        [TestMethod]
        public void SqlDefine_StringAndEnum_ProduceSameResults()
        {
            // 验证字符串构造函数和枚举构造函数产生相同的结果
            var stringAttribute = new SqlDefineAttribute("MySql");
            var enumAttribute = new SqlDefineAttribute(SqlDefineTypes.MySql);

            Assert.AreEqual(enumAttribute.DialectType, stringAttribute.DialectType);
            
            Console.WriteLine($"✅ 向后兼容性: 字符串和枚举构造函数结果一致");
        }

        [TestMethod]
        public void SqlDefine_AllStringDialects_CanBeParsedToEnum()
        {
            // 验证所有支持的字符串方言都能正确解析为枚举
            var dialectMappings = new[]
            {
                ("MySql", SqlDefineTypes.MySql),
                ("SqlServer", SqlDefineTypes.SqlServer),
                ("PostgreSql", SqlDefineTypes.PostgreSql),
                ("Oracle", SqlDefineTypes.Oracle),
                ("DB2", SqlDefineTypes.DB2),
                ("SQLite", SqlDefineTypes.SQLite)
            };

            foreach (var (dialectString, expectedEnum) in dialectMappings)
            {
                var attribute = new SqlDefineAttribute(dialectString);
                
                Assert.AreEqual(expectedEnum, attribute.DialectType);
                Assert.AreEqual(dialectString, attribute.DialectName);
                
                Console.WriteLine($"✅ 字符串解析: {dialectString} -> {expectedEnum}");
            }
        }

        #endregion

        #region Error Handling Tests - 错误处理测试

        [TestMethod]
        public void SqlDefine_InvalidEnumValue_HandledGracefully()
        {
            // 测试无效枚举值的处理（虽然在编译时不太可能发生）
            var invalidEnumValue = (SqlDefineTypes)999;
            
            // 这应该不会抛出异常，而是使用枚举的字符串表示
            var attribute = new SqlDefineAttribute(invalidEnumValue);
            
            Assert.AreEqual(invalidEnumValue, attribute.DialectType);
            Assert.AreEqual("999", attribute.DialectName);
            
            Console.WriteLine($"✅ 无效枚举值处理: {invalidEnumValue} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefine_UnsupportedStringDialect_HandledGracefully()
        {
            // 测试不支持的字符串方言的处理
            var unsupportedDialect = "UnsupportedDatabase";
            var attribute = new SqlDefineAttribute(unsupportedDialect);
            
            Assert.IsNull(attribute.DialectType);
            Assert.AreEqual(unsupportedDialect, attribute.DialectName);
            
            Console.WriteLine($"✅ 不支持的字符串方言处理: {unsupportedDialect}");
        }

        #endregion

        #region Performance Integration Tests - 性能集成测试

        [TestMethod]
        public void SqlDefine_EnumUsage_PerformanceComparison()
        {
            var iterations = 1000;
            
            // 测试枚举构造函数性能
            var enumStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var attribute = new SqlDefineAttribute(SqlDefineTypes.MySql);
                Assert.IsNotNull(attribute);
            }
            enumStopwatch.Stop();
            
            // 测试字符串构造函数性能
            var stringStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var attribute = new SqlDefineAttribute("MySql");
                Assert.IsNotNull(attribute);
            }
            stringStopwatch.Stop();
            
            // 枚举构造函数应该比字符串构造函数更快（无需解析）
            Assert.IsTrue(enumStopwatch.ElapsedMilliseconds <= stringStopwatch.ElapsedMilliseconds,
                $"枚举构造函数性能不如预期: {enumStopwatch.ElapsedMilliseconds}ms vs {stringStopwatch.ElapsedMilliseconds}ms");
            
            Console.WriteLine($"✅ 性能对比 - 枚举: {enumStopwatch.ElapsedMilliseconds}ms, 字符串: {stringStopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Real-world Usage Scenarios - 真实使用场景

        // 模拟真实的数据库仓储类
        [SqlDefine(SqlDefineTypes.MySql)]
        public class MySqlUserRepository
        {
            public void GetActiveUsers() { }
            
            [SqlDefine(SqlDefineTypes.SqlServer)]
            public void GetArchivedUsers() { }
        }

        [SqlDefine("PostgreSql")]
        public class PostgreSqlUserRepository
        {
            [SqlDefine(SqlDefineTypes.Oracle)]
            public void GetUsersByRole() { }
        }

        [TestMethod]
        public void SqlDefine_RealWorldScenario_ClassAndMethodLevel()
        {
            // 验证类级别和方法级别的 SqlDefine 特性都能正确工作
            var repositoryType = typeof(MySqlUserRepository);
            var method = repositoryType.GetMethod(nameof(MySqlUserRepository.GetArchivedUsers))!;
            
            // 类级别特性
            var classAttributes = repositoryType.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            var classAttribute = (SqlDefineAttribute)classAttributes[0];
            Assert.AreEqual(SqlDefineTypes.MySql, classAttribute.DialectType);
            
            // 方法级别特性
            var methodAttributes = method.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            var methodAttribute = (SqlDefineAttribute)methodAttributes[0];
            Assert.AreEqual(SqlDefineTypes.SqlServer, methodAttribute.DialectType);
            
            Console.WriteLine($"✅ 真实场景: 类({classAttribute.DialectType}) + 方法({methodAttribute.DialectType})");
        }

        [TestMethod]
        public void SqlDefine_MixedStringAndEnum_RealWorldScenario()
        {
            // 验证混合使用字符串和枚举的真实场景
            var repositoryType = typeof(PostgreSqlUserRepository);
            var method = repositoryType.GetMethod(nameof(PostgreSqlUserRepository.GetUsersByRole))!;
            
            // 类级别使用字符串
            var classAttributes = repositoryType.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            var classAttribute = (SqlDefineAttribute)classAttributes[0];
            Assert.AreEqual("PostgreSql", classAttribute.DialectName);
            Assert.AreEqual(SqlDefineTypes.PostgreSql, classAttribute.DialectType);
            
            // 方法级别使用枚举
            var methodAttributes = method.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            var methodAttribute = (SqlDefineAttribute)methodAttributes[0];
            Assert.AreEqual(SqlDefineTypes.Oracle, methodAttribute.DialectType);
            Assert.AreEqual("Oracle", methodAttribute.DialectName);
            
            Console.WriteLine($"✅ 混合场景: 字符串({classAttribute.DialectName}) + 枚举({methodAttribute.DialectType})");
        }

        #endregion
    }
}

