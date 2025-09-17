// -----------------------------------------------------------------------
// <copyright file="TypeInferenceService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Core;

/// <summary>
/// Default implementation of type inference service.
/// </summary>
public class TypeInferenceService : ITypeInferenceService
{
    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromServiceInterface(INamedTypeSymbol serviceInterface)
    {
        // First, try to get entity type from generic interface like IRepository<T>
        if (serviceInterface.IsGenericType && serviceInterface.TypeArguments.Length > 0)
        {
            var firstTypeArg = serviceInterface.TypeArguments[0];
            if (firstTypeArg is INamedTypeSymbol entityType &&
                entityType.TypeKind == TypeKind.Class)
            {
                return entityType;
            }
        }

        // Try to infer from interface methods
        var candidateTypes = new Dictionary<INamedTypeSymbol, int>();

        foreach (var method in serviceInterface.GetMembers().OfType<IMethodSymbol>())
        {
            AnalyzeMethodForEntityTypes(method, candidateTypes);
        }

        var bestCandidate = SelectBestEntityCandidate(candidateTypes);
        if (bestCandidate != null)
        {
            return bestCandidate;
        }

        // Try to infer from interface name pattern
        return InferFromInterfaceName(serviceInterface);
    }

    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method)
    {
        // Check parameters for entity types
        foreach (var param in method.Parameters)
        {
            if (param.Type.TypeKind == TypeKind.Class &&
                param.Type.Name != "String" &&
                param.Type.Name != "CancellationToken" &&
                !IsSystemType(param.Type))
            {
                return param.Type as INamedTypeSymbol;
            }

            // Check for collection of entities
            if (TypeAnalyzer.IsCollectionType(param.Type) &&
                param.Type is INamedTypeSymbol collectionType &&
                collectionType.TypeArguments.Length > 0)
            {
                var elementType = collectionType.TypeArguments[0];
                if (elementType.TypeKind == TypeKind.Class &&
                    elementType.Name != "String" &&
                    !IsSystemType(elementType))
                {
                    return elementType as INamedTypeSymbol;
                }
            }
        }

        // Check return type
        var returnType = method.ReturnType;
        if (returnType is INamedTypeSymbol taskType && taskType.Name == "Task" && taskType.TypeArguments.Length > 0)
        {
            returnType = taskType.TypeArguments[0];
        }

        if (TypeAnalyzer.IsCollectionType(returnType) &&
            returnType is INamedTypeSymbol collectionReturnType &&
            collectionReturnType.TypeArguments.Length > 0)
        {
            var elementType = collectionReturnType.TypeArguments[0];
            if (elementType.TypeKind == TypeKind.Class &&
                elementType.Name != "String" &&
                !IsSystemType(elementType))
            {
                return elementType as INamedTypeSymbol;
            }
        }

        if (returnType.TypeKind == TypeKind.Class &&
            returnType.Name != "String" &&
            !IsSystemType(returnType) &&
            !TypeAnalyzer.IsCollectionType(returnType))
        {
            return returnType as INamedTypeSymbol;
        }

        return null;
    }

    /// <inheritdoc/>
    public INamedTypeSymbol? GetServiceInterfaceFromSyntax(INamedTypeSymbol repositoryClass, Compilation compilation)
    {
        // First, check for generic base classes or interfaces
        foreach (var baseInterface in repositoryClass.AllInterfaces)
        {
            if (baseInterface.IsGenericType)
            {
                var interfaceName = baseInterface.ConstructedFrom.Name;
                if (interfaceName.Contains("Repository") || interfaceName.Contains("Service"))
                    return baseInterface;
            }
        }

        // Check for RepositoryFor attribute
        var repositoryForAttr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");

        if (repositoryForAttr != null && repositoryForAttr.ConstructorArguments.Length > 0)
        {
            var typeArg = repositoryForAttr.ConstructorArguments[0];
            if (typeArg.Value is INamedTypeSymbol serviceType)
                return serviceType;

            // Handle typeof(Interface) pattern
            if (typeArg.Type?.Name == "Type" && typeArg.Value != null)
            {
                var typeName = typeArg.Value.ToString();
                return FindInterfaceByName(compilation, typeName);
            }
        }

        // Try to resolve from generic service interface pattern
        var genericServiceInterface = repositoryClass.AllInterfaces
            .FirstOrDefault(i => i.IsGenericType &&
                               (i.Name.Contains("Repository") || i.Name.Contains("Service")));

        if (genericServiceInterface != null)
        {
            return ResolveGenericServiceInterface(genericServiceInterface, repositoryClass);
        }

        // Fallback: try to infer from class name
        var className = repositoryClass.Name;
        if (className.EndsWith("Repository"))
        {
            var serviceName = "I" + className.Substring(0, className.Length - "Repository".Length) + "Service";
            return FindInterfaceByName(compilation, serviceName);
        }

        return null;
    }

    /// <inheritdoc/>
    public string GetTableNameFromEntity(INamedTypeSymbol? entityType, INamedTypeSymbol? tableNameAttributeSymbol)
    {
        if (entityType == null) return "UnknownTable";

        // Check for TableName attribute
        if (tableNameAttributeSymbol != null)
        {
            var tableNameAttr = entityType.GetAttributes()
                .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, tableNameAttributeSymbol));

            if (tableNameAttr != null && tableNameAttr.ConstructorArguments.Length > 0)
            {
                var tableName = tableNameAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(tableName))
                {
                    return tableName!;
                }
            }
        }

        // Default to entity name
        return entityType.Name;
    }

    /// <inheritdoc/>
    public string GetTableName(INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceType, INamedTypeSymbol? tableNameAttributeSymbol)
    {
        // First try to get table name from entity type
        var entityType = InferEntityTypeFromServiceInterface(serviceType);

        if (entityType != null)
        {
            return GetTableNameFromEntity(entityType, tableNameAttributeSymbol);
        }

        // Fallback to service interface name
        return GetTableNameFromInterfaceName(serviceType.Name);
    }

    private void AnalyzeMethodForEntityTypes(IMethodSymbol method, Dictionary<INamedTypeSymbol, int> candidateTypes)
    {
        // Analyze parameters
        foreach (var param in method.Parameters)
        {
            var entityType = GetEntityTypeFromSymbol(param.Type);
            if (entityType != null)
            {
                candidateTypes[entityType] = candidateTypes.ContainsKey(entityType) ? candidateTypes[entityType] + 2 : 2; // Parameters are strong indicators
            }
        }

        // Analyze return type
        var returnEntityType = GetEntityTypeFromSymbol(method.ReturnType);
        if (returnEntityType != null)
        {
            candidateTypes[returnEntityType] = candidateTypes.ContainsKey(returnEntityType) ? candidateTypes[returnEntityType] + 1 : 1;
        }
    }

    private INamedTypeSymbol? GetEntityTypeFromSymbol(ITypeSymbol type)
    {
        // Handle Task<T>
        if (type is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length > 0)
        {
            type = namedType.TypeArguments[0];
        }

        // Handle collections
        if (TypeAnalyzer.IsCollectionType(type) && type is INamedTypeSymbol collectionType && collectionType.TypeArguments.Length > 0)
        {
            type = collectionType.TypeArguments[0];
        }

        // Check if it's a valid entity type
        if (type.TypeKind == TypeKind.Class &&
            type.Name != "String" &&
            !IsSystemType(type))
        {
            return type as INamedTypeSymbol;
        }

        return null;
    }

    private INamedTypeSymbol? SelectBestEntityCandidate(Dictionary<INamedTypeSymbol, int> candidateTypes)
        => !candidateTypes.Any() ? null : candidateTypes.OrderByDescending(kvp => kvp.Value).First().Key;

    private INamedTypeSymbol? InferFromInterfaceName(INamedTypeSymbol serviceInterface)
    {
        var interfaceName = serviceInterface.Name;

        // Remove 'I' prefix and common suffixes
        if (interfaceName.StartsWith("I") && interfaceName.Length > 1)
        {
            interfaceName = interfaceName.Substring(1);
        }

        var suffixes = new[] { "Service", "Repository", "Manager", "Handler" };
        foreach (var suffix in suffixes)
        {
            if (interfaceName.EndsWith(suffix))
            {
                interfaceName = interfaceName.Substring(0, interfaceName.Length - suffix.Length);
                break;
            }
        }

        return string.IsNullOrEmpty(interfaceName) ? null : FindTypeByName(serviceInterface, interfaceName);
    }

    private INamedTypeSymbol? FindTypeByName(INamedTypeSymbol contextType, string entityName)
    {
        var compilation = contextType.ContainingAssembly.GlobalNamespace.ContainingCompilation;
        if (compilation == null) return null;

        // Search in the same namespace first
        var containingNamespace = contextType.ContainingNamespace;
        var entityType = FindEntityTypeByName(containingNamespace, entityName);
        if (entityType != null) return entityType;

        // Search in common entity namespaces
        var commonNamespaces = new[]
        {
            "Models",
            "Entities",
            "Domain",
            "Core",
            $"{containingNamespace.Name}.Models",
            $"{containingNamespace.Name}.Entities"
        };

        foreach (var namespaceName in commonNamespaces)
        {
            var namespaceSymbol = compilation.GetTypeByMetadataName($"{namespaceName}.{entityName}");
            if (namespaceSymbol != null)
            {
                return namespaceSymbol;
            }
        }

        return null;
    }

    private INamedTypeSymbol? FindInterfaceByName(Compilation compilation, string interfaceName)
    {
        // Search across all assemblies
        foreach (var assembly in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(assembly) as IAssemblySymbol;
            if (assemblySymbol != null)
            {
                var type = FindTypeInNamespace(assemblySymbol.GlobalNamespace, interfaceName);
                if (type != null)
                {
                    return type;
                }
            }
        }

        // Search in current compilation
        return FindTypeInNamespace(compilation.GlobalNamespace, interfaceName);
    }

    private static INamedTypeSymbol? ResolveGenericServiceInterface(INamedTypeSymbol genericInterface, INamedTypeSymbol repositoryClass)
    {
        // For generic interfaces, return the constructed interface
        return genericInterface;
    }

    private static INamedTypeSymbol? FindEntityTypeByName(INamespaceSymbol startingNamespace, string entityName)
    {
        // Search in current namespace
        var type = FindTypeInNamespace(startingNamespace, entityName);
        if (type != null) return type;

        // Search in child namespaces
        return startingNamespace.GetNamespaceMembers().Select(x => FindEntityTypeByName(x, entityName)).Where(x => x != null).FirstOrDefault();
    }

    private static INamedTypeSymbol? FindTypeInNamespace(INamespaceSymbol namespaceSymbol, string typeName)
    {
        var type = namespaceSymbol.GetTypeMembers(typeName).FirstOrDefault();
        if (type != null)
        {
            return type;
        }

        // Search in nested namespaces
        return namespaceSymbol.GetNamespaceMembers().Select(x => FindTypeInNamespace(x, typeName)).Where(x => x != null).FirstOrDefault();
    }

    private static string GetTableNameFromInterfaceName(string interfaceName)
    {
        // Remove 'I' prefix
        if (interfaceName.StartsWith("I") && interfaceName.Length > 1) interfaceName = interfaceName.Substring(1);

        // Remove common suffixes
        var suffixes = new[] { "Service", "Repository", "Manager", "Handler" };
        foreach (var suffix in suffixes)
        {
            if (interfaceName.EndsWith(suffix))
            {
                interfaceName = interfaceName.Substring(0, interfaceName.Length - suffix.Length);
                break;
            }
        }

        // Convert to plural form (simple heuristic)
        if (!interfaceName.EndsWith("s") && !interfaceName.EndsWith("es"))
        {
            if (interfaceName.EndsWith("y"))
            {
                interfaceName = interfaceName.Substring(0, interfaceName.Length - 1) + "ies";
            }
            else
            {
                interfaceName += "s";
            }
        }

        return interfaceName;
    }


    private bool IsSystemType(ITypeSymbol type)
    {
        return type.ContainingNamespace?.ToDisplayString().StartsWith("System") == true ||
               type.SpecialType != SpecialType.None;
    }
}
