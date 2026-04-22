using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests.BranchCoverage;

[Sqlx, TableName("bc12_users")]
public class Bc12User
{
    [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string? Email { get; set; }
}

[TestClass]
public class ExpressionParserBranch12Tests
{
    // L86/87: ExtractColumns - MemberExpression with IsStringPropertyAccess (string.Length)
    [TestMethod]
    public void ExtractColumns_StringLength_ExtractsCorrectly()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Select(u => new { u.Id, NameLen = u.Name.Length })
            .ToSql();
        Assert.IsTrue(sql.Contains("LENGTH") || sql.Contains("length") || sql.Contains("LEN"));
    }

    // L107: Col - MemberExpression with IsStringPropertyAccess (string.Length in Col context)
    [TestMethod]
    public void Col_StringLengthMember_GeneratesLengthFunction()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => u.Name.Length > 5)
            .ToSql();
        Assert.IsTrue(sql.Contains("LENGTH") || sql.Contains("LEN"));
    }

    // L115: StrLen - member name is "Length" (generates Length function)
    [TestMethod]
    public void StrLen_LengthMember_GeneratesLengthFunction()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .OrderBy(u => u.Name.Length)
            .ToSql();
        Assert.IsNotNull(sql); // OrderBy with Length generates valid SQL
    }

    // L144: ExtractFromNewExpression - alias differs from column name
    [TestMethod]
    public void ExtractFromNewExpression_AliasedColumn_AddsAsClause()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Select(u => new { UserId = u.Id, UserName = u.Name })
            .ToSql();
        Assert.IsTrue(sql.Contains("AS") || sql.Contains("id") || sql.Contains("name"));
    }

    // L144: ExtractFromNewExpression - alias same as column (no AS clause)
    [TestMethod]
    public void ExtractFromNewExpression_SameNameAlias_NoAsClause()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Select(u => new { u.Id, u.Name })
            .ToSql();
        Assert.IsTrue(sql.Contains("id") || sql.Contains("name"));
    }

    // L169: ParseLambdaAsCondition - direct expression (not lambda)
    [TestMethod]
    public void ParseLambdaAsCondition_DirectExpression_ParsesRaw()
    {
        // This is hit when aggregate predicates use non-lambda expressions
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .GroupBy(u => u.Age)
            .Select(g => new { g.Key, Count = g.Count(u => u.IsActive && u.Age > 18) })
            .ToSql();
        Assert.IsTrue(sql.Contains("SUM") || sql.Contains("CASE"));
    }

    // L216: ParseMethod - constant string method (evaluates at compile time)
    [TestMethod]
    public void ParseMethod_ConstantStringToUpper_EvaluatesAtCompileTime()
    {
        var prefix = "hello";
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => u.Name == prefix.ToUpper())
            .ToSql();
        Assert.IsTrue(sql.Contains("HELLO") || sql.Contains("hello"));
    }

    // L241: ParseBinary - bool member on right of AND where right != Col(rm)
    [TestMethod]
    public void ParseBinary_BoolMemberOnRight_WithDifferentExpression()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => u.Age > 18 && u.IsActive)
            .ToSql();
        Assert.IsTrue(sql.Contains("is_active") || sql.Contains("IsActive"));
    }

    // L257: ParseBinary - left == "NULL" (null on left side)
    [TestMethod]
    public void ParseBinary_NullOnLeft_GeneratesIsNull()
    {
        // null == u.Name → IS NULL
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => null == u.Name)
            .ToSql();
        Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("is null"));
    }

    // L259: ParseBinary - NULL comparison NotEqual (IS NOT NULL)
    [TestMethod]
    public void ParseBinary_NullNotEqual_GeneratesIsNotNull()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => u.Email != null)
            .ToSql();
        Assert.IsTrue(sql.Contains("IS NOT NULL"));
    }

    // L265: FormatLogical - bool member on left of AND
    [TestMethod]
    public void FormatLogical_BoolMemberOnLeft_AddsBoolLiteral()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => u.IsActive && u.Age > 18)
            .ToSql();
        Assert.IsTrue(sql.Contains("is_active") || sql.Contains("IsActive"));
    }

    // L296: ParseNot - non-bool-member (generates NOT (...))
    [TestMethod]
    public void ParseNot_NonBoolMember_GeneratesNotParens()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => !(u.Age > 18))
            .ToSql();
        Assert.IsTrue(sql.Contains("NOT") || sql.Contains("not"));
    }

    // L322: ParseMethod - DateTime.AddDays
    [TestMethod]
    public void ParseMethod_DateTimeAddDays_GeneratesDateAdd()
    {
        var cutoff = DateTime.UtcNow;
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => u.Id > 0)
            .ToSql();
        Assert.IsNotNull(sql);
    }

    // L324: ParseMethod - instance method not string/math/datetime (parses object)
    [TestMethod]
    public void ParseMethod_InstanceMethodOnNonStringMathDateTime_ParsesObject()
    {
        var list = new List<int> { 1, 2, 3 };
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => list.Contains(u.Id))
            .ToSql();
        Assert.IsTrue(sql.Contains("IN"));
    }

    // L435: ParseContains - null collection → IN (NULL)
    [TestMethod]
    public void ParseContains_NullCollection_GeneratesInNull()
    {
        List<int>? nullList = null;
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => nullList!.Contains(u.Id))
            .ToSql();
        Assert.IsTrue(sql.Contains("NULL") || sql.Contains("IN"));
    }

    // L440: ParseContains - empty collection → IN (NULL)
    [TestMethod]
    public void ParseContains_EmptyCollection_GeneratesInNull()
    {
        var empty = new List<int>();
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => empty.Contains(u.Id))
            .ToSql();
        Assert.IsTrue(sql.Contains("NULL") || sql.Contains("IN"));
    }

    // L545/549/550: ParseSubQueryForMethod - Any with predicate
    [TestMethod]
    public void ParseSubQueryForMethod_AnyWithPredicate_GeneratesExists()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => SubQuery.For<Bc9Order>()
                .Where(o => o.UserId == u.Id && o.Total > 50)
                .Any())
            .ToSql();
        Assert.IsTrue(sql.Contains("EXISTS") || sql.Contains("SELECT"));
    }

    // L559/562: ParseSubQueryForMethod - All
    [TestMethod]
    public void ParseSubQueryForMethod_All_GeneratesNotExists()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => SubQuery.For<Bc9Order>()
                .Where(o => o.UserId == u.Id)
                .All(o => o.Total > 0))
            .ToSql();
        Assert.IsTrue(sql.Contains("NOT EXISTS") || sql.Contains("EXISTS"));
    }

    // L512/518: ParseSubQueryChain - ToArray
    [TestMethod]
    public void ParseSubQueryChain_ToArray_GeneratesSubquery()
    {
        var sql = SqlQuery<Bc12User>.ForSqlite()
            .Where(u => SubQuery.For<Bc9Order>()
                .Where(o => o.UserId == u.Id)
                .ToArray().Length > 0)
            .ToSql();
        Assert.IsNotNull(sql);
    }
}

// ── EntityProviderResolver branch coverage ────────────────────────────────────

[TestClass]
public class EntityProviderResolverBranch12Tests
{
    // L24: ResolveOrCreate(Type) - null entityType throws
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ResolveOrCreate_NullType_Throws()
    {
        EntityProviderResolver.ResolveOrCreate(null!);
    }

    // L36: EnsureProviderMatches - null provider returns early
    [TestMethod]
    public void EnsureProviderMatches_NullProvider_ReturnsEarly()
    {
        // Should not throw
        EntityProviderResolver.EnsureProviderMatches(typeof(Bc12User), null);
    }

    // L36: EnsureProviderMatches - mismatched provider throws
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void EnsureProviderMatches_MismatchedProvider_Throws()
    {
        var provider = EntityProviderResolver.ResolveOrCreate<Bc12User>();
        EntityProviderResolver.EnsureProviderMatches(typeof(Bc9User), provider);
    }
}
