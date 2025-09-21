// -----------------------------------------------------------------------
// <copyright file="SqlTemplateCoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Sqlx;
using Xunit;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for core SqlTemplate functionality.
/// </summary>
public class SqlTemplateCoreTests
{
    [Fact]
    public void SqlTemplate_Constructor_WithValidParameters_ShouldCreate()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        template.Sql.Should().Be(sql);
        template.Parameters.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void SqlTemplate_Constructor_WithNullSql_ShouldCreate()
    {
        // Act
        var template = new SqlTemplate(null!, new Dictionary<string, object?> { { "param1", 1 } });

        // Assert
        template.Sql.Should().BeNull();
        template.Parameters.Should().NotBeNull();
    }

    [Fact]
    public void SqlTemplate_Constructor_WithNullParameters_ShouldCreate()
    {
        // Act
        var template = new SqlTemplate("SELECT 1", null!);

        // Assert
        template.Sql.Should().Be("SELECT 1");
        template.Parameters.Should().BeNull();
    }

    [Fact]
    public void SqlTemplate_Constructor_WithEmptyParameters_ShouldCreate()
    {
        // Act
        var template = new SqlTemplate("SELECT 1", new Dictionary<string, object?>());

        // Assert
        template.Sql.Should().Be("SELECT 1");
        template.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void SqlTemplate_Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var template1 = new SqlTemplate("SELECT * FROM Users", parameters);
        var template2 = new SqlTemplate("SELECT * FROM Users", parameters);

        // Act & Assert
        template1.Should().Be(template2);
        (template1 == template2).Should().BeTrue();
        (template1 != template2).Should().BeFalse();
    }

    [Fact]
    public void SqlTemplate_Equality_WithDifferentSql_ShouldNotBeEqual()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var template1 = new SqlTemplate("SELECT * FROM Users", parameters);
        var template2 = new SqlTemplate("SELECT * FROM Products", parameters);

        // Act & Assert
        template1.Should().NotBe(template2);
        (template1 == template2).Should().BeFalse();
        (template1 != template2).Should().BeTrue();
    }

    [Fact]
    public void SqlTemplate_Equality_WithDifferentParameters_ShouldNotBeEqual()
    {
        // Arrange
        var template1 = new SqlTemplate("SELECT * FROM Users", new Dictionary<string, object?> { { "id", 1 } });
        var template2 = new SqlTemplate("SELECT * FROM Users", new Dictionary<string, object?> { { "name", "John" } });

        // Act & Assert
        template1.Should().NotBe(template2);
    }

    [Fact]
    public void SqlTemplate_GetHashCode_WithSameValues_ShouldBeSame()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var template1 = new SqlTemplate("SELECT * FROM Users", parameters);
        var template2 = new SqlTemplate("SELECT * FROM Users", parameters);

        // Act & Assert
        template1.GetHashCode().Should().Be(template2.GetHashCode());
    }

    [Fact]
    public void SqlTemplate_ToString_ShouldContainSqlInfo()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var template = new SqlTemplate(sql, new Dictionary<string, object?> { { "id", 1 } });

        // Act
        var result = template.ToString();

        // Assert
        result.Should().Contain("SqlTemplate");
        result.Should().Contain(sql);
    }

    [Fact]
    public void SqlTemplate_ToString_WithNullSql_ShouldWork()
    {
        // Arrange
        var template = new SqlTemplate(null!, new Dictionary<string, object?> { { "id", 1 } });

        // Act
        var result = template.ToString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("SqlTemplate");
    }

    [Theory]
    [InlineData("SELECT * FROM Users")]
    [InlineData("INSERT INTO Users (Name) VALUES (@name)")]
    [InlineData("UPDATE Users SET Name = @name WHERE Id = @id")]
    [InlineData("DELETE FROM Users WHERE Id = @id")]
    public void SqlTemplate_WithVariousQueries_ShouldWork(string sql)
    {
        // Act
        var template = new SqlTemplate(sql, new Dictionary<string, object?>());

        // Assert
        template.Sql.Should().Be(sql);
        template.Parameters.Should().NotBeNull();
    }

    [Fact]
    public void SqlTemplate_WithExpression_ShouldCreateFromExpressionToSql()
    {
        // This test demonstrates integration between SqlTemplate and ExpressionToSql
        // Arrange
        var expression = ExpressionToSql<TestEntity>.ForSqlServer()
            .Where(e => e.Id > 0)
            .OrderBy(e => e.Name);

        // Act
        var template = expression.ToTemplate();

        // Assert
        template.Should().NotBeNull();
        template.Sql.Should().NotBeNullOrWhiteSpace();
        template.Parameters.Should().NotBeNull();
    }

    /// <summary>
    /// Test entity for SqlTemplate tests.
    /// </summary>
    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }
}
