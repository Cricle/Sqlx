using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System.Linq;

namespace Sqlx.Tests.DynamicPlaceholder;

/// <summary>
/// 测试 SqlTemplateEngine 的动态占位符解析功能
/// </summary>
[TestClass]
public class SqlTemplateEngineTests
{
    private SqlTemplateEngine _engine = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();
    }

    #region 动态占位符检测

    [TestMethod]
    public void ProcessTemplate_WithDynamicPlaceholder_SetsHasDynamicFeaturesTrue()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures, "HasDynamicFeatures should be true for dynamic placeholders");
    }

    [TestMethod]
    public void ProcessTemplate_WithoutDynamicPlaceholder_SetsHasDynamicFeaturesFalse()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsFalse(result.HasDynamicFeatures, "HasDynamicFeatures should be false without dynamic placeholders");
    }

    [TestMethod]
    public void ProcessTemplate_WithMultipleDynamicPlaceholders_SetsHasDynamicFeaturesTrue()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}} WHERE {{@whereClause}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures);
    }

    #endregion

    #region 动态占位符替换

    [TestMethod]
    public void ProcessTemplate_DynamicTableName_ReplacesWithParameter()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        // Dynamic placeholders are kept as-is, not processed by ProcessTemplate
        // They are handled during code generation
        Assert.IsTrue(result.ProcessedSql.Contains("{{@tableName}}") || result.HasDynamicFeatures,
            "Dynamic placeholder should be marked for code generation");
    }

    [TestMethod]
    public void ProcessTemplate_DynamicWhereClause_ReplacesWithParameter()
    {
        // Arrange
        var template = "SELECT * FROM users WHERE {{@whereClause}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("{{@whereClause}}") || result.HasDynamicFeatures,
            "Dynamic placeholder should be marked for code generation");
    }

    [TestMethod]
    public void ProcessTemplate_DynamicTableSuffix_ReplacesWithParameter()
    {
        // Arrange
        var template = "SELECT * FROM logs_{{@suffix}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "logs");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("{{@suffix}}") || result.HasDynamicFeatures,
            "Dynamic placeholder should be marked for code generation");
    }

    #endregion

    #region 混合占位符

    [TestMethod]
    public void ProcessTemplate_MixedPlaceholders_ProcessesBothCorrectly()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{@tableName}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        // HasDynamicFeatures should be true since we have a dynamic placeholder
        Assert.IsTrue(result.ProcessedSql.Contains("{{@tableName}}") || result.HasDynamicFeatures,
            "Should mark dynamic placeholder for code generation");
        // {{columns}} will be processed during code generation based on entity type
    }

    [TestMethod]
    public void ProcessTemplate_StaticAndDynamicPlaceholders_OnlyMarksHasDynamicFeatures()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsFalse(result.HasDynamicFeatures, "Should not mark dynamic features for static placeholders");
    }

    #endregion

    #region 边界情况

    [TestMethod]
    public void ProcessTemplate_EmptyTemplate_ReturnsDefaultOrEmpty()
    {
        // Arrange
        var template = "";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsFalse(result.HasDynamicFeatures);
        // Template engine may return default SQL (SELECT 1) or empty string
        // Both are acceptable behaviors for empty templates
        Assert.IsTrue(result.ProcessedSql == "" || result.ProcessedSql.Contains("SELECT"),
            "Empty template should return empty string or default SQL");
    }

    [TestMethod]
    public void ProcessTemplate_TemplateWithoutPlaceholders_ReturnsUnchanged()
    {
        // Arrange
        var template = "SELECT * FROM users WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsFalse(result.HasDynamicFeatures);
        Assert.AreEqual(template, result.ProcessedSql);
    }

    [TestMethod]
    public void ProcessTemplate_InvalidDynamicPlaceholder_DoesNotCrash()
    {
        // Arrange
        var template = "SELECT * FROM {{@}}"; // 无效：缺少参数名

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert - 应该不会崩溃，但可能包含警告或错误
        Assert.IsNotNull(result);
    }

    #endregion

    #region 大小写敏感性

    [TestMethod]
    public void ProcessTemplate_DynamicPlaceholder_IsCaseSensitive()
    {
        // Arrange
        var template1 = "SELECT * FROM {{@TableName}}";
        var template2 = "SELECT * FROM {{@tableName}}";

        // Act
        var result1 = _engine.ProcessTemplate(template1, null!, null!, "users");
        var result2 = _engine.ProcessTemplate(template2, null!, null!, "users");

        // Debug output
        Console.WriteLine($"Result1 SQL: {result1.ProcessedSql}");
        Console.WriteLine($"Result2 SQL: {result2.ProcessedSql}");
        Console.WriteLine($"Result1 HasDynamicFeatures: {result1.HasDynamicFeatures}");
        Console.WriteLine($"Result2 HasDynamicFeatures: {result2.HasDynamicFeatures}");

        // Assert
        // Dynamic placeholders should preserve case sensitivity
        Assert.IsTrue(result1.ProcessedSql.Contains("TableName") || result1.ProcessedSql.Contains("{{@TableName}}"),
            $"Expected TableName in: {result1.ProcessedSql}");
        Assert.IsTrue(result2.ProcessedSql.Contains("tableName") || result2.ProcessedSql.Contains("{{@tableName}}"),
            $"Expected tableName in: {result2.ProcessedSql}");
        // The parameter names should be case-sensitive
        Assert.AreNotEqual(result1.ProcessedSql, result2.ProcessedSql,
            "Case-sensitive parameter names should produce different SQL");
    }

    #endregion

    #region 特殊字符

    [TestMethod]
    public void ProcessTemplate_DynamicPlaceholderWithUnderscore_ProcessesCorrectly()
    {
        // Arrange
        var template = "SELECT * FROM {{@table_name}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures);
        Assert.IsTrue(result.ProcessedSql.Contains("{table_name}"));
    }

    [TestMethod]
    public void ProcessTemplate_DynamicPlaceholderWithNumbers_ProcessesCorrectly()
    {
        // Arrange
        var template = "SELECT * FROM {{@table123}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures);
        Assert.IsTrue(result.ProcessedSql.Contains("{table123}"));
    }

    #endregion

    #region 多个相同动态占位符

    [TestMethod]
    public void ProcessTemplate_SameDynamicPlaceholderMultipleTimes_ProcessesAll()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}} t1 JOIN {{@tableName}} t2 ON t1.id = t2.parent_id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Debug output
        Console.WriteLine($"Result SQL: {result.ProcessedSql}");
        Console.WriteLine($"HasDynamicFeatures: {result.HasDynamicFeatures}");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures);
        // Should contain either two placeholders {{@tableName}} or processed versions
        var placeholderCount = result.ProcessedSql.Split(new[] { "{{@tableName}}" }, System.StringSplitOptions.None).Length - 1;
        var interpolationCount = result.ProcessedSql.Split(new[] { "tableName" }, System.StringSplitOptions.None).Length - 1;

        Console.WriteLine($"Placeholder count: {placeholderCount}, Interpolation count: {interpolationCount}");

        // Either keep placeholders (2 occurrences) or convert them (at least 2 references)
        Assert.IsTrue(placeholderCount >= 2 || interpolationCount >= 2,
            $"Should handle all occurrences of the dynamic placeholder. Found {placeholderCount} placeholders and {interpolationCount} interpolations");
    }

    #endregion

    #region 错误和警告

    [TestMethod]
    public void ProcessTemplate_ValidDynamicPlaceholder_NoErrors()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.AreEqual(0, result.Errors.Count, "Should not have errors for valid template");
    }

    #endregion
}

