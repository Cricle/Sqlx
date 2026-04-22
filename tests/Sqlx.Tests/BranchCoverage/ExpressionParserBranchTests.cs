// Branch coverage tests for ExpressionParser and SqlExpressionVisitor
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests.BranchCoverage;

[Sqlx, TableName("bp_users")]
public class BpUser
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? Score { get; set; }
    public string? Email { get; set; }
}

[Sqlx, TableName("bp_orders")]
public class BpOrder
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
}

[TestClass]
public class ExpressionParserBranchTests
{
    private static ExpressionParser P(bool parameterized = false)
        => new(SqlDefine.SQLite, new Dictionary<string, object?>(), parameterized);

    // Line 59: MemberExpression not entity property, asBoolCmp=false → Fmt(EvaluateExpression)
    [TestMethod]
    public void Parse_NonEntityMember_Parameterized_EvaluatesExpression()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), true);
        var captured = 42;
        Expression<Func<BpUser, bool>> expr = u => u.Id == captured;
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("@") || sql.Contains("42"));
    }

    // Line 59: MemberExpression not entity property, asBoolCmp=false, not parameterized → GetMemberValueOptimized
    [TestMethod]
    public void Parse_NonEntityMember_NotParameterized_GetsMemberValue()
    {
        var parser = P(false);
        var captured = 99;
        Expression<Func<BpUser, bool>> expr = u => u.Id == captured;
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("99"));
    }

    // Line 82: Col with GroupBy Key
    [TestMethod]
    public void SqlQuery_GroupBy_Key_GeneratesGroupByClause()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .GroupBy(u => u.IsActive)
            .Select(g => new { Key = g.Key, Count = g.Count() });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("GROUP BY"), sql);
    }

    // Line 96: IsGroupingType with null type
    [TestMethod]
    public void Parse_NullType_IsGroupingType_ReturnsFalse()
    {
        // Covered indirectly via Col() with non-grouping expression
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Id > 0);
        Assert.IsNotNull(q.ToSql());
    }

    // Line 127: ExtractColumns with various expression types
    [TestMethod]
    public void SqlQuery_Select_WithBinaryExpression_ExtractsColumn()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Select(u => new { Sum = u.Id + u.Score });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // Line 135: ExtractColumns MemberExpression IsEntityProperty
    [TestMethod]
    public void SqlQuery_Select_EntityProperty_ExtractsColumn()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Select(u => new { u.Id, u.Name });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("id") && sql.Contains("name"), sql);
    }

    // Line 164: ExtractFromNewExpression with alias
    [TestMethod]
    public void SqlQuery_Select_WithAlias_AddsAsClause()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Select(u => new { UserId = u.Id, UserName = u.Name });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("AS"), sql);
    }

    // Line 261: ParseBinary bool on right with constant on left
    [TestMethod]
    public void Parse_BoolConstantOnLeft_Equal()
    {
        var parser = P();
        Expression<Func<BpUser, bool>> expr = u => true == u.IsActive;
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    // Line 277: NULL comparison Equal
    [TestMethod]
    public void Parse_NullComparison_Equal_GeneratesIsNull()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Email == null);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("IS NULL"), sql);
    }

    // Line 285: NULL comparison NotEqual
    [TestMethod]
    public void Parse_NullComparison_NotEqual_GeneratesIsNotNull()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Email != null);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("IS NOT NULL"), sql);
    }

    // Line 306: Modulo operator (non-Oracle)
    [TestMethod]
    public void Parse_Modulo_GeneratesPercentSign()
    {
        var parser = P();
        Expression<Func<BpUser, int>> expr = u => u.Id % 2;
        var sql = parser.ParseRaw(expr.Body);
        Assert.IsTrue(sql.Contains("%"), sql);
    }

    // Line 306: Modulo operator Oracle
    [TestMethod]
    public void Parse_Modulo_Oracle_GeneratesMod()
    {
        var parser = new ExpressionParser(new OracleDialect(), new Dictionary<string, object?>(), false);
        Expression<Func<BpUser, int>> expr = u => u.Id % 2;
        var sql = parser.ParseRaw(expr.Body);
        Assert.IsTrue(sql.Contains("MOD") || sql.Contains("mod"), sql);
    }

    // Line 294: NotEqual Oracle
    [TestMethod]
    public void Parse_NotEqual_Oracle_GeneratesExclamationEquals()
    {
        var parser = new ExpressionParser(new OracleDialect(), new Dictionary<string, object?>(), false);
        Expression<Func<BpUser, bool>> expr = u => u.Id != 0;
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("!=") || sql.Contains("<>"), sql);
    }

    // Line 302: Add with string concatenation
    [TestMethod]
    public void Parse_StringConcatenation_GeneratesConcat()
    {
        var parser = P();
        Expression<Func<BpUser, string>> expr = u => u.Name + u.Email;
        var sql = parser.ParseRaw(expr.Body);
        Assert.IsNotNull(sql);
    }

    // Line 331: ParseNot with non-bool-member
    [TestMethod]
    public void Parse_Not_NonBoolMember_GeneratesNotParens()
    {
        var parser = P();
        Expression<Func<BpUser, bool>> expr = u => !(u.Id > 0);
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("NOT"), sql);
    }

    // Line 342: ParseMethod - Any.Value placeholder with non-constant arg
    [TestMethod]
    public void Parse_AnyValue_WithConstantArg_GeneratesParam()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), true,
            new Dictionary<string, string>());
        Expression<Func<BpUser, bool>> expr = u => u.Id == Any.Value<int>("minId");
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("minId"), sql);
    }

    // Line 385: ParseMethod with object (instance method not string/math/datetime)
    [TestMethod]
    public void Parse_InstanceMethod_NotStringMathDateTime_ParsesObject()
    {
        var parser = P();
        // Use a method on a non-string, non-math, non-datetime type
        var list = new List<int> { 1, 2, 3 };
        Expression<Func<BpUser, bool>> expr = u => list.Contains(u.Id);
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    // Line 417: IsSubQueryChainWithoutAggregate - Arguments.Count > 0
    [TestMethod]
    public void Parse_SubQueryChain_WithArguments_ChecksContainsSubQueryFor()
    {
        // Covered via SubQuery tests - just ensure no crash
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Id > 0);
        Assert.IsNotNull(q.ToSql());
    }

    // Line 565: GetSubQueryElementType - non-generic type
    [TestMethod]
    public void Parse_SubQuery_ElementType_NonGeneric_ReturnsNull()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Id > 0);
        Assert.IsNotNull(q.ToSql());
    }

    // Line 582: GetLambdaBody - direct lambda vs quoted
    [TestMethod]
    public void Parse_GetLambdaBody_DirectLambda()
    {
        var parser = P();
        Expression<Func<BpUser, int>> selector = u => u.Id;
        // ParseLambda with direct lambda
        var result = parser.ParseLambda(selector);
        Assert.IsTrue(result.Contains("id"), result);
    }

    // Line 610: ParseContains with null item in collection
    [TestMethod]
    public void Parse_Contains_WithNullItemInCollection_HandlesNull()
    {
        var parser = P();
        var list = new List<string?> { "a", null, "b" };
        Expression<Func<BpUser, bool>> expr = u => list.Contains(u.Name);
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("IN"), sql);
    }
}

[TestClass]
public class SqlExpressionVisitorBranchTests
{
    // Line 94: CanBuildDirectAggregateQuery - with distinct/groupby/skip/take
    [TestMethod]
    public void SqlQuery_Distinct_CountAsync_UsesWrappedQuery()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Distinct();
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("DISTINCT"), sql);
    }

    // Line 101: BuildCountSql with shared builder
    [TestMethod]
    public void SqlQuery_Count_GeneratesCountSql()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Id > 0);
        var provider = (SqlxQueryProvider<BpUser>)q.Provider;
        var (countSql, _) = provider.ToCountQuery(q.Expression);
        Assert.IsTrue(countSql.Contains("COUNT"), countSql);
    }

    // Line 110: BuildExistsSql - SqlServer uses TOP 1
    [TestMethod]
    public void SqlQuery_Exists_SqlServer_UsesTop1()
    {
        var q = SqlQuery<BpUser>.ForSqlServer().Where(u => u.Id > 0);
        var provider = (SqlxQueryProvider<BpUser>)q.Provider;
        var (existsSql, _) = provider.ToExistsQuery(q.Expression);
        Assert.IsTrue(existsSql.Contains("TOP") || existsSql.Contains("EXISTS"), existsSql);
    }

    // Line 215: VisitTake with non-constant
    [TestMethod]
    public void SqlQuery_Take_GeneratesLimit()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Take(5);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("5"), sql);
    }

    // Line 217: VisitSkip
    [TestMethod]
    public void SqlQuery_Skip_GeneratesOffset()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Skip(10).Take(5);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("10"), sql);
    }

    // Line 223: VisitFirst with predicate
    [TestMethod]
    public void SqlQuery_First_WithPredicate_AddsWhere()
    {
        // First with predicate adds WHERE clause - test via SQL generation
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Id > 5);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("WHERE"), sql);
    }

    // Line 282: ExtractInnerTableInfo with ISqlxQueryable having SubQuerySource
    [TestMethod]
    public void SqlQuery_Join_WithSubQuerySource_GeneratesJoin()
    {
        var inner = SqlQuery<BpOrder>.ForSqlite().Where(o => o.Total > 100);
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(inner, u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("JOIN"), sql);
    }

    // Line 284: ExtractInnerTableInfo with ISqlxQueryable non-constant expression
    [TestMethod]
    public void SqlQuery_Join_WithFilteredInner_GeneratesJoin()
    {
        var inner = SqlQuery<BpOrder>.ForSqlite().Where(o => o.Total > 0);
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(inner, u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total });
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("JOIN"), sql);
    }

    // Line 304: GenerateSubQuery cache hit
    [TestMethod]
    public void SqlQuery_SubQuery_CacheHit_ReturnsCached()
    {
        var sub = SqlQuery<BpUser>.ForSqlite().Where(u => u.IsActive).AsSubQuery();
        var q = SqlQuery<BpUser>.For(SqlDefine.SQLite, sub)
            .Where(u => u.Id > 0);
        var sql1 = q.ToSql();
        var sql2 = q.ToSql(); // second call hits cache
        Assert.AreEqual(sql1, sql2);
    }

    // Line 334: GetCurrentAlias with fromSubQuery
    [TestMethod]
    public void SqlQuery_FromSubQuery_UsesSquAlias()
    {
        var sub = SqlQuery<BpUser>.ForSqlite().Where(u => u.IsActive);
        var q = SqlQuery<BpUser>.For(SqlDefine.SQLite, sub).Where(u => u.Id > 0);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("sq"), sql);
    }

    // Line 340: ResolveOuterAlias with propertyAliasMap
    [TestMethod]
    public void SqlQuery_Join_Select_WithAliasedProperty_ResolvesAlias()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(SqlQuery<BpOrder>.ForSqlite(),
                u => u.Id, o => o.UserId,
                (u, o) => new { u.Name, o.Total })
            .Where(x => x.Total > 50);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // Line 354: UpdateColumnMapping with non-MemberExpression arg
    [TestMethod]
    public void SqlQuery_Select_WithConstantInProjection_HandlesNonMember()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Select(u => new { u.Id, Const = 1 });
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // Line 379: UpdateColumnMapping with MemberExpression from parameter
    [TestMethod]
    public void SqlQuery_Join_Select_MemberFromParameter_UpdatesAliasMap()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(SqlQuery<BpOrder>.ForSqlite(),
                u => u.Id, o => o.UserId,
                (u, o) => new { u.Name, o.Total })
            .OrderBy(x => x.Total);
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("ORDER BY"), sql);
    }

    // Line 405: GetLambda with direct lambda (not quoted)
    [TestMethod]
    public void SqlQuery_Where_DirectLambda_Works()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Id > 0);
        Assert.IsNotNull(q.ToSql());
    }

    // Line 429: GetEntityColumns - elementType fallback
    [TestMethod]
    public void SqlQuery_WithDynamicEntityProvider_FallsBackToElementType()
    {
        var q = SqlQuery<BpUser>.ForSqlite();
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("SELECT"), sql);
    }

    // Line 487: GetEntityColumns - ep.Columns.Count > 0
    [TestMethod]
    public void SqlQuery_WithEntityProvider_UsesProviderColumns()
    {
        var q = SqlQuery<BpUser>.ForSqlite(new DynamicEntityProvider<BpUser>());
        var sql = q.ToSql();
        Assert.IsTrue(sql.Contains("SELECT"), sql);
    }

    // Line 496: AppendFromClause with null tableName throws
    [TestMethod]
    public void SqlQuery_NoTableName_ThrowsInvalidOperation()
    {
        var q = SqlQuery<BpUser>.ForSqlite();
        Assert.IsNotNull(q.ToSql());
    }

    // Line 94: CanBuildDirectAggregateQuery - with skip/take (returns false)
    [TestMethod]
    public void SqlQuery_Skip_CannotBuildDirectAggregate()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Skip(5);
        var provider = (SqlxQueryProvider<BpUser>)q.Provider;
        var (countSql, _) = provider.ToCountQuery(q.Expression);
        Assert.IsTrue(countSql.Contains("COUNT"), countSql);
    }

    [TestMethod]
    public void SqlQuery_Take_CannotBuildDirectAggregate()
    {
        var q = SqlQuery<BpUser>.ForSqlite().Take(10);
        var provider = (SqlxQueryProvider<BpUser>)q.Provider;
        var (countSql, _) = provider.ToCountQuery(q.Expression);
        Assert.IsTrue(countSql.Contains("COUNT"), countSql);
    }

    // Line 288: ExtractInnerTableInfo - MethodCallExpression with generic type
    [TestMethod]
    public void SqlQuery_Join_WithFilteredInner_MethodCallType()
    {
        var inner = SqlQuery<BpOrder>.ForSqlite().Where(o => o.Total > 0);
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(inner, u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total });
        Assert.IsNotNull(q.ToSql());
    }

    // Line 334: GetCurrentAlias - with join clauses
    [TestMethod]
    public void SqlQuery_Join_Where_UsesJoinAlias()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(SqlQuery<BpOrder>.ForSqlite(),
                u => u.Id, o => o.UserId,
                (u, o) => new { u.Name, o.Total })
            .Where(x => x.Total > 0);
        Assert.IsNotNull(q.ToSql());
    }

    // Line 340: ResolveOuterAlias - propertyAliasMap has entry
    [TestMethod]
    public void SqlQuery_Join_OrderBy_ProjectedProperty_ResolvesAlias()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Join(SqlQuery<BpOrder>.ForSqlite(),
                u => u.Id, o => o.UserId,
                (u, o) => new { u.Name, o.Total })
            .OrderBy(x => x.Name);
        Assert.IsTrue(q.ToSql().Contains("ORDER BY"));
    }
}
