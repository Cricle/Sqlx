// <copyright file="SqlxVarIntegrationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Integration tests for SqlxVar feature - end-to-end testing.
/// </summary>
[TestClass]
public class SqlxVarIntegrationTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("tenant_id", "TenantId", DbType.String, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    // ===== Task 8.1: End-to-End Tests for Single Variable =====

    [TestMethod]
    public void EndToEnd_SingleVariable_InWhereClause()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant-123"), "SQL should contain the literal tenant ID value");
        Assert.IsTrue(sql.Contains("SELECT"), "SQL should contain SELECT");
        Assert.IsTrue(sql.Contains("FROM"), "SQL should contain FROM");
        Assert.IsTrue(sql.Contains("WHERE tenant_id = tenant-123"), 
            "SQL should have tenant ID as literal in WHERE clause");
        Assert.IsFalse(sql.Contains("{{var"), "SQL should not contain placeholder syntax");
    }

    [TestMethod]
    public void EndToEnd_SingleVariable_RenderedSQLContainsLiteralValue()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE tenant_id = tenant-123", sql);
    }

    [TestMethod]
    public void EndToEnd_SingleVariable_ValueNotParameter()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        // Value should be inserted as literal, not as parameter marker
        Assert.IsFalse(sql.Contains("@tenantId"), "Should not contain parameter marker");
        Assert.IsFalse(sql.Contains("$tenantId"), "Should not contain parameter marker");
        Assert.IsFalse(sql.Contains(":tenantId"), "Should not contain parameter marker");
        Assert.IsTrue(sql.Contains("tenant-123"), "Should contain literal value");
    }

    [TestMethod]
    public void EndToEnd_SingleVariable_WithColumnsAndTablePlaceholders()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("[id]"), "Should contain column names");
        Assert.IsTrue(sql.Contains("[tenant_id]"), "Should contain column names");
        Assert.IsTrue(sql.Contains("[users]"), "Should contain table name");
        Assert.IsTrue(sql.Contains("tenant-123"), "Should contain variable value");
    }

    [TestMethod]
    public void EndToEnd_SingleVariable_DifferentDialects()
    {
        // Arrange
        var repository = new TestRepository();

        // Test SQLite
        var sqliteTemplate = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns, TestRepository.VarProvider, repository));
        var sqliteSql = sqliteTemplate.Render(null);
        Assert.AreEqual("SELECT * FROM users WHERE tenant_id = tenant-123", sqliteSql);

        // Test PostgreSQL
        var pgTemplate = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns, TestRepository.VarProvider, repository));
        var pgSql = pgTemplate.Render(null);
        Assert.AreEqual("SELECT * FROM users WHERE tenant_id = tenant-123", pgSql);

        // Test MySQL
        var mysqlTemplate = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(SqlDefine.MySql, "users", TestColumns, TestRepository.VarProvider, repository));
        var mysqlSql = mysqlTemplate.Render(null);
        Assert.AreEqual("SELECT * FROM users WHERE tenant_id = tenant-123", mysqlSql);
    }

    // ===== Task 8.2: End-to-End Tests for Multiple Variables =====

    [TestMethod]
    public void EndToEnd_MultipleVariables_InSameQuery()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}} AND user_id = {{var --name userId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant-123"), "Should contain tenant ID");
        Assert.IsTrue(sql.Contains("user-456"), "Should contain user ID");
        Assert.IsTrue(sql.Contains("WHERE tenant_id = tenant-123 AND user_id = user-456"),
            "Should have both variables in WHERE clause");
    }

    [TestMethod]
    public void EndToEnd_MultipleVariables_AllResolved()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} (tenant_id, user_id, created_at) VALUES ({{var --name tenantId}}, {{var --name userId}}, {{var --name timestamp}})",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant-123"), "Should contain tenant ID");
        Assert.IsTrue(sql.Contains("user-456"), "Should contain user ID");
        Assert.IsTrue(sql.Contains("2026-02-06"), "Should contain timestamp");
    }

    [TestMethod]
    public void EndToEnd_MultipleVariables_WithOtherPlaceholders()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}} AND name = @name",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant-123"), "Should contain variable value");
        Assert.IsTrue(sql.Contains("@name"), "Should preserve SQL parameter");
        Assert.IsTrue(sql.Contains("[id]"), "Should contain columns");
    }

    // ===== Task 8.3: Tests for Static Methods =====

    [TestMethod]
    public void EndToEnd_StaticMethod_WorksCorrectly()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} (created_at) VALUES ({{var --name timestamp}})",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("2026-02-06"), "Should contain timestamp from static method");
    }

    [TestMethod]
    public void EndToEnd_MixedStaticAndInstanceMethods_BothWork()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} (tenant_id, created_at) VALUES ({{var --name tenantId}}, {{var --name timestamp}})",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant-123"), "Should contain value from instance method");
        Assert.IsTrue(sql.Contains("2026-02-06"), "Should contain value from static method");
    }

    // ===== Task 8.4: Multi-Tenant Scenario Tests =====

    [TestMethod]
    public void MultiTenant_TenantIdFiltering_WorksCorrectly()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}} ORDER BY created_at DESC",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("WHERE tenant_id = tenant-123"), 
            "Should filter by tenant ID");
        Assert.IsTrue(sql.Contains("ORDER BY"), "Should preserve ORDER BY clause");
    }

    [TestMethod]
    public void MultiTenant_MultipleContextVariables_AllResolved()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}} AND user_id = {{var --name userId}} AND created_at > {{var --name timestamp}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant_id = tenant-123"), "Should have tenant ID");
        Assert.IsTrue(sql.Contains("user_id = user-456"), "Should have user ID");
        Assert.IsTrue(sql.Contains("created_at > 2026-02-06"), "Should have timestamp");
    }

    [TestMethod]
    public void MultiTenant_RealisticQuery_GeneratesCorrectSQL()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            @"SELECT {{columns --exclude TenantId}} 
              FROM {{table}} 
              WHERE tenant_id = {{var --name tenantId}} 
              AND email = @email 
              ORDER BY created_at DESC",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("tenant_id = tenant-123"), "Should have tenant filter");
        Assert.IsTrue(sql.Contains("email = @email"), "Should preserve parameter");
        Assert.IsFalse(sql.Contains("[tenant_id]") && sql.Contains("SELECT"), 
            "Should exclude TenantId from SELECT columns");
    }

    // ===== Task 8.5: Error Scenario Tests =====

    [TestMethod]
    public void Error_MissingVarProvider_ThrowsClearException()
    {
        // Arrange
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                null,  // No VarProvider
                null));

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => template.Render(null));
        Assert.IsTrue(ex.Message.Contains("VarProvider"), 
            "Error should mention VarProvider");
        Assert.IsTrue(ex.Message.Contains("tenantId"), 
            "Error should mention the variable name");
    }

    [TestMethod]
    public void Error_UnknownVariableName_ThrowsWithAvailableVariables()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name unknownVar}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepository.VarProvider,
                repository));

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(
            () => template.Render(null));
        Assert.IsTrue(ex.Message.Contains("unknownVar"), 
            "Error should mention the unknown variable");
        Assert.IsTrue(ex.Message.Contains("Available variables"), 
            "Error should list available variables");
    }

    [TestMethod]
    public void Error_ExceptionFromVariableMethod_PropagatedWithContext()
    {
        // Arrange
        var repository = new TestRepositoryWithException();
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite,
                "users",
                TestColumns,
                TestRepositoryWithException.VarProvider,
                repository));

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(
            () => template.Render(null));
        Assert.IsTrue(ex.Message.Contains("Tenant context not set"), 
            "Original exception message should be preserved");
    }

    // Test helper classes
    private partial class TestRepository
    {
        public string GetTenantId() => "tenant-123";
        public string GetUserId() => "user-456";
        public static string GetTimestamp() => "2026-02-06";

        // Simulated generated GetVar method
        public static string GetVar(object instance, string methodName)
        {
            var repo = (TestRepository)instance;
            return methodName switch
            {
                "tenantId" => repo.GetTenantId(),
                "userId" => repo.GetUserId(),
                "timestamp" => GetTimestamp(),
                _ => throw new ArgumentException(
                    $"Unknown variable: {methodName}. Available variables: tenantId, userId, timestamp",
                    nameof(methodName))
            };
        }

        public static readonly Func<object, string, string> VarProvider = GetVar;
    }

    private partial class TestRepositoryWithException
    {
        public string GetTenantId() => throw new InvalidOperationException("Tenant context not set");

        public static string GetVar(object instance, string methodName)
        {
            var repo = (TestRepositoryWithException)instance;
            return methodName switch
            {
                "tenantId" => repo.GetTenantId(),
                _ => throw new ArgumentException($"Unknown variable: {methodName}")
            };
        }

        public static readonly Func<object, string, string> VarProvider = GetVar;
    }

    // ===== Additional Boundary Tests =====

    [TestMethod]
    public void BoundaryTest_EmptyStringValue_HandlesCorrectly()
    {
        // Arrange
        var varProvider = new Func<object, string, string>((instance, name) => string.Empty);
        var context = new PlaceholderContext(
            SqlDefine.SQLite, "users", TestColumns, varProvider, new object());
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            context);

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE tenant_id = ", sql,
            "Should handle empty string value");
    }

    [TestMethod]
    public void BoundaryTest_WhitespaceValue_PreservesWhitespace()
    {
        // Arrange
        var varProvider = new Func<object, string, string>((instance, name) => "   ");
        var context = new PlaceholderContext(
            SqlDefine.SQLite, "users", TestColumns, varProvider, new object());
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            context);

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual("SELECT * FROM users WHERE tenant_id =    ", sql,
            "Should preserve whitespace in value");
    }

    [TestMethod]
    public void BoundaryTest_SpecialSQLCharacters_InsertsAsIs()
    {
        // Arrange
        var varProvider = new Func<object, string, string>((instance, name) => "'; DROP TABLE users; --");
        var context = new PlaceholderContext(
            SqlDefine.SQLite, "users", TestColumns, varProvider, new object());
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            context);

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains("'; DROP TABLE users; --"),
            "Should insert special characters as-is (demonstrating SQL injection risk)");
    }

    [TestMethod]
    public void BoundaryTest_VeryLongValue_HandlesCorrectly()
    {
        // Arrange
        var longValue = new string('x', 10000);
        var varProvider = new Func<object, string, string>((instance, name) => longValue);
        var context = new PlaceholderContext(
            SqlDefine.SQLite, "users", TestColumns, varProvider, new object());
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            context);

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains(longValue), "Should handle very long values");
        Assert.IsTrue(sql.Length > 10000, "SQL should contain the long value");
    }

    [TestMethod]
    public void BoundaryTest_UnicodeCharacters_PreservesCorrectly()
    {
        // Arrange
        var unicodeValue = "租户-123-日本語-🎉";
        var varProvider = new Func<object, string, string>((instance, name) => unicodeValue);
        var context = new PlaceholderContext(
            SqlDefine.SQLite, "users", TestColumns, varProvider, new object());
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
            context);

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.IsTrue(sql.Contains(unicodeValue), "Should preserve Unicode characters");
    }

    [TestMethod]
    public void BoundaryTest_MultipleVarPlaceholdersInSequence_AllResolved()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE a = {{var --name tenantId}} AND b = {{var --name userId}} AND c = {{var --name timestamp}}",
            new PlaceholderContext(
                SqlDefine.SQLite, "users", TestColumns, TestRepository.VarProvider, repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual(
            "SELECT * FROM users WHERE a = tenant-123 AND b = user-456 AND c = 2026-02-06",
            sql);
    }

    [TestMethod]
    public void BoundaryTest_VarPlaceholderAtStartOfTemplate_WorksCorrectly()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "{{var --name tenantId}} IS THE TENANT",
            new PlaceholderContext(
                SqlDefine.SQLite, "users", TestColumns, TestRepository.VarProvider, repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual("tenant-123 IS THE TENANT", sql);
    }

    [TestMethod]
    public void BoundaryTest_VarPlaceholderAtEndOfTemplate_WorksCorrectly()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "THE TENANT IS {{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite, "users", TestColumns, TestRepository.VarProvider, repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual("THE TENANT IS tenant-123", sql);
    }

    [TestMethod]
    public void BoundaryTest_OnlyVarPlaceholder_WorksCorrectly()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "{{var --name tenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite, "users", TestColumns, TestRepository.VarProvider, repository));

        // Act
        var sql = template.Render(null);

        // Assert
        Assert.AreEqual("tenant-123", sql);
    }

    [TestMethod]
    public void BoundaryTest_CaseSensitiveVariableName_ExactMatchRequired()
    {
        // Arrange
        var repository = new TestRepository();
        var template = SqlTemplate.Prepare(
            "SELECT * FROM users WHERE tenant_id = {{var --name TenantId}}",
            new PlaceholderContext(
                SqlDefine.SQLite, "users", TestColumns, TestRepository.VarProvider, repository));

        // Act & Assert
        // Variable names are case-sensitive, "TenantId" != "tenantId"
        var ex = Assert.ThrowsException<ArgumentException>(() => template.Render(null));
        Assert.IsTrue(ex.Message.Contains("TenantId"), "Should mention the incorrect case");
    }
}
