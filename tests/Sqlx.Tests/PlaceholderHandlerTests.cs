using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;
using System.Data;

namespace Sqlx.Tests;

[TestClass]
public class PlaceholderHandlerTests
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
        return new PlaceholderContext(dialect ?? SqlDefine.SQLite, "users", TestColumns);
    }

    #region ColumnsPlaceholderHandler Tests

    [TestMethod]
    public void ColumnsHandler_Process_ReturnsAllColumns()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "");

        Assert.AreEqual("[id], [name], [email], [created_at]", result);
    }

    [TestMethod]
    public void ColumnsHandler_WithExclude_ExcludesSpecifiedColumn()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "--exclude Id");

        Assert.AreEqual("[name], [email], [created_at]", result);
        Assert.IsFalse(result.Contains("[id]"));
    }

    [TestMethod]
    public void ColumnsHandler_WithExcludeMultiple_ExcludesAllSpecified()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "--exclude Id,Email");

        Assert.AreEqual("[name], [created_at]", result);
    }

    [TestMethod]
    public void ColumnsHandler_MySqlDialect_UsesBackticks()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.MySql);

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("`id`"));
        Assert.IsTrue(result.Contains("`name`"));
    }

    [TestMethod]
    public void ColumnsHandler_PostgreSqlDialect_UsesDoubleQuotes()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.PostgreSql);

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("\"id\""));
        Assert.IsTrue(result.Contains("\"name\""));
    }

    [TestMethod]
    public void ColumnsHandler_GetType_ReturnsStatic()
    {
        var handler = ColumnsPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Static, handler.GetType(""));
        Assert.AreEqual(PlaceholderType.Static, handler.GetType("--exclude Id"));
    }

    #endregion

    #region ValuesPlaceholderHandler Tests

    [TestMethod]
    public void ValuesHandler_Process_ReturnsParameterPlaceholders()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "");

        Assert.AreEqual("@id, @name, @email, @created_at", result);
    }

    [TestMethod]
    public void ValuesHandler_WithExclude_ExcludesSpecifiedColumn()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "--exclude Id");

        Assert.AreEqual("@name, @email, @created_at", result);
        Assert.IsFalse(result.Contains("@id"));
    }

    [TestMethod]
    public void ValuesHandler_GetType_ReturnsStatic()
    {
        var handler = ValuesPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Static, handler.GetType(""));
    }

    #endregion

    #region SetPlaceholderHandler Tests

    [TestMethod]
    public void SetHandler_Process_ReturnsSetClause()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("[id] = @id"));
        Assert.IsTrue(result.Contains("[name] = @name"));
        Assert.IsTrue(result.Contains("[email] = @email"));
    }

    [TestMethod]
    public void SetHandler_WithExclude_ExcludesSpecifiedColumn()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "--exclude Id");

        Assert.IsFalse(result.Contains("[id] = @id"));
        Assert.IsTrue(result.Contains("[name] = @name"));
    }

    [TestMethod]
    public void SetHandler_MySqlDialect_UsesCorrectSyntax()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.MySql);

        var result = handler.Process(context, "--exclude Id");

        Assert.IsTrue(result.Contains("`name` = @name"));
    }

    [TestMethod]
    public void SetHandler_GetType_ReturnsStatic()
    {
        var handler = SetPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Static, handler.GetType(""));
    }

    #endregion

    #region TablePlaceholderHandler Tests

    [TestMethod]
    public void TableHandler_Process_ReturnsQuotedTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "");

        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    public void TableHandler_MySqlDialect_UsesBackticks()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.MySql);

        var result = handler.Process(context, "");

        Assert.AreEqual("`users`", result);
    }

    [TestMethod]
    public void TableHandler_GetType_ReturnsStatic()
    {
        var handler = TablePlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Static, handler.GetType(""));
    }

    #endregion

    #region WherePlaceholderHandler Tests

    [TestMethod]
    public void WhereHandler_Render_ReturnsWhereClause()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = CreateContext();
        var dynamicParams = new Dictionary<string, object?> { ["predicate"] = "status = 'active'" };

        var result = handler.Render(context, "--param predicate", dynamicParams);

        Assert.AreEqual("status = 'active'", result);
    }

    [TestMethod]
    public void WhereHandler_GetType_ReturnsDynamic()
    {
        var handler = WherePlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Dynamic, handler.GetType(""));
        Assert.AreEqual(PlaceholderType.Dynamic, handler.GetType("--param predicate"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereHandler_Render_WithoutParamOption_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Render(context, "", null);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereHandler_Render_MissingDynamicParam_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Render(context, "--param predicate", new Dictionary<string, object?>());
    }

    #endregion

    #region LimitPlaceholderHandler Tests

    [TestMethod]
    public void LimitHandler_WithCount_ReturnsStaticLimit()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "--count 10");

        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void LimitHandler_Render_WithParam_ReturnsDynamicLimit()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = CreateContext();
        var dynamicParams = new Dictionary<string, object?> { ["limit"] = 25 };

        var result = handler.Render(context, "--param limit", dynamicParams);

        Assert.AreEqual("LIMIT 25", result);
    }

    [TestMethod]
    public void LimitHandler_GetType_WithCount_ReturnsStatic()
    {
        var handler = LimitPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Static, handler.GetType("--count 10"));
    }

    [TestMethod]
    public void LimitHandler_GetType_WithParam_ReturnsDynamic()
    {
        var handler = LimitPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Dynamic, handler.GetType("--param limit"));
        Assert.AreEqual(PlaceholderType.Dynamic, handler.GetType(""));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void LimitHandler_Render_WithoutOption_ThrowsException()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Render(context, "", null);
    }

    #endregion

    #region OffsetPlaceholderHandler Tests

    [TestMethod]
    public void OffsetHandler_WithCount_ReturnsStaticOffset()
    {
        var handler = OffsetPlaceholderHandler.Instance;
        var context = CreateContext();

        var result = handler.Process(context, "--count 20");

        Assert.AreEqual("OFFSET 20", result);
    }

    [TestMethod]
    public void OffsetHandler_Render_WithParam_ReturnsDynamicOffset()
    {
        var handler = OffsetPlaceholderHandler.Instance;
        var context = CreateContext();
        var dynamicParams = new Dictionary<string, object?> { ["offset"] = 50 };

        var result = handler.Render(context, "--param offset", dynamicParams);

        Assert.AreEqual("OFFSET 50", result);
    }

    [TestMethod]
    public void OffsetHandler_GetType_WithCount_ReturnsStatic()
    {
        var handler = OffsetPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Static, handler.GetType("--count 20"));
    }

    [TestMethod]
    public void OffsetHandler_GetType_WithParam_ReturnsDynamic()
    {
        var handler = OffsetPlaceholderHandler.Instance;

        Assert.AreEqual(PlaceholderType.Dynamic, handler.GetType("--param offset"));
        Assert.AreEqual(PlaceholderType.Dynamic, handler.GetType(""));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void OffsetHandler_Render_WithoutOption_ThrowsException()
    {
        var handler = OffsetPlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Render(context, "", null);
    }

    #endregion

    #region PlaceholderContext Tests

    [TestMethod]
    public void PlaceholderContext_Properties_AreSet()
    {
        var context = CreateContext();

        Assert.AreEqual(SqlDefine.SQLite, context.Dialect);
        Assert.AreEqual("users", context.TableName);
        Assert.AreEqual(4, context.Columns.Count);
    }

    #endregion
}
