// -----------------------------------------------------------------------
// <copyright file="ExtensionsDataReadExpressionCacheTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class ExtensionsDataReadExpressionCacheTests
{
    [TestMethod]
    public void GetDataReadExpression_Creates_Stable_Expression_For_Same_Input()
    {
        var source = @"namespace N { public class U { public int Id { get; set; } } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("A", new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var type = (INamedTypeSymbol)compilation.GetTypeByMetadataName("N.U")!;
        var prop = type.GetMembers("Id")[0];
        var propType = ((IPropertySymbol)prop).Type;

        var e1 = Sqlx.Extensions.GetDataReadExpression(propType, "r", "Id");
        var e2 = Sqlx.Extensions.GetDataReadExpression(propType, "r", "Id");

        Assert.AreEqual(e1, e2);
        Assert.IsTrue(e1.Contains("GetInt32") || e1.Contains("IsDBNull"));
    }
}


