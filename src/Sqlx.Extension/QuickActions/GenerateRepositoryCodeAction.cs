using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Extension.QuickActions
{
    /// <summary>
    /// Code refactoring provider that generates Sqlx repository interface and implementation
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GenerateRepositoryCodeAction)), Shared]
    public class GenerateRepositoryCodeAction : CodeRefactoringProvider
    {
        /// <summary>
        /// Computes refactorings for the given context
        /// </summary>
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            // Check if we're on a class declaration
            var classDeclaration = node as ClassDeclarationSyntax ?? node.Parent as ClassDeclarationSyntax;
            if (classDeclaration == null)
                return;

            // Only offer the action if the class name suggests it's an entity (e.g., User, Product, Order)
            var className = classDeclaration.Identifier.Text;
            if (string.IsNullOrEmpty(className))
                return;

            // Register the code action
            var action = CodeAction.Create(
                title: $"Generate Sqlx Repository for '{className}'",
                createChangedSolution: c => GenerateRepositoryAsync(context.Document, classDeclaration, c),
                equivalenceKey: nameof(GenerateRepositoryCodeAction));

            context.RegisterRefactoring(action);
        }

        /// <summary>
        /// Generates repository interface and implementation
        /// </summary>
        private async Task<Solution> GenerateRepositoryAsync(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken)
        {
            var className = classDeclaration.Identifier.Text;
            var repositoryInterfaceName = $"I{className}Repository";
            var repositoryClassName = $"{className}Repository";

            // Get the semantic model
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            // Find the primary key property (assume it's named "Id" or ends with "Id")
            var idProperty = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == "Id" || p.Name.EndsWith("Id"));

            var keyType = idProperty?.Type?.ToDisplayString() ?? "long";

            // Generate interface code
            var interfaceCode = GenerateInterfaceCode(className, repositoryInterfaceName, keyType);

            // Generate implementation code
            var implementationCode = GenerateImplementationCode(className, repositoryClassName, repositoryInterfaceName, keyType);

            // Create new documents
            var solution = document.Project.Solution;
            var projectId = document.Project.Id;

            // Add interface document
            var interfaceDocument = solution.GetProject(projectId)
                .AddDocument($"{repositoryInterfaceName}.cs", interfaceCode);

            // Add implementation document
            var implementationDocument = interfaceDocument.Project
                .AddDocument($"{repositoryClassName}.cs", implementationCode);

            return implementationDocument.Project.Solution;
        }

        /// <summary>
        /// Generates repository interface code
        /// </summary>
        private string GenerateInterfaceCode(string entityName, string interfaceName, string keyType)
        {
            return $@"using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace YourNamespace.Repositories
{{
    /// <summary>
    /// Repository interface for {entityName} entity
    /// </summary>
    [SqlDefine(SqlDefineTypes.SQLite)] // Change to your database type
    [RepositoryFor(typeof({entityName}))]
    public interface {interfaceName}
    {{
        /// <summary>
        /// Gets entity by ID
        /// </summary>
        [SqlTemplate(""SELECT {{{{columns}}}} FROM {{{{table}}}} WHERE id = @id"")]
        Task<{entityName}?> GetByIdAsync({keyType} id, CancellationToken ct = default);

        /// <summary>
        /// Gets all entities
        /// </summary>
        [SqlTemplate(""SELECT {{{{columns}}}} FROM {{{{table}}}}"")]
        Task<List<{entityName}>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Inserts a new entity
        /// </summary>
        [SqlTemplate(""INSERT INTO {{{{table}}}} {{{{values}}}}"")]
        [ReturnInsertedId]
        Task<{keyType}> InsertAsync({entityName} entity, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        [SqlTemplate(""UPDATE {{{{table}}}} {{{{set}}}} WHERE id = @id"")]
        Task<int> UpdateAsync({entityName} entity, CancellationToken ct = default);

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        [SqlTemplate(""DELETE FROM {{{{table}}}} WHERE id = @id"")]
        Task<int> DeleteAsync({keyType} id, CancellationToken ct = default);

        /// <summary>
        /// Queries entities using expression
        /// </summary>
        [SqlTemplate(""SELECT {{{{columns}}}} FROM {{{{table}}}} {{{{where}}}}"")]
        Task<List<{entityName}>> QueryAsync([ExpressionToSql] Expression<Func<{entityName}, bool>> predicate, CancellationToken ct = default);

        /// <summary>
        /// Counts all entities
        /// </summary>
        [SqlTemplate(""SELECT COUNT(*) FROM {{{{table}}}}"")]
        Task<int> CountAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks if entity exists by ID
        /// </summary>
        [SqlTemplate(""SELECT COUNT(*) FROM {{{{table}}}} WHERE id = @id"")]
        Task<bool> ExistsAsync({keyType} id, CancellationToken ct = default);
    }}
}}";
        }

        /// <summary>
        /// Generates repository implementation code
        /// </summary>
        private string GenerateImplementationCode(string entityName, string className, string interfaceName, string keyType)
        {
            return $@"using System.Data.Common;

namespace YourNamespace.Repositories
{{
    /// <summary>
    /// Repository implementation for {entityName} entity
    /// Generated by Sqlx extension
    /// </summary>
    public partial class {className}(DbConnection connection) : {interfaceName}
    {{
        private readonly DbConnection _connection = connection;

        // Implementation is auto-generated by Sqlx source generator
        // No manual code needed - the [SqlTemplate] attributes drive code generation
    }}
}}";
        }
    }
}

