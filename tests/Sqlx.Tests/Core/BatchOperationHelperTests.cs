// -----------------------------------------------------------------------
// <copyright file="BatchOperationHelperTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for BatchOperationHelper to improve code coverage
    /// </summary>
    [TestClass]
    public class BatchOperationHelperTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Clear any existing state
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up after tests
        }

        [TestMethod]
        public void BatchOperationHelper_HandleBatchInsertOperation_WithValidTable_ReturnsValidSql()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.HandleBatchInsertOperation(tableName);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("BATCH_INSERT"), "Result should contain BATCH_INSERT");
            Assert.IsTrue(result.Contains(tableName), "Result should contain table name");
        }

        [TestMethod]
        public void BatchOperationHelper_HandleBatchUpdateOperation_WithValidTable_ReturnsValidSql()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.HandleBatchUpdateOperation(tableName);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("BATCH_UPDATE"), "Result should contain BATCH_UPDATE");
            Assert.IsTrue(result.Contains(tableName), "Result should contain table name");
        }

        [TestMethod]
        public void BatchOperationHelper_HandleBatchDeleteOperation_WithValidTable_ReturnsValidSql()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.HandleBatchDeleteOperation(tableName);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("BATCH_DELETE"), "Result should contain BATCH_DELETE");
            Assert.IsTrue(result.Contains(tableName), "Result should contain table name");
        }

        [TestMethod]
        public void BatchOperationHelper_IsBatchOperation_WithBatchSql_ReturnsTrue()
        {
            // Arrange
            var batchSql = "BATCH_INSERT:Users";
            
            // Act
            var result = BatchOperationHelper.IsBatchOperation(batchSql);
            
            // Assert
            Assert.IsTrue(result, "Should recognize batch operation SQL");
        }

        [TestMethod]
        public void BatchOperationHelper_IsBatchOperation_WithNonBatchSql_ReturnsFalse()
        {
            // Arrange
            var normalSql = "SELECT * FROM Users";
            
            // Act
            var result = BatchOperationHelper.IsBatchOperation(normalSql);
            
            // Assert
            Assert.IsFalse(result, "Should not recognize normal SQL as batch operation");
        }

        [TestMethod]
        public void BatchOperationHelper_IsBatchOperation_WithNullSql_ReturnsFalse()
        {
            // Arrange
            string? nullSql = null;
            
            // Act
            var result = BatchOperationHelper.IsBatchOperation(nullSql!);
            
            // Assert
            Assert.IsFalse(result, "Should handle null SQL gracefully");
        }

        [TestMethod]
        public void BatchOperationHelper_GetBatchSql_WithBatchInsert_ReturnsInsertSql()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.GetBatchSql(SqlExecuteTypes.BatchInsert, tableName);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("BATCH_INSERT"), "Result should contain BATCH_INSERT");
        }

        [TestMethod]
        public void BatchOperationHelper_GetBatchSql_WithBatchUpdate_ReturnsUpdateSql()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.GetBatchSql(SqlExecuteTypes.BatchUpdate, tableName);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("BATCH_UPDATE"), "Result should contain BATCH_UPDATE");
        }

        [TestMethod]
        public void BatchOperationHelper_GetBatchSql_WithBatchDelete_ReturnsDeleteSql()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.GetBatchSql(SqlExecuteTypes.BatchDelete, tableName);
            
            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("BATCH_DELETE"), "Result should contain BATCH_DELETE");
        }

        [TestMethod]
        public void BatchOperationHelper_GetBatchSql_WithNonBatchType_ReturnsEmpty()
        {
            // Arrange
            var tableName = "Users";
            
            // Act
            var result = BatchOperationHelper.GetBatchSql(SqlExecuteTypes.Select, tableName);
            
            // Assert
            Assert.AreEqual(string.Empty, result, "Non-batch type should return empty string");
        }
    }
}
