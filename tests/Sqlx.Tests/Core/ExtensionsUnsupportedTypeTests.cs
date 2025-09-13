// -----------------------------------------------------------------------
// <copyright file="ExtensionsUnsupportedTypeTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class ExtensionsUnsupportedTypeTests
{
    [TestMethod]
    public void GetDbType_UnsupportedType_Throws_NotSupported()
    {
        var src = "namespace N { public class X {} }";
        var tree = CSharpSyntaxTree.ParseText(src);
        var comp = CSharpCompilation.Create("UnsupAsm", new[] { tree });
        var x = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.X")!;
        Assert.ThrowsException<System.NotSupportedException>(() => Sqlx.Extensions.GetDbType(x));
    }
}


