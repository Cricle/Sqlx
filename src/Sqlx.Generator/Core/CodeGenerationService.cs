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

namespace Sqlx.Generator.Core;

/// <summary>
/// Default implementation of code generation service.
/// </summary>
public class CodeGenerationService : ICodeGenerationService
{
    /// <inheritdoc/>
    public void GenerateRepositoryMethod(RepositoryMethodContext context)
    {
        var sb = context.StringBuilder;
        var method = context.Method;
        var entityType = context.EntityType;
        var processedSql = context.ProcessedSql;
        var attributeHandler = context.AttributeHandler;
        var methodAnalyzer = context.MethodAnalyzer;

        try
        {
            // Process SQL template first to get the resolved SQL for documentation
            var sqlxAttr = method.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true ||
                                    a.AttributeClass?.Name?.Contains("SqlTemplate") == true);
            
            string? resolvedSql = null;
            if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sqlTemplate)
            {
                // Use template engine to process SQL template
                var templateEngine = new SqlTemplateEngine();
                var templateResult = templateEngine.ProcessTemplate(sqlTemplate, method, entityType, context.TableName);
                resolvedSql = templateResult.ProcessedSql;
            }

            // Generate method documentation with resolved SQL
            GenerateMethodDocumentationWithSql(sb, method, resolvedSql);

            // Generate or copy Sqlx attributes
            attributeHandler.GenerateOrCopyAttributes(sb, method, entityType, context.TableName);

            // Generate method signature
            var returnType = method.ReturnType.ToDisplayString();
            var methodName = method.Name;
            var parameters = string.Join(", ", method.Parameters.Select(p =>
                $"{p.Type.ToDisplayString()} {p.Name}"));

            sb.AppendLine($"public {returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            // Generate operation using template engine approach
            if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sqlTemplate2)
            {
                // Use template engine to process SQL template
                var templateEngine = new SqlTemplateEngine();
                var templateResult = templateEngine.ProcessTemplate(sqlTemplate2, method, entityType, context.TableName);
                
                // Generate actual database execution using shared utilities
                GenerateActualDatabaseExecution(sb, method, templateResult, entityType);
            }
            else
            {
                // Check if this method has SqlTemplate attribute - if so, report error instead of fallback
                var sqlTemplateAttr = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("SqlTemplate") == true);
                    
                if (sqlTemplateAttr != null)
                {
                    // Report compilation error for SqlTemplate methods that can't be generated
                    throw new InvalidOperationException($"Failed to generate implementation for method '{method.Name}' with SqlTemplateAttribute. Please check the SQL template syntax and parameters.");
                }
                
                // Fallback for methods without proper SQL templates
                GenerateFallbackMethodImplementation(sb, method);
            }

            // Return result if not void
            if (!method.ReturnsVoid)
            {
                // For repository methods, always wrap in Task.FromResult since they implement async interfaces
                sb.AppendLine("return global::System.Threading.Tasks.Task.FromResult(__result__);");
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
        if (repositoryClass.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SqlTemplate"))
            return;

        // Get the service interface from RepositoryFor attribute
        var serviceInterface = GetServiceInterface(context);
        if (serviceInterface == null)
            return;

        var entityType = context.TypeInferenceService.InferEntityTypeFromServiceInterface(serviceInterface);
        var tableName = context.TypeInferenceService.GetTableName(repositoryClass, serviceInterface, context.TableNameAttributeSymbol);

        var sb = new IndentedStringBuilder(string.Empty);

        // Generate the repository implementation
        GenerateRepositoryClass(sb, context, serviceInterface, entityType, tableName);

        // Add source to compilation
        var sourceText = SourceText.From(sb.ToString().Trim(), Encoding.UTF8);
        var fileName = $"{repositoryClass.ToDisplayString().Replace(".", "_")}.Repository.g.cs";
        context.ExecutionContext.AddSource(fileName, sourceText);
    }

    /// <inheritdoc/>
    public void GenerateMethodDocumentation(IndentedStringBuilder sb, IMethodSymbol method)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {GetMethodDescription(method)}");
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

    /// <summary>
    /// 生成包含解析后SQL的方法文档
    /// </summary>
    public void GenerateMethodDocumentationWithSql(IndentedStringBuilder sb, IMethodSymbol method, string? processedSql)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {GetMethodDescription(method)}");
        
        // 如果有解析后的SQL，添加到注释中
        if (!string.IsNullOrEmpty(processedSql))
        {
            sb.AppendLine("/// <para>Generated SQL:</para>");
            sb.AppendLine($"/// <code>{System.Security.SecurityElement.Escape(processedSql)}</code>");
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

    /// <inheritdoc/>
    public void GenerateMethodVariables(IndentedStringBuilder sb, IMethodSymbol method)
    {
        // Generate common variables used in repository methods
        sb.AppendLine("long __startTime__ = System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("global::System.Data.IDbCommand? __cmd__ = null;");

        if (!method.ReturnsVoid)
        {
            // For async methods (Task<T>), declare the inner type T
            var returnType = method.ReturnType.ToDisplayString();
            var actualReturnType = returnType;

            // Check if this is a Task<T> type and get the inner type
            if (method.ReturnType is INamedTypeSymbol namedType &&
                namedType.Name == "Task" &&
                namedType.TypeArguments.Length == 1)
            {
                actualReturnType = namedType.TypeArguments[0].ToDisplayString();
            }

            sb.AppendLine($"{actualReturnType} __result__ = default!;");
        }

        sb.AppendLine();
    }

    private INamedTypeSymbol? GetServiceInterface(RepositoryGenerationContext context)
    {
        var repositoryForAttr = context.RepositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");

        if (repositoryForAttr?.ConstructorArguments.Length > 0)
        {
            var typeArg = repositoryForAttr.ConstructorArguments[0];
            if (typeArg.Value is INamedTypeSymbol serviceType)
                return serviceType;
        }

        // Fallback to type inference
        var result = context.TypeInferenceService.GetServiceInterfaceFromSyntax(
            context.RepositoryClass, context.ExecutionContext.Compilation);

        // Last resort: parse the syntax directly
        return result ?? GetServiceInterfaceFromSyntax(context);
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
                                    compilation.GetTypeByMetadataName($"{repositoryClass.ContainingNamespace.ToDisplayString()}.{interfaceName}");

                                if (interfaceType != null)
                                    return interfaceType;
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void GenerateRepositoryClass(IndentedStringBuilder sb, RepositoryGenerationContext context,
        INamedTypeSymbol serviceInterface, INamedTypeSymbol? entityType, string tableName)
    {
        var repositoryClass = context.RepositoryClass;
        var namespaceName = repositoryClass.ContainingNamespace.ToDisplayString();

        // Generate namespace and usings
        // Generate namespace and usings using shared utility
        SharedCodeGenerationUtilities.GenerateFileHeader(sb, namespaceName);
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
            var sqlxAttr = method.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true ||
                                    a.AttributeClass?.Name?.Contains("SqlTemplate") == true);
            
            if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sql)
            {
                // Use template engine to process SQL template
                var templateEngine = context.TemplateEngine;
                var templateResult = templateEngine.ProcessTemplate(sql, method, entityType, tableName);
                
                var methodContext = new RepositoryMethodContext(
                    sb, method, entityType, tableName, templateResult.ProcessedSql,
                    context.AttributeHandler, context.MethodAnalyzer);

                GenerateRepositoryMethod(methodContext);
            }
            else
            {
                // Check if this method has SqlTemplate attribute - if so, report error instead of fallback
                var sqlTemplateAttr = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("SqlTemplate") == true);
                    
                if (sqlTemplateAttr != null)
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

    private void GenerateDbConnectionFieldIfNeeded(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass)
    {
        if (!HasDbConnectionField(repositoryClass))
        {
            var connectionFieldName = GetDbConnectionFieldName(repositoryClass);
            sb.AppendLine($"private readonly global::System.Data.IDbConnection {connectionFieldName};");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// 为指定的仓储类生成拦截器方法，包括执行前后的回调方法。
    /// </summary>
    /// <param name="sb">用于构建代码的字符串构建器。</param>
    /// <param name="repositoryClass">要为其生成拦截器方法的仓储类符号。</param>
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
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", method.Parameters.Select(p =>
            $"{p.Type.ToDisplayString()} {p.Name}"));

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

        if (TypeAnalyzer.IsCollectionType(returnType))
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


    private string GetDbConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        // Find the first DbConnection field, property, or constructor parameter

        // 1. Check fields (prioritize type checking, fallback to common names)
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.IsDbConnection());
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // Check by common field names if type checking didn't work
        connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.Name == "connection" ||
                                f.Name == "_connection" ||
                                f.Name == "Connection" ||
                                f.Name == "_Connection" ||
                                f.Name.EndsWith("Connection", StringComparison.OrdinalIgnoreCase));
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // 2. Check properties
        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsDbConnection() || 
                                p.Name == "connection" ||
                                p.Name == "_connection" ||
                                p.Name == "Connection" ||
                                p.Name == "_Connection" ||
                                p.Name.EndsWith("Connection", StringComparison.OrdinalIgnoreCase));
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


    private bool HasDbConnectionField(INamedTypeSymbol repositoryClass)
    {
        // Check for existing fields and properties
        var hasConnectionMember = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => f.IsDbConnection()) ||
            repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.IsDbConnection());

        if (hasConnectionMember)
            return true;

        // Check for primary constructor parameter with DbConnection
        var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
        if (primaryConstructor != null)
        {
            var hasConnectionParam = primaryConstructor.Parameters.Any(p => p.IsDbConnection());
            if (hasConnectionParam)
                return true;
        }

        // Also check regular constructors for backward compatibility
        var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
        if (constructor != null)
        {
            return constructor.Parameters.Any(p => p.IsDbConnection());
        }

        return false;
    }

    private void GenerateActualDatabaseExecution(IndentedStringBuilder sb, IMethodSymbol method, SqlTemplateResult templateResult, INamedTypeSymbol? entityType)
    {
        var returnType = method.ReturnType;
        var returnTypeString = returnType.ToDisplayString();
        var resultVariableType = ExtractInnerTypeFromTask(returnTypeString);
        var operationName = method.Name;
        
        // Generate method variables
        sb.AppendLine($"{resultVariableType} __result__ = default!;");
        sb.AppendLine("global::System.Data.IDbCommand? __cmd__ = null;");
        sb.AppendLine("var __stopwatch__ = global::System.Diagnostics.Stopwatch.StartNew();");
        sb.AppendLine();

        // Use shared utilities for database setup
        sb.AppendLine("if (_connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("_connection.Open();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        SharedCodeGenerationUtilities.GenerateCommandSetup(sb, templateResult.ProcessedSql, method);
        
        // Add try-catch block with interceptors
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();
        
        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{operationName}\", __cmd__);");
        sb.AppendLine();
        
        // Execute query based on return type
        if (IsScalarReturnType(returnTypeString))
        {
            GenerateScalarExecution(sb, returnTypeString);
        }
        else if (IsCollectionReturnType(returnTypeString))
        {
            GenerateCollectionExecution(sb, returnTypeString, entityType);
        }
        else if (IsSingleEntityReturnType(returnTypeString))
        {
            GenerateSingleEntityExecution(sb, returnTypeString, entityType);
        }
        else
        {
            // Non-query execution (INSERT, UPDATE, DELETE)
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }
        
        sb.AppendLine();
        sb.AppendLine("__stopwatch__.Stop();");
        
        // Call OnExecuted interceptor
        sb.AppendLine($"OnExecuted(\"{operationName}\", __cmd__, __result__, __stopwatch__.ElapsedTicks);");
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (global::System.Exception __ex__)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine("__stopwatch__.Stop();");
        sb.AppendLine($"OnExecuteFail(\"{operationName}\", __cmd__, __ex__, __stopwatch__.ElapsedTicks);");
        sb.AppendLine("throw;");
        
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private bool IsScalarReturnType(string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        return innerType == "int" || innerType == "bool" || innerType == "decimal" || innerType == "double" || innerType == "string";
    }

    private bool IsCollectionReturnType(string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        return innerType.Contains("List<") || innerType.Contains("IEnumerable<") || innerType.Contains("[]");
    }

    private bool IsSingleEntityReturnType(string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        return !IsScalarReturnType(returnType) && !IsCollectionReturnType(returnType) && !innerType.Equals("int", StringComparison.OrdinalIgnoreCase);
    }

    private void GenerateScalarExecution(IndentedStringBuilder sb, string returnType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");
        sb.AppendLine($"__result__ = scalarResult != null ? ({innerType})scalarResult : default({innerType});");
    }

    private void GenerateCollectionExecution(IndentedStringBuilder sb, string returnType, INamedTypeSymbol? entityType)
    {
        var innerType = ExtractInnerTypeFromTask(returnType);
        sb.AppendLine($"__result__ = new {innerType}();");
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("while (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        
        if (entityType != null)
        {
            GenerateEntityFromReader(sb, entityType, "item");
            sb.AppendLine("__result__.Add(item);");
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

    private string GetDbTypeForParameter(IParameterSymbol parameter)
    {
        // Use shared utility for DbType mapping
        return SharedCodeGenerationUtilities.GetDbType(parameter.Type);
    }

    private void GenerateFallbackMethodImplementation(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        
        if (!method.ReturnsVoid)
        {
            if (returnType.Contains("Task"))
            {
                var innerType = ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"__result__ = default({innerType});");
            }
            else
            {
                sb.AppendLine($"__result__ = default({returnType});");
            }
        }
    }

    private string ExtractInnerTypeFromTask(string taskType)
    {
        // Handle various Task<T> formats
        if (taskType.StartsWith("Task<") && taskType.EndsWith(">"))
        {
            return taskType.Substring(5, taskType.Length - 6);
        }
        if (taskType.StartsWith("System.Threading.Tasks.Task<") && taskType.EndsWith(">"))
        {
            return taskType.Substring(28, taskType.Length - 29);
        }

        // Fallback for complex generic types
        var startIndex = taskType.IndexOf('<');
        var endIndex = taskType.LastIndexOf('>');
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return taskType.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        return "object"; // Safe fallback
    }
}
