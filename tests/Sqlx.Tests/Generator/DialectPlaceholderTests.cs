// -----------------------------------------------------------------------
// <copyright file="DialectPlaceholderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using Sqlx.Generator.Core;

namespace Sqlx.Tests.Generator;

[TestClass]
public class DialectPlaceholderTests
{
    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PostgreSQL_ReturningIdClause_ShouldReturnCorrectClause()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var result = provider.GetReturningIdClause();

        // Assert
        Assert.AreEqual("RETURNING id", result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PostgreSQL_BoolLiterals_ShouldReturnCorrectValues()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var trueValue = provider.GetBoolTrueLiteral();
        var falseValue = provider.GetBoolFalseLiteral();

        // Assert
        Assert.AreEqual("true", trueValue);
        Assert.AreEqual("false", falseValue);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void PostgreSQL_LimitOffsetClause_ShouldNotRequireOrderBy()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var result = provider.GenerateLimitOffsetClause("@limit", "@offset", out bool requiresOrderBy);

        // Assert
        Assert.AreEqual("LIMIT @limit OFFSET @offset", result);
        Assert.IsFalse(requiresOrderBy);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MySQL_ReturningIdClause_ShouldReturnEmpty()
    {
        // Arrange
        var provider = new MySqlDialectProvider();

        // Act
        var result = provider.GetReturningIdClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MySQL_BoolLiterals_ShouldReturnNumericValues()
    {
        // Arrange
        var provider = new MySqlDialectProvider();

        // Act
        var trueValue = provider.GetBoolTrueLiteral();
        var falseValue = provider.GetBoolFalseLiteral();

        // Assert
        Assert.AreEqual("1", trueValue);
        Assert.AreEqual("0", falseValue);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void MySQL_LimitOffsetClause_ShouldNotRequireOrderBy()
    {
        // Arrange
        var provider = new MySqlDialectProvider();

        // Act
        var result = provider.GenerateLimitOffsetClause("@limit", "@offset", out bool requiresOrderBy);

        // Assert
        Assert.AreEqual("LIMIT @limit OFFSET @offset", result);
        Assert.IsFalse(requiresOrderBy);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SqlServer_ReturningIdClause_ShouldReturnEmpty()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();

        // Act
        var result = provider.GetReturningIdClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SqlServer_BoolLiterals_ShouldReturnNumericValues()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();

        // Act
        var trueValue = provider.GetBoolTrueLiteral();
        var falseValue = provider.GetBoolFalseLiteral();

        // Assert
        Assert.AreEqual("1", trueValue);
        Assert.AreEqual("0", falseValue);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SqlServer_LimitOffsetClause_ShouldRequireOrderBy()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();

        // Act
        var result = provider.GenerateLimitOffsetClause("@limit", "@offset", out bool requiresOrderBy);

        // Assert
        Assert.AreEqual("OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY", result);
        Assert.IsTrue(requiresOrderBy);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SQLite_ReturningIdClause_ShouldReturnEmpty()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();

        // Act
        var result = provider.GetReturningIdClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SQLite_BoolLiterals_ShouldReturnNumericValues()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();

        // Act
        var trueValue = provider.GetBoolTrueLiteral();
        var falseValue = provider.GetBoolFalseLiteral();

        // Assert
        Assert.AreEqual("1", trueValue);
        Assert.AreEqual("0", falseValue);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void SQLite_LimitOffsetClause_ShouldNotRequireOrderBy()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();

        // Act
        var result = provider.GenerateLimitOffsetClause("@limit", "@offset", out bool requiresOrderBy);

        // Assert
        Assert.AreEqual("LIMIT @limit OFFSET @offset", result);
        Assert.IsFalse(requiresOrderBy);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_TablePlaceholder_ShouldReplaceWithWrappedName()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        var template = "SELECT * FROM {{table}} WHERE id = @id";

        // Act
        var result = provider.ReplacePlaceholders(template, tableName: "users");

        // Assert
        Assert.AreEqual("SELECT * FROM \"users\" WHERE id = @id", result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_ColumnsPlaceholder_ShouldReplaceWithWrappedColumns()
    {
        // Arrange
        var provider = new MySqlDialectProvider();
        var template = "SELECT {{columns}} FROM users";
        var columns = new[] { "id", "username", "email" };

        // Act
        var result = provider.ReplacePlaceholders(template, columns: columns);

        // Assert
        Assert.AreEqual("SELECT `id`, `username`, `email` FROM users", result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_ReturningIdPlaceholder_ShouldReplaceCorrectly()
    {
        // Arrange
        var pgProvider = new PostgreSqlDialectProvider();
        var mysqlProvider = new MySqlDialectProvider();
        var template = "INSERT INTO users (name) VALUES (@name) {{returning_id}}";

        // Act
        var pgResult = pgProvider.ReplacePlaceholders(template);
        var mysqlResult = mysqlProvider.ReplacePlaceholders(template);

        // Assert
        Assert.AreEqual("INSERT INTO users (name) VALUES (@name) RETURNING id", pgResult);
        Assert.AreEqual("INSERT INTO users (name) VALUES (@name) ", mysqlResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_BoolPlaceholders_ShouldReplaceCorrectly()
    {
        // Arrange
        var pgProvider = new PostgreSqlDialectProvider();
        var mysqlProvider = new MySqlDialectProvider();
        var template = "UPDATE users SET active = {{bool_true}}, deleted = {{bool_false}}";

        // Act
        var pgResult = pgProvider.ReplacePlaceholders(template);
        var mysqlResult = mysqlProvider.ReplacePlaceholders(template);

        // Assert
        Assert.AreEqual("UPDATE users SET active = true, deleted = false", pgResult);
        Assert.AreEqual("UPDATE users SET active = 1, deleted = 0", mysqlResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_CurrentTimestampPlaceholder_ShouldReplaceCorrectly()
    {
        // Arrange
        var pgProvider = new PostgreSqlDialectProvider();
        var sqlServerProvider = new SqlServerDialectProvider();
        var template = "INSERT INTO logs (created_at) VALUES ({{current_timestamp}})";

        // Act
        var pgResult = pgProvider.ReplacePlaceholders(template);
        var sqlServerResult = sqlServerProvider.ReplacePlaceholders(template);

        // Assert
        Assert.AreEqual("INSERT INTO logs (created_at) VALUES (CURRENT_TIMESTAMP)", pgResult);
        Assert.AreEqual("INSERT INTO logs (created_at) VALUES (GETDATE())", sqlServerResult);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_MultiplePlaceholders_ShouldReplaceAll()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        var template = "INSERT INTO {{table}} ({{columns}}, created_at) VALUES (@id, @name, {{current_timestamp}}) {{returning_id}}";
        var columns = new[] { "id", "name" };

        // Act
        var result = provider.ReplacePlaceholders(template, tableName: "users", columns: columns);

        // Assert
        Assert.AreEqual("INSERT INTO \"users\" (\"id\", \"name\", created_at) VALUES (@id, @name, CURRENT_TIMESTAMP) RETURNING id", result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_EmptyTemplate_ShouldReturnEmpty()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var result = provider.ReplacePlaceholders(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_NullTemplate_ShouldReturnNull()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();

        // Act
        var result = provider.ReplacePlaceholders(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ReplacePlaceholders_NoPlaceholders_ShouldReturnOriginal()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        var template = "SELECT * FROM users WHERE id = @id";

        // Act
        var result = provider.ReplacePlaceholders(template);

        // Assert
        Assert.AreEqual(template, result);
    }
}

