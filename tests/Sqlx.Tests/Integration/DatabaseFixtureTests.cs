// -----------------------------------------------------------------------
// <copyright file="DatabaseFixtureTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using Xunit;

namespace Sqlx.Tests.Integration;

/// <summary>
/// Unit tests for DatabaseFixture test data consistency.
/// **Validates: Requirements 1.3**
/// </summary>
public class DatabaseFixtureTests : IDisposable
{
    private readonly DatabaseFixture _fixture;

    public DatabaseFixtureTests()
    {
        _fixture = new DatabaseFixture();
    }

    /// <summary>
    /// Verify DatabaseFixture provides exactly 15 users with expected balance values.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void SeedTestData_ShouldProvide15Users()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);

        // Act
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users";
        var userCount = Convert.ToInt32(cmd.ExecuteScalar());

        // Assert
        Assert.Equal(15, userCount);
    }

    /// <summary>
    /// Verify DatabaseFixture provides exactly 7 products with expected price ranges.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void SeedTestData_ShouldProvide7Products()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);

        // Act
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM products";
        var productCount = Convert.ToInt32(cmd.ExecuteScalar());

        // Assert
        Assert.Equal(7, productCount);
    }

    /// <summary>
    /// Verify DatabaseFixture provides exactly 4 orders with expected amounts.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void SeedTestData_ShouldProvide4Orders()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);

        // Act
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM orders";
        var orderCount = Convert.ToInt32(cmd.ExecuteScalar());

        // Assert
        Assert.Equal(4, orderCount);
    }

    /// <summary>
    /// Verify user balance values are within expected ranges.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void SeedTestData_UserBalances_ShouldBeWithinExpectedRanges()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);

        // Act
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(balance), MAX(balance) FROM users";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        var minBalance = reader.GetDouble(0);
        var maxBalance = reader.GetDouble(1);

        // Assert - based on seed data, min is 200.00, max is 5000.00
        Assert.True(minBalance >= 200.00, $"Min balance {minBalance} should be >= 200.00");
        Assert.True(maxBalance <= 5000.00, $"Max balance {maxBalance} should be <= 5000.00");
    }

    /// <summary>
    /// Verify product prices are within expected ranges.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void SeedTestData_ProductPrices_ShouldBeWithinExpectedRanges()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);

        // Act
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(price), MAX(price) FROM products";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        var minPrice = reader.GetDouble(0);
        var maxPrice = reader.GetDouble(1);

        // Assert - based on seed data, min is 29.99, max is 999.99
        Assert.True(minPrice >= 29.99, $"Min price {minPrice} should be >= 29.99");
        Assert.True(maxPrice <= 999.99, $"Max price {maxPrice} should be <= 999.99");
    }

    /// <summary>
    /// Verify order amounts are within expected ranges.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Fact]
    public void SeedTestData_OrderAmounts_ShouldBeWithinExpectedRanges()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        _fixture.SeedTestData(SqlDefineTypes.SQLite);

        // Act
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(total_amount), MAX(total_amount) FROM orders";
        using var reader = cmd.ExecuteReader();
        reader.Read();
        var minAmount = reader.GetDouble(0);
        var maxAmount = reader.GetDouble(1);

        // Assert - based on seed data, min is 500.00, max is 2000.00
        Assert.True(minAmount >= 500.00, $"Min amount {minAmount} should be >= 500.00");
        Assert.True(maxAmount <= 2000.00, $"Max amount {maxAmount} should be <= 2000.00");
    }

    public void Dispose()
    {
        _fixture?.Dispose();
    }
}
