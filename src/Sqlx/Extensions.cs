// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

internal static class Extensions
{
    // Cache for frequently accessed type information to improve performance
    private static readonly ConcurrentDictionary<string, bool> _dbConnectionTypeCache = new();
    private static readonly ConcurrentDictionary<string, bool> _dbTransactionTypeCache = new();
    private static readonly ConcurrentDictionary<string, bool> _dbContextTypeCache = new();
    private static readonly ConcurrentDictionary<string, string?> _dataReaderMethodCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanHaveNullValue(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated ||
               (!typeSymbol.IsValueType && typeSymbol.NullableAnnotation != NullableAnnotation.NotAnnotated);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullableType(this ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated ||
               (type is INamedTypeSymbol { IsGenericType: true } namedType &&
                namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);
    }

    internal static bool IsDbConnection(this ISymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x => IsTypeInHierarchy(x, "DbConnection", _dbConnectionTypeCache));
    }

    internal static bool IsDbTransaction(this ISymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x => IsTypeInHierarchy(x, "DbTransaction", _dbTransactionTypeCache));
    }

    internal static bool IsDbContext(this ISymbol typeSymbol)
    {
        return IsTypes(typeSymbol, x => IsTypeInHierarchy(x, "DbContext", _dbContextTypeCache));
    }

    /// <summary>
    /// Optimized type hierarchy checking with caching to avoid repeated traversals
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeInHierarchy(ITypeSymbol type, string targetTypeName, ConcurrentDictionary<string, bool> cache)
    {
        var typeKey = type.ToDisplayString();
        if (cache.TryGetValue(typeKey, out var cachedResult))
        {
            return cachedResult;
        }

        var result = CheckTypeHierarchy(type, targetTypeName);
        cache.TryAdd(typeKey, result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckTypeHierarchy(ITypeSymbol? type, string targetTypeName)
    {
        while (type != null)
        {
            if (string.Equals(type.Name, targetTypeName, StringComparison.Ordinal))
                return true;
            type = type.BaseType;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsTypes(ISymbol typeSymbol, Func<ITypeSymbol, bool> check)
    {
        return typeSymbol switch
        {
            IParameterSymbol parameterSymbol => check(parameterSymbol.Type),
            IFieldSymbol fieldSymbol => check(fieldSymbol.Type),
            IPropertySymbol propertySymbol => check(propertySymbol.Type),
            INamedTypeSymbol namedTypeSymbol => check(namedTypeSymbol),
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsCancellationToken(this ISymbol typeSymbol) => 
        string.Equals(typeSymbol.Name, "CancellationToken", StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ITypeSymbol UnwrapTaskType(this ITypeSymbol type) => UnwrapType(type, "Task");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ITypeSymbol UnwrapListType(this ITypeSymbol type) => 
        UnwrapType(type, "List", "IList", "ICollection", "IReadonlyList", "IEnumerable", "IAsyncEnumerable");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ITypeSymbol UnwrapNullableType(this ITypeSymbol type)
        => type is INamedTypeSymbol namedTypeSymbol ? UnwrapNullableType(namedTypeSymbol) : type;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static INamedTypeSymbol UnwrapNullableType(INamedTypeSymbol namedTypeSymbol)
        => string.Equals(namedTypeSymbol.Name, "Nullable", StringComparison.Ordinal) 
            ? (INamedTypeSymbol)namedTypeSymbol.TypeArguments[0] 
            : namedTypeSymbol;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsScalarType(this ITypeSymbol returnType)
    {
        return UnwrapNullableType(UnwrapTaskType(returnType)).SpecialType switch
        {
            SpecialType.System_String or
            SpecialType.System_Boolean or
            SpecialType.System_Char or
            SpecialType.System_Byte or
            SpecialType.System_Int16 or
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt16 or
            SpecialType.System_UInt32 or
            SpecialType.System_UInt64 or
            SpecialType.System_DateTime or
            SpecialType.System_Decimal or
            SpecialType.System_Double => true,
            _ => false,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsTuple(ITypeSymbol returnType) => 
        string.Equals(returnType.Name, "Tuple", StringComparison.Ordinal) || 
        string.Equals(returnType.Name, "ValueTuple", StringComparison.Ordinal);

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
        var typeKey = type.ToDisplayString();
        if (_dataReaderMethodCache.TryGetValue(typeKey, out var cachedMethod))
        {
            return cachedMethod;
        }

        var method = GetDataReaderMethodCore(type);
        _dataReaderMethodCache.TryAdd(typeKey, method);
        return method;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? GetDataReaderMethodCore(ITypeSymbol type)
    {
        var unwrapType = UnwrapNullableType(type);
        
        // Handle Guid specially first
        if (string.Equals(type.Name, "Guid", StringComparison.Ordinal))
            return "GetGuid";

        return unwrapType.SpecialType switch
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
        var method = GetDataReaderMethod(type);
        if (string.IsNullOrEmpty(method))
        {
            throw new NotSupportedException($"No support type {type.Name}");
        }

        var ordinalExpression = $"{readerName}.GetOrdinal(\"{columnName}\")";
        var isNullable = IsNullableType(type);
        var unwrapType = UnwrapNullableType(type);

        // Generate optimized null-safe data reading expression
        return GenerateNullSafeDataReadExpression(readerName, ordinalExpression, method!, isNullable, unwrapType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GenerateNullSafeDataReadExpression(string readerName, string ordinalExpression, string method, bool isNullable, ITypeSymbol unwrapType)
    {
        // For non-nullable value types that don't need null checking
        if (!isNullable && unwrapType.IsValueType && unwrapType.SpecialType != SpecialType.System_String)
        {
            return $"{readerName}.{method}({ordinalExpression})";
        }

        // Generate null-safe expression based on type characteristics
        var nullValue = GetNullValueForType(isNullable, unwrapType);
        return $"{readerName}.IsDBNull({ordinalExpression}) ? {nullValue} : {readerName}.{method}({ordinalExpression})";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetNullValueForType(bool isNullable, ITypeSymbol unwrapType)
    {
        return (isNullable, unwrapType.SpecialType) switch
        {
            (true, SpecialType.System_String) => "null",
            (false, SpecialType.System_String) => "string.Empty",
            (true, _) when unwrapType.IsValueType => "null",
            (false, _) when unwrapType.IsValueType => "default",
            _ => "null"
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ITypeSymbol UnwrapType(this ITypeSymbol type, params string[] names)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && 
            names.Any(name => string.Equals(type.Name, name, StringComparison.Ordinal)))
        {
            return namedTypeSymbol.TypeArguments[0];
        }

        return type;
    }
}
