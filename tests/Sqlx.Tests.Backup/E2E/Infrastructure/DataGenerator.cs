// <copyright file="DataGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Generates random test data with deterministic seeding for reproducibility.
/// </summary>
public class DataGenerator : IDataGenerator
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGenerator"/> class.
    /// </summary>
    /// <param name="seed">The seed for deterministic random generation. If null, uses a random seed.</param>
    public DataGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <inheritdoc/>
    public T Generate<T>()
        where T : new()
    {
        return new T();
    }

    /// <inheritdoc/>
    public IEnumerable<T> GenerateMany<T>(int count)
        where T : new()
    {
        for (int i = 0; i < count; i++)
        {
            yield return Generate<T>();
        }
    }

    /// <inheritdoc/>
    public string GenerateString(int minLength, int maxLength)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
        var length = _random.Next(minLength, maxLength + 1);
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }

        return new string(result);
    }

    /// <inheritdoc/>
    public int GenerateInt(int min, int max)
    {
        return _random.Next(min, max + 1);
    }

    /// <inheritdoc/>
    public DateTime GenerateDateTime(DateTime min, DateTime max)
    {
        var range = (max - min).TotalSeconds;
        var randomSeconds = _random.NextDouble() * range;
        return min.AddSeconds(randomSeconds);
    }

    /// <inheritdoc/>
    public decimal GenerateDecimal(decimal min, decimal max)
    {
        var range = max - min;
        var randomValue = (decimal)_random.NextDouble() * range;
        return min + randomValue;
    }

    /// <inheritdoc/>
    public bool GenerateBool()
    {
        return _random.Next(0, 2) == 1;
    }

    /// <inheritdoc/>
    public byte[] GenerateBytes(int length)
    {
        var bytes = new byte[length];
        _random.NextBytes(bytes);
        return bytes;
    }
}
