// <copyright file="ValuesPlaceholderInlineEdgeCaseTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Edge case and boundary tests for {{values}} placeholder with --inline option.
/// </summary>
[TestClass]
public class ValuesPlaceholderInlineEdgeCaseTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
    };

    #region Parameter Preservation Tests

    [TestMethod]
    public void Values_InlineWithAtParameter_PreservesParameter()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=@customName}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@customName"));
        Assert.IsFalse(sql.Contains("@name"));
    }

    [TestMethod]
    public void Values_InlineWithColonParameter_PreservesParameter()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=:customName}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains(":customName"));
        Assert.IsFalse(sql.Contains(":name"));
    }

    [TestMethod]
    public void Values_InlineWithDollarParameter_PreservesParameter()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=$customName}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("$customName"));
        Assert.IsFalse(sql.Contains("$name"));
    }

    #endregion

    #region SQL Function Tests

    [TestMethod]
    public void Values_InlineWithUUID_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=gen_random_uuid()}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("gen_random_uuid()"));
    }

    [TestMethod]
    public void Values_InlineWithSequence_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=nextval('users_id_seq')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("nextval('users_id_seq')"));
    }

    [TestMethod]
    public void Values_InlineWithConcat_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name='User_'||@id}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("'User_'||@id"));
    }

    [TestMethod]
    public void Values_InlineWithSubstring_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=SUBSTR(@fullName,1)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("SUBSTR(@fullName"));
    }

    [TestMethod]
    public void Values_InlineWithTrim_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=TRIM(@name)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("TRIM(@name)"));
    }

    [TestMethod]
    public void Values_InlineWithLower_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=LOWER(@name)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("LOWER(@name)"));
    }

    #endregion

    #region Complex Expression Tests

    [TestMethod]
    public void Values_InlineWithArithmetic_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("quantity", "Quantity", DbType.Int32, false),
            new ColumnMeta("price", "Price", DbType.Decimal, false),
            new ColumnMeta("total", "Total", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Total=@quantity*@price}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@quantity*@price"));
    }

    [TestMethod]
    public void Values_InlineWithParentheses_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("result", "Result", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "calculations", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Result=(@a+@b)*@c}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("(@a+@b)*@c"));
    }

    [TestMethod]
    public void Values_InlineWithNestedFunctions_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=UPPER(TRIM(@name))}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("UPPER(TRIM(@name))"));
    }

    #endregion

    #region Boolean and Conditional Tests

    [TestMethod]
    public void Values_InlineWithBooleanTrue_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=1}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains(", 1"));
        Assert.IsFalse(sql.Contains("@is_active"));
    }

    [TestMethod]
    public void Values_InlineWithBooleanFalse_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=0}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains(", 0"));
    }

    [TestMethod]
    public void Values_InlineWithCase_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=CASE WHEN @status='active' THEN 1 ELSE 0 END}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("CASE WHEN @status='active' THEN 1 ELSE 0 END"));
    }

    #endregion

    #region Type Conversion Tests

    [TestMethod]
    public void Values_InlineWithCastToInteger_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=CAST(@stringId AS INTEGER)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("CAST(@stringId AS INTEGER)"));
    }

    [TestMethod]
    public void Values_InlineWithCastToText_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=CAST(@id AS TEXT)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("CAST(@id AS TEXT)"));
    }

    #endregion

    #region Date/Time Function Tests

    [TestMethod]
    public void Values_InlineWithDatetime_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=datetime('now')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("datetime('now')"));
    }

    [TestMethod]
    public void Values_InlineWithDateAdd_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline UpdatedAt=datetime('now','+1 day')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("datetime('now'"));
    }

    [TestMethod]
    public void Values_InlineWithStrftime_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=strftime('%Y-%m-%d',@date)}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("strftime('%Y-%m-%d'"));
    }

    #endregion

    #region Multiple Inline Expressions Tests

    [TestMethod]
    public void Values_InlineMultipleTimestamps_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsFalse(sql.Contains("@created_at"));
        Assert.IsFalse(sql.Contains("@updated_at"));
    }

    [TestMethod]
    public void Values_InlineMultipleDifferentTypes_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=123,Name='Test',IsActive=1,CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("123"));
        Assert.IsTrue(sql.Contains("'Test'"));
        Assert.IsTrue(sql.Contains(", 1"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("@updated_at"));
    }

    #endregion

    #region Snake Case Column Matching Tests

    [TestMethod]
    public void Values_InlineWithSnakeCaseColumn_MatchesByPropertyName()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=1,CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        // IsActive 属性对应 is_active 列
        // CreatedAt 属性对应 created_at 列
        Assert.IsTrue(sql.Contains(", 1"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsFalse(sql.Contains("@is_active"));
        Assert.IsFalse(sql.Contains("@created_at"));
    }

    #endregion

    #region Null and Default Tests

    [TestMethod]
    public void Values_InlineWithNullKeyword_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=NULL}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("NULL"));
        Assert.IsFalse(sql.Contains("@name"));
    }

    [TestMethod]
    public void Values_InlineWithDefault_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=DEFAULT}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("DEFAULT"));
    }

    #endregion

    #region Combination with Exclude Tests

    [TestMethod]
    public void Values_InlineWithExcludeAndExpression_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id,UpdatedAt}}) VALUES ({{values --exclude Id,UpdatedAt --inline CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("@id"));
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@is_active"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsFalse(sql.Contains("@updated_at"));
    }

    [TestMethod]
    public void Values_InlineExcludeAll_ReturnsEmpty()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id,Name}}) VALUES ({{values --exclude Id,Name}})",
            context);

        Assert.IsTrue(template.Sql.Contains("VALUES ()"));
    }

    #endregion

    #region Property Name Case Sensitivity Tests

    [TestMethod]
    public void Values_InlineCaseInsensitivePropertyMatch_Works()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline createdat=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        // 大小写不敏感匹配
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsFalse(sql.Contains("@created_at"));
    }

    #endregion
}
