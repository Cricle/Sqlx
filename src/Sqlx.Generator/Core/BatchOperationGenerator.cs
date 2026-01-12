// -----------------------------------------------------------------------
// <copyright file="BatchOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

using Microsoft.CodeAnalysis;
using Sqlx.SqlGen;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 批量操作SQL生成器 - 处理批量INSERT/UPDATE/DELETE操作
/// </summary>
internal static class BatchOperationGenerator
{
    private const string SqlAnd = " AND ";
    private const string CommaSpace = ", ";

    /// <summary>
    /// 生成原生DbBatch批量操作逻辑
    /// </summary>
    public static void GenerateNativeDbBatch(
        IndentedStringBuilder sb,
        IParameterSymbol collectionParam,
        ReturnTypes returnType,
        SqlDefine sqlDef,
        string tableName,
        string operationType,
        ISymbol? transactionParameter,
        bool isAsync)
    {
        var objectMap = new ObjectMap(collectionParam);
        var properties = objectMap.Properties;

        if (returnType == ReturnTypes.Scalar)
            sb.AppendLine("int totalAffectedRows = 0;");

        sb.AppendLine("using var batch = dbConn.CreateBatch();");
        if (transactionParameter != null)
            sb.AppendLine($"batch.Transaction = {transactionParameter.Name};");
        sb.AppendLine();

        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var batchCommand = batch.CreateBatchCommand();");

        GenerateBatchCommandSql(sb, operationType, tableName, properties, sqlDef);
        GenerateBatchParameters(sb, properties, sqlDef, "batchCommand");

        sb.AppendLine("batch.BatchCommands.Add(batchCommand);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        GenerateBatchExecute(sb, returnType, isAsync);
    }

    /// <summary>
    /// 生成回退批量操作逻辑（逐条执行）
    /// </summary>
    public static void GenerateFallbackBatch(
        IndentedStringBuilder sb,
        IParameterSymbol collectionParam,
        ReturnTypes returnType,
        SqlDefine sqlDef,
        string tableName,
        string operationType,
        string cmdName,
        ISymbol? transactionParameter,
        string? timeoutExpression,
        bool isAsync)
    {
        var objectMap = new ObjectMap(collectionParam);
        var properties = objectMap.Properties;

        if (returnType == ReturnTypes.Scalar)
            sb.AppendLine("int totalAffectedRows = 0;");

        sb.AppendLine($"var __columns__ = \"{string.Join(CommaSpace, properties.Select(p => sqlDef.WrapColumn(p.Name)))}\";");
        sb.AppendLine($"var __values__ = \"{string.Join(CommaSpace, properties.Select(p => sqlDef.ParameterPrefix + p.GetParameterName(string.Empty)))}\";");
        sb.AppendLine();

        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        GenerateFallbackCommandSql(sb, operationType, tableName, properties, sqlDef, cmdName);

        sb.AppendLine($"{cmdName}.Parameters.Clear();");

        if (transactionParameter != null)
            sb.AppendLine($"{cmdName}.Transaction = {transactionParameter.Name};");
        if (!string.IsNullOrWhiteSpace(timeoutExpression))
            sb.AppendLine($"{cmdName}.CommandTimeout = {timeoutExpression};");

        GenerateBatchParameters(sb, properties, sqlDef, cmdName);
        GenerateFallbackExecute(sb, returnType, cmdName, isAsync);

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        if (returnType == ReturnTypes.Scalar)
            sb.AppendLine("return totalAffectedRows;");
    }

    private static void GenerateBatchCommandSql(
        IndentedStringBuilder sb,
        string operationType,
        string tableName,
        List<IPropertySymbol> properties,
        SqlDefine sqlDef)
    {
        var wrappedTable = EscapeForSqlString(sqlDef.WrapColumn(tableName));

        switch (operationType)
        {
            case "INSERT":
                var columns = string.Join(CommaSpace, properties.Select(p => EscapeForSqlString(sqlDef.WrapColumn(p.GetSqlName()))));
                var values = string.Join(CommaSpace, properties.Select(p => sqlDef.ParameterPrefix + p.GetSqlName()));
                sb.AppendLine($"batchCommand.CommandText = \"INSERT INTO {wrappedTable} ({columns}) VALUES ({values})\";");
                break;

            case "UPDATE":
                var (setProps, whereProps) = GetUpdateProperties(properties);
                if (setProps.Count == 0 || whereProps.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(
                        setProps.Count == 0 ? SqlxExceptionMessages.NoSetProperties : SqlxExceptionMessages.NoWherePropertiesUpdate));
                    return;
                }
                var setClause = string.Join(CommaSpace, setProps.Select(p => $"{EscapeForSqlString(sqlDef.WrapColumn(p.GetSqlName()))} = {sqlDef.ParameterPrefix}{p.GetSqlName()}"));
                var whereClause = string.Join(SqlAnd, whereProps.Select(p => $"{EscapeForSqlString(sqlDef.WrapColumn(p.GetSqlName()))} = {sqlDef.ParameterPrefix}{p.GetSqlName()}"));
                sb.AppendLine($"batchCommand.CommandText = \"UPDATE {wrappedTable} SET {setClause} WHERE {whereClause}\";");
                break;

            case "DELETE":
                var deleteWhereProps = GetWhereProperties(properties);
                if (deleteWhereProps.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoWherePropertiesDelete));
                    return;
                }
                var deleteWhereClause = string.Join(SqlAnd, deleteWhereProps.Select(p => $"{EscapeForSqlString(sqlDef.WrapColumn(p.GetSqlName()))} = {sqlDef.ParameterPrefix}{p.GetSqlName()}"));
                sb.AppendLine($"batchCommand.CommandText = \"DELETE FROM {wrappedTable} WHERE {deleteWhereClause}\";");
                break;

            default:
                goto case "INSERT";
        }
    }

    private static void GenerateFallbackCommandSql(
        IndentedStringBuilder sb,
        string operationType,
        string tableName,
        List<IPropertySymbol> properties,
        SqlDefine sqlDef,
        string cmdName)
    {
        var wrappedTable = sqlDef.WrapColumn(tableName);

        switch (operationType)
        {
            case "INSERT":
                sb.AppendLine($"{cmdName}.CommandText = \"INSERT INTO {wrappedTable} (\" + __columns__ + \") VALUES (\" + __values__ + \")\";");
                break;

            case "UPDATE":
                var (setProps, whereProps) = GetUpdateProperties(properties);
                if (setProps.Count == 0 || whereProps.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(
                        setProps.Count == 0 ? SqlxExceptionMessages.NoSetProperties : SqlxExceptionMessages.NoWherePropertiesUpdate));
                    return;
                }
                var setClause = string.Join(CommaSpace, setProps.Select(p => $"{sqlDef.WrapColumn(p.Name)} = {sqlDef.ParameterPrefix}{p.GetParameterName(string.Empty)}"));
                var whereClause = string.Join(SqlAnd, whereProps.Select(p => $"{sqlDef.WrapColumn(p.Name)} = {sqlDef.ParameterPrefix}{p.GetParameterName(string.Empty)}"));
                sb.AppendLine($"{cmdName}.CommandText = \"UPDATE {wrappedTable} SET {setClause} WHERE {whereClause}\";");
                break;

            case "DELETE":
                var deleteWhereProps = GetWhereProperties(properties);
                if (deleteWhereProps.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoWherePropertiesDelete));
                    return;
                }
                var deleteWhereClause = string.Join(SqlAnd, deleteWhereProps.Select(p => $"{sqlDef.WrapColumn(p.Name)} = {sqlDef.ParameterPrefix}{p.GetParameterName(string.Empty)}"));
                sb.AppendLine($"{cmdName}.CommandText = \"DELETE FROM {wrappedTable} WHERE {deleteWhereClause}\";");
                break;

            default:
                goto case "INSERT";
        }
    }

    private static void GenerateBatchParameters(
        IndentedStringBuilder sb,
        List<IPropertySymbol> properties,
        SqlDefine sqlDef,
        string cmdName)
    {
        foreach (var prop in properties)
        {
            var paramVar = $"param_{prop.Name.ToLowerInvariant()}";
            sb.AppendLine($"var {paramVar} = {cmdName}.CreateParameter();");
            sb.AppendLine($"{paramVar}.ParameterName = \"{sqlDef.ParameterPrefix}{prop.GetSqlName()}\";");
            sb.AppendLine($"{paramVar}.DbType = {prop.Type.GetDbType()};");
            sb.AppendLine($"{paramVar}.Value = (object?)item.{prop.Name} ?? global::System.DBNull.Value;");
            sb.AppendLine($"{cmdName}.Parameters.Add({paramVar});");
        }
    }

    private static void GenerateBatchExecute(IndentedStringBuilder sb, ReturnTypes returnType, bool isAsync)
    {
        var executeMethod = isAsync ? "await batch.ExecuteNonQueryAsync()" : "batch.ExecuteNonQuery()";

        if (returnType == ReturnTypes.Scalar)
        {
            sb.AppendLine($"totalAffectedRows = {executeMethod};");
            sb.AppendLine("return totalAffectedRows;");
        }
        else
        {
            sb.AppendLine($"{executeMethod};");
        }
    }

    private static void GenerateFallbackExecute(IndentedStringBuilder sb, ReturnTypes returnType, string cmdName, bool isAsync)
    {
        var executeMethod = isAsync ? $"await {cmdName}.ExecuteNonQueryAsync()" : $"{cmdName}.ExecuteNonQuery()";

        if (returnType == ReturnTypes.Scalar)
            sb.AppendLine($"totalAffectedRows += {executeMethod};");
        else
            sb.AppendLine($"{executeMethod};");
    }

    private static (List<IPropertySymbol> setProps, List<IPropertySymbol> whereProps) GetUpdateProperties(List<IPropertySymbol> properties)
    {
        var whereProps = GetWhereProperties(properties);
        var whereSet = new HashSet<IPropertySymbol>(whereProps, SymbolEqualityComparer.Default);
        var setProps = properties.Where(p => !whereSet.Contains(p) && !IsKeyProperty(p)).ToList();
        return (setProps, whereProps);
    }

    private static List<IPropertySymbol> GetWhereProperties(List<IPropertySymbol> properties)
    {
        var explicitWhere = properties.Where(p => HasAttribute(p, "WhereAttribute")).ToList();
        return explicitWhere.Count > 0 ? explicitWhere : properties.Where(IsKeyProperty).ToList();
    }

    private static bool IsKeyProperty(IPropertySymbol property)
    {
        var name = property.Name.ToUpperInvariant();
        return name is "ID" or "KEY" || name.EndsWith("ID") || name.EndsWith("KEY");
    }

    private static bool HasAttribute(IPropertySymbol property, string attributeName) =>
        property.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);

    private static string EscapeForSqlString(string name) => name.Replace("\"", "\\\"");
}
