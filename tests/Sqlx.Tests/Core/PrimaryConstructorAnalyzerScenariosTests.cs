// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorAnalyzerScenariosTests.cs" company="Microsoft">
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
public class PrimaryConstructorAnalyzerScenariosTests
{
    [TestMethod]
    public void Record_With_PrimaryConstructor_Exposes_All_Members()
    {
        var src = @"namespace N { public record User(int id, string name) { public int Age {get; init;} } }";
        var comp = CSharpCompilation.Create("PC1", new[] { CSharpSyntaxTree.ParseText(src) }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.User")!;

        Assert.IsTrue(PrimaryConstructorAnalyzer.IsRecord(type));
        Assert.IsTrue(PrimaryConstructorAnalyzer.HasPrimaryConstructor(type));
        var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(type).ToArray();
        Assert.IsTrue(members.Length > 0);
        Assert.IsTrue(members.Any(m => m.Name == "Age"));
    }

    [TestMethod]
    public void Class_With_PrimaryConstructor_And_No_Properties_Still_Recognized()
    {
        var src = @"namespace N { public class Point(int x, int y); }";
        var comp = CSharpCompilation.Create("PC2", new[] { CSharpSyntaxTree.ParseText(src) }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.Point")!;

        Assert.IsTrue(PrimaryConstructorAnalyzer.HasPrimaryConstructor(type));
        var pars = PrimaryConstructorAnalyzer.GetPrimaryConstructorParameters(type).ToArray();
        Assert.AreEqual(2, pars.Length);
    }

    [TestMethod]
    public void ParameterName_To_PropertyName_Casing_Is_Pascal()
    {
        var src = @"namespace N { public class User(int id, string userName) { } }";
        var comp = CSharpCompilation.Create("PC3", new[] { CSharpSyntaxTree.ParseText(src) }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var type = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.User")!;
        var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(type).ToArray();
        Assert.IsTrue(members.Any(m => m.Name == "Id"));
        Assert.IsTrue(members.Any(m => m.Name == "UserName"));
    }
}


