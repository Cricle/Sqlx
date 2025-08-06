// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

internal static class Extensions
{
    public static bool CanHaveNullValue(this ITypeSymbol typeSymbol, bool hasNullableAnnotations)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        var requireParameterNullCheck = !hasNullableAnnotations && !typeSymbol.IsValueType;
        return requireParameterNullCheck;
    }

    internal static bool IsDbConnection(this ITypeSymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x => x.Name == "DbConnection");
    }

    internal static bool IsDbTransaction(this ITypeSymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x => x.Name == "DbTransaction");
    }

    internal static bool IsTypes(ITypeSymbol typeSymbol, Func<ITypeSymbol, bool> check)
    {
        if (check(typeSymbol))
        {
            return true;
        }

        var baseType = typeSymbol.BaseType;
        if (baseType == null)
        {
            return false;
        }

        return IsTypes(baseType, check);
    }

    internal static bool IsDbContext(this ITypeSymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x => x.Name == "DbContext");
    }

    internal static bool IsCancellationToken(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.Name == "CancellationToken";
    }

    internal static ITypeSymbol UnwrapTaskType(this ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedTypeSymbol)
        {
            if (type.Name == "Task" && namedTypeSymbol.IsGenericType)
            {
                return namedTypeSymbol.TypeArguments[0];
            }
        }

        return type;
    }

    internal static ITypeSymbol UnwrapNullableType(this ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedTypeSymbol)
        {
            return UnwrapNullableType(namedTypeSymbol);
        }

        return type;
    }

    internal static INamedTypeSymbol UnwrapNullableType(this INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.Name == "Nullable")
        {
            return (INamedTypeSymbol)namedTypeSymbol.TypeArguments[0];
        }

        return namedTypeSymbol;
    }

    internal static bool IsScalarType(ITypeSymbol returnType)
    {
        return returnType.SpecialType switch
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

    internal static bool IsTuple(ITypeSymbol returnType)
    {
        return returnType.Name == "Tuple" || returnType.Name == "ValueTuple";
    }

    internal static ITypeSymbol GetUnderlyingType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedTypeSymbol)
        {
            if (!namedTypeSymbol.IsGenericType || namedTypeSymbol.TypeArguments.Length != 1)
            {
                return returnType;
            }

            return namedTypeSymbol.TypeArguments[0];
        }

        return returnType;
    }

    internal static bool IsList(this ITypeSymbol returnType) => returnType.Name == "IList" || returnType.Name == "List";

    internal static bool IsEnumerable(ITypeSymbol returnType) => returnType.Name == "IEnumerable";

    internal static bool IsAsyncEnumerable(ITypeSymbol returnType) => returnType.Name == "IAsyncEnumerable";

    internal static ITypeSymbol UnwrapListItem(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedTypeSymbol)
        {
            if (!IsList(returnType) && !IsEnumerable(returnType))
            {
                return returnType;
            }

            if (!namedTypeSymbol.IsGenericType || namedTypeSymbol.TypeArguments.Length != 1)
            {
                return returnType;
            }

            return namedTypeSymbol.TypeArguments[0];
        }

        return returnType;
    }

    internal static IPropertySymbol? FindIdMember(this ITypeSymbol returnType)
    {
        return returnType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(IsPrimaryKey);
    }

    internal static bool IsPrimaryKey(this IPropertySymbol propertySymbol)
    {
        return propertySymbol.Name == "Id";
    }

    internal static bool IsPrimaryKey(this IParameterSymbol propertySymbol)
    {
        return propertySymbol.Name == "Id" || propertySymbol.Name == "id";
    }

    internal static IPropertySymbol? FindMember(this ITypeSymbol returnType, string parameterName)
    {
        return returnType.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(propertySymbol => string.Equals(propertySymbol.Name, parameterName, StringComparison.InvariantCultureIgnoreCase));
    }

    internal static string GetSqlName(this IPropertySymbol propertySymbol)
    {
        var attribute = propertySymbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute");
        if (attribute != null)
        {
            var overrideName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (overrideName is not null)
            {
                return overrideName;
            }
        }

        return NameMapper.MapName(propertySymbol.Name);
    }

    internal static string GetSqlName(this ITypeSymbol typeSymbol)
    {
        var attribute = typeSymbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.DataAnnotations.Schema.TableAttribute");
        if (attribute != null)
        {
            var overrideName = attribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (overrideName is not null)
            {
                return overrideName;
            }
        }

        return NameMapper.MapName(typeSymbol.Name);
    }

    internal static string GetParameterName(this IPropertySymbol propertySymbol)
    {
        return "@" + NameMapper.MapName(propertySymbol.Name);
    }

    internal static string GetParameterName(this IParameterSymbol parameterSymbol)
    {
        return "@" + NameMapper.MapName(parameterSymbol.Name);
    }

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

    internal static string GetParameterSqlDbType(this ITypeSymbol type)
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

    internal static string GetDataReaderMethod(this ITypeSymbol type)
    {
        switch (UnwrapNullableType(type).SpecialType)
        {
            case SpecialType.System_Boolean:
                return "GetBoolean";
            case SpecialType.System_Char:
                return "GetChar";
            case SpecialType.System_String:
                return "GetString";
            case SpecialType.System_Byte:
                return "GetByte";
            case SpecialType.System_SByte:
                return "GetSByte";
            case SpecialType.System_Int16:
                return "GetInt16";
            case SpecialType.System_Int32:
                return "GetInt32";
            case SpecialType.System_Int64:
                return "GetInt64";
            case SpecialType.System_UInt16:
                return "GetUInt16";
            case SpecialType.System_UInt32:
                return "GetUInt32";
            case SpecialType.System_UInt64:
                return "GetUInt64";
            case SpecialType.System_Single:
                return "GetSingle";
            case SpecialType.System_Double:
                return "GetDouble";
            case SpecialType.System_Decimal:
                return "GetDecimal";
            case SpecialType.System_DateTime:
                return "GetDateTime";
            case SpecialType.System_Object:
                return "GetValue";
            default: break;
        }

        if (type.Name == "Guid")
        {
            return "GetGuid";
        }

        throw new NotSupportedException($"No support type {type.Name}");
    }
}
