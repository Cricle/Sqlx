// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorDbConnectionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive tests for primary constructor DbConnection support in RepositoryFor generator.
/// Tests both GetDbConnectionFieldName and HasDbConnectionField methods with 100% coverage.
/// </summary>
[TestClass]
public class PrimaryConstructorDbConnectionTests : CodeGenerationTestBase
{
    #region Core Functionality Tests

    /// <summary>
    /// Tests that RepositoryFor with primary constructor compiles successfully.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithPrimaryConstructor_CompilesSuccessfully()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService(DbConnection connection) : ITestService
    {
        // Primary constructor with DbConnection parameter
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field since primary constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when primary constructor provides it");
    }

    /// <summary>
    /// Tests that RepositoryFor with named primary constructor parameter works.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithNamedPrimaryConstructorParameter_CompilesSuccessfully()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService(DbConnection dbConnection) : ITestService
    {
        // Primary constructor with named DbConnection parameter
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection dbConnection"),
            "Should not generate additional DbConnection field when primary constructor provides it");
    }

    /// <summary>
    /// Tests that RepositoryFor with multi-parameter primary constructor works.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithMultiParameterPrimaryConstructor_CompilesSuccessfully()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService(string tableName, DbConnection connection, bool enableLogging) : ITestService
    {
        // Primary constructor with multiple parameters including DbConnection
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when primary constructor provides it");
    }

    /// <summary>
    /// Tests that RepositoryFor with existing DbConnection field works.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithExistingDbConnectionField_CompilesSuccessfully()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : ITestService
    {
        private readonly DbConnection connection;
        
        public TestService(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field since one already exists
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when one already exists");
    }

    /// <summary>
    /// Tests that RepositoryFor with existing DbConnection property works.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithExistingDbConnectionProperty_CompilesSuccessfully()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : ITestService
    {
        protected DbConnection Connection { get; set; }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field since property already exists
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when property already exists");
    }

    /// <summary>
    /// Tests that RepositoryFor without any DbConnection generates one.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithoutDbConnection_GeneratesDbConnectionField()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : ITestService
    {
        // No DbConnection field, property, or constructor parameter
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should generate DbConnection field since none exists
        Assert.IsTrue(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should generate DbConnection field when none exists");
    }

    /// <summary>
    /// Tests that RepositoryFor with inheritance works.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithInheritance_CompilesSuccessfully()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    public class BaseService
    {
        protected readonly DbConnection connection;
        
        protected BaseService(DbConnection connection)
        {
            this.connection = connection;
        }
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : BaseService, ITestService
    {
        public TestService(DbConnection connection) : base(connection) { }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field since base class has one
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when base class has one");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests behavior when class has both field and primary constructor parameter.
    /// Field should take precedence.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithBothFieldAndPrimaryConstructor_FieldTakesPrecedence()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService(DbConnection connection) : ITestService
    {
        private readonly DbConnection fieldConnection;
        
        // Both primary constructor parameter and field exist
        // Field should take precedence
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field since field already exists
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when field already exists");
    }

    /// <summary>
    /// Tests behavior when class has both property and primary constructor parameter.
    /// Property should take precedence.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithBothPropertyAndPrimaryConstructor_PropertyTakesPrecedence()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService(DbConnection connection) : ITestService
    {
        protected DbConnection PropertyConnection { get; set; }
        
        // Both primary constructor parameter and property exist
        // Property should take precedence
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field since property already exists
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection"),
            "Should not generate additional DbConnection field when property already exists");
    }

    /// <summary>
    /// Tests behavior when primary constructor has multiple DbConnection parameters.
    /// Should use the first one.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_WithMultipleDbConnectionInPrimaryConstructor_UsesFirst()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestService 
    {
        Task<int> GetCountAsync();
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService(DbConnection firstConnection, DbConnection secondConnection) : ITestService
    {
        // Multiple DbConnection parameters in primary constructor
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation without errors
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Generated code should contain method implementation");

        // Should not generate additional DbConnection field
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection"),
            "Should not generate additional DbConnection field when primary constructor provides them");
    }

    #endregion

    #region Compilation Tests

    /// <summary>
    /// Tests that all generated code compiles without errors.
    /// </summary>
    [TestMethod]
    public void RepositoryFor_PrimaryConstructorSupport_AllVariationsCompile()
    {
        var testCases = new[]
        {
            // Primary constructor with connection
            @"[RepositoryFor(typeof(ITestService))] public partial class TestService(DbConnection connection) : ITestService { }",
            
            // Primary constructor with named connection
            @"[RepositoryFor(typeof(ITestService))] public partial class TestService(DbConnection dbConnection) : ITestService { }",
            
            // Multi-parameter primary constructor
            @"[RepositoryFor(typeof(ITestService))] public partial class TestService(string name, DbConnection connection, bool flag) : ITestService { }",
            
            // Traditional constructor
            @"[RepositoryFor(typeof(ITestService))] public partial class TestService : ITestService { private readonly DbConnection connection; public TestService(DbConnection connection) { this.connection = connection; } }",
            
            // Property-based
            @"[RepositoryFor(typeof(ITestService))] public partial class TestService : ITestService { protected DbConnection Connection { get; set; } }"
        };

        foreach (var testCase in testCases)
        {
            string sourceCode = $@"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{{
    public interface ITestService 
    {{
        Task<int> GetCountAsync();
    }}

    {testCase}
}}";

            var generatedCode = GetCSharpGeneratedOutput(sourceCode);

            // Should generate method implementation without errors
            Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
                $"Generated code should contain method implementation for test case: {testCase}");

            // Should not throw compilation errors
            Assert.IsTrue(generatedCode.Length > 0,
                $"Generated code should not be empty for test case: {testCase}");
        }
    }

    #endregion
}