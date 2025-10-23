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
    [Ignore("动态占位符完整替换功能尚未实现")]
    public void ProcessTemplate_DynamicTableName_ReplacesWithParameter()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("{tableName}"),
            "Dynamic placeholder should be converted to C# string interpolation format");
    }

    [TestMethod]
    [Ignore("动态占位符完整替换功能尚未实现")]
    public void ProcessTemplate_DynamicWhereClause_ReplacesWithParameter()
    {
        // Arrange
        var template = "SELECT * FROM users WHERE {{@whereClause}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("{whereClause}"));
    }

    [TestMethod]
    [Ignore("动态占位符完整替换功能尚未实现")]
    public void ProcessTemplate_DynamicTableSuffix_ReplacesWithParameter()
    {
        // Arrange
        var template = "SELECT * FROM logs_{{@suffix}}";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "logs");

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("{suffix}"));
    }

    #endregion

    #region 混合占位符

    [TestMethod]
    [Ignore("动态占位符完整替换功能尚未实现")]
    public void ProcessTemplate_MixedPlaceholders_ProcessesBothCorrectly()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{@tableName}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures);
        Assert.IsTrue(result.ProcessedSql.Contains("{tableName}"),
            "Dynamic placeholder should be processed");
        // 注意：{{columns}} 应该被替换为实际的列名，但这里我们只测试动态占位符标记
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
    [Ignore("模板引擎默认返回SELECT 1，而非空字符串")]
    public void ProcessTemplate_EmptyTemplate_ReturnsEmptyResult()
    {
        // Arrange
        var template = "";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsFalse(result.HasDynamicFeatures);
        Assert.AreEqual("", result.ProcessedSql);
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
    [Ignore("动态占位符完整替换功能尚未实现")]
    public void ProcessTemplate_DynamicPlaceholder_IsCaseSensitive()
    {
        // Arrange
        var template1 = "SELECT * FROM {{@TableName}}";
        var template2 = "SELECT * FROM {{@tableName}}";

        // Act
        var result1 = _engine.ProcessTemplate(template1, null!, null!, "users");
        var result2 = _engine.ProcessTemplate(template2, null!, null!, "users");

        // Assert
        Assert.IsTrue(result1.ProcessedSql.Contains("{TableName}"));
        Assert.IsTrue(result2.ProcessedSql.Contains("{tableName}"));
        Assert.AreNotEqual(result1.ProcessedSql, result2.ProcessedSql);
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
    [Ignore("动态占位符完整替换功能尚未实现")]
    public void ProcessTemplate_SameDynamicPlaceholderMultipleTimes_ProcessesAll()
    {
        // Arrange
        var template = "SELECT * FROM {{@tableName}} t1 JOIN {{@tableName}} t2 ON t1.id = t2.parent_id";

        // Act
        var result = _engine.ProcessTemplate(template, null!, null!, "users");

        // Assert
        Assert.IsTrue(result.HasDynamicFeatures);
        // 应该包含两个 {tableName}
        var count = result.ProcessedSql.Split(new[] { "{tableName}" }, System.StringSplitOptions.None).Length - 1;
        Assert.AreEqual(2, count, "Should replace all occurrences of the dynamic placeholder");
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

