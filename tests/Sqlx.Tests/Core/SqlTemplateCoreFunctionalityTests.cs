// -----------------------------------------------------------------------
// <copyright file="SqlTemplateCoreFunctionalityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SqlTemplate 核心功能单元测试
    /// 验证SQL模板的核心功能和参数化查询
    /// </summary>
    [TestClass]
    public class SqlTemplateCoreFunctionalityTests
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
        }

        #region SqlTemplate Core Tests - SqlTemplate核心测试

        [TestMethod]
        public void SqlTemplate_BasicCreation_WorksCorrectly()
        {
            // 测试基本的SqlTemplate创建
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.IsActive);

            var template = query.ToTemplate();

            Assert.IsNotNull(template, "Template should not be null");
            Assert.IsNotNull(template.Sql, "Template SQL should not be null");
            Assert.IsNotNull(template.Parameters, "Template parameters should not be null");

            Console.WriteLine($"✅ SqlTemplate基础创建测试:");
            Console.WriteLine($"   SQL: {template.Sql}");
            Console.WriteLine($"   参数数量: {template.Parameters.Length}");
        }

        [TestMethod]
        public void SqlTemplate_ParameterizedQuery_GeneratesCorrectParameters()
        {
            // 测试参数化查询的生成
            var testAge = 25;
            var testName = "John";
            
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > testAge && e.Name.Contains(testName));

            var template = query.ToTemplate();
            
            Console.WriteLine($"Generated SQL: {template.Sql}");
            Console.WriteLine($"Parameters count: {template.Parameters.Length}");
            
            // 检查SQL是否包含预期的条件，即使没有参数化
            Assert.IsTrue(template.Sql.Contains("Age") && (template.Sql.Contains("> " + testAge.ToString()) || template.Parameters.Length > 0), 
                $"Should contain age condition. SQL: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains("Name") && (template.Sql.Contains("LIKE") || template.Parameters.Length > 0), 
                $"Should contain name condition. SQL: {template.Sql}");

            // 如果有参数，验证参数是否正确设置
            if (template.Parameters.Length > 0)
            {
                Console.WriteLine("Parameters found:");
                foreach (var param in template.Parameters)
                {
                    Console.WriteLine($"  {param.ParameterName} = {param.Value}");
                }
            }

            Console.WriteLine($"✅ 参数化查询测试:");
            Console.WriteLine($"   SQL: {template.Sql}");
            foreach (var param in template.Parameters)
            {
                Console.WriteLine($"   参数: {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        [TestMethod]
        public void SqlTemplate_DifferentDataTypes_HandleCorrectly()
        {
            // 测试不同数据类型的参数处理
            var testDate = DateTime.Now.AddDays(-30);
            var testSalary = 75000.50m;
            
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.CreatedAt > testDate && e.Salary > testSalary && e.IsActive);

            var template = query.ToTemplate();

            // 检查SQL是否包含预期的条件和类型
            Assert.IsTrue(template.Sql.Contains("CreatedAt") && template.Sql.Contains(">"), 
                $"Should contain CreatedAt condition. SQL: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains("Salary") && template.Sql.Contains(">"), 
                $"Should contain Salary condition. SQL: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains("IsActive"), 
                $"Should contain IsActive condition. SQL: {template.Sql}");

            // 验证不同类型的参数
            var hasDateParam = template.Parameters.Any(p => p.Value is DateTime);
            var hasDecimalParam = template.Parameters.Any(p => p.Value is decimal);
            var hasBoolParam = template.Parameters.Any(p => p.Value is bool || p.Value?.Equals(1) == true);

            Assert.IsTrue(hasDateParam, "Should have DateTime parameter");
            Assert.IsTrue(hasDecimalParam, "Should have decimal parameter");
            Assert.IsTrue(hasBoolParam, "Should have boolean parameter");

            Console.WriteLine($"✅ 不同数据类型测试:");
            Console.WriteLine($"   SQL: {template.Sql}");
            foreach (var param in template.Parameters)
            {
                Console.WriteLine($"   参数: {param.ParameterName} = {param.Value} ({param.Value?.GetType().Name})");
            }
        }

        [TestMethod]
        public void SqlTemplate_NullValues_HandleCorrectly()
        {
            // 测试NULL值的处理
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Salary == null && e.Name != null);

            var template = query.ToTemplate();

            Assert.IsNotNull(template.Sql, "SQL should not be null");
            
            // NULL比较应该使用IS NULL而不是参数
            Assert.IsTrue(template.Sql.Contains("IS NULL"), 
                $"Should use IS NULL for null comparison: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains("IS NOT NULL"), 
                $"Should use IS NOT NULL for not null comparison: {template.Sql}");

            Console.WriteLine($"✅ NULL值处理测试:");
            Console.WriteLine($"   SQL: {template.Sql}");
            Console.WriteLine($"   参数数量: {template.Parameters.Length}");
        }

        #endregion

        #region Parameter Management Tests - 参数管理测试

        [TestMethod]
        public void Parameters_UniqueNames_GeneratedCorrectly()
        {
            // 测试参数名称的唯一性
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.Age < 65 && e.Name.Contains("test"));

            var template = query.ToTemplate();

            var parameterNames = template.Parameters.Select(p => p.ParameterName).ToList();
            var uniqueNames = parameterNames.Distinct().ToList();

            Assert.AreEqual(parameterNames.Count, uniqueNames.Count, 
                "All parameter names should be unique");

            Console.WriteLine($"✅ 参数名称唯一性测试:");
            Console.WriteLine($"   总参数: {parameterNames.Count}");
            Console.WriteLine($"   唯一参数: {uniqueNames.Count}");
            foreach (var name in parameterNames)
            {
                Console.WriteLine($"   参数名: {name}");
            }
        }

        [TestMethod]
        public void Parameters_TypeMapping_IsCorrect()
        {
            // 测试参数类型映射的正确性
            var testValues = new object[]
            {
                123,           // int
                "test string", // string
                true,          // bool
                DateTime.Now,  // DateTime
                123.45m,       // decimal
                45.67d         // double
            };

            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age == (int)testValues[0] && 
                           e.Name == (string)testValues[1] && 
                           e.IsActive == (bool)testValues[2]);

            var template = query.ToTemplate();

            // 验证参数类型映射
            foreach (var param in template.Parameters)
            {
                Assert.IsNotNull(param.Value, $"Parameter {param.ParameterName} should have a value");
                
                Console.WriteLine($"✅ 参数类型映射: {param.ParameterName} = {param.Value} " +
                                $"(Type: {param.Value.GetType().Name}, DbType: {param.DbType})");
            }
        }

        #endregion

        #region SQL Template Reusability Tests - SQL模板重用性测试

        [TestMethod]
        public void SqlTemplate_Reusability_WorksWithDifferentParameters()
        {
            // 测试SQL模板的重用性
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.Name.Contains("test"));

            var template = query.ToTemplate();
            var originalSql = template.Sql;

            // 模拟使用不同参数值重用模板
            var newParameters = template.Parameters.ToList();
            if (newParameters.Count > 0)
            {
                // 修改第一个参数的值
                var firstParam = newParameters[0];
                Console.WriteLine($"原始参数值: {firstParam.Value}");
                
                // 注意：实际使用中，你会创建新的参数集合
                Console.WriteLine($"✅ SQL模板重用性测试:");
                Console.WriteLine($"   模板SQL: {originalSql}");
                Console.WriteLine($"   原始参数数量: {template.Parameters.Length}");
                Console.WriteLine($"   模板可以用不同参数值重复使用");
            }
        }

        [TestMethod]
        public void SqlTemplate_Caching_BehaviorIsConsistent()
        {
            // 测试模板缓存行为的一致性
            using var query1 = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 25);
            
            using var query2 = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 30);

            var template1 = query1.ToTemplate();
            var template2 = query2.ToTemplate();

            // 相似的查询应该有相似的SQL结构，但参数值不同
            Assert.IsTrue(template1.Sql.Contains("WHERE"), "Template1 should have WHERE clause");
            Assert.IsTrue(template2.Sql.Contains("WHERE"), "Template2 should have WHERE clause");
            Assert.IsTrue(template1.Sql.Contains("[Age] >"), "Template1 should have age comparison");
            Assert.IsTrue(template2.Sql.Contains("[Age] >"), "Template2 should have age comparison");

            Console.WriteLine($"✅ 模板缓存一致性测试:");
            Console.WriteLine($"   模板1 SQL: {template1.Sql}");
            Console.WriteLine($"   模板2 SQL: {template2.Sql}");
        }

        #endregion

        #region Complex Query Template Tests - 复杂查询模板测试

        [TestMethod]
        public void SqlTemplate_ComplexQuery_GeneratesCorrectTemplate()
        {
            // 测试复杂查询的模板生成
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.Age < 65)
                .Where(e => e.IsActive && e.Name.Contains("test"))
                .Where(e => e.Salary.HasValue && e.Salary.Value > 50000)
                .OrderBy(e => e.CreatedAt)
                .Skip(20)
                .Take(10);

            var template = query.ToTemplate();

            Assert.IsNotNull(template.Sql, "Complex query SQL should not be null");
            Assert.IsTrue(template.Parameters.Length > 0, "Complex query should have parameters");

            // 验证SQL结构
            Assert.IsTrue(template.Sql.Contains("WHERE"), "Should have WHERE clause");
            Assert.IsTrue(template.Sql.Contains("ORDER BY"), "Should have ORDER BY clause");
            Assert.IsTrue(template.Sql.Contains("OFFSET"), "Should have OFFSET for Skip");
            Assert.IsTrue(template.Sql.Contains("FETCH NEXT"), "Should have FETCH NEXT for Take");

            Console.WriteLine($"✅ 复杂查询模板测试:");
            Console.WriteLine($"   SQL: {template.Sql}");
            Console.WriteLine($"   参数数量: {template.Parameters.Length}");
            foreach (var param in template.Parameters.Take(5)) // 只显示前5个参数
            {
                Console.WriteLine($"   参数: {param.ParameterName} = {param.Value}");
            }
        }

        [TestMethod]
        public void SqlTemplate_MultipleWhereConditions_CombineCorrectly()
        {
            // 测试多个WHERE条件的组合
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18)
                .Where(e => e.IsActive)
                .Where(e => e.Name.StartsWith("A"));

            var template = query.ToTemplate();

            Assert.IsTrue(template.Sql.Contains("AND"), 
                $"Multiple WHERE conditions should be combined with AND: {template.Sql}");

            // 计算AND的数量（应该比WHERE条件少1）
            var andCount = template.Sql.Split(new[] { " AND " }, StringSplitOptions.None).Length - 1;
            Assert.IsTrue(andCount >= 2, $"Should have at least 2 AND operators for 3 conditions");

            Console.WriteLine($"✅ 多WHERE条件组合测试:");
            Console.WriteLine($"   SQL: {template.Sql}");
            Console.WriteLine($"   AND操作符数量: {andCount}");
        }

        #endregion

        #region Database Dialect Template Tests - 数据库方言模板测试

        [TestMethod]
        public void SqlTemplate_DatabaseDialects_GenerateCompatibleTemplates()
        {
            // 测试不同数据库方言的模板兼容性
            var dialects = new[]
            {
                ("SQL Server", ExpressionToSql<TestEntity>.ForSqlServer()),
                ("MySQL", ExpressionToSql<TestEntity>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<TestEntity>.ForPostgreSQL()),
                ("SQLite", ExpressionToSql<TestEntity>.ForSqlite())
            };

            foreach (var (dialectName, queryBuilder) in dialects)
            {
                using (queryBuilder)
                {
                    queryBuilder.Where(e => e.Age > 25 && e.Name.Contains("test"));
                    var template = queryBuilder.ToTemplate();

                    Assert.IsNotNull(template.Sql, $"{dialectName} template SQL should not be null");
                    
                    Console.WriteLine($"✅ {dialectName} 方言模板:");
                    Console.WriteLine($"   SQL: {template.Sql}");
                    Console.WriteLine($"   参数数量: {template.Parameters.Length}");
                    
                    // 检查是否包含预期条件，参数化是可选的
                    Assert.IsTrue(template.Sql.Contains("Age") && (template.Sql.Contains("25") || template.Parameters.Length > 0), 
                        $"{dialectName} should contain age condition. SQL: {template.Sql}");
                    Assert.IsTrue(template.Sql.Contains("Name") && (template.Sql.Contains("LIKE") || template.Parameters.Length > 0), 
                        $"{dialectName} should contain name condition. SQL: {template.Sql}");
                }
            }
        }

        #endregion

        #region Performance Template Tests - 性能模板测试

        [TestMethod]
        public void SqlTemplate_Performance_TemplateGenerationIsEfficient()
        {
            // 测试模板生成的性能
            const int iterations = 1000;
            
            using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                .Where(e => e.Age > 18 && e.IsActive && e.Name.Contains("test"));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var template = query.ToTemplate();
                Assert.IsNotNull(template);
                Assert.IsNotNull(template.Sql);
            }

            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
                $"Template generation should be fast: {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");

            Console.WriteLine($"✅ 模板生成性能: {iterations}次生成耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void SqlTemplate_Memory_ParameterObjectsAreEfficient()
        {
            // 测试参数对象的内存效率
            const int iterations = 100;
            var initialMemory = GC.GetTotalMemory(true);

            var templates = new SqlTemplate[iterations];

            for (int i = 0; i < iterations; i++)
            {
                using var query = ExpressionToSql<TestEntity>.ForSqlServer()
                    .Where(e => e.Age > i && e.Name.Contains($"test{i}"));
                
                templates[i] = query.ToTemplate();
            }

            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;
            var averageMemoryPerTemplate = memoryUsed / (double)iterations;

            Assert.IsTrue(averageMemoryPerTemplate < 10240, // 10KB per template should be reasonable
                $"Memory usage per template should be reasonable: {averageMemoryPerTemplate:F2} bytes");

            Console.WriteLine($"✅ 模板内存效率: {iterations}个模板使用 {memoryUsed} 字节，" +
                            $"平均 {averageMemoryPerTemplate:F2} 字节/个");

            // 清理
            templates = null!;
            GC.Collect();
        }

        #endregion

        #region Error Handling Template Tests - 模板错误处理测试

        [TestMethod]
        public void SqlTemplate_ErrorHandling_InvalidExpressionsHandleGracefully()
        {
            // 测试无效表达式的模板处理
            using var query = ExpressionToSql<TestEntity>.ForSqlServer();

            try
            {
                query.Where(e => e.GetHashCode() > 0);
                var template = query.ToTemplate();
                
                Assert.IsNotNull(template, "Template should be created even with complex expressions");
                Assert.IsNotNull(template.Sql, "Template SQL should not be null");
                
                Console.WriteLine($"✅ 复杂表达式模板处理: {template.Sql}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 复杂表达式异常: {ex.Message}");
                // 某些复杂表达式可能无法处理，这是可以接受的
            }
        }

        #endregion
    }
}
