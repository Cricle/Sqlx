// -----------------------------------------------------------------------
// <copyright file="ExtensionsNullableAndParamsTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Sqlx.Tests.Core;

[TestClass]
public class ExtensionsNullableAndParamsTests
{
    [TestMethod]
    public void CanHaveNullValue_And_IsNullableType_Behave_As_Expected()
    {
        var comp = CreateCompilation("namespace N { public class U { public int? A {get;set;} public string? B {get;set;} public int C {get;set;} public string D {get;set;} = string.Empty; } }");
        var u = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.U")!;
        var a = ((IPropertySymbol)u.GetMembers("A")[0]).Type; // int?
        var b = ((IPropertySymbol)u.GetMembers("B")[0]).Type; // string?
        var c = ((IPropertySymbol)u.GetMembers("C")[0]).Type; // int
        var d = ((IPropertySymbol)u.GetMembers("D")[0]).Type; // string

        Assert.IsTrue(Sqlx.Extensions.IsNullableType(a));
        Assert.IsTrue(Sqlx.Extensions.CanHaveNullValue(b));
        Assert.IsFalse(Sqlx.Extensions.IsNullableType(c));
        Assert.IsTrue(Sqlx.Extensions.CanHaveNullValue(d));
    }

    [TestMethod]
    public void Public_GetParameterName_For_ITypeSymbol_Formats_With_At_And_Strips_Underscore()
    {
        var comp = CreateCompilation("namespace N { public class U { public int user_name {get;set;} } }");
        var u = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.U")!;
        var prop = (IPropertySymbol)u.GetMembers("user_name")[0];
        var t = prop.Type;

        var parameter = Sqlx.Extensions.GetParameterName(t, "user_name");
        Assert.AreEqual("@username", parameter);
    }

    private static CSharpCompilation CreateCompilation(string src)
    {
        var tree = CSharpSyntaxTree.ParseText(src);
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };
        return CSharpCompilation.Create("ExtNullAsm", new[] { tree }, refs);
    }
}


