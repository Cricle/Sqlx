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
            
            // Handle generic type parameters for generic methods (e.g., UpdatePartialAsync<TUpdates>)
            var typeParameters = "";
            var typeConstraints = "";
            if (method.IsGenericMethod && method.TypeParameters.Length > 0)
            {
                typeParameters = "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">";
                
                // Generate type parameter constraints
                var constraintClauses = new List<string>();
                foreach (var tp in method.TypeParameters)
                {
                    var constraints = new List<string>();
                    
                    // Check for reference type constraint (class)
                    if (tp.HasReferenceTypeConstraint)
                    {
                        constraints.Add("class");
                    }
                    
                    // Check for value type constraint (struct)
                    if (tp.HasValueTypeConstraint)
                    {
                        constraints.Add("struct");
                    }
                    
                    // Check for unmanaged constraint
                    if (tp.HasUnmanagedTypeConstraint)
                    {
                        constraints.Add("unmanaged");
                    }
                    
                    // Check for notnull constraint
                    if (tp.HasNotNullConstraint)
                    {
                        constraints.Add("notnull");
                    }
                    
                    // Add type constraints (base class or interfaces)
                    foreach (var constraintType in tp.ConstraintTypes)
                    {
                        constraints.Add(constraintType.GetCachedDisplayString());
                    }
                    
                    // Check for new() constraint
                    if (tp.HasConstructorConstraint)
                    {
                        constraints.Add("new()");
                    }
                    
                    if (constraints.Count > 0)
                    {
                        constraintClauses.Add($"where {tp.Name} : {string.Join(", ", constraints)}");
                    }
                }
                
                if (constraintClauses.Count > 0)
                {
                    typeConstraints = " " + string.Join(" ", constraintClauses);
                }
            }
            
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
            sb.AppendLine($"public {asyncModifier}{returnType} {methodName}{typeParameters}({parameters}){typeConstraints}");
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
            if (templateResult.Warnings.Count > 0)
            {
                sb.AppendLine("/// <para>⚠️ Template Warnings:</para>");
                foreach (var warning in templateResult.Warnings)
                {
                    sb.AppendLine($"/// <para>  • {System.Security.SecurityElement.Escape(warning)}</para>");
                }
            }

            // Show error information
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

        // Check if the class is nested and generate containing type hierarchy
        var containingTypes = new List<INamedTypeSymbol>();
        var currentType = repositoryClass.ContainingType;
        while (currentType != null)
        {
            containingTypes.Insert(0, currentType);
            currentType = currentType.ContainingType;
        }

        // Generate containing type declarations (for nested classes)
        foreach (var containingType in containingTypes)
        {
            sb.AppendLine($"partial class {containingType.Name}");
            sb.AppendLine("{");
            sb.PushIndent();
        }

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

        // Note: Connection field should be defined in user's partial class as protected or internal
        // to be accessible from generated code. Private fields are not accessible across partial class files.

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

                try
                {
                    GenerateRepositoryMethod(methodContext);
                }
                catch (Exception ex)
                {
                    // 🔴 Catch and log exception details for debugging
                    sb.AppendLine($"// Error generating method {method.Name}: {ex.Message}");
                    sb.AppendLine($"// Exception type: {ex.GetType().Name}");
                    #if DEBUG
                    sb.AppendLine($"// Stack trace: {ex.StackTrace?.Replace(Environment.NewLine, Environment.NewLine + "// ")}");
                    #endif
                    GenerateFallbackMethod(sb, method);
                }
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

        // Close containing type declarations (for nested classes)
        for (int i = 0; i < containingTypes.Count; i++)
        {
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }

    /// <summary>
    /// Generate interceptor methods for the specified repository class, including pre and post execution callbacks.
    /// </summary>
    /// <param name="sb">The string builder used to construct code.</param>
    /// <param name="repositoryClass">The repository class symbol to generate interceptor methods for.</param>
    public void GenerateInterceptorMethods(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass)
    {
        // Collect and validate interceptor attributes
        var interceptByAttrs = repositoryClass.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "InterceptByAttribute" || a.AttributeClass?.Name == "InterceptBy")
            .ToList();

        if (interceptByAttrs.Any())
        {
            sb.AppendLine("// Interceptors from [InterceptBy] attributes");
            
            // No fields needed for static interceptors
            // For instance interceptors, we'll create new instances on each call
            
            sb.AppendLine();
        }

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
        sb.AppendLine("// Activity track（Define SQLX_ENABLE_TRACING to enable）");
        sb.AppendLine("var __activity__ = global::System.Diagnostics.Activity.Current;");
        sb.AppendLine("var __startTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine();
        sb.AppendLine("if(__activity__ != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var tags = new global::System.Diagnostics.ActivityTagsCollection();");
        sb.AppendLine("tags.Add(\"db.system\",\"sql\");");
        sb.AppendLine("tags.Add(\"db.operation\",\"operationName\");");
        sb.AppendLine($"tags.Add(\"db.statement\",\"{EscapeSqlForCSharp(templateResult.ProcessedSql)}\");");
        sb.AppendLine("__activity__.AddEvent(new ActivityEvent(\"{operationName}\",default,tags));");
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
        
        // Get database dialect early for runtime marker handling
        var currentDbDialect = GetDatabaseDialect(classSymbol);

        if (hasBatchValues)
        {
            // Generate batch INSERT code (complete execution flow, including method closure)
            GenerateBatchInsertCode(sb, processedSql, method, originalEntityType, connectionName, classSymbol, cancellationTokenArg);
            return true; // Batch INSERT handles everything including method closure
        }

        // 🚀 Check for runtime markers and generate specialized code
        if (processedSql.Contains("__RUNTIME_BATCH_UPDATE_"))
        {
            // Convert string dialect to SqlDefine instance
            var dialectInstance = ConvertDialectStringToSqlDefine(currentDbDialect);
            GenerateBatchUpdateCode(sb, processedSql, method, originalEntityType, connectionName, classSymbol, dialectInstance, cancellationTokenArg);
            return true;
        }

        if (processedSql.Contains("__RUNTIME_UPSERT_"))
        {
            GenerateUpsertCode(sb, processedSql, method, originalEntityType, connectionName, classSymbol, cancellationTokenArg, currentDbDialect);
            return true;
        }

        if (processedSql.Contains("__RUNTIME_BATCH_UPSERT_"))
        {
            GenerateBatchUpsertCode(sb, processedSql, method, originalEntityType, connectionName, classSymbol, cancellationTokenArg, currentDbDialect);
            return true;
        }

        if (processedSql.Contains("__RUNTIME_BATCH_EXISTS__"))
        {
            GenerateBatchExistsCode(sb, processedSql, method, connectionName, classSymbol, cancellationTokenArg);
            return true;
        }

        if (processedSql.Contains("__RUNTIME_GET_PAGE_"))
        {
            GenerateGetPageCode(sb, processedSql, method, originalEntityType, connectionName, classSymbol, cancellationTokenArg);
            return true;
        }

        // 🚀 TDD Green: Check for [ReturnInsertedId] or [ReturnInsertedEntity] and modify SQL
        var hasReturnInsertedId = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedIdAttribute" || a.AttributeClass?.Name == "ReturnInsertedId");
        var hasReturnInsertedEntity = method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ReturnInsertedEntityAttribute" || a.AttributeClass?.Name == "ReturnInsertedEntity");

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
            
            // For SQL Server OUTPUT INSERTED.*, we need to update columnOrder to include all columns (including Id)
            // because OUTPUT INSERTED.* returns all columns, not just the ones in the INSERT statement
            if ((currentDbDialect == "SqlServer" || currentDbDialect == "1") && originalEntityType != null)
            {
                // Get all properties including Id
                var allProperties = originalEntityType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract")
                    .OrderBy(p => p.Name == "Id" ? 0 : 1) // Put Id first
                    .ToList();
                
                // Update columnOrder to include all columns in the correct order
                templateResult.ColumnOrder.Clear();
                foreach (var prop in allProperties)
                {
                    var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
                    templateResult.ColumnOrder.Add(columnName);
                }
            }
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
            // INSERT: Add CreatedAt, CreatedBy (must start with INSERT keyword)
            if (IsSqlStatementType(processedSql, "INSERT"))
            {
                processedSql = AddAuditFieldsToInsert(processedSql, auditFieldsConfig, currentDbDialect, method);
            }
            // UPDATE: Add UpdatedAt, UpdatedBy (including DELETE converted to UPDATE)
            // Note: Must check for UPDATE keyword at start to avoid false matches with column names like "updated_at"
            else if (IsSqlStatementType(processedSql, "UPDATE") || wasDeleteConverted)
            {
                processedSql = AddAuditFieldsToUpdate(processedSql, auditFieldsConfig, currentDbDialect, method);
            }
        }

        // 🚀 TDD Green: Check for [ConcurrencyCheck]
        var concurrencyColumn = GetConcurrencyCheckColumn(originalEntityType);
        if (concurrencyColumn != null && IsSqlStatementType(processedSql, "UPDATE"))
        {
            // ADD optimistic locking: version = version + 1 AND version = @version
            processedSql = AddConcurrencyCheck(processedSql, concurrencyColumn, method);
        }

        SharedCodeGenerationUtilities.GenerateCommandSetup(sb, processedSql, method, connectionName, classSymbol);

        // Add try-catch block
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Call [InterceptBy] interceptors
        var interceptByAttrs = classSymbol?.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "InterceptByAttribute" || a.AttributeClass?.Name == "InterceptBy")
            .ToList() ?? new List<AttributeData>();

        if (interceptByAttrs.Any())
        {
            sb.AppendLine("// Call InterceptBy interceptors");
            for (int i = 0; i < interceptByAttrs.Count; i++)
            {
                var attr = interceptByAttrs[i];
                if (attr.ConstructorArguments.Length > 0)
                {
                    var interceptorType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                    if (interceptorType != null)
                    {
                        var typeName = interceptorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        
                        // Check if interceptor is static
                        var isStatic = interceptorType.IsStatic;
                        
                        // Compile-time validation
                        var implementsICommandInterceptor = interceptorType.AllInterfaces.Any(i => 
                            i.Name == "ICommandInterceptor");
                        var implementsIStaticCommandInterceptor = interceptorType.AllInterfaces.Any(i => 
                            i.Name == "IStaticCommandInterceptor");
                        
                        if (!implementsICommandInterceptor && !implementsIStaticCommandInterceptor && !isStatic)
                        {
                            // Generate compile error
                            sb.AppendLine($"#error Interceptor type '{interceptorType.Name}' must implement ICommandInterceptor or IStaticCommandInterceptor");
                            continue;
                        }
                        
                        if (isStatic)
                        {
                            // Static interceptor - call static method directly
                            sb.AppendLine($"{typeName}.OnExecuting(\"{operationName}\", __cmd__);");
                        }
                        else
                        {
                            // Instance interceptor - create new instance each time
                            sb.AppendLine($"new {typeName}().OnExecuting(\"{operationName}\", __cmd__);");
                        }
                    }
                }
            }
        }

        // Call partial method interceptor (用户自定义扩展点，可通过SQLX_ENABLE_PARTIAL_METHODS启用)
        sb.AppendLine("#if SQLX_ENABLE_PARTIAL_METHODS");
        sb.AppendLine($"OnExecuting(\"{operationName}\", __cmd__);");
        sb.AppendLine("#endif");
        sb.AppendLine();

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
                // Special case: If SQL contains {RUNTIME_SQL_} marker, it's a raw SQL execution
                // For methods returning int (like ExecuteRawAsync), use ExecuteNonQueryAsync
                var hasRuntimeSqlMarker = templateResult.ProcessedSql.Contains("{RUNTIME_SQL_");

                if (!hasSqliteLastInsertRowid &&
                    (sqlUpper.StartsWith("UPDATE ") || sqlUpper.StartsWith("DELETE ") ||
                    (sqlUpper.StartsWith("INSERT ") && innerType == "int") ||
                    (hasRuntimeSqlMarker && innerType == "int")))
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
        sb.AppendLine("var __endTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("var __elapsedTicks__ = __endTimestamp__ - __startTimestamp__;");
        sb.AppendLine();
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

        // Call [InterceptBy] interceptors
        if (interceptByAttrs.Any())
        {
            sb.AppendLine("// Call InterceptBy interceptors OnExecuted");
            for (int i = 0; i < interceptByAttrs.Count; i++)
            {
                var attr = interceptByAttrs[i];
                if (attr.ConstructorArguments.Length > 0)
                {
                    var interceptorType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                    if (interceptorType != null)
                    {
                        var typeName = interceptorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var isStatic = interceptorType.IsStatic;
                        
                        if (isStatic)
                        {
                            sb.AppendLine($"{typeName}.OnExecuted(\"{operationName}\", __cmd__);");
                        }
                        else
                        {
                            sb.AppendLine($"new {typeName}().OnExecuted(\"{operationName}\", __cmd__);");
                        }
                    }
                }
            }
        }

        // Call partial method interceptor
        sb.AppendLine("#if SQLX_ENABLE_PARTIAL_METHODS");
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

        // Call [InterceptBy] interceptors
        if (interceptByAttrs.Any())
        {
            sb.AppendLine("// Call InterceptBy interceptors OnError");
            for (int i = 0; i < interceptByAttrs.Count; i++)
            {
                var attr = interceptByAttrs[i];
                if (attr.ConstructorArguments.Length > 0)
                {
                    var interceptorType = attr.ConstructorArguments[0].Value as INamedTypeSymbol;
                    if (interceptorType != null)
                    {
                        var typeName = interceptorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var isStatic = interceptorType.IsStatic;
                        
                        if (isStatic)
                        {
                            sb.AppendLine($"{typeName}.OnError(\"{operationName}\", __cmd__, __ex__);");
                        }
                        else
                        {
                            sb.AppendLine($"new {typeName}().OnError(\"{operationName}\", __cmd__, __ex__);");
                        }
                    }
                }
            }
        }

        // Call partial method interceptor
        sb.AppendLine("#if SQLX_ENABLE_PARTIAL_METHODS");
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

    private enum ReturnTypeCategory
    {
        Scalar,
        Collection,
        SingleEntity,
        DynamicDictionary,          // Dictionary<string, object>
        DynamicDictionaryCollection, // List<Dictionary<string, object>>
        Unknown
    }

    private (ReturnTypeCategory Category, string InnerType) ClassifyReturnType(string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);

        // 检查动态字典集合类型：List<Dictionary<string, object>>
        if (IsDynamicDictionaryCollection(innerType))
            return (ReturnTypeCategory.DynamicDictionaryCollection, innerType);

        // 检查动态字典类型：Dictionary<string, object>
        if (IsDynamicDictionary(innerType))
            return (ReturnTypeCategory.DynamicDictionary, innerType);

        // 检查标量类型（支持简单名称、完全限定名称和 nullable 类型）
        // 先获取基础类型（去掉 ? 或 Nullable<>）
        var baseType = innerType.EndsWith("?") 
            ? innerType.TrimEnd('?') 
            : (innerType.StartsWith("Nullable<") || innerType.StartsWith("System.Nullable<"))
                ? innerType.Substring(innerType.IndexOf('<') + 1).TrimEnd('>')
                : innerType;
        
        if (baseType == "int" || baseType == "System.Int32" ||
            baseType == "long" || baseType == "System.Int64" ||
            baseType == "bool" || baseType == "System.Boolean" ||
            baseType == "decimal" || baseType == "System.Decimal" ||
            baseType == "double" || baseType == "System.Double" ||
            baseType == "float" || baseType == "System.Single" ||
            baseType == "DateTime" || baseType == "System.DateTime" ||
            baseType == "string" || baseType == "System.String")
            return (ReturnTypeCategory.Scalar, innerType);

        // 检查泛型类型参数（如 TResult）- 通常用于 ExecuteScalarAsync<TResult>
        // 泛型类型参数通常是单个大写字母开头的标识符，如 T, TResult, TValue 等
        if (IsGenericTypeParameter(baseType))
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
    /// 检查是否为泛型类型参数（如 TResult, T, TValue 等）
    /// </summary>
    private bool IsGenericTypeParameter(string type)
    {
        // 泛型类型参数通常是：
        // - 单个大写字母 T
        // - T 开头后跟大写字母的标识符，如 TResult, TValue, TEntity
        // - 不包含命名空间分隔符 . 或 ::
        if (string.IsNullOrEmpty(type) || type.Contains(".") || type.Contains("::"))
            return false;
        
        // 检查是否以 T 开头且后面是大写字母或为空
        if (type.Length == 1 && type[0] == 'T')
            return true;
        
        if (type.Length > 1 && type[0] == 'T' && char.IsUpper(type[1]))
            return true;
        
        return false;
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

    private void GenerateScalarExecution(IndentedStringBuilder sb, string innerType, string cancellationTokenArg = "")
    {
        sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')});");

        // Check if the type is nullable
        var isNullable = innerType.EndsWith("?") || innerType.StartsWith("Nullable<");
        var baseType = isNullable 
            ? (innerType.EndsWith("?") ? innerType.TrimEnd('?') : innerType.Substring(9, innerType.Length - 10))
            : innerType;

        // Handle numeric type conversions (e.g., SQLite COUNT returns Int64 but method expects Int32)
        if (baseType == "int" || baseType == "System.Int32")
        {
            if (isNullable)
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToInt32(scalarResult) : (int?)null;");
            }
            else
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToInt32(scalarResult) : default(int);");
            }
        }
        else if (baseType == "long" || baseType == "System.Int64")
        {
            if (isNullable)
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToInt64(scalarResult) : (long?)null;");
            }
            else
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToInt64(scalarResult) : default(long);");
            }
        }
        else if (baseType == "decimal" || baseType == "System.Decimal")
        {
            if (isNullable)
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToDecimal(scalarResult) : (decimal?)null;");
            }
            else
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToDecimal(scalarResult) : default(decimal);");
            }
        }
        else if (baseType == "double" || baseType == "System.Double")
        {
            if (isNullable)
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToDouble(scalarResult) : (double?)null;");
            }
            else
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToDouble(scalarResult) : default(double);");
            }
        }
        else if (baseType == "bool" || baseType == "System.Boolean")
        {
            if (isNullable)
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToBoolean(scalarResult) : (bool?)null;");
            }
            else
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToBoolean(scalarResult) : default(bool);");
            }
        }
        else if (baseType == "DateTime" || baseType == "System.DateTime")
        {
            if (isNullable)
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToDateTime(scalarResult) : (DateTime?)null;");
            }
            else
            {
                sb.AppendLine("__result__ = scalarResult != null && scalarResult != DBNull.Value ? Convert.ToDateTime(scalarResult) : default(DateTime);");
            }
        }
        else
        {
            // Handle generic type parameters (TResult, T, etc.) and other types
            // Use Convert.ChangeType for generic type parameters to handle runtime type conversion
            if (IsGenericTypeParameter(baseType))
            {
                sb.AppendLine($"__result__ = scalarResult != null && scalarResult != DBNull.Value ? ({innerType})Convert.ChangeType(scalarResult, typeof({innerType})) : default({innerType})!;");
            }
            else if (isNullable)
            {
                sb.AppendLine($"__result__ = scalarResult != null && scalarResult != DBNull.Value ? ({baseType})scalarResult : ({innerType})null;");
            }
            else
            {
                sb.AppendLine($"__result__ = scalarResult != null && scalarResult != DBNull.Value ? ({innerType})scalarResult : default({innerType});");
            }
        }
    }

    private void GenerateCollectionExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType, SqlTemplateResult templateResult, IMethodSymbol method, string cancellationTokenArg = "")
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        // 确保使用全局命名空间前缀，避免命名冲突
        var collectionType = innerType.StartsWith("System.") ? $"global::{innerType}" : innerType;

        // 检测LIMIT参数并预分配容量，减少List重新分配和GC压力
        var limitParam = DetectLimitParameter(templateResult.ProcessedSql, method);
        if (limitParam != null)
        {
            sb.AppendLine($"var __initialCapacity__ = {limitParam} > 0 ? {limitParam} : 16;");
            sb.AppendLine($"__result__ = new {collectionType}(__initialCapacity__);");
        }
        else
        {
            // 使用默认初始容量16，平衡小查询和大查询
            sb.AppendLine($"__result__ = new {collectionType}(16);");
        }

        sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')});");

        // 注意：必须在Read()后调用GetOrdinal()，否则空结果集会失败
        if (entityType != null && (templateResult.ColumnOrder == null || templateResult.ColumnOrder.Count == 0))
        {
            sb.AppendLine();
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
            GenerateOrdinalCachingInitialization(sb, entityType);
            sb.AppendLine("__firstRow__ = false;");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }

        if (entityType != null)
        {
            GenerateEntityFromReaderInLoop(sb, entityType, "item", templateResult);
            sb.AppendLine($"__result__.Add(item);");
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
                
                sb.AppendLine($"__result__.Add(item);");
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
        sb.AppendLine("var dict = new global::System.Collections.Generic.Dictionary<string, object>(fieldCount);");
        sb.AppendLine("for (var i = 0; i < fieldCount; i++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("dict[columnNames[i]] = reader.IsDBNull(i) ? null! : reader.GetValue(i);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine($"__result__.Add(dict);");
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
        sb.AppendLine("var fieldCount = reader.FieldCount;");
        sb.AppendLine("__result__ = new global::System.Collections.Generic.Dictionary<string, object>(fieldCount);");
        sb.AppendLine("for (var i = 0; i < fieldCount; i++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var columnName = reader.GetName(i);");
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
        // Strategy: Look for DbConnection field/property in multiple ways
        // IMPORTANT: Primary constructor parameters are NOT accessible across partial class files
        // So we must find a field or property that stores the connection
        
        var allMembers = repositoryClass.GetMembers().ToArray();

        // 1. First check fields - look for DbConnection type or connection name pattern
        var connectionField = allMembers
            .OfType<IFieldSymbol>()
            .Where(f => !f.Name.StartsWith("<")) // Exclude compiler-generated fields
            .FirstOrDefault(f => f.IsDbConnection() || IsConnectionNamePattern(f.Name));
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // 2. Check properties - look for DbConnection type or connection name pattern
        var connectionProperty = allMembers
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsDbConnection() || IsConnectionNamePattern(p.Name));
        if (connectionProperty != null)
        {
            return connectionProperty.Name;
        }

        // 3. Check primary constructor parameters (C# 12 feature)
        // NOTE: Primary constructor parameters create compiler-generated private fields like <paramName>P
        // that are NOT accessible across partial class files.
        var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
        if (primaryConstructor != null)
        {
            var connectionParam = primaryConstructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                // Check if there's a compiler-generated backing field
                var backingField = allMembers
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.Name == $"<{connectionParam.Name}>P");
                
                if (backingField != null)
                {
                    // Found compiler-generated backing field - this means user is using primary constructor
                    // but hasn't defined a field/property to store the connection.
                    // We MUST return a user-defined field name, not the backing field name.
                    // Try to find a common pattern:
                    var userField = allMembers
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.Name == $"_{connectionParam.Name}" && f.IsDbConnection());
                    
                    if (userField != null)
                    {
                        return userField.Name;
                    }
                    
                    // No user-defined field found - return the most common pattern and let it fail with a clear error
                    return $"_{connectionParam.Name}";
                }
                
                // No backing field found - parameter might be directly accessible
                return connectionParam.Name;
            }
        }

        // 4. Check regular constructor parameters and infer field name
        // This is the most common pattern: constructor parameter stored in a field
        var constructor = repositoryClass.InstanceConstructors
            .Where(c => !c.IsStatic && !c.IsImplicitlyDeclared)
            .FirstOrDefault();
        
        if (constructor != null)
        {
            var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                // Common patterns for field names based on constructor parameter:
                // 1. _paramName (most common C# convention)
                // 2. paramName (less common)
                // 3. m_paramName (older convention)
                var possibleFieldNames = new[]
                {
                    $"_{connectionParam.Name}",
                    connectionParam.Name,
                    $"m_{connectionParam.Name}"
                };

                // Try to find a field with one of these names
                // Note: During source generation, GetMembers() might not return private fields
                // from partial class declarations, so we check all possibilities
                foreach (var fieldName in possibleFieldNames)
                {
                    var field = allMembers
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(f => f.Name == fieldName);
                    if (field != null)
                    {
                        return field.Name;
                    }
                }

                // If no field found, assume the most common pattern: _paramName
                // This is a safe assumption because:
                // - It's the most common C# naming convention
                // - The user's partial class likely follows this pattern
                // - If they don't, they can use a property or different pattern
                return $"_{connectionParam.Name}";
            }
        }

        // 5. Check syntax declarations directly (fallback for partial classes)
        // This handles cases where GetMembers() doesn't return private fields from partial classes
        foreach (var syntaxRef in repositoryClass.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            if (syntax is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDecl)
            {
                // Look for field declarations in the syntax tree
                var fieldDecls = classDecl.Members
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax>();
                
                foreach (var fieldDecl in fieldDecls)
                {
                    var variable = fieldDecl.Declaration.Variables.FirstOrDefault();
                    if (variable != null)
                    {
                        var fieldName = variable.Identifier.Text;
                        // Check if this looks like a connection field
                        if (IsConnectionNamePattern(fieldName))
                        {
                            return fieldName;
                        }
                    }
                }
            }
        }

        // 6. Default fallback - use the most common field name
        return "_connection";
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
                    // Get enum name from value
                    return ConvertEnumValueToName(na.Value.Value);
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
                // Get enum name from value
                return ConvertEnumValueToName(dialectValue);
            }
        }

        // Default to SqlServer if not specified
        return "SqlServer";
    }

    /// <summary>
    /// Converts SqlDefineTypes enum value to name
    /// </summary>
    private static string ConvertEnumValueToName(object enumValue)
    {
        if (enumValue == null) return "SqlServer";
        
        // Convert enum value (int) to name
        var intValue = Convert.ToInt32(enumValue);
        return intValue switch
        {
            0 => "MySql",
            1 => "SqlServer",
            2 => "PostgreSql",
            3 => "Oracle",
            4 => "DB2",
            5 => "SQLite",
            _ => "SqlServer"
        };
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

        var additionalSets = new System.Collections.Generic.List<string>();

        // Only add updated_at if not already present in SQL
        if (sql.IndexOf(updatedAtCol, StringComparison.OrdinalIgnoreCase) < 0)
        {
            additionalSets.Add($"{updatedAtCol} = {timestampSql}");
        }

        // Check if method has updatedBy parameter
        if (!string.IsNullOrEmpty(config.UpdatedByColumn))
        {
            var updatedByCol = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.UpdatedByColumn);

            // Only add updated_by if not already present in SQL
            if (sql.IndexOf(updatedByCol, StringComparison.OrdinalIgnoreCase) < 0)
            {
                var updatedByParam = method.Parameters.FirstOrDefault(p =>
                    p.Name.Equals("updatedBy", StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals("updated_by", StringComparison.OrdinalIgnoreCase));

                if (updatedByParam != null)
                {
                    additionalSets.Add($"{updatedByCol} = @{updatedByParam.Name}");
                }
            }
        }

        // If no additional fields to add, return original SQL
        if (additionalSets.Count == 0)
        {
            return sql;
        }

        // Find WHERE clause - but NOT inside runtime markers like {RUNTIME_WHERE_EXPR_...}
        // First, check if there's a runtime WHERE marker
        var runtimeWhereIndex = sql.IndexOf("{RUNTIME_WHERE", StringComparison.OrdinalIgnoreCase);
        
        // Find the actual WHERE keyword (not inside a runtime marker)
        var whereIndex = -1;
        var searchStart = 0;
        while (true)
        {
            var idx = sql.IndexOf("WHERE", searchStart, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            
            // Check if this WHERE is inside a runtime marker
            // A runtime marker looks like {RUNTIME_WHERE_...}
            // If we find WHERE and it's preceded by {RUNTIME_ or _WHERE_, it's part of a marker
            var isInsideMarker = false;
            if (idx > 0)
            {
                // Check if this is part of {RUNTIME_WHERE_...} or similar
                var beforeWhere = sql.Substring(Math.Max(0, idx - 20), Math.Min(20, idx));
                if (beforeWhere.Contains("{RUNTIME_") || beforeWhere.Contains("_WHERE"))
                {
                    isInsideMarker = true;
                }
            }
            
            if (!isInsideMarker)
            {
                whereIndex = idx;
                break;
            }
            
            searchStart = idx + 5; // Move past this WHERE
        }
        
        if (whereIndex > 0)
        {
            // Insert before WHERE
            var beforeWhere = sql.Substring(0, whereIndex).TrimEnd();
            var afterWhere = sql.Substring(whereIndex);
            return $"{beforeWhere}, {string.Join(", ", additionalSets)} {afterWhere}";
        }
        else if (runtimeWhereIndex > 0)
        {
            // There's a runtime WHERE marker but no actual WHERE keyword
            // Insert before the runtime marker
            var beforeMarker = sql.Substring(0, runtimeWhereIndex).TrimEnd();
            var afterMarker = sql.Substring(runtimeWhereIndex);
            return $"{beforeMarker}, {string.Join(", ", additionalSets)} {afterMarker}";
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

        // Check if version column is already in SET clause (already manually added)
        var setVersionPattern = $"{versionCol} =";
        var hasVersionInSet = sql.IndexOf(setVersionPattern, StringComparison.OrdinalIgnoreCase) >= 0;

        // Check if version column is already in WHERE clause
        var whereVersionPattern = $"{versionCol} = {versionParam}";
        var hasVersionInWhere = sql.IndexOf(whereVersionPattern, StringComparison.OrdinalIgnoreCase) >= 0;

        // Find WHERE clause - but NOT inside runtime markers like {RUNTIME_WHERE_EXPR_...}
        var runtimeWhereIndex = sql.IndexOf("{RUNTIME_WHERE", StringComparison.OrdinalIgnoreCase);
        
        // Find the actual WHERE keyword (not inside a runtime marker)
        var whereIndex = -1;
        var searchStart = 0;
        while (true)
        {
            var idx = sql.IndexOf("WHERE", searchStart, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;
            
            // Check if this WHERE is inside a runtime marker
            var isInsideMarker = false;
            if (idx > 0)
            {
                var beforeWhere = sql.Substring(Math.Max(0, idx - 20), Math.Min(20, idx));
                if (beforeWhere.Contains("{RUNTIME_") || beforeWhere.Contains("_WHERE"))
                {
                    isInsideMarker = true;
                }
            }
            
            if (!isInsideMarker)
            {
                whereIndex = idx;
                break;
            }
            
            searchStart = idx + 5;
        }

        if (whereIndex > 0)
        {
            var beforeWhere = sql.Substring(0, whereIndex).TrimEnd();
            var afterWhere = sql.Substring(whereIndex);
            var newSql = sql;

            // 只有当SET子句中没有version时才添加version递增
            if (!hasVersionInSet)
            {
                newSql = $"{beforeWhere}, {versionCol} = {versionCol} + 1 {afterWhere}";
                // Recalculate afterWhere position after modification
                whereIndex = newSql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            }

            // 只有当WHERE子句中没有version检查时才添加
            if (!hasVersionInWhere)
            {
                newSql = newSql + $" AND {versionCol} = {versionParam}";
            }

            return newSql;
        }
        else if (runtimeWhereIndex > 0)
        {
            // There's a runtime WHERE marker but no actual WHERE keyword
            var beforeMarker = sql.Substring(0, runtimeWhereIndex).TrimEnd();
            var afterMarker = sql.Substring(runtimeWhereIndex);
            var newSql = sql;

            // 只有当SET子句中没有version时才添加version递增
            if (!hasVersionInSet)
            {
                newSql = $"{beforeMarker}, {versionCol} = {versionCol} + 1 {afterMarker}";
            }

            // 只有当WHERE子句中没有version检查时才添加
            // For runtime WHERE markers, append after the marker
            if (!hasVersionInWhere)
            {
                newSql = newSql + $" AND {versionCol} = {versionParam}";
            }

            return newSql;
        }
        else
        {
            var newSql = sql.TrimEnd();

            // 只有当SET子句中没有version时才添加version递增
            if (!hasVersionInSet)
            {
                newSql = $"{newSql}, {versionCol} = {versionCol} + 1";
            }

            // 添加WHERE子句（因为没有WHERE，所以version检查肯定不存在）
            newSql = $"{newSql} WHERE {versionCol} = {versionParam}";

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
        // Support quoted table names: INSERT INTO `table` (col1, col2) or INSERT INTO [table] (col1, col2)
        List<string>? specifiedColumns = null;
        var insertMatch = System.Text.RegularExpressions.Regex.Match(sql, @"INSERT\s+INTO\s+[`\[]?\w+[`\]]?\s*\(([^)]+)\)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (insertMatch.Success)
        {
            var columnsText = insertMatch.Groups[1].Value;
            specifiedColumns = columnsText.Split(',')
                .Select(c => c.Trim().Trim('[', ']', '"', '`', ' ', '\t', '\r', '\n'))  // Remove brackets, quotes, and whitespace from column names
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
            
            // 🔍 DEBUG: Add comment to generated code to verify this code is being executed
            sb.AppendLine($"// 🔍 DEBUG: Processing {specifiedColumns.Count} specified columns from SQL");
            
            foreach (var column in specifiedColumns)
            {
                var prop = allProperties.FirstOrDefault(p =>
                    SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name).Equals(column, StringComparison.OrdinalIgnoreCase));
                
                // Fallback: if no match found, try direct name matching (case-insensitive)
                if (prop == null)
                {
                    prop = allProperties.FirstOrDefault(p => p.Name.Equals(column, StringComparison.OrdinalIgnoreCase));
                }
                
                if (prop != null)
                {
                    properties.Add(prop);
                }
                else
                {
                    // 🔍 DEBUG: Log when a column doesn't match any property
                    // This helps diagnose column matching issues
                    System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] Column '{column}' from SQL did not match any property in entity '{entityType.Name}'");
                    System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] Available properties: {string.Join(", ", allProperties.Select(p => $"{p.Name} -> {SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)}"))}");
                    
                    // Add comment to generated code
                    sb.AppendLine($"// 🔍 DEBUG: Column '{column}' did not match any property");
                }
            }
            
            // 🔍 DEBUG: Log the final properties list
            if (properties.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] WARNING: No properties matched for entity '{entityType.Name}'!");
                System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] Specified columns: {string.Join(", ", specifiedColumns)}");
                System.Diagnostics.Debug.WriteLine($"[Sqlx.Generator] Available properties: {string.Join(", ", allProperties.Select(p => $"{p.Name} -> {SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)}"))}");
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
            // Extract TKey from List<TKey> - handle both qualified and unqualified names
            var keyType = innerType;
            if (keyType.StartsWith("System.Collections.Generic.List<"))
            {
                keyType = keyType.Substring("System.Collections.Generic.List<".Length).TrimEnd('>');
            }
            else if (keyType.StartsWith("List<"))
            {
                keyType = keyType.Substring("List<".Length).TrimEnd('>');
            }
            
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
            // Use the same keyType extraction logic
            var keyType = innerType;
            if (keyType.StartsWith("System.Collections.Generic.List<"))
            {
                keyType = keyType.Substring("System.Collections.Generic.List<".Length).TrimEnd('>');
            }
            else if (keyType.StartsWith("List<"))
            {
                keyType = keyType.Substring("List<".Length).TrimEnd('>');
            }
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

        // 🔍 DEBUG: Add comment showing what properties were found
        if (properties.Count == 0)
        {
            // 🔴 CRITICAL ERROR: No properties matched!
            var errorMsg = $"CRITICAL: No properties matched for batch insert! " +
                          $"Entity: {entityType.Name}, " +
                          $"Specified columns: [{string.Join(", ", specifiedColumns ?? new List<string>())}], " +
                          $"Available properties: [{string.Join(", ", allProperties.Select(p => $"{p.Name}=>{SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)}"))}]";
            throw new InvalidOperationException(errorMsg);
        }

        sb.AppendLine($"// ✓ Matched {properties.Count} properties: {string.Join(", ", properties.Select(p => p.Name))}");

        // Use string concatenation to avoid interpolation issues
        sb.AppendLine("__valuesClauses__.Add($\"(" + valuesClause + ")\");");
        sb.AppendLine("__itemIndex__++;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("var __values__ = string.Join(\", \", __valuesClauses__);");
        sb.AppendLine();

        // Replace marker in SQL - escape quotes for verbatim string literal
        var escapedBaseSql = baseSql.Replace("\"", "\"\"");
        sb.AppendLine($"var __sql__ = @\"{escapedBaseSql}\";");
        sb.AppendLine($"__sql__ = __sql__.Replace(\"{marker}{paramName}__\", __values__);");
        
        // For SQL Server and MySQL with ReturnInsertedId, append SELECT to the INSERT
        if (hasReturnInsertedId && isListReturn)
        {
            var dbDialect = GetDatabaseDialect(classSymbol);
            if (dbDialect == "SqlServer" || dbDialect == "1")
            {
                sb.AppendLine($"__sql__ += \"; SELECT CAST(SCOPE_IDENTITY() AS BIGINT)\";");
            }
            else if (dbDialect == "MySql" || dbDialect == "0")
            {
                sb.AppendLine($"__sql__ += \"; SELECT LAST_INSERT_ID()\";");
            }
        }
        
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
            // Extract TKey from List<TKey> - handle both qualified and unqualified names
            var keyType = innerType;
            if (keyType.StartsWith("System.Collections.Generic.List<"))
            {
                keyType = keyType.Substring("System.Collections.Generic.List<".Length).TrimEnd('>');
            }
            else if (keyType.StartsWith("List<"))
            {
                keyType = keyType.Substring("List<".Length).TrimEnd('>');
            }
            
            var dbDialect = GetDatabaseDialect(classSymbol);

            if (dbDialect == "SqlServer" || dbDialect == "1" || dbDialect == "MySql" || dbDialect == "0")
            {
                // SQL Server and MySQL: INSERT with appended SELECT
                sb.AppendLine($"// Execute INSERT and retrieve last inserted ID ({(dbDialect == "SqlServer" || dbDialect == "1" ? "SQL Server" : "MySQL")})");
                sb.AppendLine($"var __lastId__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}) ?? 0L);");
                sb.AppendLine($"var __batchCount__ = __batch__.Count();");
                
                if (dbDialect == "MySql" || dbDialect == "0")
                {
                    // MySQL: LAST_INSERT_ID() returns the FIRST auto-increment ID of the batch
                    sb.AppendLine($"var __firstId__ = __lastId__;");
                }
                else
                {
                    // SQL Server: SCOPE_IDENTITY() returns the LAST auto-increment ID of the batch
                    sb.AppendLine($"var __firstId__ = __lastId__ - __batchCount__ + 1;");
                }
            }
            else
            {
                // Other databases: Execute INSERT, then query last ID
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
                else if (dbDialect == "PostgreSql" || dbDialect == "2")
                {
                    // PostgreSQL should use RETURNING clause in the INSERT statement itself
                    // For now, fall back to currval (requires knowing sequence name)
                    sb.AppendLine($"__cmd__.CommandText = \"SELECT lastval()\";");
                }
                else
                {
                    // Default to SQLite
                    sb.AppendLine($"__cmd__.CommandText = \"SELECT last_insert_rowid()\";");
                }

                sb.AppendLine($"var __lastId__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}) ?? 0L);");
                sb.AppendLine($"var __batchCount__ = __batch__.Count();");
                sb.AppendLine($"var __firstId__ = __lastId__ - __batchCount__ + 1;");
            }

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
    /// Generate batch UPDATE code with CASE WHEN logic
    /// </summary>
    private static void GenerateBatchUpdateCode(
        IndentedStringBuilder sb,
        string sql,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string connectionName,
        INamedTypeSymbol classSymbol,
        SqlDefine dialect,
        string cancellationTokenArg = "")
    {
        // Extract table name from marker
        var marker = "__RUNTIME_BATCH_UPDATE_";
        var startIndex = sql.IndexOf(marker);
        if (startIndex < 0) return;

        var endIndex = sql.IndexOf("__", startIndex + marker.Length);
        var tableName = sql.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length);

        // Find the entities parameter
        var entitiesParam = method.Parameters.FirstOrDefault(p => 
            SharedCodeGenerationUtilities.IsEnumerableParameter(p));
        if (entitiesParam == null) return;

        // Infer entity type from IEnumerable<T> parameter if not provided
        if (entityType == null)
        {
            var paramType = entitiesParam.Type as INamedTypeSymbol;
            if (paramType != null && paramType.TypeArguments.Length > 0)
            {
                entityType = paramType.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        if (entityType == null) return;

        // Get properties to update (exclude Id)
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && 
                       p.Name != "EqualityContract" && p.Name != "Id")
            .ToList();

        // Create command
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"int __totalAffected__ = 0;");
        sb.AppendLine();

        // Check for empty collection
        sb.AppendLine($"if ({entitiesParam.Name} == null || !{entitiesParam.Name}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return 0;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Build UPDATE with CASE WHEN for each column
        sb.AppendLine("var __sqlBuilder__ = new global::System.Text.StringBuilder();");
        sb.AppendLine($"__sqlBuilder__.Append(\"UPDATE {tableName} SET \");");
        sb.AppendLine();

        // Generate CASE WHEN for each property
        sb.AppendLine($"// Generate CASE WHEN for each column");
        sb.AppendLine($"var __setClauses__ = new global::System.Collections.Generic.List<string>();");
        
        foreach (var prop in properties)
        {
            var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
            
            // For PostgreSQL, add ELSE clause to preserve type information and avoid type inference issues
            if (dialect.DatabaseType == "PostgreSql")
            {
                sb.AppendLine($"__setClauses__.Add(\"{snakeName} = CASE \" + ");
                sb.PushIndent();
                sb.AppendLine($"string.Join(\" \", {entitiesParam.Name}.Select((e, i) => $\"WHEN id = @id{{i}} THEN @{snakeName}{{i}}\")) + ");
                sb.AppendLine($"\" ELSE {snakeName} END\");");
                sb.PopIndent();
            }
            else
            {
                sb.AppendLine($"__setClauses__.Add(\"{snakeName} = CASE \" + ");
                sb.PushIndent();
                sb.AppendLine($"string.Join(\" \", {entitiesParam.Name}.Select((e, i) => $\"WHEN id = @id{{i}} THEN @{snakeName}{{i}}\")) + ");
                sb.AppendLine($"\" END\");");
                sb.PopIndent();
            }
        }

        sb.AppendLine();
        sb.AppendLine("__sqlBuilder__.Append(string.Join(\", \", __setClauses__));");
        sb.AppendLine();

        // Add WHERE clause with all IDs
        sb.AppendLine($"__sqlBuilder__.Append(\" WHERE id IN (\");");
        sb.AppendLine($"__sqlBuilder__.Append(string.Join(\", \", {entitiesParam.Name}.Select((e, i) => $\"@id{{i}}\")));");
        sb.AppendLine($"__sqlBuilder__.Append(\")\");");
        sb.AppendLine();

        sb.AppendLine("__cmd__.CommandText = __sqlBuilder__.ToString();");
        sb.AppendLine();

        // Bind parameters
        sb.AppendLine($"int __index__ = 0;");
        sb.AppendLine($"foreach (var __entity__ in {entitiesParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Add Id parameter
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
        sb.AppendLine("__p__.ParameterName = $\"@id{__index__}\";");
        sb.AppendLine("__p__.Value = __entity__.Id;");
        sb.AppendLine("__cmd__.Parameters.Add(__p__);");
        sb.PopIndent();
        sb.AppendLine("}");

        // Add property parameters
        foreach (var prop in properties)
        {
            var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
            sb.AppendLine($"__p__.ParameterName = $\"@{snakeName}{{__index__}}\";");
            
            // Handle nullable types
            if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated || 
                prop.Type.IsReferenceType)
            {
                sb.AppendLine($"__p__.Value = __entity__.{prop.Name} ?? (object)global::System.DBNull.Value;");
            }
            else
            {
                sb.AppendLine($"__p__.Value = __entity__.{prop.Name};");
            }
            
            sb.AppendLine("__cmd__.Parameters.Add(__p__);");
            sb.PopIndent();
            sb.AppendLine("}");
        }

        sb.AppendLine("__index__++;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute
        sb.AppendLine($"__totalAffected__ = await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine();

        // Dispose and return
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return __totalAffected__;");

        // Close method body
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generate single entity UPSERT code (database-specific)
    /// </summary>
    private static void GenerateUpsertCode(
        IndentedStringBuilder sb,
        string sql,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string connectionName,
        INamedTypeSymbol classSymbol,
        string cancellationTokenArg = "",
        string dbDialect = "")
    {
        // Extract table name from marker
        var marker = "__RUNTIME_UPSERT_";
        var startIndex = sql.IndexOf(marker);
        if (startIndex < 0) return;

        var endIndex = sql.IndexOf("__", startIndex + marker.Length);
        var tableName = sql.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length);

        // Find the entity parameter
        var entityParam = method.Parameters.FirstOrDefault(p => 
            p.Type.Equals(entityType, SymbolEqualityComparer.Default));
        if (entityParam == null) return;

        if (entityType == null) return;

        // Get all properties (including Id for upsert)
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract")
            .ToList();

        // Get properties excluding Id for UPDATE clause
        var updateProperties = properties.Where(p => p.Name != "Id").ToList();

        // Get dialect provider to properly quote column names
        var dialectProvider = Core.DialectHelper.GetDialectProvider(Core.DialectHelper.GetDialectFromRepositoryFor(classSymbol));
        
        // Create command
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Build database-specific UPSERT SQL
        sb.AppendLine($"// Database-specific UPSERT for {dbDialect}");
        
        if (dbDialect == "SqlServer")
        {
            // SQL Server uses MERGE statement
            // MERGE INTO target USING (VALUES (...)) AS source (...) ON target.id = source.id
            // WHEN MATCHED THEN UPDATE SET ...
            // WHEN NOT MATCHED THEN INSERT (...) VALUES (...);
            
            var quotedColumnNames = string.Join(", ", properties.Select(p => 
                dialectProvider.SqlDefine.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))));
            
            var paramPlaceholders = string.Join(", ", properties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                return $"@{snakeName}";
            }));
            
            // Build UPDATE SET clause (exclude Id)
            var updateSetClauses = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                return $"{quotedColumn} = source.{quotedColumn}";
            }));
            
            // Build INSERT column list and VALUES list (exclude Id for IDENTITY columns)
            var insertColumns = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                return dialectProvider.SqlDefine.WrapColumn(snakeName);
            }));
            
            var insertValues = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                return $"source.{quotedColumn}";
            }));
            
            var escapedColumnNames = quotedColumnNames.Replace("\"", "\"\"");
            var escapedUpdateSet = updateSetClauses.Replace("\"", "\"\"");
            var escapedInsertColumns = insertColumns.Replace("\"", "\"\"");
            var escapedInsertValues = insertValues.Replace("\"", "\"\"");
            
            sb.AppendLine($"__cmd__.CommandText = @\"MERGE INTO {tableName} AS target USING (VALUES ({paramPlaceholders})) AS source ({escapedColumnNames}) ON target.[id] = source.[id] WHEN MATCHED THEN UPDATE SET {escapedUpdateSet} WHEN NOT MATCHED THEN INSERT ({escapedInsertColumns}) VALUES ({escapedInsertValues});\";");
        }
        else
        {
            // For other databases, this shouldn't be reached as they use template-based upsert
            sb.AppendLine($"__cmd__.CommandText = \"{sql}\";");
        }
        
        sb.AppendLine();

        // Bind parameters
        foreach (var prop in properties)
        {
            var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
            sb.AppendLine($"__p__.ParameterName = \"@{snakeName}\";");
            
            if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated || 
                prop.Type.IsReferenceType)
            {
                sb.AppendLine($"__p__.Value = {entityParam.Name}.{prop.Name} ?? (object)global::System.DBNull.Value;");
            }
            else
            {
                sb.AppendLine($"__p__.Value = {entityParam.Name}.{prop.Name};");
            }
            
            sb.AppendLine("__cmd__.Parameters.Add(__p__);");
            sb.PopIndent();
            sb.AppendLine("}");
        }
        sb.AppendLine();

        // Execute
        sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");
        sb.AppendLine();

        // Dispose and return
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return __result__;");

        // Close method body
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generate batch UPSERT code (database-specific)
    /// </summary>
    private static void GenerateBatchUpsertCode(
        IndentedStringBuilder sb,
        string sql,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string connectionName,
        INamedTypeSymbol classSymbol,
        string cancellationTokenArg = "",
        string dbDialect = "")
    {
        // Extract table name from marker
        var marker = "__RUNTIME_BATCH_UPSERT_";
        var startIndex = sql.IndexOf(marker);
        if (startIndex < 0) return;

        var endIndex = sql.IndexOf("__", startIndex + marker.Length);
        var tableName = sql.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length);

        // Find the entities parameter
        var entitiesParam = method.Parameters.FirstOrDefault(p => 
            SharedCodeGenerationUtilities.IsEnumerableParameter(p));
        if (entitiesParam == null) return;

        // Infer entity type from IEnumerable<T> parameter if not provided
        if (entityType == null)
        {
            var paramType = entitiesParam.Type as INamedTypeSymbol;
            if (paramType != null && paramType.TypeArguments.Length > 0)
            {
                entityType = paramType.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        if (entityType == null) return;

        // Get all properties (including Id for upsert)
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract")
            .ToList();

        // Get properties excluding Id for UPDATE clause
        var updateProperties = properties.Where(p => p.Name != "Id").ToList();

        // Create command
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"int __totalAffected__ = 0;");
        sb.AppendLine();

        // Check for empty collection
        sb.AppendLine($"if ({entitiesParam.Name} == null || !{entitiesParam.Name}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return 0;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Chunk batches
        sb.AppendLine($"var __batches__ = {entitiesParam.Name}.Chunk(500);");
        sb.AppendLine();

        sb.AppendLine("foreach (var __batch__ in __batches__)");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine();

        // Build VALUES clause
        sb.AppendLine("var __valuesClauses__ = new global::System.Collections.Generic.List<string>();");
        sb.AppendLine("int __itemIndex__ = 0;");
        sb.AppendLine("foreach (var __item__ in __batch__)");
        sb.AppendLine("{");
        sb.PushIndent();

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

        // Build database-specific UPSERT SQL
        // Get dialect provider to properly quote column names
        var dialectProvider = Core.DialectHelper.GetDialectProvider(Core.DialectHelper.GetDialectFromRepositoryFor(classSymbol));
        var quotedColumnNames = string.Join(", ", properties.Select(p => 
            dialectProvider.SqlDefine.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))));
        
        // Generate database-specific UPSERT
        sb.AppendLine($"// Database-specific UPSERT for {dbDialect}");
        
        // Build the SQL string - escape quotes for verbatim string literal by doubling them
        // Note: For verbatim strings (@"..."), only double quotes need escaping, not backticks
        var escapedColumnNames = quotedColumnNames.Replace("\"", "\"\"");
        var upsertSql = $"INSERT INTO {tableName} ({escapedColumnNames}) VALUES __RUNTIME_BATCH_VALUES_{entitiesParam.Name}__";
        
        // Add conflict resolution based on dialect
        if (dbDialect == "PostgreSql")
        {
            var updateSetClauses = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                var escapedQuotedColumn = quotedColumn.Replace("\"", "\"\"");
                return $"{escapedQuotedColumn} = EXCLUDED.{escapedQuotedColumn}";
            }));
            upsertSql += $" ON CONFLICT (id) DO UPDATE SET {updateSetClauses}";
        }
        else if (dbDialect == "MySql")
        {
            var updateSetClauses = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                // For verbatim strings, only double quotes need escaping, not backticks
                var escapedQuotedColumn = quotedColumn.Replace("\"", "\"\"");
                return $"{escapedQuotedColumn} = VALUES({escapedQuotedColumn})";
            }));
            upsertSql += $" ON DUPLICATE KEY UPDATE {updateSetClauses}";
        }
        else if (dbDialect == "SQLite")
        {
            // SQLite uses INSERT OR REPLACE
            upsertSql = $"INSERT OR REPLACE INTO {tableName} ({escapedColumnNames}) VALUES __RUNTIME_BATCH_VALUES_{entitiesParam.Name}__";
        }
        else if (dbDialect == "SqlServer")
        {
            // SQL Server uses MERGE statement
            // MERGE INTO target USING (VALUES ...) AS source (...) ON ... WHEN MATCHED ... WHEN NOT MATCHED ...
            
            // Build UPDATE SET clause (exclude Id)
            var updateSetClauses = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                // For verbatim strings, only double quotes need escaping, not square brackets
                var escapedQuotedColumn = quotedColumn.Replace("\"", "\"\"");
                return $"{escapedQuotedColumn} = source.{escapedQuotedColumn}";
            }));
            
            // Build INSERT column list and VALUES list (exclude Id for IDENTITY columns)
            var insertColumns = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                return quotedColumn.Replace("\"", "\"\"");
            }));
            
            var insertValues = string.Join(", ", updateProperties.Select(p =>
            {
                var snakeName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
                var quotedColumn = dialectProvider.SqlDefine.WrapColumn(snakeName);
                return $"source.{quotedColumn.Replace("\"", "\"\"")}";
            }));
            
            upsertSql = $"MERGE INTO {tableName} AS target USING (VALUES __RUNTIME_BATCH_VALUES_{entitiesParam.Name}__) AS source ({escapedColumnNames}) ON target.[id] = source.[id] WHEN MATCHED THEN UPDATE SET {updateSetClauses} WHEN NOT MATCHED THEN INSERT ({insertColumns}) VALUES ({insertValues});";
        }
        
        // Generate the code that builds the SQL at runtime
        sb.AppendLine($"var __sql__ = @\"{upsertSql}\";");
        sb.AppendLine($"__sql__ = __sql__.Replace(\"__RUNTIME_BATCH_VALUES_{entitiesParam.Name}__\", __values__);");
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
            
            if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated || 
                prop.Type.IsReferenceType)
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
        sb.AppendLine($"__totalAffected__ += await __cmd__.ExecuteNonQueryAsync({cancellationTokenArg.TrimStart(',', ' ')});");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Dispose and return
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return __totalAffected__;");

        // Close method body
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generate batch EXISTS code
    /// </summary>
    private static void GenerateBatchExistsCode(
        IndentedStringBuilder sb,
        string sql,
        IMethodSymbol method,
        string connectionName,
        INamedTypeSymbol classSymbol,
        string cancellationTokenArg = "")
    {
        // Find the ids parameter
        var idsParam = method.Parameters.FirstOrDefault(p => 
            p.Name == "ids" && SharedCodeGenerationUtilities.IsEnumerableParameter(p));
        if (idsParam == null) return;

        // Get table name from method's containing type (repository)
        var repositoryForAttr = classSymbol?.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryForAttribute" || a.AttributeClass?.Name == "RepositoryFor");
        
        string tableName = "users"; // Default fallback
        if (repositoryForAttr != null)
        {
            // Try to extract entity type from RepositoryFor attribute
            INamedTypeSymbol? entityType = null;
            if (repositoryForAttr.AttributeClass is INamedTypeSymbol attrClass && attrClass.IsGenericType)
            {
                // Generic version: RepositoryFor<TService>
                var typeArg = attrClass.TypeArguments.FirstOrDefault();
                if (typeArg is INamedTypeSymbol serviceType && serviceType.IsGenericType)
                {
                    // Extract TEntity from ICrudRepository<TEntity, TKey> or IBatchRepository<TEntity, TKey>
                    entityType = serviceType.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
                }
            }
            else if (repositoryForAttr.ConstructorArguments.Length > 0)
            {
                // Non-generic version: RepositoryFor(typeof(TService))
                var typeArg = repositoryForAttr.ConstructorArguments[0];
                if (typeArg.Value is INamedTypeSymbol serviceType && serviceType.IsGenericType)
                {
                    // Extract TEntity from ICrudRepository<TEntity, TKey> or IBatchRepository<TEntity, TKey>
                    entityType = serviceType.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
                }
            }
            
            if (entityType != null)
            {
                // Check for [TableName] attribute first
                var tableNameAttr = entityType.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "TableNameAttribute" || a.AttributeClass?.Name == "TableName");
                if (tableNameAttr != null && tableNameAttr.ConstructorArguments.Length > 0)
                {
                    tableName = tableNameAttr.ConstructorArguments[0].Value?.ToString() ?? SharedCodeGenerationUtilities.ConvertToSnakeCase(entityType.Name);
                }
                else
                {
                    tableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(entityType.Name);
                }
            }
        }

        // Create command
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"__result__ = new global::System.Collections.Generic.List<bool>();");
        sb.AppendLine();

        // Check for empty collection
        sb.AppendLine($"if ({idsParam.Name} == null || !{idsParam.Name}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return __result__;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // For each ID, check if it exists
        sb.AppendLine($"foreach (var __id__ in {idsParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM {tableName} WHERE id = @id\";");
        sb.AppendLine();

        sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
        sb.AppendLine("__p__.ParameterName = \"@id\";");
        sb.AppendLine("__p__.Value = __id__;");
        sb.AppendLine("__cmd__.Parameters.Add(__p__);");
        sb.AppendLine();

        sb.AppendLine($"var __count__ = Convert.ToInt32(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}));");
        sb.AppendLine("__result__.Add(__count__ > 0);");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Dispose and return
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine("return __result__;");

        // Close method body
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generate GET PAGE code (COUNT + SELECT)
    /// </summary>
    private static void GenerateGetPageCode(
        IndentedStringBuilder sb,
        string sql,
        IMethodSymbol method,
        INamedTypeSymbol? entityType,
        string connectionName,
        INamedTypeSymbol classSymbol,
        string cancellationTokenArg = "")
    {
        // Extract table name from marker
        var marker = "__RUNTIME_GET_PAGE_";
        var startIndex = sql.IndexOf(marker);
        if (startIndex < 0) return;

        var endIndex = sql.IndexOf("__", startIndex + marker.Length);
        var tableName = sql.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length);

        // Extract entity type from method return type if not provided
        // GetPageAsync returns Task<PagedResult<TEntity>>
        if (entityType == null && method.ReturnType is INamedTypeSymbol returnType)
        {
            // Extract TEntity from Task<PagedResult<TEntity>>
            if (returnType.Name == "Task" && returnType.TypeArguments.Length > 0)
            {
                var pagedResultType = returnType.TypeArguments[0] as INamedTypeSymbol;
                if (pagedResultType != null && pagedResultType.Name == "PagedResult" && pagedResultType.TypeArguments.Length > 0)
                {
                    entityType = pagedResultType.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }

        // Find parameters
        var pageNumberParam = method.Parameters.FirstOrDefault(p => p.Name == "pageNumber");
        var pageSizeParam = method.Parameters.FirstOrDefault(p => p.Name == "pageSize");
        var orderByParam = method.Parameters.FirstOrDefault(p => p.Name == "orderBy");

        if (pageNumberParam == null || pageSizeParam == null)
        {
            sb.AppendLine("// Error: GetPageAsync requires pageNumber and pageSize parameters");
            sb.AppendLine("return default;");
            sb.PopIndent();
            sb.AppendLine("}");
            return;
        }

        // Get database dialect for proper SQL syntax
        var dbDialect = GetDatabaseDialect(classSymbol);
        var isPostgreSQL = dbDialect == "PostgreSql" || dbDialect == "1";
        var isSqlServer = dbDialect == "SqlServer" || dbDialect == "2";

        // Create command
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Calculate offset from pageNumber and pageSize
        sb.AppendLine($"// Calculate offset from pageNumber and pageSize");
        sb.AppendLine($"var __offset__ = ({pageNumberParam.Name} - 1) * {pageSizeParam.Name};");
        sb.AppendLine();

        // Execute COUNT query
        sb.AppendLine($"// Step 1: Get total count");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM {tableName}\";");
        sb.AppendLine($"var __totalCount__ = Convert.ToInt64(await __cmd__.ExecuteScalarAsync({cancellationTokenArg.TrimStart(',', ' ')}));");
        sb.AppendLine();

        // Build SELECT query with proper ORDER BY and pagination
        sb.AppendLine($"// Step 2: Get page data");
        
        // Get columns from entity type
        var columns = "*";
        if (entityType != null)
        {
            var properties = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract" && !p.IsImplicitlyDeclared)
                .ToList();
            
            if (properties.Any())
            {
                columns = string.Join(", ", properties.Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)));
            }
        }

        // Build ORDER BY clause
        if (orderByParam != null)
        {
            sb.AppendLine($"var __orderByClause__ = string.IsNullOrWhiteSpace({orderByParam.Name}) ? \"id\" : {orderByParam.Name};");
        }
        else
        {
            sb.AppendLine($"var __orderByClause__ = \"id\";");
        }

        // Build SQL based on dialect
        if (isSqlServer)
        {
            // SQL Server uses OFFSET...FETCH
            sb.AppendLine($"__cmd__.CommandText = $\"SELECT {columns} FROM {tableName} ORDER BY {{__orderByClause__}} OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY\";");
        }
        else
        {
            // MySQL, PostgreSQL, SQLite use LIMIT...OFFSET
            sb.AppendLine($"__cmd__.CommandText = $\"SELECT {columns} FROM {tableName} ORDER BY {{__orderByClause__}} LIMIT @limit OFFSET @offset\";");
        }
        sb.AppendLine();

        // Add parameters
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
        sb.AppendLine("__p__.ParameterName = \"@offset\";");
        sb.AppendLine("__p__.Value = __offset__;");
        sb.AppendLine("__cmd__.Parameters.Add(__p__);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
        sb.AppendLine("__p__.ParameterName = \"@limit\";");
        sb.AppendLine($"__p__.Value = {pageSizeParam.Name};");
        sb.AppendLine("__cmd__.Parameters.Add(__p__);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute reader and map results
        var entityTypeName = entityType?.ToDisplayString() ?? "object";
        sb.AppendLine($"var __items__ = new global::System.Collections.Generic.List<{entityTypeName}>();");
        sb.AppendLine($"using (var __reader__ = await __cmd__.ExecuteReaderAsync({cancellationTokenArg.TrimStart(',', ' ')}))");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("while (await __reader__.ReadAsync())");
        sb.AppendLine("{");
        sb.PushIndent();
        
        // Generate entity mapping code
        if (entityType != null)
        {
            sb.AppendLine($"var __entity__ = new {entityTypeName}();");
            
            var properties = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.SetMethod != null && p.Name != "EqualityContract" && !p.IsImplicitlyDeclared)
                .ToList();
            
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(prop.Name);
                var propType = prop.Type.ToDisplayString();
                
                // Handle nullable types
                var isNullable = prop.Type.NullableAnnotation == NullableAnnotation.Annotated || 
                                prop.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                
                // 🔧 Fix: Use Convert methods for numeric types to handle database type differences (e.g., SQLite Int64 → Int32)
                var convertMethod = GetConvertMethod(prop.Type);
                
                if (isNullable)
                {
                    if (convertMethod != null)
                    {
                        sb.AppendLine($"__entity__.{prop.Name} = __reader__.IsDBNull({i}) ? default : ({propType}){convertMethod}(__reader__.GetValue({i}));");
                    }
                    else
                    {
                        sb.AppendLine($"__entity__.{prop.Name} = __reader__.IsDBNull({i}) ? default : ({propType})__reader__.GetValue({i});");
                    }
                }
                else
                {
                    if (convertMethod != null)
                    {
                        sb.AppendLine($"__entity__.{prop.Name} = ({propType}){convertMethod}(__reader__.GetValue({i}));");
                    }
                    else
                    {
                        sb.AppendLine($"__entity__.{prop.Name} = ({propType})__reader__.GetValue({i});");
                    }
                }
            }
            
            sb.AppendLine("__items__.Add(__entity__);");
        }
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Calculate total pages
        sb.AppendLine();

        // Return PagedResult
        sb.AppendLine("__cmd__?.Dispose();");
        sb.AppendLine($"__result__ = new global::Sqlx.PagedResult<{entityTypeName}>");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("Items = __items__,");
        sb.AppendLine($"PageNumber = {pageNumberParam.Name},");
        sb.AppendLine($"PageSize = {pageSizeParam.Name},");
        sb.AppendLine("TotalCount = __totalCount__");
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine("return __result__;");

        // Close method body
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Gets the appropriate Convert method for a type to handle database type differences.
    /// For example, SQLite returns Int64 for integers, but C# entities may use Int32.
    /// </summary>
    private static string? GetConvertMethod(ITypeSymbol type)
    {
        // Get the underlying type if nullable
        var underlyingType = type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? ((INamedTypeSymbol)type).TypeArguments[0]
            : type;

        return underlyingType.SpecialType switch
        {
            SpecialType.System_Int32 => "Convert.ToInt32",
            SpecialType.System_Int64 => "Convert.ToInt64",
            SpecialType.System_Int16 => "Convert.ToInt16",
            SpecialType.System_Byte => "Convert.ToByte",
            SpecialType.System_Boolean => "Convert.ToBoolean",
            SpecialType.System_Decimal => "Convert.ToDecimal",
            SpecialType.System_Double => "Convert.ToDouble",
            SpecialType.System_Single => "Convert.ToSingle",
            SpecialType.System_DateTime => "Convert.ToDateTime",
            _ => null // For strings, objects, and other types, use direct cast
        };
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

        var dialect = SqlDefine.MySql;

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
            .Select(p => dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))));

        sb.AppendLine($"// SELECT complete entity");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT {columns} FROM {tableName} WHERE {dialect.WrapColumn("id")} = @lastId\";");
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

        var dialect = SqlDefine.SQLite;

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
            .Select(p => dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))));

        sb.AppendLine($"// SELECT complete entity");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT {columns} FROM {tableName} WHERE {dialect.WrapColumn("id")} = @lastId\";");
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

        var dialect = SqlDefine.Oracle;

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
            .Select(p => dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name))));

        sb.AppendLine($"// SELECT complete entity");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT {columns} FROM {tableName} WHERE {dialect.WrapColumn("id")} = :insertedId\";");
        sb.AppendLine("__cmd__.Parameters.Clear();");
        sb.AppendLine("{ var __p__ = __cmd__.CreateParameter(); __p__.ParameterName = \":insertedId\"; __p__.Value = __insertedId__; __cmd__.Parameters.Add(__p__); }");
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
    /// 生成列序号缓存变量的声明（初始化为-1）
    /// </summary>
    private void GenerateOrdinalCachingDeclarations(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract" && !p.IsImplicitlyDeclared)
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
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "EqualityContract" && !p.IsImplicitlyDeclared)
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

    /// <summary>
    /// Checks if the SQL statement starts with the specified keyword (e.g., SELECT, INSERT, UPDATE, DELETE).
    /// This avoids false positives when the keyword appears in column names like "updated_at".
    /// </summary>
    /// <param name="sql">The SQL statement to check.</param>
    /// <param name="keyword">The SQL keyword to look for (e.g., "UPDATE", "INSERT").</param>
    /// <returns>True if the SQL statement starts with the keyword, false otherwise.</returns>
    private static bool IsSqlStatementType(string sql, string keyword)
    {
        if (string.IsNullOrWhiteSpace(sql) || string.IsNullOrWhiteSpace(keyword))
            return false;

        // Trim leading whitespace and check if the SQL starts with the keyword followed by whitespace or end
        var trimmedSql = sql.TrimStart();
        if (trimmedSql.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            // Make sure the keyword is followed by whitespace or end of string (not part of another word)
            if (trimmedSql.Length == keyword.Length)
                return true;

            var charAfterKeyword = trimmedSql[keyword.Length];
            return char.IsWhiteSpace(charAfterKeyword) || charAfterKeyword == '(' || charAfterKeyword == '[';
        }

        return false;
    }

    /// <summary>
    /// Converts dialect string to SqlDefine instance
    /// </summary>
    private static SqlDefine ConvertDialectStringToSqlDefine(string dialectString)
    {
        return dialectString switch
        {
            "MySql" => SqlDefine.MySql,
            "SqlServer" => SqlDefine.SqlServer,
            "PostgreSql" => SqlDefine.PostgreSql,
            "SQLite" => SqlDefine.SQLite,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            _ => SqlDefine.SqlServer // Default fallback
        };
    }

}
