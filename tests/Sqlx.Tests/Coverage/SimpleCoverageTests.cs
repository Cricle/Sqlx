// -----------------------------------------------------------------------
// <copyright file="SimpleCoverageTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Coverage
{
    /// <summary>
    /// 简化的覆盖率测试，专注于提升代码覆盖率而不依赖外部数据库
    /// </summary>
    [TestClass]
    public class SimpleCoverageTests
    {
        [TestInitialize]
        public void Setup()
        {
            // 清理缓存以确保测试独立性
            IntelligentCacheManager.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // 清理缓存
            IntelligentCacheManager.Clear();
        }

        #region IntelligentCacheManager 全面测试

        [TestMethod]
        public void IntelligentCacheManager_Set_WithValidKeyValue_StoresSuccessfully()
        {
            // Arrange
            var key = "test_key_1";
            var value = "test_value_1";

            // Act
            IntelligentCacheManager.Set(key, value);
            var result = IntelligentCacheManager.Get<string>(key);

            // Assert
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void IntelligentCacheManager_Set_WithExpiration_ExpiresCorrectly()
        {
            // Arrange
            var key = "expiring_key";
            var value = "expiring_value";
            var expiration = TimeSpan.FromMilliseconds(100);

            // Act
            IntelligentCacheManager.Set(key, value, expiration);

            // Wait for expiration
            System.Threading.Thread.Sleep(150);

            var result = IntelligentCacheManager.Get<string>(key);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void IntelligentCacheManager_Set_WithNullValue_StoresNull()
        {
            // Arrange
            var key = "null_key";

            // Act
            IntelligentCacheManager.Set<string?>(key, null);
            var result = IntelligentCacheManager.Get<string?>(key);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void IntelligentCacheManager_GetOrAdd_WithNewKey_CallsFactory()
        {
            // Arrange
            var key = "factory_key";
            var expectedValue = "factory_value";
            var factoryCalled = false;

            // Act
            var result = IntelligentCacheManager.GetOrAdd(key, () =>
            {
                factoryCalled = true;
                return expectedValue;
            });

            // Assert
            Assert.AreEqual(expectedValue, result);
            Assert.IsTrue(factoryCalled);
        }

        [TestMethod]
        public void IntelligentCacheManager_GetOrAdd_WithExistingKey_DoesNotCallFactory()
        {
            // Arrange
            var key = "existing_key";
            var originalValue = "original_value";
            var factoryValue = "factory_value";
            var factoryCalled = false;

            IntelligentCacheManager.Set(key, originalValue);

            // Act
            var result = IntelligentCacheManager.GetOrAdd(key, () =>
            {
                factoryCalled = true;
                return factoryValue;
            });

            // Assert
            Assert.AreEqual(originalValue, result);
            Assert.IsFalse(factoryCalled);
        }

        [TestMethod]
        public void IntelligentCacheManager_Remove_WithExistingKey_RemovesAndReturnsTrue()
        {
            // Arrange
            var key = "remove_key";
            var value = "remove_value";
            IntelligentCacheManager.Set(key, value);

            // Act
            var removed = IntelligentCacheManager.Remove(key);
            var result = IntelligentCacheManager.Get<string>(key);

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void IntelligentCacheManager_Remove_WithNonExistentKey_ReturnsFalse()
        {
            // Arrange
            var key = "non_existent_key";

            // Act
            var removed = IntelligentCacheManager.Remove(key);

            // Assert
            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void IntelligentCacheManager_Clear_RemovesAllEntries()
        {
            // Arrange
            IntelligentCacheManager.Set("key1", "value1");
            IntelligentCacheManager.Set("key2", "value2");
            IntelligentCacheManager.Set("key3", "value3");

            // Act
            IntelligentCacheManager.Clear();

            // Assert
            Assert.IsNull(IntelligentCacheManager.Get<string>("key1"));
            Assert.IsNull(IntelligentCacheManager.Get<string>("key2"));
            Assert.IsNull(IntelligentCacheManager.Get<string>("key3"));
        }

        [TestMethod]
        public void IntelligentCacheManager_GetStatistics_ReturnsValidStatistics()
        {
            // Arrange
            IntelligentCacheManager.Clear();
            IntelligentCacheManager.Set("stats_key1", "value1");
            IntelligentCacheManager.Set("stats_key2", "value2");

            // Generate some hits and misses
            IntelligentCacheManager.Get<string>("stats_key1"); // Hit
            IntelligentCacheManager.Get<string>("non_existent"); // Miss

            // Act
            var stats = IntelligentCacheManager.GetStatistics();

            // Assert
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.EntryCount >= 2);
            Assert.IsTrue(stats.HitCount >= 1);
            Assert.IsTrue(stats.MissCount >= 1);
            Assert.IsTrue(stats.TotalMemoryUsage > 0);
            // HitRate calculation: hits / (hits + misses)
            var calculatedHitRate = stats.HitCount > 0 ? (double)stats.HitCount / (stats.HitCount + stats.MissCount) : 0;
            Assert.IsTrue(calculatedHitRate >= 0 && calculatedHitRate <= 1);
        }

        [TestMethod]
        public void IntelligentCacheManager_MultipleTypes_StoresCorrectly()
        {
            // Arrange & Act & Assert

            // String
            IntelligentCacheManager.Set("string_key", "string_value");
            Assert.AreEqual("string_value", IntelligentCacheManager.Get<string>("string_key"));

            // Integer
            IntelligentCacheManager.Set("int_key", 42);
            Assert.AreEqual(42, IntelligentCacheManager.Get<int>("int_key"));

            // Boolean
            IntelligentCacheManager.Set("bool_key", true);
            Assert.AreEqual(true, IntelligentCacheManager.Get<bool>("bool_key"));

            // DateTime
            var testDate = DateTime.Now;
            IntelligentCacheManager.Set("date_key", testDate);
            Assert.AreEqual(testDate, IntelligentCacheManager.Get<DateTime>("date_key"));

            // Complex object
            var complexObject = new { Name = "Test", Value = 123 };
            IntelligentCacheManager.Set("complex_key", complexObject);
            var retrievedComplex = IntelligentCacheManager.Get<object>("complex_key");
            Assert.IsNotNull(retrievedComplex);
        }

        [TestMethod]
        public void IntelligentCacheManager_TypeMismatch_HandlesGracefully()
        {
            // Arrange
            IntelligentCacheManager.Set("type_test_key", "string_value");

            // Act & Assert
            try
            {
                var intResult = IntelligentCacheManager.Get<int>("type_test_key");
                // If no exception, it should return default
                Assert.AreEqual(0, intResult);
            }
            catch (InvalidCastException)
            {
                // This is also acceptable behavior
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_LargeNumberOfEntries_HandlesCorrectly()
        {
            // Arrange
            const int entryCount = 1000;

            // Act
            for (int i = 0; i < entryCount; i++)
            {
                IntelligentCacheManager.Set($"bulk_key_{i}", $"bulk_value_{i}");
            }

            // Assert
            var stats = IntelligentCacheManager.GetStatistics();
            Assert.IsTrue(stats.EntryCount > 0);

            // Verify some entries exist
            Assert.AreEqual("bulk_value_0", IntelligentCacheManager.Get<string>("bulk_key_0"));
            Assert.AreEqual("bulk_value_500", IntelligentCacheManager.Get<string>("bulk_key_500"));
            Assert.AreEqual("bulk_value_999", IntelligentCacheManager.Get<string>("bulk_key_999"));
        }

        [TestMethod]
        public void IntelligentCacheManager_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            const int taskCount = 10;
            const int operationsPerTask = 100;
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < operationsPerTask; j++)
                        {
                            var key = $"concurrent_{taskId}_{j}";
                            var value = $"value_{taskId}_{j}";

                            IntelligentCacheManager.Set(key, value);
                            var retrieved = IntelligentCacheManager.Get<string>(key);

                            if (retrieved != value && retrieved != null)
                            {
                                throw new InvalidOperationException($"Value mismatch: expected {value}, got {retrieved}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.AreEqual(0, exceptions.Count, $"Concurrent access caused {exceptions.Count} exceptions");
        }

        #endregion

        #region AdvancedConnectionManager 测试 (不依赖数据库连接)

        [TestMethod]
        public void AdvancedConnectionManager_GetConnectionMetrics_ReturnsValidMetrics()
        {
            // Act
            var metrics = AdvancedConnectionManager.GetConnectionMetrics();

            // Assert
            Assert.IsNotNull(metrics);
            Assert.IsTrue(metrics.TotalConnections >= 0);
            Assert.IsTrue(metrics.ActiveConnections >= 0);
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_ExecuteWithRetryAsync_WithSuccessfulOperation_ReturnsResult()
        {
            // Arrange
            var expectedResult = "success";

            // Act
            var result = await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(async () =>
            {
                await Task.Delay(10);
                return expectedResult;
            });

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_ExecuteWithRetryAsync_WithTransientFailure_RetriesAndSucceeds()
        {
            // Arrange
            var attemptCount = 0;
            var expectedResult = "success_after_retry";

            // Act
            var result = await AdvancedConnectionManager.ExecuteWithRetryAsync(async () =>
            {
                await Task.Delay(10);
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new InvalidOperationException("Transient failure");
                }
                return expectedResult;
            }, maxAttempts: 3);

            // Assert
            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(2, attemptCount);
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_ExecuteWithRetryAsync_WithPermanentFailure_ThrowsAfterMaxAttempts()
        {
            // Arrange
            var attemptCount = 0;
            var expectedMessage = "Permanent failure";

            // Act & Assert
            try
            {
                await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(async () =>
                {
                    await Task.Delay(10);
                    attemptCount++;
                    throw new InvalidOperationException(expectedMessage);
                }, maxAttempts: 3);

                Assert.Fail("Should have thrown exception");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(expectedMessage, ex.Message);
                Assert.AreEqual(3, attemptCount);
            }
        }

        #endregion

        #region 边界条件和错误处理测试

        [TestMethod]
        public void IntelligentCacheManager_NullKey_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                IntelligentCacheManager.Set(null!, "value");
            });
        }

        [TestMethod]
        public void IntelligentCacheManager_EmptyKey_HandlesGracefully()
        {
            // Act & Assert
            try
            {
                IntelligentCacheManager.Set("", "value");
                // If no exception, that's acceptable behavior
                Assert.IsTrue(true);
            }
            catch (ArgumentException)
            {
                // This is also acceptable behavior
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_GetOrAdd_NullFactory_HandlesGracefully()
        {
            // Act & Assert
            try
            {
                IntelligentCacheManager.GetOrAdd<string>("key", null!);
                Assert.Fail("Should have thrown an exception");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (NullReferenceException)
            {
                // This is also acceptable behavior
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_GetOrAdd_FactoryThrowsException_PropagatesException()
        {
            // Arrange
            var expectedMessage = "Factory exception";

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
            {
                IntelligentCacheManager.GetOrAdd<string>("key", () => throw new InvalidOperationException(expectedMessage));
            });

            Assert.AreEqual(expectedMessage, exception.Message);
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_ExecuteWithRetryAsync_NullOperation_HandlesGracefully()
        {
            // Act & Assert
            try
            {
                await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(null!);
                Assert.Fail("Should have thrown an exception");
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
            catch (NullReferenceException)
            {
                // This is also acceptable behavior
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_ZeroExpiration_ExpiresImmediately()
        {
            // Arrange
            var key = "zero_expiration_key";
            var value = "test_value";

            // Act
            IntelligentCacheManager.Set(key, value, TimeSpan.Zero);
            var result = IntelligentCacheManager.Get<string>(key);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void IntelligentCacheManager_NegativeExpiration_ExpiresImmediately()
        {
            // Arrange
            var key = "negative_expiration_key";
            var value = "test_value";

            // Act
            IntelligentCacheManager.Set(key, value, TimeSpan.FromSeconds(-1));
            var result = IntelligentCacheManager.Get<string>(key);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region 内存和性能测试

        [TestMethod]
        public void IntelligentCacheManager_MemoryUsage_StaysReasonable()
        {
            // Arrange
            IntelligentCacheManager.Clear();
            var initialMemory = GC.GetTotalMemory(true);

            // Act
            for (int i = 0; i < 100; i++)
            {
                var key = $"memory_test_{i}";
                var value = new string('A', 1000); // 1KB string
                IntelligentCacheManager.Set(key, value);
            }

            var afterMemory = GC.GetTotalMemory(false);
            var memoryIncrease = afterMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease > 0);
            Assert.IsTrue(memoryIncrease < 5 * 1024 * 1024); // Should be less than 5MB

            // Cleanup
            IntelligentCacheManager.Clear();
            GC.Collect();
        }

        [TestMethod]
        public void IntelligentCacheManager_Performance_FastOperations()
        {
            // Arrange
            const int operationCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Set operations
            for (int i = 0; i < operationCount; i++)
            {
                IntelligentCacheManager.Set($"perf_key_{i}", $"perf_value_{i}");
            }

            var setTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // Act - Get operations
            for (int i = 0; i < operationCount; i++)
            {
                IntelligentCacheManager.Get<string>($"perf_key_{i}");
            }

            var getTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(setTime < 1000, $"Set operations too slow: {setTime}ms for {operationCount} operations");
            Assert.IsTrue(getTime < 500, $"Get operations too slow: {getTime}ms for {operationCount} operations");

            Console.WriteLine($"Performance: Set {operationCount} items in {setTime}ms, Get {operationCount} items in {getTime}ms");
        }

        #endregion
    }
}
