// -----------------------------------------------------------------------
// <copyright file="CoreFunctionalityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for core Sqlx functionality.
/// </summary>
[TestClass]
public class CoreFunctionalityTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that the CSharpGenerator can be instantiated and implements ISourceGenerator.
    /// </summary>
    [TestMethod]
    public void CSharpGenerator_CanBeInstantiated()
    {
        // Verify that our source generator can be created
        var generator = new CSharpGenerator();
        Assert.IsNotNull(generator);
        Assert.IsInstanceOfType(generator, typeof(ISourceGenerator));
    }

    /// <summary>
    /// Tests that the source generator can generate basic method implementations.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_GeneratesBasicMethodImplementation()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public partial class TestRepository
    {
        private readonly DbConnection _connection;

        [Sqlx(""SELECT COUNT(*) FROM Users"")]
        public partial int GetUserCount();
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Source generator should produce code");
        Assert.IsTrue(generatedCode.Contains("GetUserCount"), "Generated code should contain method implementation");
        Assert.IsTrue(generatedCode.Contains("_connection"), "Generated code should use connection field");
        Assert.IsTrue(generatedCode.Contains("CreateCommand"), "Generated code should create command");
    }

    /// <summary>
    /// Tests that the source generator handles async methods correctly.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_HandlesAsyncMethods()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public partial class TestRepository
    {
        private readonly DbConnection _connection;

        [Sqlx(""SELECT COUNT(*) FROM Users"")]
        public partial Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Source generator should produce code for async methods");
        Assert.IsTrue(generatedCode.Contains("GetUserCountAsync"), "Generated code should contain async method");
        Assert.IsTrue(generatedCode.Contains("await"), "Generated code should use await for async operations");
        Assert.IsTrue(generatedCode.Contains("CancellationToken"), "Generated code should handle cancellation tokens");
    }

    /// <summary>
    /// Tests that the source generator handles repository patterns correctly.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_HandlesRepositoryPattern()
    {
        string sourceCode = @"
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

    public interface IUserService
    {
        IList<User> GetAllUsers();
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserRepository : IUserService
    {
        private readonly DbConnection connection;

        public UserRepository(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Source generator should produce code for repository pattern");
        Assert.IsTrue(generatedCode.Contains("GetAllUsers"), "Generated code should contain interface methods");
        Assert.IsTrue(generatedCode.Contains("IList<TestNamespace.User>") || generatedCode.Contains("IList<User>"), "Generated code should handle generic collections");
    }
}