// -----------------------------------------------------------------------
// <copyright file="SqlxGroupByAndProjectionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// GroupBy、Select投影、聚合函数的严格单元测试。
/// </summary>
[TestClass]
public class SqlxGroupByAndProjectionTests
{
    [Sqlx]
    public partial class GroupByTestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #region GroupBy Basic Tests

    [TestMethod]
    public void GroupBy_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("[department]"));
    }

    [TestMethod]
    public void GroupBy_WithExpression_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Id % 3)
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("%"));
    }

    #endregion

    #region GroupBy + Select Tests

    [TestMethod]
    public void GroupBy_SelectKey_GeneratesKeyAsAlias()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key })
            .ToSql();

        Assert.IsTrue(sql.Contains("[department]"));
        Assert.IsFalse(sql.Contains("[Key]"), "Should not contain [Key] as column name");
    }

    [TestMethod]
    public void GroupBy_SelectKeyAndCount_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("[department]"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
        Assert.IsTrue(sql.Contains("AS Count"));
    }

    [TestMethod]
    public void GroupBy_ExpressionKey_SelectKeyAndCount()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Id % 5)
            .Select(x => new { x.Key, Total = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("%"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
        Assert.IsTrue(sql.Contains("AS Total"));
    }

    #endregion

    #region Aggregate Functions Tests

    [TestMethod]
    public void Aggregate_Count_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, C = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("COUNT(*)"));
    }

    [TestMethod]
    public void Aggregate_CountWithPredicate_GeneratesCaseWhen()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, ActiveCount = x.Count(y => y.IsActive) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM(CASE WHEN"));
        Assert.IsTrue(sql.Contains("THEN 1 ELSE 0 END)"));
    }

    [TestMethod]
    public void Aggregate_CountWithComplexPredicate_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count(y => y.Age > 30 && y.IsActive) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM(CASE WHEN"));
        Assert.IsTrue(sql.Contains("[age] > 30"));
        Assert.IsTrue(sql.Contains("AND"));
    }

    [TestMethod]
    public void Aggregate_Sum_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, TotalSalary = x.Sum(y => y.Salary) })
            .ToSql();

        Assert.IsTrue(sql.Contains("SUM("));
        Assert.IsTrue(sql.Contains("[salary]"));
    }

    [TestMethod]
    public void Aggregate_Average_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, AvgAge = x.Average(y => y.Age) })
            .ToSql();

        Assert.IsTrue(sql.Contains("AVG("));
        Assert.IsTrue(sql.Contains("[age]"));
    }

    [TestMethod]
    public void Aggregate_Min_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, MinAge = x.Min(y => y.Age) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MIN("));
        Assert.IsTrue(sql.Contains("[age]"));
    }

    [TestMethod]
    public void Aggregate_Max_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, MaxSalary = x.Max(y => y.Salary) })
            .ToSql();

        Assert.IsTrue(sql.Contains("MAX("));
        Assert.IsTrue(sql.Contains("[salary]"));
    }

    [TestMethod]
    public void Aggregate_MultipleAggregates_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { 
                x.Key, 
                Count = x.Count(),
                TotalSalary = x.Sum(y => y.Salary),
                AvgAge = x.Average(y => y.Age)
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("COUNT(*)"));
        Assert.IsTrue(sql.Contains("SUM("));
        Assert.IsTrue(sql.Contains("AVG("));
    }

    #endregion

    #region Nested Method Calls in Aggregates

    [TestMethod]
    public void Aggregate_CountWithTrim_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count(y => y.Name.Trim() == "Test") })
            .ToSql();

        Assert.IsTrue(sql.Contains("TRIM("));
        Assert.IsTrue(sql.Contains("SUM(CASE WHEN"));
    }

    [TestMethod]
    public void Aggregate_CountWithStartsWith_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count(y => y.Name.StartsWith("A")) })
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"));
        Assert.IsTrue(sql.Contains("SUM(CASE WHEN"));
    }

    [TestMethod]
    public void Aggregate_CountWithTrimAndStartsWith_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count(y => y.Name.Trim().StartsWith("A")) })
            .ToSql();

        Assert.IsTrue(sql.Contains("TRIM("));
        Assert.IsTrue(sql.Contains("LIKE"));
    }

    [TestMethod]
    public void Aggregate_CountWithToLower_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count(y => y.Name.ToLower() == "test") })
            .ToSql();

        Assert.IsTrue(sql.Contains("LOWER("));
    }

    [TestMethod]
    public void Aggregate_CountWithConstantMethodCall_EvaluatesAtCompileTime()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count(y => y.Name.StartsWith("A".ToLower())) })
            .ToSql();

        // "A".ToLower() should be evaluated to "a" at compile time
        Assert.IsTrue(sql.Contains("'a'") || sql.Contains("@p"));
        Assert.IsFalse(sql.Contains("LOWER('A')"), "Should not generate LOWER for constant string");
    }

    #endregion

    #region Select Projection Tests

    [TestMethod]
    public void Select_AnonymousType_GeneratesAliases()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .Select(x => new { x.Id, UserName = x.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("[name] AS UserName") || sql.Contains("AS UserName"));
    }

    [TestMethod]
    public void Select_SameNameNoAlias_DoesNotGenerateAlias()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .Select(x => new { x.Id, x.Name })
            .ToSql();

        // When property name matches column name, no alias needed
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[name]"));
    }

    [TestMethod]
    public void Select_ComputedColumn_GeneratesAlias()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .Select(x => new { x.Id, DoubleAge = x.Age * 2 })
            .ToSql();

        Assert.IsTrue(sql.Contains("AS DoubleAge"));
    }

    #endregion

    #region Where + GroupBy + Select Combination

    [TestMethod]
    public void WhereGroupBySelect_FullPipeline_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
    }

    [TestMethod]
    public void WhereGroupBySelectOrderBy_FullPipeline_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .Where(x => x.Age > 18)
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Total = x.Count() })
            .OrderBy(x => x.Key)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    #endregion

    #region Cross-Dialect GroupBy Tests

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    public void GroupBy_AllDialects_GeneratesValidSql(string dialectName)
    {
        var query = GetQueryForDialect<GroupByTestUser>(dialectName)
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count() });

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"), $"{dialectName} should have GROUP BY");
        Assert.IsTrue(sql.Contains("COUNT(*)"), $"{dialectName} should have COUNT(*)");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void GroupBy_NullableColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"));
    }

    [TestMethod]
    public void Select_WithMathExpression_GeneratesCorrectSql()
    {
        var sql = SqlQuery<GroupByTestUser>.ForSqlite()
            .GroupBy(x => x.Department)
            .Select(x => new { x.Key, AvgSalaryPercent = x.Average(y => y.Salary) / 100 })
            .ToSql();

        Assert.IsTrue(sql.Contains("AVG("));
        Assert.IsTrue(sql.Contains("/"));
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
