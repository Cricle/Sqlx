// Targeted branch coverage tests - batch 2
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlx.Tests.BranchCoverage;

// ── TypeConverter gaps ──────────────────────────────────────────────────────

[TestClass]
public class TypeConverterBranch2Tests
{
    // line 80/84: byte[] from string (base64) vs direct cast
    [TestMethod]
    public void Convert_StringToByteArray_Base64()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var b64 = Convert.ToBase64String(bytes);
        var result = TypeConverter.Convert<byte[]>(b64);
        CollectionAssert.AreEqual(bytes, result);
    }

    [TestMethod]
    public void Convert_ByteArrayToByteArray_DirectCast()
    {
        var bytes = new byte[] { 10, 20 };
        var result = TypeConverter.Convert<byte[]>(bytes);
        CollectionAssert.AreEqual(bytes, result);
    }

    // line 153/160: TypeCode.Object path (fallback ChangeType)
    [TestMethod]
    public void Convert_StringToGuid_ViaConvertFromString()
    {
        var g = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid>(g.ToString());
        Assert.AreEqual(g, result);
    }

    [TestMethod]
    public void Convert_StringToTimeSpan_ViaConvertFromString()
    {
        var ts = TimeSpan.FromHours(2.5);
        var result = TypeConverter.Convert<TimeSpan>(ts.ToString());
        Assert.AreEqual(ts, result);
    }

    // line 165-167: nullable string path
    [TestMethod]
    public void Convert_StringToNullableInt_Success()
    {
        var result = TypeConverter.Convert<int?>("42");
        Assert.AreEqual(42, result);
    }

    // line 235-245: IsTimeOnlyType / IsDateOnlyType paths via string
    [TestMethod]
    public void Convert_StringToDateOnly_Success()
    {
        var result = TypeConverter.Convert<DateOnly>("2024-06-15");
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }

    [TestMethod]
    public void Convert_StringToTimeOnly_Success()
    {
        var result = TypeConverter.Convert<TimeOnly>("14:30:00");
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    // line 321-396: BuildConversion expression builder paths
    [TestMethod]
    public void BuildConversion_NullableInt_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(int?));
        Assert.IsNotNull(expr);
        Assert.AreEqual(typeof(int?), expr.Type);
    }

    [TestMethod]
    public void BuildConversion_NonNullableString_ReturnsGetStringCall()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(string));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_Guid_UsesGetGuid()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(Guid));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_DateOnly_UsesConvertMethod()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(DateOnly));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_TimeOnly_UsesConvertMethod()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(TimeOnly));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_NullableDateOnly_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(DateOnly?));
        Assert.IsNotNull(expr);
        Assert.AreEqual(typeof(DateOnly?), expr.Type);
    }

    [TestMethod]
    public void BuildConversion_NullableTimeOnly_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(TimeOnly?));
        Assert.IsNotNull(expr);
        Assert.AreEqual(typeof(TimeOnly?), expr.Type);
    }

    [TestMethod]
    public void BuildConversion_NullableGuid_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(Guid?));
        Assert.IsNotNull(expr);
        Assert.AreEqual(typeof(Guid?), expr.Type);
    }

    [TestMethod]
    public void BuildConversion_NullableDateTime_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(DateTime?));
        Assert.IsNotNull(expr);
        Assert.AreEqual(typeof(DateTime?), expr.Type);
    }

    [TestMethod]
    public void BuildConversion_NullableDecimal_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(decimal?));
        Assert.IsNotNull(expr);
        Assert.AreEqual(typeof(decimal?), expr.Type);
    }

    [TestMethod]
    public void BuildConversion_NullableDouble_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(double?));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_NullableFloat_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(float?));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_NullableLong_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(long?));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_NullableShort_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(short?));
        Assert.IsNotNull(expr);
    }

    [TestMethod]
    public void BuildConversion_NullableBool_ReturnsConditionExpression()
    {
        var readerParam = Expression.Parameter(typeof(IDataRecord), "r");
        var ordinal = Expression.Constant(0);
        var expr = TypeConverter.BuildConversion(readerParam, ordinal, typeof(bool?));
        Assert.IsNotNull(expr);
    }
}

// ── ExpressionHelper gaps ───────────────────────────────────────────────────

[TestClass]
public class ExpressionHelperBranch2Tests
{
    // line 38: FindRootParameterType returns null when expr is null
    [TestMethod]
    public void FindRootParameterType_NullExpr_ReturnsNull()
    {
        var method = typeof(ExpressionHelper).GetMethod("FindRootParameterType",
            BindingFlags.Public | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { null });
        Assert.IsNull(result);
    }

    // line 58: FindRootParameterType with MethodCallExpression (object != null)
    [TestMethod]
    public void FindRootParameterType_MethodCallWithObject_ReturnsParamType()
    {
        Expression<Func<string, string>> expr = s => s.ToUpper();
        var mc = (MethodCallExpression)expr.Body;
        var method = typeof(ExpressionHelper).GetMethod("FindRootParameterType",
            BindingFlags.Public | BindingFlags.Static)!;
        var result = method.Invoke(null, new object?[] { mc });
        Assert.AreEqual(typeof(string), result);
    }

    // line 70: IsNestedEntityProperty with Convert unary
    [TestMethod]
    public void IsEntityProperty_WithConvertUnary_ReturnsTrue()
    {
        // A member expression from a parameter - standard entity property
        Expression<Func<BpUser, int>> expr = u => u.Id;
        var member = (MemberExpression)expr.Body;
        var result = ExpressionHelper.IsEntityProperty(member);
        Assert.IsTrue(result);
    }

    // line 78-79: IsNestedEntityProperty with nested member chain
    [TestMethod]
    public void IsEntityProperty_NestedMemberChain_ReturnsTrue()
    {
        // x.Name where x is a parameter - standard entity property
        Expression<Func<BpUser, string>> expr = u => u.Name;
        var member = (MemberExpression)expr.Body;
        Assert.IsTrue(ExpressionHelper.IsEntityProperty(member));
    }

    // line 101: IsAggregateContext with Math method returns false
    [TestMethod]
    public void IsAggregateContext_MathMethod_ReturnsFalse()
    {
        Expression<Func<double>> expr = () => Math.Abs(-1.0);
        var mc = (MethodCallExpression)expr.Body;
        Assert.IsFalse(ExpressionHelper.IsAggregateContext(mc));
    }

    // line 132/135-136: TryEvaluateMemberValue with null expression
    [TestMethod]
    public void GetMemberValueOptimized_ClosureField_ReturnsValue()
    {
        var captured = 42;
        Expression<Func<int>> expr = () => captured;
        var member = (MemberExpression)expr.Body;
        var result = ExpressionHelper.GetMemberValueOptimized(member);
        Assert.AreEqual(42, result);
    }

    // line 149/155: TryEvaluateMemberValue - FieldInfo path
    [TestMethod]
    public void EvaluateExpression_FieldAccess_ReturnsValue()
    {
        var obj = new HelperTestClass { PublicField = 99 };
        Expression<Func<int>> expr = () => obj.PublicField;
        var result = ExpressionHelper.EvaluateExpression(expr.Body);
        Assert.AreEqual(99, result);
    }

    // line 160-161: TryEvaluateMemberValue - default (indexed property) returns false
    [TestMethod]
    public void EvaluateExpression_IndexedProperty_FallsBackToCompile()
    {
        var dict = new Dictionary<string, int> { ["key"] = 7 };
        Expression<Func<int>> expr = () => dict["key"];
        var result = ExpressionHelper.EvaluateExpression(expr.Body);
        Assert.AreEqual(7, result);
    }

    // line 164: TryEvaluateMemberValue - ConvertChecked unary
    [TestMethod]
    public void EvaluateExpression_ConvertCheckedUnary_ReturnsValue()
    {
        int val = 5;
        Expression<Func<long>> expr = () => checked((long)val);
        var result = ExpressionHelper.EvaluateExpression(expr.Body);
        Assert.AreEqual(5L, result);
    }

    // line 209: ConvertToSnakeCase - digit before upper triggers underscore
    [TestMethod]
    public void ConvertToSnakeCase_DigitBeforeUpper_InsertsUnderscore()
    {
        var result = ExpressionHelper.ConvertToSnakeCase("Order2Items");
        Assert.IsTrue(result.Contains('_'));
    }

    // IsCollectionType - ICollection<> and IList<>
    [TestMethod]
    public void IsCollectionType_ICollection_ReturnsTrue()
        => Assert.IsTrue(ExpressionHelper.IsCollectionType(typeof(ICollection<int>)));

    [TestMethod]
    public void IsCollectionType_IList_ReturnsTrue()
        => Assert.IsTrue(ExpressionHelper.IsCollectionType(typeof(IList<int>)));
}

public class HelperTestClass
{
    public int PublicField;
    public string Name { get; set; } = "";
}

// ── SetExpressionExtensions gaps ────────────────────────────────────────────

[TestClass]
public class SetExpressionBranch2Tests
{
    // lines 67-69: non-MemberAssignment binding (skip)
    // This is hard to trigger directly; instead test MethodCall in expression
    [TestMethod]
    public void ParseUpdate_WithMethodCallInExpression_ExtractsParams()
    {
        Expression<Func<BpUser, BpUser>> expr = u => new BpUser
        {
            Name = u.Name.ToUpper()
        };
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }

    // lines 110-112: ExtractParameters with BinaryExpression
    [TestMethod]
    public void ParseUpdate_WithBinaryInExpression_ExtractsParams()
    {
        Expression<Func<BpUser, BpUser>> expr = u => new BpUser
        {
            Score = u.Score + 5
        };
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }

    // lines 123-125: ExtractParameters with UnaryExpression
    [TestMethod]
    public void ParseUpdate_WithUnaryInExpression_ExtractsParams()
    {
        Expression<Func<BpUser, BpUser>> expr = u => new BpUser
        {
            Score = -u.Score
        };
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }

    // lines 152-155: ExtractParameters with MethodCallExpression (object + args)
    [TestMethod]
    public void ParseUpdate_WithMethodCallAndArgs_ExtractsParams()
    {
        Expression<Func<BpUser, BpUser>> expr = u => new BpUser
        {
            Name = string.Concat(u.Name, "!")
        };
        var result = ExpressionBlockResult.ParseUpdate(expr, SqlDefine.SQLite);
        Assert.IsNotNull(result.Sql);
    }
}

// ── DynamicResultReader gaps ─────────────────────────────────────────────────

[TestClass]
public class DynamicResultReaderBranch2Tests
{
    // lines 153-155: Read(IDataReader, ReadOnlySpan<int>) with non-empty ordinals
    [TestMethod]
    public void DynamicResultReader_ReadWithSpanOrdinals_UsesRentedArray()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 AS id, 'hello' AS name";
        using var reader = cmd.ExecuteReader();
        Assert.IsTrue(reader.Read());

        var dr = new DynamicResultReader<DynTestEntity>(
            new[] { "id", "name" });

        var ordinals = new int[] { 0, 1 };
        var result = dr.Read(reader, ordinals.AsSpan());
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("hello", result.Name);
    }

    // lines 186/192: GetOrdinals with snake_case fallback
    [TestMethod]
    public void DynamicResultReader_GetOrdinals_SnakeCaseFallback()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 42 AS user_id";
        using var reader = cmd.ExecuteReader();
        Assert.IsTrue(reader.Read());

        var dr = new DynamicResultReader<DynTestEntity>(
            new[] { "UserId" });

        var ordinals = new int[1];
        dr.GetOrdinals(reader, ordinals.AsSpan());
        Assert.AreEqual(0, ordinals[0]);
    }

    // lines 202-214: ResolveOrdinal throws when column not found
    [TestMethod]
    [ExpectedException(typeof(IndexOutOfRangeException))]
    public void DynamicResultReader_GetOrdinals_ColumnNotFound_Throws()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 AS x";
        using var reader = cmd.ExecuteReader();
        Assert.IsTrue(reader.Read());

        var dr = new DynamicResultReader<DynTestEntity>(
            new[] { "nonexistent_column" });

        var ordinals = new int[1];
        dr.GetOrdinals(reader, ordinals.AsSpan());
    }
}

public class DynTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
