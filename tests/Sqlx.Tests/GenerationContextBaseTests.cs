// -----------------------------------------------------------------------
// <copyright file="GenerationContextBaseTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;
using System.Collections.Immutable;
using System.Linq;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for GenerationContextBase class to improve coverage.
    /// </summary>
    [TestClass]
    public class GenerationContextBaseTests
    {
        [TestMethod]
        public void GetSymbol_WithNullSymbol_ReturnsNull()
        {
            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(null, x => true);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetSymbol_WithMethodSymbol_ReturnsMatchingParameter()
        {
            // Arrange
            var methodSymbol = CreateMockMethodSymbol();

            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(methodSymbol, x => x.Name == "param1");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("param1", result.Name);
        }

        [TestMethod]
        public void GetSymbol_WithMethodSymbol_NoMatch_ReturnsNull()
        {
            // Arrange
            var methodSymbol = CreateMockMethodSymbol();

            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(methodSymbol, x => x.Name == "nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetSymbol_WithNamedTypeSymbol_ReturnsMatchingField()
        {
            // Arrange
            var namedTypeSymbol = CreateMockNamedTypeSymbolWithField();

            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(namedTypeSymbol, x => x.Name == "testField");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("testField", result.Name);
        }

        [TestMethod]
        public void GetSymbol_WithNamedTypeSymbol_ReturnsMatchingProperty()
        {
            // Arrange
            var namedTypeSymbol = CreateMockNamedTypeSymbolWithProperty();

            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(namedTypeSymbol, x => x.Name == "testProperty");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("testProperty", result.Name);
        }

        [TestMethod]
        public void GetSymbol_WithNamedTypeSymbol_ChecksBaseType()
        {
            // Arrange
            var baseType = CreateMockNamedTypeSymbolWithField();
            var namedTypeSymbol = CreateMockNamedTypeSymbolWithBaseType(baseType);

            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(namedTypeSymbol, x => x.Name == "testField");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("testField", result.Name);
        }

        [TestMethod]
        public void GetSymbol_WithNamedTypeSymbol_NoMatch_ReturnsNull()
        {
            // Arrange
            var namedTypeSymbol = CreateMockNamedTypeSymbolEmpty();

            // Act
            var result = TestableGenerationContextBase.TestGetSymbol(namedTypeSymbol, x => x.Name == "nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        // Helper methods
        private static IMethodSymbol CreateMockMethodSymbol()
        {
            var param1 = new Mock<IParameterSymbol>();
            param1.Setup(x => x.Name).Returns("param1");
            param1.Setup(x => x.Type).Returns(Mock.Of<ITypeSymbol>());

            var param2 = new Mock<IParameterSymbol>();
            param2.Setup(x => x.Name).Returns("param2");
            param2.Setup(x => x.Type).Returns(Mock.Of<ITypeSymbol>());

            var mock = new Mock<IMethodSymbol>();
            mock.Setup(x => x.Parameters).Returns(ImmutableArray.Create(param1.Object, param2.Object));

            return mock.Object;
        }

        private static INamedTypeSymbol CreateMockNamedTypeSymbolWithField()
        {
            var field = new Mock<IFieldSymbol>();
            field.Setup(x => x.Name).Returns("testField");
            field.Setup(x => x.Type).Returns(Mock.Of<ITypeSymbol>());

            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(field.Object));
            mock.Setup(x => x.BaseType).Returns((INamedTypeSymbol?)null);

            return mock.Object;
        }

        private static INamedTypeSymbol CreateMockNamedTypeSymbolWithProperty()
        {
            var property = new Mock<IPropertySymbol>();
            property.Setup(x => x.Name).Returns("testProperty");
            property.Setup(x => x.Type).Returns(Mock.Of<ITypeSymbol>());

            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.GetMembers()).Returns(ImmutableArray.Create<ISymbol>(property.Object));
            mock.Setup(x => x.BaseType).Returns((INamedTypeSymbol?)null);

            return mock.Object;
        }

        private static INamedTypeSymbol CreateMockNamedTypeSymbolWithBaseType(INamedTypeSymbol baseType)
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.GetMembers()).Returns(ImmutableArray<ISymbol>.Empty);
            mock.Setup(x => x.BaseType).Returns(baseType);

            return mock.Object;
        }

        private static INamedTypeSymbol CreateMockNamedTypeSymbolEmpty()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.GetMembers()).Returns(ImmutableArray<ISymbol>.Empty);
            mock.Setup(x => x.BaseType).Returns((INamedTypeSymbol?)null);

            return mock.Object;
        }

        // Test helper class to access protected static method
        private class TestableGenerationContextBase : GenerationContextBase
        {
            internal override ISymbol? DbConnection => null;
            internal override ISymbol? TransactionParameter => null;
            internal override ISymbol? DbContext => null;

            public static ISymbol? TestGetSymbol(ISymbol? symbol, System.Func<ISymbol, bool> check)
            {
                return GetSymbol(symbol, check);
            }
        }
    }
}
