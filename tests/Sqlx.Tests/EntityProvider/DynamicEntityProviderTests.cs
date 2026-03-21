// <copyright file="DynamicEntityProviderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.EntityProvider;

/// <summary>
/// Tests for DynamicEntityProvider internal class covering entity creation, property mapping, and type handling.
/// Uses reflection to access internal generic class.
/// </summary>
[TestClass]
public class DynamicEntityProviderTests
{
    private static Type GetDynamicEntityProviderType(Type entityType)
    {
        var providerType = typeof(SqlxException).Assembly.GetType("Sqlx.DynamicEntityProvider`1")!;
        return providerType.MakeGenericType(entityType);
    }

    private static object CreateProvider(Type entityType)
    {
        var providerType = GetDynamicEntityProviderType(entityType);
        return Activator.CreateInstance(providerType)!;
    }

    [TestMethod]
    public void EntityType_ReturnsCorrectType()
    {
        // Arrange
        var provider = CreateProvider(typeof(SimpleEntity));
        var entityTypeProperty = provider.GetType().GetProperty("EntityType")!;

        // Act
        var entityType = (Type)entityTypeProperty.GetValue(provider)!;

        // Assert
        Assert.AreEqual(typeof(SimpleEntity), entityType);
    }

    [TestMethod]
    public void Columns_SimpleEntity_ReturnsAllProperties()
    {
        // Arrange
        var provider = CreateProvider(typeof(SimpleEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        Assert.AreEqual(3, columnList.Count);
    }

    [TestMethod]
    public void Columns_ConvertsPropertyNamesToSnakeCase()
    {
        // Arrange
        var provider = CreateProvider(typeof(SimpleEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        Assert.AreEqual(3, columnList.Count);
        var columnNames = columnList.Select(c => 
        {
            var prop = c.GetType().GetProperty("Name");
            return prop?.GetValue(c) as string ?? string.Empty;
        }).ToList();
        
        // Debug: print actual column names
        Console.WriteLine($"Actual column names: {string.Join(", ", columnNames)}");
        
        CollectionAssert.Contains(columnNames, "id");
        CollectionAssert.Contains(columnNames, "user_name");
        CollectionAssert.Contains(columnNames, "email");
    }

    [TestMethod]
    public void Columns_MapsDbTypesCorrectly()
    {
        // Arrange
        var provider = CreateProvider(typeof(TypedEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        Assert.AreEqual(15, columnList.Count);
        
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method
#pragma warning disable CS8621 // Nullability of reference types in return type doesn't match the target delegate
        var columnDict = columnList.ToDictionary(
            c => c.GetType().GetProperty("PropertyName")!.GetValue(c) as string,
            c => (DbType)c.GetType().GetProperty("DbType")!.GetValue(c)!);
#pragma warning restore CS8621
#pragma warning restore CS8714

        Assert.AreEqual(DbType.SByte, columnDict["SignedByteValue"]);
        Assert.AreEqual(DbType.UInt16, columnDict["UnsignedShortValue"]);
        Assert.AreEqual(DbType.UInt32, columnDict["UnsignedIntValue"]);
        Assert.AreEqual(DbType.UInt64, columnDict["UnsignedLongValue"]);
        Assert.AreEqual(DbType.Int32, columnDict["IntValue"]);
        Assert.AreEqual(DbType.Int64, columnDict["LongValue"]);
        Assert.AreEqual(DbType.Int16, columnDict["ShortValue"]);
        Assert.AreEqual(DbType.Byte, columnDict["ByteValue"]);
        Assert.AreEqual(DbType.Boolean, columnDict["BoolValue"]);
        Assert.AreEqual(DbType.String, columnDict["StringValue"]);
        Assert.AreEqual(DbType.DateTime, columnDict["DateTimeValue"]);
        Assert.AreEqual(DbType.Decimal, columnDict["DecimalValue"]);
        Assert.AreEqual(DbType.Double, columnDict["DoubleValue"]);
        Assert.AreEqual(DbType.Single, columnDict["FloatValue"]);
        Assert.AreEqual(DbType.Guid, columnDict["GuidValue"]);
    }

    [TestMethod]
    public void Columns_HandlesNullableValueTypes()
    {
        // Arrange
        var provider = CreateProvider(typeof(NullableEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var nullableColumn = columnList.First(c => 
            c.GetType().GetProperty("PropertyName")!.GetValue(c) as string == "NullableInt");
        var isNullable = (bool)nullableColumn.GetType().GetProperty("IsNullable")!.GetValue(nullableColumn)!;
        Assert.IsTrue(isNullable);

        var nonNullableColumn = columnList.First(c => 
            c.GetType().GetProperty("PropertyName")!.GetValue(c) as string == "NonNullableInt");
        var isNotNullable = (bool)nonNullableColumn.GetType().GetProperty("IsNullable")!.GetValue(nonNullableColumn)!;
        Assert.IsFalse(isNotNullable);
    }

    [TestMethod]
    public void Columns_OnlyIncludesReadWriteProperties()
    {
        // Arrange
        var provider = CreateProvider(typeof(PropertyAccessEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var propertyNames = columnList.Select(c => 
            c.GetType().GetProperty("PropertyName")!.GetValue(c) as string).ToList();
        
        Assert.IsTrue(propertyNames.Contains("ReadWrite"));
        Assert.IsFalse(propertyNames.Contains("ReadOnly"));
        Assert.IsFalse(propertyNames.Contains("WriteOnly"));
    }

    [TestMethod]
    public void Columns_HandlesEmptyEntity()
    {
        // Arrange
        var provider = CreateProvider(typeof(EmptyEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        Assert.AreEqual(0, columnList.Count);
    }

    [TestMethod]
    public void Columns_HandlesSingleCharacterProperty()
    {
        // Arrange
        var provider = CreateProvider(typeof(SingleCharEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        Assert.AreEqual(1, columnList.Count, "Should have exactly 1 column");
        var column = columnList.First();
        var columnNameProp = column.GetType().GetProperty("Name");
        var columnName = columnNameProp?.GetValue(column) as string;
        Assert.AreEqual("x", columnName);
    }

    [TestMethod]
    public void Columns_HandlesConsecutiveUpperCase()
    {
        // Arrange
        var provider = CreateProvider(typeof(UpperCaseEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        Assert.AreEqual(2, columnList.Count, "Should have exactly 2 columns");
        var columnNames = columnList.Select(c => 
        {
            var prop = c.GetType().GetProperty("Name");
            return prop?.GetValue(c) as string ?? string.Empty;
        }).ToList();
        
        CollectionAssert.Contains(columnNames, "url");
        CollectionAssert.Contains(columnNames, "http_status");
    }

    [TestMethod]
    public void Columns_HandlesBinaryType()
    {
        // Arrange
        var provider = CreateProvider(typeof(BinaryEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First(c => 
            c.GetType().GetProperty("PropertyName")!.GetValue(c) as string == "Data");
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        Assert.AreEqual(DbType.Binary, dbType);
    }

    [TestMethod]
    public void Columns_HandlesUnknownType()
    {
        // Arrange
        var provider = CreateProvider(typeof(CustomTypeEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First(c => 
            c.GetType().GetProperty("PropertyName")!.GetValue(c) as string == "CustomValue");
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        Assert.AreEqual(DbType.Object, dbType);
    }

    [TestMethod]
    public void Columns_HandlesNullableGuid()
    {
        // Arrange
        var provider = CreateProvider(typeof(NullableGuidEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First();
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        var isNullable = (bool)column.GetType().GetProperty("IsNullable")!.GetValue(column)!;
        
        Assert.AreEqual(DbType.Guid, dbType);
        Assert.IsTrue(isNullable);
    }

    [TestMethod]
    public void Columns_HandlesDateTimeOffset()
    {
        // Arrange
        var provider = CreateProvider(typeof(DateTimeOffsetEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First();
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        Assert.AreEqual(DbType.DateTimeOffset, dbType);
    }

    [TestMethod]
    public void Columns_HandlesTimeSpan()
    {
        // Arrange
        var provider = CreateProvider(typeof(TimeSpanEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First();
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        Assert.AreEqual(DbType.Time, dbType);
    }

    [TestMethod]
    public void Columns_HandlesDateOnly()
    {
        // Arrange
        var provider = CreateProvider(typeof(DateOnlyEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First();
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        Assert.AreEqual(DbType.Date, dbType);
    }

    [TestMethod]
    public void Columns_HandlesTimeOnly()
    {
        // Arrange
        var provider = CreateProvider(typeof(TimeOnlyEntity));
        var columnsProperty = provider.GetType().GetProperty("Columns")!;

        // Act
        var columns = (System.Collections.IEnumerable)columnsProperty.GetValue(provider)!;
        var columnList = columns.Cast<object>().ToList();

        // Assert
        var column = columnList.First();
        var dbType = (DbType)column.GetType().GetProperty("DbType")!.GetValue(column)!;
        Assert.AreEqual(DbType.Time, dbType);
    }

    // Test entity classes
    private class SimpleEntity
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class TypedEntity
    {
        public sbyte SignedByteValue { get; set; }
        public ushort UnsignedShortValue { get; set; }
        public uint UnsignedIntValue { get; set; }
        public ulong UnsignedLongValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public short ShortValue { get; set; }
        public byte ByteValue { get; set; }
        public bool BoolValue { get; set; }
        public string StringValue { get; set; } = string.Empty;
        public DateTime DateTimeValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        public Guid GuidValue { get; set; }
    }

    private class NullableEntity
    {
        public int? NullableInt { get; set; }
        public int NonNullableInt { get; set; }
    }

    private class PropertyAccessEntity
    {
        public int ReadWrite { get; set; }
        public int ReadOnly { get; }
        public int WriteOnly { set { } }
    }

    private class EmptyEntity
    {
    }

    private class SingleCharEntity
    {
        public int X { get; set; }
    }

    private class UpperCaseEntity
    {
        public string URL { get; set; } = string.Empty;
        public int HTTPStatus { get; set; }
    }

    private class BinaryEntity
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    private class CustomTypeEntity
    {
        public CustomType CustomValue { get; set; } = new CustomType();
    }

    private class CustomType
    {
    }

    private class NullableGuidEntity
    {
        public Guid? Id { get; set; }
    }

    private class DateTimeOffsetEntity
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    private class TimeSpanEntity
    {
        public TimeSpan Duration { get; set; }
    }

    private class DateOnlyEntity
    {
        public DateOnly ScheduledOn { get; set; }
    }

    private class TimeOnlyEntity
    {
        public TimeOnly StartsAt { get; set; }
    }
}
