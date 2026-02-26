// <copyright file="NumericDataGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Generates numeric boundary values for E2E testing.
/// </summary>
public static class NumericDataGenerator
{
    public static int GetMinInt() => int.MinValue;

    public static int GetMaxInt() => int.MaxValue;

    public static long GetMinBigInt() => long.MinValue;

    public static long GetMaxBigInt() => long.MaxValue;

    public static int GetZeroInt() => 0;

    public static long GetZeroBigInt() => 0L;

    public static decimal GetZeroDecimal() => 0m;

    public static int GetNegativeInt() => -12345;

    public static long GetNegativeBigInt() => -9876543210L;

    public static decimal GetNegativeDecimal() => -123.456m;

    public static decimal GetHighPrecisionDecimal() => 123456789012345.1234567890m;
}
