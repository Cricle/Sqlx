// -----------------------------------------------------------------------
// <copyright file="StringFunctionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// Tests for string function support in LINQ queries.
/// </summary>
[TestClass]
public class StringFunctionTests
{
    [Sqlx]
    [TableName("users")]
    public class StringTestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    [TestMethod]
    public void Where_StringToUpper_GeneratesUpperFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.ToUpper() == "JOHN")
            .ToSql();

        Assert.IsTrue(sql.Contains("UPPER([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringToLower_GeneratesLowerFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.ToLower() == "john")
            .ToSql();

        Assert.IsTrue(sql.Contains("LOWER([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringTrim_GeneratesTrimFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.Trim() == "John")
            .ToSql();

        Assert.IsTrue(sql.Contains("TRIM([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringLength_GeneratesLengthFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.Length > 5)
            .ToSql();

        Assert.IsTrue(sql.Contains("LENGTH([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringContains_GeneratesLikeOperator()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.Contains("John"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringStartsWith_GeneratesLikeOperator()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.StartsWith("John"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringEndsWith_GeneratesLikeOperator()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.EndsWith("Smith"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "UPPER")]
    [DataRow("MySQL", "UPPER")]
    [DataRow("PostgreSQL", "UPPER")]
    [DataRow("SqlServer", "UPPER")]
    public void ToUpper_AllDialects_GeneratesUpperFunction(string dialectName, string expectedFunc)
    {
        var query = dialectName switch
        {
            "SQLite" => SqlQuery<StringTestUser>.ForSqlite(),
            "MySQL" => SqlQuery<StringTestUser>.ForMySql(),
            "PostgreSQL" => SqlQuery<StringTestUser>.ForPostgreSQL(),
            "SqlServer" => SqlQuery<StringTestUser>.ForSqlServer(),
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var sql = query.Where(u => u.Name.ToUpper() == "JOHN").ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialectName}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "LOWER")]
    [DataRow("MySQL", "LOWER")]
    [DataRow("PostgreSQL", "LOWER")]
    [DataRow("SqlServer", "LOWER")]
    public void ToLower_AllDialects_GeneratesLowerFunction(string dialectName, string expectedFunc)
    {
        var query = dialectName switch
        {
            "SQLite" => SqlQuery<StringTestUser>.ForSqlite(),
            "MySQL" => SqlQuery<StringTestUser>.ForMySql(),
            "PostgreSQL" => SqlQuery<StringTestUser>.ForPostgreSQL(),
            "SqlServer" => SqlQuery<StringTestUser>.ForSqlServer(),
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var sql = query.Where(u => u.Name.ToLower() == "john").ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialectName}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "TRIM")]
    [DataRow("MySQL", "TRIM")]
    [DataRow("PostgreSQL", "TRIM")]
    public void Trim_AllDialects_GeneratesTrimFunction(string dialectName, string expectedFunc)
    {
        var query = dialectName switch
        {
            "SQLite" => SqlQuery<StringTestUser>.ForSqlite(),
            "MySQL" => SqlQuery<StringTestUser>.ForMySql(),
            "PostgreSQL" => SqlQuery<StringTestUser>.ForPostgreSQL(),
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var sql = query.Where(u => u.Name.Trim() == "John").ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialectName}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "LENGTH")]
    [DataRow("MySQL", "LENGTH")]
    [DataRow("PostgreSQL", "LENGTH")]
    [DataRow("SqlServer", "LEN")]
    public void Length_AllDialects_GeneratesLengthFunction(string dialectName, string expectedFunc)
    {
        var query = dialectName switch
        {
            "SQLite" => SqlQuery<StringTestUser>.ForSqlite(),
            "MySQL" => SqlQuery<StringTestUser>.ForMySql(),
            "PostgreSQL" => SqlQuery<StringTestUser>.ForPostgreSQL(),
            "SqlServer" => SqlQuery<StringTestUser>.ForSqlServer(),
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        var sql = query.Where(u => u.Name.Length > 5).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialectName}] SQL: {sql}");
    }
}
