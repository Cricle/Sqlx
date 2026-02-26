// <copyright file="BinaryDataGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Generates binary data for E2E testing.
/// </summary>
public static class BinaryDataGenerator
{
    public static byte[] GenerateEmpty() => Array.Empty<byte>();

    public static byte[] GenerateSmall(int size)
    {
        var data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = (byte)(i % 256);
        }
        return data;
    }

    public static byte[] GenerateMedium()
    {
        // Generate 64KB of data
        var data = new byte[65536];
        var random = new Random(42); // Fixed seed for reproducibility
        random.NextBytes(data);
        return data;
    }

    public static byte[] GenerateAllBytes()
    {
        var data = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            data[i] = (byte)i;
        }
        return data;
    }
}
