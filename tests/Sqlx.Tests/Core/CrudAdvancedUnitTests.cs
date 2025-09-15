// -----------------------------------------------------------------------
// <copyright file="CrudAdvancedUnitTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// CRUD操作高级单元测试，针对特定功能和边界情况进行详细测试
    /// </summary>
    [TestClass]
    public class CrudAdvancedUnitTests
    {
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsDeleted { get; set; }
            public int StockQuantity { get; set; }
            public string? Tags { get; set; }
        }

        #region CREATE (INSERT) Advanced Tests

        [TestMethod]
        public void Insert_EmptyValues_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto();
            
            var sql = expr.ToSql();
            Assert.IsTrue(sql.StartsWith("INSERT INTO [Product]"));
            Assert.IsTrue(sql.Contains("([Id], [Name], [Description], [Price], [CategoryId], [CreatedAt], [UpdatedAt], [IsDeleted], [StockQuantity], [Tags])"));
        }

        [TestMethod]
        public void Insert_MultipleValueSets_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto()
                .Values(1, "Product A", "Description A", 10.5m, 1, DateTime.Now, (DateTime?)null, false, 100, "tag1,tag2")
                .AddValues(2, "Product B", (string?)null, 20.0m, 2, DateTime.Now, DateTime.Now, false, 50, (string?)null)
                .AddValues(3, "Product C", "Description C", 30.75m, 1, DateTime.Now, (DateTime?)null, true, 0, "tag3");

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("VALUES"));
            
            // 应该包含3组值
            var valueCount = sql.Split("VALUES")[1].Count(c => c == '(');
            Assert.AreEqual(3, valueCount, "Should contain 3 value sets");
        }

        [TestMethod]
        public void Insert_WithSelect_GeneratesCorrectSql()
        {
            using var selectExpr = ExpressionToSql<Product>.ForSqlServer();
            selectExpr.Where(p => p.CategoryId == 1);
            
            using var insertExpr = ExpressionToSql<Product>.ForSqlServer();
            insertExpr.InsertInto().InsertSelect(selectExpr);

            var sql = insertExpr.ToSql();
            Assert.IsTrue(sql.StartsWith("INSERT INTO [Product]"));
            Assert.IsTrue(sql.Contains("SELECT * FROM [Product] WHERE [CategoryId] = 1"));
        }

        [TestMethod]
        public void Insert_WithSelectString_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto().InsertSelect("SELECT * FROM TempProducts WHERE Active = 1");

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("SELECT * FROM TempProducts WHERE Active = 1"));
        }

        #endregion

        #region READ (SELECT) Advanced Tests

        [TestMethod]
        public void Select_CustomSelectClause_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Select("Id", "Name");

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("SELECT Id, Name FROM"));
            Assert.IsFalse(sql.Contains("SELECT * FROM"));
        }

        [TestMethod]
        public void Select_WithMultipleWhereClauses_CombinesWithAnd()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.Price > 10m)
                .Where(p => p.IsDeleted == false)
                .Where(p => p.StockQuantity > 0)
                .Where(p => p.Name.Contains("Special"));

            var sql = expr.ToSql();
            var andCount = sql.Split("AND").Length - 1;
            Assert.AreEqual(3, andCount, "Should have 3 AND operators between 4 WHERE conditions");
        }

        [TestMethod]
        public void Select_ComplexExpressions_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.Price * 1.2m > 100m)
                .Where(p => p.Name.ToUpper().StartsWith("PROD"))
                .Where(p => p.CreatedAt.AddDays(30) > DateTime.Now);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Price] * 1.2"));
            Assert.IsTrue(sql.Contains("UPPER([Name])"));
            Assert.IsTrue(sql.Contains("DATEADD(DAY"));
        }

        [TestMethod]
        public void Select_MathOperations_AllDialects()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<Product>.ForSqlServer()),
                ("MySQL", ExpressionToSql<Product>.ForMySql()),
                ("PostgreSQL", ExpressionToSql<Product>.ForPostgreSQL()),
                ("Oracle", ExpressionToSql<Product>.ForOracle()),
                ("SQLite", ExpressionToSql<Product>.ForSqlite())
            };

            foreach (var (dialectName, expr) in testCases)
            {
                using (expr)
                {
                    expr.Where(p => Math.Abs(p.Price - 50m) < 10m)
                        .Where(p => Math.Round(p.Price, 2) == p.Price)
                        .Where(p => Math.Max(p.Price, 1m) > 0);

                    var sql = expr.ToSql();
                    Assert.IsTrue(sql.Contains("ABS"), $"{dialectName}: Should contain ABS function");
                    Assert.IsTrue(sql.Contains("ROUND"), $"{dialectName}: Should contain ROUND function");
                    
                    if (dialectName == "Oracle")
                    {
                        Assert.IsTrue(sql.Contains("GREATEST"), $"{dialectName}: Should use GREATEST for MAX");
                    }
                    else
                    {
                        Assert.IsTrue(sql.Contains("MAX"), $"{dialectName}: Should contain MAX function");
                    }
                }
            }
        }

        [TestMethod]
        public void Select_StringOperations_AllDialects()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<Product>.ForSqlServer(), "+"),
                ("MySQL", ExpressionToSql<Product>.ForMySql(), "CONCAT"),
                ("PostgreSQL", ExpressionToSql<Product>.ForPostgreSQL(), "||"),
                ("Oracle", ExpressionToSql<Product>.ForOracle(), "||"),
                ("SQLite", ExpressionToSql<Product>.ForSqlite(), "CONCAT")
            };

            foreach (var (dialectName, expr, expectedConcatSyntax) in testCases)
            {
                using (expr)
                {
                    expr.Where(p => p.Name.Contains("test"))
                        .Where(p => p.Description != null && p.Description.Length > 10)
                        .Where(p => p.Name.ToLower().EndsWith("product"));

                    var sql = expr.ToSql();
                    Assert.IsTrue(sql.Contains("LIKE"), $"{dialectName}: Should use LIKE for Contains");
                    Assert.IsTrue(sql.Contains(expectedConcatSyntax), $"{dialectName}: Should use {expectedConcatSyntax} for string concatenation");
                    Assert.IsTrue(sql.Contains("LOWER"), $"{dialectName}: Should contain LOWER function");
                    
                    if (dialectName == "SQL Server")
                    {
                        Assert.IsTrue(sql.Contains("LEN"), $"{dialectName}: Should use LEN for Length");
                    }
                    else
                    {
                        Assert.IsTrue(sql.Contains("LENGTH"), $"{dialectName}: Should use LENGTH function");
                    }
                }
            }
        }

        [TestMethod]
        public void Select_GroupByAndHaving_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            var groupedExpr = expr.GroupBy(p => p.CategoryId);
            groupedExpr.Having(g => g.Key > 0);

            var sql = groupedExpr.ToSql();
            Assert.IsTrue(sql.Contains("GROUP BY [CategoryId]"));
            Assert.IsTrue(sql.Contains("HAVING [CategoryId] > 0"));
        }

        [TestMethod]
        public void Select_ComplexPagination_AllDialects()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<Product>.ForSqlServer(), "OFFSET", "FETCH"),
                ("MySQL", ExpressionToSql<Product>.ForMySql(), "LIMIT", "OFFSET"),
                ("PostgreSQL", ExpressionToSql<Product>.ForPostgreSQL(), "LIMIT", "OFFSET"),
                ("Oracle", ExpressionToSql<Product>.ForOracle(), "OFFSET", "FETCH"),
                ("SQLite", ExpressionToSql<Product>.ForSqlite(), "LIMIT", "OFFSET")
            };

            foreach (var (dialectName, expr, expectedKeyword1, expectedKeyword2) in testCases)
            {
                using (expr)
                {
                    expr.OrderBy(p => p.CreatedAt)
                        .OrderByDescending(p => p.Price)
                        .Skip(100)
                        .Take(25);

                    var sql = expr.ToSql();
                    Assert.IsTrue(sql.Contains(expectedKeyword1), $"{dialectName}: Should contain {expectedKeyword1}");
                    Assert.IsTrue(sql.Contains(expectedKeyword2), $"{dialectName}: Should contain {expectedKeyword2}");
                    Assert.IsTrue(sql.Contains("ORDER BY"), $"{dialectName}: Should have ORDER BY for pagination");
                }
            }
        }

        #endregion

        #region UPDATE Advanced Tests

        [TestMethod]
        public void Update_SetWithExpressions_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Price, p => p.Price * 1.1m)
                .Set(p => p.StockQuantity, p => p.StockQuantity - 1)
                .Set(p => p.UpdatedAt, DateTime.Now)
                .Where(p => p.Id == 1);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Price] = [Price] * 1.1"));
            Assert.IsTrue(sql.Contains("[StockQuantity] = [StockQuantity] - 1"));
            Assert.IsTrue(sql.Contains("SET"));
            Assert.IsTrue(sql.Contains("WHERE [Id] = 1"));
        }

        [TestMethod]
        public void Update_MixedSetTypes_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Name, "Updated Product")
                .Set(p => p.Price, p => p.Price + 10m)
                .Set(p => p.IsDeleted, false)
                .Set(p => p.UpdatedAt, DateTime.Now)
                .Where(p => p.CategoryId == 2);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Name] = 'Updated Product'"));
            Assert.IsTrue(sql.Contains("[Price] = [Price] + 10"));
            Assert.IsTrue(sql.Contains("[IsDeleted] = 0"));
            Assert.IsTrue(sql.Contains("WHERE [CategoryId] = 2"));
            
            // 确保有多个SET子句
            var setCount = sql.Split(",").Length;
            Assert.IsTrue(setCount >= 4, "Should have multiple SET clauses separated by commas");
        }

        [TestMethod]
        public void Update_NullValues_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Description, (string?)null)
                .Set(p => p.UpdatedAt, (DateTime?)null)
                .Set(p => p.Tags, (string?)null)
                .Where(p => p.IsDeleted == true);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("NULL"));
            Assert.IsTrue(sql.Contains("[IsDeleted] = 1"));
        }

        [TestMethod]
        public void Update_ComplexWhereConditions_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Price, p => p.Price * 0.9m)
                .Where(p => p.CreatedAt < DateTime.Now.AddMonths(-6))
                .Where(p => p.StockQuantity > 0)
                .Where(p => p.Price > 100m);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("AND"));
            Assert.IsTrue(sql.Contains("[StockQuantity] > 0"));
            Assert.IsTrue(sql.Contains("[Price] > 100"));
        }

        #endregion

        #region 特殊数据类型测试

        [TestMethod]
        public void DataTypes_NullableFields_HandledCorrectly()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.Description == null)
                .Where(p => p.UpdatedAt != null)
                .Where(p => p.Tags != null && p.Tags.Length > 0);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Description] IS NULL"));
            Assert.IsTrue(sql.Contains("[UpdatedAt] IS NOT NULL"));
            Assert.IsTrue(sql.Contains("LEN([Tags]) > 0"));
        }

        [TestMethod]
        public void DataTypes_NumericOperations_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.Price >= 10.5m)
                .Where(p => p.StockQuantity % 10 == 0)
                .Where(p => p.CategoryId * 2 < 20);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Price] >= 10.5"));
            Assert.IsTrue(sql.Contains("[StockQuantity] % 10 = 0"));
            Assert.IsTrue(sql.Contains("[CategoryId] * 2 < 20"));
        }

        [TestMethod]
        public void DataTypes_DateTimeOperations_GeneratesCorrectSql()
        {
            var testDate = new DateTime(2023, 1, 1);
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.CreatedAt >= testDate)
                .Where(p => p.UpdatedAt != null && p.UpdatedAt.Value.AddDays(30) > DateTime.Now);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[CreatedAt] >= '2023-01-01"));
            Assert.IsTrue(sql.Contains("DATEADD(DAY"));
            Assert.IsTrue(sql.Contains("IS NOT NULL"));
        }

        [TestMethod]
        public void DataTypes_BooleanOperations_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.IsDeleted)
                .Where(p => !p.IsDeleted)
                .Where(p => p.IsDeleted == true)
                .Where(p => p.IsDeleted == false);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[IsDeleted] = 1"));
            Assert.IsTrue(sql.Contains("[IsDeleted] = 0"));
            
            // 应该有4个AND连接的条件
            var andCount = sql.Split("AND").Length - 1;
            Assert.AreEqual(3, andCount);
        }

        #endregion

        #region 性能相关测试

        [TestMethod]
        public void Performance_LargeNumberOfConditions_HandlesEfficiently()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            
            // 添加大量WHERE条件
            for (int i = 1; i <= 100; i++)
            {
                expr.Where(p => p.CategoryId != i);
            }

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Length > 1000);
            Assert.IsTrue(sql.Contains("WHERE"));
            
            // 应该有99个AND操作符（100个条件之间）
            var andCount = sql.Split("AND").Length - 1;
            Assert.AreEqual(99, andCount);
        }

        [TestMethod]
        public void Performance_ComplexNestedExpressions_GeneratesCorrectSql()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => (p.Price > 10m && p.Price < 100m) || (p.StockQuantity > 0 && p.IsDeleted == false))
                .Where(p => p.Name.Contains("Special") || p.Description != null && p.Description.Contains("Premium"));

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("WHERE"));
            Assert.IsTrue(sql.Contains("AND"));
            // 复杂表达式应该被正确处理
            Assert.IsTrue(sql.Length > 100);
        }

        [TestMethod]
        public void Performance_LargeInsertBatch_HandlesCorrectly()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.InsertInto();
            
            // 添加大量插入行
            for (int i = 1; i <= 50; i++)
            {
                expr.AddValues(i, $"Product {i}", $"Description {i}", i * 10.5m, 
                    i % 5 + 1, DateTime.Now, (DateTime?)null, false, i * 10, $"tag{i}");
            }

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("VALUES"));
            
            // 应该包含50组值
            var valueCount = sql.Split("VALUES")[1].Count(c => c == '(');
            Assert.AreEqual(50, valueCount);
            
            // SQL应该相当长但仍然可管理
            Assert.IsTrue(sql.Length > 5000);
            Assert.IsTrue(sql.Length < 50000);
        }

        #endregion

        #region 边界情况和错误处理

        [TestMethod]
        public void EdgeCase_EmptyStringValues_HandledCorrectly()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Name, "")
                .Set(p => p.Description, "")
                .Where(p => p.Tags == "");

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("[Name] = ''"));
            Assert.IsTrue(sql.Contains("[Description] = ''"));
            Assert.IsTrue(sql.Contains("[Tags] = ''"));
        }

        [TestMethod]
        public void EdgeCase_SpecialCharactersInStrings_EscapedCorrectly()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Set(p => p.Name, "Product's \"Special\" Edition")
                .Set(p => p.Description, "Line 1\nLine 2\tTabbed")
                .Where(p => p.Tags != null && p.Tags.Contains("'quoted'"));

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("Product''s"));  // 单引号应该被转义
            Assert.IsTrue(sql.Contains("\"Special\""));
        }

        [TestMethod]
        public void EdgeCase_ExtremeNumericValues_HandledCorrectly()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.Price >= decimal.MinValue)
                .Where(p => p.Price <= decimal.MaxValue)
                .Where(p => p.StockQuantity >= int.MinValue)
                .Where(p => p.StockQuantity <= int.MaxValue);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains(decimal.MinValue.ToString()));
            Assert.IsTrue(sql.Contains(int.MinValue.ToString()));
        }

        [TestMethod]
        public void EdgeCase_MinMaxDateTime_HandledCorrectly()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            expr.Where(p => p.CreatedAt > DateTime.MinValue)
                .Where(p => p.CreatedAt < DateTime.MaxValue);

            var sql = expr.ToSql();
            Assert.IsTrue(sql.Contains("0001")); // DateTime.MinValue year
            Assert.IsTrue(sql.Contains("9999")); // DateTime.MaxValue year
        }

        [TestMethod]
        public void EdgeCase_OnlyTakeNoSkip_GeneratesCorrectSql()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<Product>.ForSqlServer()),
                ("MySQL", ExpressionToSql<Product>.ForMySql()),
                ("SQLite", ExpressionToSql<Product>.ForSqlite())
            };

            foreach (var (dialectName, expr) in testCases)
            {
                using (expr)
                {
                    expr.OrderBy(p => p.Id).Take(10);
                    var sql = expr.ToSql();
                    
                    if (dialectName == "SQL Server")
                    {
                        Assert.IsTrue(sql.Contains("FETCH NEXT 10 ROWS ONLY"));
                        Assert.IsFalse(sql.Contains("OFFSET"));
                    }
                    else
                    {
                        Assert.IsTrue(sql.Contains("LIMIT 10"));
                        Assert.IsFalse(sql.Contains("OFFSET"));
                    }
                }
            }
        }

        [TestMethod]
        public void EdgeCase_OnlySkipNoTake_GeneratesCorrectSql()
        {
            var testCases = new[]
            {
                ("SQL Server", ExpressionToSql<Product>.ForSqlServer()),
                ("MySQL", ExpressionToSql<Product>.ForMySql()),
                ("SQLite", ExpressionToSql<Product>.ForSqlite())
            };

            foreach (var (dialectName, expr) in testCases)
            {
                using (expr)
                {
                    expr.OrderBy(p => p.Id).Skip(10);
                    var sql = expr.ToSql();
                    
                    if (dialectName == "SQL Server")
                    {
                        Assert.IsTrue(sql.Contains("OFFSET 10 ROWS"));
                        Assert.IsFalse(sql.Contains("FETCH"));
                    }
                    else
                    {
                        Assert.IsTrue(sql.Contains("OFFSET 10"));
                        Assert.IsFalse(sql.Contains("LIMIT"));
                    }
                }
            }
        }

        #endregion

        #region 方法链和流畅接口测试

        [TestMethod]
        public void FluentInterface_ChainedOperations_ReturnsCorrectType()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            
            var result = expr
                .Where(p => p.Price > 0)
                .OrderBy(p => p.Name)
                .Skip(10)
                .Take(20);

            Assert.IsNotNull(result);
            Assert.AreSame(expr, result, "Fluent methods should return the same instance");
        }

        [TestMethod]
        public void FluentInterface_UpdateChain_ReturnsCorrectType()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            
            var result = expr
                .Set(p => p.Price, 100m)
                .Set(p => p.UpdatedAt, DateTime.Now)
                .Where(p => p.Id == 1);

            Assert.IsNotNull(result);
            Assert.AreSame(expr, result, "Fluent SET methods should return the same instance");
        }

        [TestMethod]
        public void FluentInterface_InsertChain_ReturnsCorrectType()
        {
            using var expr = ExpressionToSql<Product>.ForSqlServer();
            
            var result = expr
                .InsertInto()
                .Values(1, "Test", (string?)null, 10m, 1, DateTime.Now, (DateTime?)null, false, 100, (string?)null)
                .AddValues(2, "Test2", (string?)null, 20m, 1, DateTime.Now, (DateTime?)null, false, 200, (string?)null);

            Assert.IsNotNull(result);
            Assert.AreSame(expr, result, "Fluent INSERT methods should return the same instance");
        }

        #endregion
    }
}
