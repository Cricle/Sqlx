using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Microbenchmarks for TypeConverter hot paths.
/// These benchmarks focus on the conversion layer itself and compare representative
/// TypeConverter scenarios with direct BCL alternatives where meaningful.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TypeConverterBenchmark
{
    private const int InnerLoopCount = 256;

    private string _decimalText = null!;
    private string _dateTimeOffsetText = null!;
    private string _dateOnlyText = null!;
    private string _timeOnlyText = null!;
    private long _timeSpanTicks;
    private byte[] _guidBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _decimalText = "12345.67";
        _dateTimeOffsetText = "2024-02-20T14:30:00+08:00";
        _dateOnlyText = "2024-02-20";
        _timeOnlyText = "14:30:15";
        _timeSpanTicks = TimeSpan.FromMinutes(15).Ticks;
        _guidBytes = Guid.Parse("12345678-1234-1234-1234-123456789abc").ToByteArray();
    }

    [Benchmark(Baseline = true, Description = "TypeConverter: Int32 same type", OperationsPerInvoke = InnerLoopCount)]
    public int TypeConverter_Int32_SameType()
    {
        var sum = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            sum += TypeConverter.Convert<int>(42 + (i & 1));
        }

        return sum;
    }

    [Benchmark(Description = "TypeConverter: string -> decimal", OperationsPerInvoke = InnerLoopCount)]
    public decimal TypeConverter_StringToDecimal()
    {
        decimal sum = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            sum += TypeConverter.Convert<decimal>(_decimalText);
        }

        return sum;
    }

    [Benchmark(Description = "BCL: decimal.Parse invariant", OperationsPerInvoke = InnerLoopCount)]
    public decimal Bcl_DecimalParseInvariant()
    {
        decimal sum = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            sum += decimal.Parse(_decimalText, CultureInfo.InvariantCulture);
        }

        return sum;
    }

    [Benchmark(Description = "TypeConverter: string -> DateTimeOffset", OperationsPerInvoke = InnerLoopCount)]
    public long TypeConverter_StringToDateTimeOffset()
    {
        long ticks = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ticks += TypeConverter.Convert<DateTimeOffset>(_dateTimeOffsetText).Ticks;
        }

        return ticks;
    }

    [Benchmark(Description = "BCL: DateTimeOffset.Parse invariant", OperationsPerInvoke = InnerLoopCount)]
    public long Bcl_DateTimeOffsetParseInvariant()
    {
        long ticks = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ticks += DateTimeOffset.Parse(_dateTimeOffsetText, CultureInfo.InvariantCulture).Ticks;
        }

        return ticks;
    }

    [Benchmark(Description = "TypeConverter: string -> DateOnly", OperationsPerInvoke = InnerLoopCount)]
    public int TypeConverter_StringToDateOnly()
    {
        var day = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            day += TypeConverter.Convert<DateOnly>(_dateOnlyText).DayNumber;
        }

        return day;
    }

    [Benchmark(Description = "BCL: DateOnly.Parse invariant", OperationsPerInvoke = InnerLoopCount)]
    public int Bcl_DateOnlyParseInvariant()
    {
        var day = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            day += DateOnly.Parse(_dateOnlyText, CultureInfo.InvariantCulture).DayNumber;
        }

        return day;
    }

    [Benchmark(Description = "TypeConverter: ticks -> TimeSpan", OperationsPerInvoke = InnerLoopCount)]
    public long TypeConverter_TicksToTimeSpan()
    {
        long ticks = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ticks += TypeConverter.Convert<TimeSpan>(_timeSpanTicks + i).Ticks;
        }

        return ticks;
    }

    [Benchmark(Description = "TypeConverter: string -> TimeOnly", OperationsPerInvoke = InnerLoopCount)]
    public long TypeConverter_StringToTimeOnly()
    {
        long ticks = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ticks += TypeConverter.Convert<TimeOnly>(_timeOnlyText).Ticks;
        }

        return ticks;
    }

    [Benchmark(Description = "BCL: TimeOnly.Parse invariant", OperationsPerInvoke = InnerLoopCount)]
    public long Bcl_TimeOnlyParseInvariant()
    {
        long ticks = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ticks += TimeOnly.Parse(_timeOnlyText, CultureInfo.InvariantCulture).Ticks;
        }

        return ticks;
    }

    [Benchmark(Description = "BCL: TimeSpan.FromTicks", OperationsPerInvoke = InnerLoopCount)]
    public long Bcl_TimeSpanFromTicks()
    {
        long ticks = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ticks += TimeSpan.FromTicks(_timeSpanTicks + i).Ticks;
        }

        return ticks;
    }

    [Benchmark(Description = "TypeConverter: byte[] -> Guid", OperationsPerInvoke = InnerLoopCount)]
    public long TypeConverter_ByteArrayToGuid()
    {
        long sum = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            sum += TypeConverter.Convert<Guid>(_guidBytes).GetHashCode();
        }

        return sum;
    }

    [Benchmark(Description = "BCL: new Guid(byte[])", OperationsPerInvoke = InnerLoopCount)]
    public long Bcl_NewGuidFromBytes()
    {
        long sum = 0;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            sum += new Guid(_guidBytes).GetHashCode();
        }

        return sum;
    }
}
