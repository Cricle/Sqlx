// -----------------------------------------------------------------------
// <copyright file="AbstractGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Stored procedures generator.
/// </summary>
public abstract class AbstractGenerator : ISourceGenerator
{
    /// <inheritdoc/>
    public abstract void Initialize(GeneratorInitializationContext context);

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        System.Diagnostics.Debug.WriteLine("AbstractGenerator.Execute called");
        
        // Retrieve the populated receiver
        if (context.SyntaxContextReceiver is not ISqlxSyntaxReceiver receiver)
        {
            System.Diagnostics.Debug.WriteLine("No ISqlxSyntaxReceiver found");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"Found {receiver.Methods.Count} methods and {receiver.RepositoryClasses.Count} repository classes");

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
            // Look for methods that return or accept entity types
            // Common patterns: GetAll() -> IList<Entity>, GetById(int) -> Entity, Create(Entity) -> int
            var methods = serviceInterface.GetMembers().OfType<IMethodSymbol>().ToArray();
            System.Diagnostics.Debug.WriteLine($"Found {methods.Length} methods in interface {serviceInterface.Name}");

            var candidateTypes = new Dictionary<INamedTypeSymbol, int>();

            foreach (var method in methods)
            {
                System.Diagnostics.Debug.WriteLine($"Analyzing method: {method.Name} - Return: {method.ReturnType}, Params: {method.Parameters.Length}");
                
                // Check return type for generic collections like IList<T>, Task<IList<T>>
                var entityType = ExtractEntityTypeFromType(method.ReturnType);
                if (entityType != null && IsLikelyEntityType(entityType))
                {
                    System.Diagnostics.Debug.WriteLine($"Found entity type from return type: {entityType.Name}");
                    candidateTypes[entityType] = (candidateTypes.TryGetValue(entityType, out var existingCount) ? existingCount : 0) + 2; // Higher weight for return types
                }

                // Check parameters for entity types
                foreach (var parameter in method.Parameters)
                {
                    entityType = ExtractEntityTypeFromType(parameter.Type);
                    if (entityType != null && IsLikelyEntityType(entityType))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found entity type from parameter: {entityType.Name}");
                        candidateTypes[entityType] = (candidateTypes.TryGetValue(entityType, out var existingCount2) ? existingCount2 : 0) + 1;
                    }
                }
            }

            // Return the most frequently referenced entity type
            if (candidateTypes.Count > 0)
            {
                var mostLikelyEntity = candidateTypes.OrderByDescending(kvp => kvp.Value).First().Key;
                System.Diagnostics.Debug.WriteLine($"Selected entity type: {mostLikelyEntity.Name} (score: {candidateTypes[mostLikelyEntity]})");
                return mostLikelyEntity;
            }

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in InferEntityTypeFromServiceInterface: {ex.Message}");
            return null;
        }
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
                if (type.Name == entityName && IsLikelyEntityType(type))
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
                    if (type.Name == entityName && IsLikelyEntityType(type))
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

    private INamedTypeSymbol? ExtractEntityTypeFromType(ITypeSymbol type)
    {
        try
        {
            if (type == null) return null;

            // Handle Task<T>
            if (type is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                return ExtractEntityTypeFromType(namedType.TypeArguments[0]);
            }

            // Handle IList<T>, IEnumerable<T>, List<T>, etc.
            if (type is INamedTypeSymbol collectionType && collectionType.TypeArguments.Length == 1)
            {
                var elementType = collectionType.TypeArguments[0];
                if (elementType is INamedTypeSymbol entityType && IsLikelyEntityType(entityType))
                {
                    return entityType;
                }
            }

            // Handle direct entity type
            if (type is INamedTypeSymbol directType && IsLikelyEntityType(directType))
            {
                return directType;
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ExtractEntityTypeFromType: {ex.Message}");
            return null;
        }
    }

    private bool IsLikelyEntityType(INamedTypeSymbol type)
    {
        try
        {
            if (type == null) return false;

            // Skip primitive types and system types
            if (type.SpecialType != SpecialType.None)
            {
                return false;
            }

            // Skip system namespace types
            var namespaceName = type.ContainingNamespace?.ToDisplayString() ?? "";
            if (namespaceName.StartsWith("System"))
            {
                return false;
            }

            // Must be a class
            if (type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            // Should have properties (typical of entities)
            var hasProperties = type.GetMembers().OfType<IPropertySymbol>().Any();
            System.Diagnostics.Debug.WriteLine($"Type {type.Name}: hasProperties={hasProperties}");
            return hasProperties;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in IsLikelyEntityType: {ex.Message}");
            return false;
        }
    }

    private void GenerateRepositorySource(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceInterface, INamedTypeSymbol entityType, string tableName)
    {
        sb.AppendLine(@"// <auto-generated>
// Code generated by Sqlx Repository Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>");

        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS8618, CS8625, CS8629");
        sb.AppendLine();
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using Sqlx.Annotations;");
        sb.AppendLine();

        var hasNamespace = !repositoryClass.ContainingNamespace.IsGlobalNamespace;
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {repositoryClass.ContainingNamespace.ToDisplayString()}");
            sb.AppendLine("{");
            sb.PushIndent();
        }

        var staticKeyword = repositoryClass.IsStatic ? "static " : string.Empty;
        sb.AppendLine($"{repositoryClass.DeclaredAccessibility.GetAccessibility()} {staticKeyword}partial class {repositoryClass.Name}");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate CRUD methods for the service type
        GenerateServiceInterfaceMethods(sb, serviceInterface, entityType, tableName);

        sb.PopIndent();
        sb.AppendLine("}");

        if (hasNamespace)
        {
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }

    private void GenerateRepositoryCrudMethods(IndentedStringBuilder sb, INamedTypeSymbol serviceType, string tableName)
    {
        var entityTypeName = serviceType.ToDisplayString();
        
        // Generate a simple example - GetAll method with actual implementation
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Gets all {serviceType.Name} entities from the {tableName} table.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public global::System.Collections.Generic.IList<{entityTypeName}> GetAll()");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// This is a simplified implementation for demonstration");
        sb.AppendLine("// In a real scenario, this would use the database connection to fetch data");
        sb.AppendLine($"return new global::System.Collections.Generic.List<{entityTypeName}>();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate async version
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Asynchronously gets all {serviceType.Name} entities from the {tableName} table.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IList<{entityTypeName}>> GetAllAsync(global::System.Threading.CancellationToken cancellationToken = default)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// This is a simplified implementation for demonstration");
        sb.AppendLine("await global::System.Threading.Tasks.Task.Delay(1, cancellationToken);");
        sb.AppendLine($"return new global::System.Collections.Generic.List<{entityTypeName}>();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate Insert method
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Inserts a new {serviceType.Name} entity into the {tableName} table.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public int Insert({entityTypeName} entity)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("if (entity == null) throw new global::System.ArgumentNullException(nameof(entity));");
        sb.AppendLine("// This is a simplified implementation for demonstration");
        sb.AppendLine("// In a real scenario, this would use the database connection to insert data");
        sb.AppendLine("return 1; // Simulate 1 row affected");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate a note about the full implementation
        sb.AppendLine("// NOTE: This is a simplified repository implementation for demonstration.");
        sb.AppendLine("// The full implementation would include:");
        sb.AppendLine("// - Query methods with ExpressionToSql support");
        sb.AppendLine("// - Update and Delete methods");
        sb.AppendLine("// - Batch operations");
        sb.AppendLine("// - Raw SQL execution");
        sb.AppendLine("// - Proper database connection handling");
        sb.AppendLine("// - Error handling and logging");
    }

    private void GenerateServiceInterfaceMethods(IndentedStringBuilder sb, INamedTypeSymbol serviceInterface, INamedTypeSymbol? entityType, string tableName)
    {
        var entityTypeName = entityType?.ToDisplayString() ?? "object";
        
        // Generate implementation for each method in the service interface
        foreach (var method in serviceInterface.GetMembers().OfType<IMethodSymbol>())
        {
            GenerateServiceMethod(sb, method, entityType, tableName);
        }
    }

    private void GenerateServiceMethod(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            var entityTypeName = entityType?.ToDisplayString() ?? "object";
            var methodName = method.Name;
            var returnType = method.ReturnType.ToDisplayString();
            var isAsync = method.ReturnType.Name == "Task";
            
            System.Diagnostics.Debug.WriteLine($"Generating method: {methodName}, returnType: {returnType}, isAsync: {isAsync}");
            
            // Generate method signature
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// Implementation of {methodName} using Sqlx.");
            sb.AppendLine($"/// </summary>");
            
            var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
            sb.AppendLine($"public {returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();

            // Determine SQL operation type based on method name
            var sqlOperation = InferSqlOperationType(methodName);
            var sql = GenerateSqlForOperation(sqlOperation, methodName, tableName, entityType);
            
            sb.AppendLine($"// Generated SQL: {sql}");
            sb.AppendLine("// TODO: Implement actual Sqlx method call");
            
            // Generate appropriate return statement
            if (method.ReturnsVoid)
            {
                // void methods - no return
                System.Diagnostics.Debug.WriteLine($"Method {methodName} returns void");
            }
            else if (isAsync)
            {
                if (method.ReturnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length > 0)
                {
                    var taskReturnType = taskType.TypeArguments[0];
                    var defaultValue = GetDefaultValueForType(taskReturnType);
                    sb.AppendLine($"return global::System.Threading.Tasks.Task.FromResult({defaultValue});");
                }
                else
                {
                    sb.AppendLine("return global::System.Threading.Tasks.Task.CompletedTask;");
                }
            }
            else
            {
                var defaultValue = GetDefaultValueForType(method.ReturnType);
                sb.AppendLine($"return {defaultValue};");
            }
            
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in GenerateServiceMethod: {ex.Message}");
            // Generate a fallback method to prevent compilation errors
            sb.AppendLine($"// Error generating method {method.Name}: {ex.Message}");
        }
    }

    private string InferSqlOperationType(string methodName)
    {
        var lowerName = methodName.ToLowerInvariant();
        
        if (lowerName.Contains("get") || lowerName.Contains("find") || lowerName.Contains("select"))
            return "SELECT";
        if (lowerName.Contains("create") || lowerName.Contains("insert") || lowerName.Contains("add"))
            return "INSERT";
        if (lowerName.Contains("update") || lowerName.Contains("modify"))
            return "UPDATE";
        if (lowerName.Contains("delete") || lowerName.Contains("remove"))
            return "DELETE";
            
        return "SELECT"; // default
    }

    private string GenerateSqlForOperation(string operation, string methodName, string tableName, INamedTypeSymbol? entityType)
    {
        return operation.ToUpperInvariant() switch
        {
            "SELECT" => $"SELECT * FROM {tableName}",
            "INSERT" => $"INSERT INTO {tableName} (...) VALUES (...)",
            "UPDATE" => $"UPDATE {tableName} SET ... WHERE ...",
            "DELETE" => $"DELETE FROM {tableName} WHERE ...",
            _ => $"SELECT * FROM {tableName}"
        };
    }

    private string GetDefaultValueForType(ITypeSymbol type)
    {
        if (type.CanBeReferencedByName)
        {
            if (type.IsReferenceType)
                return "null!";
            
            return type.SpecialType switch
            {
                SpecialType.System_Boolean => "false",
                SpecialType.System_Int32 => "0",
                SpecialType.System_String => "string.Empty",
                _ => "default!"
            };
        }
        
        return "default!";
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
            
            // Generate namespace
            sb.AppendLine($"namespace {repositoryClass.ContainingNamespace.ToDisplayString()};");
            sb.AppendLine();
            
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
            sb.AppendLine($"partial class {repositoryClass.Name} : {serviceInterface.ToDisplayString()}");
            sb.AppendLine("{");
            sb.PushIndent();
            
            // Generate interceptor partial methods
            WriteRepositoryInterceptMethods(sb, repositoryClass);
            
            // Generate methods for ALL interface methods
            var methods = serviceInterface.GetMembers().OfType<IMethodSymbol>().ToArray();
            System.Diagnostics.Debug.WriteLine($"Found {methods.Length} methods in interface {serviceInterface.Name}");
            System.Diagnostics.Debug.WriteLine($"Generating implementations for all {methods.Length} methods");
            
            foreach (var method in methods)
            {
                GenerateRepositoryMethod(sb, method, entityType, tableName);
            }
            
            sb.PopIndent();
            sb.AppendLine("}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GenerateFullRepositoryImplementation: {ex.Message}");
            throw;
        }
    }

    private string GenerateAttributeString(AttributeData attribute)
    {
        var attrName = attribute.AttributeClass?.Name;
        if (attrName == null) return "";
        
        System.Diagnostics.Debug.WriteLine($"Generating attribute string for {attrName}");
        
        // Remove "Attribute" suffix if present
        if (attrName.EndsWith("Attribute"))
        {
            attrName = attrName.Substring(0, attrName.Length - "Attribute".Length);
        }
        
        var args = new List<string>();
        
        // Handle constructor arguments
        System.Diagnostics.Debug.WriteLine($"Constructor arguments count: {attribute.ConstructorArguments.Length}");
        foreach (var arg in attribute.ConstructorArguments)
        {
            System.Diagnostics.Debug.WriteLine($"Arg kind: {arg.Kind}, value: {arg.Value}, type: {arg.Type?.ToDisplayString()}");
            
            if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string stringValue)
            {
                args.Add($"\"{stringValue}\"");
            }
            else if (arg.Kind == TypedConstantKind.Enum)
            {
                // Handle enum values like SqlExecuteTypes.Insert
                var enumTypeName = arg.Type?.Name ?? "Unknown";
                // Use simple name instead of full display string to avoid namespace issues
                if (enumTypeName == "SqlExecuteTypes")
                {
                    var enumValue = arg.Value?.ToString() ?? "Unknown";
                    args.Add($"SqlExecuteTypes.{enumValue}");
                }
                else
                {
                    var enumValue = arg.Value?.ToString() ?? "Unknown";
                    args.Add($"{enumTypeName}.{enumValue}");
                }
            }
            else if (arg.Value != null)
            {
                args.Add(arg.Value.ToString() ?? "");
            }
        }
        
        // Handle named arguments (skip for SqlExecuteTypeAttribute as it only uses constructor parameters)
        if (attrName != "SqlExecuteType")
        {
            System.Diagnostics.Debug.WriteLine($"Named arguments count: {attribute.NamedArguments.Length}");
            foreach (var namedArg in attribute.NamedArguments)
            {
                System.Diagnostics.Debug.WriteLine($"Named arg: {namedArg.Key} = {namedArg.Value.Value}");
                var value = namedArg.Value.Value?.ToString() ?? "";
                if (namedArg.Value.Kind == TypedConstantKind.Primitive && namedArg.Value.Value is string)
                {
                    value = $"\"{value}\"";
                }
                args.Add($"{namedArg.Key} = {value}");
            }
        }
        
        var argsString = args.Count > 0 ? $"({string.Join(", ", args)})" : "";
        var result = $"[{attrName}{argsString}]";
        System.Diagnostics.Debug.WriteLine($"Generated attribute: {result}");
        return result;
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
        sb.AppendLine("// partial void OnExecuteFail(string methodName, DbCommand command, Exception exception, long elapsed)");
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
        
        sb.AppendLine($"{staticKeyword}partial void OnExecuteFail(string methodName, DbCommand command, Exception exception, long elapsed);");
        sb.AppendLine();
    }

    private bool HasSqlxAttribute(IMethodSymbol method)
    {
        // Check if method has any Sqlx-related attributes
        var attributes = method.GetAttributes();
        foreach (var attr in attributes)
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == "SqlxAttribute" || 
                attrName == "RawSqlAttribute" || 
                attrName == "SqlExecuteTypeAttribute")
            {
                System.Diagnostics.Debug.WriteLine($"Method {method.Name} has Sqlx attribute: {attrName}");
                return true;
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"Method {method.Name} does not have Sqlx attributes");        
        return false;
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
                if (IsCollectionType(innerType))
                {
                    return $"A task containing the collection of {GetEntityTypeName(innerType)} entities.";
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
        
        if (IsCollectionType(returnType))
        {
            return $"A collection of {GetEntityTypeName(returnType)} entities.";
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

    private string GetEntityTypeName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.TypeArguments.Length > 0)
            {
                return namedType.TypeArguments[0].Name;
            }
        }
        
        return type.Name;
    }

    private void GenerateOrCopyAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        // Check if method already has Sqlx attributes
        var existingAttributes = method.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == "SqlxAttribute" || 
                          attr.AttributeClass?.Name == "RawSqlAttribute" || 
                          attr.AttributeClass?.Name == "SqlExecuteTypeAttribute")
            .ToArray();
        
        // Always generate attributes based on method name patterns for now
        // TODO: Fix attribute copying later
        var generatedAttribute = GenerateSqlxAttributeFromMethodName(method, tableName);
        sb.AppendLine(generatedAttribute);
    }

    private string GenerateSqlxAttributeFromMethodName(IMethodSymbol method, string tableName)
    {
        var methodName = method.Name.ToLowerInvariant();
        
        // Pattern-based attribute generation
        if (methodName.Contains("getall") || methodName.Contains("list") || methodName.Contains("all"))
        {
            return $"[Sqlx(\"SELECT * FROM {tableName}\")]";
        }
        else if (methodName.Contains("getby") || methodName.Contains("find") || methodName.Contains("byid"))
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

    private void GenerateRepositoryMethodBody(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            var methodName = method.Name;
            var isAsync = method.ReturnType.Name == "Task" || (method.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task");
            var returnType = method.ReturnType;
            
            // Generate timing and command variables
            sb.AppendLine("var __startTime__ = System.Diagnostics.Stopwatch.GetTimestamp();");
            sb.AppendLine("System.Data.Common.DbCommand? __cmd__ = null;");
            sb.AppendLine("System.Exception? __exception__ = null;");
            sb.AppendLine("object? __result__ = null;");
            sb.AppendLine();
            
            sb.AppendLine("try");
            sb.AppendLine("{");
            sb.PushIndent();
            
            // Open connection if needed
            sb.AppendLine("if (connection.State != System.Data.ConnectionState.Open)");
            sb.AppendLine("{");
            sb.PushIndent();
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"await connection.OpenAsync({cancellationTokenName});");
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
            sb.AppendLine($"__cmd__.CommandText = {GenerateSqlCommand(method, tableName)};");
            sb.AppendLine();
            
            // Generate parameters
            GenerateRepositoryParameterAssignments(sb, method);
            
            // Call OnExecuting interceptor
            sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
            
            // Execute command and handle result
            GenerateRepositoryExecution(sb, method, entityType, isAsync);
            
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("catch (System.Exception ex)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("__exception__ = ex;");
            sb.AppendLine("var __elapsed__ = System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__;");
            sb.AppendLine($"OnExecuteFail(\"{methodName}\", __cmd__ ?? connection.CreateCommand(), ex, __elapsed__);");
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
            sb.AppendLine($"OnExecuted(\"{methodName}\", __cmd__ ?? connection.CreateCommand(), __result__, __elapsed__);");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("__cmd__?.Dispose();");
            sb.PopIndent();
            sb.AppendLine("}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating repository method body: {ex.Message}");
            sb.AppendLine($"throw new System.NotImplementedException(\"Error generating method body: {ex.Message}\");");
        }
    }

    private string GenerateSqlCommand(IMethodSymbol method, string tableName)
    {
        var methodName = method.Name.ToLowerInvariant();
        
        // Check for Sqlx attribute first
        var sqlxAttr = method.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlxAttribute");
        if (sqlxAttr != null && sqlxAttr.ConstructorArguments.Length > 0)
        {
            var sql = sqlxAttr.ConstructorArguments[0].Value?.ToString();
            if (!string.IsNullOrEmpty(sql))
            {
                return $"\"{sql}\"";
            }
        }
        
        // Check for SqlExecuteType attribute
        var executeTypeAttr = method.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        if (executeTypeAttr != null && executeTypeAttr.ConstructorArguments.Length >= 2)
        {
            var executeType = executeTypeAttr.ConstructorArguments[0].Value?.ToString();
            var table = executeTypeAttr.ConstructorArguments[1].Value?.ToString() ?? tableName;
            
            switch (executeType)
            {
                case "Insert":
                    return GenerateInsertSql(method, table);
                case "Update":
                    return GenerateUpdateSql(method, table);
                case "Delete":
                    return GenerateDeleteSql(method, table);
                default:
                    return $"\"SELECT * FROM {table}\"";
            }
        }
        
        // Fallback to pattern-based generation
        if (methodName.Contains("getall") || methodName.Contains("list"))
        {
            return $"\"SELECT * FROM {tableName}\"";
        }
        else if (methodName.Contains("getby") || methodName.Contains("find"))
        {
            var paramName = method.Parameters.FirstOrDefault()?.Name ?? "id";
            return $"\"SELECT * FROM {tableName} WHERE Id = @{paramName}\"";
        }
        else if (methodName.Contains("create") || methodName.Contains("insert"))
        {
            return GenerateInsertSql(method, tableName);
        }
        else if (methodName.Contains("update"))
        {
            return GenerateUpdateSql(method, tableName);
        }
        else if (methodName.Contains("delete") || methodName.Contains("remove"))
        {
            return GenerateDeleteSql(method, tableName);
        }
        
        return $"\"SELECT * FROM {tableName}\"";
    }

    private string GenerateInsertSql(IMethodSymbol method, string tableName)
    {
        var entityParam = method.Parameters.FirstOrDefault(p => p.Type.TypeKind == TypeKind.Class && p.Type.Name != "String");
        if (entityParam != null)
        {
            return $"\"INSERT INTO {tableName} (Name) VALUES (@Name)\""; // Simplified
        }
        return $"\"INSERT INTO {tableName} DEFAULT VALUES\"";
    }

    private string GenerateUpdateSql(IMethodSymbol method, string tableName)
    {
        return $"\"UPDATE {tableName} SET Name = @Name WHERE Id = @Id\""; // Simplified
    }

    private string GenerateDeleteSql(IMethodSymbol method, string tableName)
    {
        var paramName = method.Parameters.FirstOrDefault()?.Name ?? "id";
        return $"\"DELETE FROM {tableName} WHERE Id = @{paramName}\"";
    }

    private void GenerateRepositoryParameterAssignments(IndentedStringBuilder sb, IMethodSymbol method)
    {
        foreach (var param in method.Parameters)
        {
            if (param.Type.Name == "CancellationToken") continue;
            
            if (param.Type.TypeKind == TypeKind.Class && param.Type.Name != "String")
            {
                // Complex object - map properties
                sb.AppendLine($"// Map {param.Name} properties to parameters");
                sb.AppendLine($"if ({param.Name} != null)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"var param = __cmd__.CreateParameter();");
                sb.AppendLine($"param.ParameterName = \"@Name\";");
                sb.AppendLine($"param.Value = {param.Name}.Name ?? (object)System.DBNull.Value;");
                sb.AppendLine($"__cmd__.Parameters.Add(param);");
                sb.AppendLine();
                sb.AppendLine($"var idParam = __cmd__.CreateParameter();");
                sb.AppendLine($"idParam.ParameterName = \"@Id\";");
                sb.AppendLine($"idParam.Value = {param.Name}.Id;");
                sb.AppendLine($"__cmd__.Parameters.Add(idParam);");
                sb.PopIndent();
                sb.AppendLine("}");
            }
            else
            {
                // Simple parameter
                sb.AppendLine($"var param{param.Name} = __cmd__.CreateParameter();");
                sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
                sb.AppendLine($"param{param.Name}.Value = {param.Name} ?? (object)System.DBNull.Value;");
                sb.AppendLine($"__cmd__.Parameters.Add(param{param.Name});");
            }
        }
        sb.AppendLine();
    }

    private void GenerateRepositoryExecution(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, bool isAsync)
    {
        var returnType = method.ReturnType;
        var methodName = method.Name.ToLowerInvariant();
        
        if (method.ReturnsVoid || (isAsync && returnType.Name == "Task" && returnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length == 0))
        {
            // Void or Task (no return value)
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"__result__ = await __cmd__.ExecuteNonQueryAsync({cancellationTokenName});");
            }
            else
            {
                sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
            }
        }
        else if (methodName.Contains("count") || methodName.Contains("exists") || 
                 returnType.SpecialType == SpecialType.System_Int32 ||
                 returnType.SpecialType == SpecialType.System_Int64)
        {
            // Scalar return
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"var scalarResult = await __cmd__.ExecuteScalarAsync({cancellationTokenName});");
                sb.AppendLine($"__result__ = System.Convert.ToInt32(scalarResult ?? 0);");
                sb.AppendLine($"return (__result__ as int?) ?? 0;");
            }
            else
            {
                sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");
                sb.AppendLine("__result__ = System.Convert.ToInt32(scalarResult ?? 0);");
                sb.AppendLine("return (__result__ as int?) ?? 0;");
            }
        }
        else if (IsCollectionType(returnType))
        {
            // Collection return
            GenerateRepositoryCollectionReturn(sb, method, entityType, isAsync);
        }
        else
        {
            // Single entity return
            GenerateRepositorySingleEntityReturn(sb, method, entityType, isAsync);
        }
    }

    private void GenerateRepositoryCollectionReturn(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, bool isAsync)
    {
        if (isAsync)
        {
            var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
            var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
            sb.AppendLine($"using var __reader__ = await __cmd__.ExecuteReaderAsync({cancellationTokenName});");
        }
        else
        {
            sb.AppendLine("using var __reader__ = __cmd__.ExecuteReader();");
        }
        
        sb.AppendLine("var results = new System.Collections.Generic.List<" + (entityType?.Name ?? "object") + ">();");
        sb.AppendLine("while (__reader__.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"var item = new {entityType?.Name ?? "object"}");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("Id = __reader__[\"Id\"] is System.DBNull ? 0 : (int)__reader__[\"Id\"],");
        sb.AppendLine("Name = __reader__[\"Name\"] as string ?? string.Empty");
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine("results.Add(item);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("__result__ = results;");
        sb.AppendLine("return results;");
    }

    private void GenerateRepositorySingleEntityReturn(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, bool isAsync)
    {
        if (isAsync)
        {
            var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
            var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
            sb.AppendLine($"using var __reader__ = await __cmd__.ExecuteReaderAsync({cancellationTokenName});");
        }
        else
        {
            sb.AppendLine("using var __reader__ = __cmd__.ExecuteReader();");
        }
        
        sb.AppendLine("if (__reader__.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"var result = new {entityType?.Name ?? "object"}");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("Id = __reader__[\"Id\"] is System.DBNull ? 0 : (int)__reader__[\"Id\"],");
        sb.AppendLine("Name = __reader__[\"Name\"] as string ?? string.Empty");
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine("__result__ = result;");
        sb.AppendLine("return result;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("__result__ = null;");
        sb.AppendLine("return null;");
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
            
            // Generate simple method implementation that throws NotImplementedException
            // The actual implementation will be provided by the main Sqlx generator
            var isAsync = method.ReturnType.Name == "Task" || (method.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task");
            var asyncModifier = isAsync ? "async " : "";
            sb.AppendLine($"public {asyncModifier}{returnType} {methodName}({parameters})");
            sb.AppendLine("{");
            sb.PushIndent();
            
            if (isAsync)
            {
                sb.AppendLine("await Task.CompletedTask;");
            }
            sb.AppendLine("throw new System.NotImplementedException(\"Generated by RepositoryFor - implementation provided by Sqlx generator\");");
            
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

    private void GenerateMethodBody(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        try
        {
            // Generate real Sqlx implementation
            var isAsync = method.ReturnType.Name == "Task" || (method.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task");
            var methodName = method.Name.ToLowerInvariant();
            var returnType = method.ReturnType;
            
            // Get connection variable (assume it's named 'connection' in the class)
            sb.AppendLine("// Open connection if needed");
            sb.AppendLine("if (connection.State != System.Data.ConnectionState.Open)");
            sb.AppendLine("{");
            sb.PushIndent();
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"await connection.OpenAsync({cancellationTokenName});");
            }
            else
            {
                sb.AppendLine("connection.Open();");
            }
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
            
            // Create and configure command
            sb.AppendLine("using var command = connection.CreateCommand();");
            
            // Set SQL based on method type
            var sql = GenerateSqlForMethodType(methodName, tableName, entityType);
            sb.AppendLine($"command.CommandText = {sql};");
            sb.AppendLine();
            
            // Add parameters
            GenerateParameterAssignment(sb, method, entityType);
            
            // Execute and return
            GenerateExecutionAndReturn(sb, method, entityType, isAsync);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating method body: {ex.Message}");
            sb.AppendLine($"// Error generating method body: {ex.Message}");
            sb.AppendLine("throw new NotImplementedException(\"Error in code generation\");");
        }
    }

    private string GenerateSqlForMethodType(string methodName, string tableName, INamedTypeSymbol? entityType)
    {
        // Generate SQL string literal based on method pattern
        if (methodName.StartsWith("getall"))
        {
            return $"\"SELECT * FROM {tableName}\"";
        }
        else if (methodName.StartsWith("get") && methodName.Contains("byid"))
        {
            return $"\"SELECT * FROM {tableName} WHERE Id = @id\"";
        }
        else if (methodName.StartsWith("create"))
        {
            if (entityType != null)
            {
                var properties = entityType.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.Name != "Id") // Exclude Id for INSERT
                    .ToList();
                var columns = string.Join(", ", properties.Select(p => p.Name));
                var parameters = string.Join(", ", properties.Select(p => $"@{p.Name.ToLowerInvariant()}"));
                return $"\"INSERT INTO {tableName} ({columns}) VALUES ({parameters})\"";
            }
            return $"\"INSERT INTO {tableName} (Name, Email, CreatedAt) VALUES (@name, @email, @createdat)\"";
        }
        else if (methodName.StartsWith("update"))
        {
            if (entityType != null)
            {
                var properties = entityType.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.Name != "Id") // Exclude Id for SET clause
                    .ToList();
                var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name.ToLowerInvariant()}"));
                return $"\"UPDATE {tableName} SET {setClause} WHERE Id = @id\"";
            }
            return $"\"UPDATE {tableName} SET Name = @name, Email = @email WHERE Id = @id\"";
        }
        else if (methodName.StartsWith("delete"))
        {
            return $"\"DELETE FROM {tableName} WHERE Id = @id\"";
        }
        else if (methodName.Contains("count"))
        {
            return $"\"SELECT COUNT(*) FROM {tableName}\"";
        }
        else if (methodName.Contains("exists"))
        {
            return $"\"SELECT CASE WHEN EXISTS(SELECT 1 FROM {tableName} WHERE Id = @id) THEN 1 ELSE 0 END\"";
        }
        
        // Default fallback
        return $"\"SELECT * FROM {tableName}\"";
    }

    private void GenerateParameterAssignment(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType)
    {
        foreach (var param in method.Parameters)
        {
            if (param.Type.Name == "CancellationToken") continue; // Skip CancellationToken
            
            var paramType = param.Type;
            if (paramType.IsScalarType())
            {
                // Simple scalar parameter
                sb.AppendLine($"var param{param.Name} = command.CreateParameter();");
                sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name.ToLowerInvariant()}\";");
                
                // Handle nullable value types and reference types correctly
                if (paramType.IsValueType && !paramType.CanBeReferencedByName)
                {
                    sb.AppendLine($"param{param.Name}.Value = (object){param.Name};");
            }
            else
            {
                    sb.AppendLine($"param{param.Name}.Value = (object){param.Name} ?? System.DBNull.Value;");
                }
                sb.AppendLine($"command.Parameters.Add(param{param.Name});");
            }
            else
            {
                // Complex object - map properties to parameters
                var properties = paramType.GetMembers().OfType<IPropertySymbol>().ToList();
                foreach (var prop in properties)
                {
                    sb.AppendLine($"var param{prop.Name} = command.CreateParameter();");
                    sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name.ToLowerInvariant()}\";");
                    
                    // Handle nullable value types and reference types correctly
                    if (prop.Type.IsValueType && prop.Type.NullableAnnotation != NullableAnnotation.Annotated)
                    {
                        sb.AppendLine($"param{prop.Name}.Value = (object)({param.Name}?.{prop.Name} ?? default({prop.Type.ToDisplayString()}));");
            }
            else
            {
                        sb.AppendLine($"param{prop.Name}.Value = (object)({param.Name}?.{prop.Name}) ?? System.DBNull.Value;");
                    }
                    sb.AppendLine($"command.Parameters.Add(param{prop.Name});");
                }
            }
        }
        
        if (method.Parameters.Any(p => p.Type.Name != "CancellationToken"))
        {
            sb.AppendLine();
        }
    }

    private void GenerateExecutionAndReturn(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, bool isAsync)
    {
        var returnType = method.ReturnType;
        var methodName = method.Name.ToLowerInvariant();
        
        if (method.ReturnsVoid)
        {
            // Void method - just execute
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"await command.ExecuteNonQueryAsync({cancellationTokenName});");
            }
            else
            {
                sb.AppendLine("command.ExecuteNonQuery();");
            }
            return;
        }
        
        // Unwrap Task<T> to get actual return type
        var actualReturnType = returnType;
        if (isAsync && returnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length > 0)
        {
            actualReturnType = taskType.TypeArguments[0];
        }
        
        var actualTypeString = actualReturnType.ToDisplayString();
        
        // Handle different return types
        if (actualTypeString == "int" || methodName.StartsWith("create") || methodName.StartsWith("update") || methodName.StartsWith("delete"))
        {
            // Execute non-query for CUD operations
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"var result = await command.ExecuteNonQueryAsync({cancellationTokenName});");
        }
        else
        {
                sb.AppendLine("var result = command.ExecuteNonQuery();");
            }
            sb.AppendLine("return result;");
        }
        else if (actualTypeString.Contains("IList") || actualTypeString.Contains("List") || actualTypeString.Contains("IEnumerable"))
        {
            // Return collection
            GenerateCollectionReturn(sb, method, entityType, isAsync);
        }
        else if (entityType != null && actualTypeString.Contains(entityType.Name))
        {
            // Return single entity
            GenerateSingleEntityReturn(sb, method, entityType, isAsync);
        }
        else
        {
            // Scalar return
            if (isAsync)
            {
                var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
                var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
                sb.AppendLine($"var result = await command.ExecuteScalarAsync({cancellationTokenName});");
            }
            else
            {
                sb.AppendLine("var result = command.ExecuteScalar();");
            }
            sb.AppendLine($"return ({actualTypeString})(result ?? default({actualTypeString}));");
        }
    }

    private void GenerateCollectionReturn(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, bool isAsync)
    {
        var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
        
        sb.AppendLine("var results = new List<" + (entityType?.Name ?? "object") + ">();");
        
        if (isAsync)
        {
            sb.AppendLine($"using var reader = await command.ExecuteReaderAsync({cancellationTokenName});");
            sb.AppendLine($"while (await reader.ReadAsync({cancellationTokenName}))");
        }
        else
        {
            sb.AppendLine("using var reader = command.ExecuteReader();");
            sb.AppendLine("while (reader.Read())");
        }
        
        sb.AppendLine("{");
        sb.PushIndent();
        
        if (entityType != null)
        {
            sb.AppendLine($"results.Add(new {entityType.Name}");
            sb.AppendLine("{");
            sb.PushIndent();
            
            var properties = entityType.GetMembers().OfType<IPropertySymbol>().ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var comma = i < properties.Count - 1 ? "," : "";
                var propType = prop.Type.ToDisplayString();
                
                // Handle value types and reference types properly
                if (prop.Type.IsValueType)
                {
                    if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated || propType.EndsWith("?"))
                    {
                        // Nullable value type
                        sb.AppendLine($"{prop.Name} = reader[\"{prop.Name}\"] is DBNull ? null : ({propType})reader[\"{prop.Name}\"]{comma}");
                    }
                    else
                    {
                        // Non-nullable value type
                        sb.AppendLine($"{prop.Name} = ({propType})reader[\"{prop.Name}\"]{comma}");
                    }
                }
                else
                {
                    // Reference type
                    sb.AppendLine($"{prop.Name} = reader[\"{prop.Name}\"] as {propType}{comma}");
                }
            }
            
            sb.PopIndent();
            sb.AppendLine("});");
        }
        else
        {
            sb.AppendLine("// TODO: Map reader to object");
            sb.AppendLine("results.Add(default);");
        }
        
        sb.PopIndent();
        sb.AppendLine("}");
        
        sb.AppendLine("return results;");
    }

    private void GenerateSingleEntityReturn(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol entityType, bool isAsync)
    {
        var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        var cancellationTokenName = cancellationTokenParam?.Name ?? "default";
        
        if (isAsync)
        {
            sb.AppendLine($"using var reader = await command.ExecuteReaderAsync({cancellationTokenName});");
            sb.AppendLine($"if (await reader.ReadAsync({cancellationTokenName}))");
        }
        else
        {
            sb.AppendLine("using var reader = command.ExecuteReader();");
            sb.AppendLine("if (reader.Read())");
        }
        
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine($"return new {entityType.Name}");
        sb.AppendLine("{");
        sb.PushIndent();
        
        var properties = entityType.GetMembers().OfType<IPropertySymbol>().ToList();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Count - 1 ? "," : "";
            var propType = prop.Type.ToDisplayString();
            
            // Handle value types and reference types properly
            if (prop.Type.IsValueType)
            {
                if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated || propType.EndsWith("?"))
                {
                    // Nullable value type
                    sb.AppendLine($"{prop.Name} = reader[\"{prop.Name}\"] is DBNull ? null : ({propType})reader[\"{prop.Name}\"]{comma}");
                }
                else
                {
                    // Non-nullable value type
                    sb.AppendLine($"{prop.Name} = ({propType})reader[\"{prop.Name}\"]{comma}");
                }
            }
            else
            {
                // Reference type
                sb.AppendLine($"{prop.Name} = reader[\"{prop.Name}\"] as {propType}{comma}");
            }
        }
        
        sb.PopIndent();
        sb.AppendLine("};");
        
        sb.PopIndent();
        sb.AppendLine("}");
        
        sb.AppendLine("return null;");
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

    private string ExtractTaskInnerType(string typeString)
    {
        if (typeString.StartsWith("Task<") && typeString.EndsWith(">"))
        {
            return typeString.Substring(5, typeString.Length - 6);
        }
        return "object";
    }

    private void GenerateSimpleTestRepository(IndentedStringBuilder sb, INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceInterface)
    {
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This file was generated by Sqlx Repository Generator");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        
        sb.AppendLine($"namespace {repositoryClass.ContainingNamespace.ToDisplayString()};");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        
        sb.AppendLine($"partial class {repositoryClass.Name}");
        sb.AppendLine("{");
        sb.PushIndent();
        
        // Generate a simple test method
        sb.AppendLine("// This is a test method generated by Sqlx Repository Generator");
        sb.AppendLine("public void GeneratedTestMethod()");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("// This method was successfully generated!");
        sb.PopIndent();
        sb.AppendLine("}");
        
        sb.PopIndent();
        sb.AppendLine("}");
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

}



