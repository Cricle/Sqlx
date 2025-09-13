// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperLoggingAndValidateOkTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class DiagnosticHelperLoggingAndValidateOkTests
{
    [TestMethod]
    public void LogCodeGenerationContext_DoesNotThrow()
    {
        var comp = CSharpCompilation.Create("LogAsm", new[] { CSharpSyntaxTree.ParseText("namespace N { public class E{} }") });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;
        DiagnosticHelper.LogCodeGenerationContext("CTX", type, "Method");
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ValidateGeneratedCode_OkCode_Returns_NoIssues()
    {
        var comp = CSharpCompilation.Create("ValOkAsm", new[] { CSharpSyntaxTree.ParseText("namespace N { public class E{} }") });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;
        var ok = "var x = reader.IsDBNull(0) ? default : reader.GetInt32(0); var e = new N.E();";
        var issues = DiagnosticHelper.ValidateGeneratedCode(ok, type);
        // May include other checks in future; accept empty or non-critical
        Assert.IsTrue(issues.Count == 0);
    }
}


