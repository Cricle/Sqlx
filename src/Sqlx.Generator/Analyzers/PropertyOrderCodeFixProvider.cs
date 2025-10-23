// -----------------------------------------------------------------------
// <copyright file="PropertyOrderCodeFixProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Generator.Analyzers
{
    /// <summary>
    /// 代码修复提供器：自动修复属性顺序
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyOrderCodeFixProvider)), Shared]
    public class PropertyOrderCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(PropertyOrderAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // 查找类声明
            var classDeclaration = root.FindToken(diagnosticSpan.Start)
                .Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            if (classDeclaration == null)
                return;

            // 注册代码修复操作
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "将 Id 属性移至第一个位置",
                    createChangedDocument: c => MoveIdPropertyToFirstAsync(context.Document, classDeclaration, c),
                    equivalenceKey: nameof(PropertyOrderCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> MoveIdPropertyToFirstAsync(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document;

            // 查找 Id 属性
            var idProperty = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text.Equals("Id", System.StringComparison.OrdinalIgnoreCase));

            if (idProperty == null)
                return document;

            // 获取所有属性
            var properties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .ToList();

            // 如果 Id 已经是第一个，不需要修改
            if (properties.IndexOf(idProperty) == 0)
                return document;

            // 移除 Id 属性
            var otherProperties = properties.Where(p => p != idProperty).ToList();

            // 创建新的成员列表：Id 属性在前，其他属性保持原顺序
            var newMembers = classDeclaration.Members
                .Where(m => !(m is PropertyDeclarationSyntax))
                .ToList();

            // 添加 Id 属性（保留原有的注释和特性）
            newMembers.Insert(0, idProperty.WithLeadingTrivia(SyntaxFactory.TriviaList(
                SyntaxFactory.Comment("// 主键属性（必须在第一位以匹配 SQL 列顺序）"),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.CarriageReturnLineFeed)));

            // 添加其他属性
            foreach (var prop in otherProperties)
            {
                newMembers.Add(prop);
            }

            // 创建新的类声明
            var newClassDeclaration = classDeclaration.WithMembers(SyntaxFactory.List(newMembers));

            // 替换语法树
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}

