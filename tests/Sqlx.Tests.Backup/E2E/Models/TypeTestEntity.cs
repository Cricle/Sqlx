// <copyright file="TypeTestEntity.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.Models;

/// <summary>
/// Test entity with all supported data types for type conversion testing.
/// </summary>
[Sqlx]
public class TypeTestEntity
{
    public long Id { get; set; }
    
    // String types
    public string StringValue { get; set; } = string.Empty;
    public string? NullableStringValue { get; set; }
    
    // Numeric types
    public int IntValue { get; set; }
    public int? NullableIntValue { get; set; }
    public long LongValue { get; set; }
    public long? NullableLongValue { get; set; }
    public decimal DecimalValue { get; set; }
    public decimal? NullableDecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public double? NullableDoubleValue { get; set; }
    public float FloatValue { get; set; }
    public float? NullableFloatValue { get; set; }
    
    // DateTime types
    public DateTime DateTimeValue { get; set; }
    public DateTime? NullableDateTimeValue { get; set; }
    
    // Boolean types
    public bool BoolValue { get; set; }
    public bool? NullableBoolValue { get; set; }
    
    // Binary types
    public byte[]? BinaryValue { get; set; }
    
    // Guid types
    public Guid GuidValue { get; set; }
    public Guid? NullableGuidValue { get; set; }
}
