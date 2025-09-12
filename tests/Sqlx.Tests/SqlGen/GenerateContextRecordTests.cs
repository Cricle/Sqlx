// -----------------------------------------------------------------------
// <copyright file="GenerateContextRecordTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;
using Sqlx.SqlGen;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.SqlGen
{
    /// <summary>
    /// Tests for GenerateContext record classes to achieve 100% coverage.
    /// </summary>
    [TestClass]
    public class GenerateContextRecordTests
    {
        [TestMethod]
        public void SelectGenerateContext_GetColumnNames_ReturnsAliasedColumns()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();
            var context = new SelectGenerateContext(null!, tableName, objectMap);

            // Act
            var result = context.GetColumnNames();

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, " AS ");
            StringAssert.Contains(result, "Name");
        }

        [TestMethod]
        public void SelectGenerateContext_Properties_ReturnCorrectValues()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();
            var context = new SelectGenerateContext(null!, tableName, objectMap);

            // Act & Assert
            Assert.AreEqual(tableName, context.TableName);
            Assert.AreEqual(objectMap, context.Entry);
        }

        [TestMethod]
        public void UpdateGenerateContext_GetUpdateSet_ReturnsSetClause()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();
            var context = new UpdateGenerateContext(null!, tableName, objectMap);
            var prefix = "@";

            // Act
            var result = context.GetUpdateSet(prefix);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "=");
            StringAssert.Contains(result, prefix);
            StringAssert.Contains(result, "name");
        }

        [TestMethod]
        public void UpdateGenerateContext_Properties_ReturnCorrectValues()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();
            var context = new UpdateGenerateContext(null!, tableName, objectMap);

            // Act & Assert
            Assert.AreEqual(tableName, context.TableName);
            Assert.AreEqual(objectMap, context.Entry);
        }

        [TestMethod]
        public void DeleteGenerateContext_CanBeCreated()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();

            // Act
            var context = new DeleteGenerateContext(null!, tableName, objectMap);

            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(tableName, context.TableName);
            Assert.AreEqual(objectMap, context.Entry);
        }

        [TestMethod]
        public void DeleteGenerateContext_Properties_ReturnCorrectValues()
        {
            // Arrange
            var tableName = "Products";
            var objectMap = CreateMockObjectMap();
            var context = new DeleteGenerateContext(null!, tableName, objectMap);

            // Act & Assert
            Assert.AreEqual(tableName, context.TableName);
            Assert.AreEqual(objectMap, context.Entry);
            Assert.IsNull(context.Context); // We passed null for simplicity
        }

        [TestMethod]
        public void InsertGenerateContext_GetParameterNames_WithPrefix()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();
            var context = new InsertGenerateContext(null!, tableName, objectMap);
            var prefix = "@";

            // Act
            var result = context.GetParamterNames(prefix);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, prefix);
            StringAssert.Contains(result, "name");
        }

        [TestMethod]
        public void InsertGenerateContext_GetColumnNames_ReturnsJoinedNames()
        {
            // Arrange
            var tableName = "Users";
            var objectMap = CreateMockObjectMap();
            var context = new InsertGenerateContext(null!, tableName, objectMap);

            // Act
            var result = context.GetColumnNames();

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "name");
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithNullName_ReturnsNull()
        {
            // Act
            var result = GenerateContext.GetColumnName(null!);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithEmptyName_ReturnsEmpty()
        {
            // Act
            var result = GenerateContext.GetColumnName("");

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithUpperCaseUnderscores_ReturnsLowerCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("USER_NAME");

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithPascalCase_ReturnsSnakeCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("UserName");

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetParamterName_WithPrefixAndName_ReturnsFormattedName()
        {
            // Act
            var result = GenerateContext.GetParamterName("@", "UserName");

            // Assert
            Assert.AreEqual("@user_name", result);
        }

        // Helper methods
        private ObjectMap CreateMockObjectMap()
        {
            // Create mock parameter symbol
            var paramMock = new Mock<IParameterSymbol>();
            paramMock.Setup(x => x.Name).Returns("TestParam");
            paramMock.Setup(x => x.Type).Returns(CreateMockNamedType());
            
            return new ObjectMap(paramMock.Object);
        }
        
        private INamedTypeSymbol CreateMockNamedType()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.Name).Returns("TestType");
            
            // Create mock property
            var prop = new Mock<IPropertySymbol>();
            prop.Setup(x => x.Name).Returns("Name");
            prop.Setup(x => x.Type).Returns(CreateMockStringType());
            prop.Setup(x => x.CanBeReferencedByName).Returns(true);
            prop.Setup(x => x.GetAttributes()).Returns(System.Collections.Immutable.ImmutableArray<AttributeData>.Empty);
            
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray.Create<ISymbol>(prop.Object));
            
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
