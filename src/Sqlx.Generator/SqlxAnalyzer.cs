// <copyright file="SqlxAnalyzer.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

/// <summary>
/// Analyzer for Sqlx best practices and error detection.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlxAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// SQLX001: Entity class should have [Sqlx] attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingEntityAttribute = new(
        id: "SQLX001",
        title: "Missing [Sqlx] attribute",
        messageFormat: "Entity type '{0}' used in repository should have [Sqlx] attribute for optimal performance",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Adding [Sqlx] generates EntityProvider, ResultReader, and ParameterBinder. These provide AOT-compatible, reflection-free implementations.");

    /// <summary>
    /// SQLX003: Consider adding [Column] attribute for non-standard column name.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingColumnAttribute = new(
        id: "SQLX003",
        title: "Consider adding [Column] attribute",
        messageFormat: "Property '{0}' will be mapped to column '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The [Column] attribute allows customizing the database column name mapping.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingEntityAttribute, MissingColumnAttribute);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeRepositoryForAttribute, SyntaxKind.Attribute);
    }

    private void AnalyzeRepositoryForAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        var name = attribute.Name.ToString();

        if (name is not ("RepositoryFor" or "RepositoryForAttribute"))
            return;

        // Get the service type from the attribute
        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = attribute.ArgumentList.Arguments[0];
            if (firstArg.Expression is TypeOfExpressionSyntax typeOfExpr)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(typeOfExpr.Type);
                if (typeInfo.Type is INamedTypeSymbol serviceType)
                {
                    CheckEntityTypeForAttributes(context, serviceType, attribute.GetLocation());
                }
            }
        }
    }

    private void CheckEntityTypeForAttributes(SyntaxNodeAnalysisContext context, INamedTypeSymbol serviceType, Location location)
    {
        var entityType = GetEntityType(serviceType);
        if (entityType is null)
            return;

        var sqlxAttr = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");

        var hasAttr = sqlxAttr is not null &&
            entityType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlxAttr));

        if (!hasAttr)
        {
            var diagnostic = Diagnostic.Create(
                MissingEntityAttribute,
                location,
                entityType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static INamedTypeSymbol? GetEntityType(INamedTypeSymbol serviceType)
    {
        foreach (var iface in serviceType.AllInterfaces.Concat(new[] { serviceType }))
        {
            if (iface.IsGenericType && iface.TypeArguments.Length >= 1)
            {
                var ifaceName = iface.OriginalDefinition.ToDisplayString();
                if (ifaceName.Contains("ICrudRepository") || ifaceName.Contains("IQueryRepository") ||
                    ifaceName.Contains("ICommandRepository") || ifaceName.Contains("IRepository"))
                {
                    return iface.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }
        return null;
    }
}
