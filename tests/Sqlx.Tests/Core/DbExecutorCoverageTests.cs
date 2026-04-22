using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

[TestClass]
public class DbExecutorCoverageTests
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        using var createCmd = _connection.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE test_entity (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            INSERT INTO test_entity (id, name) VALUES (1, 'Test1'), (2, 'Test2'), (3, 'Test3');
            """;
        await createCmd.ExecuteNonQueryAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public void ExecuteScalar_WithEnumerableParameters_ReturnsValue()
    {
        var result = DbExecutor.ExecuteScalar(
            _connection!,
            "SELECT name FROM test_entity WHERE id = @id",
            new EnumerableParameters(new KeyValuePair<string, object?>("@id", 1)));

        Assert.AreEqual("Test1", result);
    }

    [TestMethod]
    public async Task ExecuteScalarAsync_WithEnumerableParameters_ReturnsValue()
    {
        var result = await DbExecutor.ExecuteScalarAsync(
            _connection!,
            "SELECT COUNT(*) FROM test_entity WHERE id >= @minId",
            new EnumerableParameters(new KeyValuePair<string, object?>("@minId", 2)));

        Assert.AreEqual(2L, (long)result!);
    }

    [TestMethod]
    public void ExecuteScalar_WithReadOnlyDictionaryParameters_ReturnsValue()
    {
        var parameters = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["@id"] = 2 });

        var result = DbExecutor.ExecuteScalar(
            _connection!,
            "SELECT name FROM test_entity WHERE id = @id",
            parameters);

        Assert.AreEqual("Test2", result);
    }

    [TestMethod]
    public void ExecuteReader_WithEnumerableParameters_ReturnsResults()
    {
        var results = DbExecutor.ExecuteReader(
                _connection!,
                "SELECT id, name FROM test_entity WHERE id >= @minId ORDER BY id",
                new EnumerableParameters(new KeyValuePair<string, object?>("@minId", 2)),
                ExecutorTestEntityResultReader.Default)
            .ToList();

        CollectionAssert.AreEqual(new[] { 2, 3 }, results.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public void ExecuteReader_WithReadOnlyDictionaryParameters_ReturnsResults()
    {
        var parameters = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["@minId"] = 2 });

        var results = DbExecutor.ExecuteReader(
                _connection!,
                "SELECT id, name FROM test_entity WHERE id >= @minId ORDER BY id",
                parameters,
                ExecutorTestEntityResultReader.Default)
            .ToList();

        CollectionAssert.AreEqual(new[] { 2, 3 }, results.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task ExecuteReaderAsync_WithEnumerableParameters_ReturnsResults()
    {
        var results = await ReadAllAsync(DbExecutor.ExecuteReaderAsync(
            _connection!,
            "SELECT id, name FROM test_entity WHERE id <= @maxId ORDER BY id",
            new EnumerableParameters(new KeyValuePair<string, object?>("@maxId", 2)),
            ExecutorTestEntityResultReader.Default));

        CollectionAssert.AreEqual(new[] { 1, 2 }, results.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public void ExecuteNonQuery_WithEnumerableParameters_UpdatesRows()
    {
        var affected = DbExecutor.ExecuteNonQuery(
            _connection!,
            "UPDATE test_entity SET name = @name WHERE id = @id",
            new EnumerableParameters(
                new KeyValuePair<string, object?>("@id", 2),
                new KeyValuePair<string, object?>("@name", "Renamed")));

        Assert.AreEqual(1, affected);

        using var verifyCommand = _connection!.CreateCommand();
        verifyCommand.CommandText = "SELECT name FROM test_entity WHERE id = 2";
        Assert.AreEqual("Renamed", verifyCommand.ExecuteScalar());
    }

    [TestMethod]
    public async Task ExecuteNonQueryAsync_WithEnumerableParameters_UpdatesRows()
    {
        var affected = await DbExecutor.ExecuteNonQueryAsync(
            _connection!,
            "UPDATE test_entity SET name = @name WHERE id = @id",
            new EnumerableParameters(
                new KeyValuePair<string, object?>("@id", 3),
                new KeyValuePair<string, object?>("@name", "AsyncRenamed")));

        Assert.AreEqual(1, affected);

        using var verifyCommand = _connection!.CreateCommand();
        verifyCommand.CommandText = "SELECT name FROM test_entity WHERE id = 3";
        Assert.AreEqual("AsyncRenamed", await verifyCommand.ExecuteScalarAsync());
    }

    [TestMethod]
    public void ExecuteFirstOrDefault_WithResult_ReturnsEntity()
    {
        var result = DbExecutor.ExecuteFirstOrDefault(
            _connection!,
            "SELECT id, name FROM test_entity WHERE id = @id",
            new Dictionary<string, object?> { ["@id"] = 1 },
            ExecutorTestEntityResultReader.Default);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result!.Id);
    }

    [TestMethod]
    public void ExecuteFirstOrDefault_EmptyResult_ReturnsDefault()
    {
        var result = DbExecutor.ExecuteFirstOrDefault(
            _connection!,
            "SELECT id, name FROM test_entity WHERE id = @id",
            new Dictionary<string, object?> { ["@id"] = 999 },
            ExecutorTestEntityResultReader.Default);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExecuteFirst_WithResult_ReturnsEntity()
    {
        var result = DbExecutor.ExecuteFirst(
            _connection!,
            "SELECT id, name FROM test_entity WHERE id = @id",
            new Dictionary<string, object?> { ["@id"] = 3 },
            ExecutorTestEntityResultReader.Default);

        Assert.AreEqual(3, result.Id);
    }

    [TestMethod]
    public void ExecuteFirst_EmptyResult_ThrowsInvalidOperationException()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            DbExecutor.ExecuteFirst(
                _connection!,
                "SELECT id, name FROM test_entity WHERE id = @id",
                new Dictionary<string, object?> { ["@id"] = 999 },
                ExecutorTestEntityResultReader.Default));
    }

    private static async Task<List<ExecutorTestEntity>> ReadAllAsync(IAsyncEnumerator<ExecutorTestEntity> enumerator)
    {
        var results = new List<ExecutorTestEntity>();
        await using (enumerator)
        {
            while (await enumerator.MoveNextAsync())
            {
                results.Add(enumerator.Current);
            }
        }

        return results;
    }

    private sealed class EnumerableParameters : IEnumerable<KeyValuePair<string, object?>>
    {
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _values;

        public EnumerableParameters(params KeyValuePair<string, object?>[] values)
        {
            _values = values;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Cover AddParameters(IEnumerable) -> IReadOnlyDictionary fast path (lines 493-495)
    [TestMethod]
    public void ExecuteScalar_WithReadOnlyDictionaryAsEnumerable_ReturnsValue()
    {
        // ReadOnlyDictionary implements both IEnumerable<KVP> and IReadOnlyDictionary
        IEnumerable<KeyValuePair<string, object?>> parameters =
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?> { ["@id"] = 1 });

        var result = DbExecutor.ExecuteScalar(
            _connection!,
            "SELECT name FROM test_entity WHERE id = @id",
            parameters);

        Assert.AreEqual("Test1", result);
    }

    // Cover ExecuteScalarAsync(IReadOnlyDictionary) line 272
    [TestMethod]
    public async Task ExecuteScalarAsync_WithReadOnlyDictionary_ReturnsValue()
    {
        var parameters = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["@id"] = 2 });

        var result = await DbExecutor.ExecuteScalarAsync(
            _connection!,
            "SELECT name FROM test_entity WHERE id = @id",
            parameters);

        Assert.AreEqual("Test2", result);
    }

    // Cover ExecuteNonQueryAsync(IReadOnlyDictionary) line 367
    [TestMethod]
    public async Task ExecuteNonQueryAsync_WithReadOnlyDictionary_UpdatesRows()
    {
        var parameters = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["@id"] = 1, ["@name"] = "Updated" });

        var affected = await DbExecutor.ExecuteNonQueryAsync(
            _connection!,
            "UPDATE test_entity SET name = @name WHERE id = @id",
            parameters);

        Assert.AreEqual(1, affected);
    }

    // Cover CreateCommand(IEnumerable) with transaction (lines 456-458)
    [TestMethod]
    public void ExecuteScalar_WithEnumerableParametersAndTransaction_ReturnsValue()
    {
        using var tx = _connection!.BeginTransaction();
        IEnumerable<KeyValuePair<string, object?>> parameters =
            new EnumerableParameters(new KeyValuePair<string, object?>("@id", 3));

        var result = DbExecutor.ExecuteScalar(
            _connection!,
            "SELECT name FROM test_entity WHERE id = @id",
            parameters,
            tx);

        tx.Rollback();
        Assert.AreEqual("Test3", result);
    }

    // Cover ExecuteNonQueryAsync(IEnumerable) line 387
    [TestMethod]
    public async Task ExecuteNonQueryAsync_WithEnumerableAndTransaction_UpdatesRows()
    {
        using var tx = _connection!.BeginTransaction();
        var affected = await DbExecutor.ExecuteNonQueryAsync(
            _connection!,
            "UPDATE test_entity SET name = @name WHERE id = @id",
            new EnumerableParameters(
                new KeyValuePair<string, object?>("@id", 2),
                new KeyValuePair<string, object?>("@name", "TxUpdated")),
            tx);
        tx.Rollback();

        Assert.AreEqual(1, affected);
    }
}
