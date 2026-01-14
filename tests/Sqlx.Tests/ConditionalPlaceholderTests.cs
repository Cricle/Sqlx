// <copyright file="ConditionalPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;

/// <summary>
/// Strict tests for conditional placeholders ({{if}}, {{/if}}) and block placeholder extensibility.
/// </summary>
[TestClass]
public class ConditionalPlaceholderTests
{
    private static readonly PlaceholderContext Context = new(
        SqlDefine.SQLite,
        "users",
        new List<ColumnMeta>
        {
            new("id", "Id", System.Data.DbType.Int64, true),
            new("name", "Name", System.Data.DbType.String, false),
            new("age", "Age", System.Data.DbType.Int32, false),
            new("email", "Email", System.Data.DbType.String, false),
        });

    #region IBlockPlaceholderHandler Interface Tests

    [TestMethod]
    public void IfPlaceholderHandler_ImplementsIBlockPlaceholderHandler()
    {
        var handler = IfPlaceholderHandler.Instance;
        Assert.IsInstanceOfType(handler, typeof(IBlockPlaceholderHandler));
    }

    [TestMethod]
    public void IfPlaceholderHandler_HasCorrectName()
    {
        Assert.AreEqual("if", IfPlaceholderHandler.Instance.Name);
    }

    [TestMethod]
    public void IfPlaceholderHandler_HasCorrectClosingTag()
    {
        Assert.AreEqual("/if", IfPlaceholderHandler.Instance.ClosingTagName);
    }

    [TestMethod]
    public void IfPlaceholderHandler_IsDynamic()
    {
        Assert.AreEqual(PlaceholderType.Dynamic, IfPlaceholderHandler.Instance.GetType("notnull=x"));
    }

    #endregion

    #region ShouldInclude - NotNull Condition

    [TestMethod]
    public void ShouldInclude_NotNull_ReturnsTrueWhenValueExists()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        Assert.IsTrue(handler.ShouldInclude("notnull=name", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotNull_ReturnsFalseWhenValueIsNull()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        Assert.IsFalse(handler.ShouldInclude("notnull=name", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotNull_ReturnsFalseWhenParameterMissing()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?>();
        Assert.IsFalse(handler.ShouldInclude("notnull=name", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotNull_ReturnsTrueForEmptyString()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "" };
        Assert.IsTrue(handler.ShouldInclude("notnull=name", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotNull_ReturnsTrueForZero()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["count"] = 0 };
        Assert.IsTrue(handler.ShouldInclude("notnull=count", parameters));
    }

    #endregion

    #region ShouldInclude - Null Condition

    [TestMethod]
    public void ShouldInclude_Null_ReturnsTrueWhenValueIsNull()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = null };
        Assert.IsTrue(handler.ShouldInclude("null=name", parameters));
    }

    [TestMethod]
    public void ShouldInclude_Null_ReturnsTrueWhenParameterMissing()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?>();
        Assert.IsTrue(handler.ShouldInclude("null=name", parameters));
    }

    [TestMethod]
    public void ShouldInclude_Null_ReturnsFalseWhenValueExists()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
        Assert.IsFalse(handler.ShouldInclude("null=name", parameters));
    }

    #endregion

    #region ShouldInclude - NotEmpty Condition

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsTrueForNonEmptyString()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "test" };
        Assert.IsTrue(handler.ShouldInclude("notempty=search", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsFalseForEmptyString()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "" };
        Assert.IsFalse(handler.ShouldInclude("notempty=search", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsFalseForNull()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = null };
        Assert.IsFalse(handler.ShouldInclude("notempty=search", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsTrueForNonEmptyList()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
        Assert.IsTrue(handler.ShouldInclude("notempty=ids", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsFalseForEmptyList()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
        Assert.IsFalse(handler.ShouldInclude("notempty=ids", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsTrueForNonEmptyArray()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new[] { 1, 2 } };
        Assert.IsTrue(handler.ShouldInclude("notempty=ids", parameters));
    }

    [TestMethod]
    public void ShouldInclude_NotEmpty_ReturnsFalseForEmptyArray()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = Array.Empty<int>() };
        Assert.IsFalse(handler.ShouldInclude("notempty=ids", parameters));
    }

    #endregion

    #region ShouldInclude - Empty Condition

    [TestMethod]
    public void ShouldInclude_Empty_ReturnsTrueForEmptyString()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "" };
        Assert.IsTrue(handler.ShouldInclude("empty=search", parameters));
    }

    [TestMethod]
    public void ShouldInclude_Empty_ReturnsTrueForNull()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = null };
        Assert.IsTrue(handler.ShouldInclude("empty=search", parameters));
    }

    [TestMethod]
    public void ShouldInclude_Empty_ReturnsFalseForNonEmptyString()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["search"] = "test" };
        Assert.IsFalse(handler.ShouldInclude("empty=search", parameters));
    }

    [TestMethod]
    public void ShouldInclude_Empty_ReturnsTrueForEmptyList()
    {
        var handler = IfPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
        Assert.IsTrue(handler.ShouldInclude("empty=ids", parameters));
    }

    #endregion

    #region SqlTemplate - Basic If Block Rendering

    [TestMethod]
    public void Render_IfNotNull_IncludesContentWhenNotNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Alice" });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name = @name", sql);
    }

    [TestMethod]
    public void Render_IfNotNull_ExcludesContentWhenNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    [TestMethod]
    public void Render_IfNull_IncludesContentWhenNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if null=name}}AND name IS NULL{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name IS NULL", sql);
    }

    [TestMethod]
    public void Render_IfNull_ExcludesContentWhenNotNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if null=name}}AND name IS NULL{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Bob" });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    [TestMethod]
    public void Render_IfNotEmpty_IncludesContentWhenListHasItems()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notempty=ids}}AND id IN (@id0, @id1){{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2 } });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND id IN (@id0, @id1)", sql);
    }

    [TestMethod]
    public void Render_IfNotEmpty_ExcludesContentWhenListEmpty()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notempty=ids}}AND id IN (@id0){{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int>() });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    [TestMethod]
    public void Render_IfEmpty_IncludesContentWhenEmpty()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} {{if empty=filter}}/* no filter */{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["filter"] = "" });
        Assert.AreEqual("SELECT * FROM [users] /* no filter */", sql);
    }

    #endregion

    #region SqlTemplate - Multiple Conditions

    [TestMethod]
    public void Render_MultipleConditions_AllTrue()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Alice", ["age"] = 25 });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name = @name AND age = @age", sql);
    }

    [TestMethod]
    public void Render_MultipleConditions_FirstTrueSecondFalse()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Alice", ["age"] = null });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name = @name ", sql);
    }

    [TestMethod]
    public void Render_MultipleConditions_FirstFalseSecondTrue()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null, ["age"] = 25 });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1  AND age = @age", sql);
    }

    [TestMethod]
    public void Render_MultipleConditions_AllFalse()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null, ["age"] = null });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1  ", sql);
    }

    [TestMethod]
    public void Render_ThreeConditions_MiddleOnlyTrue()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=a}}AND a=@a{{/if}} {{if notnull=b}}AND b=@b{{/if}} {{if notnull=c}}AND c=@c{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["a"] = null, ["b"] = "x", ["c"] = null });
        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1  AND b=@b ", sql);
    }

    #endregion

    #region SqlTemplate - Mixed Static and Dynamic Placeholders

    [TestMethod]
    public void Render_MixedPlaceholders_ColumnsTableAndCondition()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Alice" });
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[name]"));
        Assert.IsTrue(sql.Contains("[users]"));
        Assert.IsTrue(sql.Contains("AND name = @name"));
    }

    [TestMethod]
    public void Render_MixedPlaceholders_WithLimit()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{if notnull=name}}WHERE name = @name{{/if}} {{limit --param limit}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Alice", ["limit"] = 10 });
        Assert.IsTrue(sql.Contains("WHERE name = @name"));
        Assert.IsTrue(sql.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void Render_MixedPlaceholders_ConditionFalseWithLimit()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{if notnull=name}}WHERE name = @name{{/if}} {{limit --param limit}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null, ["limit"] = 10 });
        Assert.IsFalse(sql.Contains("WHERE name = @name"));
        Assert.IsTrue(sql.Contains("LIMIT 10"));
    }

    #endregion

    #region SqlTemplate - Complex Real-World Scenarios

    [TestMethod]
    public void Render_DynamicSearch_AllFiltersProvided()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=name}}AND name LIKE @name{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}} {{if notnull=maxAge}}AND age <= @maxAge{{/if}} ORDER BY id {{limit --param limit}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "%test%",
            ["minAge"] = 18,
            ["maxAge"] = 65,
            ["limit"] = 100,
        });
        Assert.IsTrue(sql.Contains("AND name LIKE @name"));
        Assert.IsTrue(sql.Contains("AND age >= @minAge"));
        Assert.IsTrue(sql.Contains("AND age <= @maxAge"));
        Assert.IsTrue(sql.Contains("LIMIT 100"));
    }

    [TestMethod]
    public void Render_DynamicSearch_OnlyNameFilter()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=name}}AND name LIKE @name{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}} {{if notnull=maxAge}}AND age <= @maxAge{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "%test%",
            ["minAge"] = null,
            ["maxAge"] = null,
        });
        Assert.IsTrue(sql.Contains("AND name LIKE @name"));
        Assert.IsFalse(sql.Contains("AND age >="));
        Assert.IsFalse(sql.Contains("AND age <="));
    }

    [TestMethod]
    public void Render_DynamicSearch_OnlyAgeRange()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=name}}AND name LIKE @name{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}} {{if notnull=maxAge}}AND age <= @maxAge{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "",
            ["minAge"] = 18,
            ["maxAge"] = 65,
        });
        Assert.IsFalse(sql.Contains("AND name LIKE"));
        Assert.IsTrue(sql.Contains("AND age >= @minAge"));
        Assert.IsTrue(sql.Contains("AND age <= @maxAge"));
    }

    [TestMethod]
    public void Render_DynamicSearch_NoFilters()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=name}}AND name LIKE @name{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "",
            ["minAge"] = null,
        });
        Assert.AreEqual("SELECT [id], [name], [age], [email] FROM [users] WHERE 1=1  ", sql);
    }

    [TestMethod]
    public void Render_OptionalJoin_WithCondition()
    {
        var template = SqlTemplate.Prepare(
            "SELECT u.* FROM {{table}} u {{if notnull=includeOrders}}JOIN orders o ON u.id = o.user_id{{/if}} WHERE 1=1",
            Context);
        
        var sqlWithJoin = template.Render(new Dictionary<string, object?> { ["includeOrders"] = true });
        Assert.IsTrue(sqlWithJoin.Contains("JOIN orders"));
        
        var sqlWithoutJoin = template.Render(new Dictionary<string, object?> { ["includeOrders"] = null });
        Assert.IsFalse(sqlWithoutJoin.Contains("JOIN orders"));
    }

    #endregion

    #region SqlTemplate - Edge Cases

    [TestMethod]
    public void Render_EmptyParameters_TreatsAsNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} {{if notnull=missing}}WHERE x = @missing{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?>());
        Assert.AreEqual("SELECT * FROM [users] ", sql);
    }

    [TestMethod]
    public void Render_NullParametersDictionary_TreatsAsAllNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} {{if notnull=x}}WHERE x = @x{{/if}}",
            Context);
        var sql = template.Render(null);
        Assert.AreEqual("SELECT * FROM [users] ", sql);
    }

    [TestMethod]
    public void Render_ConditionAtStart()
    {
        var template = SqlTemplate.Prepare(
            "{{if notnull=prefix}}@prefix: {{/if}}SELECT * FROM {{table}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["prefix"] = "DEBUG" });
        Assert.AreEqual("@prefix: SELECT * FROM [users]", sql);
    }

    [TestMethod]
    public void Render_ConditionAtEnd()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}}{{if notnull=suffix}} -- @suffix{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["suffix"] = "comment" });
        Assert.AreEqual("SELECT * FROM [users] -- @suffix", sql);
    }

    [TestMethod]
    public void Render_AdjacentConditions()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE {{if notnull=a}}a=@a{{/if}}{{if notnull=b}}b=@b{{/if}}",
            Context);
        var sql = template.Render(new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 });
        Assert.AreEqual("SELECT * FROM [users] WHERE a=@ab=@b", sql);
    }

    [TestMethod]
    public void Render_ConditionWithOnlyWhitespace()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}}{{if notnull=x}}   {{/if}}WHERE 1=1",
            Context);
        var sqlTrue = template.Render(new Dictionary<string, object?> { ["x"] = 1 });
        Assert.AreEqual("SELECT * FROM [users]   WHERE 1=1", sqlTrue);
        
        var sqlFalse = template.Render(new Dictionary<string, object?> { ["x"] = null });
        Assert.AreEqual("SELECT * FROM [users]WHERE 1=1", sqlFalse);
    }

    #endregion

    #region PlaceholderProcessor - Block Tag Registration

    [TestMethod]
    public void PlaceholderProcessor_IsBlockClosingTag_ReturnsTrueForSlashIf()
    {
        Assert.IsTrue(PlaceholderProcessor.IsBlockClosingTag("/if"));
    }

    [TestMethod]
    public void PlaceholderProcessor_IsBlockClosingTag_ReturnsFalseForRegularTag()
    {
        Assert.IsFalse(PlaceholderProcessor.IsBlockClosingTag("columns"));
        Assert.IsFalse(PlaceholderProcessor.IsBlockClosingTag("if"));
    }

    [TestMethod]
    public void PlaceholderProcessor_TryGetHandler_ReturnsIfHandler()
    {
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("if", out var handler));
        Assert.IsInstanceOfType(handler, typeof(IfPlaceholderHandler));
    }

    #endregion

    #region Error Handling

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ShouldInclude_InvalidCondition_ThrowsException()
    {
        var handler = IfPlaceholderHandler.Instance;
        handler.ShouldInclude("invalid=param", new Dictionary<string, object?>());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Prepare_UnknownPlaceholder_ThrowsException()
    {
        SqlTemplate.Prepare("SELECT * FROM {{unknown}}", Context);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void IfPlaceholderHandler_Process_ThrowsException()
    {
        IfPlaceholderHandler.Instance.Process(Context, "notnull=x");
    }

    #endregion

    #region HasDynamicPlaceholders Property

    [TestMethod]
    public void HasDynamicPlaceholders_TrueWhenHasIfBlock()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} {{if notnull=x}}WHERE x=@x{{/if}}",
            Context);
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void HasDynamicPlaceholders_TrueWhenHasLimit()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} {{limit --param n}}",
            Context);
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void HasDynamicPlaceholders_FalseWhenOnlyStatic()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}}",
            Context);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    #endregion
}
