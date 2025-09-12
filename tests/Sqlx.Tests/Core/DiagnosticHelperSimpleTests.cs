// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Simple tests for DiagnosticHelper class to achieve basic coverage.
    /// </summary>
    [TestClass]
    public class DiagnosticHelperSimpleTests
    {
        [TestMethod]
        public void CreateDiagnostic_WithBasicParameters_CreatesValidDiagnostic()
        {
            // Arrange
            var id = "TEST001";
            var title = "Test Title";
            var messageFormat = "Test message: {0}";
            var severity = DiagnosticSeverity.Warning;
            var messageArgs = new object[] { "test arg" };

            // Act
            var diagnostic = DiagnosticHelper.CreateDiagnostic(id, title, messageFormat, severity, null, messageArgs);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual(id, diagnostic.Id);
            Assert.AreEqual(severity, diagnostic.Severity);
            Assert.AreEqual("Sqlx", diagnostic.Descriptor.Category);
            Assert.IsTrue(diagnostic.GetMessage().Contains("test arg"));
        }

        [TestMethod]
        public void CreateDiagnostic_WithoutMessageArgs_CreatesValidDiagnostic()
        {
            // Arrange
            var id = "TEST002";
            var title = "Test Title";
            var messageFormat = "Simple test message";
            var severity = DiagnosticSeverity.Error;

            // Act
            var diagnostic = DiagnosticHelper.CreateDiagnostic(id, title, messageFormat, severity);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual(id, diagnostic.Id);
            Assert.AreEqual(severity, diagnostic.Severity);
            Assert.AreEqual("Sqlx", diagnostic.Descriptor.Category);
            Assert.IsTrue(diagnostic.Descriptor.IsEnabledByDefault);
        }

        [TestMethod]
        public void CreateEntityInferenceDiagnostic_WithMethodName_CreatesExpectedDiagnostic()
        {
            // Arrange
            var issue = "Could not infer entity type";
            var methodName = "GetUsersAsync";

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
        public void CreatePerformanceSuggestion_WithContext_CreatesExpectedDiagnostic()
        {
            // Arrange
            var suggestion = "Consider using async methods";
            var context = "database operations";

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
        public void DiagnosticIds_ConstantsAreCorrect()
        {
            // Assert
            Assert.AreEqual("SQLX1001", DiagnosticIds.PrimaryConstructorIssue);
            Assert.AreEqual("SQLX1002", DiagnosticIds.RecordTypeIssue);
            Assert.AreEqual("SQLX1003", DiagnosticIds.EntityInferenceIssue);
            Assert.AreEqual("SQLX2001", DiagnosticIds.PerformanceSuggestion);
            Assert.AreEqual("SQLX2002", DiagnosticIds.CodeQualityWarning);
            Assert.AreEqual("SQLX3001", DiagnosticIds.GenerationError);
        }
    }
}
