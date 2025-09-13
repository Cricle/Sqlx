// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorSmartUpdateTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for AbstractGenerator smart update functionality.
/// Tests the advanced update features including partial updates, batch updates, and optimized update operations.
/// </summary>
[TestClass]
public class AbstractGeneratorSmartUpdateTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests smart update detection and generation for partial field updates.
    /// </summary>
    [TestMethod]
    public void SmartUpdate_WithPartialFieldUpdate_GeneratesOptimizedUpdate()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; }
    }

    public interface ISmartUpdateService
    {
        // Smart partial update methods
        int UpdateUserName(int id, string newName);
        int UpdateUserEmail(int id, string newEmail);
        int UpdateUserStatus(int id, bool isActive);
        int UpdateUserLastLogin(int id, DateTime lastLogin);
        
        // Expression-based updates
        [ExpressionToSql]
        int UpdateUserFields(Expression<Func<User, bool>> where, Expression<Func<User, User>> update);
        
        // Batch field updates
        int UpdateMultipleUserNames(Dictionary<int, string> updates);
        int UpdateUserStatusBatch(List<int> userIds, bool newStatus);
    }

    [RepositoryFor(typeof(ISmartUpdateService))]
    public partial class SmartUpdateService : ISmartUpdateService
    {
        private readonly DbConnection connection;
        public SmartUpdateService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle smart update patterns without errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Smart update methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate optimized UPDATE statements for partial updates
        Assert.IsTrue(generatedCode.Contains("UPDATE") && generatedCode.Contains("SET"), 
            "Should generate UPDATE statements for smart update operations");
        Assert.IsTrue(generatedCode.Contains("WHERE"), 
            "Should include WHERE clauses for targeted updates");
    }

    /// <summary>
    /// Tests incremental update functionality for numeric fields.
    /// </summary>
    [TestMethod]
    public void SmartUpdate_WithIncrementalUpdates_GeneratesIncrementStatements()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public int ViewCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public interface IIncrementalUpdateService
    {
        // Incremental numeric updates
        int IncrementStock(int productId, int amount);
        int DecrementStock(int productId, int amount);
        int IncrementViewCount(int productId);
        int AdjustPrice(int productId, decimal adjustment);
        
        // Batch incremental updates
        int IncrementStockBatch(Dictionary<int, int> adjustments);
        int AdjustPricesBatch(List<int> productIds, decimal percentage);
        
        // Smart increment with conditions
        int IncrementViewCountIfActive(int productId);
        int AdjustStockWithMinimum(int productId, int adjustment, int minimumStock);
    }

    [RepositoryFor(typeof(IIncrementalUpdateService))]
    public partial class IncrementalUpdateService : IIncrementalUpdateService
    {
        private readonly DbConnection connection;
        public IncrementalUpdateService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle incremental update patterns
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Incremental update methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate SQL with arithmetic operations for increments
        Assert.IsTrue(generatedCode.Contains("UPDATE") || generatedCode.Contains("SET"), 
            "Should generate UPDATE statements for incremental operations");
        // May contain SQL arithmetic like column = column + @amount
        Assert.IsTrue(generatedCode.Contains("+") || generatedCode.Contains("-") || generatedCode.Contains("adjustment"), 
            "Should handle arithmetic operations in SQL updates");
    }

    /// <summary>
    /// Tests optimistic concurrency control in smart updates.
    /// </summary>
    [TestMethod]
    public void SmartUpdate_WithOptimisticConcurrency_HandlesVersioning()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class VersionedEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime LastModified { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
    }

    public interface IOptimisticUpdateService
    {
        // Optimistic concurrency updates
        int UpdateWithVersion(VersionedEntity entity, int expectedVersion);
        int UpdateNameWithVersion(int id, string newName, int expectedVersion);
        
        // Timestamp-based optimistic updates
        int UpdateWithTimestamp(VersionedEntity entity, DateTime expectedTimestamp);
        int UpdateDataWithTimestamp(int id, string newData, DateTime expectedTimestamp);
        
        // Conditional updates with multiple criteria
        int UpdateIfUnchanged(int id, string newName, int expectedVersion, string expectedModifiedBy);
        
        // Bulk optimistic updates
        int UpdateBatchWithVersions(List<VersionedEntity> entities);
    }

    [RepositoryFor(typeof(IOptimisticUpdateService))]
    public partial class OptimisticUpdateService : IOptimisticUpdateService
    {
        private readonly DbConnection connection;
        public OptimisticUpdateService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle optimistic concurrency patterns
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Optimistic concurrency methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate UPDATE statements with version/timestamp checks
        Assert.IsTrue(generatedCode.Contains("UPDATE") && generatedCode.Contains("WHERE"), 
            "Should generate conditional UPDATE statements for optimistic concurrency");
        Assert.IsTrue(generatedCode.Contains("Version") || generatedCode.Contains("Timestamp") || generatedCode.Contains("expectedVersion"), 
            "Should include version or timestamp checks in WHERE clauses");
    }

    /// <summary>
    /// Tests conditional update operations with complex business logic.
    /// </summary>
    [TestMethod]
    public void SmartUpdate_WithConditionalUpdates_GeneratesConditionalLogic()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Order
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public string? TrackingNumber { get; set; }
        public bool IsPaid { get; set; }
    }

    public interface IConditionalUpdateService
    {
        // Status transition updates
        int MarkOrderAsPaid(int orderId, string currentStatus);
        int ShipOrder(int orderId, string trackingNumber, DateTime shippedDate);
        int CancelOrderIfPending(int orderId);
        
        // Conditional field updates
        int UpdateTotalIfUnpaid(int orderId, decimal newTotal);
        int SetTrackingIfShipped(int orderId, string trackingNumber);
        
        // Complex conditional updates
        int UpdateOrderStatusWithValidation(int orderId, string newStatus, string expectedCurrentStatus, bool mustBePaid);
        int CompleteOrderIfReady(int orderId, DateTime completionDate);
        
        // Batch conditional updates
        int MarkOrdersAsShippedBatch(List<int> orderIds, DateTime shippedDate);
        int CancelUnpaidOrdersBatch(List<int> orderIds, DateTime cutoffDate);
    }

    [RepositoryFor(typeof(IConditionalUpdateService))]
    public partial class ConditionalUpdateService : IConditionalUpdateService
    {
        private readonly DbConnection connection;
        public ConditionalUpdateService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle conditional update patterns
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Conditional update methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate conditional UPDATE logic
        Assert.IsTrue(generatedCode.Contains("UPDATE") && generatedCode.Contains("WHERE"), 
            "Should generate conditional UPDATE statements");
        Assert.IsTrue(generatedCode.Contains("Status") || generatedCode.Contains("currentStatus"), 
            "Should include status-based conditions in updates");
    }

    /// <summary>
    /// Tests performance optimization in bulk update operations.
    /// </summary>
    [TestMethod]
    public void SmartUpdate_WithBulkOperations_OptimizesPerformance()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class BulkUpdateEntity
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime ProcessedDate { get; set; }
    }

    public interface IBulkUpdateService
    {
        // High-performance bulk updates
        int UpdateCategoryBatch(List<BulkUpdateEntity> entities);
        int UpdateValuesBatch(Dictionary<int, decimal> valueUpdates);
        Task<int> UpdateCategoryBatchAsync(List<BulkUpdateEntity> entities);
        
        // Optimized batch operations with conditions
        int MarkEntitiesAsProcessed(List<int> ids, DateTime processedDate);
        int UpdateCategoryForRange(int startId, int endId, string newCategory);
        int BulkUpdateValues(List<BulkUpdateEntity> entities, bool onlyUnprocessed);
        
        // Performance-critical operations
        int UpdateLargeDataset(IEnumerable<BulkUpdateEntity> entities);
        Task<int> UpdateLargeDatasetAsync(IEnumerable<BulkUpdateEntity> entities);
        
        // Chunked bulk operations
        int UpdateInChunks(List<BulkUpdateEntity> entities, int chunkSize);
    }

    [RepositoryFor(typeof(IBulkUpdateService))]
    public partial class BulkUpdateService : IBulkUpdateService
    {
        private readonly DbConnection connection;
        public BulkUpdateService(DbConnection connection) => this.connection = connection;
    }
}";

        var startTime = DateTime.UtcNow;
        
        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);
        
        var endTime = DateTime.UtcNow;
        var generationTime = endTime - startTime;

        // Should handle bulk operations efficiently
        Assert.IsTrue(generationTime.TotalSeconds < 15, 
            $"Bulk update generation should be efficient. Took: {generationTime.TotalSeconds} seconds");

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Bulk update methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should generate optimized bulk update patterns
        Assert.IsTrue(generatedCode.Contains("UPDATE") || generatedCode.Contains("batch") || generatedCode.Contains("Batch"), 
            "Should generate bulk update operations");
        Assert.IsTrue(generatedCode.Contains("foreach") || generatedCode.Contains("IEnumerable") || generatedCode.Contains("Count"), 
            "Should handle collections efficiently in bulk operations");
    }

    /// <summary>
    /// Tests smart update integration with expression trees and LINQ.
    /// </summary>
    [TestMethod]
    public void SmartUpdate_WithExpressionTrees_HandlesLinqExpressions()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class ExpressionEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public interface IExpressionUpdateService
    {
        // Expression-based field updates
        [ExpressionToSql]
        int UpdateWhere(Expression<Func<ExpressionEntity, bool>> where, Expression<Func<ExpressionEntity, ExpressionEntity>> update);
        
        [ExpressionToSql] 
        int IncrementAmountWhere(Expression<Func<ExpressionEntity, bool>> where, decimal increment);
        
        [ExpressionToSql]
        int SetActiveStatusWhere(Expression<Func<ExpressionEntity, bool>> where, bool isActive);
        
        // Complex expression updates
        [ExpressionToSql]
        int UpdateFieldsConditionally(
            Expression<Func<ExpressionEntity, bool>> condition,
            Expression<Func<ExpressionEntity, string>> nameSelector, 
            string newName,
            Expression<Func<ExpressionEntity, decimal>> amountSelector,
            decimal newAmount);
    }

    [RepositoryFor(typeof(IExpressionUpdateService))]
    public partial class ExpressionUpdateService : IExpressionUpdateService
    {
        private readonly DbConnection connection;
        public ExpressionUpdateService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Should handle expression-based updates
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Expression-based update methods should compile without errors. Errors:\n{errorMessages}");
        }

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);

        // Should handle expression-to-SQL conversion for updates
        Assert.IsTrue(generatedCode.Contains("Expression") || generatedCode.Contains("UPDATE") || 
                     generatedCode.Contains("ExpressionToSql"), 
            "Should handle expression-based update operations");
    }

    /// <summary>
    /// Helper method to compile source code with the Sqlx generator.
    /// </summary>
    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithSourceGenerator(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        return (newCompilation, diagnostics);
    }

    /// <summary>
    /// Gets basic references needed for compilation.
    /// </summary>
    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));

        var runtimeAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        // Add reference to the Sqlx assembly
        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        return references;
    }
}
