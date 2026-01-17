// <copyright file="ValuesPlaceholderDialectTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for {{values}} placeholder with all SQL dialects.
/// Verifies correct parameter prefix generation.
/// </summary>
[TestClass]
public class ValuesPlaceholderDialectTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    [TestMethod]
    public void Values_SQLite_UsesAtPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("@id, @name, @email", result);
    }

    [TestMethod]
    public void Values_PostgreSQL_UsesDollarPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("$id, $name, $email", result);
    }

    [TestMethod]
    public void Values_MySQL_UsesAtPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("@id, @name, @email", result);
    }

    [TestMethod]
    public void Values_SqlServer_UsesAtPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("@id, @name, @email", result);
    }

    [TestMethod]
    public void Values_Oracle_UsesColonPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual(":id, :name, :email", result);
    }

    [TestMethod]
    public void Values_DB2_UsesQuestionPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.DB2, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("?id, ?name, ?email", result);
    }

    [TestMethod]
    public void Values_InInsertStatement_SQLite_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

        Assert.AreEqual("INSERT INTO [users] ([id], [name], [email]) VALUES (@id, @name, @email)", template.Sql);
    }

    [TestMethod]
    public void Values_InInsertStatement_PostgreSQL_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

        Assert.AreEqual("INSERT INTO \"users\" (\"id\", \"name\", \"email\") VALUES ($id, $name, $email)", template.Sql);
    }

    [TestMethod]
    public void Values_InInsertStatement_Oracle_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

        Assert.AreEqual("INSERT INTO \"users\" (\"id\", \"name\", \"email\") VALUES (:id, :name, :email)", template.Sql);
    }

    [TestMethod]
    public void Values_WithExclude_AllDialects_GeneratesCorrectSql()
    {
        var dialects = new[] { SqlDefine.SQLite, SqlDefine.PostgreSql, SqlDefine.MySql, SqlDefine.SqlServer, SqlDefine.Oracle, SqlDefine.DB2 };
        
        foreach (var dialect in dialects)
        {
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);
            
            // Should not contain id parameter
            Assert.IsFalse(template.Sql.Contains("id"), $"Dialect {dialect} should not contain 'id'");
            // Should contain name and email
            Assert.IsTrue(template.Sql.Contains("name"), $"Dialect {dialect} should contain 'name'");
            Assert.IsTrue(template.Sql.Contains("email"), $"Dialect {dialect} should contain 'email'");
        }
    }
}
