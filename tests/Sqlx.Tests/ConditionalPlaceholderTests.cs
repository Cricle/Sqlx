// <copyright file="ConditionalPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for conditional placeholders ({{if}}, {{/if}}) and IN clause ({{in}}).
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
        });

    #region IN Placeholder Tests

    [TestMethod]
    public void In_WithList_GeneratesParenthesizedParameters()
    {
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id IN {{in --param ids}}", Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } });

        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (@ids_0, @ids_1, @ids_2)", sql);
    }

    [TestMethod]
    public void In_WithSingleItem_GeneratesSingleParameter()
    {
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id IN {{in --param ids}}", Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int> { 42 } });

        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (@ids_0)", sql);
    }

    [TestMethod]
    public void In_WithEmptyList_GeneratesNull()
    {
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id IN {{in --param ids}}", Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int>() });

        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (NULL)", sql);
    }

    [TestMethod]
    public void In_WithNullValue_GeneratesNull()
    {
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id IN {{in --param ids}}", Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = null });

        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (NULL)", sql);
    }

    [TestMethod]
    public void In_WithArray_GeneratesParenthesizedParameters()
    {
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id IN {{in --param ids}}", Context);
        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new[] { 10, 20, 30, 40, 50 } });

        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (@ids_0, @ids_1, @ids_2, @ids_3, @ids_4)", sql);
    }

    #endregion

    #region If Null/NotNull Tests

    [TestMethod]
    public void If_NotNull_IncludesContentWhenNotNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Alice" });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name = @name", sql);
    }

    [TestMethod]
    public void If_NotNull_ExcludesContentWhenNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    [TestMethod]
    public void If_Null_IncludesContentWhenNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if null=name}}AND name IS NULL{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["name"] = null });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name IS NULL", sql);
    }

    [TestMethod]
    public void If_Null_ExcludesContentWhenNotNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if null=name}}AND name IS NULL{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["name"] = "Bob" });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    #endregion

    #region If Empty/NotEmpty Tests

    [TestMethod]
    public void If_NotEmpty_IncludesContentWhenListHasItems()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notempty=ids}}AND id IN {{in --param ids}}{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND id IN (@ids_0, @ids_1, @ids_2)", sql);
    }

    [TestMethod]
    public void If_NotEmpty_ExcludesContentWhenListEmpty()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notempty=ids}}AND id IN {{in --param ids}}{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int>() });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    [TestMethod]
    public void If_Empty_IncludesContentWhenListEmpty()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} {{if empty=ids}}/* no filter */{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["ids"] = new List<int>() });

        Assert.AreEqual("SELECT * FROM [users] /* no filter */", sql);
    }

    [TestMethod]
    public void If_NotEmpty_WithString_IncludesWhenNotEmpty()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notempty=search}}AND name LIKE @search{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["search"] = "%test%" });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name LIKE @search", sql);
    }

    [TestMethod]
    public void If_NotEmpty_WithEmptyString_ExcludesContent()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notempty=search}}AND name LIKE @search{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?> { ["search"] = "" });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    #endregion

    #region Multiple Conditions Tests

    [TestMethod]
    public void MultipleConditions_AllTrue()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = 25,
        });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name = @name AND age = @age", sql);
    }

    [TestMethod]
    public void MultipleConditions_SomeTrue()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = null,
        });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 AND name = @name ", sql);
    }

    [TestMethod]
    public void MultipleConditions_AllFalse()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}} {{if notnull=age}}AND age = @age{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = null,
            ["age"] = null,
        });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1  ", sql);
    }

    #endregion

    #region Complex Scenarios

    [TestMethod]
    public void ComplexQuery_WithInAndConditions()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=ids}}AND id IN {{in --param ids}}{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}} {{limit --param limit}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["ids"] = new List<long> { 1, 2, 3 },
            ["minAge"] = 18,
            ["limit"] = 100,
        });

        Assert.AreEqual("SELECT [id], [name], [age] FROM [users] WHERE 1=1 AND id IN (@ids_0, @ids_1, @ids_2) AND age >= @minAge LIMIT 100", sql);
    }

    [TestMethod]
    public void ComplexQuery_WithEmptyIdsAndNoMinAge()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=ids}}AND id IN {{in --param ids}}{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}} {{limit --param limit}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["ids"] = new List<long>(),
            ["minAge"] = null,
            ["limit"] = 50,
        });

        Assert.AreEqual("SELECT [id], [name], [age] FROM [users] WHERE 1=1   LIMIT 50", sql);
    }

    [TestMethod]
    public void DynamicSearch_WithOptionalFilters()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=name}}AND name LIKE @name{{/if}} {{if notnull=minAge}}AND age >= @minAge{{/if}} {{if notnull=maxAge}}AND age <= @maxAge{{/if}} ORDER BY id {{limit --param limit}}",
            Context);

        // Only name filter
        var sql1 = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "%test%",
            ["minAge"] = null,
            ["maxAge"] = null,
            ["limit"] = 10,
        });
        StringAssert.Contains(sql1, "AND name LIKE @name");
        Assert.IsFalse(sql1.Contains("AND age >="));
        Assert.IsFalse(sql1.Contains("AND age <="));

        // Only age range
        var sql2 = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "",
            ["minAge"] = 18,
            ["maxAge"] = 65,
            ["limit"] = 10,
        });
        Assert.IsFalse(sql2.Contains("AND name LIKE"));
        StringAssert.Contains(sql2, "AND age >= @minAge");
        StringAssert.Contains(sql2, "AND age <= @maxAge");
    }

    #endregion

    #region Parameter Missing Tests

    [TestMethod]
    public void If_MissingParameter_TreatedAsNull()
    {
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE 1=1 {{if notnull=missing}}AND x = @missing{{/if}}",
            Context);

        var sql = template.Render(new Dictionary<string, object?>());

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1 ", sql);
    }

    #endregion
}
