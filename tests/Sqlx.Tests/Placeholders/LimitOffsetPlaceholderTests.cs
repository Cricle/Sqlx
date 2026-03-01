// <copyright file="LimitOffsetPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Tests for LimitPlaceholderHandler and OffsetPlaceholderHandler covering all branches.
/// </summary>
[TestClass]
public class LimitOffsetPlaceholderTests
{
    /// <summary>
    /// Tests for LimitPlaceholderHandler.
    /// </summary>
    [TestClass]
    public class LimitHandlerTests
    {
        private LimitPlaceholderHandler _handler = null!;
        private PlaceholderContext _context = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = LimitPlaceholderHandler.Instance;
            _context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
        }

        [TestMethod]
        public void Name_ReturnsLimit()
        {
            var name = _handler.Name;
            Assert.AreEqual("limit", name);
        }

        [TestMethod]
        public void GetType_ReturnsStatic()
        {
            var type = _handler.GetType("--count 10");
            Assert.AreEqual(PlaceholderType.Static, type);
        }

        [TestMethod]
        public void Process_WithCount_ReturnsLimitClause()
        {
            var result = _handler.Process(_context, "--count 10");
            Assert.AreEqual("LIMIT 10", result);
        }

        [TestMethod]
        public void Process_WithParam_ReturnsParameterizedLimitClause()
        {
            var result = _handler.Process(_context, "--param pageSize");
            Assert.AreEqual("LIMIT @pageSize", result);
        }

        [TestMethod]
        public void Process_WithoutOptions_ReturnsEmpty()
        {
            var result = _handler.Process(_context, "");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Render_WithParam_ReturnsLimitClause()
        {
            var parameters = new Dictionary<string, object?> { ["pageSize"] = 20 };

            var result = _handler.Render(_context, "--param pageSize", parameters);

            Assert.AreEqual("LIMIT 20", result);
        }

        [TestMethod]
        public void Render_WithNullValue_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["pageSize"] = null };

            var result = _handler.Render(_context, "--param pageSize", parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithoutParam_ThrowsInvalidOperationException()
        {
            var parameters = new Dictionary<string, object?>();
            _handler.Render(_context, "--count 10", parameters);
        }

        [TestMethod]
        public void Process_WithDifferentDialects_UsesCorrectParameterPrefix()
        {
            var mysqlContext = new PlaceholderContext(new MySqlDialect(), "test_table", new List<ColumnMeta>());
            var postgresContext = new PlaceholderContext(new PostgreSqlDialect(), "test_table", new List<ColumnMeta>());

            var mysqlResult = _handler.Process(mysqlContext, "--param limit");
            var postgresResult = _handler.Process(postgresContext, "--param limit");

            Assert.IsTrue(mysqlResult.Contains("@") || mysqlResult.Contains("?"));
            Assert.IsTrue(postgresResult.Contains("@") || postgresResult.Contains("$"));
        }
    }

    /// <summary>
    /// Tests for OffsetPlaceholderHandler.
    /// </summary>
    [TestClass]
    public class OffsetHandlerTests
    {
        private OffsetPlaceholderHandler _handler = null!;
        private PlaceholderContext _context = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = OffsetPlaceholderHandler.Instance;
            _context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
        }

        [TestMethod]
        public void Name_ReturnsOffset()
        {
            var name = _handler.Name;
            Assert.AreEqual("offset", name);
        }

        [TestMethod]
        public void GetType_ReturnsStatic()
        {
            var type = _handler.GetType("--count 20");
            Assert.AreEqual(PlaceholderType.Static, type);
        }

        [TestMethod]
        public void Process_WithCount_ReturnsOffsetClause()
        {
            var result = _handler.Process(_context, "--count 20");
            Assert.AreEqual("OFFSET 20", result);
        }

        [TestMethod]
        public void Process_WithParam_ReturnsParameterizedOffsetClause()
        {
            var result = _handler.Process(_context, "--param skip");
            Assert.AreEqual("OFFSET @skip", result);
        }

        [TestMethod]
        public void Process_WithoutOptions_ReturnsEmpty()
        {
            var result = _handler.Process(_context, "");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Render_WithParam_ReturnsOffsetClause()
        {
            var parameters = new Dictionary<string, object?> { ["skip"] = 40 };

            var result = _handler.Render(_context, "--param skip", parameters);

            Assert.AreEqual("OFFSET 40", result);
        }

        [TestMethod]
        public void Render_WithNullValue_ReturnsEmpty()
        {
            var parameters = new Dictionary<string, object?> { ["skip"] = null };

            var result = _handler.Render(_context, "--param skip", parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithoutParam_ThrowsInvalidOperationException()
        {
            var parameters = new Dictionary<string, object?>();
            _handler.Render(_context, "--count 20", parameters);
        }

        [TestMethod]
        public void Process_WithDifferentDialects_UsesCorrectParameterPrefix()
        {
            var mysqlContext = new PlaceholderContext(new MySqlDialect(), "test_table", new List<ColumnMeta>());
            var postgresContext = new PlaceholderContext(new PostgreSqlDialect(), "test_table", new List<ColumnMeta>());

            var mysqlResult = _handler.Process(mysqlContext, "--param offset");
            var postgresResult = _handler.Process(postgresContext, "--param offset");

            Assert.IsTrue(mysqlResult.Contains("@") || mysqlResult.Contains("?"));
            Assert.IsTrue(postgresResult.Contains("@") || postgresResult.Contains("$"));
        }
    }

    /// <summary>
    /// Tests for combined usage of Limit and Offset.
    /// </summary>
    [TestClass]
    public class CombinedUsageTests
    {
        private LimitPlaceholderHandler _limitHandler = null!;
        private OffsetPlaceholderHandler _offsetHandler = null!;
        private PlaceholderContext _context = null!;

        [TestInitialize]
        public void Setup()
        {
            _limitHandler = LimitPlaceholderHandler.Instance;
            _offsetHandler = OffsetPlaceholderHandler.Instance;
            _context = new PlaceholderContext(new SqlServerDialect(), "test_table", new List<ColumnMeta>());
        }

        [TestMethod]
        public void LimitAndOffset_Process_WithCounts_ReturnsBothClauses()
        {
            var limitResult = _limitHandler.Process(_context, "--count 10");
            var offsetResult = _offsetHandler.Process(_context, "--count 20");

            Assert.AreEqual("LIMIT 10", limitResult);
            Assert.AreEqual("OFFSET 20", offsetResult);
        }

        [TestMethod]
        public void LimitAndOffset_Process_WithParams_ReturnsBothParameterizedClauses()
        {
            var limitResult = _limitHandler.Process(_context, "--param pageSize");
            var offsetResult = _offsetHandler.Process(_context, "--param offset");

            Assert.AreEqual("LIMIT @pageSize", limitResult);
            Assert.AreEqual("OFFSET @offset", offsetResult);
        }
    }
}
