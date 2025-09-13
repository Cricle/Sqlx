// -----------------------------------------------------------------------
// <copyright file="SqlOperationInferrerMoreTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlOperationInferrerMoreTests
{
    [TestMethod]
    public void GenerateSqlTemplate_Insert_Update_Handle_EntityNull_And_NoProperties()
    {
        var comp = CreateCompilation(@"namespace N { public class E { public int Id {get;set;} } public class Z { public int Id {get;set;} public string Name {get;set;} = string.Empty; } }");
        var e = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.E")!;
        var z = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.Z")!;

        var insertNull = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Insert, "t", null);
        Assert.IsTrue(insertNull.Contains("INSERT INTO [t]"));

        var insertNoProps = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Insert, "t", e);
        Assert.IsTrue(insertNoProps.Contains("DEFAULT VALUES") || insertNoProps.Contains("VALUES"));

        var insertWithProps = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Insert, "t", z);
        Assert.IsTrue(insertWithProps.Contains("[Name]"));
        Assert.IsTrue(insertWithProps.Contains("@name"));

        var updateNull = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Update, "t", null);
        Assert.IsTrue(updateNull.Contains("UPDATE [t] SET"));

        var updateNoProps = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Update, "t", e);
        Assert.IsTrue(updateNoProps.Contains("UPDATE [t] SET"));

        var updateWithProps = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Update, "t", z);
        Assert.IsTrue(updateWithProps.Contains("[Name] = @name"));
    }

    [TestMethod]
    public void GenerateSqlTemplate_Delete_And_Scalar_Basic()
    {
        var comp = CreateCompilation(@"namespace N { public class U { } }");
        var t = (INamedTypeSymbol)comp.GetTypeByMetadataName("N.U")!;

        var del = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Delete, "users", t);
        Assert.IsTrue(del.Contains("DELETE FROM [users]"));
        Assert.IsTrue(del.Contains("WHERE [Id] = @id"));

        var scalar = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Scalar, "users", t);
        Assert.IsTrue(scalar.Contains("SELECT COUNT(*) FROM [users]"));
    }

    private static CSharpCompilation CreateCompilation(string src)
    {
        var tree = CSharpSyntaxTree.ParseText(src);
        return CSharpCompilation.Create("InfMoreAsm", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
    }
}


