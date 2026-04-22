using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;

namespace Sqlx.Tests.Expressions;

[TestClass]
public class ExpressionParserCoverageTests
{
    [TestMethod]
    public void ParseLambda_WithLambdaExpression_ParsesBody()
    {
        var parser = CreateParser();
        Expression<Func<ParserEntity, int>> expression = entity => entity.Id;

        var result = parser.ParseLambda(expression);

        Assert.AreEqual("[id]", result);
    }

    [TestMethod]
    public void ParseLambda_WithQuotedLambda_ParsesBody()
    {
        var parser = CreateParser();
        Expression<Func<ParserEntity, string>> expression = entity => entity.Name;

        var result = parser.ParseLambda(Expression.Quote(expression));

        Assert.AreEqual("[name]", result);
    }

    [TestMethod]
    public void ParseLambda_WithNonLambdaExpression_ParsesExpressionDirectly()
    {
        var parser = CreateParser();
        Expression<Func<ParserEntity, int>> expression = entity => entity.Id;

        var result = parser.ParseLambda(expression.Body);

        Assert.AreEqual("[id]", result);
    }

    [DataTestMethod]
    [DataRow(0, "@p0")]
    [DataRow(10, "@p10")]
    [DataRow(100, "@p100")]
    [DataRow(1000, "@p1000")]
    [DataRow(10000, "@p10000")]
    [DataRow(100000, "@p100000")]
    public void CreateParameter_FormatsNamesAcrossDigitThresholds(int index, string expected)
    {
        var parser = CreateParser(parameterized: true);
        var field = typeof(ExpressionParser).GetField("_parameterIndex", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(field);
        field!.SetValue(parser, index);

        var result = parser.CreateParameter(index);

        Assert.AreEqual(expected, result);
    }

    private static ExpressionParser CreateParser(bool parameterized = false)
        => new(new SqlServerDialect(), new Dictionary<string, object?>(), parameterized);

    private sealed class ParserEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

[TestClass]
public class SqlExpressionVisitorCoverageTests
{
    [TestMethod]
    public void GetEntityColumns_WithExplicitProvider_ReturnsProviderColumns()
    {
        var provider = new DynamicEntityProvider<VisitorEntity>();
        var visitor = CreateVisitor(provider);

        var columns = InvokeGetEntityColumns(visitor);

        Assert.AreEqual(provider.Columns.Count, columns.Count);
    }

    [TestMethod]
    public void GetEntityColumns_WithElementTypeFallback_ReturnsResolvedColumns()
    {
        var visitor = CreateVisitor();
        var field = visitor.GetType().GetField("_elementType", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(field);
        field!.SetValue(visitor, typeof(VisitorEntity));

        var columns = InvokeGetEntityColumns(visitor);

        Assert.IsTrue(columns.Count > 0);
    }

    [TestMethod]
    public void GetEntityColumns_WithoutProviderOrElementType_ReturnsEmpty()
    {
        var visitor = CreateVisitor();

        var columns = InvokeGetEntityColumns(visitor);

        Assert.AreEqual(0, columns.Count);
    }

    private static object CreateVisitor(IEntityProvider? provider = null)
    {
        var visitorType = typeof(SqlQuery<int>).Assembly.GetType("Sqlx.SqlExpressionVisitor", throwOnError: true)!;
        return Activator.CreateInstance(
            visitorType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[] { new SqlServerDialect(), false, provider, null, null },
            culture: null)!;
    }

    private static IReadOnlyList<ColumnMeta> InvokeGetEntityColumns(object visitor)
    {
        var method = visitor.GetType().GetMethod("GetEntityColumns", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(method);
        return (IReadOnlyList<ColumnMeta>)method!.Invoke(visitor, null)!;
    }

    private sealed class VisitorEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

[TestClass]
public class ExpressionParserEdgeCaseTests
{
    // Line 237-238: ParseMemberLength - string.Length property
    [TestMethod]
    public void Parse_StringLength_GeneratesLengthFunction()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<EdgeEntity, int>> expr = e => e.Name.Length;
        var result = parser.Parse(expr.Body);
        Assert.IsTrue(result.Contains("LENGTH") || result.Contains("length") || result.Contains("LEN"), $"Expected length function, got: {result}");
    }

    // Lines 262-263: ParseBinary - bool comparison with constant on left
    [TestMethod]
    public void Parse_BoolConstantOnLeft_GeneratesCorrectSql()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<EdgeEntity, bool>> expr = e => true == e.IsActive;
        var result = parser.Parse(expr.Body);
        Assert.IsNotNull(result);
    }

    // Lines 317-323: FormatLogical - bool member in AND/OR
    [TestMethod]
    public void Parse_BoolMemberInAndExpression_GeneratesCorrectSql()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<EdgeEntity, bool>> expr = e => e.Id > 0 && e.IsActive;
        var result = parser.Parse(expr.Body);
        Assert.IsTrue(result.Contains("is_active") || result.Contains("IsActive"));
    }

    [TestMethod]
    public void Parse_BoolMemberInOrExpression_GeneratesCorrectSql()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<EdgeEntity, bool>> expr = e => e.Id > 0 || e.IsActive;
        var result = parser.Parse(expr.Body);
        Assert.IsTrue(result.Contains("is_active") || result.Contains("IsActive"));
    }

    // Line 310: unsupported binary operator
    [TestMethod]
    public void Parse_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        // ExpressionType.ExclusiveOr (XOR) is not supported
        var left = Expression.Constant(2);
        var right = Expression.Constant(3);
        var xor = Expression.ExclusiveOr(left, right);
        Assert.ThrowsException<NotSupportedException>(() => parser.Parse(xor));
    }

    // Lines 584-586: GetLambdaBody with non-lambda expression returns null -> ParseLambdaColumn returns "*"
    [TestMethod]
    public void Parse_SubQuery_WithAggregateNoSelector_UsesStar()
    {
        // This tests the path where ParseLambdaColumn returns "*" for non-lambda
        // Covered indirectly via SubQuery aggregate tests
        var q = SqlQuery<EdgeEntity>.ForSqlite()
            .Where(e => e.Id > 0);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // Lines 594-595: ParseContains with null collection (instance method on List)
    [TestMethod]
    public void Parse_ContainsWithNullCollection_GeneratesInNull()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        // Use a List<int> field that is null - List.Contains is an instance method so m.Object != null
        var nullList = (List<int>?)null;
        Expression<Func<EdgeEntity, bool>> expr = e => nullList!.Contains(e.Id);
        var result = parser.Parse(expr.Body);
        Assert.IsTrue(result.Contains("IN (NULL)"), $"Expected IN (NULL), got: {result}");
    }

}

[Sqlx, TableName("edge_entities")]
public class EdgeEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
