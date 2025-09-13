// -----------------------------------------------------------------------
// <copyright file="ExtensionsInternalsTests.cs" company="Cricle">
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
    public class ExtensionsInternalsTests
    {
        [TestMethod]
        public void IsDbConnection_IsDbTransaction_IsDbContext_Work()
        {
            var source = @"using System.Data.Common; namespace NS { public class DbContext {} public class Svc { public void M(DbConnection c, DbTransaction t, DbContext ctx){} } }";
            var comp = CreateCompilation(source);
            var svc = comp.GetTypeByMetadataName("NS.Svc")!;
            var method = svc.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "M");
            var pConn = method.Parameters[0];
            var pTran = method.Parameters[1];
            var pCtx = method.Parameters[2];

            Assert.IsTrue(InvokeBool("IsDbConnection", pConn));
            Assert.IsTrue(InvokeBool("IsDbTransaction", pTran));
            Assert.IsTrue(InvokeBool("IsDbContext", pCtx));
        }

        [TestMethod]
        public void GetSqlName_UsesDbColumnAttribute_WhenPresent()
        {
            var source = @"namespace NS { public class DbColumnAttribute : System.Attribute { public DbColumnAttribute(string n){} } public class U { [DbColumn(""col_name"")] public int Id {get;set;} } }";
            var comp = CreateCompilation(source);
            var u = comp.GetTypeByMetadataName("NS.U")!;
            var prop = u.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "Id");
            var name = (string)typeof(Sqlx.Extensions).GetMethod("GetSqlName", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { prop })!;
            Assert.AreEqual("col_name", name);
        }

        [TestMethod]
        public void GetAccessibility_And_GetParameterName_Work()
        {
            var method = typeof(Sqlx.Extensions).GetMethod("GetAccessibility", BindingFlags.NonPublic | BindingFlags.Static)!;
            Assert.AreEqual("public", method.Invoke(null, new object[] { Accessibility.Public }));
            Assert.AreEqual("internal", method.Invoke(null, new object[] { Accessibility.Friend }));
            Assert.AreEqual("private", method.Invoke(null, new object[] { Accessibility.Private }));

            var comp = CreateCompilation("namespace N { public class C{} }");
            var type = comp.GetTypeByMetadataName("N.C")!;
            var getParamName = typeof(Sqlx.Extensions).GetMethod("GetParameterName", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(ITypeSymbol), typeof(string) }, null)!;
            var p = (string)getParamName.Invoke(null, new object[] { type, "_test_name" })!;
            Assert.AreEqual("@testname", p);
        }

        private static bool InvokeBool(string methodName, ISymbol symbol)
        {
            var m = typeof(Sqlx.Extensions).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
            return (bool)m.Invoke(null, new object[] { symbol })!;
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtIntAsm", new[] { tree }, refs);
        }
    }
}


