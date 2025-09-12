// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorParameterMemberInfoTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for PrimaryConstructorParameterMemberInfo class.
    /// </summary>
    [TestClass]
    public class PrimaryConstructorParameterMemberInfoTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange
            var parameter = CreateMockParameter("id", SpecialType.System_Int32);
            var propertyName = "Id";

            // Act
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Assert
            Assert.IsNotNull(memberInfo);
            Assert.AreEqual(propertyName, memberInfo.Name);
            Assert.AreEqual(parameter.Type, memberInfo.Type);
            Assert.AreEqual(parameter, memberInfo.Parameter);
        }

        [TestMethod]
        public void Name_ReturnsPropertyName()
        {
            // Arrange
            var parameter = CreateMockParameter("name", SpecialType.System_String);
            var propertyName = "Name";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Act
            var result = memberInfo.Name;

            // Assert
            Assert.AreEqual(propertyName, result);
        }

        [TestMethod]
        public void Type_ReturnsParameterType()
        {
            // Arrange
            var parameter = CreateMockParameter("email", SpecialType.System_String);
            var propertyName = "Email";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Act
            var result = memberInfo.Type;

            // Assert
            Assert.AreEqual(parameter.Type, result);
        }

        [TestMethod]
        public void CanWrite_ReturnsTrue()
        {
            // Arrange
            var parameter = CreateMockParameter("status", SpecialType.System_Boolean);
            var propertyName = "Status";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Act
            var result = memberInfo.CanWrite;

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsFromPrimaryConstructor_ReturnsTrue()
        {
            // Arrange
            var parameter = CreateMockParameter("count", SpecialType.System_Int32);
            var propertyName = "Count";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Act
            var result = memberInfo.IsFromPrimaryConstructor;

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GetAccessExpression_WithInstanceName_ReturnsCorrectExpression()
        {
            // Arrange
            var parameter = CreateMockParameter("value", SpecialType.System_Double);
            var propertyName = "Value";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);
            var instanceName = "entity";

            // Act
            var result = memberInfo.GetAccessExpression(instanceName);

            // Assert
            Assert.AreEqual($"{instanceName}.{propertyName}", result);
        }

        [TestMethod]
        public void GetAccessExpression_WithDifferentInstanceNames_ReturnsCorrectExpressions()
        {
            // Arrange
            var parameter = CreateMockParameter("data", SpecialType.System_Object);
            var propertyName = "Data";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Act & Assert
            Assert.AreEqual("obj.Data", memberInfo.GetAccessExpression("obj"));
            Assert.AreEqual("item.Data", memberInfo.GetAccessExpression("item"));
            Assert.AreEqual("record.Data", memberInfo.GetAccessExpression("record"));
        }

        [TestMethod]
        public void Parameter_ReturnsOriginalParameter()
        {
            // Arrange
            var parameter = CreateMockParameter("timestamp", SpecialType.System_DateTime);
            var propertyName = "Timestamp";
            var memberInfo = new PrimaryConstructorParameterMemberInfo(parameter, propertyName);

            // Act
            var result = memberInfo.Parameter;

            // Assert
            Assert.AreSame(parameter, result);
        }

        [TestMethod]
        public void DifferentPropertyNames_ProduceDifferentAccessExpressions()
        {
            // Arrange
            var parameter1 = CreateMockParameter("firstName", SpecialType.System_String);
            var parameter2 = CreateMockParameter("lastName", SpecialType.System_String);
            var memberInfo1 = new PrimaryConstructorParameterMemberInfo(parameter1, "FirstName");
            var memberInfo2 = new PrimaryConstructorParameterMemberInfo(parameter2, "LastName");
            var instanceName = "person";

            // Act
            var result1 = memberInfo1.GetAccessExpression(instanceName);
            var result2 = memberInfo2.GetAccessExpression(instanceName);

            // Assert
            Assert.AreEqual("person.FirstName", result1);
            Assert.AreEqual("person.LastName", result2);
            Assert.AreNotEqual(result1, result2);
        }

        // Helper methods
        private IParameterSymbol CreateMockParameter(string name, SpecialType specialType)
        {
            var mock = new Mock<IParameterSymbol>();
            mock.Setup(x => x.Name).Returns(name);
            mock.Setup(x => x.Type).Returns(CreateMockType(specialType));
            return mock.Object;
        }

        private ITypeSymbol CreateMockType(SpecialType specialType)
        {
            var mock = new Mock<ITypeSymbol>();
            mock.Setup(x => x.SpecialType).Returns(specialType);
            mock.Setup(x => x.Name).Returns(specialType.ToString().Replace("System_", ""));
            return mock.Object;
        }
    }
}
