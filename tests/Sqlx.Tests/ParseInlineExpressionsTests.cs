// <copyright file="ParseInlineExpressionsTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for ParseInlineExpressions method in PlaceholderHandlerBase.
/// </summary>
[TestClass]
public class ParseInlineExpressionsTests
{
    private static readonly MethodInfo ParseMethod = typeof(PlaceholderHandlerBase)
        .GetMethod("ParseInlineExpressions", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Dictionary<string, string>? ParseInlineExpressions(string options)
    {
        return ParseMethod.Invoke(null, new object[] { options }) as Dictionary<string, string>;
    }

    [TestMethod]
    public void ParseInlineExpressions_NullOptions_ReturnsNull()
    {
        var result = ParseInlineExpressions(null!);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseInlineExpressions_EmptyOptions_ReturnsNull()
    {
        var result = ParseInlineExpressions(string.Empty);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseInlineExpressions_NoInlineOption_ReturnsNull()
    {
        var result = ParseInlineExpressions("--exclude Id,Name");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseInlineExpressions_SingleExpression_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Version=Version+1");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey("Version"));
        Assert.AreEqual("Version+1", result["Version"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_MultipleExpressions_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Version=Version+1,Counter=Counter+1");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Version+1", result["Version"]);
        Assert.AreEqual("Counter+1", result["Counter"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithSpaces_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Version = Version + 1");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Version + 1", result["Version"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithMultipleSpaces_PreservesSpaces()
    {
        var result = ParseInlineExpressions("--inline Counter =  Counter  +  1");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("Counter  +  1", result["Counter"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithOtherOptions_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--exclude Id --inline Version=Version+1 --param test");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Version+1", result["Version"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithOtherOptionsAfter_StopsAtNextOption()
    {
        var result = ParseInlineExpressions("--inline Version=Version+1 --exclude Id");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Version+1", result["Version"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_ComplexExpression_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Score=Score*2+@bonus");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("Score*2+@bonus", result["Score"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_SqlFunction_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline UpdatedAt=CURRENT_TIMESTAMP");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("CURRENT_TIMESTAMP", result["UpdatedAt"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_MultipleWithDifferentComplexity_ParsesAll()
    {
        var result = ParseInlineExpressions("--inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP,Score=Score*@multiplier");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Version+1", result["Version"]);
        Assert.AreEqual("CURRENT_TIMESTAMP", result["UpdatedAt"]);
        Assert.AreEqual("Score*@multiplier", result["Score"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_CaseInsensitive_MatchesPropertyName()
    {
        var result = ParseInlineExpressions("--inline version=Version+1");
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("version"));
        Assert.IsTrue(result.ContainsKey("VERSION"));
        Assert.IsTrue(result.ContainsKey("Version"));
    }

    [TestMethod]
    public void ParseInlineExpressions_EmptyExpression_Ignored()
    {
        var result = ParseInlineExpressions("--inline Version=Version+1,,Counter=Counter+1");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void ParseInlineExpressions_MissingValue_Ignored()
    {
        var result = ParseInlineExpressions("--inline Version=");
        
        // 空表达式应该被忽略
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseInlineExpressions_MissingEquals_Ignored()
    {
        var result = ParseInlineExpressions("--inline Version");
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithParentheses_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Score=(Score+@bonus)*@multiplier");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("(Score+@bonus)*@multiplier", result["Score"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithCast_ParsesCorrectly()
    {
        // 注意：CAST 中的逗号会导致解析问题，实际使用中应避免在单个表达式中使用逗号
        // 或者使用参数代替
        var result = ParseInlineExpressions("--inline Total=CAST(Amount AS INTEGER)");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("CAST(Amount AS INTEGER)", result["Total"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithCase_ParsesCorrectly()
    {
        // CASE 表达式中的逗号会导致解析问题，实际使用中建议简化或使用参数
        var result = ParseInlineExpressions("--inline Status=CASE WHEN Active=1 THEN 1 ELSE 0 END");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("CASE WHEN Active=1 THEN 1 ELSE 0 END", result["Status"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_MultipleEqualsInExpression_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Flag=IsActive=1");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("IsActive=1", result["Flag"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithColon_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline UpdatedAt=:timestamp");
        
        Assert.IsNotNull(result);
        Assert.AreEqual(":timestamp", result["UpdatedAt"]);
    }

    [TestMethod]
    public void ParseInlineExpressions_WithDollarSign_ParsesCorrectly()
    {
        var result = ParseInlineExpressions("--inline Amount=$amount");
        
        Assert.IsNotNull(result);
        Assert.AreEqual("$amount", result["Amount"]);
    }
}
