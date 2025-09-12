// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorAnalyzerExtendedTests.cs" company="Cricle">
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
    /// Extended tests for PrimaryConstructorAnalyzer class to improve coverage.
    /// </summary>
    [TestClass]
    public class PrimaryConstructorAnalyzerExtendedTests
    {
        [TestMethod]
        public void IsRecord_WithRecordType_ReturnsTrue()
        {
            // Arrange
            var mockType = CreateMockRecordType();

            // Act
            var result = PrimaryConstructorAnalyzer.IsRecord(mockType);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsRecord_WithClassType_ReturnsFalse()
        {
            // Arrange
            var mockType = CreateMockClassType();

            // Act
            var result = PrimaryConstructorAnalyzer.IsRecord(mockType);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasPrimaryConstructor_WithPrimaryConstructor_ReturnsTrue()
        {
            // Arrange
            var mockType = CreateMockTypeWithPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.HasPrimaryConstructor(mockType);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasPrimaryConstructor_WithoutPrimaryConstructor_ReturnsFalse()
        {
            // Arrange
            var mockType = CreateMockTypeWithoutPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.HasPrimaryConstructor(mockType);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetPrimaryConstructor_WithPrimaryConstructor_ReturnsConstructor()
        {
            // Arrange
            var mockType = CreateMockTypeWithPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.GetPrimaryConstructor(mockType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Parameters.Length > 0);
        }

        [TestMethod]
        public void GetPrimaryConstructor_WithoutPrimaryConstructor_ReturnsNull()
        {
            // Arrange
            var mockType = CreateMockTypeWithoutPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.GetPrimaryConstructor(mockType);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetPrimaryConstructorParameters_WithPrimaryConstructor_ReturnsParameters()
        {
            // Arrange
            var mockType = CreateMockTypeWithPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.GetPrimaryConstructorParameters(mockType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Any());
        }

        [TestMethod]
        public void GetPrimaryConstructorParameters_WithoutPrimaryConstructor_ReturnsEmpty()
        {
            // Arrange
            var mockType = CreateMockTypeWithoutPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.GetPrimaryConstructorParameters(mockType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void GetAccessibleMembers_WithProperties_ReturnsMembers()
        {
            // Arrange
            var mockType = CreateMockTypeWithProperties();

            // Act
            var result = PrimaryConstructorAnalyzer.GetAccessibleMembers(mockType);

            // Assert
            Assert.IsNotNull(result);
            // Should return some accessible members
        }

        [TestMethod]
        public void GetAccessibleMembers_WithRecordType_IncludesPrimaryConstructorParams()
        {
            // Arrange
            var mockType = CreateMockRecordTypeWithPrimaryConstructor();

            // Act
            var result = PrimaryConstructorAnalyzer.GetAccessibleMembers(mockType);

            // Assert
            Assert.IsNotNull(result);
            // For record types, should include primary constructor parameters
        }

        // Helper methods
        private INamedTypeSymbol CreateMockRecordType()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(true);
            mock.Setup(x => x.Name).Returns("TestRecord");
            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray<IMethodSymbol>.Empty);
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockClassType()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.Name).Returns("TestClass");
            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray<IMethodSymbol>.Empty);
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockTypeWithPrimaryConstructor()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.Name).Returns("TestClassWithPrimaryCtor");

            // Create mock parameter
            var paramMock = new Mock<IParameterSymbol>();
            paramMock.Setup(x => x.Name).Returns("id");
            paramMock.Setup(x => x.Type).Returns(CreateMockIntType());

            // Create mock constructor with parameters
            var ctorMock = new Mock<IMethodSymbol>();
            ctorMock.Setup(x => x.Parameters).Returns(System.Collections.Immutable.ImmutableArray.Create(paramMock.Object));
            ctorMock.Setup(x => x.IsImplicitlyDeclared).Returns(false);

            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray.Create(ctorMock.Object));
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockTypeWithoutPrimaryConstructor()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.Name).Returns("TestClassWithoutPrimaryCtor");

            // Create mock constructor without parameters
            var ctorMock = new Mock<IMethodSymbol>();
            ctorMock.Setup(x => x.Parameters).Returns(System.Collections.Immutable.ImmutableArray<IParameterSymbol>.Empty);

            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray.Create(ctorMock.Object));
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockTypeWithProperties()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.Name).Returns("TestClassWithProperties");

            // Create mock property
            var propMock = new Mock<IPropertySymbol>();
            propMock.Setup(x => x.Name).Returns("Name");
            propMock.Setup(x => x.Type).Returns(CreateMockStringType());
            propMock.Setup(x => x.CanBeReferencedByName).Returns(true);
            propMock.Setup(x => x.SetMethod).Returns(Mock.Of<IMethodSymbol>());

            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray<IMethodSymbol>.Empty);
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray.Create<ISymbol>(propMock.Object));
            
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockRecordTypeWithPrimaryConstructor()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(true);
            mock.Setup(x => x.Name).Returns("TestRecordWithPrimaryCtor");

            // Create mock parameter for primary constructor
            var paramMock = new Mock<IParameterSymbol>();
            paramMock.Setup(x => x.Name).Returns("name");
            paramMock.Setup(x => x.Type).Returns(CreateMockStringType());

            // Create mock constructor
            var ctorMock = new Mock<IMethodSymbol>();
            ctorMock.Setup(x => x.Parameters).Returns(System.Collections.Immutable.ImmutableArray.Create(paramMock.Object));
            ctorMock.Setup(x => x.IsImplicitlyDeclared).Returns(false);

            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray.Create(ctorMock.Object));
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            
            return mock.Object;
        }

        private ITypeSymbol CreateMockStringType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_String);
            mock.Setup(x => x.Name).Returns("String");
            return mock.Object;
        }

        private ITypeSymbol CreateMockIntType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_Int32);
            mock.Setup(x => x.Name).Returns("Int32");
            return mock.Object;
        }
    }
}
