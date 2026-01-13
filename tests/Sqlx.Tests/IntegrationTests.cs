using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Runtime.Serialization;

namespace Sqlx.Tests;

/// <summary>
/// Integration tests for the complete Sqlx workflow.
/// </summary>
[TestClass]
public class IntegrationTests
{
    #region Test Entity and Repository Setup

    /// <summary>
    /// Test entity with various property types and attributes.
    /// </summary>
    [SqlxEntity]
    [SqlxParameter]
    public class TestUser
    {
        public long Id { get; set; }
        
        [Column("user_name")]
        public string Name { get; set; } = string.Empty;
        
        public string? Email { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public bool IsActive { get; set; }
        
        [IgnoreDataMember]
        public string? IgnoredProperty { get; set; }
    }

    #endregion

    #region PlaceholderContext Integration Tests

    [TestMethod]
    public void PlaceholderContext_WithEntityColumns_ProcessesCorrectly()
    {
        // Arrange - simulate what generated code would do
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("user_name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        // Act - prepare a template
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive",
            context);
        
        // Assert
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [created_at], [is_active] FROM [users] WHERE is_active = @isActive",
            template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void SqlTemplate_InsertStatement_GeneratesCorrectSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO [users] ([name], [email]) VALUES (@name, @email)",
            template.Sql);
    }

    [TestMethod]
    public void SqlTemplate_UpdateStatement_GeneratesCorrectSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE [users] SET [name] = @name, [email] = @email WHERE id = @id",
            template.Sql);
    }

    [TestMethod]
    public void SqlTemplate_WithDynamicWhere_RequiresRender()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}",
            context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        
        // Render with dynamic parameters
        var dynamicParams = new Dictionary<string, object?> { ["predicate"] = "name = 'test'" };
        var renderedSql = template.Render(dynamicParams);
        
        Assert.AreEqual(
            "SELECT [id], [name] FROM [users] WHERE name = 'test'",
            renderedSql);
    }

    [TestMethod]
    public void SqlTemplate_WithDynamicLimitOffset_RequiresRender()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}",
            context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        
        var dynamicParams = new Dictionary<string, object?> 
        { 
            ["limit"] = 10,
            ["offset"] = 20 
        };
        var renderedSql = template.Render(dynamicParams);
        
        Assert.AreEqual(
            "SELECT [id], [name] FROM [users] LIMIT 10 OFFSET 20",
            renderedSql);
    }

    #endregion

    #region Multi-Dialect Tests

    [TestMethod]
    public void SqlTemplate_MySqlDialect_GeneratesCorrectSyntax()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.MySql, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}}",
            context);
        
        Assert.AreEqual("SELECT `id`, `name` FROM `users`", template.Sql);
    }

    [TestMethod]
    public void SqlTemplate_PostgreSqlDialect_GeneratesCorrectSyntax()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            context);
        
        // PostgreSQL uses double quotes for identifiers
        Assert.IsTrue(template.Sql.Contains("\"id\""));
        Assert.IsTrue(template.Sql.Contains("\"users\""));
    }

    [TestMethod]
    public void SqlTemplate_SqlServerDialect_GeneratesCorrectSyntax()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}}",
            context);
        
        // SQL Server uses square brackets
        Assert.AreEqual("SELECT [id], [name] FROM [users]", template.Sql);
    }

    #endregion

    #region Static vs Dynamic Placeholder Tests

    [TestMethod]
    public void StaticLimit_ProcessedDuringPrepare()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --count 100}}",
            context);
        
        Assert.IsFalse(template.HasDynamicPlaceholders);
        Assert.AreEqual("SELECT [id] FROM [users] LIMIT 100", template.Sql);
    }

    [TestMethod]
    public void DynamicLimit_ProcessedDuringRender()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param pageSize}}",
            context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        
        var renderedSql = template.Render(new Dictionary<string, object?> { ["pageSize"] = 50 });
        Assert.AreEqual("SELECT [id] FROM [users] LIMIT 50", renderedSql);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void UnknownPlaceholder_ThrowsException()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        SqlTemplate.Prepare("SELECT * FROM {{unknown}}", context);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void MissingDynamicParameter_ThrowsException()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table}} WHERE {{where --param predicate}}",
            context);
        
        // Render without providing the required parameter
        template.Render(new Dictionary<string, object?>());
    }

    #endregion

    #region Complex Query Tests

    [TestMethod]
    public void ComplexQuery_WithMultiplePlaceholders_GeneratesCorrectSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
            new ColumnMeta("status", "Status", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        
        // Complex query with static and dynamic placeholders
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}} ORDER BY name {{limit --param limit}} {{offset --param offset}}",
            context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        
        var dynamicParams = new Dictionary<string, object?>
        {
            ["filter"] = "status = 'active'",
            ["limit"] = 25,
            ["offset"] = 50
        };
        
        var renderedSql = template.Render(dynamicParams);
        
        Assert.AreEqual(
            "SELECT [id], [name], [email], [status] FROM [users] WHERE status = 'active' ORDER BY name LIMIT 25 OFFSET 50",
            renderedSql);
    }

    #endregion
}
