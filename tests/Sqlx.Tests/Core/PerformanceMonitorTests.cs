// -----------------------------------------------------------------------
// <copyright file="PerformanceMonitorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for PerformanceMonitor to improve code coverage
    /// </summary>
    [TestClass]
    public class PerformanceMonitorTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Reset performance monitor state
            PerformanceMonitor.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up after tests
            PerformanceMonitor.Clear();
        }

        [TestMethod]
        public void PerformanceMonitor_RecordOperation_ExecutesSuccessfully()
        {
            // Arrange
            var initialCount = PerformanceMonitor.TotalOperations;
            
            // Act
            PerformanceMonitor.RecordOperation();
            
            // Assert
            Assert.AreEqual(initialCount + 1, PerformanceMonitor.TotalOperations, "Operation count should increment");
        }

        [TestMethod]
        public void PerformanceMonitor_TotalOperations_ReturnsValidCount()
        {
            // Arrange
            PerformanceMonitor.Clear();
            
            // Act
            var initialCount = PerformanceMonitor.TotalOperations;
            PerformanceMonitor.RecordOperation();
            PerformanceMonitor.RecordOperation();
            var finalCount = PerformanceMonitor.TotalOperations;
            
            // Assert
            Assert.AreEqual(0, initialCount, "Initial count should be zero after clear");
            Assert.AreEqual(2, finalCount, "Final count should be 2 after two operations");
        }

        [TestMethod]
        public void PerformanceMonitor_Clear_ResetsOperationCount()
        {
            // Arrange
            PerformanceMonitor.RecordOperation();
            PerformanceMonitor.RecordOperation();
            var countBeforeClear = PerformanceMonitor.TotalOperations;
            
            // Act
            PerformanceMonitor.Clear();
            var countAfterClear = PerformanceMonitor.TotalOperations;
            
            // Assert
            Assert.IsTrue(countBeforeClear >= 2, "Should have operations before clear");
            Assert.AreEqual(0, countAfterClear, "Count should be zero after clear");
        }

        [TestMethod]
        public void PerformanceMonitor_MultipleOperations_CountsCorrectly()
        {
            // Arrange
            PerformanceMonitor.Clear();
            var operationCount = 10;
            
            // Act
            for (int i = 0; i < operationCount; i++)
            {
                PerformanceMonitor.RecordOperation();
            }
            
            // Assert
            Assert.AreEqual(operationCount, PerformanceMonitor.TotalOperations, 
                $"Should record {operationCount} operations");
        }

        [TestMethod]
        public void PerformanceMonitor_ThreadSafety_HandlesMultipleThreads()
        {
            // Arrange
            PerformanceMonitor.Clear();
            var threadCount = 10;
            var operationsPerThread = 100;
            var tasks = new Task[threadCount];
            
            // Act
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        PerformanceMonitor.RecordOperation();
                    }
                });
            }
            
            Task.WaitAll(tasks);
            
            // Assert
            Assert.AreEqual(threadCount * operationsPerThread, PerformanceMonitor.TotalOperations,
                "Thread-safe operation counting should be accurate");
        }
    }
}
