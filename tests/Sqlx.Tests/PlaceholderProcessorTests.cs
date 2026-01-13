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
    public void Prepare_TablePlaceholder_ReplacesWithTableName()
    {
        var context = CreateContext();
        var template = "SELECT * FROM {{table}}";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.AreEqual("SELECT * FROM [users]", result);
    }

    [TestMethod]
    public void Prepare_ColumnsPlaceholder_ReplacesWithAllColumns()
    {
        var context = CreateContext();
        var template = "SELECT {{columns}} FROM {{table}}";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.AreEqual("SELECT [id], [name], [email], [created_at] FROM [users]", result);
    }

    [TestMethod]
    public void Prepare_ColumnsWithExclude_ExcludesSpecifiedColumn()
    {
        var context = CreateContext();
        var template = "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.IsTrue(result.Contains("[name], [email], [created_at]"));
        Assert.IsFalse(result.Contains("[id]"));
    }

    [TestMethod]
    public void Prepare_SetPlaceholder_GeneratesSetClause()
    {
        var context = CreateContext();
        var template = "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.IsTrue(result.Contains("[name] = @name"));
        Assert.IsTrue(result.Contains("[email] = @email"));
        Assert.IsFalse(result.Contains("[id] = @id"));
    }

    [TestMethod]
    public void Prepare_LimitWithCount_GeneratesStaticLimit()
    {
        var context = CreateContext();
        var template = "SELECT {{columns}} FROM {{table}} {{limit --count 10}}";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.IsTrue(result.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void ContainsDynamicPlaceholders_WithDynamicWhere_ReturnsTrue()
    {
        var template = "SELECT * FROM {{table}} WHERE {{where --param predicate}}";
        
        var result = PlaceholderProcessor.ContainsDynamicPlaceholders(template);
        
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ContainsDynamicPlaceholders_WithStaticOnly_ReturnsFalse()
    {
        var template = "SELECT {{columns}} FROM {{table}}";
        
        var result = PlaceholderProcessor.ContainsDynamicPlaceholders(template);
        
        Assert.IsFalse(result);
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
    public void Prepare_MySqlDialect_UsesBackticks()
    {
        var context = CreateContext(SqlDefine.MySql);
        var template = "SELECT {{columns}} FROM {{table}}";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.IsTrue(result.Contains("`id`"));
        Assert.IsTrue(result.Contains("`users`"));
    }

    [TestMethod]
    public void Prepare_PostgreSqlDialect_UsesDoubleQuotes()
    {
        var context = CreateContext(SqlDefine.PostgreSql);
        var template = "SELECT {{columns}} FROM {{table}}";
        
        var result = PlaceholderProcessor.Prepare(template, context);
        
        Assert.IsTrue(result.Contains("\"id\""));
        Assert.IsTrue(result.Contains("\"users\""));
    }
}
