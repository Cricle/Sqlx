// -----------------------------------------------------------------------
// <copyright file="ParameterizedSqlCoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Sqlx;
using Xunit;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for core ParameterizedSql functionality.
/// </summary>
public class ParameterizedSqlCoreTests
{
    [Fact]
    public void ParameterizedSql_Constructor_WithValidParameters_ShouldCreate()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };

        // Act
        var parameterizedSql = new ParameterizedSql(sql, parameters);

        // Assert
        parameterizedSql.Sql.Should().Be(sql);
        parameterizedSql.Parameters.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void ParameterizedSql_Constructor_WithNullSql_ShouldCreate()
    {
        // Act
        var parameterizedSql = new ParameterizedSql(null!, new Dictionary<string, object?>());

        // Assert
        parameterizedSql.Sql.Should().BeNull();
        parameterizedSql.Parameters.Should().NotBeNull();
    }

    [Fact]
    public void ParameterizedSql_Constructor_WithNullParameters_ShouldCreate()
    {
        // Act
        var parameterizedSql = new ParameterizedSql("SELECT 1", null!);

        // Assert
        parameterizedSql.Sql.Should().Be("SELECT 1");
        parameterizedSql.Parameters.Should().BeNull();
    }

    [Fact]
    public void ParameterizedSql_ToString_ShouldContainInfo()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameterizedSql = new ParameterizedSql(sql, new Dictionary<string, object?> { { "id", 1 } });

        // Act
        var result = parameterizedSql.ToString();

        // Assert
        result.Should().Contain("ParameterizedSql");
        result.Should().Contain(sql);
    }

    [Fact]
    public void ParameterizedSql_ToString_WithNullSql_ShouldWork()
    {
        // Arrange
        var parameterizedSql = new ParameterizedSql(null!, new Dictionary<string, object?>());

        // Act
        var result = parameterizedSql.ToString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("ParameterizedSql");
    }

    [Theory]
    [InlineData("SELECT * FROM Users")]
    [InlineData("INSERT INTO Users (Name) VALUES (@name)")]
    [InlineData("UPDATE Users SET Name = @name WHERE Id = @id")]
    [InlineData("DELETE FROM Users WHERE Id = @id")]
    public void ParameterizedSql_WithVariousQueries_ShouldWork(string sql)
    {
        // Act
        var parameterizedSql = new ParameterizedSql(sql, new Dictionary<string, object?>());

        // Assert
        parameterizedSql.Sql.Should().Be(sql);
        parameterizedSql.Parameters.Should().NotBeNull();
    }

    [Fact]
    public void ParameterizedSql_WithComplexParameters_ShouldWork()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id AND Name = @name AND IsActive = @isActive";
        var parameters = new Dictionary<string, object?>
        {
            { "id", 1 },
            { "name", "John Doe" },
            { "isActive", true }
        };

        // Act
        var parameterizedSql = new ParameterizedSql(sql, parameters);

        // Assert
        parameterizedSql.Sql.Should().Be(sql);
        parameterizedSql.Parameters.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void ParameterizedSql_Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };
        var parameterizedSql1 = new ParameterizedSql(sql, parameters);
        var parameterizedSql2 = new ParameterizedSql(sql, parameters);

        // Act & Assert
        parameterizedSql1.Sql.Should().Be(parameterizedSql2.Sql);
        parameterizedSql1.Parameters.Should().BeEquivalentTo(parameterizedSql2.Parameters);
    }

    [Fact]
    public void ParameterizedSql_CreateWithDictionary_ShouldWork()
    {
        // This test demonstrates creating ParameterizedSql using the factory method
        // Arrange
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var parameters = new Dictionary<string, object?> { { "id", 1 } };

        // Act
        var parameterizedSql = ParameterizedSql.Create(sql, parameters);

        // Assert
        parameterizedSql.Should().NotBeNull();
        parameterizedSql.Sql.Should().Be(sql);
        parameterizedSql.Parameters.Should().BeEquivalentTo(parameters);
    }

    /// <summary>
    /// Test entity for ParameterizedSql tests.
    /// </summary>
    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }
}
