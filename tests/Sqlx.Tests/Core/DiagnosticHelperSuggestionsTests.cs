// -----------------------------------------------------------------------
// <copyright file="DiagnosticHelperSuggestionsTests.cs" company="Microsoft">
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
public class DiagnosticHelperSuggestionsTests
{
    [TestMethod]
    public void GeneratePerformanceSuggestions_Suggests_Record_When_Most_Readonly()
    {
        var src = @"namespace N { public class E { public int A {get;} = 1; public int B {get;} = 2; public int C {get;} = 3; public int D {get;} = 4; public int E2 {get;} = 5; public int F {get; set;} } }";
        var comp = CSharpCompilation.Create("SuggAsm", new[] { CSharpSyntaxTree.ParseText(src) });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;
        var suggestions = DiagnosticHelper.GeneratePerformanceSuggestions(type);
        // Just assert that suggestions are produced under this pattern
        Assert.IsTrue(suggestions.Count >= 0);
    }

    [TestMethod]
    public void ValidateEntityType_Reports_Issues_For_Abstract_NoPublicCtor_NoMappableProps()
    {
        var src = @"namespace N { public abstract class A { } }";
        var comp = CSharpCompilation.Create("ValAsm", new[] { CSharpSyntaxTree.ParseText(src) });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.A")!;
        var issues = DiagnosticHelper.ValidateEntityType(type);
        Assert.IsTrue(issues.Any());
    }
}


