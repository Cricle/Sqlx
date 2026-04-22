using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Placeholders;

[TestClass]
public class PlaceholderProcessorCoverageTests
{
    private static PlaceholderContext CreateContext()
    {
        return new PlaceholderContext(
            SqlDefine.SQLite,
            "users",
            new[] { new ColumnMeta("id", "Id", DbType.Int64, false) });
    }

    [TestMethod]
    public void RegisterHandler_BlockHandler_RegistersClosingTag()
    {
        var handler = new CoverageBlockHandler();

        PlaceholderProcessor.RegisterHandler(handler);

        Assert.IsTrue(PlaceholderProcessor.TryGetHandler(handler.Name, out var registered));
        Assert.AreSame(handler, registered);
        Assert.IsTrue(PlaceholderProcessor.IsBlockClosingTag(handler.ClosingTagName));
    }

    [TestMethod]
    public void ExtractParameters_IgnoresQuotedIdentifiersAndComments()
    {
        var sql = """
            SELECT "@double", `@backtick`, [@bracket], '@single''quote'
            FROM users -- @line
            WHERE id = @id /* @block */
            """;

        var result = PlaceholderProcessor.ExtractParameters(sql);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }

    [TestMethod]
    public void ExtractParameters_DoesNotStartInsideIdentifiers()
    {
        var result = PlaceholderProcessor.ExtractParameters("SELECT foo@bar, foo_@baz, @real FROM users");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("real", result[0]);
    }

    [TestMethod]
    public void SqlTemplate_Render_WithCustomBlockHandler_RendersBlockContent()
    {
        PlaceholderProcessor.RegisterHandler(new CoverageBlockHandler());
        var template = SqlTemplate.Prepare("SELECT {{coverblock}}[id]{{/coverend}} FROM {{table}}", CreateContext());

        var sql = template.Render(new Dictionary<string, object?>());

        Assert.AreEqual("SELECT [id] FROM [users]", sql);
    }

    [TestMethod]
    public void SqlTemplate_Render_WithUnmatchedBlockOpening_ThrowsInvalidOperationException()
    {
        PlaceholderProcessor.RegisterHandler(new CoverageBlockHandler());
        var template = SqlTemplate.Prepare("SELECT {{coverblock}}[id]", CreateContext());

        Assert.ThrowsException<InvalidOperationException>(() => template.Render(new Dictionary<string, object?>()));
    }

    [TestMethod]
    public void SqlTemplate_Render_WithUnmatchedBlockClosing_ThrowsInvalidOperationException()
    {
        var template = SqlTemplate.Prepare("SELECT {{/if}}", CreateContext());

        Assert.ThrowsException<InvalidOperationException>(() => template.Render(new Dictionary<string, object?>()));
    }

    private sealed class CoverageBlockHandler : IBlockPlaceholderHandler
    {
        public string Name => "coverblock";

        public string ClosingTagName => "/coverend";

        public PlaceholderType GetType(string options) => PlaceholderType.Dynamic;

        public string Process(PlaceholderContext context, string options) => string.Empty;

        public string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters) => string.Empty;

        public string ProcessBlock(string options, string blockContent, IReadOnlyDictionary<string, object?>? parameters) => blockContent;
    }

    // Line 126-127: empty SQL
    [TestMethod]
    public void ExtractParameters_EmptySql_ReturnsEmpty()
    {
        var result = PlaceholderProcessor.ExtractParameters(string.Empty);
        Assert.AreEqual(0, result.Count);
    }

    // Line 149-150: parameter prefix at end of string
    [TestMethod]
    public void ExtractParameters_ParameterPrefixAtEnd_IgnoresIt()
    {
        var result = PlaceholderProcessor.ExtractParameters("SELECT @");
        Assert.AreEqual(0, result.Count);
    }

    // Lines 201-203: escaped double-quote in double-quoted identifier
    [TestMethod]
    public void ExtractParameters_EscapedDoubleQuoteInIdentifier_IgnoresParam()
    {
        // ""@param"" - escaped double-quote inside double-quoted identifier
        var result = PlaceholderProcessor.ExtractParameters("SELECT \"col\"\"@param\" FROM t WHERE id = @id");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }

    // Lines 215-217: escaped backtick in backtick identifier
    [TestMethod]
    public void ExtractParameters_EscapedBacktickInIdentifier_IgnoresParam()
    {
        // ``@param`` - escaped backtick inside backtick identifier
        var result = PlaceholderProcessor.ExtractParameters("SELECT `col``@param` FROM t WHERE id = @id");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }

    // Lines 229-231: escaped bracket in bracket identifier
    [TestMethod]
    public void ExtractParameters_EscapedBracketInIdentifier_IgnoresParam()
    {
        // [col]]@param] - escaped bracket inside bracket identifier
        var result = PlaceholderProcessor.ExtractParameters("SELECT [col]]@param] FROM t WHERE id = @id");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }

    // Lines 336-337: parameter at start of string
    [TestMethod]
    public void ExtractParameters_ParameterAtStart_ExtractsIt()
    {
        var result = PlaceholderProcessor.ExtractParameters("@id = 1");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("id", result[0]);
    }
}
