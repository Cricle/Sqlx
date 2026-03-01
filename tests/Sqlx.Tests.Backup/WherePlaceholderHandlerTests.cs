// <copyright file="WherePlaceholderHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for WherePlaceholderHandler covering all WHERE clause generation branches.
/// </summary>
[TestClass]
public class WherePlaceholderHandlerTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    [TestMethod]
    public void Name_ReturnsWhere()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;

        // Act
        var name = handler.Name;

        // Assert
        Assert.AreEqual("where", name);
    }

    [TestMethod]
    public void GetType_ReturnsDynamic()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;

        // Act
        var type = handler.GetType("--param predicate");

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Process_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);

        // Act
        handler.Process(context, "--param predicate");
    }

    // ===== --param mode tests =====

    [TestMethod]
    public void Render_WithParamOption_ReturnsParameterValue()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?>
        {
            ["predicate"] = "age > 18 AND status = 'active'"
        };

        // Act
        var result = handler.Render(context, "--param predicate", parameters);

        // Assert
        Assert.AreEqual("age > 18 AND status = 'active'", result);
    }

    [TestMethod]
    public void Render_WithParamOption_NullParameter_ReturnsEmptyString()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?>
        {
            ["predicate"] = null
        };

        // Act
        var result = handler.Render(context, "--param predicate", parameters);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_WithParamOption_MissingParameter_ThrowsException()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?>();

        // Act
        handler.Render(context, "--param predicate", parameters);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_NoOption_ThrowsException()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?>();

        // Act
        handler.Render(context, "", parameters);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_NullOptions_ThrowsException()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?>();

        // Act
        handler.Render(context, null!, parameters);
    }

    // ===== --object mode tests (basic) =====

    [TestMethod]
    public void Render_WithObjectOption_SingleProperty_GeneratesCondition()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John" };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("[name] = @name", result);
    }

    [TestMethod]
    public void Render_WithObjectOption_MultipleProperties_GeneratesAndCondition()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Email"] = "john@test.com" };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("([name] = @name AND [email] = @email)", result);
    }

    [TestMethod]
    public void Render_WithObjectOption_NullObject_Returns1Equals1()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?> { ["filter"] = null };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("1=1", result);
    }

    [TestMethod]
    public void Render_WithObjectOption_EmptyDictionary_Returns1Equals1()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?>();
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("1=1", result);
    }

    [TestMethod]
    public void Render_WithObjectOption_AllNullValues_Returns1Equals1()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = null, ["Email"] = null };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("1=1", result);
    }

    [TestMethod]
    public void Render_WithObjectOption_UnknownProperty_Ignored()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["UnknownProp"] = "value", ["Name"] = "John" };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("[name] = @name", result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_WithObjectOption_NonDictionaryObject_ThrowsException()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?> { ["filter"] = "not a dictionary" };

        // Act
        handler.Render(context, "--object filter", parameters);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_WithObjectOption_MissingParameter_ThrowsException()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var parameters = new Dictionary<string, object?>();

        // Act
        handler.Render(context, "--object filter", parameters);
    }

    // ===== Column lookup optimization tests =====

    [TestMethod]
    public void Render_WithObjectOption_FewColumns_UsesLinearSearch()
    {
        // Arrange - 3 columns (< 4, should use linear search)
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John" };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("[name] = @name", result);
    }

    [TestMethod]
    public void Render_WithObjectOption_ManyColumns_UsesHashLookup()
    {
        // Arrange - 5 columns (> 4, should use hash lookup)
        var manyColumns = new List<ColumnMeta>
        {
            new ColumnMeta("id", "Id", DbType.Int32, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
            new ColumnMeta("age", "Age", DbType.Int32, true),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false)
        };
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new SqlServerDialect(), "users", manyColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.AreEqual("([name] = @name AND [age] = @age)", result);
    }

    // ===== Dialect-specific tests =====

    [TestMethod]
    public void Render_WithObjectOption_PostgreSQL_UsesDoubleQuotes()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new PostgreSqlDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John" };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.IsTrue(result.Contains("\"name\""));
    }

    [TestMethod]
    public void Render_WithObjectOption_MySQL_UsesBackticks()
    {
        // Arrange
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(new MySqlDialect(), "users", TestColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John" };
        var parameters = new Dictionary<string, object?> { ["filter"] = filter };

        // Act
        var result = handler.Render(context, "--object filter", parameters);

        // Assert
        Assert.IsTrue(result.Contains("`name`"));
    }
}
