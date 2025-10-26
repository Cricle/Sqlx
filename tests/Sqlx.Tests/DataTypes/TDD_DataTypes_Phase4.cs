using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.DataTypes;

/// <summary>
/// Phase 4 Batch 1: 数据类型全覆盖测试
/// 新增15个数据类型测试
/// </summary>
[TestClass]
[TestCategory("TDD-Green")]
[TestCategory("DataTypes")]
[TestCategory("Phase4")]
public class TDD_DataTypes_Phase4
{
    private IDbConnection? _connection;
    private IDataTypesRepository? _repo;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE data_types_test (
                id INTEGER PRIMARY KEY,
                text_value TEXT,
                int_value INTEGER,
                real_value REAL,
                blob_value BLOB,
                datetime_value TEXT,
                guid_value TEXT,
                bool_value INTEGER,
                decimal_value TEXT
            )");

        _repo = new DataTypesRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [Description("DateTime type should work")]
    public async Task DataType_DateTime_ShouldWork()
    {
        var now = new DateTime(2025, 10, 26, 12, 30, 45);
        _connection!.Execute($"INSERT INTO data_types_test (id, datetime_value) VALUES (1, '{now:yyyy-MM-dd HH:mm:ss}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.datetime_value);
        // SQLite stores datetime as string, so we check year/month/day
        Assert.AreEqual(2025, result.datetime_value.Value.Year);
        Assert.AreEqual(10, result.datetime_value.Value.Month);
        Assert.AreEqual(26, result.datetime_value.Value.Day);
    }

    [TestMethod]
    [Description("DateTime NULL should work")]
    public async Task DataType_DateTimeNull_ShouldWork()
    {
        _connection!.Execute("INSERT INTO data_types_test (id, datetime_value) VALUES (1, NULL)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNull(result.datetime_value);
    }

    [TestMethod]
    [Description("DateTime min/max values should work")]
    public async Task DataType_DateTimeMinMax_ShouldWork()
    {
        var minDate = new DateTime(1900, 1, 1);
        var maxDate = new DateTime(2100, 12, 31);

        _connection!.Execute($"INSERT INTO data_types_test (id, datetime_value) VALUES (1, '{minDate:yyyy-MM-dd HH:mm:ss}')");
        _connection.Execute($"INSERT INTO data_types_test (id, datetime_value) VALUES (2, '{maxDate:yyyy-MM-dd HH:mm:ss}')");

        var min = await _repo!.GetByIdAsync(1);
        var max = await _repo.GetByIdAsync(2);

        Assert.IsNotNull(min?.datetime_value);
        Assert.IsNotNull(max?.datetime_value);
        Assert.AreEqual(1900, min.datetime_value.Value.Year);
        Assert.AreEqual(2100, max.datetime_value.Value.Year);
    }

    [TestMethod]
    [Description("Guid type should work")]
    public async Task DataType_Guid_ShouldWork()
    {
        var guid = Guid.NewGuid();
        _connection!.Execute($"INSERT INTO data_types_test (id, guid_value) VALUES (1, '{guid}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.guid_value);
        Assert.AreEqual(guid, result.guid_value.Value);
    }

    [TestMethod]
    [Description("Guid NULL should work")]
    public async Task DataType_GuidNull_ShouldWork()
    {
        _connection!.Execute("INSERT INTO data_types_test (id, guid_value) VALUES (1, NULL)");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNull(result.guid_value);
    }

    [TestMethod]
    [Description("Guid empty should work")]
    public async Task DataType_GuidEmpty_ShouldWork()
    {
        var emptyGuid = Guid.Empty;
        _connection!.Execute($"INSERT INTO data_types_test (id, guid_value) VALUES (1, '{emptyGuid}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.AreEqual(Guid.Empty, result.guid_value);
    }

    [TestMethod]
    [Description("Decimal high precision should work")]
    public async Task DataType_DecimalPrecision_ShouldWork()
    {
        var precise = 123456.789012m;
        _connection!.Execute($"INSERT INTO data_types_test (id, decimal_value) VALUES (1, '{precise}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.decimal_value);
        Assert.AreEqual(precise, result.decimal_value.Value);
    }

    [TestMethod]
    [Description("Decimal very large value should work")]
    public async Task DataType_DecimalLarge_ShouldWork()
    {
        var large = 999999999999.99m;
        _connection!.Execute($"INSERT INTO data_types_test (id, decimal_value) VALUES (1, '{large}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.decimal_value);
        Assert.AreEqual(large, result.decimal_value.Value);
    }

    [TestMethod]
    [Description("Decimal very small value should work")]
    public async Task DataType_DecimalSmall_ShouldWork()
    {
        var small = 0.000001m;
        _connection!.Execute($"INSERT INTO data_types_test (id, decimal_value) VALUES (1, '{small}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.decimal_value);
        Assert.IsTrue(result.decimal_value.Value < 0.00001m);
        Assert.IsTrue(result.decimal_value.Value > 0);
    }

    [TestMethod]
    [Description("Boolean true/false should work")]
    public async Task DataType_Boolean_ShouldWork()
    {
        _connection!.Execute("INSERT INTO data_types_test (id, bool_value) VALUES (1, 1)");
        _connection.Execute("INSERT INTO data_types_test (id, bool_value) VALUES (2, 0)");

        var trueResult = await _repo!.GetByIdAsync(1);
        var falseResult = await _repo.GetByIdAsync(2);

        Assert.IsNotNull(trueResult);
        Assert.IsNotNull(falseResult);
        Assert.IsTrue(trueResult.bool_value == true);
        Assert.IsTrue(falseResult.bool_value == false);
    }

    [TestMethod]
    [Description("Byte array (blob) should work")]
    public async Task DataType_ByteArray_ShouldWork()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF };
        var hexString = BitConverter.ToString(bytes).Replace("-", "");
        _connection!.Execute($"INSERT INTO data_types_test (id, blob_value) VALUES (1, X'{hexString}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.blob_value);
        CollectionAssert.AreEqual(bytes, result.blob_value);
    }

    [TestMethod]
    [Description("Empty byte array should work")]
    public async Task DataType_EmptyByteArray_ShouldWork()
    {
        _connection!.Execute("INSERT INTO data_types_test (id, blob_value) VALUES (1, X'')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        // Empty blob may be null or empty array
        Assert.IsTrue(result.blob_value == null || result.blob_value.Length == 0);
    }

    [TestMethod]
    [Description("Large byte array should work")]
    public async Task DataType_LargeByteArray_ShouldWork()
    {
        var bytes = new byte[1000];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i % 256);
        }

        var hexString = BitConverter.ToString(bytes).Replace("-", "");
        _connection!.Execute($"INSERT INTO data_types_test (id, blob_value) VALUES (1, X'{hexString}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.blob_value);
        Assert.AreEqual(1000, result.blob_value.Length);
    }

    [TestMethod]
    [Description("Long string (4000+ chars) should work")]
    public async Task DataType_LongString_ShouldWork()
    {
        var longString = new string('A', 5000);
        // Escape single quotes for SQL
        _connection!.Execute($"INSERT INTO data_types_test (id, text_value) VALUES (1, '{longString}')");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.text_value);
        Assert.AreEqual(5000, result.text_value.Length);
    }

    [TestMethod]
    [Description("Multiple data types in single record should work")]
    public async Task DataType_AllTypes_ShouldWork()
    {
        var guid = Guid.NewGuid();
        var date = new DateTime(2025, 10, 26);
        _connection!.Execute($@"
            INSERT INTO data_types_test
            (id, text_value, int_value, real_value, guid_value, datetime_value, bool_value, decimal_value)
            VALUES
            (1, 'test', 42, 3.14, '{guid}', '{date:yyyy-MM-dd}', 1, '99.99')
        ");

        var result = await _repo!.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.text_value);
        Assert.AreEqual(42, result.int_value);
        Assert.AreEqual(3.14, result.real_value!.Value, 0.01);
        Assert.AreEqual(guid, result.guid_value);
        Assert.IsTrue(result.bool_value == true);
        Assert.AreEqual(99.99m, result.decimal_value);
    }
}

// Repository interface
public interface IDataTypesRepository
{
    [SqlTemplate("SELECT * FROM data_types_test WHERE id = @id")]
    Task<DataTypesModel?> GetByIdAsync(long id);
}

// Repository implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDataTypesRepository))]
public partial class DataTypesRepository(IDbConnection connection) : IDataTypesRepository { }

// Model - using snake_case to match database columns
public class DataTypesModel
{
    public long id { get; set; }
    public string? text_value { get; set; }
    public int? int_value { get; set; }
    public double? real_value { get; set; }
    public byte[]? blob_value { get; set; }
    public DateTime? datetime_value { get; set; }
    public Guid? guid_value { get; set; }
    public bool? bool_value { get; set; }  // Make nullable
    public decimal? decimal_value { get; set; }
}

// Extension method
public static class DataTypesTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

