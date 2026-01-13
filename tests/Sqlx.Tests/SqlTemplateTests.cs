using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

[TestClass]
public class SqlTemplateTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("status", "Status", DbType.String, true),
    };

    private static PlaceholderContext CreateContext()
    {
        return new PlaceholderContext(SqlDefine.SQLite, "items", TestColumns);
    }

    [TestMethod]
    public void Prepare_StaticTemplate_ReturnsPreparedSql()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [id], [name], [status] FROM [items]", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_DynamicTemplate_HasDynamicPlaceholders()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Render_WithDynamicParameters_ReplacesPlaceholders()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 10 });
        
        Assert.IsTrue(rendered.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void Render_NoDynamicPlaceholders_ReturnsSql()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        var rendered = template.Render(null);
        
        Assert.AreEqual(template.Sql, rendered);
    }

    [TestMethod]
    public void Render_MultipleParameters_AllReplaced()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> 
        { 
            ["limit"] = 10,
            ["offset"] = 20
        });
        
        Assert.IsTrue(rendered.Contains("LIMIT 10"));
        Assert.IsTrue(rendered.Contains("OFFSET 20"));
    }
}
