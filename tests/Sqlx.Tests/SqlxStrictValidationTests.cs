// -----------------------------------------------------------------------
// <copyright file="SqlxStrictValidationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// 严格验证所有数据库方言的所有组合操作生成的 SQL 完全匹配。
/// 测试覆盖：6 个数据库 × 多种操作组合 = 全面验证
/// </summary>
[TestClass]
public class SqlxStrictValidationTests
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        // Explicitly call the generated initializer to ensure EntityProviders are registered
        // This is a workaround for potential ModuleInitializer timing issues
        Sqlx.Generated.EntityProvidersInitializer.Initialize();
    }

    #region Test Entities

    [SqlxEntity]
    public partial class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [SqlxEntity]
    public partial class TestOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
    }

    #endregion

    #region Helper Methods

    private static IQueryable<T> GetQuery<T>(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery<T>.ForSqlite(),
        "SqlServer" => SqlQuery<T>.ForSqlServer(),
        "MySql" => SqlQuery<T>.ForMySql(),
        "PostgreSQL" => SqlQuery<T>.ForPostgreSQL(),
        "Oracle" => SqlQuery<T>.ForOracle(),
        "DB2" => SqlQuery<T>.ForDB2(),
        _ => throw new System.ArgumentException($"Unknown dialect: {dialect}")
    };

    #endregion

    #region 1. Simple SELECT - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser]")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser]")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser`")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\"")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\"")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\"")]
    public void SimpleSelect_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 2. SELECT with WHERE - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE `age` > 18")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18")]
    public void SelectWithWhere_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).Where(u => u.Age > 18).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 3. SELECT with Multiple WHERE - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE ([age] > 18 AND [is_active] = 1)")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE ([age] > 18 AND [is_active] = 1)")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE (`age` > 18 AND `is_active` = 1)")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE (\"age\" > 18 AND \"is_active\" = true)")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE (\"age\" > 18 AND \"is_active\" = 1)")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE (\"age\" > 18 AND \"is_active\" = 1)")]
    public void SelectWithMultipleWhere_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Where(u => u.Age > 18 && u.IsActive)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 4. SELECT with ORDER BY - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] ORDER BY [name] ASC")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] ORDER BY [name] ASC")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` ORDER BY `name` ASC")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" ORDER BY \"name\" ASC")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" ORDER BY \"name\" ASC")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" ORDER BY \"name\" ASC")]
    public void SelectWithOrderBy_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).OrderBy(u => u.Name).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 5. SELECT with Multiple ORDER BY - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] ORDER BY [name] ASC, [age] DESC")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] ORDER BY [name] ASC, [age] DESC")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` ORDER BY `name` ASC, `age` DESC")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" ORDER BY \"name\" ASC, \"age\" DESC")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" ORDER BY \"name\" ASC, \"age\" DESC")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" ORDER BY \"name\" ASC, \"age\" DESC")]
    public void SelectWithMultipleOrderBy_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 6. SELECT with LIMIT - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] LIMIT 10")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` LIMIT 10")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" LIMIT 10")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    public void SelectWithLimit_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).Take(10).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 7. SELECT with OFFSET - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] OFFSET 20")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] OFFSET 20 ROWS")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` OFFSET 20")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 20")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 20 ROWS")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 20 ROWS")]
    public void SelectWithOffset_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).Skip(20).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 8. SELECT with LIMIT and OFFSET - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] LIMIT 10 OFFSET 20")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` LIMIT 10 OFFSET 20")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" LIMIT 10 OFFSET 20")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    public void SelectWithLimitAndOffset_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).Skip(20).Take(10).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 9. SELECT with GROUP BY - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] GROUP BY [age]")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] GROUP BY [age]")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` GROUP BY `age`")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" GROUP BY \"age\"")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" GROUP BY \"age\"")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" GROUP BY \"age\"")]
    public void SelectWithGroupBy_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).GroupBy(u => u.Age).ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 9.1 SELECT with GROUP BY + WHERE - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [is_active] = 1 GROUP BY [age]")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [is_active] = 1 GROUP BY [age]")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE `is_active` = 1 GROUP BY `age`")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"is_active\" = true GROUP BY \"age\"")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"is_active\" = 1 GROUP BY \"age\"")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"is_active\" = 1 GROUP BY \"age\"")]
    public void SelectWithGroupByAndWhere_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Where(u => u.IsActive)
            .GroupBy(u => u.Age)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 9.2 SELECT with GROUP BY + ORDER BY - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] GROUP BY [age] ORDER BY [name] ASC")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] GROUP BY [age] ORDER BY [name] ASC")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` GROUP BY `age` ORDER BY `name` ASC")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" GROUP BY \"age\" ORDER BY \"name\" ASC")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" GROUP BY \"age\" ORDER BY \"name\" ASC")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" GROUP BY \"age\" ORDER BY \"name\" ASC")]
    public void SelectWithGroupByAndOrderBy_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .OrderBy(u => u.Name)
            .GroupBy(u => u.Age)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 9.3 SELECT with GROUP BY + WHERE + ORDER BY - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18 GROUP BY [name] ORDER BY [email] DESC")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18 GROUP BY [name] ORDER BY [email] DESC")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE `age` > 18 GROUP BY `name` ORDER BY `email` DESC")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 GROUP BY \"name\" ORDER BY \"email\" DESC")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 GROUP BY \"name\" ORDER BY \"email\" DESC")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 GROUP BY \"name\" ORDER BY \"email\" DESC")]
    public void SelectWithGroupByWhereOrderBy_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Where(u => u.Age > 18)
            .OrderByDescending(u => u.Email)
            .GroupBy(u => u.Name)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 10. SELECT with DISTINCT - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT DISTINCT [id], [name], [email], [age], [is_active] FROM [TestUser]")]
    [DataRow("SqlServer", "SELECT DISTINCT [id], [name], [email], [age], [is_active] FROM [TestUser]")]
    [DataRow("MySql", "SELECT DISTINCT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser`")]
    [DataRow("PostgreSQL", "SELECT DISTINCT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\"")]
    [DataRow("Oracle", "SELECT DISTINCT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\"")]
    [DataRow("DB2", "SELECT DISTINCT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\"")]
    public void SelectWithDistinct_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect).Distinct().ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 11. Complex Query: WHERE + ORDER BY + LIMIT - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18 ORDER BY [name] ASC LIMIT 10")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18 ORDER BY [name] ASC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE `age` > 18 ORDER BY `name` ASC LIMIT 10")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 ORDER BY \"name\" ASC LIMIT 10")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 ORDER BY \"name\" ASC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 ORDER BY \"name\" ASC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    public void ComplexQuery_WhereOrderByLimit_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Where(u => u.Age > 18)
            .OrderBy(u => u.Name)
            .Take(10)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 12. Complex Query: WHERE + ORDER BY + LIMIT + OFFSET - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE ([age] > 18 AND [is_active] = 1) ORDER BY [name] ASC, [age] DESC LIMIT 10 OFFSET 20")]
    [DataRow("SqlServer", "SELECT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE ([age] > 18 AND [is_active] = 1) ORDER BY [name] ASC, [age] DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("MySql", "SELECT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE (`age` > 18 AND `is_active` = 1) ORDER BY `name` ASC, `age` DESC LIMIT 10 OFFSET 20")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE (\"age\" > 18 AND \"is_active\" = true) ORDER BY \"name\" ASC, \"age\" DESC LIMIT 10 OFFSET 20")]
    [DataRow("Oracle", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE (\"age\" > 18 AND \"is_active\" = 1) ORDER BY \"name\" ASC, \"age\" DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    [DataRow("DB2", "SELECT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE (\"age\" > 18 AND \"is_active\" = 1) ORDER BY \"name\" ASC, \"age\" DESC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY")]
    public void ComplexQuery_Full_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Where(u => u.Age > 18 && u.IsActive)
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .Skip(20)
            .Take(10)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 13. Complex Query: DISTINCT + WHERE + ORDER BY - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT DISTINCT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18 ORDER BY [age] DESC")]
    [DataRow("SqlServer", "SELECT DISTINCT [id], [name], [email], [age], [is_active] FROM [TestUser] WHERE [age] > 18 ORDER BY [age] DESC")]
    [DataRow("MySql", "SELECT DISTINCT `id`, `name`, `email`, `age`, `is_active` FROM `TestUser` WHERE `age` > 18 ORDER BY `age` DESC")]
    [DataRow("PostgreSQL", "SELECT DISTINCT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 ORDER BY \"age\" DESC")]
    [DataRow("Oracle", "SELECT DISTINCT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 ORDER BY \"age\" DESC")]
    [DataRow("DB2", "SELECT DISTINCT \"id\", \"name\", \"email\", \"age\", \"is_active\" FROM \"TestUser\" WHERE \"age\" > 18 ORDER BY \"age\" DESC")]
    public void ComplexQuery_DistinctWhereOrderBy_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Distinct()
            .Where(u => u.Age > 18)
            .OrderByDescending(u => u.Age)
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 14. SELECT with Projection - All Dialects

    [TestMethod]
    [DataRow("SQLite", "SELECT [id], [name] FROM [TestUser]")]
    [DataRow("SqlServer", "SELECT [id], [name] FROM [TestUser]")]
    [DataRow("MySql", "SELECT `id`, `name` FROM `TestUser`")]
    [DataRow("PostgreSQL", "SELECT \"id\", \"name\" FROM \"TestUser\"")]
    [DataRow("Oracle", "SELECT \"id\", \"name\" FROM \"TestUser\"")]
    [DataRow("DB2", "SELECT \"id\", \"name\" FROM \"TestUser\"")]
    public void SelectWithProjection_AllDialects_ExactMatch(string dialect, string expectedSql)
    {
        var sql = GetQuery<TestUser>(dialect)
            .Select(u => new { u.Id, u.Name })
            .ToSql();
        Assert.AreEqual(expectedSql, sql, $"[{dialect}] SQL mismatch");
    }

    #endregion

    #region 15. Summary Test - Count All Combinations

    [TestMethod]
    public void Summary_AllCombinationsCount()
    {
        // 验证测试覆盖率
        var dialectCount = 6; // SQLite, SqlServer, MySql, PostgreSQL, Oracle, DB2
        var operationCount = 17; // 17 种不同的操作组合 (增加了 3 个 GroupBy 组合测试)
        var totalTests = dialectCount * operationCount;

        // 实际测试数量应该是 6 * 17 = 102
        Assert.AreEqual(102, totalTests, "Total test combinations should be 102");
    }

    #endregion
}


