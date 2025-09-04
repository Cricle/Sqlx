// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

internal static class Extensions
{
    public static bool CanHaveNullValue(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        // Reference types can have null values unless they're non-nullable in a nullable context
        return !typeSymbol.IsValueType || typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    public static bool IsNullableType(this ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated ||
               (type is INamedTypeSymbol namedType && namedType.IsGenericType &&
                namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);
    }

    internal static bool IsDbConnection(this ISymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x =>
        {
            var type = x;
            while (type != null)
            {
                if (type.Name == "DbConnection")
                    return true;
                type = type.BaseType;
            }

            return false;
        });
    }

    internal static bool IsDbTransaction(this ISymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x =>
        {
            var type = x;
            while (type != null)
            {
                if (type.Name == "DbTransaction")
                    return true;
                type = type.BaseType;
            }

            return false;
        });
    }

    internal static bool IsTypes(ISymbol typeSymbol, Func<ITypeSymbol, bool> check)
    {
        if (typeSymbol is IParameterSymbol parSymbol && check(parSymbol.Type)) return true;
        if (typeSymbol is IFieldSymbol fieldSymbol && check(fieldSymbol.Type)) return true;
        if (typeSymbol is IPropertySymbol propertySymbol && check(propertySymbol.Type)) return true;
        if (typeSymbol is INamedTypeSymbol ntSymbol && check(ntSymbol)) return true;
        return false;
    }

    internal static bool IsDbContext(this ISymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x =>
        {
            var type = x;
            while (type != null)
            {
                if (type.Name == "DbContext")
                    return true;
                type = type.BaseType;
            }

            return false;
        });
    }

    internal static bool IsCancellationToken(this ISymbol typeSymbol) => typeSymbol.Name == "CancellationToken";

    internal static ITypeSymbol UnwrapTaskType(this ITypeSymbol type) => UnwrapType(type, "Task");

    internal static ITypeSymbol UnwrapListType(this ITypeSymbol type) => UnwrapType(type, "List", "IList", "ICollection", "IReadonlyList", "IEnumerable", "IAsyncEnumerable");

    internal static ITypeSymbol UnwrapNullableType(this ITypeSymbol type)
        => type is INamedTypeSymbol namedTypeSymbol ? UnwrapNullableType(namedTypeSymbol) : type;

    private static INamedTypeSymbol UnwrapNullableType(INamedTypeSymbol namedTypeSymbol)
        => namedTypeSymbol.Name == "Nullable" ? (INamedTypeSymbol)namedTypeSymbol.TypeArguments[0] : namedTypeSymbol;

    internal static bool IsScalarType(this ITypeSymbol returnType)
    {
        return UnwrapNullableType(UnwrapTaskType(returnType)).SpecialType switch
        {
            SpecialType.System_String => true,
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_DateTime => true,
            SpecialType.System_Decimal => true,
            SpecialType.System_Double => true,
            _ => false,
        };
    }

    internal static bool IsTuple(ITypeSymbol returnType) => returnType.Name == "Tuple" || returnType.Name == "ValueTuple";

    internal static IPropertySymbol? FindMember(this ITypeSymbol returnType, string parameterName)
    {
        return returnType.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(propertySymbol => string.Equals(propertySymbol.Name, parameterName, StringComparison.InvariantCultureIgnoreCase));
    }

    internal static string GetSqlName(this ISymbol propertySymbol)
    {
        var attribute = propertySymbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.Name == "DbColumnAttribute");
        if (attribute != null && attribute.ConstructorArguments.FirstOrDefault().Value is string name)
        {
            return name;
        }

        return NameMapper.MapName(propertySymbol.Name);
    }

    internal static string GetParameterName(this ISymbol propertySymbol, string parameterPrefx)
        => parameterPrefx + GetSqlName(propertySymbol);

    /// <summary>
    /// Public version of GetParameterName for ITypeSymbol - used by tests
    /// </summary>
    public static string GetParameterName(this ITypeSymbol typeSymbol, string baseName)
        => "@" + baseName.Replace("_", string.Empty);

    internal static string GetAccessibility(this Accessibility a)
    {
        return a switch
        {
            Accessibility.Public => "public",
            Accessibility.Friend => "internal",
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected =>"protected",
            _ => string.Empty,
        };
    }

    internal static string GetDbType(this ITypeSymbol type)
    {
        return UnwrapNullableType(type).SpecialType switch
        {
            SpecialType.System_Boolean => "global::System.Data.DbType.Boolean",
            SpecialType.System_String => "global::System.Data.DbType.String",
            SpecialType.System_Char => "global::System.Data.DbType.Char",
            SpecialType.System_Byte => "global::System.Data.DbType.Byte",
            SpecialType.System_SByte => "global::System.Data.DbType.SByte",
            SpecialType.System_Int16 => "global::System.Data.DbType.Int16",
            SpecialType.System_Int32 => "global::System.Data.DbType.Int32",
            SpecialType.System_Int64 => "global::System.Data.DbType.Int64",
            SpecialType.System_UInt16 => "global::System.Data.DbType.UInt16",
            SpecialType.System_UInt32 => "global::System.Data.DbType.UInt32",
            SpecialType.System_UInt64 => "global::System.Data.DbType.UInt64",
            SpecialType.System_Single => "global::System.Data.DbType.Single",
            SpecialType.System_Double => "global::System.Data.DbType.Double",
            SpecialType.System_Decimal => "global::System.Data.DbType.Decimal",
            SpecialType.System_DateTime => "global::System.Data.DbType.DateTime2",
            _ => throw new NotImplementedException(),
        };
    }

    internal static string? GetDataReaderMethod(this ITypeSymbol type)
    {
        var unwrapType = UnwrapNullableType(type);
        var method = unwrapType.SpecialType switch
        {
            SpecialType.System_Boolean => "GetBoolean",
            SpecialType.System_Char => "GetChar",
            SpecialType.System_String => "GetString",
            SpecialType.System_Byte => "GetByte",
            SpecialType.System_SByte => "GetSByte",
            SpecialType.System_Int16 => "GetInt16",
            SpecialType.System_Int32 => "GetInt32",
            SpecialType.System_Int64 => "GetInt64",
            SpecialType.System_UInt16 => "GetUInt16",
            SpecialType.System_UInt32 => "GetUInt32",
            SpecialType.System_UInt64 => "GetUInt64",
            SpecialType.System_Single => "GetFloat",
            SpecialType.System_Double => "GetDouble",
            SpecialType.System_Decimal => "GetDecimal",
            SpecialType.System_DateTime => "GetDateTime",
            SpecialType.System_Object => "GetValue",
            _ => null,
        };
        if (type.Name == "Guid") method = "GetGuid";
        return method;
    }

    internal static string GetDataReadIndexExpression(this ITypeSymbol type, string readerName, int index)
    {
        var method = GetDataReaderMethod(type);
        return UnwrapNullableType(type) != type
            ? $"{readerName}.IsDBNull(reader.GetOrdinal(0)) ? default : {readerName}.{method}({index})"
            : $"{readerName}.{method}({index})";
    }

    public static string GetDataReadExpression(this ITypeSymbol type, string readerName, string columnName)
    {
        var unwrapType = UnwrapNullableType(type);
        var method = GetDataReaderMethod(type);
        var isNullable = IsNullableType(type);
        var ordinalExpression = $"{readerName}.GetOrdinal(\"{columnName}\")";

        if (!string.IsNullOrEmpty(method))
        {
            // For nullable types or nullable reference types, check for DBNull
            if (isNullable || unwrapType.IsValueType || unwrapType.SpecialType == SpecialType.System_String || type.Name == "Guid")
            {
                // For nullable value types and strings, return proper null handling
                if (unwrapType.SpecialType == SpecialType.System_String)
                {
                    // String special case: can be null or empty string
                    return $"{readerName}.IsDBNull({ordinalExpression}) ? null : {readerName}.{method}({ordinalExpression})";
                }
                else if (isNullable && unwrapType.IsValueType)
                {
                    // Nullable value types: return null if DBNull
                    return $"{readerName}.IsDBNull({ordinalExpression}) ? null : {readerName}.{method}({ordinalExpression})";
                }
                else if (unwrapType.IsValueType)
                {
                    // Non-nullable value types: return default if DBNull
                    return $"{readerName}.IsDBNull({ordinalExpression}) ? default : {readerName}.{method}({ordinalExpression})";
                }
                else
                {
                    // Reference types: return null if DBNull
                    return $"{readerName}.IsDBNull({ordinalExpression}) ? null : {readerName}.{method}({ordinalExpression})";
                }
            }

            return $"{readerName}.{method}({ordinalExpression})";
        }

        throw new NotSupportedException($"No support type {type.Name}");
    }

    private static ITypeSymbol UnwrapType(this ITypeSymbol type, params string[] names)
    {
        if (type is INamedTypeSymbol namedTypeSymbol && names.Contains(type.Name) && namedTypeSymbol.IsGenericType)
        {
            return namedTypeSymbol.TypeArguments[0];
        }

        return type;
    }
}
