// <copyright file="SqlxQueryProviderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlx.Tests.QueryBuilder;

/// <summary>
/// Tests for SqlxQueryProvider covering query creation, execution, and SQL generation.
/// </summary>
[TestClass]
public class SqlxQueryProviderTests
{
    private SqlxQueryProvider<TestEntity> _provider = null!;
    private SqlDialect _dialect = null!;
    private TestDbConnection _connection = null!;
    private TestResultReader _resultReader = null!;

    [TestInitialize]
    public void Setup()
    {
        _dialect = new SqlServerDialect();
        var entityProvider = new DynamicEntityProvider<TestEntity>();
        _provider = new SqlxQueryProvider<TestEntity>(_dialect, entityProvider);
        _connection = new TestDbConnection();
        _resultReader = new TestResultReader();
        
        // Set internal properties via reflection
        var connectionProp = typeof(SqlxQueryProvider<TestEntity>).GetProperty("Connection", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        connectionProp!.SetValue(_provider, _connection);
        
        var readerProp = typeof(SqlxQueryProvider<TestEntity>).GetProperty("ResultReader", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        readerProp!.SetValue(_provider, _resultReader);
    }

    [TestMethod]
    public void Constructor_WithDialect_SetsDialect()
    {
        // Arrange & Act
        var provider = new SqlxQueryProvider<TestEntity>(_dialect);

        // Assert
        Assert.AreEqual(_dialect, provider.Dialect);
    }

    [TestMethod]
    public void Constructor_WithDialectAndEntityProvider_SetsProperties()
    {
        // Arrange
        var entityProvider = new DynamicEntityProvider<TestEntity>();

        // Act
        var provider = new SqlxQueryProvider<TestEntity>(_dialect, entityProvider);

        // Assert
        Assert.AreEqual(_dialect, provider.Dialect);
        var entityProviderProp = typeof(SqlxQueryProvider<TestEntity>).GetProperty("EntityProvider", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var actualProvider = entityProviderProp!.GetValue(provider);
        Assert.AreEqual(entityProvider, actualProvider);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithNullDialect_ThrowsArgumentNullException()
    {
        // Act
        _ = new SqlxQueryProvider<TestEntity>(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void CreateQuery_NonGeneric_ThrowsNotSupportedException()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new TestEntity() }.AsQueryable());

        // Act
        _provider.CreateQuery(expression);
    }

    [TestMethod]
    public void CreateQuery_SameType_ReturnsQueryable()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new TestEntity() }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<TestEntity>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<TestEntity>));
    }

    [TestMethod]
    public void CreateQuery_DifferentType_CreatesNewProvider()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new ProjectedEntity() }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<ProjectedEntity>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<ProjectedEntity>));
    }

    [TestMethod]
    public void CreateQuery_PrimitiveType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { 1 }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<int>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_StringType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { "test" }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<string>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_DecimalType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { 1.5m }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<decimal>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_DateTimeType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { DateTime.Now }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<DateTime>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_GuidType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { Guid.NewGuid() }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<Guid>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_TimeSpanType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { TimeSpan.Zero }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<TimeSpan>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_DateTimeOffsetType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { DateTimeOffset.UtcNow }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<DateTimeOffset>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_DateOnlyType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new DateOnly(2024, 2, 20) }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<DateOnly>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void CreateQuery_TimeOnlyType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new TimeOnly(14, 30, 0) }.AsQueryable());

        // Act
        var result = _provider.CreateQuery<TimeOnly>(expression);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Execute_NonGeneric_ThrowsNotSupportedException()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new TestEntity() }.AsQueryable());

        // Act
        _provider.Execute(expression);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Execute_WithoutConnection_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = new SqlxQueryProvider<TestEntity>(_dialect);
        var queryable = new[] { new TestEntity() }.AsQueryable();
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.First),
            new[] { typeof(TestEntity) },
            queryable.Expression);

        // Act
        provider.Execute<TestEntity>(expression);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Execute_NonMethodCallExpression_ThrowsNotSupportedException()
    {
        // Arrange
        var expression = Expression.Constant(42);

        // Act
        _provider.Execute<int>(expression);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Execute_UnsupportedMethod_ThrowsNotSupportedException()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Any),
            new[] { typeof(TestEntity) },
            queryable.Expression);

        // Act
        _provider.Execute<bool>(expression);
    }

    [TestMethod]
    public void ToSql_SimpleExpression_GeneratesSql()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();

        // Act
        var sql = _provider.ToSql(queryable.Expression);

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void ToSql_WithParameterization_GeneratesParameterizedSql()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();

        // Act
        var sql = _provider.ToSql(queryable.Expression, parameterized: true);

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void ToSqlWithParameters_ReturnsSqlAndParameters()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();

        // Act
        var (sql, parameters) = _provider.ToSqlWithParameters(queryable.Expression);

        // Assert
        Assert.IsNotNull(sql);
        Assert.IsNotNull(parameters);
    }

    [TestMethod]
    public void CreateQuery_IGroupingType_DoesNotCreateDynamicReader()
    {
        // Arrange
        var groupingType = typeof(IGrouping<int, TestEntity>);
        var createQueryMethod = typeof(SqlxQueryProvider<TestEntity>)
            .GetMethod(nameof(IQueryProvider.CreateQuery), 1, new[] { typeof(Expression) });
        var genericMethod = createQueryMethod!.MakeGenericMethod(groupingType);
        var expression = Expression.Constant(Enumerable.Empty<IGrouping<int, TestEntity>>().AsQueryable());

        // Act
        var result = genericMethod.Invoke(_provider, new object[] { expression });

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ExtractColumnExpression_WithoutArguments_ReturnsAsterisk()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Min),
            new[] { typeof(TestEntity) },
            queryable.Expression);

        // Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("ExtractColumnExpression",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { expression })!;

        // Assert
        Assert.AreEqual("*", result);
    }

    [TestMethod]
    public void ExtractColumnExpression_WithMemberExpression_ReturnsWrappedColumn()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();
        Expression<Func<TestEntity, int>> selector = x => x.Id;
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Min),
            new[] { typeof(TestEntity), typeof(int) },
            queryable.Expression,
            Expression.Quote(selector));

        // Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("ExtractColumnExpression",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { expression })!;

        // Assert
        Assert.IsTrue(result.Contains("id") || result.Contains("Id"));
    }

    [TestMethod]
    public void ExtractColumnExpression_WithQuotedExpression_UnwrapsAndProcesses()
    {
        // Arrange
        var queryable = new[] { new TestEntity() }.AsQueryable();
        Expression<Func<TestEntity, string>> selector = x => x.Name;
        var quotedExpression = Expression.Quote(selector);
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Max),
            new[] { typeof(TestEntity), typeof(string) },
            queryable.Expression,
            quotedExpression);

        // Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("ExtractColumnExpression",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { expression })!;

        // Assert
        Assert.IsTrue(result.Contains("name") || result.Contains("Name"));
    }

    [TestMethod]
    public void ExtractColumnExpression_WithColumnAttribute_UsesMappedColumnName()
    {
        // Arrange
        var provider = new SqlxQueryProvider<ColumnMappedAggregateEntity>(
            _dialect,
            new DynamicEntityProvider<ColumnMappedAggregateEntity>());
        var queryable = new[] { new ColumnMappedAggregateEntity() }.AsQueryable();
        Expression<Func<ColumnMappedAggregateEntity, int>> selector = x => x.Score;
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Max),
            new[] { typeof(ColumnMappedAggregateEntity), typeof(int) },
            queryable.Expression,
            Expression.Quote(selector));

        // Act
        var method = typeof(SqlxQueryProvider<ColumnMappedAggregateEntity>).GetMethod(
            "ExtractColumnExpression",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(provider, new object[] { expression })!;

        // Assert
        Assert.IsTrue(result.Contains("custom_score"), result);
        Assert.IsFalse(result.Contains("Score"), result);
    }

    [TestMethod]
    public void ExtractColumnExpression_WithoutEntityProvider_FallsBackToSnakeCase()
    {
        // Arrange
        var provider = new SqlxQueryProvider<FallbackAggregateEntity>(_dialect);
        var queryable = new[] { new FallbackAggregateEntity() }.AsQueryable();
        Expression<Func<FallbackAggregateEntity, int>> selector = x => x.TotalScore;
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Max),
            new[] { typeof(FallbackAggregateEntity), typeof(int) },
            queryable.Expression,
            Expression.Quote(selector));

        // Act
        var method = typeof(SqlxQueryProvider<FallbackAggregateEntity>).GetMethod(
            "ExtractColumnExpression",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(provider, new object[] { expression })!;

        // Assert
        Assert.IsTrue(result.Contains("total_score"), result);
        Assert.IsFalse(result.Contains("TotalScore"), result);
    }

    [TestMethod]
    public void ExtractColumnExpression_WithProjectionAlias_KeepsAliasName()
    {
        // Arrange
        var provider = new SqlxQueryProvider<ProjectionAliasEntity>(
            _dialect,
            new DynamicEntityProvider<TestEntity>());
        var queryable = new[] { new ProjectionAliasEntity() }.AsQueryable();
        Expression<Func<ProjectionAliasEntity, int>> selector = x => x.TotalScore;
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Max),
            new[] { typeof(ProjectionAliasEntity), typeof(int) },
            queryable.Expression,
            Expression.Quote(selector));

        // Act
        var method = typeof(SqlxQueryProvider<ProjectionAliasEntity>).GetMethod(
            "ExtractColumnExpression",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(provider, new object[] { expression })!;

        // Assert
        Assert.IsTrue(result.Contains("TotalScore"), result);
        Assert.IsFalse(result.Contains("total_score"), result);
    }

    [TestMethod]
    public void GetAggregateFunction_Min_ReturnsMinFunction()
    {
        // Arrange & Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("GetAggregateFunction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { "Min", "test_column" })!;

        // Assert
        Assert.IsTrue(result.Contains("MIN"));
    }

    [TestMethod]
    public void GetAggregateFunction_Max_ReturnsMaxFunction()
    {
        // Arrange & Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("GetAggregateFunction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { "Max", "test_column" })!;

        // Assert
        Assert.IsTrue(result.Contains("MAX"));
    }

    [TestMethod]
    public void GetAggregateFunction_Sum_ReturnsSumFunction()
    {
        // Arrange & Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("GetAggregateFunction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { "Sum", "test_column" })!;

        // Assert
        Assert.IsTrue(result.Contains("SUM"));
    }

    [TestMethod]
    public void GetAggregateFunction_Average_ReturnsAvgFunction()
    {
        // Arrange & Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("GetAggregateFunction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string)method!.Invoke(_provider, new object[] { "Average", "test_column" })!;

        // Assert
        Assert.IsTrue(result.Contains("AVG"));
    }

    [TestMethod]
    [ExpectedException(typeof(TargetInvocationException))]
    public void GetAggregateFunction_UnsupportedFunction_ThrowsNotSupportedException()
    {
        // Arrange & Act - Use reflection to call private method
        var method = typeof(SqlxQueryProvider<TestEntity>).GetMethod("GetAggregateFunction",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // This will throw TargetInvocationException wrapping NotSupportedException
        method!.Invoke(_provider, new object[] { "Unsupported", "test_column" });
    }

    [TestMethod]
    public void DynamicReaderCache_PrimitiveType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { 1 }.AsQueryable());

        // Act - CreateQuery for primitive type should not create dynamic reader
        var result = _provider.CreateQuery<int>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<int>));
    }

    [TestMethod]
    public void DynamicReaderCache_StringType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { "test" }.AsQueryable());

        // Act - CreateQuery for string type should not create dynamic reader
        var result = _provider.CreateQuery<string>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<string>));
    }

    [TestMethod]
    public void DynamicReaderCache_DecimalType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { 1.5m }.AsQueryable());

        // Act - CreateQuery for decimal type should not create dynamic reader
        var result = _provider.CreateQuery<decimal>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<decimal>));
    }

    [TestMethod]
    public void DynamicReaderCache_DateTimeType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { DateTime.Now }.AsQueryable());

        // Act - CreateQuery for DateTime type should not create dynamic reader
        var result = _provider.CreateQuery<DateTime>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<DateTime>));
    }

    [TestMethod]
    public void DynamicReaderCache_GuidType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { Guid.NewGuid() }.AsQueryable());

        // Act - CreateQuery for Guid type should not create dynamic reader
        var result = _provider.CreateQuery<Guid>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<Guid>));
    }

    [TestMethod]
    public void DynamicReaderCache_TimeSpanType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { TimeSpan.Zero }.AsQueryable());

        // Act - CreateQuery for TimeSpan type should not create dynamic reader
        var result = _provider.CreateQuery<TimeSpan>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<TimeSpan>));
    }

    [TestMethod]
    public void DynamicReaderCache_DateTimeOffsetType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { DateTimeOffset.UtcNow }.AsQueryable());

        // Act - CreateQuery for DateTimeOffset type should not create dynamic reader
        var result = _provider.CreateQuery<DateTimeOffset>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<DateTimeOffset>));
    }

    [TestMethod]
    public void DynamicReaderCache_DateOnlyType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new DateOnly(2024, 2, 20) }.AsQueryable());

        // Act - CreateQuery for DateOnly type should not create dynamic reader
        var result = _provider.CreateQuery<DateOnly>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<DateOnly>));
    }

    [TestMethod]
    public void DynamicReaderCache_TimeOnlyType_ShouldNotCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new TimeOnly(14, 30, 0) }.AsQueryable());

        // Act - CreateQuery for TimeOnly type should not create dynamic reader
        var result = _provider.CreateQuery<TimeOnly>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<TimeOnly>));
    }

    [TestMethod]
    public void DynamicReaderCache_IGroupingType_ShouldNotCreate()
    {
        // Arrange
        var groupingType = typeof(IGrouping<int, TestEntity>);
        var createQueryMethod = typeof(SqlxQueryProvider<TestEntity>)
            .GetMethod(nameof(IQueryProvider.CreateQuery), 1, new[] { typeof(Expression) });
        var genericMethod = createQueryMethod!.MakeGenericMethod(groupingType);
        var expression = Expression.Constant(Enumerable.Empty<IGrouping<int, TestEntity>>().AsQueryable());

        // Act - CreateQuery for IGrouping type should not create dynamic reader
        var result = genericMethod.Invoke(_provider, new object[] { expression });

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DynamicReaderCache_CustomType_ShouldCreate()
    {
        // Arrange
        var expression = Expression.Constant(new[] { new ProjectedEntity() }.AsQueryable());

        // Act - CreateQuery for custom type should create dynamic reader
        var result = _provider.CreateQuery<ProjectedEntity>(expression);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SqlxQueryable<ProjectedEntity>));
    }

    // Test entity classes
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ProjectedEntity
    {
        public int Id { get; set; }
    }

    private class ColumnMappedAggregateEntity
    {
        public int Id { get; set; }

        [Column("custom_score")]
        public int Score { get; set; }
    }

    private class FallbackAggregateEntity
    {
        public int TotalScore { get; set; }
    }

    private class ProjectionAliasEntity
    {
        public int TotalScore { get; set; }
    }

    // Test helper classes
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
    private class TestDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "TestDb";
        public override string DataSource => "TestSource";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotImplementedException();

        protected override DbCommand CreateDbCommand()
        {
            return new TestDbCommand { Connection = this };
        }
    }

    private class TestDbCommand : DbCommand
    {
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new TestParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => 0;
        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new TestDbParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new TestDbDataReader();
        }
    }

    private class TestParameterCollection : DbParameterCollection
    {
        private readonly List<object> _parameters = new();

        public override int Count => _parameters.Count;
        public override object SyncRoot => _parameters;

        public override int Add(object value)
        {
            _parameters.Add(value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
                _parameters.Add(value);
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains(value);
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) => _parameters.CopyTo((object[])array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf(value);
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => _parameters.Insert(index, value);
        public override void Remove(object value) => _parameters.Remove(value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) { }

        protected override DbParameter GetParameter(int index) => (DbParameter)_parameters[index];
        protected override DbParameter GetParameter(string parameterName) => throw new NotImplementedException();
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }

    private class TestDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = string.Empty;
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }

        public override void ResetDbType() { }
    }
#pragma warning restore CS8765

    private class TestDbDataReader : DbDataReader
    {
        private int _readCount = 0;

        public override int FieldCount => 2;
        public override int RecordsAffected => 0;
        public override bool HasRows => true;
        public override bool IsClosed => false;
        public override int Depth => 0;
        public override object this[int ordinal] => ordinal == 0 ? 1 : "Test";
        public override object this[string name] => name == "Id" ? 1 : "Test";

        public override bool GetBoolean(int ordinal) => true;
        public override byte GetByte(int ordinal) => 1;
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => 'A';
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => "int";
        public override DateTime GetDateTime(int ordinal) => DateTime.Now;
        public override decimal GetDecimal(int ordinal) => 1.0m;
        public override double GetDouble(int ordinal) => 1.0;
        public override Type GetFieldType(int ordinal) => ordinal == 0 ? typeof(int) : typeof(string);
        public override float GetFloat(int ordinal) => 1.0f;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 1;
        public override int GetInt32(int ordinal) => 1;
        public override long GetInt64(int ordinal) => 1;
        public override string GetName(int ordinal) => ordinal == 0 ? "Id" : "Name";
        public override int GetOrdinal(string name) => name == "Id" ? 0 : 1;
        public override string GetString(int ordinal) => "Test";
        public override object GetValue(int ordinal) => ordinal == 0 ? 1 : "Test";
        public override int GetValues(object[] values) => 2;
        public override bool IsDBNull(int ordinal) => false;
        public override bool NextResult() => false;

        public override bool Read()
        {
            if (_readCount == 0)
            {
                _readCount++;
                return true;
            }
            return false;
        }

        public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class TestResultReader : IResultReader<TestEntity>
    {
        public int PropertyCount => 2;

        public TestEntity Read(IDataReader reader)
        {
            return new TestEntity
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }

        public TestEntity Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            return new TestEntity
            {
                Id = reader.GetInt32(ordinals[0]),
                Name = reader.GetString(ordinals[1])
            };
        }

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = reader.GetOrdinal("Id");
            ordinals[1] = reader.GetOrdinal("Name");
        }
    }
}
