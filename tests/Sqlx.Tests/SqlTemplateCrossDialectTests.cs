using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Cross-dialect tests for SqlTemplate using DataRow.
/// Tests template preparation and rendering across all supported databases.
/// </summary>
[TestClass]
public class SqlTemplateCrossDialectTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("status", "Status", DbType.String, true),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    private static PlaceholderContext CreateContext(string dialect) => dialect switch
    {
        "SQLite" => new PlaceholderContext(SqlDefine.SQLite, "items", TestColumns),
        "SqlServer" => new PlaceholderContext(SqlDefine.SqlServer, "items", TestColumns),
        "MySql" => new PlaceholderContext(SqlDefine.MySql, "items", TestColumns),
        "PostgreSQL" => new PlaceholderContext(SqlDefine.PostgreSql, "items", TestColumns),
        "Oracle" => new PlaceholderContext(SqlDefine.Oracle, "items", TestColumns),
        "DB2" => new PlaceholderContext(SqlDefine.DB2, "items", TestColumns),
        _ => throw new ArgumentException($"Unknown dialect: {dialect}")
    };

    #region Basic Template Preparation

    [TestMethod]
    [DataRow("SQLite", "[id]", "[name]", "[status]", "[created_at]")]
    [DataRow("SqlServer", "[id]", "[name]", "[status]", "[created_at]")]
    [DataRow("MySql", "`id`", "`name`", "`status`", "`created_at`")]
    [DataRow("PostgreSQL", "\"id\"", "\"name\"", "\"status\"", "\"created_at\"")]
    [DataRow("Oracle", "\"id\"", "\"name\"", "\"status\"", "\"created_at\"")]
    [DataRow("DB2", "\"id\"", "\"name\"", "\"status\"", "\"created_at\"")]
    public void Prepare_ColumnsPlaceholder_AllDialects(string dialect, string col1, string col2, string col3, string col4)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);

        Assert.IsTrue(template.Sql.Contains(col1), $"[{dialect}] Missing {col1}. SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains(col2), $"[{dialect}] Missing {col2}. SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains(col3), $"[{dialect}] Missing {col3}. SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains(col4), $"[{dialect}] Missing {col4}. SQL: {template.Sql}");
    }

    [TestMethod]
    [DataRow("SQLite", "[items]")]
    [DataRow("SqlServer", "[items]")]
    [DataRow("MySql", "`items`")]
    [DataRow("PostgreSQL", "\"items\"")]
    [DataRow("Oracle", "\"items\"")]
    [DataRow("DB2", "\"items\"")]
    public void Prepare_TablePlaceholder_AllDialects(string dialect, string expectedTable)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);

        Assert.IsTrue(template.Sql.Contains(expectedTable), $"[{dialect}] SQL: {template.Sql}");
    }

    #endregion

    #region Values Placeholder

    [TestMethod]
    [DataRow("SQLite", "@id", "@name", "@status", "@created_at")]
    [DataRow("SqlServer", "@id", "@name", "@status", "@created_at")]
    [DataRow("MySql", "@id", "@name", "@status", "@created_at")]
    [DataRow("PostgreSQL", "$id", "$name", "$status", "$created_at")]
    [DataRow("Oracle", ":id", ":name", ":status", ":created_at")]
    [DataRow("DB2", "?", "?", "?", "?")]
    public void Prepare_ValuesPlaceholder_AllDialects(string dialect, string param1, string param2, string param3, string param4)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

        if (dialect == "DB2")
        {
            // DB2 uses positional parameters
            var questionMarkCount = template.Sql.Count(c => c == '?');
            Assert.AreEqual(4, questionMarkCount, $"[{dialect}] SQL: {template.Sql}");
        }
        else
        {
            Assert.IsTrue(template.Sql.Contains(param1), $"[{dialect}] SQL: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains(param2), $"[{dialect}] SQL: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains(param3), $"[{dialect}] SQL: {template.Sql}");
            Assert.IsTrue(template.Sql.Contains(param4), $"[{dialect}] SQL: {template.Sql}");
        }
    }

    #endregion

    #region Set Placeholder

    [TestMethod]
    [DataRow("SQLite", "[name] = @name")]
    [DataRow("SqlServer", "[name] = @name")]
    [DataRow("MySql", "`name` = @name")]
    [DataRow("PostgreSQL", "\"name\" = $name")]
    [DataRow("Oracle", "\"name\" = :name")]
    [DataRow("DB2", "\"name\" = ?")]
    public void Prepare_SetPlaceholder_AllDialects(string dialect, string expectedSet)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}}", context);

        Assert.IsTrue(template.Sql.Contains(expectedSet), $"[{dialect}] SQL: {template.Sql}");
    }

    #endregion

    #region Dynamic Placeholders

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Prepare_WherePlaceholder_MarkedAsDynamic(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);

        Assert.IsTrue(template.HasDynamicPlaceholders, $"[{dialect}] Should have dynamic placeholders");
    }

    [TestMethod]
    [DataRow("SQLite", "LIMIT 10")]
    [DataRow("SqlServer", "TOP 10")]
    [DataRow("MySql", "LIMIT 10")]
    [DataRow("PostgreSQL", "LIMIT 10")]
    [DataRow("Oracle", "FETCH FIRST 10 ROWS ONLY")]
    [DataRow("DB2", "FETCH FIRST 10 ROWS ONLY")]
    public void Render_LimitPlaceholder_AllDialects(string dialect, string expectedLimit)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{limit --param limit}}", context);

        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 10 });

        Assert.IsTrue(rendered.Contains(expectedLimit), $"[{dialect}] SQL: {rendered}");
    }

    [TestMethod]
    [DataRow("SQLite", "OFFSET 20")]
    [DataRow("SqlServer", "OFFSET 20")]
    [DataRow("MySql", "OFFSET 20")]
    [DataRow("PostgreSQL", "OFFSET 20")]
    [DataRow("Oracle", "OFFSET 20")]
    [DataRow("DB2", "OFFSET 20")]
    public void Render_OffsetPlaceholder_AllDialects(string dialect, string expectedOffset)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{offset --param offset}}", context);

        var rendered = template.Render(new Dictionary<string, object?> { ["offset"] = 20 });

        Assert.IsTrue(rendered.Contains(expectedOffset), $"[{dialect}] SQL: {rendered}");
    }

    #endregion

    #region Exclude Option

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Prepare_ColumnsWithExclude_ExcludesColumn(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);

        Assert.IsFalse(template.Sql.Contains("id"), $"[{dialect}] Should not contain 'id'. SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains("name"), $"[{dialect}] Should contain 'name'. SQL: {template.Sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Prepare_SetWithExclude_ExcludesColumn(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}}", context);

        Assert.IsFalse(template.Sql.Contains("id"), $"[{dialect}] Should not contain 'id'. SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains("name"), $"[{dialect}] Should contain 'name'. SQL: {template.Sql}");
    }

    #endregion

    #region Complex Templates

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Prepare_ComplexInsertTemplate_AllDialects(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})",
            context);

        Assert.IsFalse(template.HasDynamicPlaceholders, $"[{dialect}]");
        Assert.IsTrue(template.Sql.Contains("INSERT INTO"), $"[{dialect}] SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains("VALUES"), $"[{dialect}] SQL: {template.Sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Prepare_ComplexUpdateTemplate_AllDialects(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE {{where --param predicate}}",
            context);

        Assert.IsTrue(template.HasDynamicPlaceholders, $"[{dialect}]");
        Assert.IsTrue(template.Sql.Contains("UPDATE"), $"[{dialect}] SQL: {template.Sql}");
        Assert.IsTrue(template.Sql.Contains("SET"), $"[{dialect}] SQL: {template.Sql}");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Render_ComplexSelectWithPagination_AllDialects(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}} {{offset --param offset}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["predicate"] = "status = 'active'",
            ["limit"] = 10,
            ["offset"] = 20
        });

        Assert.IsTrue(rendered.Contains("SELECT"), $"[{dialect}] SQL: {rendered}");
        Assert.IsTrue(rendered.Contains("WHERE"), $"[{dialect}] SQL: {rendered}");
        Assert.IsTrue(rendered.Contains("status = 'active'"), $"[{dialect}] SQL: {rendered}");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Prepare_NoPlaceholders_ReturnsOriginalSql(string dialect)
    {
        var context = CreateContext(dialect);
        var originalSql = "SELECT * FROM items WHERE id = 1";
        var template = SqlTemplate.Prepare(originalSql, context);

        Assert.AreEqual(originalSql, template.Sql, $"[{dialect}]");
        Assert.IsFalse(template.HasDynamicPlaceholders, $"[{dialect}]");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Render_NullParameters_HandlesGracefully(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);

        var rendered = template.Render(null);

        Assert.AreEqual(template.Sql, rendered, $"[{dialect}]");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Render_EmptyParameters_HandlesGracefully(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);

        var rendered = template.Render(new Dictionary<string, object?>());

        Assert.AreEqual(template.Sql, rendered, $"[{dialect}]");
    }

    #endregion

    #region Parameter Handling

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Render_MissingParameter_ThrowsException(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);

        Assert.ThrowsException<KeyNotFoundException>(() =>
            template.Render(new Dictionary<string, object?>()));
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    [DataRow("Oracle")]
    [DataRow("DB2")]
    public void Render_NullParameterValue_HandlesCorrectly(string dialect)
    {
        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);

        var rendered = template.Render(new Dictionary<string, object?> { ["predicate"] = null });

        // Null predicate should result in empty WHERE clause or be handled gracefully
        Assert.IsNotNull(rendered, $"[{dialect}]");
    }

    #endregion
}
