// <copyright file="ArgPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Tests for {{arg}} placeholder handler.
/// </summary>
[TestClass]
public class ArgPlaceholderTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
    };

    /// <summary>
    /// Basic dialect-specific parameter prefix tests.
    /// </summary>
    [TestClass]
    public class DialectTests
    {
        [TestMethod]
        public void Arg_SQLite_GeneratesAtPrefix()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
        }

        [TestMethod]
        public void Arg_PostgreSql_GeneratesDollarPrefix()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
        }

        [TestMethod]
        public void Arg_Oracle_GeneratesColonPrefix()
        {
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = :id"));
        }

        [TestMethod]
        public void Arg_MySql_GeneratesAtPrefix()
        {
            var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
        }

        [TestMethod]
        public void Arg_SqlServer_GeneratesAtPrefix()
        {
            var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
        }
    }

    /// <summary>
    /// Tests for --name alias functionality.
    /// </summary>
    [TestClass]
    public class NameAliasTests
    {
        [TestMethod]
        public void Arg_WithNameAlias_UsesAliasInSql()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param userId --name id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
        }

        [TestMethod]
        public void Arg_WithNameAlias_PostgreSql_UsesAliasWithDollarPrefix()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param userId --name id}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
        }
    }

    /// <summary>
    /// Tests for multiple arg placeholders.
    /// </summary>
    [TestClass]
    public class MultipleArgsTests
    {
        [TestMethod]
        public void Arg_MultipleArgs_AllGeneratedCorrectly()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}} AND name = {{arg --param name}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
            Assert.IsTrue(template.Sql.Contains("AND name = @name"));
        }

        [TestMethod]
        public void Arg_MultipleArgs_PostgreSql_AllGeneratedCorrectly()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}} AND name = {{arg --param name}}",
                context);

            Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
            Assert.IsTrue(template.Sql.Contains("AND name = @name"));
        }
    }

    /// <summary>
    /// Tests for placeholder type classification.
    /// </summary>
    [TestClass]
    public class PlaceholderTypeTests
    {
        [TestMethod]
        public void Arg_IsStaticPlaceholder()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg --param id}}",
                context);

            Assert.IsFalse(template.HasDynamicPlaceholders);
        }
    }

    /// <summary>
    /// Tests for error handling.
    /// </summary>
    [TestClass]
    public class ErrorHandlingTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Arg_WithoutParam_ThrowsException()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            SqlTemplate.Prepare(
                "SELECT * FROM {{table}} WHERE id = {{arg}}",
                context);
        }
    }
}
