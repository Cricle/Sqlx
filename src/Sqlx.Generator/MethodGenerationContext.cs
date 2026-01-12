// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sqlx.SqlGen;
using Sqlx.Generator;
using Sqlx.Generator.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using static Sqlx.Generator.SharedCodeGenerationUtilities;

internal partial class MethodGenerationContext : GenerationContextBase
{
    // 常量定义
    private const string SqlInsert = "INSERT";
    private const string SqlUpdate = "UPDATE";
    private const string SqlDelete = "DELETE";
    private const string SqlAnd = " AND ";
    private const string CommaSpace = ", ";

    internal const string DbConnectionName = Constants.GeneratedVariables.Connection;
    internal const string CmdName = "__cmd__";
    internal const string DbReaderName = "__reader__";
    internal const string ResultName = "__result__";
    internal const string DataName = "__data__";
    internal const string StartTimeName = "__startTime__";
    internal const string MethodExecuting = "OnExecuting";
    internal const string MethodExecuted = "OnExecuted";
    internal const string MethodExecuteFail = "OnExecuteFail";
    internal const string GetTimestampMethod = "global::System.Diagnostics.Stopwatch.GetTimestamp()";

    private static readonly Regex ParameterNameCleanupRegex = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);

    // 类型转换映射
    private static readonly Dictionary<SpecialType, string> TypeConversionMap = new()
    {
        { SpecialType.System_Int32, "global::System.Convert.ToInt32" },
        { SpecialType.System_Int64, "global::System.Convert.ToInt64" },
        { SpecialType.System_Boolean, "global::System.Convert.ToBoolean" },
        { SpecialType.System_Decimal, "global::System.Convert.ToDecimal" },
        { SpecialType.System_Double, "global::System.Convert.ToDouble" },
        { SpecialType.System_Single, "global::System.Convert.ToSingle" }
    };

    // 属性缓存
    private readonly Dictionary<IPropertySymbol, ImmutableArray<AttributeData>> _propertyAttributeCache = new(SymbolEqualityComparer.Default);

    #region 构造函数和属性

    internal MethodGenerationContext(ClassGenerationContext classGenerationContext, IMethodSymbol methodSymbol)
    {
        ClassGenerationContext = classGenerationContext;
        MethodSymbol = methodSymbol;

        if (methodSymbol.Parameters.IsDefault)
        {
            SqlParameters = ImmutableArray<IParameterSymbol>.Empty;
            CancellationTokenKey = "default(global::System.Threading.CancellationToken)";
            IsAsync = false;
            AsyncKey = AwaitKey = string.Empty;
            SqlDef = SqlDefine.MySql;
            DeclareReturnType = ReturnTypes.Scalar;
            return;
        }

        CancellationTokenParameter = GetParameter(methodSymbol, x => x.Type.IsCancellationToken());

        // 解析 SqlxAttribute
        var rawSqlIsInParameter = true;
        var rawSqlShouldRemoveFromParams = false;
        var rawSqlParam = GetAttributeParameter(methodSymbol, "SqlxAttribute");

        if (rawSqlParam != null)
        {
            RawSqlParameter = rawSqlParam;
            var attr = rawSqlParam.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlxAttribute");
            rawSqlShouldRemoveFromParams = attr?.ConstructorArguments.Length > 0;
        }
        else if (MethodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "SqlxAttribute"))
        {
            rawSqlIsInParameter = false;
            RawSqlParameter = methodSymbol;
        }

        TimeoutParameter = GetAttributeParameter(methodSymbol, "TimeoutAttribute");
        ReaderHandlerParameter = GetAttributeParameter(methodSymbol, "ReadHandlerAttribute");
        ExpressionToSqlParameter = GetAttributeParameter(methodSymbol, "ExpressionToSqlAttribute");

        // 过滤SQL参数
        var parameters = methodSymbol.Parameters;
        RemoveIfExists(ref parameters, DbContext);
        RemoveIfExists(ref parameters, DbConnection);
        RemoveIfExists(ref parameters, TransactionParameter);
        if (rawSqlIsInParameter && rawSqlShouldRemoveFromParams) RemoveIfExists(ref parameters, RawSqlParameter);
        RemoveIfExists(ref parameters, CancellationTokenParameter);
        RemoveIfExists(ref parameters, TimeoutParameter);
        RemoveIfExists(ref parameters, ExpressionToSqlParameter);
        SqlParameters = parameters;

        DeclareReturnType = GetReturnType();
        CancellationTokenKey = CancellationTokenParameter?.Name ?? "default(global::System.Threading.CancellationToken)";
        IsAsync = MethodSymbol.ReturnType?.Name == "Task" || MethodSymbol.ReturnType?.Name == Constants.TypeNames.IAsyncEnumerable;
        SqlDef = GetSqlDefine();
        AsyncKey = IsAsync ? "async " : string.Empty;
        AwaitKey = IsAsync ? "await " : string.Empty;
    }

    public IMethodSymbol MethodSymbol { get; }
    internal ReturnTypes DeclareReturnType { get; }
    public ClassGenerationContext ClassGenerationContext { get; }
    internal override ISymbol? DbConnection => GetParameter(MethodSymbol, x => x.Type.IsDbConnection()) ?? ClassGenerationContext.DbConnection;
    internal override ISymbol? DbContext => GetParameter(MethodSymbol, x => x.Type.IsDbContext()) ?? ClassGenerationContext.DbContext;
    internal override ISymbol? TransactionParameter => GetParameter(MethodSymbol, x => x.Type.IsDbTransaction()) ?? ClassGenerationContext.TransactionParameter;
    internal ISymbol? RawSqlParameter { get; }
    internal ISymbol? CancellationTokenParameter { get; }
    internal IParameterSymbol? TimeoutParameter { get; }
    internal IParameterSymbol? ReaderHandlerParameter { get; }
    internal IParameterSymbol? ExpressionToSqlParameter { get; }
    internal ImmutableArray<IParameterSymbol> SqlParameters { get; }
    public ITypeSymbol ReturnType => MethodSymbol.ReturnType?.UnwrapTaskType() ?? throw new InvalidOperationException("Method symbol has no return type");
    internal ITypeSymbol ElementType => ReturnType.UnwrapListType();
    internal string AsyncKey { get; }
    internal string AwaitKey { get; }
    internal string CancellationTokenKey { get; }
    public bool IsAsync { get; }
    public bool ReturnIsEnumerable => ReturnType.Name == "IEnumerable" || ReturnType.Name == Constants.TypeNames.IAsyncEnumerable;
    private SqlDefine SqlDef { get; }
    private string MethodNameString => $"\"{MethodSymbol.Name}\"";

    #endregion

    #region 主要生成方法

    public bool DeclareCommand(IndentedStringBuilder sb)
    {
        // 生成方法签名
        var args = string.Join(CommaSpace, MethodSymbol.Parameters.Select(x =>
        {
            var syntax = (ParameterSyntax)x.DeclaringSyntaxReferences[0].GetSyntax();
            var prefix = string.Join(" ", syntax.Modifiers.Select(y => y.ToString()));
            if (syntax.Modifiers.Count != 0) prefix += " ";
            return prefix + x.Type.ToDisplayString() + " " + x.Name;
        }));

        var staticKeyword = MethodSymbol.IsStatic ? "static " : string.Empty;
        sb.AppendLine($"{MethodSymbol.DeclaredAccessibility.GetAccessibility()} {AsyncKey}{staticKeyword}partial {MethodSymbol.ReturnType.ToDisplayString()} {MethodSymbol.Name}({args})");
        sb.AppendLine("{");
        sb.PushIndent();

        // 获取数据库连接
        var dbConnectionExpression = GetDbConnectionExpression();
        sb.AppendLine($"global::System.Data.Common.DbConnection {DbConnectionName} = {dbConnectionExpression} ?? ");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(\"{dbConnectionExpression}\");");

        SqlGenerationHelpers.GenerateParameterNullChecks(sb, SqlParameters);

        // 打开连接
        sb.AppendLine($"if({DbConnectionName}.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLineIf(IsAsync, $"await {DbConnectionName}.OpenAsync({CancellationTokenKey});", $"{DbConnectionName}.Open();");
        sb.PopIndent();
        sb.AppendLine("}");

        // 创建命令
        sb.AppendLine($"using (var {CmdName} = {DbConnectionName}.CreateCommand())");
        sb.AppendLine("{");
        sb.PushIndent();

        if (TransactionParameter != null)
            sb.AppendLine($"{CmdName}.Transaction = {TransactionParameter.Name};");

        var timeoutExpression = GetTimeoutExpression();
        if (!string.IsNullOrWhiteSpace(timeoutExpression))
            sb.AppendLine($"{CmdName}.CommandTimeout = {timeoutExpression};");

        // 获取SQL
        var sql = GetSql();
        if (string.IsNullOrEmpty(sql))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
            return false;
        }

        if (sql?.Contains("BATCH") == true)
        {
            sb.AppendLine($"// Legacy batch operation detected: {sql}");
            sb.AppendLine(SqlxExceptionMessages.GenerateArgumentExceptionThrow(SqlxExceptionMessages.LegacyBatchSql));
            sb.PopIndent();
            sb.AppendLine("}");
            return true;
        }

        // 设置SQL命令文本
        if (ExpressionToSqlParameter != null && string.IsNullOrEmpty(sql))
        {
            GenerateExpressionToSqlCommand(sb);
        }
        else if (!string.IsNullOrEmpty(sql))
        {
            if (sql?.Contains("{{VALUES_PLACEHOLDER}}") == true)
                GenerateBatchInsertSql(sb, sql);
            else
                sb.AppendLine($"{CmdName}.CommandText = {sql};");
        }

        sb.AppendLine();

        // 绑定参数
        var columnDefines = new List<ColumnDefine>(MethodSymbol.Parameters.Length);
        var isBatchInsert = sql?.Contains("{{VALUES_PLACEHOLDER}}") == true;

        if (!isBatchInsert)
            BindParameters(sb, columnDefines);

        // 执行计时
        sb.AppendLine($"global::System.Int64 {StartTimeName} = {GetTimestampMethod};");

        if (!ReturnIsEnumerable)
        {
            sb.AppendLine("try");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{MethodExecuting}({MethodNameString}, {CmdName});");
        }

        // 执行并返回结果
        var success = ExecuteAndReturn(sb, columnDefines, sql);
        if (!success) return false;

        // 异常处理
        if (!ReturnIsEnumerable)
        {
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("catch (global::System.Exception ex)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{MethodExecuteFail}({MethodNameString}, {CmdName}, ex, {GetTimestampMethod} - {StartTimeName});");
            sb.AppendLine("throw;");
            sb.PopIndent();
            sb.AppendLine("}");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
        return true;
    }

    #endregion

    #region 执行方法

    private bool ExecuteAndReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines, string? sql)
    {
        if (DeclareReturnType == ReturnTypes.SqlTemplate)
            return GenerateSqlTemplateReturn(sb, sql);

        if (DeclareReturnType == ReturnTypes.Void)
        {
            sb.AppendLineIf(IsAsync, $"await {CmdName}.ExecuteNonQueryAsync();", $"{CmdName}.ExecuteNonQuery();");
            if (!ReturnIsEnumerable) WriteMethodExecuted(sb, "null");
            return true;
        }

        if (IsExecuteNoQuery())
            return WriteExecuteNoQuery(sb, columnDefines);

        if (ReturnType.IsCachedScalarType())
        {
            WriteScalar(sb, columnDefines);
            return true;
        }

        return WriteReturn(sb, columnDefines);
    }

    public bool WriteExecuteNoQuery(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        if (ReturnType.SpecialType != SpecialType.System_Int32 && ReturnType.SpecialType != SpecialType.System_Boolean)
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0008, MethodSymbol.Locations.FirstOrDefault()));
            return false;
        }

        sb.AppendLineIf(IsAsync, $"var {ResultName} = await {CmdName}.ExecuteNonQueryAsync();", $"var {ResultName} = {CmdName}.ExecuteNonQuery();");
        WriteOutput(sb, columnDefines);
        WriteMethodExecuted(sb, ResultName);

        sb.AppendLine(ReturnType.SpecialType == SpecialType.System_Boolean ? $"return {ResultName} > 0;" : $"return {ResultName};");
        return true;
    }

    public void WriteScalar(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        sb.AppendLineIf(IsAsync, $"var {ResultName} = await {CmdName}.ExecuteScalarAsync();", $"var {ResultName} = {CmdName}.ExecuteScalar();");
        WriteOutput(sb, columnDefines);
        if (!ReturnIsEnumerable) WriteMethodExecuted(sb, ResultName);

        var canReturnNull = ReturnType.IsNullableType() || (!ReturnType.IsValueType && ReturnType.NullableAnnotation == NullableAnnotation.Annotated);

        if (ReturnType.Name == "Nullable")
        {
            sb.AppendLine("try { ");
            WriteScalarReturn(sb, canReturnNull);
            sb.AppendLine("} catch (global::System.InvalidCastException) { return default; }");
        }
        else
        {
            WriteScalarReturn(sb, canReturnNull);
        }
    }

    private void WriteScalarReturn(IndentedStringBuilder sb, bool canReturnNull)
    {
        if (canReturnNull)
            sb.AppendLine($"if({ResultName} == null) return default;");
        else
            sb.AppendLine($"if({ResultName} == null) throw new global::System.InvalidOperationException(\"{SqlxExceptionMessages.SequenceEmpty}\");");

        sb.AppendLine($"return {SqlGenerationHelpers.GetTypeConversionExpression(ReturnType, ResultName)};");
    }

    #endregion

    #region 返回值处理

    public bool WriteReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var handler = GetHandlerInvoke();
        if (!string.IsNullOrEmpty(handler) || DbConnection != null)
        {
            var executeMethod = IsAsync ? $"await {CmdName}.ExecuteReaderAsync()" : $"{CmdName}.ExecuteReader()";
            sb.AppendLine($"using(global::System.Data.Common.DbDataReader {DbReaderName} = {executeMethod})");
            sb.AppendLine("{");
            sb.PushIndent();

            if (!string.IsNullOrEmpty(handler))
            {
                sb.AppendLine(handler!);
            }
            else if (DeclareReturnType is ReturnTypes.List or ReturnTypes.IEnumerable or ReturnTypes.IAsyncEnumerable)
            {
                WriteListReturn(sb, columnDefines);
            }
            else if (DeclareReturnType == ReturnTypes.ListDictionaryStringObject)
            {
                WriteDictionaryListReturn(sb, columnDefines);
            }
            else
            {
                WriteSingleObjectReturn(sb, columnDefines);
            }

            sb.PopIndent();
            sb.AppendLine("}");
            return true;
        }

        return WriteEfCoreReturn(sb, columnDefines);
    }

    private void WriteListReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var returnType = ElementType.UnwrapNullableType();
        var isList = DeclareReturnType == ReturnTypes.List || (IsAsync && DeclareReturnType == ReturnTypes.IEnumerable);

        if (isList) WriteDeclareReturnList(sb);

        var isScalarList = returnType.IsCachedScalarType();
        if (!isScalarList)
        {
            var columnNames = SqlGenerationHelpers.GetColumnNames(returnType, GetPropertySymbols);
            SqlGenerationHelpers.WriteCachedOrdinals(sb, columnNames, DbReaderName);
        }

        WriteBeginReader(sb);

        if (isScalarList)
            WriteScalarListItem(sb, returnType, isList);
        else if (returnType.IsTupleType)
            WriteTupleListItem(sb, returnType, isList);
        else
        {
            WriteDeclareObjectExpression(sb, returnType, isList ? ResultName : null, GetPropertySymbols(returnType));
            if (!isList) sb.AppendLine($"yield return {DataName};");
        }

        WriteEndReader(sb);
        WriteOutput(sb, columnDefines);
        WriteMethodExecuted(sb, isList ? ResultName : "null");

        if (isList) sb.AppendLine($"return {ResultName};");
    }

    private void WriteScalarListItem(IndentedStringBuilder sb, ITypeSymbol returnType, bool isList)
    {
        var method = returnType.GetDataReaderMethod();
        var isNullable = returnType.IsNullableType();
        var addOrYield = isList ? $"{ResultName}.Add" : "yield return";

        if (method == null)
        {
            sb.AppendLine($"{addOrYield}(({returnType.ToDisplayString()}){DbReaderName}[0]);");
        }
        else if (isNullable)
        {
            sb.AppendLine($"{addOrYield}({DbReaderName}.IsDBNull(0) ? null : {DbReaderName}.{method}(0));");
        }
        else if (returnType.SpecialType == SpecialType.System_String && returnType.NullableAnnotation == NullableAnnotation.NotAnnotated)
        {
            sb.AppendLine($"{addOrYield}({DbReaderName}.IsDBNull(0) ? string.Empty : {DbReaderName}.{method}(0));");
        }
        else
        {
            sb.AppendLine($"{addOrYield}({DbReaderName}.{method}(0));");
        }
    }

    private void WriteTupleListItem(IndentedStringBuilder sb, ITypeSymbol returnType, bool isList)
    {
        var tupleType = (INamedTypeSymbol)returnType.UnwrapTaskType();
        var tupleArgs = tupleType.TypeArguments;
        var fieldNames = !tupleType.TupleElements.IsDefaultOrEmpty
            ? tupleType.TupleElements.Select(e => e.Name).ToArray()
            : tupleArgs.Select((_, i) => $"Column{i}").ToArray();

        var tupleJoins = string.Join(CommaSpace, tupleArgs.Select((x, i) =>
            x.GetDataReadExpressionWithCachedOrdinal(DbReaderName, fieldNames[i], $"__ordinal_{fieldNames[i]}")));

        sb.AppendLineIf(isList, $"{ResultName}.Add(({tupleJoins}));", $"yield return ({tupleJoins});");
    }

    private void WriteDictionaryListReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var mapType = "global::System.Collections.Generic.Dictionary<global::System.String,global::System.Object?>";
        var listType = $"global::System.Collections.Generic.List<{mapType}>";

        sb.AppendLine($"{listType} {ResultName} = new {listType}();");
        WriteBeginReader(sb);
        sb.AppendLine($"{mapType} {DataName} = new {mapType}({DbReaderName}.FieldCount);");
        sb.AppendLine($"for (int i = 0; i < {DbReaderName}.FieldCount; i++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{DataName}[{DbReaderName}.GetName(i)] = {DbReaderName}[i];");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine($"{ResultName}.Add({DataName});");
        WriteEndReader(sb);
        WriteOutput(sb, columnDefines);
        sb.AppendLine($"return {ResultName};");
    }

    private void WriteSingleObjectReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var returnType = ElementType.UnwrapNullableType();
        var writeableProperties = GetPropertySymbols(returnType);
        var isList = DeclareReturnType == ReturnTypes.List;

        if (isList) WriteDeclareReturnList(sb);

        var columnNames = SqlGenerationHelpers.GetColumnNames(returnType, GetPropertySymbols);
        SqlGenerationHelpers.WriteCachedOrdinals(sb, columnNames, DbReaderName);

        var readMethod = IsAsync ? $"ReadAsync({CancellationTokenKey})" : "Read()";
        var canReturnNull = ReturnType.IsNullableType() || (!ReturnType.IsValueType && ReturnType.NullableAnnotation == NullableAnnotation.Annotated);

        if (canReturnNull)
            sb.AppendLine($"if(!{AwaitKey}{DbReaderName}.{readMethod}) return default;");
        else
        {
            sb.AppendLine($"if(!{AwaitKey}{DbReaderName}.{readMethod}) throw new global::System.InvalidOperationException(\"Sequence contains no elements\");");
        }

        sb.AppendLine();
        WriteDeclareObjectExpression(sb, returnType, isList ? ResultName : null, writeableProperties);

        var selectName = isList ? ResultName : DataName;
        WriteOutput(sb, columnDefines);
        WriteMethodExecuted(sb, selectName);
        sb.AppendLine($"return {selectName};");
    }

    #endregion

    #region EF Core 返回

    private bool WriteEfCoreReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        ISymbol setSymbol = ElementType;
        if (GetDbSetElement(out var dbSetEle)) setSymbol = dbSetEle!;

        var fromSqlRawMethod = "global::Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.FromSqlRaw";
        var dbContextAccess = DbContext?.Name ?? "this.dbContext";
        var queryCall = $"{fromSqlRawMethod}({dbContextAccess}.Set<{setSymbol.ToDisplayString()}>(),{CmdName}.CommandText, {CmdName}.Parameters.OfType<global::System.Object>().ToArray())";

        var convert = GetEfCoreConvertExpression(setSymbol);
        var isTuple = ElementType.IsTupleType;

        if ((DeclareReturnType is ReturnTypes.IEnumerable or ReturnTypes.IAsyncEnumerable) && !IsAsync)
        {
            sb.AppendLine($"{AwaitKey}foreach (var item in {queryCall}{convert})");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("yield return item;");
            sb.PopIndent();
            sb.AppendLine("}");
            WriteOutput(sb, columnDefines);
        }
        else if (DeclareReturnType == ReturnTypes.IEnumerable && IsAsync)
        {
            queryCall = $"global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync({queryCall}{convert}, {CancellationTokenKey})";
            sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall};");
            WriteOutput(sb, columnDefines);
        }
        else
        {
            queryCall += convert;
            if (IsAsync)
            {
                var method = DeclareReturnType == ReturnTypes.List
                    ? $"global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync({queryCall}, {CancellationTokenKey})"
                    : $"global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync({queryCall}, {CancellationTokenKey})";
                sb.AppendLine($"var {ResultName} = {AwaitKey}{method};");
            }
            else
            {
                var callMethod = DeclareReturnType == ReturnTypes.List ? ".ToList()" : ".FirstOrDefault()";
                sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall}{callMethod};");
            }

            WriteOutput(sb, columnDefines);
            WriteEfCoreReturnStatement(sb);
        }

        return true;
    }

    private string GetEfCoreConvertExpression(ISymbol setSymbol)
    {
        if (SymbolEqualityComparer.Default.Equals(ElementType, setSymbol) || setSymbol is not INamedTypeSymbol setNameTypeSymbol)
            return string.Empty;

        var isTuple = ElementType.IsTupleType;
        string constructExpress;

        if (isTuple)
        {
            constructExpress = $"({string.Join(CommaSpace, ((INamedTypeSymbol)ElementType).TupleElements.Select(x => $"x.{x.Name}"))})";
        }
        else
        {
            var propertyTypes = setNameTypeSymbol.GetMembers().OfType<IPropertySymbol>().ToArray();
            var construct = ((INamedTypeSymbol)ElementType).Constructors.FirstOrDefault(x => x.Parameters.All(p => FindProperty(p, propertyTypes) != null));
            if (construct == null) return string.Empty;

            constructExpress = $"new {ElementType.ToDisplayString(NullableFlowState.None)}({string.Join(CommaSpace, construct.Parameters.Select(x => $"x.{FindProperty(x, propertyTypes)!.Name}"))})";

            var constructorParamNames = new HashSet<string>(construct.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var extraProps = ElementType.GetMembers().OfType<IPropertySymbol>().Where(p => !constructorParamNames.Contains(p.Name)).ToList();
            if (extraProps.Count != 0)
                constructExpress += $"{{ {string.Join(CommaSpace, extraProps.Select(x => $"{x.Name} = x.{x.Name}"))} }}";
        }

        return $".Select(x => {constructExpress})";
    }

    private static IPropertySymbol? FindProperty(IParameterSymbol symbol, IPropertySymbol[] propertyTypes) =>
        propertyTypes.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Type, symbol.Type) && x.Name.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase));

    private void WriteEfCoreReturnStatement(IndentedStringBuilder sb)
    {
        if (DeclareReturnType == ReturnTypes.List)
        {
            sb.AppendLine($"return {ResultName};");
            return;
        }

        var canReturnNull = ReturnType.IsNullableType() || (!ReturnType.IsValueType && ReturnType.NullableAnnotation == NullableAnnotation.Annotated);
        if (!canReturnNull)
            sb.AppendLine($"return {ResultName} ?? throw new global::System.InvalidOperationException(\"Sequence contains no elements\");");
        else
            sb.AppendLine($"return {ResultName};");
    }

    private bool GetDbSetElement(out ISymbol? symbol)
    {
        symbol = null;
        return false;
    }

    #endregion

    #region 辅助方法

    private string EscapeForSqlString(string name) => SqlDef.WrapColumn(name).Replace("\"", "\\\"");
    private string EscapePropertyForSqlString(IPropertySymbol property) => SqlDef.WrapColumn(GetCachedPropertySqlName(property)).Replace("\"", "\\\"");
    private string GetCachedPropertySqlName(IPropertySymbol property) => property.GetCachedSqlName();

    private ImmutableArray<AttributeData> GetCachedPropertyAttributes(IPropertySymbol property)
    {
        if (!_propertyAttributeCache.TryGetValue(property, out var attributes))
        {
            attributes = property.GetAttributes();
            _propertyAttributeCache[property] = attributes;
        }
        return attributes;
    }

    private bool HasAttributeByName(IPropertySymbol property, string attributeName) =>
        GetCachedPropertyAttributes(property).Any(attr => attr.AttributeClass?.Name == attributeName);

    private static void RemoveIfExists(ref ImmutableArray<IParameterSymbol> pars, ISymbol? symbol)
    {
        if (symbol is IParameterSymbol parameter) pars = pars.Remove(parameter);
    }

    private static ISymbol? GetParameter(IMethodSymbol methodSymbol, Func<IParameterSymbol, bool> check) =>
        methodSymbol.Parameters.FirstOrDefault(check);

    private static IParameterSymbol? GetAttributeParameter(IMethodSymbol methodSymbol, string attributeName) =>
        methodSymbol.Parameters.FirstOrDefault(x => x.GetAttributes().Any(y =>
            y.AttributeClass?.Name == attributeName || y.AttributeClass?.Name == attributeName.Replace("Attribute", "")));

    private bool IsExecuteNoQuery() =>
        MethodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "ExecuteNoQueryAttribute");

    private string GetDbConnectionExpression()
    {
        var hasRepositoryForAttribute = ClassGenerationContext.ClassSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name is "RepositoryForAttribute" or "RepositoryFor");

        if (hasRepositoryForAttribute)
            return GetDbConnectionFieldName(ClassGenerationContext.ClassSymbol);

        if (DbContext == null && DbConnection == null)
            return "global::Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetDbConnection(this.dbContext.Database)";

        var dbContextName = GetDbContextFieldName(ClassGenerationContext.ClassSymbol);
        return DbConnection == null
            ? $"global::Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetDbConnection({dbContextName}.Database)"
            : DbConnection.Name;
    }

    private string GetDbConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        var member = repositoryClass.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.IsDbConnection())
            ?? repositoryClass.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(x => x.IsDbConnection()) as ISymbol;

        if (member == null)
        {
            var primaryCtor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
            var param = primaryCtor?.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (param != null) return param.Name;
        }

        if (member == null && repositoryClass.BaseType != null)
            return GetDbConnectionFieldName(repositoryClass.BaseType);

        return member?.Name ?? "connection";
    }

    private string GetDbContextFieldName(INamedTypeSymbol repositoryClass)
    {
        var member = repositoryClass.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.IsDbContext())
            ?? repositoryClass.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(x => x.IsDbContext()) as ISymbol;

        if (member == null)
        {
            var primaryCtor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
            var param = primaryCtor?.Parameters.FirstOrDefault(p => p.Type.IsDbContext());
            if (param != null) return param.Name;

            var ctor = repositoryClass.InstanceConstructors.FirstOrDefault();
            param = ctor?.Parameters.FirstOrDefault(p => p.Type.IsDbContext());
            if (param != null) return param.Name;
        }

        if (member == null && repositoryClass.BaseType != null)
            return GetDbContextFieldName(repositoryClass.BaseType);

        return member?.Name ?? "this.dbContext";
    }

    private void WriteMethodExecuted(IndentedStringBuilder sb, string resultName)
    {
        if (!ReturnIsEnumerable)
            sb.AppendLine($"{MethodExecuted}({MethodNameString}, {CmdName}, {resultName}, {GetTimestampMethod} - {StartTimeName});");
    }

    private void WriteOutput(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        foreach (var item in columnDefines)
        {
            if (item.Symbol is IParameterSymbol parSymbol && parSymbol.RefKind is RefKind.Ref or RefKind.Out)
                sb.AppendLine($"{item.Symbol.Name} = ({parSymbol.Type.ToDisplayString()}){item.ParameterName}.Value;");
        }
    }

    private List<IPropertySymbol> GetPropertySymbols(ITypeSymbol symbol)
    {
        var result = new List<IPropertySymbol>();
        foreach (var member in symbol.GetMembers())
        {
            if (member is IPropertySymbol property && !property.IsReadOnly)
            {
                var hasBrowsableFalse = property.GetAttributes().Any(attr =>
                    attr.AttributeClass?.Name == "BrowsableAttribute" &&
                    attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is bool b && !b);

                if (!hasBrowsableFalse) result.Add(property);
            }
        }
        return result;
    }

    #endregion

    #region 读取器辅助方法

    private void WriteDeclareReturnList(IndentedStringBuilder sb)
    {
        var elementTypeName = ElementType.ToDisplayString();
        sb.AppendLine($"global::System.Collections.Generic.List<{elementTypeName}> {ResultName} = new global::System.Collections.Generic.List<{elementTypeName}>();");
    }

    private void WriteBeginReader(IndentedStringBuilder sb)
    {
        sb.AppendLineIf(IsAsync, $"while(await {DbReaderName}.ReadAsync({CancellationTokenKey}))", $"while({DbReaderName}.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
    }

    private void WriteEndReader(IndentedStringBuilder sb)
    {
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void WriteDeclareObjectExpression(IndentedStringBuilder sb, ITypeSymbol symbol, string? listName, List<IPropertySymbol> properties)
    {
        var newExp = symbol.IsTuple() ? string.Empty : "new ";
        var expCall = symbol.IsTuple() ? string.Empty : " ()";

        if (symbol.IsAbstract || symbol.TypeKind == TypeKind.Interface || symbol.Name == "DbDataReader")
        {
            var typeName = symbol.ToDisplayString();
            sb.AppendLine(symbol.Name == "DbDataReader"
                ? $"{typeName} {DataName} = {DbReaderName};"
                : $"{typeName} {DataName} = default({symbol.ToDisplayString(NullableFlowState.None)});");
        }
        else if (symbol is INamedTypeSymbol namedType && (PrimaryConstructorAnalyzer.IsRecord(namedType) || PrimaryConstructorAnalyzer.HasPrimaryConstructor(namedType)))
        {
            sb.AppendLine($"// Enhanced entity mapping for {(PrimaryConstructorAnalyzer.IsRecord(namedType) ? "record" : "primary constructor")} type");
            SharedCodeGenerationUtilities.GenerateEntityMapping(sb, namedType, "entity");
            if (DataName != "entity" && !string.IsNullOrEmpty(listName))
                sb.AppendLine($"var {DataName} = entity;");
            return;
        }
        else
        {
            var typeName = symbol.ToDisplayString(NullableFlowState.None);
            sb.AppendLine($"{typeName} {DataName} = {newExp}{typeName}{expCall}!;");
        }

        if (!symbol.IsAbstract && symbol.TypeKind != TypeKind.Interface && symbol.Name != "DbDataReader")
        {
            foreach (var item in properties)
            {
                var columnName = item.GetSqlName();
                sb.AppendLine($"{DataName}.{item.Name} = {item.Type.GetDataReadExpressionWithCachedOrdinal(DbReaderName, columnName, $"__ordinal_{columnName}")};");
            }
        }

        if (!string.IsNullOrEmpty(listName))
            sb.AppendLine($"{listName}.Add({DataName});");
    }

    private string? GetHandlerInvoke()
    {
        if (ReaderHandlerParameter?.Type is not INamedTypeSymbol typeSymbol || !typeSymbol.IsGenericType) return null;

        if (typeSymbol.Name == "Func")
        {
            if (typeSymbol.TypeArguments.Length == 2 && typeSymbol.TypeArguments[0].Name == "DbDataReader" &&
                typeSymbol.TypeArguments[1].Name is "Task" or "ValueTask")
                return $"await {ReaderHandlerParameter.Name}({DbReaderName});";

            if (typeSymbol.TypeArguments.Length == 3 && typeSymbol.TypeArguments[0].Name == "DbDataReader" &&
                typeSymbol.TypeArguments[1].Name == "CancellationToken" && typeSymbol.TypeArguments[2].Name is "Task" or "ValueTask")
                return $"await {ReaderHandlerParameter.Name}({DbReaderName}, {CancellationTokenKey});";
        }

        if (typeSymbol.Name == "Action" && typeSymbol.TypeArguments.Length == 1 && typeSymbol.TypeArguments[0].Name == "DbDataReader")
            return $"{ReaderHandlerParameter.Name}({DbReaderName});";

        return null;
    }

    #endregion

    #region SQL获取和处理

    private string? GetSql() => GetSqlFromRawParameter() ?? GetSqlFromSqlxAttribute() ?? GetSqlFromSyntax();

    private string? GetSqlFromRawParameter()
    {
        if (RawSqlParameter?.GetAttribute("SqlxAttribute") is not { } attr) return null;
        if (attr.ConstructorArguments.Length == 0) return RawSqlParameter.Name;

        var sqlValue = ProcessSqlTemplate(attr.ConstructorArguments[0].Value?.ToString() ?? "");
        return $"@\"{EscapeSqlForCSharp(sqlValue)}\"";
    }

    private string? GetSqlFromSqlxAttribute()
    {
        var sqlxAttr = MethodSymbol.GetAttribute("SqlxAttribute");
        if (sqlxAttr == null) return null;

        var procedureName = sqlxAttr.ConstructorArguments.Length > 0
            ? sqlxAttr.ConstructorArguments[0].Value?.ToString()
            : MethodSymbol.Name;

        if (string.IsNullOrEmpty(procedureName)) return null;

        procedureName = ProcessSqlTemplate(procedureName!);
        var paramSql = string.Join(CommaSpace, SqlParameters.Select(p => p.GetParameterName(SqlDef.ParameterPrefix)));
        var call = string.IsNullOrEmpty(paramSql) ? procedureName : $"{procedureName} {paramSql}";
        return $"@\"EXEC {call}\"";
    }

    private string? GetSqlFromSyntax()
    {
        if (MethodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax syntax) return null;

        foreach (var attr in syntax.AttributeLists.SelectMany(list => list.Attributes)
                    .Where(attr => attr.Name.ToString() is "Sqlx" or "SqlxAttribute"))
        {
            if (attr.ArgumentList?.Arguments.Count > 0)
            {
                var firstArg = attr.ArgumentList.Arguments[0].ToString().Trim();
                var escaped = firstArg.Trim('"').Replace("\"", "\\\"");
                return $"@\"EXEC {escaped}\"";
            }
            return $"@\"EXEC {MethodSymbol.Name}\"";
        }
        return null;
    }

    private string ProcessSqlTemplate(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return sql;
        var templateEngine = new SqlTemplateEngine(SqlDef);
        return templateEngine.ProcessTemplate(sql, MethodSymbol, null, string.Empty, SqlDef).ProcessedSql;
    }

    private string? GetTimeoutExpression()
    {
        if (TimeoutParameter != null) return TimeoutParameter.Name;

        var methodTimeout = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "TimeoutAttribute");
        if (methodTimeout?.ConstructorArguments.Length > 0)
            return methodTimeout.ConstructorArguments[0].Value!.ToString();

        var classTimeout = ClassGenerationContext.GetAttribute(x => x.AttributeClass?.Name == "TimeoutAttribute");
        if (classTimeout?.ConstructorArguments.Length > 0)
            return classTimeout.ConstructorArguments[0].Value!.ToString();

        return null;
    }

    private void GenerateExpressionToSqlCommand(IndentedStringBuilder sb)
    {
        sb.AppendLine($"var __template__ = {ExpressionToSqlParameter!.Name}.ToTemplate();");
        sb.AppendLine($"{CmdName}.CommandText = __template__.Sql;");
        sb.AppendLine($"foreach(var __param__ in __template__.Parameters)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{CmdName}.Parameters.Add(__param__);");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    #endregion

    #region 参数绑定

    private void BindParameters(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        foreach (var item in SqlParameters)
        {
            if (item.Type.IsCachedScalarType())
            {
                var (paramName, _) = DeclareParameter(sb, item, item.Type, string.Empty);
                sb.AppendLine($"{CmdName}.Parameters.Add({paramName});");
                sb.AppendLine();
                columnDefines.Add(new ColumnDefine(paramName, item));
            }
            else
            {
                foreach (var deepMember in item.Type.GetMembers().OfType<IPropertySymbol>())
                {
                    var (paramName, _) = DeclareParameter(sb, deepMember, deepMember.Type, item.Name);
                    sb.AppendLine($"{CmdName}.Parameters.Add({paramName});");
                    sb.AppendLine();
                    columnDefines.Add(new ColumnDefine(paramName, deepMember));
                }
            }
        }
    }

    private (string parameterName, string sqlParamName) DeclareParameter(IndentedStringBuilder sb, ISymbol par, ITypeSymbol parType, string prefix)
    {
        var visitPath = string.IsNullOrEmpty(prefix) ? string.Empty : prefix.Replace(".", "?.") + "?.";
        var prefixForName = string.IsNullOrEmpty(prefix) ? string.Empty : prefix.Replace(".", "_");

        var sqlParamName = par.GetParameterName(SqlDef.ParameterPrefix + prefixForName);
        var varName = ParameterNameCleanupRegex.Replace(sqlParamName.TrimStart('@'), "_") + "_p";
        var paramName = par.GetParameterName(SqlDef.ParameterPrefix);
        var dbType = parType.GetDbType();

        sb.AppendLine($"global::System.Data.Common.DbParameter {varName} = {CmdName}.CreateParameter();");
        sb.AppendLine($"{varName}.ParameterName = \"{paramName}\";");
        sb.AppendLine($"{varName}.DbType = {dbType};");
        sb.AppendLine($"{varName}.Value = (object?){visitPath}{par.Name} ?? global::System.DBNull.Value;");

        WriteParameterSpecial(sb, par, varName);
        return (varName, paramName);
    }

    private void WriteParameterSpecial(IndentedStringBuilder sb, ISymbol par, string varName)
    {
        var columnDefine = par.GetDbColumnAttribute();
        if (columnDefine == null) return;

        var map = columnDefine.NamedArguments.ToDictionary(x => x.Key, x => x.Value.Value!);

        if (map.TryGetValue("Precision", out var precision)) sb.AppendLine($"{varName}.Precision = {precision};");
        if (map.TryGetValue("Scale", out var scale)) sb.AppendLine($"{varName}.Scale = {scale};");
        if (map.TryGetValue("Size", out var size)) sb.AppendLine($"{varName}.Size = {size};");
        if (map.TryGetValue("Direction", out var direction))
            sb.AppendLine($"{varName}.Direction = global::System.Data.ParameterDirection.{direction};");
        else if (par is IParameterSymbol parSymbol)
        {
            if (parSymbol.RefKind == RefKind.Ref)
                sb.AppendLine($"{varName}.Direction = global::System.Data.ParameterDirection.InputOutput;");
            else if (parSymbol.RefKind == RefKind.Out)
                sb.AppendLine($"{varName}.Direction = global::System.Data.ParameterDirection.Output;");
        }
    }

    #endregion

    #region SQL方言

    private SqlDefine GetSqlDefine()
    {
        var methodDef = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlDefineAttribute")
            ?? ClassGenerationContext.ClassSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlDefineAttribute");

        if (methodDef != null)
        {
            if (methodDef.ConstructorArguments.Length == 1)
            {
                return (int)methodDef.ConstructorArguments[0].Value! switch
                {
                    0 => SqlDefine.SqlServer,
                    1 => SqlDefine.MySql,
                    2 => SqlDefine.PostgreSql,
                    3 => SqlDefine.SQLite,
                    _ => SqlDefine.SqlServer,
                };
            }

            var columnLeft = methodDef.ConstructorArguments[0].Value?.ToString() ?? "[";
            var columnRight = methodDef.ConstructorArguments[1].Value?.ToString() ?? "]";
            var stringLeft = methodDef.ConstructorArguments[2].Value?.ToString() ?? "'";
            var stringRight = methodDef.ConstructorArguments[3].Value?.ToString() ?? "'";
            var paramPrefix = methodDef.ConstructorArguments[4].Value?.ToString() ?? "@";

            var dbTypeName = (columnLeft, columnRight, paramPrefix) switch
            {
                ("`", "`", "@") => "MySql",
                ("\"", "\"", "$") => "PostgreSql",
                ("\"", "\"", ":") => "Oracle",
                ("\"", "\"", "?") => "DB2",
                ("[", "]", "@") => "SqlServer",
                _ => "Custom"
            };

            return new SqlDefine(columnLeft, columnRight, stringLeft, stringRight, paramPrefix, dbTypeName);
        }

        var inferredDialect = InferDialectFromConnectionType(ClassGenerationContext.ClassSymbol);
        if (inferredDialect.HasValue) return inferredDialect.Value;

        throw new InvalidOperationException("Cannot determine SQL dialect. Please specify SqlDefineAttribute on method or class.");
    }

    private SqlDefine? InferDialectFromConnectionType(INamedTypeSymbol repositoryClass)
    {
        var connectionMember = SqlGenerationHelpers.FindConnectionMember(repositoryClass);
        if (connectionMember != null)
        {
            var connectionTypeName = connectionMember switch
            {
                IFieldSymbol field => field.Type.ToDisplayString(),
                IPropertySymbol property => property.Type.ToDisplayString(),
                IParameterSymbol parameter => parameter.Type.ToDisplayString(),
                _ => null
            };

            if (connectionTypeName != null)
                return SqlGenerationHelpers.InferDialectFromConnectionTypeName(connectionTypeName);
        }

        if (repositoryClass.BaseType != null && repositoryClass.BaseType.SpecialType != SpecialType.System_Object)
            return InferDialectFromConnectionType(repositoryClass.BaseType);

        return null;
    }

    #endregion

    #region 返回类型

    private ReturnTypes GetReturnType()
    {
        if (MethodSymbol.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task" && (!taskType.IsGenericType || taskType.TypeArguments.Length == 0))
            return ReturnTypes.Void;
        if (ReturnType.SpecialType == SpecialType.System_Void) return ReturnTypes.Void;

        var actualType = ReturnType;

        if (actualType.Name == "SqlTemplate" && actualType.ContainingNamespace?.ToDisplayString() == "Sqlx")
            return ReturnTypes.SqlTemplate;

        if (actualType.Name == "IEnumerable") return ReturnTypes.IEnumerable;
        if (actualType.Name == Constants.TypeNames.IAsyncEnumerable) return ReturnTypes.IAsyncEnumerable;

        if (actualType.Name == "List" && actualType is INamedTypeSymbol symbol && symbol.IsGenericType &&
            symbol.TypeParameters.Length == 1 && symbol.TypeArguments[0] is INamedTypeSymbol mapType &&
            mapType.IsGenericType && mapType.TypeArguments.Length == 2 &&
            mapType.TypeArguments[0].Name == "String" && mapType.TypeArguments[1].Name == "Object")
            return ReturnTypes.ListDictionaryStringObject;

        if (actualType.Name is "List" or "IList") return ReturnTypes.List;
        if (actualType.IsCachedScalarType()) return ReturnTypes.Scalar;

        return ReturnTypes.Object;
    }

    #endregion

    #region SqlTemplate返回

    private bool GenerateSqlTemplateReturn(IndentedStringBuilder sb, string? sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
            return false;
        }

        if (sql.IndexOf("{{VALUES_PLACEHOLDER}}", StringComparison.OrdinalIgnoreCase) >= 0)
            return GenerateBatchInsertSqlTemplate(sb, sql);

        sb.AppendLine("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
        sb.AppendLine();

        foreach (var param in SqlParameters)
        {
            if (param.Type.IsCachedScalarType())
                AddParameterToDictionary(sb, param, param.Type, string.Empty);
            else
                foreach (var property in param.Type.GetMembers().OfType<IPropertySymbol>())
                    AddParameterToDictionary(sb, property, property.Type, param.Name);
        }

        sb.AppendLine($"var __result__ = new global::Sqlx.SqlTemplate({sql}, parameters);");
        WriteMethodExecuted(sb, "__result__");
        sb.AppendLine("return __result__;");

        CloseTryBlock(sb);
        return true;
    }

    private void AddParameterToDictionary(IndentedStringBuilder sb, ISymbol symbol, ITypeSymbol type, string prefix)
    {
        var paramName = symbol.GetParameterName(SqlDef.ParameterPrefix);
        var visitPath = string.IsNullOrEmpty(prefix) ? symbol.Name : $"{prefix}?.{symbol.Name}";
        sb.AppendLine($"parameters[\"{paramName}\"] = {visitPath};");
    }

    private bool GenerateBatchInsertSqlTemplate(IndentedStringBuilder sb, string sqlTemplate)
    {
        var collectionParameter = SqlParameters.FirstOrDefault(p => !p.Type.IsCachedScalarType());
        if (collectionParameter == null)
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
            return false;
        }

        var objectMap = new ObjectMap(collectionParameter);
        var baseSql = Regex.Replace(sqlTemplate, @"\{\{VALUES_PLACEHOLDER\}\}", "", RegexOptions.IgnoreCase);

        sb.AppendLine($"var baseSql = \"{baseSql.Trim('"')}\";");
        sb.AppendLine("var sqlBuilder = new global::System.Text.StringBuilder(baseSql);");
        sb.AppendLine("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
        sb.AppendLine("var paramIndex = 0;");
        sb.AppendLine("var isFirst = true;");
        sb.AppendLine();

        sb.AppendLine($"if ({collectionParameter.Name} == null || !{collectionParameter.Name}.Any())");
        sb.AppendLine("    throw new global::System.InvalidOperationException(\"Collection parameter cannot be null or empty\");");
        sb.AppendLine();

        sb.AppendLine($"foreach (var item in {collectionParameter.Name})");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("if (!isFirst) sqlBuilder.Append(\", \");");
        sb.AppendLine("else isFirst = false;");
        sb.AppendLine("sqlBuilder.Append(\"(\");");

        var properties = objectMap.Properties;
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var paramName = $"{SqlDef.ParameterPrefix}{property.GetParameterName(string.Empty)}_{{paramIndex}}";

            if (i > 0) sb.AppendLine("sqlBuilder.Append(\", \");");
            sb.AppendLine($"sqlBuilder.Append(\"{paramName}\");");
            sb.AppendLine($"parameters[\"{paramName}\"] = item.{property.Name};");
        }

        sb.AppendLine("sqlBuilder.Append(\")\");");
        sb.AppendLine("paramIndex++;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("var __result__ = new global::Sqlx.SqlTemplate(sqlBuilder.ToString(), parameters);");
        WriteMethodExecuted(sb, "__result__");
        sb.AppendLine("return __result__;");

        CloseTryBlock(sb);
        return true;
    }

    private void CloseTryBlock(IndentedStringBuilder sb)
    {
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (global::System.Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{MethodExecuteFail}({MethodNameString}, {CmdName}, ex, {GetTimestampMethod} - {StartTimeName});");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    #endregion

    #region 批量INSERT

    private void GenerateBatchInsertSql(IndentedStringBuilder sb, string sqlTemplate)
    {
        var collectionParameter = SqlParameters.FirstOrDefault(p => !p.Type.IsCachedScalarType());
        if (collectionParameter == null)
        {
            sb.AppendLine($"// Warning: No collection parameter found for batch INSERT");
            sb.AppendLine($"{CmdName}.CommandText = {sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "")};");
            return;
        }

        sb.AppendLine($"if ({collectionParameter.Name} == null)");
        sb.AppendLine(SqlxExceptionMessages.GenerateArgumentNullCheck(collectionParameter.Name, SqlxExceptionMessages.CollectionParameterNull));
        sb.AppendLine();

        var objectMap = new ObjectMap(collectionParameter);
        var baseSql = sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "");
        var properties = objectMap.Properties;

        sb.AppendLine($"var baseSql = \"{baseSql.Trim('"')}\";");
        sb.AppendLine($"var sqlBuilder = new global::System.Text.StringBuilder(baseSql);");
        sb.AppendLine($"var paramIndex = 0;");
        sb.AppendLine($"var isFirst = true;");
        sb.AppendLine();

        sb.AppendLine($"if ({collectionParameter.Name}.Count == 0)");
        sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.EmptyCollection));
        sb.AppendLine();

        sb.AppendLine($"foreach (var item in {collectionParameter.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("if (!isFirst) sqlBuilder.Append(\", \");");
        sb.AppendLine("else isFirst = false;");
        sb.AppendLine();
        sb.AppendLine("sqlBuilder.Append(\"(\");");

        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var paramName = $"param_{property.Name}";

            sb.AppendLine($"var {paramName} = {CmdName}.CreateParameter();");
            sb.AppendLine($"{paramName}.ParameterName = $\"{SqlDef.ParameterPrefix}{property.GetParameterName(string.Empty)}_{{paramIndex}}\";");
            sb.AppendLine($"{paramName}.DbType = {property.Type.GetDbType()};");
            sb.AppendLine($"{paramName}.Value = (object?)item.{property.Name} ?? global::System.DBNull.Value;");
            sb.AppendLine($"{CmdName}.Parameters.Add({paramName});");

            if (i > 0) sb.AppendLine("sqlBuilder.Append(\", \");");
            sb.AppendLine($"sqlBuilder.Append($\"{SqlDef.ParameterPrefix}{property.GetParameterName(string.Empty)}_{{paramIndex}}\");");
        }

        sb.AppendLine("sqlBuilder.Append(\")\");");
        sb.AppendLine("paramIndex++;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"{CmdName}.CommandText = sqlBuilder.ToString();");
    }

    #endregion

    #region 批量操作

    private string GetEffectiveTableName(string defaultTableName)
    {
        var methodTableNameAttr = MethodSymbol.GetTableNameAttribute();
        if (methodTableNameAttr?.ConstructorArguments.Length > 0)
            return methodTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;

        var classTableNameAttr = ClassGenerationContext.ClassSymbol.GetTableNameAttribute();
        if (classTableNameAttr?.ConstructorArguments.Length > 0)
            return classTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;

        foreach (var param in SqlParameters)
        {
            var paramTableNameAttr = param.GetTableNameAttribute();
            if (paramTableNameAttr?.ConstructorArguments.Length > 0)
                return paramTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
        }

        return defaultTableName;
    }

    private void GenerateNativeDbBatchLogic(IndentedStringBuilder sb, IParameterSymbol collectionParam, ReturnTypes returnType)
    {
        var tempObjectMap = new ObjectMap(collectionParam);
        var entityType = tempObjectMap.ElementSymbol as INamedTypeSymbol;
        var tableName = GetEffectiveTableName(entityType?.Name ?? "UnknownTable");
        var operationType = SqlGenerationHelpers.InferOperationType(MethodSymbol.Name);

        BatchOperationGenerator.GenerateNativeDbBatch(sb, collectionParam, returnType, SqlDef, tableName, operationType, TransactionParameter, IsAsync);
    }

    private void GenerateFallbackBatchLogic(IndentedStringBuilder sb, IParameterSymbol collectionParam, ReturnTypes returnType)
    {
        var tempObjectMap = new ObjectMap(collectionParam);
        var entityType = tempObjectMap.ElementSymbol as INamedTypeSymbol;
        var tableName = GetEffectiveTableName(entityType?.Name ?? "UnknownTable");
        var operationType = SqlGenerationHelpers.InferOperationType(MethodSymbol.Name);

        BatchOperationGenerator.GenerateFallbackBatch(sb, collectionParam, returnType, SqlDef, tableName, operationType, CmdName, TransactionParameter, GetTimeoutExpression(), IsAsync);
    }

    #endregion

    #region 最佳实践检查

    public void PerformBestPracticeChecks()
    {
        BestPracticeAnalyzer.Analyze(MethodSymbol, GetSql(), ClassGenerationContext.GeneratorExecutionContext);
    }

    #endregion

    public sealed record ColumnDefine(string ParameterName, ISymbol Symbol);
}

internal enum ReturnTypes
{
    Void = 0,
    Scalar = 1,
    IEnumerable = 2,
    IAsyncEnumerable = 3,
    List = 4,
    ListDictionaryStringObject = 5,
    Object = 6,
    SqlTemplate = 7,
}
