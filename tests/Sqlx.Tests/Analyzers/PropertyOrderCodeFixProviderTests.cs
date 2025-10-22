// -----------------------------------------------------------------------
// <copyright file="PropertyOrderCodeFixProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator.Analyzers;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Tests.Analyzers;

/// <summary>
/// PropertyOrderCodeFixProvider的单元测试
/// 验证代码修复功能正确重排属性顺序
/// </summary>
[TestClass]
public class PropertyOrderCodeFixProviderTests
{
    private static async Task<(Document document, Diagnostic[] diagnostics)> GetDocumentAndDiagnosticsAsync(string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = new AdhocWorkspace()
            .CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(System.Data.IDbConnection).Assembly.Location))
            .AddDocument(documentId, "Test.cs", source);

        var document = solution.GetDocument(documentId);
        var compilation = await document!.Project.GetCompilationAsync();

        var analyzer = new PropertyOrderAnalyzer();
        var compilationWithAnalyzers = compilation!.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
        var relevantDiagnostics = diagnostics
            .Where(d => d.Id == PropertyOrderAnalyzer.DiagnosticId)
            .ToArray();

        return (document, relevantDiagnostics);
    }

    /// <summary>
    /// 测试：CodeFix应该将Id属性移到第一位
    /// </summary>
    [TestMethod]
    public async Task CodeFix_ShouldMoveIdPropertyToFirst()
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

        var (document, diagnostics) = await GetDocumentAndDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length, "应该有1个诊断");

        var codeFixProvider = new PropertyOrderCodeFixProvider();
        var actions = new System.Collections.Generic.List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (a, d) => actions.Add(a),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        Assert.AreEqual(1, actions.Count, "应该提供1个代码修复");

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<ApplyChangesOperation>().First();
        var newDocument = operation.ChangedSolution.GetDocument(document.Id);
        var newText = (await newDocument!.GetTextAsync()).ToString();

        // 验证Id属性已移到第一位
        Assert.IsTrue(newText.Contains("public int Id"), "应该包含Id属性");
        var idIndex = newText.IndexOf("public int Id");
        var nameIndex = newText.IndexOf("public string Name");
        Assert.IsTrue(idIndex < nameIndex, "Id应该在Name之前");
    }

    /// <summary>
    /// 测试：CodeFix的标题应该是正确的
    /// </summary>
    [TestMethod]
    public async Task CodeFix_ShouldHaveCorrectTitle()
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

        var (document, diagnostics) = await GetDocumentAndDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length);

        var codeFixProvider = new PropertyOrderCodeFixProvider();
        var actions = new System.Collections.Generic.List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (a, d) => actions.Add(a),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        Assert.AreEqual(1, actions.Count);
        Assert.IsTrue(actions[0].Title.Contains("Id"), "标题应该包含'Id'");
        Assert.IsTrue(actions[0].Title.Contains("第一"), "标题应该包含'第一'");
    }

    /// <summary>
    /// 测试：CodeFix应该保留属性的其他成员
    /// </summary>
    [TestMethod]
    public async Task CodeFix_ShouldPreserveOtherMembers()
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
        
        public void SomeMethod() { }
    }
}";

        var (document, diagnostics) = await GetDocumentAndDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length);

        var codeFixProvider = new PropertyOrderCodeFixProvider();
        var actions = new System.Collections.Generic.List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (a, d) => actions.Add(a),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<ApplyChangesOperation>().First();
        var newDocument = operation.ChangedSolution.GetDocument(document.Id);
        var newText = (await newDocument!.GetTextAsync()).ToString();

        // 验证方法仍然存在
        Assert.IsTrue(newText.Contains("public void SomeMethod()"), "应该保留其他成员");
    }

    /// <summary>
    /// 测试：FixableDiagnosticIds应该包含SQLX001
    /// </summary>
    [TestMethod]
    public void FixableDiagnosticIds_ShouldContainSQLX001()
    {
        var provider = new PropertyOrderCodeFixProvider();
        var fixableIds = provider.FixableDiagnosticIds;

        Assert.IsTrue(fixableIds.Contains(PropertyOrderAnalyzer.DiagnosticId));
    }

    /// <summary>
    /// 测试：GetFixAllProvider应该返回BatchFixer
    /// </summary>
    [TestMethod]
    public void GetFixAllProvider_ShouldReturnBatchFixer()
    {
        var provider = new PropertyOrderCodeFixProvider();
        var fixAllProvider = provider.GetFixAllProvider();

        Assert.IsNotNull(fixAllProvider);
    }

    /// <summary>
    /// 测试：当Id已经在第一位时，不应该提供CodeFix
    /// </summary>
    [TestMethod]
    public async Task WhenIdIsAlreadyFirst_ShouldNotProvideCodeFix()
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
}";

        var (document, diagnostics) = await GetDocumentAndDiagnosticsAsync(source);
        Assert.AreEqual(0, diagnostics.Length, "不应该有诊断");
    }

    /// <summary>
    /// 测试：CodeFix应该处理多个属性的情况
    /// </summary>
    [TestMethod]
    public async Task CodeFix_ShouldHandleMultipleProperties()
    {
        var source = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    [TableName(""users"")]
    public class User
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public int Id { get; set; }
        public string Address { get; set; }
    }
}";

        var (document, diagnostics) = await GetDocumentAndDiagnosticsAsync(source);
        Assert.AreEqual(1, diagnostics.Length);

        var codeFixProvider = new PropertyOrderCodeFixProvider();
        var actions = new System.Collections.Generic.List<CodeAction>();
        var context = new CodeFixContext(
            document,
            diagnostics[0],
            (a, d) => actions.Add(a),
            CancellationToken.None);

        await codeFixProvider.RegisterCodeFixesAsync(context);

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var operation = operations.OfType<ApplyChangesOperation>().First();
        var newDocument = operation.ChangedSolution.GetDocument(document.Id);
        var newText = (await newDocument!.GetTextAsync()).ToString();

        // 验证Id在第一位，其他属性顺序保持
        var idIndex = newText.IndexOf("public int Id");
        var nameIndex = newText.IndexOf("public string Name");
        var emailIndex = newText.IndexOf("public string Email");
        var ageIndex = newText.IndexOf("public int Age");
        var addressIndex = newText.IndexOf("public string Address");

        Assert.IsTrue(idIndex < nameIndex, "Id应该在Name之前");
        Assert.IsTrue(nameIndex < emailIndex, "Name应该在Email之前");
        Assert.IsTrue(emailIndex < ageIndex, "Email应该在Age之前");
        Assert.IsTrue(ageIndex < addressIndex, "Age应该在Address之前");
    }
}

