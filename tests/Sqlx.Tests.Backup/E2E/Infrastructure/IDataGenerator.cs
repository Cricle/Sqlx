// <copyright file="IDataGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Generates random test data for property-based testing.
/// </summary>
public interface IDataGenerator
{
    /// <summary>
    /// Generates a single instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to generate.</typeparam>
    /// <returns>A new instance with random data.</returns>
    T Generate<T>()
        where T : new();

    /// <summary>
    /// Generates multiple instances of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to generate.</typeparam>
    /// <param name="count">The number of instances to generate.</param>
    /// <returns>A collection of instances with random data.</returns>
    IEnumerable<T> GenerateMany<T>(int count)
        where T : new();

    /// <summary>
    /// Generates a random string.
    /// </summary>
    /// <param name="minLength">The minimum length.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>A random string.</returns>
    string GenerateString(int minLength, int maxLength);

    /// <summary>
    /// Generates a random integer.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random integer.</returns>
    int GenerateInt(int min, int max);

    /// <summary>
    /// Generates a random DateTime.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random DateTime.</returns>
    DateTime GenerateDateTime(DateTime min, DateTime max);

    /// <summary>
    /// Generates a random decimal.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random decimal.</returns>
    decimal GenerateDecimal(decimal min, decimal max);

    /// <summary>
    /// Generates a random boolean.
    /// </summary>
    /// <returns>A random boolean.</returns>
    bool GenerateBool();

    /// <summary>
    /// Generates a random byte array.
    /// </summary>
    /// <param name="length">The length of the array.</param>
    /// <returns>A random byte array.</returns>
    byte[] GenerateBytes(int length);
}
