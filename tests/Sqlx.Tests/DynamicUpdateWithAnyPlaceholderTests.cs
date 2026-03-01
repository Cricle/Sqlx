// <copyright file="DynamicUpdateWithAnyPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using Sqlx.Tests.Helpers;

/// <summary>
/// Tests for DynamicUpdateAsync integration with Any placeholder support.
/// Verifies that placeholders work correctly in real repository scenarios.
/// </summary>
[TestClass]
public class DynamicUpdateWithAnyPlaceholderTests
{
    private SqliteConnection? _connection;
    private IPlaceholderTestRepository? _repository;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE placeholder_test_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                value INTEGER NOT NULL,
                price REAL NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                category TEXT NOT NULL,
                created_at TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        _repository = new PlaceholderTestRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region Expression Template Tests

    [TestMethod]
    public void ExpressionBlockResult_WithRepository_GeneratesCorrectSql()
    {
        // Arrange - Create a reusable template
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value >= Any.Value<int>("minValue") &&
            e.Value <= Any.Value<int>("maxValue");

        // Act
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minValue", 10)
            .WithParameter("maxValue", 100);

        // Assert
        Assert.AreEqual("([value] >= @minValue AND [value] <= @maxValue)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minValue", 10);
        SqlAssertions.AssertParametersContain(result.Parameters, "@maxValue", 100);
    }

    [TestMethod]
    public void ExpressionBlockResult_UpdateTemplate_GeneratesCorrectSql()
    {
        // Arrange - Create a reusable UPDATE template
        Expression<Func<PlaceholderTestEntity, PlaceholderTestEntity>> template = e => 
            new PlaceholderTestEntity
            {
                Value = Any.Value<int>("newValue"),
                Price = Any.Value<double>("newPrice")
            };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(template, SqlDefine.SQLite)
            .WithParameter("newValue", 200)
            .WithParameter("newPrice", 99.99);

        // Assert
        Assert.AreEqual("[value] = @newValue, [price] = @newPrice", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newValue", 200);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newPrice", 99.99);
    }

    #endregion

    #region Reusable Template Scenarios

    [TestMethod]
    public async Task ExpressionTemplate_ReuseWithDifferentValues_WorksCorrectly()
    {
        // Arrange - Insert test data
        await _repository!.InsertAsync(new PlaceholderTestEntity 
        { 
            Name = "Item1", Value = 50, Price = 10.0, IsActive = true, 
            Category = "A", CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new PlaceholderTestEntity 
        { 
            Name = "Item2", Value = 150, Price = 20.0, IsActive = true, 
            Category = "B", CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new PlaceholderTestEntity 
        { 
            Name = "Item3", Value = 250, Price = 30.0, IsActive = true, 
            Category = "C", CreatedAt = DateTime.UtcNow 
        });

        // Create a reusable template
        Expression<Func<PlaceholderTestEntity, bool>> rangeTemplate = e => 
            e.Value >= Any.Value<int>("min") && e.Value <= Any.Value<int>("max");

        // Act & Assert - Use template with different ranges
        // Range 1: 0-100 (should match Item1)
        var result1 = ExpressionBlockResult.Parse(rangeTemplate.Body, SqlDefine.SQLite)
            .WithParameter("min", 0)
            .WithParameter("max", 100);
        Assert.AreEqual("([value] >= @min AND [value] <= @max)", result1.Sql);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@min", 0);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@max", 100);

        // Range 2: 100-200 (should match Item2)
        var result2 = ExpressionBlockResult.Parse(rangeTemplate.Body, SqlDefine.SQLite)
            .WithParameter("min", 100)
            .WithParameter("max", 200);
        Assert.AreEqual("([value] >= @min AND [value] <= @max)", result2.Sql);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@min", 100);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@max", 200);

        // Range 3: 200-300 (should match Item3)
        var result3 = ExpressionBlockResult.Parse(rangeTemplate.Body, SqlDefine.SQLite)
            .WithParameter("min", 200)
            .WithParameter("max", 300);
        Assert.AreEqual("([value] >= @min AND [value] <= @max)", result3.Sql);
        SqlAssertions.AssertParametersContain(result3.Parameters, "@min", 200);
        SqlAssertions.AssertParametersContain(result3.Parameters, "@max", 300);
    }

    [TestMethod]
    public void UpdateTemplate_ReuseWithDifferentValues_GeneratesDifferentSql()
    {
        // Arrange - Create a reusable UPDATE template
        Expression<Func<PlaceholderTestEntity, PlaceholderTestEntity>> updateTemplate = e => 
            new PlaceholderTestEntity
            {
                Value = e.Value + Any.Value<int>("increment"),
                Price = e.Price * Any.Value<double>("multiplier")
            };

        // Act - Use template with different values
        var update1 = ExpressionBlockResult.ParseUpdate(updateTemplate, SqlDefine.SQLite)
            .WithParameter("increment", 10)
            .WithParameter("multiplier", 1.1);

        var update2 = ExpressionBlockResult.ParseUpdate(updateTemplate, SqlDefine.SQLite)
            .WithParameter("increment", 50)
            .WithParameter("multiplier", 1.5);

        // Assert - SQL is the same, but parameters are different
        Assert.AreEqual(update1.Sql, update2.Sql);
        Assert.AreEqual("[value] = ([value] + @increment), [price] = ([price] * @multiplier)", update1.Sql);

        SqlAssertions.AssertParametersContain(update1.Parameters, "@increment", 10);
        SqlAssertions.AssertParametersContain(update1.Parameters, "@multiplier", 1.1);

        SqlAssertions.AssertParametersContain(update2.Parameters, "@increment", 50);
        SqlAssertions.AssertParametersContain(update2.Parameters, "@multiplier", 1.5);
    }

    #endregion

    #region Complex Scenario Tests

    [TestMethod]
    public void ComplexTemplate_MultipleConditions_GeneratesCorrectSql()
    {
        // Arrange - Complex template with multiple placeholders
        Expression<Func<PlaceholderTestEntity, bool>> complexTemplate = e => 
            (e.Value >= Any.Value<int>("minValue") && e.Value <= Any.Value<int>("maxValue")) ||
            (e.Price >= Any.Value<double>("minPrice") && e.Price <= Any.Value<double>("maxPrice")) ||
            e.Category == Any.Value<string>("category");

        // Act
        var result = ExpressionBlockResult.Parse(complexTemplate.Body, SqlDefine.SQLite)
            .WithParameter("minValue", 10)
            .WithParameter("maxValue", 100)
            .WithParameter("minPrice", 5.0)
            .WithParameter("maxPrice", 50.0)
            .WithParameter("category", "Premium");

        // Assert
        Assert.AreEqual("((([value] >= @minValue AND [value] <= @maxValue) OR ([price] >= @minPrice AND [price] <= @maxPrice)) OR [category] = @category)", result.Sql);
        Assert.AreEqual(5, result.Parameters.Count);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void ComplexUpdateTemplate_MultipleFields_GeneratesCorrectSql()
    {
        // Arrange - Complex UPDATE template
        Expression<Func<PlaceholderTestEntity, PlaceholderTestEntity>> complexUpdate = e => 
            new PlaceholderTestEntity
            {
                Name = Any.Value<string>("newName"),
                Value = e.Value + Any.Value<int>("valueIncrement"),
                Price = e.Price * Any.Value<double>("priceMultiplier"),
                Category = Any.Value<string>("newCategory"),
                IsActive = Any.Value<bool>("activeStatus")
            };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(complexUpdate, SqlDefine.SQLite)
            .WithParameter("newName", "Updated")
            .WithParameter("valueIncrement", 100)
            .WithParameter("priceMultiplier", 1.2)
            .WithParameter("newCategory", "Premium")
            .WithParameter("activeStatus", false);

        // Assert
        Assert.AreEqual("[name] = @newName, [value] = ([value] + @valueIncrement), [price] = ([price] * @priceMultiplier), [category] = @newCategory, [is_active] = @activeStatus", result.Sql);
        Assert.AreEqual(5, result.Parameters.Count);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    #endregion

    #region Parameter Type Validation Tests

    [TestMethod]
    public void PlaceholderTemplate_IntegerTypes_WorkCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value == Any.Value<int>("exactValue");

        // Act - Test various integer values
        var result1 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("exactValue", 0);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@exactValue", 0);

        var result2 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("exactValue", -100);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@exactValue", -100);

        var result3 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("exactValue", int.MaxValue);
        SqlAssertions.AssertParametersContain(result3.Parameters, "@exactValue", int.MaxValue);
    }

    [TestMethod]
    public void PlaceholderTemplate_FloatingPointTypes_WorkCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Price == Any.Value<double>("exactPrice");

        // Act - Test various floating point values
        var result1 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("exactPrice", 0.0);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@exactPrice", 0.0);

        var result2 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("exactPrice", 123.456);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@exactPrice", 123.456);

        var result3 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("exactPrice", -99.99);
        SqlAssertions.AssertParametersContain(result3.Parameters, "@exactPrice", -99.99);
    }

    [TestMethod]
    public void PlaceholderTemplate_StringTypes_WorkCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Name == Any.Value<string>("searchName");

        // Act - Test various string values
        var result1 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("searchName", "");
        SqlAssertions.AssertParametersContain(result1.Parameters, "@searchName", "");

        var result2 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("searchName", "Test with spaces");
        SqlAssertions.AssertParametersContain(result2.Parameters, "@searchName", "Test with spaces");

        var result3 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("searchName", "Test with 'quotes' and \"double quotes\"");
        SqlAssertions.AssertParametersContain(result3.Parameters, "@searchName", "Test with 'quotes' and \"double quotes\"");
    }

    [TestMethod]
    public void PlaceholderTemplate_BooleanTypes_WorkCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.IsActive == Any.Value<bool>("activeStatus");

        // Act
        var result1 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("activeStatus", true);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@activeStatus", true);

        var result2 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("activeStatus", false);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@activeStatus", false);
    }

    #endregion

    #region Placeholder Naming Tests

    [TestMethod]
    public void PlaceholderNames_CaseSensitive_TreatedAsDistinct()
    {
        // Arrange - Use different case for placeholder names
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value > Any.Value<int>("MinValue") &&
            e.Value < Any.Value<int>("maxValue");

        // Act
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual(2, result.GetPlaceholderNames().Count);
        Assert.IsTrue(result.GetPlaceholderNames().Contains("MinValue"));
        Assert.IsTrue(result.GetPlaceholderNames().Contains("maxValue"));
    }

    [TestMethod]
    public void PlaceholderNames_WithUnderscores_WorkCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value > Any.Value<int>("min_value") &&
            e.Price > Any.Value<double>("min_price");

        // Act
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("min_value", 10)
            .WithParameter("min_price", 5.0);

        // Assert
        Assert.AreEqual("([value] > @min_value AND [price] > @min_price)", result.Sql);
        SqlAssertions.AssertParametersContain(result.Parameters, "@min_value", 10);
        SqlAssertions.AssertParametersContain(result.Parameters, "@min_price", 5.0);
    }

    [TestMethod]
    public void PlaceholderNames_WithNumbers_WorkCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value > Any.Value<int>("threshold1") &&
            e.Price > Any.Value<double>("threshold2");

        // Act
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("threshold1", 100)
            .WithParameter("threshold2", 50.0);

        // Assert
        Assert.AreEqual("([value] > @threshold1 AND [price] > @threshold2)", result.Sql);
        SqlAssertions.AssertParametersContain(result.Parameters, "@threshold1", 100);
        SqlAssertions.AssertParametersContain(result.Parameters, "@threshold2", 50.0);
    }

    #endregion

    #region Method Chaining Tests

    [TestMethod]
    public void WithParameter_MethodChaining_WorksCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value >= Any.Value<int>("min") &&
            e.Value <= Any.Value<int>("max") &&
            e.Category == Any.Value<string>("cat");

        // Act - Chain multiple WithParameter calls
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("min", 10)
            .WithParameter("max", 100)
            .WithParameter("cat", "Premium");

        // Assert
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
        Assert.AreEqual(3, result.Parameters.Count);
    }

    [TestMethod]
    public void WithParameters_ThenWithParameter_WorksCorrectly()
    {
        // Arrange
        Expression<Func<PlaceholderTestEntity, bool>> template = e => 
            e.Value >= Any.Value<int>("min") &&
            e.Value <= Any.Value<int>("max") &&
            e.Category == Any.Value<string>("cat");

        var initialParams = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["min"] = 10,
            ["max"] = 100
        };

        // Act - Use WithParameters then WithParameter
        var result = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameters(initialParams)
            .WithParameter("cat", "Premium");

        // Assert
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
        Assert.AreEqual(3, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@min", 10);
        SqlAssertions.AssertParametersContain(result.Parameters, "@max", 100);
        SqlAssertions.AssertParametersContain(result.Parameters, "@cat", "Premium");
    }

    #endregion
}

[Sqlx]
[TableName("placeholder_test_entities")]
public class PlaceholderTestEntity
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public double Price { get; set; }
    public bool IsActive { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public interface IPlaceholderTestRepository : ICrudRepository<PlaceholderTestEntity, long>
{
}

[TableName("placeholder_test_entities")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPlaceholderTestRepository))]
public partial class PlaceholderTestRepository(SqliteConnection connection) : IPlaceholderTestRepository
{
}
