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
        Assert.AreEqual("SELECT [id], [name], [status] FROM [items]", template.PreparedSql);
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
    public void StaticParameters_ExtractsParameterNames()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE id = @id AND name = @name", context);
        
        Assert.AreEqual(2, template.StaticParameters.Count);
        Assert.IsTrue(template.StaticParameters.Contains("id"));
        Assert.IsTrue(template.StaticParameters.Contains("name"));
    }

    [TestMethod]
    public void Template_PreservesOriginalTemplate()
    {
        var context = CreateContext();
        var originalTemplate = "SELECT {{columns}} FROM {{table}}";
        var template = SqlTemplate.Prepare(originalTemplate, context);
        
        Assert.AreEqual(originalTemplate, template.Template);
    }
}
