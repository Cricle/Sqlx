// <copyright file="IfPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Tests for IfPlaceholderHandler covering all conditional branches.
/// </summary>
[TestClass]
public class IfPlaceholderTests
{
    /// <summary>
    /// Basic handler functionality tests.
    /// </summary>
    [TestClass]
    public class HandlerTests
    {
        private IfPlaceholderHandler _handler = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = IfPlaceholderHandler.Instance;
        }

        [TestMethod]
        public void Name_ReturnsIf()
        {
            var name = _handler.Name;
            Assert.AreEqual("if", name);
        }

        [TestMethod]
        public void ClosingTagName_ReturnsSlashIf()
        {
            var closingTag = _handler.ClosingTagName;
            Assert.AreEqual("/if", closingTag);
        }

        [TestMethod]
        public void GetType_ReturnsDynamic()
        {
            var type = _handler.GetType("notnull=param");
            Assert.AreEqual(PlaceholderType.Dynamic, type);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Process_ThrowsInvalidOperationException()
        {
            var context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
            _handler.Process(context, "notnull=param");
        }

        [TestMethod]
        public void Render_ReturnsEmptyString()
        {
            var context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
            var parameters = new Dictionary<string, object?>();

            var result = _handler.Render(context, "notnull=param", parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_WithNullParameters_HandlesGracefully()
        {
            var blockContent = "content";

            var result = _handler.ProcessBlock("notnull=param", blockContent, null);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ProcessBlock_InvalidCondition_ThrowsInvalidOperationException()
        {
            var parameters = new Dictionary<string, object?>();
            var blockContent = "content";

            _handler.ProcessBlock("invalid=param", blockContent, parameters);
        }
    }

    /// <summary>
    /// Tests for notnull condition.
    /// </summary>
    [TestClass]
    public class NotNullTests
    {
        private IfPlaceholderHandler _handler = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = IfPlaceholderHandler.Instance;
        }

        [TestMethod]
        public void ProcessBlock_NotNull_WithNonNullValue_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
            var blockContent = "AND name = @name";

            var result = _handler.ProcessBlock("notnull=name", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_NotNull_WithNullValue_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = null };
            var blockContent = "AND name = @name";

            var result = _handler.ProcessBlock("notnull=name", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_NotNull_WithMissingParameter_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?>();
            var blockContent = "AND name = @name";

            var result = _handler.ProcessBlock("notnull=name", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }
    }

    /// <summary>
    /// Tests for null condition.
    /// </summary>
    [TestClass]
    public class NullTests
    {
        private IfPlaceholderHandler _handler = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = IfPlaceholderHandler.Instance;
        }

        [TestMethod]
        public void ProcessBlock_Null_WithNullValue_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = null };
            var blockContent = "AND name IS NULL";

            var result = _handler.ProcessBlock("null=name", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_Null_WithNonNullValue_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
            var blockContent = "AND name IS NULL";

            var result = _handler.ProcessBlock("null=name", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_Null_WithMissingParameter_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?>();
            var blockContent = "AND name IS NULL";

            var result = _handler.ProcessBlock("null=name", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }
    }

    /// <summary>
    /// Tests for notempty condition.
    /// </summary>
    [TestClass]
    public class NotEmptyTests
    {
        private IfPlaceholderHandler _handler = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = IfPlaceholderHandler.Instance;
        }

        [TestMethod]
        public void ProcessBlock_NotEmpty_WithNonEmptyString_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
            var blockContent = "AND name = @name";

            var result = _handler.ProcessBlock("notempty=name", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_NotEmpty_WithEmptyString_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = "" };
            var blockContent = "AND name = @name";

            var result = _handler.ProcessBlock("notempty=name", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_NotEmpty_WithNullValue_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = null };
            var blockContent = "AND name = @name";

            var result = _handler.ProcessBlock("notempty=name", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_NotEmpty_WithNonEmptyCollection_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
            var blockContent = "AND id IN @ids";

            var result = _handler.ProcessBlock("notempty=ids", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_NotEmpty_WithEmptyCollection_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
            var blockContent = "AND id IN @ids";

            var result = _handler.ProcessBlock("notempty=ids", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_NotEmpty_WithEnumerable_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["items"] = NonEmptyEnumerable() };
            var blockContent = "AND id IN @items";

            var result = _handler.ProcessBlock("notempty=items", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        private static IEnumerable<int> NonEmptyEnumerable()
        {
            yield return 1;
            yield return 2;
        }
    }

    /// <summary>
    /// Tests for empty condition.
    /// </summary>
    [TestClass]
    public class EmptyTests
    {
        private IfPlaceholderHandler _handler = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = IfPlaceholderHandler.Instance;
        }

        [TestMethod]
        public void ProcessBlock_Empty_WithEmptyString_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = "" };
            var blockContent = "AND name IS NULL";

            var result = _handler.ProcessBlock("empty=name", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_Empty_WithNonEmptyString_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = "Alice" };
            var blockContent = "AND name IS NULL";

            var result = _handler.ProcessBlock("empty=name", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_Empty_WithNullValue_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["name"] = null };
            var blockContent = "AND name IS NULL";

            var result = _handler.ProcessBlock("empty=name", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_Empty_WithEmptyCollection_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["ids"] = new List<int>() };
            var blockContent = "AND 1=0";

            var result = _handler.ProcessBlock("empty=ids", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        [TestMethod]
        public void ProcessBlock_Empty_WithNonEmptyCollection_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["ids"] = new List<int> { 1, 2, 3 } };
            var blockContent = "AND 1=0";

            var result = _handler.ProcessBlock("empty=ids", blockContent, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ProcessBlock_Empty_WithEnumerable_ReturnsBlockContent()
        {
            var parameters = new Dictionary<string, object?> { ["items"] = EmptyEnumerable() };
            var blockContent = "AND 1=0";

            var result = _handler.ProcessBlock("empty=items", blockContent, parameters);

            Assert.AreEqual(blockContent, result);
        }

        private static IEnumerable<int> EmptyEnumerable()
        {
            yield break;
        }
    }
}
