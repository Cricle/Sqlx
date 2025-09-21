// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlCoreTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Sqlx;
using Xunit;
using System;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for core ExpressionToSql functionality.
/// </summary>
public class ExpressionToSqlCoreTests
{
    /// <summary>
    /// Test entity for ExpressionToSql tests.
    /// </summary>
    public class TestUser
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal Salary { get; set; }
    }

    [Fact]
    public void ExpressionToSql_ForSqlServer_ShouldCreateInstance()
    {
        // Act
        var result = ExpressionToSql<TestUser>.ForSqlServer();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExpressionToSql_ForMySql_ShouldCreateInstance()
    {
        // Act
        var result = ExpressionToSql<TestUser>.ForMySql();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExpressionToSql_ForPostgreSQL_ShouldCreateInstance()
    {
        // Act
        var result = ExpressionToSql<TestUser>.ForPostgreSQL();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExpressionToSql_ForSqlite_ShouldCreateInstance()
    {
        // Act
        var result = ExpressionToSql<TestUser>.ForSqlite();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExpressionToSql_ForOracle_ShouldCreateInstance()
    {
        // Act
        var result = ExpressionToSql<TestUser>.ForOracle();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExpressionToSql_ForDB2_ShouldCreateInstance()
    {
        // Act
        var result = ExpressionToSql<TestUser>.ForDB2();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExpressionToSql_Where_ShouldReturnSameInstance()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expression.Where(u => u.Id == 1);

        // Assert
        result.Should().BeSameAs(expression);
    }

    [Fact]
    public void ExpressionToSql_OrderBy_ShouldReturnSameInstance()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expression.OrderBy(u => u.Name);

        // Assert
        result.Should().BeSameAs(expression);
    }

    [Fact]
    public void ExpressionToSql_Take_ShouldReturnSameInstance()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expression.Take(10);

        // Assert
        result.Should().BeSameAs(expression);
    }

    [Fact]
    public void ExpressionToSql_Skip_ShouldReturnSameInstance()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer();

        // Act
        var result = expression.Skip(5);

        // Assert
        result.Should().BeSameAs(expression);
    }

    [Fact]
    public void ExpressionToSql_ToSql_ShouldGenerateValidSql()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id > 0)
            .OrderBy(u => u.Name)
            .Take(10);

        // Act
        var sql = expression.ToSql();

        // Assert
        sql.Should().NotBeNullOrWhiteSpace();
        sql.Should().Contain("SELECT");
        sql.Should().Contain("FROM");
        sql.Should().Contain("WHERE");
        sql.Should().Contain("ORDER BY");
    }

    [Fact]
    public void ExpressionToSql_ToWhereClause_ShouldGenerateWhereClause()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id > 0)
            .Where(u => u.IsActive == true);

        // Act
        var whereClause = expression.ToWhereClause();

        // Assert
        whereClause.Should().NotBeNullOrWhiteSpace();
        // ToWhereClause returns the conditions without the WHERE keyword
        whereClause.Should().Contain("AND");
    }

    [Fact]
    public void ExpressionToSql_ToTemplate_ShouldGenerateTemplate()
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id > 0);

        // Act
        var template = expression.ToTemplate();

        // Assert
        template.Should().NotBeNull();
        template.Sql.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ExpressionToSql_ChainedCalls_ShouldWork()
    {
        // Arrange & Act
        var expression = ExpressionToSql<TestUser>.ForSqlServer()
            .Where(u => u.Id > 0)
            .Where(u => u.IsActive == true)
            .OrderBy(u => u.Name)
            .OrderByDescending(u => u.CreatedDate)
            .Skip(10)
            .Take(20);

        var sql = expression.ToSql();

        // Assert
        sql.Should().NotBeNullOrWhiteSpace();
        expression.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void ExpressionToSql_Take_WithDifferentValues_ShouldWork(int takeCount)
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer()
            .Take(takeCount);

        // Act
        var sql = expression.ToSql();

        // Assert
        sql.Should().NotBeNullOrWhiteSpace();
        sql.Should().Contain(takeCount.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(500)]
    public void ExpressionToSql_Skip_WithDifferentValues_ShouldWork(int skipCount)
    {
        // Arrange
        var expression = ExpressionToSql<TestUser>.ForSqlServer()
            .Skip(skipCount);

        // Act
        var sql = expression.ToSql();

        // Assert
        sql.Should().NotBeNullOrWhiteSpace();
        sql.Should().Contain(skipCount.ToString());
    }
}
