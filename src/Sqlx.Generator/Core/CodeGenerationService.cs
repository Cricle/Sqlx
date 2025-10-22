// -----------------------------------------------------------------------
// <copyright file="CodeGenerationService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using static Sqlx.Generator.SharedCodeGenerationUtilities;

namespace Sqlx.Generator;

/// <summary>
/// Default implementation of code generation service.
/// </summary>
public class CodeGenerationService
{
    private static readonly SqlTemplateEngine TemplateEngine = new();
    /// <inheritdoc/>
    public void GenerateRepositoryMethod(RepositoryMethodContext context)
    {
        var sb = context.StringBuilder;
        var method = context.Method;
        var entityType = context.EntityType;
        var processedSql = context.ProcessedSql;
        var attributeHandler = context.AttributeHandler;

        try
        {
            // Get SQL attributes once to avoid repeated calls
            var sqlxAttr = GetSqlAttribute(method);
            var sqlTemplate = GetSqlTemplateFromAttribute(sqlxAttr);

            // Process SQL template if available
            SqlTemplateResult? templateResult = null;
            if (sqlTemplate != null)
            {
                templateResult = TemplateEngine.ProcessTemplate(sqlTemplate, method, entityType, context.TableName);
            }

            // Generate method documentation with resolved SQL and template metadata
            GenerateEnhancedMethodDocumentation(sb, method, sqlTemplate, templateResult);

            // Generate or copy Sqlx attributes
            attributeHandler.GenerateOrCopyAttributes(sb, method, entityType, context.TableName);

            // Generate method signature - 使用缓存版本提升性能
            var returnType = method.ReturnType.GetCachedDisplayString();
            var methodName = method.Name;
            var parameters = string.Join(", ", method.Parameters.Select(p =>
                $"{p.Type.GetCachedDisplayString()} {p.Name}"));

            sb.AppendLine($"public {returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            if (templateResult != null)
            {
                var connectionName = GetDbConnectionFieldName(context.ClassSymbol);
                GenerateActualDatabaseExecution(sb, method, templateResult, entityType, connectionName);
            }
            else if (sqlxAttr != null)
            {
                throw new InvalidOperationException($"Failed to generate implementation for method '{method.Name}' with SQL attribute. Please check the SQL template syntax and parameters.");
            }
            else
            {
                GenerateFallbackMethodImplementation(sb, method);
            }

            // Return result if not void
            if (!method.ReturnsVoid)
            {
                // Check if the return type is Task or Task<T>
                var methodReturnType = method.ReturnType;
                var isTaskReturn = methodReturnType.Name == "Task" &&
                                   methodReturnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

                if (isTaskReturn)
                {
                    // For async methods returning Task<T>, wrap in Task.FromResult
                    sb.AppendLine("return global::System.Threading.Tasks.Task.FromResult(__result__);");
                }
                else
                {
                    // For synchronous methods, cast and return directly
                    var returnTypeName = methodReturnType.GetCachedDisplayString();
                    sb.AppendLine($"return ({returnTypeName})__result__;");
                }
            }

            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }
        catch (System.Exception)
        {
            // Generate a fallback method on error
            GenerateFallbackMethod(sb, method);
        }
    }

    /// <inheritdoc/>
    public void GenerateRepositoryImplementation(RepositoryGenerationContext context)
    {
        var repositoryClass = context.RepositoryClass;

        // Skip if the class has SqlTemplate attribute
        if (HasAttributeWithName(repositoryClass, "SqlTemplate"))
            return;

        // Get the service interface from RepositoryFor attribute
        var serviceInterface = GetServiceInterface(context);
        if (serviceInterface == null)
            return;

        // 简化：直接从接口名推断实体类型和表名
        var entityType = InferEntityTypeFromInterface(serviceInterface);
        var tableName = GetTableNameFromType(repositoryClass, entityType);

        var sb = new IndentedStringBuilder(string.Empty);

        // Generate the repository implementation
        GenerateRepositoryClass(sb, context, serviceInterface, entityType, tableName);

        // Add source to compilation
        var sourceText = SourceText.From(sb.ToString().Trim(), Encoding.UTF8);
        var fileName = $"{repositoryClass.GetCachedDisplayString().Replace(".", "_")}.Repository.g.cs";  // 使用缓存版本
        context.ExecutionContext.AddSource(fileName, sourceText);
    }

    /// <summary>
    /// Generate enhanced method documentation with detailed template processing information
    /// </summary>
    public void GenerateEnhancedMethodDocumentation(IndentedStringBuilder sb, IMethodSymbol method, string? originalTemplate, SqlTemplateResult? templateResult)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {GetMethodDescription(method)}");

        // Add detailed information if template processing results are available
        if (templateResult != null)
        {
            // Show original template (use para instead of code to avoid XML parsing issues)
            if (!string.IsNullOrEmpty(originalTemplate))
            {
                sb.AppendLine("/// <para>📝 Original Template:</para>");
                // Use para tags to avoid XML code block parsing issues with multi-line SQL
                foreach (var line in originalTemplate.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        sb.AppendLine($"/// <para>    {System.Security.SecurityElement.Escape(trimmedLine)}</para>");
                    }
                }
            }

            // Show processed SQL
            if (!string.IsNullOrEmpty(templateResult.ProcessedSql))
            {
                sb.AppendLine("/// <para>📋 Generated SQL (Template Processed):</para>");
                // Use para tags to avoid XML code block parsing issues with multi-line SQL
                foreach (var line in templateResult.ProcessedSql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        sb.AppendLine($"/// <para>    {System.Security.SecurityElement.Escape(trimmedLine)}</para>");
                    }
                }
            }

            // Show parameter information
            // 性能优化：使用Count检查集合是否为空，比Any()更直接
            if (templateResult.Parameters.Count > 0)
            {
                sb.AppendLine("/// <para>🔧 Template Parameters:</para>");
                foreach (var param in templateResult.Parameters)
                {
                    sb.AppendLine($"/// <para>  • @{param.Key}</para>");
                }
            }

            // Show dynamic features
            if (templateResult.HasDynamicFeatures)
            {
                sb.AppendLine("/// <para>⚡ Contains dynamic template features (conditions, loops, functions)</para>");
            }

            // Show warning information
            // 性能优化：使用Count检查集合是否为空，比Any()更直接
            if (templateResult.Warnings.Count > 0)
            {
                sb.AppendLine("/// <para>⚠️ Template Warnings:</para>");
                foreach (var warning in templateResult.Warnings)
                {
                    sb.AppendLine($"/// <para>  • {System.Security.SecurityElement.Escape(warning)}</para>");
                }
            }

            // Show error information
            // 性能优化：使用Count检查集合是否为空，比Any()更直接
            if (templateResult.Errors.Count > 0)
            {
                sb.AppendLine("/// <para>❌ Template Errors:</para>");
                foreach (var error in templateResult.Errors)
                {
                    sb.AppendLine($"/// <para>  • {System.Security.SecurityElement.Escape(error)}</para>");
                }
            }

            sb.AppendLine("/// <para>🚀 This method was generated by Sqlx Advanced Template Engine</para>");
        }

        sb.AppendLine("/// </summary>");

        foreach (var parameter in method.Parameters)
        {
            sb.AppendLine($"/// <param name=\"{parameter.Name}\">{GetParameterDescription(parameter)}</param>");
        }

        if (!method.ReturnsVoid)
        {
            sb.AppendLine($"/// <returns>{GetReturnDescription(method)}</returns>");
        }
    }

    private INamedTypeSymbol? GetServiceInterface(RepositoryGenerationContext context)
    {
        var repositoryForAttr = GetRepositoryForAttribute(context.RepositoryClass);

        if (repositoryForAttr?.ConstructorArguments.Length > 0)
        {
            var typeArg = repositoryForAttr.ConstructorArguments[0];
            if (typeArg.Value is INamedTypeSymbol serviceType)
                return serviceType;
        }

        // 简化：如果没有RepositoryFor属性，返回null
        return GetServiceInterfaceFromSyntax(context);
    }

    private INamedTypeSymbol? GetServiceInterfaceFromSyntax(RepositoryGenerationContext context)
    {
        try
        {
            var repositoryClass = context.RepositoryClass;
            var compilation = context.ExecutionContext.Compilation;

            // Get the syntax node for the repository class
            var syntaxReferences = repositoryClass.DeclaringSyntaxReferences;
            if (syntaxReferences.Length == 0)
                return null;

            if (syntaxReferences[0].GetSyntax() is not Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDeclaration)
                return null;

            // Look for RepositoryFor attribute in the syntax
            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();

                    if (attributeName == "RepositoryFor" || attributeName == "RepositoryForAttribute")
                    {
                        // Look for typeof(InterfaceName) in the arguments
                        if (attribute.ArgumentList?.Arguments.Count > 0)
                        {
                            var argText = attribute.ArgumentList.Arguments[0].ToString();

                            // Parse typeof(InterfaceName) pattern
                            if (argText.StartsWith("typeof(") && argText.EndsWith(")"))
                            {
                                var interfaceName = argText.Substring(7, argText.Length - 8);

                                // Try to find the interface type in the compilation
                                var interfaceType = compilation.GetTypeByMetadataName(interfaceName) ??
                                    compilation.GetTypeByMetadataName($"{repositoryClass.ContainingNamespace.GetCachedDisplayString()}.{interfaceName}");

                                if (interfaceType != null)
                                    return interfaceType;
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch (System.Exception)
        {
            // Return null for any reflection or analysis errors
            return null;
        }
    }

    private void GenerateRepositoryClass(IndentedStringBuilder sb, RepositoryGenerationContext context,
        INamedTypeSymbol serviceInterface, INamedTypeSymbol? entityType, string tableName)
    {
        var repositoryClass = context.RepositoryClass;
        var namespaceName = repositoryClass.ContainingNamespace.GetCachedDisplayString();  // 使用缓存版本

        // Generate namespace and usings
        // Generate namespace and usings using shared utility
        GenerateFileHeader(sb, namespaceName);
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using Sqlx.Annotations;");
        sb.AppendLine();

        // Generate partial class
        sb.AppendLine($"partial class {repositoryClass.Name}");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate connection field if needed
        // Skip DbConnection field generation as it's likely already defined in partial class
        // GenerateDbConnectionFieldIfNeeded(sb, repositoryClass);

        // Generate repository methods using template engine
        foreach (var method in serviceInterface.GetMembers().OfType<IMethodSymbol>())
        {
            var sqlxAttr = GetSqlAttribute(method);
            var sql = GetSqlTemplateFromAttribute(sqlxAttr);

            if (sql != null)
            {
                // Use template engine to process SQL template
                var templateEngine = context.TemplateEngine;
                var templateResult = templateEngine.ProcessTemplate(sql, method, entityType, tableName);

                var methodContext = new RepositoryMethodContext(
                    sb, method, entityType, tableName, templateResult.ProcessedSql,
                    context.AttributeHandler, context.RepositoryClass);

                GenerateRepositoryMethod(methodContext);
            }
            else
            {
                // Check if this method has SqlTemplate attribute - if so, report error instead of fallback
                if (HasAttributeWithName(method, "SqlTemplate"))
                {
                    // Report compilation error for SqlTemplate methods that can't be generated
                    throw new InvalidOperationException($"Failed to generate implementation for method '{method.Name}' with SqlTemplateAttribute. Please check the SQL template syntax and parameters.");
                }

                GenerateFallbackMethod(sb, method);
            }
        }

        // Generate interceptor methods
        GenerateInterceptorMethods(sb, repositoryClass);

        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generate interceptor methods for the specified repository class, including pre and post execution callbacks.
    /// </summary>
    /// <param name="sb">The string builder used to construct code.</param>
    /// <param name="repositoryClass">The repository class symbol to generate interceptor methods for.</param>
    public void GenerateInterceptorMethods(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called before executing a repository operation.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"operationName\">The name of the operation being executed.</param>");
        sb.AppendLine("/// <param name=\"command\">The database command to be executed.</param>");
        sb.AppendLine("partial void OnExecuting(string operationName, global::System.Data.IDbCommand command);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called after successfully executing a repository operation.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"operationName\">The name of the operation that was executed.</param>");
        sb.AppendLine("/// <param name=\"command\">The database command that was executed.</param>");
        sb.AppendLine("/// <param name=\"result\">The result of the operation.</param>");
        sb.AppendLine("/// <param name=\"elapsedTicks\">The elapsed time in ticks.</param>");
        sb.AppendLine("partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called when a repository operation fails with an exception.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"operationName\">The name of the operation that failed.</param>");
        sb.AppendLine("/// <param name=\"command\">The database command that failed.</param>");
        sb.AppendLine("/// <param name=\"exception\">The exception that occurred.</param>");
        sb.AppendLine("/// <param name=\"elapsedTicks\">The elapsed time in ticks before failure.</param>");
        sb.AppendLine("partial void OnExecuteFail(string operationName, global::System.Data.IDbCommand command, global::System.Exception exception, long elapsedTicks);");
        sb.AppendLine();
    }

    private void GenerateFallbackMethod(IndentedStringBuilder sb, IMethodSymbol method)
    {
        sb.AppendLine($"// Error generating method {method.Name}: Generation failed");
        var returnType = method.ReturnType.GetCachedDisplayString();  // 使用缓存版本
        var parameters = string.Join(", ", method.Parameters.Select(p =>
            $"{p.Type.GetCachedDisplayString()} {p.Name}"));  // 使用缓存版本

        sb.AppendLine($"public {returnType} {method.Name}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        if (!method.ReturnsVoid)
        {
            sb.AppendLine($"return default({returnType});");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private string GetMethodDescription(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();

        if (methodName.Contains("create") || methodName.Contains("insert") || methodName.Contains("add"))
        {
            return "Creates a new entity in the database.";
        }
        else if (methodName.Contains("update") || methodName.Contains("modify"))
        {
            return "Updates an existing entity in the database.";
        }
        else if (methodName.Contains("delete") || methodName.Contains("remove"))
        {
            return "Deletes an entity from the database.";
        }
        else if (methodName.Contains("get") || methodName.Contains("find") || methodName.Contains("select"))
        {
            return "Retrieves entity data from the database.";
        }
        else if (methodName.Contains("count"))
        {
            return "Counts the number of entities in the database.";
        }
        else if (methodName.Contains("exists"))
        {
            return "Checks if an entity exists in the database.";
        }

        return "Executes a database operation.";
    }

    private string GetParameterDescription(IParameterSymbol parameter)
    {
        if (parameter.Type.Name == "CancellationToken")
        {
            return "A cancellation token that can be used to cancel the operation.";
        }

        if (parameter.Type.TypeKind == TypeKind.Class && parameter.Type.Name != "String")
        {
            return $"The {parameter.Type.Name} entity to process.";
        }

        return $"The {parameter.Name} parameter.";
    }

    private string GetReturnDescription(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var methodName = method.Name.ToLowerInvariant();

        if (returnType.Name == "Task")
        {
            if (returnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length == 0)
            {
                return "A task representing the asynchronous operation.";
            }
            else if (returnType is INamedTypeSymbol genericTask && genericTask.TypeArguments.Length == 1)
            {
                var innerType = genericTask.TypeArguments[0];
                if (innerType is INamedTypeSymbol namedType &&
                    (namedType.Name is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList" ||
                     (namedType.IsGenericType && namedType.Name is "IList" or "List" or "IEnumerable" or "ICollection" or "IReadOnlyList")))
                {
                    return $"A task containing the collection of entities.";
                }
                else if (innerType.SpecialType == SpecialType.System_Int32)
                {
                    return "A task containing the number of affected rows.";
                }
                else
                {
                    return $"A task containing the {innerType.Name} result.";
                }
            }
        }

        if (returnType.IsCollectionType())
        {
            return "A collection of entities.";
        }

        if (returnType.SpecialType == SpecialType.System_Int32)
        {
            if (methodName.Contains("create") || methodName.Contains("insert") ||
                methodName.Contains("update") || methodName.Contains("delete"))
            {
                return "The number of affected rows.";
            }
            return "The result value.";
        }

        return $"The {returnType.Name} result.";
    }

    private void GenerateActualDatabaseExecution(IndentedStringBuilder sb, IMethodSymbol method, SqlTemplateResult templateResult, INamedTypeSymbol? entityType, string connectionName)
    {
        var returnType = method.ReturnType;
        var returnTypeString = returnType.GetCachedDisplayString();  // 使用缓存版本
        var resultVariableType = ExtractInnerTypeFromTask(returnTypeString);
        var operationName = method.Name;
        var repositoryType = method.ContainingType.Name;

        // 从方法返回类型重新推断实体类型（覆盖接口级别的推断）
        // 这样可以正确处理返回标量的方法（如 INSERT 返回 ID）
        var methodEntityType = TryInferEntityTypeFromMethodReturnType(returnType);
        // 如果方法返回实体类型，使用方法级别的推断
        // 如果方法返回标量类型（methodEntityType == null），也要覆盖以避免错误映射
        entityType = methodEntityType;

        // Generate execution context (stack allocation, string literals for zero ToString() overhead)
        sb.AppendLine("// 创建执行上下文（栈分配，使用字符串字面量）");
        sb.AppendLine("var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(");
        sb.PushIndent();
        sb.AppendLine($"\"{operationName}\",");
        sb.AppendLine($"\"{repositoryType}\",");
        sb.AppendLine($"@\"{EscapeSqlForCSharp(templateResult.ProcessedSql)}\");");
        sb.PopIndent();
        sb.AppendLine();

        // Generate method variables
        sb.AppendLine($"{resultVariableType} __result__ = default!;");
        sb.AppendLine("global::System.Data.IDbCommand? __cmd__ = null;");
        sb.AppendLine();

        // Use shared utilities for database setup
        sb.AppendLine($"if ({connectionName}.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{connectionName}.Open();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        SharedCodeGenerationUtilities.GenerateCommandSetup(sb, templateResult.ProcessedSql, method, connectionName);

        // Add try-catch block with interceptors
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Call global interceptor - OnExecuting
        sb.AppendLine("// 全局拦截器：执行前");
        sb.AppendLine("global::Sqlx.Interceptors.SqlxInterceptors.OnExecuting(ref __ctx__);");
        sb.AppendLine();

        // Call partial method interceptor (for backward compatibility)
        sb.AppendLine("// Partial方法拦截器（向后兼容）");
        sb.AppendLine($"OnExecuting(\"{operationName}\", __cmd__);");
        sb.AppendLine();

        // 性能优化：单次分类返回类型，避免重复计算
        var (returnCategory, innerType) = ClassifyReturnType(returnTypeString);
        switch (returnCategory)
        {
            case ReturnTypeCategory.Scalar:
                GenerateScalarExecution(sb, innerType);
                break;
            case ReturnTypeCategory.Collection:
                GenerateCollectionExecution(sb, returnTypeString, entityType);
                break;
            case ReturnTypeCategory.SingleEntity:
                GenerateSingleEntityExecution(sb, returnTypeString, entityType);
                break;
            default:
                // Non-query execution (INSERT, UPDATE, DELETE)
                sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
                break;
        }

        sb.AppendLine();

        // Update context
        sb.AppendLine("// 更新执行上下文");
        sb.AppendLine("__ctx__.EndTimestamp = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("__ctx__.Result = __result__;");
        sb.AppendLine();

        // Call global interceptor - OnExecuted
        sb.AppendLine("// 全局拦截器：执行成功");
        sb.AppendLine("global::Sqlx.Interceptors.SqlxInterceptors.OnExecuted(ref __ctx__);");
        sb.AppendLine();

        // Call partial method interceptor (for backward compatibility)
        sb.AppendLine("// Partial方法拦截器（向后兼容）");
        sb.AppendLine($"OnExecuted(\"{operationName}\", __cmd__, __result__, __ctx__.EndTimestamp - __ctx__.StartTimestamp);");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (global::System.Exception __ex__)");
        sb.AppendLine("{");
        sb.PushIndent();

        // Update context with exception
        sb.AppendLine("// 更新执行上下文");
        sb.AppendLine("__ctx__.EndTimestamp = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("__ctx__.Exception = __ex__;");
        sb.AppendLine();

        // Call global interceptor - OnFailed
        sb.AppendLine("// 全局拦截器：执行失败");
        sb.AppendLine("global::Sqlx.Interceptors.SqlxInterceptors.OnFailed(ref __ctx__);");
        sb.AppendLine();

        // Call partial method interceptor (for backward compatibility)
        sb.AppendLine("// Partial方法拦截器（向后兼容）");
        sb.AppendLine($"OnExecuteFail(\"{operationName}\", __cmd__, __ex__, __ctx__.EndTimestamp - __ctx__.StartTimestamp);");
        sb.AppendLine();

        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Escape SQL string for C# verbatim string literal
    /// </summary>
    private static string EscapeSqlForCSharp(string sql)
    {
        return sql.Replace("\"", "\"\"");
    }

    // 性能优化：枚举避免重复字符串比较
    private enum ReturnTypeCategory
    {
        Scalar,
        Collection,
        SingleEntity,
        Unknown
    }

    // 性能优化：单次调用ExtractInnerTypeFromTask，避免重复计算
    private (ReturnTypeCategory Category, string InnerType) ClassifyReturnType(string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);

        // 检查标量类型
        if (innerType == "int" || innerType == "bool" || innerType == "decimal" || innerType == "double" || innerType == "string" || innerType == "long")
            return (ReturnTypeCategory.Scalar, innerType);

        // 检查集合类型（支持完全限定名称）
        if (innerType.Contains("List<") || innerType.Contains(".List<") ||  // System.Collections.Generic.List<>
            innerType.Contains("IEnumerable<") || innerType.Contains(".IEnumerable<") ||
            innerType.Contains("ICollection<") || innerType.Contains(".ICollection<") ||
            innerType.Contains("[]"))
            return (ReturnTypeCategory.Collection, innerType);

        // 检查单实体类型
        if (!innerType.Equals("int", StringComparison.OrdinalIgnoreCase))
            return (ReturnTypeCategory.SingleEntity, innerType);

        return (ReturnTypeCategory.Unknown, innerType);
    }

    private bool IsScalarReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.Scalar;
    private bool IsCollectionReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.Collection;
    private bool IsSingleEntityReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.SingleEntity;

    private void GenerateScalarExecution(IndentedStringBuilder sb, string innerType)
    {
        sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");
        sb.AppendLine($"__result__ = scalarResult != null ? ({innerType})scalarResult : default({innerType});");
    }

    private void GenerateCollectionExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        // 确保使用全局命名空间前缀，避免命名冲突
        var collectionType = innerType.StartsWith("System.") ? $"global::{innerType}" : innerType;
        sb.AppendLine($"__result__ = new {collectionType}();");
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("while (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityFromReader(sb, entityType, "item");
            sb.AppendLine($"(({collectionType})__result__).Add(item);");
        }

        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateSingleEntityExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType)
    {
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("if (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityFromReader(sb, entityType, "__result__");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("else");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__result__ = default;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateEntityFromReader(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName)
    {
        // Use shared utility for entity mapping
        SharedCodeGenerationUtilities.GenerateEntityMapping(sb, entityType, variableName);
    }

    private void GenerateFallbackMethodImplementation(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var returnType = method.ReturnType.GetCachedDisplayString();  // 使用缓存版本

        if (!method.ReturnsVoid)
        {
            if (returnType.Contains("Task"))
            {
                var innerType = SharedCodeGenerationUtilities.ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"__result__ = default({innerType});");
            }
            else
            {
                sb.AppendLine($"__result__ = default({returnType});");
            }
        }
    }


    internal static string GetDbConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        // 性能优化：一次性获取所有成员，避免重复遍历（使用数组）
        var allMembers = repositoryClass.GetMembers().ToArray();

        // 1. 首先检查字段 - 按类型和名称模式查找
        var connectionField = allMembers
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.IsDbConnection() || IsConnectionNamePattern(f.Name));
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // 2. 检查属性 - 按类型和名称模式查找
        var connectionProperty = allMembers
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsDbConnection() || IsConnectionNamePattern(p.Name));
        if (connectionProperty != null)
        {
            return connectionProperty.Name;
        }

        // 3. Check primary constructor parameters
        var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
        if (primaryConstructor != null)
        {
            var connectionParam = primaryConstructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                return connectionParam.Name;
            }
        }

        // 4. Check regular constructor parameters (fallback)
        var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
        if (constructor != null)
        {
            var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                return connectionParam.Name;
            }
        }

        // Default fallback - common field names
        return "connection";
    }

    /// <summary>Get SQL attribute from method, checking both Sqlx and SqlTemplate attributes</summary>
    private static AttributeData? GetSqlAttribute(IMethodSymbol method) =>
        method.GetSqlxAttribute();  // 使用扩展方法简化代码

    /// <summary>Get SQL template string from attribute</summary>
    private static string? GetSqlTemplateFromAttribute(AttributeData? attribute) =>
        attribute?.ConstructorArguments.FirstOrDefault().Value as string;

    /// <summary>Check if attribute has specific name</summary>
    private static bool HasAttributeWithName(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name?.Contains(attributeName) == true);

    /// <summary>Get RepositoryFor attribute from class</summary>
    private static AttributeData? GetRepositoryForAttribute(INamedTypeSymbol repositoryClass) =>
        repositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");

    /// <summary>Check if name matches common connection name patterns</summary>
    private static bool IsConnectionNamePattern(string name) =>
        name == "connection" ||
        name == "_connection" ||
        name == "Connection" ||
        name == "_Connection" ||
        name.EndsWith("Connection", StringComparison.OrdinalIgnoreCase);

    private INamedTypeSymbol? InferEntityTypeFromInterface(INamedTypeSymbol serviceInterface)
    {
        // 1. 尝试从接口的泛型参数中获取实体类型
        if (serviceInterface.TypeArguments.Length > 0)
            return serviceInterface.TypeArguments[0] as INamedTypeSymbol;

        // 2. 如果接口不是泛型的，尝试从方法返回类型推断
        // 遍历接口的所有方法，找到第一个返回实体类型的方法
        foreach (var member in serviceInterface.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                // 先尝试从返回类型推断
                var entityType = TryInferEntityTypeFromMethodReturnType(method.ReturnType);
                if (entityType != null)
                    return entityType;

                // 如果返回类型不是实体，尝试从参数类型推断（用于INSERT/UPDATE等方法）
                foreach (var param in method.Parameters)
                {
                    if (param.Type is INamedTypeSymbol paramType && !IsScalarType(paramType))
                    {
                        // 排除常见的非实体类型
                        var paramTypeName = paramType.GetCachedDisplayString();
                        if (!paramTypeName.StartsWith("System.") &&
                            !paramTypeName.StartsWith("Microsoft.") &&
                            !paramType.IsGenericType)
                        {
                            return paramType;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 从方法返回类型推断实体类型
    /// 支持: User, User?, Task&lt;User&gt;, Task&lt;User?&gt;, Task&lt;List&lt;User&gt;&gt;, IEnumerable&lt;User&gt; 等
    /// </summary>
    private INamedTypeSymbol? TryInferEntityTypeFromMethodReturnType(ITypeSymbol returnType)
    {
        // 处理可空类型: User? -> User
        if (returnType.NullableAnnotation == NullableAnnotation.Annotated && returnType is INamedTypeSymbol namedType)
        {
            // 对于值类型的可空类型 (Nullable<T>)，获取 T
            if (namedType.IsGenericType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                returnType = namedType.TypeArguments[0];
            }
        }

        // 如果是泛型类型，检查是否是 Task/ValueTask/List/IEnumerable 等容器
        if (returnType is INamedTypeSymbol namedReturnType && namedReturnType.IsGenericType)
        {
            var typeName = namedReturnType.ConstructedFrom.GetCachedDisplayString();

            // Task<T>, ValueTask<T>
            if (typeName.StartsWith("System.Threading.Tasks.Task<") ||
                typeName.StartsWith("System.Threading.Tasks.ValueTask<"))
            {
                var innerType = namedReturnType.TypeArguments[0];
                return TryInferEntityTypeFromMethodReturnType(innerType); // 递归处理内层类型
            }

            // List<T>, IEnumerable<T>, ICollection<T>, IReadOnlyList<T> 等集合类型
            if (typeName.Contains("List<") ||
                typeName.Contains("IEnumerable<") ||
                typeName.Contains("ICollection<") ||
                typeName.Contains("IReadOnlyList<") ||
                typeName.Contains("IReadOnlyCollection<"))
            {
                var elementType = namedReturnType.TypeArguments[0];
                // 集合的元素类型如果不是基元类型，则认为是实体类型
                if (elementType is INamedTypeSymbol namedElementType && !IsScalarType(namedElementType))
                {
                    return namedElementType;
                }
            }
        }

        // 如果返回类型本身是一个命名类型，且不是基元类型或Task，则可能是实体类型
        if (returnType is INamedTypeSymbol candidateType &&
            !IsScalarType(candidateType) &&
            !candidateType.GetCachedDisplayString().StartsWith("System.Threading.Tasks."))
        {
            return candidateType;
        }

        return null;
    }

    /// <summary>
    /// 判断是否是标量类型（基元类型、string、DateTime 等，而非实体类型）
    /// </summary>
    private bool IsScalarType(INamedTypeSymbol type)
    {
        // 基元类型（int, long, bool, string 等）
        if (type.SpecialType != SpecialType.None)
            return true;

        var typeName = type.GetCachedDisplayString();

        // 常见的标量类型
        if (typeName == "System.DateTime" ||
            typeName == "System.DateTimeOffset" ||
            typeName == "System.TimeSpan" ||
            typeName == "System.Guid" ||
            typeName == "System.Decimal" ||
            typeName == "System.Byte[]")
        {
            return true;
        }

        // System命名空间下的值类型通常是标量
        if (typeName.StartsWith("System.") && type.IsValueType)
        {
            return true;
        }

        return false;
    }

    private string GetTableNameFromType(INamedTypeSymbol repositoryClass, INamedTypeSymbol? entityType)
    {
        // 简化：使用实体类型名作为表名，如果没有则使用repository类名
        return entityType?.Name ?? repositoryClass.Name.Replace("Repository", "");
    }
}
