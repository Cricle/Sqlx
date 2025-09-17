// -----------------------------------------------------------------------
// <copyright file="CodeGenerationService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
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

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"GenerateRepositoryImplementation: Starting for {repositoryClass.Name}");
#endif

        // Skip if the class has SqlTemplate attribute
        if (repositoryClass.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SqlTemplate"))
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Skipping {repositoryClass.Name} because it has SqlTemplate attribute");
#endif
            return;
        }

        // Get the service interface from RepositoryFor attribute
        var serviceInterface = GetServiceInterface(context);
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"GetServiceInterface returned: {serviceInterface?.Name ?? "null"}");
#endif
        if (serviceInterface == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Returning early because serviceInterface is null");
#endif
            return;
        }

        var entityType = context.TypeInferenceService.InferEntityTypeFromServiceInterface(serviceInterface);
        var tableName = context.TypeInferenceService.GetTableName(repositoryClass, serviceInterface, context.TableNameAttributeSymbol);

        var sb = new IndentedStringBuilder(string.Empty);

        // Generate the repository implementation
        GenerateRepositoryClass(sb, context, serviceInterface, entityType, tableName);

        // Add source to compilation
        var sourceText = SourceText.From(sb.ToString().Trim(), Encoding.UTF8);
        var fileName = $"{repositoryClass.ToDisplayString().Replace(".", "_")}.Repository.g.cs";
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Adding source file: {fileName}");
        System.Diagnostics.Debug.WriteLine($"Source text length: {sourceText.Length}");
        System.Diagnostics.Debug.WriteLine($"Source text preview: {sourceText.ToString().Substring(0, System.Math.Min(200, sourceText.Length))}...");
#endif
        context.ExecutionContext.AddSource(fileName, sourceText);
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Successfully added source file: {fileName}");
#endif
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"GetServiceInterface: Looking for RepositoryForAttribute on {context.RepositoryClass.Name}");
#endif
        var repositoryForAttr = context.RepositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"RepositoryForAttribute found: {repositoryForAttr != null}");
        if (repositoryForAttr != null)
        {
            System.Diagnostics.Debug.WriteLine($"Constructor arguments count: {repositoryForAttr.ConstructorArguments.Length}");
            for (int i = 0; i < repositoryForAttr.ConstructorArguments.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"  Arg {i}: {repositoryForAttr.ConstructorArguments[i].Value} (Type: {repositoryForAttr.ConstructorArguments[i].Type})");
            }
        }
#endif

        if (repositoryForAttr?.ConstructorArguments.Length > 0)
        {
            var typeArg = repositoryForAttr.ConstructorArguments[0];
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Type argument value: {typeArg.Value}");
            System.Diagnostics.Debug.WriteLine($"Type argument is INamedTypeSymbol: {typeArg.Value is INamedTypeSymbol}");
#endif
            if (typeArg.Value is INamedTypeSymbol serviceType)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Returning service type: {serviceType.Name}");
#endif
                return serviceType;
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Falling back to type inference");
#endif
        // Fallback to type inference
        var result = context.TypeInferenceService.GetServiceInterfaceFromSyntax(
            context.RepositoryClass, context.ExecutionContext.Compilation);
            
        if (result == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Type inference also failed, trying syntax-based parsing");
#endif
            // Last resort: parse the syntax directly
            result = GetServiceInterfaceFromSyntax(context);
        }
        
        return result;
    }

    private INamedTypeSymbol? GetServiceInterfaceFromSyntax(RepositoryGenerationContext context)
    {
        try
        {
            var repositoryClass = context.RepositoryClass;
            var compilation = context.ExecutionContext.Compilation;
            
            // Get the syntax node for the repository class
            var syntaxReferences = repositoryClass.DeclaringSyntaxReferences;
            if (syntaxReferences.Length == 0) return null;
            
            var syntaxNode = syntaxReferences[0].GetSyntax();
            if (syntaxNode is not Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDeclaration)
                return null;
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Parsing syntax for class: {classDeclaration.Identifier.Text}");
#endif
            
            // Look for RepositoryFor attribute in the syntax
            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Found attribute: {attributeName}");
#endif
                    
                    if (attributeName == "RepositoryFor" || attributeName == "RepositoryForAttribute")
                    {
                        // Look for typeof(InterfaceName) in the arguments
                        if (attribute.ArgumentList?.Arguments.Count > 0)
                        {
                            var firstArg = attribute.ArgumentList.Arguments[0];
                            var argText = firstArg.ToString();
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"Attribute argument: {argText}");
#endif
                            
                            // Parse typeof(InterfaceName) pattern
                            if (argText.StartsWith("typeof(") && argText.EndsWith(")"))
                            {
                                var interfaceName = argText.Substring(7, argText.Length - 8); // Remove "typeof(" and ")"
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"Extracted interface name: {interfaceName}");
#endif
                                
                                // Try to find the interface type in the compilation
                                var interfaceType = compilation.GetTypeByMetadataName(interfaceName);
                                if (interfaceType == null)
                                {
                                    // Try with the current namespace
                                    var currentNamespace = repositoryClass.ContainingNamespace.ToDisplayString();
                                    interfaceType = compilation.GetTypeByMetadataName($"{currentNamespace}.{interfaceName}");
                                }
                                
                                if (interfaceType != null)
                                {
#if DEBUG
                                    System.Diagnostics.Debug.WriteLine($"Found interface type: {interfaceType.Name}");
#endif
                                    return interfaceType;
                                }
                            }
                        }
                    }
                }
            }
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"No interface found in syntax parsing");
#endif
            return null;
        }
        catch (System.Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error in syntax parsing: {ex.Message}");
#endif
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

    private void GenerateInterceptorMethods(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass)
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
        // Look for existing connection field/property
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.Type.AllInterfaces.Any(i => i.Name == "IDbConnection") || 
                                f.Type.Name == "IDbConnection");
                                
        if (connectionField != null)
        {
            return connectionField.Name;
        }
        
        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Type.AllInterfaces.Any(i => i.Name == "IDbConnection") || 
                                p.Type.Name == "IDbConnection");
                                
        if (connectionProperty != null)
        {
            return connectionProperty.Name;
        }
        
        return "_connection";
    }

    private bool HasDbConnectionField(INamedTypeSymbol repositoryClass)
    {
        return repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .Any(f => f.Type.AllInterfaces.Any(i => i.Name == "IDbConnection") || 
                     f.Type.Name == "IDbConnection") ||
               repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.Type.AllInterfaces.Any(i => i.Name == "IDbConnection") || 
                     p.Type.Name == "IDbConnection");
    }
}
