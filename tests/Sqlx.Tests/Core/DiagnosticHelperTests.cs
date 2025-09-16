// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 诊断辅助类的全面测试
    /// </summary>
    [TestClass]
    public class DiagnosticHelperTests : TestBase
    {
        #region SQL质量分析测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_SelectStar_ReturnsWarning()
        {
            // Arrange
            var sql = "SELECT * FROM [User] WHERE [IsActive] = 1";
            var methodName = "GetActiveUsers";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.AreEqual(1, diagnostics.Count);
            Assert.AreEqual("SQLX3002", diagnostics[0].Id);
            Assert.IsTrue(diagnostics[0].GetMessage().Contains("避免使用 SELECT *"));
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_DeleteWithoutWhere_ReturnsSecurityWarning()
        {
            // Arrange
            var sql = "DELETE FROM [User]";
            var methodName = "DeleteAllUsers";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.AreEqual(1, diagnostics.Count);
            Assert.AreEqual("SQLX3003", diagnostics[0].Id);
            Assert.IsTrue(diagnostics[0].GetMessage().Contains("UPDATE/DELETE 语句缺少 WHERE 子句"));
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_UpdateWithoutWhere_ReturnsSecurityWarning()
        {
            // Arrange
            var sql = "UPDATE [User] SET [IsActive] = 0";
            var methodName = "DeactivateAllUsers";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.AreEqual(1, diagnostics.Count);
            Assert.AreEqual("SQLX3003", diagnostics[0].Id);
            Assert.IsTrue(diagnostics[0].GetMessage().Contains("UPDATE/DELETE 语句缺少 WHERE 子句"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_HardcodedString_ReturnsSqlInjectionWarning()
        {
            // Arrange
            var sql = "SELECT * FROM [User] WHERE [Name] = 'admin'";
            var methodName = "GetAdminUser";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            var injectionWarning = diagnostics.FirstOrDefault(d => d.Id == "SQLX3003");
            Assert.IsNotNull(injectionWarning);
            Assert.IsTrue(injectionWarning.GetMessage().Contains("SQL注入风险"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_OrderByWithoutLimit_ReturnsPerformanceWarning()
        {
            // Arrange
            var sql = "SELECT [Id], [Name] FROM [User] ORDER BY [Name]";
            var methodName = "GetOrderedUsers";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            var performanceWarning = diagnostics.FirstOrDefault(d => d.Id == "SQLX2001");
            Assert.IsNotNull(performanceWarning);
            Assert.IsTrue(performanceWarning.GetMessage().Contains("ORDER BY 查询建议添加 LIMIT/TOP"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_MultipleJoins_ReturnsPerformanceWarning()
        {
            // Arrange
            var sql = @"SELECT u.Name FROM [User] u 
                       INNER JOIN [Department] d ON u.DepartmentId = d.Id 
                       LEFT JOIN [Manager] m ON d.ManagerId = m.Id 
                       RIGHT JOIN [Location] l ON d.LocationId = l.Id";
            var methodName = "GetUsersWithDetails";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            var joinWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("JOIN 操作"));
            Assert.IsNotNull(joinWarning);
            Assert.IsTrue(joinWarning.GetMessage().Contains("3 个 JOIN"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_Subquery_ReturnsPerformanceWarning()
        {
            // Arrange
            var sql = "SELECT * FROM [User] WHERE [DepartmentId] IN (SELECT [Id] FROM [Department] WHERE [Budget] > 10000)";
            var methodName = "GetHighBudgetUsers";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            var subqueryWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("子查询"));
            Assert.IsNotNull(subqueryWarning);
            Assert.IsTrue(subqueryWarning.GetMessage().Contains("考虑使用 JOIN 或 CTE"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_EmptyOrNullSql_ReturnsEmptyList()
        {
            // Arrange & Act
            var emptyDiagnostics = DiagnosticHelper.AnalyzeSqlQuality("", "TestMethod");
            var nullDiagnostics = DiagnosticHelper.AnalyzeSqlQuality(null!, "TestMethod");

            // Assert
            Assert.IsNotNull(emptyDiagnostics);
            Assert.AreEqual(0, emptyDiagnostics.Count);
            Assert.IsNotNull(nullDiagnostics);
            Assert.AreEqual(0, nullDiagnostics.Count);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeSqlQuality_GoodSql_ReturnsNoWarnings()
        {
            // Arrange
            var sql = "SELECT [Id], [Name], [Email] FROM [User] WHERE [IsActive] = @isActive LIMIT 100";
            var methodName = "GetActiveUsersAsync";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.AreEqual(0, diagnostics.Count);
        }

        #endregion

        #region 诊断创建方法测试

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateUsageGuidanceDiagnostic_ValidParameters_ReturnsCorrectDiagnostic()
        {
            // Arrange
            var issue = "方法命名不符合约定";
            var suggestion = "建议使用GetXxxAsync命名";

            // Act
            var diagnostic = DiagnosticHelper.CreateUsageGuidanceDiagnostic(issue, suggestion);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX3001", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(issue));
            Assert.IsTrue(diagnostic.GetMessage().Contains(suggestion));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateSqlQualityDiagnostic_ValidParameters_ReturnsCorrectDiagnostic()
        {
            // Arrange
            var issue = "检测到SELECT *使用";
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostic = DiagnosticHelper.CreateSqlQualityDiagnostic(issue, sql);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX3002", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(issue));
            Assert.IsTrue(diagnostic.GetMessage().Contains(sql));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateSecurityWarningDiagnostic_ValidParameters_ReturnsCorrectDiagnostic()
        {
            // Arrange
            var issue = "检测到SQL注入风险";
            var context = "GetUserByName";

            // Act
            var diagnostic = DiagnosticHelper.CreateSecurityWarningDiagnostic(issue, context);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX3003", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(issue));
            Assert.IsTrue(diagnostic.GetMessage().Contains(context));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateBestPracticeDiagnostic_ValidParameters_ReturnsCorrectDiagnostic()
        {
            // Arrange
            var practice = "建议添加CancellationToken参数";
            var context = "GetUsersAsync";

            // Act
            var diagnostic = DiagnosticHelper.CreateBestPracticeDiagnostic(practice, context);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX3004", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(practice));
            Assert.IsTrue(diagnostic.GetMessage().Contains(context));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void CreatePerformanceSuggestion_ValidParameters_ReturnsCorrectDiagnostic()
        {
            // Arrange
            var suggestion = "考虑使用批量操作";
            var context = "CreateUsersAsync";

            // Act
            var diagnostic = DiagnosticHelper.CreatePerformanceSuggestion(suggestion, context);

            // Assert
            Assert.IsNotNull(diagnostic);
            Assert.AreEqual("SQLX2001", diagnostic.Id);
            Assert.AreEqual(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.IsTrue(diagnostic.GetMessage().Contains(suggestion));
            Assert.IsTrue(diagnostic.GetMessage().Contains(context));
        }

        #endregion

        #region 边界条件和错误处理测试

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void CreateDiagnostic_EmptyParameters_HandlesGracefully()
        {
            // Arrange & Act
            var diagnostic1 = DiagnosticHelper.CreateDiagnostic("", "", "", DiagnosticSeverity.Info);
            var diagnostic2 = DiagnosticHelper.CreateDiagnostic(null!, null!, null!, DiagnosticSeverity.Info);

            // Assert
            Assert.IsNotNull(diagnostic1);
            Assert.IsNotNull(diagnostic2);
            Assert.AreEqual("SQLX_UNKNOWN", diagnostic1.Id);
            Assert.AreEqual("SQLX_UNKNOWN", diagnostic2.Id);
        }

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void AnalyzeSqlQuality_VeryLongSql_HandlesCorrectly()
        {
            // Arrange
            var longSql = new string('A', 10000) + " SELECT * FROM [User]";
            var methodName = "TestMethod";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(longSql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            // 应该检测到SELECT *
            var selectStarWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("SELECT *"));
            Assert.IsNotNull(selectStarWarning);
        }

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void AnalyzeSqlQuality_CaseInsensitive_DetectsIssues()
        {
            // Arrange
            var sql = "select * from user where name = 'test'";
            var methodName = "TestMethod";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Count > 0);
            var selectStarWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("SELECT *"));
            Assert.IsNotNull(selectStarWarning);
        }

        #endregion

        #region 复合场景测试

        [TestMethod]
        [TestCategory("Integration")]
        public void AnalyzeSqlQuality_MultipleIssues_ReturnsAllDiagnostics()
        {
            // Arrange - SQL with multiple issues
            var sql = "SELECT * FROM [User] WHERE [Name] = 'admin' ORDER BY [Name]";
            var methodName = "GetAdminUsers";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Count >= 2);

            // Should detect SELECT *
            var selectStarWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("SELECT *"));
            Assert.IsNotNull(selectStarWarning);

            // Should detect SQL injection risk
            var injectionWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("SQL注入风险"));
            Assert.IsNotNull(injectionWarning);

            // Should detect ORDER BY without LIMIT
            var orderByWarning = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("ORDER BY"));
            Assert.IsNotNull(orderByWarning);
        }

        [TestMethod]
        [TestCategory("Performance")]
        public void AnalyzeSqlQuality_PerformanceTest_CompletesWithinThreshold()
        {
            // Arrange
            var sql = "SELECT * FROM [User] WHERE [IsActive] = @isActive";
            var methodName = "GetActiveUsers";

            // Act & Assert
            var executionTime = MeasureExecutionTime(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var diagnostics = DiagnosticHelper.AnalyzeSqlQuality(sql, methodName);
                    Assert.IsNotNull(diagnostics);
                }
            });

            Assert.IsTrue(executionTime < 1000, $"Performance test failed: {executionTime}ms > 1000ms");
            WriteTestOutput($"AnalyzeSqlQuality performance: {executionTime}ms for 1000 iterations");
        }

        #endregion
    }
}
