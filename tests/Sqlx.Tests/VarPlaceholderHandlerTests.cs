// <copyright file="VarPlaceholderHandlerTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for VarPlaceholderHandler.
/// </summary>
[TestClass]
public class VarPlaceholderHandlerTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    // ===== Task 7.1: Placeholder Parsing Tests =====

    [TestMethod]
    public void Name_ReturnsVar()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;

        // Act
        var name = handler.Name;

        // Assert
        Assert.AreEqual("var", name);
    }

    [TestMethod]
    public void GetType_ReturnsDynamic()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;

        // Act
        var type = handler.GetType("--name tenantId");

        // Assert
        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    public void Parse_WithNameOption_ExtractsVariableName()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var varProvider = new Func<object, string, string>((instance, name) =>
        {
            Assert.AreEqual("tenantId", name, "Variable name should be extracted correctly");
            return "tenant-123";
        });
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act
        var result = handler.Render(context, "--name tenantId", null);

        // Assert
        Assert.AreEqual("tenant-123", result);
    }

    [TestMethod]
    public void Parse_MissingNameOption_ThrowsException()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            (instance, name) => "value",
            new object());

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => handler.Render(context, "", null));
        Assert.IsTrue(ex.Message.Contains("--name"), 
            "Error message should mention --name option");
    }

    [TestMethod]
    public void Parse_WithOtherOptions_IgnoresThem()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var varProvider = new Func<object, string, string>((instance, name) =>
        {
            Assert.AreEqual("userId", name);
            return "user-456";
        });
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act
        var result = handler.Render(context, "--name userId --other option", null);

        // Assert
        Assert.AreEqual("user-456", result);
    }

    // ===== Task 7.2: VarProvider Invocation Tests =====

    [TestMethod]
    public void Render_CallsVarProviderWithCorrectParameters()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var repositoryInstance = new TestRepository();
        var instancePassed = false;
        var namePassed = false;

        var varProvider = new Func<object, string, string>((instance, name) =>
        {
            Assert.AreEqual(repositoryInstance, instance, "Instance should be passed correctly");
            Assert.AreEqual("tenantId", name, "Variable name should be passed correctly");
            instancePassed = true;
            namePassed = true;
            return "tenant-123";
        });

        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            repositoryInstance);

        // Act
        var result = handler.Render(context, "--name tenantId", null);

        // Assert
        Assert.IsTrue(instancePassed, "Instance should be passed to VarProvider");
        Assert.IsTrue(namePassed, "Variable name should be passed to VarProvider");
        Assert.AreEqual("tenant-123", result);
    }

    [TestMethod]
    public void Render_ReturnsValueDirectly()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var expectedValue = "tenant-123";
        var varProvider = new Func<object, string, string>((instance, name) => expectedValue);
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act
        var result = handler.Render(context, "--name tenantId", null);

        // Assert
        Assert.AreEqual(expectedValue, result, "Value should be returned directly as literal");
    }

    [TestMethod]
    public void Render_NullVarProvider_ThrowsException()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            null,
            null);

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => handler.Render(context, "--name tenantId", null));
        Assert.IsTrue(ex.Message.Contains("VarProvider"), 
            "Error message should mention VarProvider");
        Assert.IsTrue(ex.Message.Contains("tenantId"), 
            "Error message should mention the variable name");
    }

    [TestMethod]
    public void Render_VarProviderReturnsValue_UsedAsLiteral()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var varProvider = new Func<object, string, string>((instance, name) => "literal-value");
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act
        var result = handler.Render(context, "--name test", null);

        // Assert
        // The value should be returned as-is, not wrapped in quotes or parameter markers
        Assert.AreEqual("literal-value", result);
    }

    // ===== Task 7.3: Error Handling Tests =====

    [TestMethod]
    public void Render_UnknownVariableName_PropagatesException()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var varProvider = new Func<object, string, string>((instance, name) =>
        {
            throw new ArgumentException($"Unknown variable: {name}. Available variables: tenantId, userId");
        });
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(
            () => handler.Render(context, "--name unknownVar", null));
        Assert.IsTrue(ex.Message.Contains("unknownVar"), 
            "Error message should mention the unknown variable name");
        Assert.IsTrue(ex.Message.Contains("Available variables"), 
            "Error message should list available variables");
    }

    [TestMethod]
    public void Render_ExceptionFromVariableMethod_Propagated()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var expectedException = new InvalidOperationException("Tenant context not set");
        var varProvider = new Func<object, string, string>((instance, name) =>
        {
            throw expectedException;
        });
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => handler.Render(context, "--name tenantId", null));
        Assert.AreEqual(expectedException, ex, "Exception should be propagated as-is");
    }

    [TestMethod]
    public void Render_ExceptionIncludesVariableName()
    {
        // Arrange
        var handler = VarPlaceholderHandler.Instance;
        var varProvider = new Func<object, string, string>((instance, name) =>
        {
            throw new ArgumentException($"Unknown variable: {name}");
        });
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            TestColumns,
            varProvider,
            new object());

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(
            () => handler.Render(context, "--name myVar", null));
        Assert.IsTrue(ex.Message.Contains("myVar"), 
            "Exception message should include the variable name for debugging");
    }

    // Test helper class
    private class TestRepository
    {
        public string GetTenantId() => "tenant-123";
        public string GetUserId() => "user-456";
    }
}
