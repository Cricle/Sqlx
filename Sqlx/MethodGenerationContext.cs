// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContext.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using static Sqlx.Extensions;

internal class MethodGenerationContext : GenerationContextBase
{
    internal const string CmdName = "__cmd__";
    internal const string DbReaderName = "__reader__";
    internal const string ResultName = "__result__";
    internal const string DataName = "__data__";

    internal MethodGenerationContext(ClassGenerationContext classGenerationContext, IMethodSymbol methodSymbol)
    {
        ClassGenerationContext = classGenerationContext;
        MethodSymbol = methodSymbol;

        CancellationTokenParameter = GetParameter(methodSymbol, x => x.Type.IsCancellationToken());
        RawSqlParameter = GetAttributeParamter(methodSymbol, "RawSqlAttribute");
        TimeoutParameter = GetAttributeParamter(methodSymbol, "TimeoutAttribute");
        ReaderHandlerParameter = GetAttributeParamter(methodSymbol, "ReaderHandlerAttribute");

        var parameters = methodSymbol.Parameters;
        RemoveIfExists(ref parameters, DbContext);
        RemoveIfExists(ref parameters, DbConnection);
        RemoveIfExists(ref parameters, TransactionParameter);
        RemoveIfExists(ref parameters, RawSqlParameter);
        RemoveIfExists(ref parameters, CancellationTokenParameter);
        RemoveIfExists(ref parameters, TimeoutParameter);
        SqlParameters = parameters;
        DeclareReturnType = GetReturnType();
        CancellationTokenKey = CancellationTokenParameter?.Name ?? "default";
        IsAsync = MethodSymbol.ReturnType.Name == "Task" || MethodSymbol.ReturnType.Name == "IAsyncEnumerable";
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
    internal IParameterSymbol? RawSqlParameter { get; }

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

        sb.AppendLine($"using(global::System.Data.Common.DbCommand {CmdName} = {dbConnectionExpression}.CreateCommand())");
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
        var parameterPrefx = GetParamterPrefx();
        var columnDefines = new List<ColumnDefine>();
        foreach (var item in SqlParameters)
        {
            var parName = DeclareParamter(sb, item, parameterPrefx);
            sb.AppendLine($"{CmdName}.Parameters.Add({parName.ParameterName});");
            sb.AppendLine();
            columnDefines.Add(parName);
        }

        // Execute
        if (IsExecuteNoQuery())
        {
            WriteExecuteNoQuery(sb, CmdName, columnDefines);
        }
        else if (IsScalarType(ReturnType))
        {
            WriteScalar(sb, CmdName, columnDefines);
        }
        else
        {
            var succeed = WriteReturn(sb, columnDefines);
            if (!succeed) return false;
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

    private bool WriteExecuteNoQuery(IndentedStringBuilder sb, string cmdName, List<ColumnDefine> columnDefines)
    {
        var cancellationTokenName = CancellationTokenParameter?.Name ?? string.Empty;
        if (!(ReturnType.SpecialType == SpecialType.System_Int32))
        {
            ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(Messages.SP0008, MethodSymbol.Locations.FirstOrDefault()));
            return false;
        }

        if (IsAsync)
        {
            sb.AppendLine($"var {ResultName} = {cmdName}.ExecuteNonQuery();");
        }
        else
        {
            sb.AppendLine($"var {ResultName} = await {cmdName}.ExecuteNonQueryAsync({cancellationTokenName});");
        }

        WriteOutput(sb, columnDefines);
        sb.AppendLine($"return {ResultName};");
        return true;
    }

    private void WriteScalar(IndentedStringBuilder sb, string cmdName, List<ColumnDefine> columnDefines)
    {
        var cancellationTokenName = CancellationTokenParameter?.Name ?? string.Empty;

        if (IsAsync)
        {
            sb.AppendLine($"var {ResultName} = await {cmdName}.ExecuteScalarAsync({cancellationTokenName});");
        }
        else
        {
            sb.AppendLine($"var {ResultName} = {cmdName}.ExecuteScalar();");
        }

        WriteOutput(sb, columnDefines);
        sb.AppendLine($"return ({ReturnType.ToDisplayString()})global::System.Convert.ChangeType({ResultName}, typeof({ReturnType.ToDisplayString()}));");
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
                if (IsScalarType(returnType))
                {
                    if (isList)
                    {
                        sb.AppendLine($"{ResultName}.Add({returnType.GetDataReadIndexExpression(DbReaderName, 0)});");
                    }
                    else
                    {
                        sb.AppendLine($"yield return {returnType.GetDataReadIndexExpression(DbReaderName, 0)};");
                    }
                }
                else if (isTuple)
                {
                    var tupleJoins = string.Join(", ", ((INamedTypeSymbol)returnType.UnwrapTaskType()).TypeArguments.Select((x, index) => x.GetDataReadIndexExpression(DbReaderName, index)));
                    if (isList)
                    {
                        sb.AppendLine($"{ResultName}.Add(({tupleJoins}));");
                    }
                    else
                    {
                        sb.AppendLine($"yield return ({tupleJoins});");
                    }
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

                if (isList)
                    sb.AppendLine($"return {ResultName};");
            }
            else
            {
                var writeableProperties = GetPropertySymbols(returnType);

                var isList = DeclareReturnType == ReturnTypes.List;
                if (isList) WriteDeclareReturnList(sb);

                // List<T> or T
                WriteDeclareObjectExpression(sb, returnType, isList ? ResultName : null, writeableProperties);

                sb.AppendLine(isList ? $"return {ResultName};" : $"return {DataName};");
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

        // Declare class same as "xx data = new xx()".
        sb.AppendLine($"{symbol.ToDisplayString()} {DataName} = {newExp} {symbol.ToDisplayString()}{expCall};");

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
            if (item.Symbol.RefKind == RefKind.Ref || item.Symbol.RefKind == RefKind.Out)
            {
                sb.AppendLine($"{item.Symbol.Name} = ({item.Symbol.Type.ToDisplayString()}){item.ParameterName};");
            }
        }
    }

    private ColumnDefine DeclareParamter(IndentedStringBuilder sb, IParameterSymbol par, string parameterPrefx)
    {
        var columnDefine = par.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "DbColumnAttribute");

        var parName = par.GetParameterName(parameterPrefx);
        var name = par.GetParameterName(parameterPrefx);
        var dbType = par.Type.GetDbType();

        sb.AppendLine($"global::System.Data.Common.DbParameter {parName} = {CmdName}.CreateParameter();");
        sb.AppendLine($"{parName}.ParameterName = \"{name}\";");
        sb.AppendLine($"{parName}.DbType = {dbType};");
        sb.AppendLine($"{parName}.Value = {par.Name};");
        WriteParamterSpecial(sb, par, parName, columnDefine?.NamedArguments.ToDictionary(x => x.Key, x => x.Value.Value!) ?? new Dictionary<string, object>());

        return new ColumnDefine(parName, par);
    }

    private void WriteParamterSpecial(IndentedStringBuilder sb, IParameterSymbol par, string parName, Dictionary<string, object> map)
    {
        if (map.TryGetValue("Precision", out var precision))
            sb.AppendLine($"{parName}.Precision = {precision};");
        if (map.TryGetValue("Scale", out var scale))
            sb.AppendLine($"{parName}.Scale = {scale};");
        if (map.TryGetValue("Size", out var size))
            sb.AppendLine($"{parName}.Size = {size};");
        if (map.TryGetValue("Direction", out var direction))
        {
            sb.AppendLine($"{parName}.Direction = global::System.Data.{direction};");
        }
        else if (par.RefKind == RefKind.Ref)
        {
            sb.AppendLine($"{parName}.Direction = global::System.Data.InputOutput;");
        }
        else if (par.RefKind == RefKind.Out)
        {
            sb.AppendLine($"{parName}.Direction = global::System.Data.Output;");
        }
    }

    private string? GetSql()
    {
        if (RawSqlParameter != null) return RawSqlParameter.Name;
        var sqlxAttr = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlAttribute");
        return $"\"{sqlxAttr?.ConstructorArguments.FirstOrDefault().Value}\"";
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

    private string GetParamterPrefx()
    {
        var sqlAttr = MethodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlAttribute");
        return sqlAttr?.NamedArguments.FirstOrDefault(x => x.Key == "ParameterPrefix").Value.Value?.ToString() ?? "@";
    }

    private ReturnTypes GetReturnType()
    {
        if (ReturnType.SpecialType == SpecialType.System_Void) return ReturnTypes.Void;
        var actualType = ReturnType;

        if (actualType.Name == "IEnumerable") return ReturnTypes.IEnumerable;
        if (actualType.Name == "IAsyncEnumerable") return ReturnTypes.IAsyncEnumerable;
        if (actualType.Name == "List" || actualType.Name == "IList") return ReturnTypes.List;
        if (IsScalarType(actualType)) return ReturnTypes.Scalar;
        if (actualType.Name == "Dictionary"
            && actualType is INamedTypeSymbol symbol
            && symbol.IsGenericType
            && symbol.TypeParameters.Length == 2
            && symbol.TypeParameters[0].Name == "String"
            && symbol.TypeParameters[1].Name == "Object") return ReturnTypes.DictionaryStringObject;
        return ReturnTypes.Void;
    }

    private sealed class ColumnDefine
    {
        public ColumnDefine(string parameterName, IParameterSymbol symbol)
        {
            ParameterName = parameterName;
            Symbol = symbol;
        }

        public string ParameterName { get; }

        public IParameterSymbol Symbol { get; }
    }
}

internal enum ReturnTypes
{
    Void = 0,
    Scalar = 1,
    IEnumerable = 2,
    IAsyncEnumerable = 3,
    List = 4,
    DictionaryStringObject = 5,
}
