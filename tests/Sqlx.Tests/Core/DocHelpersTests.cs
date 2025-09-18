// -----------------------------------------------------------------------
// <copyright file="DocHelpersTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class DocHelpersTests
    {
        [TestMethod]
        public void GetParameterDescription_CoversToken_Entity_Primitive()
        {
            var source = @"using System.Threading; namespace NS { public class User { public int Id {get;set;} } public class Svc { public void M(User u, int id, CancellationToken cancellationToken){} } }";
            var comp = CreateCompilation(source);
            var svc = comp.GetTypeByMetadataName("NS.Svc")!;
            var method = svc.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "M");

            var descs = method.Parameters.Select(p => InvokePrivate("GetParameterDescription", p)).ToArray();

            Assert.IsTrue(descs[2].Contains("cancellation token"));
            Assert.IsTrue(descs[0].Contains("User entity") || descs[0].Contains("User"));
            Assert.IsTrue(descs[1].Contains("id"));
        }

        [TestMethod]
        public void GetReturnDescription_CoversTask_GenericTask_Int_Collection()
        {
            var source = @"using System.Collections.Generic; using System.Threading.Tasks; namespace NS { public class User{} public class Svc { public Task A() => Task.CompletedTask; public Task<int> B() => Task.FromResult(1); public Task<List<User>> C() => Task.FromResult(new List<User>()); public int D() => 1; public List<User> E() => new List<User>(); public Task<string> F() => Task.FromResult(""x""); } }";
            var comp = CreateCompilation(source);
            var svc = comp.GetTypeByMetadataName("NS.Svc")!;
            var methods = svc.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary).OrderBy(m => m.Name).ToList();

            var results = methods.Select(m => InvokePrivate("GetReturnDescription", m)).ToList();

            Assert.IsTrue(results.Any(s => s.Contains("asynchronous operation")));
            Assert.IsTrue(results.Any(s => s.Contains("number of affected rows") || s.Contains("result value")));
            Assert.IsTrue(results.Any(s => s.Contains("collection of User entities")));
            Assert.IsTrue(
                results.Any(s => s.Contains("The Int32 result") || s.Contains("The result value")) ||
                results.Any(s => s.Contains("A task containing the String result") || s.Contains("The String result")));
        }

        private static string InvokePrivate(string name, object arg)
        {
            var method = typeof(AbstractGenerator).GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var testGen = new TestGen();
            return (string)method!.Invoke(testGen, new object[] { arg })!;
        }

        private static CSharpCompilation CreateCompilation(string src)
        {
            var tree = CSharpSyntaxTree.ParseText(src);
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
            };
            return CSharpCompilation.Create("DocHlpAsm", new[] { tree }, refs);
        }

        private sealed class TestGen : AbstractGenerator
        {
            public override void Initialize(GeneratorInitializationContext context)
            {
            }
        }
    }
}


