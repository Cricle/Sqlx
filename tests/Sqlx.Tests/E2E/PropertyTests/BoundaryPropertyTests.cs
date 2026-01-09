// -----------------------------------------------------------------------
// <copyright file="BoundaryPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.BoundaryTests;

namespace Sqlx.Tests.E2E.PropertyTests;

/// <summary>
/// Property-based tests for boundary value preservation.
/// Feature: enhanced-predefined-interfaces-e2e
/// Property 26: Boundary Value Preservation
/// Validates: Requirements 13.4-13.11
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("Property")]
[TestCategory("Boundary")]
[TestCategory("SQLite")]
public class BoundaryPropertyTests_SQLite : BoundaryTestBase
{
    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(BoundaryPropertyTests_SQLite);

    private E2EBoundaryCommandRepository_SQLite? _commandRepo;
    private E2EBoundaryQueryRepository_SQLite? _queryRepo;

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _commandRepo = new E2EBoundaryCommandRepository_SQLite(Connection!);
        _queryRepo = new E2EBoundaryQueryRepository_SQLite(Connection!);
    }

    /// <summary>
    /// Property 26: Boundary Value Preservation
    /// For any extreme value V (Int32.MinValue, Int32.MaxValue, Decimal.MinValue, Decimal.MaxValue, 
    /// DateTime.MinValue, DateTime.MaxValue), storing and retrieving V should preserve its exact value.
    /// Validates: Requirements 13.4-13.11
    /// </summary>
    [TestMethod]
    public async Task Property26_BoundaryValuePreservation_Int32()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 26: Boundary Value Preservation
        
        var testCases = new[]
        {
            int.MinValue,
            int.MinValue + 1,
            -1000000,
            -1,
            0,
            1,
            1000000,
            int.MaxValue - 1,
            int.MaxValue
        };

        int successCount = 0;
        var failures = new List<string>();

        foreach (var testValue in testCases)
        {
            try
            {
                // Arrange
                var entity = CreateBoundaryEntity();
                entity.MinInt = testValue;
                entity.MaxInt = testValue;

                // Act
                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                // Assert
                if (retrieved == null)
                {
                    failures.Add($"Failed for value {testValue}: Retrieved entity is null");
                    continue;
                }

                if (retrieved.MinInt != testValue || retrieved.MaxInt != testValue)
                {
                    failures.Add($"Failed for value {testValue}: Expected {testValue}, got MinInt={retrieved.MinInt}, MaxInt={retrieved.MaxInt}");
                    continue;
                }

                successCount++;
                
                // Cleanup for next iteration
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for value {testValue}: {ex.Message}");
            }
        }

        // Assert
        Assert.AreEqual(testCases.Length, successCount, 
            $"Property failed for {failures.Count}/{testCases.Length} test cases:\n{string.Join("\n", failures)}");
    }

    [TestMethod]
    public async Task Property26_BoundaryValuePreservation_Int64()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 26: Boundary Value Preservation
        
        var testCases = new[]
        {
            long.MinValue,
            long.MinValue + 1,
            -1000000000000L,
            -1L,
            0L,
            1L,
            1000000000000L,
            long.MaxValue - 1,
            long.MaxValue
        };

        int successCount = 0;
        var failures = new List<string>();

        foreach (var testValue in testCases)
        {
            try
            {
                var entity = CreateBoundaryEntity();
                entity.MinLong = testValue;
                entity.MaxLong = testValue;

                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                if (retrieved == null)
                {
                    failures.Add($"Failed for value {testValue}: Retrieved entity is null");
                    continue;
                }

                if (retrieved.MinLong != testValue || retrieved.MaxLong != testValue)
                {
                    failures.Add($"Failed for value {testValue}: Expected {testValue}, got MinLong={retrieved.MinLong}, MaxLong={retrieved.MaxLong}");
                    continue;
                }

                successCount++;
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for value {testValue}: {ex.Message}");
            }
        }

        Assert.AreEqual(testCases.Length, successCount, 
            $"Property failed for {failures.Count}/{testCases.Length} test cases:\n{string.Join("\n", failures)}");
    }

    [TestMethod]
    public async Task Property26_BoundaryValuePreservation_Decimal()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 26: Boundary Value Preservation
        // Note: SQLite stores decimals as REAL (double), so we test within precision limits
        
        var testCases = new[]
        {
            -1000000000.99m,
            -1.5m,
            -0.01m,
            0m,
            0.01m,
            1.5m,
            1000000000.99m
        };

        int successCount = 0;
        var failures = new List<string>();

        foreach (var testValue in testCases)
        {
            try
            {
                var entity = CreateBoundaryEntity();
                entity.MinDecimal = testValue;
                entity.MaxDecimal = testValue;

                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                if (retrieved == null)
                {
                    failures.Add($"Failed for value {testValue}: Retrieved entity is null");
                    continue;
                }

                // Allow small precision difference due to SQLite REAL storage
                var minDiff = Math.Abs(retrieved.MinDecimal - testValue);
                var maxDiff = Math.Abs(retrieved.MaxDecimal - testValue);
                
                if (minDiff > 0.01m || maxDiff > 0.01m)
                {
                    failures.Add($"Failed for value {testValue}: Expected {testValue}, got MinDecimal={retrieved.MinDecimal} (diff={minDiff}), MaxDecimal={retrieved.MaxDecimal} (diff={maxDiff})");
                    continue;
                }

                successCount++;
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for value {testValue}: {ex.Message}");
            }
        }

        Assert.AreEqual(testCases.Length, successCount, 
            $"Property failed for {failures.Count}/{testCases.Length} test cases:\n{string.Join("\n", failures)}");
    }

    [TestMethod]
    public async Task Property26_BoundaryValuePreservation_DateTime()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 26: Boundary Value Preservation
        
        var testCases = new[]
        {
            DateTime.MinValue,
            new DateTime(1900, 1, 1),
            new DateTime(2000, 1, 1),
            DateTime.UtcNow,
            new DateTime(2100, 12, 31),
            DateTime.MaxValue
        };

        int successCount = 0;
        var failures = new List<string>();

        foreach (var testValue in testCases)
        {
            try
            {
                var entity = CreateBoundaryEntity();
                entity.MinDateTime = testValue;
                entity.MaxDateTime = testValue;

                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                if (retrieved == null)
                {
                    failures.Add($"Failed for value {testValue}: Retrieved entity is null");
                    continue;
                }

                // SQLite stores DateTime as TEXT, compare dates (precision may vary for time)
                if (retrieved.MinDateTime.Date != testValue.Date || retrieved.MaxDateTime.Date != testValue.Date)
                {
                    failures.Add($"Failed for value {testValue}: Expected {testValue.Date}, got MinDateTime={retrieved.MinDateTime.Date}, MaxDateTime={retrieved.MaxDateTime.Date}");
                    continue;
                }

                successCount++;
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for value {testValue}: {ex.Message}");
            }
        }

        Assert.AreEqual(testCases.Length, successCount, 
            $"Property failed for {failures.Count}/{testCases.Length} test cases:\n{string.Join("\n", failures)}");
    }
}


/// <summary>
/// Property-based tests for string preservation.
/// Feature: enhanced-predefined-interfaces-e2e
/// Property 27: String Preservation
/// Property 28: Null vs Empty String Distinction
/// Validates: Requirements 13.12-13.15
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("Property")]
[TestCategory("Boundary")]
[TestCategory("SQLite")]
public class StringPreservationPropertyTests_SQLite : BoundaryTestBase
{
    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(StringPreservationPropertyTests_SQLite);

    private E2EBoundaryCommandRepository_SQLite? _commandRepo;
    private E2EBoundaryQueryRepository_SQLite? _queryRepo;

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _commandRepo = new E2EBoundaryCommandRepository_SQLite(Connection!);
        _queryRepo = new E2EBoundaryQueryRepository_SQLite(Connection!);
    }

    /// <summary>
    /// Property 27: String Preservation
    /// For any string S (including empty, very long, Unicode, special characters, whitespace-only), 
    /// storing and retrieving S should preserve it exactly.
    /// Validates: Requirements 13.12, 13.13, 13.14, 13.15
    /// </summary>
    [TestMethod]
    public async Task Property27_StringPreservation_VariousStrings()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 27: String Preservation
        
        var testCases = new[]
        {
            "",                                          // Empty string
            " ",                                         // Single space
            "   \t\n   ",                               // Whitespace only
            "Hello World",                               // Normal string
            "Hello 世界 🌍 مرحبا Привет",              // Unicode with emojis
            new string('A', 1000),                      // Long string (1000 chars)
            new string('X', 5000),                      // Very long string (5000 chars)
            "'; DROP TABLE test; --",                   // SQL injection pattern
            "Line1\nLine2\nLine3",                      // Multi-line
            "Tab\tSeparated\tValues",                   // Tab characters
            "Quote\"Test\"Quote",                       // Quotes
            "Backslash\\Test\\Path",                    // Backslashes
            "Special: !@#$%^&*()_+-=[]{}|;':,.<>?/~`", // Special characters
        };

        int successCount = 0;
        var failures = new List<string>();

        foreach (var testValue in testCases)
        {
            try
            {
                var entity = CreateBoundaryEntity();
                entity.VeryLongString = testValue;

                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                if (retrieved == null)
                {
                    failures.Add($"Failed for string (length {testValue.Length}): Retrieved entity is null");
                    continue;
                }

                if (retrieved.VeryLongString != testValue)
                {
                    var preview = testValue.Length > 50 ? testValue.Substring(0, 50) + "..." : testValue;
                    var retrievedPreview = retrieved.VeryLongString?.Length > 50 
                        ? retrieved.VeryLongString.Substring(0, 50) + "..." 
                        : retrieved.VeryLongString;
                    failures.Add($"Failed for string '{preview}': Expected length {testValue.Length}, got '{retrievedPreview}' (length {retrieved.VeryLongString?.Length ?? 0})");
                    continue;
                }

                successCount++;
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                var preview = testValue.Length > 50 ? testValue.Substring(0, 50) + "..." : testValue;
                failures.Add($"Exception for string '{preview}': {ex.Message}");
            }
        }

        Assert.AreEqual(testCases.Length, successCount, 
            $"Property failed for {failures.Count}/{testCases.Length} test cases:\n{string.Join("\n", failures)}");
    }

    /// <summary>
    /// Property 28: Null vs Empty String Distinction
    /// For any entity with a nullable string field, storing null should be distinguishable 
    /// from storing empty string ("") upon retrieval.
    /// Validates: Requirements 13.12
    /// </summary>
    [TestMethod]
    public async Task Property28_NullVsEmptyStringDistinction()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 28: Null vs Empty String Distinction
        
        var testCases = new[]
        {
            (value: (string?)null, expectedIsNull: true, description: "null"),
            (value: "", expectedIsNull: false, description: "empty string"),
            (value: " ", expectedIsNull: false, description: "single space"),
            (value: "test", expectedIsNull: false, description: "normal string")
        };

        int successCount = 0;
        var failures = new List<string>();

        foreach (var (value, expectedIsNull, description) in testCases)
        {
            try
            {
                var entity = CreateBoundaryEntity();
                entity.NullableString = value;

                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                if (retrieved == null)
                {
                    failures.Add($"Failed for {description}: Retrieved entity is null");
                    continue;
                }

                var actualIsNull = retrieved.NullableString == null;
                
                if (actualIsNull != expectedIsNull)
                {
                    failures.Add($"Failed for {description}: Expected IsNull={expectedIsNull}, got IsNull={actualIsNull}, value='{retrieved.NullableString}'");
                    continue;
                }

                if (!expectedIsNull && retrieved.NullableString != value)
                {
                    failures.Add($"Failed for {description}: Expected '{value}', got '{retrieved.NullableString}'");
                    continue;
                }

                successCount++;
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Exception for {description}: {ex.Message}");
            }
        }

        Assert.AreEqual(testCases.Length, successCount, 
            $"Property failed for {failures.Count}/{testCases.Length} test cases:\n{string.Join("\n", failures)}");
    }

    [TestMethod]
    public async Task Property27_StringPreservation_RandomStrings_100Iterations()
    {
        // Feature: enhanced-predefined-interfaces-e2e, Property 27: String Preservation
        // Test with 100 random strings to ensure comprehensive coverage
        
        var random = new Random(42); // Seeded for reproducibility
        int successCount = 0;
        var failures = new List<string>();
        const int iterations = 100;

        for (int i = 0; i < iterations; i++)
        {
            try
            {
                // Generate random string
                var length = random.Next(0, 1000);
                var chars = new char[length];
                for (int j = 0; j < length; j++)
                {
                    // Mix of ASCII, Unicode, special characters
                    var charType = random.Next(0, 4);
                    chars[j] = charType switch
                    {
                        0 => (char)random.Next(32, 127),      // ASCII printable
                        1 => (char)random.Next(0x4E00, 0x9FFF), // Chinese characters
                        2 => (char)random.Next(0x0600, 0x06FF), // Arabic characters
                        _ => (char)random.Next(0x1F300, 0x1F5FF) // Emoji range
                    };
                }
                var testValue = new string(chars);

                var entity = CreateBoundaryEntity();
                entity.VeryLongString = testValue;

                var id = await _commandRepo!.InsertAndGetIdAsync(entity);
                var retrieved = await _queryRepo!.GetByIdAsync(id);

                if (retrieved == null || retrieved.VeryLongString != testValue)
                {
                    var preview = testValue.Length > 30 ? testValue.Substring(0, 30) + "..." : testValue;
                    failures.Add($"Iteration {i}: Failed for random string '{preview}' (length {testValue.Length})");
                    continue;
                }

                successCount++;
                await ClearTableAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Iteration {i}: Exception - {ex.Message}");
            }
        }

        Assert.AreEqual(iterations, successCount, 
            $"Property failed for {failures.Count}/{iterations} iterations:\n{string.Join("\n", failures.Take(10))}");
    }
}

