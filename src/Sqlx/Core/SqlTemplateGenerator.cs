// -----------------------------------------------------------------------
// <copyright file="SqlTemplateGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// High-performance SQL template generator with caching and optimization.
/// </summary>
internal static class SqlTemplateGenerator
{
    /// <summary>
    /// Generates optimized SELECT template.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateSelect(string tableName, INamedTypeSymbol? entityType, string? whereClause = null)
    {
        return IntelligentCacheManager.GetOrAdd($"SELECT_{tableName}_{whereClause ?? "ALL"}", () =>
        {
            var sb = OptimizedStringBuilder.Create(256);
            
            if (entityType != null)
            {
                var columns = GetSelectableColumns(entityType);
                sb.Append("SELECT ").Append(columns).Append(" FROM ").Append(tableName);
            }
            else
            {
                sb.Append("SELECT * FROM ").Append(tableName);
            }
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                sb.Append(" WHERE ").Append(whereClause);
            }
            
            return sb.ToString();
        });
    }
    
    /// <summary>
    /// Generates optimized INSERT template.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateInsert(string tableName, INamedTypeSymbol? entityType)
    {
        if (entityType == null)
            return $"INSERT INTO {tableName} DEFAULT VALUES";
            
        return IntelligentCacheManager.GetOrAdd($"INSERT_{tableName}_{entityType.ToDisplayString()}", () =>
        {
            var properties = GetInsertableProperties(entityType);
            if (properties.Length == 0)
                return $"INSERT INTO {tableName} DEFAULT VALUES";
            
            var sb = OptimizedStringBuilder.Create(512);
            
            sb.Append("INSERT INTO ").Append(tableName).Append(" (");
            sb.AppendJoin(", ", properties.Select(p => p.Name).ToArray());
            sb.Append(") VALUES (");
            sb.AppendJoin(", ", properties.Select(p => "@" + p.Name.ToLowerInvariant()).ToArray());
            sb.Append(")");
            
            return sb.ToString();
        });
    }
    
    /// <summary>
    /// Generates optimized UPDATE template.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateUpdate(string tableName, INamedTypeSymbol? entityType, string? whereClause = null)
    {
        if (entityType == null)
            return $"UPDATE {tableName} SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = @id";
            
        var cacheKey = $"UPDATE_{tableName}_{whereClause ?? "ID"}";
        return IntelligentCacheManager.GetOrAdd(cacheKey, () =>
        {
            var properties = GetUpdatableProperties(entityType);
            if (properties.Length == 0)
                return $"UPDATE {tableName} SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = @id";
            
            var sb = OptimizedStringBuilder.Create(512);
            
            sb.Append("UPDATE ").Append(tableName).Append(" SET ");
            
            var setClauses = properties.Select(p => $"{p.Name} = @{p.Name.ToLowerInvariant()}").ToArray();
            sb.AppendJoin(", ", setClauses);
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                sb.Append(" WHERE ").Append(whereClause);
            }
            else
            {
                sb.Append(" WHERE Id = @id");
            }
            
            return sb.ToString();
        });
    }
    
    /// <summary>
    /// Generates optimized DELETE template.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateDelete(string tableName, string? whereClause = null)
    {
        return IntelligentCacheManager.GetOrAdd($"DELETE_{tableName}_{whereClause ?? "ID"}", () =>
        {
            var sb = OptimizedStringBuilder.Create(128);
            
            sb.Append("DELETE FROM ").Append(tableName);
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                sb.Append(" WHERE ").Append(whereClause);
            }
            else
            {
                sb.Append(" WHERE Id = @id");
            }
            
            return sb.ToString();
        });
    }
    
    /// <summary>
    /// Generates batch INSERT template for multiple entities.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GenerateBatchInsert(string tableName, INamedTypeSymbol? entityType, int batchSize = 100)
    {
        if (entityType == null)
            return $"INSERT INTO {tableName} DEFAULT VALUES";
            
        return IntelligentCacheManager.GetOrAdd($"BATCH_INSERT_{tableName}_{entityType.ToDisplayString()}_{batchSize}", () =>
        {
            var properties = GetInsertableProperties(entityType);
            if (properties.Length == 0)
                return $"INSERT INTO {tableName} DEFAULT VALUES";
            
            var sb = OptimizedStringBuilder.Create(1024);
            
            sb.Append("INSERT INTO ").Append(tableName).Append(" (");
            sb.AppendJoin(", ", properties.Select(p => p.Name).ToArray());
            sb.Append(") VALUES ");
            
            var valueTemplate = "(" + string.Join(", ", properties.Select(p => "@" + p.Name.ToLowerInvariant() + "{0}")) + ")";
            
            for (int i = 0; i < batchSize; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendTemplate(valueTemplate, i);
            }
            
            return sb.ToString();
        });
    }
    
    /// <summary>
    /// Gets properties that can be selected (readable).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetSelectableColumns(INamedTypeSymbol entityType)
    {
        return IntelligentCacheManager.GetOrAdd($"SELECTABLE_COLUMNS_{entityType.ToDisplayString()}", () =>
        {
            var properties = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.GetMethod != null && p.GetMethod.DeclaredAccessibility == Accessibility.Public)
                .Select(p => p.Name)
                .ToArray();
            
            return properties.Length > 0 ? string.Join(", ", properties) : "*";
        });
    }
    
    /// <summary>
    /// Gets properties that can be inserted (writable, non-identity).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IPropertySymbol[] GetInsertableProperties(INamedTypeSymbol entityType)
    {
        return IntelligentCacheManager.GetOrAdd($"INSERTABLE_PROPS_{entityType.ToDisplayString()}", () =>
        {
            return entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null && 
                           p.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                           p.Name != "Id" && // Assume Id is auto-generated
                           !p.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Identity") == true))
                .ToArray();
        });
    }
    
    /// <summary>
    /// Gets properties that can be updated (writable, non-key).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IPropertySymbol[] GetUpdatableProperties(INamedTypeSymbol entityType)
    {
        return IntelligentCacheManager.GetOrAdd($"UPDATABLE_PROPS_{entityType.ToDisplayString()}", () =>
        {
            return entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null && 
                           p.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                           p.Name != "Id" && // Don't update primary key
                           !p.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Key") == true))
                .ToArray();
        });
    }
}
