// <copyright file="PlaceholderContextTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for PlaceholderContext, including VarProvider support.
/// </summary>
[TestClass]
public class PlaceholderContextTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    // ===== Task 6.2: PlaceholderContext Extension Tests =====

    [TestMethod]
    public void Constructor_WithoutVarProvider_CreatesValidContext()
    {
        // Arrange & Act
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        // Assert
        Assert.IsNotNull(context);
        Assert.AreEqual(SqlDefine.SQLite, context.Dialect);
        Assert.AreEqual("users", context.TableName);
        Assert.AreEqual(TestColumns, context.Columns);
        Assert.IsNull(context.VarProvider, "VarProvider should be null when not provided");
        Assert.IsNull(context.Instance, "Instance should be null when not provided");
    }

    [TestMethod]
    public void Constructor_WithVarProvider_CreatesValidContext()
    {
        // Arrange
        Func<object, string, string> varProvider = (instance, name) => "test-value";
        var repositoryInstance = new object();

        // Act
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            repositoryInstance);

        // Assert
        Assert.IsNotNull(context);
        Assert.AreEqual(SqlDefine.SQLite, context.Dialect);
        Assert.AreEqual("users", context.TableName);
        Assert.AreEqual(TestColumns, context.Columns);
        Assert.IsNotNull(context.VarProvider, "VarProvider should not be null");
        Assert.AreEqual(varProvider, context.VarProvider);
        Assert.IsNotNull(context.Instance, "Instance should not be null");
        Assert.AreEqual(repositoryInstance, context.Instance);
    }

    [TestMethod]
    public void Constructor_WithNullVarProvider_AllowsNull()
    {
        // Arrange & Act
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            null,
            null);

        // Assert
        Assert.IsNotNull(context);
        Assert.IsNull(context.VarProvider, "VarProvider can be null");
        Assert.IsNull(context.Instance, "Instance can be null");
    }

    [TestMethod]
    public void VarProvider_CanBeInvoked_ReturnsExpectedValue()
    {
        // Arrange
        var expectedValue = "tenant-123";
        Func<object, string, string> varProvider = (instance, name) =>
        {
            if (name == "tenantId") return expectedValue;
            throw new ArgumentException($"Unknown variable: {name}");
        };
        var repositoryInstance = new object();

        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            repositoryInstance);

        // Act
        var result = context.VarProvider!(context.Instance!, "tenantId");

        // Assert
        Assert.AreEqual(expectedValue, result);
    }

    [TestMethod]
    public void Instance_CanBeAnyObject_StoresCorrectly()
    {
        // Arrange
        var repositoryInstance = new TestRepository();
        Func<object, string, string> varProvider = (instance, name) => "value";

        // Act
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            repositoryInstance);

        // Assert
        Assert.IsNotNull(context.Instance);
        Assert.IsInstanceOfType(context.Instance, typeof(TestRepository));
        Assert.AreEqual(repositoryInstance, context.Instance);
    }

    [TestMethod]
    public void BackwardCompatibility_ExistingCodeWithoutVarProvider_StillWorks()
    {
        // Arrange & Act
        // This simulates existing code that doesn't use VarProvider
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        // Assert
        Assert.IsNotNull(context);
        Assert.AreEqual(SqlDefine.SQLite, context.Dialect);
        Assert.AreEqual("users", context.TableName);
        Assert.AreEqual(TestColumns, context.Columns);
        // VarProvider and Instance should be null for backward compatibility
        Assert.IsNull(context.VarProvider);
        Assert.IsNull(context.Instance);
    }

    // Test helper class
    private class TestRepository
    {
        public string GetTenantId() => "tenant-123";
        public string GetUserId() => "user-456";
    }
}
