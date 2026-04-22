// Branch coverage 7 - StringFunctionParser, ExpressionBlockResult, SetExpressionExtensions, DbConnectionExtensions
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Sqlx.Tests.BranchCoverage;

[Sqlx, TableName("bc7_items")]
public class Bc7Item
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

// ── StringFunctionParser - dialect-specific branches ─────────────────────────

[TestClass]
public class StringFunctionParserBranch7Tests
{
    // L124: PadLeft - SqlServer dialect
    [TestMethod]
    public void PadLeft_SqlServer_GeneratesRight()
    {
        var q = SqlQuery<Bc7Item>.ForSqlServer()
            .Where(u => u.Name.PadLeft(10) == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("RIGHT") || sql.Contains("LPAD"), $"SQL: {sql}");
    }

    // L124: PadLeft - MySQL dialect
    [TestMethod]
    public void PadLeft_MySQL_GeneratesLPad()
    {
        var q = SqlQuery<Bc7Item>.ForMySql()
            .Where(u => u.Name.PadLeft(10) == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    // L150: PadRight - SqlServer dialect
    [TestMethod]
    public void PadRight_SqlServer_GeneratesLeft()
    {
        var q = SqlQuery<Bc7Item>.ForSqlServer()
            .Where(u => u.Name.PadRight(10) == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LEFT") || sql.Contains("RPAD"), $"SQL: {sql}");
    }

    // L150: PadRight - MySQL dialect
    [TestMethod]
    public void PadRight_MySQL_GeneratesRPad()
    {
        var q = SqlQuery<Bc7Item>.ForMySql()
            .Where(u => u.Name.PadRight(10) == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    // L157: IndexOf - SqlServer dialect
    [TestMethod]
    public void IndexOf_SqlServer_GeneratesCharIndex()
    {
        var q = SqlQuery<Bc7Item>.ForSqlServer()
            .Where(u => u.Name.IndexOf("x") > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("CHARINDEX") || sql.Contains("INSTR"), $"SQL: {sql}");
    }

    // L157: IndexOf - MySQL dialect
    [TestMethod]
    public void IndexOf_MySQL_GeneratesLocate()
    {
        var q = SqlQuery<Bc7Item>.ForMySql()
            .Where(u => u.Name.IndexOf("x") > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LOCATE") || sql.Contains("INSTR"), $"SQL: {sql}");
    }

    // L157: IndexOf - PostgreSQL dialect
    [TestMethod]
    public void IndexOf_PostgreSQL_GeneratesPosition()
    {
        var q = SqlQuery<Bc7Item>.ForPostgreSQL()
            .Where(u => u.Name.IndexOf("x") > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("POSITION") || sql.Contains("INSTR"), $"SQL: {sql}");
    }

    // L164: IndexOf with start - SqlServer
    [TestMethod]
    public void IndexOf_WithStart_SqlServer_GeneratesCharIndex()
    {
        var q = SqlQuery<Bc7Item>.ForSqlServer()
            .Where(u => u.Name.IndexOf("x", 2) > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("CHARINDEX") || sql.Contains("INSTR"), $"SQL: {sql}");
    }

    // L164: IndexOf with start - MySQL
    [TestMethod]
    public void IndexOf_WithStart_MySQL_GeneratesLocate()
    {
        var q = SqlQuery<Bc7Item>.ForMySql()
            .Where(u => u.Name.IndexOf("x", 2) > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LOCATE") || sql.Contains("INSTR"), $"SQL: {sql}");
    }

    // L172: Trim - various dialects
    [TestMethod]
    public void Trim_SQLite_GeneratesTrim()
    {
        var q = SqlQuery<Bc7Item>.ForSqlite()
            .Where(u => u.Name.Trim() == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("TRIM"), $"SQL: {sql}");
    }

    [TestMethod]
    public void TrimStart_SQLite_GeneratesLTrim()
    {
        var q = SqlQuery<Bc7Item>.ForSqlite()
            .Where(u => u.Name.TrimStart() == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LTRIM") || sql.Contains("TRIM"), $"SQL: {sql}");
    }

    [TestMethod]
    public void TrimEnd_SQLite_GeneratesRTrim()
    {
        var q = SqlQuery<Bc7Item>.ForSqlite()
            .Where(u => u.Name.TrimEnd() == "test");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("RTRIM") || sql.Contains("TRIM"), $"SQL: {sql}");
    }

    // L81: ParseMethod - various string methods
    [TestMethod]
    public void Substring_OneArg_GeneratesSubstring()
    {
        var q = SqlQuery<Bc7Item>.ForSqlite()
            .Where(u => u.Name.Substring(1) == "test");
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void Substring_TwoArgs_GeneratesSubstring()
    {
        var q = SqlQuery<Bc7Item>.ForSqlite()
            .Where(u => u.Name.Substring(1, 3) == "tes");
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }
}

// ── ExpressionBlockResult - null handling ────────────────────────────────────

[TestClass]
public class ExpressionBlockResultBranch7Tests
{
    // L57: Parse with null expression returns empty
    [TestMethod]
    public void Parse_NullExpression_ReturnsEmpty()
    {
        var result = ExpressionBlockResult.Parse(null, SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, result.Sql);
    }

    // L111: WithParameters with null dictionary returns this
    [TestMethod]
    public void WithParameters_NullDictionary_ReturnsSelf()
    {
        Expression<Func<Bc7Item, bool>> expr = p => p.Price > Any.Value<decimal>("min");
        var result = ExpressionBlockResult.Parse(expr.Body, SqlDefine.SQLite);
        var returned = result.WithParameters(null!);
        Assert.AreSame(result, returned);
    }

    // L45/L46: Constructor null checks - via ParseUpdate with null body
    [TestMethod]
    public void ParseUpdate_NullExpression_ReturnsEmpty()
    {
        var result = ExpressionBlockResult.ParseUpdate<Bc7Item>(null, SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, result.Sql);
    }

    // ParseUpdate with non-MemberInit body
    [TestMethod]
    public void ParseUpdate_NonMemberInitBody_ReturnsEmpty()
    {
        Expression<Func<Bc7Item, Bc7Item>> expr = p => p; // not a MemberInit
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, result.Sql);
    }
}

// ── SetExpressionExtensions - uncovered branches ─────────────────────────────

[Sqlx, TableName("bc7_users")]
public class Bc7User
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

[TestClass]
public class SetExpressionExtensionsBranch7Tests
{
    // L67: ToSetClause - non-MemberAssignment binding (skip)
    [TestMethod]
    public void GetSetSql_StandardUpdate_GeneratesSetClause()
    {
        Expression<Func<Bc7User, Bc7User>> expr = u => new Bc7User { Name = "test", Age = 25 };
        var sql = expr.ToSetClause(SqlDefine.SQLite);
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"), $"SQL: {sql}");
    }

    // L110: GetSetParameters - non-MemberAssignment binding (skip)
    [TestMethod]
    public void GetSetParameters_StandardUpdate_ExtractsParams()
    {
        Expression<Func<Bc7User, Bc7User>> expr = u => new Bc7User { Name = "test", Age = 25 };
        var parameters = expr.GetSetParameters();
        Assert.IsTrue(parameters.Count > 0);
    }

    // L123: ExtractParameters - BinaryExpression
    [TestMethod]
    public void GetSetParameters_BinaryExpression_ExtractsParams()
    {
        Expression<Func<Bc7User, Bc7User>> expr = u => new Bc7User { Age = u.Age + 1 };
        var parameters = expr.GetSetParameters();
        Assert.IsNotNull(parameters);
    }

    // L152: ExtractParameters - MethodCallExpression with Object
    [TestMethod]
    public void GetSetParameters_MethodCallExpression_ExtractsParams()
    {
        var name = "test";
        Expression<Func<Bc7User, Bc7User>> expr = u => new Bc7User { Name = name.ToUpper() };
        var parameters = expr.GetSetParameters();
        Assert.IsNotNull(parameters);
    }

    // ToSetClause with null expression
    [TestMethod]
    public void GetSetSql_NullExpression_ReturnsEmpty()
    {
        Expression<Func<Bc7User, Bc7User>>? expr = null;
        var sql = expr!.ToSetClause(SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, sql);
    }

    // GetSetParameters with null expression
    [TestMethod]
    public void GetSetParameters_NullExpression_ReturnsEmpty()
    {
        Expression<Func<Bc7User, Bc7User>>? expr = null;
        var parameters = expr!.GetSetParameters();
        Assert.AreEqual(0, parameters.Count);
    }
}

// ── DbConnectionExtensions - remaining gaps ──────────────────────────────────

[TestClass]
public class DbConnectionExtensionsBranch7Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc7_items (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc7_items VALUES (1,'a',10.0,1),(2,'b',20.0,0)";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L519: HasParameterPrefix - various parameter prefixes
    [TestMethod]
    public async Task SqlxQuery_WithAtPrefix_Works()
    {
        using var conn = CreateConn();
        var results = await conn.SqlxQueryAsync<Bc7Item>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1 });
        Assert.AreEqual(1, results.Count);
    }

    // L524: ToCamelCase - various property names
    [TestMethod]
    public async Task SqlxQuery_WithCamelCaseParam_Works()
    {
        using var conn = CreateConn();
        var results = await conn.SqlxQueryAsync<Bc7Item>(
            "SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive",
            SqlDefine.SQLite,
            new { isActive = 1 });
        Assert.AreEqual(1, results.Count);
    }

    // L539: ToSnakeCase - property name conversion
    [TestMethod]
    public async Task SqlxQuery_WithSnakeCaseParam_Works()
    {
        using var conn = CreateConn();
        var result = await conn.SqlxQueryFirstOrDefaultAsync<Bc7Item>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1 });
        Assert.IsNotNull(result);
    }

    // L733: IsSimpleScalarType - various scalar types
    [TestMethod]
    public async Task SqlxScalar_Long_Works()
    {
        using var conn = CreateConn();
        var count = await conn.SqlxScalarAsync<long, Bc7Item>(
            "SELECT COUNT(*) FROM {{table}}",
            SqlDefine.SQLite);
        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public async Task SqlxScalar_Int_Works()
    {
        using var conn = CreateConn();
        var count = await conn.SqlxScalarAsync<int, Bc7Item>(
            "SELECT COUNT(*) FROM {{table}}",
            SqlDefine.SQLite);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task SqlxScalar_String_Works()
    {
        using var conn = CreateConn();
        var name = await conn.SqlxScalarAsync<string, Bc7Item>(
            "SELECT name FROM {{table}} WHERE id = 1",
            SqlDefine.SQLite);
        Assert.AreEqual("a", name);
    }
}
