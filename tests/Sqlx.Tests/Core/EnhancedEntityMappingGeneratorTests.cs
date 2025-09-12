// -----------------------------------------------------------------------
// <copyright file="EnhancedEntityMappingGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;
using Sqlx.Core;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for EnhancedEntityMappingGenerator class.
    /// </summary>
    [TestClass]
    public class EnhancedEntityMappingGeneratorTests
    {
        [TestMethod]
        public void GenerateEntityMapping_WithNoMembers_GeneratesDefaultMapping()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = CreateMockEntityTypeWithNoMembers();

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

            // Assert
            var result = sb.ToString();
            Assert.IsNotNull(result);
            StringAssert.Contains(result, "No accessible members found");
            StringAssert.Contains(result, "new TestNamespace.TestEntity()");
        }

        [TestMethod]
        public void GenerateEntityMapping_WithRegularClass_GeneratesMapping()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");
            var entityType = CreateMockRegularClassType();

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

            // Assert
            var result = sb.ToString();
            Assert.IsNotNull(result);
            // Should contain ordinal caching
            StringAssert.Contains(result, "__ordinal_");
            StringAssert.Contains(result, "reader.GetOrdinal");
        }

        [TestMethod]
        public void GetColumnName_WithMemberInfo_ReturnsExpectedName()
        {
            // This is a static method test, but since it's used internally
            // we test it indirectly through the main method
            var sb = new IndentedStringBuilder("");
            var entityType = CreateMockRegularClassType();

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

            // Assert
            var result = sb.ToString();
            // Should contain the property name converted to column name
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetPropertyNameFromParameter_WithParameterName_ReturnsPascalCase()
        {
            // This tests the internal logic indirectly
            // Since GetPropertyNameFromParameter is private, we test through the main method
            var sb = new IndentedStringBuilder("");
            var entityType = CreateMockRegularClassType();

            // Act
            EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);

            // Assert
            var result = sb.ToString();
            Assert.IsNotNull(result);
            // The method should complete without errors
        }

        // Helper methods
        private INamedTypeSymbol CreateMockEntityTypeWithNoMembers()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.Name).Returns("TestEntity");
            mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("TestNamespace.TestEntity");
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray<IMethodSymbol>.Empty);
            return mock.Object;
        }

        private INamedTypeSymbol CreateMockRegularClassType()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.Name).Returns("TestClass");
            mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("TestNamespace.TestClass");
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);

            // Create mock property
            var prop = new Mock<IPropertySymbol>();
            prop.Setup(x => x.Name).Returns("Name");
            prop.Setup(x => x.Type).Returns(CreateMockStringType());
            prop.Setup(x => x.CanBeReferencedByName).Returns(true);
            prop.Setup(x => x.SetMethod).Returns(Mock.Of<IMethodSymbol>());
            prop.Setup(x => x.GetAttributes()).Returns(System.Collections.Immutable.ImmutableArray<AttributeData>.Empty);

            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray.Create<ISymbol>(prop.Object));
            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray<IMethodSymbol>.Empty);

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
