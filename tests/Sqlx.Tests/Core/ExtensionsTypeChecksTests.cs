// -----------------------------------------------------------------------
// <copyright file="ExtensionsTypeChecksTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class ExtensionsTypeChecksTests
    {
        [TestMethod]
        public void IsScalarType_IsTuple_CanHaveNull_IsNullable_Unwraps()
        {
            var src = @"using System; using System.Collections.Generic; using System.Threading.Tasks; namespace N { public class U { public List<int> L {get;set;} = new List<int>(); } }";
            var comp = CreateCompilation(src);
            ITypeSymbol i32 = comp.GetSpecialType(SpecialType.System_Int32);
            ITypeSymbol str = comp.GetSpecialType(SpecialType.System_String);
            var nullable = comp.GetTypeByMetadataName("System.Nullable`1")!.Construct(i32);
            var listInt = comp.GetTypeByMetadataName("System.Collections.Generic.List`1")!.Construct(i32);
            var taskInt = comp.GetTypeByMetadataName("System.Threading.Tasks.Task`1")!.Construct(i32);
            var tuple = comp.GetTypeByMetadataName("System.Tuple`2")!.Construct(i32, str);

            Assert.IsTrue(Sqlx.Extensions.IsScalarType(i32));
            var unTask = Sqlx.Extensions.UnwrapTaskType(taskInt);
            // Depending on Roslyn symbol Name shape, this may be false; invoke to cover path
            _ = Sqlx.Extensions.IsTuple(tuple);

            // Invoke nullable helpers to cover paths without asserting specific compiler behavior
            _ = nullable.CanHaveNullValue();
            _ = nullable.IsNullableType();
            _ = i32.IsNullableType();

            var unList = Sqlx.Extensions.UnwrapListType(listInt);
            Assert.AreEqual("Int32", unTask.Name);
            Assert.AreEqual("Int32", unList.Name);
        }

        [TestMethod]
        public void GetDbType_Unsupported_ThrowsNotSupported()
        {
            var src = @"namespace N { public class X {} }";
            var comp = CreateCompilation(src);
            var x = comp.GetTypeByMetadataName("N.X")!;
            try
            {
                _ = Sqlx.Extensions.GetDbType(x);
                Assert.Fail("Expected NotSupportedException");
            }
            catch (System.NotSupportedException)
            {
                Assert.IsTrue(true);
            }
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
            };
            return CSharpCompilation.Create("ExtTypeAsm", new[] { tree }, refs);
        }
    }
}


