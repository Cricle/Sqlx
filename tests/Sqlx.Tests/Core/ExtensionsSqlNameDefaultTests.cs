// -----------------------------------------------------------------------
// <copyright file="ExtensionsSqlNameDefaultTests.cs" company="Cricle">
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
    public class ExtensionsSqlNameDefaultTests
    {
        [TestMethod]
        public void GetSqlName_WithoutAttribute_UsesNameMapperSnakeCase()
        {
            var src = @"namespace N { public class U { public int UserName {get;set;} } }";
            var comp = CreateCompilation(src);
            var u = comp.GetTypeByMetadataName("N.U")!;
            var prop = u.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "UserName");
            var name = (string)typeof(Sqlx.Extensions).GetMethod("GetSqlName", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { prop })!;
            Assert.AreEqual("user_name", name);
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtSqlNameAsm", new[] { tree }, refs);
        }
    }
}


