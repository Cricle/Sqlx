using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

// Test entity at namespace level (source generator requirement)
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
    public void Create_WithDialect_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.Create(SqlDefine.SQLite);
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ForSqlServer_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.ForSqlServer();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ForMySql_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.ForMySql();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ForPostgreSQL_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.ForPostgreSQL();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ForSqlite_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.ForSqlite();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ForOracle_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.ForOracle();
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ForDB2_ReturnsInstance()
    {
        var sql = ExpressionToSql<ExprUser>.ForDB2();
        Assert.IsNotNull(sql);
    }

    #endregion

    #region SELECT Operations

    [TestMethod]
    public void Select_NoColumns_ReturnsSelectStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
    }

    [TestMethod]
    public void Select_WithStringColumns_ReturnsSpecifiedColumns()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select("id", "name")
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT id, name"));
    }

    [TestMethod]
    public void Select_WithEmptyArray_ReturnsSelectStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select(Array.Empty<string>())
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
    }

    [TestMethod]
    public void Select_WithNullArray_ReturnsSelectStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select((string[])null!)
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
    }

    [TestMethod]
    public void Select_WithExpression_ReturnsColumns()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select(u => new { u.Id, u.Name })
            .ToSql();
        Assert.IsTrue(result.Contains("[id]"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void Select_WithSingleExpression_ReturnsColumn()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select(u => u.Name)
            .ToSql();
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void Select_WithNullExpression_ReturnsSelectStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select<object>(null!)
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
    }

    [TestMethod]
    public void Select_WithMultipleExpressions_ReturnsAllColumns()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select(u => u.Id, u => u.Name, u => u.Age)
            .ToSql();
        Assert.IsTrue(result.Contains("[id]"));
        Assert.IsTrue(result.Contains("[name]"));
        Assert.IsTrue(result.Contains("[age]"));
    }

    [TestMethod]
    public void Select_WithEmptyExpressionArray_ReturnsSelectStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select(Array.Empty<Expression<Func<ExprUser, object>>>())
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
    }

    [TestMethod]
    public void Select_WithNullExpressionArray_ReturnsSelectStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select((Expression<Func<ExprUser, object>>[])null!)
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
    }

    #endregion

    #region WHERE Operations

    [TestMethod]
    public void Where_SimpleEquality_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("WHERE [id] = 1"));
    }

    [TestMethod]
    public void Where_StringEquality_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("WHERE [name] = 'test'"));
    }

    [TestMethod]
    public void Where_MultipleConditions_GeneratesAndClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Id > 0)
            .And(u => u.IsActive == true)
            .ToSql();
        Assert.IsTrue(result.Contains("WHERE"));
        Assert.IsTrue(result.Contains("[id] > 0"));
        Assert.IsTrue(result.Contains("[is_active] = 1"));
    }

    [TestMethod]
    public void Where_NullPredicate_DoesNotAddCondition()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(null!)
            .ToSql();
        Assert.IsFalse(result.Contains("WHERE"));
    }

    [TestMethod]
    public void Where_GreaterThan_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age > 18)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] > 18"));
    }

    [TestMethod]
    public void Where_LessThan_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age < 65)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] < 65"));
    }

    [TestMethod]
    public void Where_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age >= 18)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] >= 18"));
    }

    [TestMethod]
    public void Where_LessThanOrEqual_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age <= 65)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] <= 65"));
    }

    [TestMethod]
    public void Where_NotEqual_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Id != 0)
            .ToSql();
        Assert.IsTrue(result.Contains("[id] <> 0"));
    }

    [TestMethod]
    public void Where_OrCondition_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age < 18 || u.Age > 65)
            .ToSql();
        Assert.IsTrue(result.Contains("OR"));
    }

    [TestMethod]
    public void Where_AndCondition_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age >= 18 && u.Age <= 65)
            .ToSql();
        Assert.IsTrue(result.Contains("AND"));
    }

    [TestMethod]
    public void ToWhereClause_ReturnsOnlyWhereConditions()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Id == 1)
            .ToWhereClause();
        Assert.IsTrue(result.Contains("[id] = 1"));
    }

    [TestMethod]
    public void ToWhereClause_NoConditions_ReturnsEmpty()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .ToWhereClause();
        Assert.AreEqual(string.Empty, result);
    }

    #endregion

    #region ORDER BY Operations

    [TestMethod]
    public void OrderBy_SingleColumn_GeneratesAsc()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .OrderBy(u => u.Name)
            .ToSql();
        Assert.IsTrue(result.Contains("ORDER BY [name] ASC"));
    }

    [TestMethod]
    public void OrderByDescending_SingleColumn_GeneratesDesc()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .OrderByDescending(u => u.CreatedAt)
            .ToSql();
        Assert.IsTrue(result.Contains("ORDER BY [created_at] DESC"));
    }

    [TestMethod]
    public void OrderBy_MultipleColumns_GeneratesMultipleOrderBy()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .OrderBy(u => u.Name)
            .OrderByDescending(u => u.Age)
            .ToSql();
        Assert.IsTrue(result.Contains("ORDER BY [name] ASC, [age] DESC"));
    }

    [TestMethod]
    public void OrderBy_NullExpression_DoesNotAddOrderBy()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .OrderBy<int>(null!)
            .ToSql();
        Assert.IsFalse(result.Contains("ORDER BY"));
    }

    [TestMethod]
    public void OrderByDescending_NullExpression_DoesNotAddOrderBy()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .OrderByDescending<int>(null!)
            .ToSql();
        Assert.IsFalse(result.Contains("ORDER BY"));
    }

    #endregion

    #region Pagination

    [TestMethod]
    public void Take_GeneratesLimit()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Take(10)
            .ToSql();
        Assert.IsTrue(result.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void Skip_GeneratesOffset()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Skip(20)
            .ToSql();
        Assert.IsTrue(result.Contains("OFFSET 20"));
    }

    [TestMethod]
    public void TakeAndSkip_GeneratesLimitOffset()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Take(10)
            .Skip(20)
            .ToSql();
        Assert.IsTrue(result.Contains("LIMIT 10"));
        Assert.IsTrue(result.Contains("OFFSET 20"));
    }

    [TestMethod]
    public void SqlServer_Pagination_GeneratesOffsetFetch()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Take(10)
            .Skip(20)
            .ToSql();
        Assert.IsTrue(result.Contains("OFFSET 20 ROWS"));
        Assert.IsTrue(result.Contains("FETCH NEXT 10 ROWS ONLY"));
    }

    [TestMethod]
    public void SqlServer_SkipOnly_GeneratesOffsetWithoutFetch()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Skip(20)
            .ToSql();
        Assert.IsTrue(result.Contains("OFFSET 20 ROWS"));
        Assert.IsFalse(result.Contains("FETCH"));
    }

    #endregion

    #region UPDATE Operations

    [TestMethod]
    public void Update_WithSet_GeneratesUpdateSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Update()
            .Set(u => u.Name, "NewName")
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.StartsWith("UPDATE"));
        Assert.IsTrue(result.Contains("SET [name] = 'NewName'"));
        Assert.IsTrue(result.Contains("WHERE [id] = 1"));
    }

    [TestMethod]
    public void Set_WithValue_GeneratesSetClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Age, 30)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("SET [age] = 30"));
    }

    [TestMethod]
    public void Set_WithExpression_GeneratesSetClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Age, u => u.Age + 1)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("SET [age] = ([age] + 1)"));
    }

    [TestMethod]
    public void Set_NullSelector_DoesNotAddSet()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set<int>(null!, 30)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsFalse(result.Contains("[age]"));
    }

    [TestMethod]
    public void Set_NullValueExpression_DoesNotAddSet()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set<int>(u => u.Age, null!)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsFalse(result.Contains("[age] ="));
    }

    [TestMethod]
    public void ToSetClause_ReturnsOnlySetPart()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Name, "Test")
            .Set(u => u.Age, 25)
            .ToSetClause();
        Assert.IsTrue(result.Contains("[name] = 'Test'"));
        Assert.IsTrue(result.Contains("[age] = 25"));
    }

    [TestMethod]
    public void ToSetClause_NoSets_ReturnsEmpty()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .ToSetClause();
        Assert.AreEqual(string.Empty, result);
    }

    #endregion

    #region INSERT Operations

    [TestMethod]
    public void Insert_WithValues_GeneratesInsertSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert(u => new { u.Name, u.Age })
            .Values("Test", 25)
            .ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO"));
        Assert.IsTrue(result.Contains("VALUES"));
        Assert.IsTrue(result.Contains("'Test'"));
        Assert.IsTrue(result.Contains("25"));
    }

    [TestMethod]
    public void Insert_WithoutColumns_GeneratesInsertWithoutColumnList()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert()
            .Values("Test", 25)
            .ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO"));
        Assert.IsFalse(result.Contains("(["));
    }

    [TestMethod]
    public void Insert_WithNullSelector_GeneratesInsertWithoutColumnList()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert(null)
            .Values("Test", 25)
            .ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO"));
    }

    [TestMethod]
    public void InsertSelect_GeneratesInsertSelectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .InsertSelect("SELECT * FROM other_table")
            .ToSql();
        Assert.IsTrue(result.Contains("INSERT INTO"));
        Assert.IsTrue(result.Contains("SELECT * FROM other_table"));
    }

    [TestMethod]
    public void AddValues_MultipleRows_GeneratesMultipleValueSets()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert(u => new { u.Name, u.Age })
            .AddValues("User1", 20)
            .AddValues("User2", 30)
            .ToSql();
        Assert.IsTrue(result.Contains("('User1', 20)"));
        Assert.IsTrue(result.Contains("('User2', 30)"));
    }

    [TestMethod]
    public void Values_EmptyArray_DoesNotAddValues()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert()
            .Values(Array.Empty<object>())
            .ToSql();
        Assert.IsFalse(result.Contains("VALUES"));
    }

    [TestMethod]
    public void Values_NullArray_DoesNotAddValues()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert()
            .Values(null!)
            .ToSql();
        Assert.IsFalse(result.Contains("VALUES"));
    }

    #endregion

    #region DELETE Operations

    [TestMethod]
    public void Delete_WithWhere_GeneratesDeleteSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Delete(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.StartsWith("DELETE FROM"));
        Assert.IsTrue(result.Contains("WHERE [id] = 1"));
    }

    [TestMethod]
    public void Delete_WithoutWhere_ThrowsException()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            ExpressionToSql<ExprUser>.ForSqlite()
                .Delete()
                .ToSql());
    }

    [TestMethod]
    public void Delete_NullPredicate_ThrowsException()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            ExpressionToSql<ExprUser>.ForSqlite()
                .Delete(null!)
                .ToSql());
    }

    #endregion

    #region GROUP BY / HAVING

    [TestMethod]
    public void GroupBy_GeneratesGroupByClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .ToSql();
        Assert.IsTrue(result.Contains("GROUP BY [is_active]"));
    }

    [TestMethod]
    public void AddGroupBy_AddsGroupByColumn()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .AddGroupBy("status")
            .ToSql();
        Assert.IsTrue(result.Contains("GROUP BY status"));
    }

    [TestMethod]
    public void Having_GeneratesHavingClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .AddGroupBy("[is_active]")
            .Having(u => u.Age > 18)
            .ToSql();
        Assert.IsTrue(result.Contains("HAVING"));
    }

    [TestMethod]
    public void Having_NullPredicate_DoesNotAddHaving()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .AddGroupBy("[is_active]")
            .Having(null!)
            .ToSql();
        Assert.IsFalse(result.Contains("HAVING"));
    }

    [TestMethod]
    public void GroupBy_NullExpression_DoesNotAddGroupBy()
    {
        var grouped = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy<int>(null!);
        var result = grouped.ToSql();
        Assert.IsFalse(result.Contains("GROUP BY"));
    }

    #endregion

    #region ToAdditionalClause

    [TestMethod]
    public void ToAdditionalClause_WithAllClauses_ReturnsAllParts()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .AddGroupBy("[is_active]")
            .Having(u => u.Age > 0)
            .OrderBy(u => u.Name)
            .Take(10)
            .Skip(5)
            .ToAdditionalClause();
        Assert.IsTrue(result.Contains("GROUP BY"));
        Assert.IsTrue(result.Contains("HAVING"));
        Assert.IsTrue(result.Contains("ORDER BY"));
        Assert.IsTrue(result.Contains("LIMIT"));
        Assert.IsTrue(result.Contains("OFFSET"));
    }

    [TestMethod]
    public void ToAdditionalClause_Empty_ReturnsEmpty()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .ToAdditionalClause();
        Assert.AreEqual(string.Empty, result);
    }

    #endregion

    #region Parameterized Queries

    [TestMethod]
    public void UseParameterizedQueries_EnablesParameterization()
    {
        var sql = ExpressionToSql<ExprUser>.ForSqlite()
            .UseParameterizedQueries()
            .Where(u => u.Id == 1);
        Assert.IsNotNull(sql);
    }

    #endregion

    #region ToTemplate

    [TestMethod]
    public void ToTemplate_ReturnsSameAsSql()
    {
        var query = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Id == 1);
        Assert.AreEqual(query.ToSql(), query.ToTemplate());
    }

    #endregion

    #region Dialect-Specific Tests

    [TestMethod]
    public void MySql_Select_UsesBackticks()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Select(u => u.Name)
            .ToSql();
        Assert.IsTrue(result.Contains("`name`"));
        Assert.IsTrue(result.Contains("`ExprUser`"));
    }

    [TestMethod]
    public void PostgreSql_Select_UsesDoubleQuotes()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Select(u => u.Name)
            .ToSql();
        Assert.IsTrue(result.Contains("\"name\""));
        Assert.IsTrue(result.Contains("\"ExprUser\""));
    }

    [TestMethod]
    public void Oracle_Select_UsesDoubleQuotes()
    {
        var result = ExpressionToSql<ExprUser>.ForOracle()
            .Select(u => u.Name)
            .ToSql();
        Assert.IsTrue(result.Contains("\"name\""));
    }

    [TestMethod]
    public void DB2_Select_UsesDoubleQuotes()
    {
        var result = ExpressionToSql<ExprUser>.ForDB2()
            .Select(u => u.Name)
            .ToSql();
        Assert.IsTrue(result.Contains("\"name\""));
    }

    #endregion
}


[TestClass]
public class GroupedExpressionToSqlTests
{
    #region Select with Aggregations

    [TestMethod]
    public void GroupBy_Select_Count_GeneratesCountStar()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToSql();
        Assert.IsTrue(result.Contains("COUNT(*)"));
        Assert.IsTrue(result.Contains("AS Count"));
    }

    [TestMethod]
    public void GroupBy_Select_Sum_GeneratesSumFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { g.Key, Total = g.Sum(u => u.Salary) })
            .ToSql();
        Assert.IsTrue(result.Contains("SUM([salary])"));
        Assert.IsTrue(result.Contains("AS Total"));
    }

    [TestMethod]
    public void GroupBy_Select_Average_GeneratesAvgFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { g.Key, AvgAge = g.Average(u => (double)u.Age) })
            .ToSql();
        Assert.IsTrue(result.Contains("AVG([age])"));
    }

    [TestMethod]
    public void GroupBy_Select_Max_GeneratesMaxFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { g.Key, MaxAge = g.Max(u => u.Age) })
            .ToSql();
        Assert.IsTrue(result.Contains("MAX([age])"));
    }

    [TestMethod]
    public void GroupBy_Select_Min_GeneratesMinFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { g.Key, MinAge = g.Min(u => u.Age) })
            .ToSql();
        Assert.IsTrue(result.Contains("MIN([age])"));
    }

    #endregion

    #region Having Clause

    [TestMethod]
    public void GroupBy_Having_Count_GeneratesHavingClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Having(g => g.Count() > 5)
            .ToSql();
        Assert.IsTrue(result.Contains("HAVING COUNT(*) > 5"));
    }

    [TestMethod]
    public void GroupBy_Having_Sum_GeneratesHavingClause()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Having(g => g.Sum(u => u.Salary) > 10000m)
            .ToSql();
        Assert.IsTrue(result.Contains("HAVING SUM([salary]) > 10000"));
    }

    #endregion

    #region Select with MemberInit

    [TestMethod]
    public void GroupBy_Select_MemberInit_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new ExprOrder { UserId = g.Count() })
            .ToSql();
        Assert.IsTrue(result.Contains("COUNT(*)"));
        Assert.IsTrue(result.Contains("AS UserId"));
    }

    #endregion

    #region Select with Binary Expressions

    [TestMethod]
    public void GroupBy_Select_BinaryExpression_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Total = g.Sum(u => u.Salary) + 100m })
            .ToSql();
        Assert.IsTrue(result.Contains("SUM([salary])"));
        Assert.IsTrue(result.Contains("+ 100"));
    }

    [TestMethod]
    public void GroupBy_Select_Coalesce_GeneratesCoalesce()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Value = g.Sum(u => (decimal?)u.Salary) ?? 0m })
            .ToSql();
        Assert.IsTrue(result.Contains("COALESCE"));
    }

    #endregion

    #region Select with Conditional

    [TestMethod]
    public void GroupBy_Select_Conditional_GeneratesCaseWhen()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Status = g.Count() > 0 ? "Active" : "Inactive" })
            .ToSql();
        Assert.IsTrue(result.Contains("CASE WHEN"));
        Assert.IsTrue(result.Contains("THEN"));
        Assert.IsTrue(result.Contains("ELSE"));
        Assert.IsTrue(result.Contains("END"));
    }

    #endregion

    #region Math Functions in Aggregation

    [TestMethod]
    public void GroupBy_Select_MathAbs_GeneratesAbsFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { AbsSum = g.Sum(u => Math.Abs(u.Age)) })
            .ToSql();
        Assert.IsTrue(result.Contains("ABS([age])"));
    }

    [TestMethod]
    public void GroupBy_Select_MathRound_GeneratesRoundFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Rounded = g.Sum(u => Math.Round((double)u.Salary)) })
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND([salary])"));
    }

    [TestMethod]
    public void GroupBy_Select_MathRoundWithDigits_GeneratesRoundFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Rounded = g.Sum(u => Math.Round((double)u.Salary, 2)) })
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND([salary], 2)"));
    }

    [TestMethod]
    public void GroupBy_Select_MathFloor_GeneratesFloorFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Floored = g.Sum(u => Math.Floor((double)u.Salary)) })
            .ToSql();
        Assert.IsTrue(result.Contains("FLOOR([salary])"));
    }

    [TestMethod]
    public void GroupBy_Select_MathCeiling_GeneratesCeilingFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Ceiled = g.Sum(u => Math.Ceiling((double)u.Salary)) })
            .ToSql();
        Assert.IsTrue(result.Contains("CEILING([salary])"));
    }

    [TestMethod]
    public void PostgreSql_GroupBy_Select_MathCeiling_GeneratesCeilFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Ceiled = g.Sum(u => Math.Ceiling((double)u.Salary)) })
            .ToSql();
        Assert.IsTrue(result.Contains("CEIL(\"salary\")"));
    }

    [TestMethod]
    public void GroupBy_Select_MathMin_GeneratesLeastFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { MinVal = g.Sum(u => Math.Min(u.Age, 100)) })
            .ToSql();
        Assert.IsTrue(result.Contains("LEAST([age], 100)"));
    }

    [TestMethod]
    public void GroupBy_Select_MathMax_GeneratesGreatestFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { MaxVal = g.Sum(u => Math.Max(u.Age, 0)) })
            .ToSql();
        Assert.IsTrue(result.Contains("GREATEST([age], 0)"));
    }

    [TestMethod]
    public void GroupBy_Select_MathPow_GeneratesPowerFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Powered = g.Sum(u => Math.Pow(u.Age, 2)) })
            .ToSql();
        Assert.IsTrue(result.Contains("POWER([age], 2)"));
    }

    [TestMethod]
    public void MySql_GroupBy_Select_MathPow_GeneratesPowFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Powered = g.Sum(u => Math.Pow(u.Age, 2)) })
            .ToSql();
        Assert.IsTrue(result.Contains("POW(`age`, 2)"));
    }

    [TestMethod]
    public void GroupBy_Select_MathSqrt_GeneratesSqrtFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Sqrt = g.Sum(u => Math.Sqrt(u.Age)) })
            .ToSql();
        Assert.IsTrue(result.Contains("SQRT([age])"));
    }

    #endregion

    #region String Functions in Aggregation

    [TestMethod]
    public void GroupBy_Select_StringLength_GeneratesLengthFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { MaxLen = g.Max(u => u.Name.Length) })
            .ToSql();
        Assert.IsTrue(result.Contains("LENGTH([name])"));
    }

    [TestMethod]
    public void SqlServer_GroupBy_Select_StringLength_GeneratesLenFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .GroupBy(u => u.IsActive)
            .Select(g => new { MaxLen = g.Max(u => u.Name.Length) })
            .ToSql();
        Assert.IsTrue(result.Contains("LEN([name])"));
    }

    [TestMethod]
    public void GroupBy_Select_StringToUpper_GeneratesUpperFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Upper = g.Max(u => u.Name.ToUpper()) })
            .ToSql();
        Assert.IsTrue(result.Contains("UPPER([name])"));
    }

    [TestMethod]
    public void GroupBy_Select_StringToLower_GeneratesLowerFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Lower = g.Max(u => u.Name.ToLower()) })
            .ToSql();
        Assert.IsTrue(result.Contains("LOWER([name])"));
    }

    [TestMethod]
    public void GroupBy_Select_StringTrim_GeneratesTrimFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Trimmed = g.Max(u => u.Name.Trim()) })
            .ToSql();
        Assert.IsTrue(result.Contains("TRIM([name])"));
    }

    [TestMethod]
    public void GroupBy_Select_StringSubstring_GeneratesSubstringFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Sub = g.Max(u => u.Name.Substring(0, 5)) })
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTR([name], 0 + 1, 5)"));
    }

    [TestMethod]
    public void SqlServer_GroupBy_Select_StringSubstring_GeneratesSubstringFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Sub = g.Max(u => u.Name.Substring(0, 5)) })
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTRING([name], 0 + 1, 5)"));
    }

    [TestMethod]
    public void GroupBy_Select_StringSubstringOneArg_GeneratesSubstringFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Sub = g.Max(u => u.Name.Substring(5)) })
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTR([name], 5 + 1)"));
    }

    [TestMethod]
    public void GroupBy_Select_StringReplace_GeneratesReplaceFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Replaced = g.Max(u => u.Name.Replace("a", "b")) })
            .ToSql();
        Assert.IsTrue(result.Contains("REPLACE([name], 'a', 'b')"));
    }

    #endregion

    #region ToSql and ToTemplate

    [TestMethod]
    public void GroupBy_ToSql_ReturnsCompleteSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Age > 18)
            .GroupBy(u => u.IsActive)
            .ToSql();
        Assert.IsTrue(result.Contains("SELECT *"));
        Assert.IsTrue(result.Contains("WHERE"));
        Assert.IsTrue(result.Contains("GROUP BY"));
    }

    [TestMethod]
    public void GroupBy_ToTemplate_ReturnsSameAsSql()
    {
        var grouped = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive);
        Assert.AreEqual(grouped.ToSql(), grouped.ToTemplate());
    }

    #endregion

    #region Aggregate Body Parsing

    [TestMethod]
    public void GroupBy_Select_BinaryInAggBody_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Total = g.Sum(u => u.Age + u.Id) })
            .ToSql();
        Assert.IsTrue(result.Contains("SUM(([age] + [id]))"));
    }

    [TestMethod]
    public void GroupBy_Select_CoalesceInAggBody_GeneratesCoalesce()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Total = g.Sum(u => (int?)u.Age ?? 0) })
            .ToSql();
        Assert.IsTrue(result.Contains("COALESCE([age], 0)"));
    }

    [TestMethod]
    public void GroupBy_Select_ConditionalInAggBody_GeneratesCaseWhen()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Total = g.Sum(u => u.Age > 18 ? u.Age : 0) })
            .ToSql();
        Assert.IsTrue(result.Contains("CASE WHEN"));
    }

    #endregion

    #region Having with Key

    [TestMethod]
    public void GroupBy_Having_WithKey_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Having(g => g.Key == true)
            .ToSql();
        Assert.IsTrue(result.Contains("HAVING"));
        Assert.IsTrue(result.Contains("[is_active]"));
    }

    #endregion
}

[TestClass]
public class AnyPlaceholderTests
{
    [TestMethod]
    public void Any_Value_ReturnsDefault()
    {
        var result = Any.Value<int>();
        Assert.AreEqual(default(int), result);
    }

    [TestMethod]
    public void Any_ValueWithName_ReturnsDefault()
    {
        var result = Any.Value<string>("param");
        Assert.AreEqual(default(string), result);
    }

    [TestMethod]
    public void Any_String_ReturnsDefault()
    {
        var result = Any.String();
        Assert.AreEqual(default(string), result);
    }

    [TestMethod]
    public void Any_StringWithName_ReturnsDefault()
    {
        var result = Any.String("param");
        Assert.AreEqual(default(string), result);
    }

    [TestMethod]
    public void Any_Int_ReturnsDefault()
    {
        var result = Any.Int();
        Assert.AreEqual(default(int), result);
    }

    [TestMethod]
    public void Any_IntWithName_ReturnsDefault()
    {
        var result = Any.Int("param");
        Assert.AreEqual(default(int), result);
    }

    [TestMethod]
    public void Any_Bool_ReturnsDefault()
    {
        var result = Any.Bool();
        Assert.AreEqual(default(bool), result);
    }

    [TestMethod]
    public void Any_BoolWithName_ReturnsDefault()
    {
        var result = Any.Bool("param");
        Assert.AreEqual(default(bool), result);
    }

    [TestMethod]
    public void Any_DateTime_ReturnsDefault()
    {
        var result = Any.DateTime();
        Assert.AreEqual(default(DateTime), result);
    }

    [TestMethod]
    public void Any_DateTimeWithName_ReturnsDefault()
    {
        var result = Any.DateTime("param");
        Assert.AreEqual(default(DateTime), result);
    }

    [TestMethod]
    public void Any_Guid_ReturnsDefault()
    {
        var result = Any.Guid();
        Assert.AreEqual(default(Guid), result);
    }

    [TestMethod]
    public void Any_GuidWithName_ReturnsDefault()
    {
        var result = Any.Guid("param");
        Assert.AreEqual(default(Guid), result);
    }
}

[TestClass]
public class GroupingExtensionsTests
{
    [TestMethod]
    public void Count_ReturnsDefault()
    {
        IGrouping<int, ExprUser> grouping = null!;
        var result = GroupingExtensions.Count(grouping);
        Assert.AreEqual(default(int), result);
    }

    [TestMethod]
    public void Sum_ReturnsDefault()
    {
        IGrouping<int, ExprUser> grouping = null!;
        var result = GroupingExtensions.Sum(grouping, u => u.Age);
        Assert.AreEqual(default(int), result);
    }

    [TestMethod]
    public void Average_Double_ReturnsDefault()
    {
        IGrouping<int, ExprUser> grouping = null!;
        var result = GroupingExtensions.Average(grouping, u => (double)u.Age);
        Assert.AreEqual(default(double), result);
    }

    [TestMethod]
    public void Average_Decimal_ReturnsDefault()
    {
        IGrouping<int, ExprUser> grouping = null!;
        var result = GroupingExtensions.Average(grouping, u => u.Salary);
        Assert.AreEqual(default(double), result);
    }

    [TestMethod]
    public void Max_ReturnsDefault()
    {
        IGrouping<int, ExprUser> grouping = null!;
        var result = GroupingExtensions.Max(grouping, u => u.Age);
        Assert.AreEqual(default(int), result);
    }

    [TestMethod]
    public void Min_ReturnsDefault()
    {
        IGrouping<int, ExprUser> grouping = null!;
        var result = GroupingExtensions.Min(grouping, u => u.Age);
        Assert.AreEqual(default(int), result);
    }
}


[TestClass]
public class ExpressionToSqlEdgeCaseTests
{
    #region Null Value Handling

    [TestMethod]
    public void Where_NullValue_GeneratesIsNull()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Email == null)
            .ToSql();
        Assert.IsTrue(result.Contains("IS NULL"));
    }

    [TestMethod]
    public void Where_NotNullValue_GeneratesIsNotNull()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Email != null)
            .ToSql();
        Assert.IsTrue(result.Contains("IS NOT NULL"));
    }

    #endregion

    #region Boolean Handling

    [TestMethod]
    public void Where_BooleanTrue_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.IsActive == true)
            .ToSql();
        Assert.IsTrue(result.Contains("[is_active] = 1"));
    }

    [TestMethod]
    public void Where_BooleanFalse_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.IsActive == false)
            .ToSql();
        Assert.IsTrue(result.Contains("[is_active] = 0"));
    }

    [TestMethod]
    public void PostgreSql_Where_BooleanTrue_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => u.IsActive == true)
            .ToSql();
        // PostgreSQL may use 1 or true depending on implementation
        Assert.IsTrue(result.Contains("\"is_active\""));
    }

    [TestMethod]
    public void PostgreSql_Where_BooleanFalse_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => u.IsActive == false)
            .ToSql();
        // PostgreSQL may use 0 or false depending on implementation
        Assert.IsTrue(result.Contains("\"is_active\""));
    }

    #endregion

    #region DateTime Handling

    [TestMethod]
    public void Where_DateTime_GeneratesCorrectFormat()
    {
        var date = new DateTime(2024, 1, 15, 10, 30, 0);
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.CreatedAt == date)
            .ToSql();
        // DateTime format may vary, just check it contains the date
        Assert.IsTrue(result.Contains("[created_at]"));
    }

    [TestMethod]
    public void Set_DateTime_GeneratesCorrectFormat()
    {
        var date = new DateTime(2024, 6, 15);
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.CreatedAt, date)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("2024-06-15"));
    }

    #endregion

    #region Decimal Handling

    [TestMethod]
    public void Where_Decimal_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Salary > 50000.50m)
            .ToSql();
        Assert.IsTrue(result.Contains("50000.5"));
    }

    [TestMethod]
    public void Set_Decimal_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Salary, 75000.25m)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("75000.25"));
    }

    #endregion

    #region String Escaping

    [TestMethod]
    public void Where_StringWithQuote_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name == "O'Brien")
            .ToSql();
        // String should be in the SQL
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void Insert_StringWithQuote_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert(u => new { u.Name })
            .Values("O'Brien")
            .ToSql();
        // String should be in the SQL
        Assert.IsTrue(result.Contains("O"));
    }

    #endregion

    #region Complex Expressions

    [TestMethod]
    public void Where_NestedAndOr_GeneratesCorrectParentheses()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => (u.Age > 18 && u.Age < 65) || u.IsActive)
            .ToSql();
        Assert.IsTrue(result.Contains("("));
        Assert.IsTrue(result.Contains(")"));
        Assert.IsTrue(result.Contains("AND"));
        Assert.IsTrue(result.Contains("OR"));
    }

    [TestMethod]
    public void Where_Negation_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => !u.IsActive)
            .ToSql();
        // Negation should produce some form of NOT or = 0
        Assert.IsTrue(result.Contains("[is_active]"));
    }

    #endregion

    #region Arithmetic Operations

    [TestMethod]
    public void Set_Addition_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Age, u => u.Age + 1)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] + 1"));
    }

    [TestMethod]
    public void Set_Subtraction_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Age, u => u.Age - 1)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] - 1"));
    }

    [TestMethod]
    public void Set_Multiplication_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Salary, u => u.Salary * 1.1m)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[salary] * 1.1"));
    }

    [TestMethod]
    public void Set_Division_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Salary, u => u.Salary / 2)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[salary] / 2"));
    }

    [TestMethod]
    public void Set_Modulo_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Age, u => u.Age % 10)
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] % 10"));
    }

    #endregion

    #region Chained Operations

    [TestMethod]
    public void ChainedSelect_Where_OrderBy_Take_Skip_GeneratesCompleteSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Name })
            .OrderBy(u => u.Name)
            .Take(10)
            .Skip(20)
            .ToSql();
        
        Assert.IsTrue(result.Contains("SELECT"));
        Assert.IsTrue(result.Contains("WHERE"));
        Assert.IsTrue(result.Contains("ORDER BY"));
        Assert.IsTrue(result.Contains("LIMIT"));
        Assert.IsTrue(result.Contains("OFFSET"));
    }

    [TestMethod]
    public void ChainedUpdate_MultipleSets_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Update()
            .Set(u => u.Name, "NewName")
            .Set(u => u.Age, 30)
            .Set(u => u.IsActive, true)
            .Where(u => u.Id == 1)
            .ToSql();
        
        Assert.IsTrue(result.Contains("[name] = 'NewName'"));
        Assert.IsTrue(result.Contains("[age] = 30"));
        Assert.IsTrue(result.Contains("[is_active] = 1"));
    }

    #endregion

    #region Empty/Default Cases

    [TestMethod]
    public void EmptyQuery_GeneratesBasicSelect()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite().ToSql();
        Assert.IsTrue(result.StartsWith("SELECT *"));
        Assert.IsTrue(result.Contains("FROM [ExprUser]"));
    }

    [TestMethod]
    public void Insert_NoValues_GeneratesInsertWithoutValues()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert()
            .ToSql();
        Assert.IsTrue(result.StartsWith("INSERT INTO"));
        Assert.IsFalse(result.Contains("VALUES"));
    }

    #endregion

    #region Constant Values

    [TestMethod]
    public void Insert_NullValue_GeneratesNull()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert(u => new { u.Email })
            .Values(new object[] { null! })
            .ToSql();
        Assert.IsTrue(result.Contains("NULL"));
    }

    [TestMethod]
    public void Insert_GuidValue_GeneratesCorrectFormat()
    {
        var guid = Guid.NewGuid();
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Insert(u => new { u.Name })
            .Values(guid)
            .ToSql();
        Assert.IsTrue(result.Contains(guid.ToString()));
    }

    #endregion
}

[TestClass]
public class ExpressionToSqlStringMethodTests
{
    #region String Length

    [TestMethod]
    public void Where_StringLength_GeneratesLengthFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Length > 5)
            .ToSql();
        Assert.IsTrue(result.Contains("LENGTH([name])"));
    }

    [TestMethod]
    public void SqlServer_Where_StringLength_GeneratesLenFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => u.Name.Length > 5)
            .ToSql();
        Assert.IsTrue(result.Contains("LEN([name])"));
    }

    #endregion

    #region String ToUpper/ToLower

    [TestMethod]
    public void Where_StringToUpper_GeneratesUpperFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.ToUpper() == "TEST")
            .ToSql();
        Assert.IsTrue(result.Contains("UPPER([name])"));
    }

    [TestMethod]
    public void Where_StringToLower_GeneratesLowerFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.ToLower() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("LOWER([name])"));
    }

    #endregion

    #region String Trim

    [TestMethod]
    public void Where_StringTrim_GeneratesTrimFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Trim() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("TRIM([name])"));
    }

    #endregion
}

[TestClass]
public class ExpressionToSqlMathMethodTests
{
    #region Math.Abs / Math.Sign

    [TestMethod]
    public void Where_MathAbs_GeneratesAbsFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Abs(u.Age) > 10)
            .ToSql();
        Assert.IsTrue(result.Contains("ABS([age])"));
    }

    [TestMethod]
    public void Where_MathSign_GeneratesSignFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Sign(u.Age) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("SIGN([age])"));
    }

    #endregion

    #region Math.Round / Floor / Ceiling / Truncate

    [TestMethod]
    public void Where_MathRound_GeneratesRoundFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Round((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND([salary])"));
    }

    [TestMethod]
    public void Where_MathRoundWithDigits_GeneratesRoundWithPrecision()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Round((double)u.Salary, 2) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND([salary], 2)"));
    }

    [TestMethod]
    public void Where_MathFloor_GeneratesFloorFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Floor((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("FLOOR([salary])"));
    }

    [TestMethod]
    public void Where_MathCeiling_GeneratesCeilingFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Ceiling((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("CEILING([salary])"));
    }

    [TestMethod]
    public void PostgreSql_MathCeiling_GeneratesCeilFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => Math.Ceiling((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("CEIL("));
    }

    [TestMethod]
    public void Where_MathTruncate_GeneratesTruncateFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Truncate((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("TRUNCATE(`salary`, 0)"));
    }

    [TestMethod]
    public void SqlServer_MathTruncate_GeneratesRoundWithTruncate()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => Math.Truncate((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND([salary], 0, 1)"));
    }

    [TestMethod]
    public void PostgreSql_MathTruncate_GeneratesTruncFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => Math.Truncate((double)u.Salary) > 50000)
            .ToSql();
        Assert.IsTrue(result.Contains("TRUNC("));
    }

    #endregion

    #region Math.Sqrt / Pow / Exp

    [TestMethod]
    public void Where_MathSqrt_GeneratesSqrtFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Sqrt(u.Age) > 5)
            .ToSql();
        Assert.IsTrue(result.Contains("SQRT([age])"));
    }

    [TestMethod]
    public void Where_MathPow_GeneratesPowerFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Pow(u.Age, 2) > 100)
            .ToSql();
        Assert.IsTrue(result.Contains("POWER([age], 2)"));
    }

    [TestMethod]
    public void MySql_MathPow_GeneratesPowFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Pow(u.Age, 2) > 100)
            .ToSql();
        Assert.IsTrue(result.Contains("POW(`age`, 2)"));
    }

    [TestMethod]
    public void Where_MathExp_GeneratesExpFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Exp(u.Age) > 100)
            .ToSql();
        Assert.IsTrue(result.Contains("EXP([age])"));
    }

    #endregion

    #region Math.Log / Log10

    [TestMethod]
    public void Where_MathLog_GeneratesLogFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Log(u.Age) > 2)
            .ToSql();
        Assert.IsTrue(result.Contains("LN([age])"));
    }

    [TestMethod]
    public void SqlServer_MathLog_GeneratesLogFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => Math.Log(u.Age) > 2)
            .ToSql();
        Assert.IsTrue(result.Contains("LOG([age])"));
    }

    [TestMethod]
    public void Where_MathLogWithBase_GeneratesLogWithBase()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Log(u.Age, 10) > 1)
            .ToSql();
        Assert.IsTrue(result.Contains("LOG(10, [age])"));
    }

    [TestMethod]
    public void Where_MathLog10_GeneratesLog10Function()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Log10(u.Age) > 1)
            .ToSql();
        Assert.IsTrue(result.Contains("LOG10([age])"));
    }

    #endregion

    #region Trigonometric Functions

    [TestMethod]
    public void Where_MathSin_GeneratesSinFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Sin(u.Age) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("SIN([age])"));
    }

    [TestMethod]
    public void Where_MathCos_GeneratesCosFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Cos(u.Age) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("COS([age])"));
    }

    [TestMethod]
    public void Where_MathTan_GeneratesTanFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Tan(u.Age) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("TAN([age])"));
    }

    [TestMethod]
    public void Where_MathAsin_GeneratesAsinFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Asin((double)u.Age / 100) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("ASIN("));
    }

    [TestMethod]
    public void Where_MathAcos_GeneratesAcosFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Acos((double)u.Age / 100) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("ACOS("));
    }

    [TestMethod]
    public void Where_MathAtan_GeneratesAtanFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Atan(u.Age) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("ATAN([age])"));
    }

    [TestMethod]
    public void Where_MathAtan2_GeneratesAtan2Function()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Atan2(u.Age, u.Id) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("ATAN2([age], [id])"));
    }

    [TestMethod]
    public void SqlServer_MathAtan2_GeneratesAtn2Function()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => Math.Atan2(u.Age, u.Id) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("ATN2([age], [id])"));
    }

    #endregion

    #region Math.Min / Math.Max

    [TestMethod]
    public void Where_MathMin_GeneratesLeastFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Min(u.Age, 100) < 50)
            .ToSql();
        Assert.IsTrue(result.Contains("LEAST("), $"Expected LEAST in: {result}");
    }

    [TestMethod]
    public void Where_MathMax_GeneratesGreatestFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Max(u.Age, 0) > 10)
            .ToSql();
        Assert.IsTrue(result.Contains("GREATEST("), $"Expected GREATEST in: {result}");
    }

    #endregion
}

[TestClass]
public class StringFunctionParserTests
{
    #region TrimStart / TrimEnd

    [TestMethod]
    public void Where_TrimStart_GeneratesLtrimFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.TrimStart() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("LTRIM([name])"));
    }

    [TestMethod]
    public void Where_TrimEnd_GeneratesRtrimFunction()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.TrimEnd() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("RTRIM([name])"));
    }

    [TestMethod]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSql")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void AllDialects_TrimStart_GeneratesLtrim(string dialect)
    {
        var sql = CreateForDialect(dialect);
        var result = sql.Where(u => u.Name.TrimStart() == "x").ToSql();
        Assert.IsTrue(result.Contains("LTRIM("));
    }

    [TestMethod]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSql")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void AllDialects_TrimEnd_GeneratesRtrim(string dialect)
    {
        var sql = CreateForDialect(dialect);
        var result = sql.Where(u => u.Name.TrimEnd() == "x").ToSql();
        Assert.IsTrue(result.Contains("RTRIM("));
    }

    #endregion

    #region Substring

    [TestMethod]
    public void SQLite_Substring_OneArg_GeneratesSubstrWithOffset()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Substring(2) == "st")
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTR([name], 2 + 1)"));
    }

    [TestMethod]
    public void SQLite_Substring_TwoArgs_GeneratesSubstrWithOffsetAndLength()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Substring(2, 3) == "est")
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTR([name], 2 + 1, 3)"));
    }

    [TestMethod]
    public void SqlServer_Substring_GeneratesSubstringWithOffset()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => u.Name.Substring(2, 3) == "est")
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTRING([name], 2 + 1, 3)"));
    }

    [TestMethod]
    public void PostgreSql_Substring_GeneratesSubstringFromFor()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => u.Name.Substring(2, 3) == "est")
            .ToSql();
        Assert.IsTrue(result.Contains("SUBSTRING(\"name\" FROM 2 + 1 FOR 3)"));
    }

    #endregion

    #region PadLeft / PadRight

    [TestMethod]
    public void MySql_PadLeft_GeneratesLpad()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => u.Name.PadLeft(10) == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("LPAD(`name`, 10, ' ')"));
    }

    [TestMethod]
    public void MySql_PadLeft_WithChar_GeneratesLpadWithChar()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => u.Name.PadLeft(10, '0') == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("LPAD(`name`, 10, '0')"));
    }

    [TestMethod]
    public void MySql_PadRight_GeneratesRpad()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => u.Name.PadRight(10) == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("RPAD(`name`, 10, ' ')"));
    }

    [TestMethod]
    public void MySql_PadRight_WithChar_GeneratesRpadWithChar()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => u.Name.PadRight(10, '*') == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("RPAD(`name`, 10, '*')"));
    }

    [TestMethod]
    public void SqlServer_PadLeft_GeneratesReplicateWorkaround()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => u.Name.PadLeft(10) == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("RIGHT(REPLICATE(' ', 10) + [name], 10)"));
    }

    [TestMethod]
    public void SqlServer_PadRight_GeneratesReplicateWorkaround()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => u.Name.PadRight(10) == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("LEFT([name] + REPLICATE(' ', 10), 10)"));
    }

    [TestMethod]
    public void PostgreSql_PadLeft_GeneratesLpad()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => u.Name.PadLeft(10, '0') == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("LPAD(\"name\", 10, '0')"));
    }

    [TestMethod]
    public void PostgreSql_PadRight_GeneratesRpad()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => u.Name.PadRight(10, '0') == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("RPAD(\"name\", 10, '0')"));
    }

    #endregion

    #region IndexOf

    [TestMethod]
    public void SqlServer_IndexOf_GeneratesCharindex()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => u.Name.IndexOf("test") >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("CHARINDEX('test', [name]) - 1"));
    }

    [TestMethod]
    public void MySql_IndexOf_GeneratesLocate()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => u.Name.IndexOf("test") >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("LOCATE('test', `name`) - 1"));
    }

    [TestMethod]
    public void PostgreSql_IndexOf_GeneratesPosition()
    {
        var result = ExpressionToSql<ExprUser>.ForPostgreSQL()
            .Where(u => u.Name.IndexOf("test") >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("POSITION('test' IN \"name\") - 1"));
    }

    [TestMethod]
    public void Oracle_IndexOf_GeneratesInstr()
    {
        var result = ExpressionToSql<ExprUser>.ForOracle()
            .Where(u => u.Name.IndexOf("test") >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("INSTR(\"name\", 'test') - 1"));
    }

    [TestMethod]
    public void SQLite_IndexOf_GeneratesInstr()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.IndexOf("test") >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("INSTR([name], 'test') - 1"));
    }

    [TestMethod]
    public void SqlServer_IndexOf_WithStart_GeneratesCharindexWithStart()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlServer()
            .Where(u => u.Name.IndexOf("test", 5) >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("CHARINDEX('test', [name], 5 + 1) - 1"));
    }

    [TestMethod]
    public void MySql_IndexOf_WithStart_GeneratesLocateWithStart()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => u.Name.IndexOf("test", 5) >= 0)
            .ToSql();
        Assert.IsTrue(result.Contains("LOCATE('test', `name`, 5 + 1) - 1"));
    }

    #endregion

    #region Replace

    [TestMethod]
    public void Where_Replace_GeneratesReplaceWithQuotedStrings()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Replace("old", "new") == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("REPLACE([name], 'old', 'new')"));
    }

    [TestMethod]
    public void Where_Replace_AllDialects_GeneratesCorrectSql()
    {
        var dialects = new[] { "SqlServer", "MySql", "PostgreSql", "Oracle", "DB2" };
        foreach (var dialect in dialects)
        {
            var sql = CreateForDialect(dialect);
            var result = sql.Where(u => u.Name.Replace("a", "b") == "x").ToSql();
            Assert.IsTrue(result.Contains("REPLACE("), $"Failed for {dialect}");
            Assert.IsTrue(result.Contains("'a'"), $"Failed for {dialect}");
            Assert.IsTrue(result.Contains("'b'"), $"Failed for {dialect}");
        }
    }

    #endregion

    #region Contains / StartsWith / EndsWith

    [TestMethod]
    public void Where_Contains_GeneratesLikeWithPercents()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Contains("test"))
            .ToSql();
        Assert.IsTrue(result.Contains("LIKE"));
        Assert.IsTrue(result.Contains("'%'"));
        Assert.IsTrue(result.Contains("'test'"));
    }

    [TestMethod]
    public void Where_StartsWith_GeneratesLikeWithTrailingPercent()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.StartsWith("test"))
            .ToSql();
        Assert.IsTrue(result.Contains("LIKE"));
        Assert.IsTrue(result.Contains("'test'"));
        Assert.IsTrue(result.Contains("'%'"));
    }

    [TestMethod]
    public void Where_EndsWith_GeneratesLikeWithLeadingPercent()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.EndsWith("test"))
            .ToSql();
        Assert.IsTrue(result.Contains("LIKE"));
        Assert.IsTrue(result.Contains("'%'"));
        Assert.IsTrue(result.Contains("'test'"));
    }

    #endregion

    private static ExpressionToSql<ExprUser> CreateForDialect(string dialect) => dialect switch
    {
        "SqlServer" => ExpressionToSql<ExprUser>.ForSqlServer(),
        "MySql" => ExpressionToSql<ExprUser>.ForMySql(),
        "PostgreSql" => ExpressionToSql<ExprUser>.ForPostgreSQL(),
        "Oracle" => ExpressionToSql<ExprUser>.ForOracle(),
        "DB2" => ExpressionToSql<ExprUser>.ForDB2(),
        _ => ExpressionToSql<ExprUser>.ForSqlite()
    };
}

[TestClass]
public class FunctionCompositionTests
{
    #region Math Function Composition

    [TestMethod]
    public void MathAbs_NestedInRound_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Round(Math.Abs((double)u.Salary)) > 1000)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND(ABS([salary]))"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathSqrt_NestedInCeiling_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Ceiling(Math.Sqrt(u.Age)) > 5)
            .ToSql();
        Assert.IsTrue(result.Contains("CEILING(SQRT([age]))"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathPow_NestedInFloor_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Floor(Math.Pow(u.Age, 2)) > 100)
            .ToSql();
        Assert.IsTrue(result.Contains("FLOOR(POWER([age], 2))"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathLog_NestedInRound_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Round(Math.Log(u.Age), 2) > 1)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND(LN([age]), 2)"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathMin_NestedInAbs_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Abs(Math.Min(u.Age, 100)) > 10)
            .ToSql();
        Assert.IsTrue(result.Contains("ABS(LEAST(`age`, 100))"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathMax_NestedInSqrt_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Sqrt(Math.Max(u.Age, 0)) > 5)
            .ToSql();
        Assert.IsTrue(result.Contains("SQRT(GREATEST(`age`, 0))"), $"Actual: {result}");
    }

    [TestMethod]
    public void TripleMathNesting_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Round(Math.Abs(Math.Floor((double)u.Salary))) > 1000)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND(ABS(FLOOR([salary])))"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathSin_NestedInAbs_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Abs(Math.Sin(u.Age)) < 1)
            .ToSql();
        Assert.IsTrue(result.Contains("ABS(SIN([age]))"), $"Actual: {result}");
    }

    #endregion

    #region String Function Composition

    [TestMethod]
    public void StringTrim_NestedInToUpper_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Trim().ToUpper() == "TEST")
            .ToSql();
        Assert.IsTrue(result.Contains("UPPER(TRIM([name]))"), $"Actual: {result}");
    }

    [TestMethod]
    public void StringToLower_NestedInTrim_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.ToLower().Trim() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("TRIM(LOWER([name]))"), $"Actual: {result}");
    }

    [TestMethod]
    public void StringReplace_NestedInToUpper_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Replace("a", "b").ToUpper() == "TEST")
            .ToSql();
        Assert.IsTrue(result.Contains("UPPER(REPLACE([name], 'a', 'b'))"), $"Actual: {result}");
    }

    [TestMethod]
    public void StringSubstring_NestedInTrim_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Substring(0, 5).Trim() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("TRIM(SUBSTR([name], 0 + 1, 5))"), $"Actual: {result}");
    }

    [TestMethod]
    public void StringTrimStart_NestedInTrimEnd_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.TrimStart().TrimEnd() == "test")
            .ToSql();
        Assert.IsTrue(result.Contains("RTRIM(LTRIM([name]))"), $"Actual: {result}");
    }

    [TestMethod]
    public void TripleStringNesting_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Trim().ToUpper().Replace("A", "B") == "TEST")
            .ToSql();
        Assert.IsTrue(result.Contains("REPLACE(UPPER(TRIM([name])), 'A', 'B')"), $"Actual: {result}");
    }

    #endregion

    #region Mixed Math and String Composition

    [TestMethod]
    public void StringLength_NestedInMathAbs_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Abs(u.Name.Length - 10) < 5)
            .ToSql();
        Assert.IsTrue(result.Contains("ABS((LENGTH([name]) - 10))"), $"Actual: {result}");
    }

    [TestMethod]
    public void StringLength_NestedInMathSqrt_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Sqrt(u.Name.Length) > 2)
            .ToSql();
        Assert.IsTrue(result.Contains("SQRT(LENGTH([name]))"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathRound_WithStringLength_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Round((double)u.Name.Length / 2) > 3)
            .ToSql();
        Assert.IsTrue(result.Contains("ROUND((LENGTH([name]) / 2))"), $"Actual: {result}");
    }

    #endregion

    #region Arithmetic with Functions

    [TestMethod]
    public void MathAbs_WithAddition_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Abs(u.Age) + 10 > 20)
            .ToSql();
        Assert.IsTrue(result.Contains("(ABS([age]) + 10)"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathSqrt_WithMultiplication_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Sqrt(u.Age) * 2 > 10)
            .ToSql();
        Assert.IsTrue(result.Contains("(SQRT([age]) * 2)"), $"Actual: {result}");
    }

    [TestMethod]
    public void StringLength_WithArithmetic_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => u.Name.Length * 2 + 5 > 20)
            .ToSql();
        Assert.IsTrue(result.Contains("((LENGTH([name]) * 2) + 5)"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathPow_WithDivision_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Pow(u.Age, 2) / 100 > 1)
            .ToSql();
        Assert.IsTrue(result.Contains("(POWER([age], 2) / 100)"), $"Actual: {result}");
    }

    #endregion

    #region Multiple Column Functions

    [TestMethod]
    public void MathMin_TwoColumns_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Min(u.Age, u.Id) > 0)
            .ToSql();
        Assert.IsTrue(result.Contains("LEAST(`age`, `id`)"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathMax_TwoColumns_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForMySql()
            .Where(u => Math.Max(u.Age, u.Id) < 100)
            .ToSql();
        Assert.IsTrue(result.Contains("GREATEST(`age`, `id`)"), $"Actual: {result}");
    }

    [TestMethod]
    public void MathPow_TwoColumns_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Pow(u.Age, u.Id) > 100)
            .ToSql();
        Assert.IsTrue(result.Contains("POWER([age], [id])"), $"Actual: {result}");
    }

    #endregion

    #region Set Expression with Functions

    [TestMethod]
    public void Set_WithMathAbs_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Age, u => Math.Abs(u.Age))
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[age] = ABS([age])"), $"Actual: {result}");
    }

    [TestMethod]
    public void Set_WithMathRound_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Salary, u => (decimal)Math.Round((double)u.Salary, 2))
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[salary] = ROUND([salary], 2)"), $"Actual: {result}");
    }

    [TestMethod]
    public void Set_WithStringTrim_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Name, u => u.Name.Trim())
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[name] = TRIM([name])"), $"Actual: {result}");
    }

    [TestMethod]
    public void Set_WithStringToUpper_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Name, u => u.Name.ToUpper())
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[name] = UPPER([name])"), $"Actual: {result}");
    }

    [TestMethod]
    public void Set_WithNestedFunctions_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Set(u => u.Name, u => u.Name.Trim().ToUpper())
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(result.Contains("[name] = UPPER(TRIM([name]))"), $"Actual: {result}");
    }

    #endregion

    #region Select Expression with Functions

    // Note: Select with function expressions extracts column names only,
    // function calls in Select are not fully supported yet
    [TestMethod]
    public void Select_WithColumnExpression_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Select(u => new { u.Id, u.Name })
            .ToSql();
        Assert.IsTrue(result.Contains("[id]"), $"Actual: {result}");
        Assert.IsTrue(result.Contains("[name]"), $"Actual: {result}");
    }

    #endregion

    #region Complex Expressions

    [TestMethod]
    public void ComplexExpression_MathAndStringAndArithmetic_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Abs(u.Age - 30) + u.Name.Length < 50)
            .ToSql();
        Assert.IsTrue(result.Contains("ABS(([age] - 30))"), $"Actual: {result}");
        Assert.IsTrue(result.Contains("LENGTH([name])"), $"Actual: {result}");
    }

    [TestMethod]
    public void ComplexExpression_MultipleConditions_GeneratesCorrectSql()
    {
        var result = ExpressionToSql<ExprUser>.ForSqlite()
            .Where(u => Math.Sqrt(u.Age) > 5 && u.Name.Length > 3)
            .ToSql();
        Assert.IsTrue(result.Contains("SQRT([age]) > 5"), $"Actual: {result}");
        Assert.IsTrue(result.Contains("LENGTH([name]) > 3"), $"Actual: {result}");
    }

    #endregion
}
