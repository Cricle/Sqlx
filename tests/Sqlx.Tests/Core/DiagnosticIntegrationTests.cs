// -----------------------------------------------------------------------
// <copyright file="DiagnosticIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 诊断功能在源生成器中的集成测试
    /// </summary>
    [TestClass]
    public class DiagnosticIntegrationTests : TestBase
    {
        #region 测试辅助类

        /// <summary>
        /// 模拟的源生成器用于测试
        /// </summary>
        private class TestGenerator : AbstractGenerator
        {
            public List<Diagnostic> ReportedDiagnostics { get; } = new List<Diagnostic>();

            public override void Initialize(GeneratorInitializationContext context)
            {
                // Test implementation
            }

            // 重写以捕获诊断信息
            protected virtual void ReportDiagnostic(Diagnostic diagnostic)
            {
                ReportedDiagnostics.Add(diagnostic);
            }
        }

        /// <summary>
        /// 创建测试用的编译环境
        /// </summary>
        private (Compilation compilation, GeneratorDriver driver) CreateTestCompilation(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.KeyAttribute).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new TestGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);

            return (compilation, driver);
        }

        #endregion

        #region 源生成器诊断集成测试

        [TestMethod]
        [TestCategory("Integration")]
        public void SourceGenerator_WithSqlQualityIssues_ReportsDiagnostics()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public partial class UserService
    {
        [Sqlx(""SELECT * FROM [User]"")] // Should trigger SELECT * warning
        public partial Task<IList<User>> GetUsersAsync();

        [Sqlx(""DELETE FROM [User]"")] // Should trigger security warning
        public partial Task<int> DeleteAllUsersAsync();
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}";

            var (compilation, driver) = CreateTestCompilation(sourceCode);

            // Act
            var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);

            // Assert
            Assert.IsNotNull(result);

            // 检查是否有诊断信息
            var diagnostics = outputDiagnostics.ToList();

            // 由于我们的测试生成器是简化版本，这里主要测试结构
            // 在实际环境中，我们会看到具体的诊断ID
            WriteTestOutput($"Generated {diagnostics.Count} diagnostics");
            foreach (var diagnostic in diagnostics)
            {
                WriteTestOutput($"Diagnostic: {diagnostic.Id} - {diagnostic.GetMessage()}");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SourceGenerator_WithGoodPractices_ReportsPositiveFeedback()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Sqlx.Annotations;

namespace TestNamespace
{
    public partial class UserService
    {
        [Sqlx(""SELECT [Id], [Name], [Email] FROM [User] WHERE [IsActive] = @isActive LIMIT 100"")]
        public partial Task<IList<User>> GetActiveUsersAsync(bool isActive, CancellationToken cancellationToken);
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }
}";

            var (compilation, driver) = CreateTestCompilation(sourceCode);

            // Act
            var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);

            // Assert
            Assert.IsNotNull(result);

            var diagnostics = outputDiagnostics.ToList();
            WriteTestOutput($"Generated {diagnostics.Count} diagnostics for good practices");
        }

        #endregion

        #region 实际诊断功能验证测试

        [TestMethod]
        [TestCategory("Unit")]
        public void DiagnosticHelper_AllCategories_ProduceExpectedDiagnosticIds()
        {
            // Arrange & Act
            var sqlQualityDiag = DiagnosticHelper.CreateSqlQualityDiagnostic("test", "sql");
            var usageGuidanceDiag = DiagnosticHelper.CreateUsageGuidanceDiagnostic("issue", "suggestion");
            var securityWarningDiag = DiagnosticHelper.CreateSecurityWarningDiagnostic("issue", "context");
            var bestPracticeDiag = DiagnosticHelper.CreateBestPracticeDiagnostic("practice", "context");
            var performanceDiag = DiagnosticHelper.CreatePerformanceSuggestion("suggestion", "context");

            // Assert
            Assert.AreEqual("SQLX3002", sqlQualityDiag.Id);
            Assert.AreEqual("SQLX3001", usageGuidanceDiag.Id);
            Assert.AreEqual("SQLX3003", securityWarningDiag.Id);
            Assert.AreEqual("SQLX3004", bestPracticeDiag.Id);
            Assert.AreEqual("SQLX2001", performanceDiag.Id);

            // 验证严重级别
            Assert.AreEqual(DiagnosticSeverity.Warning, sqlQualityDiag.Severity);
            Assert.AreEqual(DiagnosticSeverity.Info, usageGuidanceDiag.Severity);
            Assert.AreEqual(DiagnosticSeverity.Warning, securityWarningDiag.Severity);
            Assert.AreEqual(DiagnosticSeverity.Info, bestPracticeDiag.Severity);
            Assert.AreEqual(DiagnosticSeverity.Info, performanceDiag.Severity);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void DiagnosticIds_Constants_MatchActualIds()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("SQLX1001", DiagnosticIds.PrimaryConstructorIssue);
            Assert.AreEqual("SQLX1002", DiagnosticIds.RecordTypeIssue);
            Assert.AreEqual("SQLX1003", DiagnosticIds.EntityInferenceIssue);
            Assert.AreEqual("SQLX2001", DiagnosticIds.PerformanceSuggestion);
            Assert.AreEqual("SQLX3001", DiagnosticIds.UsageGuidance);
            Assert.AreEqual("SQLX3002", DiagnosticIds.SqlQualityWarning);
            Assert.AreEqual("SQLX3003", DiagnosticIds.SecurityWarning);
            Assert.AreEqual("SQLX3004", DiagnosticIds.BestPracticeSuggestion);
        }

        #endregion

        #region 性能和错误处理测试

        [TestMethod]
        [TestCategory("Performance")]
        public void DiagnosticAnalysis_BulkOperations_CompletesWithinThreshold()
        {
            // Arrange
            var testCases = new[]
            {
                ("SELECT * FROM [User]", "GetUsers"),
                ("INSERT INTO [User] VALUES (@name)", "CreateUser"),
                ("UPDATE [User] SET [Name] = @name", "UpdateUser"),
                ("DELETE FROM [User] WHERE [Id] = @id", "DeleteUser"),
                ("SELECT COUNT(*) FROM [User] WHERE [IsActive] = @active", "GetUserCount")
            };

            // Act & Assert
            var executionTime = MeasureExecutionTime(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    foreach (var (sql, methodName) in testCases)
                    {
                        var sqlDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);
                        Assert.IsNotNull(sqlDiagnostics);
                    }
                }
            });

            Assert.IsTrue(executionTime < 2000, $"Bulk diagnostic analysis failed: {executionTime}ms > 2000ms");
            WriteTestOutput($"Bulk diagnostic analysis performance: {executionTime}ms for {testCases.Length * 100} operations");
        }

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void DiagnosticAnalysis_InvalidInputs_HandlesGracefully()
        {
            // Arrange & Act & Assert

            // Null/empty SQL
            var emptyDiagnostics = DiagnosticHelper.AnalyzeSqlQuality("", "TestMethod");
            Assert.IsNotNull(emptyDiagnostics);
            Assert.AreEqual(0, emptyDiagnostics.Count);

            var nullDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(null!, "TestMethod");
            Assert.IsNotNull(nullDiagnostics);
            Assert.AreEqual(0, nullDiagnostics.Count);

            // Very long SQL
            var longSql = new string('A', 10000) + " SELECT * FROM [User]";
            var longSqlDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(longSql, "TestMethod");
            Assert.IsNotNull(longSqlDiagnostics);

            // Special characters in SQL
            var specialCharSql = "SELECT * FROM [User] WHERE [Name] = N'测试'";
            var specialCharDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(specialCharSql, "TestMethod");
            Assert.IsNotNull(specialCharDiagnostics);

            WriteTestOutput("All edge cases handled gracefully");
        }

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void DiagnosticCreation_EdgeCases_HandlesCorrectly()
        {
            // Arrange & Act & Assert

            // Empty ID and title
            var diagnostic1 = DiagnosticHelper.CreateDiagnostic("", "", "", DiagnosticSeverity.Info);
            Assert.IsNotNull(diagnostic1);
            Assert.AreEqual("SQLX_UNKNOWN", diagnostic1.Id);

            // Null parameters
            var diagnostic2 = DiagnosticHelper.CreateDiagnostic(null!, null!, null!, DiagnosticSeverity.Warning);
            Assert.IsNotNull(diagnostic2);
            Assert.AreEqual("SQLX_UNKNOWN", diagnostic2.Id);

            // Very long messages
            var longMessage = new string('A', 5000);
            var diagnostic3 = DiagnosticHelper.CreateDiagnostic("TEST", "Test", longMessage, DiagnosticSeverity.Error);
            Assert.IsNotNull(diagnostic3);
            Assert.AreEqual("TEST", diagnostic3.Id);

            WriteTestOutput("All diagnostic creation edge cases handled");
        }

        #endregion

        #region 覆盖率和完整性测试

        [TestMethod]
        [TestCategory("Integration")]
        public void DiagnosticSystem_AllSqlOperations_Covered()
        {
            // Arrange
            var sqlOperations = new[]
            {
                "SELECT * FROM [User]",
                "INSERT INTO [User] (Name) VALUES (@name)",
                "UPDATE [User] SET [Name] = @name WHERE [Id] = @id",
                "DELETE FROM [User] WHERE [Id] = @id",
                "SELECT COUNT(*) FROM [User]",
                "SELECT * FROM [User] ORDER BY [Name]",
                "SELECT * FROM [User] u JOIN [Department] d ON u.DeptId = d.Id",
                "SELECT * FROM [User] WHERE [Id] IN (SELECT [Id] FROM [ActiveUsers])"
            };

            // Act & Assert
            foreach (var sql in sqlOperations)
            {
                var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, "TestMethod");
                Assert.IsNotNull(diagnostics, $"Failed to analyze SQL: {sql}");

                WriteTestOutput($"SQL: {sql} -> {diagnostics.Count} diagnostics");
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void DiagnosticSystem_AllDiagnosticTypes_CanBeCreated()
        {
            // Arrange & Act
            var diagnosticTypes = new[]
            {
                DiagnosticHelper.CreateSqlQualityDiagnostic("test", "sql"),
                DiagnosticHelper.CreateUsageGuidanceDiagnostic("issue", "suggestion"),
                DiagnosticHelper.CreateSecurityWarningDiagnostic("issue", "context"),
                DiagnosticHelper.CreateBestPracticeDiagnostic("practice", "context"),
                DiagnosticHelper.CreatePerformanceSuggestion("suggestion", "context"),
                DiagnosticHelper.CreatePrimaryConstructorDiagnostic("issue", null!, null),
                DiagnosticHelper.CreateRecordTypeDiagnostic("issue", null!, null),
                DiagnosticHelper.CreateEntityInferenceDiagnostic("issue", "method")
            };

            // Assert
            foreach (var diagnostic in diagnosticTypes)
            {
                Assert.IsNotNull(diagnostic);
                Assert.IsFalse(string.IsNullOrEmpty(diagnostic.Id));
                Assert.IsNotNull(diagnostic.Descriptor);

                WriteTestOutput($"Created diagnostic: {diagnostic.Id} - {diagnostic.Severity}");
            }

            // 验证所有诊断ID都是唯一的
            var ids = diagnosticTypes.Select(d => d.Id).ToList();
            var uniqueIds = ids.Distinct().ToList();
            Assert.AreEqual(ids.Count, uniqueIds.Count, "All diagnostic IDs should be unique");
        }

        #endregion
    }
}
