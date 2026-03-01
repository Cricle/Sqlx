using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

[TestClass]
public class PlaceholderProcessorTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    private static PlaceholderContext CreateContext(SqlDialect? dialect = null)
    {
        return new PlaceholderContext(
            dialect ?? SqlDefine.SQLite,
            "users",
            TestColumns);
    }

    [TestMethod]
    public void SqlTemplate_TablePlaceholder_ReplacesWithTableName()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);
        
        Assert.AreEqual("SELECT * FROM [users]", template.Sql);
    }

    [TestMethod]
    public void SqlTemplate_ColumnsPlaceholder_ReplacesWithAllColumns()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [id], [name], [email], [created_at] FROM [users]", template.Sql);
    }

    [TestMethod]
    public void SqlTemplate_ColumnsWithExclude_ExcludesSpecifiedColumn()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);
        
        Assert.IsTrue(template.Sql.Contains("[name], [email], [created_at]"));
        Assert.IsFalse(template.Sql.Contains("[id]"));
    }

    [TestMethod]
    public void SqlTemplate_SetPlaceholder_GeneratesSetClause()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);
        
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
        Assert.IsTrue(template.Sql.Contains("[email] = @email"));
        Assert.IsFalse(template.Sql.Contains("[id] = @id"));
    }

    [TestMethod]
    public void SqlTemplate_LimitWithCount_GeneratesStaticLimit()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --count 10}}", context);
        
        Assert.IsTrue(template.Sql.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void SqlTemplate_WithDynamicWhere_HasDynamicPlaceholders()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void SqlTemplate_WithStaticOnly_NoDynamicPlaceholders()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void ExtractParameters_WithMultipleParams_ReturnsAllParamNames()
    {
        var sql = "SELECT * FROM users WHERE name = @name AND email = @email AND id = @id";
        
        var result = PlaceholderProcessor.ExtractParameters(sql);
        
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Contains("name"));
        Assert.IsTrue(result.Contains("email"));
        Assert.IsTrue(result.Contains("id"));
    }

    [TestMethod]
    public void SqlTemplate_MySqlDialect_UsesBackticks()
    {
        var context = CreateContext(SqlDefine.MySql);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("`id`"));
        Assert.IsTrue(template.Sql.Contains("`users`"));
    }

    [TestMethod]
    public void SqlTemplate_PostgreSqlDialect_UsesDoubleQuotes()
    {
        var context = CreateContext(SqlDefine.PostgreSql);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("\"id\""));
        Assert.IsTrue(template.Sql.Contains("\"users\""));
    }

    [TestMethod]
    public void RegisterHandler_CustomHandler_CanBeUsed()
    {
        // Verify handler registration works
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("columns", out var handler));
        Assert.AreEqual("columns", handler.Name);
    }
}
