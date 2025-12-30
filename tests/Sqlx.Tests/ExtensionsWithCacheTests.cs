// -----------------------------------------------------------------------
// <copyright file="ExtensionsWithCacheTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for ExtensionsWithCache class to improve coverage.
    /// </summary>
    [TestClass]
    public class ExtensionsWithCacheTests
    {
        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithNullableString_ReturnsCorrectExpression()
        {
            // Arrange
            var stringType = CreateMockNullableStringType();
            var readerName = "reader";
            var columnName = "Name";
            var ordinalVar = "__ordinal_Name";

            // Act
            var result = stringType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
            StringAssert.Contains(result, "IsDBNull");
            StringAssert.Contains(result, "? null :");
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithNonNullableString_ReturnsCorrectExpression()
        {
            // Arrange
            var stringType = CreateMockNonNullableStringType();
            var readerName = "reader";
            var columnName = "Name";
            var ordinalVar = "__ordinal_Name";

            // Act
            var result = stringType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
            StringAssert.Contains(result, "IsDBNull");
            StringAssert.Contains(result, "string.Empty");
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithNullableInt_ReturnsCorrectExpression()
        {
            // Arrange
            var intType = CreateMockNullableIntType();
            var readerName = "reader";
            var columnName = "Id";
            var ordinalVar = "__ordinal_Id";

            // Act
            var result = intType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
            StringAssert.Contains(result, "IsDBNull");
            StringAssert.Contains(result, "? null :");
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithNonNullableInt_ReturnsCorrectExpression()
        {
            // Arrange
            var intType = CreateMockNonNullableIntType();
            var readerName = "reader";
            var columnName = "Id";
            var ordinalVar = "__ordinal_Id";

            // Act
            var result = intType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithBoolType_ReturnsCorrectExpression()
        {
            // Arrange
            var boolType = CreateMockBoolType();
            var readerName = "reader";
            var columnName = "IsActive";
            var ordinalVar = "__ordinal_IsActive";

            // Act
            var result = boolType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithDateTimeType_ReturnsCorrectExpression()
        {
            // Arrange
            var dateTimeType = CreateMockDateTimeType();
            var readerName = "reader";
            var columnName = "CreatedAt";
            var ordinalVar = "__ordinal_CreatedAt";

            // Act
            var result = dateTimeType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithGuidType_ReturnsCorrectExpression()
        {
            // Arrange
            var guidType = CreateMockGuidType();
            var readerName = "reader";
            var columnName = "Id";
            var ordinalVar = "__ordinal_Id";

            // Act
            var result = guidType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
        }

        [TestMethod]
        public void GetDataReadExpressionWithCachedOrdinal_WithDecimalType_ReturnsCorrectExpression()
        {
            // Arrange
            var decimalType = CreateMockDecimalType();
            var readerName = "reader";
            var columnName = "Price";
            var ordinalVar = "__ordinal_Price";

            // Act
            var result = decimalType.GetDataReadExpressionWithCachedOrdinal(readerName, columnName, ordinalVar);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, readerName);
            StringAssert.Contains(result, ordinalVar);
        }

        // Helper methods
        private static ITypeSymbol CreateMockNullableStringType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_String);
            mock.Setup(x => x.Name).Returns("String");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.Annotated);
            mock.Setup(x => x.IsValueType).Returns(false);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockNonNullableStringType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_String);
            mock.Setup(x => x.Name).Returns("String");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.NotAnnotated);
            mock.Setup(x => x.IsValueType).Returns(false);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockNullableIntType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_Int32);
            mock.Setup(x => x.Name).Returns("Int32");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.Annotated);
            mock.Setup(x => x.IsValueType).Returns(true);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockNonNullableIntType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_Int32);
            mock.Setup(x => x.Name).Returns("Int32");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.NotAnnotated);
            mock.Setup(x => x.IsValueType).Returns(true);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockBoolType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_Boolean);
            mock.Setup(x => x.Name).Returns("Boolean");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.NotAnnotated);
            mock.Setup(x => x.IsValueType).Returns(true);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockDateTimeType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_DateTime);
            mock.Setup(x => x.Name).Returns("DateTime");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.NotAnnotated);
            mock.Setup(x => x.IsValueType).Returns(true);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockGuidType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.None);
            mock.Setup(x => x.Name).Returns("Guid");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.NotAnnotated);
            mock.Setup(x => x.IsValueType).Returns(true);
            return mock.Object;
        }

        private static ITypeSymbol CreateMockDecimalType()
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(SpecialType.System_Decimal);
            mock.Setup(x => x.Name).Returns("Decimal");
            mock.Setup(x => x.NullableAnnotation).Returns(NullableAnnotation.NotAnnotated);
            mock.Setup(x => x.IsValueType).Returns(true);
            return mock.Object;
        }
    }
}
