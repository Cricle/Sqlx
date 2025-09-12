// -----------------------------------------------------------------------
// <copyright file="PostgreSqlDialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for PostgreSqlDialectProvider to achieve comprehensive coverage.
    /// </summary>
    [TestClass]
    public class PostgreSqlDialectProviderTests
    {
        private PostgreSqlDialectProvider _provider = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _provider = new PostgreSqlDialectProvider();
        }

        [TestMethod]
        public void SqlDefine_ReturnsPostgreSQLFormat()
        {
            // Act
            var sqlDefine = _provider.SqlDefine;

            // Assert
            Assert.IsNotNull(sqlDefine);
            Assert.AreEqual(SqlDefine.PgSql, sqlDefine);
        }

        [TestMethod]
        public void DialectType_ReturnsPostgresql()
        {
            // Act
            var dialectType = _provider.DialectType;

            // Assert
            Assert.AreEqual(SqlDefineTypes.Postgresql, dialectType);
        }

        [TestMethod]
        public void GenerateLimitClause_WithLimitAndOffset_ReturnsCorrectSyntax()
        {
            // Act
            var result = _provider.GenerateLimitClause(10, 5);

            // Assert
            Assert.AreEqual("LIMIT 10 OFFSET 5", result);
        }

        [TestMethod]
        public void GenerateLimitClause_WithLimitOnly_ReturnsCorrectSyntax()
        {
            // Act
            var result = _provider.GenerateLimitClause(10, null);

            // Assert
            Assert.AreEqual("LIMIT 10", result);
        }

        [TestMethod]
        public void GenerateLimitClause_WithOffsetOnly_ReturnsCorrectSyntax()
        {
            // Act
            var result = _provider.GenerateLimitClause(null, 5);

            // Assert
            Assert.AreEqual("OFFSET 5", result);
        }

        [TestMethod]
        public void GenerateLimitClause_WithNoLimitOrOffset_ReturnsEmpty()
        {
            // Act
            var result = _provider.GenerateLimitClause(null, null);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GenerateInsertWithReturning_WithColumns_ReturnsCorrectSQL()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Name", "Email", "Age" };

            // Act
            var result = _provider.GenerateInsertWithReturning(tableName, columns);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("INSERT INTO \"Users\""));
            Assert.IsTrue(result.Contains("(\"Name\", \"Email\", \"Age\")"));
            Assert.IsTrue(result.Contains("VALUES ($1, $2, $3)"));
            Assert.IsTrue(result.Contains("RETURNING \"Id\""));
        }

        [TestMethod]
        public void GenerateBatchInsert_WithColumns_ReturnsCorrectSQL()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Name", "Email" };
            var batchSize = 3;

            // Act
            var result = _provider.GenerateBatchInsert(tableName, columns, batchSize);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("INSERT INTO \"Users\""));
            Assert.IsTrue(result.Contains("(\"Name\", \"Email\")"));
            Assert.IsTrue(result.Contains("($1, $2)"));
            Assert.IsTrue(result.Contains("($3, $4)"));
            Assert.IsTrue(result.Contains("($5, $6)"));
        }

        [TestMethod]
        public void GenerateUpsert_WithKeyColumns_ReturnsCorrectSQL()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Id", "Name", "Email" };
            var keyColumns = new[] { "Id" };

            // Act
            var result = _provider.GenerateUpsert(tableName, columns, keyColumns);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("INSERT INTO \"Users\""));
            Assert.IsTrue(result.Contains("ON CONFLICT (\"Id\")"));
            Assert.IsTrue(result.Contains("DO UPDATE SET"));
            Assert.IsTrue(result.Contains("\"Name\" = EXCLUDED.\"Name\""));
            Assert.IsTrue(result.Contains("\"Email\" = EXCLUDED.\"Email\""));
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithInt32_ReturnsINTEGER()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(int));

            // Assert
            Assert.AreEqual("INTEGER", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithInt64_ReturnsBIGINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(long));

            // Assert
            Assert.AreEqual("BIGINT", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithDecimal_ReturnsDECIMAL()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(decimal));

            // Assert
            Assert.AreEqual("DECIMAL(18,2)", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithDouble_ReturnsDOUBLEPRECISION()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(double));

            // Assert
            Assert.AreEqual("DOUBLE PRECISION", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithFloat_ReturnsREAL()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(float));

            // Assert
            Assert.AreEqual("REAL", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithString_ReturnsVARCHAR()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(string));

            // Assert
            Assert.AreEqual("VARCHAR(4000)", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithDateTime_ReturnsTIMESTAMP()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(DateTime));

            // Assert
            Assert.AreEqual("TIMESTAMP", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithBoolean_ReturnsBOOLEAN()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(bool));

            // Assert
            Assert.AreEqual("BOOLEAN", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithByte_ReturnsSMALLINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(byte));

            // Assert
            Assert.AreEqual("SMALLINT", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithGuid_ReturnsUUID()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(Guid));

            // Assert
            Assert.AreEqual("UUID", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithUnsupportedType_ReturnsVARCHAR()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(object));

            // Assert
            Assert.AreEqual("VARCHAR(4000)", result);
        }

        [TestMethod]
        public void FormatDateTime_WithDateTime_ReturnsCorrectFormat()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 14, 30, 45, 123);

            // Act
            var result = _provider.FormatDateTime(dateTime);

            // Assert
            Assert.AreEqual("'2023-12-25 14:30:45.123'::timestamp", result);
        }

        [TestMethod]
        public void GetCurrentDateTimeSyntax_ReturnsCorrectPostgreSQLFunction()
        {
            // Act
            var result = _provider.GetCurrentDateTimeSyntax();

            // Assert
            Assert.AreEqual("CURRENT_TIMESTAMP", result);
        }

        [TestMethod]
        public void GetConcatenationSyntax_WithMultipleExpressions_ReturnsCorrectSyntax()
        {
            // Arrange
            var expressions = new[] { "field1", "field2", "field3" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual("field1 || field2 || field3", result);
        }

        [TestMethod]
        public void GetConcatenationSyntax_WithTwoExpressions_ReturnsCorrectSyntax()
        {
            // Arrange
            var expressions = new[] { "firstName", "lastName" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual("firstName || lastName", result);
        }

        [TestMethod]
        public void GetConcatenationSyntax_WithOneExpression_ReturnsExpression()
        {
            // Arrange
            var expressions = new[] { "singleField" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual("singleField", result);
        }

        [TestMethod]
        public void GetConcatenationSyntax_WithEmptyArray_ReturnsEmpty()
        {
            // Arrange
            var expressions = new string[0];

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

    }
}
