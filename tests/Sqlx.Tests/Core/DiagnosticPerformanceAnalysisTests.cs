// -----------------------------------------------------------------------
// <copyright file="DiagnosticPerformanceAnalysisTests.cs" company="Cricle">
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
    /// 诊断性能分析的全面测试
    /// </summary>
    [TestClass]
    public class DiagnosticPerformanceAnalysisTests : TestBase
    {
        #region 测试辅助方法

        /// <summary>
        /// 创建测试用的方法符号
        /// </summary>
        private IMethodSymbol CreateMethodSymbol(string methodCode, string additionalTypes = "")
        {
            var fullCode = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Sqlx.Annotations;

{additionalTypes}

public class TestService
{{
    {methodCode}
}}

public class User 
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }}
    public string Email {{ get; set; }}
}}";

            var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
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

        /// <summary>
        /// 创建测试用的实体类型符号
        /// </summary>
        private INamedTypeSymbol CreateEntitySymbol(string entityCode)
        {
            var fullCode = $@"
using System;

{entityCode}";

            var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var entityType = compilation.GetTypeByMetadataName("TestEntity");

            Assert.IsNotNull(entityType, "Could not find entity type");
            return entityType!;
        }

        #endregion

        #region 批量操作检测测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_CollectionParameterWithInsert_SuggestsBatchOperation()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""INSERT INTO [User] (Name, Email) VALUES (@name, @email)"")]
                public Task<int> CreateUsersAsync(IList<User> users) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "INSERT INTO [User] (Name, Email) VALUES (@name, @email)";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var batchWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("批量操作"));
            Assert.IsNotNull(batchWarning);
            Assert.AreEqual("SQLX2001", batchWarning.Id);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_CollectionParameterWithUpdate_SuggestsBatchOperation()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""UPDATE [User] SET [Name] = @name WHERE [Id] = @id"")]
                public Task<int> UpdateUsersAsync(IList<User> users) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "UPDATE [User] SET [Name] = @name WHERE [Id] = @id";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var batchWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("批量操作"));
            Assert.IsNotNull(batchWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_CollectionParameterWithDelete_SuggestsBatchOperation()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""DELETE FROM [User] WHERE [Id] = @id"")]
                public Task<int> DeleteUsersAsync(IList<int> userIds) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "DELETE FROM [User] WHERE [Id] = @id";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var batchWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("批量操作"));
            Assert.IsNotNull(batchWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_CollectionParameterWithBatchKeyword_NoWarning()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""BATCH INSERT INTO [User] (Name, Email) VALUES (@name, @email)"")]
                public Task<int> BatchCreateUsersAsync(IList<User> users) 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "BATCH INSERT INTO [User] (Name, Email) VALUES (@name, @email)";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var batchWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("批量操作"));
            Assert.IsNull(batchWarning);
        }

        #endregion

        #region 分页查询检测测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SelectReturnsListWithoutPaging_SuggestsPaging()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task<List<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var pagingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("分页支持"));
            Assert.IsNotNull(pagingWarning);
            Assert.IsTrue(pagingWarning.GetMessage().Contains("LIMIT/OFFSET 或 TOP"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SelectReturnsIEnumerableWithoutPaging_SuggestsPaging()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task<IEnumerable<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var pagingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("分页支持"));
            Assert.IsNotNull(pagingWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SelectWithLimit_NoPagingWarning()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User] LIMIT 100"")]
                public Task<List<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User] LIMIT 100";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var pagingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("分页支持"));
            Assert.IsNull(pagingWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SelectWithTop_NoPagingWarning()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT TOP 100 * FROM [User]"")]
                public Task<List<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT TOP 100 * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var pagingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("分页支持"));
            Assert.IsNull(pagingWarning);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SelectWithOffset_NoPagingWarning()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User] OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY"")]
                public Task<List<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User] OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var pagingWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("分页支持"));
            Assert.IsNull(pagingWarning);
        }

        #endregion

        #region 实体大小分析测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_LargeEntity_SuggestsProjection()
        {
            // Arrange
            var entityCode = @"
public class TestEntity
{
    public int Id { get; set; }
    public string Property1 { get; set; }
    public string Property2 { get; set; }
    public string Property3 { get; set; }
    public string Property4 { get; set; }
    public string Property5 { get; set; }
    public string Property6 { get; set; }
    public string Property7 { get; set; }
    public string Property8 { get; set; }
    public string Property9 { get; set; }
    public string Property10 { get; set; }
    public string Property11 { get; set; }
    public string Property12 { get; set; }
    public string Property13 { get; set; }
    public string Property14 { get; set; }
    public string Property15 { get; set; }
    public string Property16 { get; set; } // 16+ properties
}";
            var entity = CreateEntitySymbol(entityCode);

            var methodCode = @"
                [Sqlx(""SELECT * FROM [TestEntity]"")]
                public Task<List<TestEntity>> GetEntitiesAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode, entityCode);
            var sql = "SELECT * FROM [TestEntity]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, entity, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var projectionWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("个属性"));
            Assert.IsNotNull(projectionWarning);
            Assert.IsTrue(projectionWarning.GetMessage().Contains("投影查询"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_ManyStringProperties_SuggestsOptimization()
        {
            // Arrange
            var entityCode = @"
public class TestEntity
{
    public int Id { get; set; }
    public string String1 { get; set; }
    public string String2 { get; set; }
    public string String3 { get; set; }
    public string String4 { get; set; }
    public string String5 { get; set; }
    public string String6 { get; set; } // 6+ string properties
}";
            var entity = CreateEntitySymbol(entityCode);

            var methodCode = @"
                [Sqlx(""SELECT * FROM [TestEntity]"")]
                public Task<List<TestEntity>> GetEntitiesAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode, entityCode);
            var sql = "SELECT * FROM [TestEntity]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, entity, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var stringOptimizationWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("字符串属性"));
            Assert.IsNotNull(stringOptimizationWarning);
            Assert.IsTrue(stringOptimizationWarning.GetMessage().Contains("字符串池优化"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SmallEntity_NoWarnings()
        {
            // Arrange
            var entityCode = @"
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}";
            var entity = CreateEntitySymbol(entityCode);

            var methodCode = @"
                [Sqlx(""SELECT * FROM [TestEntity]"")]
                public Task<TestEntity> GetEntityAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode, entityCode);
            var sql = "SELECT * FROM [TestEntity]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, entity, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var entityWarnings = diagnostics
                .Where(d => d.GetMessage().Contains("属性") || d.GetMessage().Contains("字符串"));
            Assert.IsFalse(entityWarnings.Any());
        }

        #endregion

        #region 同步方法检测测试

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_SyncSelectMethod_SuggestsAsync()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public List<User> GetUsers() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var asyncWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("异步方法"));
            Assert.IsNotNull(asyncWarning);
            Assert.AreEqual("SQLX3004", asyncWarning.Id);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AnalyzePerformanceOpportunities_AsyncSelectMethod_NoAsyncWarning()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task<List<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            var asyncWarning = diagnostics
                .FirstOrDefault(d => d.GetMessage().Contains("异步方法"));
            Assert.IsNull(asyncWarning);
        }

        #endregion

        #region 边界条件和性能测试

        [TestMethod]
        [TestCategory("EdgeCase")]
        public void AnalyzePerformanceOpportunities_NullEntity_HandlesGracefully()
        {
            // Arrange
            var methodCode = @"
                [Sqlx(""SELECT * FROM [User]"")]
                public Task<List<User>> GetUsersAsync() 
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode);
            var sql = "SELECT * FROM [User]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, null, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            // Should not throw exception with null entity
        }


        #endregion

        #region 复合场景测试

        [TestMethod]
        [TestCategory("Integration")]
        public void AnalyzePerformanceOpportunities_MethodWithMultipleIssues_ReportsAllIssues()
        {
            // Arrange - Method with multiple performance issues
            var entityCode = @"
public class TestEntity
{
    public int Id { get; set; }
    public string String1 { get; set; }
    public string String2 { get; set; }
    public string String3 { get; set; }
    public string String4 { get; set; }
    public string String5 { get; set; }
    public string String6 { get; set; }
    public string Property1 { get; set; }
    public string Property2 { get; set; }
    public string Property3 { get; set; }
    public string Property4 { get; set; }
    public string Property5 { get; set; }
    public string Property6 { get; set; }
    public string Property7 { get; set; }
    public string Property8 { get; set; }
    public string Property9 { get; set; }
    public string Property10 { get; set; } // Large entity with many strings
}";
            var entity = CreateEntitySymbol(entityCode);

            var methodCode = @"
                [Sqlx(""SELECT * FROM [TestEntity]"")]
                public List<TestEntity> GetEntities() // Sync method, no paging, returns collection
                { 
                    throw new NotImplementedException(); 
                }";
            var method = CreateMethodSymbol(methodCode, entityCode);
            var sql = "SELECT * FROM [TestEntity]";

            // Act
            var diagnostics = DiagnosticHelper.AnalyzePerformanceOpportunities(method, entity, sql);

            // Assert
            Assert.IsNotNull(diagnostics);
            Assert.IsTrue(diagnostics.Count >= 3);

            // Should suggest async
            var asyncSuggestion = diagnostics.Any(d => d.GetMessage().Contains("异步方法"));
            Assert.IsTrue(asyncSuggestion);

            // Should suggest paging
            var pagingSuggestion = diagnostics.Any(d => d.GetMessage().Contains("分页支持"));
            Assert.IsTrue(pagingSuggestion);

            // Should suggest projection for large entity
            var projectionSuggestion = diagnostics.Any(d => d.GetMessage().Contains("投影查询"));
            Assert.IsTrue(projectionSuggestion);

            // Should suggest string optimization
            var stringSuggestion = diagnostics.Any(d => d.GetMessage().Contains("字符串"));
            Assert.IsTrue(stringSuggestion);
        }

        #endregion
    }
}
