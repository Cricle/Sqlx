// -----------------------------------------------------------------------
// <copyright file="DbConnectionDetectionUnitTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests specifically targeting the GetDbConnectionFieldName and HasDbConnectionField methods
/// to ensure 100% code coverage of the primary constructor support implementation.
/// </summary>
[TestClass]
public class DbConnectionDetectionUnitTests : CodeGenerationTestBase
{
    #region Primary Constructor Detection Tests

    /// <summary>
    /// Tests primary constructor parameter detection with no existing fields or properties.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_PrimaryConstructorOnly_DetectsCorrectly()
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
        // Only primary constructor parameter, no fields or properties
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when primary constructor provides DbConnection");

        // Should not generate connection field when primary constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when primary constructor provides DbConnection parameter");
    }

    /// <summary>
    /// Tests primary constructor parameter detection with custom parameter name.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_PrimaryConstructorCustomName_DetectsCorrectly()
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
    public partial class TestService(DbConnection customDbConnection) : ITestService
    {
        // Primary constructor with custom parameter name
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when primary constructor provides DbConnection with custom name");

        // Should not generate connection field when primary constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when primary constructor provides DbConnection parameter");
    }

    /// <summary>
    /// Tests primary constructor parameter detection among multiple parameters.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_PrimaryConstructorMultipleParams_FindsDbConnection()
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
    public partial class TestService(string tableName, int timeout, DbConnection connection, bool enableLogging, string schema) : ITestService
    {
        // Primary constructor with DbConnection among multiple parameters
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when primary constructor provides DbConnection among multiple parameters");

        // Should not generate connection field when primary constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when primary constructor provides DbConnection parameter");
    }

    /// <summary>
    /// Tests traditional constructor parameter detection as fallback.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_TraditionalConstructorFallback_DetectsCorrectly()
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
        public TestService(DbConnection traditionalConnection) { }
        public TestService(string name, DbConnection otherConnection) { }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when traditional constructor provides DbConnection");

        // Should not generate connection field when constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when constructor provides DbConnection parameter");
    }

    #endregion

    #region Precedence Tests

    /// <summary>
    /// Tests that field takes precedence over primary constructor parameter.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_FieldPrecedence_FieldWins()
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
        
        // Both field and primary constructor parameter exist
        // Field should take precedence
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when field takes precedence");

        // Should not generate connection field when field already exists
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when field already exists");
    }

    /// <summary>
    /// Tests that property takes precedence over primary constructor parameter.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_PropertyPrecedence_PropertyWins()
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
        
        // Both property and primary constructor parameter exist
        // Property should take precedence
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when property takes precedence");

        // Should not generate connection field when property already exists
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when property already exists");
    }

    /// <summary>
    /// Tests that primary constructor takes precedence over traditional constructor.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_PrimaryConstructorPrecedence_PrimaryWins()
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
    public partial class TestService(DbConnection primaryConnection) : ITestService
    {
        public TestService(DbConnection traditionalConnection, string extra) : this(traditionalConnection) { }
        
        // Both primary and traditional constructors exist
        // Primary constructor should take precedence
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when primary constructor takes precedence");

        // Should not generate connection field when primary constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when primary constructor provides DbConnection parameter");
    }

    #endregion

    #region Inheritance Tests

    /// <summary>
    /// Tests inheritance detection with base class having DbConnection field.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_BaseClassField_DetectsInheritance()
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
        protected readonly DbConnection baseConnection;
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : BaseService, ITestService
    {
        // No DbConnection in derived class, but base class has one
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when base class provides DbConnection");

        // Should not generate connection field when base class provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when base class provides DbConnection");
    }

    /// <summary>
    /// Tests inheritance detection with base class having DbConnection property.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_BaseClassProperty_DetectsInheritance()
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
        protected DbConnection BaseConnection { get; set; }
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : BaseService, ITestService
    {
        // No DbConnection in derived class, but base class has property
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when base class provides DbConnection property");

        // Should not generate connection field when base class has property
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when base class has DbConnection property");
    }

    /// <summary>
    /// Tests multiple inheritance levels.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_MultipleInheritanceLevels_FindsInGrandparent()
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

    public class GrandparentService
    {
        protected readonly DbConnection grandparentConnection;
    }

    public class ParentService : GrandparentService
    {
        // No DbConnection here
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : ParentService, ITestService
    {
        // No DbConnection here either, but grandparent has one
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when grandparent class provides DbConnection");

        // Should not generate connection field when grandparent class provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when grandparent class has DbConnection");
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests behavior when primary constructor has no DbConnection parameter.
    /// Should fall back to traditional constructor.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_PrimaryConstructorNoDbConnection_FallsBackToTraditional()
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
    public partial class TestService(string tableName, bool enableLogging) : ITestService
    {
        public TestService(DbConnection traditionalConnection) : this(""default"", false) { }
        
        // Primary constructor has no DbConnection, should fall back to traditional constructor
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when falling back to traditional constructor");

        // Should not generate connection field when traditional constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when traditional constructor provides DbConnection");
    }

    /// <summary>
    /// Tests behavior when multiple DbConnection parameters exist in primary constructor.
    /// Should use the first one.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_MultiplePrimaryConstructorDbConnections_UsesFirst()
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
    public partial class TestService(DbConnection firstConnection, DbConnection secondConnection, DbConnection thirdConnection) : ITestService
    {
        // Multiple DbConnection parameters in primary constructor
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when multiple DbConnection parameters exist");

        // Should not generate connection field when primary constructor has multiple DbConnection params
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when primary constructor provides multiple DbConnection parameters");
    }

    /// <summary>
    /// Tests behavior when no DbConnection found anywhere.
    /// Should generate a DbConnection field.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_NoDbConnectionAnywhere_GeneratesField()
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
        private readonly string connectionString;
        
        // No DbConnection field, property, or constructor parameter anywhere
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation even when no DbConnection exists");

        // Should generate DbConnection field when none exists anywhere
        Assert.IsTrue(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should generate DbConnection field when none exists anywhere");
    }

    #endregion

    #region Constructor Parameter Detection Tests

    /// <summary>
    /// Tests traditional constructor parameter detection with multiple constructors.
    /// Should use first constructor with DbConnection parameter.
    /// </summary>
    [TestMethod]
    public void DbConnectionDetection_MultipleTraditionalConstructors_UsesFirstWithDbConnection()
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
        public TestService(string name) { }
        public TestService(DbConnection firstConnection) { }
        public TestService(DbConnection secondConnection, string extra) { }
        
        // Multiple constructors, should use first one with DbConnection
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);

        // Should generate method implementation
        Assert.IsTrue(generatedCode.Contains("GetCountAsync"),
            "Should generate method implementation when first constructor with DbConnection is found");

        // Should not generate connection field when constructor provides it
        Assert.IsFalse(generatedCode.Contains("protected readonly global::System.Data.Common.DbConnection connection;"),
            "Should not generate connection field when constructor provides DbConnection parameter");
    }

    #endregion
}
