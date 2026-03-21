using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Data.Sqlite;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks the hot paths used by DynamicResultReader when consuming cached ordinals.
/// This isolates the difference between:
/// 1. the extension-method fast path that reuses a pooled ordinal array once per query
/// 2. the span-based fallback path that must materialize ordinals for each row
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DynamicResultReaderOrdinalBenchmark
{
    private const string SelectProjectionSql = """
        SELECT id, name, email, age
        FROM users
        WHERE age >= @minAge
        ORDER BY id
        LIMIT @limit
        """;

    private static readonly string[] ProjectionColumns = { "id", "name", "email", "age" };

    private SqliteConnection _connection = null!;
    private DynamicResultReader<ProjectionRow> _reader = null!;

    [Params(100, 1000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _connection = DatabaseSetup.CreateConnection();
        DatabaseSetup.InitializeDatabase(_connection);
        DatabaseSetup.SeedData(_connection, 10_000);
        _reader = new DynamicResultReader<ProjectionRow>(ProjectionColumns);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "DynamicReader: ToList fast path")]
    public List<ProjectionRow> DynamicReader_ToList_FastPath()
    {
        using var command = CreateCommand();
        using var dataReader = command.ExecuteReader();
        return _reader.ToList(dataReader);
    }

    [Benchmark(Description = "DynamicReader: manual span loop")]
    public List<ProjectionRow> DynamicReader_ManualSpanLoop()
    {
        using var command = CreateCommand();
        using var dataReader = command.ExecuteReader();

        Span<int> ordinals = stackalloc int[_reader.PropertyCount];
        _reader.GetOrdinals(dataReader, ordinals);

        var results = new List<ProjectionRow>(RowCount);
        while (dataReader.Read())
        {
            results.Add(_reader.Read(dataReader, ordinals));
        }

        return results;
    }

    [Benchmark(Description = "DynamicReader: manual pooled array loop")]
    public List<ProjectionRow> DynamicReader_ManualArrayLoop()
    {
        using var command = CreateCommand();
        using var dataReader = command.ExecuteReader();

        var arrayReader = (IArrayOrdinalReader<ProjectionRow>)_reader;
        var ordinals = ArrayPool<int>.Shared.Rent(_reader.PropertyCount);
        try
        {
            _reader.GetOrdinals(dataReader, ordinals.AsSpan(0, _reader.PropertyCount));

            var results = new List<ProjectionRow>(RowCount);
            while (dataReader.Read())
            {
                results.Add(arrayReader.Read(dataReader, ordinals));
            }

            return results;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(ordinals);
        }
    }

    [Benchmark(Description = "DynamicReader: ToListAsync fast path")]
    public async Task<List<ProjectionRow>> DynamicReader_ToListAsync_FastPath()
    {
        await using var command = CreateCommand();
        await using var dataReader = await command.ExecuteReaderAsync();
        return await _reader.ToListAsync(dataReader);
    }

    [Benchmark(Description = "DynamicReader: manual span loop async")]
    public async Task<List<ProjectionRow>> DynamicReader_ManualSpanLoopAsync()
    {
        await using var command = CreateCommand();
        await using var dataReader = await command.ExecuteReaderAsync();

        var ordinals = GC.AllocateUninitializedArray<int>(_reader.PropertyCount);
        _reader.GetOrdinals(dataReader, ordinals);

        var results = new List<ProjectionRow>(RowCount);
        while (await dataReader.ReadAsync())
        {
            results.Add(_reader.Read(dataReader, ordinals));
        }

        return results;
    }

    private SqliteCommand CreateCommand()
    {
        var command = _connection.CreateCommand();
        command.CommandText = SelectProjectionSql;

        var minAge = command.CreateParameter();
        minAge.ParameterName = "@minAge";
        minAge.Value = 25;
        command.Parameters.Add(minAge);

        var limit = command.CreateParameter();
        limit.ParameterName = "@limit";
        limit.Value = RowCount;
        command.Parameters.Add(limit);

        return command;
    }

    public sealed class ProjectionRow
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Age { get; set; }
    }
}
