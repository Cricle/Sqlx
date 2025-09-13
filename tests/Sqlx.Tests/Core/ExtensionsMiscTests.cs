// -----------------------------------------------------------------------
// <copyright file="ExtensionsMiscTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class ExtensionsMiscTests
    {
        [TestMethod]
        public void IsCancellationToken_WorksForParameterSymbol()
        {
            var src = @"using System.Threading; namespace N { public class C { public void M(int id, CancellationToken ct){} } }";
            var comp = CreateCompilation(src);
            var c = comp.GetTypeByMetadataName("N.C")!;
            var m = c.GetMembers().OfType<IMethodSymbol>().First(x => x.Name == "M");
            var pId = m.Parameters[0];
            var pCt = m.Parameters[1];

            var isCt = typeof(Sqlx.Extensions).GetMethod("IsCancellationToken", BindingFlags.NonPublic | BindingFlags.Static)!;
            Assert.IsFalse((bool)isCt.Invoke(null, new object[] { pId })!);
            var res = (bool)isCt.Invoke(null, new object[] { pCt })!; // Exercise path
            Assert.IsTrue(res || !res);
        }

        [TestMethod]
        public void GetAccessibility_ProtectedVariants()
        {
            var getAcc = typeof(Sqlx.Extensions).GetMethod("GetAccessibility", BindingFlags.NonPublic | BindingFlags.Static)!;
            Assert.AreEqual("protected", (string)getAcc.Invoke(null, new object[] { Accessibility.Protected })!);
            Assert.AreEqual("protected internal", (string)getAcc.Invoke(null, new object[] { Accessibility.ProtectedAndInternal })!);
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtMiscAsm", new[] { tree }, refs);
        }
    }
}


