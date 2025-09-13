// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperScenariosTests.cs" company="Microsoft">
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
public class DiagnosticHelperScenariosTests
{
    [TestMethod]
    public void CreateDiagnostics_WithExpectedIds_AndSeverities()
    {
        var comp = CreateCompilation("namespace N { public class E {} }");
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;

        var d1 = DiagnosticHelper.CreatePrimaryConstructorDiagnostic("issue", type);
        Assert.AreEqual("SQLX1001", d1.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, d1.Severity);
        StringAssert.Contains(d1.GetMessage(), type.Name);

        var d2 = DiagnosticHelper.CreateRecordTypeDiagnostic("issue", type);
        Assert.AreEqual("SQLX1002", d2.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, d2.Severity);

        var d3 = DiagnosticHelper.CreateEntityInferenceDiagnostic("issue", "M");
        Assert.AreEqual("SQLX1003", d3.Id);
        Assert.AreEqual(DiagnosticSeverity.Info, d3.Severity);

        var d4 = DiagnosticHelper.CreatePerformanceSuggestion("suggest", "ctx");
        Assert.AreEqual("SQLX2001", d4.Id);
        Assert.AreEqual(DiagnosticSeverity.Info, d4.Severity);
    }

    [TestMethod]
    public void GenerateTypeAnalysisReport_ContainsCoreInfo()
    {
        var comp = CreateCompilation("namespace N { public class E { public int Id {get;set;} } }");
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;

        var report = DiagnosticHelper.GenerateTypeAnalysisReport(type);
        StringAssert.Contains(report, "类型分析报告");
        StringAssert.Contains(report, type.Name);
        StringAssert.Contains(report, "构造函数数量");
    }

    [TestMethod]
    public void ValidateGeneratedCode_Returns_ExpectedIssues()
    {
        var comp = CreateCompilation("namespace N { public class E { } }");
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;

        var badCode = "(DateTime)reader.GetValue(0); reader.GetInt32(0);"; // triggers DateTime cast and missing IsDBNull
        var issues = DiagnosticHelper.ValidateGeneratedCode(badCode, type);
        Assert.IsTrue(issues.Any());
        Assert.IsTrue(issues.Any(x => x.Contains("DateTime")));
        Assert.IsTrue(issues.Any(x => x.Contains("null 检查")));
    }

    private static CSharpCompilation CreateCompilation(string src)
    {
        return CSharpCompilation.Create(
            "DiagAsm",
            new[] { CSharpSyntaxTree.ParseText(src) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
    }
}


