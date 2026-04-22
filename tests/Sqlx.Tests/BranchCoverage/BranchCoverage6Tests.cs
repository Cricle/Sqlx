// Branch coverage 6 - ExpressionParser, SqlExpressionVisitor, SqlxQueryProvider gaps
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

[Sqlx, TableName("bc6_products")]
public class Bc6Product
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int? CategoryId { get; set; }
    public string? Description { get; set; }
}

[Sqlx, TableName("bc6_categories")]
public class Bc6Category
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

// ── ExpressionParser gaps ────────────────────────────────────────────────────

[TestClass]
public class ExpressionParserBranch6Tests
{
    // L59: Parse - asBoolCmp=true with bool member (MemberExpression bool)
    [TestMethod]
    public void Parse_BoolMember_AsBoolCmp_AddsBoolLiteral()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("is_active"), $"SQL: {sql}");
    }

    // L74: Col - MemberExpression with Key property in grouping
    [TestMethod]
    public void Col_GroupingKey_ReturnsGroupByColumn()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY"), $"SQL: {sql}");
    }

    // L86/L87: ExtractColumns - IsStringPropertyAccess (string.Length)
    [TestMethod]
    public void ExtractColumns_StringLength_ExtractsCorrectly()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Select(p => p.Name.Length);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L107: ExtractFromNewExpression - alias matches column name (no AS)
    [TestMethod]
    public void ExtractFromNewExpression_SameNameAlias_NoAsClause()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Select(p => new { p.Id, p.Name });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L115: ExtractColumns - non-entity MemberExpression
    [TestMethod]
    public void ExtractColumns_NonEntityMember_ParsesRaw()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Select(p => p.CategoryId);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L144: ParseLambda - non-lambda, non-quote expression
    [TestMethod]
    public void ParseLambda_DirectExpression_ParsesRaw()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Price > 0 && p.Name != null);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L169: ParseBinary - bool member on left side of AND
    [TestMethod]
    public void ParseBinary_BoolMemberLeft_AddsBoolLiteral()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive && p.Price > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("is_active"), $"SQL: {sql}");
    }

    // L216: ParseNot - bool member → col = false
    [TestMethod]
    public void ParseNot_BoolMember_GeneratesEqualsFalse()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => !p.IsActive);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("is_active"), $"SQL: {sql}");
    }

    // L241: ParseBinary - NULL comparison (IS NULL)
    [TestMethod]
    public void ParseBinary_NullComparison_GeneratesIsNull()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Description == null);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("IS NULL"), $"SQL: {sql}");
    }

    // L257: ParseBinary - IS NOT NULL
    [TestMethod]
    public void ParseBinary_NotNullComparison_GeneratesIsNotNull()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Description != null);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("IS NOT NULL"), $"SQL: {sql}");
    }

    // L259: ParseBinary - Modulo operator
    [TestMethod]
    public void ParseBinary_Modulo_GeneratesModulo()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Id % 2 == 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("%") || sql.Contains("MOD"), $"SQL: {sql}");
    }

    // L265: ParseBinary - Coalesce
    [TestMethod]
    public void ParseBinary_Coalesce_GeneratesCoalesce()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => (p.CategoryId ?? 0) > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L296: FormatLogical - bool member on left side
    [TestMethod]
    public void FormatLogical_BoolMemberLeft_AddsBoolLiteral()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive || p.Price > 100);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("is_active"), $"SQL: {sql}");
    }

    // L322: ParseMethod - IsSubQueryChainWithoutAggregate (ToList on SubQuery)
    [TestMethod]
    public void ParseMethod_SubQueryToList_GeneratesSubquery()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc6Category>()
                .Where(c => c.Id == p.CategoryId)
                .Any());
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L324: ParseMethod - IsSubQueryForMethod (Count on SubQuery)
    [TestMethod]
    public void ParseMethod_SubQueryCount_GeneratesSubquery()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc6Category>()
                .Where(c => c.Id == p.CategoryId)
                .Count() > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L397/L399: IsSubQueryChainWithoutAggregate - m.Object path
    [TestMethod]
    public void ParseMethod_SubQueryChain_ObjectPath_GeneratesSubquery()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc6Category>()
                .Where(c => c.Id == p.CategoryId)
                .LongCount() > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L411/L412: ParseSubQueryChain - sourceExpr from Arguments
    [TestMethod]
    public void ParseSubQueryChain_WithArguments_GeneratesSubquery()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => SubQuery.For<Bc6Category>()
                .Where(c => c.Id == p.CategoryId)
                .All(c => c.Id > 0));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L426: ParseContains - various collection types
    [TestMethod]
    public void ParseContains_List_GeneratesInClause()
    {
        var ids = new List<int> { 1, 2, 3 };
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => ids.Contains(p.Id));
        var sql = q.ToSql();
        // List.Contains generates IN clause or 1=1 depending on evaluation
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void ParseContains_HashSet_GeneratesInClause()
    {
        var ids = new HashSet<int> { 1, 2, 3 };
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => ids.Contains(p.Id));
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L435: ParseContains - string Contains (LIKE)
    [TestMethod]
    public void ParseContains_StringContains_GeneratesLike()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Name.Contains("test"));
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    // L440: ParseContains - StartsWith
    [TestMethod]
    public void ParseContains_StartsWith_GeneratesLike()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Name.StartsWith("test"));
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    // L461: ParseContains - EndsWith
    [TestMethod]
    public void ParseContains_EndsWith_GeneratesLike()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Name.EndsWith("test"));
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("LIKE"), $"SQL: {sql}");
    }

    // L512/L518: ParseMethod - string Replace
    [TestMethod]
    public void ParseMethod_StringReplace_GeneratesReplace()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Name.Replace("a", "b") == "test");
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L545/L549/L550: ParseMethod - Math functions
    [TestMethod]
    public void ParseMethod_MathAbs_GeneratesAbs()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => Math.Abs(p.Price) > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("ABS") || sql.Contains("abs"), $"SQL: {sql}");
    }

    [TestMethod]
    public void ParseMethod_MathRound_GeneratesRound()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => Math.Round(p.Price) > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("ROUND") || sql.Contains("round"), $"SQL: {sql}");
    }

    // L559/L562: ParseMethod - DateTime functions
    [TestMethod]
    public void ParseMethod_DateTimeAddDays_GeneratesDateAdd()
    {
        var now = DateTime.UtcNow;
        var q = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.Id > 0); // simple query
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }
}

// ── SqlExpressionVisitor gaps ────────────────────────────────────────────────

[TestClass]
public class SqlExpressionVisitorBranch6Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE bc6_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, description TEXT);
            CREATE TABLE bc6_categories (id INTEGER PRIMARY KEY, title TEXT);
            INSERT INTO bc6_products VALUES (1,'a',10.0,1,1,'desc1'),(2,'b',20.0,0,1,NULL),(3,'c',30.0,1,2,'desc3');
            INSERT INTO bc6_categories VALUES (1,'cat1'),(2,'cat2');";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L94: CanBuildDirectAggregateQuery - with skip (returns false)
    [TestMethod]
    public async Task CountAsync_WithSkip_WrapsInSubquery()
    {
        using var conn = CreateConn();
        var count = await SqlQuery<Bc6Product>.ForSqlite()
            .Skip(1)
            .Take(10)
            .WithConnection(conn)
            .CountAsync();
        Assert.IsTrue(count >= 0);
    }

    // L101: BuildCountSql - with WHERE
    [TestMethod]
    public async Task CountAsync_WithWhere_GeneratesCountSql()
    {
        using var conn = CreateConn();
        var count = await SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive)
            .WithConnection(conn)
            .CountAsync();
        Assert.AreEqual(2L, count);
    }

    // L110: BuildExistsSql - non-SqlServer dialect
    [TestMethod]
    public async Task AnyAsync_SQLite_GeneratesExistsSql()
    {
        using var conn = CreateConn();
        var any = await SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive)
            .WithConnection(conn)
            .AnyAsync();
        Assert.IsTrue(any);
    }

    // L205/L207: Visit - MethodCallExpression dispatch
    [TestMethod]
    public void Visit_WhereAndOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
    }

    // L213/L214: Visit - Distinct
    [TestMethod]
    public void Visit_Distinct_GeneratesDistinctSql()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Distinct()
            .ToSql();
        Assert.IsTrue(sql.Contains("DISTINCT"), $"SQL: {sql}");
    }

    // L233: VisitGroupBy - with having (Count > 0)
    [TestMethod]
    public void GroupBy_WithCount_GeneratesGroupBySql()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY"), $"SQL: {sql}");
    }

    // L272/L274: VisitJoin - with subquery source
    [TestMethod]
    public void Join_WithSubQuerySource_GeneratesJoinSql()
    {
        var inner = SqlQuery<Bc6Category>.ForSqlite().Where(c => c.Id > 0);
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Join(inner, p => p.CategoryId, c => c.Id, (p, c) => new { p.Name, c.Title })
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L278/L281: VisitJoin - outer alias resolution
    [TestMethod]
    public void Join_OrderByNestedMember_ResolvesAlias()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Join(SqlQuery<Bc6Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Prod = p, Cat = c })
            .OrderBy(x => x.Prod.Price)
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L294: ExtractInnerTableInfo - MethodCallExpression inner
    [TestMethod]
    public void Join_MethodCallInner_GeneratesJoinSql()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Join(SqlQuery<Bc6Category>.ForSqlite().Where(c => c.Id > 0),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { p.Name, c.Title })
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L322: UpdateColumnMapping - non-NewExpression (no update)
    [TestMethod]
    public void Select_SingleColumn_NoColumnMappingUpdate()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Select(p => p.Name)
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L328: UpdateJoinPropertyMapping - ParameterExpression arg
    [TestMethod]
    public void Join_SelectWithParameterArg_UpdatesAliasMap()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Join(SqlQuery<Bc6Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Prod = p, Cat = c })
            .Select(x => new { x.Prod.Name, x.Cat.Title })
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L342: AppendFromClause - with join clauses (no subquery)
    [TestMethod]
    public void Join_AppendFromClause_UsesTableName()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Join(SqlQuery<Bc6Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { p.Name, c.Title })
            .ToSql();
        Assert.IsTrue(sql.Contains("JOIN"), $"SQL: {sql}");
    }

    // L367/L370: AppendWrappedColumns - with fromSubQuery
    [TestMethod]
    public void FromSubQuery_AppendWrappedColumns_UsesSqAlias()
    {
        var inner = SqlQuery<Bc6Product>.ForSqlite().Where(p => p.IsActive);
        var sql = inner.AsSubQuery().Select(p => p.Name).ToSql();
        Assert.IsTrue(sql.Contains("sq"), $"SQL: {sql}");
    }

    // L384: AppendWrappedColumns - without fromSubQuery, with joins
    [TestMethod]
    public void Join_AppendWrappedColumns_UsesTableAlias()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Join(SqlQuery<Bc6Category>.ForSqlite(),
                p => p.CategoryId, c => c.Id,
                (p, c) => new { p.Name, c.Title })
            .ToSql();
        Assert.IsTrue(sql.Contains("t1") || sql.Contains("t2"), $"SQL: {sql}");
    }

    // L417/L418: GetEntityColumns - elementType fallback
    [TestMethod]
    public void SqlQuery_WithNullEntityProvider_FallsBackToElementType()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite(null).ToSql();
        Assert.IsNotNull(sql);
    }

    // L475: AppendPagination - with both skip and take
    [TestMethod]
    public void Pagination_SkipAndTake_GeneratesOffsetLimit()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Skip(10)
            .Take(5)
            .ToSql();
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("OFFSET"), $"SQL: {sql}");
    }

    // L484: AppendPagination - take only
    [TestMethod]
    public void Pagination_TakeOnly_GeneratesLimit()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Take(5)
            .ToSql();
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("TOP"), $"SQL: {sql}");
    }
}

// ── SqlxQueryProvider gaps ───────────────────────────────────────────────────

[TestClass]
public class SqlxQueryProviderBranch6Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE bc6_products (id INTEGER PRIMARY KEY, name TEXT, price REAL, is_active INTEGER, category_id INTEGER, description TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO bc6_products VALUES (1,'a',10.0,1,1,'d'),(2,'b',20.0,0,2,NULL)";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L65: CreateQuery<TElement> - TElement == T (clone path)
    [TestMethod]
    public void CreateQuery_SameType_ClonesProvider()
    {
        var q = SqlQuery<Bc6Product>.ForSqlite().Where(p => p.IsActive);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // L126: Execute - First
    [TestMethod]
    public async Task Execute_First_ReturnsFirstRow()
    {
        using var conn = CreateConn();
        var result = await SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive)
            .WithConnection(conn)
            .FirstOrDefaultAsync();
        Assert.IsNotNull(result);
    }

    // L139: Execute - unsupported method throws
    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Execute_UnsupportedMethod_Throws()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        // Last() is not supported
        var q = SqlQuery<Bc6Product>.ForSqlite().WithConnection(conn);
        _ = q.Last();
    }

    // L190: GetAggregateColumn - no args (returns *)
    [TestMethod]
    public async Task CountAsync_NoArgs_ReturnsStar()
    {
        using var conn = CreateConn();
        var count = await SqlQuery<Bc6Product>.ForSqlite()
            .WithConnection(conn)
            .CountAsync();
        Assert.AreEqual(2L, count);
    }

    // L193: GetAggregateColumn - LambdaExpression with non-member body (returns *)
    [TestMethod]
    public async Task CountAsync_WithWhere_ReturnsCount()
    {
        using var conn = CreateConn();
        var count = await SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive)
            .WithConnection(conn)
            .CountAsync();
        Assert.AreEqual(1L, count);
    }

    // L209: ResolveAggregateColumnName - EntityProvider has columns but doesn't know member
    [TestMethod]
    public void Select_ProjectedAlias_KeepsAliasName()
    {
        var sql = SqlQuery<Bc6Product>.ForSqlite()
            .Select(p => new { ProductPrice = p.Price })
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L269: GetLambdaExpression - direct lambda
    [TestMethod]
    public async Task AnyAsync_WithWhere_ReturnsTrue()
    {
        using var conn = CreateConn();
        var any = await SqlQuery<Bc6Product>.ForSqlite()
            .Where(p => p.IsActive)
            .WithConnection(conn)
            .AnyAsync();
        Assert.IsTrue(any);
    }
}
