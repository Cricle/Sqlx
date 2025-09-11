// -----------------------------------------------------------------------
// <copyright file="DbDataReaderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DbDataReaderTests : CodeGenerationTestBase
{
    [TestMethod]
    public void DbDataReaderResult()
    {
        string source = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace Foo
{
    partial class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial System.Data.Common.DbDataReader M(int clientId, string? personId);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Verify that the generated code contains the essential parts
        Assert.IsTrue(output.Contains("public partial System.Data.Common.DbDataReader M(int clientId, string? personId)"),
            "Should generate the correct method signature");
        Assert.IsTrue(output.Contains("EXEC sp_TestSP") || output.Contains("sp_TestSP"),
            "Should contain the stored procedure call");
        Assert.IsTrue(output.Contains("@client_id") && output.Contains("@person_id"),
            "Should contain parameter names");
        Assert.IsTrue(output.Contains("ExecuteReader"),
            "Should call ExecuteReader method");
        
        // Verify modern code generation features are present
        Assert.IsTrue(output.Contains("OnExecuting") && output.Contains("OnExecuted"),
            "Should include interceptor methods");
        Assert.IsTrue(output.Contains("global::System.DBNull.Value"),
            "Should handle null values properly");
    }
}
