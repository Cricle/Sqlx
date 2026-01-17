// <copyright file="SetPlaceholderDialectTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for {{set}} placeholder with all SQL dialects.
/// Verifies correct parameter prefix generation.
/// </summary>
[TestClass]
public class SetPlaceholderDialectTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    [TestMethod]
    public void Set_SQLite_UsesSquareBracketsAndAtPrefix()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[id] = @id, [name] = @name, [email] = @email", result);
    }

    [TestMethod]
    public void Set_PostgreSQL_UsesDoubleQuotesAndDollarPrefix()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("\"id\" = $id, \"name\" = $name, \"email\" = $email", result);
    }

    [TestMethod]
    public void Set_MySQL_UsesBackticksAndAtPrefix()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("`id` = @id, `name` = @name, `email` = @email", result);
    }

    [TestMethod]
    public void Set_SqlServer_UsesSquareBracketsAndAtPrefix()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[id] = @id, [name] = @name, [email] = @email", result);
    }

    [TestMethod]
    public void Set_Oracle_UsesDoubleQuotesAndColonPrefix()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("\"id\" = :id, \"name\" = :name, \"email\" = :email", result);
    }

    [TestMethod]
    public void Set_DB2_UsesDoubleQuotesAndQuestionPrefix()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.DB2, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("\"id\" = ?id, \"name\" = ?name, \"email\" = ?email", result);
    }

    [TestMethod]
    public void Set_InUpdateStatement_SQLite_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [users] SET [id] = @id, [name] = @name, [email] = @email WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_InUpdateStatement_PostgreSQL_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = $id", context);

        Assert.AreEqual("UPDATE \"users\" SET \"id\" = $id, \"name\" = $name, \"email\" = $email WHERE id = $id", template.Sql);
    }

    [TestMethod]
    public void Set_InUpdateStatement_Oracle_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = :id", context);

        Assert.AreEqual("UPDATE \"users\" SET \"id\" = :id, \"name\" = :name, \"email\" = :email WHERE id = :id", template.Sql);
    }

    [TestMethod]
    public void Set_WithExcludeId_AllDialects_GeneratesCorrectSql()
    {
        var dialects = new[] { SqlDefine.SQLite, SqlDefine.PostgreSql, SqlDefine.MySql, SqlDefine.SqlServer, SqlDefine.Oracle, SqlDefine.DB2 };
        
        foreach (var dialect in dialects)
        {
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);
            
            // Should not contain id in SET clause (but will be in WHERE)
            var setPart = template.Sql.Split(new[] { " WHERE " }, StringSplitOptions.None)[0];
            Assert.IsFalse(setPart.Contains("id ="), $"Dialect {dialect} SET clause should not contain 'id ='");
            // Should contain name and email
            Assert.IsTrue(setPart.Contains("name"), $"Dialect {dialect} should contain 'name'");
            Assert.IsTrue(setPart.Contains("email"), $"Dialect {dialect} should contain 'email'");
        }
    }

    [TestMethod]
    public void Set_EmptyColumnList_ReturnsEmpty()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", Array.Empty<ColumnMeta>());

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void Set_SingleColumn_NoTrailingComma()
    {
        var handler = SetPlaceholderHandler.Instance;
        var singleColumn = new[] { new ColumnMeta("name", "Name", DbType.String, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", singleColumn);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[name] = @name", result);
        Assert.IsFalse(result.EndsWith(","));
    }
}
