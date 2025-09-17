// -----------------------------------------------------------------------
// <copyright file="DiagnosticGuidanceServiceTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 诊断指导服务相关组件的测试
    /// 注意：由于GeneratorExecutionContext是结构体且难以模拟，这里主要测试诊断逻辑组件
    /// </summary>
    [TestClass]
    public class DiagnosticGuidanceServiceTests : TestBase
    {
        #region 诊断ID常量测试

        [TestMethod]
        [TestCategory("Unit")]
        public void DiagnosticIds_AllConstants_HaveCorrectValues()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("SQLX1001", DiagnosticIds.PrimaryConstructorIssue);
            Assert.AreEqual("SQLX1002", DiagnosticIds.RecordTypeIssue);
            Assert.AreEqual("SQLX1003", DiagnosticIds.EntityInferenceIssue);
            Assert.AreEqual("SQLX2001", DiagnosticIds.PerformanceSuggestion);
            Assert.AreEqual("SQLX2002", DiagnosticIds.CodeQualityWarning);
            Assert.AreEqual("SQLX2003", DiagnosticIds.GenerationError);
            Assert.AreEqual("SQLX3001", DiagnosticIds.UsageGuidance);
            Assert.AreEqual("SQLX3002", DiagnosticIds.SqlQualityWarning);
            Assert.AreEqual("SQLX3003", DiagnosticIds.SecurityWarning);
            Assert.AreEqual("SQLX3004", DiagnosticIds.BestPracticeSuggestion);
            Assert.AreEqual("SQLX3005", DiagnosticIds.PerformanceOptimization);
            Assert.AreEqual("SQLX3006", DiagnosticIds.NamingConvention);
            Assert.AreEqual("SQLX3007", DiagnosticIds.MethodSignatureGuidance);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void DiagnosticIds_UniqueValues_NoCollisions()
        {
            // Arrange
            var allIds = new[]
            {
                DiagnosticIds.PrimaryConstructorIssue,
                DiagnosticIds.RecordTypeIssue,
                DiagnosticIds.EntityInferenceIssue,
                DiagnosticIds.PerformanceSuggestion,
                DiagnosticIds.CodeQualityWarning,
                DiagnosticIds.GenerationError,
                DiagnosticIds.UsageGuidance,
                DiagnosticIds.SqlQualityWarning,
                DiagnosticIds.SecurityWarning,
                DiagnosticIds.BestPracticeSuggestion,
                DiagnosticIds.PerformanceOptimization,
                DiagnosticIds.NamingConvention,
                DiagnosticIds.MethodSignatureGuidance
            };

            // Act
            var uniqueIds = allIds.Distinct().ToArray();

            // Assert
            Assert.AreEqual(allIds.Length, uniqueIds.Length, "All diagnostic IDs should be unique");
            
            foreach (var id in allIds)
            {
                Assert.IsTrue(id.StartsWith("SQLX"), $"Diagnostic ID {id} should start with 'SQLX'");
                Assert.IsTrue(id.Length >= 7, $"Diagnostic ID {id} should have proper format");
            }
        }

        #endregion

        #region 诊断分类测试

        [TestMethod]
        [TestCategory("Unit")]
        public void DiagnosticCategories_ByIdRange_CorrectlyGrouped()
        {
            // Arrange & Act & Assert

            // SQLX1xxx - 实体和类型相关
            Assert.IsTrue(DiagnosticIds.PrimaryConstructorIssue.StartsWith("SQLX1"));
            Assert.IsTrue(DiagnosticIds.RecordTypeIssue.StartsWith("SQLX1"));
            Assert.IsTrue(DiagnosticIds.EntityInferenceIssue.StartsWith("SQLX1"));

            // SQLX2xxx - 性能相关
            Assert.IsTrue(DiagnosticIds.PerformanceSuggestion.StartsWith("SQLX2"));
            Assert.IsTrue(DiagnosticIds.CodeQualityWarning.StartsWith("SQLX2"));
            Assert.IsTrue(DiagnosticIds.GenerationError.StartsWith("SQLX2"));

            // SQLX3xxx - 使用指导相关
            Assert.IsTrue(DiagnosticIds.UsageGuidance.StartsWith("SQLX3"));
            Assert.IsTrue(DiagnosticIds.SqlQualityWarning.StartsWith("SQLX3"));
            Assert.IsTrue(DiagnosticIds.SecurityWarning.StartsWith("SQLX3"));
            Assert.IsTrue(DiagnosticIds.BestPracticeSuggestion.StartsWith("SQLX3"));
            Assert.IsTrue(DiagnosticIds.PerformanceOptimization.StartsWith("SQLX3"));
            Assert.IsTrue(DiagnosticIds.NamingConvention.StartsWith("SQLX3"));
            Assert.IsTrue(DiagnosticIds.MethodSignatureGuidance.StartsWith("SQLX3"));
        }

        #endregion

        #region 诊断服务组件集成测试

        [TestMethod]
        [TestCategory("Integration")]
        public void DiagnosticComponents_Integration_WorkTogether()
        {
            // Arrange
            var sql = "SELECT * FROM [User]";
            var methodName = "GetUsers";

            // Act
            var sqlQualityDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(sqlQualityDiagnostics);
            Assert.IsTrue(sqlQualityDiagnostics.Count > 0);

            // 验证诊断的基本结构
            foreach (var diagnostic in sqlQualityDiagnostics)
            {
                Assert.IsNotNull(diagnostic);
                Assert.IsFalse(string.IsNullOrEmpty(diagnostic.Id));
                Assert.IsNotNull(diagnostic.Descriptor);
                Assert.IsTrue(diagnostic.Id.StartsWith("SQLX"));
            }

            WriteTestOutput($"Generated {sqlQualityDiagnostics.Count} diagnostics for integration test");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void DiagnosticComponents_AllAnalyzers_ProduceDiagnostics()
        {
            // Arrange
            var testScenarios = new[]
            {
                // SQL质量问题
                ("SELECT * FROM [User]", "GetUsers", "SqlQuality"),
                ("DELETE FROM [User]", "DeleteAll", "Security"),
                ("SELECT * FROM [User] ORDER BY [Name]", "GetOrderedUsers", "Performance"),
                
                // 使用模式问题（这里我们不能完全测试，因为需要IMethodSymbol）
                // 但我们可以测试SQL分析部分
            };

            // Act & Assert
            foreach (var (sql, methodName, category) in testScenarios)
            {
                var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);
                Assert.IsNotNull(diagnostics, $"Failed to analyze {category} scenario");
                
                WriteTestOutput($"{category}: {sql} -> {diagnostics.Count} diagnostics");
            }
        }

        #endregion

        #region 错误处理测试

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void DiagnosticComponents_InvalidInputs_HandleGracefully()
        {
            // Arrange & Act & Assert
            
            // 空SQL
            var emptyDiagnostics = DiagnosticHelper.AnalyzeSqlQuality("", "TestMethod");
            Assert.IsNotNull(emptyDiagnostics);
            Assert.AreEqual(0, emptyDiagnostics.Count);

            // null SQL
            var nullDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(null!, "TestMethod");
            Assert.IsNotNull(nullDiagnostics);
            Assert.AreEqual(0, nullDiagnostics.Count);

            // 极长SQL
            var longSql = new string('A', 5000) + " SELECT * FROM [User]";
            var longDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(longSql, "TestMethod");
            Assert.IsNotNull(longDiagnostics);

            WriteTestOutput("All error handling scenarios completed successfully");
        }

        #endregion


        #region 覆盖率验证测试

        [TestMethod]
        [TestCategory("Coverage")]
        public void DiagnosticComponents_AllSqlTypes_Covered()
        {
            // Arrange
            var sqlTypes = new Dictionary<string, string[]>
            {
                ["SELECT"] = new[] 
                {
                    "SELECT * FROM [User]",
                    "SELECT [Id], [Name] FROM [User]",
                    "SELECT COUNT(*) FROM [User]",
                    "SELECT * FROM [User] WHERE [IsActive] = 1"
                },
                ["INSERT"] = new[]
                {
                    "INSERT INTO [User] (Name) VALUES (@name)",
                    "INSERT INTO [User] (Name, Email) VALUES (@name, @email)"
                },
                ["UPDATE"] = new[]
                {
                    "UPDATE [User] SET [Name] = @name",
                    "UPDATE [User] SET [Name] = @name WHERE [Id] = @id"
                },
                ["DELETE"] = new[]
                {
                    "DELETE FROM [User]",
                    "DELETE FROM [User] WHERE [Id] = @id"
                }
            };

            // Act & Assert
            foreach (var sqlType in sqlTypes)
            {
                WriteTestOutput($"Testing {sqlType.Key} operations:");
                
                foreach (var sql in sqlType.Value)
                {
                    var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, "TestMethod");
                    Assert.IsNotNull(diagnostics, $"Failed to analyze: {sql}");
                    
                    WriteTestOutput($"  {sql} -> {diagnostics.Count} diagnostics");
                }
            }
        }

        #endregion
    }
}