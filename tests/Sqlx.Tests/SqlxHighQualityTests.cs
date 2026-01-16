// -----------------------------------------------------------------------
// <copyright file="SqlxHighQualityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// 全方位高质量单元测试：边界条件、错误处理、并发安全、复杂场景。
/// </summary>
[TestClass]
public class SqlxHighQualityTests
{
    #region Test Entities

    [SqlxEntity]
    public partial class HQTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? NullableValue { get; set; }
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Boundary Value Tests - 边界值测试

    [TestMethod]
    public void BoundaryTest_IntMaxValue_HandlesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Id == int.MaxValue)
            .ToSql();

        Assert.IsTrue(sql.Contains(int.MaxValue.ToString()));
    }

    [TestMethod]
    public void BoundaryTest_IntMinValue_HandlesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Id == int.MinValue)
            .ToSql();

        Assert.IsTrue(sql.Contains(int.MinValue.ToString()));
    }

    [TestMethod]
    public void BoundaryTest_EmptyString_HandlesCorrectly()
    {
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == "")
            .ToSqlWithParameters();

        Assert.IsTrue(parameters.Any(p => p.Value?.ToString() == ""));
    }

    [TestMethod]
    public void BoundaryTest_VeryLongString_HandlesCorrectly()
    {
        var longString = new string('x', 10000);
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == longString)
            .ToSqlWithParameters();

        // Check that parameters exist and SQL contains parameter placeholder
        Assert.IsTrue(parameters.Any(), "Should have at least one parameter");
        Assert.IsTrue(sql.Contains("@p"), "SQL should contain parameter placeholder");
    }

    [TestMethod]
    public void BoundaryTest_TakeZero_GeneratesCorrectSql()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Take(0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LIMIT 0"));
    }

    [TestMethod]
    public void BoundaryTest_SkipZero_GeneratesCorrectSql()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Skip(0)
            .ToSql();

        Assert.IsTrue(sql.Contains("SELECT"));
    }

    #endregion

    #region NULL Handling Tests - NULL 处理测试

    [TestMethod]
    public void NullTest_NullableIsNull_GeneratesIsNull()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.NullableValue == null)
            .ToSql();

        Assert.IsTrue(sql.Contains("IS NULL"));
    }

    [TestMethod]
    public void NullTest_NullableIsNotNull_GeneratesIsNotNull()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.NullableValue != null)
            .ToSql();

        Assert.IsTrue(sql.Contains("IS NOT NULL"));
    }

    [TestMethod]
    public void NullTest_CoalesceOperator_GeneratesCoalesce()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => (e.NullableValue ?? 0) > 10)
            .ToSql();

        Assert.IsTrue(sql.Contains("COALESCE") || sql.Contains("IFNULL"));
    }

    #endregion

    #region Special Characters Tests - 特殊字符测试

    [TestMethod]
    public void SpecialChars_SingleQuote_HandlesCorrectly()
    {
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == "O'Brien")
            .ToSqlWithParameters();

        Assert.AreEqual("O'Brien", parameters.First().Value);
    }

    [TestMethod]
    public void SpecialChars_Unicode_HandlesCorrectly()
    {
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == "你好世界🌍")
            .ToSqlWithParameters();

        Assert.AreEqual("你好世界🌍", parameters.First().Value);
    }

    [TestMethod]
    public void SpecialChars_SqlInjection_PreventedByParameterization()
    {
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == "'; DROP TABLE Users; --")
            .ToSqlWithParameters();

        Assert.IsFalse(sql.Contains("DROP TABLE"));
        Assert.AreEqual("'; DROP TABLE Users; --", parameters.First().Value);
    }

    #endregion

    #region Complex Query Tests - 复杂查询测试

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void ComplexQuery_AllClauses_AllDialects(string dialectName)
    {
        var query = GetQueryForDialect<HQTestEntity>(dialectName)
            .Where(e => e.IsActive)
            .Where(e => e.Amount > 100)
            .OrderBy(e => e.Name)
            .ThenByDescending(e => e.Amount)
            .Skip(10)
            .Take(20)
            .Distinct();

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DISTINCT"));
    }

    [TestMethod]
    public void ComplexQuery_NestedBooleanLogic_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => (e.Name == "Test1" || e.Name == "Test2") && e.IsActive)
            .ToSql();

        Assert.IsTrue(sql.Contains("AND"));
        Assert.IsTrue(sql.Contains("OR"));
    }

    [TestMethod]
    public void ComplexQuery_MultipleOrderBy_PreservesOrder()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .OrderBy(e => e.Name)
            .ThenByDescending(e => e.Amount)
            .ThenBy(e => e.CreatedAt)
            .ToSql();

        var orderByIndex = sql.IndexOf("ORDER BY");
        var nameIndex = sql.IndexOf("[name]", orderByIndex);
        var amountIndex = sql.IndexOf("[amount]", nameIndex);
        var createdIndex = sql.IndexOf("[created_at]", amountIndex);

        Assert.IsTrue(nameIndex < amountIndex && amountIndex < createdIndex);
    }

    #endregion

    #region Parameterized Query Tests - 参数化查询测试

    [TestMethod]
    public void ParameterizedQuery_SingleParameter_GeneratesCorrectly()
    {
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == "Test")
            .ToSqlWithParameters();

        Assert.AreEqual(1, parameters.Count());
        Assert.IsTrue(sql.Contains("@p"));
    }

    [TestMethod]
    public void ParameterizedQuery_MultipleParameters_GeneratesCorrectly()
    {
        var (sql, parameters) = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name == "Test" && e.Amount > 100)
            .ToSqlWithParameters();

        Assert.IsTrue(parameters.Count() >= 2);
    }

    [TestMethod]
    [DataRow("SQLite", "@p")]
    [DataRow("SqlServer", "@p")]
    [DataRow("MySql", "@p")]
    [DataRow("PostgreSQL", "$p")]
    [DataRow("Oracle", ":p")]
    public void ParameterizedQuery_CorrectPrefix_AllDialects(string dialectName, string expectedPrefix)
    {
        var (sql, parameters) = GetQueryForDialect<HQTestEntity>(dialectName)
            .Where(e => e.Name == "Test")
            .ToSqlWithParameters();

        Assert.IsTrue(sql.Contains(expectedPrefix) || dialectName == "DB2");
    }

    #endregion

    #region Concurrent Access Tests - 并发访问测试

    [TestMethod]
    public void ConcurrentQueries_100Tasks_ThreadSafe()
    {
        var tasks = new List<Task<string>>();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                return SqlQuery<HQTestEntity>.ForSqlite()
                    .Where(e => e.Id == index)
                    .ToSql();
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.IsTrue(tasks.All(t => t.Result.Contains("WHERE")));
    }

    [TestMethod]
    public void ConcurrentParameterizedQueries_100Tasks_ThreadSafe()
    {
        var tasks = new List<Task<(string, IEnumerable<KeyValuePair<string, object?>>)>>();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                return SqlQuery<HQTestEntity>.ForSqlite()
                    .Where(e => e.Id == index)
                    .ToSqlWithParameters();
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.IsTrue(tasks.All(t => t.Result.Item2.Any()));
    }

    #endregion

    #region Performance Boundary Tests - 性能边界测试

    [TestMethod]
    public void PerformanceTest_100WhereConditions_GeneratesCorrectly()
    {
        var query = SqlQuery<HQTestEntity>.ForSqlite().AsQueryable();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            query = query.Where(e => e.Id != index);
        }

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Length > 1000);
    }

    [TestMethod]
    public void PerformanceTest_LargeInClause_GeneratesCorrectly()
    {
        var largeList = Enumerable.Range(1, 1000).ToList();
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => largeList.Contains(e.Id))
            .ToSql();

        Assert.IsTrue(sql.Contains("IN"));
    }

    #endregion

    #region Error Handling Tests - 错误处理测试

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ErrorHandling_ToSqlOnNonSqlxQueryable_ThrowsException()
    {
        var list = new List<HQTestEntity> { new HQTestEntity() };
        var query = list.AsQueryable();
        
        SqlxQueryableExtensions.ToSql(query);
    }

    [TestMethod]
    public void ErrorHandling_EmptyQuery_ReturnsValidSql()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite().ToSql();

        Assert.IsTrue(sql.StartsWith("SELECT"));
        Assert.IsTrue(sql.Contains("FROM"));
    }

    #endregion

    #region String Function Tests - 字符串函数测试

    [TestMethod]
    public void StringFunction_ToUpper_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name.ToUpper() == "TEST")
            .ToSql();

        Assert.IsTrue(sql.Contains("UPPER"));
    }

    [TestMethod]
    public void StringFunction_ToLower_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name.ToLower() == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LOWER"));
    }

    [TestMethod]
    public void StringFunction_Contains_GeneratesLike()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name.Contains("test"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"));
    }

    [TestMethod]
    public void StringFunction_StartsWith_GeneratesLike()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name.StartsWith("test"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"));
    }

    [TestMethod]
    public void StringFunction_EndsWith_GeneratesLike()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => e.Name.EndsWith("test"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"));
    }

    #endregion

    #region Math Function Tests - 数学函数测试

    [TestMethod]
    public void MathFunction_Abs_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => Math.Abs(e.Id) > 10)
            .ToSql();

        Assert.IsTrue(sql.Contains("ABS"));
    }

    [TestMethod]
    public void MathFunction_Round_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => Math.Round(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("ROUND"));
    }

    [TestMethod]
    public void MathFunction_Floor_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => Math.Floor(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("FLOOR"));
    }

    [TestMethod]
    public void MathFunction_Ceiling_GeneratesCorrectly()
    {
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => Math.Ceiling(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("CEIL"));
    }

    #endregion

    #region Collection Operations Tests - 集合操作测试

    [TestMethod]
    public void CollectionOp_InEmptyList_GeneratesCorrectly()
    {
        var emptyList = new List<int>();
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => emptyList.Contains(e.Id))
            .ToSql();

        Assert.IsTrue(sql.Contains("IN"));
    }

    [TestMethod]
    public void CollectionOp_InSingleItem_GeneratesCorrectly()
    {
        var singleList = new List<int> { 1 };
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => singleList.Contains(e.Id))
            .ToSql();

        Assert.IsTrue(sql.Contains("IN"));
    }

    [TestMethod]
    public void CollectionOp_InManyItems_GeneratesCorrectly()
    {
        var manyItems = Enumerable.Range(1, 100).ToList();
        var sql = SqlQuery<HQTestEntity>.ForSqlite()
            .Where(e => manyItems.Contains(e.Id))
            .ToSql();

        Assert.IsTrue(sql.Contains("IN"));
    }

    #endregion

    #region Cross-Dialect Consistency Tests - 跨方言一致性测试

    [TestMethod]
    public void CrossDialect_SimpleQuery_AllDialectsGenerateValidSql()
    {
        var dialects = new[] { "SQLite", "SqlServer", "MySql", "PostgreSQL", "Oracle", "DB2" };

        foreach (var dialect in dialects)
        {
            var sql = GetQueryForDialect<HQTestEntity>(dialect)
                .Where(e => e.Amount > 100)
                .ToSql();

            Assert.IsTrue(sql.Contains("SELECT"), $"{dialect} should have SELECT");
            Assert.IsTrue(sql.Contains("FROM"), $"{dialect} should have FROM");
            Assert.IsTrue(sql.Contains("WHERE"), $"{dialect} should have WHERE");
        }
    }

    [TestMethod]
    public void CrossDialect_ComplexQuery_AllDialectsGenerateValidSql()
    {
        var dialects = new[] { "SQLite", "SqlServer", "MySql", "PostgreSQL", "Oracle", "DB2" };

        foreach (var dialect in dialects)
        {
            var sql = GetQueryForDialect<HQTestEntity>(dialect)
                .Where(e => e.Amount > 100 && e.IsActive)
                .OrderBy(e => e.Name)
                .Skip(10)
                .Take(20)
                .ToSql();

            Assert.IsTrue(sql.Contains("SELECT"), $"{dialect} should have SELECT");
            Assert.IsTrue(sql.Contains("WHERE"), $"{dialect} should have WHERE");
            Assert.IsTrue(sql.Contains("ORDER BY"), $"{dialect} should have ORDER BY");
        }
    }

    #endregion

    #region Helper Methods

    private static IQueryable<T> GetQueryForDialect<T>(string dialectName) => dialectName switch
    {
        "SQLite" => SqlQuery<T>.ForSqlite(),
        "SqlServer" => SqlQuery<T>.ForSqlServer(),
        "MySql" => SqlQuery<T>.ForMySql(),
        "PostgreSQL" => SqlQuery<T>.ForPostgreSQL(),
        "Oracle" => SqlQuery<T>.ForOracle(),
        "DB2" => SqlQuery<T>.ForDB2(),
        _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
    };

    #endregion
}

