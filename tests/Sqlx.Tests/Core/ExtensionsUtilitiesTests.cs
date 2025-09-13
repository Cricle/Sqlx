// -----------------------------------------------------------------------
// <copyright file="ExtensionsUtilitiesTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class ExtensionsUtilitiesTests
    {
        [TestMethod]
        public void GetDbType_And_GetDataReaderMethod_CoverCommonTypes()
        {
            var source = @"namespace TNS {
                public enum E1 : int { A=0 }
                public class C1 { public int Id {get;set;} public string? Name {get;set;} }
            }";
            var comp = CreateCompilation(source);

            ITypeSymbol intT = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol strT = comp.GetSpecialType(SpecialType.System_String);
            var enumT = (INamedTypeSymbol)comp.GetTypeByMetadataName("TNS.E1")!;
            var dtT = comp.GetSpecialType(SpecialType.System_DateTime);
            var dtoT = (INamedTypeSymbol)comp.GetTypeByMetadataName("System.DateTimeOffset")!;
            var tsT = (INamedTypeSymbol)comp.GetTypeByMetadataName("System.TimeSpan")!;

            Assert.IsTrue(Sqlx.Extensions.GetDbType(intT).Contains("Int32"));
            Assert.IsTrue(Sqlx.Extensions.GetDbType(strT).Contains("String"));

            Assert.AreEqual("GetInt32", Sqlx.Extensions.GetDataReaderMethod(intT));
            Assert.AreEqual("GetString", Sqlx.Extensions.GetDataReaderMethod(strT));
            Assert.AreEqual("GetInt32", Sqlx.Extensions.GetDataReaderMethod(enumT));
            Assert.AreEqual("GetDateTime", Sqlx.Extensions.GetDataReaderMethod(dtT));
            Assert.AreEqual("GetDateTimeOffset", Sqlx.Extensions.GetDataReaderMethod(dtoT));
            Assert.AreEqual("GetTimeSpan", Sqlx.Extensions.GetDataReaderMethod(tsT));
        }

        [TestMethod]
        public void GetDataReadExpression_NullableAndEnumBranches()
        {
            var source = @"namespace TNS { public enum E1:int { A=0 } }";
            var comp = CreateCompilation(source);
            ITypeSymbol intT = comp.GetSpecialType(SpecialType.System_Int32);
            var nullableIntT = comp.GetTypeByMetadataName("System.Nullable`1")!.Construct(intT);
            var enumT = (INamedTypeSymbol)comp.GetTypeByMetadataName("TNS.E1")!;
            var nullableEnumT = comp.GetTypeByMetadataName("System.Nullable`1")!.Construct(enumT);

            var expr1 = Sqlx.Extensions.GetDataReadExpression(intT, "r", "Id");
            var expr2 = Sqlx.Extensions.GetDataReadExpression(nullableIntT, "r", "Id");
            var expr3 = Sqlx.Extensions.GetDataReadExpression(enumT, "r", "Id");
            var expr4 = Sqlx.Extensions.GetDataReadExpression(nullableEnumT, "r", "Id");

            Assert.IsTrue(expr1.Contains("GetInt32("));
            Assert.IsTrue(expr2.Contains("IsDBNull("));
            Assert.IsTrue(expr3.Contains("(global::TNS.E1)"));
            Assert.IsTrue(expr4.Contains("IsDBNull("));
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DateTimeOffset).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtTestsAsm", new[] { tree }, refs);
        }
    }
}


