// -----------------------------------------------------------------------
// <copyright file="AttributeHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Text;

namespace Sqlx.Generator.Core;

/// <summary>
/// Default implementation of attribute handler.
/// </summary>
public class AttributeHandler
{
    /// <summary>
    /// Generates or copies attributes for a method.
    /// </summary>
    public void GenerateOrCopyAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        // Check if method already has SQL attributes
        var existingSqlAttributes = method.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == "SqlxAttribute" ||
                          attr.AttributeClass?.Name == "SqlExecuteTypeAttribute" ||
                          attr.AttributeClass?.Name == "SqlTemplateAttribute")
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

    /// <summary>
    /// Generates a Sqlx attribute for a method.
    /// </summary>
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

    /// <summary>
    /// Generates an attribute from existing attribute data.
    /// </summary>
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
            else if (attributeClass.Name == "SqlTemplateAttribute")
            {
                sb.Append("global::Sqlx.Annotations.SqlTemplate(");
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var sqlArg = attribute.ConstructorArguments[0];
                    if (sqlArg.Value != null)
                    {
                        sb.Append($"\"{sqlArg.Value.ToString().Replace("\"", "\\\"")}\"");
                    }
                }

                // Add named arguments if present
                if (attribute.NamedArguments.Length > 0)
                {
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        sb.Append($", {namedArg.Key} = ");
                        if (namedArg.Value.Value is string stringValue)
                        {
                            sb.Append($"\"{stringValue}\"");
                        }
                        else if (namedArg.Value.Value is bool boolValue)
                        {
                            sb.Append(boolValue ? "true" : "false");
                        }
                        else if (namedArg.Key == "Dialect" && namedArg.Value.Value is int dialectValue)
                        {
                            // Convert dialect enum value to proper enum name
                            var dialectName = dialectValue switch
                            {
                                0 => "MySql",
                                1 => "SqlServer",
                                2 => "PostgreSql",
                                3 => "Oracle",
                                4 => "DB2",
                                5 => "SQLite",
                                _ => "SQLite"
                            };
                            sb.Append($"global::Sqlx.Annotations.SqlDefineTypes.{dialectName}");
                        }
                        else if (namedArg.Key == "Operation" && namedArg.Value.Value is int operationValue)
                        {
                            // Convert operation enum value to proper enum name
                            var operationName = operationValue switch
                            {
                                0 => "Select",
                                1 => "Insert",
                                2 => "Update",
                                3 => "Delete",
                                _ => "Select"
                            };
                            sb.Append($"global::Sqlx.SqlOperation.{operationName}");
                        }
                        else
                        {
                            sb.Append(namedArg.Value.Value?.ToString() ?? "null");
                        }
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
