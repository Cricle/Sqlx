using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Tests.Expressions;

[TestClass]
public class ExpressionHelperTests
{
    [TestMethod]
    public void FindRootParameterType_HandlesParameterMemberConvertMethodCallAndFallback()
    {
        var parameter = Expression.Parameter(typeof(SampleEntity), "e");
        Assert.AreEqual(typeof(SampleEntity), ExpressionHelper.FindRootParameterType(parameter));

        Expression<Func<SampleEntity, string>> memberExpr = e => e.Name;
        Assert.AreEqual(typeof(SampleEntity), ExpressionHelper.FindRootParameterType(memberExpr.Body));

        Expression<Func<SampleEntity, object>> convertExpr = e => (object)e.Name;
        Assert.AreEqual(typeof(SampleEntity), ExpressionHelper.FindRootParameterType(convertExpr.Body));

        Expression<Func<SampleEntity, int>> methodCallExpr = e => e.Name.Length;
        Assert.AreEqual(typeof(SampleEntity), ExpressionHelper.FindRootParameterType(methodCallExpr.Body));

        Assert.IsNull(ExpressionHelper.FindRootParameterType(Expression.Constant(1)));
    }

    [TestMethod]
    public void IsEntityProperty_AndIsCollectionType_ReturnExpectedResults()
    {
        Expression<Func<SampleEntity, string>> direct = e => e.Name;
        Assert.IsTrue(ExpressionHelper.IsEntityProperty((MemberExpression)direct.Body));

        Expression<Func<NamedHolder, string>> converted = e => ((IHasName)e).TextProperty;
        Assert.IsTrue(ExpressionHelper.IsEntityProperty((MemberExpression)converted.Body));

        Expression<Func<SampleWrapper, string>> nested = e => e.Entity.Name;
        Assert.IsTrue(ExpressionHelper.IsEntityProperty((MemberExpression)nested.Body));

        Assert.IsTrue(ExpressionHelper.IsCollectionType(typeof(int[])));
        Assert.IsTrue(ExpressionHelper.IsCollectionType(typeof(List<int>)));
        Assert.IsTrue(ExpressionHelper.IsCollectionType(typeof(IEnumerable<int>)));
        Assert.IsFalse(ExpressionHelper.IsCollectionType(typeof(string)));
    }

    [TestMethod]
    public void GetMemberValueOptimized_ReadsFieldPropertyConvertAndFallback()
    {
        var holder = new ValueHolder { NumberField = 12, TextProperty = "field" };

        Expression<Func<int>> fieldExpr = () => holder.NumberField;
        Assert.AreEqual(12, ExpressionHelper.GetMemberValueOptimized((MemberExpression)fieldExpr.Body));

        Expression<Func<string>> propertyExpr = () => holder.TextProperty;
        Assert.AreEqual("field", ExpressionHelper.GetMemberValueOptimized((MemberExpression)propertyExpr.Body));

        Expression<Func<string>> convertExpr = () => ((IHasName)holder).TextProperty;
        Assert.AreEqual("field", ExpressionHelper.GetMemberValueOptimized((MemberExpression)convertExpr.Body));

        Expression<Func<string>> fallbackExpr = () => CreateHolder().TextProperty;
        Assert.AreEqual("created", ExpressionHelper.GetMemberValueOptimized((MemberExpression)fallbackExpr.Body));
    }

    [TestMethod]
    public void EvaluateExpression_ReturnsConstantAndCompiledResults()
    {
        Assert.AreEqual(5, ExpressionHelper.EvaluateExpression(Expression.Constant(5)));

        var offset = 3;
        Expression<Func<int>> expression = () => offset + 2;

        Assert.AreEqual(5, ExpressionHelper.EvaluateExpression(expression.Body));
        Assert.AreEqual(5, ExpressionHelper.EvaluateExpression(expression.Body));
    }

    [TestMethod]
    public void ConvertToSnakeCase_HandlesCachedNamesAcronymsAndDigits()
    {
        Assert.AreEqual("already_lower", ExpressionHelper.ConvertToSnakeCase("already_lower"));
        Assert.AreEqual("user_id", ExpressionHelper.ConvertToSnakeCase("UserID"));
        Assert.AreEqual("http2_server", ExpressionHelper.ConvertToSnakeCase("Http2Server"));
        Assert.AreEqual("version2_alpha", ExpressionHelper.ConvertToSnakeCase("Version2Alpha"));
        Assert.AreEqual("xml_parser", ExpressionHelper.ConvertToSnakeCaseCore("XMLParser"));
    }

    [TestMethod]
    public void RemoveOuterParentheses_RemovesOnlyWrappingPair()
    {
        Assert.AreEqual("value", ExpressionHelper.RemoveOuterParentheses("(value)"));
        Assert.AreEqual("value", ExpressionHelper.RemoveOuterParentheses("value"));
        Assert.AreEqual("(value", ExpressionHelper.RemoveOuterParentheses("((value)"));
    }

    [TestMethod]
    public void AggregateAndStringHelpers_ReturnExpectedResults()
    {
        Assert.IsTrue(ExpressionHelper.IsStringType(typeof(string)));

        Expression<Func<SampleEntity, int>> lengthExpr = e => e.Name.Length;
        Assert.IsTrue(ExpressionHelper.IsStringPropertyAccess((MemberExpression)lengthExpr.Body));

        var add = Expression.Add(
            Expression.Constant("a"),
            Expression.Constant("b"),
            typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!);
        Assert.IsTrue(ExpressionHelper.IsStringConcatenation(add));

        Expression<Func<BoolEntity, bool>> booleanMember = e => e.Active;
        var member = (MemberExpression)booleanMember.Body;
        Assert.IsTrue(ExpressionHelper.IsBooleanMember(member));

        var countMethod = typeof(ExpressionHelperAggregateMethods).GetMethod(nameof(ExpressionHelperAggregateMethods.Count))!;
        var aggregateCall = Expression.Call(countMethod, Expression.Constant(1));
        Assert.IsTrue(ExpressionHelper.IsAggregateContext(aggregateCall));
    }

    private static ValueHolder CreateHolder() => new() { TextProperty = "created" };

    private sealed class SampleEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SampleWrapper
    {
        public SampleEntity Entity { get; set; } = new();
    }

    private interface IHasName
    {
        string TextProperty { get; }
    }

    private sealed class ValueHolder : IHasName
    {
        public int NumberField;

        public string TextProperty { get; set; } = string.Empty;
    }

    private sealed class NamedHolder : IHasName
    {
        public string TextProperty { get; set; } = string.Empty;
    }

    private sealed class BoolEntity
    {
        public bool Active { get; set; }
    }

}

internal static class ExpressionHelperAggregateMethods
{
    public static int Count(int value) => value;
}
