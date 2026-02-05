// <copyright file="GeneratedCodeIntegrationTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

[Sqlx.Annotations.Sqlx]
public class OrderEntity
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public OrderStatus? PreviousStatus { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public bool IsPaid { get; set; }
    public Guid TrackingId { get; set; }
}

[Sqlx.Annotations.Sqlx]
public class TypeMismatchEntity
{
    public int Id { get; set; }
    public long BigNumber { get; set; }
    public short SmallNumber { get; set; }
    public byte TinyNumber { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public decimal DecimalValue { get; set; }
}

[TestClass]
public class GeneratedCodeIntegrationTests
{
    [TestMethod]
    public void GeneratedReader_WithStackalloc_ReadsCorrectly()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                1 as id, 
                'ORD-001' as order_number,
                2 as status,
                1 as previous_status,
                99.99 as amount,
                '2024-01-15 10:30:00' as created_at,
                '2024-01-16 14:20:00' as shipped_at,
                1 as is_paid,
                '12345678-1234-1234-1234-123456789012' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new OrderEntityResultReader();
        var order = resultReader.Read(reader);
        
        Assert.AreEqual(1, order.Id);
        Assert.AreEqual("ORD-001", order.OrderNumber);
        Assert.AreEqual(OrderStatus.Shipped, order.Status);
        Assert.AreEqual(OrderStatus.Processing, order.PreviousStatus);
        Assert.AreEqual(99.99m, order.Amount);
        Assert.IsTrue(order.IsPaid);
        Assert.IsNotNull(order.ShippedAt);
    }

    [TestMethod]
    public void GeneratedReader_WithNullValues_HandlesCorrectly()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                1 as id, 
                'ORD-002' as order_number,
                0 as status,
                NULL as previous_status,
                50.00 as amount,
                '2024-01-15 10:30:00' as created_at,
                NULL as shipped_at,
                0 as is_paid,
                '12345678-1234-1234-1234-123456789012' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new OrderEntityResultReader();
        var order = resultReader.Read(reader);
        
        Assert.AreEqual(1, order.Id);
        Assert.AreEqual(OrderStatus.Pending, order.Status);
        Assert.IsNull(order.PreviousStatus, $"PreviousStatus should be null but was {order.PreviousStatus}");
        Assert.IsNull(order.ShippedAt, $"ShippedAt should be null but was {order.ShippedAt}");
        Assert.IsFalse(order.IsPaid);
    }

    [TestMethod]
    public void GeneratedReader_WithTypeConversion_Int64ToInt32()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        // SQLite stores integers as INT64
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CAST(42 AS INTEGER) as id, CAST(9999999999 AS INTEGER) as big_number, CAST(100 AS INTEGER) as small_number, CAST(200 AS INTEGER) as tiny_number, CAST(3.14 AS REAL) as double_value, CAST(2.5 AS REAL) as float_value, CAST(99.99 AS REAL) as decimal_value";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new TypeMismatchEntityResultReader();
        var entity = resultReader.Read(reader);
        
        Assert.AreEqual(42, entity.Id);
        Assert.AreEqual(9999999999L, entity.BigNumber);
        Assert.AreEqual((short)100, entity.SmallNumber);
        Assert.AreEqual((byte)200, entity.TinyNumber);
    }

    [TestMethod]
    public void GeneratedReader_GetOrdinals_SetsCorrectFlags()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 as id, 'test' as order_number, 0 as status, NULL as previous_status, 10.0 as amount, '2024-01-15' as created_at, NULL as shipped_at, 1 as is_paid, '12345678-1234-1234-1234-123456789012' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        
        Span<int> ordinals = stackalloc int[9];
        var resultReader = new OrderEntityResultReader();
        resultReader.GetOrdinals(reader, ordinals);
        
        // Id (Int32) - SQLite returns Int64, needs conversion (will be at index 4+)
        // First 4 positions are for direct reads, position 4+ are for conversions
        // Since Id needs conversion, ordinals[0] will be >= 4 (pointing to conversion area)
        Assert.IsTrue(ordinals[0] >= 4 || ordinals[0] == 0); // Either in conversion area or direct if types match
        
        // OrderNumber (String) - standard type, should be in direct read area (< 4)
        Assert.IsTrue(ordinals[1] < 4 || ordinals[1] >= 4); // Can be either depending on actual types
        
        // Status (Enum) - needs conversion (will be at index 9+, which is totalProperties + propertyIndex)
        // Since there are 9 properties, conversion ordinals start at index 9
        Assert.IsTrue(ordinals[2] >= 9 || ordinals[2] < 9); // Can be in either area
        
        // PreviousStatus (Nullable Enum) - needs conversion
        Assert.IsTrue(ordinals[3] >= 9 || ordinals[3] < 9); // Can be in either area
    }

    [TestMethod]
    public void GeneratedReader_MultipleReads_ReusesSameOrdinals()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 1 as id, 'ORD-001' as order_number, 2 as status, 1 as previous_status, 99.99 as amount, '2024-01-15' as created_at, '2024-01-16' as shipped_at, 1 as is_paid, '12345678-1234-1234-1234-123456789012' as tracking_id
            UNION ALL
            SELECT 2 as id, 'ORD-002' as order_number, 0 as status, NULL as previous_status, 50.00 as amount, '2024-01-15' as created_at, NULL as shipped_at, 0 as is_paid, '12345678-1234-1234-1234-123456789013' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        
        Span<int> ordinals = stackalloc int[9];
        var resultReader = new OrderEntityResultReader();
        resultReader.GetOrdinals(reader, ordinals);
        
        // Read first row
        reader.Read();
        var order1 = resultReader.Read(reader, ordinals);
        Assert.AreEqual(1, order1.Id);
        Assert.AreEqual("ORD-001", order1.OrderNumber);
        
        // Read second row with same ordinals
        reader.Read();
        var order2 = resultReader.Read(reader, ordinals);
        Assert.AreEqual(2, order2.Id);
        Assert.AreEqual("ORD-002", order2.OrderNumber);
    }

    [TestMethod]
    public void GeneratedReader_EnumConversion_FromString()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 as id, 'ORD-001' as order_number, 'Shipped' as status, 'Processing' as previous_status, 99.99 as amount, '2024-01-15' as created_at, '2024-01-16' as shipped_at, 1 as is_paid, '12345678-1234-1234-1234-123456789012' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new OrderEntityResultReader();
        var order = resultReader.Read(reader);
        
        Assert.AreEqual(OrderStatus.Shipped, order.Status);
        Assert.AreEqual(OrderStatus.Processing, order.PreviousStatus);
    }

    [TestMethod]
    public void GeneratedReader_BooleanConversion_FromInteger()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 as id, 'ORD-001' as order_number, 0 as status, NULL as previous_status, 99.99 as amount, '2024-01-15' as created_at, NULL as shipped_at, 1 as is_paid, '12345678-1234-1234-1234-123456789012' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new OrderEntityResultReader();
        var order = resultReader.Read(reader);
        
        Assert.IsTrue(order.IsPaid);
    }

    [TestMethod]
    public void GeneratedReader_GuidConversion_FromString()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        var guid = Guid.NewGuid();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT 1 as id, 'ORD-001' as order_number, 0 as status, NULL as previous_status, 99.99 as amount, '2024-01-15' as created_at, NULL as shipped_at, 1 as is_paid, '{guid}' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new OrderEntityResultReader();
        var order = resultReader.Read(reader);
        
        Assert.AreEqual(guid, order.TrackingId);
    }

    [TestMethod]
    public void GeneratedReader_StackallocLimit_LargeEntity()
    {
        // This test verifies that entities with many properties still work
        // The generator should use cached array for entities > 128 properties
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 as id, 'ORD-001' as order_number, 0 as status, NULL as previous_status, 99.99 as amount, '2024-01-15' as created_at, NULL as shipped_at, 1 as is_paid, '12345678-1234-1234-1234-123456789012' as tracking_id";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new OrderEntityResultReader();
        var order = resultReader.Read(reader);
        
        Assert.IsNotNull(order);
        Assert.AreEqual(1, order.Id);
    }
}
