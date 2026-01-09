// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

internal static class Extensions
{
    // No caching in source generators to ensure fresh compilation each time


    public static bool IsNullableType(this ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated ||
               (type is INamedTypeSymbol { IsGenericType: true } namedType &&
                namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDbConnection(this ISymbol typeSymbol) =>
        IsTypeInHierarchy(typeSymbol, "DbConnection");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDbTransaction(this ISymbol typeSymbol) =>
        IsTypeInHierarchy(typeSymbol, "DbTransaction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDbContext(this ISymbol typeSymbol) =>
        IsTypeInHierarchy(typeSymbol, "DbContext");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeInHierarchy(ISymbol symbol, string targetTypeName)
    {
        // Get the type from the symbol (field, property, or type itself)
        ITypeSymbol? type = symbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IParameterSymbol parameter => parameter.Type,
            ITypeSymbol typeSymbol => typeSymbol,
            _ => null
        };

        if (type == null) return false;

        while (type != null)
        {
            if (type.Name.Contains(targetTypeName)) return true;
            type = type.BaseType!;
        }
        return false;
    }

    private static bool IsTaskType(this ITypeSymbol typeSymbol) =>
        typeSymbol.Name == "Task" && typeSymbol.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

    public static ITypeSymbol? GetTaskResultType(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType &&
        namedType.IsTaskType() && namedType.TypeArguments.Length == 1
            ? namedType.TypeArguments[0] : null;

    private static INamedTypeSymbol? FindGenericInterface(this ITypeSymbol typeSymbol, string interfaceName)
    {
        return typeSymbol.AllInterfaces.FirstOrDefault(i =>
            i.Name == interfaceName &&
            i.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic" &&
            i.TypeArguments.Length == 1);
    }

    public static bool IsIEnumerable(this ITypeSymbol typeSymbol) =>
        typeSymbol.FindGenericInterface("IEnumerable") != null;

    public static ITypeSymbol? GetEnumerableElementType(this ITypeSymbol typeSymbol) =>
        typeSymbol.FindGenericInterface("IEnumerable")?.TypeArguments[0];

    public static bool IsList(this ITypeSymbol typeSymbol) =>
        typeSymbol.FindGenericInterface("IList") != null;

    public static bool IsArray(this ITypeSymbol typeSymbol) => typeSymbol.TypeKind == TypeKind.Array;

    public static string GetSqlName(this ISymbol propertySymbol)
    {
        var columnAttr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute");

        if (columnAttr?.ConstructorArguments.FirstOrDefault().Value is string columnName)
        {
            return columnName;
        }

        var tableNameAttr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableNameAttribute");

        if (tableNameAttr?.ConstructorArguments.FirstOrDefault().Value is string tableName)
        {
            return tableName;
        }

        return propertySymbol.Name;
    }

    public static string GetDbType(this ITypeSymbol type) =>
        type.Name switch
        {
            "String" => "global::System.Data.DbType.String",
            "Int32" => "global::System.Data.DbType.Int32",
            "Int64" => "global::System.Data.DbType.Int64",
            "Double" => "global::System.Data.DbType.Double",
            "Decimal" => "global::System.Data.DbType.Decimal",
            "Boolean" => "global::System.Data.DbType.Boolean",
            "DateTime" => "global::System.Data.DbType.DateTime",
            "Guid" => "global::System.Data.DbType.Guid",
            "Byte" => "global::System.Data.DbType.Byte",
            "Int16" => "global::System.Data.DbType.Int16",
            "Single" => "global::System.Data.DbType.Single",
            _ => "global::System.Data.DbType.String"
        };

    public static string? GetDataReaderMethod(this ITypeSymbol type)
    {
        return type.Name switch
        {
            "String" => "GetString",
            "Int32" => "GetInt32",
            "Int64" => "GetInt64",
            "Double" => "GetDouble",
            "Decimal" => "GetDecimal",
            "Boolean" => "GetBoolean",
            "DateTime" => "GetDateTime",
            "Guid" => "GetGuid",
            "Byte" => "GetByte",
            "SByte" => "GetByte",
            "Int16" => "GetInt16",
            "UInt16" => "GetInt16",
            "UInt32" => "GetInt32",
            "UInt64" => "GetInt64",
            "Single" => "GetFloat",
            "Char" => "GetChar",
            _ => null
        };
    }

    public static string GetParameterName(this ISymbol symbol, string prefix = "") => prefix + NameMapper.MapName(symbol.Name);

    public static bool IsScalarType(this ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean or
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64 or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Decimal or
            SpecialType.System_Char or
            SpecialType.System_String or
            SpecialType.System_DateTime => true,
            _ => type.Name == "Guid" || type.Name == "TimeSpan"
        };
    }

    public static ITypeSymbol UnwrapNullableType(this ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0];
        }
        return type;
    }

    public static ITypeSymbol UnwrapTaskType(this ITypeSymbol type)
    {
        return type.GetTaskResultType() ?? type;
    }

    public static ITypeSymbol UnwrapListType(this ITypeSymbol type) =>
        type.GetEnumerableElementType() ?? type;

    public static bool IsCancellationToken(this ITypeSymbol type) =>
        type.Name == "CancellationToken" &&
        type.ContainingNamespace?.ToDisplayString() == "System.Threading";

    public static string GetAccessibility(this Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "private"
    };

    public static bool IsTuple(this ITypeSymbol type) =>
        type is INamedTypeSymbol namedType &&
        (namedType.IsTupleType ||
         (namedType.Name.StartsWith("ValueTuple") &&
          namedType.ContainingNamespace?.ToDisplayString() == "System"));

    public static string GetDataReadExpressionWithCachedOrdinal(this ITypeSymbol type, string readerName, string columnName, string ordinalVariableName) =>
        GetDataReadExpressionCore(type, readerName, ordinalVariableName, ordinalVariableName);

    private static string GetDataReadExpressionCore(ITypeSymbol type, string readerName, string accessor, string ordinalExpr)
    {
        var method = type.GetDataReaderMethod();
        var isNullable = type.IsNullableType();

        if (method == null)
        {
            return $"{readerName}[{accessor}]";
        }

        if (isNullable)
        {
            return $"{readerName}.IsDBNull({ordinalExpr}) ? null : {readerName}.{method}({accessor})";
        }

        // For non-nullable string types, return empty string instead of null
        if (type.SpecialType == SpecialType.System_String && type.NullableAnnotation == NullableAnnotation.NotAnnotated)
        {
            return $"{readerName}.IsDBNull({ordinalExpr}) ? string.Empty : {readerName}.{method}({accessor})";
        }

        return $"{readerName}.{method}({accessor})";
    }

    /// <summary>
    /// Determines if a type is a collection type (IEnumerable, List, Array, etc.)
    /// </summary>
    public static bool IsCollectionType(this ITypeSymbol type) =>
        type.IsArray() || type.IsIEnumerable() || type.IsList();

    /// <summary>
    /// 获取指定名称的属性（缓存结果以提升性能）
    /// </summary>
    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

    /// <summary>
    /// 获取Sqlx相关属性
    /// </summary>
    public static AttributeData? GetSqlxAttribute(this IMethodSymbol method) =>
        method.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name?.Contains("Sqlx") == true ||
            a.AttributeClass?.Name?.Contains("SqlTemplate") == true);

    /// <summary>
    /// 获取TableNameBy属性
    /// </summary>
    public static AttributeData? GetTableNameByAttribute(this ISymbol symbol) =>
        symbol.GetAttribute("TableNameByAttribute");

    /// <summary>
    /// 获取DbColumn属性
    /// </summary>
    public static AttributeData? GetDbColumnAttribute(this ISymbol symbol) =>
        symbol.GetAttribute("DbColumnAttribute");
}
