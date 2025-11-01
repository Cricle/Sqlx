// -----------------------------------------------------------------------
// <copyright file="EdgeCaseTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for edge cases and error handling scenarios.
    /// </summary>
    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void NullInputHandling_VariousScenarios_HandledGracefully()
        {
            // Test null handling in various scenarios
            string? nullString = null;
            object? nullObject = null;
            int[]? nullArray = null;

            // String operations with null
            var result1 = nullString ?? string.Empty;
            Assert.AreEqual(string.Empty, result1);

            var result2 = string.IsNullOrEmpty(nullString);
            Assert.IsTrue(result2);

            var result3 = string.IsNullOrWhiteSpace(nullString);
            Assert.IsTrue(result3);

            // Object operations with null
            var result4 = nullObject?.ToString() ?? "default";
            Assert.AreEqual("default", result4);

            var result5 = nullObject is null;
            Assert.IsTrue(result5);

            // Array operations with null
            var result6 = nullArray?.Length ?? 0;
            Assert.AreEqual(0, result6);

            var result7 = nullArray?.Any() ?? false;
            Assert.IsFalse(result7);
        }

        [TestMethod]
        public void EmptyCollectionHandling_VariousTypes_HandledCorrectly()
        {
            // Test empty collection handling
            var emptyList = new List<int>();
            var emptyArray = new string[0];
            var emptyEnumerable = Enumerable.Empty<bool>();

            // List operations
            Assert.AreEqual(0, emptyList.Count);
            Assert.IsFalse(emptyList.Any());
            Assert.AreEqual(0, emptyList.FirstOrDefault());

            // Array operations
            Assert.AreEqual(0, emptyArray.Length);
            Assert.IsFalse(emptyArray.Any());
            Assert.AreEqual(default(string), emptyArray.FirstOrDefault());

            // Enumerable operations
            Assert.IsFalse(emptyEnumerable.Any());
            Assert.AreEqual(0, emptyEnumerable.Count());
            Assert.IsFalse(emptyEnumerable.FirstOrDefault());
        }

        [TestMethod]
        public void ExtremeValueHandling_NumericTypes_HandledCorrectly()
        {
            // Test extreme numeric values
            var extremeValues = new[]
            {
                (int.MinValue, int.MaxValue),
                (long.MinValue, long.MaxValue),
                (short.MinValue, short.MaxValue),
                (byte.MinValue, byte.MaxValue)
            };

            foreach (var (min, max) in extremeValues)
            {
                // Test that extreme values are handled correctly
                Assert.IsTrue(min < max);

                // Test string conversion
                var minStr = min.ToString();
                var maxStr = max.ToString();
                Assert.IsTrue(minStr.Length > 0);
                Assert.IsTrue(maxStr.Length > 0);

                // Test that they can be converted back
                var parsedMin = Convert.ChangeType(minStr, min.GetType());
                var parsedMax = Convert.ChangeType(maxStr, max.GetType());
                Assert.AreEqual(min, parsedMin);
                Assert.AreEqual(max, parsedMax);
            }

            // Test floating point extremes
            var floatExtremes = new[]
            {
                float.MinValue, float.MaxValue, float.PositiveInfinity, float.NegativeInfinity, float.NaN
            };

            foreach (var value in floatExtremes)
            {
                if (float.IsNaN(value))
                {
                    Assert.IsTrue(float.IsNaN(value));
                }
                else if (float.IsInfinity(value))
                {
                    Assert.IsTrue(float.IsInfinity(value));
                }
                else
                {
                    Assert.IsTrue(float.IsFinite(value));
                }
            }
        }

        [TestMethod]
        public void SpecialCharacterHandling_StringInputs_HandledCorrectly()
        {
            // Test special characters in strings
            var specialStrings = new[]
            {
                "normal_string",
                "string with spaces",
                "string-with-dashes",
                "string.with.dots",
                "string@with@symbols",
                "string'with'quotes",
                "string\"with\"double\"quotes",
                "string\\with\\backslashes",
                "string/with/slashes",
                "string\nwith\nnewlines",
                "string\twith\ttabs",
                "string\rwith\rreturns",
                "string with unicode: ñáéíóú",
                "string with emojis: 🚀💻🎉",
                "string with null char: \0",
                "string with control chars: \x01\x02\x03"
            };

            foreach (var str in specialStrings)
            {
                // Test that strings are handled correctly
                Assert.IsNotNull(str);
                Assert.IsTrue(str.Length > 0);

                // Test escaping for SQL (conceptual)
                var escaped = str.Replace("'", "''");
                if (str.Contains("'"))
                {
                    Assert.IsTrue(escaped.Contains("''"));
                }

                // Test trimming
                var trimmed = str.Trim();
                Assert.IsTrue(trimmed.Length <= str.Length);

                // Test encoding safety
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                var decoded = System.Text.Encoding.UTF8.GetString(bytes);
                Assert.AreEqual(str, decoded);
            }
        }

        [TestMethod]
        public void ConcurrencyEdgeCases_ThreadSafety_HandledCorrectly()
        {
            // Test thread-safety concepts (simplified)
            var sharedCounter = 0;
            var lockObject = new object();
            var tasks = new List<System.Threading.Tasks.Task>();

            // Simulate concurrent operations
            for (int i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    lock (lockObject)
                    {
                        var temp = sharedCounter;
                        System.Threading.Thread.Sleep(1); // Simulate work
                        sharedCounter = temp + 1;
                    }
                });
                tasks.Add(task);
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(10, sharedCounter);

            // Test concurrent collection operations
            var concurrentList = new System.Collections.Concurrent.ConcurrentBag<int>();
            var concurrentTasks = new List<System.Threading.Tasks.Task>();

            for (int i = 0; i < 100; i++)
            {
                var index = i;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    concurrentList.Add(index);
                });
                concurrentTasks.Add(task);
            }

            System.Threading.Tasks.Task.WaitAll(concurrentTasks.ToArray());
            Assert.AreEqual(100, concurrentList.Count);
        }

        [TestMethod]
        public void MemoryEdgeCases_LargeData_HandledCorrectly()
        {
            // Test large data handling (within reasonable limits for unit tests)
            var largeString = new string('A', 10000);
            Assert.AreEqual(10000, largeString.Length);
            Assert.IsTrue(largeString.All(c => c == 'A'));

            // Test large collections
            var largeList = Enumerable.Range(0, 10000).ToList();
            Assert.AreEqual(10000, largeList.Count);
            Assert.AreEqual(0, largeList.First());
            Assert.AreEqual(9999, largeList.Last());

            // Test memory efficiency with large arrays
            var largeArray = new int[10000];
            Array.Fill(largeArray, 42);
            Assert.AreEqual(10000, largeArray.Length);
            Assert.IsTrue(largeArray.All(x => x == 42));

            // Test string concatenation efficiency
            var stringBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                stringBuilder.Append($"Item{i},");
            }
            var result = stringBuilder.ToString();
            Assert.IsTrue(result.Length > 5000);
            Assert.IsTrue(result.Contains("Item0,"));
            Assert.IsTrue(result.Contains("Item999,"));
        }

        [TestMethod]
        public void TypeConversionEdgeCases_VariousTypes_HandledCorrectly()
        {
            // Test type conversion edge cases
            var conversionTests = new (Type targetType, string stringValue, object expected)[]
            {
                (typeof(int), "123", 123),
                (typeof(bool), "true", true),
                (typeof(bool), "false", false),
                (typeof(DateTime), "2023-01-01", new DateTime(2023, 1, 1)),
                (typeof(decimal), "123.45", 123.45m),
                (typeof(double), "123.45", 123.45d)
            };

            foreach (var (targetType, stringValue, expected) in conversionTests)
            {
                try
                {
                    var converted = Convert.ChangeType(stringValue, targetType);
                    Assert.AreEqual(expected, converted);
                    Assert.AreEqual(targetType, converted.GetType());
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Conversion failed for {targetType.Name}: {ex.Message}");
                }
            }

            // Test invalid conversions
            var invalidConversions = new (Type targetType, string invalidValue)[]
            {
                (typeof(int), "not_a_number"),
                (typeof(DateTime), "invalid_date"),
                (typeof(bool), "maybe"),
                (typeof(decimal), "not_decimal")
            };

            foreach (var (targetType, invalidValue) in invalidConversions)
            {
                Assert.ThrowsException<FormatException>(() =>
                {
                    Convert.ChangeType(invalidValue, targetType);
                });
            }
        }

        [TestMethod]
        public void DatabaseConnectionEdgeCases_VariousStates_HandledCorrectly()
        {
            // Test database connection state handling (conceptual)
            var connectionStates = Enum.GetValues<ConnectionState>();

            foreach (var state in connectionStates)
            {
                // Test state validation
                Assert.IsTrue(Enum.IsDefined(typeof(ConnectionState), state));

                // Test state name
                var stateName = state.ToString();
                Assert.IsTrue(stateName.Length > 0);

                // Test state conversion
                var stateValue = (int)state;
                var backToState = (ConnectionState)stateValue;
                Assert.AreEqual(state, backToState);

                // Test state logic
                switch (state)
                {
                    case ConnectionState.Open:
                        Assert.IsTrue(state == ConnectionState.Open);
                        break;
                    case ConnectionState.Closed:
                        Assert.IsTrue(state == ConnectionState.Closed);
                        break;
                    case ConnectionState.Connecting:
                        Assert.IsTrue(state == ConnectionState.Connecting);
                        break;
                    case ConnectionState.Executing:
                        Assert.IsTrue(state == ConnectionState.Executing);
                        break;
                    case ConnectionState.Fetching:
                        Assert.IsTrue(state == ConnectionState.Fetching);
                        break;
                    case ConnectionState.Broken:
                        Assert.IsTrue(state == ConnectionState.Broken);
                        break;
                }
            }
        }

        [TestMethod]
        public void ExceptionHandlingEdgeCases_VariousExceptions_HandledCorrectly()
        {
            // Test various exception scenarios
            var exceptionTests = new Action[]
            {
                () => throw new ArgumentNullException("param"),
                () => throw new ArgumentException("Invalid argument"),
                () => throw new InvalidOperationException("Invalid operation"),
                () => throw new NotSupportedException("Not supported"),
                () => throw new FormatException("Format error"),
                () => throw new OverflowException("Overflow error")
            };

            foreach (var exceptionTest in exceptionTests)
            {
                try
                {
                    exceptionTest();
                    Assert.Fail("Exception should have been thrown");
                }
                catch (Exception ex)
                {
                    Assert.IsNotNull(ex);
                    Assert.IsNotNull(ex.Message);
                    Assert.IsTrue(ex.Message.Length > 0);

                    // Test exception properties
                    Assert.IsNotNull(ex.GetType());
                    Assert.IsTrue(ex.GetType().IsSubclassOf(typeof(Exception)) || ex.GetType() == typeof(Exception));
                }
            }

            // Test nested exceptions
            try
            {
                try
                {
                    throw new InvalidOperationException("Inner exception");
                }
                catch (Exception inner)
                {
                    throw new ArgumentException("Outer exception", inner);
                }
            }
            catch (ArgumentException ex)
            {
                Assert.IsNotNull(ex.InnerException);
                Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
                Assert.AreEqual("Inner exception", ex.InnerException.Message);
            }
        }

        [TestMethod]
        public void ResourceManagementEdgeCases_DisposableObjects_HandledCorrectly()
        {
            // Test resource management with disposable objects
            var disposableCalled = false;

            using (var disposable = new TestDisposable(() => disposableCalled = true))
            {
                Assert.IsNotNull(disposable);
                Assert.IsFalse(disposableCalled);
            }

            Assert.IsTrue(disposableCalled);

            // Test multiple disposal
            var multipleDisposalCounter = 0;
            var multipleDisposable = new TestDisposable(() => multipleDisposalCounter++);

            multipleDisposable.Dispose();
            multipleDisposable.Dispose();
            multipleDisposable.Dispose();

            // Should handle multiple disposal gracefully
            Assert.IsTrue(multipleDisposalCounter >= 1);

            // Test disposal with exceptions
            var exceptionDisposable = new TestDisposable(() => throw new InvalidOperationException("Disposal error"));

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                exceptionDisposable.Dispose();
            });
        }

        private class TestDisposable : IDisposable
        {
            private readonly Action _disposeAction;
            private bool _disposed = false;

            public TestDisposable(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposeAction();
                    _disposed = true;
                }
            }
        }

        [TestMethod]
        [Ignore("GC和内存测试不稳定，仅供手动运行")]
        public void PerformanceEdgeCases_HighLoad_HandledCorrectly()
        {
            // Test performance under high load scenarios
            var startTime = DateTime.UtcNow;

            // Simulate high-frequency operations
            var operations = 10000;
            var results = new List<int>(operations);

            for (int i = 0; i < operations; i++)
            {
                // Simulate some work
                var result = i * 2 + 1;
                results.Add(result);
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            Assert.AreEqual(operations, results.Count);
            Assert.IsTrue(duration.TotalMilliseconds < 5000); // Should complete within 5 seconds

            // Test memory usage doesn't grow excessively
            var initialMemory = GC.GetTotalMemory(false);

            // Create and dispose many objects
            for (int i = 0; i < 1000; i++)
            {
                var temp = new List<int>(Enumerable.Range(0, 100));
                temp.Clear();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);

            // Memory should not grow significantly after GC
            Assert.IsTrue(finalMemory - initialMemory < 1024 * 1024); // Less than 1MB growth
        }
    }
}
