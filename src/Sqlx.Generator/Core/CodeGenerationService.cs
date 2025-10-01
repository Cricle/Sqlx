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

            // Generate method signature
            var returnType = method.ReturnType.ToDisplayString();
            var methodName = method.Name;
            var parameters = string.Join(", ", method.Parameters.Select(p =>
                $"{p.Type.ToDisplayString()} {p.Name}"));

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
        var fileName = $"{repositoryClass.ToDisplayString().Replace(".", "_")}.Repository.g.cs";
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
            // Show original template
            if (!string.IsNullOrEmpty(originalTemplate))
            {
                sb.AppendLine("/// <para>📝 Original Template:</para>");
                sb.AppendLine($"/// <code>{System.Security.SecurityElement.Escape(originalTemplate)}</code>");
            }

            // Show processed SQL
            if (!string.IsNullOrEmpty(templateResult.ProcessedSql))
            {
                sb.AppendLine("/// <para>📋 Generated SQL (Template Processed):</para>");
                sb.AppendLine($"/// <code>{System.Security.SecurityElement.Escape(templateResult.ProcessedSql)}</code>");
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
        var namespaceName = repositoryClass.ContainingNamespace.ToDisplayString();

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
        var returnTypeString = returnType.ToDisplayString();
        var resultVariableType = ExtractInnerTypeFromTask(returnTypeString);
        var operationName = method.Name;

        // Generate method variables
        sb.AppendLine($"{resultVariableType} __result__ = default!;");
        sb.AppendLine("global::System.Data.IDbCommand? __cmd__ = null;");
        sb.AppendLine("var __startTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
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

        // Call OnExecuting interceptor
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
        sb.AppendLine("var __endTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");

        // Call OnExecuted interceptor
        sb.AppendLine($"OnExecuted(\"{operationName}\", __cmd__, __result__, global::System.Diagnostics.Stopwatch.GetElapsedTime(__startTimestamp__, __endTimestamp__).Ticks);");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (global::System.Exception __ex__)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __failTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine($"OnExecuteFail(\"{operationName}\", __cmd__, __ex__, global::System.Diagnostics.Stopwatch.GetElapsedTime(__startTimestamp__, __failTimestamp__).Ticks);");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
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
        if (innerType == "int" || innerType == "bool" || innerType == "decimal" || innerType == "double" || innerType == "string")
            return (ReturnTypeCategory.Scalar, innerType);

        // 检查集合类型
        if (innerType.Contains("List<") || innerType.Contains("IEnumerable<") || innerType.Contains("[]"))
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

    private void GenerateFallbackMethodImplementation(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();

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
        // 性能优化：一次性获取所有成员，避免重复遍历
        var allMembers = repositoryClass.GetMembers().ToList();

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
        method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true ||
                                                  a.AttributeClass?.Name?.Contains("SqlTemplate") == true);

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
        // 简化：从接口的泛型参数中获取实体类型
        if (serviceInterface.TypeArguments.Length > 0)
            return serviceInterface.TypeArguments[0] as INamedTypeSymbol;
        return null;
    }

    private string GetTableNameFromType(INamedTypeSymbol repositoryClass, INamedTypeSymbol? entityType)
    {
        // 简化：使用实体类型名作为表名，如果没有则使用repository类名
        return entityType?.Name ?? repositoryClass.Name.Replace("Repository", "");
    }
}
