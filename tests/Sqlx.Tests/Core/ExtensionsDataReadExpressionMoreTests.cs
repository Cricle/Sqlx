// -----------------------------------------------------------------------
// <copyright file="ExtensionsDataReadExpressionMoreTests.cs" company="Cricle">
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
    public class ExtensionsDataReadExpressionMoreTests
    {
        [TestMethod]
        public void GetDataReadExpression_TimeSpan_Guid_Paths()
        {
            var src = @"namespace N { public struct S{} }";
            var comp = CreateCompilation(src);
            var ts = (INamedTypeSymbol)comp.GetTypeByMetadataName("System.TimeSpan")!;
            var guid = (INamedTypeSymbol)comp.GetTypeByMetadataName("System.Guid")!;
            var nullableTs = comp.GetTypeByMetadataName("System.Nullable`1")!.Construct(ts);
            var nullableGuid = comp.GetTypeByMetadataName("System.Nullable`1")!.Construct(guid);

            var tsExpr = Sqlx.Extensions.GetDataReadExpression(ts, "r", "Col");
            var guidExpr = Sqlx.Extensions.GetDataReadExpression(guid, "r", "Col");
            var nTsExpr = Sqlx.Extensions.GetDataReadExpression(nullableTs, "r", "Col");
            var nGuidExpr = Sqlx.Extensions.GetDataReadExpression(nullableGuid, "r", "Col");

            Assert.IsTrue(tsExpr.Contains("GetTimeSpan(")); // direct path for non-nullable value types
            Assert.IsTrue(guidExpr.Contains("GetGuid("));
            Assert.IsTrue(nTsExpr.Contains("IsDBNull(") && nTsExpr.Contains("GetTimeSpan("));
            Assert.IsTrue(nGuidExpr.Contains("IsDBNull(") && nGuidExpr.Contains("GetGuid("));
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TimeSpan).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtMoreAsm", new[] { tree }, refs);
        }
    }
}


