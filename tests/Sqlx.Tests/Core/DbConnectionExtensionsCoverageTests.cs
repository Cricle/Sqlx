using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

[TestClass]
public class DbConnectionExtensionsCoverageTests
{
    private static async Task<SqliteConnection> CreateTestConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE batch_test_entity (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            INSERT INTO batch_test_entity (id, name) VALUES
                (1, 'Alpha'),
                (2, 'Beta'),
                (3, 'Gamma');
            """;
        await createCmd.ExecuteNonQueryAsync();
        return connection;
    }

    [TestMethod]
    public async Task SqlxScalar_WithSnakeCaseAliasObjectParameters_ReturnsScalarValue()
    {
        await using var connection = await CreateTestConnectionAsync();

        var count = connection.SqlxScalar<long, BatchTestEntity>(
            "SELECT COUNT(*) FROM {{table}} WHERE id >= @min_id",
            SqlDefine.SQLite,
            new { MinId = 2 });

        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public async Task SqlxQueryAsync_WithSingleCharacterAlias_ResolvesCamelCaseParameter()
    {
        await using var connection = await CreateTestConnectionAsync();

        var rows = await connection.SqlxQueryAsync<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @x",
            SqlDefine.SQLite,
            new SingleCharacterParameter { X = 1 });

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Alpha", rows[0].Name);
    }

    [TestMethod]
    public async Task SqlxQuery_WithPrefixedAndEmptyDictionaryKeys_Works()
    {
        await using var connection = await CreateTestConnectionAsync();

        var result = connection.SqlxQuerySingle<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id AND name = @name",
            SqlDefine.SQLite,
            new Dictionary<string, object?>
            {
                ["@id"] = 2,
                ["name"] = "Beta",
                [""] = "ignored",
            });

        Assert.AreEqual(2, result.Id);
        Assert.AreEqual("Beta", result.Name);
    }

    [TestMethod]
    public async Task SqlxQuery_WithNullParameterObject_UsesNoParameterPath()
    {
        await using var connection = await CreateTestConnectionAsync();

        var rows = connection.SqlxQuery<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite,
            (object?)null);

        Assert.AreEqual(3, rows.Count);
    }

    [TestMethod]
    public async Task SqlxQuery_WithReadOnlyDictionaryParameters_UsesDirectDictionaryPath()
    {
        await using var connection = await CreateTestConnectionAsync();
        var parameters = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["id"] = 2 });

        var rows = connection.SqlxQuery<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            parameters);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Beta", rows[0].Name);
    }

    [TestMethod]
    public async Task SqlxScalar_WithLowercasePropertyName_UsesOriginalPropertyName()
    {
        await using var connection = await CreateTestConnectionAsync();

        var count = connection.SqlxScalar<long, BatchTestEntity>(
            "SELECT COUNT(*) FROM {{table}} WHERE id >= @min_id",
            SqlDefine.SQLite,
            new LowercaseParameter { minId = 2 });

        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefault_ReturnsNullWhenNoRowsMatch()
    {
        await using var connection = await CreateTestConnectionAsync();

        var result = connection.SqlxQueryFirstOrDefault<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 999 });

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefault_ReturnsEntityWhenRowExists()
    {
        await using var connection = await CreateTestConnectionAsync();

        var result = connection.SqlxQueryFirstOrDefault<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1 });

        Assert.IsNotNull(result);
        Assert.AreEqual("Alpha", result!.Name);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefaultAsync_WithoutParameters_ReturnsFirstRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var result = await connection.SqlxQueryFirstOrDefaultAsync<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result!.Id);
    }

    [TestMethod]
    public async Task SqlxQuerySingleAsync_WithNoRows_ThrowsInvalidOperationException()
    {
        await using var connection = await CreateTestConnectionAsync();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            connection.SqlxQuerySingleAsync<BatchTestEntity>(
                "SELECT {{columns}} FROM {{table}} WHERE id = @id",
                SqlDefine.SQLite,
                new { id = 999 }));
    }

    [TestMethod]
    public async Task SqlxQuerySingleAsync_WithMultipleRows_ThrowsInvalidOperationException()
    {
        await using var connection = await CreateTestConnectionAsync();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            connection.SqlxQuerySingleAsync<BatchTestEntity>(
                "SELECT {{columns}} FROM {{table}} ORDER BY id",
                SqlDefine.SQLite));
    }

    [TestMethod]
    public async Task SqlxQuerySingle_WithNoRows_ThrowsInvalidOperationException()
    {
        await using var connection = await CreateTestConnectionAsync();

        Assert.ThrowsException<InvalidOperationException>(() =>
            connection.SqlxQuerySingle<BatchTestEntity>(
                "SELECT {{columns}} FROM {{table}} WHERE id = @id",
                SqlDefine.SQLite,
                new { id = 999 }));
    }

    [TestMethod]
    public void HasParameterPrefix_SupportsQuestionMarkPrefix()
    {
        var method = typeof(DbConnectionExtensions).GetMethod(
            "HasParameterPrefix",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.IsNotNull(method);
        Assert.AreEqual(true, method!.Invoke(null, new object[] { "?id" }));
    }

    [TestMethod]
    public async Task SqlxExecute_Sync_WithTemplatePlaceholders_UpdatesRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = connection.SqlxExecute<BatchTestEntity>(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            SqlDefine.SQLite,
            new BatchTestEntity { Id = 1, Name = "UpdatedSync" });

        Assert.AreEqual(1, affected);

        using var verifyCommand = connection.CreateCommand();
        verifyCommand.CommandText = "SELECT name FROM batch_test_entity WHERE id = 1";
        Assert.AreEqual("UpdatedSync", verifyCommand.ExecuteScalar());
    }

    [TestMethod]
    public async Task SqlxExecute_WithoutParameterObject_UsesStaticTemplate()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = connection.SqlxExecute<BatchTestEntity>(
            "DELETE FROM {{table}} WHERE id = 3",
            SqlDefine.SQLite);

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task SqlxQuery_NoParams_ReturnsAllRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var rows = connection.SqlxQuery<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.AreEqual(3, rows.Count);
    }

    [TestMethod]
    public async Task SqlxQuery_TwoTypeParams_NoParams_ReturnsAllRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var rows = connection.SqlxQuery<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.AreEqual(3, rows.Count);
    }

    [TestMethod]
    public async Task SqlxQueryAsync_NoParams_ReturnsAllRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var rows = await connection.SqlxQueryAsync<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.AreEqual(3, rows.Count);
    }

    [TestMethod]
    public async Task SqlxQueryAsync_TwoTypeParams_NoParams_ReturnsAllRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var rows = await connection.SqlxQueryAsync<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.AreEqual(3, rows.Count);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefault_NoParams_ReturnsFirstRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = connection.SqlxQueryFirstOrDefault<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.IsNotNull(row);
        Assert.AreEqual(1, row!.Id);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefault_TwoTypeParams_NoParams_ReturnsFirstRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = connection.SqlxQueryFirstOrDefault<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.IsNotNull(row);
        Assert.AreEqual(1, row!.Id);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefaultAsync_TwoTypeParams_NoParams_ReturnsFirstRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = await connection.SqlxQueryFirstOrDefaultAsync<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} ORDER BY id",
            SqlDefine.SQLite);

        Assert.IsNotNull(row);
        Assert.AreEqual(1, row!.Id);
    }

    [TestMethod]
    public async Task SqlxQueryFirstOrDefaultAsync_TwoTypeParams_WithParams_ReturnsRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = await connection.SqlxQueryFirstOrDefaultAsync<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 2 });

        Assert.IsNotNull(row);
        Assert.AreEqual("Beta", row!.Name);
    }

    [TestMethod]
    public async Task SqlxQuerySingle_NoParams_ReturnsSingleRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        // Use a query that returns exactly one row
        var row = connection.SqlxQuerySingle<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = 1",
            SqlDefine.SQLite);

        Assert.AreEqual(1, row.Id);
        Assert.AreEqual("Alpha", row.Name);
    }

    [TestMethod]
    public async Task SqlxQuerySingle_TwoTypeParams_NoParams_ReturnsSingleRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = connection.SqlxQuerySingle<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = 2",
            SqlDefine.SQLite);

        Assert.AreEqual(2, row.Id);
    }

    [TestMethod]
    public async Task SqlxQuerySingleAsync_NoParams_ReturnsSingleRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = await connection.SqlxQuerySingleAsync<BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = 3",
            SqlDefine.SQLite);

        Assert.AreEqual(3, row.Id);
        Assert.AreEqual("Gamma", row.Name);
    }

    [TestMethod]
    public async Task SqlxQuerySingleAsync_TwoTypeParams_NoParams_ReturnsSingleRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = await connection.SqlxQuerySingleAsync<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = 1",
            SqlDefine.SQLite);

        Assert.AreEqual(1, row.Id);
    }

    [TestMethod]
    public async Task SqlxQuerySingleAsync_TwoTypeParams_WithParams_ReturnsSingleRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var row = await connection.SqlxQuerySingleAsync<BatchTestEntity, BatchTestEntity>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 2 });

        Assert.AreEqual(2, row.Id);
    }

    [TestMethod]
    public async Task SqlxScalar_NoParams_ReturnsCount()
    {
        await using var connection = await CreateTestConnectionAsync();

        var count = connection.SqlxScalar<long, BatchTestEntity>(
            "SELECT COUNT(*) FROM {{table}}",
            SqlDefine.SQLite);

        Assert.AreEqual(3L, count);
    }

    [TestMethod]
    public async Task SqlxScalarAsync_NoParams_ReturnsCount()
    {
        await using var connection = await CreateTestConnectionAsync();

        var count = await connection.SqlxScalarAsync<long, BatchTestEntity>(
            "SELECT COUNT(*) FROM {{table}}",
            SqlDefine.SQLite);

        Assert.AreEqual(3L, count);
    }

    [TestMethod]
    public async Task SqlxExecuteAsync_NoParams_DeletesRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = await connection.SqlxExecuteAsync<BatchTestEntity>(
            "DELETE FROM {{table}} WHERE id = 1",
            SqlDefine.SQLite);

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task SqlxExecute_TwoTypeParams_WithParams_UpdatesRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = connection.SqlxExecute<BatchTestEntity>(
            "UPDATE {{table}} SET name = @name WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 1, name = "AlphaUpdated" });

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task SqlxExecuteAsync_TwoTypeParams_WithParams_UpdatesRow()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = await connection.SqlxExecuteAsync<BatchTestEntity>(
            "UPDATE {{table}} SET name = @name WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 2, name = "BetaUpdated" });

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task SqlxExecuteAsync_TwoTypeParams_NoParams_DeletesRows()
    {
        await using var connection = await CreateTestConnectionAsync();

        var affected = await connection.SqlxExecuteAsync<BatchTestEntity>(
            "DELETE FROM {{table}} WHERE id = 3",
            SqlDefine.SQLite);

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task SqlxQuerySingle_WithUnsupportedScalarType_ThrowsInvalidOperationException()
    {
        await using var connection = await CreateTestConnectionAsync();

        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
            connection.SqlxQuerySingle<int>(
                "SELECT id FROM {{table}} WHERE id = 1",
                SqlDefine.SQLite));

        StringAssert.Contains(ex.Message, "No result reader is available");
    }

    private sealed class SingleCharacterParameter
    {
        public int X { get; set; }
    }

    private sealed class LowercaseParameter
    {
        public int minId { get; set; }
    }
}
