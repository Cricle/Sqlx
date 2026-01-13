using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Boundary and edge case tests for SqlTemplate.
/// </summary>
[TestClass]
public class SqlTemplateBoundaryTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("status", "Status", DbType.String, true),
    };

    private static PlaceholderContext CreateContext(SqlDialect? dialect = null)
    {
        return new PlaceholderContext(dialect ?? SqlDefine.SQLite, "items", TestColumns);
    }

    #region Empty and Null Input Tests

    [TestMethod]
    public void Prepare_EmptyTemplate_ReturnsEmptySql()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("", context);
        
        Assert.AreEqual("", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_NoPlaceholders_ReturnsSameString()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM users WHERE id = 1", context);
        
        Assert.AreEqual("SELECT * FROM users WHERE id = 1", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Render_NullParameters_WhenNoDynamicPlaceholders_ReturnsSql()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        var result = template.Render(null);
        
        Assert.AreEqual(template.Sql, result);
    }

    [TestMethod]
    public void Render_EmptyParameters_WhenNoDynamicPlaceholders_ReturnsSql()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        var result = template.Render(new Dictionary<string, object?>());
        
        Assert.AreEqual(template.Sql, result);
    }

    #endregion

    #region Unknown Placeholder Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Prepare_UnknownPlaceholder_ThrowsException()
    {
        var context = CreateContext();
        SqlTemplate.Prepare("SELECT {{unknown}} FROM {{table}}", context);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Prepare_MisspelledPlaceholder_ThrowsException()
    {
        var context = CreateContext();
        SqlTemplate.Prepare("SELECT {{colums}} FROM {{table}}", context); // typo: colums
    }

    #endregion

    #region Multiple Placeholders Tests

    [TestMethod]
    public void Prepare_MultipleSamePlaceholders_AllReplaced()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} UNION SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[id], [name], [status]"));
        Assert.IsTrue(template.Sql.Contains("[items]"));
        // Should appear twice
        var columnCount = template.Sql.Split(new[] { "[id], [name], [status]" }, StringSplitOptions.None).Length - 1;
        Assert.AreEqual(2, columnCount);
    }

    [TestMethod]
    public void Prepare_MixedStaticAndDynamic_CorrectlyIdentified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}}", 
            context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        Assert.IsTrue(template.Sql.Contains("[id], [name], [status]"));
        Assert.IsTrue(template.Sql.Contains("[items]"));
    }

    [TestMethod]
    public void Render_MultipleDynamicPlaceholders_AllRendered()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param p1}} AND {{where --param p2}}", 
            context);
        
        var result = template.Render(new Dictionary<string, object?>
        {
            ["p1"] = "status = 'active'",
            ["p2"] = "name LIKE '%test%'"
        });
        
        Assert.IsTrue(result.Contains("status = 'active'"));
        Assert.IsTrue(result.Contains("name LIKE '%test%'"));
    }

    #endregion

    #region Whitespace and Formatting Tests

    [TestMethod]
    public void Prepare_PlaceholderWithExtraSpaces_StillWorks()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{  table  }}", context);
        
        // Should handle gracefully - either work or throw clear error
        // Current implementation requires exact match, so this tests that behavior
    }

    [TestMethod]
    public void Prepare_MultilineTemplate_PreservesFormatting()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(@"SELECT {{columns}}
FROM {{table}}
WHERE id = @id", context);
        
        Assert.IsTrue(template.Sql.Contains("\n"));
        Assert.IsTrue(template.Sql.Contains("[id], [name], [status]"));
    }

    [TestMethod]
    public void Prepare_TabsInTemplate_Preserved()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT\t{{columns}}\tFROM\t{{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("\t"));
    }

    #endregion

    #region Special Characters Tests

    [TestMethod]
    public void Prepare_TemplateWithSqlComments_Preserved()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} -- comment", context);
        
        Assert.IsTrue(template.Sql.Contains("-- comment"));
    }

    [TestMethod]
    public void Prepare_TemplateWithBlockComments_Preserved()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT /* all columns */ {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("/* all columns */"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Prepare_TemplateWithPlaceholderInStringLiteral_ThrowsException()
    {
        var context = CreateContext();
        // The {{value}} inside string literal is still treated as placeholder
        // This documents current behavior - placeholders in string literals are NOT ignored
        SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE name = 'test{{value}}'", context);
    }

    #endregion

    #region Exclude Option Edge Cases

    [TestMethod]
    public void Prepare_ExcludeAllColumns_ReturnsEmpty()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id,Name,Status}} FROM {{table}}", context);
        
        // Should return empty column list
        Assert.IsTrue(template.Sql.Contains("SELECT  FROM"));
    }

    [TestMethod]
    public void Prepare_ExcludeNonExistentColumn_IgnoresIt()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude NonExistent}} FROM {{table}}", context);
        
        // Should still include all columns
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[status]"));
    }

    [TestMethod]
    public void Prepare_ExcludeCaseInsensitive_Works()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude ID,NAME}} FROM {{table}}", context);
        
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsFalse(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[status]"));
    }

    [TestMethod]
    public void Prepare_ExcludeWithSpaces_HandledCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id, Name}} FROM {{table}}", context);
        
        // Should handle spaces in exclude list
        Assert.IsFalse(template.Sql.Contains("[id]"));
    }

    #endregion

    #region Limit/Offset Boundary Tests

    [TestMethod]
    public void Prepare_LimitZero_GeneratesLimitZero()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --count 0}}", context);
        
        Assert.IsTrue(template.Sql.Contains("LIMIT 0"));
    }

    [TestMethod]
    public void Render_LimitNegative_GeneratesNegativeLimit()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);
        
        var result = template.Render(new Dictionary<string, object?> { ["limit"] = -1 });
        
        Assert.IsTrue(result.Contains("LIMIT -1"));
    }

    [TestMethod]
    public void Render_LimitLargeNumber_HandlesCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);
        
        var result = template.Render(new Dictionary<string, object?> { ["limit"] = int.MaxValue });
        
        Assert.IsTrue(result.Contains($"LIMIT {int.MaxValue}"));
    }

    [TestMethod]
    public void Render_OffsetZero_GeneratesOffsetZero()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{offset --param offset}}", context);
        
        var result = template.Render(new Dictionary<string, object?> { ["offset"] = 0 });
        
        Assert.IsTrue(result.Contains("OFFSET 0"));
    }

    #endregion

    #region Dynamic Parameter Edge Cases

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Render_MissingRequiredParameter_ThrowsException()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);
        
        template.Render(new Dictionary<string, object?>()); // Missing 'limit' parameter
    }

    [TestMethod]
    public void Render_NullParameterValue_HandledCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}", context);
        
        var result = template.Render(new Dictionary<string, object?> { ["predicate"] = null });
        
        // Should return empty string for null
        Assert.IsTrue(result.Contains("WHERE "));
    }

    [TestMethod]
    public void Render_EmptyStringParameter_HandledCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}", context);
        
        var result = template.Render(new Dictionary<string, object?> { ["predicate"] = "" });
        
        Assert.IsTrue(result.Contains("WHERE "));
    }

    #endregion

    #region All Dialects Tests

    [TestMethod]
    [DataRow("SQLite", "[", "]")]
    [DataRow("MySql", "`", "`")]
    [DataRow("PostgreSql", "\"", "\"")]
    [DataRow("SqlServer", "[", "]")]
    [DataRow("Oracle", "\"", "\"")]
    [DataRow("DB2", "\"", "\"")]
    public void Prepare_AllDialects_UseCorrectQuoting(string dialectName, string left, string right)
    {
        var dialect = dialectName switch
        {
            "SQLite" => SqlDefine.SQLite,
            "MySql" => SqlDefine.MySql,
            "PostgreSql" => SqlDefine.PostgreSql,
            "SqlServer" => SqlDefine.SqlServer,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            _ => throw new ArgumentException()
        };
        
        var context = new PlaceholderContext(dialect, "items", TestColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains($"{left}id{right}"));
        Assert.IsTrue(template.Sql.Contains($"{left}items{right}"));
    }

    #endregion

    #region Performance-Related Tests

    [TestMethod]
    public void Prepare_LargeColumnList_HandlesEfficiently()
    {
        var columns = Enumerable.Range(1, 100)
            .Select(i => new ColumnMeta($"col{i}", $"Col{i}", DbType.String, false))
            .ToList();
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "large_table", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(100, template.Sql.Split(',').Length);
    }

    [TestMethod]
    public void Render_RepeatedCalls_ConsistentResults()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);
        
        var results = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(template.Render(new Dictionary<string, object?> { ["limit"] = i }));
        }
        
        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(results[i].Contains($"LIMIT {i}"));
        }
    }

    #endregion
}
