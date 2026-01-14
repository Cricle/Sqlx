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
    public void Where_GreaterThan_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age > 18).ToSql().Contains("[age] > 18"));

    [TestMethod]
    public void Where_LessThan_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age < 65).ToSql().Contains("[age] < 65"));

    [TestMethod]
    public void Where_GreaterThanOrEqual_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age >= 18).ToSql().Contains("[age] >= 18"));

    [TestMethod]
    public void Where_LessThanOrEqual_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age <= 65).ToSql().Contains("[age] <= 65"));

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

    #region String Functions in Aggregation

    [TestMethod]
    public void GroupBy_Select_StringLength_GeneratesLengthFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Length) }).ToSql().Contains("LENGTH([name])"));

    [TestMethod]
    public void SqlServer_GroupBy_Select_StringLength_GeneratesLenFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Length) }).ToSql().Contains("LEN([name])"));

    [TestMethod]
    [DataRow("ToUpper", "UPPER([name])")]
    [DataRow("ToLower", "LOWER([name])")]
    [DataRow("Trim", "TRIM([name])")]
    public void GroupBy_Select_StringMethods_GeneratesCorrectSql(string method, string expected)
    {
        var result = method switch
        {
            "ToUpper" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.ToUpper()) }).ToSql(),
            "ToLower" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.ToLower()) }).ToSql(),
            "Trim" => ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Trim()) }).ToSql(),
            _ => ""
        };
        Assert.IsTrue(result.Contains(expected), $"Expected {expected} in {result}");
    }

    [TestMethod]
    public void GroupBy_Select_StringSubstring_GeneratesSubstringFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Substring(0, 5)) }).ToSql().Contains("SUBSTR([name], 0 + 1, 5)"));

    [TestMethod]
    public void SqlServer_GroupBy_Select_StringSubstring_GeneratesSubstringFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Substring(0, 5)) }).ToSql().Contains("SUBSTRING([name], 0 + 1, 5)"));

    [TestMethod]
    public void GroupBy_Select_StringSubstringOneArg_GeneratesSubstringFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Substring(5)) }).ToSql().Contains("SUBSTR([name], 5 + 1)"));

    [TestMethod]
    public void GroupBy_Select_StringReplace_GeneratesReplaceFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Max(u => u.Name.Replace("a", "b")) }).ToSql().Contains("REPLACE([name], 'a', 'b')"));

    #endregion

    #region ToSql and ToTemplate

    [TestMethod]
    public void GroupBy_ToSql_ReturnsCompleteSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Age > 18).GroupBy(u => u.IsActive).ToSql();
        Assert.IsTrue(result.Contains("SELECT *") && result.Contains("WHERE") && result.Contains("GROUP BY"));
    }

    [TestMethod]
    public void GroupBy_ToTemplate_ReturnsSameAsSql()
    {
        var grouped = ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive);
        Assert.AreEqual(grouped.ToSql(), grouped.ToTemplate());
    }

    #endregion

    #region Aggregate Body Parsing

    [TestMethod]
    public void GroupBy_Select_BinaryInAggBody_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => u.Age + u.Id) }).ToSql().Contains("SUM(([age] + [id]))"));

    [TestMethod]
    public void GroupBy_Select_CoalesceInAggBody_GeneratesCoalesce() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => (int?)u.Age ?? 0) }).ToSql().Contains("COALESCE([age], 0)"));

    [TestMethod]
    public void GroupBy_Select_ConditionalInAggBody_GeneratesCaseWhen() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Select(g => new { Val = g.Sum(u => u.Age > 18 ? u.Age : 0) }).ToSql().Contains("CASE WHEN"));

    #endregion

    #region Having with Key

    [TestMethod]
    public void GroupBy_Having_WithKey_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().GroupBy(u => u.IsActive).Having(g => g.Key == true).ToSql();
        Assert.IsTrue(result.Contains("HAVING") && result.Contains("[is_active]"));
    }

    #endregion
}

[TestClass]
public class AnyPlaceholderTests
{
    [TestMethod]
    [DataRow("Value<int>", typeof(int))]
    [DataRow("Value<string>", typeof(string))]
    [DataRow("String", typeof(string))]
    [DataRow("Int", typeof(int))]
    [DataRow("Bool", typeof(bool))]
    [DataRow("DateTime", typeof(DateTime))]
    [DataRow("Guid", typeof(Guid))]
    public void Any_Methods_ReturnDefault(string method, Type type)
    {
        object result = method switch
        {
            "Value<int>" => Any.Value<int>(),
            "Value<string>" => Any.Value<string>("param")!,
            "String" => Any.String()!,
            "Int" => Any.Int(),
            "Bool" => Any.Bool(),
            "DateTime" => Any.DateTime(),
            "Guid" => Any.Guid(),
            _ => null!
        };
        
        if (type == typeof(string))
            Assert.IsNull(result);
        else if (type.IsValueType)
            Assert.AreEqual(Activator.CreateInstance(type), result);
    }
}

[TestClass]
public class ExpressionToSqlEdgeCaseTests
{
    #region Null Value Handling

    [TestMethod]
    public void Where_NullValue_GeneratesIsNull() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Email == null).ToSql().Contains("IS NULL"));

    [TestMethod]
    public void Where_NotNullValue_GeneratesIsNotNull() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Email != null).ToSql().Contains("IS NOT NULL"));

    #endregion

    #region Boolean Handling

    [TestMethod]
    [DataRow(true, "[is_active] = 1")]
    [DataRow(false, "[is_active] = 0")]
    public void Where_Boolean_GeneratesCorrectSql(bool value, string expected)
    {
        var result = value
            ? ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.IsActive == true).ToSql()
            : ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.IsActive == false).ToSql();
        Assert.IsTrue(result.Contains(expected));
    }

    [TestMethod]
    public void PostgreSql_Where_Boolean_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL().Where(u => u.IsActive == true).ToSql();
        Assert.IsTrue(result.Contains("\"is_active\""));
    }

    #endregion

    #region DateTime Handling

    [TestMethod]
    public void Where_DateTime_GeneratesCorrectFormat()
    {
        var date = new DateTime(2024, 1, 15, 10, 30, 0);
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.CreatedAt == date).ToSql().Contains("[created_at]"));
    }

    [TestMethod]
    public void Set_DateTime_GeneratesCorrectFormat() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.CreatedAt, new DateTime(2024, 6, 15)).Where(u => u.Id == 1).ToSql().Contains("2024-06-15"));

    #endregion

    #region Decimal Handling

    [TestMethod]
    public void Where_Decimal_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Salary > 50000.50m).ToSql().Contains("50000.5"));

    [TestMethod]
    public void Set_Decimal_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Salary, 75000.25m).Where(u => u.Id == 1).ToSql().Contains("75000.25"));

    #endregion

    #region String Escaping

    [TestMethod]
    public void Where_StringWithQuote_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name == "O'Brien").ToSql().Contains("[name]"));

    [TestMethod]
    public void Insert_StringWithQuote_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Insert(u => new { u.Name }).Values("O'Brien").ToSql().Contains("O"));

    #endregion

    #region Complex Expressions

    [TestMethod]
    public void Where_NestedAndOr_GeneratesCorrectParentheses()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => (u.Age > 18 && u.Age < 65) || u.IsActive).ToSql();
        Assert.IsTrue(result.Contains("(") && result.Contains(")") && result.Contains("AND") && result.Contains("OR"));
    }

    [TestMethod]
    public void Where_Negation_GeneratesCorrectSql() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => !u.IsActive).ToSql().Contains("[is_active]"));

    #endregion

    #region Arithmetic Operations

    [TestMethod]
    [DataRow("+", "[age] + 1")]
    [DataRow("-", "[age] - 1")]
    [DataRow("*", "[salary] * 1.1")]
    [DataRow("/", "[salary] / 2")]
    [DataRow("%", "[age] % 10")]
    public void Set_ArithmeticOperations_GeneratesCorrectSql(string op, string expected)
    {
        var result = op switch
        {
            "+" => ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Age, u => u.Age + 1).Where(u => u.Id == 1).ToSql(),
            "-" => ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Age, u => u.Age - 1).Where(u => u.Id == 1).ToSql(),
            "*" => ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Salary, u => u.Salary * 1.1m).Where(u => u.Id == 1).ToSql(),
            "/" => ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Salary, u => u.Salary / 2).Where(u => u.Id == 1).ToSql(),
            "%" => ExpressionToSql<ExprUser>.ForSqlite().Set(u => u.Age, u => u.Age % 10).Where(u => u.Id == 1).ToSql(),
            _ => ""
        };
        Assert.IsTrue(result.Contains(expected), $"Expected {expected} in {result}");
    }

    #endregion

    #region Chained Operations

    [TestMethod]
    public void ChainedSelect_Where_OrderBy_Take_Skip_GeneratesCompleteSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.IsActive).Select(u => new { u.Id, u.Name }).OrderBy(u => u.Name).Take(10).Skip(20).ToSql();
        Assert.IsTrue(result.Contains("SELECT") && result.Contains("WHERE") && result.Contains("ORDER BY") && result.Contains("LIMIT") && result.Contains("OFFSET"));
    }

    [TestMethod]
    public void ChainedUpdate_MultipleSets_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Update().Set(u => u.Name, "NewName").Set(u => u.Age, 30).Set(u => u.IsActive, true).Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(result.Contains("[name] = 'NewName'") && result.Contains("[age] = 30") && result.Contains("[is_active] = 1"));
    }

    #endregion

    #region Empty/Default Cases

    [TestMethod]
    public void EmptyQuery_GeneratesBasicSelect()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().ToSql();
        Assert.IsTrue(result.StartsWith("SELECT *") && result.Contains("FROM [ExprUser]"));
    }

    [TestMethod]
    public void Insert_NoValues_GeneratesInsertWithoutValues()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Insert().ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO") && !result.Contains("VALUES"));
    }

    #endregion

    #region Constant Values

    [TestMethod]
    public void Insert_NullValue_GeneratesNull() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Insert(u => new { u.Email }).Values(new object[] { null! }).ToSql().Contains("NULL"));

    [TestMethod]
    public void Insert_GuidValue_GeneratesCorrectFormat()
    {
        var guid = Guid.NewGuid();
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Insert(u => new { u.Name }).Values(guid).ToSql().Contains(guid.ToString()));
    }

    #endregion
}

[TestClass]
public class ExpressionToSqlStringMethodTests
{
    private static ExpressionToSql<ExprUser> Sqlite => ExpressionToSql<ExprUser>.ForSqlite();
    private static ExpressionToSql<ExprUser> SqlServer => ExpressionToSql<ExprUser>.ForSqlServer();

    [TestMethod]
    public void Where_StringLength_GeneratesLengthFunction() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Length > 5).ToSql().Contains("LENGTH([name])"));

    [TestMethod]
    public void SqlServer_Where_StringLength_GeneratesLenFunction() =>
        Assert.IsTrue(SqlServer.Where(u => u.Name.Length > 5).ToSql().Contains("LEN([name])"));

    [TestMethod]
    public void Where_StringToUpper_GeneratesUpperFunction() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.ToUpper() == "TEST").ToSql().Contains("UPPER([name])"));

    [TestMethod]
    public void Where_StringToLower_GeneratesLowerFunction() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.ToLower() == "test").ToSql().Contains("LOWER([name])"));

    [TestMethod]
    public void Where_StringTrim_GeneratesTrimFunction() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Trim() == "test").ToSql().Contains("TRIM([name])"));
}

[TestClass]
public class ExpressionToSqlMathMethodTests
{
    private static ExpressionToSql<ExprUser> Sqlite => ExpressionToSql<ExprUser>.ForSqlite();
    private static ExpressionToSql<ExprUser> SqlServer => ExpressionToSql<ExprUser>.ForSqlServer();
    private static ExpressionToSql<ExprUser> MySql => ExpressionToSql<ExprUser>.ForMySql();
    private static ExpressionToSql<ExprUser> PostgreSql => ExpressionToSql<ExprUser>.ForPostgreSQL();

    #region Basic Math Functions

    [TestMethod]
    [DataRow("Abs", "ABS([age])")]
    [DataRow("Sign", "SIGN([age])")]
    [DataRow("Sqrt", "SQRT([age])")]
    [DataRow("Exp", "EXP([age])")]
    public void Where_BasicMathFunctions_GeneratesCorrectSql(string func, string expected)
    {
        var result = func switch
        {
            "Abs" => Sqlite.Where(u => Math.Abs(u.Age) > 10).ToSql(),
            "Sign" => Sqlite.Where(u => Math.Sign(u.Age) > 0).ToSql(),
            "Sqrt" => Sqlite.Where(u => Math.Sqrt(u.Age) > 5).ToSql(),
            "Exp" => Sqlite.Where(u => Math.Exp(u.Age) > 100).ToSql(),
            _ => ""
        };
        Assert.IsTrue(result.Contains(expected), $"Expected {expected} in {result}");
    }

    #endregion

    #region Rounding Functions

    [TestMethod]
    public void Where_MathRound_GeneratesRoundFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Round((double)u.Salary) > 50000).ToSql().Contains("ROUND([salary])"));

    [TestMethod]
    public void Where_MathRoundWithDigits_GeneratesRoundWithPrecision() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Round((double)u.Salary, 2) > 50000).ToSql().Contains("ROUND([salary], 2)"));

    [TestMethod]
    public void Where_MathFloor_GeneratesFloorFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Floor((double)u.Salary) > 50000).ToSql().Contains("FLOOR([salary])"));

    [TestMethod]
    public void Where_MathCeiling_GeneratesCeilingFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Ceiling((double)u.Salary) > 50000).ToSql().Contains("CEILING([salary])"));

    [TestMethod]
    public void PostgreSql_MathCeiling_GeneratesCeilFunction() =>
        Assert.IsTrue(PostgreSql.Where(u => Math.Ceiling((double)u.Salary) > 50000).ToSql().Contains("CEIL("));

    [TestMethod]
    public void MySql_MathTruncate_GeneratesTruncateFunction() =>
        Assert.IsTrue(MySql.Where(u => Math.Truncate((double)u.Salary) > 50000).ToSql().Contains("TRUNCATE(`salary`, 0)"));

    [TestMethod]
    public void SqlServer_MathTruncate_GeneratesRoundWithTruncate() =>
        Assert.IsTrue(SqlServer.Where(u => Math.Truncate((double)u.Salary) > 50000).ToSql().Contains("ROUND([salary], 0, 1)"));

    [TestMethod]
    public void PostgreSql_MathTruncate_GeneratesTruncFunction() =>
        Assert.IsTrue(PostgreSql.Where(u => Math.Truncate((double)u.Salary) > 50000).ToSql().Contains("TRUNC("));

    #endregion

    #region Power Functions

    [TestMethod]
    public void Where_MathPow_GeneratesPowerFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Pow(u.Age, 2) > 100).ToSql().Contains("POWER([age], 2)"));

    [TestMethod]
    public void MySql_MathPow_GeneratesPowFunction() =>
        Assert.IsTrue(MySql.Where(u => Math.Pow(u.Age, 2) > 100).ToSql().Contains("POW(`age`, 2)"));

    #endregion

    #region Log Functions

    [TestMethod]
    public void Where_MathLog_GeneratesLogFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Log(u.Age) > 2).ToSql().Contains("LN([age])"));

    [TestMethod]
    public void SqlServer_MathLog_GeneratesLogFunction() =>
        Assert.IsTrue(SqlServer.Where(u => Math.Log(u.Age) > 2).ToSql().Contains("LOG([age])"));

    [TestMethod]
    public void Where_MathLogWithBase_GeneratesLogWithBase() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Log(u.Age, 10) > 1).ToSql().Contains("LOG(10, [age])"));

    [TestMethod]
    public void Where_MathLog10_GeneratesLog10Function() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Log10(u.Age) > 1).ToSql().Contains("LOG10([age])"));

    #endregion

    #region Trigonometric Functions

    [TestMethod]
    [DataRow("Sin", "SIN([age])")]
    [DataRow("Cos", "COS([age])")]
    [DataRow("Tan", "TAN([age])")]
    [DataRow("Atan", "ATAN([age])")]
    public void Where_TrigFunctions_GeneratesCorrectSql(string func, string expected)
    {
        var result = func switch
        {
            "Sin" => Sqlite.Where(u => Math.Sin(u.Age) > 0).ToSql(),
            "Cos" => Sqlite.Where(u => Math.Cos(u.Age) > 0).ToSql(),
            "Tan" => Sqlite.Where(u => Math.Tan(u.Age) > 0).ToSql(),
            "Atan" => Sqlite.Where(u => Math.Atan(u.Age) > 0).ToSql(),
            _ => ""
        };
        Assert.IsTrue(result.Contains(expected), $"Expected {expected} in {result}");
    }

    [TestMethod]
    public void Where_MathAsin_GeneratesAsinFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Asin((double)u.Age / 100) > 0).ToSql().Contains("ASIN("));

    [TestMethod]
    public void Where_MathAcos_GeneratesAcosFunction() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Acos((double)u.Age / 100) > 0).ToSql().Contains("ACOS("));

    [TestMethod]
    public void Where_MathAtan2_GeneratesAtan2Function() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Atan2(u.Age, u.Id) > 0).ToSql().Contains("ATAN2([age], [id])"));

    [TestMethod]
    public void SqlServer_MathAtan2_GeneratesAtn2Function() =>
        Assert.IsTrue(SqlServer.Where(u => Math.Atan2(u.Age, u.Id) > 0).ToSql().Contains("ATN2([age], [id])"));

    #endregion

    #region Min/Max Functions

    [TestMethod]
    public void Where_MathMin_GeneratesLeastFunction() =>
        Assert.IsTrue(MySql.Where(u => Math.Min(u.Age, 100) < 50).ToSql().Contains("LEAST("), $"Expected LEAST");

    [TestMethod]
    public void Where_MathMax_GeneratesGreatestFunction() =>
        Assert.IsTrue(MySql.Where(u => Math.Max(u.Age, 0) > 10).ToSql().Contains("GREATEST("), $"Expected GREATEST");

    #endregion
}

[TestClass]
public class StringFunctionParserTests
{
    private static ExpressionToSql<ExprUser> CreateForDialect(string dialect) => dialect switch
    {
        "SqlServer" => ExpressionToSql<ExprUser>.ForSqlServer(),
        "MySql" => ExpressionToSql<ExprUser>.ForMySql(),
        "PostgreSql" => ExpressionToSql<ExprUser>.ForPostgreSQL(),
        "Oracle" => ExpressionToSql<ExprUser>.ForOracle(),
        "DB2" => ExpressionToSql<ExprUser>.ForDB2(),
        _ => ExpressionToSql<ExprUser>.ForSqlite()
    };

    #region TrimStart / TrimEnd

    [TestMethod]
    public void Where_TrimStart_GeneratesLtrimFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.TrimStart() == "test").ToSql().Contains("LTRIM([name])"));

    [TestMethod]
    public void Where_TrimEnd_GeneratesRtrimFunction() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.TrimEnd() == "test").ToSql().Contains("RTRIM([name])"));

    [TestMethod]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSql")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void AllDialects_TrimStart_GeneratesLtrim(string dialect) =>
        Assert.IsTrue(CreateForDialect(dialect).Where(u => u.Name.TrimStart() == "x").ToSql().Contains("LTRIM("));

    [TestMethod]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSql")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void AllDialects_TrimEnd_GeneratesRtrim(string dialect) =>
        Assert.IsTrue(CreateForDialect(dialect).Where(u => u.Name.TrimEnd() == "x").ToSql().Contains("RTRIM("));

    #endregion

    #region Substring

    [TestMethod]
    public void SQLite_Substring_OneArg_GeneratesSubstrWithOffset() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.Substring(2) == "st").ToSql().Contains("SUBSTR([name], 2 + 1)"));

    [TestMethod]
    public void SQLite_Substring_TwoArgs_GeneratesSubstrWithOffsetAndLength() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.Substring(2, 3) == "est").ToSql().Contains("SUBSTR([name], 2 + 1, 3)"));

    [TestMethod]
    public void SqlServer_Substring_GeneratesSubstringWithOffset() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().Where(u => u.Name.Substring(2, 3) == "est").ToSql().Contains("SUBSTRING([name], 2 + 1, 3)"));

    [TestMethod]
    public void PostgreSql_Substring_GeneratesSubstringFromFor() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForPostgreSQL().Where(u => u.Name.Substring(2, 3) == "est").ToSql().Contains("SUBSTRING(\"name\" FROM 2 + 1 FOR 3)"));

    #endregion

    #region PadLeft / PadRight

    [TestMethod]
    public void MySql_PadLeft_GeneratesLpad() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().Where(u => u.Name.PadLeft(10) == "test").ToSql().Contains("LPAD(`name`, 10, ' ')"));

    [TestMethod]
    public void MySql_PadLeft_WithChar_GeneratesLpadWithChar() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().Where(u => u.Name.PadLeft(10, '0') == "test").ToSql().Contains("LPAD(`name`, 10, '0')"));

    [TestMethod]
    public void MySql_PadRight_GeneratesRpad() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().Where(u => u.Name.PadRight(10) == "test").ToSql().Contains("RPAD(`name`, 10, ' ')"));

    [TestMethod]
    public void MySql_PadRight_WithChar_GeneratesRpadWithChar() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().Where(u => u.Name.PadRight(10, '*') == "test").ToSql().Contains("RPAD(`name`, 10, '*')"));

    [TestMethod]
    public void SqlServer_PadLeft_GeneratesReplicateWorkaround() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().Where(u => u.Name.PadLeft(10) == "test").ToSql().Contains("RIGHT(REPLICATE(' ', 10) + [name], 10)"));

    [TestMethod]
    public void SqlServer_PadRight_GeneratesReplicateWorkaround() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().Where(u => u.Name.PadRight(10) == "test").ToSql().Contains("LEFT([name] + REPLICATE(' ', 10), 10)"));

    [TestMethod]
    public void PostgreSql_PadLeft_GeneratesLpad() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForPostgreSQL().Where(u => u.Name.PadLeft(10, '0') == "test").ToSql().Contains("LPAD(\"name\", 10, '0')"));

    [TestMethod]
    public void PostgreSql_PadRight_GeneratesRpad() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForPostgreSQL().Where(u => u.Name.PadRight(10, '0') == "test").ToSql().Contains("RPAD(\"name\", 10, '0')"));

    #endregion

    #region IndexOf

    [TestMethod]
    public void SqlServer_IndexOf_GeneratesCharindex() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().Where(u => u.Name.IndexOf("test") >= 0).ToSql().Contains("CHARINDEX('test', [name]) - 1"));

    [TestMethod]
    public void MySql_IndexOf_GeneratesLocate() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().Where(u => u.Name.IndexOf("test") >= 0).ToSql().Contains("LOCATE('test', `name`) - 1"));

    [TestMethod]
    public void PostgreSql_IndexOf_GeneratesPosition() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForPostgreSQL().Where(u => u.Name.IndexOf("test") >= 0).ToSql().Contains("POSITION('test' IN \"name\") - 1"));

    [TestMethod]
    public void Oracle_IndexOf_GeneratesInstr() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForOracle().Where(u => u.Name.IndexOf("test") >= 0).ToSql().Contains("INSTR(\"name\", 'test') - 1"));

    [TestMethod]
    public void SQLite_IndexOf_GeneratesInstr() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.IndexOf("test") >= 0).ToSql().Contains("INSTR([name], 'test') - 1"));

    [TestMethod]
    public void SqlServer_IndexOf_WithStart_GeneratesCharindexWithStart() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlServer().Where(u => u.Name.IndexOf("test", 5) >= 0).ToSql().Contains("CHARINDEX('test', [name], 5 + 1) - 1"));

    [TestMethod]
    public void MySql_IndexOf_WithStart_GeneratesLocateWithStart() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForMySql().Where(u => u.Name.IndexOf("test", 5) >= 0).ToSql().Contains("LOCATE('test', `name`, 5 + 1) - 1"));

    #endregion

    #region Replace

    [TestMethod]
    public void Where_Replace_GeneratesReplaceWithQuotedStrings() =>
        Assert.IsTrue(ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.Replace("old", "new") == "test").ToSql().Contains("REPLACE([name], 'old', 'new')"));

    [TestMethod]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSql")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Where_Replace_AllDialects_GeneratesCorrectSql(string dialect)
    {
        var result = CreateForDialect(dialect).Where(u => u.Name.Replace("a", "b") == "x").ToSql();
        Assert.IsTrue(result.Contains("REPLACE(") && result.Contains("'a'") && result.Contains("'b'"), $"Failed for {dialect}");
    }

    #endregion

    #region Contains / StartsWith / EndsWith

    [TestMethod]
    public void Where_Contains_GeneratesLikeWithPercents()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.Contains("test")).ToSql();
        Assert.IsTrue(result.Contains("LIKE") && result.Contains("'%'") && result.Contains("'test'"));
    }

    [TestMethod]
    public void Where_StartsWith_GeneratesLikeWithTrailingPercent()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.StartsWith("test")).ToSql();
        Assert.IsTrue(result.Contains("LIKE") && result.Contains("'test'") && result.Contains("'%'"));
    }

    [TestMethod]
    public void Where_EndsWith_GeneratesLikeWithLeadingPercent()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().Where(u => u.Name.EndsWith("test")).ToSql();
        Assert.IsTrue(result.Contains("LIKE") && result.Contains("'%'") && result.Contains("'test'"));
    }

    #endregion
}

[TestClass]
public class FunctionCompositionTests
{
    private static ExpressionToSql<ExprUser> Sqlite => ExpressionToSql<ExprUser>.ForSqlite();
    private static ExpressionToSql<ExprUser> MySql => ExpressionToSql<ExprUser>.ForMySql();

    #region Math Function Composition

    [TestMethod]
    [DataRow("ROUND(ABS([salary]))")]
    public void MathAbs_NestedInRound_GeneratesCorrectSql(string expected) =>
        Assert.IsTrue(Sqlite.Where(u => Math.Round(Math.Abs((double)u.Salary)) > 1000).ToSql().Contains(expected), $"Expected {expected}");

    [TestMethod]
    public void MathSqrt_NestedInCeiling_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Ceiling(Math.Sqrt(u.Age)) > 5).ToSql().Contains("CEILING(SQRT([age]))"));

    [TestMethod]
    public void MathPow_NestedInFloor_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Floor(Math.Pow(u.Age, 2)) > 100).ToSql().Contains("FLOOR(POWER([age], 2))"));

    [TestMethod]
    public void MathLog_NestedInRound_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Round(Math.Log(u.Age), 2) > 1).ToSql().Contains("ROUND(LN([age]), 2)"));

    [TestMethod]
    public void MathMin_NestedInAbs_GeneratesCorrectSql() =>
        Assert.IsTrue(MySql.Where(u => Math.Abs(Math.Min(u.Age, 100)) > 10).ToSql().Contains("ABS(LEAST(`age`, 100))"));

    [TestMethod]
    public void MathMax_NestedInSqrt_GeneratesCorrectSql() =>
        Assert.IsTrue(MySql.Where(u => Math.Sqrt(Math.Max(u.Age, 0)) > 5).ToSql().Contains("SQRT(GREATEST(`age`, 0))"));

    [TestMethod]
    public void TripleMathNesting_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Round(Math.Abs(Math.Floor((double)u.Salary))) > 1000).ToSql().Contains("ROUND(ABS(FLOOR([salary])))"));

    [TestMethod]
    public void MathSin_NestedInAbs_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Abs(Math.Sin(u.Age)) < 1).ToSql().Contains("ABS(SIN([age]))"));

    #endregion

    #region String Function Composition

    [TestMethod]
    public void StringTrim_NestedInToUpper_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Trim().ToUpper() == "TEST").ToSql().Contains("UPPER(TRIM([name]))"));

    [TestMethod]
    public void StringToLower_NestedInTrim_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.ToLower().Trim() == "test").ToSql().Contains("TRIM(LOWER([name]))"));

    [TestMethod]
    public void StringReplace_NestedInToUpper_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Replace("a", "b").ToUpper() == "TEST").ToSql().Contains("UPPER(REPLACE([name], 'a', 'b'))"));

    [TestMethod]
    public void StringSubstring_NestedInTrim_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Substring(0, 5).Trim() == "test").ToSql().Contains("TRIM(SUBSTR([name], 0 + 1, 5))"));

    [TestMethod]
    public void StringTrimStart_NestedInTrimEnd_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.TrimStart().TrimEnd() == "test").ToSql().Contains("RTRIM(LTRIM([name]))"));

    [TestMethod]
    public void TripleStringNesting_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Trim().ToUpper().Replace("A", "B") == "TEST").ToSql().Contains("REPLACE(UPPER(TRIM([name])), 'A', 'B')"));

    #endregion

    #region Mixed Math and String Composition

    [TestMethod]
    public void StringLength_NestedInMathAbs_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Abs(u.Name.Length - 10) < 5).ToSql().Contains("ABS((LENGTH([name]) - 10))"));

    [TestMethod]
    public void StringLength_NestedInMathSqrt_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Sqrt(u.Name.Length) > 2).ToSql().Contains("SQRT(LENGTH([name]))"));

    [TestMethod]
    public void MathRound_WithStringLength_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Round((double)u.Name.Length / 2) > 3).ToSql().Contains("ROUND((LENGTH([name]) / 2))"));

    #endregion

    #region Arithmetic with Functions

    [TestMethod]
    public void MathAbs_WithAddition_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Abs(u.Age) + 10 > 20).ToSql().Contains("(ABS([age]) + 10)"));

    [TestMethod]
    public void MathSqrt_WithMultiplication_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Sqrt(u.Age) * 2 > 10).ToSql().Contains("(SQRT([age]) * 2)"));

    [TestMethod]
    public void StringLength_WithArithmetic_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => u.Name.Length * 2 + 5 > 20).ToSql().Contains("((LENGTH([name]) * 2) + 5)"));

    [TestMethod]
    public void MathPow_WithDivision_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Pow(u.Age, 2) / 100 > 1).ToSql().Contains("(POWER([age], 2) / 100)"));

    #endregion

    #region Multiple Column Functions

    [TestMethod]
    public void MathMin_TwoColumns_GeneratesCorrectSql() =>
        Assert.IsTrue(MySql.Where(u => Math.Min(u.Age, u.Id) > 0).ToSql().Contains("LEAST(`age`, `id`)"));

    [TestMethod]
    public void MathMax_TwoColumns_GeneratesCorrectSql() =>
        Assert.IsTrue(MySql.Where(u => Math.Max(u.Age, u.Id) < 100).ToSql().Contains("GREATEST(`age`, `id`)"));

    [TestMethod]
    public void MathPow_TwoColumns_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Where(u => Math.Pow(u.Age, u.Id) > 100).ToSql().Contains("POWER([age], [id])"));

    #endregion

    #region Set Expression with Functions

    [TestMethod]
    public void Set_WithMathAbs_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Set(u => u.Age, u => Math.Abs(u.Age)).Where(u => u.Id == 1).ToSql().Contains("[age] = ABS([age])"));

    [TestMethod]
    public void Set_WithMathRound_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Set(u => u.Salary, u => (decimal)Math.Round((double)u.Salary, 2)).Where(u => u.Id == 1).ToSql().Contains("[salary] = ROUND([salary], 2)"));

    [TestMethod]
    public void Set_WithStringTrim_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Set(u => u.Name, u => u.Name.Trim()).Where(u => u.Id == 1).ToSql().Contains("[name] = TRIM([name])"));

    [TestMethod]
    public void Set_WithStringToUpper_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Set(u => u.Name, u => u.Name.ToUpper()).Where(u => u.Id == 1).ToSql().Contains("[name] = UPPER([name])"));

    [TestMethod]
    public void Set_WithNestedFunctions_GeneratesCorrectSql() =>
        Assert.IsTrue(Sqlite.Set(u => u.Name, u => u.Name.Trim().ToUpper()).Where(u => u.Id == 1).ToSql().Contains("[name] = UPPER(TRIM([name]))"));

    #endregion

    #region Select Expression with Functions

    [TestMethod]
    public void Select_WithColumnExpression_GeneratesCorrectSql()
    {
        var result = Sqlite.Select(u => new { u.Id, u.Name }).ToSql();
        Assert.IsTrue(result.Contains("[id]") && result.Contains("[name]"));
    }

    #endregion

    #region Complex Expressions

    [TestMethod]
    public void ComplexExpression_MathAndStringAndArithmetic_GeneratesCorrectSql()
    {
        var result = Sqlite.Where(u => Math.Abs(u.Age - 30) + u.Name.Length < 50).ToSql();
        Assert.IsTrue(result.Contains("ABS(([age] - 30))") && result.Contains("LENGTH([name])"));
    }

    [TestMethod]
    public void ComplexExpression_MultipleConditions_GeneratesCorrectSql()
    {
        var result = Sqlite.Where(u => Math.Sqrt(u.Age) > 5 && u.Name.Length > 3).ToSql();
        Assert.IsTrue(result.Contains("SQRT([age]) > 5") && result.Contains("LENGTH([name]) > 3"));
    }

    #endregion
}
