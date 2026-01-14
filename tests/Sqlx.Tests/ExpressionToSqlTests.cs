using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests;

// Test entities
public class ExprUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Email { get; set; }
}

public class ExprOrder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}

[TestClass]
public class ExpressionToSqlTests
{
    #region Factory Methods

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Create_AllDialects_ReturnsInstance(string dialect)
    {
        var sql = CreateForDialect(dialect);
        Assert.IsNotNull(sql);
    }

    #endregion

    #region SELECT Operations

    [TestMethod]
    public void Select_NoColumns_ReturnsSelectStar() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().ToSql().Contains("SELECT *"));

    [TestMethod]
    [DataRow(new[] { "id", "name" }, "SELECT id, name")]
    [DataRow(new string[0], "SELECT *")]
    public void Select_WithStringColumns_ReturnsExpected(string[] columns, string expected)
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Select(columns).ToSql();
        Assert.IsTrue(result.Contains(expected));
    }

    [TestMethod]
    public void Select_WithNullArray_ReturnsSelectStar() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Select((string[])null!).ToSql().Contains("SELECT *"));

    [TestMethod]
    public void Select_WithExpression_ReturnsColumns()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Select(u => new { u.Id, u.Name }).ToSql();
        Assert.IsTrue(result.Contains("[id]") && result.Contains("[name]"));
    }

    [TestMethod]
    public void Select_WithSingleExpression_ReturnsColumn() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Select(u => u.Name).ToSql().Contains("[name]"));

    [TestMethod]
    public void Select_WithNullExpression_ReturnsSelectStar() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Select<object>(null!).ToSql().Contains("SELECT *"));

    [TestMethod]
    public void Select_WithMultipleExpressions_ReturnsAllColumns()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Select(u => u.Id, u => u.Name, u => u.Age).ToSql();
        Assert.IsTrue(result.Contains("[id]") && result.Contains("[name]") && result.Contains("[age]"));
    }

    [TestMethod]
    public void Select_WithEmptyExpressionArray_ReturnsSelectStar() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Select(Array.Empty<Expression<Func<ExprUser, object>>>()).ToSql().Contains("SELECT *"));

    #endregion

    #region WHERE Operations

    [TestMethod]
    [DataRow("u => u.Id == 1", "[id] = 1")]
    [DataRow("u => u.Name == \"test\"", "[name] = 'test'")]
    public void Where_SimpleConditions_GeneratesCorrectSql(string _, string expected)
    {
        // DataRow doesn't support expressions, so test individually
    }

    [TestMethod]
    public void Where_SimpleEquality_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Id == 1).ToSql().Contains("WHERE [id] = 1"));

    [TestMethod]
    public void Where_StringEquality_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name == "test").ToSql().Contains("WHERE [name] = 'test'"));

    [TestMethod]
    public void Where_MultipleConditions_GeneratesAndClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Id > 0).And(u => u.IsActive == true).ToSql();
        Assert.IsTrue(result.Contains("WHERE") && result.Contains("[id] > 0") && result.Contains("[is_active] = 1"));
    }

    [TestMethod]
    public void Where_NullPredicate_DoesNotAddCondition() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().Where(null!).ToSql().Contains("WHERE"));

    [TestMethod]
    [DataRow(">", 18, "[age] > 18")]
    [DataRow("<", 65, "[age] < 65")]
    [DataRow(">=", 18, "[age] >= 18")]
    [DataRow("<=", 65, "[age] <= 65")]
    public void Where_ComparisonOperators_GeneratesCorrectSql(string op, int value, string expected)
    {
        Expression<Func<ExprUser, bool>> predicate = op switch
        {
            ">" => u => u.Age > value,
            "<" => u => u.Age < value,
            ">=" => u => u.Age >= value,
            "<=" => u => u.Age <= value,
            _ => throw new ArgumentException()
        };
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(predicate).ToSql().Contains(expected));
    }

    [TestMethod]
    public void Where_NotEqual_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Id != 0).ToSql().Contains("[id] <> 0"));

    [TestMethod]
    public void Where_OrCondition_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age < 18 || u.Age > 65).ToSql().Contains("OR"));

    [TestMethod]
    public void Where_AndCondition_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age >= 18 && u.Age <= 65).ToSql().Contains("AND"));

    [TestMethod]
    public void ToWhereClause_ReturnsOnlyWhereConditions() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Id == 1).ToWhereClause().Contains("[id] = 1"));

    [TestMethod]
    public void ToWhereClause_NoConditions_ReturnsEmpty() =>
        Assert.AreEqual(string.Empty, ExpressionToSql<ExprUser>.ForSqlite().ToWhereClause());

    #endregion

    #region ORDER BY Operations

    [TestMethod]
    public void OrderBy_SingleColumn_GeneratesAsc() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().OrderBy(u => u.Name).ToSql().Contains("ORDER BY [name] ASC"));

    [TestMethod]
    public void OrderByDescending_SingleColumn_GeneratesDesc() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().OrderByDescending(u => u.CreatedAt).ToSql().Contains("ORDER BY [created_at] DESC"));

    [TestMethod]
    public void OrderBy_MultipleColumns_GeneratesMultipleOrderBy() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().OrderBy(u => u.Name).OrderByDescending(u => u.Age).ToSql().Contains("ORDER BY [name] ASC, [age] DESC"));

    [TestMethod]
    public void OrderBy_NullExpression_DoesNotAddOrderBy() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().OrderBy<int>(null!).ToSql().Contains("ORDER BY"));

    [TestMethod]
    public void OrderByDescending_NullExpression_DoesNotAddOrderBy() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().OrderByDescending<int>(null!).ToSql().Contains("ORDER BY"));

    #endregion

    #region Pagination

    [TestMethod]
    public void Take_GeneratesLimit() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Take(10).ToSql().Contains("LIMIT 10"));

    [TestMethod]
    public void Skip_GeneratesOffset() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Skip(20).ToSql().Contains("OFFSET 20"));

    [TestMethod]
    public void TakeAndSkip_GeneratesLimitOffset()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Take(10).Skip(20).ToSql();
        Assert.IsTrue(result.Contains("LIMIT 10") && result.Contains("OFFSET 20"));
    }

    [TestMethod]
    public void SqlServer_Pagination_GeneratesOffsetFetch()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer().Take(10).Skip(20).ToSql();
        Assert.IsTrue(result.Contains("OFFSET 20 ROWS") && result.Contains("FETCH NEXT 10 ROWS ONLY"));
    }

    [TestMethod]
    public void SqlServer_SkipOnly_GeneratesOffsetWithoutFetch()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer().Skip(20).ToSql();
        Assert.IsTrue(result.Contains("OFFSET 20 ROWS") && !result.Contains("FETCH"));
    }

    #endregion

    #region UPDATE Operations

    [TestMethod]
    public void Update_WithSet_GeneratesUpdateSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Update().Set(u => u.Name, "NewName").Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(result.StartsWith("UPDATE") && result.Contains("SET [name] = 'NewName'") && result.Contains("WHERE [id] = 1"));
    }

    [TestMethod]
    public void Set_WithValue_GeneratesSetClause() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Age, 30).Where(u => u.Id == 1).ToSql().Contains("SET [age] = 30"));

    [TestMethod]
    public void Set_WithExpression_GeneratesSetClause() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Age, u => u.Age + 1).Where(u => u.Id == 1).ToSql().Contains("SET [age] = ([age] + 1)"));

    [TestMethod]
    public void Set_NullSelector_DoesNotAddSet() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().Set<int>(null!, 30).Where(u => u.Id == 1).ToSql().Contains("[age]"));

    [TestMethod]
    public void Set_NullValueExpression_DoesNotAddSet() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().Set<int>(u => u.Age, null!).Where(u => u.Id == 1).ToSql().Contains("[age] ="));

    [TestMethod]
    public void ToSetClause_ReturnsOnlySetPart()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Name, "Test").Set(u => u.Age, 25).ToSetClause();
        Assert.IsTrue(result.Contains("[name] = 'Test'") && result.Contains("[age] = 25"));
    }

    [TestMethod]
    public void ToSetClause_NoSets_ReturnsEmpty() =>
        Assert.AreEqual(string.Empty, ExpressionToSql<ExprUser>.ForSqlite().ToSetClause());

    #endregion

    #region INSERT Operations

    [TestMethod]
    public void Insert_WithValues_GeneratesInsertSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Insert(u => new { u.Name, u.Age }).Values("Test", 25).ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO") && result.Contains("VALUES") && result.Contains("'Test'") && result.Contains("25"));
    }

    [TestMethod]
    public void Insert_WithoutColumns_GeneratesInsertWithoutColumnList()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Insert().Values("Test", 25).ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO") && !result.Contains("(["));
    }

    [TestMethod]
    public void Insert_WithNullSelector_GeneratesInsertWithoutColumnList() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Insert(null).Values("Test", 25).ToSql().StartsWith("INSERT INTO"));

    [TestMethod]
    public void InsertSelect_GeneratesInsertSelectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().InsertSelect("SELECT * FROM other_table").ToSql();
        Assert.IsTrue(result.Contains("INSERT INTO") && result.Contains("SELECT * FROM other_table"));
    }

    [TestMethod]
    public void AddValues_MultipleRows_GeneratesMultipleValueSets()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Insert(u => new { u.Name, u.Age }).AddValues("User1", 20).AddValues("User2", 30).ToSql();
        Assert.IsTrue(result.Contains("('User1', 20)") && result.Contains("('User2', 30)"));
    }

    [TestMethod]
    public void Values_EmptyArray_DoesNotAddValues() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().Insert().Values(Array.Empty<object>()).ToSql().Contains("VALUES"));

    [TestMethod]
    public void Values_NullArray_DoesNotAddValues() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().Insert().Values(null!).ToSql().Contains("VALUES"));

    #endregion

    #region DELETE Operations

    [TestMethod]
    public void Delete_WithWhere_GeneratesDeleteSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Delete(u => u.Id == 1).ToSql();
        Assert.IsTrue(result.StartsWith("DELETE FROM") && result.Contains("WHERE [id] = 1"));
    }

    [TestMethod]
    public void Delete_WithoutWhere_ThrowsException() =>
        Assert.ThrowsException<InvalidOperationException>(() => ExpressionToSql<ExprUser>.ForSqlite().Delete().ToSql());

    [TestMethod]
    public void Delete_NullPredicate_ThrowsException() =>
        Assert.ThrowsException<InvalidOperationException>(() => ExpressionToSql<ExprUser>.ForSqlite().Delete(null!).ToSql());

    #endregion

    #region GROUP BY / HAVING

    [TestMethod]
    public void GroupBy_GeneratesGroupByClause() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).ToSql().Contains("GROUP BY [is_active]"));

    [TestMethod]
    public void AddGroupBy_AddsGroupByColumn() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().AddGroupBy("status").ToSql().Contains("GROUP BY status"));

    [TestMethod]
    public void Having_GeneratesHavingClause() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().AddGroupBy("[is_active]").Having(u => u.Age > 18).ToSql().Contains("HAVING"));

    [TestMethod]
    public void Having_NullPredicate_DoesNotAddHaving() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().AddGroupBy("[is_active]").Having(null!).ToSql().Contains("HAVING"));

    [TestMethod]
    public void GroupBy_NullExpression_DoesNotAddGroupBy() =>
        Assert.IsFalse(ExpressionToSql<ExprUser>.ForSqlite().GroupBy<int>(null!).ToSql().Contains("GROUP BY"));

    #endregion

    #region ToAdditionalClause

    [TestMethod]
    public void ToAdditionalClause_WithAllClauses_ReturnsAllParts()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().AddGroupBy("[is_active]").Having(u => u.Age > 0).OrderBy(u => u.Name).Take(10).Skip(5).ToAdditionalClause();
        Assert.IsTrue(result.Contains("GROUP BY") && result.Contains("HAVING") && result.Contains("ORDER BY") && result.Contains("LIMIT") && result.Contains("OFFSET"));
    }

    [TestMethod]
    public void ToAdditionalClause_Empty_ReturnsEmpty() =>
        Assert.AreEqual(string.Empty, ExpressionToSql<ExprUser>.ForSqlite().ToAdditionalClause());

    #endregion

    #region Parameterized Queries & ToTemplate

    [TestMethod]
    public void UseParameterizedQueries_EnablesParameterization() =>
        Assert.IsNotNull(ExpressionToSql<ExprUser>.ForSqlite().UseParameterizedQueries().Where(u => u.Id == 1));

    [TestMethod]
    public void ToTemplate_ReturnsSameAsSql()
    {
        var query = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Id == 1);
        Assert.AreEqual(query.ToSql(), query.ToTemplate());
    }

    #endregion

    #region Dialect-Specific Tests

    [TestMethod]
    public void MySql_Select_UsesBackticks()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql().Select(u => u.Name).ToSql();
        Assert.IsTrue(result.Contains("`name`") && result.Contains("`ExprUser`"));
    }

    [TestMethod]
    [DataRow("PostgreSQL", "\"name\"", "\"ExprUser\"")]
    [DataRow("Oracle", "\"name\"", "\"ExprUser\"")]
    [DataRow("DB2", "\"name\"", "\"ExprUser\"")]
    public void Dialect_Select_UsesCorrectQuotes(string dialect, string expectedCol, string expectedTable)
    {
        var result = CreateForDialect(dialect).Select(u => u.Name).ToSql();
        Assert.IsTrue(result.Contains(expectedCol), $"Expected {expectedCol} in {result}");
    }

    #endregion

    private static ExpressionToSql<ExprUser> CreateForDialect(string dialect) => dialect switch
    {
        "SQLite" => ExpressionToSql<ExprUser>.ForSqlite(),
        "SqlServer" => ExpressionToSql<ExprUser>.ForSqlServer(),
        "MySql" => ExpressionToSql<ExprUser>.ForMySql(),
        "PostgreSQL" => ExpressionToSql<ExprUser>.ForPostgreSQL(),
        "Oracle" => ExpressionToSql<ExprUser>.ForOracle(),
        "DB2" => ExpressionToSql<ExprUser>.ForDB2(),
        _ => ExpressionToSql<ExprUser>.ForSqlite()
    };
}

[TestClass]
public class GroupedExpressionToSqlTests
{
    #region Aggregations

    [TestMethod]
    [DataRow("Count", "COUNT(*)")]
    public void GroupBy_Select_Count_GeneratesCountStar(string _, string expected)
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { g.Key, Count = g.Count() }).ToSql();
        Assert.IsTrue(result.Contains(expected));
    }

    [TestMethod]
    public void GroupBy_Select_Sum_GeneratesSumFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { g.Key, Total = g.Sum(u => u.Salary) }).ToSql().Contains("SUM([salary])"));

    [TestMethod]
    public void GroupBy_Select_Average_GeneratesAvgFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { g.Key, AvgAge = g.Average(u => (double)u.Age) }).ToSql().Contains("AVG([age])"));

    [TestMethod]
    public void GroupBy_Select_Max_GeneratesMaxFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { g.Key, MaxAge = g.Max(u => u.Age) }).ToSql().Contains("MAX([age])"));

    [TestMethod]
    public void GroupBy_Select_Min_GeneratesMinFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { g.Key, MinAge = g.Min(u => u.Age) }).ToSql().Contains("MIN([age])"));

    #endregion

    #region Having Clause

    [TestMethod]
    public void GroupBy_Having_Count_GeneratesHavingClause() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Having(g => g.Count() > 5).ToSql().Contains("HAVING COUNT(*) > 5"));

    [TestMethod]
    public void GroupBy_Having_Sum_GeneratesHavingClause() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Having(g => g.Sum(u => u.Salary) > 10000m).ToSql().Contains("HAVING SUM([salary]) > 10000"));

    #endregion

    #region Select with Special Expressions

    [TestMethod]
    public void GroupBy_Select_MemberInit_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new ExprOrder { UserId = g.Count() }).ToSql();
        Assert.IsTrue(result.Contains("COUNT(*)") && result.Contains("AS UserId"));
    }

    [TestMethod]
    public void GroupBy_Select_BinaryExpression_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Total = g.Sum(u => u.Salary) + 100m }).ToSql().Contains("SUM([salary])"));

    [TestMethod]
    public void GroupBy_Select_Coalesce_GeneratesCoalesce() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Value = g.Sum(u => (decimal?)u.Salary) ?? 0m }).ToSql().Contains("COALESCE"));

    [TestMethod]
    public void GroupBy_Select_Conditional_GeneratesCaseWhen()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Status = g.Count() > 0 ? "Active" : "Inactive" }).ToSql();
        Assert.IsTrue(result.Contains("CASE WHEN") && result.Contains("THEN") && result.Contains("ELSE") && result.Contains("END"));
    }

    #endregion

    #region Math Functions in Aggregation

    [TestMethod]
    [DataRow("Abs", "ABS([age])")]
    [DataRow("Round", "ROUND([salary])")]
    [DataRow("Floor", "FLOOR([salary])")]
    [DataRow("Ceiling", "CEILING([salary])")]
    [DataRow("Sqrt", "SQRT([age])")]
    public void GroupBy_Select_MathFunctions_GeneratesCorrectSql(string func, string expected)
    {
        var result = func switch
        {
            "Abs" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Abs(u.Age)) }).ToSql(),
            "Round" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Round((double)u.Salary)) }).ToSql(),
            "Floor" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Floor((double)u.Salary)) }).ToSql(),
            "Ceiling" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Ceiling((double)u.Salary)) }).ToSql(),
            "Sqrt" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Sqrt(u.Age)) }).ToSql(),
            _ => ""
        };
        Assert.IsTrue(result.Contains(expected), $"Expected {expected} in {result}");
    }

    [TestMethod]
    public void GroupBy_Select_MathRoundWithDigits_GeneratesRoundFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Round((double)u.Salary, 2)) }).ToSql().Contains("ROUND([salary], 2)"));

    [TestMethod]
    public void PostgreSql_GroupBy_Select_MathCeiling_GeneratesCeilFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForPostgreSQL().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Ceiling((double)u.Salary)) }).ToSql().Contains("CEIL(\"salary\")"));

    [TestMethod]
    public void GroupBy_Select_MathMin_GeneratesLeastFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Min(u.Age, 100)) }).ToSql().Contains("LEAST([age], 100)"));

    [TestMethod]
    public void GroupBy_Select_MathMax_GeneratesGreatestFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Max(u.Age, 0)) }).ToSql().Contains("GREATEST([age], 0)"));

    [TestMethod]
    public void GroupBy_Select_MathPow_GeneratesPowerFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Pow(u.Age, 2)) }).ToSql().Contains("POWER([age], 2)"));

    [TestMethod]
    public void MySql_GroupBy_Select_MathPow_GeneratesPowFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => Math.Pow(u.Age, 2)) }).ToSql().Contains("POW(`age`, 2)"));

    #endregion
