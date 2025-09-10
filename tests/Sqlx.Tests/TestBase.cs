// -----------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Base class for all Sqlx tests providing common functionality.
/// </summary>
[TestClass]
public abstract class TestBase
{
    /// <summary>
    /// Test context for accessing test information.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Setup method called before each test.
    /// </summary>
    [TestInitialize]
    public virtual void Setup()
    {
        // Clear caches before each test to ensure clean state
        TypeAnalyzer.ClearCaches();
        DatabaseDialectFactory.ClearCache();
    }

    /// <summary>
    /// Cleanup method called after each test.
    /// </summary>
    [TestCleanup]
    public virtual void Cleanup()
    {
        // Optionally clear caches after tests to free memory
        if (TestContext.TestName?.Contains("Performance") != true)
        {
            TypeAnalyzer.ClearCaches();
            DatabaseDialectFactory.ClearCache();
        }
    }

    /// <summary>
    /// Asserts that an action throws a specific exception type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="expectedMessage">Optional expected message content.</param>
    /// <returns>The thrown exception for further assertions.</returns>
    protected static TException AssertThrows<TException>(Action action, string? expectedMessage = null)
        where TException : Exception
    {
        try
        {
            action();
            Assert.Fail($"Expected {typeof(TException).Name} to be thrown, but no exception was thrown.");
            return null!; // This line will never be reached
        }
        catch (TException ex)
        {
            if (expectedMessage != null)
            {
                Assert.IsTrue(ex.Message.Contains(expectedMessage), 
                    $"Expected exception message to contain '{expectedMessage}', but was '{ex.Message}'");
            }
            return ex;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected {typeof(TException).Name} to be thrown, but {ex.GetType().Name} was thrown instead: {ex.Message}");
            return null!; // This line will never be reached
        }
    }

    /// <summary>
    /// Asserts that two collections have the same elements (order independent).
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="expected">The expected collection.</param>
    /// <param name="actual">The actual collection.</param>
    protected static void AssertCollectionsEqual<T>(System.Collections.Generic.IEnumerable<T> expected, 
        System.Collections.Generic.IEnumerable<T> actual)
    {
        var expectedList = expected.ToList();
        var actualList = actual.ToList();
        
        Assert.AreEqual(expectedList.Count, actualList.Count, 
            $"Collections have different counts. Expected: {expectedList.Count}, Actual: {actualList.Count}");
        
        foreach (var item in expectedList)
        {
            Assert.IsTrue(actualList.Contains(item), $"Expected item '{item}' not found in actual collection");
        }
    }

    /// <summary>
    /// Creates a test-specific temporary file name.
    /// </summary>
    /// <param name="extension">File extension (without dot).</param>
    /// <returns>A unique temporary file path.</returns>
    protected string CreateTempFileName(string extension = "tmp")
    {
        var testName = TestContext.TestName ?? "UnknownTest";
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{testName}_{timestamp}_{Guid.NewGuid():N}.{extension}";
    }

    /// <summary>
    /// Measures the execution time of an action.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    /// <returns>The elapsed time in milliseconds.</returns>
    protected static long MeasureExecutionTime(Action action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Writes a test message to the test output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    protected void WriteTestOutput(string message)
    {
        TestContext.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    /// <summary>
    /// Skips the current test with a reason.
    /// </summary>
    /// <param name="reason">The reason for skipping.</param>
    protected void SkipTest(string reason)
    {
        Assert.Inconclusive($"Test skipped: {reason}");
    }
}

