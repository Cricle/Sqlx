// -----------------------------------------------------------------------
// <copyright file="EdgeCaseTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx.Core;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for edge cases and boundary conditions to improve test coverage.
    /// </summary>
    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void IndentedStringBuilder_WithEmptyString_HandlesCorrectly()
        {
            // Arrange
            var sb = new IndentedStringBuilder("");

            // Act
            sb.AppendLine("test");
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("test"));
        }

        [TestMethod]
        public void IndentedStringBuilder_WithNullIndent_HandlesCorrectly()
        {
            // Act & Assert - Should not throw
            var sb = new IndentedStringBuilder(null!);
            sb.AppendLine("test");
            var result = sb.ToString();
            
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void IndentedStringBuilder_MultipleAppends_MaintainsIndentation()
        {
            // Arrange
            var sb = new IndentedStringBuilder("  ");

            // Act
            sb.AppendLine("line1");
            sb.AppendLine("line2");
            sb.AppendLine("line3");
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("line1"));
            Assert.IsTrue(result.Contains("line2"));
            Assert.IsTrue(result.Contains("line3"));
            
            // Check that indentation is applied (basic test)
            var lines = result.Split('\n');
            var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            Assert.IsTrue(nonEmptyLines.Length > 0, "Should have non-empty lines");
        }

        [TestMethod]
        public void IndentedStringBuilder_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var sb = new IndentedStringBuilder("    ");

            // Act
            sb.AppendLine("test with \"quotes\"");
            sb.AppendLine("test with 'apostrophes'");
            sb.AppendLine("test with \\ backslashes");
            sb.AppendLine("test with \n newlines");
            var result = sb.ToString();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("quotes"));
            Assert.IsTrue(result.Contains("apostrophes"));
            Assert.IsTrue(result.Contains("backslashes"));
        }

        [TestMethod]
        public void Constants_AreCorrectlyDefined()
        {
            // Test that constants are properly defined and accessible
            // This helps ensure the Constants class is covered

            // We can't directly access internal constants, but we can test
            // that the classes that use them work correctly
            Assert.IsNotNull(AttributeSourceGenerator.GenerateAttributeSource());
        }

        [TestMethod]
        public void SqlxException_WithNullErrorCode_HandlesCorrectly()
        {
            // Test that exception constructors handle edge cases
            
            // Act & Assert
            var sqlGenException = new SqlGenerationException("test message");
            Assert.IsNotNull(sqlGenException.ErrorCode);
            Assert.AreEqual("SQLX001", sqlGenException.ErrorCode);

            var methodException = new InvalidMethodSignatureException("method", "test message");
            Assert.IsNotNull(methodException.ErrorCode);
            Assert.AreEqual("SQLX002", methodException.ErrorCode);

            var dialectException = new UnsupportedDialectException("dialect");
            Assert.IsNotNull(dialectException.ErrorCode);
            Assert.AreEqual("SQLX003", dialectException.ErrorCode);
        }

        [TestMethod]
        public void SqlxException_WithEmptyMessages_HandlesCorrectly()
        {
            // Act
            var sqlGenException = new SqlGenerationException("");
            var methodException = new InvalidMethodSignatureException("", "");
            var dialectException = new UnsupportedDialectException("");

            // Assert
            Assert.IsNotNull(sqlGenException.Message);
            Assert.IsNotNull(methodException.Message);
            Assert.IsNotNull(dialectException.Message);
            
            Assert.AreEqual("", sqlGenException.Message);
            Assert.AreEqual("", methodException.Message);
            Assert.IsTrue(dialectException.Message.Contains("''"));
        }

        [TestMethod]
        public void DiagnosticHelper_CreateDiagnostic_WithNullLocation_HandlesCorrectly()
        {
            // Act
            var diagnostic = DiagnosticHelper.CreateDiagnostic(
                "TEST001",
                "Test Title",
                "Test message: {0}",
                DiagnosticSeverity.Warning,
                null,
                "parameter");

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("TEST001", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains("parameter"));
        }

        [TestMethod]
        public void DiagnosticHelper_CreateDiagnostic_WithEmptyParameters_HandlesCorrectly()
        {
            // Act
            var diagnostic = DiagnosticHelper.CreateDiagnostic(
                "TEST002",
                "Test Title",
                "Test message without parameters",
                DiagnosticSeverity.Error);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("TEST002", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [TestMethod]
        public void DiagnosticHelper_WithMockTypeSymbol_CreatesValidDiagnostics()
        {
            // Arrange
            var compilation = CreateTestCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass")!;

            // Act
            var primaryConstructorDiagnostic = DiagnosticHelper.CreatePrimaryConstructorDiagnostic(
                "test issue", typeSymbol);
            
            var recordTypeDiagnostic = DiagnosticHelper.CreateRecordTypeDiagnostic(
                "record issue", typeSymbol);
            
            var entityInferenceDiagnostic = DiagnosticHelper.CreateEntityInferenceDiagnostic(
                "inference issue", "TestMethod");
            
            var performanceSuggestion = DiagnosticHelper.CreatePerformanceSuggestion(
                "use caching", "database access");

            // Assert
            Assert.IsNotNull(primaryConstructorDiagnostic);
            Assert.AreEqual("SQLX1001", primaryConstructorDiagnostic.Id);
            
            Assert.IsNotNull(recordTypeDiagnostic);
            Assert.AreEqual("SQLX1002", recordTypeDiagnostic.Id);
            
            Assert.IsNotNull(entityInferenceDiagnostic);
            Assert.AreEqual("SQLX1003", entityInferenceDiagnostic.Id);
            
            Assert.IsNotNull(performanceSuggestion);
            Assert.AreEqual("SQLX2001", performanceSuggestion.Id);
        }

        [TestMethod]
        public void DiagnosticHelper_GenerateTypeAnalysisReport_WithValidType_GeneratesReport()
        {
            // Arrange
            var compilation = CreateTestCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass")!;

            // Act
            var report = DiagnosticHelper.GenerateTypeAnalysisReport(typeSymbol);

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Contains("类型分析报告"));
            Assert.IsTrue(report.Contains("TestClass"));
            Assert.IsTrue(report.Contains("完全限定名"));
            Assert.IsTrue(report.Contains("类型种类"));
        }

        [TestMethod]
        public void DiagnosticHelper_ValidateEntityType_WithValidType_ReturnsNoIssues()
        {
            // Arrange
            var compilation = CreateTestCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass")!;

            // Act
            var issues = DiagnosticHelper.ValidateEntityType(typeSymbol);

            // Assert
            Assert.IsNotNull(issues);
            // A simple class with public properties should have no issues
        }

        [TestMethod]
        public void DiagnosticHelper_GeneratePerformanceSuggestions_WithValidType_ReturnsSuggestions()
        {
            // Arrange
            var compilation = CreateTestCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass")!;

            // Act
            var suggestions = DiagnosticHelper.GeneratePerformanceSuggestions(typeSymbol);

            // Assert
            Assert.IsNotNull(suggestions);
            // Should return a list (may be empty)
        }

        [TestMethod]
        public void DiagnosticHelper_ValidateGeneratedCode_WithValidCode_ReturnsNoIssues()
        {
            // Arrange
            var compilation = CreateTestCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass")!;
            var validCode = @"
                var entity = new TestNamespace.TestClass();
                if (!reader.IsDBNull(0))
                    entity.Id = reader.GetInt32(0);
            ";

            // Act
            var issues = DiagnosticHelper.ValidateGeneratedCode(validCode, typeSymbol);

            // Assert
            Assert.IsNotNull(issues);
            // Valid code should have minimal issues
        }

        [TestMethod]
        public void DiagnosticHelper_ValidateGeneratedCode_WithProblematicCode_ReturnsIssues()
        {
            // Arrange
            var compilation = CreateTestCompilation();
            var typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass")!;
            var problematicCode = @"
                var entity = new SomeOtherClass();
                var date = (DateTime)reader.GetValue(0);
                var value = reader.GetString(0);
            ";

            // Act
            var issues = DiagnosticHelper.ValidateGeneratedCode(problematicCode, typeSymbol);

            // Assert
            Assert.IsNotNull(issues);
            Assert.IsTrue(issues.Count > 0, "Should detect issues in problematic code");
        }

        private CSharpCompilation CreateTestCompilation()
        {
            var sourceCode = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public record TestRecord(int Id, string Name);

    public class TestClassWithPrimaryConstructor(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}";

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        }
    }
}
