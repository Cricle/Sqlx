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
using System.Text.RegularExpressions;

/// <summary>
/// Analyzer for Sqlx best practices and error detection.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlxAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// SQLX001: Entity class should have [SqlxEntity] or [SqlxParameter] attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingEntityAttribute = new(
        id: "SQLX001",
        title: "Missing [SqlxEntity] or [SqlxParameter] attribute",
        messageFormat: "Entity type '{0}' used in repository should have [SqlxEntity] and/or [SqlxParameter] attribute for optimal performance",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Adding [SqlxEntity] generates EntityProvider and ResultReader. Adding [SqlxParameter] generates ParameterBinder. These provide AOT-compatible, reflection-free implementations.");

    /// <summary>
    /// SQLX002: Unknown placeholder in SqlTemplate.
    /// </summary>
    public static readonly DiagnosticDescriptor UnknownPlaceholder = new(
        id: "SQLX002",
        title: "Unknown placeholder",
        messageFormat: "Unknown placeholder '{0}' in SqlTemplate",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Only built-in placeholders are supported: columns, values, set, table, where, limit, offset.");

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

    private static readonly string[] KnownPlaceholders = { "columns", "values", "set", "table", "where", "limit", "offset" };
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)(?:\s+[^}]+)?\}\}", RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingEntityAttribute, UnknownPlaceholder, MissingColumnAttribute);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSqlTemplateAttribute, SyntaxKind.Attribute);
        context.RegisterSyntaxNodeAction(AnalyzeRepositoryForAttribute, SyntaxKind.Attribute);
    }

    private void AnalyzeSqlTemplateAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        var name = attribute.Name.ToString();

        if (name is not ("SqlTemplate" or "SqlTemplateAttribute"))
            return;

        // Get the template string from the attribute
        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = attribute.ArgumentList.Arguments[0];
            if (firstArg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var template = literal.Token.ValueText;
                CheckPlaceholders(context, template, literal.GetLocation());
            }
        }
    }

    private void CheckPlaceholders(SyntaxNodeAnalysisContext context, string template, Location location)
    {
        var matches = PlaceholderRegex.Matches(template);
        foreach (Match match in matches)
        {
            var placeholderName = match.Groups[1].Value.ToLowerInvariant();
            if (!KnownPlaceholders.Contains(placeholderName))
            {
                var diagnostic = Diagnostic.Create(
                    UnknownPlaceholder,
                    location,
                    placeholderName);
                context.ReportDiagnostic(diagnostic);
            }
        }
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

        var sqlxEntityAttr = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxEntityAttribute");
        var sqlxParamAttr = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxParameterAttribute");

        var hasEntityAttr = sqlxEntityAttr is not null &&
            entityType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlxEntityAttr));
        var hasParamAttr = sqlxParamAttr is not null &&
            entityType.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlxParamAttr));

        if (!hasEntityAttr && !hasParamAttr)
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
