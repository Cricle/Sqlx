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

namespace Sqlx.Extension.Diagnostics
{
    /// <summary>
    /// Code fix provider for SqlTemplate parameter issues
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SqlTemplateParameterCodeFixProvider)), Shared]
    public class SqlTemplateParameterCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                SqlTemplateParameterAnalyzer.MissingParameterDiagnosticId,
                SqlTemplateParameterAnalyzer.UnusedParameterDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var node = root.FindToken(diagnosticSpan.Start).Parent;

                if (diagnostic.Id == SqlTemplateParameterAnalyzer.MissingParameterDiagnosticId)
                {
                    RegisterAddParameterFix(context, diagnostic, node);
                }
                else if (diagnostic.Id == SqlTemplateParameterAnalyzer.UnusedParameterDiagnosticId)
                {
                    RegisterRemoveParameterFix(context, diagnostic, node);
                }
            }
        }

        private void RegisterAddParameterFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode node)
        {
            // Extract parameter name from diagnostic message
            var parameterName = ExtractParameterName(diagnostic.GetMessage());
            if (string.IsNullOrEmpty(parameterName))
                return;

            var action = CodeAction.Create(
                title: $"Add parameter '{parameterName}'",
                createChangedDocument: c => AddParameterAsync(context.Document, node, parameterName, c),
                equivalenceKey: $"AddParameter_{parameterName}");

            context.RegisterCodeFix(action, diagnostic);
        }

        private void RegisterRemoveParameterFix(CodeFixContext context, Diagnostic diagnostic, SyntaxNode node)
        {
            var action = CodeAction.Create(
                title: "Remove unused parameter",
                createChangedDocument: c => RemoveParameterAsync(context.Document, node, c),
                equivalenceKey: "RemoveParameter");

            context.RegisterCodeFix(action, diagnostic);
        }

        private async Task<Document> AddParameterAsync(
            Document document,
            SyntaxNode node,
            string parameterName,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            // Find the method declaration
            var methodDeclaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration == null)
                return document;

            // Create new parameter
            var newParameter = SyntaxFactory.Parameter(
                SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName("object")); // Default to object type

            // Add parameter to method
            var newParameterList = methodDeclaration.ParameterList.AddParameters(newParameter);
            var newMethod = methodDeclaration.WithParameterList(newParameterList);

            // Replace old method with new method
            var newRoot = root.ReplaceNode(methodDeclaration, newMethod);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveParameterAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            // Find the parameter
            var parameter = node.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
            if (parameter == null)
                return document;

            // Find the method
            var methodDeclaration = parameter.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration == null)
                return document;

            // Remove parameter
            var newParameterList = methodDeclaration.ParameterList.Parameters.Remove(parameter);
            var newMethod = methodDeclaration.WithParameterList(
                methodDeclaration.ParameterList.WithParameters(newParameterList));

            // Replace old method with new method
            var newRoot = root.ReplaceNode(methodDeclaration, newMethod);

            return document.WithSyntaxRoot(newRoot);
        }

        private string ExtractParameterName(string message)
        {
            // Extract parameter name from message like "SQL parameter '@paramName' is used..."
            var match = System.Text.RegularExpressions.Regex.Match(message, @"'@?([^']+)'");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

