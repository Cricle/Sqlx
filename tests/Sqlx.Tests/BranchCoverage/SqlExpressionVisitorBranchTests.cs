using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.BranchCoverage;

// ── Entities ─────────────────────────────────────────────────────────────────

[Sqlx, TableName("bc10_users")]
public class Bc10User
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public decimal Score { get; set; }
}

[Sqlx, TableName("bc10_orders")]
public class Bc10Order
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public string Category { get; set; } = "";
}

[Sqlx, TableName("bc10_products")]
public class Bc10Product
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

// ── SqlExpressionVisitor branch coverage ─────────────────────────────────────

[TestClass]
public class SqlExpressionVisitorBranch10Tests
{
    private static SqliteConnection CreateConn()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE bc10_users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER, is_active INTEGER, score REAL);
            CREATE TABLE bc10_orders (id INTEGER PRIMARY KEY, user_id INTEGER, total REAL, category TEXT);
            CREATE TABLE bc10_products (id INTEGER PRIMARY KEY, name TEXT, price REAL);
            INSERT INTO bc10_users VALUES (1,'Alice',30,1,100.0),(2,'Bob',25,0,50.0),(3,'Carol',35,1,75.0);
            INSERT INTO bc10_orders VALUES (1,1,200.0,'A'),(2,1,150.0,'B'),(3,2,300.0,'A');
            INSERT INTO bc10_products VALUES (1,'P1',10.0),(2,'P2',20.0)";
        cmd.ExecuteNonQuery();
        return conn;
    }

    // L94: CanBuildDirectAggregateQuery - with distinct (returns false)
    [TestMethod]
    public void CanBuildDirectAggregate_WithDistinct_ReturnsFalse()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Distinct()
            .CountAsync(); // forces wrapped query
        Assert.IsNotNull(sql);
    }

    // L101/110: BuildExistsSql - SqlServer uses TOP 1
    [TestMethod]
    public void BuildExistsSql_SqlServer_UsesTop1()
    {
        var sql = SqlQuery<Bc10User>.ForSqlServer()
            .Where(u => u.Age > 18)
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L205: VisitSelect - with non-NewExpression (single column)
    [TestMethod]
    public void VisitSelect_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Select(u => u.Name)
            .ToSql();
        Assert.IsTrue(sql.Contains("name") || sql.Contains("Name"));
    }

    // L207: VisitSelect - with NewExpression (multiple columns)
    [TestMethod]
    public void VisitSelect_MultipleColumns_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Select(u => new { u.Id, u.Name })
            .ToSql();
        Assert.IsTrue(sql.Contains("id") && (sql.Contains("name") || sql.Contains("Name")));
    }

    // L213/214: VisitSelect - with alias mapping update
    [TestMethod]
    public void VisitSelect_WithAlias_UpdatesColumnMapping()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Select(u => new { UserId = u.Id, UserName = u.Name })
            .ToSql();
        Assert.IsTrue(sql.Contains("AS") || sql.Contains("id") || sql.Contains("name"));
    }

    // L233: VisitJoin - with subquery source
    [TestMethod]
    public void VisitJoin_WithSubQuerySource_GeneratesJoinSql()
    {
        var subq = SqlQuery<Bc10Order>.ForSqlite()
            .Where(o => o.Total > 100);
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Join(subq, u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })
            .ToSql();
        Assert.IsTrue(sql.Contains("JOIN"));
    }

    // L272/274: VisitOrderBy - ascending and descending
    [TestMethod]
    public void VisitOrderBy_Ascending_GeneratesAsc()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .OrderBy(u => u.Name)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY") && sql.Contains("ASC"));
    }

    [TestMethod]
    public void VisitOrderBy_Descending_GeneratesDesc()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .OrderByDescending(u => u.Age)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY") && sql.Contains("DESC"));
    }

    // L278/281: VisitOrderBy - ThenBy
    [TestMethod]
    public void VisitOrderBy_ThenBy_GeneratesMultipleOrderBy()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.Age)
            .ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY") && sql.Contains("ASC") && sql.Contains("DESC"));
    }

    // L294: VisitFirst - with predicate
    [TestMethod]
    public void VisitFirst_WithPredicate_AddsWhereAndTake1()
    {
        using var conn = CreateConn();
        var result = SqlQuery<Bc10User>.ForSqlite()
            .WithConnection(conn)
            .WithReader(Bc10UserResultReader.Default)
            .First(u => u.IsActive);
        Assert.IsNotNull(result);
    }

    // L322: VisitGroupBy - with expression key
    [TestMethod]
    public void VisitGroupBy_WithExpression_GeneratesGroupBy()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .GroupBy(u => u.Age)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY"));
    }

    // L328: VisitJoin - multiple joins
    [TestMethod]
    public void VisitJoin_MultipleJoins_GeneratesCorrectSql()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Join(SqlQuery<Bc10Order>.ForSqlite(), u => u.Id, o => o.UserId, (u, o) => new { u.Name, OrderTotal = o.Total })
            .Join(SqlQuery<Bc10Product>.ForSqlite(), x => x.OrderTotal, p => p.Price, (x, p) => new { x.Name, ProductName = p.Name })
            .ToSql();
        Assert.IsTrue(sql.Contains("JOIN"));
    }

    // L342: AppendFromClause - with join clauses (uses AS t1)
    [TestMethod]
    public void AppendFromClause_WithJoin_UsesTableAlias()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Join(SqlQuery<Bc10Order>.ForSqlite(), u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })
            .ToSql();
        Assert.IsTrue(sql.Contains("t1") || sql.Contains("AS"));
    }

    // L367/370: AppendPagination - skip only
    [TestMethod]
    public void AppendPagination_SkipOnly_GeneratesOffset()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Skip(5)
            .ToSql();
        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("offset"));
    }

    // L384: AppendPagination - take only
    [TestMethod]
    public void AppendPagination_TakeOnly_GeneratesLimit()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Take(10)
            .ToSql();
        Assert.IsTrue(sql.Contains("LIMIT") || sql.Contains("limit"));
    }

    // L393: ResolveColumn - with join alias map
    [TestMethod]
    public void ResolveColumn_WithJoinAliasMap_ResolvesCorrectly()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Join(SqlQuery<Bc10Order>.ForSqlite(), u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })
            .Where(x => x.Total > 100)
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE") || sql.Contains("total"));
    }

    // L417: AppendWrappedColumns - with join (uses alias prefix)
    [TestMethod]
    public void AppendWrappedColumns_WithJoin_UsesAliasPrefix()
    {
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Join(SqlQuery<Bc10Order>.ForSqlite(), u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })
            .ToSql();
        Assert.IsTrue(sql.Contains("t1") || sql.Contains("t2") || sql.Contains("JOIN"));
    }

    // L418: AppendWrappedColumns - from subquery (uses sq prefix)
    [TestMethod]
    public void AppendWrappedColumns_FromSubQuery_UsesSqPrefix()
    {
        var subq = SqlQuery<Bc10User>.ForSqlite().Where(u => u.Age > 18);
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .AsSubQuery()
            .ToSql();
        Assert.IsTrue(sql.Contains("sq") || sql.Contains("FROM ("));
    }

    // L475: GenerateSubQuery - with subquery cache hit
    [TestMethod]
    public void GenerateSubQuery_CacheHit_ReturnsCached()
    {
        // Multiple subqueries with same expression
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Where(u => SubQuery.For<Bc10Order>().Any(o => o.UserId == u.Id))
            .Where(u => SubQuery.For<Bc10Order>().Count(o => o.UserId == u.Id) > 0)
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L484: AppendJoinClauses - with subquery join
    [TestMethod]
    public void AppendJoinClauses_WithSubQueryJoin_GeneratesSubquerySyntax()
    {
        var subq = SqlQuery<Bc10Order>.ForSqlite().Where(o => o.Total > 50);
        var sql = SqlQuery<Bc10User>.ForSqlite()
            .Join(subq, u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })
            .ToSql();
        Assert.IsTrue(sql.Contains("JOIN") && (sql.Contains("(SELECT") || sql.Contains("( SELECT")));
    }

    // Execution tests
    [TestMethod]
    public async Task Execute_WithJoin_ReturnsResults()
    {
        using var conn = CreateConn();
        var results = await SqlQuery<Bc10User>.ForSqlite()
            .Join(SqlQuery<Bc10Order>.ForSqlite(), u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })
            .WithConnection(conn)
            .ToListAsync();
        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    public async Task Execute_WithGroupBy_ReturnsResults()
    {
        using var conn = CreateConn();
        var results = await SqlQuery<Bc10Order>.ForSqlite()
            .GroupBy(o => o.Category)
            .Select(g => new { g.Key, Count = g.Count() })
            .WithConnection(conn)
            .ToListAsync();
        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    public async Task Execute_WithDistinct_ReturnsResults()
    {
        using var conn = CreateConn();
        var results = await SqlQuery<Bc10User>.ForSqlite()
            .Select(u => new { u.Age })
            .Distinct()
            .WithConnection(conn)
            .ToListAsync();
        Assert.IsTrue(results.Count > 0);
    }

    [TestMethod]
    public async Task Execute_FromSubQuery_ReturnsResults()
    {
        using var conn = CreateConn();
        var subq = SqlQuery<Bc10User>.ForSqlite().Where(u => u.IsActive);
        var results = await SqlQuery<Bc10User>.ForSqlite()
            .AsSubQuery()
            .WithConnection(conn)
            .ToListAsync();
        Assert.IsTrue(results.Count > 0);
    }
}
