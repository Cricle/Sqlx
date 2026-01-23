// <copyright file="SetPlaceholderInlineTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for {{set}} placeholder with --inline option for expression support.
/// </summary>
[TestClass]
public class SetPlaceholderInlineTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("version", "Version", DbType.Int32, false),
        new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        new ColumnMeta("counter", "Counter", DbType.Int32, false),
    };

    [TestMethod]
    public void Set_InlineSingleExpression_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id", context);

        Console.WriteLine($"Actual SQL: {template.Sql}");
        
        // 实际生成的 SQL 不包含空格
        var expected = "UPDATE [users] SET [name] = @name, [version] = [version]+1, [updated_at] = @updated_at, [counter] = @counter WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void Set_InlineMultipleExpressions_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,Counter=Counter+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[version] = [version]+1"));
        Assert.IsTrue(sql.Contains("[counter] = [counter]+1"));
        Assert.IsTrue(sql.Contains("[updated_at] = @updated_at"));
    }

    [TestMethod]
    public void Set_InlineWithCurrentTimestamp_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        // 不排除 UpdatedAt，而是用内联表达式覆盖它
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsFalse(sql.Contains("[updated_at] = @updated_at"));
    }

    [TestMethod]
    public void Set_InlineComplexExpression_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter*2+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("[counter] = [counter]*2+1"));
    }

    [TestMethod]
    public void Set_InlineWithParameters_PreservesParameters()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter+@increment}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("[counter] = [counter]+@increment"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Set_InlinePostgreSQL_UsesDoubleQuotesAndDollarSign()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = $id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"version\" = \"version\"+1"));
        Assert.IsTrue(sql.Contains("\"name\" = $name"));
    }

    [TestMethod]
    public void Set_InlineMySQL_UsesBackticks()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("`version` = `version`+1"));
        Assert.IsTrue(sql.Contains("`name` = @name"));
    }

    [TestMethod]
    public void Set_InlineOnlyExpressions_NoStandardParameters()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
            context);

        Console.WriteLine($"Actual SQL: {template.Sql}");
        Assert.AreEqual("UPDATE [users] SET [version] = [version]+1 WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_InlineMixedCase_MatchesPropertyName()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
            context);

        // 确保属性名 Version 被正确识别并替换为列名 version
        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        Assert.IsTrue(sql.Contains("[version] = [version]+1"));
    }

    [TestMethod]
    public void Set_InlineWithSpaces_HandlesCorrectly()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version = Version + 1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Console.WriteLine($"Actual SQL: {sql}");
        // 表达式中的空格会被保留
        Assert.IsTrue(sql.Contains("[version] = [version] + 1"));
    }
}
