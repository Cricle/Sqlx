using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Extension.QuickActions
{
    /// <summary>
    /// Code refactoring provider that adds CRUD methods to Sqlx repository interfaces
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddCrudMethodsCodeAction)), Shared]
    public class AddCrudMethodsCodeAction : CodeRefactoringProvider
    {
        /// <summary>
        /// Computes refactorings for the given context
        /// </summary>
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            // Check if we're on an interface declaration
            var interfaceDeclaration = node as InterfaceDeclarationSyntax ?? node.Parent as InterfaceDeclarationSyntax;
            if (interfaceDeclaration == null)
                return;

            var interfaceName = interfaceDeclaration.Identifier.Text;

            // Only offer for repository interfaces
            if (!interfaceName.EndsWith("Repository"))
                return;

            // Get semantic model
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration);

            // Check if interface has SqlDefine or RepositoryFor attribute
            var hasSqlxAttributes = interfaceSymbol?.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "SqlDefineAttribute" ||
                a.AttributeClass?.Name == "RepositoryForAttribute") ?? false;

            if (!hasSqlxAttributes)
                return;

            // Register different actions for different method types
            RegisterCrudActions(context, interfaceDeclaration);
        }

        private void RegisterCrudActions(CodeRefactoringContext context, InterfaceDeclarationSyntax interfaceDeclaration)
        {
            // Add GetById method
            var getByIdAction = CodeAction.Create(
                title: "Add GetById method",
                createChangedDocument: c => AddGetByIdMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddGetByIdMethod");
            context.RegisterRefactoring(getByIdAction);

            // Add GetAll method
            var getAllAction = CodeAction.Create(
                title: "Add GetAll method",
                createChangedDocument: c => AddGetAllMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddGetAllMethod");
            context.RegisterRefactoring(getAllAction);

            // Add Insert method
            var insertAction = CodeAction.Create(
                title: "Add Insert method",
                createChangedDocument: c => AddInsertMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddInsertMethod");
            context.RegisterRefactoring(insertAction);

            // Add Update method
            var updateAction = CodeAction.Create(
                title: "Add Update method",
                createChangedDocument: c => AddUpdateMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddUpdateMethod");
            context.RegisterRefactoring(updateAction);

            // Add Delete method
            var deleteAction = CodeAction.Create(
                title: "Add Delete method",
                createChangedDocument: c => AddDeleteMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddDeleteMethod");
            context.RegisterRefactoring(deleteAction);

            // Add Query method
            var queryAction = CodeAction.Create(
                title: "Add Query method (Expression)",
                createChangedDocument: c => AddQueryMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddQueryMethod");
            context.RegisterRefactoring(queryAction);

            // Add Count method
            var countAction = CodeAction.Create(
                title: "Add Count method",
                createChangedDocument: c => AddCountMethodAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddCountMethod");
            context.RegisterRefactoring(countAction);

            // Add all CRUD methods at once
            var addAllAction = CodeAction.Create(
                title: "Add all CRUD methods",
                createChangedDocument: c => AddAllCrudMethodsAsync(context.Document, interfaceDeclaration, c),
                equivalenceKey: "AddAllCrudMethods");
            context.RegisterRefactoring(addAllAction);
        }

        private async Task<Document> AddGetByIdMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateGetByIdMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddGetAllMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateGetAllMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddInsertMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateInsertMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddUpdateMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateUpdateMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddDeleteMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateDeleteMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddQueryMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateQueryMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddCountMethodAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var method = CreateCountMethod();
            return await AddMethodToInterfaceAsync(document, interfaceDecl, method, ct);
        }

        private async Task<Document> AddAllCrudMethodsAsync(Document document, InterfaceDeclarationSyntax interfaceDecl, CancellationToken ct)
        {
            var methods = new[]
            {
                CreateGetByIdMethod(),
                CreateGetAllMethod(),
                CreateInsertMethod(),
                CreateUpdateMethod(),
                CreateDeleteMethod(),
                CreateQueryMethod(),
                CreateCountMethod()
            };

            var root = await document.GetSyntaxRootAsync(ct);
            var newInterface = interfaceDecl.AddMembers(methods);
            var newRoot = root.ReplaceNode(interfaceDecl, newInterface);
            newRoot = Formatter.Format(newRoot, document.Project.Solution.Workspace);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddMethodToInterfaceAsync(
            Document document,
            InterfaceDeclarationSyntax interfaceDecl,
            MethodDeclarationSyntax method,
            CancellationToken ct)
        {
            var root = await document.GetSyntaxRootAsync(ct);
            var newInterface = interfaceDecl.AddMembers(method);
            var newRoot = root.ReplaceNode(interfaceDecl, newInterface);
            newRoot = Formatter.Format(newRoot, document.Project.Solution.Workspace);

            return document.WithSyntaxRoot(newRoot);
        }

        // Method creation helpers
        private MethodDeclarationSyntax CreateGetByIdMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Gets entity by ID
        /// </summary>
        [SqlTemplate(""SELECT {{columns}} FROM {{table}} WHERE id = @id"")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }

        private MethodDeclarationSyntax CreateGetAllMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Gets all entities
        /// </summary>
        [SqlTemplate(""SELECT {{columns}} FROM {{table}}"")]
        Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }

        private MethodDeclarationSyntax CreateInsertMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Inserts a new entity and returns the inserted ID
        /// </summary>
        [SqlTemplate(""INSERT INTO {{table}} {{values}}"")]
        [ReturnInsertedId]
        Task<TKey> InsertAsync(TEntity entity, CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }

        private MethodDeclarationSyntax CreateUpdateMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        [SqlTemplate(""UPDATE {{table}} {{set}} WHERE id = @id"")]
        Task<int> UpdateAsync(TEntity entity, CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }

        private MethodDeclarationSyntax CreateDeleteMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
        Task<int> DeleteAsync(TKey id, CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }

        private MethodDeclarationSyntax CreateQueryMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Queries entities using expression
        /// </summary>
        [SqlTemplate(""SELECT {{columns}} FROM {{table}} {{where}}"")]
        Task<List<TEntity>> QueryAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }

        private MethodDeclarationSyntax CreateCountMethod()
        {
            return SyntaxFactory.ParseMemberDeclaration(@"
        /// <summary>
        /// Counts all entities
        /// </summary>
        [SqlTemplate(""SELECT COUNT(*) FROM {{table}}"")]
        Task<int> CountAsync(CancellationToken ct = default);
    ") as MethodDeclarationSyntax;
        }
    }
}

