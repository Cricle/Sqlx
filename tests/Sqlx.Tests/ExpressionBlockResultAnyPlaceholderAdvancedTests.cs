// <copyright file="ExpressionBlockResultAnyPlaceholderAdvancedTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Expressions;
using Sqlx.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Advanced tests for ExpressionBlockResult with Any placeholder support.
/// Covers edge cases, complex scenarios, and error handling.
/// </summary>
[TestClass]
public class ExpressionBlockResultAnyPlaceholderAdvancedTests
{
    public class Product
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Description { get; set; }
        public double Rating { get; set; }
    }

    #region Complex Expression Tests

    [TestMethod]
    public void Parse_NestedAnyPlaceholders_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            ((p.Price >= Any.Value<decimal>("minPrice") && p.Price <= Any.Value<decimal>("maxPrice")) ||
             p.Category == Any.Value<string>("category")) &&
            p.IsActive;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 10.0m)
            .WithParameter("maxPrice", 100.0m)
            .WithParameter("category", "Electronics");

        // Assert
        Assert.AreEqual("((([price] >= @minPrice AND [price] <= @maxPrice) OR [category] = @category) AND [is_active] = 1)", result.Sql);
        Assert.AreEqual(3, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minPrice", 10.0m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@maxPrice", 100.0m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@category", "Electronics");
    }

    [TestMethod]
    public void Parse_AnyPlaceholderInMathExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price * Any.Value<decimal>("multiplier") > Any.Value<decimal>("threshold");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("multiplier", 1.2m)
            .WithParameter("threshold", 50.0m);

        // Assert
        Assert.AreEqual("([price] * @multiplier) > @threshold", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@multiplier", 1.2m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@threshold", 50.0m);
    }

    [TestMethod]
    public void Parse_AnyPlaceholderWithStringFunction_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Name.ToLower().Contains(Any.Value<string>("searchTerm").ToLower());

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("searchTerm", "laptop");

        // Assert
        // Note: The exact SQL may vary based on how Contains is translated
        Assert.IsTrue(result.Sql.Contains("LOWER"));
        Assert.IsTrue(result.Sql.Contains("@searchTerm"));
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@searchTerm", "laptop");
    }

    [TestMethod]
    public void ParseUpdate_AnyPlaceholderWithExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<Product, Product>> updateExpr = p => new Product
        {
            Price = p.Price * Any.Value<decimal>("priceMultiplier"),
            Stock = p.Stock + Any.Value<int>("stockIncrement")
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite)
            .WithParameter("priceMultiplier", 1.1m)
            .WithParameter("stockIncrement", 10);

        // Assert
        Assert.AreEqual("[price] = ([price] * @priceMultiplier), [stock] = ([stock] + @stockIncrement)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@priceMultiplier", 1.1m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@stockIncrement", 10);
    }

    [TestMethod]
    public void ParseUpdate_MixedAnyAndConstant_GeneratesCorrectSql()
    {
        // Arrange
        var now = DateTime.UtcNow;
        Expression<Func<Product, Product>> updateExpr = p => new Product
        {
            Price = Any.Value<decimal>("newPrice"),
            UpdatedAt = now,
            IsActive = Any.Value<bool>("isActive")
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite)
            .WithParameter("newPrice", 99.99m)
            .WithParameter("isActive", true);

        // Assert
        Assert.AreEqual("[price] = @newPrice, [updated_at] = @p0, [is_active] = @isActive", result.Sql);
        Assert.AreEqual(3, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newPrice", 99.99m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@p0", now);
        SqlAssertions.AssertParametersContain(result.Parameters, "@isActive", true);
    }

    #endregion

    #region Data Type Tests

    [TestMethod]
    public void Parse_AnyPlaceholderWithDecimal_HandlesCorrectly()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price == Any.Value<decimal>("exactPrice");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("exactPrice", 123.456m);

        // Assert
        Assert.AreEqual("[price] = @exactPrice", result.Sql);
        SqlAssertions.AssertParametersContain(result.Parameters, "@exactPrice", 123.456m);
    }

    [TestMethod]
    public void Parse_AnyPlaceholderWithDouble_HandlesCorrectly()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Rating >= Any.Value<double>("minRating");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minRating", 4.5);

        // Assert
        Assert.AreEqual("[rating] >= @minRating", result.Sql);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minRating", 4.5);
    }

    [TestMethod]
    public void Parse_AnyPlaceholderWithDateTime_HandlesCorrectly()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Expression<Func<Product, bool>> predicate = p => 
            p.CreatedAt >= Any.Value<DateTime>("startDate");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("startDate", testDate);

        // Assert
        Assert.AreEqual("[created_at] >= @startDate", result.Sql);
        SqlAssertions.AssertParametersContain(result.Parameters, "@startDate", testDate);
    }

    [TestMethod]
    public void Parse_AnyPlaceholderWithNullableType_HandlesCorrectly()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.UpdatedAt == Any.Value<DateTime?>("updateDate");

        // Act - with null value
        var result1 = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("updateDate", null);

        // Assert
        Assert.AreEqual("[updated_at] = @updateDate", result1.Sql);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@updateDate", null);

        // Act - with non-null value
        var testDate = DateTime.UtcNow;
        var result2 = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("updateDate", testDate);

        // Assert
        Assert.AreEqual("[updated_at] = @updateDate", result2.Sql);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@updateDate", testDate);
    }

    [TestMethod]
    public void Parse_AnyPlaceholderWithBool_HandlesCorrectly()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.IsActive == Any.Value<bool>("activeStatus");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("activeStatus", true);

        // Assert
        Assert.AreEqual("[is_active] = @activeStatus", result.Sql);
        SqlAssertions.AssertParametersContain(result.Parameters, "@activeStatus", true);
    }

    #endregion

    #region Placeholder Management Tests

    [TestMethod]
    public void GetPlaceholderNames_ReturnsAllPlaceholders()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price >= Any.Value<decimal>("minPrice") &&
            p.Price <= Any.Value<decimal>("maxPrice") &&
            p.Category == Any.Value<string>("category") &&
            p.Stock > Any.Value<int>("minStock");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);
        var placeholders = result.GetPlaceholderNames();

        // Assert
        Assert.AreEqual(4, placeholders.Count);
        Assert.IsTrue(placeholders.Contains("minPrice"));
        Assert.IsTrue(placeholders.Contains("maxPrice"));
        Assert.IsTrue(placeholders.Contains("category"));
        Assert.IsTrue(placeholders.Contains("minStock"));
    }

    [TestMethod]
    public void AreAllPlaceholdersFilled_ReturnsFalseWhenNotFilled()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price >= Any.Value<decimal>("minPrice") &&
            p.Category == Any.Value<string>("category");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(result.AreAllPlaceholdersFilled());

        // Fill one placeholder
        result.WithParameter("minPrice", 10.0m);
        Assert.IsFalse(result.AreAllPlaceholdersFilled());

        // Fill all placeholders
        result.WithParameter("category", "Electronics");
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void WithParameter_CanUpdateExistingParameter()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("threshold");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("threshold", 50.0m);

        // Assert
        SqlAssertions.AssertParametersContain(result.Parameters, "@threshold", 50.0m);

        // Update parameter
        result.WithParameter("threshold", 100.0m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@threshold", 100.0m);
    }

    [TestMethod]
    public void WithParameters_FillsMultiplePlaceholders()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price >= Any.Value<decimal>("minPrice") &&
            p.Price <= Any.Value<decimal>("maxPrice") &&
            p.Category == Any.Value<string>("category");

        var parameters = new Dictionary<string, object?>
        {
            ["minPrice"] = 10.0m,
            ["maxPrice"] = 100.0m,
            ["category"] = "Electronics"
        };

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameters(parameters);

        // Assert
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
        SqlAssertions.AssertParametersContain(result.Parameters, "@minPrice", 10.0m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@maxPrice", 100.0m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@category", "Electronics");
    }

    [TestMethod]
    public void WithParameters_NullDictionary_DoesNotThrow()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("threshold");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameters(null!);

        // Assert
        Assert.IsFalse(result.AreAllPlaceholdersFilled());
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void WithParameter_NonExistentPlaceholder_ThrowsArgumentException()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("threshold");

        // Act & Assert
        ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("nonExistent", 100.0m);
    }

    [TestMethod]
    public void WithParameter_NonExistentPlaceholder_ExceptionContainsAvailablePlaceholders()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("threshold") &&
            p.Stock > Any.Value<int>("minStock");

        try
        {
            // Act
            ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
                .WithParameter("wrongName", 100);

            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException ex)
        {
            // Assert
            Assert.IsTrue(ex.Message.Contains("wrongName"));
            Assert.IsTrue(ex.Message.Contains("threshold"));
            Assert.IsTrue(ex.Message.Contains("minStock"));
        }
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AnyValue_DirectExecution_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var value = Any.Value<int>("test");
    }

    [TestMethod]
    public void AnyValue_DirectExecution_ExceptionMessageIsDescriptive()
    {
        try
        {
            // Act
            var value = Any.Value<string>("testPlaceholder");
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException ex)
        {
            // Assert
            Assert.IsTrue(ex.Message.Contains("Any.Value"));
            Assert.IsTrue(ex.Message.Contains("testPlaceholder"));
            Assert.IsTrue(ex.Message.Contains("placeholder"));
            Assert.IsTrue(ex.Message.Contains("should not be executed directly"));
            Assert.IsTrue(ex.Message.Contains("expression trees"));
        }
    }

    #endregion

    #region Dialect-Specific Tests

    [TestMethod]
    public void Parse_AllDialects_GenerateCorrectParameterPrefixes()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("minPrice");

        // Act & Assert - SQLite
        var sqlite = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 10.0m);
        Assert.AreEqual("[price] > @minPrice", sqlite.Sql);
        Assert.IsTrue(sqlite.Parameters.ContainsKey("@minPrice"));

        // PostgreSQL
        var postgres = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.PostgreSql)
            .WithParameter("minPrice", 10.0m);
        Assert.AreEqual("\"price\" > @minPrice", postgres.Sql);
        Assert.IsTrue(postgres.Parameters.ContainsKey("@minPrice"));

        // MySQL
        var mysql = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.MySql)
            .WithParameter("minPrice", 10.0m);
        Assert.AreEqual("`price` > @minPrice", mysql.Sql);
        Assert.IsTrue(mysql.Parameters.ContainsKey("@minPrice"));

        // SQL Server
        var sqlserver = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SqlServer)
            .WithParameter("minPrice", 10.0m);
        Assert.AreEqual("[price] > @minPrice", sqlserver.Sql);
        Assert.IsTrue(sqlserver.Parameters.ContainsKey("@minPrice"));

        // Oracle
        var oracle = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.Oracle)
            .WithParameter("minPrice", 10.0m);
        Assert.AreEqual("\"price\" > :minPrice", oracle.Sql);
        Assert.IsTrue(oracle.Parameters.ContainsKey(":minPrice"));
    }

    [TestMethod]
    public void ParseUpdate_AllDialects_GenerateCorrectSql()
    {
        // Arrange
        Expression<Func<Product, Product>> updateExpr = p => new Product
        {
            Price = Any.Value<decimal>("newPrice")
        };

        // Act & Assert - SQLite
        var sqlite = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite)
            .WithParameter("newPrice", 99.99m);
        Assert.AreEqual("[price] = @newPrice", sqlite.Sql);

        // PostgreSQL
        var postgres = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.PostgreSql)
            .WithParameter("newPrice", 99.99m);
        Assert.AreEqual("\"price\" = @newPrice", postgres.Sql);

        // MySQL
        var mysql = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.MySql)
            .WithParameter("newPrice", 99.99m);
        Assert.AreEqual("`price` = @newPrice", mysql.Sql);

        // Oracle
        var oracle = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.Oracle)
            .WithParameter("newPrice", 99.99m);
        Assert.AreEqual("\"price\" = :newPrice", oracle.Sql);
    }

    #endregion

    #region Reusability Tests

    [TestMethod]
    public void Parse_SameTemplateMultipleTimes_GeneratesIndependentResults()
    {
        // Arrange
        Expression<Func<Product, bool>> template = p => 
            p.Price >= Any.Value<decimal>("minPrice") &&
            p.Price <= Any.Value<decimal>("maxPrice");

        // Act - Create two independent results
        var result1 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 10.0m)
            .WithParameter("maxPrice", 50.0m);

        var result2 = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 100.0m)
            .WithParameter("maxPrice", 500.0m);

        // Assert - Results are independent
        SqlAssertions.AssertParametersContain(result1.Parameters, "@minPrice", 10.0m);
        SqlAssertions.AssertParametersContain(result1.Parameters, "@maxPrice", 50.0m);

        SqlAssertions.AssertParametersContain(result2.Parameters, "@minPrice", 100.0m);
        SqlAssertions.AssertParametersContain(result2.Parameters, "@maxPrice", 500.0m);

        // SQL should be the same
        Assert.AreEqual(result1.Sql, result2.Sql);
    }

    [TestMethod]
    public void Parse_TemplateWithDifferentDialects_GeneratesDifferentSql()
    {
        // Arrange
        Expression<Func<Product, bool>> template = p => 
            p.Price > Any.Value<decimal>("threshold");

        // Act
        var sqlite = ExpressionBlockResult.Parse(template.Body, SqlDefine.SQLite);
        var postgres = ExpressionBlockResult.Parse(template.Body, SqlDefine.PostgreSql);
        var mysql = ExpressionBlockResult.Parse(template.Body, SqlDefine.MySql);

        // Assert - SQL is different for each dialect
        Assert.AreNotEqual(sqlite.Sql, postgres.Sql);
        Assert.AreNotEqual(sqlite.Sql, mysql.Sql);
        Assert.AreNotEqual(postgres.Sql, mysql.Sql);

        // But placeholder names are the same
        Assert.AreEqual(1, sqlite.GetPlaceholderNames().Count);
        Assert.AreEqual(1, postgres.GetPlaceholderNames().Count);
        Assert.AreEqual(1, mysql.GetPlaceholderNames().Count);
        Assert.AreEqual("threshold", sqlite.GetPlaceholderNames()[0]);
        Assert.AreEqual("threshold", postgres.GetPlaceholderNames()[0]);
        Assert.AreEqual("threshold", mysql.GetPlaceholderNames()[0]);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Parse_NoPlaceholders_WorksNormally()
    {
        // Arrange
        var minPrice = 10.0m;
        Expression<Func<Product, bool>> predicate = p => p.Price > minPrice;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[price] > @p0", result.Sql);
        Assert.AreEqual(0, result.GetPlaceholderNames().Count);
        Assert.IsTrue(result.AreAllPlaceholdersFilled()); // No placeholders to fill
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@p0", 10.0m);
    }

    [TestMethod]
    public void Parse_OnlyPlaceholders_NoConstantParameters()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("minPrice");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[price] > @minPrice", result.Sql);
        Assert.AreEqual(1, result.GetPlaceholderNames().Count);
        Assert.AreEqual(0, result.Parameters.Count); // No parameters until filled
    }

    [TestMethod]
    public void Parse_SamePlaceholderNameMultipleTimes_UsesConsistentParameterName()
    {
        // Arrange
        Expression<Func<Product, bool>> predicate = p => 
            p.Price >= Any.Value<decimal>("threshold") &&
            p.Stock >= Any.Value<int>("threshold"); // Same name, different type

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        // Both should use @threshold parameter name
        Assert.IsTrue(result.Sql.Contains("@threshold"));
        Assert.AreEqual(1, result.GetPlaceholderNames().Count);
        Assert.AreEqual("threshold", result.GetPlaceholderNames()[0]);
    }

    [TestMethod]
    public void ParseUpdate_EmptyMemberInit_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<Product, Product>> updateExpr = p => new Product { };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual(string.Empty, result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
        Assert.AreEqual(0, result.GetPlaceholderNames().Count);
    }

    [TestMethod]
    public void Parse_NullExpression_ReturnsEmpty()
    {
        // Act
        var result = ExpressionBlockResult.Parse(null, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual(string.Empty, result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
        Assert.AreEqual(0, result.GetPlaceholderNames().Count);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void ParseUpdate_NullExpression_ReturnsEmpty()
    {
        // Act
        var result = ExpressionBlockResult.ParseUpdate<Product>(null, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual(string.Empty, result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
        Assert.AreEqual(0, result.GetPlaceholderNames().Count);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    #endregion

    #region Integration with Constant Parameters

    [TestMethod]
    public void Parse_MixedPlaceholdersAndConstants_BothWorkCorrectly()
    {
        // Arrange
        var constantValue = 100;
        Expression<Func<Product, bool>> predicate = p => 
            p.Price > Any.Value<decimal>("minPrice") &&
            p.Stock > constantValue &&
            p.Category == Any.Value<string>("category");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minPrice", 50.0m)
            .WithParameter("category", "Electronics");

        // Assert
        Assert.AreEqual("(([price] > @minPrice AND [stock] > @p0) AND [category] = @category)", result.Sql);
        Assert.AreEqual(3, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minPrice", 50.0m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@p0", 100); // Constant parameter
        SqlAssertions.AssertParametersContain(result.Parameters, "@category", "Electronics");
    }

    [TestMethod]
    public void ParseUpdate_MixedPlaceholdersAndConstants_BothWorkCorrectly()
    {
        // Arrange
        var constantDate = DateTime.UtcNow;
        Expression<Func<Product, Product>> updateExpr = p => new Product
        {
            Price = Any.Value<decimal>("newPrice"),
            UpdatedAt = constantDate,
            Category = Any.Value<string>("newCategory")
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite)
            .WithParameter("newPrice", 99.99m)
            .WithParameter("newCategory", "Books");

        // Assert
        Assert.AreEqual("[price] = @newPrice, [updated_at] = @p0, [category] = @newCategory", result.Sql);
        Assert.AreEqual(3, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newPrice", 99.99m);
        SqlAssertions.AssertParametersContain(result.Parameters, "@p0", constantDate);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newCategory", "Books");
    }

    #endregion
}
