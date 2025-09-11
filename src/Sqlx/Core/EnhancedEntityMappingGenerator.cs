// -----------------------------------------------------------------------
// <copyright file="EnhancedEntityMappingGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sqlx.Core;

/// <summary>
/// Enhanced entity mapping generator that supports primary constructors and records.
/// </summary>
internal static class EnhancedEntityMappingGenerator
{
    /// <summary>
    /// Generates optimized entity mapping code with support for primary constructors and records.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateEntityMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType).ToList();
        
        if (!members.Any())
        {
            sb.AppendLine("// No accessible members found for entity mapping");
            sb.AppendLine($"var entity = new {entityType.ToDisplayString()}();");
            return;
        }

        // Generate GetOrdinal caching for performance optimization - ENHANCED MAPPING GENERATOR
        foreach (var member in members)
        {
            var columnName = GetColumnName(member);
            sb.AppendLine($"int __ordinal_{columnName} = reader.GetOrdinal(\"{columnName}\");");
        }

        // Determine how to create the entity
        if (PrimaryConstructorAnalyzer.IsRecord(entityType))
        {
            GenerateRecordMapping(sb, entityType, members);
        }
        else if (PrimaryConstructorAnalyzer.HasPrimaryConstructor(entityType))
        {
            GeneratePrimaryConstructorMapping(sb, entityType, members);
        }
        else
        {
            GenerateTraditionalMapping(sb, entityType, members);
        }
    }

    /// <summary>
    /// Generates mapping for record types.
    /// </summary>
    private static void GenerateRecordMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, List<IMemberInfo> members)
    {
        var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(entityType);
        
        if (primaryConstructor != null && primaryConstructor.Parameters.Length > 0)
        {
            // Use primary constructor for records
            sb.AppendLine($"var entity = new {entityType.ToDisplayString()}(");
            sb.PushIndent();

            var parameters = primaryConstructor.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var propertyName = GetPropertyNameFromParameter(param.Name);
                var member = members.FirstOrDefault(m => m.Name == propertyName);
                
                var comma = i < parameters.Length - 1 ? "," : "";
                var columnName = GetColumnName(member ?? new PrimaryConstructorParameterMemberInfo(param, propertyName));
                var ordinalVar = $"__ordinal_{columnName}";
                
                var dataReadExpression = Extensions.GetDataReadExpression(param.Type, "reader", columnName);
                sb.AppendLine($"{dataReadExpression}{comma}");
            }

            sb.PopIndent();
            sb.AppendLine(");");
        }
        else
        {
            // Fallback to object initializer for records without primary constructor
            GenerateObjectInitializerMapping(sb, entityType, members);
        }
    }

    /// <summary>
    /// Generates mapping for classes with primary constructors.
    /// </summary>
    private static void GeneratePrimaryConstructorMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, List<IMemberInfo> members)
    {
        var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(entityType);
        
        if (primaryConstructor != null && primaryConstructor.Parameters.Length > 0)
        {
            // Use primary constructor
            sb.AppendLine($"var entity = new {entityType.ToDisplayString()}(");
            sb.PushIndent();

            var parameters = primaryConstructor.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var propertyName = GetPropertyNameFromParameter(param.Name);
                var member = members.FirstOrDefault(m => m.Name == propertyName);
                
                var comma = i < parameters.Length - 1 ? "," : "";
                var columnName = GetColumnName(member ?? new PrimaryConstructorParameterMemberInfo(param, propertyName));
                var ordinalVar = $"__ordinal_{columnName}";
                
                var dataReadExpression = Extensions.GetDataReadExpression(param.Type, "reader", columnName);
                sb.AppendLine($"{dataReadExpression}{comma}");
            }

            sb.PopIndent();
            sb.AppendLine(");");

            // Set additional properties that are not covered by primary constructor
            var primaryConstructorParamNames = new HashSet<string>(parameters.Select(p => GetPropertyNameFromParameter(p.Name)));
            var additionalMembers = members.Where(m => !primaryConstructorParamNames.Contains(m.Name) && 
                                                      m.CanWrite && 
                                                      !m.IsFromPrimaryConstructor).ToList();

            foreach (var member in additionalMembers)
            {
                var columnName = GetColumnName(member);
                var ordinalVar = $"__ordinal_{columnName}";
                var dataReadExpression = Extensions.GetDataReadExpression(member.Type, "reader", columnName);
                sb.AppendLine($"entity.{member.Name} = {dataReadExpression};");
            }
        }
        else
        {
            // Fallback to object initializer
            GenerateObjectInitializerMapping(sb, entityType, members);
        }
    }

    /// <summary>
    /// Generates traditional mapping using object initializer.
    /// </summary>
    private static void GenerateTraditionalMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, List<IMemberInfo> members)
    {
        GenerateObjectInitializerMapping(sb, entityType, members);
    }

    /// <summary>
    /// Generates mapping using object initializer syntax.
    /// </summary>
    private static void GenerateObjectInitializerMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, List<IMemberInfo> members)
    {
        var writableMembers = members.Where(m => m.CanWrite && !m.IsFromPrimaryConstructor).ToList();
        
        if (writableMembers.Any())
        {
            sb.AppendLine($"var entity = new {entityType.ToDisplayString()}");
            sb.AppendLine("{");
            sb.PushIndent();

            for (int i = 0; i < writableMembers.Count; i++)
            {
                var member = writableMembers[i];
                var comma = i < writableMembers.Count - 1 ? "," : "";
                var columnName = GetColumnName(member);
                var ordinalVar = $"__ordinal_{columnName}";

                var dataReadExpression = Extensions.GetDataReadExpression(member.Type, "reader", columnName);
                sb.AppendLine($"{member.Name} = {dataReadExpression}{comma}");
            }

            sb.PopIndent();
            sb.AppendLine("};");
        }
        else
        {
            sb.AppendLine($"var entity = new {entityType.ToDisplayString()}();");
        }
    }

    /// <summary>
    /// Gets the column name for a member.
    /// </summary>
    private static string GetColumnName(IMemberInfo member)
    {
        // For now, use the member name as column name
        // In the future, this could check for column attributes
        return member.Name;
    }

    /// <summary>
    /// Gets the data read expression for a type.
    /// </summary>
    private static string GetDataReadExpression(ITypeSymbol type, string readerName, string columnName, string ordinalVar)
    {
        var unwrapType = type.UnwrapNullableType();
        var method = type.GetDataReaderMethod();
        var isNullable = type.IsNullableType();
        

        if (!string.IsNullOrEmpty(method))
        {
            // For nullable types or nullable reference types, check for DBNull
            if (isNullable || unwrapType.IsValueType || unwrapType.SpecialType == SpecialType.System_String || type.Name == "Guid")
            {
                // For nullable value types and strings, return proper null handling
                if (unwrapType.SpecialType == SpecialType.System_String)
                {
                    // String special case: check if nullable annotation is present
                    if (isNullable)
                    {
                        return $"{readerName}.IsDBNull({ordinalVar}) ? null : {readerName}.{method}({ordinalVar})";
                    }
                    else
                    {
                        // Non-nullable string: return empty string or throw
                        return $"{readerName}.IsDBNull({ordinalVar}) ? string.Empty : {readerName}.{method}({ordinalVar})";
                    }
                }
                else if (isNullable && unwrapType.IsValueType)
                {
                    // Nullable value types: return null if DBNull
                    return $"{readerName}.IsDBNull({ordinalVar}) ? null : {readerName}.{method}({ordinalVar})";
                }
                else if (unwrapType.IsValueType)
                {
                    // Non-nullable value types: return default if DBNull
                    return $"{readerName}.IsDBNull({ordinalVar}) ? default : {readerName}.{method}({ordinalVar})";
                }
                else
                {
                    // Reference types: return null if DBNull
                    return $"{readerName}.IsDBNull({ordinalVar}) ? null : {readerName}.{method}({ordinalVar})";
                }
            }
            else
            {
                // For non-nullable types, use the method directly
                return $"{readerName}.{method}({ordinalVar})";
            }
        }
        else
        {
            // Enhanced fallback handling for unsupported types
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            // Special handling for enum types
            if (unwrapType.TypeKind == TypeKind.Enum)
            {
                var underlyingType = ((INamedTypeSymbol)unwrapType).EnumUnderlyingType;
                var underlyingMethod = underlyingType?.GetDataReaderMethod();
                
                if (!string.IsNullOrEmpty(underlyingMethod))
                {
                    var unwrappedTypeName = unwrapType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (isNullable)
                    {
                        return $"{readerName}.IsDBNull({ordinalVar}) ? null : ({unwrappedTypeName}){readerName}.{underlyingMethod}({ordinalVar})";
                    }
                    else
                    {
                        return $"{readerName}.IsDBNull({ordinalVar}) ? default({unwrappedTypeName}) : ({unwrappedTypeName}){readerName}.{underlyingMethod}({ordinalVar})";
                    }
                }
            }
            
            // Try to use Convert methods for better type safety
            if (TryGetConvertMethod(unwrapType, out var convertMethod))
            {
                if (isNullable || type.IsReferenceType)
                {
                    return $"{readerName}.IsDBNull({ordinalVar}) ? null : {convertMethod}({readerName}.GetValue({ordinalVar}))";
                }
                else
                {
                    return $"{convertMethod}({readerName}.GetValue({ordinalVar}))";
                }
            }
            
            // Final fallback to GetValue with casting (less preferred)
            if (isNullable || type.IsReferenceType)
            {
                return $"{readerName}.IsDBNull({ordinalVar}) ? null : ({typeName}){readerName}.GetValue({ordinalVar})";
            }
            else
            {
                return $"{readerName}.IsDBNull({ordinalVar}) ? default({typeName}) : ({typeName}){readerName}.GetValue({ordinalVar})";
            }
        }
    }

    /// <summary>
    /// Tries to get a Convert method for better type safety.
    /// </summary>
    private static bool TryGetConvertMethod(ITypeSymbol type, out string convertMethod)
    {
        convertMethod = type.SpecialType switch
        {
            SpecialType.System_Boolean => "global::System.Convert.ToBoolean",
            SpecialType.System_Byte => "global::System.Convert.ToByte",
            SpecialType.System_SByte => "global::System.Convert.ToSByte",
            SpecialType.System_Int16 => "global::System.Convert.ToInt16",
            SpecialType.System_Int32 => "global::System.Convert.ToInt32",
            SpecialType.System_Int64 => "global::System.Convert.ToInt64",
            SpecialType.System_UInt16 => "global::System.Convert.ToUInt16",
            SpecialType.System_UInt32 => "global::System.Convert.ToUInt32",
            SpecialType.System_UInt64 => "global::System.Convert.ToUInt64",
            SpecialType.System_Single => "global::System.Convert.ToSingle",
            SpecialType.System_Double => "global::System.Convert.ToDouble",
            SpecialType.System_Decimal => "global::System.Convert.ToDecimal",
            SpecialType.System_DateTime => "global::System.Convert.ToDateTime",
            SpecialType.System_String => "global::System.Convert.ToString",
            _ => string.Empty
        };
        
        // Handle special types by name
        if (string.IsNullOrEmpty(convertMethod))
        {
            convertMethod = type.Name switch
            {
                "Guid" => "global::System.Guid.Parse",
                "DateTimeOffset" => "global::System.DateTimeOffset.Parse",
                "TimeSpan" => "global::System.TimeSpan.Parse",
                _ => string.Empty
            };
        }
        
        return !string.IsNullOrEmpty(convertMethod);
    }

    /// <summary>
    /// Converts a parameter name to the corresponding property name (PascalCase).
    /// </summary>
    private static string GetPropertyNameFromParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return parameterName;

        // Convert camelCase to PascalCase
        return char.ToUpper(parameterName[0]) + parameterName.Substring(1);
    }
}
