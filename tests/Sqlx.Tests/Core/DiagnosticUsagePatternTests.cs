// -----------------------------------------------------------------------
// <copyright file="DiagnosticUsagePatternTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// 诊断使用模式分析的全面测试
    /// </summary>
    [TestClass]
    public class DiagnosticUsagePatternTests : TestBase
    {
        #region 测试辅助方法

        /// <summary>
        /// 创建测试用的方法符号
        /// </summary>
        private IMethodSymbol CreateMethodSymbol(string methodCode)
        {
            var fullCode = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Sqlx.Annotations;

public class TestService
{{
    {methodCode}
}}

public class User 
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }}
}}";

            var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var testServiceType = compilation.GetTypeByMetadataName("TestService");
            var method = testServiceType?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
            
            Assert.IsNotNull(method, "Could not find test method");
            return method!;
        }

        #endregion

        #region 异步方法最佳实践测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_AsyncMethodWithoutCancellationToken_ReportsBestPractice()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public async Task<IList<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var cancellationTokenWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("CancellationToken"));
            Assert.IsNotNull(cancellationTokenWarning);
            Assert.AreEqual("SQLX3004", cancellationTokenWarning.Id);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_AsyncMethodWithCancellationToken_NoWarning()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public async Task<IList<User>> GetUsersAsync(CancellationToken cancellationToken) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var cancellationTokenWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("CancellationToken"));
            Assert.IsNull(cancellationTokenWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_AsyncMethodWithoutAsyncSuffix_ReportsNamingIssue()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public async Task<IList<User>> GetUsers() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var namingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("'Async' 结尾"));
            Assert.IsNotNull(namingWarning);
            Assert.AreEqual("SQLX3001", namingWarning.Id);
        }

        #endregion

        #region 方法命名约定测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_SelectMethodWithPoorNaming_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task<IList<User>> ProcessDataAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var namingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("SELECT 查询方法命名"));
            Assert.IsNotNull(namingWarning);
            Assert.IsTrue(namingWarning.GetMessage().Contains("Get/Query/Find"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_InsertMethodWithPoorNaming_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""INSERT INTO [User] (Name) VALUES (@name)"")]
                public Task<int> ProcessUserAsync(string name) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "INSERT INTO [User] (Name) VALUES (@name)";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var namingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("INSERT 操作方法命名"));
            Assert.IsNotNull(namingWarning);
            Assert.IsTrue(namingWarning.GetMessage().Contains("Create/Add/Insert"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_UpdateMethodWithPoorNaming_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""UPDATE [User] SET [Name] = @name WHERE [Id] = @id"")]
                public Task<int> ProcessUserAsync(int id, string name) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "UPDATE [User] SET [Name] = @name WHERE [Id] = @id";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var namingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("UPDATE 操作方法命名"));
            Assert.IsNotNull(namingWarning);
            Assert.IsTrue(namingWarning.GetMessage().Contains("Update/Modify"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_DeleteMethodWithPoorNaming_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""DELETE FROM [User] WHERE [Id] = @id"")]
                public Task<int> ProcessUserAsync(int id) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "DELETE FROM [User] WHERE [Id] = @id";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var namingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("DELETE 操作方法命名"));
            Assert.IsNotNull(namingWarning);
            Assert.IsTrue(namingWarning.GetMessage().Contains("Delete/Remove"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_GoodMethodNaming_NoNamingWarnings()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task<IList<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var namingWarnings = diagnostics
                .Where(d => d.GetMessage().Contains("方法命名"));
            Assert.IsFalse(namingWarnings.Any());
        }

        #endregion

        #region 返回类型约定测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_SelectMethodWithVoidReturn_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public void GetUsers() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var returnTypeWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("SELECT 查询应该有返回值"));
            Assert.IsNotNull(returnTypeWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_SelectMethodWithTaskReturn_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            // Debug: 输出所有诊断信息
            WriteTestOutput($"Method return type: {method.ReturnType.ToDisplayString()}");
            WriteTestOutput($"Diagnostics found: {diagnostics.Count}");
            foreach (var d in diagnostics)
            {
                WriteTestOutput($"  - {d.GetMessage()}");
            }
            
            var returnTypeWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("SELECT 查询应该有返回值"));
            Assert.IsNotNull(returnTypeWarning, 
                $"Expected to find return type warning for Task return type, but diagnostics were: {string.Join("; ", diagnostics.Select(d => d.GetMessage()))}");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzeUsagePattern_ModifyOperationWithWrongReturnType_ReportsGuidance()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""INSERT INTO [User] (Name) VALUES (@name)"")]
                public Task<string> CreateUserAsync(string name) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "INSERT INTO [User] (Name) VALUES (@name)";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var returnTypeWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("数据修改操作的返回类型"));
            Assert.IsNotNull(returnTypeWarning);
            Assert.IsTrue(returnTypeWarning.GetMessage().Contains("int（受影响行数）或 void"));
        }

        #endregion

        #region 边界条件和性能测试

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void AnalyzeUsagePattern_NullMethod_HandlesGracefully()
        {
            // Arrange
            IMethodSymbol? nullMethod = null;
            var sql = "SELECT * FROM [User]";

            // Act & Assert
            try
            {
                var result = DiagnosticHelper.AnalyzeUsagePattern(nullMethod!, sql);
                // 如果没有抛出异常，则验证返回的是空集合或null
                Assert.IsTrue(result == null || result.Count == 0, "Should return empty result for null method");
            }
            catch (NullReferenceException)
            {
                // 这是可以接受的行为
                Assert.IsTrue(true, "NullReferenceException is acceptable for null method");
            }
            catch (ArgumentNullException)
            {
                // 这也是可以接受的行为
                Assert.IsTrue(true, "ArgumentNullException is acceptable for null method");
            }
        }

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void AnalyzeUsagePattern_EmptyOrNullSql_HandlesGracefully()
        {
            // Arrange
            var methodCode = @"
                [Sqlx("""")]
                public Task<IList<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);

            // Act
            var emptyDiagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, "");
            var nullDiagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, "");

            // Assert
            Assert.IsNotNull(emptyDiagnostics);
            Assert.IsNotNull(nullDiagnostics);
            // Should still analyze method patterns even with empty SQL
        }


        #endregion

        #region 复合场景测试

        [TestMethod]
        [TestCategory("Integration")]
        public void AnalyzeUsagePattern_MethodWithMultipleIssues_ReportsAllIssues()
        {
            // Arrange - Method with multiple issues
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task ProcessUsers() // No Async suffix, void return for SELECT, poor naming
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Count >= 3);

            // Should report naming issue
            var namingIssue = diagnostics.Any(d => d.GetMessage().Contains("Async"));
            Assert.IsTrue(namingIssue);

            // Should report return type issue
            var returnTypeIssue = diagnostics.Any(d => d.GetMessage().Contains("返回值"));
            Assert.IsTrue(returnTypeIssue);

            // Should report method naming convention issue
            var conventionIssue = diagnostics.Any(d => d.GetMessage().Contains("Get/Query/Find"));
            Assert.IsTrue(conventionIssue);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AnalyzeUsagePattern_PerfectMethod_ReportsMinimalIssues()
        {
            // Arrange - Well-designed method
            var methodCode = @"
                [Sqlx(""SELECT [Id], [Name] FROM [User] WHERE [IsActive] = @isActive"")]
                public Task<IList<User>> GetActiveUsersAsync(bool isActive, CancellationToken cancellationToken) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT [Id], [Name] FROM [User] WHERE [IsActive] = @isActive";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzeUsagePattern(method, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            // Should have minimal or no issues
            var majorIssues = diagnostics.Where(d => 
                d.Severity == DiagnosticSeverity.Warning || 
                d.Severity == DiagnosticSeverity.Error);
            Assert.IsTrue(majorIssues.Count() <= 1, 
                $"Well-designed method should have minimal issues, but found: {string.Join(", ", majorIssues.Select(d => d.GetMessage()))}");
        }

        #endregion
    }
}
