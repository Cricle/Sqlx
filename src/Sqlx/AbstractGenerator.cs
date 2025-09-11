// -----------------------------------------------------------------------
// <copyright file="AbstractGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sqlx.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Stored procedures generator.
/// </summary>
public abstract partial class AbstractGenerator : ISourceGenerator
{
    /// <inheritdoc/>
    public abstract void Initialize(GeneratorInitializationContext context);

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine("AbstractGenerator.Execute called");
#endif

        // Retrieve the populated receiver
        if (context.SyntaxContextReceiver is not ISqlxSyntaxReceiver receiver)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("No ISqlxSyntaxReceiver found");
#endif
            return;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"Found {receiver.Methods.Count} methods and {receiver.RepositoryClasses.Count} repository classes");
#endif

        INamedTypeSymbol? sqlxAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");
        INamedTypeSymbol? rawSqlAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.RawSqlAttribute");
        INamedTypeSymbol? expressionToSqlAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute");
        INamedTypeSymbol? sqlExecuteTypeAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlExecuteTypeAttribute");
        INamedTypeSymbol? repositoryForAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryForAttribute");
        INamedTypeSymbol? tableNameAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.TableNameAttribute");

        System.Diagnostics.Debug.WriteLine($"RepositoryForAttribute symbol: {repositoryForAttributeSymbol?.ToDisplayString() ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"TableNameAttribute symbol: {tableNameAttributeSymbol?.ToDisplayString() ?? "null"}");

        if (sqlxAttributeSymbol == null || rawSqlAttributeSymbol == null || expressionToSqlAttributeSymbol == null || sqlExecuteTypeAttributeSymbol == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Messages.SP0001, null));
            return;
        }

        // Note: repositoryForAttributeSymbol and tableNameAttributeSymbol can be null if not found,
        // but we still continue with repository generation if they are available

        var hasNullableAnnotations = context.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;

        // Group the fields by class, and generate the source for existing methods
        foreach (IGrouping<ISymbol?, IMethodSymbol> group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            var key = (INamedTypeSymbol)group.Key!;
            var ctx = new ClassGenerationContext(key, group.ToList(), sqlxAttributeSymbol);
            ctx.SetExecutionContext(context);
            var sb = new IndentedStringBuilder(string.Empty);

            if (ctx.CreateSource(sb)) context.AddSource($"{key.ToDisplayString().Replace(".", "_")}.Sql.g.cs", SourceText.From(sb.ToString().Trim(), Encoding.UTF8));
        }

        // Generate repository implementations
        System.Diagnostics.Debug.WriteLine($"Processing {receiver.RepositoryClasses.Count} repository classes");
        foreach (var repositoryClass in receiver.RepositoryClasses)
        {
            System.Diagnostics.Debug.WriteLine($"Generating repository implementation for {repositoryClass.Name}");
            GenerateRepositoryImplementation(context, repositoryClass, repositoryForAttributeSymbol, tableNameAttributeSymbol, sqlxAttributeSymbol);
        }
    }

    private void GenerateRepositoryImplementation(GeneratorExecutionContext context, INamedTypeSymbol repositoryClass,
        INamedTypeSymbol? repositoryForAttributeSymbol, INamedTypeSymbol? tableNameAttributeSymbol, INamedTypeSymbol sqlxAttributeSymbol)
    {
        System.Diagnostics.Debug.WriteLine($"=== GenerateRepositoryImplementation START for {repositoryClass.Name} ===");

        // Skip if the class has SqlTemplate attribute (as specified in requirement 4)
        if (repositoryClass.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SqlTemplate"))
        {
            return;
        }

        // Get the service interface from RepositoryFor attribute
        var repositoryForAttr = repositoryClass.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");
        System.Diagnostics.Debug.WriteLine($"RepositoryFor attribute found: {repositoryForAttr != null}");

        if (repositoryForAttr == null)
        {
            System.Diagnostics.Debug.WriteLine($"No RepositoryFor attribute found");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Constructor arguments count: {repositoryForAttr.ConstructorArguments.Length}");

        // Try to get service interface from attribute constructor arguments
        INamedTypeSymbol? serviceInterface = null;

        if (repositoryForAttr.ConstructorArguments.Length > 0)
        {
            var firstArg = repositoryForAttr.ConstructorArguments[0];
            System.Diagnostics.Debug.WriteLine($"First argument kind: {firstArg.Kind}");
            System.Diagnostics.Debug.WriteLine($"First argument value: {firstArg.Value}");
            System.Diagnostics.Debug.WriteLine($"First argument type: {firstArg.Type}");

            if (firstArg.Kind == TypedConstantKind.Type)
            {
                serviceInterface = firstArg.Value as INamedTypeSymbol;
                System.Diagnostics.Debug.WriteLine($"Got type from TypedConstantKind.Type: {serviceInterface?.Name}");
                System.Diagnostics.Debug.WriteLine($"IsGenericType: {serviceInterface?.IsGenericType}");
                System.Diagnostics.Debug.WriteLine($"TypeArguments.Length: {serviceInterface?.TypeArguments.Length}");
                System.Diagnostics.Debug.WriteLine($"TypeParameters.Length: {serviceInterface?.TypeParameters.Length}");

                // Check if we already have a constructed generic type (with type arguments)
                if (serviceInterface?.IsGenericType == true && serviceInterface.TypeArguments.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Already have constructed generic type: {serviceInterface.ToDisplayString()}");
                    // This is already a constructed generic type like IRepository<User>, use it as-is
                }
                // If it's an unbound generic type (only type parameters), we need to construct it
                else if (serviceInterface?.IsGenericType == true && serviceInterface.TypeParameters.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found generic interface: {serviceInterface.Name} with {serviceInterface.TypeParameters.Length} type parameters");
                    // For generic interfaces, we'll construct the interface with the actual type arguments from the repository class
                    serviceInterface = ResolveGenericServiceInterface(serviceInterface, repositoryClass);
                }
            }
            else if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string typeName)
            {
                System.Diagnostics.Debug.WriteLine($"Got string type name: {typeName}");
                serviceInterface = FindTypeByName(context.Compilation, typeName);
            }
        }

        // Fallback: If constructor arguments didn't work, try to parse the syntax directly
        if (serviceInterface == null)
        {
            System.Diagnostics.Debug.WriteLine($"Fallback: Attempting syntax-based type resolution");
            serviceInterface = GetServiceInterfaceFromSyntax(repositoryClass, context.Compilation);
        }
        System.Diagnostics.Debug.WriteLine($"Service interface: {serviceInterface?.Name}");

        // Skip if service type is not an interface
        if (serviceInterface?.TypeKind != TypeKind.Interface)
        {
            System.Diagnostics.Debug.WriteLine($"Service interface is not an interface or is null");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Generating repository implementation for {repositoryClass.Name} implementing {serviceInterface.Name}");
        System.Diagnostics.Debug.WriteLine($"Service interface type: {serviceInterface.GetType()}");
        System.Diagnostics.Debug.WriteLine($"Service interface kind: {serviceInterface.TypeKind}");

        try
        {
            System.Diagnostics.Debug.WriteLine($"Starting entity type inference for interface {serviceInterface.Name}");

            // Infer entity type from service interface
            var entityType = InferEntityTypeFromServiceInterface(serviceInterface);
            if (entityType == null)
            {
                System.Diagnostics.Debug.WriteLine($"Could not infer entity type from interface {serviceInterface.Name}, using interface name as fallback");
                // Use a fallback approach: try to find an entity type based on interface name
                entityType = TryInferEntityFromInterfaceName(serviceInterface, context.Compilation);
            }

            if (entityType == null)
            {
                System.Diagnostics.Debug.WriteLine($"Still could not infer entity type, generating repository anyway with basic table name");
                // Continue generation with a generic entity (we'll use the interface name)
            }

            System.Diagnostics.Debug.WriteLine($"Inferred entity type: {entityType?.Name ?? "null"}");

            // Get table name - prioritize entity's TableName attribute
            var tableName = entityType != null ? GetTableNameFromEntity(entityType, tableNameAttributeSymbol)
                                               : GetTableNameFromInterfaceName(serviceInterface.Name);
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = GetTableName(repositoryClass, serviceInterface, tableNameAttributeSymbol);
            }

            System.Diagnostics.Debug.WriteLine($"Using table name: {tableName}");

            var sb = new IndentedStringBuilder(string.Empty);

            // Generate the complete repository implementation
            GenerateFullRepositoryImplementation(sb, repositoryClass, serviceInterface, entityType, tableName);

            var fileName = $"{repositoryClass.Name}.Repository.g.cs";
            var sourceCode = sb.ToString();
            System.Diagnostics.Debug.WriteLine($"Generated repository implementation:");
            System.Diagnostics.Debug.WriteLine(sourceCode);
            System.Diagnostics.Debug.WriteLine($"Adding source file: {fileName}");

            context.AddSource(fileName, SourceText.From(sourceCode, Encoding.UTF8));
            System.Diagnostics.Debug.WriteLine("Successfully added repository source file");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in repository generation: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            // Generate a fallback implementation to prevent compilation errors
            try
            {
                var sb = new IndentedStringBuilder(string.Empty);
                sb.AppendLine("// <auto-generated>");
                sb.AppendLine("// Fallback implementation due to generation error");
                sb.AppendLine($"// Error: {ex.Message}");
                sb.AppendLine("// </auto-generated>");
                sb.AppendLine();
                sb.AppendLine($"namespace {repositoryClass.ContainingNamespace.ToDisplayString()};");
                sb.AppendLine();
                sb.AppendLine($"partial class {repositoryClass.Name} : {serviceInterface.ToDisplayString()}");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("// Error occurred during code generation");
                sb.PopIndent();
                sb.AppendLine("}");

                context.AddSource($"{repositoryClass.Name}.Error.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Fallback generation also failed: {fallbackEx.Message}");
            }
        }
    }

    private string GetTableName(INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceType, INamedTypeSymbol? tableNameAttributeSymbol)
    {
        // Check for TableName attribute on the repository class
        var tableNameAttr = repositoryClass.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute");
        if (tableNameAttr != null && tableNameAttr.ConstructorArguments.Length > 0)
        {
            return tableNameAttr.ConstructorArguments[0].Value?.ToString() ?? serviceType.Name;
        }

        // Check for TableName attribute on the service type
        tableNameAttr = serviceType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute");
        if (tableNameAttr != null && tableNameAttr.ConstructorArguments.Length > 0)
        {
            return tableNameAttr.ConstructorArguments[0].Value?.ToString() ?? serviceType.Name;
        }

        // Default to service type name
        return serviceType.Name;
    }

    private static INamedTypeSymbol? FindTypeByName(Compilation compilation, string typeName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Searching for type: {typeName}");

            // Search in all assemblies in the compilation - look for any type (class, interface, etc.)
            var allTypes = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Found {allTypes.Count} types matching '{typeName}'");
            foreach (var type in allTypes)
            {
                System.Diagnostics.Debug.WriteLine($"  - {type.Name} ({type.TypeKind}) in namespace {type.ContainingNamespace?.ToDisplayString()}");
            }

            return allTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finding type '{typeName}': {ex.Message}");
            return null;
        }
    }

    private INamedTypeSymbol? GetServiceInterfaceFromSyntax(INamedTypeSymbol repositoryClass, Compilation compilation)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Parsing syntax for repository class: {repositoryClass.Name}");

            // Get the syntax tree and semantic model
            var syntaxTrees = compilation.SyntaxTrees;
            foreach (var syntaxTree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();

                // Find class declarations that match our repository class
                var classDeclarations = root.DescendantNodes()
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                    .Where(cls => cls.Identifier.ValueText == repositoryClass.Name);

                foreach (var classDecl in classDeclarations)
                {
                    System.Diagnostics.Debug.WriteLine($"Found class declaration for: {classDecl.Identifier.ValueText}");

                    // Find RepositoryFor attributes on this specific class
                    var attributeLists = classDecl.AttributeLists;
                    foreach (var attributeList in attributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            if (attribute.Name.ToString().Contains("RepositoryFor"))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found RepositoryFor attribute on {classDecl.Identifier.ValueText}: {attribute}");

                                if (attribute.ArgumentList?.Arguments.Count > 0)
                                {
                                    var firstArg = attribute.ArgumentList.Arguments[0];
                                    System.Diagnostics.Debug.WriteLine($"First argument syntax: {firstArg}");

                                    // Look for typeof(InterfaceName) pattern
                                    if (firstArg.Expression.ToString().StartsWith("typeof(") &&
                                        firstArg.Expression.ToString().EndsWith(")"))
                                    {
                                        var typeExpression = firstArg.Expression.ToString();
                                        var typeName = typeExpression.Substring(7, typeExpression.Length - 8); // Remove "typeof(" and ")"
                                        System.Diagnostics.Debug.WriteLine($"Extracted type name from syntax: {typeName}");

                                        // Find the interface by name
                                        var interfaceSymbol = FindInterfaceByName(compilation, typeName);
                                        if (interfaceSymbol != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Successfully resolved interface: {interfaceSymbol.Name} for class {repositoryClass.Name}");
                                            return interfaceSymbol;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("Could not find service interface from syntax");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing syntax: {ex.Message}");
            return null;
        }
    }

    private INamedTypeSymbol? FindInterfaceByName(Compilation compilation, string interfaceName)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Searching for interface: {interfaceName}");

            // Handle generic interfaces like IGenericService<User>
            if (interfaceName.Contains("<") && interfaceName.Contains(">"))
            {
                // Extract base name (e.g., "IGenericService" from "IGenericService<User>")
                var baseName = interfaceName.Substring(0, interfaceName.IndexOf('<'));
                var typeArgString = interfaceName.Substring(interfaceName.IndexOf('<') + 1, interfaceName.LastIndexOf('>') - interfaceName.IndexOf('<') - 1);

                System.Diagnostics.Debug.WriteLine($"Looking for generic interface base: {baseName} with type argument: {typeArgString}");

                // Find all interfaces with the base name
                var baseInterfaces = compilation.GetSymbolsWithName(baseName, SymbolFilter.Type)
                    .OfType<INamedTypeSymbol>()
                    .Where(t => t.TypeKind == TypeKind.Interface && t.IsGenericType)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {baseInterfaces.Count} generic interfaces with base name '{baseName}'");

                if (baseInterfaces.Any())
                {
                    var baseInterface = baseInterfaces.First();

                    // Try to find the type argument in the compilation
                    var typeArgSymbol = FindTypeByName(compilation, typeArgString);
                    if (typeArgSymbol != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found type argument: {typeArgSymbol.Name}");

                        // Construct the generic interface with the specific type argument
                        var constructedInterface = baseInterface.Construct(typeArgSymbol);
                        System.Diagnostics.Debug.WriteLine($"Constructed generic interface: {constructedInterface.ToDisplayString()}");
                        return constructedInterface;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not find type argument '{typeArgString}', returning base interface");
                        return baseInterface;
                    }
                }
            }
            else
            {
                // Search for the interface in all symbol tables
                var interfaces = compilation.GetSymbolsWithName(interfaceName, SymbolFilter.Type)
                    .OfType<INamedTypeSymbol>()
                    .Where(t => t.TypeKind == TypeKind.Interface)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Found {interfaces.Count} interfaces with name '{interfaceName}'");

                return interfaces.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error finding interface '{interfaceName}': {ex.Message}");
            return null;
        }
    }

    private INamedTypeSymbol? InferEntityTypeFromServiceInterface(INamedTypeSymbol serviceInterface)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== InferEntityTypeFromServiceInterface START ===");
            System.Diagnostics.Debug.WriteLine($"Service interface: {serviceInterface.Name}");
            System.Diagnostics.Debug.WriteLine($"Is generic: {serviceInterface.IsGenericType}");

            // For generic interfaces, check if we have type arguments (already constructed)
            if (serviceInterface.IsGenericType && serviceInterface.TypeArguments.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Found {serviceInterface.TypeArguments.Length} type arguments");
                for (int i = 0; i < serviceInterface.TypeArguments.Length; i++)
                {
                    var typeArg = serviceInterface.TypeArguments[i];
                    System.Diagnostics.Debug.WriteLine($"Type argument {i}: {typeArg.Name} ({typeArg.ToDisplayString()})");

                    // Assume the first type argument is the entity type for repositories
                    if (i == 0 && TypeAnalyzer.IsLikelyEntityType(typeArg))
                    {
                        System.Diagnostics.Debug.WriteLine($"Using first type argument as entity type: {typeArg.Name}");
                        return typeArg as INamedTypeSymbol;
                    }
                }
            }

            var methods = serviceInterface.GetMembers().OfType<IMethodSymbol>().ToArray();
            System.Diagnostics.Debug.WriteLine($"Found {methods.Length} methods in interface {serviceInterface.Name}");

            var candidateTypes = new Dictionary<INamedTypeSymbol, int>();

            foreach (var method in methods)
            {
                AnalyzeMethodForEntityTypes(method, candidateTypes);
            }

            return SelectBestEntityCandidate(candidateTypes) ?? InferFromInterfaceName(serviceInterface);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in InferEntityTypeFromServiceInterface: {ex.Message}");
            return null;
        }
    }

    private void AnalyzeMethodForEntityTypes(IMethodSymbol method, Dictionary<INamedTypeSymbol, int> candidateTypes)
    {
        System.Diagnostics.Debug.WriteLine($"Analyzing method: {method.Name}");

        // Check return type (higher weight)
        var returnEntityType = TypeAnalyzer.ExtractEntityType(method.ReturnType);
        if (returnEntityType != null && TypeAnalyzer.IsLikelyEntityType(returnEntityType))
        {
            candidateTypes[returnEntityType] = (candidateTypes.TryGetValue(returnEntityType, out var existingCount) ? existingCount : 0) + 2;
        }

        // Check parameters (lower weight)
        foreach (var parameter in method.Parameters)
        {
            var paramEntityType = TypeAnalyzer.ExtractEntityType(parameter.Type);
            if (paramEntityType != null && TypeAnalyzer.IsLikelyEntityType(paramEntityType))
            {
                candidateTypes[paramEntityType] = (candidateTypes.TryGetValue(paramEntityType, out var existingCount2) ? existingCount2 : 0) + 1;
            }
        }
    }

    private INamedTypeSymbol? SelectBestEntityCandidate(Dictionary<INamedTypeSymbol, int> candidateTypes)
    {
        if (candidateTypes.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("No candidate entity types found");
            return null;
        }

        var bestCandidate = candidateTypes.OrderByDescending(kvp => kvp.Value).First();
        System.Diagnostics.Debug.WriteLine($"Selected entity type: {bestCandidate.Key.Name} with score {bestCandidate.Value}");
        return bestCandidate.Key;
    }

    private INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method)
    {
        System.Diagnostics.Debug.WriteLine($"Inferring entity type for method: {method.Name}");
        
        // Extract entity type from return type
        var returnEntityType = TypeAnalyzer.ExtractEntityType(method.ReturnType);
        if (returnEntityType != null && TypeAnalyzer.IsLikelyEntityType(returnEntityType))
        {
            System.Diagnostics.Debug.WriteLine($"Found entity type from return type: {returnEntityType.Name}");
            return returnEntityType;
        }

        // Check parameters for entity types
        foreach (var parameter in method.Parameters)
        {
            var paramEntityType = TypeAnalyzer.ExtractEntityType(parameter.Type);
            if (paramEntityType != null && TypeAnalyzer.IsLikelyEntityType(paramEntityType))
            {
                System.Diagnostics.Debug.WriteLine($"Found entity type from parameter {parameter.Name}: {paramEntityType.Name}");
                return paramEntityType;
            }
        }

        System.Diagnostics.Debug.WriteLine($"No specific entity type found for method: {method.Name}");
        return null;
    }

    private string? GetTableNameForEntityType(INamedTypeSymbol? entityType)
    {
        if (entityType == null) return null;

        // Check for TableName attribute
        var tableNameAttr = entityType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute");
        if (tableNameAttr != null && tableNameAttr.ConstructorArguments.Length > 0)
        {
            var tableName = tableNameAttr.ConstructorArguments[0].Value?.ToString();
            if (!string.IsNullOrEmpty(tableName))
            {
                System.Diagnostics.Debug.WriteLine($"Found TableName attribute on {entityType.Name}: {tableName}");
                return tableName;
            }
        }

        // Use entity type name as default table name
        System.Diagnostics.Debug.WriteLine($"Using default table name for {entityType.Name}: {entityType.Name}");
        return entityType.Name;
    }

    private INamedTypeSymbol? InferFromInterfaceName(INamedTypeSymbol serviceInterface)
    {
        // Fallback: try to infer from interface name
        // E.g., IUserService -> User
        var interfaceName = serviceInterface.Name;
        if (interfaceName.StartsWith("I") && interfaceName.EndsWith("Service"))
        {
            var entityName = interfaceName.Substring(1, interfaceName.Length - 8); // Remove 'I' prefix and 'Service' suffix
            System.Diagnostics.Debug.WriteLine($"Trying to find entity type by name: {entityName}");

            // Look for a type with this name in the same namespace or related namespaces
            var possibleEntityType = FindTypeByName(serviceInterface, entityName);
            if (possibleEntityType != null)
            {
                System.Diagnostics.Debug.WriteLine($"Found entity type by name inference: {possibleEntityType.Name}");
                return possibleEntityType;
            }
        }

        System.Diagnostics.Debug.WriteLine($"No entity type found in interface {serviceInterface.Name}");
        return null;
    }

    private INamedTypeSymbol? FindTypeByName(INamedTypeSymbol serviceInterface, string entityName)
    {
        try
        {
            // Search in the same namespace first
            var currentNamespace = serviceInterface.ContainingNamespace;
            System.Diagnostics.Debug.WriteLine($"Searching for type '{entityName}' in namespace '{currentNamespace.ToDisplayString()}'");

            // Get all types in the current namespace
            var typesInNamespace = currentNamespace.GetTypeMembers();
            foreach (var type in typesInNamespace)
            {
                if (type.Name == entityName && TypeAnalyzer.IsLikelyEntityType(type))
                {
                    System.Diagnostics.Debug.WriteLine($"Found entity type '{entityName}' in same namespace");
                    return type;
                }
            }

            // Search in parent namespace
            if (!currentNamespace.IsGlobalNamespace)
            {
                var parentNamespace = currentNamespace.ContainingNamespace;
                var parentTypes = parentNamespace.GetTypeMembers();
                foreach (var type in parentTypes)
                {
                    if (type.Name == entityName && TypeAnalyzer.IsLikelyEntityType(type))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found entity type '{entityName}' in parent namespace");
                        return type;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Could not find entity type '{entityName}'");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in FindTypeByName: {ex.Message}");
            return null;
        }
    }

    private string GetTableNameFromEntity(INamedTypeSymbol? entityType, INamedTypeSymbol? tableNameAttributeSymbol)
    {
        try
        {
            if (entityType == null)
            {
                System.Diagnostics.Debug.WriteLine("Entity type is null, cannot get table name from entity");
                return "UnknownTable";
            }

            // Check for TableName attribute on the entity type
            var allAttributes = entityType.GetAttributes();
            System.Diagnostics.Debug.WriteLine($"Entity {entityType.Name} has {allAttributes.Length} attributes:");
            foreach (var attr in allAttributes)
            {
                System.Diagnostics.Debug.WriteLine($"  - {attr.AttributeClass?.Name} ({attr.AttributeClass?.ToDisplayString()})");
            }

            var tableNameAttr = allAttributes.FirstOrDefault(attr =>
                attr.AttributeClass?.Name == "TableNameAttribute" ||
                attr.AttributeClass?.Name == "TableName");

            if (tableNameAttr != null)
            {
                System.Diagnostics.Debug.WriteLine($"Found TableName attribute: {tableNameAttr.AttributeClass?.Name}");
                System.Diagnostics.Debug.WriteLine($"Constructor arguments count: {tableNameAttr.ConstructorArguments.Length}");

                if (tableNameAttr.ConstructorArguments.Length > 0)
                {
                    var firstArg = tableNameAttr.ConstructorArguments[0];
                    System.Diagnostics.Debug.WriteLine($"First argument kind: {firstArg.Kind}, value: {firstArg.Value}");

                    var tableName = firstArg.Value?.ToString();
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found TableName attribute on entity: {tableName}");
                        return tableName!;
                    }
                }
                else
                {
                    // Fallback: Try to extract from the original syntax reference
                    System.Diagnostics.Debug.WriteLine("Trying syntax-based TableName resolution");
                    var syntaxRef = entityType.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxRef != null)
                    {
                        var syntaxTree = syntaxRef.SyntaxTree;
                        var node = syntaxRef.GetSyntax();

                        // Find the class declaration
                        if (node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDecl)
                        {
                            foreach (var attributeList in classDecl.AttributeLists)
                            {
                                foreach (var attribute in attributeList.Attributes)
                                {
                                    if (attribute.Name.ToString().Contains("TableName"))
                                    {
                                        if (attribute.ArgumentList?.Arguments.Count > 0)
                                        {
                                            var firstArg = attribute.ArgumentList.Arguments[0];
                                            var argText = firstArg.Expression.ToString();

                                            // Remove quotes from string literal
                                            if (argText.StartsWith("\"") && argText.EndsWith("\""))
                                            {
                                                argText = argText.Substring(1, argText.Length - 2);
                                                System.Diagnostics.Debug.WriteLine($"Found TableName from syntax: {argText}");
                                                return argText;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No TableName attribute found on entity");
            }

            // Default to entity type name
            var defaultName = entityType.Name;
            System.Diagnostics.Debug.WriteLine($"Using default table name: {defaultName}");
            return defaultName;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting table name: {ex.Message}");
            return entityType?.Name ?? "UnknownTable";
        }
    }

    private void GenerateFullRepositoryImplementation(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceInterface, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            // Generate file header
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// This file was generated by Sqlx Repository Generator");
            sb.AppendLine("// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();

            sb.AppendLine("#nullable disable");
            sb.AppendLine("#pragma warning disable CS8618, CS8625, CS8629, CS8601, CS8600, CS8603, CS8669");
            sb.AppendLine();

            // Generate namespace (using traditional block syntax for compatibility)
            var namespaceName = repositoryClass.ContainingNamespace.ToDisplayString();
            if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
                sb.PushIndent();
            }

            // Generate using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Data.Common;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Sqlx.Annotations;");
            sb.AppendLine();

            // Generate partial class that implements the service interface
            sb.AppendLine($"{repositoryClass.DeclaredAccessibility.GetAccessibility()} partial class {repositoryClass.Name} : {serviceInterface.ToDisplayString()}");
            sb.AppendLine("{");
            sb.PushIndent();

            // Generate DbConnection field if needed
            var connectionFieldName = GetDbConnectionFieldName(repositoryClass);
            GenerateDbConnectionFieldIfNeeded(sb, repositoryClass, connectionFieldName);

            // Generate interceptor partial methods
            WriteRepositoryInterceptMethods(sb, repositoryClass);

            // Generate methods for ALL interface methods
            var methods = serviceInterface.GetMembers().OfType<IMethodSymbol>().ToArray();
            System.Diagnostics.Debug.WriteLine($"Found {methods.Length} methods in interface {serviceInterface.Name}");
            System.Diagnostics.Debug.WriteLine($"Generating implementations for all {methods.Length} methods");

            foreach (var method in methods)
            {
                // Infer entity type for each method individually based on its return type
                var methodEntityType = InferEntityTypeFromMethod(method) ?? entityType;
                var methodTableName = GetTableNameForEntityType(methodEntityType) ?? tableName;
                GenerateRepositoryMethod(sb, method, methodEntityType, methodTableName);
            }

            sb.PopIndent();
            sb.AppendLine("}");

            // Close namespace if we opened it
            if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
            {
                sb.PopIndent();
                sb.AppendLine("}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GenerateFullRepositoryImplementation: {ex.Message}");
            throw;
        }
    }

    private void WriteRepositoryInterceptMethods(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass)
    {
        var staticKeyword = repositoryClass.IsStatic ? "static " : string.Empty;

        sb.AppendLine("// ===================================================================");
        sb.AppendLine("// Interceptor partial methods");
        sb.AppendLine("// Implement these in your partial class to add custom logic:");
        sb.AppendLine("//");
        sb.AppendLine("// partial void OnExecuting(string methodName, DbCommand command)");
        sb.AppendLine("// {");
        sb.AppendLine("//     // Add logic before SQL execution");
        sb.AppendLine("//     // e.g., logging, parameter validation, caching");
        sb.AppendLine("// }");
        sb.AppendLine("//");
        sb.AppendLine("// partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)");
        sb.AppendLine("// {");
        sb.AppendLine("//     // Add logic after successful execution");
        sb.AppendLine("//     // e.g., result caching, metrics, logging");
        sb.AppendLine("// }");
        sb.AppendLine("//");
        sb.AppendLine("// partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)");
        sb.AppendLine("// {");
        sb.AppendLine("//     // Add logic when execution fails");
        sb.AppendLine("//     // e.g., error logging, retry logic, fallback handling");
        sb.AppendLine("// }");
        sb.AppendLine("// ===================================================================");
        sb.AppendLine();

        sb.AppendLine($"{staticKeyword}partial void OnExecuting(string methodName, DbCommand command);");
        sb.AppendLine();

        sb.AppendLine($"{staticKeyword}partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed);");
        sb.AppendLine();

        sb.AppendLine($"{staticKeyword}partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed);");
        sb.AppendLine();
    }

    private string GenerateSqlxAttribute(AttributeData attribute)
    {
        var attrName = attribute.AttributeClass?.Name ?? "Unknown";

        // Remove "Attribute" suffix if present
        if (attrName.EndsWith("Attribute"))
        {
            attrName = attrName.Substring(0, attrName.Length - "Attribute".Length);
        }

        var args = new List<string>();

        // Handle constructor arguments
        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string stringValue)
            {
                args.Add($"\"{stringValue}\"");
            }
            else if (arg.Kind == TypedConstantKind.Enum)
            {
                var enumTypeName = arg.Type?.Name ?? "Unknown";
                if (enumTypeName == "SqlExecuteTypes")
                {
                    var enumValueInt = Convert.ToInt32(arg.Value ?? 0);
                    var enumName = enumValueInt switch
                    {
                        0 => "Select",
                        1 => "Update",
                        2 => "Insert",
                        3 => "Delete",
                        4 => "BatchInsert",
                        5 => "BatchUpdate",
                        6 => "BatchDelete",
                        7 => "BatchCommand",
                        _ => arg.Value?.ToString() ?? "Unknown"
                    };
                    args.Add($"SqlExecuteTypes.{enumName}");
                }
                else
                {
                    args.Add(arg.Value?.ToString() ?? "null");
                }
            }
            else if (arg.Value != null)
            {
                args.Add(arg.Value.ToString() ?? "");
            }
        }

        return $"[{attrName}({string.Join(", ", args)})]";
    }

    private SqlDefine GetSqlDefineForRepository(IMethodSymbol method)
    {
        // Check for SqlDefine attribute on method first
        var methodSqlDefineAttr = method.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlDefineAttribute");
        if (methodSqlDefineAttr != null)
        {
            return ParseSqlDefineAttribute(methodSqlDefineAttr);
        }

        // Check for SqlDefine attribute on containing class
        var classSqlDefineAttr = method.ContainingType.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlDefineAttribute");
        if (classSqlDefineAttr != null)
        {
            return ParseSqlDefineAttribute(classSqlDefineAttr);
        }

        // Try to infer database dialect from the connection type
        var inferredDialect = InferDialectFromConnectionType(method.ContainingType);
        if (inferredDialect.HasValue)
        {
            return inferredDialect.Value;
        }

        // Default to SqlServer as fallback
        return SqlDefine.SqlServer;
    }

    private SqlDefine ParseSqlDefineAttribute(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 1)
        {
            var defineType = (int)attribute.ConstructorArguments[0].Value!;
            return defineType switch
            {
                1 => SqlDefine.SqlServer,
                2 => SqlDefine.PgSql,
                _ => SqlDefine.MySql,
            };
        }
        else if (attribute.ConstructorArguments.Length == 5)
        {
            return new SqlDefine(
                attribute.ConstructorArguments[0].Value?.ToString() ?? "[",
                attribute.ConstructorArguments[1].Value?.ToString() ?? "]",
                attribute.ConstructorArguments[2].Value?.ToString() ?? "'",
                attribute.ConstructorArguments[3].Value?.ToString() ?? "'",
                attribute.ConstructorArguments[4].Value?.ToString() ?? "@");
        }

        return SqlDefine.SqlServer;
    }

    private SqlDefine? InferDialectFromConnectionType(INamedTypeSymbol repositoryClass)
    {
        // Find DbConnection field or property in the repository class
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.IsDbConnection());
            
        if (connectionField != null)
        {
            var connectionTypeName = connectionField.Type.ToDisplayString();
            return InferDialectFromConnectionTypeName(connectionTypeName);
        }

        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(x => x.IsDbConnection());
            
        if (connectionProperty != null)
        {
            var connectionTypeName = connectionProperty.Type.ToDisplayString();
            return InferDialectFromConnectionTypeName(connectionTypeName);
        }

        // Look for constructor parameter with connection type
        var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
        if (constructor != null)
        {
            var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                var connectionTypeName = connectionParam.Type.ToDisplayString();
                return InferDialectFromConnectionTypeName(connectionTypeName);
            }
        }

        // Check base classes
        if (repositoryClass.BaseType != null && repositoryClass.BaseType.SpecialType != SpecialType.System_Object)
        {
            return InferDialectFromConnectionType(repositoryClass.BaseType);
        }

        return null;
    }

    private SqlDefine? InferDialectFromConnectionTypeName(string connectionTypeName)
    {
        return connectionTypeName.ToLowerInvariant() switch
        {
            var name when name.Contains("sqlite") => SqlDefine.SQLite,
            var name when name.Contains("mysql") || name.Contains("mariadb") => SqlDefine.MySql,
            var name when name.Contains("postgres") || name.Contains("npgsql") => SqlDefine.PgSql,
            var name when name.Contains("oracle") => SqlDefine.Oracle,
            var name when name.Contains("db2") => SqlDefine.DB2,
            var name when name.Contains("sqlserver") || name.Contains("sqlconnection") => SqlDefine.SqlServer,
            _ => null
        };
    }

    private bool IsCollectionType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            // Check for IList<T>, List<T>, IEnumerable<T>, etc.
            if (namedType.Name == "IList" || namedType.Name == "List" ||
                namedType.Name == "IEnumerable" || namedType.Name == "ICollection")
            {
                return true;
            }

            // Check for Task<IList<T>>, etc.
            if (namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                return IsCollectionType(namedType.TypeArguments[0]);
            }
        }

        return false;
    }

    private bool IsScalarReturnType(ITypeSymbol type, bool isAsync)
    {
        // Handle async methods first - unwrap Task<T>
        if (isAsync && type is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
        {
            type = namedType.TypeArguments[0];
        }

        // Check for scalar types that should use ExecuteScalar
        return type.SpecialType == SpecialType.System_Int32 ||
               type.SpecialType == SpecialType.System_Int64 ||
               type.SpecialType == SpecialType.System_Boolean ||
               type.SpecialType == SpecialType.System_Decimal ||
               type.SpecialType == SpecialType.System_Double ||
               type.SpecialType == SpecialType.System_Single;
    }

    private void GenerateRepositoryMethod(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            var methodName = method.Name;
            var returnType = method.ReturnType.ToDisplayString(); // Keep nullable annotations in method signatures
            var parameters = string.Join(", ", method.Parameters.Select(p =>
            {
                var paramStr = $"{p.Type.ToDisplayString()} {p.Name}";
                // Add default value if the parameter has one
                if (p.HasExplicitDefaultValue)
                {
                    if (p.ExplicitDefaultValue == null)
                    {
                        paramStr += " = default";
                    }
                    else
                    {
                        paramStr += $" = {p.ExplicitDefaultValue}";
                    }
                }
                else if (p.Type.Name == "CancellationToken")
                {
                    // Always add default for CancellationToken if not explicitly set
                    paramStr += " = default";
                }
                return paramStr;
            }));

            System.Diagnostics.Debug.WriteLine($"Generating method: {methodName} with return type: {returnType}");

            // Generate XML documentation
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Generated implementation of {methodName} using Sqlx.");
            sb.AppendLine("/// This method was automatically generated by the RepositoryFor source generator.");
            sb.AppendLine("/// </summary>");

            // Add parameter documentation
            foreach (var param in method.Parameters)
            {
                sb.AppendLine($"/// <param name=\"{param.Name}\">{GetParameterDescription(param)}</param>");
            }

            // Add return documentation
            if (!method.ReturnsVoid)
            {
                sb.AppendLine($"/// <returns>{GetReturnDescription(method)}</returns>");
            }

            // Generate or copy Sqlx attributes
            GenerateOrCopyAttributes(sb, method, entityType, tableName);

            // Generate complete method implementation with actual database access
            var isAsync = method.ReturnType.Name == "Task" || (method.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task");
            var asyncModifier = isAsync ? "async " : "";
            sb.AppendLine($"public {asyncModifier}{returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            // Generate high-performance, secure database operations
            GenerateOptimizedRepositoryMethodBody(sb, method, entityType, tableName, isAsync);

            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating method {method.Name}: {ex.Message}");
            // Generate a fallback method
            sb.AppendLine($"// Error generating method {method.Name}: {ex.Message}");
            sb.AppendLine($"public {method.ReturnType.ToDisplayString()} {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"))})");
            sb.AppendLine("{");
            sb.PushIndent();
            if (!method.ReturnsVoid)
            {
                var defaultValue = GetDefaultValueForReturnType(method.ReturnType);
                sb.AppendLine($"return {defaultValue};");
            }
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    private string GenerateSqlxAttribute(IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            var methodName = method.Name.ToLowerInvariant();
            var entityTypeName = entityType?.Name ?? "Entity";

            // Determine the appropriate Sqlx attribute based on method name patterns
            if (methodName.Contains("getall") || methodName.StartsWith("findall"))
            {
                return $"[Sqlx(\"SELECT * FROM {tableName}\")]";
            }
            else if (methodName.Contains("getby") || methodName.Contains("findby") || methodName.StartsWith("get") && method.Parameters.Length > 0)
            {
                var paramName = method.Parameters.FirstOrDefault()?.Name ?? "id";
                return $"[Sqlx(\"SELECT * FROM {tableName} WHERE Id = @{paramName}\")]";
            }
            else if (methodName.Contains("create") || methodName.Contains("insert") || methodName.Contains("add"))
            {
                return $"[SqlExecuteType(SqlExecuteTypes.Insert, \"{tableName}\")]";
            }
            else if (methodName.Contains("update") || methodName.Contains("modify"))
            {
                return $"[SqlExecuteType(SqlExecuteTypes.Update, \"{tableName}\")]";
            }
            else if (methodName.Contains("delete") || methodName.Contains("remove"))
            {
                return $"[SqlExecuteType(SqlExecuteTypes.Delete, \"{tableName}\")]";
            }
            else if (methodName.Contains("count"))
            {
                return $"[Sqlx(\"SELECT COUNT(*) FROM {tableName}\")]";
            }
            else if (methodName.Contains("exists"))
            {
                var paramName = method.Parameters.FirstOrDefault()?.Name ?? "id";
                return $"[Sqlx(\"SELECT COUNT(*) FROM {tableName} WHERE Id = @{paramName}\")]";
            }
            else
            {
                // Default to a SELECT query for unknown patterns
                return $"[Sqlx(\"SELECT * FROM {tableName}\")]";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating Sqlx attribute: {ex.Message}");
            return $"// Error generating Sqlx attribute: {ex.Message}";
        }
    }
    private void GenerateOptimizedRepositoryMethodBody(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        var methodName = method.Name;

        // Setup execution context with interceptors
        sb.AppendLine("var __startTime__ = System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("System.Data.Common.DbCommand? __cmd__ = null;");
        
        // Declare __result__ with correct type to avoid boxing
        var resultType = GetResultVariableType(method);
        sb.AppendLine($"{resultType} __result__ = default;");
        
        sb.AppendLine("System.Exception? __exception__ = null;");
        sb.AppendLine();

        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Check for SqlExecuteType attribute
        var executeTypeAttr = method.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (executeTypeAttr != null && executeTypeAttr.ConstructorArguments.Length >= 2)
        {
            // Parse enum value more robustly
            var enumValueObj = executeTypeAttr.ConstructorArguments[0].Value;
            var executeTypeInt = enumValueObj switch
            {
                int intValue => intValue,
                string strValue when int.TryParse(strValue, out var intVal) => intVal,
                _ => 0 // Default to Select
            };
            var table = executeTypeAttr.ConstructorArguments[1].Value?.ToString() ?? tableName;

            switch (executeTypeInt)
            {
                case 0: // Select
                    // For SqlExecuteType.Select, determine if it's collection or single based on return type
                    if (IsCollectionType(method.ReturnType) || (isAsync && IsAsyncCollectionReturnType(method.ReturnType)))
                    {
                        GenerateSelectOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    }
                    else if (IsScalarReturnType(method.ReturnType, isAsync))
                    {
                        GenerateScalarOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    }
                    else
                    {
                        GenerateSelectSingleOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    }
                    break;
                case 1: // Update
                    GenerateUpdateOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    break;
                case 2: // Insert
                    GenerateInsertOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    break;
                case 3: // Delete
                    GenerateDeleteOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    break;
                case 4: // BatchInsert
                case 5: // BatchUpdate
                case 6: // BatchDelete
                case 7: // BatchCommand
                    GenerateBatchOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName, executeTypeInt);
                    break;
                default:
                    if (IsCollectionType(method.ReturnType) || (isAsync && IsAsyncCollectionReturnType(method.ReturnType)))
                    {
                        GenerateSelectOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    }
                    else if (IsScalarReturnType(method.ReturnType, isAsync))
                    {
                        GenerateScalarOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    }
                    else
                    {
                        GenerateSelectSingleOperationWithInterceptors(sb, method, entityType, table, isAsync, methodName);
                    }
                    break;
            }
        }
        else
        {
            // Check for Sqlx attribute
            var sqlxAttr = method.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlxAttribute");
            if (sqlxAttr != null)
            {
                GenerateCustomSqlOperationWithInterceptors(sb, method, entityType, sqlxAttr, isAsync, methodName);
            }
            // Check for SqlExecuteTypeAttribute before method name patterns
            else if (method.GetAttributes().Any(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute"))
            {
                // SqlExecuteType operations are handled in GenerateOptimizedRepositoryMethodBody
                // Let them fall through to the default case which calls that method
            }
            else
            {
                // Fallback based on method name patterns
                var methodNameLower = methodName.ToLowerInvariant();
                if (methodNameLower.Contains("create") || methodNameLower.Contains("insert"))
                {
                    GenerateInsertOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
                else if (methodNameLower.Contains("update") || methodNameLower.Contains("modify"))
                {
                    GenerateUpdateOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
                else if (methodNameLower.Contains("delete") || methodNameLower.Contains("remove"))
                {
                    GenerateDeleteOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
                else if (methodNameLower.Contains("count") || methodNameLower.Contains("exists") ||
                         IsScalarReturnType(method.ReturnType, isAsync))
                {
                    GenerateScalarOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
                else if (IsCollectionType(method.ReturnType) || (isAsync && IsAsyncCollectionReturnType(method.ReturnType)))
                {
                    GenerateSelectOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
                else if (method.ReturnsVoid || (method.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task" && taskType.TypeArguments.Length == 0))
                {
                    // For void methods, generate a simple operation
                    GenerateVoidOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
                else
                {
                    GenerateSelectSingleOperationWithInterceptors(sb, method, entityType, tableName, isAsync, methodName);
                }
            }
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (System.Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__exception__ = ex;");
        sb.AppendLine("var __elapsed__ = System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__;");
        sb.AppendLine($"OnExecuteFail(\"{methodName}\", __cmd__, ex, __elapsed__);");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("finally");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("if (__exception__ == null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __elapsed__ = System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__;");
        sb.AppendLine($"OnExecuted(\"{methodName}\", __cmd__, __result__, __elapsed__);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("__cmd__?.Dispose();");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateOptimizedEntityMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        // Use the enhanced entity mapping generator that supports primary constructors and records
        Core.EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
    }

    private string GetCancellationTokenParameter(IMethodSymbol method)
    {
        var cancellationToken = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        return cancellationToken?.Name ?? "default";
    }

    private bool IsAutoGeneratedProperty(IPropertySymbol property)
    {
        // Check if this is an auto-generated property (like Id with auto-increment)
        // For now, we'll exclude properties named "Id" from INSERT operations
        return property.Name == "Id";
    }

    private List<IPropertySymbol> GetInsertableProperties(INamedTypeSymbol entityType)
    {
        // For records and primary constructors, we need to handle members differently
        if (Core.PrimaryConstructorAnalyzer.IsRecord(entityType) || Core.PrimaryConstructorAnalyzer.HasPrimaryConstructor(entityType))
        {
            var members = Core.PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType);
            var properties = new List<IPropertySymbol>();
            
            // Include all accessible members that can be mapped to database columns
            foreach (var member in members)
            {
                if (member is Core.PropertyMemberInfo propMember)
                {
                    if (propMember.Property.CanBeReferencedByName && !IsAutoGeneratedProperty(propMember.Property))
                    {
                        properties.Add(propMember.Property);
                    }
                }
                else if (member is Core.PrimaryConstructorParameterMemberInfo paramMember)
                {
                    // For primary constructor parameters without corresponding properties,
                    // we need to create a synthetic property representation
                    // For now, let's check if there's a corresponding property and include it
                    var correspondingProperty = entityType.GetMembers().OfType<IPropertySymbol>()
                        .FirstOrDefault(p => string.Equals(p.Name, member.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (correspondingProperty != null && !IsAutoGeneratedProperty(correspondingProperty))
                    {
                        properties.Add(correspondingProperty);
                    }
                }
            }
            
            return properties.Distinct().ToList();
        }
        
        return entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "Id") // Exclude Id for INSERT
            .ToList();
    }

    private List<IPropertySymbol> GetUpdatableProperties(INamedTypeSymbol entityType)
    {
        return entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "Id") // Exclude Id for UPDATE SET clause
            .ToList();
    }

    private string GetDbTypeForProperty(IPropertySymbol property)
    {
        return GetDbTypeForParameterType(property.Type);
    }

    private string GetDbTypeForParameterType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Int32 => "global::System.Data.DbType.Int32",
            SpecialType.System_Int64 => "global::System.Data.DbType.Int64",
            SpecialType.System_String => "global::System.Data.DbType.String",
            SpecialType.System_DateTime => "global::System.Data.DbType.DateTime",
            SpecialType.System_Boolean => "global::System.Data.DbType.Boolean",
            SpecialType.System_Decimal => "global::System.Data.DbType.Decimal",
            SpecialType.System_Double => "global::System.Data.DbType.Double",
            SpecialType.System_Single => "global::System.Data.DbType.Single",
            _ => "global::System.Data.DbType.Object"
        };
    }

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated;
    }

    private bool IsAsyncCollectionReturnType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
        {
            return IsCollectionType(namedType.TypeArguments[0]);
        }
        return false;
    }

    private string GetDefaultValueForReturnType(ITypeSymbol returnType)
    {
        var typeDisplayString = returnType.ToDisplayString();

        // Handle Task<T> first to avoid conflict with collection handling
        if (typeDisplayString.StartsWith("Task<") || typeDisplayString.Contains("Task<"))
        {
            // Handle Task<T> by getting the inner type and generating default for that
            if (returnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            {
                var innerType = namedType.TypeArguments[0];
                var innerDefault = GetDefaultValueForReturnType(innerType);
                return $"System.Threading.Tasks.Task.FromResult<{innerType.ToDisplayString()}>({innerDefault})";
            }
            // Fallback for when we can't determine the inner type
            return "System.Threading.Tasks.Task.FromResult<object>(null!)";
        }

        if (typeDisplayString == "Task" || typeDisplayString == "System.Threading.Tasks.Task")
        {
            return "System.Threading.Tasks.Task.CompletedTask";
        }

        // Handle collection types
        if (typeDisplayString.Contains("IList<") || typeDisplayString.Contains("List<") ||
            typeDisplayString.Contains("IEnumerable<") || typeDisplayString.Contains("[]"))
        {
            var genericTypeName = ExtractGenericTypeName(typeDisplayString);
            if (typeDisplayString.Contains("IList<"))
            {
                return $"new System.Collections.Generic.List<{genericTypeName}>()";
            }
            else if (typeDisplayString.Contains("List<"))
            {
                return $"new System.Collections.Generic.List<{genericTypeName}>()";
            }
            else if (typeDisplayString.Contains("IEnumerable<"))
            {
                return $"new System.Collections.Generic.List<{genericTypeName}>()";
            }
            else if (typeDisplayString.Contains("[]"))
            {
                var elementType = typeDisplayString.Replace("[]", "");
                return $"new {elementType}[0]";
            }
        }

        // Handle primitive types
        return typeDisplayString switch
        {
            "bool" or "System.Boolean" => "false",
            "int" or "System.Int32" => "0",
            "long" or "System.Int64" => "0L",
            "string" or "System.String" => "string.Empty",
            "void" => "",
            _ when returnType.CanBeReferencedByName && returnType.IsReferenceType => "null!",
            _ when returnType.CanBeReferencedByName => "default",
            _ => "null!"
        };
    }

    private string ExtractGenericTypeName(string typeString)
    {
        var startIndex = typeString.IndexOf('<') + 1;
        var endIndex = typeString.LastIndexOf('>');
        if (startIndex > 0 && endIndex > startIndex)
        {
            return typeString.Substring(startIndex, endIndex - startIndex);
        }
        return "object";
    }
    private static INamedTypeSymbol? TryInferEntityFromInterfaceName(INamedTypeSymbol serviceInterface, Compilation compilation)
    {
        System.Diagnostics.Debug.WriteLine($"Trying to infer entity from interface name: {serviceInterface.Name}");

        var interfaceName = serviceInterface.Name;

        // Remove 'I' prefix if present
        if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
        {
            interfaceName = interfaceName.Substring(1);
        }

        // Remove 'Service' suffix if present
        if (interfaceName.EndsWith("Service"))
        {
            interfaceName = interfaceName.Substring(0, interfaceName.Length - 7);
        }

        // Try to find an entity type with this name
        var entityType = FindTypeByName(compilation, interfaceName);
        System.Diagnostics.Debug.WriteLine($"Found entity type: {entityType?.Name ?? "null"}");

        return entityType;
    }

    private static string GetTableNameFromInterfaceName(string interfaceName)
    {
        System.Diagnostics.Debug.WriteLine($"Getting table name from interface name: {interfaceName}");

        var tableName = interfaceName;

        // Remove 'I' prefix if present
        if (tableName.StartsWith("I") && tableName.Length > 1 && char.IsUpper(tableName[1]))
        {
            tableName = tableName.Substring(1);
        }

        // Remove 'Service' suffix if present
        if (tableName.EndsWith("Service"))
        {
            tableName = tableName.Substring(0, tableName.Length - 7);
        }

        // Convert to plural form (simple heuristic)
        if (!tableName.EndsWith("s"))
        {
            tableName += "s";
        }

        System.Diagnostics.Debug.WriteLine($"Generated table name: {tableName}");
        return tableName;
    }

    // ===================================================================
    // CRUD Operations with Interceptors Support
    // ===================================================================

    private void GenerateInsertOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        var entityParam = method.Parameters.FirstOrDefault(p => p.Type.TypeKind == TypeKind.Class && p.Type.Name != "String" && p.Type.Name != "CancellationToken");

        if (entityParam == null || entityType == null)
        {
            sb.AppendLine("// Error: Unable to generate INSERT without entity parameter");
            sb.AppendLine("throw new global::System.InvalidOperationException(\"INSERT operation requires an entity parameter\");");
            return;
        }

        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Get insertable properties
        var properties = GetInsertableProperties(entityType);
        if (!properties.Any())
        {
            sb.AppendLine("// Error: No insertable properties found");
            sb.AppendLine("throw new global::System.InvalidOperationException(\"No insertable properties found in entity\");");
            return;
        }

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        var sqlDefine = GetSqlDefineForRepository(method);
        var columns = string.Join(", ", properties.Select(p => sqlDefine.WrapColumn(p.GetSqlName()).Replace("\"", "\\\"")));
        var parameters = string.Join(", ", properties.Select(p => $"{sqlDefine.ParameterPrefix}{p.GetSqlName()}"));
        sb.AppendLine($"__cmd__.CommandText = \"INSERT INTO {sqlDefine.WrapColumn(tableName).Replace("\"", "\\\"")} ({columns}) VALUES ({parameters})\";");
        sb.AppendLine();

        // Add parameters
        foreach (var prop in properties)
        {
            var sqlName = prop.GetSqlName();
            sb.AppendLine($"var param{prop.Name} = __cmd__.CreateParameter();");
            sb.AppendLine($"param{prop.Name}.ParameterName = \"{sqlDefine.ParameterPrefix}{sqlName}\";");
            sb.AppendLine($"param{prop.Name}.DbType = {GetDbTypeForProperty(prop)};");

            if (prop.Type.IsReferenceType || IsNullableValueType(prop.Type))
            {
                sb.AppendLine($"param{prop.Name}.Value = (object?){entityParam.Name}.{prop.Name} ?? global::System.DBNull.Value;");
            }
            else
            {
                sb.AppendLine($"param{prop.Name}.Value = {entityParam.Name}.{prop.Name};");
            }
            sb.AppendLine($"__cmd__.Parameters.Add(param{prop.Name});");
            sb.AppendLine();
        }

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute and return
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }

        // Determine return type and cast accordingly
        var returnType = method.ReturnType;
        if (isAsync && returnType is INamedTypeSymbol namedReturnType && namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
        {
            returnType = namedReturnType.TypeArguments[0];
        }

        if (returnType.SpecialType == SpecialType.System_Void || (returnType is INamedTypeSymbol nt && nt.Name == "Task" && nt.TypeArguments.Length == 0))
        {
            // void or Task (no return value)
        }
        else if (returnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("return System.Convert.ToInt32(__result__);");
        }
        else if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine("return System.Convert.ToInt32(__result__) > 0;");
        }
        else
        {
            sb.AppendLine("return __result__;");
        }
    }

    private void GenerateUpdateOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        var entityParam = method.Parameters.FirstOrDefault(p => p.Type.TypeKind == TypeKind.Class && p.Type.Name != "String" && p.Type.Name != "CancellationToken");

        if (entityParam == null || entityType == null)
        {
            sb.AppendLine("// Error: Unable to generate UPDATE without entity parameter");
            sb.AppendLine("throw new global::System.InvalidOperationException(\"UPDATE operation requires an entity parameter\");");
            return;
        }

        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Get updatable properties
        var properties = GetUpdatableProperties(entityType);
        if (!properties.Any())
        {
            sb.AppendLine("// Error: No updatable properties found");
            sb.AppendLine("throw new global::System.InvalidOperationException(\"No updatable properties found in entity\");");
            return;
        }

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        var sqlDefine = GetSqlDefineForRepository(method);
        var setClauses = string.Join(", ", properties.Select(p => $"{sqlDefine.WrapColumn(p.GetSqlName()).Replace("\"", "\\\"")} = {sqlDefine.ParameterPrefix}{p.GetSqlName()}"));
        sb.AppendLine($"__cmd__.CommandText = \"UPDATE {sqlDefine.WrapColumn(tableName).Replace("\"", "\\\"")} SET {setClauses} WHERE {sqlDefine.WrapColumn("id").Replace("\"", "\\\"")} = {sqlDefine.ParameterPrefix}id\";");
        sb.AppendLine();

        // Add SET clause parameters
        foreach (var prop in properties)
        {
            var sqlName = prop.GetSqlName();
            sb.AppendLine($"var param{prop.Name} = __cmd__.CreateParameter();");
            sb.AppendLine($"param{prop.Name}.ParameterName = \"{sqlDefine.ParameterPrefix}{sqlName}\";");
            sb.AppendLine($"param{prop.Name}.DbType = {GetDbTypeForProperty(prop)};");

            if (prop.Type.IsReferenceType || IsNullableValueType(prop.Type))
            {
                sb.AppendLine($"param{prop.Name}.Value = (object?){entityParam.Name}.{prop.Name} ?? global::System.DBNull.Value;");
            }
            else
            {
                sb.AppendLine($"param{prop.Name}.Value = {entityParam.Name}.{prop.Name};");
            }
            sb.AppendLine($"__cmd__.Parameters.Add(param{prop.Name});");
            sb.AppendLine();
        }

        // Add WHERE clause parameter (Id)
        var idProperty = GetIdProperty(entityType);
        if (idProperty != null)
        {
            sb.AppendLine("var paramId = __cmd__.CreateParameter();");
            sb.AppendLine($"paramId.ParameterName = \"{sqlDefine.ParameterPrefix}id\";");
            sb.AppendLine($"paramId.DbType = {GetDbTypeForProperty(idProperty)};");
            sb.AppendLine($"paramId.Value = {entityParam.Name}.{idProperty.Name};");
            sb.AppendLine("__cmd__.Parameters.Add(paramId);");
            sb.AppendLine();
        }

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute and return
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }

        // Determine return type and cast accordingly
        var returnType = method.ReturnType;
        if (isAsync && returnType is INamedTypeSymbol namedReturnType && namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
        {
            returnType = namedReturnType.TypeArguments[0];
        }

        if (returnType.SpecialType == SpecialType.System_Void || (returnType is INamedTypeSymbol nt && nt.Name == "Task" && nt.TypeArguments.Length == 0))
        {
            // void or Task (no return value)
        }
        else if (returnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("return System.Convert.ToInt32(__result__);");
        }
        else if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine("return System.Convert.ToInt32(__result__) > 0;");
        }
        else
        {
            sb.AppendLine("return __result__;");
        }
    }

    private void GenerateDeleteOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        // Check if we have an ID parameter or entity parameter
        var idParam = method.Parameters.FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                                                            p.Type.SpecialType == SpecialType.System_Int32 ||
                                                            p.Type.SpecialType == SpecialType.System_String);
        var entityParam = method.Parameters.FirstOrDefault(p => p.Type.TypeKind == TypeKind.Class && p.Type.Name != "String" && p.Type.Name != "CancellationToken");

        if (idParam != null)
        {
            // Delete by ID
            sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM [{tableName}] WHERE [Id] = @Id\";");
            sb.AppendLine("var paramId = __cmd__.CreateParameter();");
            sb.AppendLine("paramId.ParameterName = \"@Id\";");
            sb.AppendLine($"paramId.Value = {idParam.Name};");
            sb.AppendLine("__cmd__.Parameters.Add(paramId);");
        }
        else if (entityParam != null && entityType != null)
        {
            // Delete by entity (using its Id property)
            var idProperty = GetIdProperty(entityType);
            if (idProperty != null)
            {
                sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM [{tableName}] WHERE [Id] = @Id\";");
                sb.AppendLine("var paramId = __cmd__.CreateParameter();");
                sb.AppendLine("paramId.ParameterName = \"@Id\";");
                sb.AppendLine($"paramId.Value = {entityParam.Name}.{idProperty.Name};");
                sb.AppendLine("__cmd__.Parameters.Add(paramId);");
            }
            else
            {
                sb.AppendLine("// Error: Unable to find Id property for DELETE operation");
                sb.AppendLine("throw new global::System.InvalidOperationException(\"Entity must have an Id property for DELETE operation\");");
                return;
            }
        }
        else
        {
            sb.AppendLine("// Error: DELETE operation requires an ID parameter or entity with Id property");
            sb.AppendLine("throw new global::System.InvalidOperationException(\"DELETE operation requires an ID parameter or entity with Id property\");");
            return;
        }

        sb.AppendLine();

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute and return
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }

        // Determine return type and cast accordingly
        var returnType = method.ReturnType;
        if (isAsync && returnType is INamedTypeSymbol namedReturnType && namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
        {
            returnType = namedReturnType.TypeArguments[0];
        }

        if (returnType.SpecialType == SpecialType.System_Void || (returnType is INamedTypeSymbol nt && nt.Name == "Task" && nt.TypeArguments.Length == 0))
        {
            // void or Task (no return value)
        }
        else if (returnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("return System.Convert.ToInt32(__result__);");
        }
        else if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine("return System.Convert.ToInt32(__result__) > 0;");
        }
        else
        {
            sb.AppendLine("return __result__;");
        }
    }

    private void GenerateSelectOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        // Check if we have parameters for WHERE clause
        var whereParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();

        if (whereParams.Any())
        {
            var firstParam = whereParams.First();
            if (firstParam.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}] WHERE [Id] = @Id\";");
                sb.AppendLine("var paramId = __cmd__.CreateParameter();");
                sb.AppendLine("paramId.ParameterName = \"@Id\";");
                sb.AppendLine($"paramId.Value = {firstParam.Name};");
                sb.AppendLine("__cmd__.Parameters.Add(paramId);");
            }
            else
            {
                // Generic parameter handling
                sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}] WHERE [{firstParam.Name}] = @{firstParam.Name}\";");
                sb.AppendLine($"var param{firstParam.Name} = __cmd__.CreateParameter();");
                sb.AppendLine($"param{firstParam.Name}.ParameterName = \"@{firstParam.Name}\";");
                sb.AppendLine($"param{firstParam.Name}.Value = {firstParam.Name};");
                sb.AppendLine($"__cmd__.Parameters.Add(param{firstParam.Name});");
            }
        }
        else
        {
            sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}]\";");
        }

        sb.AppendLine();

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute and process results
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync();");
        }
        else
        {
            sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        }

        // Generate result collection and mapping
        var entityTypeName = entityType?.ToDisplayString() ?? "object";
        sb.AppendLine($"var results = new global::System.Collections.Generic.List<{entityTypeName}>();");
        sb.AppendLine();

        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"while (await reader.ReadAsync({cancellationToken}))");
        }
        else
        {
            sb.AppendLine("while (reader.Read())");
        }

        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateOptimizedEntityMapping(sb, entityType);
            sb.AppendLine("results.Add(entity);");
        }
        else
        {
            sb.AppendLine("// Generic object mapping when entity type is unknown");
            sb.AppendLine("var row = new global::System.Dynamic.ExpandoObject();");
            sb.AppendLine("var dict = (global::System.Collections.Generic.IDictionary<string, object>)row;");
            sb.AppendLine("for (int i = 0; i < reader.FieldCount; i++)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("results.Add((object)row);");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("__result__ = results;");

        // Cast to correct return type based on method signature
        var methodReturnType = method.ReturnType;
        if (isAsync && methodReturnType is INamedTypeSymbol namedReturnType && namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
        {
            methodReturnType = namedReturnType.TypeArguments[0];
        }

        var returnTypeName = methodReturnType.ToDisplayString();
        if (returnTypeName.Contains("IList") || returnTypeName.Contains("IEnumerable") || returnTypeName.Contains("ICollection"))
        {
            sb.AppendLine($"return ({returnTypeName})results;");
        }
        else
        {
            sb.AppendLine("return results;");
        }
    }

    private void GenerateSelectSingleOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        // Check if we have parameters for WHERE clause
        var whereParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();

        if (whereParams.Any())
        {
            var firstParam = whereParams.First();
            if (firstParam.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}] WHERE [Id] = @Id LIMIT 1\";");
                sb.AppendLine("var paramId = __cmd__.CreateParameter();");
                sb.AppendLine("paramId.ParameterName = \"@Id\";");
                sb.AppendLine($"paramId.Value = {firstParam.Name};");
                sb.AppendLine("__cmd__.Parameters.Add(paramId);");
            }
            else
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}] WHERE [{firstParam.Name}] = @{firstParam.Name} LIMIT 1\";");
                sb.AppendLine($"var param{firstParam.Name} = __cmd__.CreateParameter();");
                sb.AppendLine($"param{firstParam.Name}.ParameterName = \"@{firstParam.Name}\";");
                sb.AppendLine($"param{firstParam.Name}.Value = {firstParam.Name};");
                sb.AppendLine($"__cmd__.Parameters.Add(param{firstParam.Name});");
            }
        }
        else
        {
            sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}] LIMIT 1\";");
        }

        sb.AppendLine();

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute and process single result
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync();");
            sb.AppendLine($"if (await reader.ReadAsync({cancellationToken}))");
        }
        else
        {
            sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
            sb.AppendLine("if (reader.Read())");
        }

        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateOptimizedEntityMapping(sb, entityType);
            sb.AppendLine("__result__ = entity;");
            sb.AppendLine("return entity;");
        }
        else
        {
            sb.AppendLine("// Generic object mapping when entity type is unknown");
            sb.AppendLine("var result = new global::System.Dynamic.ExpandoObject();");
            sb.AppendLine("var dict = (global::System.Collections.Generic.IDictionary<string, object>)result;");
            sb.AppendLine("for (int i = 0; i < reader.FieldCount; i++)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("__result__ = result;");
            sb.AppendLine("return result;");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("__result__ = null;");
        sb.AppendLine("return null;");
    }

    private void GenerateVoidOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");
        sb.AppendLine($"__cmd__.CommandText = \"SELECT * FROM [{tableName}] LIMIT 1\";");
        sb.AppendLine();

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute without processing result for void methods
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync();");
            sb.AppendLine($"if (await reader.ReadAsync({cancellationToken}))");
        }
        else
        {
            sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
            sb.AppendLine("if (reader.Read())");
        }

        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// Void operation - just execute without returning result");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("__result__ = null;");
        // No return statement for void methods
    }

    private void GenerateCustomSqlOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, AttributeData sqlxAttr, bool isAsync, string methodName)
    {
        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        // Get SQL from attribute
        var sql = sqlxAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "SELECT 1";
        sb.AppendLine($"__cmd__.CommandText = \"{sql.Replace("\"", "\\\"")}\";");
        sb.AppendLine();

        // Add parameters
        foreach (var param in method.Parameters.Where(p => p.Type.Name != "CancellationToken"))
        {
            sb.AppendLine($"var param{param.Name} = __cmd__.CreateParameter();");
            sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
            sb.AppendLine($"param{param.Name}.Value = {param.Name};");
            sb.AppendLine($"__cmd__.Parameters.Add(param{param.Name});");
            sb.AppendLine();
        }

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute based on expected return type
        var returnType = method.ReturnType;
        var isCollection = IsCollectionType(returnType) || (isAsync && IsAsyncCollectionReturnType(returnType));

        if (isCollection)
        {
            // Multiple results
            if (isAsync)
            {
                var cancellationToken = GetCancellationTokenParameter(method);
                sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync();");
            }
            else
            {
                sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
            }

            var entityTypeName = entityType?.ToDisplayString() ?? "object";
            sb.AppendLine($"var results = new global::System.Collections.Generic.List<{entityTypeName}>();");
            sb.AppendLine();

            if (isAsync)
            {
                var cancellationToken = GetCancellationTokenParameter(method);
                sb.AppendLine($"while (await reader.ReadAsync({cancellationToken}))");
            }
            else
            {
                sb.AppendLine("while (reader.Read())");
            }

            sb.AppendLine("{");
            sb.PushIndent();

            if (entityType != null)
            {
                GenerateOptimizedEntityMapping(sb, entityType);
                sb.AppendLine("results.Add(entity);");
            }
            else
            {
                sb.AppendLine("// Generic object mapping when entity type is unknown");
                sb.AppendLine("var row = new global::System.Dynamic.ExpandoObject();");
                sb.AppendLine("var dict = (global::System.Collections.Generic.IDictionary<string, object>)row;");
                sb.AppendLine("for (int i = 0; i < reader.FieldCount; i++)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine("results.Add((object)row);");
            }

            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("__result__ = results;");

            // Cast to correct return type based on method signature
            var methodReturnType2 = method.ReturnType;
            if (isAsync && methodReturnType2 is INamedTypeSymbol namedReturnType2 && namedReturnType2.Name == "Task" && namedReturnType2.TypeArguments.Length == 1)
            {
                methodReturnType2 = namedReturnType2.TypeArguments[0];
            }

            var returnTypeName = methodReturnType2.ToDisplayString();
            if (returnTypeName.Contains("IList") || returnTypeName.Contains("IEnumerable") || returnTypeName.Contains("ICollection"))
            {
                sb.AppendLine($"return ({returnTypeName})results;");
            }
            else
            {
                sb.AppendLine("return results;");
            }
        }
        else
        {
            // Single result - check if it's a scalar or entity
            if (IsScalarReturnType(returnType, isAsync))
            {
                // True scalar result (int, string, etc.)
                if (isAsync)
                {
                    var cancellationToken = GetCancellationTokenParameter(method);
                    sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationToken});");
                }
                else
                {
                    sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");
                }

                // Convert scalar result to proper return type
                var unwrappedReturnType = returnType;
                if (isAsync && returnType is INamedTypeSymbol asyncReturnType && asyncReturnType.Name == "Task" && asyncReturnType.TypeArguments.Length == 1)
                {
                    unwrappedReturnType = asyncReturnType.TypeArguments[0];
                }
                
                // Direct conversion without intermediate variables for better performance
                if (unwrappedReturnType.SpecialType == SpecialType.System_Int32)
                {
                    sb.AppendLine("__result__ = scalarResult == null ? 0 : global::System.Convert.ToInt32(scalarResult);");
                    sb.AppendLine("return __result__;");
                }
                else if (unwrappedReturnType.SpecialType == SpecialType.System_Int64)
                {
                    sb.AppendLine("__result__ = scalarResult == null ? 0L : global::System.Convert.ToInt64(scalarResult);");
                    sb.AppendLine("return __result__;");
                }
                else if (unwrappedReturnType.SpecialType == SpecialType.System_Boolean)
                {
                    sb.AppendLine("__result__ = scalarResult == null ? false : global::System.Convert.ToBoolean(scalarResult);");
                    sb.AppendLine("return __result__;");
                }
                else if (unwrappedReturnType.SpecialType == SpecialType.System_Decimal)
                {
                    sb.AppendLine("__result__ = scalarResult == null ? 0m : global::System.Convert.ToDecimal(scalarResult);");
                    sb.AppendLine("return __result__;");
                }
                else if (unwrappedReturnType.SpecialType == SpecialType.System_Double)
                {
                    sb.AppendLine("__result__ = scalarResult == null ? 0.0 : global::System.Convert.ToDouble(scalarResult);");
                    sb.AppendLine("return __result__;");
                }
                else if (unwrappedReturnType.SpecialType == SpecialType.System_Single)
                {
                    sb.AppendLine("__result__ = scalarResult == null ? 0f : global::System.Convert.ToSingle(scalarResult);");
                    sb.AppendLine("return __result__;");
                }
                else if (unwrappedReturnType.SpecialType == SpecialType.System_String)
                {
                    sb.AppendLine("__result__ = scalarResult?.ToString() ?? string.Empty;");
                    sb.AppendLine("return __result__;");
                }
                else
                {
                    sb.AppendLine("__result__ = scalarResult;");
                    sb.AppendLine("return __result__;");
                }
            }
            else
            {
                // Single entity result
                if (isAsync)
                {
                    var cancellationToken = GetCancellationTokenParameter(method);
                    sb.AppendLine($"using var reader = await __cmd__.ExecuteReaderAsync();");
                    sb.AppendLine($"if (await reader.ReadAsync({cancellationToken}))");
                }
                else
                {
                    sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
                    sb.AppendLine("if (reader.Read())");
                }

                sb.AppendLine("{");
                sb.PushIndent();

                if (entityType != null)
                {
                    GenerateOptimizedEntityMapping(sb, entityType);
                    sb.AppendLine("__result__ = entity;");
                    sb.AppendLine("return entity;");
                }
                else
                {
                    sb.AppendLine("// Generic object mapping when entity type is unknown");
                    sb.AppendLine("var result = new global::System.Dynamic.ExpandoObject();");
                    sb.AppendLine("var dict = (global::System.Collections.Generic.IDictionary<string, object>)result;");
                    sb.AppendLine("for (int i = 0; i < reader.FieldCount; i++)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine("__result__ = result;");
                    sb.AppendLine("return result;");
                }

                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine();

                sb.AppendLine("__result__ = null;");
                
                // Check if this is a Task return type (void async)
                var methodReturnType = method.ReturnType;
                if (methodReturnType is INamedTypeSymbol namedReturnType && namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 0)
                {
                    // For Task (void async), don't generate a return statement
                }
                else
                {
                    sb.AppendLine("return null;");
                }
            }
        }
    }

    private IPropertySymbol? GetIdProperty(INamedTypeSymbol entityType)
    {
        // Look for property named "Id" first
        var idProperty = entityType.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (idProperty != null)
        {
            return idProperty;
        }

        // Look for property with name ending in "Id" (e.g., UserId, PersonId)
        idProperty = entityType.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

        if (idProperty != null)
        {
            return idProperty;
        }

        // Look for property with Key attribute
        idProperty = entityType.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == "KeyAttribute" ||
                attr.AttributeClass?.Name == "Key"));

        return idProperty;
    }

    private void GenerateInsertSqlForScalar(IndentedStringBuilder sb, INamedTypeSymbol entityType, string tableName, string paramName)
    {
        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null && p.DeclaredAccessibility == Accessibility.Public
                     && !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (properties.Any())
        {
            var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            sb.AppendLine($"__cmd__.CommandText = \"INSERT INTO [{tableName}] ({columns}) VALUES ({values})\";");

            foreach (var prop in properties)
            {
                sb.AppendLine($"var param{prop.Name} = __cmd__.CreateParameter();");
                sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                sb.AppendLine($"param{prop.Name}.Value = {paramName}.{prop.Name} ?? (object)DBNull.Value;");
                sb.AppendLine($"__cmd__.Parameters.Add(param{prop.Name});");
            }
        }
        else
        {
            sb.AppendLine($"__cmd__.CommandText = \"INSERT INTO [{tableName}] DEFAULT VALUES\";");
        }
    }

    private void GenerateUpdateSqlForScalar(IndentedStringBuilder sb, INamedTypeSymbol entityType, string tableName, string paramName)
    {
        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null && p.DeclaredAccessibility == Accessibility.Public
                     && !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var idProperty = GetIdProperty(entityType);

        if (properties.Any() && idProperty != null)
        {
            var setClause = string.Join(", ", properties.Select(p => $"[{p.Name}] = @{p.Name}"));
            sb.AppendLine($"__cmd__.CommandText = \"UPDATE [{tableName}] SET {setClause} WHERE [{idProperty.Name}] = @{idProperty.Name}\";");

            // Add parameters for SET clause
            foreach (var prop in properties)
            {
                sb.AppendLine($"var param{prop.Name} = __cmd__.CreateParameter();");
                sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                sb.AppendLine($"param{prop.Name}.Value = {paramName}.{prop.Name} ?? (object)DBNull.Value;");
                sb.AppendLine($"__cmd__.Parameters.Add(param{prop.Name});");
            }

            // Add parameter for WHERE clause
            sb.AppendLine($"var param{idProperty.Name} = __cmd__.CreateParameter();");
            sb.AppendLine($"param{idProperty.Name}.ParameterName = \"@{idProperty.Name}\";");
            sb.AppendLine($"param{idProperty.Name}.Value = {paramName}.{idProperty.Name};");
            sb.AppendLine($"__cmd__.Parameters.Add(param{idProperty.Name});");
        }
        else
        {
            sb.AppendLine($"__cmd__.CommandText = \"UPDATE [{tableName}] SET Id = Id\";");
        }
    }

    private void GenerateSelectSqlForScalar(IndentedStringBuilder sb, string methodNameLower, string tableName, IMethodSymbol method)
    {
        if (methodNameLower.Contains("count"))
        {
            sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM [{tableName}]\";");
        }
        else if (methodNameLower.Contains("exists"))
        {
            var whereParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
            if (whereParams.Any())
            {
                var firstParam = whereParams.First();
                if (firstParam.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM [{tableName}] WHERE [Id] = @Id\";");
                    sb.AppendLine("var paramId = __cmd__.CreateParameter();");
                    sb.AppendLine("paramId.ParameterName = \"@Id\";");
                    sb.AppendLine($"paramId.Value = {firstParam.Name};");
                    sb.AppendLine("__cmd__.Parameters.Add(paramId);");
                }
                else
                {
                    sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM [{tableName}] WHERE [{firstParam.Name}] = @{firstParam.Name}\";");
                    sb.AppendLine($"var param{firstParam.Name} = __cmd__.CreateParameter();");
                    sb.AppendLine($"param{firstParam.Name}.ParameterName = \"@{firstParam.Name}\";");
                    sb.AppendLine($"param{firstParam.Name}.Value = {firstParam.Name};");
                    sb.AppendLine($"__cmd__.Parameters.Add(param{firstParam.Name});");
                }
            }
            else
            {
                sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM [{tableName}]\";");
            }
        }
        else
        {
            // Default scalar query
            sb.AppendLine($"__cmd__.CommandText = \"SELECT COUNT(*) FROM [{tableName}]\";");
        }
    }

    private void GenerateBatchOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName, int executeTypeInt)
    {
        // Find collection parameter
        var collectionParam = method.Parameters.FirstOrDefault(p => 
            p.Type is INamedTypeSymbol namedType && 
            (namedType.Name == "IEnumerable" || namedType.Name == "List" || namedType.Name == "IList" || namedType.Name == "ICollection"));
            
        if (collectionParam == null)
        {
            sb.AppendLine("throw new global::System.ArgumentException(\"BatchCommand requires a collection parameter\");");
            return;
        }

        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("connection.Open();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Null check
        sb.AppendLine($"if ({collectionParam.Name} == null)");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({collectionParam.Name}));");
        sb.AppendLine();

        // Initialize return value for counting operations
        var actualReturnType = method.ReturnType;
        if (isAsync && actualReturnType is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
        {
            actualReturnType = namedType.TypeArguments[0];
        }
        
        if (actualReturnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("int totalAffectedRows = 0;");
        }
        sb.AppendLine();

        // Create DbBatch for proper batch processing
        sb.AppendLine("using var batch = connection.CreateBatch();");
        var transactionParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "DbTransaction" || p.Type.Name == "IDbTransaction");
        if (transactionParam != null)
        {
            sb.AppendLine($"batch.Transaction = {transactionParam.Name};");
        }
        sb.AppendLine();

        // Generate batch commands using foreach loop
        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("var batchCommand = batch.CreateBatchCommand();");

        // Generate SQL based on batch operation type
        switch (executeTypeInt)
        {
            case 4: // BatchInsert
            case 7: // BatchCommand (assume insert for now)
                {
                    // Build columns and parameters from entity properties
                    var sqlDefine = GetSqlDefineForRepository(method);
                    INamedTypeSymbol? elementType = null;
                    if (collectionParam.Type is INamedTypeSymbol nt && nt.TypeArguments.Length > 0)
                    {
                        elementType = nt.TypeArguments[0] as INamedTypeSymbol;
                    }
                    elementType ??= entityType;

                    var props = elementType == null
                        ? new List<IPropertySymbol>()
                        : elementType.GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName && p.GetMethod != null).ToList();

                    var wrappedTable = sqlDefine.WrapColumn(tableName).Replace("\"", "\\\"");
                    var columns = string.Join(", ", props.Select(p => sqlDefine.WrapColumn(p.GetSqlName()).Replace("\"", "\\\"")));
                    var parameters = string.Join(", ", props.Select(p => $"{sqlDefine.ParameterPrefix}{p.GetSqlName()}"));

                    sb.AppendLine($"batchCommand.CommandText = \"INSERT INTO {wrappedTable} ({columns}) VALUES ({parameters})\";");

                    foreach (var prop in props)
                    {
                        var sqlName = prop.GetSqlName();
                        sb.AppendLine($"var param{prop.Name} = batchCommand.CreateParameter();");
                        sb.AppendLine($"param{prop.Name}.ParameterName = \"{sqlDefine.ParameterPrefix}{sqlName}\";");
                        sb.AppendLine($"param{prop.Name}.DbType = {GetDbTypeForProperty(prop)};");
                        if (prop.Type.IsReferenceType || IsNullableValueType(prop.Type))
                        {
                            sb.AppendLine($"param{prop.Name}.Value = (object?)item.{prop.Name} ?? global::System.DBNull.Value;");
                        }
                        else
                        {
                            sb.AppendLine($"param{prop.Name}.Value = item.{prop.Name};");
                        }
                        sb.AppendLine($"batchCommand.Parameters.Add(param{prop.Name});");
                    }
                }
                break;
                
            case 5: // BatchUpdate
                {
                    // Build UPDATE statement for all properties
                    var sqlDefine = GetSqlDefineForRepository(method);
                    INamedTypeSymbol? elementType = null;
                    if (collectionParam.Type is INamedTypeSymbol nt && nt.TypeArguments.Length > 0)
                    {
                        elementType = nt.TypeArguments[0] as INamedTypeSymbol;
                    }
                    elementType ??= entityType;

                    var props = elementType == null
                        ? new List<IPropertySymbol>()
                        : elementType.GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "Id").ToList();

                    var wrappedTable = sqlDefine.WrapColumn(tableName).Replace("\"", "\\\"");
                    var setClause = string.Join(", ", props.Select(p => $"{sqlDefine.WrapColumn(p.GetSqlName()).Replace("\"", "\\\"")} = {sqlDefine.ParameterPrefix}{p.GetSqlName()}"));

                    sb.AppendLine($"batchCommand.CommandText = \"UPDATE {wrappedTable} SET {setClause} WHERE Id = {sqlDefine.ParameterPrefix}Id\";");

                    // Add Id parameter
                    sb.AppendLine("var paramId = batchCommand.CreateParameter();");
                    sb.AppendLine($"paramId.ParameterName = \"{sqlDefine.ParameterPrefix}Id\";");
                    sb.AppendLine("paramId.DbType = global::System.Data.DbType.Int32;");
                    sb.AppendLine("paramId.Value = item.Id;");
                    sb.AppendLine("batchCommand.Parameters.Add(paramId);");

                    // Add other properties
                    foreach (var prop in props)
                    {
                        var sqlName = prop.GetSqlName();
                        sb.AppendLine($"var param{prop.Name} = batchCommand.CreateParameter();");
                        sb.AppendLine($"param{prop.Name}.ParameterName = \"{sqlDefine.ParameterPrefix}{sqlName}\";");
                        sb.AppendLine($"param{prop.Name}.DbType = {GetDbTypeForProperty(prop)};");
                        if (prop.Type.IsReferenceType || IsNullableValueType(prop.Type))
                        {
                            sb.AppendLine($"param{prop.Name}.Value = (object?)item.{prop.Name} ?? global::System.DBNull.Value;");
                        }
                        else
                        {
                            sb.AppendLine($"param{prop.Name}.Value = item.{prop.Name};");
                        }
                        sb.AppendLine($"batchCommand.Parameters.Add(param{prop.Name});");
                    }
                }
                break;
                
            case 6: // BatchDelete
                {
                    var sqlDefine = GetSqlDefineForRepository(method);
                    var wrappedTable = sqlDefine.WrapColumn(tableName).Replace("\"", "\\\"");
                    
                    sb.AppendLine($"batchCommand.CommandText = \"DELETE FROM {wrappedTable} WHERE Id = {sqlDefine.ParameterPrefix}Id\";");
                    
                    sb.AppendLine("var paramId = batchCommand.CreateParameter();");
                    sb.AppendLine($"paramId.ParameterName = \"{sqlDefine.ParameterPrefix}Id\";");
                    sb.AppendLine("paramId.DbType = global::System.Data.DbType.Int32;");
                    sb.AppendLine("paramId.Value = item.Id;");
                    sb.AppendLine("batchCommand.Parameters.Add(paramId);");
                }
                break;
        }

        // Add the batch command to the batch
        sb.AppendLine("batch.BatchCommands.Add(batchCommand);");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create a temporary command for interceptors (batch doesn't have a single command)
        sb.AppendLine("__cmd__ = connection.CreateCommand();");
        sb.AppendLine($"__cmd__.CommandText = \"Batch {GetBatchOperationName(executeTypeInt)} with {{{collectionParam.Name}.Count()}} items\";");
        sb.AppendLine();

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute the batch
        if (isAsync)
        {
            if (actualReturnType.SpecialType == SpecialType.System_Int32)
            {
                sb.AppendLine("totalAffectedRows = await batch.ExecuteNonQueryAsync();");
            }
            else
            {
                sb.AppendLine("await batch.ExecuteNonQueryAsync();");
            }
        }
        else
        {
            if (actualReturnType.SpecialType == SpecialType.System_Int32)
            {
                sb.AppendLine("totalAffectedRows = batch.ExecuteNonQuery();");
            }
            else
            {
                sb.AppendLine("batch.ExecuteNonQuery();");
            }
        }

        // Set result
        if (actualReturnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("__result__ = totalAffectedRows;");
        }

        // Return statement
        if (actualReturnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("return totalAffectedRows;");
        }
    }

    private string GetBatchOperationName(int executeTypeInt) => executeTypeInt switch
    {
        4 => "INSERT",
        5 => "UPDATE", 
        6 => "DELETE",
        7 => "COMMAND",
        _ => "UNKNOWN"
    };

    private void GenerateScalarOperationWithInterceptors(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, bool isAsync, string methodName)
    {
        var methodNameLower = methodName.ToLowerInvariant();

        // Connection setup
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__cmd__ = connection.CreateCommand();");

        // Check for SqlExecuteType attribute first
        var sqlExecuteTypeAttr = method.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (sqlExecuteTypeAttr != null && sqlExecuteTypeAttr.ConstructorArguments.Length > 0)
        {
            var enumValueObj = sqlExecuteTypeAttr.ConstructorArguments[0].Value;
            var sqlExecuteType = enumValueObj switch
            {
                int intValue => intValue,
                string strValue when int.TryParse(strValue, out var intVal) => intVal,
                _ => 0 // Select
            };

            // Handle specific SqlExecuteTypes for scalar operations
            switch (sqlExecuteType)
            {
                case 2: // Insert
                    // For INSERT operations returning scalar (affected rows), generate appropriate INSERT SQL
                    if (entityType != null)
                    {
                        var entityParam = method.Parameters.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Type, entityType));
                        if (entityParam != null)
                        {
                            GenerateInsertSqlForScalar(sb, entityType, tableName, entityParam.Name);
                        }
                        else
                        {
                            sb.AppendLine($"__cmd__.CommandText = \"INSERT INTO [{tableName}] DEFAULT VALUES\";");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"__cmd__.CommandText = \"INSERT INTO [{tableName}] DEFAULT VALUES\";");
                    }
                    break;

                case 1: // Update
                    // For UPDATE operations returning scalar (affected rows), generate appropriate UPDATE SQL
                    if (entityType != null)
                    {
                        var entityParam = method.Parameters.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Type, entityType));
                        if (entityParam != null)
                        {
                            GenerateUpdateSqlForScalar(sb, entityType, tableName, entityParam.Name);
                        }
                        else
                        {
                            sb.AppendLine($"__cmd__.CommandText = \"UPDATE [{tableName}] SET Id = Id\";");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"__cmd__.CommandText = \"UPDATE [{tableName}] SET Id = Id\";");
                    }
                    break;

                case 3: // Delete
                    // For DELETE operations returning scalar (affected rows), generate appropriate DELETE SQL
                    var idParam = method.Parameters.FirstOrDefault(p =>
                        p.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                        p.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase));
                    if (idParam != null)
                    {
                        sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM [{tableName}] WHERE [Id] = @{idParam.Name}\";");
                        sb.AppendLine($"var param{idParam.Name} = __cmd__.CreateParameter();");
                        sb.AppendLine($"param{idParam.Name}.ParameterName = \"@{idParam.Name}\";");
                        sb.AppendLine($"param{idParam.Name}.Value = {idParam.Name};");
                        sb.AppendLine($"__cmd__.Parameters.Add(param{idParam.Name});");
                    }
                    else
                    {
                        sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM [{tableName}]\";");
                    }
                    break;

                default: // Select or other
                    GenerateSelectSqlForScalar(sb, methodNameLower, tableName, method);
                    break;
            }
        }
        else
        {
            // Fallback to method name based inference
            GenerateSelectSqlForScalar(sb, methodNameLower, tableName, method);
        }

        sb.AppendLine();

        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute scalar operation
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");
        }

        // Convert result based on return type
        var returnType = method.ReturnType;
        if (isAsync && returnType is INamedTypeSymbol namedReturnType && namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
        {
            returnType = namedReturnType.TypeArguments[0];
        }

        if (returnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("var intResult = scalarResult == null ? 0 : System.Convert.ToInt32(scalarResult);");
            sb.AppendLine("__result__ = intResult;");
            sb.AppendLine("return intResult;");
        }
        else if (returnType.SpecialType == SpecialType.System_Int64)
        {
            sb.AppendLine("var longResult = scalarResult == null ? 0L : System.Convert.ToInt64(scalarResult);");
            sb.AppendLine("__result__ = longResult;");
            sb.AppendLine("return longResult;");
        }
        else if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine("var boolResult = scalarResult == null ? false : System.Convert.ToInt32(scalarResult) > 0;");
            sb.AppendLine("__result__ = boolResult;");
            sb.AppendLine("return boolResult;");
        }
        else
        {
            sb.AppendLine("__result__ = scalarResult;");
            sb.AppendLine("return scalarResult;");
        }
    }


    /// <summary>
    /// Gets the proper type declaration for the __result__ variable to avoid boxing.
    /// </summary>
    private string GetResultVariableType(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        
        // Handle async methods - unwrap Task<T> to T
        if (returnType is INamedTypeSymbol namedReturnType && 
            namedReturnType.Name == "Task" && 
            namedReturnType.TypeArguments.Length == 1)
        {
            returnType = namedReturnType.TypeArguments[0];
        }
        
        // For nullable reference types, use the full type with nullability
        if (returnType.CanBeReferencedByName)
        {
            var typeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            // Handle nullable types properly
            if (returnType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return typeName; // Already includes '?'
            }
            else if (!returnType.IsValueType && returnType.SpecialType != SpecialType.System_String)
            {
                return $"{typeName}?"; // Add nullable annotation for reference types
            }
            else
            {
                return typeName;
            }
        }
        
        // Fallback to object for unknown types
        return "object?";
    }

    /// <summary>
    /// Resolves generic service interface with actual type arguments from repository class.
    /// </summary>
    private static INamedTypeSymbol? ResolveGenericServiceInterface(INamedTypeSymbol genericInterface, INamedTypeSymbol repositoryClass)
    {
        System.Diagnostics.Debug.WriteLine($"=== ResolveGenericServiceInterface START ===");
        System.Diagnostics.Debug.WriteLine($"Generic interface: {genericInterface.Name}");
        System.Diagnostics.Debug.WriteLine($"Repository class: {repositoryClass.Name}");

        // Look for type arguments in repository class name or base types
        // Example: UserRepository<User> should resolve IRepository<T> to IRepository<User>

        if (repositoryClass.IsGenericType && repositoryClass.TypeArguments.Length > 0)
        {
            System.Diagnostics.Debug.WriteLine($"Repository class is generic with {repositoryClass.TypeArguments.Length} type arguments");

            // Try to construct the generic interface with repository's type arguments
            if (genericInterface.TypeParameters.Length == repositoryClass.TypeArguments.Length)
            {
                var constructedInterface = genericInterface.ConstructedFrom.Construct(repositoryClass.TypeArguments.ToArray());
                System.Diagnostics.Debug.WriteLine($"Constructed interface: {constructedInterface.ToDisplayString()}");
                return constructedInterface;
            }
        }

        // If repository class itself doesn't have type arguments, look for hints in the class name
        // Example: UserRepository -> try to infer User type
        var className = repositoryClass.Name;
        if (className.EndsWith("Repository"))
        {
            var entityName = className.Substring(0, className.Length - "Repository".Length);
            System.Diagnostics.Debug.WriteLine($"Inferred entity name: {entityName}");

            // Try to find the entity type in the same namespace or related namespaces
            var entityType = FindEntityTypeByName(repositoryClass.ContainingNamespace, entityName);
            if (entityType != null && genericInterface.TypeParameters.Length == 1)
            {
                System.Diagnostics.Debug.WriteLine($"Found entity type: {entityType.ToDisplayString()}");
                var constructedInterface = genericInterface.ConstructedFrom.Construct(entityType);
                System.Diagnostics.Debug.WriteLine($"Constructed interface: {constructedInterface.ToDisplayString()}");
                return constructedInterface;
            }
        }

        System.Diagnostics.Debug.WriteLine("Could not resolve generic interface, returning original");
        return genericInterface;
    }

    /// <summary>
    /// Finds entity type by name in the repository's context.
    /// </summary>
    private static INamedTypeSymbol? FindEntityTypeByName(INamespaceSymbol startingNamespace, string entityName)
    {
        // Search in the same namespace first
        var currentNamespace = startingNamespace;
        var entityType = FindTypeInNamespace(currentNamespace, entityName);
        if (entityType != null)
        {
            return entityType;
        }

        // Search in parent namespaces
        var parentNamespace = currentNamespace.ContainingNamespace;
        while (parentNamespace != null && !parentNamespace.IsGlobalNamespace)
        {
            entityType = FindTypeInNamespace(parentNamespace, entityName);
            if (entityType != null)
            {
                return entityType;
            }
            parentNamespace = parentNamespace.ContainingNamespace;
        }

        return null;
    }

    /// <summary>
    /// Finds a type by name within a specific namespace.
    /// </summary>
    private static INamedTypeSymbol? FindTypeInNamespace(INamespaceSymbol namespaceSymbol, string typeName)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol namedType && namedType.Name == typeName)
            {
                return namedType;
            }
            else if (member is INamespaceSymbol childNamespace)
            {
                var result = FindTypeInNamespace(childNamespace, typeName);
                if (result != null)
                {
                    return result;
                }
            }
        }
        return null;
    }

    private string GetDbConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        // Find the first DbConnection field or property
        var dbConnectionMember = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.IsDbConnection()) ??
            repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(x => x.IsDbConnection()) as ISymbol;

        // If not found in the class, check base class
        if (dbConnectionMember == null && repositoryClass.BaseType != null)
        {
            return GetDbConnectionFieldName(repositoryClass.BaseType);
        }

        return dbConnectionMember?.Name ?? "connection"; // Fallback to "connection" if not found
    }

    private void GenerateDbConnectionFieldIfNeeded(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass, string connectionFieldName)
    {
        var hasDbConnection = HasDbConnectionField(repositoryClass);
        if (!hasDbConnection)
        {
            // Only generate the field if it doesn't exist
            sb.AppendLine("// Auto-generated DbConnection field for repository operations");
            sb.AppendLine("// This field is available to both RepositoryFor and Sqlx generators");
            sb.AppendLine($"protected readonly global::System.Data.Common.DbConnection {connectionFieldName};");
            sb.AppendLine();
            System.Diagnostics.Debug.WriteLine($"Generated DbConnection field: {connectionFieldName}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"DbConnection field already exists: {connectionFieldName}");
        }
    }

    private bool HasDbConnectionField(INamedTypeSymbol repositoryClass)
    {
        // Check if the class or any of its base classes have a DbConnection field or property
        var current = repositoryClass;
        while (current != null)
        {
            // Check for fields
            var hasField = current.GetMembers()
                .OfType<IFieldSymbol>()
                .Any(x => x.IsDbConnection());

            // Check for properties  
            var hasProperty = current.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(x => x.IsDbConnection());

            if (hasField || hasProperty)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Generates optimized parameter value assignment code based on the property type.
    /// </summary>
    private static string GenerateParameterValueAssignment(string parameterName, string propertyAccess, ITypeSymbol propertyType)
    {
        // For reference types and nullable value types, we need the object cast for DBNull handling
        if (propertyType.IsReferenceType || IsNullableValueType(propertyType))
        {
            return $"{parameterName}.Value = (object?){propertyAccess} ?? global::System.DBNull.Value;";
        }
        else
        {
            // For non-nullable value types, direct assignment (will be boxed to object automatically)
            return $"{parameterName}.Value = {propertyAccess};";
        }
    }

    /// <summary>
    /// Generates optimized parameter value assignment for batch operations.
    /// </summary>
    private static string GenerateBatchParameterValueAssignment(string parameterName, string itemAccess, ITypeSymbol propertyType)
    {
        return GenerateParameterValueAssignment(parameterName, itemAccess, propertyType);
    }
}
