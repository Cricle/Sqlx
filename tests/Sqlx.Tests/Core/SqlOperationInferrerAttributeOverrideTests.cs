// -----------------------------------------------------------------------
// <copyright file="SqlOperationInferrerAttributeOverrideTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlOperationInferrerAttributeOverrideTests
{
    [TestMethod]
    public void Attribute_Overrides_MethodName_Inference()
    {
        var source = @"using System; using Sqlx.Annotations; namespace N {
public interface IRepo {
  [SqlExecuteType(2, ""users"")] int GetShouldBeInsert();
  [SqlExecuteType(1, ""users"")] int AddShouldBeUpdate();
}
}
namespace Sqlx.Annotations {
  [AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
  public sealed class SqlExecuteTypeAttribute : Attribute {
    public SqlExecuteTypeAttribute(int executeType, string tableName) {}
  }
}";

        var tree = CSharpSyntaxTree.ParseText(source);
        var comp = CSharpCompilation.Create("InfAttrAsm", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var repo = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.IRepo")!;
        var methods = repo.GetMembers().OfType<IMethodSymbol>().ToArray();
        var m1 = methods.First(m => m.Name == "GetShouldBeInsert");
        var m2 = methods.First(m => m.Name == "AddShouldBeUpdate");

        Assert.AreEqual(SqlOperationType.Insert, SqlOperationInferrer.InferOperation(m1));
        Assert.AreEqual(SqlOperationType.Update, SqlOperationInferrer.InferOperation(m2));
    }
}


