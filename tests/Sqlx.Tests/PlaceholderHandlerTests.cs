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

    private static PlaceholderContext CreateContext(
        SqlDialect? dialect = null,
        IReadOnlyDictionary<string, object?>? dynamicParams = null)
    {
        return new PlaceholderContext(
            dialect ?? SqlDefine.SQLite,
            "users",
            TestColumns,
            dynamicParams);
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
    public void ValuesHandler_PostgreSqlDialect_UsesDollarPrefix()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = CreateContext(SqlDefine.PostgreSql);

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("$id"));
        Assert.IsTrue(result.Contains("$name"));
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
    public void WhereHandler_Process_ReturnsWhereClause()
    {
        var handler = WherePlaceholderHandler.Instance;
        var dynamicParams = new Dictionary<string, object?> { ["predicate"] = "status = 'active'" };
        var context = CreateContext(dynamicParams: dynamicParams);

        var result = handler.Process(context, "--param predicate");

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
    public void WhereHandler_WithoutParamOption_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Process(context, "");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereHandler_MissingDynamicParam_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Process(context, "--param predicate");
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
    public void LimitHandler_WithParam_ReturnsDynamicLimit()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var dynamicParams = new Dictionary<string, object?> { ["limit"] = 25 };
        var context = CreateContext(dynamicParams: dynamicParams);

        var result = handler.Process(context, "--param limit");

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
    public void LimitHandler_WithoutOption_ThrowsException()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Process(context, "");
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
    public void OffsetHandler_WithParam_ReturnsDynamicOffset()
    {
        var handler = OffsetPlaceholderHandler.Instance;
        var dynamicParams = new Dictionary<string, object?> { ["offset"] = 50 };
        var context = CreateContext(dynamicParams: dynamicParams);

        var result = handler.Process(context, "--param offset");

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
    public void OffsetHandler_WithoutOption_ThrowsException()
    {
        var handler = OffsetPlaceholderHandler.Instance;
        var context = CreateContext();

        handler.Process(context, "");
    }

    #endregion

    #region PlaceholderContext Tests

    [TestMethod]
    public void PlaceholderContext_WithDynamicParameters_CreatesNewContext()
    {
        var context = CreateContext();
        var dynamicParams = new Dictionary<string, object?> { ["key"] = "value" };

        var newContext = context.WithDynamicParameters(dynamicParams);

        Assert.IsNull(context.DynamicParameters);
        Assert.IsNotNull(newContext.DynamicParameters);
        Assert.AreEqual("value", newContext.DynamicParameters["key"]);
        Assert.AreEqual(context.Dialect, newContext.Dialect);
        Assert.AreEqual(context.TableName, newContext.TableName);
    }

    [TestMethod]
    public void PlaceholderContext_GetDynamicParameterValue_ReturnsValue()
    {
        var dynamicParams = new Dictionary<string, object?> { ["param1"] = 42 };
        var context = CreateContext(dynamicParams: dynamicParams);

        var value = context.GetDynamicParameterValue("param1", "test");

        Assert.AreEqual(42, value);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void PlaceholderContext_GetDynamicParameterValue_MissingParam_ThrowsException()
    {
        var context = CreateContext();

        context.GetDynamicParameterValue("missing", "test");
    }

    #endregion
}
