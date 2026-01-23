// <copyright file="ValuesPlaceholderInlineTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for {{values}} placeholder with --inline option for expression support.
/// </summary>
[TestClass]
public class ValuesPlaceholderInlineTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        new ColumnMeta("version", "Version", DbType.Int32, false),
    };

    #region Basic Functionality Tests

    [TestMethod]
    public void Values_InlineSingleExpression_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@email"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("@version"));
        Assert.IsFalse(sql.Contains("@created_at"));
    }

    [TestMethod]
    public void Values_InlineMultipleExpressions_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP,Version=1}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@email"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains(", 1"));
        Assert.IsFalse(sql.Contains("@created_at"));
        Assert.IsFalse(sql.Contains("@version"));
    }

    [TestMethod]
    public void Values_InlineWithExclude_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@email"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("@version"));
    }

    [TestMethod]
    public void Values_InlineDefaultValue_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Version=0}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@email"));
        Assert.IsTrue(sql.Contains("@created_at"));
        Assert.IsTrue(sql.Contains(", 0"));
        Assert.IsFalse(sql.Contains("@version"));
    }

    #endregion

    #region Dialect Tests

    [TestMethod]
    public void Values_Inline_SQLite_UsesCorrectSyntax()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=datetime('now')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("datetime('now')"));
    }

    [TestMethod]
    public void Values_Inline_PostgreSQL_UsesCorrectSyntax()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=NOW()}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("$id"));
        Assert.IsTrue(sql.Contains("$name"));
        Assert.IsTrue(sql.Contains("NOW()"));
    }

    [TestMethod]
    public void Values_Inline_MySQL_UsesCorrectSyntax()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=NOW()}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("NOW()"));
    }

    [TestMethod]
    public void Values_Inline_SqlServer_UsesCorrectSyntax()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=GETDATE()}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("GETDATE()"));
    }

    [TestMethod]
    public void Values_Inline_Oracle_UsesCorrectSyntax()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=SYSDATE}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains(":id"));
        Assert.IsTrue(sql.Contains(":name"));
        Assert.IsTrue(sql.Contains("SYSDATE"));
    }

    #endregion

    #region Expression Tests

    [TestMethod]
    public void Values_InlineWithNull_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Email=NULL}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("NULL"));
        Assert.IsFalse(sql.Contains("@email"));
    }

    [TestMethod]
    public void Values_InlineWithStringLiteral_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name='DefaultName'}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("'DefaultName'"));
        Assert.IsFalse(sql.Contains("@name"));
    }

    [TestMethod]
    public void Values_InlineWithNumericLiteral_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=123,Version=1}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("123"));
        Assert.IsTrue(sql.Contains(", 1"));
        Assert.IsFalse(sql.Contains("@id"));
        Assert.IsFalse(sql.Contains("@version"));
    }

    [TestMethod]
    public void Values_InlineWithFunction_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=UPPER(@name)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("UPPER(@name)"));
        // 注意：这里 @name 仍然是参数，但在 VALUES 中使用了函数包装
    }

    [TestMethod]
    public void Values_InlineWithCast_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Version=CAST(@ver AS INTEGER)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("CAST(@ver AS INTEGER)"));
    }

    [TestMethod]
    public void Values_InlineWithCoalesce_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Email=COALESCE(@email,'default@example.com')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("COALESCE(@email"));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Values_InlineAllColumns_OnlyUsesExpressions()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=@id,CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
    }

    [TestMethod]
    public void Values_InlineNoMatch_UsesStandardParameters()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline NonExistent=123}})",
            context);

        var sql = template.Sql;
        // NonExistent 不存在，所以所有列都使用标准参数
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@email"));
        Assert.IsTrue(sql.Contains("@created_at"));
        Assert.IsTrue(sql.Contains("@version"));
    }

    [TestMethod]
    public void Values_InlineEmptyColumns_ReturnsEmpty()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", System.Array.Empty<ColumnMeta>());
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=123}})",
            context);

        Assert.IsTrue(template.Sql.Contains("VALUES ()"));
    }

    [TestMethod]
    public void Values_InlineWithSpaces_HandlesCorrectly()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Version = 1}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("1"));
        Assert.IsFalse(sql.Contains("@version"));
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void Values_InlineCompleteInsert_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP,Version=1}})",
            context);

        var expected = "INSERT INTO [users] ([name], [email], [created_at], [version]) VALUES (@name, @email, CURRENT_TIMESTAMP, 1)";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void Values_InlineMixedParametersAndExpressions_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("created_by", "CreatedBy", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Status='pending',CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("'pending'"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("@created_by"));
    }

    #endregion
}
