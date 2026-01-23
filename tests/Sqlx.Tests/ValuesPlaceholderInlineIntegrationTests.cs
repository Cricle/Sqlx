// <copyright file="ValuesPlaceholderInlineIntegrationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Integration tests for {{values}} placeholder with --inline option in real-world scenarios.
/// </summary>
[TestClass]
public class ValuesPlaceholderInlineIntegrationTests
{
    #region Audit Trail Scenarios

    [TestMethod]
    public void Values_InlineAuditTrail_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("entity_type", "EntityType", DbType.String, false),
            new ColumnMeta("entity_id", "EntityId", DbType.Int64, false),
            new ColumnMeta("action", "Action", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("created_by", "CreatedBy", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "audit_logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@entity_type"));
        Assert.IsTrue(sql.Contains("@entity_id"));
        Assert.IsTrue(sql.Contains("@action"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("@created_by"));
    }

    #endregion

    #region Default Value Scenarios

    [TestMethod]
    public void Values_InlineDefaultStatus_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("priority", "Priority", DbType.Int32, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status='pending',Priority=0,CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var expected = "INSERT INTO [tasks] ([name], [status], [priority], [created_at]) VALUES (@name, 'pending', 0, CURRENT_TIMESTAMP)";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region UUID Generation Scenarios

    [TestMethod]
    public void Values_InlineUUIDGeneration_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Guid, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.PostgreSql, "entities", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=gen_random_uuid(),CreatedAt=NOW()}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("gen_random_uuid()"));
        Assert.IsTrue(sql.Contains("$name"));
        Assert.IsTrue(sql.Contains("NOW()"));
    }

    #endregion

    #region Sequence Generation Scenarios

    [TestMethod]
    public void Values_InlineSequenceNextVal_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("code", "Code", DbType.String, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.PostgreSql, "products", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=nextval('products_id_seq')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("nextval('products_id_seq')"));
        Assert.IsTrue(sql.Contains("$code"));
        Assert.IsTrue(sql.Contains("$name"));
    }

    #endregion

    #region Computed Value Scenarios

    [TestMethod]
    public void Values_InlineComputedTotal_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("quantity", "Quantity", DbType.Int32, false),
            new ColumnMeta("unit_price", "UnitPrice", DbType.Decimal, false),
            new ColumnMeta("total", "Total", DbType.Decimal, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "order_items", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Total=@quantity*@unitPrice,CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@quantity"));
        Assert.IsTrue(sql.Contains("@unit_price"));
        Assert.IsTrue(sql.Contains("@quantity*@unitPrice"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
    }

    #endregion

    #region String Transformation Scenarios

    [TestMethod]
    public void Values_InlineEmailNormalization_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Email=LOWER(TRIM(@email)),CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("LOWER(TRIM(@email))"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
    }

    #endregion

    #region Conditional Value Scenarios

    [TestMethod]
    public void Values_InlineConditionalStatus_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new ColumnMeta("status", "Status", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "accounts", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status=CASE WHEN @isActive=1 THEN 'active' ELSE 'inactive' END}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("@is_active"));
        Assert.IsTrue(sql.Contains("CASE WHEN @isActive=1 THEN 'active' ELSE 'inactive' END"));
    }

    #endregion

    #region Timestamp Scenarios

    [TestMethod]
    public void Values_InlineMultipleTimestamps_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("data", "Data", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
            new ColumnMeta("expires_at", "ExpiresAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "sessions", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP,ExpiresAt=datetime('now','+1 hour')}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@data"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("datetime('now'"));
    }

    #endregion

    #region Version Control Scenarios

    [TestMethod]
    public void Values_InlineInitialVersion_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("title", "Title", DbType.String, false),
            new ColumnMeta("content", "Content", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "documents", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Version=1,CreatedAt=CURRENT_TIMESTAMP}})",
            context);

        var expected = "INSERT INTO [documents] ([title], [content], [version], [created_at]) VALUES (@title, @content, 1, CURRENT_TIMESTAMP)";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region Null Handling Scenarios

    [TestMethod]
    public void Values_InlineCoalesceDefault_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("description", "Description", DbType.String, true),
            new ColumnMeta("status", "Status", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Description=COALESCE(@description,'No description'),Status='active'}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@name"));
        Assert.IsTrue(sql.Contains("COALESCE(@description"));
        Assert.IsTrue(sql.Contains("'active'"));
    }

    #endregion

    #region Cross-Dialect Scenarios

    [TestMethod]
    public void Values_InlineCrossDialect_SQLite_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=datetime('now')}})",
            context);

        Assert.IsTrue(template.Sql.Contains("datetime('now')"));
    }

    [TestMethod]
    public void Values_InlineCrossDialect_PostgreSQL_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.PostgreSql, "logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=NOW()}})",
            context);

        Assert.IsTrue(template.Sql.Contains("NOW()"));
        Assert.IsTrue(template.Sql.Contains("$name"));
    }

    [TestMethod]
    public void Values_InlineCrossDialect_MySQL_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.MySql, "logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=NOW()}})",
            context);

        Assert.IsTrue(template.Sql.Contains("NOW()"));
        Assert.IsTrue(template.Sql.Contains("@name"));
    }

    [TestMethod]
    public void Values_InlineCrossDialect_SqlServer_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SqlServer, "logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=GETDATE()}})",
            context);

        Assert.IsTrue(template.Sql.Contains("GETDATE()"));
        Assert.IsTrue(template.Sql.Contains("@name"));
    }

    [TestMethod]
    public void Values_InlineCrossDialect_Oracle_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.Oracle, "logs", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=SYSDATE}})",
            context);

        Assert.IsTrue(template.Sql.Contains("SYSDATE"));
        Assert.IsTrue(template.Sql.Contains(":name"));
    }

    #endregion

    #region Complex Real-World Scenarios

    [TestMethod]
    public void Values_InlineCompleteUserRegistration_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("username", "Username", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("password_hash", "PasswordHash", DbType.String, false),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new ColumnMeta("is_verified", "IsVerified", DbType.Boolean, false),
            new ColumnMeta("role", "Role", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Email=LOWER(@email),IsActive=1,IsVerified=0,Role='user',CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("@username"));
        Assert.IsTrue(sql.Contains("LOWER(@email)"));
        Assert.IsTrue(sql.Contains("@password_hash"));
        Assert.IsTrue(sql.Contains(", 1"));
        Assert.IsTrue(sql.Contains(", 0"));
        Assert.IsTrue(sql.Contains("'user'"));
        Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
    }

    #endregion
}
