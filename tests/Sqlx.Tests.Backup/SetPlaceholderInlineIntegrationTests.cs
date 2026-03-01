// <copyright file="SetPlaceholderInlineIntegrationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Integration tests for {{set --inline}} with real-world scenarios.
/// </summary>
[TestClass]
public class SetPlaceholderInlineIntegrationTests
{
    [TestMethod]
    public void Set_Inline_VersionedEntityUpdate_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("title", "Title", DbType.String, false),
            new ColumnMeta("content", "Content", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "documents", columns);
        // 分开两个表达式，避免逗号冲突
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id AND version = @version",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[title] = @title"));
        Assert.IsTrue(sql.Contains("[content] = @content"));
        Assert.IsTrue(sql.Contains("[version] = [version]+1"));
        Assert.IsTrue(sql.Contains("[updated_at] = @updated_at"));
    }

    [TestMethod]
    public void Set_Inline_CounterIncrement_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("view_count", "ViewCount", DbType.Int32, false),
            new ColumnMeta("like_count", "LikeCount", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "posts", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline ViewCount=ViewCount+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[view_count] = [view_count]+1"));
        Assert.IsTrue(sql.Contains("[like_count] = @like_count"));
    }

    [TestMethod]
    public void Set_Inline_ConditionalUpdate_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new ColumnMeta("retry_count", "RetryCount", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "jobs", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline RetryCount=RetryCount+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[status] = @status"));
        Assert.IsTrue(sql.Contains("[retry_count] = [retry_count]+1"));
    }

    [TestMethod]
    public void Set_Inline_ScoreCalculation_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("base_score", "BaseScore", DbType.Decimal, false),
            new ColumnMeta("bonus_score", "BonusScore", DbType.Decimal, false),
            new ColumnMeta("total_score", "TotalScore", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "players", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline TotalScore=BaseScore+BonusScore}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[base_score] = @base_score"));
        Assert.IsTrue(sql.Contains("[bonus_score] = @bonus_score"));
        Assert.IsTrue(sql.Contains("[total_score] = [base_score]+[bonus_score]"));
    }

    [TestMethod]
    public void Set_Inline_StringManipulation_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("first_name", "FirstName", DbType.String, false),
            new ColumnMeta("last_name", "LastName", DbType.String, false),
            new ColumnMeta("full_name", "FullName", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline FullName=FirstName||' '||LastName}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[first_name] = @first_name"));
        Assert.IsTrue(sql.Contains("[last_name] = @last_name"));
        Assert.IsTrue(sql.Contains("[full_name] = [first_name]||' '||[last_name]"));
    }

    [TestMethod]
    public void Set_Inline_NullHandling_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, true),
            new ColumnMeta("default_name", "DefaultName", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline DefaultName=COALESCE(Name,'Unknown')}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[default_name] = COALESCE([name]"));
    }

    [TestMethod]
    public void Set_Inline_DateCalculation_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("expires_at", "ExpiresAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "sessions", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline ExpiresAt=datetime(CreatedAt,'+30 days')}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[created_at] = @created_at"));
        Assert.IsTrue(sql.Contains("[expires_at] = datetime([created_at]"));
    }

    [TestMethod]
    public void Set_Inline_MixedStandardAndExpression_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        // 标准参数
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
        Assert.IsTrue(sql.Contains("[updated_at] = @updated_at"));
        // 表达式
        Assert.IsTrue(sql.Contains("[version] = [version]+1"));
    }

    [TestMethod]
    public void Set_Inline_ComplexBusinessLogic_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("price", "Price", DbType.Decimal, false),
            new ColumnMeta("discount", "Discount", DbType.Decimal, false),
            new ColumnMeta("tax_rate", "TaxRate", DbType.Decimal, false),
            new ColumnMeta("final_price", "FinalPrice", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline FinalPrice=(Price-Discount)*(1+TaxRate)}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[price] = @price"));
        Assert.IsTrue(sql.Contains("[discount] = @discount"));
        Assert.IsTrue(sql.Contains("[tax_rate] = @tax_rate"));
        Assert.IsTrue(sql.Contains("[final_price] = ([price]-[discount])*(1+[tax_rate])"));
    }

    [TestMethod]
    public void Set_Inline_AuditTrail_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("data", "Data", DbType.String, false),
            new ColumnMeta("modified_count", "ModifiedCount", DbType.Int32, false),
            new ColumnMeta("last_modified_at", "LastModifiedAt", DbType.DateTime, false),
            new ColumnMeta("last_modified_by", "LastModifiedBy", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "records", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline ModifiedCount=ModifiedCount+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[data] = @data"));
        Assert.IsTrue(sql.Contains("[last_modified_by] = @last_modified_by"));
        Assert.IsTrue(sql.Contains("[last_modified_at] = @last_modified_at"));
        Assert.IsTrue(sql.Contains("[modified_count] = [modified_count]+1"));
    }
}
