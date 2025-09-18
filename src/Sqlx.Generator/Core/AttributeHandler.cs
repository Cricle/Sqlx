// -----------------------------------------------------------------------
// <copyright file="AttributeHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Sqlx.Generator.Core;

/// <summary>
/// Default implementation of attribute handler.
/// </summary>
public class AttributeHandler : IAttributeHandler
{
    /// <inheritdoc/>
    public void GenerateOrCopyAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        // Check if method already has SQL attributes
        var existingSqlAttributes = method.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == "SqlxAttribute" ||
                          attr.AttributeClass?.Name == "SqlExecuteTypeAttribute")
            .ToArray();

        if (existingSqlAttributes.Any())
        {
            // Copy existing attributes as-is
            foreach (var attr in existingSqlAttributes)
            {
                sb.AppendLine(GenerateSqlxAttribute(attr));
            }
        }
        else
        {
            // Generate new attribute
            sb.AppendLine(GenerateSqlxAttribute(method, entityType, tableName));
        }
    }

    /// <inheritdoc/>
    public string GenerateSqlxAttribute(IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            var methodName = method.Name.ToLowerInvariant();
            var entityTypeName = entityType?.Name ?? "Entity";

            // Determine the appropriate Sqlx attribute based on method name patterns
            if (methodName.Contains("getall") || methodName.StartsWith("findall"))
            {
                return $"[global::Sqlx.Annotations.Sqlx(\"SELECT * FROM {tableName}\")]";
            }
            else if (methodName.Contains("getby") || methodName.Contains("findby") ||
                    (methodName.StartsWith("get") && method.Parameters.Length > 0))
            {
                var paramName = method.Parameters.FirstOrDefault()?.Name ?? "id";
                return $"[global::Sqlx.Annotations.Sqlx(\"SELECT * FROM {tableName} WHERE Id = @{paramName}\")]";
            }
            else if (methodName.Contains("create") || methodName.Contains("insert") || methodName.Contains("add"))
            {
                return $"[global::Sqlx.Annotations.SqlExecuteType(SqlOperation.Insert, \"{tableName}\")]";
            }
            else if (methodName.Contains("update") || methodName.Contains("modify"))
            {
                return $"[global::Sqlx.Annotations.SqlExecuteType(SqlOperation.Update, \"{tableName}\")]";
            }
            else if (methodName.Contains("delete") || methodName.Contains("remove"))
            {
                return $"[global::Sqlx.Annotations.SqlExecuteType(SqlOperation.Delete, \"{tableName}\")]";
            }
            else if (methodName.Contains("count"))
            {
                return $"[global::Sqlx.Annotations.Sqlx(\"SELECT COUNT(*) FROM {tableName}\")]";
            }
            else if (methodName.Contains("exists"))
            {
                var paramName = method.Parameters.FirstOrDefault()?.Name ?? "id";
                return $"[global::Sqlx.Annotations.Sqlx(\"SELECT COUNT(*) FROM {tableName} WHERE Id = @{paramName}\")]";
            }
            else
            {
                // Default to a SELECT query for unknown patterns
                return $"[global::Sqlx.Annotations.Sqlx(\"SELECT * FROM {tableName}\")]";
            }
        }
        catch
        {
            // Fallback on error
            return $"[global::Sqlx.Annotations.Sqlx(\"SELECT * FROM {tableName}\")]";
        }
    }

    /// <inheritdoc/>
    public string GenerateSqlxAttribute(AttributeData attribute)
    {
        try
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append('[');

            // Handle different attribute types
            if (attributeClass.Name == "SqlxAttribute")
            {
                sb.Append("global::Sqlx.Annotations.Sqlx(");
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var sqlArg = attribute.ConstructorArguments[0];
                    if (sqlArg.Value != null)
                    {
                        sb.Append($"\"{sqlArg.Value.ToString().Replace("\"", "\\\"")}\"");
                    }
                }
                sb.Append(')');
            }
            else if (attributeClass.Name == "SqlExecuteTypeAttribute")
            {
                sb.Append("global::Sqlx.Annotations.SqlExecuteType(");
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var executeTypeArg = attribute.ConstructorArguments[0];
                    sb.Append($"SqlOperation.{GetSqlExecuteTypeName(executeTypeArg)}");

                    if (attribute.ConstructorArguments.Length > 1)
                    {
                        var tableNameArg = attribute.ConstructorArguments[1];
                        if (tableNameArg.Value != null)
                        {
                            sb.Append($", \"{tableNameArg.Value}\"");
                        }
                    }
                }
                sb.Append(')');
            }
            else
            {
                // Generic attribute handling
                sb.Append(attributeClass.Name);
                if (attribute.ConstructorArguments.Length > 0)
                {
                    sb.Append('(');
                    for (int i = 0; i < attribute.ConstructorArguments.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");

                        var arg = attribute.ConstructorArguments[i];
                        if (arg.Value is string stringValue)
                        {
                            sb.Append($"\"{stringValue.Replace("\"", "\\\"")}\"");
                        }
                        else
                        {
                            sb.Append(arg.Value?.ToString() ?? "null");
                        }
                    }
                    sb.Append(')');
                }
            }

            sb.Append(']');
            return sb.ToString();
        }
        catch
        {
            // Fallback on error
            return $"[{attribute.AttributeClass?.Name ?? "UnknownAttribute"}]";
        }
    }

    private string GetSqlExecuteTypeName(TypedConstant executeTypeArg)
    {
        if (executeTypeArg.Value is int intValue)
        {
            return intValue switch
            {
                0 => "None",
                1 => "Insert",
                2 => "Update",
                3 => "Delete",
                4 => "Select",
                _ => intValue.ToString()
            };
        }

        return executeTypeArg.Value?.ToString() ?? "None";
    }
}
