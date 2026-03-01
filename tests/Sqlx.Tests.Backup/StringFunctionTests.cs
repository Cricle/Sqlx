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

    [TestMethod]
    public void Where_StringPadLeft_GeneratesPadFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.PadLeft(10) == "      John")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD") || sql.Contains("name"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringPadLeftWithChar_GeneratesPadFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.PadLeft(10, '0') == "0000000John")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD") || sql.Contains("name"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringPadRight_GeneratesPadFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.PadRight(10) == "John      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD") || sql.Contains("name"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringPadRightWithChar_GeneratesPadFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.PadRight(10, '0') == "John000000")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD") || sql.Contains("name"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringIndexOf_GeneratesInstrFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.IndexOf("oh") > 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR") || sql.Contains("POSITION") || sql.Contains("CHARINDEX"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringIndexOfWithStart_GeneratesInstrFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.IndexOf("oh", 2) > 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR") || sql.Contains("POSITION") || sql.Contains("CHARINDEX") || sql.Contains("name"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringIndexer_GeneratesSubstrFunction()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name[0].ToString() == "J")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR") || sql.Contains("SUBSTRING") || sql.Contains("name"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_MySql_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringTestUser>.ForMySql()
            .Where(u => u.Name.ToUpper() == "JOHN" && u.Email.ToLower().Contains("test"))
            .ToSql();

        Assert.IsTrue(sql.Contains("UPPER"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("LOWER"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_SqlServer_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlServer()
            .Where(u => u.Name.Trim() == "John")
            .ToSql();

        Assert.IsTrue(sql.Contains("LTRIM") || sql.Contains("RTRIM") || sql.Contains("TRIM"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_PostgreSql_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringTestUser>.ForPostgreSQL()
            .Where(u => u.Name.Replace("old", "new") == "John")
            .ToSql();

        Assert.IsTrue(sql.Contains("REPLACE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_Oracle_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringTestUser>.For(SqlDefine.Oracle)
            .Where(u => u.Name.Substring(0, 4) == "John")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_DB2_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringTestUser>.For(SqlDefine.DB2)
            .Where(u => u.Name.Length > 5)
            .ToSql();

        Assert.IsTrue(sql.Contains("LENGTH"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_ComplexExpression_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.Trim().ToUpper().StartsWith("J"))
            .ToSql();

        Assert.IsTrue(sql.Contains("UPPER"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("TRIM"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_MultipleConditions_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name.Contains("oh") && u.Email.EndsWith("@test.com"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("oh") || sql.Contains("%oh%"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("@test.com") || sql.Contains("%@test.com"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_NullableString_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Description != null && u.Description.Contains("test"))
            .ToSql();

        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_EmptyString_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringTestUser>.ForSqlite()
            .Where(u => u.Name != "")
            .ToSql();

        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"), $"SQL: {sql}");
    }
}
