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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using static Sqlx.Generator.SharedCodeGenerationUtilities;

internal partial class MethodGenerationContext : GenerationContextBase
{
    // 性能优化：缓存常用的SQL字符串字面量（常量字段优先）
    private const string SqlInsert = "INSERT";
    private const string SqlUpdate = "UPDATE";
    private const string SqlDelete = "DELETE";
    private const string SqlAnd = " AND ";
    private const string CommaSpace = ", ";

    internal const string DbConnectionName = Constants.GeneratedVariables.Connection;
    internal const string CmdName = "__cmd__";  // Use different name to avoid conflicts with AbstractGenerator
    internal const string DbReaderName = "__reader__";
    internal const string ResultName = "__result__";
    internal const string DataName = "__data__";
    internal const string StartTimeName = "__startTime__";

    internal const string MethodExecuting = "OnExecuting";
    internal const string MethodExecuted = "OnExecuted";
    internal const string MethodExecuteFail = "OnExecuteFail";

    internal const string GetTimestampMethod = "global::System.Diagnostics.Stopwatch.GetTimestamp()";

    // 性能优化：预编译正则表达式（非常量字段在后）
    private static readonly Regex ParameterNameCleanupRegex = new("[^a-zA-Z0-9_]", RegexOptions.Compiled);

    // 性能优化：预构建类型转换映射，避免重复的if-else链
    private static readonly Dictionary<SpecialType, string> TypeConversionMap = new()
    {
        { SpecialType.System_Int32, "global::System.Convert.ToInt32" },
        { SpecialType.System_Int64, "global::System.Convert.ToInt64" },
        { SpecialType.System_Boolean, "global::System.Convert.ToBoolean" },
        { SpecialType.System_Decimal, "global::System.Convert.ToDecimal" },
        { SpecialType.System_Double, "global::System.Convert.ToDouble" },
        { SpecialType.System_Single, "global::System.Convert.ToSingle" }
    };

    // 性能优化：辅助方法减少重复代码
    /// <summary>为生成的SQL字符串转义列名或表名</summary>
    private string EscapeForSqlString(string name) => SqlDef.WrapColumn(name).Replace("\"", "\\\"");

    /// <summary>为生成的SQL字符串转义属性的SQL名称</summary>
    private string EscapePropertyForSqlString(IPropertySymbol property) => SqlDef.WrapColumn(GetCachedPropertySqlName(property)).Replace("\"", "\\\"");

    /// <summary>性能优化：统一的数据库连接查找逻辑，避免重复代码</summary>
    private ISymbol? FindConnectionMember(INamedTypeSymbol repositoryClass)
    {
        // 一次性获取所有成员，避免多次枚举（性能优化：使用数组而不是List）
        var allMembers = repositoryClass.GetMembers().ToArray();

        // 优先查找字段
        var connectionField = allMembers.OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.IsDbConnection());
        if (connectionField != null) return connectionField;

        // 然后查找属性
        var connectionProperty = allMembers.OfType<IPropertySymbol>()
            .FirstOrDefault(x => x.IsDbConnection());
        if (connectionProperty != null) return connectionProperty;

        // 最后查找构造函数参数
        var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
        if (constructor != null)
        {
            var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null) return connectionParam;
        }

        return null;
    }

    /// <summary>性能优化：缓存属性的属性查找，避免重复枚举</summary>
    private readonly Dictionary<IPropertySymbol, ImmutableArray<AttributeData>> _propertyAttributeCache = new(SymbolEqualityComparer.Default);

    private ImmutableArray<AttributeData> GetCachedPropertyAttributes(IPropertySymbol property)
    {
        if (!_propertyAttributeCache.TryGetValue(property, out var attributes))
        {
            attributes = property.GetAttributes();
            _propertyAttributeCache[property] = attributes;
        }
        return attributes;
    }

    /// <summary>性能优化：快速检查属性是否包含指定特性</summary>
    private bool HasAttributeByName(IPropertySymbol property, string attributeName)
    {
        var attributes = GetCachedPropertyAttributes(property);
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.Name == attributeName)
                return true;
        }
        return false;
    }

    /// <summary>性能优化：使用全局缓存获取属性SQL名称</summary>
    private string GetCachedPropertySqlName(IPropertySymbol property) =>
        property.GetCachedSqlName();

    internal MethodGenerationContext(ClassGenerationContext classGenerationContext, IMethodSymbol methodSymbol)
    {
        ClassGenerationContext = classGenerationContext;
        MethodSymbol = methodSymbol;

        // Guard for testing - if methodSymbol.Parameters is uninitialized, skip parameter processing
        if (methodSymbol.Parameters.IsDefault)
        {
            SqlParameters = ImmutableArray<IParameterSymbol>.Empty;
            CancellationTokenKey = "default(global::System.Threading.CancellationToken)";
            IsAsync = false;
            AsyncKey = string.Empty;
            AwaitKey = string.Empty;
            SqlDef = SqlDefine.MySql;
            DeclareReturnType = ReturnTypes.Scalar;
            return;
        }

        CancellationTokenParameter = GetParameter(methodSymbol, x => x.Type.IsCancellationToken());

        var rawSqlIsInParamter = true;
        var rawSqlShouldRemoveFromParams = false;
        // RawSqlAttribute functionality has been merged into SqlxAttribute
        var rawSqlParam = GetAttributeParamter(methodSymbol, "SqlxAttribute");
        if (rawSqlParam != null)
        {
            RawSqlParameter = rawSqlParam;

            // Check if RawSql has a constructor argument (pre-defined SQL)
            var attributes = rawSqlParam.GetAttributes();
            var attr = attributes.IsDefaultOrEmpty
                ? null
                : attributes.FirstOrDefault(x => x.AttributeClass?.Name == "SqlxAttribute");
            rawSqlShouldRemoveFromParams = attr?.ConstructorArguments.Length > 0;
        }
        else if (!MethodSymbol.GetAttributes().IsDefaultOrEmpty && MethodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "SqlxAttribute"))
        {
            rawSqlIsInParamter = false;
            RawSqlParameter = methodSymbol;
        }
        else
        {
            RawSqlParameter = null;
        }

        TimeoutParameter = GetAttributeParamter(methodSymbol, "TimeoutAttribute");
        ReaderHandlerParameter = GetAttributeParamter(methodSymbol, "ReadHandlerAttribute");
        ExpressionToSqlParameter = GetAttributeParamter(methodSymbol, "ExpressionToSqlAttribute");

        var parameters = methodSymbol.Parameters;
        RemoveIfExists(ref parameters, DbContext);
        RemoveIfExists(ref parameters, DbConnection);
        RemoveIfExists(ref parameters, TransactionParameter);
        if (rawSqlIsInParamter && rawSqlShouldRemoveFromParams) RemoveIfExists(ref parameters, RawSqlParameter);
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

    /// <summary>
    ///  Gets the SqlAttribute if the method paramters has.
    /// </summary>
    internal ISymbol? RawSqlParameter { get; }

    /// <summary>
    ///  Gets the <see cref="System.Threading.CancellationToken"/> if the method paramters has.
    /// </summary>
    internal ISymbol? CancellationTokenParameter { get; }

    /// <summary>
    /// Gets the commandtimeout.
    /// </summary>
    internal IParameterSymbol? TimeoutParameter { get; }

    /// <summary>
    /// Gets the DbReaderHandler.
    /// </summary>
    internal IParameterSymbol? ReaderHandlerParameter { get; }

    /// <summary>
    /// Gets the ExpressionToSql parameter for LINQ expression processing.
    /// </summary>
    internal IParameterSymbol? ExpressionToSqlParameter { get; }

    /// <summary>
    /// Gets the method paramters remove the extars.
    /// </summary>
    internal ImmutableArray<IParameterSymbol> SqlParameters { get; }

    public ITypeSymbol ReturnType => MethodSymbol.ReturnType?.UnwrapTaskType() ?? throw new InvalidOperationException("Method symbol has no return type");

    internal ITypeSymbol ElementType => ReturnType.UnwrapListType();

    internal string AsyncKey { get; }

    internal string AwaitKey { get; }

    internal string CancellationTokenKey { get; }

    public bool IsAsync { get; }

    /// <summary>
    /// Public properties for tests
    /// </summary>
    public bool ReturnIsEnumerable => ReturnType.Name == "IEnumerable" || ReturnType.Name == Constants.TypeNames.IAsyncEnumerable;

    private SqlDefine SqlDef { get; }

    private string MethodNameString => $"\"{MethodSymbol.Name}\"";

    public bool DeclareCommand(IndentedStringBuilder sb)
    {
        var args = string.Join(CommaSpace, MethodSymbol.Parameters.Select(x =>
        {
            var paramterSyntax = (ParameterSyntax)x.DeclaringSyntaxReferences[0].GetSyntax();
            var prefx = string.Join(" ", paramterSyntax.Modifiers.Select(y => y.ToString()));
            if (paramterSyntax!.Modifiers.Count != 0) prefx += " ";
            return prefx + x.Type.ToDisplayString() + " " + x.Name;
        }));

        var staticKeyword = MethodSymbol.IsStatic ? "static " : string.Empty;

        // Always generate partial methods for source generation - the source generator should generate implementations
        // for methods that have [Sqlx] attributes, whether they are declared as partial or not
        sb.AppendLine($"{MethodSymbol.DeclaredAccessibility.GetAccessibility()} {AsyncKey}{staticKeyword}partial {MethodSymbol.ReturnType.ToDisplayString()} {MethodSymbol.Name}({args})");

        sb.AppendLine("{");
        sb.PushIndent();

        var dbContext = DbContext;
        var dbConnection = DbConnection;

        // Check if this class has RepositoryFor attribute
        var hasRepositoryForAttribute = ClassGenerationContext.ClassSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "RepositoryForAttribute" || attr.AttributeClass?.Name == "RepositoryFor");

        // Always declare variables in this scope to avoid conflicts with AbstractGenerator
        // Use different variable names to prevent conflicts

        string dbConnectionExpression;

        if (hasRepositoryForAttribute)
        {
            // For RepositoryFor classes, use the actual DbConnection field name
            // Skip connection validation as the RepositoryFor generator handles this
            var connectionFieldName = GetDbConnectionFieldName(ClassGenerationContext.ClassSymbol);
            dbConnectionExpression = connectionFieldName;
        }
        else
        {
            // For regular classes, check for connection availability
            if (dbContext == null && dbConnection == null)
            {
                // Fallback: assume a field named 'dbContext' exists and use EF Core database facade
                dbConnectionExpression = "global::Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetDbConnection(this.dbContext.Database)";
            }
            else
            {
                var dbContextName = GetDbContextFieldName(ClassGenerationContext.ClassSymbol);
                dbConnectionExpression = dbConnection == null ? $"global::Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetDbConnection({dbContextName}.Database)" : dbConnection.Name;
            }
        }

        sb.AppendLine($"global::System.Data.Common.DbConnection {DbConnectionName} = {dbConnectionExpression} ?? ");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(\"{dbConnectionExpression}\");");

        // Generate parameter null checks first (fail fast)
        GenerateParameterNullChecks(sb);

        sb.AppendLine($"if({DbConnectionName}.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();

        if (IsAsync)
        {
            sb.AppendLine($"await {DbConnectionName}.OpenAsync({CancellationTokenKey});");
        }
        else
        {
            sb.AppendLine($"{DbConnectionName}.Open();");
        }

        sb.PopIndent();
        sb.AppendLine("}");

        // Always declare the variable with a unique name to avoid conflicts
        sb.AppendLine($"using (var {CmdName} = {DbConnectionName}.CreateCommand())");
        sb.AppendLine("{");
        sb.PushIndent();

        // Don't declare variables here as they will be declared by specific generation methods
        // This avoids duplicate declarations

        if (TransactionParameter != null)
            sb.AppendLine($"{CmdName}.Transaction = {TransactionParameter.Name};");
        var timeoutExpression = GetTimeoutExpression();
        if (!string.IsNullOrWhiteSpace(timeoutExpression))
            sb.AppendLine($"{CmdName}.CommandTimeout = {timeoutExpression};");

        var sql = GetSql();
        if (string.IsNullOrEmpty(sql))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
            return false;
        }
        // Legacy batch operations detection - redirect to proper BatchCommand
        if (!string.IsNullOrEmpty(sql) && sql?.Contains("BATCH") == true)
        {
            // Generate proper error message for better developer experience
            sb.AppendLine($"// Legacy batch operation detected: {sql}");
            sb.AppendLine("// Use Constants.SqlExecuteTypeValues.BatchCommand for proper batch operations");
            sb.AppendLine(SqlxExceptionMessages.GenerateArgumentExceptionThrow(SqlxExceptionMessages.LegacyBatchSql));
            sb.PopIndent();
            sb.AppendLine("}");
            return true;
        }

        // Set the SQL command text
        if (ExpressionToSqlParameter != null && string.IsNullOrEmpty(sql))
        {
            // Pure ExpressionToSql case - SQL comes from the expression
            sb.AppendLine($"var __template__ = {ExpressionToSqlParameter.Name}.ToTemplate();");
            sb.AppendLine($"{CmdName}.CommandText = __template__.Sql;");
            sb.AppendLine($"foreach(var __param__ in __template__.Parameters)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{CmdName}.Parameters.Add(__param__);");
            sb.PopIndent();
            sb.AppendLine("}");
        }
        else if (!string.IsNullOrEmpty(sql))
        {
            // Check if this is a batch INSERT operation with placeholder
            if (sql?.Contains("{{VALUES_PLACEHOLDER}}") == true)
            {
                GenerateBatchInsertSql(sb, sql);
            }
            else
            {
                // Static SQL case (from RawSql or stored procedure)
                sb.AppendLine($"{CmdName}.CommandText = {sql};");
            }
        }

        sb.AppendLine();

        // Paramters - skip for batch INSERT operations as they're handled in GenerateBatchInsertSql
        // 性能优化：预估容量减少重分配
        var columnDefines = new List<ColumnDefine>(MethodSymbol.Parameters.Length);
        var isBatchInsert = !string.IsNullOrEmpty(sql) && sql?.Contains("{{VALUES_PLACEHOLDER}}") == true;

        if (!isBatchInsert)
        {
            foreach (var item in SqlParameters)
            {
                var isScalarType = item.Type.IsCachedScalarType();
                if (isScalarType)
                {
                    HandlerColumn(item, item.Type, string.Empty);
                }
                else
                {
                    foreach (var deepMember in item.Type.GetMembers().OfType<IPropertySymbol>())
                    {
                        HandlerColumn(deepMember, deepMember.Type, $"{item.Name}");
                    }
                }

                void HandlerColumn(ISymbol par, ITypeSymbol parType, string prefx)
                {
                    var parName = DeclareParamter(sb, par, parType, prefx);
                    sb.AppendLine($"{CmdName}.Parameters.Add({parName.ParameterName});");
                    sb.AppendLine();
                    columnDefines.Add(parName);
                }
            }
        }

        sb.AppendLine($"global::System.Int64 {StartTimeName} = ");
        sb.AppendLine($"    {GetTimestampMethod};");
        if (!ReturnIsEnumerable)
        {
            sb.AppendLine("try");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{MethodExecuting}({MethodNameString}, {CmdName});");
        }

        // SqlTemplate generation mode - return SQL and parameters without executing
        if (DeclareReturnType == ReturnTypes.SqlTemplate)
        {
            return GenerateSqlTemplateReturn(sb, sql);
        }

        // Execute
        if (DeclareReturnType == ReturnTypes.Void)
        {
            // For void/Task methods, execute non-query and return
            sb.AppendLineIf(IsAsync, $"await {CmdName}.ExecuteNonQueryAsync();", $"{CmdName}.ExecuteNonQuery();");
            if (!ReturnIsEnumerable)
            {
                WriteMethodExecuted(sb, "null");
            }
        }
        else if (IsExecuteNoQuery())
        {
            WriteExecuteNoQuery(sb, columnDefines);
        }
        else if (ReturnType.IsCachedScalarType())
        {
            WriteScalar(sb, columnDefines);
        }
        else
        {
            var succeed = WriteReturn(sb, columnDefines);
            if (!succeed) return false;
        }

        if (!ReturnIsEnumerable)
        {
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("catch (global::System.Exception ex)");
            sb.AppendLine("{");
            sb.PushIndent();

            sb.AppendLine($"{MethodExecuteFail}({MethodNameString}, {CmdName}, ex, ");
            sb.AppendLine($"    {GetTimestampMethod} - {StartTimeName});");
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

    private static void RemoveIfExists(ref ImmutableArray<IParameterSymbol> pars, ISymbol? symbol)
    {
        if (symbol is IParameterSymbol parameter)
        {
            pars = pars.Remove(parameter);
        }
    }

    private static ISymbol? GetParameter(IMethodSymbol methodSymbol, Func<IParameterSymbol, bool> check)
        => methodSymbol.Parameters.FirstOrDefault(check);

    private static IParameterSymbol? GetAttributeParamter(IMethodSymbol methodSymbol, string attributeName)
        => methodSymbol.Parameters.FirstOrDefault(x => x.GetAttributes().Any(y =>
            y.AttributeClass?.Name == attributeName ||
            y.AttributeClass?.Name == attributeName.Replace("Attribute", "")));

    private bool IsExecuteNoQuery()
    {
        // Check for explicit ExecuteNoQueryAttribute
        if (MethodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "ExecuteNoQueryAttribute"))
            return true;

        return false;
    }

    public bool WriteExecuteNoQuery(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        // Support int and bool return types for ExecuteNonQuery operations
        if (!(ReturnType.SpecialType == SpecialType.System_Int32 || ReturnType.SpecialType == SpecialType.System_Boolean))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0008, MethodSymbol.Locations.FirstOrDefault()));
            return false;
        }

        sb.AppendLineIf(IsAsync, $"var {ResultName} = await {CmdName}.ExecuteNonQueryAsync();", $"var {ResultName} = {CmdName}.ExecuteNonQuery();");

        WriteOutput(sb, columnDefines);
        WriteMethodExecuted(sb, ResultName);

        // Generate appropriate return statement based on return type
        if (ReturnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine($"return {ResultName} > 0;");
        }
        else
        {
            sb.AppendLine($"return {ResultName};");
        }
        return true;
    }

    private void WriteMethodExecuted(IndentedStringBuilder sb, string resultName)
    {
        if (!ReturnIsEnumerable)
        {
            sb.AppendLine($"{MethodExecuted}({MethodNameString}, {CmdName}, {resultName}, ");
            sb.AppendLine($"    {GetTimestampMethod} - {StartTimeName});");
        }
    }

    public void WriteScalar(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        sb.AppendLineIf(IsAsync, $"var {ResultName} = await {CmdName}.ExecuteScalarAsync();", $"var {ResultName} = {CmdName}.ExecuteScalar();");

        WriteOutput(sb, columnDefines);
        if (!ReturnIsEnumerable)
        {
            WriteMethodExecuted(sb, ResultName);
        }

        if (ReturnType.Name == "Nullable")
        {
            sb.AppendLine("try");
            sb.AppendLine("{");
            sb.PushIndent();

            WriteReturn();

            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("catch (global::System.InvalidCastException)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("return default;");
            sb.PopIndent();
            sb.AppendLine("}");
        }
        else
        {
            WriteReturn();
        }

        void WriteReturn()
        {
            var canReturnNull = ReturnType.IsNullableType() ||
                              (!ReturnType.IsValueType && ReturnType.NullableAnnotation == NullableAnnotation.Annotated);

            if (canReturnNull)
            {
                sb.AppendLine($"if({ResultName} == null) return default;");
            }
            else
            {
                sb.AppendLine($"if({ResultName} == null) throw new global::System.InvalidOperationException(\"{SqlxExceptionMessages.SequenceEmpty}\");");
            }

            // 性能优化：使用字典查找替代重复的if-else链
            if (TypeConversionMap.TryGetValue(ReturnType.SpecialType, out var converter))
            {
                sb.AppendLine($"return {converter}({ResultName});");
            }
            else if (ReturnType.SpecialType == SpecialType.System_String)
            {
                sb.AppendLine($"return {ResultName}?.ToString() ?? string.Empty;");
            }
            else
            {
                sb.AppendLine($"return ({ReturnType.ToDisplayString()}){ResultName};");  // Direct cast for generic types
            }
        }
    }

    public bool WriteReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var dbConnection = DbConnection;

        var handler = GetHandlerInvoke();
        var hasHandler = !string.IsNullOrEmpty(handler);

        var returnType = ElementType.UnwrapNullableType();
        var isTuple = returnType.IsTupleType;

        if (hasHandler || dbConnection != null)
        {
            var executeMethod = IsAsync ? $"await {CmdName}.ExecuteReaderAsync()" : $"{CmdName}.ExecuteReader()";
            sb.AppendLine($"using(global::System.Data.Common.DbDataReader {DbReaderName} = {executeMethod})");
            sb.AppendLine("{");
            sb.PushIndent();
            if (hasHandler)
            {
                sb.AppendLine(handler!);
            }
            else if (DeclareReturnType == ReturnTypes.List || DeclareReturnType == ReturnTypes.IEnumerable || DeclareReturnType == ReturnTypes.IAsyncEnumerable)
            {
                // For async methods, treat IEnumerable as List to avoid yield syntax issues
                var isList = DeclareReturnType == ReturnTypes.List || (IsAsync && DeclareReturnType == ReturnTypes.IEnumerable);
                if (isList) WriteDeclareReturnList(sb);

                // For scalar types, don't cache ordinals - just use ordinal 0 directly
                // This avoids issues when the actual column name doesn't match "Column0"
                var isScalarList = returnType.IsCachedScalarType();
                
                if (!isScalarList)
                {
                    // Cache column ordinals for performance (only for non-scalar types)
                    var columnNames = GetColumnNames(returnType);
                    WriteCachedOrdinals(sb, columnNames);
                }

                WriteBeginReader(sb);

                if (isScalarList)
                {
                    // For scalar lists, read directly from ordinal 0 without caching
                    // Use the reader method directly with ordinal 0
                    var method = returnType.GetDataReaderMethod();
                    if (method == null)
                    {
                        // Fallback to indexer for types without specific reader method
                        sb.AppendLineIf(isList, $"{ResultName}.Add(({returnType.ToDisplayString()}){DbReaderName}[0]);", $"yield return ({returnType.ToDisplayString()}){DbReaderName}[0];");
                    }
                    else
                    {
                        // Use the specific reader method with ordinal 0
                        var isNullable = returnType.IsNullableType();
                        if (isNullable)
                        {
                            sb.AppendLineIf(isList, $"{ResultName}.Add({DbReaderName}.IsDBNull(0) ? null : {DbReaderName}.{method}(0));", $"yield return {DbReaderName}.IsDBNull(0) ? null : {DbReaderName}.{method}(0);");
                        }
                        else if (returnType.SpecialType == SpecialType.System_String && returnType.NullableAnnotation == NullableAnnotation.NotAnnotated)
                        {
                            // For non-nullable strings, return empty string instead of null
                            sb.AppendLineIf(isList, $"{ResultName}.Add({DbReaderName}.IsDBNull(0) ? string.Empty : {DbReaderName}.{method}(0));", $"yield return {DbReaderName}.IsDBNull(0) ? string.Empty : {DbReaderName}.{method}(0);");
                        }
                        else
                        {
                            sb.AppendLineIf(isList, $"{ResultName}.Add({DbReaderName}.{method}(0));", $"yield return {DbReaderName}.{method}(0);");
                        }
                    }
                }
                else if (isTuple)
                {
                    var tupleType = (INamedTypeSymbol)returnType.UnwrapTaskType();
                    var tupleArgs = tupleType.TypeArguments;

                    // For tuples, try to get field names from tuple type
                    string[] fieldNames;
                    if (tupleType.TupleElements != null && tupleType.TupleElements.Length > 0)
                    {
                        fieldNames = tupleType.TupleElements.Select(e => e.Name).ToArray();
                    }
                    else
                    {
                        // Fallback to default column names
                        fieldNames = tupleArgs.Select((_, index) => $"Column{index}").ToArray();
                    }

                    var tupleJoins = string.Join(CommaSpace, tupleArgs.Select((x, index) =>
                        x.GetDataReadExpressionWithCachedOrdinal(DbReaderName, fieldNames[index], $"__ordinal_{fieldNames[index]}")));
                    sb.AppendLineIf(isList, $"{ResultName}.Add(({tupleJoins}));", $"yield return ({tupleJoins});");
                }
                else
                {
                    WriteDeclareObjectExpression(sb, returnType, isList ? ResultName : null, GetPropertySymbols(returnType));
                    if (!isList)
                    {
                        sb.AppendLine($"yield return {DataName};");
                    }
                }

                WriteEndReader(sb);
                WriteOutput(sb, columnDefines);
                WriteMethodExecuted(sb, isList ? ResultName : "null");

                if (isList)
                    sb.AppendLine($"return {ResultName};");
            }
            else if (DeclareReturnType == ReturnTypes.ListDictionaryStringObject)
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
            else
            {
                var writeableProperties = GetPropertySymbols(returnType);

                var isList = DeclareReturnType == ReturnTypes.List;
                if (isList) WriteDeclareReturnList(sb);

                // Cache column ordinals for performance
                var columnNames = GetColumnNames(returnType);
                WriteCachedOrdinals(sb, columnNames);

                var readMethod = IsAsync ? $"ReadAsync({CancellationTokenKey})" : "Read()";

                // Check if return type allows null
                var canReturnNull = ReturnType.IsNullableType() ||
                                  (!ReturnType.IsValueType && ReturnType.NullableAnnotation == NullableAnnotation.Annotated);

                if (canReturnNull)
                {
                    sb.AppendLine($"if(!{AwaitKey}{DbReaderName}.{readMethod}) return default;");
                }
                else
                {
                    sb.AppendLine($"if(!{AwaitKey}{DbReaderName}.{readMethod}) throw new global::System.InvalidOperationException(");
                    sb.AppendLine($"        \"Sequence contains no elements\");");
                }

                sb.AppendLine();

                // List<T> or T
                WriteDeclareObjectExpression(sb, returnType, isList ? ResultName : null, writeableProperties);

                var selectName = isList ? ResultName : DataName;

                WriteOutput(sb, columnDefines);
                WriteMethodExecuted(sb, selectName);
                sb.AppendLine($"return {selectName};");
            }

            sb.PopIndent();
            sb.AppendLine("}");
            return true;
        }

        ISymbol setSymbol = ElementType;

        if (GetDbSetElement(out var dbSetEle)) setSymbol = dbSetEle!;

        var fromSqlRawMethod = "global::Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.FromSqlRaw";
        var __dbContextAccess__ = DbContext != null ? DbContext.Name : "this.dbContext";
        var queryCall = $"{fromSqlRawMethod}({__dbContextAccess__}.Set<{setSymbol.ToDisplayString()}>(),{CmdName}.CommandText, {CmdName}.Parameters.OfType<global::System.Object>().ToArray())";

        var convert = string.Empty;
        if (!SymbolEqualityComparer.Default.Equals(ElementType, setSymbol) && setSymbol is INamedTypeSymbol setNameTypeSymbol)
        {
            string constructExpress = string.Empty;
            if (isTuple)
            {
                constructExpress = $"({string.Join(", ", ((INamedTypeSymbol)ElementType).TupleElements.Select(x => $"x.{x.Name}"))})";
            }
            else
            {
                var propertyTypes = setNameTypeSymbol.GetMembers().OfType<IPropertySymbol>().ToArray();
                var construct = ((INamedTypeSymbol)ElementType).Constructors.FirstOrDefault(x => x.Parameters.All(HasProperty));
                if (construct == null)
                {
                    // No suitable constructor found for entity mapping
                    return false;
                }

                constructExpress = $"new {ElementType.ToDisplayString(NullableFlowState.None)}({string.Join(", ", construct.Parameters.Select(x => $"x.{FindProperty(x)!.Name}"))})";

                // 性能优化：避免复杂的LINQ嵌套，使用HashSet提高查找效率
                var constructorParamNames = new HashSet<string>(construct.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                var properties = new List<IPropertySymbol>();
                foreach (var member in ElementType.GetMembers())
                {
                    if (member is IPropertySymbol property && !constructorParamNames.Contains(property.Name))
                        properties.Add(property);
                }
                if (properties.Count != 0)
                {
                    constructExpress += $"{{ {string.Join(", ", properties.Select(x => $"{x.Name} = x.{x.Name}"))} }}";
                }

                IPropertySymbol? FindProperty(IParameterSymbol symbol) => propertyTypes.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Type, symbol.Type) && x.Name.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase));

                bool HasProperty(IParameterSymbol symbol) => FindProperty(symbol) != null;
            }

            convert = $".Select(x => {constructExpress})";
        }

        // Use yield only for non-async IEnumerable methods, not for Task<IEnumerable<T>>
        if ((DeclareReturnType == ReturnTypes.IEnumerable || DeclareReturnType == ReturnTypes.IAsyncEnumerable) && !IsAsync)
        {
            sb.AppendLine($"{AwaitKey}foreach (var item in {queryCall}{convert})");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("yield return item;");
            sb.PopIndent();
            sb.AppendLine("}");
            WriteOutput(sb, columnDefines);
        }
        // For async methods returning Task<IEnumerable<T>>, collect to list and return
        else if (DeclareReturnType == ReturnTypes.IEnumerable && IsAsync)
        {
            queryCall += convert;
            // Treat async IEnumerable as List for proper collection handling
            queryCall = $"global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync({queryCall}, {CancellationTokenKey})";
            sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall};");
            WriteOutput(sb, columnDefines);
        }
        else
        {
            queryCall += convert;
            if (IsAsync)
            {
                if (DeclareReturnType != ReturnTypes.List)
                {
                    queryCall = $"global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync({queryCall}, {CancellationTokenKey})";
                }
                else
                {
                    queryCall = $"global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync({queryCall}, {CancellationTokenKey})";
                }

                sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall};");
            }
            else
            {
                var callMethod = DeclareReturnType == ReturnTypes.List ? ".ToList()" : ".FirstOrDefault()";
                sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall}{callMethod};");
            }

            WriteOutput(sb, columnDefines);

            // Check if return type allows null for EF Core queries
            if (DeclareReturnType != ReturnTypes.List)
            {
                var canReturnNull = ReturnType.IsNullableType() ||
                                  (!ReturnType.IsValueType && ReturnType.NullableAnnotation == NullableAnnotation.Annotated);

                if (!canReturnNull)
                {
                    sb.AppendLine($"return {ResultName} ?? throw new global::System.InvalidOperationException(");
                    sb.AppendLine($"        \"Sequence contains no elements\");");
                }
                else
                {
                    sb.AppendLine($"return {ResultName};");
                }
            }
            else
            {
                sb.AppendLine($"return {ResultName};");
            }
        }

        return true;
    }

    private bool GetDbSetElement(out ISymbol? symbol)
    {
        symbol = null;
        // DbSetType support removed to reduce complexity
        return false;
    }

    private List<IPropertySymbol> GetPropertySymbols(ITypeSymbol symbol)
    {
        // 性能优化：单次遍历过滤可写属性，避免复杂的LINQ嵌套
        var writeableProperties = new List<IPropertySymbol>();
        foreach (var member in symbol.GetMembers())
        {
            if (member is IPropertySymbol property && !property.IsReadOnly)
            {
                // 检查是否有BrowsableAttribute且为false
                bool hasBrowsableFalse = false;
                foreach (var attr in property.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "BrowsableAttribute" &&
                        attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is bool b && !b)
                    {
                        hasBrowsableFalse = true;
                        break;
                    }
                }

                if (!hasBrowsableFalse)
                    writeableProperties.Add(property);
            }
        }
        return writeableProperties;
    }

    private void WriteDeclareReturnList(IndentedStringBuilder sb)
    {
        // 性能优化：缓存ToDisplayString结果，避免重复调用
        var elementTypeName = ElementType.ToDisplayString();
        sb.AppendLine($"global::System.Collections.Generic.List<{elementTypeName}> {ResultName} = new global::System.Collections.Generic.List<{elementTypeName}>();");
    }

    private void WriteBeginReader(IndentedStringBuilder sb)
    {
        if (IsAsync)
        {
            sb.AppendLine($"while(await {DbReaderName}.ReadAsync({CancellationTokenKey}))");
        }
        else
        {
            sb.AppendLine($"while({DbReaderName}.Read())");
        }

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

        // Check if the type is abstract or cannot be instantiated
        if (symbol.IsAbstract || symbol.TypeKind == TypeKind.Interface || symbol.Name == "DbDataReader")
        {
            // For abstract types like DbDataReader, we can't instantiate them directly
            // Return the reader itself or handle specially
            if (symbol.Name == "DbDataReader")
            {
                // 性能优化：缓存ToDisplayString结果，避免重复调用
                var symbolTypeName = symbol.ToDisplayString();
                sb.AppendLine($"{symbolTypeName} {DataName} = {DbReaderName};");
            }
            else
            {
                // 性能优化：缓存ToDisplayString结果，避免重复调用
                var symbolTypeName = symbol.ToDisplayString();
                var symbolTypeNameWithNullFlow = symbol.ToDisplayString(NullableFlowState.None);
                sb.AppendLine($"{symbolTypeName} {DataName} = default({symbolTypeNameWithNullFlow});");
            }
        }
        else if (symbol is INamedTypeSymbol namedType)
        {
            // Check if this is a record or has a primary constructor
            if (PrimaryConstructorAnalyzer.IsRecord(namedType) || PrimaryConstructorAnalyzer.HasPrimaryConstructor(namedType))
            {
                // Use enhanced entity mapping for records and primary constructors
                sb.AppendLine($"// Enhanced entity mapping for {(PrimaryConstructorAnalyzer.IsRecord(namedType) ? "record" : "primary constructor")} type");
                // Simple entity mapping - removed EnhancedEntityMappingGenerator
                SharedCodeGenerationUtilities.GenerateEntityMapping(sb, namedType, "entity");
                // Rename the generated entity variable to match expected DataName if it will be used
                if (DataName != "entity" && !string.IsNullOrEmpty(listName))
                {
                    sb.AppendLine($"var {DataName} = entity;");
                }
                return; // Early return since enhanced mapping handles everything
            }
            else
            {
                // Traditional class instantiation
                // 性能优化：缓存ToDisplayString结果，避免重复调用
                var symbolTypeNameWithNullFlow = symbol.ToDisplayString(NullableFlowState.None);
                sb.AppendLine($"{symbolTypeNameWithNullFlow} {DataName} = {newExp}{symbolTypeNameWithNullFlow}{expCall}!;");
            }
        }
        else
        {
            // Fallback for non-named types
            // 性能优化：缓存ToDisplayString结果，避免重复调用
            var symbolTypeNameWithNullFlow = symbol.ToDisplayString(NullableFlowState.None);
            sb.AppendLine($"{symbolTypeNameWithNullFlow} {DataName} = {newExp}{symbolTypeNameWithNullFlow}{expCall}!;");
        }

        // Only set properties for non-abstract types that can be instantiated
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
        if (ReaderHandlerParameter == null || ReaderHandlerParameter.Type is not INamedTypeSymbol typeSymbol || !typeSymbol.IsGenericType) return null;

        // Check the handler
        if (typeSymbol.Name == "Func")
        {
            if (typeSymbol.TypeArguments.Length == 2
                && typeSymbol.TypeArguments[0].Name == "DbDataReader"
                && (typeSymbol.TypeArguments[1].Name == "Task" || typeSymbol.TypeArguments[1].Name == "ValueTask"))
            {
                return $"await {ReaderHandlerParameter.Name}({DbReaderName});";
            }

            if (typeSymbol.TypeArguments.Length == 2
                && typeSymbol.TypeArguments[0].Name == "DbDataReader"
                && typeSymbol.TypeArguments[1].Name == "CancellationToken"
                && (typeSymbol.TypeArguments[2].Name == "Task" || typeSymbol.TypeArguments[2].Name == "ValueTask"))
            {
                return $"await {ReaderHandlerParameter.Name}({DbReaderName}, {CancellationTokenKey});";
            }

            return null;
        }

        if (typeSymbol.Name == "Action"
            && typeSymbol.TypeArguments.Length == 1
            && typeSymbol.TypeArguments[0].Name == "DbDataReader")
        {
            return $"{ReaderHandlerParameter.Name}({DbReaderName});";
        }

        return null;
    }

    private void WriteOutput(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        foreach (var item in columnDefines)
        {
            if (item.Symbol is IParameterSymbol parSymbol && (parSymbol.RefKind == RefKind.Ref || parSymbol.RefKind == RefKind.Out))
            {
                sb.AppendLine($"{item.Symbol.Name} = ({parSymbol.Type.ToDisplayString()}){item.ParameterName}.Value;");
            }
        }
    }

    private SqlDefine GetSqlDefine()
    {
        var methodAttributes = MethodSymbol.GetAttributes();
        var classAttributes = ClassGenerationContext.ClassSymbol.GetAttributes();

        var methodDef = (!methodAttributes.IsDefaultOrEmpty
                ? methodAttributes.FirstOrDefault(x => x.AttributeClass?.Name == "SqlDefineAttribute")
                : null) ??
            (!classAttributes.IsDefaultOrEmpty
                ? classAttributes.FirstOrDefault(x => x.AttributeClass?.Name == "SqlDefineAttribute")
                : null);

        if (methodDef != null)
        {
            if (methodDef.ConstructorArguments.Length == 1)
            {
                var define = (int)methodDef.ConstructorArguments[0].Value!;
                return define switch
                {
                    0 => SqlDefine.SqlServer,
                    1 => SqlDefine.MySql,
                    2 => SqlDefine.PostgreSql,
                    3 => SqlDefine.SQLite,
                    _ => SqlDefine.SqlServer, // Default fallback
                };
            }

            // Custom SqlDefine with explicit delimiters - infer DbTypeName from pattern
            var columnLeft = methodDef.ConstructorArguments[0].Value?.ToString() ?? "[";
            var columnRight = methodDef.ConstructorArguments[1].Value?.ToString() ?? "]";
            var stringLeft = methodDef.ConstructorArguments[2].Value?.ToString() ?? "'";
            var stringRight = methodDef.ConstructorArguments[3].Value?.ToString() ?? "'";
            var paramPrefix = methodDef.ConstructorArguments[4].Value?.ToString() ?? "@";
            
            // Infer DbTypeName from delimiter pattern
            var dbTypeName = (columnLeft, columnRight, paramPrefix) switch
            {
                ("`", "`", "@") => "MySql",
                ("\"", "\"", "$") => "PostgreSql",
                ("\"", "\"", ":") => "Oracle",
                ("\"", "\"", "?") => "DB2",
                ("[", "]", "@") => "SqlServer",  // Default for [, ], @
                _ => "Custom"
            };
            
            return new SqlDefine(columnLeft, columnRight, stringLeft, stringRight, paramPrefix, dbTypeName);
        }

        // Try to infer database dialect from the connection type
        var inferredDialect = InferDialectFromConnectionType(ClassGenerationContext.ClassSymbol);
        if (inferredDialect.HasValue)
        {
            return inferredDialect.Value;
        }

        // 激进优化：强制明确配置SQL方言
        throw new InvalidOperationException("Cannot determine SQL dialect. Please specify SqlDefineAttribute on method or class.");
    }

    private SqlDefine? InferDialectFromConnectionType(INamedTypeSymbol repositoryClass)
    {
        // 性能优化：使用统一的连接查找方法
        var connectionMember = FindConnectionMember(repositoryClass);
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
            {
                return InferDialectFromConnectionTypeName(connectionTypeName);
            }
        }

        // Check base classes
        if (repositoryClass.BaseType != null && repositoryClass.BaseType.SpecialType != SpecialType.System_Object)
        {
            return InferDialectFromConnectionType(repositoryClass.BaseType);
        }

        return null;
    }

    private SqlDefine? InferDialectFromConnectionTypeName(string connectionTypeName)
    {
        return connectionTypeName.ToLowerInvariant() switch
        {
            var name when name.Contains("sqlite") => SqlDefine.SQLite,
            var name when name.Contains("mysql") || name.Contains("mariadb") => SqlDefine.MySql,
            var name when name.Contains("postgres") || name.Contains("npgsql") => SqlDefine.PostgreSql,
            var name when name.Contains("sqlserver") || name.Contains("sqlconnection") => SqlDefine.SqlServer,
            _ => null
        };
    }

    private List<string> GetColumnNames(ITypeSymbol returnType)
    {
        // 性能优化：预估容量减少重分配
        var columnNames = new List<string>(10); // 大多数表不会超过10列

        if (returnType.IsCachedScalarType())
        {
            columnNames.Add("Column0");
        }
        else if (returnType.IsTuple())
        {
            var tupleType = (INamedTypeSymbol)returnType.UnwrapTaskType();

            if (tupleType.TupleElements != null && tupleType.TupleElements.Length > 0)
            {
                columnNames.AddRange(tupleType.TupleElements.Select(e => e.Name));
            }
            else
            {
                var tupleArgs = tupleType.TypeArguments;
                columnNames.AddRange(tupleArgs.Select((_, index) => $"Column{index}"));
            }
        }
        else
        {
            // For object types, get property names
            var properties = GetPropertySymbols(returnType);
            columnNames.AddRange(properties.Select(p => p.GetSqlName()));
        }

        return columnNames;
    }

    private void WriteCachedOrdinals(IndentedStringBuilder sb, List<string> columnNames)
    {
        foreach (var columnName in columnNames)
        {
            sb.AppendLine($"int __ordinal_{columnName} = {DbReaderName}.GetOrdinal(\"{columnName}\");");
        }
    }

    private ColumnDefine DeclareParamter(IndentedStringBuilder sb, ISymbol par, ITypeSymbol parType, string prefx)
    {
        var columnDefine = par.GetDbColumnAttribute();  // 使用扩展方法简化代码

        var visitPath = string.IsNullOrEmpty(prefx) ? string.Empty : prefx + ".";
        visitPath = visitPath.Replace(".", "?.");
        var parNamePrefx = string.IsNullOrEmpty(prefx) ? string.Empty : prefx.Replace(".", "_");

        // Generate C# variable name (remove @ prefix and make it a valid identifier)
        var sqlParamName = par.GetParameterName(SqlDef.ParameterPrefix + parNamePrefx);
        var parName = ParameterNameCleanupRegex.Replace(sqlParamName.TrimStart('@'), "_") + "_p";

        // Generate SQL parameter name
        var name = par.GetParameterName(SqlDef.ParameterPrefix);
        var dbType = parType.GetDbType();

        sb.AppendLine($"global::System.Data.Common.DbParameter {parName} = {CmdName}.CreateParameter();");
        sb.AppendLine($"{parName}.ParameterName = \"{name}\";");
        sb.AppendLine($"{parName}.DbType = {dbType};");
        sb.AppendLine($"{parName}.Value = (object?){visitPath}{par.Name} ?? global::System.DBNull.Value;");
        WriteParamterSpecial(sb, par, parName, columnDefine?.NamedArguments.ToDictionary(x => x.Key, x => x.Value.Value!) ?? new Dictionary<string, object>());

        return new ColumnDefine(parName, par);
    }

    private void WriteParamterSpecial(IndentedStringBuilder sb, ISymbol par, string parName, Dictionary<string, object> map)
    {
        if (map.TryGetValue("Precision", out var precision))
            sb.AppendLine($"{parName}.Precision = {precision};");
        if (map.TryGetValue("Scale", out var scale))
            sb.AppendLine($"{parName}.Scale = {scale};");
        if (map.TryGetValue("Size", out var size))
            sb.AppendLine($"{parName}.Size = {size};");
        if (map.TryGetValue("Direction", out var direction))
        {
            sb.AppendLine($"{parName}.Direction = global::System.Data.ParameterDirection.{direction};");
        }
        else if (par is IParameterSymbol parSymbol)
        {
            if (parSymbol.RefKind == RefKind.Ref)
            {
                sb.AppendLine($"{parName}.Direction = global::System.Data.ParameterDirection.InputOutput;");
            }
            else if (parSymbol.RefKind == RefKind.Out)
            {
                sb.AppendLine($"{parName}.Direction = global::System.Data.ParameterDirection.Output;");
            }
        }
    }

    private string? GetSql() =>
        GetSqlFromRawParameter() ??
        GetSqlFromSqlxAttribute() ??
        GetSqlFromSyntax();

    private string? GetSqlFromRawParameter()
    {
        if (RawSqlParameter?.GetAttribute("SqlxAttribute") is not { } attr)  // 使用扩展方法
            return null;

        if (attr.ConstructorArguments.Length == 0)
            return RawSqlParameter.Name;

        var sqlValue = ProcessSqlTemplate(attr.ConstructorArguments[0].Value?.ToString() ?? "");
        return $"@\"{EscapeSqlForCSharp(sqlValue)}\"";
    }

    private string? GetSqlFromSqlxAttribute()
    {
        var sqlxAttr = MethodSymbol.GetAttribute("SqlxAttribute");  // 使用扩展方法
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

    private void GenerateBatchInsertSql(IndentedStringBuilder sb, string sqlTemplate)
    {
        // Find the collection parameter (should be IEnumerable<T>)
        var collectionParameter = SqlParameters.FirstOrDefault(p => !p.Type.IsCachedScalarType());
        if (collectionParameter == null)
        {
            // Fallback to regular SQL with user-friendly error handling
            sb.AppendLine($"// Warning: No collection parameter found for batch INSERT");
            sb.AppendLine($"{CmdName}.CommandText = {sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "")};");
            return;
        }

        // Add null check for user safety
        sb.AppendLine($"if ({collectionParameter.Name} == null)");
        sb.AppendLine(SqlxExceptionMessages.GenerateArgumentNullCheck(collectionParameter.Name, SqlxExceptionMessages.CollectionParameterNull));
        sb.AppendLine();

        var objectMap = new ObjectMap(collectionParameter);
        var baseSql = sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "");
        // 性能优化：直接使用IEnumerable，避免创建中间List
        var properties = objectMap.Properties;

        // Generate optimized batch INSERT logic with StringBuilder for better performance
        sb.AppendLine($"var baseSql = \"{baseSql.Trim('"')}\";");
        sb.AppendLine($"var sqlBuilder = new global::System.Text.StringBuilder(baseSql);");
        sb.AppendLine($"var paramIndex = 0;");
        sb.AppendLine($"var isFirst = true;");
        sb.AppendLine();

        // Check for empty collection to avoid generating invalid SQL
        // 性能优化：使用Count检查集合是否为空，比Any()更直接
        sb.AppendLine($"if ({collectionParameter.Name}.Count == 0)");
        sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.EmptyCollection));
        sb.AppendLine();

        // Generate optimized loop with minimal allocations
        sb.AppendLine($"foreach (var item in {collectionParameter.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Add comma separator for subsequent VALUES clauses
        sb.AppendLine("if (!isFirst) sqlBuilder.Append(\", \");");
        sb.AppendLine("else isFirst = false;");
        sb.AppendLine();
        sb.AppendLine("sqlBuilder.Append(\"(\");");

        // Generate parameter creation with cleaner variable names
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var paramName = $"param_{property.Name}";

            sb.AppendLine($"var {paramName} = {CmdName}.CreateParameter();");
            sb.AppendLine($"{paramName}.ParameterName = $\"{SqlDef.ParameterPrefix}{property.GetParameterName(string.Empty)}_{{paramIndex}}\";");
            sb.AppendLine($"{paramName}.DbType = {property.Type.GetDbType()};");
            sb.AppendLine($"{paramName}.Value = (object?)item.{property.Name} ?? global::System.DBNull.Value;");
            sb.AppendLine($"{CmdName}.Parameters.Add({paramName});");

            // Add parameter to VALUES clause
            if (i > 0)
            {
                sb.AppendLine($"sqlBuilder.Append(\", \").Append({paramName}.ParameterName);");
            }
            else
            {
                sb.AppendLine($"sqlBuilder.Append({paramName}.ParameterName);");
            }

            if (i < properties.Count - 1) sb.AppendLine();
        }

        sb.AppendLine("sqlBuilder.Append(\")\");");
        sb.AppendLine("paramIndex++;");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Set the final optimized SQL
        sb.AppendLine($"{CmdName}.CommandText = sqlBuilder.ToString();");
    }

    private string? GetTimeoutExpression()
    {
        if (TimeoutParameter != null) return TimeoutParameter.Name;
        var methodTimeout = MethodSymbol.GetAttribute("TimeoutAttribute");  // 使用扩展方法
        if (methodTimeout != null && methodTimeout.ConstructorArguments.Length != 0)
            return methodTimeout.ConstructorArguments[0].Value!.ToString();

        var par = ClassGenerationContext.GetFieldOrProperty(x => x.GetAttributes().Any(y => y.AttributeClass?.Name == "TimeoutAttribute"));
        if (par != null) return par.Name;

        var classTimeout = ClassGenerationContext.GetAttribute(x => x.AttributeClass?.Name == "TimeoutAttribute");
        if (classTimeout != null && classTimeout.ConstructorArguments.Length != 0)
            return classTimeout.ConstructorArguments[0].Value!.ToString();

        return null;
    }

    private ReturnTypes GetReturnType()
    {
        // Treat non-generic Task as void-returning method
        if (MethodSymbol.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task" && (!taskType.IsGenericType || taskType.TypeArguments.Length == 0))
            return ReturnTypes.Void;
        if (ReturnType.SpecialType == SpecialType.System_Void) return ReturnTypes.Void;
        var actualType = ReturnType;

        // Check for SqlTemplate return type
        if (actualType.Name == "SqlTemplate" && 
            actualType.ContainingNamespace?.ToDisplayString() == "Sqlx")
        {
            return ReturnTypes.SqlTemplate;
        }

        if (actualType.Name == "IEnumerable") return ReturnTypes.IEnumerable;
        if (actualType.Name == Constants.TypeNames.IAsyncEnumerable) return ReturnTypes.IAsyncEnumerable;
        if (actualType.Name == "List"
            && actualType is INamedTypeSymbol symbol
            && symbol.IsGenericType
            && symbol.TypeParameters.Length == 1
            && symbol.TypeArguments[0] is INamedTypeSymbol mapType
            && mapType.IsGenericType
            && mapType.TypeArguments.Length == 2
            && mapType.TypeArguments[0].Name == "String"
            && mapType.TypeArguments[1].Name == "Object") return ReturnTypes.ListDictionaryStringObject;
        if (actualType.Name == "List" || actualType.Name == "IList") return ReturnTypes.List;
        if (actualType.IsCachedScalarType()) return ReturnTypes.Scalar;
        return ReturnTypes.Object;
    }

    public sealed record ColumnDefine(string ParameterName, ISymbol Symbol);

    private string GetEffectiveTableName(string defaultTableName)
    {
        // Check for TableName attribute on method
        var methodTableNameAttr = MethodSymbol.GetTableNameAttribute();
        if (methodTableNameAttr != null && methodTableNameAttr.ConstructorArguments.Length > 0)
        {
            return methodTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
        }

        // Check for TableName attribute on class
        var classTableNameAttr = ClassGenerationContext.ClassSymbol.GetTableNameAttribute();
        if (classTableNameAttr != null && classTableNameAttr.ConstructorArguments.Length > 0)
        {
            return classTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
        }

        // Check for TableName attribute on parameters
        foreach (var param in SqlParameters)
        {
            var paramTableNameAttr = param.GetTableNameAttribute();
            if (paramTableNameAttr != null && paramTableNameAttr.ConstructorArguments.Length > 0)
            {
                return paramTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
            }
        }

        return defaultTableName;
    }

    /// <summary>
    /// 检查方法的最佳实践并提供诊断建议
    /// </summary>
    public void PerformBestPracticeChecks()
    {
        CheckAsyncMethodBestPractices();
        CheckSqlSafetyPractices();
        CheckPerformanceOptimizations();
        CheckAdvancedQueryPatterns();
    }

    /// <summary>
    /// 检查异步方法的最佳实践
    /// </summary>
    private void CheckAsyncMethodBestPractices()
    {
        if (IsAsync)
        {
            var hasCancellationToken = MethodSymbol.Parameters.Any(p => p.Type.IsCancellationToken());
            if (!hasCancellationToken)
            {
                ReportDiagnostic(Messages.SP0024, MethodSymbol.Name);
            }
        }
    }

    /// <summary>
    /// 检查SQL安全性实践
    /// </summary>
    private void CheckSqlSafetyPractices()
    {
        var sql = GetSql()?.ToUpperInvariant();
        if (string.IsNullOrEmpty(sql)) return;

        // 检查DELETE没有WHERE子句
        if (sql.Contains("DELETE") && !sql.Contains("WHERE"))
        {
            ReportDiagnostic(Messages.SP0020);
        }

        // 检查UPDATE没有WHERE子句
        if (sql.Contains("UPDATE") && !sql.Contains("WHERE"))
        {
            ReportDiagnostic(Messages.SP0021);
        }

        // 检查SELECT *
        if (sql.Contains("SELECT *"))
        {
            ReportDiagnostic(Messages.SP0016);
        }
    }

    /// <summary>
    /// 检查性能优化建议
    /// </summary>
    private void CheckPerformanceOptimizations()
    {
        // 检查连接参数建议
        var hasDbConnectionParam = MethodSymbol.Parameters.Any(p => p.Type.IsDbConnection());
        var hasDbConnectionMember = ClassGenerationContext.DbConnection != null;

        if (!hasDbConnectionParam && hasDbConnectionMember)
        {
            ReportDiagnostic(Messages.SP0025);
        }
    }

    /// <summary>
    /// 检查高级查询模式和潜在问题
    /// </summary>
    private void CheckAdvancedQueryPatterns()
    {
        var sql = GetSql()?.ToUpperInvariant();
        if (string.IsNullOrEmpty(sql)) return;

        CheckForComplexJoins(sql);
        CheckForSubqueryOptimization(sql);
        CheckForLargeResultSets(sql);
        CheckForNPlusOneQuery();
        CheckForSyncVsAsync();
        CheckQueryComplexity(sql);
    }

    /// <summary>
    /// 检查复杂JOIN操作
    /// </summary>
    private void CheckForComplexJoins(string sql)
    {
        var joinCount = CountOccurrences(sql, "JOIN");
        if (joinCount >= 3)
        {
            ReportDiagnostic(Messages.SP0033);
        }
    }

    /// <summary>
    /// 检查子查询优化机会
    /// </summary>
    private void CheckForSubqueryOptimization(string sql)
    {
        if (sql.Contains("SELECT") && (sql.Contains("IN (SELECT") || sql.Contains("EXISTS (SELECT")))
        {
            ReportDiagnostic(Messages.SP0034, "Consider rewriting as JOIN");
        }
    }

    /// <summary>
    /// 检查大结果集警告
    /// </summary>
    private void CheckForLargeResultSets(string sql)
    {
        if (sql.Contains("SELECT") && !sql.Contains("LIMIT") && !sql.Contains("TOP") &&
            !sql.Contains("WHERE") && !sql.Contains("ROWNUM"))
        {
            ReportDiagnostic(Messages.SP0032);
        }
    }

    /// <summary>
    /// 检查潜在的N+1查询问题
    /// </summary>
    private void CheckForNPlusOneQuery()
    {
        // 检查方法名是否暗示单条记录查询但在循环上下文中使用
        var methodName = MethodSymbol.Name.ToLowerInvariant();
        if (methodName.Contains("get") && methodName.Contains("by") &&
            (methodName.Contains("id") || methodName.Contains("single")))
        {
            // 检查返回类型是否是单个对象而不是列表
            var returnType = MethodSymbol.ReturnType.ToDisplayString().ToLowerInvariant();
            if (!returnType.Contains("list") && !returnType.Contains("enumerable") && !returnType.Contains("array"))
            {
                ReportDiagnostic(Messages.SP0031, MethodSymbol.Name);
            }
        }
    }

    /// <summary>
    /// 检查同步vs异步模式
    /// </summary>
    private void CheckForSyncVsAsync()
    {
        var returnType = MethodSymbol.ReturnType.ToDisplayString();
        if (!returnType.Contains("Task") && !returnType.Contains("ValueTask"))
        {
            ReportDiagnostic(Messages.SP0038);
        }
    }

    /// <summary>
    /// 检查查询复杂度
    /// </summary>
    private void CheckQueryComplexity(string sql)
    {
        var complexityScore = 0;
        complexityScore += CountOccurrences(sql, "JOIN") * 2;
        complexityScore += CountOccurrences(sql, "UNION") * 3;
        complexityScore += CountOccurrences(sql, "SUBQUERY") * 2;
        complexityScore += CountOccurrences(sql, "CASE WHEN") * 1;
        complexityScore += CountOccurrences(sql, "GROUP BY") * 1;
        complexityScore += CountOccurrences(sql, "HAVING") * 2;

        if (complexityScore >= 8)
        {
            ReportDiagnostic(Messages.SP0040);
        }
    }

    /// <summary>
    /// 统计字符串中指定子字符串的出现次数
    /// </summary>
    private static int CountOccurrences(string text, string substring)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(substring))
            return 0;

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    /// <summary>
    /// 安全地报告诊断信息（只在有执行上下文时）
    /// </summary>
    private void ReportDiagnostic(DiagnosticDescriptor descriptor, params object[] messageArgs)
    {
        try
        {
            var context = ClassGenerationContext?.GeneratorExecutionContext;
            if (context.HasValue)
            {
                context.Value.ReportDiagnostic(
                    Diagnostic.Create(descriptor, MethodSymbol.Locations.FirstOrDefault(), messageArgs));
            }
        }
        catch
        {
            // 忽略诊断报告错误，不影响代码生成
        }
    }

    private bool GenerateBatchCommandLogic(IndentedStringBuilder sb)
    {
        // Find collection parameter
        var collectionParam = SqlParameters.FirstOrDefault(p => !p.Type.IsCachedScalarType());
        if (collectionParam == null)
        {
            sb.AppendLine(SqlxExceptionMessages.GenerateArgumentExceptionThrow(SqlxExceptionMessages.BatchCommandRequiresCollection));
            sb.PopIndent();
            sb.AppendLine("}");
            return true;
        }

        // Null check and validation
        sb.AppendLine($"if ({collectionParam.Name} == null)");
        sb.AppendLine(SqlxExceptionMessages.GenerateArgumentNullCheck(collectionParam.Name, SqlxExceptionMessages.CollectionParameterNull));
        sb.AppendLine();

        var returnType = GetReturnType();

        // Check if DbBatch is supported
        sb.AppendLine("// Try to use native DbBatch if supported, otherwise fallback to individual commands");
        sb.AppendLine($"if ({DbConnectionName} is global::System.Data.Common.DbConnection dbConn && dbConn.CanCreateBatch)");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate native DbBatch implementation
        GenerateNativeDbBatchLogic(sb, collectionParam, returnType);

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("else");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate fallback implementation
        GenerateFallbackBatchLogic(sb, collectionParam, returnType);

        sb.PopIndent();
        sb.AppendLine("}");

        sb.PopIndent();
        sb.AppendLine("}");
        return true;
    }

    private void GenerateNativeDbBatchLogic(IndentedStringBuilder sb, IParameterSymbol collectionParam, ReturnTypes returnType)
    {
        // Determine table, operation and properties
        var tempObjectMap = new ObjectMap(collectionParam);
        var entityType = tempObjectMap.ElementSymbol as INamedTypeSymbol;
        var tableName = GetEffectiveTableName(entityType?.Name ?? "UnknownTable");

        var objectMap = new ObjectMap(collectionParam);
        // 性能优化：避免ToList()，直接使用IEnumerable
        var properties = objectMap.Properties;
        var operationType = GetBatchOperationType();

        // Initialize return value for counting operations
        if (returnType == ReturnTypes.Scalar)
        {
            sb.AppendLine("int totalAffectedRows = 0;");
        }

        sb.AppendLine("using var batch = dbConn.CreateBatch();");
        if (TransactionParameter != null)
            sb.AppendLine($"batch.Transaction = {TransactionParameter.Name};");
        sb.AppendLine();

        // Build batch commands
        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("var batchCommand = batch.CreateBatchCommand();");

        // Generate SQL based on operation type
        switch (operationType)
        {
            case "INSERT":
                GenerateBatchInsertSql(sb, tableName!, properties);
                break;
            case "UPDATE":
                GenerateBatchUpdateSql(sb, tableName!, properties);
                break;
            case "DELETE":
                GenerateBatchDeleteSql(sb, tableName!, properties);
                break;
            default:
                GenerateBatchInsertSql(sb, tableName!, properties); // Default to INSERT
                break;
        }

        // Add parameters for each property
        foreach (var prop in properties)
        {
            var paramVar = $"param_{prop.Name.ToLowerInvariant()}";
            sb.AppendLine($"var {paramVar} = batchCommand.CreateParameter();");
            sb.AppendLine($"{paramVar}.ParameterName = \"{SqlDef.ParameterPrefix}{prop.GetSqlName()}\";");
            sb.AppendLine($"{paramVar}.DbType = {prop.Type.GetDbType()};");
            sb.AppendLine($"{paramVar}.Value = (object?)item.{prop.Name} ?? global::System.DBNull.Value;");
            sb.AppendLine($"batchCommand.Parameters.Add({paramVar});");
        }

        sb.AppendLine("batch.BatchCommands.Add(batchCommand);");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute the batch
        if (IsAsync)
        {
            if (returnType == ReturnTypes.Scalar)
            {
                sb.AppendLine("totalAffectedRows = await batch.ExecuteNonQueryAsync();");
            }
            else
            {
                sb.AppendLine("await batch.ExecuteNonQueryAsync();");
            }
        }
        else
        {
            if (returnType == ReturnTypes.Scalar)
            {
                sb.AppendLine("totalAffectedRows = batch.ExecuteNonQuery();");
            }
            else
            {
                sb.AppendLine("batch.ExecuteNonQuery();");
            }
        }

        // Return result if needed
        if (returnType == ReturnTypes.Scalar)
        {
            sb.AppendLine("return totalAffectedRows;");
        }
    }

    private void GenerateFallbackBatchLogic(IndentedStringBuilder sb, IParameterSymbol collectionParam, ReturnTypes returnType)
    {
        // Determine table and properties
        var tempObjectMap = new ObjectMap(collectionParam);
        var entityType = tempObjectMap.ElementSymbol as INamedTypeSymbol;
        var tableName = GetEffectiveTableName(entityType?.Name ?? "UnknownTable");

        var objectMap = new ObjectMap(collectionParam);
        // 性能优化：避免ToList()，直接使用IEnumerable
        var properties = objectMap.Properties;
        var operationType = GetBatchOperationType();

        // Initialize return value for counting operations
        if (returnType == ReturnTypes.Scalar)
        {
            sb.AppendLine("int totalAffectedRows = 0;");
        }

        // Precompute column and value placeholders
        sb.AppendLine($"var __columns__ = \"{string.Join(", ", properties.Select(p => SqlDef.WrapColumn(p.Name)))}\";");
        sb.AppendLine($"var __values__ = \"{string.Join(", ", properties.Select(p => SqlDef.ParameterPrefix + p.GetParameterName(string.Empty)))}\";");
        sb.AppendLine();

        // Build individual commands using foreach loop
        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate command text based on operation type
        switch (operationType)
        {
            case "INSERT":
                sb.AppendLine($"{CmdName}.CommandText = \"INSERT INTO {SqlDef.WrapColumn(tableName!)} (\" + __columns__ + \") VALUES (\" + __values__ + \")\";");
                break;
            case "UPDATE":
                var setProperties = GetSetProperties(properties);
                var whereProperties = GetWhereProperties(properties);

                // 性能优化：使用Count检查集合是否为空，比Any()更直接
                if (setProperties.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoSetProperties));
                    break;
                }

                if (whereProperties.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoWherePropertiesUpdate));
                    break;
                }

                var setClause = string.Join(CommaSpace, setProperties.Select(p => $"{SqlDef.WrapColumn(p.Name)} = {SqlDef.ParameterPrefix}{p.GetParameterName(string.Empty)}"));
                var whereClause = string.Join(SqlAnd, whereProperties.Select(p => GenerateWhereCondition(p)));
                sb.AppendLine($"{CmdName}.CommandText = \"UPDATE {SqlDef.WrapColumn(tableName!)} SET {setClause} WHERE {whereClause}\";");
                break;
            case "DELETE":
                var deleteWhereProperties = GetWhereProperties(properties);

                // 性能优化：使用Count检查集合是否为空，比Any()更直接
                if (deleteWhereProperties.Count == 0)
                {
                    sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoWherePropertiesDelete));
                    break;
                }

                var deleteWhereClause = string.Join(SqlAnd, deleteWhereProperties.Select(p => GenerateWhereCondition(p)));
                sb.AppendLine($"{CmdName}.CommandText = \"DELETE FROM {SqlDef.WrapColumn(tableName!)} WHERE {deleteWhereClause}\";");
                break;
            default:
                sb.AppendLine($"{CmdName}.CommandText = \"INSERT INTO {SqlDef.WrapColumn(tableName!)} (\" + __columns__ + \") VALUES (\" + __values__ + \")\";");
                break;
        }

        sb.AppendLine($"{CmdName}.Parameters.Clear();");

        // Set transaction and timeout if specified
        if (TransactionParameter != null)
            sb.AppendLine($"{CmdName}.Transaction = {TransactionParameter.Name};");
        var timeoutExpression = GetTimeoutExpression();
        if (!string.IsNullOrWhiteSpace(timeoutExpression))
            sb.AppendLine($"{CmdName}.CommandTimeout = {timeoutExpression};");

        // Add parameters for each property
        foreach (var prop in properties)
        {
            var paramVar = $"param_{prop.Name.ToLowerInvariant()}";
            sb.AppendLine($"var {paramVar} = {CmdName}.CreateParameter();");
            sb.AppendLine($"{paramVar}.ParameterName = \"{SqlDef.ParameterPrefix}{prop.GetParameterName(string.Empty)}\";");
            sb.AppendLine($"{paramVar}.DbType = {prop.Type.GetDbType()};");
            sb.AppendLine($"{paramVar}.Value = (object?)item.{prop.Name} ?? global::System.DBNull.Value;");
            sb.AppendLine($"{CmdName}.Parameters.Add({paramVar});");
        }

        // Execute the command
        if (IsAsync)
        {
            if (returnType == ReturnTypes.Scalar)
            {
                sb.AppendLine($"totalAffectedRows += await {CmdName}.ExecuteNonQueryAsync();");
            }
            else
            {
                sb.AppendLine($"await {CmdName}.ExecuteNonQueryAsync();");
            }
        }
        else
        {
            if (returnType == ReturnTypes.Scalar)
            {
                sb.AppendLine($"totalAffectedRows += {CmdName}.ExecuteNonQuery();");
            }
            else
            {
                sb.AppendLine($"{CmdName}.ExecuteNonQuery();");
            }
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Return result if needed
        if (returnType == ReturnTypes.Scalar)
        {
            sb.AppendLine("return totalAffectedRows;");
        }
    }

    private void GenerateBatchInsertSql(IndentedStringBuilder sb, string tableName, List<IPropertySymbol> properties)
    {
        var columns = string.Join(CommaSpace, properties.Select(EscapePropertyForSqlString));
        var values = string.Join(CommaSpace, properties.Select(p => SqlDef.ParameterPrefix + p.GetSqlName()));
        var wrappedTable = EscapeForSqlString(tableName);
        sb.AppendLine($"batchCommand.CommandText = \"INSERT INTO {wrappedTable} ({columns}) VALUES ({values})\";");
    }

    private void GenerateBatchUpdateSql(IndentedStringBuilder sb, string tableName, List<IPropertySymbol> properties)
    {
        var setProperties = GetSetProperties(properties);
        var whereProperties = GetWhereProperties(properties);

        // 性能优化：使用Count检查集合是否为空，比Any()更直接
        if (setProperties.Count == 0)
        {
            sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoSetProperties));
            return;
        }

        if (whereProperties.Count == 0)
        {
            sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoWherePropertiesUpdate));
            return;
        }

        var setClause = string.Join(CommaSpace, setProperties.Select(p => $"{EscapePropertyForSqlString(p)} = {SqlDef.ParameterPrefix}{p.GetSqlName()}"));
        var whereClause = string.Join(SqlAnd, whereProperties.Select(p => GenerateWhereCondition(p)));
        var wrappedTable = EscapeForSqlString(tableName);

        sb.AppendLine($"batchCommand.CommandText = \"UPDATE {wrappedTable} SET {setClause} WHERE {whereClause}\";");
    }

    private void GenerateBatchDeleteSql(IndentedStringBuilder sb, string tableName, List<IPropertySymbol> properties)
    {
        var whereProperties = GetWhereProperties(properties);

        // 性能优化：使用Count检查集合是否为空，比Any()更直接
        if (whereProperties.Count == 0)
        {
            sb.AppendLine(SqlxExceptionMessages.GenerateInvalidOperationThrow(SqlxExceptionMessages.NoWherePropertiesDelete));
            return;
        }

        var whereClause = string.Join(SqlAnd, whereProperties.Select(p => GenerateWhereCondition(p)));
        var wrappedTable = EscapeForSqlString(tableName);
        sb.AppendLine($"batchCommand.CommandText = \"DELETE FROM {wrappedTable} WHERE {whereClause}\";");
    }

    private string GetBatchOperationType()
    {
        // Infer operation type from method name
        return GetOperationFromMethodName();
    }

    private string GetOperationFromMethodName()
    {
        // Try to infer operation type from method name
        var methodName = MethodSymbol.Name.ToUpperInvariant();

        if (methodName.Contains(SqlInsert) || methodName.Contains("ADD") || methodName.Contains("CREATE"))
            return SqlInsert;
        if (methodName.Contains(SqlUpdate) || methodName.Contains("MODIFY") || methodName.Contains("CHANGE"))
            return SqlUpdate;
        if (methodName.Contains(SqlDelete) || methodName.Contains("REMOVE"))
            return SqlDelete;

        // 激进优化：直接抛出异常，强制明确操作类型，提高代码质量
        throw new InvalidOperationException($"Cannot infer operation type from method name '{methodName}'. Use explicit SqlExecuteType attribute.");
    }

    private bool IsKeyProperty(IPropertySymbol property) =>
        property.Name.ToUpperInvariant() is var name && (name is "ID" or "KEY" || name.EndsWith("ID") || name.EndsWith("KEY"));

    private List<IPropertySymbol> GetSetProperties(List<IPropertySymbol> properties)
    {
        // 性能优化：单次遍历查找 [Set] 属性
        var explicitSetProperties = new List<IPropertySymbol>();
        foreach (var property in properties)
        {
            if (HasSetAttribute(property))
                explicitSetProperties.Add(property);
        }

        if (explicitSetProperties.Count > 0)
        {
            return explicitSetProperties;
        }

        // If no explicit marking, use all non-Where properties (excluding primary keys)
        var whereProperties = GetWhereProperties(properties);
        var whereSet = new HashSet<IPropertySymbol>(whereProperties, SymbolEqualityComparer.Default); // 性能优化：使用HashSet提高查找效率

        // 性能优化：单次遍历过滤非Where和非Key属性
        var result = new List<IPropertySymbol>();
        foreach (var property in properties)
        {
            if (!whereSet.Contains(property) && !IsKeyProperty(property))
                result.Add(property);
        }
        return result;
    }

    private List<IPropertySymbol> GetWhereProperties(List<IPropertySymbol> properties)
    {
        // 性能优化：单次遍历查找 [Where] 属性
        var explicitWhereProperties = new List<IPropertySymbol>();
        foreach (var property in properties)
        {
            if (HasWhereAttribute(property))
                explicitWhereProperties.Add(property);
        }

        if (explicitWhereProperties.Count > 0)
        {
            return explicitWhereProperties;
        }

        // 性能优化：单次遍历查找 Key 属性
        var keyProperties = new List<IPropertySymbol>();
        foreach (var property in properties)
        {
            if (IsKeyProperty(property))
                keyProperties.Add(property);
        }
        return keyProperties;
    }

    private bool HasSetAttribute(IPropertySymbol property) => HasAttributeByName(property, "SetAttribute");

    private bool HasWhereAttribute(IPropertySymbol property) => HasAttributeByName(property, "WhereAttribute");

    private string GenerateWhereCondition(IPropertySymbol property)
    {
        // 性能优化：使用缓存的属性查找
        var attributes = GetCachedPropertyAttributes(property);
        var whereAttr = attributes.FirstOrDefault(a => a.AttributeClass?.Name == "WhereAttribute");
        var operatorStr = "="; // Default operator

        if (whereAttr != null && whereAttr.ConstructorArguments.Length > 0)
        {
            operatorStr = whereAttr.ConstructorArguments[0].Value?.ToString() ?? "=";
        }
        else if (whereAttr != null)
        {
            // Check Operator property
            var operatorProp = whereAttr.NamedArguments.FirstOrDefault(na => na.Key == "Operator");
            if (!operatorProp.Equals(default(KeyValuePair<string, Microsoft.CodeAnalysis.TypedConstant>)))
            {
                operatorStr = operatorProp.Value.Value?.ToString() ?? "=";
            }
        }

        return $"{EscapePropertyForSqlString(property)} {operatorStr} {SqlDef.ParameterPrefix}{property.GetSqlName()}";
    }


    private string GetDbConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        // Find the first DbConnection field or property
        var dbConnectionMember = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.IsDbConnection()) ??
            repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(x => x.IsDbConnection()) as ISymbol;

        // If not found, check for primary constructor parameter with DbConnection
        if (dbConnectionMember == null)
        {
            var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
            if (primaryConstructor != null)
            {
                var connectionParam = primaryConstructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
                if (connectionParam != null)
                {
                    // For primary constructor parameters, use the parameter name as field name
                    return connectionParam.Name;
                }
            }

        }

        // If not found in the class, check base class
        if (dbConnectionMember == null && repositoryClass.BaseType != null)
        {
            return GetDbConnectionFieldName(repositoryClass.BaseType);
        }

        return dbConnectionMember?.Name ?? "connection"; // Fallback to "connection" if not found
    }

    private string GetDbContextFieldName(INamedTypeSymbol repositoryClass)
    {
        // Find the first DbContext field or property
        var dbContextMember = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.IsDbContext()) ??
            repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(x => x.IsDbContext()) as ISymbol;

        // If not found, check for primary constructor parameter with DbContext
        if (dbContextMember == null)
        {
            var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
            if (primaryConstructor != null)
            {
                var contextParam = primaryConstructor.Parameters.FirstOrDefault(p => p.Type.IsDbContext());
                if (contextParam != null)
                {
                    // For primary constructor parameters, use the parameter name as field name
                    return contextParam.Name;
                }
            }

            // Also check regular constructors for backward compatibility
            var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
            if (constructor != null)
            {
                var contextParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbContext());
                if (contextParam != null)
                {
                    return contextParam.Name;
                }
            }
        }

        // If not found in the class, check base class
        if (dbContextMember == null && repositoryClass.BaseType != null)
        {
            return GetDbContextFieldName(repositoryClass.BaseType);
        }

        return dbContextMember?.Name ?? "this.dbContext"; // Fallback to "this.dbContext" if not found
    }

    private void GenerateParameterNullChecks(IndentedStringBuilder sb)
    {
        // Generate null checks for non-nullable reference type parameters
        // This implements fail-fast principle by checking parameters before opening connection

        // 性能优化：单次遍历过滤需要检查的参数，并直接生成代码
        bool hasNullChecks = false;
        foreach (var param in SqlParameters)
        {
            if (ShouldGenerateNullCheck(param))
            {
                if (!hasNullChecks)
                {
                    sb.AppendLine("// Parameter null checks (fail fast)");
                    hasNullChecks = true;
                }
                sb.AppendLine($"if ({param.Name} == null)");
                sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({param.Name}));");
            }
        }

        if (hasNullChecks)
        {
            sb.AppendLine();
        }
    }

    private string ProcessSqlTemplate(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return sql;

        // 性能优化：使用SqlTemplateEngine替代重复的SqlTemplatePlaceholder
        var templateEngine = new SqlTemplateEngine(SqlDef);
        var result = templateEngine.ProcessTemplate(sql, MethodSymbol, null, string.Empty, SqlDef);
        return result.ProcessedSql;
    }

    private bool ShouldGenerateNullCheck(IParameterSymbol parameter)
    {
        // Skip system parameters
        var typeName = parameter.Type.ToDisplayString();
        if (typeName == "CancellationToken" ||
            typeName.Contains("DbTransaction") ||
            typeName.Contains("IDbTransaction") ||
            typeName.Contains("DbConnection") ||
            typeName.Contains("IDbConnection"))
        {
            return false;
        }

        // Skip parameters with special attributes
        if (parameter.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "TimeoutAttribute" ||
            a.AttributeClass?.Name == "ExpressionToSqlAttribute" ||
            a.AttributeClass?.Name == "ExpressionToSql"))
        {
            return false;
        }

        // Check if parameter is a reference type that could be null
        if (parameter.Type.IsReferenceType)
        {
            // For strings, let individual operations handle nullability
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                return false;
            }

            // Check for collection types that should not be null
            if (parameter.Type is INamedTypeSymbol namedType)
            {
                var baseTypeName = namedType.Name;
                if (baseTypeName == "IEnumerable" ||
                    baseTypeName == "List" ||
                    baseTypeName == "IList" ||
                    baseTypeName == "ICollection" ||
                    (namedType.IsGenericType && namedType.TypeArguments.Length > 0))
                {
                    return true; // Collections should not be null
                }
            }

            // Check for entity types (custom classes)
            if (parameter.Type.TypeKind == TypeKind.Class &&
                !parameter.Type.ToDisplayString().StartsWith("System."))
            {
                return true; // Custom entity types should not be null
            }
        }

        return false;
    }

    /// <summary>
    /// Generates code that returns SqlTemplate without executing the query.
    /// </summary>
    private bool GenerateSqlTemplateReturn(IndentedStringBuilder sb, string? sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(
                Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
            return false;
        }

        // Check for batch INSERT operations (case-insensitive)
        if (sql.IndexOf("{{VALUES_PLACEHOLDER}}", StringComparison.OrdinalIgnoreCase) >= 0 ||
            sql.IndexOf("{{values_placeholder}}", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return GenerateBatchInsertSqlTemplate(sb, sql);
        }

        // Create parameters dictionary
        sb.AppendLine("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
        sb.AppendLine();

        // Add parameters to dictionary
        foreach (var param in SqlParameters)
        {
            var isScalarType = param.Type.IsCachedScalarType();
            if (isScalarType)
            {
                AddParameterToDictionary(sb, param, param.Type, string.Empty);
            }
            else
            {
                // Handle complex type parameters
                foreach (var property in param.Type.GetMembers().OfType<IPropertySymbol>())
                {
                    AddParameterToDictionary(sb, property, property.Type, param.Name);
                }
            }
        }

        // Return SqlTemplate
        sb.AppendLine($"var __result__ = new global::Sqlx.SqlTemplate({sql}, parameters);");
        
        // Call MethodExecuted event
        WriteMethodExecuted(sb, "__result__");
        
        sb.AppendLine("return __result__;");

        // Close try block
        sb.PopIndent();
        sb.AppendLine("}");
        
        // Add catch block
        sb.AppendLine("catch (global::System.Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{MethodExecuteFail}({MethodNameString}, {CmdName}, ex, ");
        sb.AppendLine($"    {GetTimestampMethod} - {StartTimeName});");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");

        // Close the method
        sb.PopIndent();
        sb.AppendLine("}");

        return true;
    }

    /// <summary>
    /// Adds a parameter to the parameters dictionary.
    /// </summary>
    private void AddParameterToDictionary(
        IndentedStringBuilder sb,
        ISymbol symbol,
        ITypeSymbol type,
        string prefix)
    {
        var paramName = symbol.GetParameterName(SqlDef.ParameterPrefix);
        var visitPath = string.IsNullOrEmpty(prefix)
            ? symbol.Name
            : $"{prefix}?.{symbol.Name}";

        sb.AppendLine($"parameters[\"{paramName}\"] = {visitPath};");
    }

    /// <summary>
    /// Generates SqlTemplate for batch INSERT operations.
    /// </summary>
    private bool GenerateBatchInsertSqlTemplate(IndentedStringBuilder sb, string sqlTemplate)
    {
        // Find the collection parameter
        var collectionParameter = SqlParameters.FirstOrDefault(p => !p.Type.IsCachedScalarType());
        if (collectionParameter == null)
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(
                Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
            return false;
        }

        var objectMap = new ObjectMap(collectionParameter);
        // Handle both uppercase and lowercase placeholders using Regex for case-insensitive replacement
        var baseSql = System.Text.RegularExpressions.Regex.Replace(
            sqlTemplate, 
            @"\{\{VALUES_PLACEHOLDER\}\}", 
            "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        sb.AppendLine($"var baseSql = \"{baseSql.Trim('"')}\";");
        sb.AppendLine("var sqlBuilder = new global::System.Text.StringBuilder(baseSql);");
        sb.AppendLine("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
        sb.AppendLine("var paramIndex = 0;");
        sb.AppendLine("var isFirst = true;");
        sb.AppendLine();

        // Check for empty collection
        sb.AppendLine($"if ({collectionParameter.Name} == null || !{collectionParameter.Name}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("throw new global::System.InvalidOperationException(\"Collection parameter cannot be null or empty\");");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate loop
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

            if (i > 0)
            {
                sb.AppendLine("sqlBuilder.Append(\", \");");
            }

            sb.AppendLine($"sqlBuilder.Append(\"{paramName}\");");
            sb.AppendLine($"parameters[\"{paramName}\"] = item.{property.Name};");
        }

        sb.AppendLine("sqlBuilder.Append(\")\");");
        sb.AppendLine("paramIndex++;");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("var __result__ = new global::Sqlx.SqlTemplate(sqlBuilder.ToString(), parameters);");
        
        // Call MethodExecuted event
        WriteMethodExecuted(sb, "__result__");
        
        sb.AppendLine("return __result__;");

        // Close try block
        sb.PopIndent();
        sb.AppendLine("}");
        
        // Add catch block
        sb.AppendLine("catch (global::System.Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{MethodExecuteFail}({MethodNameString}, {CmdName}, ex, ");
        sb.AppendLine($"    {GetTimestampMethod} - {StartTimeName});");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");

        // Close the method
        sb.PopIndent();
        sb.AppendLine("}");

        return true;
    }
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
