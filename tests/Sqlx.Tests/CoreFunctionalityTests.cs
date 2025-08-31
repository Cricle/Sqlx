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

        [Sqlx(""GetUserCount"")]
        public partial int GetUserCount();
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Source generator should produce code");
        Assert.IsTrue(generatedCode.Contains("GetUserCount"), "Generated code should contain method implementation");
        Assert.IsTrue(generatedCode.Contains("_connection"), "Generated code should use connection field");
        Assert.IsTrue(generatedCode.Contains("CreateCommand"), "Generated code should create command");
        Assert.IsTrue(generatedCode.Contains("EXEC GetUserCount"), "Generated code should call stored procedure");
    }
}