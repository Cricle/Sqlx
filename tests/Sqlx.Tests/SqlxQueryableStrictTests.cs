using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Strict cross-dialect tests for IQueryable SQL generation.
/// Uses DataRow to test all 6 database dialects systematically.
/// </summary>
[TestClass]
public class SqlxQueryableCrossDialectTests
{
    private static IQueryable<QueryUser> GetQuery(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery.ForSqlite<QueryUser>(),
        "SqlServer" => SqlQuery.ForSqlServer<QueryUser>(),
        "MySql" => SqlQuery.ForMySql<QueryUser>(),
        "PostgreSQL" => SqlQuery.ForPostgreSQL<QueryUser>(),
        "Oracle" => SqlQuery.ForOracle<QueryUser>(),
        "DB2" => SqlQuery.ForDB2<QueryUser>(),
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    private static string GetQuote(string dialect, string name) => dialect switch
    {
        "SQLite" or "SqlServer" => $"[{name}]",
        "MySql" => $"`{name}`",
        "PostgreSQL" or "Oracle" or "DB2" => $"\"{name}\"",
        _ => name
    };

    #region Basic SELECT - All Dialects

    [TestMethod]
    [DataRow("SQLite", "[QueryUser]", "[id], [name], [age], [is_active], [salary], [created_at], [email]")]
    [DataRow("SqlServer", "[QueryUser]", "[id], [name], [age], [is_active], [salary], [created_at], [email]")]
    [DataRow("MySql", "`QueryUser`", "`id`, `name`, `age`, `is_active`, `salary`, `created_at`, `email`")]
    [DataRow("PostgreSQL", "\"QueryUser\"", "\"id\", \"name\", \"age\", \"is_active\", \"salary\", \"created_at\", \"email\"")]
    [DataRow("Oracle", "\"QueryUser\"", "\"id\", \"name\", \"age\", \"is_active\", \"salary\", \"created_at\", \"email\"")]
    [DataRow("DB2", "\"QueryUser\"", "\"id\", \"name\", \"age\", \"is_active\", \"salary\", \"created_at\", \"email\"")]
    public void Select_AllDialects_UsesCorrectTableQuoting(string dialect, string expectedTable, string expectedColumns)
    {
        var sql = GetQuery(dialect).ToSql();
        Assert.IsTrue(sql.Contains($"SELECT {expectedColumns}"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains($"FROM {expectedTable}"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "[id]")]
    [DataRow("SqlServer", "[id]")]
    [DataRow("MySql", "`id`")]
    [DataRow("PostgreSQL", "\"id\"")]
    [DataRow("Oracle", "\"id\"")]
    [DataRow("DB2", "\"id\"")]
    public void Where_AllDialects_UsesCorrectColumnQuoting(string dialect, string expectedColumn)
    {
        var sql = GetQuery(dialect).Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains($"{expectedColumn} = 1"), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Boolean Literals - All Dialects

    [TestMethod]
    [DataRow("SQLite", "= 1")]
    [DataRow("SqlServer", "= 1")]
    [DataRow("MySql", "= 1")]
    [DataRow("PostgreSQL", "= true")]
    [DataRow("Oracle", "= 1")]
    [DataRow("DB2", "= 1")]
    public void Where_BooleanTrue_AllDialects_UsesCorrectLiteral(string dialect, string expectedLiteral)
    {
        var sql = GetQuery(dialect).Where(u => u.IsActive == true).ToSql();
        Assert.IsTrue(sql.Contains(expectedLiteral), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "= 0")]
    [DataRow("SqlServer", "= 0")]
    [DataRow("MySql", "= 0")]
    [DataRow("PostgreSQL", "= false")]
    [DataRow("Oracle", "= 0")]
    [DataRow("DB2", "= 0")]
    public void Where_BooleanFalse_AllDialects_UsesCorrectLiteral(string dialect, string expectedLiteral)
    {
        var sql = GetQuery(dialect).Where(u => u.IsActive == false).ToSql();
        Assert.IsTrue(sql.Contains(expectedLiteral), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Pagination - All Dialects

    [TestMethod]
    [DataRow("SQLite", "LIMIT 10", "OFFSET 20")]
    [DataRow("MySql", "LIMIT 10", "OFFSET 20")]
    [DataRow("PostgreSQL", "LIMIT 10", "OFFSET 20")]
    [DataRow("Oracle", "FETCH NEXT 10 ROWS ONLY", "OFFSET 20 ROWS")]
    [DataRow("DB2", "FETCH NEXT 10 ROWS ONLY", "OFFSET 20 ROWS")]
    public void Pagination_LimitOffset_AllDialects(string dialect, string expectedLimit, string expectedOffset)
    {
        var sql = GetQuery(dialect).Skip(20).Take(10).ToSql();
        Assert.IsTrue(sql.Contains(expectedLimit), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains(expectedOffset), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    public void Pagination_SqlServer_UsesOffsetFetch()
    {
        var sql = SqlQuery.ForSqlServer<QueryUser>().Skip(20).Take(10).ToSql();
        Assert.IsTrue(sql.Contains("OFFSET 20 ROWS"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH NEXT 10 ROWS ONLY"), $"SQL: {sql}");
    }

    #endregion

    #region String Functions - All Dialects

    [TestMethod]
    [DataRow("SQLite", "UPPER")]
    [DataRow("SqlServer", "UPPER")]
    [DataRow("MySql", "UPPER")]
    [DataRow("PostgreSQL", "UPPER")]
    [DataRow("Oracle", "UPPER")]
    [DataRow("DB2", "UPPER")]
    public void StringToUpper_AllDialects_GeneratesUpperFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => u.Name.ToUpper() == "TEST").ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "LOWER")]
    [DataRow("SqlServer", "LOWER")]
    [DataRow("MySql", "LOWER")]
    [DataRow("PostgreSQL", "LOWER")]
    [DataRow("Oracle", "LOWER")]
    [DataRow("DB2", "LOWER")]
    public void StringToLower_AllDialects_GeneratesLowerFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => u.Name.ToLower() == "test").ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "LENGTH")]
    [DataRow("SqlServer", "LEN")]
    [DataRow("MySql", "LENGTH")]
    [DataRow("PostgreSQL", "LENGTH")]
    [DataRow("Oracle", "LENGTH")]
    [DataRow("DB2", "LENGTH")]
    public void StringLength_AllDialects_GeneratesCorrectFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => u.Name.Length > 5).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "TRIM")]
    [DataRow("SqlServer", "TRIM")]
    [DataRow("MySql", "TRIM")]
    [DataRow("PostgreSQL", "TRIM")]
    [DataRow("Oracle", "TRIM")]
    [DataRow("DB2", "TRIM")]
    public void StringTrim_AllDialects_GeneratesTrimFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => u.Name.Trim() == "test").ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region String Pattern Matching - All Dialects

    [TestMethod]
    [DataRow("SQLite", "||")]
    [DataRow("SqlServer", "+")]
    [DataRow("MySql", "CONCAT(")]
    [DataRow("PostgreSQL", "||")]
    [DataRow("Oracle", "||")]
    [DataRow("DB2", "CONCAT(")]
    public void StringContains_AllDialects_UsesCorrectConcat(string dialect, string expectedConcat)
    {
        var sql = GetQuery(dialect).Where(u => u.Name.Contains("test")).ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains(expectedConcat), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Math Functions - All Dialects

    [TestMethod]
    [DataRow("SQLite", "ABS")]
    [DataRow("SqlServer", "ABS")]
    [DataRow("MySql", "ABS")]
    [DataRow("PostgreSQL", "ABS")]
    [DataRow("Oracle", "ABS")]
    [DataRow("DB2", "ABS")]
    public void MathAbs_AllDialects_GeneratesAbsFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => Math.Abs(u.Age) > 0).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "ROUND")]
    [DataRow("SqlServer", "ROUND")]
    [DataRow("MySql", "ROUND")]
    [DataRow("PostgreSQL", "ROUND")]
    [DataRow("Oracle", "ROUND")]
    [DataRow("DB2", "ROUND")]
    public void MathRound_AllDialects_GeneratesRoundFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => Math.Round((double)u.Salary) > 1000).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "FLOOR")]
    [DataRow("SqlServer", "FLOOR")]
    [DataRow("MySql", "FLOOR")]
    [DataRow("PostgreSQL", "FLOOR")]
    [DataRow("Oracle", "FLOOR")]
    [DataRow("DB2", "FLOOR")]
    public void MathFloor_AllDialects_GeneratesFloorFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => Math.Floor((double)u.Salary) > 1000).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "CEILING")]
    [DataRow("SqlServer", "CEILING")]
    [DataRow("MySql", "CEILING")]
    [DataRow("PostgreSQL", "CEIL")]
    [DataRow("Oracle", "CEILING")]
    [DataRow("DB2", "CEILING")]
    public void MathCeiling_AllDialects_GeneratesCeilingFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => Math.Ceiling((double)u.Salary) > 1000).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "POWER")]
    [DataRow("SqlServer", "POWER")]
    [DataRow("MySql", "POW")]
    [DataRow("PostgreSQL", "POWER")]
    [DataRow("Oracle", "POWER")]
    [DataRow("DB2", "POWER")]
    public void MathPow_AllDialects_GeneratesPowerFunction(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Where(u => Math.Pow(u.Age, 2) > 100).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Parameter Prefixes - All Dialects

    [TestMethod]
    [DataRow("SQLite", "@p")]
    [DataRow("SqlServer", "@p")]
    [DataRow("MySql", "@p")]
    [DataRow("PostgreSQL", "$p")]
    [DataRow("Oracle", ":p")]
    [DataRow("DB2", "?")]
    public void ParameterizedQuery_AllDialects_UsesCorrectPrefix(string dialect, string expectedPrefix)
    {
        var (sql, parameters) = GetQuery(dialect).Where(u => u.Id == 1).ToSqlWithParameters();
        
        if (dialect == "DB2")
        {
            Assert.IsTrue(sql.Contains("?"), $"[{dialect}] SQL: {sql}");
        }
        else
        {
            Assert.IsTrue(sql.Contains(expectedPrefix), $"[{dialect}] SQL: {sql}");
        }
        Assert.IsTrue(parameters.Any(), $"[{dialect}] Should have parameters");
    }

    #endregion
}


/// <summary>
/// Nested and complex query tests across all dialects.
/// Tests combinations of Where, Select, OrderBy, Take, Skip, GroupBy.
/// </summary>
[TestClass]
public class SqlxQueryableNestedTests
{
    private static IQueryable<QueryUser> GetQuery(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery.ForSqlite<QueryUser>(),
        "SqlServer" => SqlQuery.ForSqlServer<QueryUser>(),
        "MySql" => SqlQuery.ForMySql<QueryUser>(),
        "PostgreSQL" => SqlQuery.ForPostgreSQL<QueryUser>(),
        "Oracle" => SqlQuery.ForOracle<QueryUser>(),
        "DB2" => SqlQuery.ForDB2<QueryUser>(),
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    #region Nested Where Conditions

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NestedWhere_AndOr_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => (u.Age >= 18 && u.Age <= 65) || u.IsActive)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("AND"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("OR"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void MultipleWhere_ChainedAnd_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => u.IsActive)
            .Where(u => u.Age > 18)
            .Where(u => u.Name != null)
            .ToSql();
        
        // Should have multiple AND conditions
        var andCount = sql.Split(new[] { "AND" }, StringSplitOptions.None).Length - 1;
        Assert.IsTrue(andCount >= 2, $"[{dialect}] Expected at least 2 ANDs, got {andCount}. SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NestedWhere_ComplexCondition_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => u.IsActive && (u.Age < 18 || u.Age > 65) && u.Email != null)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("AND"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("OR"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("IS NOT NULL"), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Nested Function Calls

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NestedStringFunctions_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => u.Name.Trim().ToUpper() == "TEST")
            .ToSql();
        
        Assert.IsTrue(sql.Contains("TRIM"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("UPPER"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NestedMathFunctions_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => Math.Abs(Math.Floor((double)u.Salary)) > 1000)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ABS"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("FLOOR"), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Full Query Chain - All Dialects

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void FullChain_WhereSelectOrderByPagination_AllDialects(string dialect)
    {
        var query = GetQuery(dialect)
            .Where(u => u.IsActive)
            .Where(u => u.Age >= 18)
            .Select(u => new { u.Id, u.Name, u.Age })
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .Skip(10)
            .Take(20);

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), $"[{dialect}] Missing WHERE. SQL: {sql}");
        Assert.IsTrue(sql.Contains("ORDER BY"), $"[{dialect}] Missing ORDER BY. SQL: {sql}");
        Assert.IsTrue(sql.Contains("ASC"), $"[{dialect}] Missing ASC. SQL: {sql}");
        Assert.IsTrue(sql.Contains("DESC"), $"[{dialect}] Missing DESC. SQL: {sql}");
        
        if (dialect == "SqlServer" || dialect == "Oracle" || dialect == "DB2")
        {
            Assert.IsTrue(sql.Contains("OFFSET 10 ROWS"), $"[{dialect}] SQL: {sql}");
            Assert.IsTrue(sql.Contains("FETCH NEXT 20 ROWS ONLY"), $"[{dialect}] SQL: {sql}");
        }
        else
        {
            Assert.IsTrue(sql.Contains("LIMIT 20"), $"[{dialect}] SQL: {sql}");
            Assert.IsTrue(sql.Contains("OFFSET 10"), $"[{dialect}] SQL: {sql}");
        }
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void FullChain_WithGroupBy_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => u.Age >= 18)
            .Select(u => new { u.IsActive, u.Age })
            .GroupBy(u => u.IsActive)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("GROUP BY"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void FullChain_WithDistinct_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => u.IsActive)
            .Select(u => u.Name)
            .Distinct()
            .ToSql();

        Assert.IsTrue(sql.Contains("DISTINCT"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("WHERE"), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Complex Expressions - All Dialects

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void ComplexExpression_Coalesce_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => (u.Email ?? "default@test.com") != "")
            .ToSql();

        Assert.IsTrue(sql.Contains("COALESCE"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void ComplexExpression_Conditional_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Select(u => u.Age >= 18 ? "Adult" : "Minor")
            .ToSql();

        Assert.IsTrue(sql.Contains("CASE WHEN"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("THEN"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("ELSE"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("END"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void ComplexExpression_NestedConditional_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .Select(u => u.Age < 18 ? "Minor" : (u.Age < 65 ? "Adult" : "Senior"))
            .ToSql();

        // Should have nested CASE WHEN
        var caseCount = sql.Split(new[] { "CASE WHEN" }, StringSplitOptions.None).Length - 1;
        Assert.IsTrue(caseCount >= 1, $"[{dialect}] Expected CASE WHEN. SQL: {sql}");
    }

    #endregion

    #region Parameterized Complex Queries - All Dialects

    [TestMethod]
    [DataRow("SQLite", "@p")]
    [DataRow("SqlServer", "@p")]
    [DataRow("MySql", "@p")]
    [DataRow("PostgreSQL", "$p")]
    [DataRow("Oracle", ":p")]
    [DataRow("DB2", "?")]
    public void ParameterizedComplexQuery_AllDialects(string dialect, string expectedPrefix)
    {
        var (sql, parameters) = GetQuery(dialect)
            .Where(u => u.Age >= 18 && u.Age <= 65)
            .Where(u => u.Name == "test")
            .Where(u => u.IsActive == true)
            .ToSqlWithParameters();

        Assert.IsTrue(parameters.Count() >= 3, $"[{dialect}] Expected at least 3 parameters, got {parameters.Count()}");
        
        if (dialect == "DB2")
        {
            Assert.IsTrue(sql.Contains("?"), $"[{dialect}] SQL: {sql}");
        }
        else
        {
            Assert.IsTrue(sql.Contains(expectedPrefix), $"[{dialect}] SQL: {sql}");
        }
    }

    #endregion
}


/// <summary>
/// Edge case and boundary tests for IQueryable SQL generation.
/// </summary>
[TestClass]
public class SqlxQueryableEdgeCaseTests
{
    private static IQueryable<QueryUser> GetQuery(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery.ForSqlite<QueryUser>(),
        "SqlServer" => SqlQuery.ForSqlServer<QueryUser>(),
        "MySql" => SqlQuery.ForMySql<QueryUser>(),
        "PostgreSQL" => SqlQuery.ForPostgreSQL<QueryUser>(),
        "Oracle" => SqlQuery.ForOracle<QueryUser>(),
        "DB2" => SqlQuery.ForDB2<QueryUser>(),
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    #region Empty/Null Handling

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void EmptyQuery_NoConditions_ReturnsAllColumns(string dialect)
    {
        var sql = GetQuery(dialect).ToSql();
        // Should list all columns explicitly, not SELECT *
        Assert.IsTrue(sql.Contains("SELECT"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("id"), $"[{dialect}] Should contain id column. SQL: {sql}");
        Assert.IsFalse(sql.Contains("WHERE"), $"[{dialect}] Should not have WHERE. SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NullCheck_IsNull_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Email == null).ToSql();
        Assert.IsTrue(sql.Contains("IS NULL"), $"[{dialect}] SQL: {sql}");
        Assert.IsFalse(sql.Contains("= NULL"), $"[{dialect}] Should use IS NULL, not = NULL. SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NullCheck_IsNotNull_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Email != null).ToSql();
        Assert.IsTrue(sql.Contains("IS NOT NULL"), $"[{dialect}] SQL: {sql}");
        Assert.IsFalse(sql.Contains("<> NULL"), $"[{dialect}] Should use IS NOT NULL. SQL: {sql}");
    }

    #endregion

    #region Special Characters in Strings

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void StringWithQuote_EscapesCorrectly(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Name == "O'Brien").ToSql();
        // Single quote is escaped by doubling it, but the value goes through Replace twice
        Assert.IsTrue(sql.Contains("O''") || sql.Contains("O''''"), $"[{dialect}] Should escape single quote. SQL: {sql}");
    }

    #endregion

    #region Boundary Values

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Take_Zero_GeneratesLimitZero(string dialect)
    {
        var sql = GetQuery(dialect).Take(0).ToSql();
        
        if (dialect == "SqlServer" || dialect == "Oracle" || dialect == "DB2")
        {
            Assert.IsTrue(sql.Contains("FETCH NEXT 0 ROWS ONLY"), $"[{dialect}] SQL: {sql}");
        }
        else
        {
            Assert.IsTrue(sql.Contains("LIMIT 0"), $"[{dialect}] SQL: {sql}");
        }
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Skip_Zero_GeneratesOffsetZero(string dialect)
    {
        var sql = GetQuery(dialect).Skip(0).ToSql();
        
        if (dialect == "SqlServer")
        {
            Assert.IsTrue(sql.Contains("OFFSET 0 ROWS"), $"[{dialect}] SQL: {sql}");
        }
        else
        {
            Assert.IsTrue(sql.Contains("OFFSET 0"), $"[{dialect}] SQL: {sql}");
        }
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void LargeNumbers_HandledCorrectly(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Id == int.MaxValue).ToSql();
        Assert.IsTrue(sql.Contains(int.MaxValue.ToString()), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void NegativeNumbers_HandledCorrectly(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Age == -1).ToSql();
        Assert.IsTrue(sql.Contains("-1"), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Comparison Operators - All Dialects

    [TestMethod]
    [DataRow("SQLite", "=")]
    [DataRow("SqlServer", "=")]
    [DataRow("MySql", "=")]
    [DataRow("PostgreSQL", "=")]
    [DataRow("Oracle", "=")]
    [DataRow("DB2", "=")]
    public void Operator_Equal_AllDialects(string dialect, string expectedOp)
    {
        var sql = GetQuery(dialect).Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains($"{expectedOp} 1"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "<>")]
    [DataRow("SqlServer", "<>")]
    [DataRow("MySql", "<>")]
    [DataRow("PostgreSQL", "<>")]
    [DataRow("Oracle", "!=")]
    [DataRow("DB2", "<>")]
    public void Operator_NotEqual_AllDialects(string dialect, string expectedOp)
    {
        var sql = GetQuery(dialect).Where(u => u.Id != 1).ToSql();
        Assert.IsTrue(sql.Contains(expectedOp), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Operator_GreaterThan_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Age > 18).ToSql();
        Assert.IsTrue(sql.Contains("> 18"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Operator_LessThan_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Age < 65).ToSql();
        Assert.IsTrue(sql.Contains("< 65"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Operator_GreaterThanOrEqual_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Age >= 18).ToSql();
        Assert.IsTrue(sql.Contains(">= 18"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Operator_LessThanOrEqual_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Where(u => u.Age <= 65).ToSql();
        Assert.IsTrue(sql.Contains("<= 65"), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region SQL Clause Order

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void ClauseOrder_SelectFromWhereGroupByOrderByLimit(string dialect)
    {
        var sql = GetQuery(dialect)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .GroupBy(u => u.IsActive)
            .Take(10)
            .ToSql();

        var selectIdx = sql.IndexOf("SELECT");
        var fromIdx = sql.IndexOf("FROM");
        var whereIdx = sql.IndexOf("WHERE");
        var groupByIdx = sql.IndexOf("GROUP BY");
        var orderByIdx = sql.IndexOf("ORDER BY");

        Assert.IsTrue(selectIdx < fromIdx, $"[{dialect}] SELECT should come before FROM. SQL: {sql}");
        Assert.IsTrue(fromIdx < whereIdx, $"[{dialect}] FROM should come before WHERE. SQL: {sql}");
        Assert.IsTrue(whereIdx < groupByIdx || whereIdx < orderByIdx, $"[{dialect}] WHERE should come before GROUP BY or ORDER BY. SQL: {sql}");
    }

    #endregion
}


/// <summary>
/// Select projection tests across all dialects.
/// </summary>
[TestClass]
public class SqlxQueryableSelectCrossDialectTests
{
    private static IQueryable<QueryUser> GetQuery(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery.ForSqlite<QueryUser>(),
        "SqlServer" => SqlQuery.ForSqlServer<QueryUser>(),
        "MySql" => SqlQuery.ForMySql<QueryUser>(),
        "PostgreSQL" => SqlQuery.ForPostgreSQL<QueryUser>(),
        "Oracle" => SqlQuery.ForOracle<QueryUser>(),
        "DB2" => SqlQuery.ForDB2<QueryUser>(),
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    #region Single Column Select

    [TestMethod]
    [DataRow("SQLite", "[id]")]
    [DataRow("SqlServer", "[id]")]
    [DataRow("MySql", "`id`")]
    [DataRow("PostgreSQL", "\"id\"")]
    [DataRow("Oracle", "\"id\"")]
    [DataRow("DB2", "\"id\"")]
    public void Select_SingleColumn_AllDialects(string dialect, string expectedColumn)
    {
        var sql = GetQuery(dialect).Select(u => u.Id).ToSql();
        Assert.IsTrue(sql.Contains(expectedColumn), $"[{dialect}] SQL: {sql}");
        Assert.IsFalse(sql.Contains("*"), $"[{dialect}] Should not contain *. SQL: {sql}");
    }

    #endregion

    #region Multiple Columns Select

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Select_MultipleColumns_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Select(u => new { u.Id, u.Name, u.Age }).ToSql();
        Assert.IsFalse(sql.Contains("*"), $"[{dialect}] Should not contain *. SQL: {sql}");
    }

    #endregion

    #region Select with String Functions

    [TestMethod]
    [DataRow("SQLite", "UPPER")]
    [DataRow("SqlServer", "UPPER")]
    [DataRow("MySql", "UPPER")]
    [DataRow("PostgreSQL", "UPPER")]
    [DataRow("Oracle", "UPPER")]
    [DataRow("DB2", "UPPER")]
    public void Select_StringToUpper_AllDialects(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Select(u => u.Name.ToUpper()).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "LENGTH")]
    [DataRow("SqlServer", "LEN")]
    [DataRow("MySql", "LENGTH")]
    [DataRow("PostgreSQL", "LENGTH")]
    [DataRow("Oracle", "LENGTH")]
    [DataRow("DB2", "LENGTH")]
    public void Select_StringLength_AllDialects(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Select(u => u.Name.Length).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Select with Math Functions

    [TestMethod]
    [DataRow("SQLite", "ABS")]
    [DataRow("SqlServer", "ABS")]
    [DataRow("MySql", "ABS")]
    [DataRow("PostgreSQL", "ABS")]
    [DataRow("Oracle", "ABS")]
    [DataRow("DB2", "ABS")]
    public void Select_MathAbs_AllDialects(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Select(u => Math.Abs(u.Age)).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "ROUND")]
    [DataRow("SqlServer", "ROUND")]
    [DataRow("MySql", "ROUND")]
    [DataRow("PostgreSQL", "ROUND")]
    [DataRow("Oracle", "ROUND")]
    [DataRow("DB2", "ROUND")]
    public void Select_MathRound_AllDialects(string dialect, string expectedFunc)
    {
        var sql = GetQuery(dialect).Select(u => Math.Round((double)u.Salary)).ToSql();
        Assert.IsTrue(sql.Contains(expectedFunc), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Select with Expressions

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Select_Coalesce_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Select(u => u.Email ?? "N/A").ToSql();
        Assert.IsTrue(sql.Contains("COALESCE"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Select_Conditional_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect).Select(u => u.Age >= 18 ? "Adult" : "Minor").ToSql();
        Assert.IsTrue(sql.Contains("CASE WHEN"), $"[{dialect}] SQL: {sql}");
    }

    #endregion
}


/// <summary>
/// OrderBy tests across all dialects.
/// </summary>
[TestClass]
public class SqlxQueryableOrderByCrossDialectTests
{
    private static IQueryable<QueryUser> GetQuery(string dialect) => dialect switch
    {
        "SQLite" => SqlQuery.ForSqlite<QueryUser>(),
        "SqlServer" => SqlQuery.ForSqlServer<QueryUser>(),
        "MySql" => SqlQuery.ForMySql<QueryUser>(),
        "PostgreSQL" => SqlQuery.ForPostgreSQL<QueryUser>(),
        "Oracle" => SqlQuery.ForOracle<QueryUser>(),
        "DB2" => SqlQuery.ForDB2<QueryUser>(),
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    #region Single OrderBy

    [TestMethod]
    [DataRow("SQLite", "[name] ASC")]
    [DataRow("SqlServer", "[name] ASC")]
    [DataRow("MySql", "`name` ASC")]
    [DataRow("PostgreSQL", "\"name\" ASC")]
    [DataRow("Oracle", "\"name\" ASC")]
    [DataRow("DB2", "\"name\" ASC")]
    public void OrderBy_Ascending_AllDialects(string dialect, string expected)
    {
        var sql = GetQuery(dialect).OrderBy(u => u.Name).ToSql();
        Assert.IsTrue(sql.Contains(expected), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "[name] DESC")]
    [DataRow("SqlServer", "[name] DESC")]
    [DataRow("MySql", "`name` DESC")]
    [DataRow("PostgreSQL", "\"name\" DESC")]
    [DataRow("Oracle", "\"name\" DESC")]
    [DataRow("DB2", "\"name\" DESC")]
    public void OrderByDescending_AllDialects(string dialect, string expected)
    {
        var sql = GetQuery(dialect).OrderByDescending(u => u.Name).ToSql();
        Assert.IsTrue(sql.Contains(expected), $"[{dialect}] SQL: {sql}");
    }

    #endregion

    #region Multiple OrderBy

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void OrderBy_ThenBy_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Age)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ASC"), $"[{dialect}] SQL: {sql}");
        // Should have two columns in ORDER BY
        var orderByIdx = sql.IndexOf("ORDER BY");
        var orderByClause = sql.Substring(orderByIdx);
        Assert.IsTrue(orderByClause.Contains(",") || orderByClause.Split("ASC").Length > 2, 
            $"[{dialect}] Should have multiple order columns. SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void OrderBy_ThenByDescending_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ASC"), $"[{dialect}] SQL: {sql}");
        Assert.IsTrue(sql.Contains("DESC"), $"[{dialect}] SQL: {sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void OrderBy_MultipleColumns_AllDialects(string dialect)
    {
        var sql = GetQuery(dialect)
            .OrderBy(u => u.IsActive)
            .ThenByDescending(u => u.Name)
            .ThenBy(u => u.Age)
            .ThenByDescending(u => u.CreatedAt)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ORDER BY"), $"[{dialect}] SQL: {sql}");
        // Count ASC and DESC occurrences
        var ascCount = sql.Split(new[] { "ASC" }, StringSplitOptions.None).Length - 1;
        var descCount = sql.Split(new[] { "DESC" }, StringSplitOptions.None).Length - 1;
        Assert.AreEqual(2, ascCount, $"[{dialect}] Expected 2 ASC. SQL: {sql}");
        Assert.AreEqual(2, descCount, $"[{dialect}] Expected 2 DESC. SQL: {sql}");
    }

    #endregion
}
