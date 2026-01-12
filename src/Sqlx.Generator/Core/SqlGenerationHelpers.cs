// -----------------------------------------------------------------------
// <copyright file="SqlGenerationHelpers.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

using Microsoft.CodeAnalysis;
using Sqlx.SqlGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// SQL生成辅助方法 - 提供通用的SQL生成功能
/// </summary>
internal static class SqlGenerationHelpers
{
    private static readonly Regex ParameterNameCleanupRegex = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);

    private static readonly Dictionary<SpecialType, string> TypeConversionMap = new()
    {
        { SpecialType.System_Int32, "global::System.Convert.ToInt32" },
        { SpecialType.System_Int64, "global::System.Convert.ToInt64" },
        { SpecialType.System_Boolean, "global::System.Convert.ToBoolean" },
        { SpecialType.System_Decimal, "global::System.Convert.ToDecimal" },
        { SpecialType.System_Double, "global::System.Convert.ToDouble" },
        { SpecialType.System_Single, "global::System.Convert.ToSingle" }
    };

    /// <summary>
    /// 为SQL字符串转义列名或表名
    /// </summary>
    public static string EscapeForSqlString(string name, SqlDefine sqlDef) =>
        sqlDef.WrapColumn(name).Replace("\"", "\\\"");

    /// <summary>
    /// 清理参数名称，移除非法字符
    /// </summary>
    public static string CleanParameterName(string name) =>
        ParameterNameCleanupRegex.Replace(name.TrimStart('@'), "_") + "_p";

    /// <summary>
    /// 获取类型转换表达式
    /// </summary>
    public static string GetTypeConversionExpression(ITypeSymbol returnType, string resultName)
    {
        if (TypeConversionMap.TryGetValue(returnType.SpecialType, out var converter))
            return $"{converter}({resultName})";

        if (returnType.SpecialType == SpecialType.System_String)
            return $"{resultName}?.ToString() ?? string.Empty";

        return $"({returnType.ToDisplayString()}){resultName}";
    }

    /// <summary>
    /// 生成参数空检查代码
    /// </summary>
    public static void GenerateParameterNullChecks(IndentedStringBuilder sb, IEnumerable<IParameterSymbol> parameters)
    {
        bool hasChecks = false;
        foreach (var param in parameters)
        {
            if (ShouldGenerateNullCheck(param))
            {
                if (!hasChecks)
                {
                    sb.AppendLine("// Parameter null checks (fail fast)");
                    hasChecks = true;
                }
                sb.AppendLine($"if ({param.Name} == null)");
                sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({param.Name}));");
            }
        }
        if (hasChecks) sb.AppendLine();
    }

    /// <summary>
    /// 生成参数声明代码
    /// </summary>
    public static (string parameterName, string variableName) DeclareParameter(
        IndentedStringBuilder sb,
        ISymbol symbol,
        ITypeSymbol type,
        string prefix,
        SqlDefine sqlDef,
        string cmdName)
    {
        var visitPath = string.IsNullOrEmpty(prefix) ? string.Empty : prefix.Replace(".", "?.") + "?.";
        var prefixForName = string.IsNullOrEmpty(prefix) ? string.Empty : prefix.Replace(".", "_");

        var sqlParamName = symbol.GetParameterName(sqlDef.ParameterPrefix + prefixForName);
        var varName = CleanParameterName(sqlParamName);
        var paramName = symbol.GetParameterName(sqlDef.ParameterPrefix);
        var dbType = type.GetDbType();

        sb.AppendLine($"global::System.Data.Common.DbParameter {varName} = {cmdName}.CreateParameter();");
        sb.AppendLine($"{varName}.ParameterName = \"{paramName}\";");
        sb.AppendLine($"{varName}.DbType = {dbType};");
        sb.AppendLine($"{varName}.Value = (object?){visitPath}{symbol.Name} ?? global::System.DBNull.Value;");

        WriteParameterSpecials(sb, symbol, varName);

        return (varName, paramName);
    }

    /// <summary>
    /// 生成列序号缓存代码
    /// </summary>
    public static void WriteCachedOrdinals(IndentedStringBuilder sb, List<string> columnNames, string readerName)
    {
        foreach (var columnName in columnNames)
            sb.AppendLine($"int __ordinal_{columnName} = {readerName}.GetOrdinal(\"{columnName}\");");
    }

    /// <summary>
    /// 获取列名列表
    /// </summary>
    public static List<string> GetColumnNames(ITypeSymbol returnType, Func<ITypeSymbol, List<IPropertySymbol>> getProperties)
    {
        var columnNames = new List<string>(10);

        if (returnType.IsCachedScalarType())
        {
            columnNames.Add("Column0");
        }
        else if (returnType.IsTuple())
        {
            var tupleType = (INamedTypeSymbol)returnType.UnwrapTaskType();
            if (tupleType.TupleElements != null && tupleType.TupleElements.Length > 0)
                columnNames.AddRange(tupleType.TupleElements.Select(e => e.Name));
            else
                columnNames.AddRange(tupleType.TypeArguments.Select((_, i) => $"Column{i}"));
        }
        else
        {
            columnNames.AddRange(getProperties(returnType).Select(p => p.GetSqlName()));
        }

        return columnNames;
    }

    /// <summary>
    /// 从方法名推断操作类型
    /// </summary>
    public static string InferOperationType(string methodName)
    {
        var upper = methodName.ToUpperInvariant();

        if (upper.Contains("INSERT") || upper.Contains("ADD") || upper.Contains("CREATE"))
            return "INSERT";
        if (upper.Contains("UPDATE") || upper.Contains("MODIFY") || upper.Contains("CHANGE"))
            return "UPDATE";
        if (upper.Contains("DELETE") || upper.Contains("REMOVE"))
            return "DELETE";

        throw new InvalidOperationException($"Cannot infer operation type from method name '{methodName}'. Use explicit SqlExecuteType attribute.");
    }

    /// <summary>
    /// 查找数据库连接成员
    /// </summary>
    public static ISymbol? FindConnectionMember(INamedTypeSymbol repositoryClass)
    {
        var allMembers = repositoryClass.GetMembers().ToArray();

        // 优先查找字段
        var field = allMembers.OfType<IFieldSymbol>().FirstOrDefault(x => x.IsDbConnection());
        if (field != null) return field;

        // 然后查找属性
        var property = allMembers.OfType<IPropertySymbol>().FirstOrDefault(x => x.IsDbConnection());
        if (property != null) return property;

        // 最后查找构造函数参数
        var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
        var param = constructor?.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
        if (param != null) return param;

        return null;
    }

    /// <summary>
    /// 从连接类型名推断SQL方言
    /// </summary>
    public static SqlDefine? InferDialectFromConnectionTypeName(string connectionTypeName)
    {
        var lower = connectionTypeName.ToLowerInvariant();
        return lower switch
        {
            var n when n.Contains("sqlite") => SqlDefine.SQLite,
            var n when n.Contains("mysql") || n.Contains("mariadb") => SqlDefine.MySql,
            var n when n.Contains("postgres") || n.Contains("npgsql") => SqlDefine.PostgreSql,
            var n when n.Contains("sqlserver") || n.Contains("sqlconnection") => SqlDefine.SqlServer,
            _ => null
        };
    }

    private static bool ShouldGenerateNullCheck(IParameterSymbol parameter)
    {
        var typeName = parameter.Type.ToDisplayString();

        // 跳过系统参数
        if (typeName == "CancellationToken" ||
            typeName.Contains("DbTransaction") || typeName.Contains("IDbTransaction") ||
            typeName.Contains("DbConnection") || typeName.Contains("IDbConnection"))
            return false;

        // 跳过特殊属性参数
        if (parameter.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "TimeoutAttribute" or "ExpressionToSqlAttribute" or "ExpressionToSql"))
            return false;

        // 检查引用类型
        if (parameter.Type.IsReferenceType)
        {
            if (parameter.Type.SpecialType == SpecialType.System_String)
                return false;

            if (parameter.Type is INamedTypeSymbol namedType)
            {
                var baseName = namedType.Name;
                if (baseName is "IEnumerable" or "List" or "IList" or "ICollection" ||
                    (namedType.IsGenericType && namedType.TypeArguments.Length > 0))
                    return true;
            }

            if (parameter.Type.TypeKind == TypeKind.Class &&
                !parameter.Type.ToDisplayString().StartsWith("System."))
                return true;
        }

        return false;
    }

    private static void WriteParameterSpecials(IndentedStringBuilder sb, ISymbol symbol, string varName)
    {
        var columnDefine = symbol.GetDbColumnAttribute();
        if (columnDefine == null) return;

        var map = columnDefine.NamedArguments.ToDictionary(x => x.Key, x => x.Value.Value!);

        if (map.TryGetValue("Precision", out var precision))
            sb.AppendLine($"{varName}.Precision = {precision};");
        if (map.TryGetValue("Scale", out var scale))
            sb.AppendLine($"{varName}.Scale = {scale};");
        if (map.TryGetValue("Size", out var size))
            sb.AppendLine($"{varName}.Size = {size};");
        if (map.TryGetValue("Direction", out var direction))
            sb.AppendLine($"{varName}.Direction = global::System.Data.ParameterDirection.{direction};");
        else if (symbol is IParameterSymbol parSymbol)
        {
            if (parSymbol.RefKind == RefKind.Ref)
                sb.AppendLine($"{varName}.Direction = global::System.Data.ParameterDirection.InputOutput;");
            else if (parSymbol.RefKind == RefKind.Out)
                sb.AppendLine($"{varName}.Direction = global::System.Data.ParameterDirection.Output;");
        }
    }
}
