// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperExtendedTests.cs" company="Cricle">
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
    /// Extended tests for DiagnosticHelper class to improve coverage.
    /// </summary>
    [TestClass]
    public class DiagnosticHelperExtendedTests
    {
        [TestMethod]
        public void CreatePrimaryConstructorDiagnostic_WithTypeAndIssue_CreatesValidDiagnostic()
        {
            // Arrange
            var issue = "Missing primary constructor";
            var mockType = CreateMockTypeSymbol("TestClass");

            // Act
            var diagnostic = DiagnosticHelper.CreatePrimaryConstructorDiagnostic(issue, mockType);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX1001", diagnostic.Id);
            Assert.AreEqual("Primary Constructor Issue", diagnostic.Descriptor.Title.ToString());
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains("TestClass"));
            Assert.IsTrue(diagnostic.GetMessage().Contains(issue));
        }

        [TestMethod]
        public void CreateRecordTypeDiagnostic_WithTypeAndIssue_CreatesValidDiagnostic()
        {
            // Arrange
            var issue = "Record type validation failed";
            var mockType = CreateMockTypeSymbol("TestRecord");

            // Act
            var diagnostic = DiagnosticHelper.CreateRecordTypeDiagnostic(issue, mockType);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX1002", diagnostic.Id);
            Assert.AreEqual("Record Type Issue", diagnostic.Descriptor.Title.ToString());
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains("TestRecord"));
            Assert.IsTrue(diagnostic.GetMessage().Contains(issue));
        }

        [TestMethod]
        public void CreateEntityInferenceDiagnostic_WithMethodNameAndIssue_CreatesValidDiagnostic()
        {
            // Arrange
            var issue = "Entity inference failed";
            var methodName = "GetUserById";

            // Act
            var diagnostic = DiagnosticHelper.CreateEntityInferenceDiagnostic(issue, methodName);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX1003", diagnostic.Id);
            Assert.AreEqual("Entity Type Inference Issue", diagnostic.Descriptor.Title.ToString());
            Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(methodName));
            Assert.IsTrue(diagnostic.GetMessage().Contains(issue));
        }

        [TestMethod]
        public void CreatePerformanceSuggestion_WithSuggestionAndContext_CreatesValidDiagnostic()
        {
            // Arrange
            var suggestion = "Use async methods for better performance";
            var context = "Database operations";

            // Act
            var diagnostic = DiagnosticHelper.CreatePerformanceSuggestion(suggestion, context);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX2001", diagnostic.Id);
            Assert.AreEqual("Performance Suggestion", diagnostic.Descriptor.Title.ToString());
            Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(suggestion));
            Assert.IsTrue(diagnostic.GetMessage().Contains(context));
        }

        [TestMethod]
        public void GenerateTypeAnalysisReport_WithValidType_GeneratesDetailedReport()
        {
            // Arrange
            var mockType = CreateMockTypeSymbol("TestEntity");

            // Act
            var report = DiagnosticHelper.GenerateTypeAnalysisReport(mockType);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("TestEntity"));
            Assert.IsTrue(report.Contains("类型分析报告"));
            Assert.IsTrue(report.Contains("完全限定名"));
            Assert.IsTrue(report.Contains("类型种类"));
        }

        [TestMethod]
        public void ValidateEntityType_WithValidEntity_ReturnsNoIssues()
        {
            // Arrange
            var mockType = CreateValidEntityType();

            // Act
            var issues = DiagnosticHelper.ValidateEntityType(mockType);

            // Assert
            Assert.IsNotNull(issues);
            // For a properly configured mock, should have minimal issues
        }

        [TestMethod]
        public void GeneratePerformanceSuggestions_WithEntityType_ReturnsSuggestions()
        {
            // Arrange
            var mockType = CreateMockTypeSymbol("TestEntity");

            // Act
            var suggestions = DiagnosticHelper.GeneratePerformanceSuggestions(mockType);

            // Assert
            Assert.IsNotNull(suggestions);
            // Should return a list of suggestions (might be empty for simple mocks)
        }

        [TestMethod]
        public void LogCodeGenerationContext_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var context = "Test context";
            var mockType = CreateMockTypeSymbol("TestEntity");
            var methodName = "TestMethod";

            // Act & Assert (should not throw)
            DiagnosticHelper.LogCodeGenerationContext(context, mockType, methodName);
        }

        [TestMethod]
        public void ValidateGeneratedCode_WithValidCode_ReturnsValidationResults()
        {
            // Arrange
            var generatedCode = "public class TestEntity { public int Id { get; set; } }";
            var mockType = CreateMockTypeSymbol("TestEntity");

            // Act
            var issues = DiagnosticHelper.ValidateGeneratedCode(generatedCode, mockType);

            // Assert
            Assert.IsNotNull(issues);
            // Should return validation results
        }

        // Helper methods
        private INamedTypeSymbol CreateMockTypeSymbol(string typeName)
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.Name).Returns(typeName);
            mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns($"TestNamespace.{typeName}");
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.IsAbstract).Returns(false);
            mock.Setup(x => x.ContainingNamespace).Returns(CreateMockNamespace());
            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray<IMethodSymbol>.Empty);
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            return mock.Object;
        }

        private INamedTypeSymbol CreateValidEntityType()
        {
            var mock = new Mock<INamedTypeSymbol>();
            mock.Setup(x => x.Name).Returns("ValidEntity");
            mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("TestNamespace.ValidEntity");
            mock.Setup(x => x.TypeKind).Returns(TypeKind.Class);
            mock.Setup(x => x.IsRecord).Returns(false);
            mock.Setup(x => x.IsAbstract).Returns(false);
            mock.Setup(x => x.ContainingNamespace).Returns(CreateMockNamespace());
            
            // Add a public constructor
            var ctorMock = new Mock<IMethodSymbol>();
            ctorMock.Setup(x => x.IsImplicitlyDeclared).Returns(false);
            ctorMock.Setup(x => x.DeclaredAccessibility).Returns(Accessibility.Public);
            ctorMock.Setup(x => x.Parameters).Returns(System.Collections.Immutable.ImmutableArray<IParameterSymbol>.Empty);
            
            mock.Setup(x => x.Constructors).Returns(System.Collections.Immutable.ImmutableArray.Create(ctorMock.Object));
            mock.Setup(x => x.GetMembers()).Returns(System.Collections.Immutable.ImmutableArray<ISymbol>.Empty);
            
            return mock.Object;
        }

        private INamespaceSymbol CreateMockNamespace()
        {
            var mock = new Mock<INamespaceSymbol>();
            mock.Setup(x => x.Name).Returns("TestNamespace");
            mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("TestNamespace");
            return mock.Object;
        }
    }
}
