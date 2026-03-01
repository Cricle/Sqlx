// <copyright file="SqlAssertions.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sqlx.Tests.Helpers;

/// <summary>
/// Custom assertions for SQL-related validations.
/// Provides reusable assertion methods to reduce code duplication in tests.
/// </summary>
public static class SqlAssertions
{
    /// <summary>
    /// Asserts that SQL contains expected text (case-insensitive).
    /// </summary>
    /// <param name="actualSql">The actual SQL string.</param>
    /// <param name="expectedFragment">The expected fragment to find.</param>
    public static void AssertSqlContains(string actualSql, string expectedFragment)
    {
        Assert.IsTrue(
            actualSql.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase),
            $"Expected SQL to contain '{expectedFragment}' but got: {actualSql}");
    }

    /// <summary>
    /// Asserts that SQL matches expected pattern with normalized whitespace.
    /// </summary>
    /// <param name="actualSql">The actual SQL string.</param>
    /// <param name="expectedSql">The expected SQL string.</param>
    public static void AssertSqlEquals(string actualSql, string expectedSql)
    {
        var normalizedActual = NormalizeSql(actualSql);
        var normalizedExpected = NormalizeSql(expectedSql);

        Assert.AreEqual(normalizedExpected, normalizedActual,
            $"SQL mismatch.\nExpected: {expectedSql}\nActual: {actualSql}");
    }

    /// <summary>
    /// Asserts that parameters contain expected key-value pair.
    /// </summary>
    /// <param name="parameters">The parameter dictionary.</param>
    /// <param name="key">The parameter key.</param>
    /// <param name="expectedValue">The expected parameter value.</param>
    public static void AssertParametersContain(
        IDictionary<string, object?> parameters,
        string key,
        object? expectedValue)
    {
        Assert.IsTrue(parameters.ContainsKey(key),
            $"Parameters should contain key '{key}'");
        Assert.AreEqual(expectedValue, parameters[key],
            $"Parameter '{key}' should equal '{expectedValue}' but was '{parameters[key]}'");
    }

    /// <summary>
    /// Asserts that SQL is properly parameterized (contains expected number of parameters).
    /// </summary>
    /// <param name="sql">The SQL string to check.</param>
    /// <param name="expectedParameterCount">Expected number of parameters.</param>
    public static void AssertSqlIsParameterized(string sql, int expectedParameterCount)
    {
        var parameterMatches = Regex.Matches(sql, @"@p\d+");
        Assert.AreEqual(expectedParameterCount, parameterMatches.Count,
            $"Expected {expectedParameterCount} parameters but found {parameterMatches.Count} in SQL: {sql}");
    }

    /// <summary>
    /// Asserts that SQL does not contain any inline values (is fully parameterized).
    /// </summary>
    /// <param name="sql">The SQL string to check.</param>
    public static void AssertSqlHasNoInlineValues(string sql)
    {
        // Check for common inline value patterns
        var hasInlineString = Regex.IsMatch(sql, @"'[^']*'(?!.*@)");
        var hasInlineNumber = Regex.IsMatch(sql, @"\b\d+\b(?!.*@)");

        Assert.IsFalse(hasInlineString || hasInlineNumber,
            $"SQL should not contain inline values but got: {sql}");
    }

    /// <summary>
    /// Normalizes SQL by trimming and collapsing whitespace.
    /// </summary>
    /// <param name="sql">The SQL string to normalize.</param>
    /// <returns>Normalized SQL string.</returns>
    private static string NormalizeSql(string sql)
    {
        return Regex.Replace(sql.Trim(), @"\s+", " ");
    }
}
