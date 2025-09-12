// -----------------------------------------------------------------------
// <copyright file="ExtensionMethodTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ExtensionMethodTests : CodeGenerationTestBase
{
    [TestMethod]
    public void ScalarResult()
    {
        string source = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Foo
{
    static partial class C
    {
        [Sqlx(""sp_TestSP"")]
        public static partial Task<int> M(this DbConnection connection, int clientId, string? personId);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Validate that code generation succeeded and contains expected elements
        Assert.IsTrue(output.Contains("namespace Foo"), "Generated code should contain Foo namespace");
        Assert.IsTrue(output.Contains("internal static partial class C"), "Generated code should contain partial class C");
        Assert.IsTrue(output.Contains("public async static partial System.Threading.Tasks.Task<int> M"), "Generated code should contain method M");
        Assert.IsTrue(output.Contains("EXEC sp_TestSP"), "Generated code should contain stored procedure call");
        Assert.IsTrue(output.Contains("@client_id"), "Generated code should contain client_id parameter");
        Assert.IsTrue(output.Contains("@person_id"), "Generated code should contain person_id parameter");
        Assert.IsTrue(output.Contains("OnExecuting"), "Generated code should contain OnExecuting method");
        Assert.IsTrue(output.Contains("OnExecuted"), "Generated code should contain OnExecuted method");
        Assert.IsTrue(output.Contains("OnExecuteFail"), "Generated code should contain OnExecuteFail method");
    }
}
