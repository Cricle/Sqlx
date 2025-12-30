// -----------------------------------------------------------------------
// <copyright file="PropertyOrderAnalyzerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator.Analyzers;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.Analyzers;

/// <summary>
/// PropertyOrderAnalyzer的单元测试
/// 验证SQLX001诊断器正确检测属性顺序问题
/// </summary>
[TestClass]
public class PropertyOrderAnalyzerTests
{
    private static async Task<Diagnostic[]> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Data.IDbConnection).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new PropertyOrderAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            System.Collections.Immutable.ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == PropertyOrderAnalyzer.DiagnosticId).ToArray();
    }

    /// <summary>
    /// 测试：当Id属性在第一位时，不应该报告诊断
    /// </summary>
    [TestMethod]
    public async Task WhenIdPropertyIsFirst_ShouldNotReportDiagnostic()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(0, diagnostics.Length, "不应该报告诊断");
    }

    /// <summary>
    /// 测试：当Id属性不在第一位时，应该报告SQLX001诊断
    /// </summary>
    [TestMethod]
    public async Task WhenIdPropertyIsNotFirst_ShouldReportDiagnostic()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Email { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length, "应该报告1个诊断");
        Assert.AreEqual(PropertyOrderAnalyzer.DiagnosticId, diagnostics[0].Id);
    }

    /// <summary>
    /// 测试：没有TableName或RepositoryFor特性的类不应该报告诊断
    /// </summary>
    [TestMethod]
    public async Task WhenClassHasNoSqlxAttributes_ShouldNotReportDiagnostic()
    {
        var source = @"
namespace TestNamespace
{
    public class User
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(0, diagnostics.Length, "不应该报告诊断");
    }

    /// <summary>
    /// 测试：没有公共属性的类不应该报告诊断
    /// </summary>
    [TestMethod]
    public async Task WhenClassHasNoProperties_ShouldNotReportDiagnostic()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(0, diagnostics.Length, "不应该报告诊断");
    }

    /// <summary>
    /// 测试：只有静态属性的类不应该报告诊断
    /// </summary>
    [TestMethod]
    public async Task WhenClassHasOnlyStaticProperties_ShouldNotReportDiagnostic()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public static string Name { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(0, diagnostics.Length, "不应该报告诊断");
    }

    /// <summary>
    /// 测试：诊断的严重性应该是Warning
    /// </summary>
    [TestMethod]
    public async Task DiagnosticSeverity_ShouldBeWarning()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[0].Severity);
    }

    /// <summary>
    /// 测试：多个类时，每个类独立检查
    /// </summary>
    [TestMethod]
    public async Task WhenMultipleClasses_EachClassIsCheckedIndependently()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TableName(""orders"")]
    public class Order
    {
        public string OrderNumber { get; set; }
        public int Id { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length, "应该只报告Order类的诊断");
    }

    /// <summary>
    /// 测试：只有Id属性的类不应该报告诊断
    /// </summary>
    [TestMethod]
    public async Task WhenClassHasOnlyIdProperty_ShouldNotReportDiagnostic()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public int Id { get; set; }
    }
}";

        var diagnostics = await GetDiagnosticsAsync(source);
        Assert.AreEqual(0, diagnostics.Length, "不应该报告诊断");
    }

}

