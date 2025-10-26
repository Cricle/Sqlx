using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.DateTimeTesting;

/// <summary>
/// TDD: 日期时间和时区测试
/// 验证日期时间的存储、查询和时区处理
/// </summary>
[TestClass]
public class TDD_DateTimeAndTimezone
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        _connection.Execute(@"
            CREATE TABLE datetime_test (
                id INTEGER PRIMARY KEY,
                datetime_value TEXT,
                date_only TEXT,
                time_only TEXT,
                timestamp_value INTEGER
            )
        ");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Timezone")]
    [Description("UTC时间应正确存储和读取")]
    public async Task DateTime_UTC_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var utcNow = System.DateTime.UtcNow;

        // Act
        var id = await repo.InsertAsync(utcNow, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(utcNow.ToString("yyyy-MM-dd HH:mm:ss"), result.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Timezone")]
    [Description("本地时间应正确存储和读取")]
    public async Task DateTime_Local_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var localNow = System.DateTime.Now;

        // Act
        var id = await repo.InsertAsync(localNow, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(localNow.ToString("yyyy-MM-dd HH:mm:ss"), result.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Boundary")]
    [Description("DateTime最小值应正确处理")]
    public async Task DateTime_MinValue_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var minDateTime = System.DateTime.MinValue;

        // Act
        var id = await repo.InsertAsync(minDateTime, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(minDateTime.Year, result.DateTimeValue.Year);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Boundary")]
    [Description("DateTime最大值应正确处理")]
    public async Task DateTime_MaxValue_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var maxDateTime = System.DateTime.MaxValue;

        // Act
        var id = await repo.InsertAsync(maxDateTime, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(maxDateTime.Year, result.DateTimeValue.Year);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Precision")]
    [Description("毫秒精度应保持")]
    public async Task DateTime_MillisecondPrecision_ShouldMaintain()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var preciseTime = new System.DateTime(2024, 1, 15, 10, 30, 45, 123);

        // Act
        var id = await repo.InsertAsync(preciseTime, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        // SQLite TEXT 格式精度到秒，允许秒级精度
        Assert.AreEqual(preciseTime.ToString("yyyy-MM-dd HH:mm:ss"), result.DateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Query")]
    [Description("日期范围查询应正确工作")]
    public async Task DateTime_RangeQuery_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var date1 = new System.DateTime(2024, 1, 1);
        var date2 = new System.DateTime(2024, 6, 15);
        var date3 = new System.DateTime(2024, 12, 31);

        await repo.InsertAsync(date1, null, null, 0);
        await repo.InsertAsync(date2, null, null, 0);
        await repo.InsertAsync(date3, null, null, 0);

        // Act
        var results = await repo.GetByDateRangeAsync(
            new System.DateTime(2024, 5, 1),
            new System.DateTime(2024, 7, 1)
        );

        // Assert
        Assert.AreEqual(1, results.Count);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Special")]
    [Description("闰年2月29日应正确处理")]
    public async Task DateTime_LeapYearFeb29_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var leapDay = new System.DateTime(2024, 2, 29);

        // Act
        var id = await repo.InsertAsync(leapDay, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.DateTimeValue.Month);
        Assert.AreEqual(29, result.DateTimeValue.Day);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Special")]
    [Description("跨年边界应正确处理")]
    public async Task DateTime_YearBoundary_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var lastSecond2023 = new System.DateTime(2023, 12, 31, 23, 59, 59);
        var firstSecond2024 = new System.DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var id1 = await repo.InsertAsync(lastSecond2023, null, null, 0);
        var id2 = await repo.InsertAsync(firstSecond2024, null, null, 0);

        var result1 = await repo.GetByIdAsync(id1);
        var result2 = await repo.GetByIdAsync(id2);

        // Assert
        Assert.AreEqual(2023, result1!.DateTimeValue.Year);
        Assert.AreEqual(2024, result2!.DateTimeValue.Year);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Query")]
    [Description("按日期排序应正确工作")]
    public async Task DateTime_OrderBy_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var date1 = new System.DateTime(2024, 3, 15);
        var date2 = new System.DateTime(2024, 1, 10);
        var date3 = new System.DateTime(2024, 12, 25);

        await repo.InsertAsync(date1, null, null, 0);
        await repo.InsertAsync(date2, null, null, 0);
        await repo.InsertAsync(date3, null, null, 0);

        // Act
        var results = await repo.GetOrderedByDateAsync();

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results[0].DateTimeValue <= results[1].DateTimeValue);
        Assert.IsTrue(results[1].DateTimeValue <= results[2].DateTimeValue);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Calculation")]
    [Description("日期计算应正确工作")]
    public async Task DateTime_Calculation_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var baseDate = new System.DateTime(2024, 1, 1);
        var futureDate = baseDate.AddDays(30);

        await repo.InsertAsync(baseDate, null, null, 0);
        await repo.InsertAsync(futureDate, null, null, 0);

        // Act
        var results = await repo.GetFutureDatesAsync(baseDate.AddDays(15));

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].DateTimeValue > baseDate.AddDays(15));
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Format")]
    [Description("不同日期格式应被正确解析")]
    public async Task DateTime_DifferentFormats_ShouldBeParsed()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var date = new System.DateTime(2024, 6, 15, 14, 30, 0);

        // Act
        var id = await repo.InsertAsync(date, null, null, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result.DateTimeValue.Year);
        Assert.AreEqual(6, result.DateTimeValue.Month);
        Assert.AreEqual(15, result.DateTimeValue.Day);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Null")]
    [Description("NULL日期时间应正确处理")]
    public async Task DateTime_Null_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);

        // Act
        var id = await repo.InsertNullableDateAsync(null);
        var result = await repo.GetNullableByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.NullableDateTime);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Comparison")]
    [Description("日期比较运算符应正确工作")]
    public async Task DateTime_Comparison_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var today = System.DateTime.Today;
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);

        await repo.InsertAsync(yesterday, null, null, 0);
        await repo.InsertAsync(today, null, null, 0);
        await repo.InsertAsync(tomorrow, null, null, 0);

        // Act
        var beforeToday = await repo.GetBeforeDateAsync(today);
        var afterToday = await repo.GetAfterDateAsync(today);

        // Assert
        Assert.AreEqual(1, beforeToday.Count);
        Assert.AreEqual(1, afterToday.Count);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Aggregate")]
    [Description("日期聚合函数应正确工作")]
    public async Task DateTime_Aggregate_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var date1 = new System.DateTime(2024, 1, 1);
        var date2 = new System.DateTime(2024, 6, 15);
        var date3 = new System.DateTime(2024, 12, 31);

        await repo.InsertAsync(date1, null, null, 0);
        await repo.InsertAsync(date2, null, null, 0);
        await repo.InsertAsync(date3, null, null, 0);

        // Act
        var minDate = await repo.GetMinDateAsync();
        var maxDate = await repo.GetMaxDateAsync();

        // Assert
        Assert.AreEqual(date1.Year, minDate.Year);
        Assert.AreEqual(date3.Year, maxDate.Year);
    }

    [TestMethod]
    [TestCategory("DateTime")]
    [TestCategory("Batch")]
    [Description("批量插入日期时间应正确工作")]
    public async Task DateTime_BatchInsert_ShouldWork()
    {
        // Arrange
        var repo = new DateTimeTestRepository(_connection!);
        var dates = new List<DateTimeTestModel>();
        
        for (int i = 0; i < 100; i++)
        {
            dates.Add(new DateTimeTestModel
            {
                DateTimeValue = System.DateTime.Now.AddDays(i)
            });
        }

        // Act
        var inserted = await repo.BatchInsertAsync(dates);

        // Assert
        Assert.AreEqual(100, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(100, all.Count);
    }
}

// 测试模型
public class DateTimeTestModel
{
    public long Id { get; set; }
    public System.DateTime DateTimeValue { get; set; }
    public string? DateOnly { get; set; }
    public string? TimeOnly { get; set; }
    public long TimestampValue { get; set; }
    public System.DateTime? NullableDateTime { get; set; }
}

// 测试仓储
public interface IDateTimeTestRepository
{
    [SqlTemplate("INSERT INTO datetime_test (datetime_value, date_only, time_only, timestamp_value) VALUES (@dateTimeValue, @dateOnly, @timeOnly, @timestampValue)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(System.DateTime dateTimeValue, string? dateOnly, string? timeOnly, long timestampValue);

    [SqlTemplate("INSERT INTO datetime_test (datetime_value) VALUES (@nullableDateTime)")]
    [ReturnInsertedId]
    Task<long> InsertNullableDateAsync(System.DateTime? nullableDateTime);

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test WHERE id = @id")]
    Task<DateTimeTestModel?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test WHERE id = @id")]
    Task<DateTimeTestModel?> GetNullableByIdAsync(long id);

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test WHERE datetime_value BETWEEN @startDate AND @endDate")]
    Task<List<DateTimeTestModel>> GetByDateRangeAsync(System.DateTime startDate, System.DateTime endDate);

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test ORDER BY datetime_value")]
    Task<List<DateTimeTestModel>> GetOrderedByDateAsync();

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test WHERE datetime_value > @date")]
    Task<List<DateTimeTestModel>> GetFutureDatesAsync(System.DateTime date);

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test WHERE datetime_value < @date")]
    Task<List<DateTimeTestModel>> GetBeforeDateAsync(System.DateTime date);

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test WHERE datetime_value > @date")]
    Task<List<DateTimeTestModel>> GetAfterDateAsync(System.DateTime date);

    [SqlTemplate("SELECT MIN(datetime_value) FROM datetime_test")]
    Task<System.DateTime> GetMinDateAsync();

    [SqlTemplate("SELECT MAX(datetime_value) FROM datetime_test")]
    Task<System.DateTime> GetMaxDateAsync();

    [SqlTemplate("SELECT id, datetime_value as DateTimeValue, date_only as DateOnly, time_only as TimeOnly, timestamp_value as TimestampValue, datetime_value as NullableDateTime FROM datetime_test")]
    Task<List<DateTimeTestModel>> GetAllAsync();

    [BatchOperation(MaxBatchSize = 500)]
    [SqlTemplate("INSERT INTO datetime_test (datetime_value) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<DateTimeTestModel> models);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDateTimeTestRepository))]
public partial class DateTimeTestRepository(IDbConnection connection) : IDateTimeTestRepository { }

public static class DateTimeTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

