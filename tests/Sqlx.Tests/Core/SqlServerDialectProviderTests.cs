// -----------------------------------------------------------------------
// <copyright file="SqlServerDialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Comprehensive tests for SqlServerDialectProvider to achieve 100% code coverage
    /// </summary>
    [TestClass]
    public class SqlServerDialectProviderTests
    {
        private SqlServerDialectProvider _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new SqlServerDialectProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up after tests
        }

        [TestMethod]
        public void SqlServerDialectProvider_SqlDefine_ReturnsSqlServerDefine()
        {
            // Act
            var sqlDefine = _provider.SqlDefine;

            // Assert
            Assert.IsNotNull(sqlDefine, "SqlDefine should not be null");
            Assert.AreEqual(SqlDefine.SqlServer, sqlDefine, "Should return SQL Server define");
        }

        [TestMethod]
        public void SqlServerDialectProvider_DialectType_ReturnsSqlServer()
        {
            // Act
            var dialectType = _provider.DialectType;

            // Assert
            Assert.AreEqual(SqlDefineTypes.SqlServer, dialectType, "Should return SQL Server dialect type");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateLimitClause_WithLimitAndOffset_ReturnsCorrectClause()
        {
            // Arrange
            var limit = 10;
            var offset = 20;

            // Act
            var result = _provider.GenerateLimitClause(limit, offset);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("OFFSET 20 ROWS"), "Should contain offset clause");
            Assert.IsTrue(result.Contains("FETCH NEXT 10 ROWS ONLY"), "Should contain fetch clause");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateLimitClause_WithLimitOnly_ReturnsCorrectClause()
        {
            // Arrange
            var limit = 15;
            int? offset = null;

            // Act
            var result = _provider.GenerateLimitClause(limit, offset);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("OFFSET 0 ROWS"), "Should contain zero offset");
            Assert.IsTrue(result.Contains("FETCH NEXT 15 ROWS ONLY"), "Should contain fetch clause");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateLimitClause_WithOffsetOnly_ReturnsCorrectClause()
        {
            // Arrange
            int? limit = null;
            var offset = 25;

            // Act
            var result = _provider.GenerateLimitClause(limit, offset);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("OFFSET 25 ROWS"), "Should contain offset clause");
            Assert.IsFalse(result.Contains("FETCH"), "Should not contain fetch clause");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateLimitClause_WithNullValues_ReturnsEmpty()
        {
            // Arrange
            int? limit = null;
            int? offset = null;

            // Act
            var result = _provider.GenerateLimitClause(limit, offset);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for null values");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateInsertWithReturning_WithValidData_ReturnsCorrectSql()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Name", "Email", "Age" };

            // Act
            var result = _provider.GenerateInsertWithReturning(tableName, columns);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("INSERT INTO"), "Should contain INSERT INTO");
            Assert.IsTrue(result.Contains("OUTPUT INSERTED"), "Should contain OUTPUT INSERTED clause");
            Assert.IsTrue(result.Contains("VALUES"), "Should contain VALUES clause");
            Assert.IsTrue(result.Contains("@name"), "Should contain lowercase parameter names");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateInsertWithReturning_WithSingleColumn_ReturnsCorrectSql()
        {
            // Arrange
            var tableName = "Categories";
            var columns = new[] { "Name" };

            // Act
            var result = _provider.GenerateInsertWithReturning(tableName, columns);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("[Categories]"), "Should contain wrapped table name");
            Assert.IsTrue(result.Contains("[Name]"), "Should contain wrapped column name");
            Assert.IsTrue(result.Contains("@name"), "Should contain parameter");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateBatchInsert_WithValidData_ReturnsCorrectSql()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Name", "Email" };
            var batchSize = 3;

            // Act
            var result = _provider.GenerateBatchInsert(tableName, columns, batchSize);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("INSERT INTO"), "Should contain INSERT INTO");
            Assert.IsTrue(result.Contains("VALUES"), "Should contain VALUES");
            Assert.IsTrue(result.Contains("@name0"), "Should contain first batch parameters");
            Assert.IsTrue(result.Contains("@name2"), "Should contain last batch parameters");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateBatchInsert_WithSingleBatch_ReturnsCorrectSql()
        {
            // Arrange
            var tableName = "Products";
            var columns = new[] { "Name", "Price" };
            var batchSize = 1;

            // Act
            var result = _provider.GenerateBatchInsert(tableName, columns, batchSize);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("@name0"), "Should contain batch 0 parameters");
            Assert.IsFalse(result.Contains("@name1"), "Should not contain batch 1 parameters");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateUpsert_WithValidData_ReturnsCorrectMergeStatement()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Id", "Name", "Email" };
            var keyColumns = new[] { "Id" };

            // Act
            var result = _provider.GenerateUpsert(tableName, columns, keyColumns);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("MERGE"), "Should contain MERGE statement");
            Assert.IsTrue(result.Contains("WHEN MATCHED THEN"), "Should contain WHEN MATCHED clause");
            Assert.IsTrue(result.Contains("WHEN NOT MATCHED THEN"), "Should contain WHEN NOT MATCHED clause");
            Assert.IsTrue(result.Contains("UPDATE SET"), "Should contain UPDATE SET");
            Assert.IsTrue(result.Contains("INSERT"), "Should contain INSERT");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GenerateUpsert_WithMultipleKeys_ReturnsCorrectMergeStatement()
        {
            // Arrange
            var tableName = "UserProfiles";
            var columns = new[] { "UserId", "ProfileId", "Name", "Value" };
            var keyColumns = new[] { "UserId", "ProfileId" };

            // Act
            var result = _provider.GenerateUpsert(tableName, columns, keyColumns);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("target.[UserId] = source.[UserId] AND target.[ProfileId] = source.[ProfileId]"),
                "Should contain compound key condition");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithInt32_ReturnsINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(int));

            // Assert
            Assert.AreEqual("INT", result, "Int32 should map to INT");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithInt64_ReturnsBIGINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(long));

            // Assert
            Assert.AreEqual("BIGINT", result, "Int64 should map to BIGINT");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithDecimal_ReturnsDECIMAL()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(decimal));

            // Assert
            Assert.AreEqual("DECIMAL(18,2)", result, "Decimal should map to DECIMAL(18,2)");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithDouble_ReturnsFLOAT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(double));

            // Assert
            Assert.AreEqual("FLOAT", result, "Double should map to FLOAT");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithSingle_ReturnsREAL()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(float));

            // Assert
            Assert.AreEqual("REAL", result, "Single should map to REAL");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithString_ReturnsNVARCHAR()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(string));

            // Assert
            Assert.AreEqual("NVARCHAR(4000)", result, "String should map to NVARCHAR(4000)");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithDateTime_ReturnsDATETIME2()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(DateTime));

            // Assert
            Assert.AreEqual("DATETIME2", result, "DateTime should map to DATETIME2");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithBoolean_ReturnsBIT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(bool));

            // Assert
            Assert.AreEqual("BIT", result, "Boolean should map to BIT");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithByte_ReturnsTINYINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(byte));

            // Assert
            Assert.AreEqual("TINYINT", result, "Byte should map to TINYINT");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithGuid_ReturnsUNIQUEIDENTIFIER()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(Guid));

            // Assert
            Assert.AreEqual("UNIQUEIDENTIFIER", result, "Guid should map to UNIQUEIDENTIFIER");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetDatabaseTypeName_WithUnknownType_ReturnsNVARCHAR()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(object));

            // Assert
            Assert.AreEqual("NVARCHAR(4000)", result, "Unknown type should map to NVARCHAR(4000)");
        }

        [TestMethod]
        public void SqlServerDialectProvider_FormatDateTime_WithValidDateTime_ReturnsFormattedString()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 15, 30, 45, 123);

            // Act
            var result = _provider.FormatDateTime(dateTime);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.Contains("2023-12-25"), "Should contain date part");
            Assert.IsTrue(result.Contains("15:30:45"), "Should contain time part");
            Assert.IsTrue(result.Contains("123"), "Should contain milliseconds");
            Assert.IsTrue(result.StartsWith("'") && result.EndsWith("'"), "Should be wrapped in quotes");
        }

        [TestMethod]
        public void SqlServerDialectProvider_FormatDateTime_WithMinValue_ReturnsFormattedString()
        {
            // Arrange
            var dateTime = DateTime.MinValue;

            // Act
            var result = _provider.FormatDateTime(dateTime);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsTrue(result.StartsWith("'") && result.EndsWith("'"), "Should be wrapped in quotes");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetCurrentDateTimeSyntax_ReturnsGETDATE()
        {
            // Act
            var result = _provider.GetCurrentDateTimeSyntax();

            // Assert
            Assert.AreEqual("GETDATE()", result, "Should return GETDATE() function");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetConcatenationSyntax_WithMultipleExpressions_ReturnsCorrectSyntax()
        {
            // Arrange
            var expressions = new[] { "FirstName", "' '", "LastName" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual("FirstName + ' ' + LastName", result, "Should use + operator for concatenation");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetConcatenationSyntax_WithTwoExpressions_ReturnsCorrectSyntax()
        {
            // Arrange
            var expressions = new[] { "Name", "Suffix" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual("Name + Suffix", result, "Should concatenate two expressions with +");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetConcatenationSyntax_WithSingleExpression_ReturnsExpression()
        {
            // Arrange
            var expressions = new[] { "SingleValue" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual("SingleValue", result, "Should return single expression as-is");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetConcatenationSyntax_WithEmptyArray_ReturnsEmpty()
        {
            // Arrange
            var expressions = new string[0];

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for empty array");
        }

        [TestMethod]
        public void SqlServerDialectProvider_GetConcatenationSyntax_WithNullArray_ThrowsException()
        {
            // Arrange
            string[]? expressions = null;

            // Act & Assert
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                _provider.GetConcatenationSyntax(expressions!);
            }, "Should throw NullReferenceException for null array");
        }
    }
}