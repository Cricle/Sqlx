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
    private static readonly Core.TemplateInheritanceResolver TemplateResolver = new();

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

            // Extract table name from RepositoryFor attribute (if available)
            var resolvedTableName = context.TableName;
            if (context.ClassSymbol != null)
            {
                resolvedTableName = Core.DialectHelper.GetTableNameFromRepositoryFor(context.ClassSymbol, entityType) ?? context.TableName;
            }

            // If no template on method, try to resolve from inherited interfaces
            if (sqlTemplate == null && method.ContainingType is INamedTypeSymbol containingType)
            {
                // Extract dialect from RepositoryFor attribute
                var dialect = Core.DialectHelper.GetDialectFromRepositoryFor(context.ClassSymbol);
                var dialectProvider = Core.DialectHelper.GetDialectProvider(dialect);

                // Try to resolve inherited templates for this interface
                var inheritedTemplates = TemplateResolver.ResolveInheritedTemplates(
                    containingType,
                    dialectProvider,
                    resolvedTableName,
                    entityType);

                // Find matching template for this method
                var matchingTemplate = inheritedTemplates.FirstOrDefault(t =>
                    t.Method.Name == method.Name &&
                    t.Method.Parameters.Length == method.Parameters.Length);

                if (matchingTemplate != null)
                {
                    sqlTemplate = matchingTemplate.ProcessedSql;
                }
            }

            // Process SQL template if available (use resolvedTableName and correct dialect)
            SqlTemplateResult? templateResult = null;
            if (sqlTemplate != null)
            {
                // Get the correct dialect for this repository
                var dialectType = Core.DialectHelper.GetDialectFromRepositoryFor(context.ClassSymbol);
                var dialectProvider = Core.DialectHelper.GetDialectProvider(dialectType);
                var sqlDefine = dialectProvider.SqlDefine;

                templateResult = TemplateEngine.ProcessTemplate(sqlTemplate, method, entityType, resolvedTableName, sqlDefine);
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

            // Add async modifier for Task-based methods
            var asyncModifier = returnType.Contains("Task") ? "async " : "";
            sb.AppendLine($"public {asyncModifier}{returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            bool methodBodyComplete = false;

            if (templateResult != null)
            {
                var connectionName = GetDbConnectionFieldName(context.ClassSymbol);
                methodBodyComplete = GenerateActualDatabaseExecution(sb, method, templateResult, entityType, connectionName, context.ClassSymbol);
            }
            else if (sqlxAttr != null)
            {
                throw new InvalidOperationException($"Failed to generate implementation for method '{method.Name}' with SQL attribute. Please check the SQL template syntax and parameters.");
            }
            else
            {
                GenerateFallbackMethodImplementation(sb, method);
            }

            // Only add return statement and close method if body is not already complete
            if (!methodBodyComplete)
            {
                // Return result if not void
                if (!method.ReturnsVoid)
                {
                    // Check if the return type is Task or Task<T>
                    var methodReturnType = method.ReturnType;
                    var isTaskReturn = methodReturnType.Name == "Task" &&
                                       methodReturnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

                    if (isTaskReturn)
                    {
                        // For async methods, return directly (the async keyword handles Task wrapping)
                        sb.AppendLine("return __result__;");
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
            }
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
        sb.AppendLine("using Sqlx;");  // For ExpressionExtensions
        sb.AppendLine("using Sqlx.Annotations;");
        sb.AppendLine();

        // Generate partial class
        sb.AppendLine($"partial class {repositoryClass.Name}");
        sb.AppendLine("{");
        sb.PushIndent();

        // 🔧 Transaction支持：添加Repository级别的Transaction属性
        // 用户可以通过设置此属性让所有Repository操作参与同一个事务
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Gets or sets the transaction to use for all database operations.");
        sb.AppendLine("/// When set, all generated methods will use this transaction.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public global::System.Data.Common.DbTransaction? Transaction { get; set; }");
        sb.AppendLine();

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

            // Track whether SQL came from inherited template (already processed)
            bool sqlAlreadyProcessed = false;

            // If no template on method, try to resolve from inherited interfaces
            if (sql == null && method.ContainingType is INamedTypeSymbol containingType)
            {
                // Extract dialect and table name from RepositoryFor attribute
                var dialect = Core.DialectHelper.GetDialectFromRepositoryFor(repositoryClass);
                var tblName = Core.DialectHelper.GetTableNameFromRepositoryFor(repositoryClass, entityType) ?? tableName;
                var dialectProvider = Core.DialectHelper.GetDialectProvider(dialect);

                // Try to resolve inherited templates for this interface
                var inheritedTemplates = TemplateResolver.ResolveInheritedTemplates(
                    containingType,
                    dialectProvider,
                    tblName,
                    entityType);

                // Find matching template for this method
                var matchingTemplate = inheritedTemplates.FirstOrDefault(t =>
                    t.Method.Name == method.Name &&
                    t.Method.Parameters.Length == method.Parameters.Length);

                if (matchingTemplate != null)
                {
                    sql = matchingTemplate.ProcessedSql;
                    sqlAlreadyProcessed = true; // Mark that dialect placeholders are already replaced
                }
            }

            if (sql != null)
            {
                string processedSql;

                if (sqlAlreadyProcessed)
                {
                    // SQL came from inherited template - dialect placeholders already replaced
                    // Still need to process for other placeholders ({{values}}, {{where}}, etc.)
                    var templateEngine = context.TemplateEngine;
                    var dialectType = Core.DialectHelper.GetDialectFromRepositoryFor(context.RepositoryClass);
                    var dialectProvider = Core.DialectHelper.GetDialectProvider(dialectType);
                    var sqlDefine = dialectProvider.SqlDefine;

                    var templateResult = templateEngine.ProcessTemplate(sql, method, entityType, tableName, sqlDefine);
                    processedSql = templateResult.ProcessedSql;
                }
                else
                {
                    // SQL has original template - needs full processing
                    var templateEngine = context.TemplateEngine;
                    var dialectType = Core.DialectHelper.GetDialectFromRepositoryFor(context.RepositoryClass);
                    var dialectProvider = Core.DialectHelper.GetDialectProvider(dialectType);
                    var sqlDefine = dialectProvider.SqlDefine;

                    var templateResult = templateEngine.ProcessTemplate(sql, method, entityType, tableName, sqlDefine);
                    processedSql = templateResult.ProcessedSql;
                }

                var methodContext = new RepositoryMethodContext(
                    sb, method, entityType, tableName, processedSql,
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
        sb.AppendLine("partial void OnExecuting(string operationName, global::System.Data.Common.DbCommand command);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called after successfully executing a repository operation.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"operationName\">The name of the operation that was executed.</param>");
        sb.AppendLine("/// <param name=\"command\">The database command that was executed.</param>");
        sb.AppendLine("/// <param name=\"result\">The result of the operation.</param>");
        sb.AppendLine("/// <param name=\"elapsedTicks\">The elapsed time in ticks.</param>");
        sb.AppendLine("partial void OnExecuted(string operationName, global::System.Data.Common.DbCommand command, object? result, long elapsedTicks);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called when a repository operation fails with an exception.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"operationName\">The name of the operation that failed.</param>");
        sb.AppendLine("/// <param name=\"command\">The database command that failed.</param>");
        sb.AppendLine("/// <param name=\"exception\">The exception that occurred.</param>");
        sb.AppendLine("/// <param name=\"elapsedTicks\">The elapsed time in ticks before failure.</param>");
        sb.AppendLine("partial void OnExecuteFail(string operationName, global::System.Data.Common.DbCommand command, global::System.Exception exception, long elapsedTicks);");
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

        // Add async modifier for Task-based methods
        var asyncModifier = returnType.Contains("Task") ? "async " : "";
        sb.AppendLine($"public {asyncModifier}{returnType} {method.Name}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        if (!method.ReturnsVoid)
        {
            // For async methods, extract the inner type from Task<T>
            var defaultType = returnType;
            if (asyncModifier != "" && returnType.StartsWith("System.Threading.Tasks.Task<") && returnType.EndsWith(">"))
            {
                // Extract inner type from Task<T>
                defaultType = returnType.Substring("System.Threading.Tasks.Task<".Length,
                                                   returnType.Length - "System.Threading.Tasks.Task<".Length - 1);
            }
            sb.AppendLine($"return default({defaultType});");
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

    private bool GenerateActualDatabaseExecution(IndentedStringBuilder sb, IMethodSymbol method, SqlTemplateResult templateResult, INamedTypeSymbol? entityType, string connectionName, INamedTypeSymbol classSymbol)
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
        sb.AppendLine("global::System.Data.Common.DbCommand? __cmd__ = null;");
        sb.AppendLine();

        // 🚀 检查是否有CancellationToken参数
        var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        var hasCancellationToken = cancellationTokenParam != null;
        var cancellationTokenArg = hasCancellationToken ? $", {cancellationTokenParam!.Name}" : "";

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
            // Generate batch INSERT code (complete execution flow, including method closure)
            GenerateBatchInsertCode(sb, processedSql, method, originalEntityType, connectionName, classSymbol, cancellationTokenArg);
            return true; // Batch INSERT handles everything including method closure
        }

        // 🚀 TDD Green: Check for [ReturnInsertedId] or [ReturnInsertedEntity] and modify SQL
        var hasReturnInsertedId = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedIdAttribute" || a.AttributeClass?.Name == "ReturnInsertedId");
        var hasReturnInsertedEntity = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedEntityAttribute" || a.AttributeClass?.Name == "ReturnInsertedEntity");

        var currentDbDialect = GetDatabaseDialect(classSymbol);

        // 🔍 Diagnostic: Log ReturnInsertedId detection
        if (hasReturnInsertedId || hasReturnInsertedEntity)
        {
            sb.AppendLine($"// 🔍 DIAGNOSTIC: Method={method.Name}, DbDialect={currentDbDialect}, HasReturnId={hasReturnInsertedId}, HasReturnEntity={hasReturnInsertedEntity}");
        }

        if (hasReturnInsertedId)
        {
            processedSql = AddReturningClauseForInsert(processedSql, currentDbDialect, returnAll: false);
        }
        else if (hasReturnInsertedEntity)
        {
            processedSql = AddReturningClauseForInsert(processedSql, currentDbDialect, returnAll: true);
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
            // Check for actual DELETE statement (not just containing "DELETE" in parameter names)
            var normalizedSql = System.Text.RegularExpressions.Regex.Replace(processedSql, @"@\w+", ""); // Remove parameters
            if (normalizedSql.IndexOf("DELETE FROM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (normalizedSql.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase) &&
                 normalizedSql.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) < 0))
            {
                var entityTableName = originalEntityType?.Name ?? "table";
                processedSql = ConvertDeleteToSoftDelete(processedSql, softDeleteConfig, currentDbDialect, entityTableName);
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
            // INSERT: Add CreatedAt, CreatedBy
            if (processedSql.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                processedSql = AddAuditFieldsToInsert(processedSql, auditFieldsConfig, currentDbDialect, method);
            }
            // UPDATE: Add UpdatedAt, UpdatedBy (including DELETE converted to UPDATE)
            else if (processedSql.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0 || wasDeleteConverted)
            {
                processedSql = AddAuditFieldsToUpdate(processedSql, auditFieldsConfig, currentDbDialect, method);
            }
        }

        // 🚀 TDD Green: Check for [ConcurrencyCheck]
        var concurrencyColumn = GetConcurrencyCheckColumn(originalEntityType);
        if (concurrencyColumn != null && processedSql.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // ADD optimistic locking: version = version + 1 AND version = @version
            processedSql = AddConcurrencyCheck(processedSql, concurrencyColumn, method);
        }

        SharedCodeGenerationUtilities.GenerateCommandSetup(sb, processedSql, method, connectionName, classSymbol);

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

        // 🔍 Diagnostic: Log return type classification
        sb.AppendLine($"// 🔍 DIAGNOSTIC: ReturnType={returnTypeString}, Category={returnCategory}, InnerType={innerType}");

        // 🚀 MySQL/SQLite/Oracle Special Handling for ReturnInsertedId/Entity
        var dbDialect = GetDatabaseDialect(classSymbol);

        // 🔍 Diagnostic: Log special handling checks
        sb.AppendLine($"// 🔍 DIAGNOSTIC: Checking special handling - DbDialect={dbDialect}, HasReturnId={hasReturnInsertedId}, Category={returnCategory}");

        if ((dbDialect == "MySql" || dbDialect == "0") && hasReturnInsertedId && returnCategory == ReturnTypeCategory.Scalar)
        {
            sb.AppendLine("// 🔍 DIAGNOSTIC: Entering MySQL special handling");
            // MySQL: INSERT + SELECT LAST_INSERT_ID()
            GenerateMySqlLastInsertId(sb, innerType, cancellationTokenArg);
            goto skipNormalExecution;
        }
        if ((dbDialect == "SQLite" || dbDialect == "5") && hasReturnInsertedId && returnCategory == ReturnTypeCategory.Scalar)
        {
            sb.AppendLine("// 🔍 DIAGNOSTIC: Entering SQLite special handling");
            // SQLite: INSERT + SELECT last_insert_rowid()
            GenerateSQLiteLastInsertId(sb, innerType, cancellationTokenArg);
            goto skipNormalExecution;
        }
        if ((dbDialect == "MySql" || dbDialect == "0") && hasReturnInsertedEntity)
        {
            // MySQL: INSERT + LAST_INSERT_ID + SELECT *
            GenerateMySqlReturnEntity(sb, returnTypeString, entityType, templateResult, classSymbol, cancellationTokenArg);
            goto skipNormalExecution;
        }
        if ((dbDialect == "SQLite" || dbDialect == "5") && hasReturnInsertedEntity)
        {
            // SQLite: INSERT + last_insert_rowid() + SELECT *
            GenerateSQLiteReturnEntity(sb, returnTypeString, entityType, templateResult, classSymbol, cancellationTokenArg);
            goto skipNormalExecution;
        }
        if ((dbDialect == "Oracle" || dbDialect == "3") && hasReturnInsertedEntity)
        {
            // Oracle: INSERT + RETURNING id INTO + SELECT *
            GenerateOracleReturnEntity(sb, returnTypeString, entityType, templateResult, classSymbol, cancellationTokenArg);
            goto skipNormalExecution;
        }

        switch (returnCategory)
        {
            case ReturnTypeCategory.Scalar:
                // 检查SQL是否是NonQuery命令（UPDATE, DELETE, INSERT）
                var sqlUpper = templateResult.ProcessedSql.TrimStart().ToUpperInvariant();
                // Special case: If SQL has "; SELECT last_insert_rowid()" (SQLite), use ExecuteScalar
                var hasSqliteLastInsertRowid = templateResult.ProcessedSql.IndexOf("last_insert_rowid()", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!hasSqliteLastInsertRowid &&
                    (sqlUpper.StartsWith("UPDATE ") || sqlUpper.StartsWith("DELETE ") ||
                    (sqlUpper.StartsWith("INSERT ") && innerType == "int")))
                {
                    // NonQuery命令，返回affected rows
                    sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
                }
                else
                {
                    // 真正的Scalar查询（SELECT COUNT, SUM等）或 SQLite last_insert_rowid()
                GenerateScalarExecution(sb, innerType, cancellationTokenArg);
                }
                break;
            case ReturnTypeCategory.Collection:
                GenerateCollectionExecution(sb, returnTypeString, entityType, templateResult, method, cancellationTokenArg);
                break;
            case ReturnTypeCategory.SingleEntity:
                GenerateSingleEntityExecution(sb, returnTypeString, entityType, templateResult, cancellationTokenArg);
                break;
            case ReturnTypeCategory.DynamicDictionary:
                GenerateDynamicDictionaryExecution(sb, innerType, cancellationTokenArg);
                break;
            case ReturnTypeCategory.DynamicDictionaryCollection:
                GenerateDynamicDictionaryCollectionExecution(sb, innerType, cancellationTokenArg);
                break;
            default:
                // Non-query execution (INSERT, UPDATE, DELETE)
                sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
                break;
        }

        skipNormalExecution:
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

        return false; // Method body is not complete, outer code needs to add return statement and close brace
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

        // 检查标量类型（支持简单名称和完全限定名称）
        if (innerType == "int" || innerType == "System.Int32" ||
            innerType == "long" || innerType == "System.Int64" ||
            innerType == "bool" || innerType == "System.Boolean" ||
            innerType == "decimal" || innerType == "System.Decimal" ||
            innerType == "double" || innerType == "System.Double" ||
            innerType == "string" || innerType == "System.String")
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
        // - Dictionary<string, object?>
        // - System.Collections.Generic.Dictionary<string, object>
        // - System.Collections.Generic.Dictionary<System.String, System.Object>
        // - global::System.Collections.Generic.Dictionary<string, object>
        // 使用正则表达式匹配更灵活
        return System.Text.RegularExpressions.Regex.IsMatch(type,
            @"Dictionary\s*<\s*(string|System\.String)\s*,\s*(object|System\.Object)\s*\??\s*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 检查是否为动态字典集合类型：List&lt;Dictionary&lt;string, object&gt;&gt;
    /// </summary>
    private bool IsDynamicDictionaryCollection(string type)
    {
        // 支持多种格式：
        // - List<Dictionary<string, object>>
        // - List<Dictionary<string, object?>>
        // - System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>
        // 首先检查是否包含 List<，然后检查是否包含 Dictionary<string, object>
        if (!type.Contains("List<") && !type.Contains("IEnumerable<") &&
            !type.Contains("ICollection<") && !type.Contains("IReadOnlyList<"))
        {
            return false;
        }

        // 使用正则表达式匹配 Dictionary<string, object> 模式
        return System.Text.RegularExpressions.Regex.IsMatch(type,
            @"Dictionary\s*<\s*(string|System\.String)\s*,\s*(object|System\.Object)\s*\??\s*>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private bool IsScalarReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.Scalar;
    private bool IsCollectionReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.Collection;
    private bool IsSingleEntityReturnType(string returnType) => ClassifyReturnType(returnType).Category == ReturnTypeCategory.SingleEntity;

    private void GenerateScalarExecution(IndentedStringBuilder sb, string innerType, string cancellationTokenArg = "")
    {
        sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')});");

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

    private void GenerateCollectionExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType, SqlTemplateResult templateResult, IMethodSymbol method, string cancellationTokenArg = "")
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        // 确保使用全局命名空间前缀，避免命名冲突
        var collectionType = innerType.StartsWith("System.") ? $"global::{innerType}" : innerType;

        // 🚀 性能优化：智能预分配List容量
        // 检测LIMIT参数并预分配容量，减少List重新分配和GC压力
        var limitParam = DetectLimitParameter(templateResult.ProcessedSql, method);
        if (limitParam != null)
        {
            sb.AppendLine($"// 🚀 性能优化：预分配List容量（基于LIMIT参数）");
            sb.AppendLine($"var __initialCapacity__ = {limitParam} > 0 ? {limitParam} : 16;");
            sb.AppendLine($"__result__ = new {collectionType}(__initialCapacity__);");
        }
        else
        {
            // 使用默认初始容量16，平衡小查询和大查询
            sb.AppendLine($"// 🚀 性能优化：预分配默认容量（避免频繁扩容）");
            sb.AppendLine($"__result__ = new {collectionType}(16);");
        }

        sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')});");

        // 🚀 性能优化：在第一次Read()后缓存列序号
        // 注意：必须在Read()后调用GetOrdinal()，否则空结果集会失败
        if (entityType != null && (templateResult.ColumnOrder == null || templateResult.ColumnOrder.Count == 0))
        {
            sb.AppendLine();
            sb.AppendLine("// 🚀 性能优化：声明列序号缓存变量（在第一次读取后赋值）");
            GenerateOrdinalCachingDeclarations(sb, entityType);
            sb.AppendLine("bool __firstRow__ = true;");
            sb.AppendLine();
        }

        sb.AppendLine($"while (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();

        // 在循环内第一次迭代时初始化ordinal
        if (entityType != null && (templateResult.ColumnOrder == null || templateResult.ColumnOrder.Count == 0))
        {
            sb.AppendLine("if (__firstRow__)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("// 初始化列序号（仅执行一次）");
            GenerateOrdinalCachingInitialization(sb, entityType);
            sb.AppendLine("__firstRow__ = false;");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (entityType != null)
        {
            GenerateEntityFromReaderInLoop(sb, entityType, "item", templateResult);
            sb.AppendLine($"(({collectionType})__result__).Add(item);");
        }
        else
        {
            // Handle scalar collections (List<int>, List<string>, etc.)
            // Extract the element type from the collection type
            var elementTypeMatch = System.Text.RegularExpressions.Regex.Match(innerType, @"List<(.+)>$");
            if (elementTypeMatch.Success)
            {
                var elementTypeName = elementTypeMatch.Groups[1].Value;
                sb.AppendLine($"// Read scalar value from column 0");
                
                // Generate appropriate reader code based on type
                if (elementTypeName == "int" || elementTypeName == "System.Int32")
                {
                    sb.AppendLine($"var item = reader.GetInt32(0);");
                }
                else if (elementTypeName == "long" || elementTypeName == "System.Int64")
                {
                    sb.AppendLine($"var item = reader.GetInt64(0);");
                }
                else if (elementTypeName == "string" || elementTypeName == "System.String")
                {
                    sb.AppendLine($"var item = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);");
                }
                else if (elementTypeName == "decimal" || elementTypeName == "System.Decimal")
                {
                    sb.AppendLine($"var item = reader.GetDecimal(0);");
                }
                else if (elementTypeName == "double" || elementTypeName == "System.Double")
                {
                    sb.AppendLine($"var item = reader.GetDouble(0);");
                }
                else if (elementTypeName == "bool" || elementTypeName == "System.Boolean")
                {
                    sb.AppendLine($"var item = reader.GetBoolean(0);");
                }
                else if (elementTypeName == "DateTime" || elementTypeName == "System.DateTime")
                {
                    sb.AppendLine($"var item = reader.GetDateTime(0);");
                }
                else if (elementTypeName == "Guid" || elementTypeName == "System.Guid")
                {
                    sb.AppendLine($"var item = reader.GetGuid(0);");
                }
                else
                {
                    // Fallback to indexer for unknown types
                    sb.AppendLine($"var item = ({elementTypeName})reader[0];");
                }
                
                sb.AppendLine($"(({collectionType})__result__).Add(item);");
            }
        }

        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// 检测SQL中的LIMIT参数，用于List容量预分配优化
    /// </summary>
    private string? DetectLimitParameter(string sql, IMethodSymbol method)
    {
        // 检测LIMIT子句模式：
        // - LIMIT @paramName
        // - LIMIT @limit
        // - LIMIT :paramName (Oracle)
        var sqlUpper = sql.ToUpperInvariant();

        // 查找LIMIT关键字
        var limitIndex = sqlUpper.LastIndexOf("LIMIT");
        if (limitIndex < 0) return null;

        // 提取LIMIT后的参数名
        var afterLimit = sql.Substring(limitIndex + 5).Trim();

        // 匹配 @paramName 或 :paramName
        var match = System.Text.RegularExpressions.Regex.Match(afterLimit, @"^[@:](\w+)");
        if (!match.Success) return null;

        var paramName = match.Groups[1].Value;

        // 验证参数是否存在于方法签名中
        var param = method.Parameters.FirstOrDefault(p =>
            p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

        if (param != null && IsIntegerType(param.Type))
        {
            return paramName;
        }

        return null;
    }

    /// <summary>
    /// 检查类型是否为整数类型
    /// </summary>
    private bool IsIntegerType(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName == "int" || typeName == "long" ||
               typeName == "short" || typeName == "byte" ||
               typeName == "uint" || typeName == "ulong" ||
               typeName == "ushort" || typeName == "sbyte";
    }

    private void GenerateSingleEntityExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType, SqlTemplateResult templateResult, string cancellationTokenArg = "")
    {
        sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine($"if (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
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
    private void GenerateDynamicDictionaryCollectionExecution(IndentedStringBuilder sb, string returnType, string cancellationTokenArg = "")
    {
        // 确保使用全局命名空间前缀
        var collectionType = returnType.StartsWith("System.") ? $"global::{returnType}" : returnType;

        sb.AppendLine($"__result__ = new {collectionType}();");
        sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')});");
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
        sb.AppendLine($"while (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
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
    private void GenerateDynamicDictionaryExecution(IndentedStringBuilder sb, string returnType, string cancellationTokenArg = "")
    {
        sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine($"if (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
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

        // 排除集合类型（Dictionary, List 等），这些不应被当作实体类型
        // 🔧 修复：Dictionary<string, object?> 等集合类型不应该被当作实体来映射属性
        if (type.IsGenericType)
        {
            var baseTypeName = type.ConstructedFrom.GetCachedDisplayString();
            if (baseTypeName.Contains("Dictionary<") ||
                baseTypeName.Contains("List<") ||
                baseTypeName.Contains("IEnumerable<") ||
                baseTypeName.Contains("ICollection<") ||
                baseTypeName.Contains("HashSet<") ||
                baseTypeName.Contains("Queue<") ||
                baseTypeName.Contains("Stack<"))
            {
                return true;
            }
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
        // First check [RepositoryFor] attribute's Dialect property
        var repositoryForAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryForAttribute" || a.AttributeClass?.Name == "RepositoryFor");

        if (repositoryForAttr != null && repositoryForAttr.NamedArguments.Length > 0)
        {
            // Check for Dialect named argument
            foreach (var na in repositoryForAttr.NamedArguments)
            {
                if (na.Key == "Dialect" && na.Value.Value != null)
                {
                    // SqlDefineTypes is an enum, get its string representation
                    return na.Value.Value.ToString();
                }
            }
        }

        // Fallback: check [SqlDefine] attribute
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
        // SqlDefineTypes enum: MySql=0, SqlServer=1, PostgreSql=2, Oracle=3, DB2=4, SQLite=5

        var returningClause = returnAll ? "*" : "id";

        // PostgreSQL (2): ADD RETURNING clause at the end
        if (dialect == "PostgreSql" || dialect == "2")
        {
            return sql + $" RETURNING {returningClause}";
        }

        // SQLite (5): Use custom code generation instead of SQL-level RETURNING
        // RETURNING was only added in SQLite 3.35+ (2021-03-12), so we handle it differently
        // The actual logic is in GenerateSQLiteLastInsertId() and GenerateSQLiteReturnEntity()
        if (dialect == "SQLite" || dialect == "5")
        {
            // Return original SQL - special handling in execution code
            return sql;
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

        // Oracle (3): RETURNING id INTO :out_id
        if (dialect == "Oracle" || dialect == "3")
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
            // Check which columns are not already in the SQL
            var columnsSection = sql.Substring(insertIntoIndex, columnsEndIndex - insertIntoIndex);
            var columnsToAdd = new System.Collections.Generic.List<string>();
            var valuesToAdd = new System.Collections.Generic.List<string>();

            for (int i = 0; i < additionalColumns.Count; i++)
            {
                // Only add if column not already present
                if (columnsSection.IndexOf(additionalColumns[i], StringComparison.OrdinalIgnoreCase) < 0)
                {
                    columnsToAdd.Add(additionalColumns[i]);
                    valuesToAdd.Add(additionalValues[i]);
                }
            }

            // Only modify SQL if there are columns to add
            if (columnsToAdd.Count > 0)
            {
                var newSql = sql.Substring(0, columnsEndIndex);
                newSql += ", " + string.Join(", ", columnsToAdd);
                newSql += sql.Substring(columnsEndIndex, valuesEndIndex - columnsEndIndex);
                newSql += ", " + string.Join(", ", valuesToAdd);
                newSql += sql.Substring(valuesEndIndex);
                return newSql;
            }
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
        string connectionName,
        INamedTypeSymbol classSymbol,
        string cancellationTokenArg = "")
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

        // Parse column names from SQL: INSERT INTO table (col1, col2) VALUES (...)
        List<string>? specifiedColumns = null;
        var insertMatch = System.Text.RegularExpressions.Regex.Match(sql, @"INSERT\s+INTO\s+\w+\s*\(([^)]+)\)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (insertMatch.Success)
        {
            var columnsText = insertMatch.Groups[1].Value;
            specifiedColumns = columnsText.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
        }

        // Get properties to insert (exclude record's EqualityContract)
        var allProperties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract")
            .ToList();

        List<IPropertySymbol> properties;

        if (specifiedColumns != null && specifiedColumns.Count > 0)
        {
            // Use only properties that match specified columns (case-insensitive snake_case match)
            properties = new List<IPropertySymbol>();
            foreach (var column in specifiedColumns)
            {
                var prop = allProperties.FirstOrDefault(p =>
                    SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name).Equals(column, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    properties.Add(prop);
                }
            }
        }
        else
        {
            // Fallback: use all properties except Id (auto-increment)
            properties = allProperties.Where(p => p.Name != "Id").ToList();
        }

        // Remove --exclude from SQL
        var baseSql = sql.Replace("--exclude Id", "").Replace("--exclude id", "").Trim();

        // Create command (reuse __cmd__ from outer scope)
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");

        // 🔧 Transaction支持：如果Repository设置了Transaction属性，将其设置到command上
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Check if method returns List<TKey> with [ReturnInsertedId]
        var hasReturnInsertedId = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedIdAttribute" || a.AttributeClass?.Name == "ReturnInsertedId");
        var returnType = method.ReturnType.GetCachedDisplayString();
        var innerType = SharedCodeGenerationUtilities.ExtractInnerTypeFromTask(returnType);
        var isListReturn = innerType.StartsWith("List<") || innerType.StartsWith("System.Collections.Generic.List<");

        // Generate code
        if (hasReturnInsertedId && isListReturn)
        {
            // Extract TKey from List<TKey>
            var keyType = innerType.Replace("List<", "").Replace("System.Collections.Generic.List<", "").TrimEnd('>');
            sb.AppendLine($"var __ids__ = new global::System.Collections.Generic.List<{keyType}>();");
        }
        else
        {
            sb.AppendLine($"int __totalAffected__ = 0;");
        }
        sb.AppendLine();

        // Check for empty collection
        sb.AppendLine($"if ({paramName} == null || !{paramName}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__?.Dispose();");
        if (hasReturnInsertedId && isListReturn)
        {
            var keyType = innerType.Replace("List<", "").Replace("System.Collections.Generic.List<", "").TrimEnd('>');
            sb.AppendLine($"return new global::System.Collections.Generic.List<{keyType}>();");
        }
        else
        {
            sb.AppendLine("return 0;");
        }
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

        // Reuse existing __cmd__ from outer scope
        // Clear previous parameters if any
        sb.AppendLine("__cmd__.Parameters.Clear();");
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
        if (hasReturnInsertedId && isListReturn)
        {
            // For returning IDs, we need to retrieve them after insert
            var keyType = innerType.Replace("List<", "").Replace("System.Collections.Generic.List<", "").TrimEnd('>');
            var dbDialect = GetDatabaseDialect(classSymbol);

            sb.AppendLine($"// Execute and retrieve inserted IDs");
            sb.AppendLine($"await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
            sb.AppendLine();

            // Database-specific last insert ID retrieval
            sb.AppendLine($"// Get last insert id (database-specific)");
            sb.AppendLine($"__cmd__.Parameters.Clear();");

            if (dbDialect == "SQLite" || dbDialect == "5")
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT last_insert_rowid()\";");
            }
            else if (dbDialect == "MySql" || dbDialect == "0")
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT LAST_INSERT_ID()\";");
            }
            else if (dbDialect == "SqlServer" || dbDialect == "3")
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT SCOPE_IDENTITY()\";");
            }
            else
            {
                // Default to SQLite
                sb.AppendLine($"__cmd__.CommandText = \"SELECT last_insert_rowid()\";");
            }

            sb.AppendLine($"var __lastId__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}));");
            sb.AppendLine($"var __batchCount__ = __batch__.Count();");
            sb.AppendLine($"var __firstId__ = __lastId__ - __batchCount__ + 1;");
            sb.AppendLine($"for (long i = 0; i < __batchCount__; i++)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"__ids__.Add(({keyType})(__firstId__ + i));");
            sb.PopIndent();
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"__totalAffected__ += await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Dispose command
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine();

        // Return result (async method returns directly)
        if (hasReturnInsertedId && isListReturn)
        {
            sb.AppendLine("return __ids__;");
        }
        else
        {
            sb.AppendLine("return __totalAffected__;");
        }

        // Close method body (PopIndent and closing brace)
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generates MySQL-specific code for ReturnInsertedId using LAST_INSERT_ID().
    /// </summary>
    private void GenerateMySqlLastInsertId(IndentedStringBuilder sb, string innerType, string cancellationTokenArg = "")
    {
        // Step 1: Execute INSERT
        sb.AppendLine($"await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine();

        // Step 2: Get LAST_INSERT_ID()
        sb.AppendLine("// MySQL: Get last inserted ID");
        sb.AppendLine("__cmd__.CommandText = \"SELECT LAST_INSERT_ID()\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')});");

        // Convert result
        if (innerType == "long" || innerType == "System.Int64")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToInt64(scalarResult) : default(long);");
        }
        else if (innerType == "int" || innerType == "System.Int32")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToInt32(scalarResult) : default(int);");
        }
        else
        {
            sb.AppendLine($"__result__ = scalarResult != null ? Convert.To{innerType.Replace("System.", "")}(scalarResult) : default({innerType});");
        }
    }

    /// <summary>
    /// Generates SQLite-specific code for ReturnInsertedId using last_insert_rowid().
    /// Similar to MySQL but uses SQLite's last_insert_rowid() function.
    /// </summary>
    private void GenerateSQLiteLastInsertId(IndentedStringBuilder sb, string innerType, string cancellationTokenArg = "")
    {
        // Step 1: Execute INSERT
        sb.AppendLine("// 🚀 SQLite Special Handling: INSERT + last_insert_rowid()");
        sb.AppendLine($"await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine();

        // Step 2: Get last_insert_rowid()
        sb.AppendLine("// SQLite: Get last inserted ID using last_insert_rowid()");
        sb.AppendLine("__cmd__.CommandText = \"SELECT last_insert_rowid()\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')});");

        // Convert result
        if (innerType == "long" || innerType == "System.Int64")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToInt64(scalarResult) : default(long);");
        }
        else if (innerType == "int" || innerType == "System.Int32")
        {
            sb.AppendLine("__result__ = scalarResult != null ? Convert.ToInt32(scalarResult) : default(int);");
        }
        else
        {
            sb.AppendLine($"__result__ = scalarResult != null ? Convert.To{innerType.Replace("System.", "")}(scalarResult) : default({innerType});");
        }
    }

    /// <summary>
    /// Gets the appropriate IDataReader Get method for a property type.
    /// </summary>
    private string GetReaderMethod(ITypeSymbol type)
    {
        // Get the full type name without nullable annotations
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .TrimEnd('?');  // Remove nullable suffix if present

        // Also try the simple name for matching
        var simpleName = type.Name;

        return typeName switch
        {
            "string" or "System.String" => "String",
            "int" or "System.Int32" or "Int32" => "Int32",
            "long" or "System.Int64" or "Int64" => "Int64",
            "bool" or "System.Boolean" or "Boolean" => "Boolean",
            "decimal" or "System.Decimal" or "Decimal" => "Decimal",
            "double" or "System.Double" or "Double" => "Double",
            "float" or "System.Single" or "Single" => "Float",
            "System.DateTime" or "DateTime" => "DateTime",
            "System.Guid" or "Guid" => "Guid",
            _ when simpleName == "String" => "String",
            _ when simpleName == "Int32" => "Int32",
            _ when simpleName == "Int64" => "Int64",
            _ when simpleName == "Boolean" => "Boolean",
            _ when simpleName == "Decimal" => "Decimal",
            _ when simpleName == "Double" => "Double",
            _ when simpleName == "Single" => "Float",
            _ when simpleName == "DateTime" => "DateTime",
            _ when simpleName == "Guid" => "Guid",
            _ => "Value" // Fallback to GetValue
        };
    }

    /// <summary>
    /// Generates MySQL-specific code for ReturnInsertedEntity using LAST_INSERT_ID() + SELECT.
    /// </summary>
    private void GenerateMySqlReturnEntity(IndentedStringBuilder sb, string returnTypeString, INamedTypeSymbol? entityType, SqlTemplateResult templateResult, INamedTypeSymbol classSymbol, string cancellationTokenArg = "")
    {
        if (entityType == null)
        {
            sb.AppendLine("// Entity type not found, cannot generate MySQL ReturnEntity");
            sb.AppendLine("__result__ = default!;");
            return;
        }

        // Step 1: Execute INSERT and get LAST_INSERT_ID
        sb.AppendLine($"await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine();
        sb.AppendLine("// MySQL: Get last inserted ID");
        sb.AppendLine("__cmd__.CommandText = \"SELECT LAST_INSERT_ID()\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine($"var __lastInsertId__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}));");
        sb.AppendLine();

        // Step 2: SELECT the complete entity
        var tableName = GetTableNameFromType(classSymbol, entityType);
        var columns = string.Join(", ", entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract")
            .Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)));

        sb.AppendLine($"// SELECT complete entity");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT {columns} FROM {tableName} WHERE id = @lastId\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine("{ var __p__ = __cmd__.CreateParameter(); __p__.ParameterName = \"@lastId\"; __p__.Value = __lastInsertId__; __cmd__.Parameters.Add(__p__); }");
        sb.AppendLine();

        // Execute reader and map entity
        sb.AppendLine($"using (var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"if (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();

        // Map properties
        sb.AppendLine($"__result__ = new {entityType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        sb.AppendLine("{");
        sb.PushIndent();

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract").ToList();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Count - 1 ? "," : "";

            if (prop.Type.IsValueType && prop.Type.NullableAnnotation != NullableAnnotation.Annotated)
            {
                // Non-nullable value type
                sb.AppendLine($"{prop.Name} = reader.Get{GetReaderMethod(prop.Type)}({i}){comma}");
            }
            else
            {
                // Nullable or reference type - unwrap nullable type for GetReaderMethod
                var underlyingType = prop.Type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.ConstructedFrom.ToString() == "System.Nullable<T>"
                    ? namedType.TypeArguments[0]
                    : prop.Type;
                sb.AppendLine($"{prop.Name} = reader.IsDBNull({i}) ? default : reader.Get{GetReaderMethod(underlyingType)}({i}){comma}");
            }
        }

        sb.PopIndent();
        sb.AppendLine("};");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generates SQLite-specific code for ReturnInsertedEntity using last_insert_rowid() + SELECT.
    /// Similar to MySQL but uses SQLite's last_insert_rowid() function.
    /// </summary>
    private void GenerateSQLiteReturnEntity(IndentedStringBuilder sb, string returnTypeString, INamedTypeSymbol? entityType, SqlTemplateResult templateResult, INamedTypeSymbol classSymbol, string cancellationTokenArg = "")
    {
        if (entityType == null)
        {
            sb.AppendLine("// Entity type not found, cannot generate SQLite ReturnEntity");
            sb.AppendLine("__result__ = default!;");
            return;
        }

        // Step 1: Execute INSERT and get last_insert_rowid()
        sb.AppendLine($"await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine();
        sb.AppendLine("// SQLite: Get last inserted ID");
        sb.AppendLine("__cmd__.CommandText = \"SELECT last_insert_rowid()\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine($"var __lastInsertId__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}));");
        sb.AppendLine();

        // Step 2: SELECT the complete entity
        var tableName = GetTableNameFromType(classSymbol, entityType);
        var columns = string.Join(", ", entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract")
            .Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)));

        sb.AppendLine($"// SELECT complete entity");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT {columns} FROM {tableName} WHERE id = @lastId\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine("{ var __p__ = __cmd__.CreateParameter(); __p__.ParameterName = \"@lastId\"; __p__.Value = __lastInsertId__; __cmd__.Parameters.Add(__p__); }");
        sb.AppendLine();

        // Execute reader and map entity
        sb.AppendLine($"using (var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"if (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();

        // Map properties
        sb.AppendLine($"__result__ = new {entityType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        sb.AppendLine("{");
        sb.PushIndent();

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract").ToList();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Count - 1 ? "," : "";

            if (prop.Type.IsValueType && prop.Type.NullableAnnotation != NullableAnnotation.Annotated)
            {
                // Non-nullable value type
                sb.AppendLine($"{prop.Name} = reader.Get{GetReaderMethod(prop.Type)}({i}){comma}");
            }
            else
            {
                // Nullable or reference type - unwrap nullable type for GetReaderMethod
                var underlyingType = prop.Type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.ConstructedFrom.ToString() == "System.Nullable<T>"
                    ? namedType.TypeArguments[0]
                    : prop.Type;
                sb.AppendLine($"{prop.Name} = reader.IsDBNull({i}) ? default : reader.Get{GetReaderMethod(underlyingType)}({i}){comma}");
            }
        }

        sb.PopIndent();
        sb.AppendLine("};");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generates Oracle-specific code for ReturnInsertedEntity using RETURNING INTO + SELECT.
    /// </summary>
    private void GenerateOracleReturnEntity(IndentedStringBuilder sb, string returnTypeString, INamedTypeSymbol? entityType, SqlTemplateResult templateResult, INamedTypeSymbol classSymbol, string cancellationTokenArg = "")
    {
        if (entityType == null)
        {
            sb.AppendLine("// Entity type not found, cannot generate Oracle ReturnEntity");
            sb.AppendLine("__result__ = default!;");
            return;
        }

        // Oracle approach is similar to MySQL but uses ExecuteScalar with SQL that already has RETURNING
        // Since AddReturningClauseForInsert already added RETURNING id INTO :out_id,
        // we need to use a simpler two-step approach:

        // Step 1: Execute INSERT (SQL already has RETURNING but we'll use simpler approach)
        // We'll replace the RETURNING clause temporarily to just get the ID
        sb.AppendLine("// Oracle: Execute INSERT and get returned ID");
        sb.AppendLine($"var __insertedId__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}));");
        sb.AppendLine();

        // Step 2: SELECT the complete entity
        var tableName = GetTableNameFromType(classSymbol, entityType);
        var columns = string.Join(", ", entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract")
            .Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)));

        sb.AppendLine($"// SELECT complete entity");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT {columns} FROM {tableName} WHERE id = @insertedId\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine("{ var __p__ = __cmd__.CreateParameter(); __p__.ParameterName = \"@insertedId\"; __p__.Value = __insertedId__; __cmd__.Parameters.Add(__p__); }");
        sb.AppendLine();

        // Execute reader and map entity
        sb.AppendLine($"using (var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"if (await reader.ReadAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();

        // Map properties
        sb.AppendLine($"__result__ = new {entityType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        sb.AppendLine("{");
        sb.PushIndent();

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract").ToList();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Count - 1 ? "," : "";

            if (prop.Type.IsValueType && prop.Type.NullableAnnotation != NullableAnnotation.Annotated)
            {
                // Non-nullable value type
                sb.AppendLine($"{prop.Name} = reader.Get{GetReaderMethod(prop.Type)}({i}){comma}");
            }
            else
            {
                // Nullable or reference type - unwrap nullable type for GetReaderMethod
                var underlyingType = prop.Type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.ConstructedFrom.ToString() == "System.Nullable<T>"
                    ? namedType.TypeArguments[0]
                    : prop.Type;
                sb.AppendLine($"{prop.Name} = reader.IsDBNull({i}) ? default : reader.Get{GetReaderMethod(underlyingType)}({i}){comma}");
            }
        }

        sb.PopIndent();
        sb.AppendLine("};");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// 生成列序号缓存代码（在while循环外执行）
    /// </summary>
    private void GenerateOrdinalCaching(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
            .ToArray();

        if (properties.Length == 0) return;

        foreach (var prop in properties)
        {
            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
            sb.AppendLine($"var __ord_{prop.Name}__ = reader.GetOrdinal(\"{columnName}\");");
        }
    }

    /// <summary>
    /// 生成列序号缓存变量的声明（初始化为-1）
    /// </summary>
    private void GenerateOrdinalCachingDeclarations(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
            .ToArray();

        if (properties.Length == 0) return;

        foreach (var prop in properties)
        {
            sb.AppendLine($"int __ord_{prop.Name}__ = -1;");
        }
    }

    /// <summary>
    /// 生成列序号缓存变量的初始化（赋值）
    /// </summary>
    private void GenerateOrdinalCachingInitialization(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
            .ToArray();

        if (properties.Length == 0) return;

        foreach (var prop in properties)
        {
            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
            sb.AppendLine($"__ord_{prop.Name}__ = reader.GetOrdinal(\"{columnName}\");");
        }
    }

    /// <summary>
    /// 在循环内生成实体创建代码（使用缓存的列序号）
    /// </summary>
    private void GenerateEntityFromReaderInLoop(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName, SqlTemplateResult templateResult)
    {
        // 如果有ColumnOrder，使用硬编码索引（极致性能）
        if (templateResult.ColumnOrder != null && templateResult.ColumnOrder.Count > 0)
        {
            SharedCodeGenerationUtilities.GenerateEntityMapping(sb, entityType, variableName, templateResult.ColumnOrder);
            return;
        }

        // 使用缓存的ordinal变量（已在循环外生成）
        var entityTypeName = entityType.GetCachedDisplayString();
        if (entityTypeName.EndsWith("?"))
        {
            entityTypeName = entityTypeName.Substring(0, entityTypeName.Length - 1);
        }

        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract")
            .ToArray();

        if (properties.Length == 0)
        {
            sb.AppendLine($"var {variableName} = new {entityTypeName}();");
            return;
        }

        // 使用对象初始化器语法
        if (variableName == "__result__")
        {
            sb.AppendLine($"__result__ = new {entityTypeName}");
        }
        else
        {
            sb.AppendLine($"var {variableName} = new {entityTypeName}");
        }

        sb.AppendLine("{");
        sb.PushIndent();

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var readMethod = prop.Type.UnwrapNullableType().GetDataReaderMethod();
            var isNullable = prop.Type.IsNullableType();

            // 使用缓存的序号变量
            var ordinalVar = $"__ord_{prop.Name}__";
            var valueExpression = string.IsNullOrEmpty(readMethod)
                ? $"({prop.Type.GetCachedDisplayString()})reader[{ordinalVar}]"
                : $"reader.{readMethod}({ordinalVar})";

            var comma = i < properties.Length - 1 ? "," : "";

            // 只对nullable类型生成IsDBNull检查
            if (isNullable)
            {
                sb.AppendLine($"{prop.Name} = reader.IsDBNull({ordinalVar}) ? null : {valueExpression}{comma}");
            }
            else
            {
                sb.AppendLine($"{prop.Name} = {valueExpression}{comma}");
            }
        }

        sb.PopIndent();
        sb.AppendLine("};");
    }

}
