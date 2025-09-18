// -----------------------------------------------------------------------
// <copyright file="SqlTemplateIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS0618 // Type or member is obsolete - Testing obsolete API for compatibility

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Core;

/// <summary>
/// Integration tests for SQL template compatibility with existing functionality
/// </summary>
[TestClass]
public class SqlTemplateIntegrationTests : TestBase
{
    [TestMethod]
    public void AdvancedRender_BackwardCompatible_WithOriginalCreate()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id AND Name = @name";
        var parameters = new Dictionary<string, object?> 
        { 
            { "id", 123 }, 
            { "name", "John" } 
        };

        // Act
        var originalTemplate = SqlTemplate.Parse(sql).Execute(parameters);
        var advancedTemplate = SqlTemplate.Render("SELECT * FROM Users WHERE Id = {{id}} AND Name = {{name}}", 
            new { id = 123, name = "John" });

        // Assert
        Assert.AreEqual(originalTemplate.Sql, "SELECT * FROM Users WHERE Id = @id AND Name = @name");
        StringAssert.Contains(advancedTemplate.Sql, "@p0");
        StringAssert.Contains(advancedTemplate.Sql, "@p1");
        
        // Both should have same parameter values, just different names
        Assert.AreEqual(originalTemplate.Parameters["id"], advancedTemplate.Parameters["p0"]);
        Assert.AreEqual(originalTemplate.Parameters["name"], advancedTemplate.Parameters["p1"]);
    }

    [TestMethod]
    public void AdvancedRender_CompatibleWith_CreateWithObject()
    {
        // Arrange & Act
        var originalTemplate = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @Id").Execute(new { Id = 456 });
        var advancedTemplate = SqlTemplate.Render("SELECT * FROM Users WHERE Id = {{Id}}", new { Id = 456 });

        // Assert
        Assert.AreEqual(456, originalTemplate.Parameters["Id"]);
        Assert.AreEqual(456, advancedTemplate.Parameters["p0"]);
    }

    [TestMethod]
    public void AdvancedRender_CompatibleWith_CreateGenericDictionary()
    {
        // Arrange
        var parameters = new Dictionary<string, int> { { "userId", 789 }, { "status", 1 } };

        // Act
        var originalTemplate = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @userId AND Status = @status", parameters);
        var advancedTemplate = SqlTemplate.Render("SELECT * FROM Users WHERE Id = {{userId}} AND Status = {{status}}", 
            new { userId = 789, status = 1 });

        // Assert
        Assert.AreEqual(789, originalTemplate.Parameters["userId"]);
        Assert.AreEqual(1, originalTemplate.Parameters["status"]);
        Assert.AreEqual(789, advancedTemplate.Parameters["p0"]);
        Assert.AreEqual(1, advancedTemplate.Parameters["p1"]);
    }

    [TestMethod]
    public void AdvancedRender_CompatibleWith_CreateSingleParameter()
    {
        // Arrange & Act
        var originalTemplate = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @userId", "userId", 101);
        var advancedTemplate = SqlTemplate.Render("SELECT * FROM Users WHERE Id = {{userId}}", new { userId = 101 });

        // Assert
        Assert.AreEqual(101, originalTemplate.Parameters["userId"]);
        Assert.AreEqual(101, advancedTemplate.Parameters["p0"]);
    }

    [TestMethod]
    public void AdvancedRender_CompatibleWith_CreateTwoParameters()
    {
        // Arrange & Act
        var originalTemplate = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id AND Status = @status", 
            "id", 202, "status", "Active");
        var advancedTemplate = SqlTemplate.Render("SELECT * FROM Users WHERE Id = {{id}} AND Status = {{status}}", 
            new { id = 202, status = "Active" });

        // Assert
        Assert.AreEqual(202, originalTemplate.Parameters["id"]);
        Assert.AreEqual("Active", originalTemplate.Parameters["status"]);
        Assert.AreEqual(202, advancedTemplate.Parameters["p0"]);
        Assert.AreEqual("Active", advancedTemplate.Parameters["p1"]);
    }

    [TestMethod]
    public void AdvancedRender_WorksWith_ExistingToStringImplementation()
    {
        // Arrange
        var originalTemplate = SqlTemplate.Create("SELECT COUNT(*) FROM Users", new Dictionary<string, object?>());
        var advancedTemplate = SqlTemplate.Render("SELECT COUNT(*) FROM Users", new { });

        // Act
        var originalToString = originalTemplate.ToString();
        var advancedToString = advancedTemplate.ToString();

        // Assert
        StringAssert.Contains(originalToString, "SqlTemplate");
        StringAssert.Contains(originalToString, "0 params");
        StringAssert.Contains(advancedToString, "SqlTemplate");
        StringAssert.Contains(advancedToString, "0 params");
    }

    [TestMethod]
    public void AdvancedRender_WorksWith_EmptyTemplate()
    {
        // Arrange & Act
        var originalEmpty = SqlTemplate.Empty;
        var advancedEmpty = SqlTemplate.Render("", new { });

        // Assert
        Assert.AreEqual(string.Empty, originalEmpty.Sql);
        Assert.AreEqual(string.Empty, advancedEmpty.Sql);
        Assert.AreEqual(0, originalEmpty.Parameters.Count);
        Assert.AreEqual(0, advancedEmpty.Parameters.Count);
    }

    [TestMethod]
    public void AdvancedRender_HandlesComplexScenario_WithAllFeatures()
    {
        // Arrange
        var template = @"
            SELECT 
                {{join(',', columns)}},
                {{upper(status)}} as Status
            FROM {{table(tableName)}}
            WHERE 1=1
            {{if hasDateFilter}}
                AND CreatedDate >= {{startDate}}
            {{endif}}
            {{if hasCategories}}
                AND Category IN (
                {{each category in categories}}
                    {{category}}{{if index < count(categories) - 1}},{{endif}}
                {{endeach}}
                )
            {{endif}}
            ORDER BY {{column(orderBy)}}";

        var options = new SqlTemplateOptions 
        { 
            Dialect = SqlDialectType.SqlServer,
            UseParameterizedQueries = true,
            SafeMode = true
        };

        // Act
        var result = SqlTemplate.Render(template, new 
        {
            columns = new[] { "Id", "Name", "Description" },
            status = "active",
            tableName = "Products",
            hasDateFilter = true,
            startDate = new System.DateTime(2023, 1, 1),
            hasCategories = true,
            categories = new[] { "Electronics", "Books", "Clothing" },
            orderBy = "CreatedDate"
        }, options);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.Sql, "ACTIVE as Status");
        StringAssert.Contains(result.Sql, "[Products]");
        StringAssert.Contains(result.Sql, "AND CreatedDate >= @p0");
        StringAssert.Contains(result.Sql, "AND Category IN (");
        StringAssert.Contains(result.Sql, "ORDER BY [CreatedDate]");
        
        // Should have parameters for date and categories
        Assert.IsTrue(result.Parameters.Count >= 4); // date + 3 categories
    }

    [TestMethod]
    public void AdvancedRender_SqlParameterTypes_HandleCorrectly()
    {
        // Arrange
        var template = @"
            SELECT * FROM Users 
            WHERE 
                Id = {{id}}
                AND Name = {{name}}
                AND IsActive = {{isActive}}
                AND CreatedDate = {{createdDate}}
                AND Score = {{score}}
                AND Data = {{data}}";

        var testDate = new System.DateTime(2023, 6, 15, 14, 30, 0);
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SqlTemplate.Render(template, new 
        {
            id = 12345,
            name = "Test User",
            isActive = true,
            createdDate = testDate,
            score = 98.5m,
            data = testData
        });

        // Assert
        Assert.AreEqual(6, result.Parameters.Count);
        Assert.AreEqual(12345, result.Parameters["p0"]);
        Assert.AreEqual("Test User", result.Parameters["p1"]);
        Assert.AreEqual(true, result.Parameters["p2"]);
        Assert.AreEqual(testDate, result.Parameters["p3"]);
        Assert.AreEqual(98.5m, result.Parameters["p4"]);
        CollectionAssert.AreEqual(testData, (byte[])result.Parameters["p5"]!);
    }

    [TestMethod]
    public void AdvancedRender_WithNullValues_HandlesLikeOriginal()
    {
        // Arrange & Act
        var originalTemplate = SqlTemplate.Create("SELECT * FROM Users WHERE Name = @name", "name", (string?)null);
        var advancedTemplate = SqlTemplate.Render("SELECT * FROM Users WHERE Name = {{name}}", new { name = (string?)null });

        // Assert
        Assert.IsNull(originalTemplate.Parameters["name"]);
        Assert.IsNull(advancedTemplate.Parameters["p0"]);
    }
}
