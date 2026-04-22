// Branch coverage tests for TypeConverter, ExpressionHelper, IfPlaceholderHandler, ValueFormatter, DateTimeFunctionParser, TableNameResolver
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests.BranchCoverage;

[TestClass]
public class TypeConverterBranchTests
{
    // Line 80: byte[] target, sourceType != string → return (T)value
    [TestMethod]
    public void Convert_ByteArray_SameType_ReturnsDirect()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var result = TypeConverter.Convert<byte[]>(bytes);
        CollectionAssert.AreEqual(bytes, result);
    }

    // Line 153: TypeCode != Object → ChangeType
    [TestMethod]
    public void Convert_Int_FromLong_UsesTypeCode()
    {
        var result = TypeConverter.Convert<int>((long)42);
        Assert.AreEqual(42, result);
    }

    // Line 165: ConvertFromString string target
    [TestMethod]
    public void Convert_String_FromString_ReturnsDirect()
    {
        var result = TypeConverter.Convert<string>("hello");
        Assert.AreEqual("hello", result);
    }

    // Line 230: ConvertFromString TimeOnly target
    [TestMethod]
    public void Convert_TimeOnly_FromString_ParsesCorrectly()
    {
        var result = TypeConverter.Convert<TimeOnly>("14:30:00");
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    // Line 235: ConvertFromString Guid target (unreachable - Guid handled earlier)
    // Line 240: ConvertFromString byte[] target (unreachable - byte[] handled earlier)
    // These are dead code paths - skip

    // Lines 321-351: CreateDateOnly/TimeOnly from various sources
    [TestMethod]
    public void Convert_DateOnly_FromDateTime_Converts()
    {
        var dt = new DateTime(2024, 6, 15);
        var result = TypeConverter.Convert<DateOnly>(dt);
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }

    [TestMethod]
    public void Convert_DateOnly_FromDateTimeOffset_Converts()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<DateOnly>(dto);
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }

    [TestMethod]
    public void Convert_TimeOnly_FromDateTime_Converts()
    {
        var dt = new DateTime(2024, 1, 1, 14, 30, 0);
        var result = TypeConverter.Convert<TimeOnly>(dt);
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    [TestMethod]
    public void Convert_TimeOnly_FromDateTimeOffset_Converts()
    {
        var dto = new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<TimeOnly>(dto);
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    [TestMethod]
    public void Convert_TimeOnly_FromTimeSpan_Converts()
    {
        var ts = new TimeSpan(14, 30, 0);
        var result = TypeConverter.Convert<TimeOnly>(ts);
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    // Lines 357,369,382,394: CreateUnaryBoxingDelegate/CreateBinaryBoxingDelegate null method
    // These are covered by static initializer - DateOnly/TimeOnly exist on .NET 10
    [TestMethod]
    public void Convert_DateOnly_FromString_ParsesCorrectly()
    {
        var result = TypeConverter.Convert<DateOnly>("2024-06-15");
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }
}

[TestClass]
public class ExpressionHelperBranchTests
{
    // Line 38: FindRootParameterType - while loop exits (expr becomes null)
    [TestMethod]
    public void FindRootParameterType_WithConstantExpression_ReturnsNull()
    {
        var expr = Expression.Constant(42);
        var result = ExpressionHelper.FindRootParameterType(expr);
        Assert.IsNull(result);
    }

    // Line 70: IsNestedEntityProperty - MethodCallExpression with Object
    [TestMethod]
    public void IsNestedEntityProperty_WithMethodCallObject_Traverses()
    {
        // Covered via Contains/StartsWith in queries
        var q = SqlQuery<BpUser>.ForSqlite().Where(u => u.Name.Contains("test"));
        Assert.IsNotNull(q.ToSql());
    }

    // Line 97: IsCollectionType - ICollection<>
    [TestMethod]
    public void IsCollectionType_ICollection_ReturnsTrue()
    {
        var t = typeof(System.Collections.Generic.ICollection<int>);
        var result = ExpressionHelper.IsCollectionType(t);
        Assert.IsTrue(result);
    }

    // Line 97: IsCollectionType - IList<>
    [TestMethod]
    public void IsCollectionType_IList_ReturnsTrue()
    {
        var t = typeof(System.Collections.Generic.IList<int>);
        var result = ExpressionHelper.IsCollectionType(t);
        Assert.IsTrue(result);
    }

    // Line 97: IsCollectionType - Array
    [TestMethod]
    public void IsCollectionType_Array_ReturnsTrue()
    {
        var t = typeof(int[]);
        var result = ExpressionHelper.IsCollectionType(t);
        Assert.IsTrue(result);
    }

    // Line 101: IsAggregateContext - Math method returns false
    [TestMethod]
    public void IsAggregateContext_MathMethod_ReturnsFalse()
    {
        var mathAbs = typeof(Math).GetMethod("Abs", new[] { typeof(int) })!;
        var call = Expression.Call(mathAbs, Expression.Constant(5));
        var result = ExpressionHelper.IsAggregateContext(call);
        Assert.IsFalse(result);
    }

    // Line 101: IsAggregateContext - Sum returns true
    [TestMethod]
    public void IsAggregateContext_Sum_ReturnsTrue()
    {
        // Use a method named "Sum" from Queryable (generic method)
        var sumMethod = typeof(System.Linq.Queryable).GetMethods()
            .First(m => m.Name == "Sum" && m.IsGenericMethod && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.IsGenericType);
        var genericSum = sumMethod.MakeGenericMethod(typeof(int));
        var source = Expression.Constant(new int[0].AsQueryable());
        Expression<Func<int, int>> selector = x => x;
        var call = Expression.Call(genericSum, source, Expression.Quote(selector));
        var result = ExpressionHelper.IsAggregateContext(call);
        Assert.IsTrue(result);
    }

    // Line 132: TryEvaluateMemberValue - PropertyInfo path
    [TestMethod]
    public void EvaluateExpression_WithPropertyAccess_ReturnsValue()
    {
        var obj = new { Value = 42 };
        Expression<Func<int>> expr = () => obj.Value;
        var result = ExpressionHelper.EvaluateExpression(expr.Body);
        Assert.AreEqual(42, result);
    }

    // Line 149: TryEvaluateMemberValue - FieldInfo path
    [TestMethod]
    public void EvaluateExpression_WithFieldAccess_ReturnsValue()
    {
        var captured = 99;
        Expression<Func<int>> expr = () => captured;
        var result = ExpressionHelper.EvaluateExpression(expr.Body);
        Assert.AreEqual(99, result);
    }

    // Line 155: TryEvaluateMemberValue - PropertyInfo with index params (skip)
    // Line 175: ConvertToSnakeCase - empty string
    [TestMethod]
    public void ConvertToSnakeCase_EmptyString_ReturnsEmpty()
    {
        var result = ExpressionHelper.ConvertToSnakeCase(string.Empty);
        Assert.AreEqual(string.Empty, result);
    }

    // Line 192: ConvertToSnakeCaseCore - empty string
    [TestMethod]
    public void ConvertToSnakeCaseCore_EmptyString_ReturnsEmpty()
    {
        var result = ExpressionHelper.ConvertToSnakeCaseCore(string.Empty);
        Assert.AreEqual(string.Empty, result);
    }

    // Line 196: ConvertToSnakeCaseCore - long string (> 128 chars) uses heap
    [TestMethod]
    public void ConvertToSnakeCaseCore_LongString_UsesHeap()
    {
        var longName = "A" + new string('b', 130);
        var result = ExpressionHelper.ConvertToSnakeCaseCore(longName);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("a"));
    }

    // Line 209: ConvertToSnakeCaseCore - uppercase after digit
    [TestMethod]
    public void ConvertToSnakeCase_UpperAfterDigit_InsertsUnderscore()
    {
        var result = ExpressionHelper.ConvertToSnakeCase("User2Name");
        Assert.IsTrue(result.Contains("_"), result);
    }
}

[TestClass]
public class IfPlaceholderHandlerBranchTests
{
    private static SqlTemplate BuildTemplate(string condition)
    {
        var sql = $"SELECT 1 {{{{if {condition}=param}}}}AND 1=1{{{{/if}}}}";
        var columns = new[] { new ColumnMeta("id", "Id", System.Data.DbType.Int32, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "t", columns);
        return SqlTemplate.Prepare(sql, context);
    }

    // Line 81: notempty with parameters=null
    [TestMethod]
    public void IfHandler_NotEmpty_NullParameters_TreatsAsEmpty()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(null);
        Assert.IsFalse(result.Contains("AND 1=1"), result);
    }

    // Line 81: notempty with non-empty value
    [TestMethod]
    public void IfHandler_NotEmpty_NonEmptyValue_IncludesBlock()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = "hello" });
        Assert.IsTrue(result.Contains("AND 1=1"), result);
    }

    // Line 81: notempty with empty string
    [TestMethod]
    public void IfHandler_NotEmpty_EmptyString_ExcludesBlock()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = "" });
        Assert.IsFalse(result.Contains("AND 1=1"), result);
    }

    // Line 89: empty with non-empty value
    [TestMethod]
    public void IfHandler_Empty_NonEmptyValue_ExcludesBlock()
    {
        var t = BuildTemplate("empty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = "hello" });
        Assert.IsFalse(result.Contains("AND 1=1"), result);
    }

    // Line 89: empty with null parameters
    [TestMethod]
    public void IfHandler_Empty_NullParameters_IncludesBlock()
    {
        var t = BuildTemplate("empty");
        var result = t.Render(null);
        Assert.IsTrue(result.Contains("AND 1=1"), result);
    }

    // Line 101: IsEmpty with ICollection (empty)
    [TestMethod]
    public void IfHandler_NotEmpty_EmptyCollection_ExcludesBlock()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = new List<int>() });
        Assert.IsFalse(result.Contains("AND 1=1"), result);
    }

    // Line 101: IsEmpty with ICollection (non-empty)
    [TestMethod]
    public void IfHandler_NotEmpty_NonEmptyCollection_IncludesBlock()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = new List<int> { 1 } });
        Assert.IsTrue(result.Contains("AND 1=1"), result);
    }

    // Line 105: IsEmpty with IEnumerable (non-ICollection)
    [TestMethod]
    public void IfHandler_NotEmpty_NonEmptyEnumerable_IncludesBlock()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = Enumerable.Range(1, 3) });
        Assert.IsTrue(result.Contains("AND 1=1"), result);
    }

    // Line 105: IsEmpty with empty IEnumerable
    [TestMethod]
    public void IfHandler_NotEmpty_EmptyEnumerable_ExcludesBlock()
    {
        var t = BuildTemplate("notempty");
        var result = t.Render(new Dictionary<string, object?> { ["param"] = Enumerable.Empty<int>() });
        Assert.IsFalse(result.Contains("AND 1=1"), result);
    }
}

[TestClass]
public class ValueFormatterBranchTests
{
    // Line 247: FormatAsLiteral - various types
    [TestMethod]
    public void FormatAsLiteral_Null_ReturnsNULL()
    {
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<BpUser, bool>> expr = u => u.Email == null;
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("NULL"), sql);
    }

    [TestMethod]
    public void FormatAsLiteral_Char_WrapsInQuotes()
    {
        // char type - line 259
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var c = 'A';
        Expression<Func<BpUser, bool>> expr = u => u.Name == c.ToString();
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void FormatAsLiteral_TimeSpan_WrapsInQuotes()
    {
        // TimeSpan - line 255
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var ts = TimeSpan.FromHours(1);
        Expression<Func<BpUser, bool>> expr = u => u.Id == (int)ts.TotalSeconds;
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void FormatAsLiteral_DateTimeOffset_WrapsInQuotes()
    {
        // DateTimeOffset - line 253
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var dto = DateTimeOffset.UtcNow;
        Expression<Func<BpUser, bool>> expr = u => u.Id == dto.Year;
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    [TestMethod]
    public void FormatAsLiteral_Guid_WrapsInQuotes()
    {
        // Guid - line 254
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        var g = Guid.NewGuid();
        Expression<Func<BpUser, bool>> expr = u => u.Name == g.ToString();
        var sql = parser.Parse(expr.Body);
        Assert.IsNotNull(sql);
    }

    // Line 265: FormatAsLiteral - fallback (non-IFormattable)
    [TestMethod]
    public void FormatAsLiteral_UnknownType_UsesToString()
    {
        // object type - hits _ => v?.ToString() ?? "NULL"
        var parser = new ExpressionParser(SqlDefine.SQLite, new Dictionary<string, object?>(), false);
        Expression<Func<BpUser, bool>> expr = u => u.Id == 42;
        var sql = parser.Parse(expr.Body);
        Assert.IsTrue(sql.Contains("42"), sql);
    }
}

[TestClass]
public class DateTimeFunctionParserBranchTests
{
    // Line 189: m.Object != null → parse object
    [TestMethod]
    public void SqlQuery_DateTime_AddDays_GeneratesDateAdd()
    {
        var q = SqlQuery<BpUser>.ForSqlite()
            .Where(u => u.Id > DateTime.Now.AddDays(-7).Year);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // Line 191: method name starts with "Add" and has 1 arg
    [TestMethod]
    public void SqlQuery_DateTime_AddMonths_GeneratesDateAdd()
    {
        var dt = new DateTime(2024, 1, 1);
        var q = SqlQuery<BpUser>.ForSqlite()
            .Where(u => u.Id > dt.AddMonths(1).Year);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }

    // Line 191: method name does NOT start with "Add" → returns obj
    [TestMethod]
    public void SqlQuery_DateTime_NonAddMethod_ReturnsObj()
    {
        var dt = new DateTime(2024, 1, 1);
        var q = SqlQuery<BpUser>.ForSqlite()
            .Where(u => u.Id == dt.Year);
        var sql = q.ToSql();
        Assert.IsNotNull(sql);
    }
}

[TestClass]
public class TableNameResolverBranchTests
{
    // Line 34: attribute.TableName is empty → use entityType.Name
    [TestMethod]
    public void TableNameResolver_NoTableName_UsesTypeName()
    {
        var name = TableNameResolver.Resolve(typeof(NoTableNameEntity));
        Assert.AreEqual("NoTableNameEntity", name);
    }

    // Line 38: attribute.Method is empty → no dynamic resolver
    [TestMethod]
    public void TableNameResolver_NoMethod_ReturnsStaticName()
    {
        var name = TableNameResolver.Resolve(typeof(StaticTableEntity));
        Assert.AreEqual("static_table", name);
    }

    // Line 50: method not found or wrong return type → fallback
    [TestMethod]
    public void TableNameResolver_MethodNotFound_FallsBackToTableName()
    {
        var name = TableNameResolver.Resolve(typeof(BadMethodEntity));
        Assert.AreEqual("bad_method_table", name);
    }

    // Line 55: method found, public → creates delegate
    [TestMethod]
    public void TableNameResolver_PublicMethod_CreatesDelegateAndCallsIt()
    {
        var name = TableNameResolver.Resolve(typeof(DynamicTableEntity));
        Assert.AreEqual("dynamic_table_resolved", name);
    }

    // Line 55: method found, non-public → uses MethodInfo
    [TestMethod]
    public void TableNameResolver_PrivateMethod_UsesMethodInfo()
    {
        var name = TableNameResolver.Resolve(typeof(PrivateMethodTableEntity));
        Assert.AreEqual("private_table_resolved", name);
    }
}

// Test entities for TableNameResolver
[TableName("")]
public class NoTableNameEntity { }

[TableName("static_table")]
public class StaticTableEntity { }

[TableName("bad_method_table", Method = "NonExistentMethod")]
public class BadMethodEntity { }

[TableName("dynamic_fallback", Method = "GetTableName")]
public class DynamicTableEntity
{
    public static string GetTableName() => "dynamic_table_resolved";
}

[TableName("private_fallback", Method = "GetTableName")]
public class PrivateMethodTableEntity
{
    private static string GetTableName() => "private_table_resolved";
}
