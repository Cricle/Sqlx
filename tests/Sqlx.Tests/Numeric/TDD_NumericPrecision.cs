using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Numeric;

/// <summary>
/// TDD: 数值精度测试
/// 验证各种数值类型的精度处理
/// </summary>
[TestClass]
public class TDD_NumericPrecision
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        _connection.Execute(@"
            CREATE TABLE numeric_test (
                id INTEGER PRIMARY KEY,
                decimal_value TEXT,
                double_value REAL,
                float_value REAL,
                int_value INTEGER,
                long_value INTEGER
            )
        ");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("Decimal最大值应正确存储和读取")]
    public async Task Numeric_DecimalMaxValue_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var maxDecimal = decimal.MaxValue;

        // Act
        var id = await repo.InsertAsync(maxDecimal, 0, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(maxDecimal, result.DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("Decimal最小值应正确存储和读取")]
    public async Task Numeric_DecimalMinValue_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var minDecimal = decimal.MinValue;

        // Act
        var id = await repo.InsertAsync(minDecimal, 0, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(minDecimal, result.DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("高精度Decimal应保持精度")]
    public async Task Numeric_HighPrecisionDecimal_ShouldMaintainPrecision()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var preciseValue = 123456789.123456789m;

        // Act
        var id = await repo.InsertAsync(preciseValue, 0, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(preciseValue, result.DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("Double精度边界值测试")]
    public async Task Numeric_DoublePrecisionBoundary_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var doubleValue = 1.7976931348623157E+308; // 接近Double.MaxValue

        // Act
        var id = await repo.InsertAsync(0, doubleValue, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(doubleValue, result.DoubleValue, 1E+300); // 允许一定误差
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("负零应正确处理")]
    public async Task Numeric_NegativeZero_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var negativeZero = -0.0;

        // Act
        var id = await repo.InsertAsync(0, negativeZero, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, Math.Abs(result.DoubleValue));
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("科学计数法表示的数值应正确存储")]
    public async Task Numeric_ScientificNotation_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var scientificValue = 1.23e-10;

        // Act
        var id = await repo.InsertAsync(0, scientificValue, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(scientificValue, result.DoubleValue, 1e-15);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("非常小的Decimal值应保持精度")]
    public async Task Numeric_VerySmallDecimal_ShouldMaintainPrecision()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var tinyValue = 0.000000000000000001m;

        // Act
        var id = await repo.InsertAsync(tinyValue, 0, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(tinyValue, result.DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("整数溢出边界测试")]
    public async Task Numeric_IntegerOverflowBoundary_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var maxInt = int.MaxValue;
        var minInt = int.MinValue;

        // Act
        var id1 = await repo.InsertAsync(0, 0, 0, maxInt, 0);
        var id2 = await repo.InsertAsync(0, 0, 0, minInt, 0);
        
        var result1 = await repo.GetByIdAsync(id1);
        var result2 = await repo.GetByIdAsync(id2);

        // Assert
        Assert.AreEqual(maxInt, result1!.IntValue);
        Assert.AreEqual(minInt, result2!.IntValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("Long最大最小值测试")]
    public async Task Numeric_LongBoundary_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var maxLong = long.MaxValue;
        var minLong = long.MinValue;

        // Act
        var id1 = await repo.InsertAsync(0, 0, 0, 0, maxLong);
        var id2 = await repo.InsertAsync(0, 0, 0, 0, minLong);
        
        var result1 = await repo.GetByIdAsync(id1);
        var result2 = await repo.GetByIdAsync(id2);

        // Assert
        Assert.AreEqual(maxLong, result1!.LongValue);
        Assert.AreEqual(minLong, result2!.LongValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("浮点数精度丢失应在可接受范围内")]
    public async Task Numeric_FloatingPointPrecisionLoss_ShouldBeAcceptable()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var floatValue = 0.1f + 0.2f; // 典型的浮点精度问题

        // Act
        var id = await repo.InsertAsync(0, 0, floatValue, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0.3f, result.FloatValue, 0.0001f); // 允许小误差
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("货币计算精度测试")]
    public async Task Numeric_CurrencyCalculation_ShouldMaintainPrecision()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var price = 19.99m;
        var quantity = 3;
        var total = price * quantity; // 59.97

        // Act
        var id = await repo.InsertAsync(total, 0, 0, 0, 0);
        var result = await repo.GetByIdAsync(id);

        // Assert
        Assert.AreEqual(59.97m, result!.DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("除法精度测试")]
    public async Task Numeric_DivisionPrecision_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        var result1 = 10m / 3m; // 3.3333...
        var result2 = 1.0 / 3.0; // double除法

        // Act
        var id1 = await repo.InsertAsync(result1, 0, 0, 0, 0);
        var id2 = await repo.InsertAsync(0, result2, 0, 0, 0);
        
        var data1 = await repo.GetByIdAsync(id1);
        var data2 = await repo.GetByIdAsync(id2);

        // Assert
        Assert.IsTrue(data1!.DecimalValue > 3.33m && data1.DecimalValue < 3.34m);
        Assert.AreEqual(result2, data2!.DoubleValue, 1e-10);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("零值各种形式测试")]
    public async Task Numeric_ZeroVariations_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);

        // Act & Assert
        var id1 = await repo.InsertAsync(0m, 0.0, 0f, 0, 0L);
        var id2 = await repo.InsertAsync(decimal.Zero, 0.0, 0f, 0, 0L);
        var id3 = await repo.InsertAsync(-0m, -0.0, -0f, 0, 0L);

        var result1 = await repo.GetByIdAsync(id1);
        var result2 = await repo.GetByIdAsync(id2);
        var result3 = await repo.GetByIdAsync(id3);

        Assert.AreEqual(0m, result1!.DecimalValue);
        Assert.AreEqual(0m, result2!.DecimalValue);
        Assert.AreEqual(0m, result3!.DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("数值比较精度测试")]
    public async Task Numeric_ComparisonPrecision_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        await repo.InsertAsync(100.001m, 0, 0, 0, 0);
        await repo.InsertAsync(100.002m, 0, 0, 0, 0);
        await repo.InsertAsync(100.003m, 0, 0, 0, 0);

        // Act
        var results = await repo.GetByDecimalRangeAsync(100.0015m, 100.0025m);

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(100.002m, results[0].DecimalValue);
    }

    [TestMethod]
    [TestCategory("Numeric")]
    [TestCategory("Precision")]
    [Description("聚合函数精度测试")]
    public async Task Numeric_AggregatePrecision_ShouldWork()
    {
        // Arrange
        var repo = new NumericTestRepository(_connection!);
        await repo.InsertAsync(10.5m, 0, 0, 0, 0);
        await repo.InsertAsync(20.5m, 0, 0, 0, 0);
        await repo.InsertAsync(30.5m, 0, 0, 0, 0);

        // Act
        var sum = await repo.GetDecimalSumAsync();

        // Assert
        Assert.AreEqual(61.5m, sum);
    }
}

// 测试模型
public class NumericTestModel
{
    public long Id { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public int IntValue { get; set; }
    public long LongValue { get; set; }
}

// 测试仓储
public interface INumericTestRepository
{
    [SqlTemplate("INSERT INTO numeric_test (decimal_value, double_value, float_value, int_value, long_value) VALUES (@decimalValue, @doubleValue, @floatValue, @intValue, @longValue)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(decimal decimalValue, double doubleValue, float floatValue, int intValue, long longValue);

    [SqlTemplate("SELECT id, CAST(decimal_value AS REAL) as DecimalValue, double_value as DoubleValue, float_value as FloatValue, int_value as IntValue, long_value as LongValue FROM numeric_test WHERE id = @id")]
    Task<NumericTestModel?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, CAST(decimal_value AS REAL) as DecimalValue, double_value as DoubleValue, float_value as FloatValue, int_value as IntValue, long_value as LongValue FROM numeric_test WHERE CAST(decimal_value AS REAL) BETWEEN @min AND @max")]
    Task<List<NumericTestModel>> GetByDecimalRangeAsync(decimal min, decimal max);

    [SqlTemplate("SELECT SUM(CAST(decimal_value AS REAL)) FROM numeric_test")]
    Task<decimal> GetDecimalSumAsync();
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(INumericTestRepository))]
public partial class NumericTestRepository(IDbConnection connection) : INumericTestRepository { }

public static class NumericTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

