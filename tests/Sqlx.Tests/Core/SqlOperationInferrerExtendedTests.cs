// -----------------------------------------------------------------------
// <copyright file="SqlOperationInferrerExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx.Core;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Extended tests for SqlOperationInferrer class to improve coverage.
    /// </summary>
    [TestClass]
    public class SqlOperationInferrerExtendedTests
    {
        [TestMethod]
        public void InferOperation_WithNullMethod_ReturnsSelect()
        {
            // Act
            var result = SqlOperationInferrer.InferOperation(null!);

            // Assert
            Assert.AreEqual(SqlOperationType.Select, result);
        }

        [TestMethod]
        public void InferOperation_WithSelectMethodName_ReturnsSelect()
        {
            // Arrange
            var method = CreateMockMethod("GetUsers", CreateMockReturnType());

            // Act
            var result = SqlOperationInferrer.InferOperation(method!);

            // Assert
            Assert.AreEqual(SqlOperationType.Select, result);
        }

        [TestMethod]
        public void InferOperation_WithInsertMethodName_ReturnsInsert()
        {
            // Arrange
            var method = CreateMockMethod("CreateUser", CreateMockReturnType());

            // Act
            var result = SqlOperationInferrer.InferOperation(method);

            // Assert
            Assert.AreEqual(SqlOperationType.Insert, result);
        }

        [TestMethod]
        public void InferOperation_WithUpdateMethodName_ReturnsUpdate()
        {
            // Arrange
            var method = CreateMockMethod("UpdateUser", CreateMockReturnType());

            // Act
            var result = SqlOperationInferrer.InferOperation(method);

            // Assert
            Assert.AreEqual(SqlOperationType.Update, result);
        }

        [TestMethod]
        public void InferOperation_WithDeleteMethodName_ReturnsDelete()
        {
            // Arrange
            var method = CreateMockMethod("DeleteUser", CreateMockReturnType());

            // Act
            var result = SqlOperationInferrer.InferOperation(method);

            // Assert
            Assert.AreEqual(SqlOperationType.Delete, result);
        }

        [TestMethod]
        public void InferOperation_WithScalarMethodName_ReturnsScalar()
        {
            // Arrange
            var method = CreateMockMethod("CountUsers", CreateMockReturnType());

            // Act
            var result = SqlOperationInferrer.InferOperation(method);

            // Assert
            Assert.AreEqual(SqlOperationType.Scalar, result);
        }

        // Note: Testing with SqlExecuteTypeAttribute is complex due to TypedConstant creation
        // This functionality is covered by integration tests

        [TestMethod]
        public void GenerateSqlTemplate_WithSelectOperation_ReturnsSelectSql()
        {
            // Arrange
            var tableName = "Users";

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Select, tableName, null);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "SELECT");
            StringAssert.Contains(result, tableName);
        }

        [TestMethod]
        public void GenerateSqlTemplate_WithInsertOperation_ReturnsInsertSql()
        {
            // Arrange
            var tableName = "Users";
            var entityType = CreateMockEntityType();

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Insert, tableName, entityType);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "INSERT");
            StringAssert.Contains(result, tableName);
        }

        [TestMethod]
        public void GenerateSqlTemplate_WithUpdateOperation_ReturnsUpdateSql()
        {
            // Arrange
            var tableName = "Users";
            var entityType = CreateMockEntityType();

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Update, tableName, entityType);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "UPDATE");
            StringAssert.Contains(result, tableName);
        }

        [TestMethod]
        public void GenerateSqlTemplate_WithDeleteOperation_ReturnsDeleteSql()
        {
            // Arrange
            var tableName = "Users";

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Delete, tableName, null);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "DELETE");
            StringAssert.Contains(result, tableName);
        }

        [TestMethod]
        public void GenerateSqlTemplate_WithScalarOperation_ReturnsScalarSql()
        {
            // Arrange
            var tableName = "Users";

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Scalar, tableName, null);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "COUNT");
            StringAssert.Contains(result, tableName);
        }

        [TestMethod]
        public void GenerateSqlTemplate_WithInsertAndNullEntity_ReturnsGenericInsert()
        {
            // Arrange
            var tableName = "Users";

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Insert, tableName, null);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "INSERT");
            StringAssert.Contains(result, tableName);
        }

        [TestMethod]
        public void GenerateSqlTemplate_WithUpdateAndNullEntity_ReturnsGenericUpdate()
        {
            // Arrange
            var tableName = "Users";

            // Act
            var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Update, tableName, null);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "UPDATE");
            StringAssert.Contains(result, tableName);
        }

        // Helper methods
        private IMethodSymbol CreateMockMethod(string methodName, ITypeSymbol returnType)
        {
            var mock = new Mock<IMethodSymbol>();
            mock.Setup(x => x.Name).Returns(methodName);
            mock.Setup(x => x.ReturnType).Returns(returnType);
            mock.Setup(x => x.Parameters).Returns(System.Collections.Immutable.ImmutableArray<IParameterSymbol>.Empty);
            mock.Setup(x => x.GetAttributes()).Returns(System.Collections.Immutable.ImmutableArray<AttributeData>.Empty);
            return mock.Object;
        }

        // Helper method for creating mock method with attributes removed due to complexity

        private ITypeSymbol CreateMockReturnType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_Void);
            mock.Setup(x => x.Name).Returns("void");
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockEntityType()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.Name).Returns("User");
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);

            // Create mock properties
            var nameProp = new Mock<IPropertySymbol>();
            nameProp.Setup(x => x.Name).Returns("Name");
            nameProp.Setup(x => x.IsReadOnly).Returns(false);
            nameProp.Setup(x => x.Type).Returns(CreateMockStringType());

            var emailProp = new Mock<IPropertySymbol>();
            emailProp.Setup(x => x.Name).Returns("Email");
            emailProp.Setup(x => x.IsReadOnly).Returns(false);
            emailProp.Setup(x => x.Type).Returns(CreateMockStringType());

            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray.Create<ISymbol>(
                nameProp.Object, 
                emailProp.Object));

            return mock.Object;
        }

        private ITypeSymbol CreateMockStringType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_String);
            mock.Setup(x => x.Name).Returns("String");
            return mock.Object;
        }
    }
}
