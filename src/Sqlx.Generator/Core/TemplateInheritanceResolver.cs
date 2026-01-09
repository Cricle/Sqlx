// -----------------------------------------------------------------------
// <copyright file="TemplateInheritanceResolver.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// Resolves SQL template inheritance from base interfaces.
/// Supports placeholder replacement based on dialect.
/// </summary>
internal class TemplateInheritanceResolver
{
    /// <summary>
    /// Resolves inherited SQL templates from base interfaces.
    /// </summary>
    /// <param name="interfaceSymbol">The interface to scan for inherited templates.</param>
    /// <param name="dialectProvider">The dialect provider for placeholder replacement.</param>
    /// <param name="tableName">The table name to use for {{table}} placeholder.</param>
    /// <param name="entityType">The entity type to extract column information.</param>
    /// <returns>A list of resolved method templates.</returns>
    public List<MethodTemplate> ResolveInheritedTemplates(
        INamedTypeSymbol interfaceSymbol,
        IDatabaseDialectProvider dialectProvider,
        string? tableName,
        INamedTypeSymbol? entityType)
    {
        var templates = new List<MethodTemplate>();
        var visited = new HashSet<string>();

        // Recursively collect templates from this interface and all base interfaces
        CollectTemplatesRecursive(interfaceSymbol, dialectProvider, tableName, entityType, templates, visited);

        return templates;
    }

    private void CollectTemplatesRecursive(
        INamedTypeSymbol interfaceSymbol,
        IDatabaseDialectProvider dialectProvider,
        string? tableName,
        INamedTypeSymbol? entityType,
        List<MethodTemplate> templates,
        HashSet<string> visited)
    {
        // Avoid processing the same interface twice
        var interfaceName = interfaceSymbol.ToDisplayString();
        if (!visited.Add(interfaceName))
            return;

        // Process all methods in the current interface
        foreach (var member in interfaceSymbol.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                var template = ExtractMethodTemplate(method, dialectProvider, tableName, entityType);
                if (template != null)
                {
                    templates.Add(template);
                }
            }
        }

        // Recursively process base interfaces
        foreach (var baseInterface in interfaceSymbol.Interfaces)
        {
            CollectTemplatesRecursive(baseInterface, dialectProvider, tableName, entityType, templates, visited);
        }
    }

    private MethodTemplate? ExtractMethodTemplate(
        IMethodSymbol method,
        IDatabaseDialectProvider dialectProvider,
        string? tableName,
        INamedTypeSymbol? entityType)
    {
        // Look for SqlTemplate or Sqlx attributes
        var sqlTemplateAttr = method.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlTemplateAttribute" ||
                                   attr.AttributeClass?.Name == "SqlxAttribute");

        if (sqlTemplateAttr == null)
            return null;

        // Extract SQL template string
        var sqlTemplate = sqlTemplateAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
        if (string.IsNullOrEmpty(sqlTemplate))
            return null;

        // Check if template contains any placeholders (static or dynamic)
        // Dynamic placeholders like {{column}}, {{where}}, {{orderby}} will be processed by SqlTemplateEngine
        var containsAnyPlaceholder = sqlTemplate.Contains("{{");
        
        if (!containsAnyPlaceholder)
        {
            // No placeholders at all, return as-is
            return new MethodTemplate
            {
                Method = method,
                OriginalSql = sqlTemplate,
                ProcessedSql = sqlTemplate,
                ContainsPlaceholders = false
            };
        }

        // Check if template contains only static placeholders that can be replaced now
        // Dynamic placeholders ({{column}}, {{where}}, {{orderby}}, {{set}}, {{values}}, {{limit}}, {{offset}})
        // should NOT be replaced here - they will be processed by SqlTemplateEngine
        var containsStaticPlaceholders = DialectPlaceholders.ContainsPlaceholders(sqlTemplate);
        
        // Extract column names from entity type
        string[]? columns = null;
        if (entityType != null)
        {
            columns = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                .Select(p => p.Name)
                .ToArray();
        }

        // Replace only static placeholders ({{table}}, {{columns}}, {{returning_id}}, etc.)
        // Dynamic placeholders will be handled by SqlTemplateEngine later
        var processedSql = containsStaticPlaceholders 
            ? dialectProvider.ReplacePlaceholders(sqlTemplate, tableName, columns)
            : sqlTemplate;

        return new MethodTemplate
        {
            Method = method,
            OriginalSql = sqlTemplate,
            ProcessedSql = processedSql,
            ContainsPlaceholders = true
        };
    }
}

/// <summary>
/// Represents a method with its SQL template.
/// </summary>
internal class MethodTemplate
{
    /// <summary>
    /// The method symbol.
    /// </summary>
    public IMethodSymbol Method { get; set; } = null!;

    /// <summary>
    /// The original SQL template with placeholders.
    /// </summary>
    public string OriginalSql { get; set; } = string.Empty;

    /// <summary>
    /// The processed SQL with placeholders replaced.
    /// </summary>
    public string ProcessedSql { get; set; } = string.Empty;

    /// <summary>
    /// Whether the template contains dialect placeholders.
    /// </summary>
    public bool ContainsPlaceholders { get; set; }
}

