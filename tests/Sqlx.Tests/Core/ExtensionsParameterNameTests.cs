// -----------------------------------------------------------------------
// <copyright file="ExtensionsParameterNameTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class ExtensionsParameterNameTests
    {
        [TestMethod]
        public void GetParameterName_ForProperty_UsesSnakeCaseWithPrefix()
        {
            var src = @"namespace N { public class U { public int UserName {get;set;} } }";
            var comp = CreateCompilation(src);
            var u = comp.GetTypeByMetadataName("N.U")!;
            var prop = u.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "UserName");
            var getParam = typeof(Sqlx.Extensions).GetMethod("GetParameterName", BindingFlags.NonPublic | BindingFlags.Static, null, new System.Type[] { typeof(ISymbol), typeof(string) }, null)!;
            var paramName = (string)getParam.Invoke(null, new object[] { prop, "@" })!;
            Assert.AreEqual("@user_name", paramName);
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtParamAsm", new[] { tree }, refs);
        }
    }
}


