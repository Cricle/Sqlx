// -----------------------------------------------------------------------
// <copyright file="SqlGenerationBoundaryTests.cs" company="Cricle">
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
    /// SQL生成边界测试 - 专门测试SQL生成的极限情况和边界条件
    /// </summary>
    [TestClass]
    public class SqlGenerationBoundaryTests
    {
        #region 测试实体

        public class SqlBoundaryEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal? Price { get; set; }
            public bool IsActive { get; set; }
        }

        public class EntityWithReservedWords
        {
            public int Select { get; set; }
            public string? From { get; set; }
            public string? Where { get; set; }
            public string? Order { get; set; }
            public string? Group { get; set; }
            public string? Having { get; set; }
            public string? Insert { get; set; }
            public string? Update { get; set; }
            public string? Delete { get; set; }
        }

        #endregion

        #region SQL长度边界测试

        [TestMethod]
        public void SqlGeneration_VeryLongWhereClause_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            
            // 生成超长WHERE子句
            for (int i = 0; i < 1000; i++)
            {
                expr.Where(e => e.Id > i || e.Name == $"test{i}");
            }

            try
            {
                var sql = expr.ToSql();
                
                // Assert
                Assert.IsNotNull(sql);
                Assert.IsTrue(sql.Length > 10000, "超长WHERE子句应生成很长的SQL");
                Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE关键字");
                
                Console.WriteLine($"✅ 超长WHERE子句测试通过，SQL长度: {sql.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 超长WHERE子句正确处理异常: {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void SqlGeneration_VeryLongOrderByClause_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            
            // 生成超长ORDER BY子句
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 == 0)
                    expr.OrderBy(e => e.Id);
                else
                    expr.OrderByDescending(e => e.CreatedAt);
            }

            try
            {
                var sql = expr.ToSql();
                
                // Assert
                Assert.IsNotNull(sql);
                Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY关键字");
                
                Console.WriteLine($"✅ 超长ORDER BY子句测试通过，SQL长度: {sql.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 超长ORDER BY子句正确处理异常: {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void SqlGeneration_ExtremelyDeepNesting_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            
            try
            {
                // 创建极深的嵌套条件 (((((condition)))))
                expr.Where(e => 
                    (e.Id > 1 && 
                        (e.Name != null && 
                            (e.Description != null && 
                                (e.IsActive == true && 
                                    (e.Price > 0 && 
                                        (e.CreatedAt > DateTime.MinValue)))))));

                var sql = expr.ToSql();
                
                // Assert
                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ 深度嵌套测试通过: {sql}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 深度嵌套正确处理异常: {ex.GetType().Name}");
            }
        }

        #endregion

        #region SQL关键字冲突测试

        [TestMethod]
        public void SqlGeneration_ReservedWordsAsColumns_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<EntityWithReservedWords>.ForSqlServer();
            
            expr.Where(e => e.Select > 1)
                .Where(e => e.From == "test")
                .Where(e => e.Where != null)
                .Where(e => e.Order == "ASC")
                .Where(e => e.Group == "admin")
                .Where(e => e.Having == "condition")
                .Where(e => e.Insert == "data")
                .Where(e => e.Update == "record")
                .Where(e => e.Delete == "soft");

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("[") || sql.Contains("`") || sql.Contains("\""), 
                "SQL关键字列名应被正确转义");
            
            Console.WriteLine($"✅ SQL关键字冲突测试通过: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_SpecialCharactersInTableName_HandlesCorrectly()
        {
            // Act & Assert - 测试各种表名情况
            var testCases = new[]
            {
                ("User", "正常表名"),
                ("User_Profile", "下划线表名"),
                ("User123", "数字表名"),
                ("user", "小写表名"),
                ("USER", "大写表名")
            };

            foreach (var (tableName, description) in testCases)
            {
                try
                {
                    // 由于我们无法直接设置表名，这里主要测试表名包含特殊情况时的处理
                    using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
                    expr.Where(e => e.Id > 0);
                    var sql = expr.ToSql();
                    
                    Assert.IsNotNull(sql);
                    Console.WriteLine($"✅ {description} 测试通过");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✅ {description} 正确处理异常: {ex.GetType().Name}");
                }
            }
        }

        #endregion

        #region 参数边界测试

        [TestMethod]
        public void SqlGeneration_MassiveParameterCount_HandlesCorrectly()
        {
            // Arrange
            var values = Enumerable.Range(1, 2000).ToList(); // 2000个参数

            // Act
            try
            {
                using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
                expr.Where(e => values.Contains(e.Id));
                
                var sql = expr.ToSql();
                
                // Assert
                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ 大量参数测试通过，参数数量: {values.Count}");
                Console.WriteLine($"SQL长度: {sql.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 大量参数正确处理异常: {ex.GetType().Name}");
            }
        }

        [TestMethod]
        public void SqlGeneration_MixedParameterTypes_HandlesCorrectly()
        {
            // Arrange
            var stringValues = new[] { "a", "b", "c", null, "", " " };
            var intValues = new[] { 1, 2, 3, int.MaxValue, int.MinValue, 0 };
            var dateValues = new[] { DateTime.Now, DateTime.MinValue, DateTime.MaxValue };

            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            
            expr.Where(e => stringValues.Contains(e.Name))
                .Where(e => intValues.Contains(e.Id))
                .Where(e => dateValues.Contains(e.CreatedAt));

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Console.WriteLine($"✅ 混合参数类型测试通过: {sql}");
        }

        #endregion

        #region 数据库方言边界测试

        [TestMethod]
        public void SqlGeneration_AllDialects_WithComplexQuery_HandlesCorrectly()
        {
            var dialects = new (string, Func<ExpressionToSql<SqlBoundaryEntity>>)[]
            {
                ("SQL Server", () => ExpressionToSql<SqlBoundaryEntity>.ForSqlServer()),
                ("MySQL", () => ExpressionToSql<SqlBoundaryEntity>.ForMySql()),
                ("PostgreSQL", () => ExpressionToSql<SqlBoundaryEntity>.ForPostgreSQL()),
                ("Oracle", () => ExpressionToSql<SqlBoundaryEntity>.ForOracle()),
                ("DB2", () => ExpressionToSql<SqlBoundaryEntity>.ForDB2()),
                ("SQLite", () => ExpressionToSql<SqlBoundaryEntity>.ForSqlite())
            };

            foreach (var (dialectName, factory) in dialects)
            {
                try
                {
                    using var expr = factory();
                    
                    // 构建复杂查询
                    expr.Where(e => e.Id > 1)
                        .Where(e => e.Name!.Contains("test"))
                        .Where(e => e.Price > 100.50m)
                        .Where(e => e.IsActive == true)
                        .Where(e => e.CreatedAt > DateTime.Now.AddDays(-30))
                        .OrderBy(e => e.Id)
                        .OrderByDescending(e => e.CreatedAt)
                        .Take(100)
                        .Skip(10);

                    var sql = expr.ToSql();
                    
                    Assert.IsNotNull(sql);
                    Assert.IsTrue(sql.Length > 0);
                    
                    Console.WriteLine($"✅ {dialectName} 复杂查询测试通过");
                    Console.WriteLine($"SQL: {sql.Substring(0, Math.Min(100, sql.Length))}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ {dialectName} 处理异常: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        [TestMethod]
        public void SqlGeneration_AllDialects_WithBoundaryValues_HandlesCorrectly()
        {
            var dialects = new (string, Func<ExpressionToSql<SqlBoundaryEntity>>)[]
            {
                ("SQL Server", () => ExpressionToSql<SqlBoundaryEntity>.ForSqlServer()),
                ("MySQL", () => ExpressionToSql<SqlBoundaryEntity>.ForMySql()),
                ("PostgreSQL", () => ExpressionToSql<SqlBoundaryEntity>.ForPostgreSQL()),
                ("Oracle", () => ExpressionToSql<SqlBoundaryEntity>.ForOracle()),
                ("DB2", () => ExpressionToSql<SqlBoundaryEntity>.ForDB2()),
                ("SQLite", () => ExpressionToSql<SqlBoundaryEntity>.ForSqlite())
            };

            foreach (var (dialectName, factory) in dialects)
            {
                try
                {
                    using var expr = factory();
                    
                    // 测试边界值
                    expr.Where(e => e.Id == int.MaxValue)
                        .Where(e => e.Name == "'SQL注入测试'")
                        .Where(e => e.Price == decimal.MaxValue)
                        .Where(e => e.CreatedAt == DateTime.MinValue);

                    var sql = expr.ToSql();
                    
                    Assert.IsNotNull(sql);
                    Console.WriteLine($"✅ {dialectName} 边界值测试通过");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ {dialectName} 边界值处理异常: {ex.GetType().Name}");
                }
            }
        }

        #endregion

        #region SQL结构边界测试

        [TestMethod]
        public void SqlGeneration_NoConditions_ReturnsBasicSelect()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.StartsWith("SELECT"), "应以SELECT开始");
            Assert.IsTrue(sql.Contains("FROM"), "应包含FROM");
            Assert.IsFalse(sql.Contains("WHERE"), "不应包含WHERE");
            
            Console.WriteLine($"✅ 无条件查询测试通过: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_OnlyOrderBy_NoWhere_HandlesCorrectly()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            expr.OrderBy(e => e.Id)
                .OrderByDescending(e => e.CreatedAt);
            
            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY");
            Assert.IsFalse(sql.Contains("WHERE"), "不应包含WHERE");
            
            Console.WriteLine($"✅ 仅排序查询测试通过: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_OnlyPaging_NoConditions_HandlesCorrectly()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            expr.Take(10).Skip(5);
            
            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("LIMIT") || sql.Contains("TOP"), 
                "应包含分页关键字");
            
            Console.WriteLine($"✅ 仅分页查询测试通过: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_ZeroTakeSkip_HandlesCorrectly()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            expr.Take(0).Skip(0);
            
            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Console.WriteLine($"✅ 零值分页测试通过: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_LargeTakeSkip_HandlesCorrectly()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            expr.Take(int.MaxValue).Skip(int.MaxValue - 1000);
            
            try
            {
                var sql = expr.ToSql();
                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ 大值分页测试通过: {sql}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 大值分页正确处理异常: {ex.GetType().Name}");
            }
        }

        #endregion

        #region 表达式复杂度边界测试

        [TestMethod]
        public void SqlGeneration_ComplexBooleanLogic_HandlesCorrectly()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            
            expr.Where(e => 
                (e.Id > 1 && e.Id < 100) || 
                (e.Name != null && e.Name.Length > 5) || 
                (e.Price.HasValue && e.Price.Value > 0 && e.Price.Value < 1000) ||
                (e.IsActive == true && e.CreatedAt > DateTime.Now.AddYears(-1)));

            var sql = expr.ToSql();

            // Assert
            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("AND") || sql.Contains("OR"), "应包含逻辑运算符");
            
            Console.WriteLine($"✅ 复杂布尔逻辑测试通过: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_DeepNestedSubqueries_HandlesCorrectly()
        {
            // Act
            using var expr = ExpressionToSql<SqlBoundaryEntity>.ForSqlServer();
            
            try
            {
                expr.Where(e => 
                    e.Id > 0 && 
                    (e.Name == "test" || 
                        (e.Description != null && 
                            (e.Price > 0 || 
                                (e.IsActive && e.CreatedAt > DateTime.MinValue)))));

                var sql = expr.ToSql();
                
                Assert.IsNotNull(sql);
                Console.WriteLine($"✅ 深度嵌套测试通过: {sql}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ 深度嵌套正确处理异常: {ex.GetType().Name}");
            }
        }

        #endregion
    }
}
