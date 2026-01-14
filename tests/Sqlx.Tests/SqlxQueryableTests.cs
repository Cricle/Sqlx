using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests;

public class QueryUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public decimal Salary { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Email { get; set; }
}

[TestClass]
public class SqlxQueryableTests
{
    #region Interface Implementation

    [TestMethod]
    public void SqlxQueryable_ImplementsIQueryable()
    {
        var query = SqlQuery.ForSqlite<QueryUser>();
        Assert.IsInstanceOfType(query, typeof(IQueryable<QueryUser>));
    }

    [TestMethod]
    public void SqlxQueryable_ImplementsIOrderedQueryable()
    {
        var query = SqlQuery.ForSqlite<QueryUser>().OrderBy(u => u.Name);
        Assert.IsInstanceOfType(query, typeof(IOrderedQueryable<QueryUser>));
    }

    [TestMethod]
    public void SqlxQueryable_ElementType_ReturnsCorrectType()
    {
        var query = SqlQuery.ForSqlite<QueryUser>();
        Assert.AreEqual(typeof(QueryUser), query.ElementType);
    }

    [TestMethod]
    public void SqlxQueryable_Expression_IsNotNull()
    {
        var query = SqlQuery.ForSqlite<QueryUser>();
        Assert.IsNotNull(query.Expression);
    }

    [TestMethod]
    public void SqlxQueryable_Provider_IsNotNull()
    {
        var query = SqlQuery.ForSqlite<QueryUser>();
        Assert.IsNotNull(query.Provider);
        Assert.IsInstanceOfType(query.Provider, typeof(SqlxQueryProvider));
    }

    #endregion

    #region Factory Methods

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void SqlQuery_AllDialects_ReturnsInstance(string dialect)
    {
        var query = dialect switch
        {
            "SQLite" => SqlQuery.ForSqlite<QueryUser>(),
            "SqlServer" => SqlQuery.ForSqlServer<QueryUser>(),
            "MySql" => SqlQuery.ForMySql<QueryUser>(),
            "PostgreSQL" => SqlQuery.ForPostgreSQL<QueryUser>(),
            "Oracle" => SqlQuery.ForOracle<QueryUser>(),
            "DB2" => SqlQuery.ForDB2<QueryUser>(),
            _ => throw new ArgumentException()
        };
        Assert.IsNotNull(query);
    }

    [TestMethod]
    public void SqlQuery_For_WithDialect_ReturnsInstance()
    {
        var query = SqlQuery.For<QueryUser>(SqlDefine.SQLite);
        Assert.IsNotNull(query);
    }

    #endregion

    #region Method Chaining

    [TestMethod]
    public void SqlxQueryable_MethodChaining_PreservesExpression()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10);

        Assert.IsNotNull(query.Expression);
        var sql = query.ToSql();
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    [TestMethod]
    public void SqlxQueryable_MultipleWhere_ChainsCorrectly()
    {
        var query = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .Where(u => u.Age > 18);

        var sql = query.ToSql();
        Assert.IsTrue(sql.Contains("AND"));
    }

    #endregion

    #region ToSql Basic

    [TestMethod]
    public void ToSql_NoConditions_ReturnsSelectStar()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().ToSql();
        Assert.IsTrue(sql.Contains("SELECT *"));
        Assert.IsTrue(sql.Contains("FROM [QueryUser]"));
    }

    [TestMethod]
    public void ToSql_WithWhere_ReturnsWhereClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[id] = 1"));
    }

    [TestMethod]
    public void ToSql_WithOrderBy_ReturnsOrderByClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .OrderBy(u => u.Name)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY [name] ASC"));
    }

    [TestMethod]
    public void ToSql_WithOrderByDescending_ReturnsDescClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .OrderByDescending(u => u.CreatedAt)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY [created_at] DESC"));
    }

    [TestMethod]
    public void ToSql_WithTake_ReturnsLimitClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Take(10)
            .ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void ToSql_WithSkip_ReturnsOffsetClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Skip(20)
            .ToSql();
        Assert.IsTrue(sql.Contains("OFFSET 20"));
    }

    [TestMethod]
    public void ToSql_WithTakeAndSkip_ReturnsBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Take(10)
            .Skip(20)
            .ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"));
        Assert.IsTrue(sql.Contains("OFFSET 20"));
    }

    #endregion

    #region Dialect-Specific

    [TestMethod]
    public void ToSql_SqlServer_UsesSquareBrackets()
    {
        var sql = SqlQuery.ForSqlServer<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(sql.Contains("[QueryUser]"));
        Assert.IsTrue(sql.Contains("[id]"));
    }

    [TestMethod]
    public void ToSql_MySql_UsesBackticks()
    {
        var sql = SqlQuery.ForMySql<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(sql.Contains("`QueryUser`"));
        Assert.IsTrue(sql.Contains("`id`"));
    }

    [TestMethod]
    public void ToSql_PostgreSQL_UsesDoubleQuotes()
    {
        var sql = SqlQuery.ForPostgreSQL<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSql();
        Assert.IsTrue(sql.Contains("\"QueryUser\""));
        Assert.IsTrue(sql.Contains("\"id\""));
    }

    [TestMethod]
    public void ToSql_SqlServer_Pagination_UsesOffsetFetch()
    {
        var sql = SqlQuery.ForSqlServer<QueryUser>()
            .Take(10)
            .Skip(20)
            .ToSql();
        Assert.IsTrue(sql.Contains("OFFSET 20 ROWS"));
        Assert.IsTrue(sql.Contains("FETCH NEXT 10 ROWS ONLY"));
    }

    #endregion

    #region Error Handling

    [TestMethod]
    public void GetEnumerator_ThrowsNotSupportedException()
    {
        var query = SqlQuery.ForSqlite<QueryUser>();
        Assert.ThrowsException<NotSupportedException>(() => query.GetEnumerator());
    }

    [TestMethod]
    public void ToSql_OnNonSqlxQueryable_ThrowsInvalidOperationException()
    {
        var list = new[] { new QueryUser() }.AsQueryable();
        Assert.ThrowsException<InvalidOperationException>(() => list.ToSql());
    }

    #endregion
}


[TestClass]
public class SqlxQueryableWhereTests
{
    #region Basic Where Conditions

    [TestMethod]
    public void Where_Equality_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("[id] = 1"));
    }

    [TestMethod]
    public void Where_StringEquality_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name == "test").ToSql();
        Assert.IsTrue(sql.Contains("[name] = 'test'"));
    }

    [TestMethod]
    public void Where_GreaterThan_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Age > 18).ToSql();
        Assert.IsTrue(sql.Contains("[age] > 18"));
    }

    [TestMethod]
    public void Where_LessThan_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Age < 65).ToSql();
        Assert.IsTrue(sql.Contains("[age] < 65"));
    }

    [TestMethod]
    public void Where_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Age >= 18).ToSql();
        Assert.IsTrue(sql.Contains("[age] >= 18"));
    }

    [TestMethod]
    public void Where_LessThanOrEqual_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Age <= 65).ToSql();
        Assert.IsTrue(sql.Contains("[age] <= 65"));
    }

    [TestMethod]
    public void Where_NotEqual_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Id != 0).ToSql();
        Assert.IsTrue(sql.Contains("[id] <> 0"));
    }

    #endregion

    #region Logical Operators

    [TestMethod]
    public void Where_AndCondition_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Age >= 18 && u.Age <= 65).ToSql();
        Assert.IsTrue(sql.Contains("AND"));
    }

    [TestMethod]
    public void Where_OrCondition_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Age < 18 || u.Age > 65).ToSql();
        Assert.IsTrue(sql.Contains("OR"));
    }

    [TestMethod]
    public void Where_MultipleWhere_GeneratesAndClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .Where(u => u.Age > 18)
            .ToSql();
        Assert.IsTrue(sql.Contains("AND"));
    }

    #endregion

    #region Null Handling

    [TestMethod]
    public void Where_NullEquality_GeneratesIsNull()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Email == null).ToSql();
        Assert.IsTrue(sql.Contains("IS NULL"));
    }

    [TestMethod]
    public void Where_NotNull_GeneratesIsNotNull()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Email != null).ToSql();
        Assert.IsTrue(sql.Contains("IS NOT NULL"));
    }

    #endregion

    #region Boolean Handling

    [TestMethod]
    public void Where_BooleanTrue_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.IsActive == true).ToSql();
        Assert.IsTrue(sql.Contains("[is_active] = 1"));
    }

    [TestMethod]
    public void Where_BooleanFalse_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.IsActive == false).ToSql();
        Assert.IsTrue(sql.Contains("[is_active] = 0"));
    }

    [TestMethod]
    public void Where_BooleanMember_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.IsActive).ToSql();
        Assert.IsTrue(sql.Contains("[is_active] = 1"));
    }

    #endregion

    #region String Methods

    [TestMethod]
    public void Where_StringContains_GeneratesLike()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.Contains("test")).ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringStartsWith_GeneratesLike()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.StartsWith("test")).ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringEndsWith_GeneratesLike()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.EndsWith("test")).ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringLength_GeneratesLengthFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.Length > 5).ToSql();
        Assert.IsTrue(sql.Contains("LENGTH([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringToUpper_GeneratesUpperFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.ToUpper() == "TEST").ToSql();
        Assert.IsTrue(sql.Contains("UPPER([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringToLower_GeneratesLowerFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.ToLower() == "test").ToSql();
        Assert.IsTrue(sql.Contains("LOWER([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Where_StringTrim_GeneratesTrimFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.Trim() == "test").ToSql();
        Assert.IsTrue(sql.Contains("TRIM([name])"), $"SQL: {sql}");
    }

    #endregion

    #region Math Methods

    [TestMethod]
    public void Where_MathAbs_GeneratesAbsFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => Math.Abs(u.Age) > 0).ToSql();
        Assert.IsTrue(sql.Contains("ABS([age])"));
    }

    [TestMethod]
    public void Where_MathRound_GeneratesRoundFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => Math.Round((double)u.Salary) > 1000).ToSql();
        Assert.IsTrue(sql.Contains("ROUND([salary])"));
    }

    [TestMethod]
    public void Where_MathFloor_GeneratesFloorFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => Math.Floor((double)u.Salary) > 1000).ToSql();
        Assert.IsTrue(sql.Contains("FLOOR([salary])"));
    }

    [TestMethod]
    public void Where_MathCeiling_GeneratesCeilingFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => Math.Ceiling((double)u.Salary) > 1000).ToSql();
        Assert.IsTrue(sql.Contains("CEILING([salary])"));
    }

    #endregion

    #region Coalesce and Conditional

    [TestMethod]
    public void Where_NullCoalescing_GeneratesCoalesce()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => (u.Email ?? "default") == "test").ToSql();
        Assert.IsTrue(sql.Contains("COALESCE"));
    }

    [TestMethod]
    public void Where_Conditional_GeneratesCaseWhen()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => (u.Age > 18 ? "adult" : "minor") == "adult").ToSql();
        Assert.IsTrue(sql.Contains("CASE WHEN"));
    }

    #endregion
}


[TestClass]
public class SqlxQueryableSelectTests
{
    #region Basic Select

    [TestMethod]
    public void Select_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => u.Name).ToSql();
        Assert.IsTrue(sql.Contains("[name]"), $"SQL: {sql}");
        Assert.IsFalse(sql.Contains("*"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_MultipleColumns_AnonymousType_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => new { u.Id, u.Name }).ToSql();
        Assert.IsTrue(sql.Contains("[id]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[name]"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_AllColumns_WithoutSelect_GeneratesSelectStar()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().ToSql();
        Assert.IsTrue(sql.Contains("SELECT *"), $"SQL: {sql}");
    }

    #endregion

    #region Select with Functions

    [TestMethod]
    public void Select_StringToUpper_GeneratesUpperFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => u.Name.ToUpper()).ToSql();
        Assert.IsTrue(sql.Contains("UPPER([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_StringToLower_GeneratesLowerFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => u.Name.ToLower()).ToSql();
        Assert.IsTrue(sql.Contains("LOWER([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_StringLength_GeneratesLengthFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => u.Name.Length).ToSql();
        Assert.IsTrue(sql.Contains("LENGTH([name])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_MathAbs_GeneratesAbsFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => Math.Abs(u.Age)).ToSql();
        Assert.IsTrue(sql.Contains("ABS([age])"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_MathRound_GeneratesRoundFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => Math.Round((double)u.Salary)).ToSql();
        Assert.IsTrue(sql.Contains("ROUND([salary])"), $"SQL: {sql}");
    }

    #endregion

    #region Select with Expressions

    [TestMethod]
    public void Select_Coalesce_GeneratesCoalesceFunction()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => u.Email ?? "N/A").ToSql();
        Assert.IsTrue(sql.Contains("COALESCE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Select_Conditional_GeneratesCaseWhen()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Select(u => u.Age > 18 ? "Adult" : "Minor").ToSql();
        Assert.IsTrue(sql.Contains("CASE WHEN"), $"SQL: {sql}");
    }

    #endregion

    #region Select with Where

    [TestMethod]
    public void Select_WithWhere_GeneratesBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Name })
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[id]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[name]"), $"SQL: {sql}");
    }

    #endregion
}



[TestClass]
public class SqlxQueryableOrderByTests
{
    #region Basic OrderBy

    [TestMethod]
    public void OrderBy_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().OrderBy(u => u.Name).ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY [name] ASC"), $"SQL: {sql}");
    }

    [TestMethod]
    public void OrderByDescending_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().OrderByDescending(u => u.CreatedAt).ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY [created_at] DESC"), $"SQL: {sql}");
    }

    [TestMethod]
    public void ThenBy_AfterOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Age)
            .ToSql();
        Assert.IsTrue(sql.Contains("[name] ASC"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[age] ASC"), $"SQL: {sql}");
    }

    [TestMethod]
    public void ThenByDescending_AfterOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.CreatedAt)
            .ToSql();
        Assert.IsTrue(sql.Contains("[name] ASC"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[created_at] DESC"), $"SQL: {sql}");
    }

    [TestMethod]
    public void OrderBy_MultipleColumns_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Age)
            .ThenByDescending(u => u.CreatedAt)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[name] ASC"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[age] ASC"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[created_at] DESC"), $"SQL: {sql}");
    }

    #endregion

    #region OrderBy with Where

    [TestMethod]
    public void OrderBy_WithWhere_GeneratesBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
    }

    #endregion
}


[TestClass]
public class SqlxQueryablePaginationTests
{
    #region Basic Pagination

    [TestMethod]
    public void Take_GeneratesLimitClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Take(10).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Skip_GeneratesOffsetClause()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Skip(20).ToSql();
        Assert.IsTrue(sql.Contains("OFFSET 20"), $"SQL: {sql}");
    }

    [TestMethod]
    public void TakeAndSkip_GeneratesBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Take(10).Skip(20).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 20"), $"SQL: {sql}");
    }

    [TestMethod]
    public void SkipAndTake_GeneratesBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Skip(20).Take(10).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 20"), $"SQL: {sql}");
    }

    #endregion

    #region Dialect-Specific Pagination

    [TestMethod]
    public void SqlServer_Pagination_UsesOffsetFetch()
    {
        var sql = SqlQuery.ForSqlServer<QueryUser>().Skip(20).Take(10).ToSql();
        Assert.IsTrue(sql.Contains("OFFSET 20 ROWS"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH NEXT 10 ROWS ONLY"), $"SQL: {sql}");
    }

    [TestMethod]
    public void MySql_Pagination_UsesLimitOffset()
    {
        var sql = SqlQuery.ForMySql<QueryUser>().Skip(20).Take(10).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 20"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PostgreSQL_Pagination_UsesLimitOffset()
    {
        var sql = SqlQuery.ForPostgreSQL<QueryUser>().Skip(20).Take(10).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 10"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 20"), $"SQL: {sql}");
    }

    #endregion

    #region Pagination with Where and OrderBy

    [TestMethod]
    public void Pagination_WithWhereAndOrderBy_GeneratesAllClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Skip(20)
            .Take(10)
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("LIMIT 10"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 20"), $"SQL: {sql}");
    }

    #endregion
}



[TestClass]
public class SqlxQueryableGroupByTests
{
    #region Basic GroupBy

    [TestMethod]
    public void GroupBy_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().GroupBy(u => u.IsActive).ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY [is_active]"), $"SQL: {sql}");
    }

    [TestMethod]
    public void GroupBy_WithWhere_GeneratesBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.Age > 18)
            .GroupBy(u => u.IsActive)
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("GROUP BY"), $"SQL: {sql}");
    }

    [TestMethod]
    public void GroupBy_WithSelect_GeneratesBothClauses()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>()
            .Select(u => new { u.IsActive, u.Age })
            .GroupBy(u => u.IsActive)
            .ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY"), $"SQL: {sql}");
    }

    #endregion
}



[TestClass]
public class SqlxQueryableDialectTests
{
    #region Quote Styles

    [TestMethod]
    public void SQLite_UsesSquareBrackets()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("[QueryUser]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[id]"), $"SQL: {sql}");
    }

    [TestMethod]
    public void SqlServer_UsesSquareBrackets()
    {
        var sql = SqlQuery.ForSqlServer<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("[QueryUser]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("[id]"), $"SQL: {sql}");
    }

    [TestMethod]
    public void MySql_UsesBackticks()
    {
        var sql = SqlQuery.ForMySql<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("`QueryUser`"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("`id`"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PostgreSQL_UsesDoubleQuotes()
    {
        var sql = SqlQuery.ForPostgreSQL<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("\"QueryUser\""), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"id\""), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_UsesDoubleQuotes()
    {
        var sql = SqlQuery.ForOracle<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("\"QueryUser\""), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"id\""), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_UsesDoubleQuotes()
    {
        var sql = SqlQuery.ForDB2<QueryUser>().Where(u => u.Id == 1).ToSql();
        Assert.IsTrue(sql.Contains("\"QueryUser\""), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"id\""), $"SQL: {sql}");
    }

    #endregion

    #region Boolean Literals

    [TestMethod]
    public void SQLite_BooleanUsesNumeric()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.IsActive == true).ToSql();
        Assert.IsTrue(sql.Contains("= 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PostgreSQL_BooleanUsesLiteral()
    {
        var sql = SqlQuery.ForPostgreSQL<QueryUser>().Where(u => u.IsActive == true).ToSql();
        Assert.IsTrue(sql.Contains("= true") || sql.Contains("= 1"), $"SQL: {sql}");
    }

    #endregion

    #region Pagination Syntax

    [TestMethod]
    public void SQLite_Pagination_UsesLimitOffset()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Skip(10).Take(20).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 20"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 10"), $"SQL: {sql}");
    }

    [TestMethod]
    public void SqlServer_Pagination_UsesOffsetFetch()
    {
        var sql = SqlQuery.ForSqlServer<QueryUser>().Skip(10).Take(20).ToSql();
        Assert.IsTrue(sql.Contains("OFFSET 10 ROWS"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH NEXT 20 ROWS ONLY"), $"SQL: {sql}");
    }

    [TestMethod]
    public void MySql_Pagination_UsesLimitOffset()
    {
        var sql = SqlQuery.ForMySql<QueryUser>().Skip(10).Take(20).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 20"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 10"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PostgreSQL_Pagination_UsesLimitOffset()
    {
        var sql = SqlQuery.ForPostgreSQL<QueryUser>().Skip(10).Take(20).ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 20"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET 10"), $"SQL: {sql}");
    }

    #endregion

    #region String Concatenation

    [TestMethod]
    public void SQLite_StringConcat_UsesPipeOperator()
    {
        var sql = SqlQuery.ForSqlite<QueryUser>().Where(u => u.Name.Contains("test")).ToSql();
        Assert.IsTrue(sql.Contains("||"), $"SQL: {sql}");
    }

    [TestMethod]
    public void MySql_StringConcat_UsesConcatFunction()
    {
        var sql = SqlQuery.ForMySql<QueryUser>().Where(u => u.Name.Contains("test")).ToSql();
        Assert.IsTrue(sql.Contains("CONCAT("), $"SQL: {sql}");
    }

    #endregion
}



[TestClass]
public class SqlxQueryableParameterizedTests
{
    #region Basic Parameterized Queries

    [TestMethod]
    public void ToSqlWithParameters_ReturnsParameterizedSql()
    {
        var (sql, parameters) = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSqlWithParameters();
        
        Assert.IsTrue(sql.Contains("@p"), $"SQL: {sql}");
        Assert.IsTrue(parameters.Count > 0, "Should have parameters");
    }

    [TestMethod]
    public void ToSqlWithParameters_StringValue_CreatesParameter()
    {
        var (sql, parameters) = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.Name == "test")
            .ToSqlWithParameters();
        
        Assert.IsTrue(sql.Contains("@p"), $"SQL: {sql}");
        Assert.IsTrue(parameters.ContainsValue("test"), "Should contain 'test' value");
    }

    [TestMethod]
    public void ToSqlWithParameters_MultipleConditions_CreatesMultipleParameters()
    {
        var (sql, parameters) = SqlQuery.ForSqlite<QueryUser>()
            .Where(u => u.Id == 1 && u.Name == "test")
            .ToSqlWithParameters();
        
        Assert.IsTrue(parameters.Count >= 2, $"Should have at least 2 parameters, got {parameters.Count}");
    }

    #endregion

    #region Dialect-Specific Parameter Prefixes

    [TestMethod]
    public void SqlServer_UsesAtPrefix()
    {
        var (sql, _) = SqlQuery.ForSqlServer<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSqlWithParameters();
        
        Assert.IsTrue(sql.Contains("@p"), $"SQL: {sql}");
    }

    [TestMethod]
    public void MySql_UsesAtPrefix()
    {
        var (sql, _) = SqlQuery.ForMySql<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSqlWithParameters();
        
        Assert.IsTrue(sql.Contains("@p"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PostgreSQL_UsesDollarPrefix()
    {
        var (sql, _) = SqlQuery.ForPostgreSQL<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSqlWithParameters();
        
        Assert.IsTrue(sql.Contains("$p"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_UsesColonPrefix()
    {
        var (sql, _) = SqlQuery.ForOracle<QueryUser>()
            .Where(u => u.Id == 1)
            .ToSqlWithParameters();
        
        Assert.IsTrue(sql.Contains(":p"), $"SQL: {sql}");
    }

    #endregion
}
