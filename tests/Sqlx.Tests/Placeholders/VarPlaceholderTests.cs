// <copyright file="VarPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Tests for VarPlaceholderHandler.
/// </summary>
[TestClass]
public class VarPlaceholderTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    /// <summary>
    /// Basic handler functionality and parsing tests.
    /// </summary>
    [TestClass]
    public class HandlerTests
    {
        [TestMethod]
        public void Name_ReturnsVar()
        {
            var handler = VarPlaceholderHandler.Instance;
            var name = handler.Name;
            Assert.AreEqual("var", name);
        }

        [TestMethod]
        public void GetType_ReturnsDynamic()
        {
            var handler = VarPlaceholderHandler.Instance;
            var type = handler.GetType("--name tenantId");
            Assert.AreEqual(PlaceholderType.Dynamic, type);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Process_ThrowsInvalidOperationException()
        {
            var handler = VarPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, null, null);
            handler.Process(context, "--name tenantId");
        }

        [TestMethod]
        public void Parse_WithNameOption_ExtractsVariableName()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) =>
            {
                Assert.AreEqual("tenantId", name, "Variable name should be extracted correctly");
                return "tenant-123";
            });
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name tenantId", null);

            Assert.AreEqual("tenant-123", result);
        }

        [TestMethod]
        public void Parse_MissingNameOption_ThrowsException()
        {
            var handler = VarPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, (instance, name) => "value", new object());

            var ex = Assert.ThrowsException<InvalidOperationException>(() => handler.Render(context, "", null));
            Assert.IsTrue(ex.Message.Contains("--name"), "Error message should mention --name option");
        }

        [TestMethod]
        public void Parse_NullOptions_ThrowsException()
        {
            var handler = VarPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, (instance, name) => "value", new object());

            var ex = Assert.ThrowsException<InvalidOperationException>(() => handler.Render(context, null!, null));
            Assert.IsTrue(ex.Message.Contains("--name"), "Error message should mention --name option");
        }

        [TestMethod]
        public void Parse_WhitespaceOptions_ThrowsException()
        {
            var handler = VarPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, (instance, name) => "value", new object());

            var ex = Assert.ThrowsException<InvalidOperationException>(() => handler.Render(context, "   ", null));
            Assert.IsTrue(ex.Message.Contains("--name"), "Error message should mention --name option");
        }

        [TestMethod]
        public void Parse_WithOtherOptions_IgnoresThem()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) =>
            {
                Assert.AreEqual("userId", name);
                return "user-456";
            });
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name userId --other option", null);

            Assert.AreEqual("user-456", result);
        }
    }

    /// <summary>
    /// Tests for VarProvider invocation.
    /// </summary>
    [TestClass]
    public class VarProviderTests
    {
        [TestMethod]
        public void Render_CallsVarProviderWithCorrectParameters()
        {
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

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, repositoryInstance);

            var result = handler.Render(context, "--name tenantId", null);

            Assert.IsTrue(instancePassed, "Instance should be passed to VarProvider");
            Assert.IsTrue(namePassed, "Variable name should be passed to VarProvider");
            Assert.AreEqual("tenant-123", result);
        }

        [TestMethod]
        public void Render_ReturnsValueDirectly()
        {
            var handler = VarPlaceholderHandler.Instance;
            var expectedValue = "tenant-123";
            var varProvider = new Func<object, string, string>((instance, name) => expectedValue);
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name tenantId", null);

            Assert.AreEqual(expectedValue, result, "Value should be returned directly as literal");
        }

        [TestMethod]
        public void Render_NullVarProvider_ThrowsException()
        {
            var handler = VarPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, null, null);

            var ex = Assert.ThrowsException<InvalidOperationException>(() => handler.Render(context, "--name tenantId", null));
            Assert.IsTrue(ex.Message.Contains("VarProvider"), "Error message should mention VarProvider");
            Assert.IsTrue(ex.Message.Contains("tenantId"), "Error message should mention the variable name");
        }

        [TestMethod]
        public void Render_VarProviderReturnsValue_UsedAsLiteral()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) => "literal-value");
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name test", null);

            Assert.AreEqual("literal-value", result);
        }

        [TestMethod]
        public void Render_VarProviderReturnsNumericString_UsedAsLiteral()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) => "123");
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name count", null);

            Assert.AreEqual("123", result);
        }

        [TestMethod]
        public void Render_VarProviderReturnsEmptyString_ReturnsEmptyString()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) => string.Empty);
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name empty", null);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Render_VarProviderReturnsSqlKeyword_UsedAsLiteral()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) => "CURRENT_TIMESTAMP");
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var result = handler.Render(context, "--name timestamp", null);

            Assert.AreEqual("CURRENT_TIMESTAMP", result);
        }

        private class TestRepository
        {
            public string GetTenantId() => "tenant-123";
            public string GetUserId() => "user-456";
        }
    }

    /// <summary>
    /// Tests for error handling.
    /// </summary>
    [TestClass]
    public class ErrorHandlingTests
    {
        [TestMethod]
        public void Render_UnknownVariableName_PropagatesException()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) =>
            {
                throw new ArgumentException($"Unknown variable: {name}. Available variables: tenantId, userId");
            });
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var ex = Assert.ThrowsException<ArgumentException>(() => handler.Render(context, "--name unknownVar", null));
            Assert.IsTrue(ex.Message.Contains("unknownVar"), "Error message should mention the unknown variable name");
            Assert.IsTrue(ex.Message.Contains("Available variables"), "Error message should list available variables");
        }

        [TestMethod]
        public void Render_ExceptionFromVariableMethod_Propagated()
        {
            var handler = VarPlaceholderHandler.Instance;
            var expectedException = new InvalidOperationException("Tenant context not set");
            var varProvider = new Func<object, string, string>((instance, name) =>
            {
                throw expectedException;
            });
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var ex = Assert.ThrowsException<InvalidOperationException>(() => handler.Render(context, "--name tenantId", null));
            Assert.AreEqual(expectedException, ex, "Exception should be propagated as-is");
        }

        [TestMethod]
        public void Render_ExceptionIncludesVariableName()
        {
            var handler = VarPlaceholderHandler.Instance;
            var varProvider = new Func<object, string, string>((instance, name) =>
            {
                throw new ArgumentException($"Unknown variable: {name}");
            });
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, varProvider, new object());

            var ex = Assert.ThrowsException<ArgumentException>(() => handler.Render(context, "--name myVar", null));
            Assert.IsTrue(ex.Message.Contains("myVar"), "Exception message should include the variable name for debugging");
        }
    }
}
