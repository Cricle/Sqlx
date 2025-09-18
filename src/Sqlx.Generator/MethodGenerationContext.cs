// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sqlx.SqlGen;
using Sqlx.Generator.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using static Sqlx.Extensions;

internal partial class MethodGenerationContext : GenerationContextBase
{
    internal const string DbConnectionName = Constants.GeneratedVariables.Connection;
    internal const string CmdName = "__methodCmd__";  // Use different name to avoid conflicts with AbstractGenerator
    internal const string DbReaderName = "__methodReader__";
    internal const string ResultName = "__methodResult__";
    internal const string DataName = "__methodData__";
    internal const string StartTimeName = "__methodStartTime__";

    internal const string MethodExecuting = "OnExecuting";
    internal const string MethodExecuted = "OnExecuted";
    internal const string MethodExecuteFail = "OnExecuteFail";

    internal const string GetTimestampMethod = "global::System.Diagnostics.Stopwatch.GetTimestamp()";

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

#pragma warning disable CS0219 // Variable assigned but never used
        var rawSqlIsInParamter = true;
        var rawSqlShouldRemoveFromParams = false;
#pragma warning restore CS0219
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
        IsAsync = MethodSymbol.ReturnType?.Name == "Task" || MethodSymbol.ReturnType?.Name == Consts.IAsyncEnumerable;
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
    public bool ReturnIsEnumerable => ReturnType.Name == "IEnumerable" || ReturnType.Name == Consts.IAsyncEnumerable;

    private SqlDefine SqlDef { get; }

    private string MethodNameString => $"\"{MethodSymbol.Name}\"";

    public bool DeclareCommand(IndentedStringBuilder sb)
    {
        var args = string.Join(", ", MethodSymbol.Parameters.Select(x =>
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

        // Check if this is a BatchCommand operation
        var sqlExecuteTypeAttr = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        var isBatchCommand = false;
        if (sqlExecuteTypeAttr != null)
        {
            var enumValueObj = sqlExecuteTypeAttr.ConstructorArguments[0].Value;
            var type = enumValueObj switch
            {
                int intValue => intValue,
                string strValue when int.TryParse(strValue, out var intVal) => intVal,
                _ => Constants.SqlExecuteTypeValues.Select
            };
            isBatchCommand = type == Constants.SqlExecuteTypeValues.BatchCommand ||
                           type == Constants.SqlExecuteTypeValues.BatchInsert ||
                           type == Constants.SqlExecuteTypeValues.BatchUpdate ||
                           type == Constants.SqlExecuteTypeValues.BatchDelete;
        }

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

        // Handle BatchCommand operations first
        if (isBatchCommand)
        {
            return GenerateBatchCommandLogic(sb);
        }

        // Legacy batch operations detection - redirect to proper BatchCommand
        if (!string.IsNullOrEmpty(sql) && sql?.Contains("BATCH") == true)
        {
            // Generate proper error message for better developer experience
            sb.AppendLine($"// Legacy batch operation detected: {sql}");
            sb.AppendLine("// Use Constants.SqlExecuteTypeValues.BatchCommand for proper batch operations");
            sb.AppendLine("throw new global::System.ArgumentException(\"Legacy BATCH SQL syntax detected. Please use Constants.SqlExecuteTypeValues.BatchCommand for proper batch operations.\");");
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
                // Static SQL case (from SqlExecuteType, RawSql, or stored procedure)
                sb.AppendLine($"{CmdName}.CommandText = {sql};");

                // If we have ExpressionToSql parameter and this is a SqlExecuteType operation, 
                // append dynamic WHERE clause for SELECT/DELETE or handle UPDATE specially
                if (ExpressionToSqlParameter != null)
                {
                    var sqlExecuteType = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
                    if (sqlExecuteType != null)
                    {
                        var type = (int)Enum.Parse(typeof(int), sqlExecuteType.ConstructorArguments[0].Value?.ToString() ?? "0");
                        GenerateExpressionToSqlEnhancement(sb, type);
                    }
                }
            }
        }

        sb.AppendLine();

        // Paramters - skip for batch INSERT operations as they're handled in GenerateBatchInsertSql
        var columnDefines = new List<ColumnDefine>();
        var isBatchInsert = !string.IsNullOrEmpty(sql) && sql?.Contains("{{VALUES_PLACEHOLDER}}") == true;

        if (!isBatchInsert)
        {
            foreach (var item in SqlParameters)
            {
                var isScalarType = item.Type.IsScalarType();
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
        else if (ReturnType.IsScalarType())
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

        // Check for SqlExecuteTypeAttribute with INSERT/UPDATE/DELETE operations
        var sqlExecuteTypeAttr = MethodSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (sqlExecuteTypeAttr != null && sqlExecuteTypeAttr.ConstructorArguments.Length > 0)
        {
            var enumValueObj = sqlExecuteTypeAttr.ConstructorArguments[0].Value;
            var sqlExecuteType = enumValueObj switch
            {
                int intValue => intValue,
                string strValue when int.TryParse(strValue, out var intVal) => intVal,
                _ => 0 // Select
            };

            // INSERT (2), UPDATE (1), DELETE (3) should use ExecuteNonQuery
            return sqlExecuteType == 1 || sqlExecuteType == 2 || sqlExecuteType == 3;
        }

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
        var cancellationTokenName = CancellationTokenParameter?.Name ?? string.Empty;

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
                sb.AppendLine($"if({ResultName} == null) throw new global::System.InvalidOperationException(");
                sb.AppendLine($"    \"Sequence contains no elements\");");
            }

            // Direct assignment and return with proper conversion since ExecuteScalar returns object
            if (ReturnType.SpecialType == SpecialType.System_Int32)
            {
                sb.AppendLine($"return global::System.Convert.ToInt32({ResultName});");  // Handle SQLite Int64->Int32 conversion
            }
            else if (ReturnType.SpecialType == SpecialType.System_Int64)
            {
                sb.AppendLine($"return global::System.Convert.ToInt64({ResultName});");  // Convert to long
            }
            else if (ReturnType.SpecialType == SpecialType.System_Boolean)
            {
                sb.AppendLine($"return global::System.Convert.ToBoolean({ResultName});");  // Convert to bool
            }
            else if (ReturnType.SpecialType == SpecialType.System_Decimal)
            {
                sb.AppendLine($"return global::System.Convert.ToDecimal({ResultName});");  // Handle Double->Decimal conversion
            }
            else if (ReturnType.SpecialType == SpecialType.System_Double)
            {
                sb.AppendLine($"return global::System.Convert.ToDouble({ResultName});");  // Convert to double
            }
            else if (ReturnType.SpecialType == SpecialType.System_Single)
            {
                sb.AppendLine($"return global::System.Convert.ToSingle({ResultName});");  // Convert to float
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

                // Cache column ordinals for performance
                var columnNames = GetColumnNames(returnType);
                WriteCachedOrdinals(sb, columnNames);

                WriteBeginReader(sb);

                if (returnType.IsScalarType())
                {
                    sb.AppendLineIf(isList, $"{ResultName}.Add({returnType.GetDataReadExpressionWithCachedOrdinal(DbReaderName, "Column0", "__ordinal_Column0")});", $"yield return {returnType.GetDataReadExpressionWithCachedOrdinal(DbReaderName, "Column0", "__ordinal_Column0")};");
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

                    var tupleJoins = string.Join(", ", tupleArgs.Select((x, index) =>
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
                var properties = ElementType.GetMembers().OfType<IPropertySymbol>().Where(x => !construct.Parameters.All(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase))).ToList();
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
        var setElement = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbSetTypeAttribute");
        if (setElement == null)
        {
            // Do not report diagnostic; allow fallback to ElementType
            return false;
        }

        symbol = (ISymbol)setElement.ConstructorArguments[0].Value!;
        return true;
    }

    private List<IPropertySymbol> GetPropertySymbols(ITypeSymbol symbol)
    {
        var writeableProperties = symbol.GetMembers().OfType<IPropertySymbol>()
            .Where(x => !x.IsReadOnly && !x.GetAttributes().Any(y => y.AttributeClass?.Name == "BrowsableAttribute" && y.ConstructorArguments.FirstOrDefault().Value is bool b && !b))
            .ToList();
        return writeableProperties;
    }

    private void WriteDeclareReturnList(IndentedStringBuilder sb)
    {
        sb.AppendLine($"global::System.Collections.Generic.List<{ElementType.ToDisplayString()}> {ResultName} = new global::System.Collections.Generic.List<{ElementType.ToDisplayString()}>();");
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
        var newExp = IsTuple(symbol) ? string.Empty : "new ";
        var expCall = IsTuple(symbol) ? string.Empty : " ()";

        // Check if the type is abstract or cannot be instantiated
        if (symbol.IsAbstract || symbol.TypeKind == TypeKind.Interface || symbol.Name == "DbDataReader")
        {
            // For abstract types like DbDataReader, we can't instantiate them directly
            // Return the reader itself or handle specially
            if (symbol.Name == "DbDataReader")
            {
                sb.AppendLine($"{symbol.ToDisplayString()} {DataName} = {DbReaderName};");
            }
            else
            {
                sb.AppendLine($"{symbol.ToDisplayString()} {DataName} = default({symbol.ToDisplayString(NullableFlowState.None)});");
            }
        }
        else if (symbol is INamedTypeSymbol namedType)
        {
            // Check if this is a record or has a primary constructor
            if (PrimaryConstructorAnalyzer.IsRecord(namedType) || PrimaryConstructorAnalyzer.HasPrimaryConstructor(namedType))
            {
                // Use enhanced entity mapping for records and primary constructors
                sb.AppendLine($"// Enhanced entity mapping for {(PrimaryConstructorAnalyzer.IsRecord(namedType) ? "record" : "primary constructor")} type");
                EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, namedType);
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
                sb.AppendLine($"{symbol.ToDisplayString(NullableFlowState.None)} {DataName} = {newExp}{symbol.ToDisplayString(NullableFlowState.None)}{expCall}!;");
            }
        }
        else
        {
            // Fallback for non-named types
            sb.AppendLine($"{symbol.ToDisplayString(NullableFlowState.None)} {DataName} = {newExp}{symbol.ToDisplayString(NullableFlowState.None)}{expCall}!;");
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

            return new SqlDefine(
                methodDef.ConstructorArguments[0].Value?.ToString() ?? "[",
                methodDef.ConstructorArguments[1].Value?.ToString() ?? "]",
                methodDef.ConstructorArguments[2].Value?.ToString() ?? "'",
                methodDef.ConstructorArguments[3].Value?.ToString() ?? "'",
                methodDef.ConstructorArguments[4].Value?.ToString() ?? "@");
        }

        // Try to infer database dialect from the connection type
        var inferredDialect = InferDialectFromConnectionType(ClassGenerationContext.ClassSymbol);
        if (inferredDialect.HasValue)
        {
            return inferredDialect.Value;
        }

        // Default to SqlServer as fallback
        return SqlDefine.SqlServer;
    }

    private SqlDefine? InferDialectFromConnectionType(INamedTypeSymbol repositoryClass)
    {
        // Find DbConnection field or property in the repository class
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.IsDbConnection());

        if (connectionField != null)
        {
            var connectionTypeName = connectionField.Type.ToDisplayString();
            return InferDialectFromConnectionTypeName(connectionTypeName);
        }

        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(x => x.IsDbConnection());

        if (connectionProperty != null)
        {
            var connectionTypeName = connectionProperty.Type.ToDisplayString();
            return InferDialectFromConnectionTypeName(connectionTypeName);
        }

        // Look for constructor parameter with connection type
        if (!repositoryClass.InstanceConstructors.IsDefaultOrEmpty)
        {
            var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
            if (constructor != null)
            {
                var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
                if (connectionParam != null)
                {
                    var connectionTypeName = connectionParam.Type.ToDisplayString();
                    return InferDialectFromConnectionTypeName(connectionTypeName);
                }
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
        var columnNames = new List<string>();

        if (returnType.IsScalarType())
        {
            columnNames.Add("Column0");
        }
        else if (IsTuple(returnType))
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
        var columnDefine = par.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbColumnAttribute");

        var visitPath = string.IsNullOrEmpty(prefx) ? string.Empty : prefx + ".";
        visitPath = visitPath.Replace(".", "?.");
        var parNamePrefx = string.IsNullOrEmpty(prefx) ? string.Empty : prefx.Replace(".", "_");

        // Generate C# variable name (remove @ prefix and make it a valid identifier)
        var sqlParamName = par.GetParameterName(SqlDef.ParameterPrefix + parNamePrefx);
        var parName = Regex.Replace(sqlParamName.TrimStart('@'), "[^a-zA-Z0-9_]", "_") + "_p";

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

    private string? GetSql()
    {
        if (RawSqlParameter != null)
        {
            var attr = RawSqlParameter.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlxAttribute");
            if (attr != null)
            {
                if (attr.ConstructorArguments.Length == 0)
                {
                    return RawSqlParameter.Name;
                }

                var sqlValue = attr.ConstructorArguments[0].Value?.ToString() ?? "";

                // 处理 SQL 模板占位符
                if (!string.IsNullOrEmpty(sqlValue) && SqlTemplatePlaceholder.ContainsPlaceholders(sqlValue))
                {
                    var context = new SqlPlaceholderContext(SqlDef)
                    {
                        Method = MethodSymbol,
                        TableName = null, // 临时简化
                        EntityType = null // 临时简化
                    };
                    sqlValue = SqlTemplatePlaceholder.ProcessTemplate(sqlValue, context);
                }

                // Escape the SQL string properly for C# code generation
                var escapedSql = sqlValue.Replace("\"", "\\\"").Replace("\r\n", "\\r\\n").Replace("\n", "\\n").Replace("\r", "\\r");
                return $"\"{escapedSql}\"";
            }
        }

        // Check for SqlxAttribute (stored procedure) - emit plain proc call with parameters
        var sqlxAttr = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlxAttribute");
        if (sqlxAttr != null)
        {
            var procedureName = sqlxAttr.ConstructorArguments.Length > 0
                ? sqlxAttr.ConstructorArguments[0].Value?.ToString()
                : MethodSymbol.Name;

            if (!string.IsNullOrEmpty(procedureName))
            {
                // 处理 SQL 模板占位符
                if (!string.IsNullOrEmpty(procedureName) && SqlTemplatePlaceholder.ContainsPlaceholders(procedureName!))
                {
                    var context = new SqlPlaceholderContext(SqlDef)
                    {
                        Method = MethodSymbol,
                        TableName = null, // 临时简化
                        EntityType = null // 临时简化
                    };
                    procedureName = SqlTemplatePlaceholder.ProcessTemplate(procedureName!, context);
                }

                var paramSql = string.Join(", ", SqlParameters.Select(p => p.GetParameterName(SqlDef.ParameterPrefix)));
                var call = string.IsNullOrEmpty(paramSql) ? procedureName : $"{procedureName} {paramSql}";
                return $"\"EXEC {call}\"";
            }
        }

        // Fallback: parse syntax for [Sqlx("...")] when attribute is unbound
        var syntax = MethodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax;
        if (syntax != null)
        {
            foreach (var list in syntax.AttributeLists)
            {
                foreach (var attr in list.Attributes)
                {
                    var nameText = attr.Name.ToString();
                    if (nameText is "Sqlx" or "SqlxAttribute")
                    {
                        if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count > 0)
                        {
                            var firstArg = attr.ArgumentList.Arguments[0].ToString().Trim();
                            // Expect a string literal; just embed as-is, escaping quotes minimally
                            var sp = firstArg.Trim('"');
                            var escaped = sp.Replace("\"", "\\\"");
                            return $"\"EXEC {escaped}\"";
                        }
                        else
                        {
                            // Default to method name when no argument provided
                            return $"\"EXEC {MethodSymbol.Name}\"";
                        }
                    }
                }
            }
        }

        // TODO: 未来版本中将添加基于方法名的智能操作推断
        // 目前优先保持向后兼容性，主要依赖 Sqlx 特性和 SqlExecuteType 特性

        // 向后兼容：检查已弃用的 SqlExecuteTypeAttribute
        var sqlExecuteType = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (sqlExecuteType != null)
        {
            // 发出弃用警告
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SQLX_DEPRECATED_001",
                        "SqlExecuteType is deprecated",
                        "SqlExecuteTypeAttribute is deprecated. Use method naming conventions and ExpressionToSql parameters instead.",
                        "Usage",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    MethodSymbol.Locations.FirstOrDefault()));

            // 仍然处理以保持向后兼容
            var enumValueObj = sqlExecuteType.ConstructorArguments[0].Value;
            var type = enumValueObj switch
            {
                int intValue => intValue,
                string strValue when int.TryParse(strValue, out var intVal) => intVal,
                _ => Constants.SqlExecuteTypeValues.Select
            };
            var tableName = sqlExecuteType.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
            tableName = GetEffectiveTableName(tableName);

            return type switch
            {
                Constants.SqlExecuteTypeValues.Select => HandleSelectOperation(tableName),
                Constants.SqlExecuteTypeValues.Insert => HandleInsertOperation(tableName),
                Constants.SqlExecuteTypeValues.Update => HandleUpdateOperation(tableName),
                Constants.SqlExecuteTypeValues.Delete => HandleDeleteOperation(tableName),
                Constants.SqlExecuteTypeValues.BatchInsert => HandleBatchOperation("INSERT", tableName),
                Constants.SqlExecuteTypeValues.BatchUpdate => HandleBatchOperation("UPDATE", tableName),
                Constants.SqlExecuteTypeValues.BatchDelete => HandleBatchOperation("DELETE", tableName),
                Constants.SqlExecuteTypeValues.BatchCommand => "/* ADO.NET BatchCommand will be used */",
                _ => string.Empty
            };
        }

        // If we have ExpressionToSql parameter but no other SQL source, use dynamic SQL
        if (ExpressionToSqlParameter != null)
        {
            // For ExpressionToSql, the SQL is generated from the expression parameter at runtime
            // Check if ToTemplate method exists, otherwise fall back to simple approach
            return $"\"SELECT * FROM UnknownTable\"";
        }

        return string.Empty;
    }

    private string GetEffectiveTableName(string defaultTableName)
    {
        // Check for TableName attribute on method
        var methodTableNameAttr = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "TableNameAttribute");
        if (methodTableNameAttr != null && methodTableNameAttr.ConstructorArguments.Length > 0)
        {
            return methodTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
        }

        // Check for TableName attribute on class
        var classTableNameAttr = ClassGenerationContext.ClassSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "TableNameAttribute");
        if (classTableNameAttr != null && classTableNameAttr.ConstructorArguments.Length > 0)
        {
            return classTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
        }

        // Check for TableName attribute on parameters
        foreach (var param in SqlParameters)
        {
            var paramTableNameAttr = param.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "TableNameAttribute");
            if (paramTableNameAttr != null && paramTableNameAttr.ConstructorArguments.Length > 0)
            {
                return paramTableNameAttr.ConstructorArguments[0].Value?.ToString() ?? defaultTableName;
            }
        }

        return defaultTableName;
    }

    private string? HandleBatchOperation(string operation, string tableName)
    {
        // Batch operations can work without a predefined table name
        // The table name can be determined at runtime from the entity type or custom SQL
        if (string.IsNullOrEmpty(tableName))
        {
            return $"/* {operation} operation - table name will be determined at runtime */";
        }

        return operation switch
        {
            "INSERT" => $"INSERT INTO {tableName} (/* columns */) VALUES (/* batch values */)",
            "UPDATE" => $"UPDATE {tableName} SET /* columns = values */ WHERE /* condition */",
            "DELETE" => $"DELETE FROM {tableName} WHERE /* condition */",
            _ => $"/* {operation} operation on {tableName} */"
        };
    }

    private string? HandleSelectOperation(string tableName)
    {
        // SELECT operation - always return base SQL, dynamic parts handled in code generation
        if (string.IsNullOrEmpty(tableName))
        {
            // If no table name specified, assume it will be provided dynamically or via custom SQL
            return "\"SELECT * FROM /* table name will be determined at runtime */\"";
        }
        return $"\"SELECT * FROM {SqlDef.WrapColumn(tableName)}\"";
    }

    private string? HandleInsertOperation(string tableName)
    {
        // INSERT can have:
        // 1. ExpressionToSql + Entity parameter (single insert)
        // 2. ExpressionToSql + IEnumerable<Entity> (batch insert)
        // 3. Just Entity parameter (simple insert)

        // Handle case where table name is not provided
        if (string.IsNullOrEmpty(tableName))
        {
            if (ExpressionToSqlParameter != null)
            {
                return "/* INSERT statement will be generated from expression */";
            }
            tableName = "/* table name will be inferred from entity type */";
        }

        var entityParameter = MethodSymbol.Parameters.FirstOrDefault(p =>
            !p.GetAttributes().Any(a => a.AttributeClass?.Name == "ExpressionToSqlAttribute" || a.AttributeClass?.Name == "ExpressionToSql") &&
            !IsSystemParameter(p));

        if (entityParameter != null)
        {
            var objectMap = new ObjectMap(entityParameter);
            var context = new InsertGenerateContext(this, tableName, objectMap);
            var baseSql = new SqlGenerator().Generate(SqlDef, Constants.SqlExecuteTypeValues.Insert, context);

            if (ExpressionToSqlParameter != null)
            {
                // Insert with additional conditions from expression
                return $"$\"{baseSql} \" + {ExpressionToSqlParameter.Name}.ToAdditionalClause()";
            }

            return $"\"{baseSql}\"";
        }

        // Fallback to expression-only insert
        if (ExpressionToSqlParameter != null)
        {
            return $"\"INSERT INTO {tableName} VALUES (...)\"";
        }

        return string.Empty;
    }

    private string? HandleUpdateOperation(string tableName)
    {
        // UPDATE needs:
        // 1. ExpressionToSql for WHERE clause
        // 2. Entity parameter or explicit SET values
        // Simple approach: Use ExpressionToSql for both SET and WHERE

        // Handle case where table name is not provided
        if (string.IsNullOrEmpty(tableName))
        {
            if (ExpressionToSqlParameter != null)
            {
                return "/* UPDATE statement will be generated from expression */";
            }
            tableName = "/* table name will be inferred from entity type */";
        }

        if (ExpressionToSqlParameter != null)
        {
            // Let ExpressionToSql handle the entire UPDATE statement
            return $"\"UPDATE {tableName} SET field = value\"";
        }

        // Fallback: find entity parameter for simple update
        var entityParameter = MethodSymbol.Parameters.FirstOrDefault(p =>
            !IsSystemParameter(p));

        if (entityParameter != null)
        {
            var objectMap = new ObjectMap(entityParameter);
            var context = new UpdateGenerateContext(this, tableName, objectMap);
            var baseSql = new SqlGenerator().Generate(SqlDef, Constants.SqlExecuteTypeValues.Update, context);
            return $"\"{baseSql}\"";
        }

        return string.Empty;
    }

    private string? HandleDeleteOperation(string tableName)
    {
        // DELETE operation needs a WHERE clause for safety

        // Handle case where table name is not provided
        if (string.IsNullOrEmpty(tableName))
        {
            if (ExpressionToSqlParameter != null)
            {
                return "/* DELETE statement will be generated from expression */";
            }
            tableName = "/* table name will be inferred from entity type */";
        }

        // If we have ExpressionToSql parameter, let it handle the WHERE clause
        if (ExpressionToSqlParameter != null)
        {
            return $"\"DELETE FROM {tableName}\"";
        }

        // For simple DELETE by ID, look for an ID parameter
        var idParameter = MethodSymbol.Parameters.FirstOrDefault(p =>
            !IsSystemParameter(p) &&
            (p.Name.ToLowerInvariant() == "id" || p.Name.ToLowerInvariant().EndsWith("id")));

        if (idParameter != null)
        {
            // Generate simple DELETE with WHERE Id = @param
            var paramName = idParameter.Name.ToLowerInvariant();
            return $"\"DELETE FROM {SqlDef.WrapColumn(tableName)} WHERE {SqlDef.WrapColumn("Id")} = {SqlDef.ParameterPrefix}{paramName}\"";
        }

        // Check for entity parameter that might have an Id property
        var entityParameter = MethodSymbol.Parameters.FirstOrDefault(p =>
            !IsSystemParameter(p) && p.Type.TypeKind == TypeKind.Class);

        if (entityParameter != null)
        {
            // Assume entity has an Id property for DELETE WHERE clause
            var objectMap = new ObjectMap(entityParameter);
            var context = new DeleteGenerateContext(this, tableName, objectMap);
            var baseSql = new SqlGenerator().Generate(SqlDef, Constants.SqlExecuteTypeValues.Delete, context);
            return $"\"{baseSql.Replace("{0}", "Id = @Id")}\"";
        }

        // Fallback: require ExpressionToSql for safety - don't generate DELETE without WHERE
        throw new InvalidOperationException($"DELETE operation for method {MethodSymbol.Name} requires either an 'id' parameter, entity parameter with Id property, or ExpressionToSql parameter for WHERE clause safety");
    }

    private bool IsSystemParameter(IParameterSymbol parameter)
    {
        // Check if parameter is a system parameter (CancellationToken, etc.)
        var typeName = parameter.Type.Name;
        return typeName == "CancellationToken" ||
               typeName == "DbTransaction" ||
               typeName == "DbConnection" ||
               parameter.GetAttributes().Any(a =>
                   a.AttributeClass?.Name == "TimeoutAttribute" ||
                   a.AttributeClass?.Name == "ExpressionToSqlAttribute" ||
                   a.AttributeClass?.Name == "ExpressionToSql");
    }
    private void GenerateBatchInsertSql(IndentedStringBuilder sb, string sqlTemplate)
    {
        // Find the collection parameter (should be IEnumerable<T>)
        var collectionParameter = SqlParameters.FirstOrDefault(p => !p.Type.IsScalarType());
        if (collectionParameter == null)
        {
            // Fallback to regular SQL with user-friendly error handling
            sb.AppendLine($"// Warning: No collection parameter found for batch INSERT");
            sb.AppendLine($"{CmdName}.CommandText = {sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "")};");
            return;
        }

        // Add null check for user safety
        sb.AppendLine($"if ({collectionParameter.Name} == null)");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({collectionParameter.Name}), \"Collection parameter cannot be null for batch INSERT\");");
        sb.AppendLine();

        var objectMap = new ObjectMap(collectionParameter);
        var baseSql = sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "");
        var properties = objectMap.Properties.ToList();

        // Generate optimized batch INSERT logic with StringBuilder for better performance
        sb.AppendLine($"var baseSql = \"{baseSql.Trim('"')}\";");
        sb.AppendLine($"var sqlBuilder = new global::System.Text.StringBuilder(baseSql);");
        sb.AppendLine($"var paramIndex = 0;");
        sb.AppendLine($"var isFirst = true;");
        sb.AppendLine();

        // Check for empty collection to avoid generating invalid SQL
        sb.AppendLine($"if (!{collectionParameter.Name}.Any())");
        sb.AppendLine($"    throw new global::System.InvalidOperationException(\"Cannot perform batch INSERT with empty collection\");");
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

    private void GenerateExpressionToSqlEnhancement(IndentedStringBuilder sb, int operationType)
    {
        // Generate code to enhance the base SQL with ExpressionToSql functionality
        switch (operationType)
        {
            case Constants.SqlExecuteTypeValues.Select:
            case Constants.SqlExecuteTypeValues.Delete:
                // For SELECT and DELETE, append WHERE clause
                sb.AppendLine($"var __whereClause__ = {ExpressionToSqlParameter!.Name}.ToWhereClause();");
                sb.AppendLine($"if (!string.IsNullOrEmpty(__whereClause__))");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"{CmdName}.CommandText += \" WHERE \" + __whereClause__;");
                sb.PopIndent();
                sb.AppendLine("}");
                break;

            case Constants.SqlExecuteTypeValues.Update:
                // For UPDATE, let ExpressionToSql handle the entire statement
                sb.AppendLine($"var __template__ = {ExpressionToSqlParameter!.Name}.ToTemplate();");
                sb.AppendLine($"{CmdName}.CommandText = __template__.Sql;");
                sb.AppendLine($"foreach(var __param__ in __template__.Parameters)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"{CmdName}.Parameters.Add(__param__);");
                sb.PopIndent();
                sb.AppendLine("}");
                break;

            case Constants.SqlExecuteTypeValues.Insert:
                // For INSERT, ExpressionToSql might provide additional INSERT clauses
                sb.AppendLine($"var __insertAddition__ = {ExpressionToSqlParameter!.Name}.ToAdditionalClause();");
                sb.AppendLine($"if (!string.IsNullOrEmpty(__insertAddition__))");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"{CmdName}.CommandText += \" \" + __insertAddition__;");
                sb.PopIndent();
                sb.AppendLine("}");
                break;
        }
    }

    private string? GetTimeoutExpression()
    {
        if (TimeoutParameter != null) return TimeoutParameter.Name;
        var methodTimeout = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "TimeoutAttribute");
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

        if (actualType.Name == "IEnumerable") return ReturnTypes.IEnumerable;
        if (actualType.Name == Consts.IAsyncEnumerable) return ReturnTypes.IAsyncEnumerable;
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
        if (actualType.IsScalarType()) return ReturnTypes.Scalar;
        return ReturnTypes.Object;
    }

    public sealed record ColumnDefine(string ParameterName, ISymbol Symbol);

    private string? GetTableNameFromSqlExecuteType()
    {
        var sqlExecuteTypeAttr = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (sqlExecuteTypeAttr != null && sqlExecuteTypeAttr.ConstructorArguments.Length > 1)
        {
            return sqlExecuteTypeAttr.ConstructorArguments[1].Value?.ToString();
        }
        return null;
    }

    private bool GenerateBatchCommandLogic(IndentedStringBuilder sb)
    {
        // Find collection parameter
        var collectionParam = SqlParameters.FirstOrDefault(p => !p.Type.IsScalarType());
        if (collectionParam == null)
        {
            sb.AppendLine("throw new global::System.ArgumentException(\"BatchCommand requires a collection parameter\");");
            sb.PopIndent();
            sb.AppendLine("}");
            return true;
        }

        // Null check and validation
        sb.AppendLine($"if ({collectionParam.Name} == null)");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({collectionParam.Name}));");
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
        var tableName = GetTableNameFromSqlExecuteType();
        if (string.IsNullOrEmpty(tableName))
        {
            // Infer table name from entity type
            var tempObjectMap = new ObjectMap(collectionParam);
            var entityType = tempObjectMap.ElementSymbol as INamedTypeSymbol;
            tableName = entityType?.Name ?? "UnknownTable";
        }

        var objectMap = new ObjectMap(collectionParam);
        var properties = objectMap.Properties.ToList();
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
        var tableName = GetTableNameFromSqlExecuteType();
        if (string.IsNullOrEmpty(tableName))
        {
            // Infer table name from entity type
            var tempObjectMap = new ObjectMap(collectionParam);
            var entityType = tempObjectMap.ElementSymbol as INamedTypeSymbol;
            tableName = entityType?.Name ?? "UnknownTable";
        }

        var objectMap = new ObjectMap(collectionParam);
        var properties = objectMap.Properties.ToList();
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

                if (!setProperties.Any())
                {
                    sb.AppendLine("throw new global::System.InvalidOperationException(\"No properties marked with [Set] attribute or eligible for SET clause in batch update\");");
                    break;
                }

                if (!whereProperties.Any())
                {
                    sb.AppendLine("throw new global::System.InvalidOperationException(\"No properties marked with [Where] attribute or eligible for WHERE clause in batch update\");");
                    break;
                }

                var setClause = string.Join(", ", setProperties.Select(p => $"{SqlDef.WrapColumn(p.Name)} = {SqlDef.ParameterPrefix}{p.GetParameterName(string.Empty)}"));
                var whereClause = string.Join(" AND ", whereProperties.Select(p => GenerateWhereCondition(p)));
                sb.AppendLine($"{CmdName}.CommandText = \"UPDATE {SqlDef.WrapColumn(tableName!)} SET {setClause} WHERE {whereClause}\";");
                break;
            case "DELETE":
                var deleteWhereProperties = GetWhereProperties(properties);

                if (!deleteWhereProperties.Any())
                {
                    sb.AppendLine("throw new global::System.InvalidOperationException(\"No properties marked with [Where] attribute or eligible for WHERE clause in batch delete\");");
                    break;
                }

                var deleteWhereClause = string.Join(" AND ", deleteWhereProperties.Select(p => GenerateWhereCondition(p)));
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
        var columns = string.Join(", ", properties.Select(p => SqlDef.WrapColumn(p.GetSqlName()).Replace("\"", "\\\"")));
        var values = string.Join(", ", properties.Select(p => SqlDef.ParameterPrefix + p.GetSqlName()));
        var wrappedTable = SqlDef.WrapColumn(tableName).Replace("\"", "\\\"");
        sb.AppendLine($"batchCommand.CommandText = \"INSERT INTO {wrappedTable} ({columns}) VALUES ({values})\";");
    }

    private void GenerateBatchUpdateSql(IndentedStringBuilder sb, string tableName, List<IPropertySymbol> properties)
    {
        var setProperties = GetSetProperties(properties);
        var whereProperties = GetWhereProperties(properties);

        if (!setProperties.Any())
        {
            sb.AppendLine("throw new global::System.InvalidOperationException(\"No properties marked with [Set] attribute or eligible for SET clause in batch update\");");
            return;
        }

        if (!whereProperties.Any())
        {
            sb.AppendLine("throw new global::System.InvalidOperationException(\"No properties marked with [Where] attribute or eligible for WHERE clause in batch update\");");
            return;
        }

        var setClause = string.Join(", ", setProperties.Select(p => $"{SqlDef.WrapColumn(p.GetSqlName()).Replace("\"", "\\\"")} = {SqlDef.ParameterPrefix}{p.GetSqlName()}"));
        var whereClause = string.Join(" AND ", whereProperties.Select(p => GenerateWhereCondition(p)));
        var wrappedTable = SqlDef.WrapColumn(tableName).Replace("\"", "\\\"");

        sb.AppendLine($"batchCommand.CommandText = \"UPDATE {wrappedTable} SET {setClause} WHERE {whereClause}\";");
    }

    private void GenerateBatchDeleteSql(IndentedStringBuilder sb, string tableName, List<IPropertySymbol> properties)
    {
        var whereProperties = GetWhereProperties(properties);

        if (!whereProperties.Any())
        {
            sb.AppendLine("throw new global::System.InvalidOperationException(\"No properties marked with [Where] attribute or eligible for WHERE clause in batch delete\");");
            return;
        }

        var whereClause = string.Join(" AND ", whereProperties.Select(p => GenerateWhereCondition(p)));
        var wrappedTable = SqlDef.WrapColumn(tableName).Replace("\"", "\\\"");
        sb.AppendLine($"batchCommand.CommandText = \"DELETE FROM {wrappedTable} WHERE {whereClause}\";");
    }

    private string GetBatchOperationType()
    {
        // First check SqlExecuteType attribute for specific batch operation types
        var sqlExecuteTypeAttr = MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SqlExecuteTypeAttribute");

        if (sqlExecuteTypeAttr != null && sqlExecuteTypeAttr.ConstructorArguments.Length > 0)
        {
            var enumValueObj = sqlExecuteTypeAttr.ConstructorArguments[0].Value;
            var type = enumValueObj switch
            {
                int intValue => intValue,
                string strValue when int.TryParse(strValue, out var intVal) => intVal,
                _ => Constants.SqlExecuteTypeValues.Select
            };

            return type switch
            {
                Constants.SqlExecuteTypeValues.BatchInsert => "INSERT",
                Constants.SqlExecuteTypeValues.BatchUpdate => "UPDATE",
                Constants.SqlExecuteTypeValues.BatchDelete => "DELETE",
                Constants.SqlExecuteTypeValues.BatchCommand => GetOperationFromMethodName(), // Fallback to method name inference
                _ => "INSERT"
            };
        }

        // Fallback to method name inference
        return GetOperationFromMethodName();
    }

    private string GetOperationFromMethodName()
    {
        // Try to infer operation type from method name
        var methodName = MethodSymbol.Name.ToUpperInvariant();

        if (methodName.Contains("INSERT") || methodName.Contains("ADD") || methodName.Contains("CREATE"))
            return "INSERT";
        if (methodName.Contains("UPDATE") || methodName.Contains("MODIFY") || methodName.Contains("CHANGE"))
            return "UPDATE";
        if (methodName.Contains("DELETE") || methodName.Contains("REMOVE"))
            return "DELETE";

        // Default to INSERT for backward compatibility
        return "INSERT";
    }

    private bool IsKeyProperty(IPropertySymbol property)
    {
        var name = property.Name.ToUpperInvariant();
        return name == "ID" || name.EndsWith("ID") || name == "KEY" || name.EndsWith("KEY");
    }

    private List<IPropertySymbol> GetSetProperties(List<IPropertySymbol> properties)
    {
        // 首先查找标记了 [Set] 特性的属性
        var explicitSetProperties = properties.Where(p => HasSetAttribute(p)).ToList();

        if (explicitSetProperties.Any())
        {
            return explicitSetProperties;
        }

        // 如果没有显式标记，则使用所有非 Where 属性（排除主键）
        var whereProperties = GetWhereProperties(properties);
        return properties.Where(p => !whereProperties.Contains(p) && !IsKeyProperty(p)).ToList();
    }

    private List<IPropertySymbol> GetWhereProperties(List<IPropertySymbol> properties)
    {
        // 首先查找标记了 [Where] 特性的属性
        var explicitWhereProperties = properties.Where(p => HasWhereAttribute(p)).ToList();

        if (explicitWhereProperties.Any())
        {
            return explicitWhereProperties;
        }

        // 如果没有显式标记，则使用主键属性作为默认 WHERE 条件
        return properties.Where(p => IsKeyProperty(p)).ToList();
    }

    private bool HasSetAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(a => a.AttributeClass?.Name == "SetAttribute");
    }

    private bool HasWhereAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(a => a.AttributeClass?.Name == "WhereAttribute");
    }

    private string GenerateWhereCondition(IPropertySymbol property)
    {
        var whereAttr = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "WhereAttribute");
        var operatorStr = "="; // 默认操作符

        if (whereAttr != null && whereAttr.ConstructorArguments.Length > 0)
        {
            operatorStr = whereAttr.ConstructorArguments[0].Value?.ToString() ?? "=";
        }
        else if (whereAttr != null)
        {
            // 检查 Operator 属性
            var operatorProp = whereAttr.NamedArguments.FirstOrDefault(na => na.Key == "Operator");
            if (!operatorProp.Equals(default(KeyValuePair<string, Microsoft.CodeAnalysis.TypedConstant>)))
            {
                operatorStr = operatorProp.Value.Value?.ToString() ?? "=";
            }
        }

        return $"{SqlDef.WrapColumn(property.GetSqlName()).Replace("\"", "\\\"")} {operatorStr} {SqlDef.ParameterPrefix}{property.GetSqlName()}";
    }


    private void GenerateBatchExecution(IndentedStringBuilder sb)
    {
        var returnType = GetReturnType();

        if (IsAsync)
        {
            if (returnType != ReturnTypes.Void)
            {
                sb.AppendLine($"return await {CmdName}.ExecuteNonQueryAsync();");
            }
            else
            {
                sb.AppendLine($"await {CmdName}.ExecuteNonQueryAsync();");
            }
        }
        else
        {
            if (returnType != ReturnTypes.Void)
            {
                sb.AppendLine($"return {CmdName}.ExecuteNonQuery();");
            }
            else
            {
                sb.AppendLine($"{CmdName}.ExecuteNonQuery();");
            }
        }
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

            // Also check regular constructors for backward compatibility
            var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
            if (constructor != null)
            {
                var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
                if (connectionParam != null)
                {
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
        var parametersToCheck = SqlParameters.Where(p => ShouldGenerateNullCheck(p)).ToList();

        if (parametersToCheck.Any())
        {
            sb.AppendLine("// Parameter null checks (fail fast)");
            foreach (var param in parametersToCheck)
            {
                sb.AppendLine($"if ({param.Name} == null)");
                sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({param.Name}));");
            }
            sb.AppendLine();
        }
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
}

internal enum SqlTypes
{
    MySql = 0,
    SqlServer = 1,
    Postgresql = 2,
}

internal static class ExtensionsWithCache
{
    internal static string GetDataReadExpressionWithCachedOrdinal(this ITypeSymbol type, string readerName, string columnName, string ordinalVariableName)
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
                        return $"{readerName}.IsDBNull({ordinalVariableName}) ? null : {readerName}.{method}({ordinalVariableName})";
                    }
                    else
                    {
                        // Non-nullable string: return empty string or throw
                        return $"{readerName}.IsDBNull({ordinalVariableName}) ? string.Empty : {readerName}.{method}({ordinalVariableName})";
                    }
                }
                else if (isNullable && unwrapType.IsValueType)
                {
                    // Nullable value types: return null if DBNull
                    return $"{readerName}.IsDBNull({ordinalVariableName}) ? null : {readerName}.{method}({ordinalVariableName})";
                }
                else if (unwrapType.IsValueType)
                {
                    // Special handling for enum types - need explicit casting
                    if (unwrapType.TypeKind == TypeKind.Enum)
                    {
                        var enumTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return $"{readerName}.IsDBNull({ordinalVariableName}) ? default({enumTypeName}) : ({enumTypeName}){readerName}.{method}({ordinalVariableName})";
                    }

                    // Non-nullable value types: return default if DBNull
                    return $"{readerName}.IsDBNull({ordinalVariableName}) ? default : {readerName}.{method}({ordinalVariableName})";
                }
                else
                {
                    // Reference types: return null if DBNull
                    return $"{readerName}.IsDBNull({ordinalVariableName}) ? null : {readerName}.{method}({ordinalVariableName})";
                }
            }

            return $"{readerName}.{method}({ordinalVariableName})";
        }

        // Enhanced fallback handling for unsupported types
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Special handling for enum types
        if (unwrapType.TypeKind == TypeKind.Enum)
        {
            var underlyingType = ((INamedTypeSymbol)unwrapType).EnumUnderlyingType;
            var underlyingMethod = underlyingType?.GetDataReaderMethod();

            if (!string.IsNullOrEmpty(underlyingMethod))
            {
                if (isNullable)
                {
                    return $"{readerName}.IsDBNull({ordinalVariableName}) ? null : ({typeName}){readerName}.{underlyingMethod}({ordinalVariableName})";
                }
                else
                {
                    return $"{readerName}.IsDBNull({ordinalVariableName}) ? default({typeName}) : ({typeName}){readerName}.{underlyingMethod}({ordinalVariableName})";
                }
            }
        }

        // Final fallback to GetValue with casting (less preferred)
        if (isNullable || type.IsReferenceType)
        {
            return $"{readerName}.IsDBNull({ordinalVariableName}) ? null : ({typeName}){readerName}.GetValue({ordinalVariableName})";
        }
        else
        {
            return $"{readerName}.IsDBNull({ordinalVariableName}) ? default({typeName}) : ({typeName}){readerName}.GetValue({ordinalVariableName})";
        }
    }


}
