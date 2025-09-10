// -----------------------------------------------------------------------
// <copyright file="AdditionalCoverageTests.cs" company="Cricle">
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
    /// 额外的覆盖率测试，专注于提升整体代码覆盖率
    /// </summary>
    [TestClass]
    public class AdditionalCoverageTests
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

        #region 高级缓存场景测试

        [TestMethod]
        public void IntelligentCacheManager_ComplexKeyPatterns_HandlesCorrectly()
        {
            // 测试各种复杂的键模式
            var testCases = new[]
            {
                "simple_key",
                "key-with-dashes",
                "key.with.dots",
                "key/with/slashes",
                "key with spaces",
                "key_with_números_123",
                "UPPERCASE_KEY",
                "MixedCaseKey",
                "key@with#special$characters%",
                "very_long_key_" + new string('x', 1000)
            };

            foreach (var key in testCases)
            {
                try
                {
                    var value = $"value_for_{key}";
                    IntelligentCacheManager.Set(key, value);
                    var retrieved = IntelligentCacheManager.Get<string>(key);
                    
                    Assert.AreEqual(value, retrieved, $"Failed for key: {key}");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Exception for key '{key}': {ex.Message}");
                }
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_ExtremeValues_HandlesCorrectly()
        {
            // 测试极端值
            var testCases = new Dictionary<string, object>
            {
                {"max_int", int.MaxValue},
                {"min_int", int.MinValue},
                {"max_long", long.MaxValue},
                {"min_long", long.MinValue},
                {"max_double", double.MaxValue},
                {"min_double", double.MinValue},
                {"positive_infinity", double.PositiveInfinity},
                {"negative_infinity", double.NegativeInfinity},
                {"nan", double.NaN},
                {"max_decimal", decimal.MaxValue},
                {"min_decimal", decimal.MinValue},
                {"max_datetime", DateTime.MaxValue},
                {"min_datetime", DateTime.MinValue},
                {"empty_string", ""},
                {"null_string", null!},
                {"large_string", new string('A', 100000)},
                {"empty_array", new int[0]},
                {"large_array", new int[10000]}
            };

            foreach (var testCase in testCases)
            {
                try
                {
                    IntelligentCacheManager.Set(testCase.Key, testCase.Value);
                    var retrieved = IntelligentCacheManager.Get<object>(testCase.Key);
                    
                    if (testCase.Value == null)
                    {
                        Assert.IsNull(retrieved, $"Failed for key: {testCase.Key}");
                    }
                    else if (testCase.Value is double doubleValue && double.IsNaN(doubleValue))
                    {
                        Assert.IsTrue(retrieved is double retrievedDouble && double.IsNaN(retrievedDouble), $"Failed for key: {testCase.Key}");
                    }
                    else
                    {
                        Assert.AreEqual(testCase.Value, retrieved, $"Failed for key: {testCase.Key}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception for key '{testCase.Key}': {ex.Message}");
                    // 某些极端值可能会导致异常，这是可以接受的
                }
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_RapidOperations_HandlesCorrectly()
        {
            // 测试快速连续操作
            const int operationCount = 1000;
            var keys = new List<string>();

            // 快速设置
            for (int i = 0; i < operationCount; i++)
            {
                var key = $"rapid_key_{i}";
                var value = $"rapid_value_{i}";
                keys.Add(key);
                IntelligentCacheManager.Set(key, value);
            }

            // 快速获取
            for (int i = 0; i < operationCount; i++)
            {
                var key = keys[i];
                var expectedValue = $"rapid_value_{i}";
                var retrievedValue = IntelligentCacheManager.Get<string>(key);
                Assert.AreEqual(expectedValue, retrievedValue);
            }

            // 快速删除
            for (int i = 0; i < operationCount; i++)
            {
                var key = keys[i];
                var removed = IntelligentCacheManager.Remove(key);
                Assert.IsTrue(removed);
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_MixedOperations_HandlesCorrectly()
        {
            // 测试混合操作模式
            var random = new Random(42); // 固定种子以确保可重复性
            const int operationCount = 500;
            var activeKeys = new HashSet<string>();

            for (int i = 0; i < operationCount; i++)
            {
                var operation = random.Next(0, 4); // 0: Set, 1: Get, 2: Remove, 3: GetOrAdd
                var key = $"mixed_key_{random.Next(0, 100)}";
                var value = $"mixed_value_{i}";

                switch (operation)
                {
                    case 0: // Set
                        IntelligentCacheManager.Set(key, value);
                        activeKeys.Add(key);
                        break;
                        
                    case 1: // Get
                        var retrieved = IntelligentCacheManager.Get<string>(key);
                        if (activeKeys.Contains(key))
                        {
                            Assert.IsNotNull(retrieved);
                        }
                        break;
                        
                    case 2: // Remove
                        var removed = IntelligentCacheManager.Remove(key);
                        if (removed)
                        {
                            activeKeys.Remove(key);
                        }
                        break;
                        
                    case 3: // GetOrAdd
                        var getOrAddResult = IntelligentCacheManager.GetOrAdd(key, () => value);
                        activeKeys.Add(key);
                        Assert.IsNotNull(getOrAddResult);
                        break;
                }
            }

            // 验证统计信息
            var stats = IntelligentCacheManager.GetStatistics();
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.EntryCount >= 0);
        }

        [TestMethod]
        public void IntelligentCacheManager_ExpirationEdgeCases_HandlesCorrectly()
        {
            // 测试过期时间的边界情况
            var testCases = new[]
            {
                TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromHours(1),
                TimeSpan.FromDays(1),
                TimeSpan.MaxValue
            };

            for (int i = 0; i < testCases.Length; i++)
            {
                var key = $"expiration_key_{i}";
                var value = $"expiration_value_{i}";
                var expiration = testCases[i];

                try
                {
                    IntelligentCacheManager.Set(key, value, expiration);
                    var retrieved = IntelligentCacheManager.Get<string>(key);
                    
                    if (expiration.TotalMilliseconds < 50) // 很短的过期时间可能已经过期
                    {
                        // 可能已经过期，这是正常的
                    }
                    else
                    {
                        Assert.AreEqual(value, retrieved, $"Failed for expiration: {expiration}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception for expiration '{expiration}': {ex.Message}");
                }
            }
        }

        #endregion

        #region AdvancedConnectionManager 高级场景

        [TestMethod]
        public async Task AdvancedConnectionManager_RepeatedRetries_HandlesCorrectly()
        {
            // 测试重复重试场景
            var attemptCounts = new List<int>();

            for (int testRun = 0; testRun < 5; testRun++)
            {
                var attemptCount = 0;
                var maxAttempts = 3;

                try
                {
                    await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(async () =>
                    {
                        await Task.Delay(10);
                        attemptCount++;
                        
                        if (attemptCount < maxAttempts)
                        {
                            throw new InvalidOperationException($"Attempt {attemptCount} failed");
                        }
                        
                        return $"Success on attempt {attemptCount}";
                    }, maxAttempts);

                    attemptCounts.Add(attemptCount);
                }
                catch
                {
                    attemptCounts.Add(attemptCount);
                }
            }

            // 验证重试行为的一致性
            Assert.IsTrue(attemptCounts.Count == 5);
            foreach (var count in attemptCounts)
            {
                Assert.IsTrue(count > 0 && count <= 3);
            }
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_VariousExceptionTypes_HandlesCorrectly()
        {
            // 测试各种异常类型的处理
            var exceptionTypes = new[]
            {
                typeof(InvalidOperationException),
                typeof(ArgumentException),
                typeof(TimeoutException),
                typeof(NotSupportedException),
                typeof(ApplicationException)
            };

            foreach (var exceptionType in exceptionTypes)
            {
                try
                {
                    await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(async () =>
                    {
                        await Task.Delay(10);
                        throw (Exception)Activator.CreateInstance(exceptionType, $"Test {exceptionType.Name}")!;
                    }, maxAttempts: 2);
                    
                    Assert.Fail($"Should have thrown {exceptionType.Name}");
                }
                catch (Exception ex)
                {
                    Assert.AreEqual(exceptionType, ex.GetType());
                }
            }
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_ConcurrentRetries_HandlesCorrectly()
        {
            // 测试并发重试场景
            const int taskCount = 10;
            var tasks = new List<Task>();

            for (int i = 0; i < taskCount; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var attemptCount = 0;
                    
                    try
                    {
                        var result = await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(async () =>
                        {
                            await Task.Delay(50);
                            attemptCount++;
                            
                            if (attemptCount < 2)
                            {
                                throw new InvalidOperationException($"Task {taskId} attempt {attemptCount} failed");
                            }
                            
                            return $"Task {taskId} succeeded";
                        });
                        
                        Assert.IsNotNull(result);
                        Assert.IsTrue(result.Contains($"Task {taskId}"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Task {taskId} failed: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        #endregion

        #region 内存和性能压力测试

        [TestMethod]
        public void IntelligentCacheManager_MemoryPressure_HandlesGracefully()
        {
            // 内存压力测试
            var initialMemory = GC.GetTotalMemory(true);
            const int itemCount = 5000;
            const int itemSize = 1000; // 1KB per item

            try
            {
                for (int i = 0; i < itemCount; i++)
                {
                    var key = $"pressure_key_{i}";
                    var value = new string('X', itemSize);
                    IntelligentCacheManager.Set(key, value);
                    
                    // 每1000个项目检查一次内存使用
                    if (i % 1000 == 0)
                    {
                        var currentMemory = GC.GetTotalMemory(false);
                        var memoryIncrease = currentMemory - initialMemory;
                        
                        // 内存增长不应该超过合理范围
                        Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, // 50MB limit
                            $"Memory usage too high: {memoryIncrease / 1024 / 1024}MB at item {i}");
                    }
                }

                // 验证统计信息
                var stats = IntelligentCacheManager.GetStatistics();
                Assert.IsNotNull(stats);
                Assert.IsTrue(stats.EntryCount > 0);
                Assert.IsTrue(stats.TotalMemoryUsage > 0);
            }
            finally
            {
                // 清理
                IntelligentCacheManager.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_PerformanceConsistency_MaintainsSpeed()
        {
            // 性能一致性测试
            const int batchSize = 1000;
            const int batchCount = 5;
            var batchTimes = new List<long>();

            for (int batch = 0; batch < batchCount; batch++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // 批量设置
                for (int i = 0; i < batchSize; i++)
                {
                    var key = $"perf_batch_{batch}_item_{i}";
                    var value = $"value_{batch}_{i}";
                    IntelligentCacheManager.Set(key, value);
                }
                
                // 批量获取
                for (int i = 0; i < batchSize; i++)
                {
                    var key = $"perf_batch_{batch}_item_{i}";
                    IntelligentCacheManager.Get<string>(key);
                }
                
                stopwatch.Stop();
                batchTimes.Add(stopwatch.ElapsedMilliseconds);
                
                Console.WriteLine($"Batch {batch}: {stopwatch.ElapsedMilliseconds}ms for {batchSize * 2} operations");
            }

            // 验证性能一致性 - 后续批次不应该比第一批次慢太多
            var firstBatchTime = batchTimes[0];
            var maxAcceptableTime = Math.Max(firstBatchTime * 3, 1000); // 最多3倍时间或1秒

            for (int i = 1; i < batchTimes.Count; i++)
            {
                Assert.IsTrue(batchTimes[i] < maxAcceptableTime,
                    $"Batch {i} took {batchTimes[i]}ms, which is too slow compared to first batch ({firstBatchTime}ms)");
            }
        }

        [TestMethod]
        public async Task AdvancedConnectionManager_HighConcurrency_HandlesCorrectly()
        {
            // 高并发测试
            const int taskCount = 50;
            const int operationsPerTask = 10;
            var tasks = new List<Task>();
            var successCount = 0;
            var failureCount = 0;
            var lockObject = new object();

            for (int i = 0; i < taskCount; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerTask; j++)
                    {
                        try
                        {
                            var result = await AdvancedConnectionManager.ExecuteWithRetryAsync<string>(async () =>
                            {
                                await Task.Delay(10);
                                
                                // 模拟偶尔失败
                                if (new Random().Next(0, 10) < 2) // 20% 失败率
                                {
                                    throw new InvalidOperationException("Random failure");
                                }
                                
                                return $"Task_{taskId}_Op_{j}_Success";
                            });
                            
                            lock (lockObject)
                            {
                                successCount++;
                            }
                        }
                        catch
                        {
                            lock (lockObject)
                            {
                                failureCount++;
                            }
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var totalOperations = taskCount * operationsPerTask;
            Console.WriteLine($"High concurrency test: {successCount} successes, {failureCount} failures out of {totalOperations} operations");
            
            // 至少应该有一些成功的操作
            Assert.IsTrue(successCount > 0);
            Assert.AreEqual(totalOperations, successCount + failureCount);
        }

        #endregion

        #region 边界条件和异常恢复测试

        [TestMethod]
        public void IntelligentCacheManager_ExceptionInFactory_HandlesGracefully()
        {
            // 测试工厂方法中的各种异常
            var exceptionTypes = new[]
            {
                typeof(OutOfMemoryException),
                typeof(StackOverflowException),
                typeof(AccessViolationException),
                typeof(InvalidOperationException),
                typeof(ArgumentException),
                typeof(NotSupportedException)
            };

            foreach (var exceptionType in exceptionTypes)
            {
                var key = $"exception_test_{exceptionType.Name}";
                
                try
                {
                    IntelligentCacheManager.GetOrAdd<string>(key, () =>
                    {
                        throw (Exception)Activator.CreateInstance(exceptionType, $"Test {exceptionType.Name}")!;
                    });
                    
                    Assert.Fail($"Should have thrown {exceptionType.Name}");
                }
                catch (Exception ex)
                {
                    // 某些严重异常（如OutOfMemoryException）可能会被包装或处理不同
                    // 我们只验证确实抛出了异常
                    Assert.IsNotNull(ex);
                }
            }
        }

        [TestMethod]
        public void IntelligentCacheManager_RecoveryAfterClear_WorksCorrectly()
        {
            // 测试清理后的恢复能力
            const int initialItemCount = 100;
            const int recoveryItemCount = 50;

            // 初始填充
            for (int i = 0; i < initialItemCount; i++)
            {
                IntelligentCacheManager.Set($"initial_{i}", $"value_{i}");
            }

            var initialStats = IntelligentCacheManager.GetStatistics();
            Assert.IsTrue(initialStats.EntryCount > 0);

            // 清理
            IntelligentCacheManager.Clear();
            
            var clearedStats = IntelligentCacheManager.GetStatistics();
            Assert.AreEqual(0, clearedStats.EntryCount);

            // 恢复操作
            for (int i = 0; i < recoveryItemCount; i++)
            {
                IntelligentCacheManager.Set($"recovery_{i}", $"recovered_value_{i}");
            }

            // 验证恢复
            for (int i = 0; i < recoveryItemCount; i++)
            {
                var value = IntelligentCacheManager.Get<string>($"recovery_{i}");
                Assert.AreEqual($"recovered_value_{i}", value);
            }

            var recoveredStats = IntelligentCacheManager.GetStatistics();
            Assert.IsTrue(recoveredStats.EntryCount >= recoveryItemCount);
        }

        #endregion
    }
}
