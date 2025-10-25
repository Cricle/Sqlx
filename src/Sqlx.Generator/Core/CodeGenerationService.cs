// -----------------------------------------------------------------------
// <copyright file="CodeGenerationService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
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
            {
                var paramType = p.Type.GetCachedDisplayString();
                var paramName = p.Name;
                // Include default value if parameter has one
                var defaultValue = p.HasExplicitDefaultValue ? $" = {GetDefaultValueString(p)}" : string.Empty;
                return $"{paramType} {paramName}{defaultValue}";
            }));

            sb.AppendLine($"public {returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            if (templateResult != null)
            {
                var connectionName = GetDbConnectionFieldName(context.ClassSymbol);
                GenerateActualDatabaseExecution(sb, method, templateResult, entityType, connectionName, context.ClassSymbol);
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
        catch (System.Exception ex)
        {
            // 🔴 重新抛出异常，附加详细的上下文信息
            // 不要吞没异常，这会导致调试困难
            var methodName = method.Name;
            var className = method.ContainingType?.Name ?? "Unknown";
            var errorMessage = $"Failed to generate method '{methodName}' in class '{className}'. " +
                             $"Error: {ex.Message}. " +
                             $"Check the SQL template syntax, method parameters, and entity type definition.";

            throw new InvalidOperationException(errorMessage, ex);
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

            // Show parameter information with types
            // 性能优化：使用Count检查集合是否为空，比Any()更直接
            if (method.Parameters.Length > 0)
            {
                sb.AppendLine("/// <para>📌 Method Parameters:</para>");
                foreach (var param in method.Parameters)
                {
                    var paramType = param.Type.GetCachedDisplayString();
                    var paramName = param.Name;
                    // 检查是否有特殊特性
                    var attributes = string.Empty;
                    if (param.GetAttributes().Any(a => a.AttributeClass?.Name == "DynamicSqlAttribute"))
                    {
                        attributes = " [DynamicSql]";
                    }
                    else if (param.GetAttributes().Any(a => a.AttributeClass?.Name == "ExpressionToSqlAttribute"))
                    {
                        attributes = " [ExpressionToSql]";
                    }
                    sb.AppendLine($"/// <para>  • {paramType} {paramName}{attributes}</para>");
                }
            }

            // Show SQL parameter placeholders
            if (templateResult.Parameters.Count > 0)
            {
                sb.AppendLine("/// <para>🔧 SQL Parameter Placeholders:</para>");
                foreach (var param in templateResult.Parameters)
                {
                    // 尝试从方法参数中找到对应的类型
                    var methodParam = method.Parameters.FirstOrDefault(p =>
                        string.Equals(p.Name, param.Key, StringComparison.OrdinalIgnoreCase));
                    var paramInfo = methodParam != null
                        ? $"@{param.Key} ({methodParam.Type.GetCachedDisplayString()})"
                        : $"@{param.Key}";
                    sb.AppendLine($"/// <para>  • {paramInfo}</para>");
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

        if (repositoryForAttr != null)
        {
            // Check if it's a generic RepositoryFor<T> attribute
            if (repositoryForAttr.AttributeClass is INamedTypeSymbol attrClass && attrClass.IsGenericType)
            {
                // Generic version: RepositoryFor<TService>
                // TService can be a simple type or a generic type like ICrudRepository<User, int>
                var typeArg = attrClass.TypeArguments.FirstOrDefault();
                if (typeArg is INamedTypeSymbol serviceType)
                {
                    // Return the type directly - it can be a constructed generic type
                    return serviceType;
                }
                // Handle ITypeSymbol that might not be INamedTypeSymbol
                if (typeArg != null && typeArg.TypeKind == TypeKind.Interface)
                {
                    return typeArg as INamedTypeSymbol;
                }
            }
            // Non-generic version: RepositoryFor(typeof(TService))
            else if (repositoryForAttr.ConstructorArguments.Length > 0)
            {
                var typeArg = repositoryForAttr.ConstructorArguments[0];
                // Handle both simple types and generic types
                if (typeArg.Value is INamedTypeSymbol serviceType)
                {
                    // Return the type directly - it can be a constructed generic type
                    return serviceType;
                }
                // Try to get as ITypeSymbol first
                if (typeArg.Value is ITypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Interface)
                {
                    return typeSymbol as INamedTypeSymbol;
                }
            }
        }

        // Fallback: try to get from syntax
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
        catch (System.Exception ex)
        {
            // 🔴 记录异常信息，在DEBUG模式下输出诊断
            // 这个方法用于推断接口类型，失败时返回null是合理的，但应该记录原因
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] Failed to get service interface from syntax for class '{context.RepositoryClass.Name}': {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] Stack trace: {ex.StackTrace}");
#else
            // 在Release模式下，避免编译器警告
            _ = ex;
#endif
            // 在生产环境仍然返回null，让调用者处理
            // 但至少在开发时能看到错误信息
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
        // Support interface inheritance - collect methods from base interfaces too
        var allMethods = GetAllInterfaceMethods(serviceInterface);

        foreach (var method in allMethods)
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
        {
            var paramType = p.Type.GetCachedDisplayString();
            var paramName = p.Name;
            var defaultValue = p.HasExplicitDefaultValue ? $" = {GetDefaultValueString(p)}" : string.Empty;
            return $"{paramType} {paramName}{defaultValue}";
        }));

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

    private void GenerateActualDatabaseExecution(IndentedStringBuilder sb, IMethodSymbol method, SqlTemplateResult templateResult, INamedTypeSymbol? entityType, string connectionName, INamedTypeSymbol classSymbol)
    {
        var returnType = method.ReturnType;
        var returnTypeString = returnType.GetCachedDisplayString();  // 使用缓存版本

        // 🚀 强制启用追踪和指标（性能影响微小，提供完整可观测性）
        var resultVariableType = ExtractInnerTypeFromTask(returnTypeString);
        var operationName = method.Name;
        var repositoryType = method.ContainingType.Name;

        // 从方法返回类型重新推断实体类型（覆盖接口级别的推断）
        // 这样可以正确处理返回标量的方法（如 INSERT 返回 ID）
        var methodEntityType = TryInferEntityTypeFromMethodReturnType(returnType);

        // ⚠️ IMPORTANT: Save original entityType for soft delete checking BEFORE overwriting
        // Soft delete needs the original entity type from the interface/class level
        var originalEntityType = entityType;

        // 如果方法返回实体类型，使用方法级别的推断
        // 如果方法返回标量类型（methodEntityType == null），也要覆盖以避免错误映射
        entityType = methodEntityType;

        // 🚀 Activity跟踪和指标（默认禁用以获得最佳性能，可通过定义SQLX_ENABLE_TRACING启用）
        sb.AppendLine("#if SQLX_ENABLE_TRACING");
        sb.AppendLine("// Activity跟踪（可通过定义SQLX_ENABLE_TRACING条件编译启用）");
        sb.AppendLine("var __activity__ = global::System.Diagnostics.Activity.Current;");
        sb.AppendLine("var __startTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine();
        sb.AppendLine("// 设置Activity标签（如果存在）");
        sb.AppendLine("if (__activity__ != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"__activity__.DisplayName = \"{operationName}\";");
        sb.AppendLine("__activity__.SetTag(\"db.system\", \"sql\");");
        sb.AppendLine($"__activity__.SetTag(\"db.operation\", \"{operationName}\");");
        sb.AppendLine($"__activity__.SetTag(\"db.statement\", @\"{EscapeSqlForCSharp(templateResult.ProcessedSql)}\");");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // Generate method variables
        sb.AppendLine($"{resultVariableType} __result__ = default!;");
        sb.AppendLine("global::System.Data.IDbCommand? __cmd__ = null;");
        sb.AppendLine();

        // 🔐 动态占位符验证（如果模板包含动态特性）
        if (templateResult.HasDynamicFeatures)
        {
            GenerateDynamicPlaceholderValidation(sb, method);
        }

        // Use shared utilities for database setup
        // 🚀 性能优化：默认不检查连接状态（假设调用者已打开连接）
        // 如需自动打开连接，可定义 SQLX_ENABLE_AUTO_OPEN 条件编译符号
        // 这样可以减少每次查询8-12%的开销
        sb.AppendLine("#if SQLX_ENABLE_AUTO_OPEN");
        sb.AppendLine($"if ({connectionName}.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{connectionName}.Open();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // 🚀 TDD Phase 3: Check for batch INSERT operation FIRST (before any SQL modifications)
        var processedSql = templateResult.ProcessedSql;
        var hasBatchValues = processedSql.Contains("__RUNTIME_BATCH_VALUES_");

        if (hasBatchValues)
        {
            // Generate batch INSERT code (complete execution flow)
            GenerateBatchInsertCode(sb, processedSql, method, originalEntityType, connectionName);
            return; // Batch INSERT handles everything, exit early
        }

        // 🚀 TDD Green: Check for [ReturnInsertedId] or [ReturnInsertedEntity] and modify SQL
        var hasReturnInsertedId = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedIdAttribute" || a.AttributeClass?.Name == "ReturnInsertedId");
        var hasReturnInsertedEntity = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedEntityAttribute" || a.AttributeClass?.Name == "ReturnInsertedEntity");

        if (hasReturnInsertedId)
        {
            var dbDialect = GetDatabaseDialect(classSymbol);
            processedSql = AddReturningClauseForInsert(processedSql, dbDialect, returnAll: false);
        }
        else if (hasReturnInsertedEntity)
        {
            var dbDialect = GetDatabaseDialect(classSymbol);
            processedSql = AddReturningClauseForInsert(processedSql, dbDialect, returnAll: true);
        }

        // 🚀 TDD Green: Check for [SoftDelete]
        // Use originalEntityType (not entityType which may be null for scalar returns)
        var softDeleteConfig = GetSoftDeleteConfig(originalEntityType);
        var wasDeleteConverted = false;  // Track if DELETE was converted to UPDATE

        if (softDeleteConfig != null)
        {
            var hasIncludeDeleted = method.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "IncludeDeletedAttribute" || a.AttributeClass?.Name == "IncludeDeleted");

            // Convert DELETE to UPDATE (soft delete)
            if (processedSql.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var dbDialect = GetDatabaseDialect(classSymbol);
                var entityTableName = originalEntityType?.Name ?? "table";
                processedSql = ConvertDeleteToSoftDelete(processedSql, softDeleteConfig, dbDialect, entityTableName);
                wasDeleteConverted = true;  // Mark that DELETE was converted to UPDATE
            }
            // Add soft delete filter to SELECT queries (if not already present and not [IncludeDeleted])
            else if (!hasIncludeDeleted && processedSql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var flagColumn = SharedCodeGenerationUtilities.ConvertToSnakeCase(softDeleteConfig.FlagColumn);
                var hasWhere = processedSql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!hasWhere)
                {
                    // No WHERE clause - add one
                    processedSql = processedSql + $" WHERE {flagColumn} = false";
                }
                else
                {
                    // Has WHERE clause - append with AND
                    var whereIndex = processedSql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
                    var insertIndex = whereIndex + 5; // Length of "WHERE"
                    processedSql = processedSql.Insert(insertIndex, $" {flagColumn} = false AND");
                }
            }
        }

        // 🚀 TDD Green: Check for [AuditFields]
        var auditFieldsConfig = GetAuditFieldsConfig(originalEntityType);

        if (auditFieldsConfig != null)
        {
            var dbDialect = GetDatabaseDialect(classSymbol);

            // INSERT: Add CreatedAt, CreatedBy
            if (processedSql.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                processedSql = AddAuditFieldsToInsert(processedSql, auditFieldsConfig, dbDialect, method);
            }
            // UPDATE: Add UpdatedAt, UpdatedBy (including DELETE converted to UPDATE)
            else if (processedSql.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0 || wasDeleteConverted)
            {
                processedSql = AddAuditFieldsToUpdate(processedSql, auditFieldsConfig, dbDialect, method);
            }
        }

        // 🚀 TDD Green: Check for [ConcurrencyCheck]
        var concurrencyColumn = GetConcurrencyCheckColumn(originalEntityType);
        if (concurrencyColumn != null && processedSql.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // ADD optimistic locking: version = version + 1 AND version = @version
            processedSql = AddConcurrencyCheck(processedSql, concurrencyColumn, method);
        }

        SharedCodeGenerationUtilities.GenerateCommandSetup(sb, processedSql, method, connectionName);

        // Add try-catch block
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Call partial method interceptor (用户自定义扩展点，可通过SQLX_ENABLE_PARTIAL_METHODS启用)
        sb.AppendLine("#if SQLX_ENABLE_PARTIAL_METHODS");
        sb.AppendLine("// Partial方法：用户自定义拦截逻辑");
        sb.AppendLine($"OnExecuting(\"{operationName}\", __cmd__);");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // 性能优化：单次分类返回类型，避免重复计算
        var (returnCategory, innerType) = ClassifyReturnType(returnTypeString);
        switch (returnCategory)
        {
            case ReturnTypeCategory.Scalar:
                GenerateScalarExecution(sb, innerType);
                break;
            case ReturnTypeCategory.Collection:
                GenerateCollectionExecution(sb, returnTypeString, entityType, templateResult);
                break;
            case ReturnTypeCategory.SingleEntity:
                GenerateSingleEntityExecution(sb, returnTypeString, entityType, templateResult);
                break;
            case ReturnTypeCategory.DynamicDictionary:
                GenerateDynamicDictionaryExecution(sb, innerType);
                break;
            case ReturnTypeCategory.DynamicDictionaryCollection:
                GenerateDynamicDictionaryCollectionExecution(sb, innerType);
                break;
            default:
                // Non-query execution (INSERT, UPDATE, DELETE)
                sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
                break;
        }

        sb.AppendLine();

        // 生成指标和追踪代码（强制启用）
        sb.AppendLine("#if SQLX_ENABLE_TRACING");
        sb.AppendLine("// 计算执行耗时");
        sb.AppendLine("var __endTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("var __elapsedTicks__ = __endTimestamp__ - __startTimestamp__;");
        sb.AppendLine();
        sb.AppendLine("// 更新Activity（成功）");
        sb.AppendLine("if (__activity__ != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __elapsedMs__ = __elapsedTicks__ * 1000.0 / global::System.Diagnostics.Stopwatch.Frequency;");
        sb.AppendLine("__activity__.SetTag(\"db.duration_ms\", (long)__elapsedMs__);");
        sb.AppendLine("__activity__.SetTag(\"db.success\", true);");
        sb.AppendLine("#if NET5_0_OR_GREATER");
        sb.AppendLine("__activity__.SetStatus(global::System.Diagnostics.ActivityStatusCode.Ok);");
        sb.AppendLine("#endif");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // Call partial method interceptor
        sb.AppendLine("#if SQLX_ENABLE_PARTIAL_METHODS");
        sb.AppendLine("// Partial方法：用户自定义成功处理");
        sb.AppendLine("#if SQLX_ENABLE_TRACING");
        sb.AppendLine($"OnExecuted(\"{operationName}\", __cmd__, __result__, __elapsedTicks__);");
        sb.AppendLine("#else");
        sb.AppendLine($"OnExecuted(\"{operationName}\", __cmd__, __result__, 0);");
        sb.AppendLine("#endif");
        sb.AppendLine("#endif");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (global::System.Exception __ex__)");
        sb.AppendLine("{");
        sb.PushIndent();

        // 生成指标和追踪代码（强制启用）
        sb.AppendLine("#if SQLX_ENABLE_TRACING");
        sb.AppendLine("var __endTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("var __elapsedTicks__ = __endTimestamp__ - __startTimestamp__;");
        sb.AppendLine();
        sb.AppendLine("// 更新Activity（失败）");
        sb.AppendLine("if (__activity__ != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __elapsedMs__ = __elapsedTicks__ * 1000.0 / global::System.Diagnostics.Stopwatch.Frequency;");
        sb.AppendLine("__activity__.SetTag(\"db.duration_ms\", (long)__elapsedMs__);");
        sb.AppendLine("__activity__.SetTag(\"db.success\", false);");
        sb.AppendLine("#if NET5_0_OR_GREATER");
        sb.AppendLine("__activity__.SetStatus(global::System.Diagnostics.ActivityStatusCode.Error, __ex__.Message);");
        sb.AppendLine("#endif");
        // AOT-friendly: Use exception message instead of GetType() which requires reflection
        sb.AppendLine("__activity__.SetTag(\"error.message\", __ex__.Message);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // Call partial method interceptor
        sb.AppendLine("#if SQLX_ENABLE_PARTIAL_METHODS");
        sb.AppendLine("// Partial方法：用户自定义异常处理");
        sb.AppendLine("#if SQLX_ENABLE_TRACING");
        sb.AppendLine($"OnExecuteFail(\"{operationName}\", __cmd__, __ex__, __elapsedTicks__);");
        sb.AppendLine("#else");
        sb.AppendLine($"OnExecuteFail(\"{operationName}\", __cmd__, __ex__, 0);");
        sb.AppendLine("#endif");
        sb.AppendLine("#endif");
        sb.AppendLine();

        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("finally");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// 🚀 性能关键：及时释放Command资源（减少2-3μs开销）");
        sb.AppendLine("__cmd__?.Dispose();");
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
        DynamicDictionary,          // Dictionary<string, object>
        DynamicDictionaryCollection, // List<Dictionary<string, object>>
        Unknown
    }

    // 性能优化：单次调用ExtractInnerTypeFromTask，避免重复计算
    private (ReturnTypeCategory Category, string InnerType) ClassifyReturnType(string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);

        // 检查动态字典集合类型：List<Dictionary<string, object>>
        if (IsDynamicDictionaryCollection(innerType))
            return (ReturnTypeCategory.DynamicDictionaryCollection, innerType);

        // 检查动态字典类型：Dictionary<string, object>
        if (IsDynamicDictionary(innerType))
            return (ReturnTypeCategory.DynamicDictionary, innerType);

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

    /// <summary>
    /// 检查是否为动态字典类型：Dictionary&lt;string, object&gt;
    /// </summary>
    private bool IsDynamicDictionary(string type)
    {
        // 支持多种格式：
        // - Dictionary<string, object>
        // - System.Collections.Generic.Dictionary<string, object>
        // - global::System.Collections.Generic.Dictionary<string, object>
        return type.Contains("Dictionary<string, object>") ||
               type.Contains("Dictionary<System.String, System.Object>");
    }

    /// <summary>
    /// 检查是否为动态字典集合类型：List&lt;Dictionary&lt;string, object&gt;&gt;
    /// </summary>
    private bool IsDynamicDictionaryCollection(string type)
    {
        // 支持多种格式：
        // - List<Dictionary<string, object>>
        // - System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>
        return (type.Contains("List<") && type.Contains("Dictionary<string, object>")) ||
               (type.Contains("List<") && type.Contains("Dictionary<System.String, System.Object>"));
    }

    private bool IsScalarReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.Scalar;
    private bool IsCollectionReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.Collection;
    private bool IsSingleEntityReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.SingleEntity;

    private void GenerateScalarExecution(IndentedStringBuilder sb, string innerType)
    {
        sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");

        // Handle numeric type conversions (e.g., SQLite COUNT returns Int64 but method expects Int32)
        if (innerType == "int" || innerType == "System.Int32")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToInt32(scalarResult) : default(int);");
        }
        else if (innerType == "long" || innerType == "System.Int64")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToInt64(scalarResult) : default(long);");
        }
        else if (innerType == "decimal" || innerType == "System.Decimal")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToDecimal(scalarResult) : default(decimal);");
        }
        else if (innerType == "double" || innerType == "System.Double")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToDouble(scalarResult) : default(double);");
        }
        else if (innerType == "bool" || innerType == "System.Boolean")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToBoolean(scalarResult) : default(bool);");
        }
        else
        {
            // Direct cast for other types
            sb.AppendLine($"__result__ = scalarResult != null ? ({innerType})scalarResult : default({innerType});");
        }
    }

    private void GenerateCollectionExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType, SqlTemplateResult templateResult)
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
            GenerateEntityFromReader(sb, entityType, "item", templateResult);
            sb.AppendLine($"(({collectionType})__result__).Add(item);");
        }

        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateSingleEntityExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType, SqlTemplateResult templateResult)
    {
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("if (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityFromReader(sb, entityType, "__result__", templateResult);
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

    /// <summary>
    /// 生成动态字典集合的执行代码：List&lt;Dictionary&lt;string, object&gt;&gt;
    /// 适用于运行时列不确定的查询（如报表、动态查询）
    /// </summary>
    private void GenerateDynamicDictionaryCollectionExecution(IndentedStringBuilder sb, string returnType)
    {
        // 确保使用全局命名空间前缀
        var collectionType = returnType.StartsWith("System.") ? $"global::{returnType}" : returnType;

        sb.AppendLine($"__result__ = new {collectionType}();");
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine();
        sb.AppendLine("// 🚀 性能优化：预读取列名，避免每行重复调用GetName()");
        sb.AppendLine("var fieldCount = reader.FieldCount;");
        sb.AppendLine("var columnNames = new string[fieldCount];");
        sb.AppendLine("for (var i = 0; i < fieldCount; i++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("columnNames[i] = reader.GetName(i);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("while (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// 🚀 性能优化：预分配容量");
        sb.AppendLine("var dict = new global::System.Collections.Generic.Dictionary<string, object>(fieldCount);");
        sb.AppendLine("for (var i = 0; i < fieldCount; i++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// 🛡️ 安全处理DBNull");
        sb.AppendLine("dict[columnNames[i]] = reader.IsDBNull(i) ? null! : reader.GetValue(i);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine($"(({collectionType})__result__).Add(dict);");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// 生成单行动态字典的执行代码：Dictionary&lt;string, object&gt;?
    /// 适用于返回单行动态结果的查询
    /// </summary>
    private void GenerateDynamicDictionaryExecution(IndentedStringBuilder sb, string returnType)
    {
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("if (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// 🚀 性能优化：预分配容量");
        sb.AppendLine("var fieldCount = reader.FieldCount;");
        sb.AppendLine("__result__ = new global::System.Collections.Generic.Dictionary<string, object>(fieldCount);");
        sb.AppendLine("for (var i = 0; i < fieldCount; i++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var columnName = reader.GetName(i);");
        sb.AppendLine("// 🛡️ 安全处理DBNull");
        sb.AppendLine("__result__[columnName] = reader.IsDBNull(i) ? null! : reader.GetValue(i);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("else");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__result__ = null;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateEntityFromReader(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName, SqlTemplateResult templateResult)
    {
        // 🚀 性能优化：默认使用硬编码索引访问（极致性能）
        // 源分析器会检测列顺序不匹配并发出警告
        SharedCodeGenerationUtilities.GenerateEntityMapping(sb, entityType, variableName, templateResult.ColumnOrder);
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
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute" ||
                                   (attr.AttributeClass?.OriginalDefinition != null &&
                                    attr.AttributeClass.OriginalDefinition.Name == "RepositoryForAttribute"));

    /// <summary>Get formatted default value string for parameter</summary>
    private static string GetDefaultValueString(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return string.Empty;

        var value = parameter.ExplicitDefaultValue;

        // Handle null
        if (value == null)
            return "default";

        // Handle strings
        if (value is string str)
            return $"\"{str}\"";

        // Handle booleans
        if (value is bool b)
            return b ? "true" : "false";

        // Handle numbers and other primitives
        return value.ToString() ?? "default";
    }

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

    /// <summary>
    /// 生成动态占位符验证代码（内联到生成的方法中）
    /// </summary>
    /// <param name="sb">代码字符串构建器</param>
    /// <param name="method">方法符号</param>
    private void GenerateDynamicPlaceholderValidation(IndentedStringBuilder sb, IMethodSymbol method)
    {
        sb.AppendLine("// 🔐 动态占位符验证（编译时生成，运行时零反射开销）");
        sb.AppendLine();

        foreach (var parameter in method.Parameters)
        {
            // 检查参数是否有 [DynamicSql] 特性
            var dynamicSqlAttr = parameter.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "DynamicSqlAttribute");

            if (dynamicSqlAttr == null)
                continue;

            // 参数必须是 string 类型
            if (parameter.Type.SpecialType != SpecialType.System_String)
            {
                // 这应该在分析器阶段就报错，这里作为防御性编程
                continue;
            }

            // 获取 DynamicSqlType 类型（默认为 Identifier = 0）
            var dynamicSqlType = 0; // DynamicSqlType.Identifier
            if (dynamicSqlAttr.NamedArguments.Length > 0)
            {
                var typeArg = dynamicSqlAttr.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "Type");
                if (typeArg.Value.Value is int typeValue)
                {
                    dynamicSqlType = typeValue;
                }
            }

            var paramName = parameter.Name;

            // 根据 DynamicSqlType 生成不同的验证代码


            switch (dynamicSqlType)
            {
                case 0: // Identifier
                    sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidIdentifier({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid identifier: {{{paramName}}}. Only letters, digits, and underscores are allowed.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    break;

                case 1: // Fragment
                    sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidFragment({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid SQL fragment: {{{paramName}}}. Contains dangerous keywords or operations.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    break;

                case 2: // TablePart
                    sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidTablePart({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid table part: {{{paramName}}}. Only letters and digits are allowed.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    break;
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Gets all methods from an interface including methods from base interfaces.
    /// Supports interface inheritance like ICrudRepository.
    /// </summary>
    private IEnumerable<IMethodSymbol> GetAllInterfaceMethods(INamedTypeSymbol interfaceSymbol)
    {
        // Get methods directly defined in this interface
        var directMethods = interfaceSymbol.GetMembers().OfType<IMethodSymbol>();

        // Get methods from all base interfaces
        var baseMethods = interfaceSymbol.AllInterfaces
            .SelectMany(baseInterface => baseInterface.GetMembers().OfType<IMethodSymbol>());

        // Combine and deduplicate (in case of method overrides)
        return directMethods.Concat(baseMethods)
            .GroupBy(m => m.Name + "_" + string.Join("_", m.Parameters.Select(p => p.Type.ToDisplayString())))
            .Select(g => g.First());
    }

    /// <summary>
    /// Gets the table name from TableNameAttribute or infers it from entity type.
    /// Checks both repository class and entity type for TableNameAttribute.
    /// </summary>
    private string GetTableNameFromType(INamedTypeSymbol repositoryClass, INamedTypeSymbol? entityType)
    {
        // First, check if repository class has TableNameAttribute
        var repositoryTableNameAttr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute" || attr.AttributeClass?.Name == "TableName");

        if (repositoryTableNameAttr != null && repositoryTableNameAttr.ConstructorArguments.Length > 0)
        {
            var tableName = repositoryTableNameAttr.ConstructorArguments[0].Value?.ToString();
            if (!string.IsNullOrEmpty(tableName))
                return tableName;
        }

        // Second, check if entity type has TableNameAttribute
        if (entityType != null)
        {
            var entityTableNameAttr = entityType.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute" || attr.AttributeClass?.Name == "TableName");

            if (entityTableNameAttr != null && entityTableNameAttr.ConstructorArguments.Length > 0)
            {
                var tableName = entityTableNameAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(tableName))
                    return tableName;
            }
        }

        // Fallback: infer from entity type name (convert to lowercase, keep plural if present)
        if (entityType != null)
        {
            return entityType.Name.ToLowerInvariant();
        }

        // Last resort: use repository class name
        return repositoryClass.Name.Replace("Repository", "").ToLowerInvariant();
    }

    /// <summary>
    /// Gets the database dialect from SqlDefineAttribute on the class.
    /// </summary>
    private static string GetDatabaseDialect(INamedTypeSymbol classSymbol)
    {
        var sqlDefineAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SqlDefineAttribute");

        if (sqlDefineAttr != null && sqlDefineAttr.ConstructorArguments.Length > 0)
        {
            var dialectValue = sqlDefineAttr.ConstructorArguments[0].Value;
            if (dialectValue != null)
            {
                // SqlDefineTypes is an enum, get its string representation
                return dialectValue.ToString();
            }
        }

        // Default to SqlServer if not specified
        return "SqlServer";
    }

    /// <summary>
    /// Adds RETURNING/OUTPUT clause to INSERT statement based on database dialect.
    /// TDD: This method makes the green tests pass by adding the appropriate syntax.
    /// </summary>
    /// <param name="sql">The original SQL INSERT statement</param>
    /// <param name="dialect">Database dialect (enum value or name)</param>
    /// <param name="returnAll">If true, returns all columns (*); if false, returns only id</param>
    private static string AddReturningClauseForInsert(string sql, string dialect, bool returnAll = false)
    {
        // Dialect can be either enum value (0, 1, 2...) or string name
        // SqlDefineTypes enum: MySql=0, SqlServer=1, PostgreSql=2, SQLite=3, Oracle=4

        var returningClause = returnAll ? "*" : "id";

        // PostgreSQL (2) and SQLite (3): ADD RETURNING clause at the end
        if (dialect == "PostgreSql" || dialect == "2" || dialect == "SQLite" || dialect == "3")
        {
            return sql + $" RETURNING {returningClause}";
        }

        // SQL Server (1): INSERT OUTPUT INSERTED.* VALUES ...
        if (dialect == "SqlServer" || dialect == "1")
        {
            var outputClause = returnAll ? "OUTPUT INSERTED.*" : "OUTPUT INSERTED.id";
            // Find the position of VALUES keyword
            var valuesIndex = sql.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
            if (valuesIndex > 0)
            {
                // Insert OUTPUT INSERTED.* before VALUES
                return sql.Insert(valuesIndex, outputClause + " ");
            }
            // Fallback: add at the end
            return sql + " " + outputClause;
        }

        // MySQL (0): For MySQL, we'll need a different approach (LAST_INSERT_ID())
        // For now, just return the original SQL
        // The execution logic will handle getting the ID separately
        if (dialect == "MySql" || dialect == "0")
        {
            // MySQL requires SELECT LAST_INSERT_ID() after INSERT
            // We'll handle this in execution logic
            return sql;
        }

        // Oracle (4): RETURNING id INTO :out_id
        if (dialect == "Oracle" || dialect == "4")
        {
            if (returnAll)
            {
                // Oracle doesn't support RETURNING * INTO easily
                // We'll need to list all columns explicitly
                return sql + " RETURNING * INTO :out_entity"; // Simplified for now
            }
            return sql + " RETURNING id INTO :out_id";
        }

        // Default: return unchanged
        return sql;
    }

    /// <summary>
    /// Detects soft delete configuration from [SoftDelete] attribute on entity type.
    /// </summary>
    private static SoftDeleteConfig? GetSoftDeleteConfig(INamedTypeSymbol? entityType)
    {
        if (entityType == null) return null;

        var attr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SoftDeleteAttribute" ||
                                a.AttributeClass?.Name == "SoftDelete");

        if (attr == null) return null;

        var flagColumn = "IsDeleted";
        string? timestampColumn = null;
        string? deletedByColumn = null;

        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "FlagColumn" && namedArg.Value.Value != null)
            {
                flagColumn = namedArg.Value.Value.ToString() ?? "IsDeleted";
            }
            else if (namedArg.Key == "TimestampColumn" && namedArg.Value.Value != null)
            {
                timestampColumn = namedArg.Value.Value.ToString();
            }
            else if (namedArg.Key == "DeletedByColumn" && namedArg.Value.Value != null)
            {
                deletedByColumn = namedArg.Value.Value.ToString();
            }
        }

        return new SoftDeleteConfig
        {
            FlagColumn = flagColumn,
            TimestampColumn = timestampColumn,
            DeletedByColumn = deletedByColumn
        };
    }

    /// <summary>
    /// Converts DELETE statement to UPDATE for soft delete.
    /// </summary>
    private static string ConvertDeleteToSoftDelete(string sql, SoftDeleteConfig config, string dialect, string tableName)
    {
        if (sql.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return sql;
        }

        var flagColumn = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.FlagColumn);
        var setClause = $"{flagColumn} = true";

        // Add timestamp if configured
        if (!string.IsNullOrEmpty(config.TimestampColumn))
        {
            var timestampColumn = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.TimestampColumn);
            var timestampSql = GetCurrentTimestampSql(dialect);
            setClause += $", {timestampColumn} = {timestampSql}";
        }

        // Extract WHERE clause from DELETE statement
        // Pattern: DELETE FROM table WHERE condition
        var deleteFromIndex = sql.IndexOf("DELETE FROM", StringComparison.OrdinalIgnoreCase);
        if (deleteFromIndex < 0)
        {
            deleteFromIndex = sql.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase);
        }

        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        string whereClause = "";

        if (whereIndex > deleteFromIndex)
        {
            whereClause = sql.Substring(whereIndex);
        }
        else
        {
            // No WHERE clause, add default (this is dangerous but we'll allow it)
            whereClause = "WHERE 1=1";
        }

        // Convert to UPDATE
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        return $"UPDATE {snakeTableName} SET {setClause} {whereClause}";
    }

    /// <summary>
    /// Gets the current timestamp SQL for different database dialects.
    /// </summary>
    private static string GetCurrentTimestampSql(string dialect)
    {
        // Handle both enum value and string name
        return dialect switch
        {
            "PostgreSql" or "2" => "NOW()",
            "SqlServer" or "1" => "GETDATE()",
            "MySql" or "0" => "NOW()",
            "SQLite" or "3" => "datetime('now')",
            "Oracle" or "4" => "SYSDATE",
            _ => "CURRENT_TIMESTAMP"
        };
    }

    /// <summary>
    /// Configuration for soft delete feature.
    /// </summary>
    private class SoftDeleteConfig
    {
        public string FlagColumn { get; set; } = "IsDeleted";
        public string? TimestampColumn { get; set; }
        public string? DeletedByColumn { get; set; }
    }

    /// <summary>
    /// Detects audit fields configuration from [AuditFields] attribute on entity type.
    /// </summary>
    private static AuditFieldsConfig? GetAuditFieldsConfig(INamedTypeSymbol? entityType)
    {
        if (entityType == null) return null;

        var attr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "AuditFieldsAttribute" ||
                                a.AttributeClass?.Name == "AuditFields");

        if (attr == null) return null;

        var createdAtColumn = "CreatedAt";
        string? createdByColumn = null;
        var updatedAtColumn = "UpdatedAt";
        string? updatedByColumn = null;

        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "CreatedAtColumn" && namedArg.Value.Value != null)
            {
                createdAtColumn = namedArg.Value.Value.ToString() ?? "CreatedAt";
            }
            else if (namedArg.Key == "CreatedByColumn" && namedArg.Value.Value != null)
            {
                createdByColumn = namedArg.Value.Value.ToString();
            }
            else if (namedArg.Key == "UpdatedAtColumn" && namedArg.Value.Value != null)
            {
                updatedAtColumn = namedArg.Value.Value.ToString() ?? "UpdatedAt";
            }
            else if (namedArg.Key == "UpdatedByColumn" && namedArg.Value.Value != null)
            {
                updatedByColumn = namedArg.Value.Value.ToString();
            }
        }

        return new AuditFieldsConfig
        {
            CreatedAtColumn = createdAtColumn,
            CreatedByColumn = createdByColumn,
            UpdatedAtColumn = updatedAtColumn,
            UpdatedByColumn = updatedByColumn
        };
    }

    /// <summary>
    /// Adds audit fields (CreatedAt, CreatedBy) to INSERT statement.
    /// </summary>
    private static string AddAuditFieldsToInsert(string sql, AuditFieldsConfig config, string dialect, IMethodSymbol method)
    {
        var createdAtCol = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.CreatedAtColumn);
        var timestampSql = GetCurrentTimestampSql(dialect);

        // 简单实现：在VALUES子句末尾添加审计字段
        // INSERT INTO table (col1, col2) VALUES (val1, val2)
        // 变为: INSERT INTO table (col1, col2, created_at, created_by) VALUES (val1, val2, NOW(), @createdBy)

        var additionalColumns = new System.Collections.Generic.List<string> { createdAtCol };
        var additionalValues = new System.Collections.Generic.List<string> { timestampSql };

        // Check if method has createdBy parameter
        if (!string.IsNullOrEmpty(config.CreatedByColumn))
        {
            var createdByParam = method.Parameters.FirstOrDefault(p =>
                p.Name.Equals("createdBy", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("created_by", StringComparison.OrdinalIgnoreCase));

            if (createdByParam != null)
            {
                var createdByCol = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.CreatedByColumn);
                additionalColumns.Add(createdByCol);
                additionalValues.Add("@" + createdByParam.Name);
            }
        }

        // Find INSERT INTO ... (columns) VALUES (values)
        var insertIntoIndex = sql.IndexOf("INSERT INTO", StringComparison.OrdinalIgnoreCase);
        if (insertIntoIndex < 0) return sql;

        var valuesIndex = sql.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
        if (valuesIndex < 0) return sql;

        // Find the closing parenthesis of columns list
        var columnsEndIndex = sql.LastIndexOf(')', valuesIndex);
        var valuesStartIndex = sql.IndexOf('(', valuesIndex);
        var valuesEndIndex = sql.LastIndexOf(')');

        if (columnsEndIndex > 0 && valuesStartIndex > 0 && valuesEndIndex > valuesStartIndex)
        {
            // Add columns
            var newSql = sql.Substring(0, columnsEndIndex);
            newSql += ", " + string.Join(", ", additionalColumns);
            newSql += sql.Substring(columnsEndIndex, valuesEndIndex - columnsEndIndex);
            newSql += ", " + string.Join(", ", additionalValues);
            newSql += sql.Substring(valuesEndIndex);
            return newSql;
        }

        return sql;
    }

    /// <summary>
    /// Adds audit fields (UpdatedAt, UpdatedBy) to UPDATE statement.
    /// </summary>
    private static string AddAuditFieldsToUpdate(string sql, AuditFieldsConfig config, string dialect, IMethodSymbol method)
    {
        var updatedAtCol = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.UpdatedAtColumn);
        var timestampSql = GetCurrentTimestampSql(dialect);

        // UPDATE table SET col1 = val1 WHERE ...
        // 变为: UPDATE table SET col1 = val1, updated_at = NOW(), updated_by = @updatedBy WHERE ...

        var additionalSets = new System.Collections.Generic.List<string> { $"{updatedAtCol} = {timestampSql}" };

        // Check if method has updatedBy parameter
        if (!string.IsNullOrEmpty(config.UpdatedByColumn))
        {
            var updatedByParam = method.Parameters.FirstOrDefault(p =>
                p.Name.Equals("updatedBy", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("updated_by", StringComparison.OrdinalIgnoreCase));

            if (updatedByParam != null)
            {
                var updatedByCol = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.UpdatedByColumn);
                additionalSets.Add($"{updatedByCol} = @{updatedByParam.Name}");
            }
        }

        // Find WHERE clause
        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        if (whereIndex > 0)
        {
            // Insert before WHERE
            var beforeWhere = sql.Substring(0, whereIndex).TrimEnd();
            var afterWhere = sql.Substring(whereIndex);
            return $"{beforeWhere}, {string.Join(", ", additionalSets)} {afterWhere}";
        }
        else
        {
            // No WHERE clause, append at end
            return sql.TrimEnd() + ", " + string.Join(", ", additionalSets);
        }
    }

    /// <summary>
    /// Configuration for audit fields feature.
    /// </summary>
    private class AuditFieldsConfig
    {
        public string CreatedAtColumn { get; set; } = "CreatedAt";
        public string? CreatedByColumn { get; set; }
        public string UpdatedAtColumn { get; set; } = "UpdatedAt";
        public string? UpdatedByColumn { get; set; }
    }

    /// <summary>
    /// Detects concurrency check column from [ConcurrencyCheck] attribute on entity properties.
    /// </summary>
    private static string? GetConcurrencyCheckColumn(INamedTypeSymbol? entityType)
    {
        if (entityType == null) return null;

        // 遍历实体的所有属性，找到标记[ConcurrencyCheck]的属性
        foreach (var member in entityType.GetMembers())
        {
            if (member is IPropertySymbol property)
            {
                var hasConcurrencyCheck = property.GetAttributes()
                    .Any(a => a.AttributeClass?.Name == "ConcurrencyCheckAttribute" ||
                             a.AttributeClass?.Name == "ConcurrencyCheck");

                if (hasConcurrencyCheck)
                {
                    return property.Name;  // 返回属性名，如"Version"
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Adds concurrency check to UPDATE statement:
    /// 1. SET clause: version = version + 1
    /// 2. WHERE clause: AND version = @version
    /// </summary>
    private static string AddConcurrencyCheck(string sql, string versionColumn, IMethodSymbol method)
    {
        var versionCol = SharedCodeGenerationUtilities.ConvertToSnakeCase(versionColumn);
        var versionParam = "@" + versionColumn.ToLower();

        // 找到WHERE子句的位置
        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);

        if (whereIndex > 0)
        {
            // 有WHERE子句：在SET末尾添加version递增，在WHERE末尾添加version检查
            var beforeWhere = sql.Substring(0, whereIndex).TrimEnd();
            var afterWhere = sql.Substring(whereIndex);

            // 添加version递增到SET子句
            var newSql = $"{beforeWhere}, {versionCol} = {versionCol} + 1 {afterWhere}";

            // 在WHERE子句末尾添加version检查
            newSql = newSql + $" AND {versionCol} = {versionParam}";

            return newSql;
        }
        else
        {
            // 无WHERE子句：创建WHERE version = @version，并在SET末尾添加version递增
            var newSql = sql.TrimEnd();
            newSql = $"{newSql}, {versionCol} = {versionCol} + 1 WHERE {versionCol} = {versionParam}";

            return newSql;
        }
    }

    /// <summary>
    /// TDD Phase 3: Generate batch INSERT code with auto-chunking
    /// </summary>
    private static void GenerateBatchInsertCode(
        IndentedStringBuilder sb,
        string sql,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string connectionName)
    {
        // Extract parameter name from __RUNTIME_BATCH_VALUES_paramName__
        var marker = "__RUNTIME_BATCH_VALUES_";
        var startIndex = sql.IndexOf(marker);
        if (startIndex < 0) return;

        var endIndex = sql.IndexOf("__", startIndex + marker.Length);
        var paramName = sql.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length);

        var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
        if (param == null) return;

        // Infer entity type from IEnumerable<T> parameter if not provided
        if (entityType == null)
        {
            var paramType = param.Type as INamedTypeSymbol;
            if (paramType != null && paramType.TypeArguments.Length > 0)
            {
                entityType = paramType.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        if (entityType == null) return; // Still null after inference, cannot proceed

        // Get MaxBatchSize from [BatchOperation] attribute
        var batchOpAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "BatchOperationAttribute");

        int maxBatchSize = 1000; // Default
        if (batchOpAttr != null)
        {
            var maxBatchSizeArg = batchOpAttr.NamedArguments.FirstOrDefault(a => a.Key == "MaxBatchSize");
            if (maxBatchSizeArg.Value.Value != null)
            {
                maxBatchSize = (int)maxBatchSizeArg.Value.Value;
            }
        }

        // Get properties to insert (excluding Id if specified)
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
            .ToList();

        // Check if columns exclude Id from SQL template
        var excludeId = sql.Contains("--exclude Id") || sql.Contains("--exclude id");
        if (excludeId)
        {
            properties = properties.Where(p => p.Name != "Id").ToList();
        }

        // Remove --exclude from SQL
        var baseSql = sql.Replace("--exclude Id", "").Replace("--exclude id", "").Trim();

        // Generate code
        sb.AppendLine($"int __totalAffected__ = 0;");
        sb.AppendLine();

        // Check for empty collection
        sb.AppendLine($"if ({paramName} == null || !{paramName}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("return global::System.Threading.Tasks.Task.FromResult(0);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Chunk batches
        if (batchOpAttr != null)
        {
            sb.AppendLine($"var __batches__ = {paramName}.Chunk({maxBatchSize});");
        }
        else
        {
            sb.AppendLine($"var __batches__ = new[] {{ {paramName} }};");
        }
        sb.AppendLine();

        sb.AppendLine("foreach (var __batch__ in __batches__)");
        sb.AppendLine("{");
        sb.PushIndent();

        // Create command
        sb.AppendLine($"var __cmd__ = {connectionName}.CreateCommand();");
        sb.AppendLine();

        // Build VALUES clause
        sb.AppendLine("var __valuesClauses__ = new global::System.Collections.Generic.List<string>();");
        sb.AppendLine("int __itemIndex__ = 0;");
        sb.AppendLine("foreach (var __item__ in __batch__)");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate (@param0_0, @param0_1, ...) for each item
        var paramPlaceholders = properties.Select(p =>
        {
            var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
            return $"@{snakeName}{{__itemIndex__}}";
        });
        var valuesClause = string.Join(", ", paramPlaceholders);

        sb.AppendLine($"__valuesClauses__.Add($\"({valuesClause})\");");
        sb.AppendLine("__itemIndex__++;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("var __values__ = string.Join(\", \", __valuesClauses__);");
        sb.AppendLine();

        // Replace marker in SQL
        sb.AppendLine($"var __sql__ = @\"{baseSql}\";");
        sb.AppendLine($"__sql__ = __sql__.Replace(\"{marker}{paramName}__\", __values__);");
        sb.AppendLine("__cmd__.CommandText = __sql__;");
        sb.AppendLine();

        // Bind parameters
        sb.AppendLine("__itemIndex__ = 0;");
        sb.AppendLine("foreach (var __item__ in __batch__)");
        sb.AppendLine("{");
        sb.PushIndent();

        foreach (var prop in properties)
        {
            var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
            sb.AppendLine($"__p__.ParameterName = $\"@{snakeName}{{__itemIndex__}}\";");

            // Handle nullable
            if (prop.Type.IsReferenceType || prop.NullableAnnotation == NullableAnnotation.Annotated)
            {
                sb.AppendLine($"__p__.Value = __item__.{prop.Name} ?? (object)global::System.DBNull.Value;");
            }
            else
            {
                sb.AppendLine($"__p__.Value = __item__.{prop.Name};");
            }

            sb.AppendLine("__cmd__.Parameters.Add(__p__);");
            sb.PopIndent();
            sb.AppendLine("}");
        }

        sb.AppendLine("__itemIndex__++;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute
        sb.AppendLine("__totalAffected__ += __cmd__.ExecuteNonQuery();");
        sb.AppendLine("__cmd__.Dispose();");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Return result
        sb.AppendLine("return global::System.Threading.Tasks.Task.FromResult(__totalAffected__);");
    }

}
