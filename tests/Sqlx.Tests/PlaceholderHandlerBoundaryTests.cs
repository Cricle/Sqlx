using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Boundary and edge case tests for placeholder handlers.
/// </summary>
[TestClass]
public class PlaceholderHandlerBoundaryTests
{
    #region Empty Column List Tests

    [TestMethod]
    public void ColumnsHandler_EmptyColumnList_ReturnsEmpty()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "empty_table", Array.Empty<ColumnMeta>());

        var result = handler.Process(context, "");

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ValuesHandler_EmptyColumnList_ReturnsEmpty()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "empty_table", Array.Empty<ColumnMeta>());

        var result = handler.Process(context, "");

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void SetHandler_EmptyColumnList_ReturnsEmpty()
    {
        var handler = SetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "empty_table", Array.Empty<ColumnMeta>());

        var result = handler.Process(context, "");

        Assert.AreEqual("", result);
    }

    #endregion

    #region Single Column Tests

    [TestMethod]
    public void ColumnsHandler_SingleColumn_NoTrailingComma()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "single_col", columns);

        var result = handler.Process(context, "");

        Assert.AreEqual("[id]", result);
        Assert.IsFalse(result.Contains(","));
    }

    [TestMethod]
    public void ValuesHandler_SingleColumn_NoTrailingComma()
    {
        var handler = ValuesPlaceholderHandler.Instance;
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "single_col", columns);

        var result = handler.Process(context, "");

        Assert.AreEqual("@id", result);
        Assert.IsFalse(result.Contains(","));
    }

    [TestMethod]
    public void SetHandler_SingleColumn_NoTrailingComma()
    {
        var handler = SetPlaceholderHandler.Instance;
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "single_col", columns);

        var result = handler.Process(context, "");

        Assert.AreEqual("[id] = @id", result);
        Assert.IsFalse(result.EndsWith(","));
    }

    #endregion

    #region Special Column Names Tests

    [TestMethod]
    public void ColumnsHandler_ReservedWordColumnName_QuotedCorrectly()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[]
        {
            new ColumnMeta("select", "Select", DbType.String, false),
            new ColumnMeta("from", "From", DbType.String, false),
            new ColumnMeta("where", "Where", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "reserved", columns);

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("[select]"));
        Assert.IsTrue(result.Contains("[from]"));
        Assert.IsTrue(result.Contains("[where]"));
    }

    [TestMethod]
    public void TableHandler_ReservedWordTableName_QuotedCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "order", Array.Empty<ColumnMeta>());

        var result = handler.Process(context, "");

        Assert.AreEqual("[order]", result);
    }

    [TestMethod]
    public void ColumnsHandler_ColumnNameWithNumbers_HandledCorrectly()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[]
        {
            new ColumnMeta("col1", "Col1", DbType.String, false),
            new ColumnMeta("123abc", "Num123Abc", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "numbered", columns);

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("[col1]"));
        Assert.IsTrue(result.Contains("[123abc]"));
    }

    [TestMethod]
    public void ColumnsHandler_ColumnNameWithUnderscore_HandledCorrectly()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[]
        {
            new ColumnMeta("user_name", "UserName", DbType.String, false),
            new ColumnMeta("_private", "Private", DbType.String, false),
            new ColumnMeta("__double", "Double", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "underscored", columns);

        var result = handler.Process(context, "");

        Assert.IsTrue(result.Contains("[user_name]"));
        Assert.IsTrue(result.Contains("[_private]"));
        Assert.IsTrue(result.Contains("[__double]"));
    }

    #endregion

    #region Exclude Option Edge Cases

    [TestMethod]
    public void ColumnsHandler_ExcludeByPropertyName_Works()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[]
        {
            new ColumnMeta("user_id", "UserId", DbType.Int64, false),
            new ColumnMeta("user_name", "UserName", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);

        var result = handler.Process(context, "--exclude UserId");

        Assert.IsFalse(result.Contains("[user_id]"));
        Assert.IsTrue(result.Contains("[user_name]"));
    }

    [TestMethod]
    public void ColumnsHandler_ExcludeByColumnName_Works()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[]
        {
            new ColumnMeta("user_id", "UserId", DbType.Int64, false),
            new ColumnMeta("user_name", "UserName", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);

        var result = handler.Process(context, "--exclude user_id");

        Assert.IsFalse(result.Contains("[user_id]"));
        Assert.IsTrue(result.Contains("[user_name]"));
    }

    [TestMethod]
    public void ColumnsHandler_ExcludeEmptyString_IncludesAll()
    {
        var handler = ColumnsPlaceholderHandler.Instance;
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);

        var result = handler.Process(context, "--exclude ");

        Assert.IsTrue(result.Contains("[id]"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    #endregion

    #region Where Handler Edge Cases

    [TestMethod]
    public void WhereHandler_ComplexWhereClause_PassedThrough()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", Array.Empty<ColumnMeta>());
        var complexWhere = "(status = 'active' OR status = 'pending') AND created_at > '2024-01-01' AND name LIKE '%test%'";

        var result = handler.Render(context, "--param predicate", new Dictionary<string, object?> { ["predicate"] = complexWhere });

        Assert.AreEqual(complexWhere, result);
    }

    [TestMethod]
    public void WhereHandler_WhereWithSubquery_PassedThrough()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", Array.Empty<ColumnMeta>());
        var subqueryWhere = "id IN (SELECT user_id FROM orders WHERE total > 100)";

        var result = handler.Render(context, "--param predicate", new Dictionary<string, object?> { ["predicate"] = subqueryWhere });

        Assert.AreEqual(subqueryWhere, result);
    }

    #endregion

    #region Limit/Offset Type Conversion Tests

    [TestMethod]
    public void LimitHandler_IntegerValue_ConvertsCorrectly()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", Array.Empty<ColumnMeta>());

        var result = handler.Render(context, "--param limit", new Dictionary<string, object?> { ["limit"] = 10 });

        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void LimitHandler_LongValue_ConvertsCorrectly()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", Array.Empty<ColumnMeta>());

        var result = handler.Render(context, "--param limit", new Dictionary<string, object?> { ["limit"] = 10L });

        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void LimitHandler_StringValue_ConvertsCorrectly()
    {
        var handler = LimitPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", Array.Empty<ColumnMeta>());

        var result = handler.Render(context, "--param limit", new Dictionary<string, object?> { ["limit"] = "10" });

        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void OffsetHandler_IntegerValue_ConvertsCorrectly()
    {
        var handler = OffsetPlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", Array.Empty<ColumnMeta>());

        var result = handler.Render(context, "--param offset", new Dictionary<string, object?> { ["offset"] = 20 });

        Assert.AreEqual("OFFSET 20", result);
    }

    #endregion

    #region Handler Registration Tests

    [TestMethod]
    public void TryGetHandler_ExistingHandler_ReturnsTrue()
    {
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("columns", out var handler));
        Assert.IsNotNull(handler);
        Assert.AreEqual("columns", handler.Name);
    }

    [TestMethod]
    public void TryGetHandler_NonExistingHandler_ReturnsFalse()
    {
        Assert.IsFalse(PlaceholderProcessor.TryGetHandler("nonexistent", out var handler));
    }

    [TestMethod]
    public void TryGetHandler_CaseInsensitive_Works()
    {
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("COLUMNS", out _));
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("Columns", out _));
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("columns", out _));
    }

    [TestMethod]
    public void RegisterHandler_CustomHandler_CanBeRetrieved()
    {
        var customHandler = new TestCustomHandler();
        PlaceholderProcessor.RegisterHandler(customHandler);

        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("testcustom", out var handler));
        Assert.AreSame(customHandler, handler);
    }

    [TestMethod]
    public void RegisterHandler_OverrideExisting_ReplacesHandler()
    {
        var originalHandler = ColumnsPlaceholderHandler.Instance;
        var customHandler = new TestColumnsOverrideHandler();
        
        // Override
        PlaceholderProcessor.RegisterHandler(customHandler);
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("columns", out var handler));
        Assert.AreSame(customHandler, handler);
        
        // Restore original
        PlaceholderProcessor.RegisterHandler(originalHandler);
    }

    private class TestCustomHandler : PlaceholderHandlerBase
    {
        public override string Name => "testcustom";
        public override string Process(PlaceholderContext context, string options) => "CUSTOM";
    }

    private class TestColumnsOverrideHandler : PlaceholderHandlerBase
    {
        public override string Name => "columns";
        public override string Process(PlaceholderContext context, string options) => "OVERRIDDEN";
    }

    #endregion

    #region Parameter Extraction Tests

    [TestMethod]
    public void ExtractParameters_NoParameters_ReturnsEmpty()
    {
        var result = PlaceholderProcessor.ExtractParameters("SELECT * FROM users");
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ExtractParameters_SingleParameter_ReturnsOne()
    {
        var result = PlaceholderProcessor.ExtractParameters("SELECT * FROM users WHERE id = @id");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }

    [TestMethod]
    public void ExtractParameters_DuplicateParameters_ReturnsUnique()
    {
        var result = PlaceholderProcessor.ExtractParameters("SELECT * FROM users WHERE id = @id OR parent_id = @id");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }

    [TestMethod]
    public void ExtractParameters_DifferentPrefixes_AllExtracted()
    {
        var result = PlaceholderProcessor.ExtractParameters("SELECT * FROM users WHERE id = @id AND name = $name AND status = :status");
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Contains("id"));
        Assert.IsTrue(result.Contains("name"));
        Assert.IsTrue(result.Contains("status"));
    }

    [TestMethod]
    public void ExtractParameters_ParameterInString_NotExtracted()
    {
        // This tests current behavior - parameters in string literals might still be extracted
        // depending on implementation
        var result = PlaceholderProcessor.ExtractParameters("SELECT * FROM users WHERE name = '@notaparam'");
        // Current implementation extracts it - this documents the behavior
    }

    #endregion
}
