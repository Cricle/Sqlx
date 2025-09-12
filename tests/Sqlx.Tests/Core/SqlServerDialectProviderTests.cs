// -----------------------------------------------------------------------
// <copyright file="SqlServerDialectProviderTests.cs" company="Cricle">
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
    /// Tests for SqlServerDialectProvider to achieve comprehensive coverage.
    /// </summary>
    [TestClass]
    public class SqlServerDialectProviderTests
    {
        private SqlServerDialectProvider _provider = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _provider = new SqlServerDialectProvider();
        }

        [TestMethod]
        public void SqlDefine_ReturnsSqlServerFormat()
        {
            // Act
            var sqlDefine = _provider.SqlDefine;

            // Assert
            Assert.IsNotNull(sqlDefine);
            Assert.AreEqual(SqlDefine.SqlServer, sqlDefine);
        }

        [TestMethod]
        public void DialectType_ReturnsSqlServer()
        {
            // Act
            var dialectType = _provider.DialectType;

            // Assert
            Assert.AreEqual(SqlDefineTypes.SqlServer, dialectType);
        }

        [TestMethod]
        public void GenerateLimitClause_WithLimitAndOffset_ReturnsCorrectSqlServerSyntax()
        {
            // Act
            var result = _provider.GenerateLimitClause(10, 5);

            // Assert - SQL Server uses OFFSET...FETCH syntax
            Assert.AreEqual("OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY", result);
        }

        [TestMethod]
        public void GenerateLimitClause_WithLimitOnly_ReturnsCorrectSyntax()
        {
            // Act
            var result = _provider.GenerateLimitClause(10, null);

            // Assert
            Assert.AreEqual("OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", result);
        }

        [TestMethod]
        public void GenerateLimitClause_WithOffsetOnly_ReturnsCorrectSyntax()
        {
            // Act
            var result = _provider.GenerateLimitClause(null, 5);

            // Assert
            Assert.AreEqual("OFFSET 5 ROWS", result);
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
            Assert.IsTrue(result.Contains("INSERT INTO [Users]"));
            Assert.IsTrue(result.Contains("([Name], [Email], [Age])"));
            Assert.IsTrue(result.Contains("OUTPUT INSERTED.[Id]"));
            Assert.IsTrue(result.Contains("VALUES (@name, @email, @age)"));
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
            Assert.IsTrue(result.Contains("INSERT INTO [Users]"));
            Assert.IsTrue(result.Contains("([Name], [Email])"));
            Assert.IsTrue(result.Contains("(@name0, @email0)"));
            Assert.IsTrue(result.Contains("(@name1, @email1)"));
            Assert.IsTrue(result.Contains("(@name2, @email2)"));
        }

        [TestMethod]
        public void GenerateUpsert_WithKeyColumns_ReturnsCorrectMERGEStatement()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Id", "Name", "Email" };
            var keyColumns = new[] { "Id" };

            // Act
            var result = _provider.GenerateUpsert(tableName, columns, keyColumns);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("MERGE [Users] AS target"));
            Assert.IsTrue(result.Contains("USING ("));
            Assert.IsTrue(result.Contains("VALUES (@id, @name, @email)"));
            Assert.IsTrue(result.Contains("AS source ([Id], [Name], [Email])"));
            Assert.IsTrue(result.Contains("ON (target.[Id] = source.[Id])"));
            Assert.IsTrue(result.Contains("WHEN MATCHED THEN"));
            Assert.IsTrue(result.Contains("UPDATE SET"));
            Assert.IsTrue(result.Contains("[Name] = source.[Name]"));
            Assert.IsTrue(result.Contains("[Email] = source.[Email]"));
            Assert.IsTrue(result.Contains("WHEN NOT MATCHED THEN"));
            Assert.IsTrue(result.Contains("INSERT ([Id], [Name], [Email])"));
            Assert.IsTrue(result.Contains("VALUES (source.[Id], source.[Name], source.[Email])"));
        }

        [TestMethod]
        public void GenerateUpsert_WithMultipleKeyColumns_ReturnsCorrectSQL()
        {
            // Arrange
            var tableName = "UserProfiles";
            var columns = new[] { "UserId", "ProfileId", "DisplayName", "LastModified" };
            var keyColumns = new[] { "UserId", "ProfileId" };

            // Act
            var result = _provider.GenerateUpsert(tableName, columns, keyColumns);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("target.[UserId] = source.[UserId] AND target.[ProfileId] = source.[ProfileId]"));
            Assert.IsTrue(result.Contains("[DisplayName] = source.[DisplayName]"));
            Assert.IsTrue(result.Contains("[LastModified] = source.[LastModified]"));
            Assert.IsFalse(result.Contains("UserId] = source.[UserId],"));
            Assert.IsFalse(result.Contains("ProfileId] = source.[ProfileId],"));
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithInt32_ReturnsINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(int));

            // Assert
            Assert.AreEqual("INT", result);
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
        public void GetDatabaseTypeName_WithDouble_ReturnsFLOAT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(double));

            // Assert
            Assert.AreEqual("FLOAT", result);
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
        public void GetDatabaseTypeName_WithString_ReturnsNVARCHAR()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(string));

            // Assert
            Assert.AreEqual("NVARCHAR(4000)", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithDateTime_ReturnsDATETIME2()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(DateTime));

            // Assert
            Assert.AreEqual("DATETIME2", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithBoolean_ReturnsBIT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(bool));

            // Assert
            Assert.AreEqual("BIT", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithByte_ReturnsTINYINT()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(byte));

            // Assert
            Assert.AreEqual("TINYINT", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithGuid_ReturnsUNIQUEIDENTIFIER()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(Guid));

            // Assert
            Assert.AreEqual("UNIQUEIDENTIFIER", result);
        }

        [TestMethod]
        public void GetDatabaseTypeName_WithUnsupportedType_ReturnsNVARCHAR()
        {
            // Act
            var result = _provider.GetDatabaseTypeName(typeof(object));

            // Assert
            Assert.AreEqual("NVARCHAR(4000)", result);
        }

        [TestMethod]
        public void FormatDateTime_WithDateTime_ReturnsCorrectFormat()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 14, 30, 45, 123);

            // Act
            var result = _provider.FormatDateTime(dateTime);

            // Assert
            Assert.AreEqual("'2023-12-25 14:30:45.123'", result);
        }

        [TestMethod]
        public void GetCurrentDateTimeSyntax_ReturnsCorrectSqlServerFunction()
        {
            // Act
            var result = _provider.GetCurrentDateTimeSyntax();

            // Assert
            Assert.AreEqual("GETDATE()", result);
        }

        [TestMethod]
        public void GetConcatenationSyntax_WithMultipleExpressions_ReturnsCorrectSyntax()
        {
            // Arrange
            var expressions = new[] { "field1", "field2", "field3" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert - SQL Server uses + for concatenation
            Assert.AreEqual("field1 + field2 + field3", result);
        }

        [TestMethod]
        public void GetConcatenationSyntax_WithTwoExpressions_ReturnsCorrectSyntax()
        {
            // Arrange
            var expressions = new[] { "firstName", "lastName" };

            // Act
            var result = _provider.GetConcatenationSyntax(expressions);

            // Assert
            Assert.AreEqual("firstName + lastName", result);
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

        [TestMethod]
        public void GenerateUpsert_ComplexScenario_GeneratesValidMERGE()
        {
            // Arrange
            var tableName = "Products";
            var columns = new[] { "ProductId", "CategoryId", "Name", "Price", "Stock", "LastUpdated" };
            var keyColumns = new[] { "ProductId" };

            // Act
            var result = _provider.GenerateUpsert(tableName, columns, keyColumns);

            // Assert
            Assert.IsNotNull(result);
            
            // Verify structure
            Assert.IsTrue(result.Contains("MERGE [Products] AS target"));
            Assert.IsTrue(result.Contains("VALUES (@productid, @categoryid, @name, @price, @stock, @lastupdated)"));
            Assert.IsTrue(result.Contains("AS source ([ProductId], [CategoryId], [Name], [Price], [Stock], [LastUpdated])"));
            Assert.IsTrue(result.Contains("ON (target.[ProductId] = source.[ProductId])"));
            
            // Verify UPDATE clause doesn't include key column
            Assert.IsTrue(result.Contains("[CategoryId] = source.[CategoryId]"));
            Assert.IsTrue(result.Contains("[Name] = source.[Name]"));
            Assert.IsTrue(result.Contains("[Price] = source.[Price]"));
            Assert.IsTrue(result.Contains("[Stock] = source.[Stock]"));
            Assert.IsTrue(result.Contains("[LastUpdated] = source.[LastUpdated]"));
            Assert.IsFalse(result.Contains("[ProductId] = source.[ProductId],")); // Key column should not be in UPDATE
            
            // Verify INSERT clause
            Assert.IsTrue(result.Contains("INSERT ([ProductId], [CategoryId], [Name], [Price], [Stock], [LastUpdated])"));
            Assert.IsTrue(result.Contains("VALUES (source.[ProductId], source.[CategoryId], source.[Name], source.[Price], source.[Stock], source.[LastUpdated])"));
        }
    }
}
