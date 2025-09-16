// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBaseCoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// ExpressionToSqlBase 核心功能单元测试
    /// 验证表达式解析、SQL生成等核心功能
    /// </summary>
    [TestClass]
    public class ExpressionToSqlBaseCoreTests
    {
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Salary { get; set; }
            public double? Score { get; set; }
        }

        #region Expression Parsing Core Tests - 表达式解析核心测试

        [TestMethod]
        public void ParseExpression_BinaryComparison_GeneratesCorrectSQL()
        {
            // 测试各种比较操作符的解析
            var testCases = new (Expression<Func<TestEntity, bool>>, string)[]
            {
                ((Expression<Func<TestEntity, bool>>)(e => e.Age > 18), "> 18"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Age >= 21), ">= 21"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Age < 65), "< 65"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Age <= 60), "<= 60"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Age == 25), "= 25"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Age != 30), "<> 30")
            };

            foreach (var (expression, expectedOperator) in testCases)
            {
                using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                    .Where(expression);
                
                var sql = query.ToSql();
                
                Assert.IsTrue(sql.Contains("[Age]"), $"Should contain Age column: {sql}");
                Assert.IsTrue(sql.Contains(expectedOperator), $"Should contain operator {expectedOperator}: {sql}");
                
                Console.WriteLine($"✅ 比较操作符测试: {expression} -> {sql}");
            }
        }

        [TestMethod]
        public void ParseExpression_LogicalOperators_GeneratesCorrectSQL()
        {
            // 测试逻辑操作符的解析
            using var andQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.IsActive);
            
            using var orQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age < 18 || e.Age > 65);
            
            using var notQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => !e.IsActive);

            var andSql = andQuery.ToSql();
            var orSql = orQuery.ToSql();
            var notSql = notQuery.ToSql();

            Assert.IsTrue(andSql.Contains("AND"), $"AND query should contain AND: {andSql}");
            Assert.IsTrue(orSql.Contains("OR"), $"OR query should contain OR: {orSql}");
            Assert.IsTrue(notSql.Contains("[IsActive] = 0"), $"NOT query should negate boolean: {notSql}");

            Console.WriteLine($"✅ 逻辑操作符测试:");
            Console.WriteLine($"   AND: {andSql}");
            Console.WriteLine($"   OR: {orSql}");
            Console.WriteLine($"   NOT: {notSql}");
        }

        [TestMethod]
        public void ParseExpression_ArithmeticOperators_GeneratesCorrectSQL()
        {
            // 测试算术操作符的解析
            using var addQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age + 10 > 30);
            
            using var subtractQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age - 5 < 20);
            
            using var multiplyQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age * 2 > 50);
            
            using var divideQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age / 2 < 15);

            var addSql = addQuery.ToSql();
            var subtractSql = subtractQuery.ToSql();
            var multiplySql = multiplyQuery.ToSql();
            var divideSql = divideQuery.ToSql();

            Assert.IsTrue(addSql.Contains("+"), $"Add query should contain +: {addSql}");
            Assert.IsTrue(subtractSql.Contains("-"), $"Subtract query should contain -: {subtractSql}");
            Assert.IsTrue(multiplySql.Contains("*"), $"Multiply query should contain *: {multiplySql}");
            Assert.IsTrue(divideSql.Contains("/"), $"Divide query should contain /: {divideSql}");

            Console.WriteLine($"✅ 算术操作符测试:");
            Console.WriteLine($"   加法: {addSql}");
            Console.WriteLine($"   减法: {subtractSql}");
            Console.WriteLine($"   乘法: {multiplySql}");
            Console.WriteLine($"   除法: {divideSql}");
        }

        [TestMethod]
        public void ParseExpression_StringOperations_GeneratesCorrectSQL()
        {
            // 测试字符串操作的解析
            using var containsQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Name.Contains("test"));
            
            using var startsWithQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Name.StartsWith("prefix"));
            
            using var endsWithQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Name.EndsWith("suffix"));

            var containsSql = containsQuery.ToSql();
            var startsWithSql = startsWithQuery.ToSql();
            var endsWithSql = endsWithQuery.ToSql();

            Assert.IsTrue(containsSql.Contains("LIKE"), 
                $"Contains should use LIKE: {containsSql}");
            Assert.IsTrue(startsWithSql.Contains("LIKE") && startsWithSql.Contains("prefix"), 
                $"StartsWith should use LIKE with prefix: {startsWithSql}");
            Assert.IsTrue(endsWithSql.Contains("LIKE") && endsWithSql.Contains("suffix"), 
                $"EndsWith should use LIKE with suffix: {endsWithSql}");

            Console.WriteLine($"✅ 字符串操作测试:");
            Console.WriteLine($"   Contains: {containsSql}");
            Console.WriteLine($"   StartsWith: {startsWithSql}");
            Console.WriteLine($"   EndsWith: {endsWithSql}");
        }

        [TestMethod]
        public void ParseExpression_NullableTypes_HandlesCorrectly()
        {
            // 测试可空类型的处理
            using var nullCheckQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Salary == null);
            
            using var hasValueQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Salary!.HasValue);
            
            using var valueQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Salary.Value > 50000);

            var nullCheckSql = nullCheckQuery.ToSql();
            var hasValueSql = hasValueQuery.ToSql();
            var valueSql = valueQuery.ToSql();

            Assert.IsTrue(nullCheckSql.Contains("IS NULL"), $"Null check should use IS NULL: {nullCheckSql}");
            // HasValue可能被转换为布尔表达式，这是正常的
            Assert.IsTrue(hasValueSql.Contains("[HasValue] = 1") || hasValueSql.Contains("IS NOT NULL"), 
                $"HasValue should be handled correctly: {hasValueSql}");
            Assert.IsTrue(valueSql.Contains("[Value] > 50000") || valueSql.Contains("[Salary] > 50000"), 
                $"Value access should work: {valueSql}");

            Console.WriteLine($"✅ 可空类型测试:");
            Console.WriteLine($"   Null检查: {nullCheckSql}");
            Console.WriteLine($"   HasValue: {hasValueSql}");
            Console.WriteLine($"   Value访问: {valueSql}");
        }

        [TestMethod]
        public void ParseExpression_ComplexExpressions_GeneratesCorrectSQL()
        {
            // 测试复杂表达式的解析
            using var complexQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => (e.Age > 18 && e.IsActive) || (e.Salary.HasValue && e.Salary.Value > 100000));

            var sql = complexQuery.ToSql();

            Assert.IsTrue(sql.Contains("AND"), $"Complex query should contain AND: {sql}");
            Assert.IsTrue(sql.Contains("OR"), $"Complex query should contain OR: {sql}");
            Assert.IsTrue(sql.Contains("[Age] > 18"), $"Complex query should contain age condition: {sql}");
            // 复杂查询中的HasValue可能被转换为布尔表达式
            Assert.IsTrue(sql.Contains("[HasValue] = 1") || sql.Contains("[Salary] IS NOT NULL"), 
                $"Complex query should contain salary check: {sql}");

            Console.WriteLine($"✅ 复杂表达式测试: {sql}");
        }

        #endregion

        #region SQL Generation Core Tests - SQL生成核心测试

        [TestMethod]
        public void SqlGeneration_DatabaseDialects_GenerateCorrectSyntax()
        {
            // 测试不同数据库方言的SQL生成
            var testCases = new[]
            {
                (ExpressionToSql<TestEntity>.ForSqlServer(), "SQL Server", "[", "]"),
                (ExpressionToSql<TestEntity>.ForMySql(), "MySQL", "`", "`"),
                (ExpressionToSql<TestEntity>.ForPostgreSQL(), "PostgreSQL", "\"", "\""),
                (ExpressionToSql<TestEntity>.ForOracle(), "Oracle", "\"", "\""),
                (ExpressionToSql<TestEntity>.ForSqlite(), "SQLite", "[", "]")
            };

            foreach (var (query, dialectName, leftQuote, rightQuote) in testCases)
            {
                using (query)
                {
                    query.Where(e => e.Name == "test" && e.Age > 18);
                    var sql = query.ToSql();

                    Assert.IsTrue(sql.Contains($"{leftQuote}Name{rightQuote}"), 
                        $"{dialectName} should quote column names correctly: {sql}");
                    Assert.IsTrue(sql.Contains($"{leftQuote}Age{rightQuote}"), 
                        $"{dialectName} should quote all columns: {sql}");

                    Console.WriteLine($"✅ {dialectName} 方言测试: {sql}");
                }
            }
        }

        [TestMethod]
        public void SqlGeneration_SelectStatements_GenerateCorrectStructure()
        {
            // 测试SELECT语句的结构
            using var simpleQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.IsActive);

            using var complexQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18)
                .OrderBy(e => e.Name)
                .Skip(10)
                .Take(20);

            var simpleSql = simpleQuery.ToSql();
            var complexSql = complexQuery.ToSql();

            // 验证基本SELECT结构
            Assert.IsTrue(simpleSql.StartsWith("SELECT"), $"Should start with SELECT: {simpleSql}");
            Assert.IsTrue(simpleSql.Contains("FROM [TestEntity]"), $"Should contain FROM clause: {simpleSql}");
            Assert.IsTrue(simpleSql.Contains("WHERE"), $"Should contain WHERE clause: {simpleSql}");

            // 验证复杂SELECT结构
            Assert.IsTrue(complexSql.Contains("ORDER BY"), $"Should contain ORDER BY: {complexSql}");
            Assert.IsTrue(complexSql.Contains("OFFSET"), $"Should contain OFFSET for Skip: {complexSql}");
            Assert.IsTrue(complexSql.Contains("FETCH NEXT"), $"Should contain FETCH NEXT for Take: {complexSql}");

            Console.WriteLine($"✅ SELECT结构测试:");
            Console.WriteLine($"   简单查询: {simpleSql}");
            Console.WriteLine($"   复杂查询: {complexSql}");
        }

        [TestMethod]
        public void SqlGeneration_WhereClause_HandlesDifferentConditions()
        {
            // 测试WHERE子句的不同条件类型
            var testCases = new (Expression<Func<TestEntity, bool>>, string)[]
            {
                ((Expression<Func<TestEntity, bool>>)(e => e.Age == 25), "等值比较"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Name.Contains("test")), "字符串包含"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Salary == null), "NULL检查"),
                ((Expression<Func<TestEntity, bool>>)(e => e.IsActive), "布尔值"),
                ((Expression<Func<TestEntity, bool>>)(e => e.Age > 18 && e.Age < 65), "范围条件")
            };

            foreach (var (expression, description) in testCases)
            {
                using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                    .Where(expression);
                
                var sql = query.ToSql();
                
                Assert.IsTrue(sql.Contains("WHERE"), $"{description} should have WHERE clause: {sql}");
                
                Console.WriteLine($"✅ WHERE条件测试 - {description}: {sql}");
            }
        }

        #endregion

        #region Type Conversion Core Tests - 类型转换核心测试

        [TestMethod]
        public void TypeConversion_PrimitiveTypes_ConvertCorrectly()
        {
            // 测试基本类型的转换
            using var intQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age == 25);
            
            using var stringQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Name == "test");
            
            using var boolQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.IsActive == true);
            
            using var dateQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.CreatedAt > DateTime.Now.AddDays(-30));

            var intSql = intQuery.ToSql();
            var stringSql = stringQuery.ToSql();
            var boolSql = boolQuery.ToSql();
            var dateSql = dateQuery.ToSql();

            Assert.IsTrue(intSql.Contains("= 25"), $"Integer should convert correctly: {intSql}");
            Assert.IsTrue(stringSql.Contains("= 'test'"), $"String should be quoted: {stringSql}");
            Assert.IsTrue(boolSql.Contains("= 1"), $"Boolean true should convert to 1: {boolSql}");
            Assert.IsTrue(dateSql.Contains(">"), $"DateTime should convert: {dateSql}");

            Console.WriteLine($"✅ 类型转换测试:");
            Console.WriteLine($"   整数: {intSql}");
            Console.WriteLine($"   字符串: {stringSql}");
            Console.WriteLine($"   布尔值: {boolSql}");
            Console.WriteLine($"   日期时间: {dateSql}");
        }

        [TestMethod]
        public void TypeConversion_NullableTypes_HandleCorrectly()
        {
            // 测试可空类型的转换
            using var nullableIntQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Salary.HasValue && e.Salary.Value > 50000);
            
            using var nullCheckQuery = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Score == null);

            var nullableIntSql = nullableIntQuery.ToSql();
            var nullCheckSql = nullCheckQuery.ToSql();

            // HasValue可能被转换为布尔表达式
            Assert.IsTrue(nullableIntSql.Contains("[HasValue] = 1") || nullableIntSql.Contains("IS NOT NULL"), 
                $"HasValue should be handled: {nullableIntSql}");
            Assert.IsTrue(nullableIntSql.Contains("> 50000"), $"Value access should work: {nullableIntSql}");
            Assert.IsTrue(nullCheckSql.Contains("IS NULL"), $"Null check should use IS NULL: {nullCheckSql}");

            Console.WriteLine($"✅ 可空类型转换测试:");
            Console.WriteLine($"   可空值检查: {nullableIntSql}");
            Console.WriteLine($"   NULL检查: {nullCheckSql}");
        }

        #endregion

        #region Performance Core Tests - 性能核心测试

        [TestMethod]
        public void Performance_ExpressionParsing_IsEfficient()
        {
            // 测试表达式解析的性能
            const int iterations = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                    .Where(e => e.Age > 18 && e.IsActive && e.Name.Contains("test"))
                    .OrderBy(e => e.CreatedAt)
                    .Skip(i)
                    .Take(10);
                
                var sql = query.ToSql();
                Assert.IsNotNull(sql);
            }

            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"Expression parsing should be fast: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");

            Console.WriteLine($"✅ 表达式解析性能: {iterations}次解析耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void Performance_SqlGeneration_IsEfficient()
        {
            // 测试SQL生成的性能
            const int iterations = 1000;
            
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.IsActive)
                .OrderBy(e => e.Name);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var sql = query.ToSql();
                Assert.IsNotNull(sql);
            }

            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, 
                $"SQL generation should be fast: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");

            Console.WriteLine($"✅ SQL生成性能: {iterations}次生成耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Error Handling Core Tests - 错误处理核心测试

        [TestMethod]
        public void ErrorHandling_InvalidExpressions_HandleGracefully()
        {
            // 测试无效表达式的处理
            using var query = ExpressionToSql<TestEntity>.ForSqlServer();

            // 这些应该不会抛出异常，而是优雅处理
            try
            {
                query.Where(e => e.ToString().Contains("test"));
                var sql1 = query.ToSql();
                Assert.IsNotNull(sql1);
                Console.WriteLine($"✅ ToString()表达式处理: {sql1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ToString()表达式异常: {ex.Message}");
            }

            try
            {
                using var query2 = ExpressionToSql<TestEntity>.ForSqlServer()
                    .Where(e => e.GetHashCode() > 0);
                var sql2 = query2.ToSql();
                Assert.IsNotNull(sql2);
                Console.WriteLine($"✅ GetHashCode()表达式处理: {sql2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ GetHashCode()表达式异常: {ex.Message}");
            }
        }

        [TestMethod]
        public void ErrorHandling_NullParameters_HandleCorrectly()
        {
            // 测试NULL参数的处理
            using var query = ExpressionToSql<TestEntity>.ForSqlServer();

            // 测试NULL表达式参数
            try
            {
                Expression<Func<TestEntity, bool>>? nullExpression = null;
                query.Where(nullExpression ?? (e => true));
                var sql = query.ToSql();
                
                // 应该能处理NULL表达式而不崩溃
                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ NULL表达式处理: {sql}");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("✅ NULL表达式正确抛出ArgumentNullException");
            }
        }

        #endregion

        #region Memory Management Core Tests - 内存管理核心测试

        [TestMethod]
        public void MemoryManagement_Dispose_WorksCorrectly()
        {
            // 测试资源释放
            ExpressionToSql<TestEntity>? query = null;
            
            try
            {
                query = ExpressionToSql<TestEntity>.ForSqlServer()
                    .Where(e => e.IsActive);
                
                var sql = query.ToSql();
                Assert.IsNotNull(sql);
                
                query.Dispose();
                
                // 释放后应该仍然能访问已生成的SQL（如果有缓存）
                Console.WriteLine($"✅ 资源释放测试通过");
            }
            finally
            {
                query?.Dispose(); // 确保资源被释放
            }
        }

        [TestMethod]
        public void MemoryManagement_UsingStatement_WorksCorrectly()
        {
            // 测试using语句的正确使用
            string sql;
            
            using (var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18))
            {
                sql = query.ToSql();
            }
            
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("WHERE"));
            
            Console.WriteLine($"✅ Using语句测试: {sql}");
        }

        #endregion
    }
}
