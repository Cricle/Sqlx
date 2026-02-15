// <copyright file="ContextGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

/// <summary>
/// Source generator that creates SqlxContext implementations for classes marked with [SqlxContext].
/// </summary>
/// <remarks>
/// <para>
/// This generator produces complete context implementations including:
/// </para>
/// <list type="bullet">
/// <item><description>Repository properties with lazy initialization</description></item>
/// <item><description>Transaction propagation logic</description></item>
/// <item><description>Transaction cleanup logic</description></item>
/// <item><description>DI-friendly constructor (if not provided by user)</description></item>
/// </list>
/// <para>
/// Required attributes on the context class:
/// </para>
/// <list type="bullet">
/// <item><description>[SqlxContext] - Marks the class for generation</description></item>
/// <item><description>[IncludeRepository(typeof(RepositoryType))] - Specifies repositories to include</description></item>
/// <item><description>[SqlDefine(SqlDefineTypes.XXX)] - Specifies the database dialect (optional)</description></item>
/// </list>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class ContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetTarget(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static ClassDeclarationSyntax? GetTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        foreach (var attrList in classDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name is "SqlxContext" or "SqlxContextAttribute")
                    return classDecl;
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty) return;

        var sqlxContextAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxContextAttribute");
        var includeRepositoryAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.IncludeRepositoryAttribute");
        var repositoryForAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryForAttribute");
        var sqlDefineAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlDefineAttribute");

        if (sqlxContextAttr is null || includeRepositoryAttr is null) return;

        foreach (var classDecl in classes.Distinct())
        {
            if (classDecl is null) continue;

            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol) continue;

            var contextAttrData = typeSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlxContextAttr));
            if (contextAttrData is null) continue;

            // Get all [IncludeRepository] attributes
            var includeRepoAttrs = typeSymbol.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, includeRepositoryAttr))
                .ToList();

            if (includeRepoAttrs.Count == 0) continue;

            // Extract repository information
            var repositories = new List<RepositoryInfo>();
            foreach (var includeRepoAttr in includeRepoAttrs)
            {
                var repositoryType = includeRepoAttr.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
                if (repositoryType is null) continue;

                // Find the [RepositoryFor] attribute on the repository type
                var repoForAttrData = repositoryType.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, repositoryForAttr));
                if (repoForAttrData is null) continue;

                var serviceType = repoForAttrData.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
                if (serviceType is null) continue;

                // Extract entity type from ICrudRepository<TEntity, TKey>
                var entityType = GetEntityType(serviceType);
                if (entityType is null) continue;

                var entityName = entityType.Name;
                var propertyName = Pluralize(entityName);
                var paramName = ToCamelCase(propertyName);
                var fieldName = $"_{paramName}";

                repositories.Add(new RepositoryInfo
                {
                    RepositoryType = repositoryType.ToDisplayString(),
                    RepositoryTypeName = repositoryType.Name,
                    EntityType = entityType.ToDisplayString(),
                    EntityName = entityName,
                    PropertyName = propertyName,
                    ParamName = paramName,
                    FieldName = fieldName
                });
            }

            if (repositories.Count == 0) continue;

            // Get dialect from [SqlDefine] attribute
            var dialect = GetSqlDefine(typeSymbol, sqlDefineAttr);

            // Check if user has provided a constructor
            var hasUserConstructor = HasUserProvidedConstructor(classDecl);

            var source = GenerateSource(typeSymbol, repositories, dialect, hasUserConstructor);
            context.AddSource($"{typeSymbol.Name}.Context.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static INamedTypeSymbol? GetEntityType(INamedTypeSymbol serviceType)
    {
        // Look for ICrudRepository<TEntity, TKey> in the interface hierarchy
        var allInterfaces = serviceType.AllInterfaces.Concat(new[] { serviceType });
        foreach (var iface in allInterfaces)
        {
            if (iface.Name == "ICrudRepository" && iface.TypeArguments.Length == 2)
            {
                return iface.TypeArguments[0] as INamedTypeSymbol;
            }
        }
        return null;
    }

    private static string GetSqlDefine(INamedTypeSymbol typeSymbol, INamedTypeSymbol? sqlDefineAttr)
    {
        if (sqlDefineAttr is null) return "SQLite";
        var attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlDefineAttr));
        if (attr?.ConstructorArguments.Length > 0)
        {
            var value = attr.ConstructorArguments[0].Value;
            return value is int intValue ? MapDialectEnum(intValue) ?? "SQLite" : value?.ToString() ?? "SQLite";
        }
        return "SQLite";
    }

    private static string? MapDialectEnum(int dialectValue) => dialectValue switch
    {
        0 => "MySql",
        1 => "SqlServer",
        2 => "PostgreSql",
        3 => "Oracle",
        4 => "DB2",
        5 => "SQLite",
        _ => null
    };

    private static bool HasUserProvidedConstructor(ClassDeclarationSyntax classDecl)
    {
        // Check if the class has any constructor declarations
        return classDecl.Members.OfType<ConstructorDeclarationSyntax>().Any();
    }

    private static string Pluralize(string name)
    {
        // Simple pluralization rules
        if (name.EndsWith("y") && name.Length > 1 && !IsVowel(name[name.Length - 2]))
        {
            return name.Substring(0, name.Length - 1) + "ies";
        }
        if (name.EndsWith("s") || name.EndsWith("x") || name.EndsWith("z") || 
            name.EndsWith("ch") || name.EndsWith("sh"))
        {
            return name + "es";
        }
        return name + "s";
    }

    private static bool IsVowel(char c)
    {
        return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' ||
               c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string GenerateSource(INamedTypeSymbol typeSymbol, List<RepositoryInfo> repositories, string dialect, bool hasUserConstructor)
    {
        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
        var className = typeSymbol.Name;
        var builder = new IndentedStringBuilder(null);

        // File header
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System.Data.Common;");
        builder.AppendLine("using Sqlx;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine();

        // Namespace
        builder.AppendLine($"namespace {namespaceName}");
        builder.AppendLine("{");
        builder.PushIndent();

        // Class declaration
        builder.AppendLine($"partial class {className}");
        builder.AppendLine("{");
        builder.PushIndent();

        // Generate backing fields (nullable for lazy resolution)
        foreach (var repo in repositories)
        {
            builder.AppendLine($"private {repo.RepositoryType}? {repo.FieldName};");
        }
        builder.AppendLine();
        
        // Store service provider for lazy resolution
        builder.AppendLine("private readonly System.IServiceProvider _serviceProvider;");
        builder.AppendLine();

        // Generate constructor if user hasn't provided one
        // Generate a ServiceProvider-based constructor for lazy resolution
        if (!hasUserConstructor)
        {
            builder.AppendLine($"public {className}(DbConnection connection, SqlxContextOptions? options, System.IServiceProvider serviceProvider)");
            builder.PushIndent();
            builder.AppendLine(": base(connection, options, ownsConnection: false)");
            builder.PopIndent();
            builder.AppendLine("{");
            builder.PushIndent();
            builder.AppendLine("_serviceProvider = serviceProvider;");
            builder.PopIndent();
            builder.AppendLine("}");
            builder.AppendLine();
        }

        // Generate repository properties with lazy initialization
        foreach (var repo in repositories)
        {
            builder.AppendLine($"public {repo.RepositoryType} {repo.PropertyName}");
            builder.AppendLine("{");
            builder.PushIndent();
            builder.AppendLine("get");
            builder.AppendLine("{");
            builder.PushIndent();
            builder.AppendLine($"if ({repo.FieldName} == null)");
            builder.AppendLine("{");
            builder.PushIndent();
            builder.AppendLine($"{repo.FieldName} = _serviceProvider.GetRequiredService<{repo.RepositoryType}>();");
            builder.AppendLine($"((ISqlxRepository){repo.FieldName}).Connection = Connection;");
            builder.AppendLine($"{repo.FieldName}.Transaction = Transaction;");
            builder.PopIndent();
            builder.AppendLine("}");
            builder.AppendLine($"return {repo.FieldName};");
            builder.PopIndent();
            builder.AppendLine("}");
            builder.PopIndent();
            builder.AppendLine("}");
            builder.AppendLine();
        }

        // Generate PropagateTransactionToRepositories override
        builder.AppendLine("protected override void PropagateTransactionToRepositories()");
        builder.AppendLine("{");
        builder.PushIndent();
        foreach (var repo in repositories)
        {
            builder.AppendLine($"if ({repo.FieldName} != null) {repo.FieldName}.Transaction = Transaction;");
        }
        builder.PopIndent();
        builder.AppendLine("}");
        builder.AppendLine();

        // Generate ClearRepositoryTransactions override
        builder.AppendLine("protected override void ClearRepositoryTransactions()");
        builder.AppendLine("{");
        builder.PushIndent();
        foreach (var repo in repositories)
        {
            builder.AppendLine($"if ({repo.FieldName} != null) {repo.FieldName}.Transaction = null;");
        }
        builder.PopIndent();
        builder.AppendLine("}");

        // Close class
        builder.PopIndent();
        builder.AppendLine("}");

        // Close namespace
        builder.PopIndent();
        builder.AppendLine("}");

        return builder.ToString();
    }

    private class RepositoryInfo
    {
        public string RepositoryType { get; set; } = string.Empty;
        public string RepositoryTypeName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string ParamName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
    }
}
