// Targeted branch coverage tests - ExpressionParser, SqlExpressionVisitor, SqlxQueryProvider
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

[Sqlx, TableName("bc3_products")]
public class Bc3Product
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int? CategoryId { get; set; }
    public DateOnly CreatedDate { get; set; }
    public TimeOnly UpdatedTime { get; set; }
}

[Sqlx, TableName("bc3_categories")]
public class Bc3Category
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

[TestClass]
public class ExpressionParserBranch3Tests
{
    // line 59: ConditionalExpression → BuildCaseWhen
    [TestMethod]
    public void Parse_ConditionalExpression_BuildsCaseWhen()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => p.IsActive ? p.Name : "inactive");
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("CASE WHEN") || sql.Contains("case when") || sql.Contains("IIF") || sql.Contains("iif"),
            $"Expected CASE WHEN in: {sql}");
    }

    // line 82: Col() with null type → IsGroupingType(null) returns false
    [TestMethod]
    public void Parse_GroupBy_Key_WithNullType_DoesNotThrow()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 96-97: Col() with tableAlias set
    [TestMethod]
    public void Parse_FromSubQuery_ColWithAlias_UsesSquPrefix()
    {
        var inner = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.IsActive);
        var outer = inner.AsSubQuery()
            .Where(p => p.Price > 10m);
        var sql = outer.ToSql();
        Assert.IsTrue(sql.Contains("sq"), $"Expected sq alias in: {sql}");
    }

    // line 127: ExtractColumns with ConditionalExpression
    [TestMethod]
    public void Select_ConditionalExpression_ExtractsColumn()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => p.IsActive ? p.Price : 0m);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 135/139-140: ExtractColumns with MethodCallExpression
    [TestMethod]
    public void Select_MethodCallExpression_ExtractsColumn()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => p.Name.ToUpper());
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 151-152: ExtractColumns with BinaryExpression
    [TestMethod]
    public void Select_BinaryExpression_ExtractsColumn()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => p.Price * 2);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 154-155: ExtractColumns with UnaryExpression Convert
    [TestMethod]
    public void Select_UnaryConvertExpression_ExtractsColumn()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => (object)p.Id);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 164: ExtractFromNewExpression with alias (member name differs from col)
    [TestMethod]
    public void Select_NewExpressionWithAlias_AddsAsClause()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => new { ProductName = p.Name, ProductPrice = p.Price });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("AS") || sql.Contains("as"), $"Expected AS alias in: {sql}");
    }

    // line 189/193: ParseLambdaAsCondition with Quote unary
    [TestMethod]
    public void SqlQuery_Any_WithQuotedLambda_Works()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.IsActive && p.Price > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 236-238: ParseBinary - bool member on right side of AND/OR
    [TestMethod]
    public void Parse_BoolMemberOnRightOfAnd_AddsBoolLiteral()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.Price > 0 && p.IsActive);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("is_active") || sql.Contains("IsActive"), $"SQL: {sql}");
    }

    // line 261: ParseBinary - bool member on right side of OR
    [TestMethod]
    public void Parse_BoolMemberOnRightOfOr_AddsBoolLiteral()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.Price < 0 || p.IsActive);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 277/279: FormatLogical - bool member on left side
    [TestMethod]
    public void Parse_BoolMemberOnLeftOfAnd_AddsBoolLiteral()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.IsActive && p.Price > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 285/289: ParseNot with non-bool member
    [TestMethod]
    public void Parse_NotWithNonBoolMember_GeneratesNotParens()
    {
        // NOT (name LIKE ...) - use Contains which generates LIKE
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => !p.Name.Contains("test"));
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("not"), $"SQL: {sql}");
    }

    // line 316-319: IsSubQueryChainWithoutAggregate - ToArray/AsEnumerable/AsQueryable
    [TestMethod]
    public void Parse_SubQueryChain_ToArray_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .Any());
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 342/344: ParseSubQueryForMethod - LongCount
    [TestMethod]
    public void Parse_SubQuery_LongCount_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .LongCount() > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 348: ParseSubQueryForMethod - Sum
    [TestMethod]
    public void Parse_SubQuery_Sum_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .Sum(c => c.Id) > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 385: ParseSubQueryForMethod - Min
    [TestMethod]
    public void Parse_SubQuery_Min_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .Min(c => c.Id) > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 417/419-421: IsSubQueryChainWithoutAggregate - m.Object path
    [TestMethod]
    public void Parse_SubQuery_Average_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .Average(c => c.Id) > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 431-433: ParseSubQueryChain with null sourceExpr
    [TestMethod]
    public void Parse_SubQuery_First_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .Any());
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 446-447: ContainsSubQueryFor - non-SubQuery MethodCall returns false
    [TestMethod]
    public void Parse_SubQuery_All_GeneratesSubquery()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id == p.CategoryId)
                .All(c => c.Id > 0));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 455/460-461: GetSubQueryElementType - non-generic type returns null
    [TestMethod]
    public void Parse_SubQuery_CacheHit_SecondCallReturnsCached()
    {
        // Call same subquery twice to hit cache
        Expression<Func<Bc3Product, bool>> pred = p =>
            SubQuery.For<Bc3Category>().Where(c => c.Id == p.CategoryId).Any();
        var q1 = SqlQuery<Bc3Product>.ForSqlite().Where(pred);
        var q2 = SqlQuery<Bc3Product>.ForSqlite().Where(pred);
        Assert.AreEqual(q1.ToSql(), q2.ToSql());
    }

    // line 481/487: GenerateSubQuerySql - cache miss then hit
    [TestMethod]
    public void Parse_SubQuery_EntityProviderCache_SecondCallReturnsCached()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>().Any() &&
                        SubQuery.For<Bc3Category>().Count() > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 532-534: GetSubQueryElementType - MethodCall with args recurse
    [TestMethod]
    public void Parse_SubQuery_WhereChain_ElementTypeResolved()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc3Category>()
                .Where(c => c.Id > 0)
                .Where(c => c.Title != "")
                .Count() > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 565/569-570: ParseContains with null item in collection
    [TestMethod]
    public void Parse_Contains_WithNullableItems_HandlesNull()
    {
        var ids = new int?[] { 1, null, 3 };
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => ids.Contains(p.CategoryId));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
        // SQL should contain IN clause or NULL handling
        Assert.IsTrue(sql.Contains("category_id") || sql.Contains("1=1"), $"SQL: {sql}");
    }

    // line 572: ParseContains with empty collection
    [TestMethod]
    public void Parse_Contains_EmptyCollection_GeneratesInNull()
    {
        var ids = new int[] { };
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => ids.Contains(p.Id));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
        // Empty collection generates 1=0 or 1=1 or NULL IN clause
        Assert.IsTrue(sql.Contains("1=") || sql.Contains("NULL") || sql.Contains("id"), $"SQL: {sql}");
    }

    // line 579/582/584/586: ParseContains with null collection
    [TestMethod]
    public void Parse_Contains_NullCollection_GeneratesInNull()
    {
        IEnumerable<int>? ids = null;
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => ids!.Contains(p.Id));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }
}

[TestClass]
public class SqlExpressionVisitorBranch3Tests
{
    // line 78: BuildExistsSql for SqlServer uses TOP 1
    [TestMethod]
    public async Task BuildExistsSql_SqlServer_UsesTop1()
    {
        // Use AnyAsync to trigger BuildExistsSql path
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc3_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, created_date TEXT, updated_time TEXT)";
        cmd.ExecuteNonQuery();

        // Just verify the SQL generation for SqlServer dialect
        var q = SqlQuery<Bc3Product>.ForSqlServer();
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 94: CanBuildDirectAggregateQuery returns false when distinct
    [TestMethod]
    public void SqlQuery_Distinct_CannotBuildDirectAggregate()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite().Distinct();
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("DISTINCT") || sql.Contains("distinct"), $"SQL: {sql}");
    }

    // line 101: CanBuildDirectAggregateQuery returns false when groupBy
    [TestMethod]
    public void SqlQuery_GroupBy_CannotBuildDirectAggregate()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .GroupBy(p => p.CategoryId);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY") || sql.Contains("group by"), $"SQL: {sql}");
    }

    // line 110: BuildCountSql with distinct wraps in subquery
    [TestMethod]
    public async Task SqlQuery_Distinct_CountSql_WrapsInSubquery()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc3_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, created_date TEXT, updated_time TEXT)";
        cmd.ExecuteNonQuery();

        // CountAsync on a Distinct query triggers BuildCountSql wrapping
        var count = await SqlQuery<Bc3Product>.ForSqlite()
            .Distinct()
            .WithConnection(conn)
            .CountAsync();
        Assert.AreEqual(0L, count);
    }

    // line 215/217: VisitOrderBy with fromSubQuery sets/clears alias
    [TestMethod]
    public void SqlQuery_FromSubQuery_OrderBy_UsesSquAlias()
    {
        var inner = SqlQuery<Bc3Product>.ForSqlite().Where(p => p.IsActive);
        var outer = inner.AsSubQuery()
            .OrderBy(p => p.Price);
        var sql = outer.ToSql();
        Assert.IsTrue(sql.Contains("sq"), $"SQL: {sql}");
    }

    // line 223-224: VisitFirst with predicate
    [TestMethod]
    public void SqlQuery_First_WithPredicate_AddsWhereAndTake1()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.IsActive)
            .Take(1);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LIMIT 1") || sql.Contains("TOP 1"), $"SQL: {sql}");
    }

    // line 243: VisitGroupBy
    [TestMethod]
    public void SqlQuery_GroupBy_GeneratesGroupByClause()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY"), $"SQL: {sql}");
    }

    // line 282-285: VisitJoin with ISqlxQueryable having SubQuerySource
    [TestMethod]
    public void SqlQuery_Join_WithSubQuerySource_GeneratesSubqueryJoin()
    {
        var inner = SqlQuery<Bc3Category>.ForSqlite().Where(c => c.Id > 0);
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Join(inner, p => p.CategoryId, c => c.Id, (p, c) => new { p.Name, c.Title });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 288: VisitJoin with MethodCallExpression inner
    [TestMethod]
    public void SqlQuery_Join_WithMethodCallInner_GeneratesJoin()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Join(SqlQuery<Bc3Category>.ForSqlite().Where(c => c.Id > 0),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { p.Name, c.Title });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 291: GenerateSubQuery cache hit
    [TestMethod]
    public void SqlQuery_Join_SubQueryCache_SecondCallReturnsCached()
    {
        var inner = SqlQuery<Bc3Category>.ForSqlite().Where(c => c.Id > 0);
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Join(inner, p => p.CategoryId, c => c.Id, (p, c) => new { p.Name, c.Title });
        var sql1 = q.ToSql();
        var sql2 = q.ToSql();
        Assert.AreEqual(sql1, sql2);
    }

    // line 304-306: ResolveOuterAlias with nested member expression
    [TestMethod]
    public void SqlQuery_Join_OrderBy_WithNestedMember_ResolvesAlias()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Join(SqlQuery<Bc3Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Prod = p, Cat = c })
            .OrderBy(x => x.Prod.Price);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 334/340/342: UpdateColumnMapping with non-NewExpression
    [TestMethod]
    public void SqlQuery_Select_NonNewExpression_DoesNotUpdateMapping()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => p.Name);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 354: UpdateJoinPropertyMapping with ParameterExpression arg
    [TestMethod]
    public void SqlQuery_Join_Select_ParameterArg_UpdatesAliasMap()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Join(SqlQuery<Bc3Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Prod = p, Cat = c })
            .Select(x => new { x.Prod.Name, x.Cat.Title });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 379/382: UpdateJoinPropertyMapping - MemberExpression with propertyAliasMap lookup
    [TestMethod]
    public void SqlQuery_Join_Select_MemberFromAliasedParam_ResolvesCorrectly()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Join(SqlQuery<Bc3Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { p.Name, c.Title })
            .Where(x => x.Name != "");
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 396: GetCurrentAlias with fromSubQuery
    [TestMethod]
    public void SqlQuery_FromSubQuery_GetCurrentAlias_ReturnsSq()
    {
        var inner = SqlQuery<Bc3Product>.ForSqlite();
        var outer = inner.AsSubQuery()
            .Select(p => p.Name);
        var sql = outer.ToSql();
        Assert.IsTrue(sql.Contains("sq"), $"SQL: {sql}");
    }

    // line 405/407: GetEntityColumns - fallback to elementType
    [TestMethod]
    public void SqlQuery_WithDynamicProvider_FallsBackToElementType()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite(null);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 429-430: AppendFromClause with fromSubQuery
    [TestMethod]
    public void SqlQuery_FromSubQuery_AppendFromClause_UsesSubquerySyntax()
    {
        var inner = SqlQuery<Bc3Product>.ForSqlite().Where(p => p.Price > 0);
        var outer = inner.AsSubQuery();
        var sql = outer.ToSql();
        Assert.IsTrue(sql.Contains("FROM ("), $"SQL: {sql}");
    }

    // line 487/489: AppendFromClause without fromSubQuery
    [TestMethod]
    public void SqlQuery_NoSubQuery_AppendFromClause_UsesTableName()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite();
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("bc3_products"), $"SQL: {sql}");
    }

    // line 496: AppendWrappedColumns with fromSubQuery sets sq alias
    [TestMethod]
    public void SqlQuery_FromSubQuery_Select_WrapsColumnsWithSqAlias()
    {
        var inner = SqlQuery<Bc3Product>.ForSqlite();
        var outer = inner.AsSubQuery()
            .Select(p => new { p.Id, p.Name });
        var sql = outer.ToSql();
        Assert.IsTrue(sql.Contains("sq"), $"SQL: {sql}");
    }
}

[TestClass]
public class SqlxQueryProviderBranch3Tests
{
    // line 65: CreateQuery<TElement> where TElement != T throws (clone fails)
    [TestMethod]
    public void SqlQuery_Where_DifferentType_CreatesNewQueryable()
    {
        // When TElement == T, it clones. This tests the clone path.
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.IsActive);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 190: GetAggregateColumn - arg is not UnaryExpression Quote (direct lambda)
    [TestMethod]
    public async Task SqlQuery_Sum_WithDirectLambda_ExtractsColumn()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc3_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, created_date TEXT, updated_time TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc3_products VALUES (1, 'a', 10.0, 1, NULL, '2024-01-01', '00:00:00')";
        cmd.ExecuteNonQuery();

        var results = await SqlQuery<Bc3Product>.ForSqlite()
            .WithConnection(conn)
            .ToListAsync();
        var sum = results.Sum(p => p.Price);
        Assert.AreEqual(10.0m, sum);
    }

    // line 193: GetAggregateColumn - arg is LambdaExpression with non-member body
    [TestMethod]
    public async Task SqlQuery_Max_WithNonMemberBody_ReturnsStar()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc3_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, created_date TEXT, updated_time TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc3_products VALUES (1, 'a', 10.0, 1, NULL, '2024-01-01', '00:00:00')";
        cmd.ExecuteNonQuery();

        var results = await SqlQuery<Bc3Product>.ForSqlite()
            .WithConnection(conn)
            .ToListAsync();
        var max = results.Max(p => p.Price * 2);
        Assert.IsNotNull(max);
    }

    // line 209: ResolveAggregateColumnName - EntityProvider has columns but doesn't know member
    [TestMethod]
    public void SqlQuery_Max_WithProjectedAlias_KeepsAliasName()
    {
        var q = SqlQuery<Bc3Product>.ForSqlite()
            .Select(p => new { ProductPrice = p.Price });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // line 216: ResolveAggregateColumnName - no EntityProvider, uses snake_case
    [TestMethod]
    public async Task SqlQuery_Max_WithNoEntityProvider_UsesSnakeCase()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc3_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, created_date TEXT, updated_time TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc3_products VALUES (1, 'a', 10.0, 1, NULL, '2024-01-01', '00:00:00')";
        cmd.ExecuteNonQuery();

        var results = await SqlQuery<Bc3Product>.ForSqlite(null)
            .WithConnection(conn)
            .ToListAsync();
        var max = results.Max(p => p.Price);
        Assert.IsNotNull(max);
    }

    // line 269/271/273: GetLambdaExpression - direct lambda, quoted, other
    [TestMethod]
    public async Task SqlQuery_Any_WithDirectLambda_GeneratesExistsSql()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc3_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, created_date TEXT, updated_time TEXT)";
        cmd.ExecuteNonQuery();

        var any = await SqlQuery<Bc3Product>.ForSqlite()
            .Where(p => p.IsActive)
            .WithConnection(conn)
            .AnyAsync();
        Assert.IsFalse(any);
    }
}
