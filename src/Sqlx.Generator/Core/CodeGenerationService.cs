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
        var tableName = context.TableName;
        var operationGenerator = context.OperationGenerator;
        var attributeHandler = context.AttributeHandler;
        var methodAnalyzer = context.MethodAnalyzer;

        try
        {
            var analysis = methodAnalyzer.AnalyzeMethod(method);
            var methodName = method.Name;
            var returnType = method.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", method.Parameters.Select(p =>
                $"{p.Type.ToDisplayString()} {p.Name}"));

            // Generate method documentation
            GenerateMethodDocumentation(sb, method);

            // Generate or copy Sqlx attributes
            attributeHandler.GenerateOrCopyAttributes(sb, method, entityType, tableName);

            // Generate method signature (no async modifier for repository methods since we use synchronous IDbConnection)
            sb.AppendLine($"public {returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            // Generate method variables
            GenerateMethodVariables(sb, method);

            // Generate try block for error handling
            sb.AppendLine("try");
            sb.AppendLine("{");
            sb.PushIndent();

            // Generate operation using the appropriate generator (always use non-async for repository methods)
            var operationContext = new OperationGenerationContext(
                sb, method, entityType, tableName, false, methodName);
            operationGenerator.GenerateOperation(operationContext);

            // Return result if not void
            if (!method.ReturnsVoid)
            {
                // For repository methods, always wrap in Task.FromResult since they implement async interfaces
                sb.AppendLine("return global::System.Threading.Tasks.Task.FromResult(__repoResult__);");
            }

            // Close try block and add catch/finally
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("catch (System.Exception)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("throw;");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("finally");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("__repoCmd__?.Dispose();");
            sb.PopIndent();
            sb.AppendLine("}");

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

    /// <inheritdoc/>
    public void GenerateMethodVariables(IndentedStringBuilder sb, IMethodSymbol method)
    {
        // Generate common variables used in repository methods
        sb.AppendLine("long __repoStartTime__ = System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("global::System.Data.IDbCommand? __repoCmd__ = null;");

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

            sb.AppendLine($"{actualReturnType} __repoResult__ = default!;");
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
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Sqlx.Annotations;");
        sb.AppendLine();

        // Generate partial class
        sb.AppendLine($"partial class {repositoryClass.Name}");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate connection field if needed
        GenerateDbConnectionFieldIfNeeded(sb, repositoryClass);

        // Generate repository methods
        foreach (var method in serviceInterface.GetMembers().OfType<IMethodSymbol>())
        {
            var operationGenerator = context.OperationFactory.GetGenerator(method);
            if (operationGenerator != null)
            {
                var methodContext = new RepositoryMethodContext(
                    sb, method, entityType, tableName, operationGenerator,
                    context.AttributeHandler, context.MethodAnalyzer);

                GenerateRepositoryMethod(methodContext);
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

    public void GenerateInterceptorMethods(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called before executing a repository operation.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("partial void OnExecuting(string operationName, global::System.Data.IDbCommand command);");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Called after executing a repository operation.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks);");
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
                if (TypeAnalyzer.IsCollectionType(innerType))
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
            .FirstOrDefault(f => IsCommonConnectionFieldName(f.Name));
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // 2. Check properties
        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsDbConnection() || IsCommonConnectionFieldName(p.Name));
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

    /// <summary>
    /// Checks if the field name matches common connection field naming patterns.
    /// </summary>
    /// <param name="fieldName">The field name to check.</param>
    /// <returns>True if it's a common connection field name.</returns>
    private static bool IsCommonConnectionFieldName(string fieldName)
    {
        return fieldName == "connection" || 
               fieldName == "_connection" || 
               fieldName == "Connection" || 
               fieldName == "_Connection" ||
               fieldName.EndsWith("Connection", StringComparison.OrdinalIgnoreCase);
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
}
