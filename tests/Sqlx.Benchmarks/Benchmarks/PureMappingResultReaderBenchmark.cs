using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Pure in-memory mapping benchmark that isolates ResultReader overhead from SQL execution.
/// This is useful when validating improvements to cached ordinals and DynamicResultReader
/// without SQLite command execution noise dominating the measurement.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PureMappingResultReaderBenchmark
{
    private BenchmarkUser[] _rows = null!;
    private InMemoryBenchmarkUserDataReader _dataReader = null!;
    private DynamicResultReader<BenchmarkUser> _dynamicReader = null!;
    private BenchmarkUserResultReader _generatedReader = null!;
    private string[] _columnNames = null!;

    [Params(100, 1000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = new BenchmarkUser[RowCount];
        for (var i = 0; i < RowCount; i++)
        {
            _rows[i] = new BenchmarkUser
            {
                Id = i + 1,
                Name = $"User{i:D6}",
                Email = i % 5 == 0 ? string.Empty : $"user{i}@example.com",
                Age = 18 + (i % 50),
                IsActive = i % 2 == 0,
                CreatedAt = new DateTime(2024, 1, 1).AddDays(i),
                UpdatedAt = i % 3 == 0 ? null : new DateTime(2024, 6, 1).AddDays(i),
                Balance = 1000m + i,
                Description = i % 4 == 0 ? null : $"Description {i}",
                Score = i * 10,
            };
        }

        _dataReader = new InMemoryBenchmarkUserDataReader(_rows);
        _generatedReader = BenchmarkUserResultReader.Default;
        _columnNames = BenchmarkUserEntityProvider.Default.Columns.Select(column => column.Name).ToArray();
        _dynamicReader = new DynamicResultReader<BenchmarkUser>(_columnNames);
    }

    [Benchmark(Baseline = true, Description = "PureMap: generated cached ordinals")]
    public long GeneratedReader_CachedOrdinals()
    {
        _dataReader.Reset();
        Span<int> ordinals = stackalloc int[_generatedReader.PropertyCount];
        _generatedReader.GetOrdinals(_dataReader, ordinals);

        long checksum = 0;
        while (_dataReader.Read())
        {
            var entity = _generatedReader.Read(_dataReader, ordinals);
            checksum += entity.Id + entity.Age + entity.Score;
        }

        return checksum;
    }

    [Benchmark(Description = "PureMap: generated uncached ordinals")]
    public long GeneratedReader_UncachedOrdinals()
    {
        _dataReader.Reset();

        long checksum = 0;
        while (_dataReader.Read())
        {
            var entity = _generatedReader.Read(_dataReader);
            checksum += entity.Id + entity.Age + entity.Score;
        }

        return checksum;
    }

    [Benchmark(Description = "PureMap: dynamic array fast path")]
    public long DynamicReader_ArrayFastPath()
    {
        _dataReader.Reset();

        var arrayReader = (IArrayOrdinalReader<BenchmarkUser>)_dynamicReader;
        var ordinals = ArrayPool<int>.Shared.Rent(_dynamicReader.PropertyCount);
        try
        {
            _dynamicReader.GetOrdinals(_dataReader, ordinals.AsSpan(0, _dynamicReader.PropertyCount));

            long checksum = 0;
            while (_dataReader.Read())
            {
                var entity = arrayReader.Read(_dataReader, ordinals);
                checksum += entity.Id + entity.Age + entity.Score;
            }

            return checksum;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(ordinals);
        }
    }

    [Benchmark(Description = "PureMap: dynamic span fallback")]
    public long DynamicReader_SpanFallback()
    {
        _dataReader.Reset();

        Span<int> ordinals = stackalloc int[_dynamicReader.PropertyCount];
        _dynamicReader.GetOrdinals(_dataReader, ordinals);

        long checksum = 0;
        while (_dataReader.Read())
        {
            var entity = _dynamicReader.Read(_dataReader, ordinals);
            checksum += entity.Id + entity.Age + entity.Score;
        }

        return checksum;
    }

    private sealed class InMemoryBenchmarkUserDataReader : DbDataReader
    {
        private static readonly Dictionary<string, int> Ordinals = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = 0,
            ["name"] = 1,
            ["email"] = 2,
            ["age"] = 3,
            ["is_active"] = 4,
            ["created_at"] = 5,
            ["updated_at"] = 6,
            ["balance"] = 7,
            ["description"] = 8,
            ["score"] = 9,
        };

        private readonly BenchmarkUser[] _rows;
        private int _index = -1;

        public InMemoryBenchmarkUserDataReader(BenchmarkUser[] rows)
        {
            _rows = rows;
        }

        public void Reset()
        {
            _index = -1;
        }

        private BenchmarkUser Current => _rows[_index];

        public override bool Read()
        {
            _index++;
            return _index < _rows.Length;
        }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Read());
        }

        public override int GetOrdinal(string name) => Ordinals[name];

        public override bool GetBoolean(int ordinal) => ordinal switch
        {
            4 => Current.IsActive,
            _ => throw new InvalidOperationException(),
        };

        public override DateTime GetDateTime(int ordinal) => ordinal switch
        {
            5 => Current.CreatedAt,
            6 => Current.UpdatedAt ?? default,
            _ => throw new InvalidOperationException(),
        };

        public override decimal GetDecimal(int ordinal) => ordinal switch
        {
            7 => Current.Balance,
            _ => throw new InvalidOperationException(),
        };

        public override int GetInt32(int ordinal) => ordinal switch
        {
            3 => Current.Age,
            9 => Current.Score,
            _ => throw new InvalidOperationException(),
        };

        public override long GetInt64(int ordinal) => ordinal switch
        {
            0 => Current.Id,
            _ => throw new InvalidOperationException(),
        };

        public override string GetString(int ordinal) => ordinal switch
        {
            1 => Current.Name,
            2 => Current.Email,
            8 => Current.Description ?? string.Empty,
            _ => throw new InvalidOperationException(),
        };

        public override bool IsDBNull(int ordinal) => ordinal switch
        {
            6 => Current.UpdatedAt is null,
            8 => Current.Description is null,
            _ => false,
        };

        public override string GetName(int ordinal) => ordinal switch
        {
            0 => "id",
            1 => "name",
            2 => "email",
            3 => "age",
            4 => "is_active",
            5 => "created_at",
            6 => "updated_at",
            7 => "balance",
            8 => "description",
            9 => "score",
            _ => throw new IndexOutOfRangeException(),
        };

        public override object GetValue(int ordinal) => ordinal switch
        {
            0 => Current.Id,
            1 => Current.Name,
            2 => Current.Email,
            3 => Current.Age,
            4 => Current.IsActive,
            5 => Current.CreatedAt,
            6 => Current.UpdatedAt is null ? DBNull.Value : (object)Current.UpdatedAt.Value,
            7 => Current.Balance,
            8 => Current.Description is null ? DBNull.Value : Current.Description,
            9 => Current.Score,
            _ => throw new IndexOutOfRangeException(),
        };

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }

            return count;
        }

        [return: DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int ordinal) => ordinal switch
        {
            0 => typeof(long),
            1 => typeof(string),
            2 => typeof(string),
            3 => typeof(int),
            4 => typeof(bool),
            5 => typeof(DateTime),
            6 => typeof(DateTime),
            7 => typeof(decimal),
            8 => typeof(string),
            9 => typeof(int),
            _ => throw new IndexOutOfRangeException(),
        };

        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override int FieldCount => 10;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override int Depth => 0;
        public override bool HasRows => _rows.Length > 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override bool NextResult() => false;

        public override byte GetByte(int ordinal) => throw new NotImplementedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override char GetChar(int ordinal) => throw new NotImplementedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override double GetDouble(int ordinal) => throw new NotImplementedException();
        public override float GetFloat(int ordinal) => throw new NotImplementedException();
        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
        public override short GetInt16(int ordinal) => throw new NotImplementedException();
        public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
}
