// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectProviderExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core;

[TestClass]
public class DatabaseDialectProviderExtensionsTests
{
    [TestMethod]
    public void WrapColumn_WithMySqlProvider_UsesBackticks()
    {
        // Arrange
        var provider = new MySqlDialectProvider();
        var columnName = "user_id";

        // Act
        var result = provider.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("`user_id`", result);
    }

    [TestMethod]
    public void WrapColumn_WithSqlServerProvider_UsesBrackets()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();
        var columnName = "user_id";

        // Act
        var result = provider.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("[user_id]", result);
    }

    [TestMethod]
    public void WrapColumn_WithPostgreSqlProvider_UsesDoubleQuotes()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        var columnName = "user_id";

        // Act
        var result = provider.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("\"user_id\"", result);
    }

    [TestMethod]
    public void WrapColumn_WithSQLiteProvider_UsesBrackets()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();
        var columnName = "user_id";

        // Act
        var result = provider.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("[user_id]", result);
    }

    [TestMethod]
    public void WrapString_WithAllProviders_UsesCorrectQuotes()
    {
        // Arrange
        var providers = new IDatabaseDialectProvider[]
        {
            new MySqlDialectProvider(),
            new SqlServerDialectProvider(),
            new PostgreSqlDialectProvider(),
            new SQLiteDialectProvider()
        };
        var testString = "test value";

        // Act & Assert
        foreach (var provider in providers)
        {
            var result = provider.WrapString(testString);
            Assert.AreEqual("'test value'", result);
            Assert.IsTrue(result.StartsWith("'"));
            Assert.IsTrue(result.EndsWith("'"));
            Assert.IsTrue(result.Contains(testString));
        }
    }

    [TestMethod]
    public void GetParameterPrefix_WithMySqlProvider_ReturnsAtSign()
    {
        // Arrange
        var provider = new MySqlDialectProvider();

        // Act
        var result = provider.GetParameterPrefix();

        // Assert
        Assert.AreEqual("@", result);
    }

    [TestMethod]
    public void GetParameterPrefix_WithSqlServerProvider_ReturnsAtSign()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();

        // Act
        var result = provider.GetParameterPrefix();

        // Assert
        Assert.AreEqual("@", result);
    }

    [TestMethod]
    public void GetParameterPrefix_WithPostgreSqlProvider_ReturnsDollarSign()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var result = provider.GetParameterPrefix();

        // Assert
        Assert.AreEqual("$", result);
    }

    [TestMethod]
    public void GetParameterPrefix_WithSQLiteProvider_ReturnsAtSign()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();

        // Act
        var result = provider.GetParameterPrefix();

        // Assert
        Assert.AreEqual("@", result);
    }

    [TestMethod]
    public void WrapColumn_WithEmptyString_ReturnsWrappedEmptyString()
    {
        // Arrange
        var provider = new MySqlDialectProvider();
        var columnName = string.Empty;

        // Act
        var result = provider.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("``", result);
    }

    [TestMethod]
    public void WrapString_WithEmptyString_ReturnsWrappedEmptyString()
    {
        // Arrange
        var provider = new MySqlDialectProvider();
        var value = string.Empty;

        // Act
        var result = provider.WrapString(value);

        // Assert
        Assert.AreEqual("''", result);
    }

    [TestMethod]
    public void WrapColumn_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();
        var columnName = "user's_column";

        // Act
        var result = provider.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("[user's_column]", result);
        Assert.IsTrue(result.Contains(columnName));
    }

    [TestMethod]
    public void WrapString_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        var value = "test'with\"quotes";

        // Act
        var result = provider.WrapString(value);

        // Assert
        Assert.AreEqual("'test'with\"quotes'", result);
        Assert.IsTrue(result.Contains(value));
    }
}
