// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sqlx.SqlGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using static Sqlx.Extensions;

internal class MethodGenerationContext : GenerationContextBase
{
    internal const string DbConnectionName = "__conn__";
    internal const string CmdName = "__cmd__";
    internal const string DbReaderName = "__reader__";
    internal const string ResultName = "__result__";
    internal const string DataName = "__data__";
    internal const string StartTimeName = "__startTime__";

    internal const string MethodExecuting = "OnExecuting";
    internal const string MethodExecuted = "OnExecuted";
    internal const string MethodExecuteFail = "OnExecuteFail";

    internal const string GetTimestampMethod = "global::System.Diagnostics.Stopwatch.GetTimestamp()";

    internal MethodGenerationContext(ClassGenerationContext classGenerationContext, IMethodSymbol methodSymbol)
    {
        ClassGenerationContext = classGenerationContext;
        MethodSymbol = methodSymbol;

        CancellationTokenParameter = GetParameter(methodSymbol, x => x.Type.IsCancellationToken());

        var rawSqlIsInParamter = true;
        RawSqlParameter = GetAttributeParamter(methodSymbol, "RawSqlAttribute");
        if (RawSqlParameter == null && MethodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "RawSqlAttribute"))
        {
            rawSqlIsInParamter = false;
            RawSqlParameter = methodSymbol;
        }

        TimeoutParameter = GetAttributeParamter(methodSymbol, "TimeoutAttribute");
        ReaderHandlerParameter = GetAttributeParamter(methodSymbol, "ReadHandlerAttribute");

        var parameters = methodSymbol.Parameters;
        RemoveIfExists(ref parameters, DbContext);
        RemoveIfExists(ref parameters, DbConnection);
        RemoveIfExists(ref parameters, TransactionParameter);
        if (rawSqlIsInParamter) RemoveIfExists(ref parameters, RawSqlParameter);
        RemoveIfExists(ref parameters, CancellationTokenParameter);
        RemoveIfExists(ref parameters, TimeoutParameter);
        SqlParameters = parameters;
        DeclareReturnType = GetReturnType();
        CancellationTokenKey = CancellationTokenParameter?.Name ?? "default(global::System.Threading.CancellationToken)";
        IsAsync = MethodSymbol.ReturnType.Name == "Task" || MethodSymbol.ReturnType.Name == "IAsyncEnumerable";
        SqlDef = GetSqlDefine();
        if (IsAsync)
        {
            AsyncKey = "async ";
            AwaitKey = "await ";
        }
        else
        {
            AsyncKey = string.Empty;
            AwaitKey = string.Empty;
        }
    }

    internal IMethodSymbol MethodSymbol { get; }

    internal ReturnTypes DeclareReturnType { get; }

    internal ClassGenerationContext ClassGenerationContext { get; }

    internal override ISymbol? DbConnection => GetParameter(MethodSymbol, x => x.Type.IsDbConnection());

    internal override ISymbol? DbContext => GetParameter(MethodSymbol, x => x.Type.IsDbContext());

    internal override ISymbol? TransactionParameter => GetParameter(MethodSymbol, x => x.Type.IsDbTransaction());

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
    /// Gets the method paramters remove the extars.
    /// </summary>
    internal ImmutableArray<IParameterSymbol> SqlParameters { get; }

    internal ITypeSymbol ReturnType => MethodSymbol.ReturnType.UnwrapTaskType();

    internal ITypeSymbol ElementType => ReturnType.UnwrapListType();

    internal string AsyncKey { get; }

    internal string AwaitKey { get; }

    internal string CancellationTokenKey { get; }

    internal bool IsAsync { get; }

    private SqlDefine SqlDef { get; }

    private string MethodNameString => $"\"{MethodSymbol.Name}\"";

    private bool ReturnIsEnumerable => ReturnType.Name == "IEnumerable" || ReturnType.Name == "IAsyncEnumerable";

    public bool DeclareCommand(IndentedStringBuilder sb)
    {
        var args = string.Join(", ", MethodSymbol.Parameters.Select(x =>
        {
            var paramterSyntax = (ParameterSyntax)x.DeclaringSyntaxReferences[0].GetSyntax();
            var prefx = string.Join(" ", paramterSyntax.Modifiers.Select(y => y.ToString()));
            if (paramterSyntax!.Modifiers.Count != 0) prefx += " ";
            return prefx + x.ToDisplayString();
        }));
        var staticKeyword = MethodSymbol.IsStatic ? "static " : string.Empty;
        sb.AppendLine($"{MethodSymbol.DeclaredAccessibility.GetAccessibility()} {AsyncKey}{staticKeyword}partial {MethodSymbol.ReturnType.ToDisplayString()} {MethodSymbol.Name}({args})");
        sb.AppendLine("{");
        sb.PushIndent();

        var dbContext = DbContext ?? ClassGenerationContext.DbContext;
        var dbConnection = DbConnection ?? ClassGenerationContext.DbConnection;
        if (dbContext == null && dbConnection == null)
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0006, MethodSymbol.Locations[0]));
            return false;
        }

        var dbConnectionExpression = dbConnection == null ? $"global::Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.GetDbConnection({dbContext!.Name}.Database)" : dbConnection.Name;

        sb.AppendLine($"global::System.Data.Common.DbConnection {DbConnectionName} = {dbConnectionExpression};");
        sb.AppendLine($"if({DbConnectionName}.State != global::System.Data.ConnectionState.Open )");
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

        sb.AppendLine($"using(global::System.Data.Common.DbCommand {CmdName} = {DbConnectionName}.CreateCommand())");
        sb.AppendLine("{");
        sb.PushIndent();

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

        sb.AppendLine($"{CmdName}.CommandText = {sql};");
        sb.AppendLine();

        // Paramters
        var columnDefines = new List<ColumnDefine>();
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

        sb.AppendLine($"global::System.Int64 {StartTimeName} = {GetTimestampMethod};");
        if (!ReturnIsEnumerable)
        {
            sb.AppendLine("try");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{MethodExecuting}({MethodNameString}, {CmdName});");
        }

        // Execute
        if (IsExecuteNoQuery())
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

    private static void RemoveIfExists(ref ImmutableArray<IParameterSymbol> pars, ISymbol? symbol)
    {
        if (symbol != null) pars = pars.Remove((IParameterSymbol)symbol);
    }

    private static ISymbol? GetParameter(IMethodSymbol methodSymbol, Func<IParameterSymbol, bool> check)
        => methodSymbol.Parameters.FirstOrDefault(check);

    private static IParameterSymbol? GetAttributeParamter(IMethodSymbol methodSymbol, string attributeName)
        => methodSymbol.Parameters.FirstOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass?.Name == attributeName));

    private bool IsExecuteNoQuery() => MethodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "ExecuteNoQueryAttribute");

    private bool WriteExecuteNoQuery(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        if (!(ReturnType.SpecialType == SpecialType.System_Int32))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0008, MethodSymbol.Locations.FirstOrDefault()));
            return false;
        }

        sb.AppendLineIf(IsAsync, $"var {ResultName} = {CmdName}.ExecuteNonQuery();", $"var {ResultName} = await {CmdName}.ExecuteNonQueryAsync({CancellationTokenKey});");

        WriteOutput(sb, columnDefines);
        WriteMethodExecuted(sb, ResultName);
        sb.AppendLine($"return {ResultName};");
        return true;
    }

    private void WriteMethodExecuted(IndentedStringBuilder sb, string resultName)
    {
        if (!ReturnIsEnumerable)
        {
            sb.AppendLine($"{MethodExecuted}({MethodNameString}, {CmdName}, {resultName}, {GetTimestampMethod} - {StartTimeName});");
        }
    }

    private void WriteScalar(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var cancellationTokenName = CancellationTokenParameter?.Name ?? string.Empty;

        sb.AppendLineIf(IsAsync, $"var {ResultName} = await {CmdName}.ExecuteScalarAsync({cancellationTokenName});", $"var {ResultName} = {CmdName}.ExecuteScalar();");

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
            sb.AppendLine($"if({ResultName} == null) return default;");
            sb.AppendLine($"return ({ReturnType.ToDisplayString()})global::System.Convert.ChangeType({ResultName}, typeof({ReturnType.UnwrapNullableType().ToDisplayString(NullableFlowState.NotNull)}));");
        }
    }

    private bool WriteReturn(IndentedStringBuilder sb, List<ColumnDefine> columnDefines)
    {
        var dbContext = DbContext ?? ClassGenerationContext.DbContext;
        var dbConnection = DbConnection ?? ClassGenerationContext.DbConnection;

        var handler = GetHandlerInvoke();
        var hasHandler = !string.IsNullOrEmpty(handler);

        var returnType = ElementType.UnwrapNullableType();
        var isTuple = returnType.IsTupleType;

        if (hasHandler || dbConnection != null)
        {
            var executeMethod = IsAsync ? $"await {CmdName}.ExecuteReaderAsync({CancellationTokenKey})" : $"{CmdName}.ExecuteReader()";
            sb.AppendLine($"using(global::System.Data.Common.DbDataReader {DbReaderName} = {executeMethod})");
            sb.AppendLine("{");
            sb.PushIndent();
            if (hasHandler)
            {
                sb.AppendLine(handler!);
            }
            else if (DeclareReturnType == ReturnTypes.List || DeclareReturnType == ReturnTypes.IEnumerable || DeclareReturnType == ReturnTypes.IAsyncEnumerable)
            {
                var isList = DeclareReturnType == ReturnTypes.List;
                if (isList) WriteDeclareReturnList(sb);

                WriteBeginReader(sb);
                if (returnType.IsScalarType())
                {
                    sb.AppendLineIf(isList, $"{ResultName}.Add({returnType.GetDataReadIndexExpression(DbReaderName, 0)});", $"yield return {returnType.GetDataReadIndexExpression(DbReaderName, 0)};");
                }
                else if (isTuple)
                {
                    var tupleJoins = string.Join(", ", ((INamedTypeSymbol)returnType.UnwrapTaskType()).TypeArguments.Select((x, index) => x.GetDataReadIndexExpression(DbReaderName, index)));
                    sb.AppendLineIf(isList, $"{ResultName}.Add(({tupleJoins}));", $"yield return ({tupleJoins});");
                }
                else
                {
                    var writeableProperties = GetPropertySymbols(returnType);
                    WriteDeclareObjectExpression(sb, returnType, isList ? ResultName : null, writeableProperties);
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

                var readMethod = IsAsync ? $"ReadAsync({CancellationTokenKey})" : "Read()";
                sb.AppendLine($"if(!{AwaitKey}{DbReaderName}.{readMethod}) return default;");

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

        var fromSqlRawMethod = "global::Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.FromSqlRaw";
        var firstOrDefaultAsyncMethod = "global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync";
        var toListAsyncMethod = "global::Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync";

        ISymbol setSymbol = ElementType;

        if (GetDbSetElement(out var dbSetEle))
        {
            setSymbol = dbSetEle!;
        }

        var queryCall = $"{fromSqlRawMethod}({dbContext!.Name}.Set<{setSymbol.ToDisplayString()}>(),{CmdName}.CommandText, {CmdName}.Parameters.OfType<global::System.Object>().ToArray())";

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
                    // TODO: report
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

        if (DeclareReturnType == ReturnTypes.IEnumerable || DeclareReturnType == ReturnTypes.IAsyncEnumerable)
        {
            sb.AppendLine($"{AwaitKey}foreach (var item in {queryCall}{convert})");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("yield return item;");
            sb.PopIndent();
            sb.AppendLine("}");
            WriteOutput(sb, columnDefines);
        }
        else
        {
            queryCall += convert;
            if (IsAsync)
            {
                if (DeclareReturnType != ReturnTypes.List)
                {
                    queryCall = $"{firstOrDefaultAsyncMethod}({queryCall}, {CancellationTokenKey})";
                }
                else
                {
                    queryCall = $"{toListAsyncMethod}({queryCall}, {CancellationTokenKey})";
                }

                sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall};");
            }
            else
            {
                var callMethod = DeclareReturnType == ReturnTypes.List ? ".ToList()" : ".FirstOrDefault()";
                sb.AppendLine($"var {ResultName} = {AwaitKey}{queryCall}{callMethod};");
            }

            WriteOutput(sb, columnDefines);
            sb.AppendLine($"return {ResultName};");
        }

        return true;
    }

    private bool GetDbSetElement(out ISymbol? symbol)
    {
        symbol = null;
        var setElement = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbSetTypeAttribute");
        if (setElement == null)
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0009, MethodSymbol.Locations[0]));
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
        sb.AppendLine($"global::System.Collections.Generic.List<{ElementType.ToDisplayString()}> {ResultName} = new global::System.Collections.Generic.List<{ElementType.ToDisplayString(NullableFlowState.None)}>();");
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

        // Declare class same as "xx data = new xx()".
        sb.AppendLine($"{symbol.ToDisplayString()} {DataName} = {newExp} {symbol.ToDisplayString(NullableFlowState.None)}{expCall};");

        foreach (var item in properties)
        {
            sb.AppendLine($"{DataName}.{item.Name} = {item.Type.GetDataReadExpression(DbReaderName, item.GetSqlName())};");
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
                sb.AppendLine($"{item.Symbol.Name} = ({parSymbol.Type.ToDisplayString()}){item.ParameterName};");
            }
        }
    }

    private SqlDefine GetSqlDefine()
    {
        var methodDef = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlDefineAttribute") ??
            ClassGenerationContext.ClassSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlDefineAttribute");
        if (methodDef == null) return SqlDefine.MySql;

        if (methodDef.ConstructorArguments.Length == 1)
        {
            var define = (int)methodDef.ConstructorArguments[0].Value!;
            return define switch
            {
                1 => SqlDefine.SqlService,
                2 => SqlDefine.PgSql,
                _ => SqlDefine.MySql,
            };
        }

        return new SqlDefine(
            methodDef.ConstructorArguments[0].ToString()!,
            methodDef.ConstructorArguments[1].ToString()!,
            methodDef.ConstructorArguments[2].ToString()!,
            methodDef.ConstructorArguments[3].ToString()!,
            methodDef.ConstructorArguments[4].ToString()!);
    }

    private ColumnDefine DeclareParamter(IndentedStringBuilder sb, ISymbol par, ITypeSymbol parType, string prefx)
    {
        var columnDefine = par.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbColumnAttribute");

        var visitPath = string.IsNullOrEmpty(prefx) ? string.Empty : prefx + ".";
        visitPath = visitPath.Replace(".", "?.");
        var parNamePrefx = string.IsNullOrEmpty(prefx) ? string.Empty : prefx.Replace(".", "_");
        var parName = par.GetParameterName(SqlDef.ParamterPrefx + parNamePrefx);
        parName = Regex.Replace(parName, "[^a-zA-Z0-9@_]", "_") + "_p";
        var name = par.GetParameterName(SqlDef.ParamterPrefx);
        var dbType = parType.GetDbType();

        sb.AppendLine($"global::System.Data.Common.DbParameter {parName} = {CmdName}.CreateParameter();");
        sb.AppendLine($"{parName}.ParameterName = \"{name}\";");
        sb.AppendLine($"{parName}.DbType = {dbType};");
        sb.AppendLine($"{parName}.Value = {visitPath}{par.Name};");
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
            var attr = RawSqlParameter.GetAttributes().First(x => x.AttributeClass?.Name == "RawSqlAttribute");
            if (attr.ConstructorArguments.Length == 0)
            {
                return RawSqlParameter.Name;
            }

            return $"\"\"\"\n{attr.ConstructorArguments[0].Value?.ToString()}\n\"\"\"";
        }

        var sqlExeucteType = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (sqlExeucteType != null && MethodSymbol.Parameters.Length == 1)
        {
            var type = (SqlExecuteTypes)Enum.Parse(typeof(SqlExecuteTypes), sqlExeucteType.ConstructorArguments[0].Value?.ToString());
            var sql = new SqlGenerator().Generate(SqlDef, type, new InsertGenerateContext(this, sqlExeucteType.ConstructorArguments[1].Value?.ToString() ?? string.Empty, MethodSymbol.Parameters[0], new ObjectMap(MethodSymbol.Parameters[0])));
            if (!string.IsNullOrEmpty(sql)) return $"\"{sql}\"";
        }

        return string.Empty;
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
        if (ReturnType.SpecialType == SpecialType.System_Void) return ReturnTypes.Void;
        var actualType = ReturnType;

        if (actualType.Name == "IEnumerable") return ReturnTypes.IEnumerable;
        if (actualType.Name == "IAsyncEnumerable") return ReturnTypes.IAsyncEnumerable;
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
        return ReturnTypes.Void;
    }

    private sealed record ColumnDefine(string ParameterName, ISymbol Symbol);
}

internal enum ReturnTypes
{
    Void = 0,
    Scalar = 1,
    IEnumerable = 2,
    IAsyncEnumerable = 3,
    List = 4,
    ListDictionaryStringObject = 5,
}

internal enum SqlTypes
{
    MySql = 0,
    SqlServer = 1,
    Postgresql = 2,
}
