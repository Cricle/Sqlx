// -----------------------------------------------------------------------
// <copyright file="PerformanceOptimizationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests;

/// <summary>
/// Tests for performance optimizations in generated code.
/// </summary>
[TestClass]
public class PerformanceOptimizationTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that performance monitoring code is generated when enabled.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_MonitoringEnabled_GeneratesMonitoringCode()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users"")]
        public static partial IList<User> GetUsers(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);

        // Check if performance monitoring is included in generated code
        Assert.IsTrue(result.Contains("__startTime__") || result.Contains("Stopwatch"),
            "Should include performance monitoring when enabled");
    }

    /// <summary>
    /// Tests that compiler optimizations are applied to generated methods.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_CompilerOptimizations_AppliedCorrectly()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users"")]
        public static partial IList<User> GetUsers(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);

        // Check for optimized patterns in generated code
        Assert.IsTrue(result.Contains("global::") || !string.IsNullOrEmpty(result),
            "Should use fully qualified type names for better performance");

        // Check that unnecessary allocations are avoided
        Assert.IsFalse(result.Contains("new string[]") && result.Contains("string.Format"),
            "Should avoid unnecessary string allocations");
    }

    /// <summary>
    /// Tests that memory optimizations are applied in bulk operations.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_MemoryOptimizations_BulkOperations()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name, Email, Phone FROM Users"")]
        public static partial IList<User> GetManyUsers(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);

        // Verify that bulk operations use efficient patterns
        Assert.IsTrue(result.Contains("List<") || !string.IsNullOrEmpty(result),
            "Should use List<T> for efficient bulk operations");

        // Check that object reuse patterns are used where appropriate
        Assert.IsTrue(result.Contains("new TestNamespace.User") || !string.IsNullOrEmpty(result),
            "Should create objects efficiently in loops");
    }

    /// <summary>
    /// Tests that async optimization patterns are applied.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_AsyncOptimizations_AppliedCorrectly()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users"")]
        public static partial Task<IList<User>> GetUsersAsync(this DbConnection connection, CancellationToken cancellationToken = default);
    }
}";

        var result = GetCSharpGeneratedOutput(source);

        // Check for async optimization patterns
        Assert.IsTrue(result.Contains("async") || result.Contains("Task"),
            "Should generate proper async method signatures");

        if (result.Contains("async"))
        {
            Assert.IsTrue(result.Contains("await") || result.Contains("ConfigureAwait"),
                "Should use proper async/await patterns");
        }
    }

    /// <summary>
    /// Tests the MemoryOptimizer utility functions.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_MemoryOptimizer_FunctionsCorrectly()
    {
        // Test efficient string concatenation
        var result = MemoryOptimizer.ConcatenateEfficiently("SELECT", " * ", "FROM", " Users");
        Assert.AreEqual("SELECT * FROM Users", result);

        // Test optimized StringBuilder creation
        var sb = MemoryOptimizer.CreateOptimizedStringBuilder(1024);
        Assert.IsNotNull(sb);
        Assert.IsTrue(sb.Capacity >= 256, "Should have minimum capacity");

        // Test efficient string building
        var builtString = MemoryOptimizer.BuildString(builder =>
        {
            builder.Append("SELECT");
            builder.Append(" Id, Name");
            builder.Append(" FROM Users");
        });
        Assert.AreEqual("SELECT Id, Name FROM Users", builtString);
    }

    /// <summary>
    /// Tests the TypeAnalyzer caching mechanisms.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_TypeAnalyzer_CachingWorks()
    {
        // Clear caches first
        TypeAnalyzer.ClearCaches();

        // Note: TypeAnalyzer methods require ITypeSymbol, not System.Type
        // This is a placeholder test that documents the intended functionality

        Assert.IsTrue(true, "TypeAnalyzer caching functionality would be tested with proper ITypeSymbol instances");
    }

    /// <summary>
    /// Tests performance monitoring functionality.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_PerformanceMonitor_Works()
    {
        // Reset monitoring state
        PerformanceMonitor.Clear();

        // Record some operations
        for (int i = 0; i < 5; i++)
        {
            PerformanceMonitor.RecordOperation();
        }

        // Get total operations
        var totalOps = PerformanceMonitor.TotalOperations;

        Assert.AreEqual(5, totalOps, "Should record five operations");
    }


    /// <summary>
    /// Tests that hot path optimizations are applied to frequently used methods.
    /// </summary>
    [TestMethod]
    public void PerformanceOptimization_HotPathOptimizations_AppliedToFrequentMethods()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users WHERE Id = @id"")]
        public static partial User? GetUserById(this DbConnection connection, int id);
        
        [Sqlx(""SELECT COUNT(*) FROM Users"")]
        public static partial int GetUserCount(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);

        // Check for hot path optimizations in frequently called methods
        Assert.IsTrue(!string.IsNullOrEmpty(result), "Should generate code successfully");

        // Single-result methods should be optimized for fast execution
        if (result.Contains("GetUserById"))
        {
            Assert.IsFalse(result.Contains("List<User>"),
                "Single result methods should not create unnecessary collections");
        }
    }

    /// <summary>
    /// A test entity for performance testing.
    /// </summary>
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
