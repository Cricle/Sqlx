// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Core;

/// <summary>
/// SQL template processing engine implementation.
/// This is the core engine that processes SQL templates with placeholders and generates appropriate code.
/// </summary>
public class SqlTemplateEngine : ISqlTemplateEngine
{
    private static readonly Regex ParameterRegex = new(@"[@:$]\w+", RegexOptions.Compiled);
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)(?::(\w+))?\}\}", RegexOptions.Compiled);

    /// <inheritdoc/>
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        var result = new SqlTemplateResult();
        
        if (string.IsNullOrWhiteSpace(templateSql))
        {
            result.ProcessedSql = "SELECT 1"; // Safe fallback
            result.Warnings.Add("Empty SQL template provided");
            return result;
        }

        var processedSql = templateSql;

        // Step 1: Process template placeholders ({{placeholder:type}})
        processedSql = ProcessPlaceholders(processedSql, method, entityType, tableName, result);

        // Step 2: Extract and validate parameters (@param, :param, $param)
        ProcessParameters(processedSql, method, result);

        // Step 3: Detect dynamic features
        result.HasDynamicFeatures = HasDynamicFeatures(processedSql);

        result.ProcessedSql = processedSql;
        return result;
    }

    /// <inheritdoc/>
    public TemplateValidationResult ValidateTemplate(string templateSql)
    {
        var result = new TemplateValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(templateSql))
        {
            result.IsValid = false;
            result.Errors.Add("SQL template cannot be empty");
            return result;
        }

        // Check for common SQL injection patterns
        if (ContainsPotentialInjection(templateSql))
        {
            result.Warnings.Add("Template contains potential SQL injection risks");
        }

        // Validate placeholder syntax
        var placeholders = PlaceholderRegex.Matches(templateSql);
        foreach (Match placeholder in placeholders)
        {
            var placeholderName = placeholder.Groups[1].Value;
            var placeholderType = placeholder.Groups[2].Value;

            if (!IsValidPlaceholder(placeholderName, placeholderType))
            {
                result.Errors.Add($"Invalid placeholder: {placeholder.Value}");
                result.IsValid = false;
            }
        }

        return result;
    }

    private string ProcessPlaceholders(string sql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, SqlTemplateResult result)
    {
        return PlaceholderRegex.Replace(sql, match =>
        {
            var placeholderName = match.Groups[1].Value.ToLowerInvariant();
            var placeholderType = match.Groups[2].Value.ToLowerInvariant();

            return placeholderName switch
            {
                "table" => ProcessTablePlaceholder(tableName, placeholderType, entityType),
                "columns" => ProcessColumnsPlaceholder(placeholderType, entityType, result),
                "values" => ProcessValuesPlaceholder(placeholderType, entityType, method),
                "where" => ProcessWherePlaceholder(placeholderType, entityType, method),
                "set" => ProcessSetPlaceholder(placeholderType, entityType, method),
                "orderby" => ProcessOrderByPlaceholder(placeholderType, entityType),
                _ => match.Value // Keep original if not recognized
            };
        });
    }

    private string ProcessTablePlaceholder(string tableName, string type, INamedTypeSymbol? entityType)
    {
        // Convert table name to snake_case
        var snakeTableName = ConvertToSnakeCase(tableName);
        
        return type switch
        {
            "quoted" => $"[{snakeTableName}]",
            "schema" => $"dbo.{snakeTableName}",
            _ => snakeTableName
        };
    }

    private string ProcessColumnsPlaceholder(string type, INamedTypeSymbol? entityType, SqlTemplateResult result)
    {
        if (entityType == null)
        {
            result.Warnings.Add("Cannot infer columns without entity type");
            return "*";
        }

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
            .ToList();

        return type switch
        {
            "auto" => string.Join(", ", properties.Select(p => ConvertToSnakeCase(p.Name))),
            "quoted" => string.Join(", ", properties.Select(p => $"[{ConvertToSnakeCase(p.Name)}]")),
            "prefixed" => string.Join(", ", properties.Select(p => $"t.{ConvertToSnakeCase(p.Name)}")),
            _ => string.Join(", ", properties.Select(p => ConvertToSnakeCase(p.Name)))
        };
    }

    private string ProcessValuesPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method)
    {
        if (entityType == null)
        {
            // Use method parameters
            var parameters = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
            return string.Join(", ", parameters.Select(p => $"@{p.Name}"));
        }

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "Id")
            .ToList();

        return string.Join(", ", properties.Select(p => $"@{p.Name}"));
    }

    private string ProcessWherePlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method)
    {
        return type switch
        {
            "id" => $"{ConvertToSnakeCase("Id")} = @id",
            "auto" => GenerateAutoWhereClause(method),
            _ => "1=1"
        };
    }

    private string ProcessSetPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method)
    {
        if (entityType == null)
        {
            var parameters = method.Parameters.Where(p => 
                p.Type.Name != "CancellationToken" && 
                !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase)).ToList();
            return string.Join(", ", parameters.Select(p => $"{p.Name} = @{p.Name}"));
        }

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.SetMethod != null && p.Name != "Id")
            .ToList();

        return string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
    }

    private string ProcessOrderByPlaceholder(string type, INamedTypeSymbol? entityType)
    {
        return type switch
        {
            "id" => "Id",
            "name" => "Name",
            _ => "Id"
        };
    }

    private string GenerateAutoWhereClause(IMethodSymbol method)
    {
        var parameters = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
        if (!parameters.Any())
            return "1=1";

        return string.Join(" AND ", parameters.Select(p => $"{InferColumnName(p.Name)} = @{p.Name}"));
    }

    private string InferColumnName(string parameterName)
    {
        // Convert to snake_case for database column names
        return ConvertToSnakeCase(parameterName);
    }

    /// <summary>
    /// Converts C# property names to snake_case database column names.
    /// </summary>
    private string ConvertToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // If already contains underscores and is all lowercase, return as-is
        if (name.Contains("_") && name.All(c => char.IsLower(c) || c == '_' || char.IsDigit(c)))
        {
            return name;
        }

        // If already contains underscores and is all caps, convert to lowercase
        if (name.Contains("_") && name.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
        {
            return name.ToLower();
        }

        // Convert PascalCase/camelCase to snake_case
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            
            if (char.IsUpper(current))
            {
                // Add underscore before uppercase letters (except at the beginning)
                if (i > 0 && !char.IsUpper(name[i - 1]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(current));
            }
            else
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }

    private void ProcessParameters(string sql, IMethodSymbol method, SqlTemplateResult result)
    {
        var parameterMatches = ParameterRegex.Matches(sql);
        var methodParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();

        foreach (Match match in parameterMatches)
        {
            var paramName = match.Value.Substring(1); // Remove @ : or $
            var methodParam = methodParams.FirstOrDefault(p => 
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

            if (methodParam != null)
            {
                result.Parameters.Add(new ParameterMapping
                {
                    Name = paramName,
                    Type = methodParam.Type.ToDisplayString(),
                    IsNullable = methodParam.Type.CanBeReferencedByName && methodParam.NullableAnnotation == NullableAnnotation.Annotated,
                    DbType = InferDbType(methodParam.Type)
                });
            }
            else
            {
                result.Warnings.Add($"Parameter '{paramName}' not found in method signature");
            }
        }
    }

    private string InferDbType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "String",
            SpecialType.System_Int32 => "Int32",
            SpecialType.System_Int64 => "Int64",
            SpecialType.System_Boolean => "Boolean",
            SpecialType.System_DateTime => "DateTime",
            SpecialType.System_Decimal => "Decimal",
            SpecialType.System_Double => "Double",
            SpecialType.System_Single => "Single",
            SpecialType.System_Byte => "Byte",
            SpecialType.System_Int16 => "Int16",
            _ => "Object"
        };
    }

    private bool HasDynamicFeatures(string sql)
    {
        // Check for conditional logic, loops, or other dynamic features
        return sql.Contains("IF") || sql.Contains("CASE") || sql.Contains("WHILE") || 
               sql.Contains("{{") || sql.Contains("}}");
    }

    private bool ContainsPotentialInjection(string sql)
    {
        var dangerousPatterns = new[]
        {
            "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE"
        };

        var upperSql = sql.ToUpperInvariant();
        return dangerousPatterns.Any(pattern => upperSql.Contains(pattern));
    }

    private bool IsValidPlaceholder(string name, string type)
    {
        var validPlaceholders = new[] { "table", "columns", "values", "where", "set", "orderby" };
        return validPlaceholders.Contains(name.ToLowerInvariant());
    }
}


