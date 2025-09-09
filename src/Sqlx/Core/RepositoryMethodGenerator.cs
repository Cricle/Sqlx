// -----------------------------------------------------------------------
// <copyright file="RepositoryMethodGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// Optimized repository method generator.
/// </summary>
internal static class RepositoryMethodGenerator
{
    /// <summary>
    /// Generates a complete repository method implementation.
    /// </summary>
    public static void GenerateMethod(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        var operation = SqlOperationInferrer.InferOperation(method);
        var isAsync = TypeAnalyzer.IsAsyncType(method.ReturnType);

        // Generate method signature
        CodeGenerator.GenerateMethodSignature(sb, method);

        // Generate method body
        GenerateMethodBody(sb, method, entityType, tableName, operation, isAsync);

        // Close method
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateMethodBody(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, SqlOperationType operation, bool isAsync)
    {
        // Variable declarations
        sb.AppendLine("System.Data.Common.DbCommand? __cmd__ = null;");
        sb.AppendLine("object? __result__ = null;");
        sb.AppendLine("var __startTime__ = System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine();

        // Try-catch block
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Connection setup
        CodeGenerator.GenerateConnectionSetup(sb, isAsync);

        // Command setup
        var sql = SqlOperationInferrer.GenerateSqlTemplate(operation, tableName, entityType);
        sb.AppendLine("__cmd__ = connection.CreateCommand();");
        sb.AppendLine($"__cmd__.CommandText = \"{sql.Replace("\"", "\\\"")}\";");
        sb.AppendLine();

        // Parameters
        CodeGenerator.GenerateParameterAssignment(sb, method, entityType);

        // Interceptors
        GenerateInterceptors(sb, method, operation, isAsync);

        // Close try block
        sb.PopIndent();
        sb.AppendLine("}");

        // Exception handling
        GenerateExceptionHandling(sb, method);

        // Finally block
        GenerateFinallyBlock(sb);
    }

    private static void GenerateInterceptors(IndentedStringBuilder sb, IMethodSymbol method, SqlOperationType operation, bool isAsync)
    {
        var methodName = method.Name;

        // OnExecuting
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute and handle result
        var returnType = TypeAnalyzer.GetInnerType(method.ReturnType);
        var entityType = TypeAnalyzer.ExtractEntityType(method.ReturnType);

        CodeGenerator.GenerateExecutionAndReturn(sb, method, operation, entityType, isAsync);

        sb.AppendLine();
        sb.AppendLine("var __elapsed__ = System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__;");
        sb.AppendLine($"OnExecuted(\"{methodName}\", __cmd__, __result__, __elapsed__);");
    }

    private static void GenerateExceptionHandling(IndentedStringBuilder sb, IMethodSymbol method)
    {
        sb.AppendLine("catch (System.Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine("var __elapsed__ = System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__;");
        sb.AppendLine($"OnExecuteFail(\"{method.Name}\", __cmd__, ex, __elapsed__);");
        sb.AppendLine("throw;");
        
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateFinallyBlock(IndentedStringBuilder sb)
    {
        sb.AppendLine("finally");
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine("__cmd__?.Dispose();");
        
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generates interceptor method stubs.
    /// </summary>
    public static void GenerateInterceptorMethods(IndentedStringBuilder sb)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called before executing a command.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("partial void OnExecuting(string methodName, System.Data.Common.DbCommand command);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called after successfully executing a command.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("partial void OnExecuted(string methodName, System.Data.Common.DbCommand command, object? result, long elapsed);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called when command execution fails.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("partial void OnExecuteFail(string methodName, System.Data.Common.DbCommand? command, System.Exception exception, long elapsed);");
        sb.AppendLine();
    }
}
