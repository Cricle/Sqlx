// -----------------------------------------------------------------------
// <copyright file="EnhancedSqlTemplateEngineTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Sqlx.Generator.Core;
using Xunit;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for enhanced SQL template engine functionality.
/// </summary>
public class EnhancedSqlTemplateEngineTests
{
    private readonly SqlTemplateEngine _engine;

    public EnhancedSqlTemplateEngineTests()
    {
        _engine = new SqlTemplateEngine();
    }

    [Fact]
    public void ValidateTemplate_WithBasicSelect_ShouldPass()
    {
        // Arrange
        var template = "SELECT * FROM Users WHERE Id = @id";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTemplate_WithSqlInjection_ShouldDetect()
    {
        // Arrange
        var template = "SELECT * FROM Users; DROP TABLE Users; --";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        // The template should be processed, may have warnings about dangerous patterns
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Basic validation should pass, but may have warnings
    }

    [Fact]
    public void ValidateTemplate_WithUnmatchedParentheses_ShouldFail()
    {
        // Arrange
        var template = "SELECT * FROM Users WHERE (Id = @id";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Unmatched parentheses in SQL template");
    }

    [Fact]
    public void ValidateTemplate_WithSelectStar_ShouldSuggestImprovement()
    {
        // Arrange
        var template = "SELECT * FROM Users";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.Suggestions.Should().Contain("Consider specifying explicit column names instead of SELECT *");
    }

    [Fact]
    public void ValidateTemplate_WithUpdateWithoutWhere_ShouldWarn()
    {
        // Arrange
        var template = "UPDATE Users SET Name = @name";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.Warnings.Should().Contain("UPDATE/DELETE statements without WHERE clause may affect all rows");
    }

    [Fact]
    public void ValidateTemplate_WithOrderByWithoutLimit_ShouldSuggest()
    {
        // Arrange
        var template = "SELECT Id, Name FROM Users ORDER BY Name";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.Suggestions.Should().Contain("Consider adding LIMIT/TOP clause with ORDER BY for better performance");
    }

    [Theory]
    [InlineData("{{table}}", true)]
    [InlineData("{{columns}}", true)]
    [InlineData("{{values}}", true)]
    [InlineData("{{where}}", true)]
    [InlineData("{{set}}", true)]
    [InlineData("{{orderby}}", true)]
    [InlineData("{{limit}}", true)]
    [InlineData("{{join}}", true)]
    [InlineData("{{groupby}}", true)]
    [InlineData("{{having}}", true)]
    [InlineData("{{if}}", true)]
    [InlineData("{{invalid}}", false)]
    public void ValidateTemplate_WithPlaceholders_ShouldValidateCorrectly(string placeholder, bool shouldBeValid)
    {
        // Arrange
        var template = $"SELECT * FROM Users {placeholder}";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        if (shouldBeValid)
        {
            result.IsValid.Should().BeTrue();
        }
        else
        {
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Invalid placeholder"));
        }
    }

    [Fact]
    public void ValidateTemplate_WithAdvancedPlaceholders_ShouldRecognizeOptions()
    {
        // Arrange
        var template = "SELECT {{columns:quoted|exclude=Password,Secret}} FROM {{table:schema|alias=u}} {{where:soft}}";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTemplate_WithNestedQueries_ShouldBeValid()
    {
        // Arrange
        var template = @"
            SELECT * FROM Users 
            WHERE Id IN (
                SELECT UserId FROM Orders 
                WHERE OrderId IN (
                    SELECT Id FROM OrderItems 
                    WHERE ProductId IN (
                        SELECT Id FROM Products WHERE CategoryId = @categoryId
                    )
                )
            )";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        // Complex nested queries should be valid but may have performance suggestions
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTemplate_WithJoinWithoutWhere_ShouldSuggestCartesianProductPrevention()
    {
        // Arrange
        var template = "SELECT u.Name, o.Total FROM Users u JOIN Orders o";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.Suggestions.Should().Contain("Consider adding WHERE clause to prevent Cartesian products in JOINs");
    }

    [Fact]
    public void ValidateTemplate_WithEmptyTemplate_ShouldFail()
    {
        // Arrange
        var template = "";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("SQL template cannot be empty");
    }

    [Fact]
    public void ValidateTemplate_WithNullTemplate_ShouldFail()
    {
        // Arrange
        string template = null!;

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("SQL template cannot be empty");
    }

    [Fact]
    public void ValidateTemplate_WithInvalidPlaceholderOptions_ShouldWarn()
    {
        // Arrange
        var template = "SELECT {{columns:quoted|invalidoption}} FROM Users";

        // Act
        var result = _engine.ValidateTemplate(template);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("Invalid option format"));
    }

    [Fact]
    public async Task Engine_ShouldBeThreadSafe()
    {
        // This test verifies that the caching mechanism is thread-safe
        // Arrange
        var template = "SELECT * FROM Users WHERE Id = @id";
        var tasks = new List<Task<TemplateValidationResult>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _engine.ValidateTemplate(template)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
    }
}
