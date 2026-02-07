// <copyright file="RepositoryGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

/// <summary>
/// Source generator that creates repository implementations for classes marked with [RepositoryFor].
/// </summary>
/// <remarks>
/// <para>
/// This generator produces complete repository implementations including:
/// </para>
/// <list type="bullet">
/// <item><description>Static SqlTemplate fields prepared at initialization</description></item>
/// <item><description>Method implementations for all [SqlTemplate] decorated interface methods</description></item>
/// <item><description>Parameter binding using generated IParameterBinder</description></item>
/// <item><description>Result reading using generated IResultReader</description></item>
/// <item><description>Activity tracking for observability</description></item>
/// <item><description>Interceptor hooks for logging and debugging</description></item>
/// </list>
/// <para>
/// Required attributes on the repository class:
/// </para>
/// <list type="bullet">
/// <item><description>[RepositoryFor(typeof(IServiceInterface))] - Specifies the interface to implement</description></item>
/// <item><description>[SqlDefine(SqlDefineTypes.XXX)] - Specifies the database dialect</description></item>
/// <item><description>[TableName("table_name")] - Specifies the database table name</description></item>
/// </list>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public class RepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetTarget(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static ClassDeclarationSyntax? GetTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        foreach (var attrList in classDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name is "RepositoryFor" or "RepositoryForAttribute")
                    return classDecl;
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty) return;

        var repositoryForAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryForAttribute");
        var sqlDefineAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlDefineAttribute");
        var tableNameAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.TableNameAttribute");
        var sqlTemplateAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlTemplateAttribute");
        var returnInsertedIdAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ReturnInsertedIdAttribute");
        var expressionToSqlAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute");

        if (repositoryForAttr is null) return;

        foreach (var classDecl in classes.Distinct())
        {
            if (classDecl is null) continue;

            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol) continue;

            var repoForAttrData = typeSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, repositoryForAttr));
            if (repoForAttrData is null) continue;

            var serviceType = repoForAttrData.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
            if (serviceType is null) continue;

            // Get dialect and table name with priority: RepositoryFor > SqlDefine/TableName attributes > inferred
            var sqlDefine = GetSqlDefineFromRepositoryFor(repoForAttrData) ?? GetSqlDefine(typeSymbol, sqlDefineAttr);
            var tableName = GetTableNameFromRepositoryFor(repoForAttrData) ?? GetTableName(typeSymbol, tableNameAttr, serviceType);
            var entityType = GetEntityType(serviceType);

            if (entityType is null) continue;

            var source = GenerateSource(typeSymbol, serviceType, entityType, sqlDefine, tableName, 
                sqlTemplateAttr, returnInsertedIdAttr, expressionToSqlAttr, compilation);
            context.AddSource($"{typeSymbol.Name}.Repository.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }
    
    private static string? GetSqlDefineFromRepositoryFor(AttributeData repoForAttrData)
    {
        var dialectArg = repoForAttrData.NamedArguments.FirstOrDefault(a => a.Key == "Dialect");
        return dialectArg.Value.Value is int dialectValue ? MapDialectEnum(dialectValue) : null;
    }
    
    private static string? GetTableNameFromRepositoryFor(AttributeData repoForAttrData)
    {
        var tableNameArg = repoForAttrData.NamedArguments.FirstOrDefault(a => a.Key == "TableName");
        return tableNameArg.Value.Value as string;
    }

    private static string GetSqlDefine(INamedTypeSymbol typeSymbol, INamedTypeSymbol? sqlDefineAttr)
    {
        if (sqlDefineAttr is null) return "SQLite";
        var attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlDefineAttr));
        if (attr?.ConstructorArguments.Length > 0)
        {
            var value = attr.ConstructorArguments[0].Value;
            return value is int intValue ? MapDialectEnum(intValue) ?? "SQLite" : value?.ToString() ?? "SQLite";
        }
        return "SQLite";
    }

    private static string? MapDialectEnum(int dialectValue) => dialectValue switch
    {
        0 => "MySql",
        1 => "SqlServer",
        2 => "PostgreSql",
        3 => "Oracle",
        4 => "DB2",
        5 => "SQLite",
        _ => null
    };

    private static string GetTableName(INamedTypeSymbol typeSymbol, INamedTypeSymbol? tableNameAttr, INamedTypeSymbol? serviceType)
    {
        // Priority 1: Try to get table name from [TableName] attribute on repository class
        if (tableNameAttr is not null)
        {
            var attr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, tableNameAttr));
            if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                return name;
        }
        
        // Priority 2: Try to get table name from [TableName] attribute on entity class
        var entityType = serviceType is not null ? GetEntityType(serviceType) : null;
        if (entityType is not null && tableNameAttr is not null)
        {
            var entityAttr = entityType.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, tableNameAttr));
            if (entityAttr?.ConstructorArguments.Length > 0 && entityAttr.ConstructorArguments[0].Value is string entityTableName)
                return entityTableName;
        }
        
        // Priority 3: Infer from entity type name
        if (entityType is not null)
        {
            return entityType.Name;
        }
        
        // Priority 4: Use repository class name without "Repository" suffix
        var repoName = typeSymbol.Name;
        if (repoName.EndsWith("Repository"))
            return repoName.Substring(0, repoName.Length - "Repository".Length);
        
        return repoName;
    }
    
    private static INamedTypeSymbol? GetServiceType(INamedTypeSymbol typeSymbol)
    {
        // Get the service type from [RepositoryFor] attribute
        var repositoryForAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryForAttribute");
        return repositoryForAttr?.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
    }

    private static INamedTypeSymbol? GetEntityType(INamedTypeSymbol serviceType)
    {
        // Look for ICrudRepository<TEntity, TKey> or similar
        foreach (var iface in serviceType.AllInterfaces.Concat(new[] { serviceType }))
        {
            if (iface.IsGenericType && iface.TypeArguments.Length >= 1)
            {
                var name = iface.OriginalDefinition.ToDisplayString();
                if (name.Contains("ICrudRepository") || name.Contains("IQueryRepository") || 
                    name.Contains("ICommandRepository") || name.Contains("IRepository"))
                {
                    return iface.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }
        return null;
    }

    private static INamedTypeSymbol? GetKeyType(INamedTypeSymbol serviceType)
    {
        foreach (var iface in serviceType.AllInterfaces.Concat(new[] { serviceType }))
        {
            if (iface.IsGenericType && iface.TypeArguments.Length >= 2)
            {
                var name = iface.OriginalDefinition.ToDisplayString();
                if (name.Contains("ICrudRepository") || name.Contains("IQueryRepository") || 
                    name.Contains("ICommandRepository") || name.Contains("IRepository"))
                {
                    return iface.TypeArguments[1] as INamedTypeSymbol;
                }
            }
        }
        return null;
    }

    private static string GenerateSource(
        INamedTypeSymbol repoType,
        INamedTypeSymbol serviceType,
        INamedTypeSymbol entityType,
        string sqlDefine,
        string tableName,
        INamedTypeSymbol? sqlTemplateAttr,
        INamedTypeSymbol? returnInsertedIdAttr,
        INamedTypeSymbol? expressionToSqlAttr,
        Compilation compilation)
    {
        var ns = repoType.ContainingNamespace.IsGlobalNamespace ? null : repoType.ContainingNamespace.ToDisplayString();
        var repoName = repoType.Name;
        var repoFullName = repoType.ToDisplayString();
        var entityFullName = entityType.ToDisplayString();
        var entityName = entityType.Name;
        var keyType = GetKeyType(serviceType);
        var keyTypeName = keyType?.ToDisplayString() ?? "int";
        var entityNs = entityType.ContainingNamespace.IsGlobalNamespace ? null : entityType.ContainingNamespace.ToDisplayString();

        var sb = new IndentedStringBuilder(null);
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace " + (ns ?? "Global") + ";");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Data.Common;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Sqlx;");
        // Add entity namespace if different from repository namespace
        if (entityNs is not null && entityNs != ns)
        {
            sb.AppendLine($"using {entityNs};");
        }
        sb.AppendLine();

        sb.AppendLine($"public partial class {repoName}");
        sb.AppendLine("{");
        sb.PushIndent();

        // Find or generate DbConnection field
        var connectionInfo = FindOrGenerateDbConnection(repoType, sb);

        // Generate Transaction property only if not already defined by user
        if (!HasMember(repoType, "Transaction"))
        {
            sb.AppendLine("/// <summary>Gets or sets the transaction for this repository.</summary>");
            sb.AppendLine("public DbTransaction? Transaction { get; set; }");
            sb.AppendLine();
        }

        // Generate static PlaceholderContext
        GeneratePlaceholderContext(sb, repoType, entityName, entityFullName, sqlDefine, tableName);
        sb.AppendLine();

        // Generate static SqlTemplate fields for each method
        var methods = GetMethodsWithSqlTemplate(serviceType, sqlTemplateAttr);
        var methodFieldNames = BuildMethodFieldNames(methods);
        
        // Generate static parameter name fields
        var paramNameFields = GenerateParameterNameFields(sb, methods, entityName, sqlDefine);
        if (paramNameFields.Count > 0)
            sb.AppendLine();
        
        GenerateSqlTemplateFields(sb, methods, sqlTemplateAttr, methodFieldNames, entityName);
        sb.AppendLine();

        // Note: We use the generic ResultReader generated by SqlxGenerator instead of generating method-specific ones
        // This reduces code duplication and simplifies maintenance

        // Generate method implementations (skip methods already implemented by user)
        foreach (var method in methods)
        {
            // Skip if method is already implemented in the repository class
            if (IsMethodAlreadyImplemented(repoType, method))
            {
                continue;
            }
            
            GenerateMethodImplementation(sb, method, repoFullName, entityFullName, entityName, keyTypeName, 
                sqlTemplateAttr, returnInsertedIdAttr, expressionToSqlAttr, connectionInfo, methodFieldNames, paramNameFields);
            sb.AppendLine();
        }

        // Generate interceptor partial methods
        GenerateInterceptorMethods(sb);

        sb.PopIndent();
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Builds a mapping from method signature to unique field name.
    /// </summary>
    private static Dictionary<string, string> BuildMethodFieldNames(List<IMethodSymbol> methods)
    {
        var result = new Dictionary<string, string>();
        var nameCounts = new Dictionary<string, int>();

        foreach (var method in methods)
        {
            var signature = GetMethodSignature(method);
            var baseName = ToCamelCase(method.Name);
            
            if (!nameCounts.TryGetValue(method.Name, out var count))
            {
                nameCounts[method.Name] = 1;
                result[signature] = $"_{baseName}Template";
            }
            else
            {
                nameCounts[method.Name] = count + 1;
                result[signature] = $"_{baseName}{count + 1}Template";
            }
        }

        return result;
    }

    /// <summary>
    /// Information about the DbConnection source.
    /// </summary>
    private readonly record struct ConnectionInfo(string? AccessExpression, bool FromMethodParameter);

    /// <summary>
    /// Finds the DbConnection source in the repository class, or generates one if needed.
    /// Priority: field > property > primary constructor parameter (auto-generate field)
    /// </summary>
    private static ConnectionInfo FindOrGenerateDbConnection(INamedTypeSymbol repoType, IndentedStringBuilder sb)
    {
        // 1. Check fields first (exclude compiler-generated backing fields)
        foreach (var member in repoType.GetMembers())
        {
            if (member is IFieldSymbol field && !field.IsStatic && !field.IsImplicitlyDeclared && IsDbConnectionType(field.Type))
            {
                return new ConnectionInfo
                {
                    AccessExpression = field.Name,
                    FromMethodParameter = false
                };
            }
        }

        // 2. Check properties
        foreach (var member in repoType.GetMembers())
        {
            if (member is IPropertySymbol prop && !prop.IsStatic && prop.GetMethod != null && IsDbConnectionType(prop.Type))
            {
                return new ConnectionInfo
                {
                    AccessExpression = prop.Name,
                    FromMethodParameter = false
                };
            }
        }

        // 3. Check primary constructor parameters (C# 12+) and auto-generate field
        if (repoType.InstanceConstructors.Length > 0)
        {
            var primaryCtor = repoType.InstanceConstructors
                .FirstOrDefault(c => c.Parameters.Length > 0 && c.DeclaringSyntaxReferences.Length > 0);
            
            if (primaryCtor != null)
            {
                var connectionParam = primaryCtor.Parameters.FirstOrDefault(p => IsDbConnectionType(p.Type));
                if (connectionParam != null)
                {
                    // Generate field from primary constructor parameter
                    var connectionTypeName = connectionParam.Type.ToDisplayString();
                    sb.AppendLine($"/// <summary>Database connection from primary constructor.</summary>");
                    sb.AppendLine($"private readonly {connectionTypeName} _connection = {connectionParam.Name};");
                    sb.AppendLine();
                    
                    return new ConnectionInfo
                    {
                        AccessExpression = "_connection",
                        FromMethodParameter = false
                    };
                }
            }
        }

        // No class-level connection found, generate a default field
        sb.AppendLine("/// <summary>Database connection (auto-generated).</summary>");
        sb.AppendLine("private readonly System.Data.Common.DbConnection _connection = null!;");
        sb.AppendLine();
        
        return new ConnectionInfo
        {
            AccessExpression = "_connection",
            FromMethodParameter = false
        };
    }

    /// <summary>
    /// Finds the DbConnection source in the repository class.
    /// Priority: property > constructor parameter (method parameter checked per-method)
    /// </summary>
    private static ConnectionInfo FindDbConnection(INamedTypeSymbol repoType)
    {
        // 1. Check properties first
        foreach (var member in repoType.GetMembers())
        {
            if (member is IPropertySymbol prop && !prop.IsStatic && prop.GetMethod != null && IsDbConnectionType(prop.Type))
            {
                return new ConnectionInfo
                {
                    AccessExpression = prop.Name,
                    FromMethodParameter = false
                };
            }
        }

        // 2. Check fields
        foreach (var member in repoType.GetMembers())
        {
            if (member is IFieldSymbol field && !field.IsStatic && IsDbConnectionType(field.Type))
            {
                return new ConnectionInfo
                {
                    AccessExpression = field.Name,
                    FromMethodParameter = false
                };
            }
        }

        // 3. Check primary constructor parameters (C# 12+)
        if (repoType.InstanceConstructors.Length > 0)
        {
            var primaryCtor = repoType.InstanceConstructors
                .FirstOrDefault(c => c.Parameters.Length > 0 && c.DeclaringSyntaxReferences.Length > 0);
            
            if (primaryCtor != null)
            {
                foreach (var param in primaryCtor.Parameters)
                {
                    if (IsDbConnectionType(param.Type))
                    {
                        // Use parameter name directly (primary constructor captures it)
                        return new ConnectionInfo
                        {
                            AccessExpression = param.Name,
                            FromMethodParameter = false
                        };
                    }
                }
            }
        }

        // No class-level connection found, will need method parameter
        return new ConnectionInfo
        {
            AccessExpression = null,
            FromMethodParameter = true
        };
    }

    /// <summary>
    /// Gets the connection access expression for a specific method.
    /// Checks method parameter first, then falls back to class-level connection.
    /// </summary>
    private static string GetConnectionExpression(IMethodSymbol method, ConnectionInfo classConnectionInfo)
    {
        var methodParam = method.Parameters.FirstOrDefault(p => IsDbConnectionType(p.Type));
        return methodParam?.Name ?? classConnectionInfo.AccessExpression ?? "_connection";
    }

    /// <summary>
    /// Checks if a type is DbConnection or derives from it.
    /// </summary>
    private static bool IsDbConnectionType(ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            var name = current.ToDisplayString();
            if (name == "System.Data.Common.DbConnection" || name.EndsWith("Connection"))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Checks if a type has a member (field or property) with the given name.
    /// </summary>
    private static bool HasMember(INamedTypeSymbol type, string memberName)
    {
        return type.GetMembers(memberName).Length > 0;
    }

    private static void GeneratePlaceholderContext(IndentedStringBuilder sb, INamedTypeSymbol repoType, string entityName, string entityFullName, string sqlDefine, string tableName)
    {
        sb.AppendLine("// Static context - shared across all methods that don't need {{var}} support");
        sb.AppendLine($"private const global::Sqlx.Annotations.SqlDefineTypes _dialectType = global::Sqlx.Annotations.SqlDefineTypes.{sqlDefine};");
        sb.AppendLine($"private static readonly global::Sqlx.PlaceholderContext _staticContext = new global::Sqlx.PlaceholderContext(");
        sb.PushIndent();
        sb.AppendLine($"dialect: global::Sqlx.SqlDefine.{sqlDefine},");
        sb.AppendLine($"tableName: \"{tableName}\",");
        sb.AppendLine($"columns: {entityName}EntityProvider.Default.Columns);");
        sb.PopIndent();
        sb.AppendLine($"private static readonly string _paramPrefix = _staticContext.Dialect.ParameterPrefix;");
        sb.AppendLine();
        
        // Check if repository has [SqlxVar] methods
        var hasVarMethods = HasSqlxVarMethods(repoType);
        
        if (hasVarMethods)
        {
            sb.AppendLine("// Dynamic context - cached instance (no lock needed, worst case is creating multiple instances)");
            sb.AppendLine("private global::Sqlx.PlaceholderContext? _dynamicContext;");
            sb.AppendLine();
            sb.AppendLine("private global::Sqlx.PlaceholderContext GetDynamicContext()");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("if (_dynamicContext != null) return _dynamicContext;");
            sb.AppendLine();
            sb.AppendLine("var context = new global::Sqlx.PlaceholderContext(");
            sb.PushIndent();
            sb.AppendLine($"_staticContext.Dialect,");
            sb.AppendLine($"_staticContext.TableName,");
            sb.AppendLine($"_staticContext.Columns,");
            sb.AppendLine($"varProvider: GetVarValue,");
            sb.AppendLine($"instance: this);");
            sb.PopIndent();
            sb.AppendLine();
            sb.AppendLine("_dynamicContext = context;");
            sb.AppendLine("return context;");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();
            
            // Generate VarProvider method
            sb.AppendLine("// Variable provider for {{var}} placeholder support");
            sb.AppendLine("private string GetVarValue(object instance, string variableName)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("return variableName switch");
            sb.AppendLine("{");
            sb.PushIndent();
            
            // Generate cases for each [SqlxVar] method
            var varMethods = GetSqlxVarMethods(repoType);
            foreach (var method in varMethods)
            {
                var varName = GetVarName(method);
                sb.AppendLine($"\"{varName}\" => {method.Name}(),");
            }
            
            sb.AppendLine("_ => throw new ArgumentException($\"Unknown variable: {variableName}\", nameof(variableName))");
            sb.PopIndent();
            sb.AppendLine("};");
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }
    
    /// <summary>
    /// Checks if the repository type has any [SqlxVar] methods.
    /// </summary>
    private static bool HasSqlxVarMethods(INamedTypeSymbol repoType)
    {
        foreach (var member in repoType.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                var attrs = method.GetAttributes();
                if (attrs.Any(a => a.AttributeClass?.Name == "SqlxVarAttribute"))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Gets all [SqlxVar] methods from the repository type.
    /// </summary>
    private static List<IMethodSymbol> GetSqlxVarMethods(INamedTypeSymbol repoType)
    {
        var methods = new List<IMethodSymbol>();
        foreach (var member in repoType.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                var attrs = method.GetAttributes();
                if (attrs.Any(a => a.AttributeClass?.Name == "SqlxVarAttribute"))
                {
                    methods.Add(method);
                }
            }
        }
        return methods;
    }
    
    /// <summary>
    /// Gets the variable name from a [SqlxVar] method.
    /// Uses the attribute's constructor parameter (VariableName property).
    /// </summary>
    private static string GetVarName(IMethodSymbol method)
    {
        var attr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SqlxVarAttribute");
        
        if (attr != null)
        {
            // Check constructor argument (first parameter is variableName)
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string varName)
            {
                return varName;
            }
            
            // Fallback: check for VariableName property (though it's readonly)
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "VariableName" && namedArg.Value.Value is string name && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
        }
        
        // Default to method name
        return method.Name;
    }

    /// <summary>
    /// Generates static parameter name fields for all scalar parameters.
    /// Returns a dictionary mapping parameter name to field name.
    /// </summary>
    private static Dictionary<string, string> GenerateParameterNameFields(IndentedStringBuilder sb, List<IMethodSymbol> methods, string entityName, string sqlDefine)
    {
        var paramNames = new HashSet<string>();
        
        // Collect all scalar parameter names
        foreach (var method in methods)
        {
            foreach (var param in method.Parameters)
            {
                if (param.Name == "cancellationToken") continue;
                if (param.Type.ToDisplayString().Contains("Expression<")) continue;
                if (param.Type.ToDisplayString().Contains("IQueryable<")) continue;
                if (param.Type.ToDisplayString().Contains("SqlxQueryable<")) continue;
                if (param.Type.Name == entityName) continue;
                
                var paramTypeName = param.Type.ToDisplayString();
                if (paramTypeName.Contains("<") && paramTypeName.Contains(entityName)) continue;
                
                paramNames.Add(param.Name);
            }
        }
        
        if (paramNames.Count == 0)
            return new Dictionary<string, string>();
        
        // Get parameter prefix based on database type (compile-time constant)
        var paramPrefix = GetParameterPrefix(sqlDefine);
        
        sb.AppendLine("// Static parameter names (cached at initialization)");
        var result = new Dictionary<string, string>();
        foreach (var name in paramNames.OrderBy(n => n))
        {
            var fieldName = $"_param_{name}";
            result[name] = fieldName;
            sb.AppendLine($"private static readonly string {fieldName} = _paramPrefix + \"{name}\";");
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the parameter prefix for the specified database type.
    /// </summary>
    private static string GetParameterPrefix(string sqlDefine) => sqlDefine switch
    {
        "SqlDefineTypes.SQLite" => "@",
        "SqlDefineTypes.SqlServer" => "@",
        "SqlDefineTypes.MySql" => "@",
        "SqlDefineTypes.PostgreSql" => "$",
        "SqlDefineTypes.Oracle" => ":",
        "SqlDefineTypes.DB2" => "@",
        _ => "@" // Default to @ for unknown types
    };

    private static List<IMethodSymbol> GetMethodsWithSqlTemplate(INamedTypeSymbol serviceType, INamedTypeSymbol? sqlTemplateAttr)
    {
        var methods = new List<IMethodSymbol>();
        if (sqlTemplateAttr is null) return methods;

        var processedSignatures = new HashSet<string>();

        // First, get methods from the direct interface (higher priority)
        var directMembers = serviceType.GetMembers().OfType<IMethodSymbol>().ToList();
        foreach (var member in directMembers)
        {
            var attrs = member.GetAttributes();
            var hasSqlTemplate = attrs.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlTemplateAttr));
            var isSpecial = IsSpecialMethod(member);
            
            if (hasSqlTemplate || isSpecial)
            {
                var signature = GetMethodSignature(member);
                processedSignatures.Add(signature);
                methods.Add(member);
            }
        }

        // Then, get methods from base interfaces (lower priority, skip if same signature)
        foreach (var iface in serviceType.AllInterfaces)
        {
            foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
            {
                var attrs = member.GetAttributes();
                var hasSqlTemplate = attrs.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlTemplateAttr));
                var isSpecial = IsSpecialMethod(member);
                
                if (hasSqlTemplate || isSpecial)
                {
                    var signature = GetMethodSignature(member);
                    if (processedSignatures.Add(signature))
                    {
                        methods.Add(member);
                    }
                }
            }
        }

        return methods;
    }

    /// <summary>
    /// Checks if a method is already implemented in the repository class.
    /// </summary>
    private static bool IsMethodAlreadyImplemented(INamedTypeSymbol repoType, IMethodSymbol method)
    {
        var signature = GetMethodSignature(method);
        
        foreach (var member in repoType.GetMembers(method.Name))
        {
            if (member is IMethodSymbol existingMethod && !existingMethod.IsAbstract)
            {
                var existingSignature = GetMethodSignature(existingMethod);
                if (existingSignature == signature)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a method is a special method that should be generated even without [SqlTemplate].
    /// Currently supports: AsQueryable()
    /// </summary>
    private static bool IsSpecialMethod(IMethodSymbol method)
    {
        // AsQueryable() method
        if (method.Name == "AsQueryable" && 
            method.Parameters.Length == 0 &&
            method.ReturnType.ToDisplayString().Contains("IQueryable<"))
        {
            return true;
        }

        return false;
    }

    private static string GetMethodSignature(IMethodSymbol method) =>
        $"{method.Name}({string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()))})";

    private static void GenerateSqlTemplateFields(IndentedStringBuilder sb, List<IMethodSymbol> methods, INamedTypeSymbol? sqlTemplateAttr, Dictionary<string, string> methodFieldNames, string entityName)
    {
        sb.AppendLine("// Static SqlTemplate fields - prepared once at initialization");
        
        foreach (var method in methods)
        {
            var template = GetSqlTemplate(method, sqlTemplateAttr);
            if (template is null) continue;

            var signature = GetMethodSignature(method);
            var fieldName = methodFieldNames[signature];

            // Check if template uses {{var}} placeholder
            if (UsesVarPlaceholder(template))
            {
                // Generate static template for the first stage (static placeholders only)
                sb.AppendLine($"// Template for {method.Name} uses {{{{var}}}} - static part prepared here, dynamic part at runtime");
                sb.AppendLine($"private static readonly global::Sqlx.SqlTemplate {fieldName}_Static = global::Sqlx.SqlTemplate.Prepare(");
                sb.PushIndent();
                sb.AppendLine($"\"{EscapeString(template)}\",");
                sb.AppendLine("_staticContext);");
                sb.PopIndent();
                continue;
            }

            // Regular template without {{var}}
            sb.AppendLine($"private static readonly global::Sqlx.SqlTemplate {fieldName} = global::Sqlx.SqlTemplate.Prepare(");
            sb.PushIndent();
            sb.AppendLine($"\"{EscapeString(template)}\",");
            sb.AppendLine("_staticContext);");
            sb.PopIndent();
            
            // Generate static ordinals struct for methods that use {{columns}} and return entities
            // Static ordinals are no longer generated - struct ordinals are created on-demand from reader
        }
    }

    /// <summary>
    /// Gets a unique field name for a method, handling overloads.
    /// </summary>
    private static string GetUniqueFieldName(IMethodSymbol method, Dictionary<string, int> fieldNameCounts)
    {
        var baseName = $"_{ToCamelCase(method.Name)}Template";
        if (!fieldNameCounts.TryGetValue(method.Name, out var count))
        {
            fieldNameCounts[method.Name] = 1;
            return baseName;
        }
        fieldNameCounts[method.Name] = count + 1;
        return $"_{ToCamelCase(method.Name)}{count + 1}Template";
    }

    /// <summary>
    /// Checks if the SQL template uses {{columns}} placeholder which means column order is static.
    /// </summary>
    private static bool UsesStaticColumns(string template) =>
        template.Contains("{{columns}}") || template.Contains("{{columns ");

    /// <summary>
    /// Checks if the SQL template uses {{var}} placeholder which requires dynamic context.
    /// </summary>
    private static bool UsesVarPlaceholder(string template) =>
        template.Contains("{{var}}") || template.Contains("{{var ");

    /// <summary>
    /// Checks if the method returns an entity or list of entities.
    /// </summary>
    private static bool ReturnsEntity(IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        return returnType.Contains("Task<List<") || 
               returnType.Contains("Task<IList<") || 
               returnType.Contains("Task<IEnumerable<") ||
               !returnType.Contains("Task<int>") && 
               !returnType.Contains("Task<long>") && 
               !returnType.Contains("Task<bool>") &&
               !returnType.Contains("SqlTemplate");
    }

    private static string? GetSqlTemplate(IMethodSymbol method, INamedTypeSymbol? sqlTemplateAttr)
    {
        if (sqlTemplateAttr is null) return null;
        var attr = method.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlTemplateAttr));
        return attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string template
            ? template : null;
    }

    private static void GenerateMethodImplementation(
        IndentedStringBuilder sb,
        IMethodSymbol method,
        string repoFullName,
        string entityFullName,
        string entityName,
        string keyTypeName,
        INamedTypeSymbol? sqlTemplateAttr,
        INamedTypeSymbol? returnInsertedIdAttr,
        INamedTypeSymbol? expressionToSqlAttr,
        ConnectionInfo connectionInfo,
        Dictionary<string, string> methodFieldNames,
        Dictionary<string, string> paramNameFields)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var signature = GetMethodSignature(method);
        var fieldName = methodFieldNames[signature];
        var ordinalsFieldName = fieldName.Replace("Template", "Ordinals");
        var template = GetSqlTemplate(method, sqlTemplateAttr);
        var useStaticOrdinals = template != null && UsesStaticColumns(template) && ReturnsEntity(method);
        var isReturnInsertedId = returnInsertedIdAttr is not null && 
            method.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, returnInsertedIdAttr));

        // Get connection expression for this method (method param > property > ctor param)
        var connectionExpression = GetConnectionExpression(method, connectionInfo);

        // Check if this is a sync method (doesn't return Task)
        var isAsync = returnType.Contains("Task<") || returnType == "Task" || returnType.Contains("System.Threading.Tasks.Task");

        // Check if this is a SqlTemplate return type (debug/inspection method)
        if (returnType == "Sqlx.SqlTemplate" || returnType == "global::Sqlx.SqlTemplate" || returnType.EndsWith(".SqlTemplate"))
        {
            GenerateSqlTemplateReturnMethod(sb, method, fieldName);
            return;
        }

        // Check if this is an IQueryable return type
        if (returnType.Contains("IQueryable<") || returnType.Contains("System.Linq.IQueryable<"))
        {
            GenerateIQueryableReturnMethod(sb, method, entityFullName, connectionExpression);
            return;
        }

        // Check if this is a simple scalar method (no entity dependencies)
        // These methods don't use entity-specific readers or binders
        var isSimpleScalarMethod = IsSimpleScalarMethod(method, entityName);

        // Build parameter list
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine("/// <inheritdoc/>");
        if (isAsync)
        {
            sb.AppendLine($"public async {returnType} {methodName}({parameters})");
        }
        else
        {
            sb.AppendLine($"public {returnType} {methodName}({parameters})");
        }
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR || !SQLX_DISABLE_ACTIVITY || !SQLX_DISABLE_METRICS");
        sb.AppendLine("var startTime = Stopwatch.GetTimestamp();");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // Check if method has dynamic parameters (Expression parameters only)
        var hasDynamicParams = HasDynamicParameters(method, expressionToSqlAttr);
        
        // Generate dynamic params first (before activity) so we can use converted SQL in activity tags
        if (hasDynamicParams)
        {
            GenerateDynamicParamsDeclaration(sb, method);
        }

        // Activity tracking (uses dynamicParams if available)
        GenerateActivityStart(sb, methodName, method, hasDynamicParams);

        sb.AppendLine();
        sb.AppendLine($"using DbCommand cmd = {connectionExpression}.CreateCommand();");
        sb.AppendLine("if (Transaction != null) cmd.Transaction = Transaction;");

        // Check if template uses {{var}} placeholder
        var usesVar = template != null && UsesVarPlaceholder(template);
        
        if (usesVar)
        {
            // Template uses {{var}} - use pre-prepared static template, then render dynamic parts at runtime
            sb.AppendLine("// Template uses {{var}} - static parts already prepared, render dynamic parts at runtime");
            sb.AppendLine($"var {fieldName} = global::Sqlx.SqlTemplate.Prepare(");
            sb.PushIndent();
            sb.AppendLine($"{fieldName}_Static.Sql,");
            sb.AppendLine("GetDynamicContext());");
            sb.PopIndent();
            sb.AppendLine($"cmd.CommandText = {fieldName}.Sql;");
        }
        else if (hasDynamicParams)
        {
            // Render SQL using dynamicParams
            GenerateDynamicRender(sb, fieldName, method);
        }
        else
        {
            sb.AppendLine($"cmd.CommandText = {fieldName}.Sql;");
        }

        sb.AppendLine();

        // Bind parameters (skip entity-specific binding for simple scalar methods)
        if (isSimpleScalarMethod)
        {
            GenerateSimpleParameterBinding(sb, method, paramNameFields);
        }
        else
        {
            GenerateParameterBinding(sb, method, entityName, expressionToSqlAttr, paramNameFields);
        }

        sb.AppendLine();
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine($"OnExecuting(\"{methodName}\", cmd, {fieldName});");
        sb.AppendLine("#endif");
        sb.AppendLine();

        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Execute and return based on return type
        GenerateExecuteAndReturn(sb, method, repoFullName, entityFullName, entityName, keyTypeName, isReturnInsertedId, methodName, fieldName, useStaticOrdinals, ordinalsFieldName, isAsync);

        sb.PopIndent();
        sb.AppendLine("}");

        // Catch block
        GenerateCatchBlock(sb, repoFullName, methodName, fieldName);

        // Finally block
        GenerateFinallyBlock(sb, methodName, fieldName);

        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Checks if a method is a simple scalar method that doesn't depend on entity types.
    /// These methods only use primitive parameters and return primitive types.
    /// </summary>
    private static bool IsSimpleScalarMethod(IMethodSymbol method, string entityName)
    {
        // Check if return type is a simple scalar (int, long, bool, string, etc.)
        var returnType = method.ReturnType.ToDisplayString();
        var isScalarReturn = returnType.Contains("Task<int>") || 
                            returnType.Contains("Task<long>") || 
                            returnType.Contains("Task<bool>") ||
                            returnType.Contains("Task<string>") ||
                            returnType.Contains("Task<decimal>") ||
                            returnType.Contains("Task<double>") ||
                            returnType.Contains("Task<float>") ||
                            returnType == "int" ||
                            returnType == "long" ||
                            returnType == "bool" ||
                            returnType == "string" ||
                            returnType == "decimal" ||
                            returnType == "double" ||
                            returnType == "float";

        if (!isScalarReturn) return false;

        // Check if all parameters are simple scalars (no entity types, no expressions)
        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            
            var paramType = param.Type.ToDisplayString();
            
            // If parameter is entity type or contains entity type, not simple
            if (paramType.Contains(entityName)) return false;
            
            // If parameter is expression, not simple
            if (paramType.Contains("Expression<")) return false;
            
            // If parameter is queryable, not simple
            if (paramType.Contains("IQueryable<")) return false;
        }

        return true;
    }

    /// <summary>
    /// Generates simple parameter binding for scalar methods that don't use entity types.
    /// </summary>
    private static void GenerateSimpleParameterBinding(IndentedStringBuilder sb, IMethodSymbol method, Dictionary<string, string> paramNameFields)
    {
        sb.AppendLine("// Bind parameters");
        
        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            
            var paramType = param.Type;
            var isNullable = IsNullableType(paramType);
            var paramFieldName = paramNameFields.TryGetValue(param.Name, out var fn) ? fn : $"_paramPrefix + \"{param.Name}\"";
            
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("var p = cmd.CreateParameter();");
            sb.AppendLine($"p.ParameterName = {paramFieldName};");
            if (isNullable)
            {
                sb.AppendLine($"p.Value = {param.Name} ?? (object)DBNull.Value;");
            }
            else
            {
                sb.AppendLine($"p.Value = {param.Name};");
            }
            sb.AppendLine("cmd.Parameters.Add(p);");
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }

    private static bool HasDynamicParameters(IMethodSymbol method, INamedTypeSymbol? expressionToSqlAttr)
    {
        foreach (var param in method.Parameters)
        {
            var typeName = param.Type.ToDisplayString();
            // Check for Expression<Func<T, bool>> or Expression<Func<T, T>> parameters - these are truly dynamic
            if (typeName.Contains("Expression<"))
                return true;
        }
        return false;
    }

    private static void GenerateDynamicParamsDeclaration(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var expressionParams = method.Parameters.Where(p => p.Type.ToDisplayString().Contains("Expression<")).ToList();

        // For single parameter, use optimized variable instead of dictionary
        if (expressionParams.Count == 1)
        {
            var param = expressionParams[0];
            var paramType = param.Type.ToDisplayString();
            
            sb.AppendLine($"// Convert expression to SQL");
            
            // Check if this is a SET expression (Expression<Func<T, T>>) or WHERE expression (Expression<Func<T, bool>>)
            if (IsSetExpression(paramType))
            {
                sb.AppendLine($"var {param.Name}Sql = global::Sqlx.SetExpressionExtensions.ToSetClause({param.Name}, _staticContext.Dialect);");
                sb.AppendLine($"var {param.Name}Params = global::Sqlx.SetExpressionExtensions.GetSetParameters({param.Name});");
            }
            else
            {
                sb.AppendLine($"var {param.Name}Sql = global::Sqlx.ExpressionExtensions.ToWhereClause({param.Name}, _staticContext.Dialect);");
            }
        }
        else if (expressionParams.Count == 2)
        {
            sb.AppendLine("// Convert expressions to SQL");
            foreach (var param in expressionParams)
            {
                var paramType = param.Type.ToDisplayString();
                
                if (IsSetExpression(paramType))
                {
                    sb.AppendLine($"var {param.Name}Sql = global::Sqlx.SetExpressionExtensions.ToSetClause({param.Name}, _staticContext.Dialect);");
                    sb.AppendLine($"var {param.Name}Params = global::Sqlx.SetExpressionExtensions.GetSetParameters({param.Name});");
                }
                else
                {
                    sb.AppendLine($"var {param.Name}Sql = global::Sqlx.ExpressionExtensions.ToWhereClause({param.Name}, _staticContext.Dialect);");
                }
            }
        }
        else
        {
            // Fallback to dictionary for 3+ parameters (rare case)
            sb.AppendLine("// Create dynamic parameters for runtime rendering");
            sb.AppendLine($"var dynamicParams = new Dictionary<string, object?>({expressionParams.Count})");
            sb.AppendLine("{");
            sb.PushIndent();

            foreach (var param in expressionParams)
            {
                var paramType = param.Type.ToDisplayString();
                
                if (IsSetExpression(paramType))
                {
                    sb.AppendLine($"[\"{param.Name}\"] = global::Sqlx.SetExpressionExtensions.ToSetClause({param.Name}, _staticContext.Dialect),");
                }
                else
                {
                    sb.AppendLine($"[\"{param.Name}\"] = global::Sqlx.ExpressionExtensions.ToWhereClause({param.Name}, _staticContext.Dialect),");
                }
            }

            sb.PopIndent();
            sb.AppendLine("};");
            
            // Also get SET parameters for all SET expressions
            sb.AppendLine();
            sb.AppendLine("// Get SET expression parameters");
            foreach (var param in expressionParams)
            {
                var paramType = param.Type.ToDisplayString();
                if (IsSetExpression(paramType))
                {
                    sb.AppendLine($"var {param.Name}Params = global::Sqlx.SetExpressionExtensions.GetSetParameters({param.Name});");
                }
            }
        }
    }
    private static bool IsSetExpression(string expressionType)
    {
        // Check if the expression returns the same type as the input (Expression<Func<T, T>>)
        // This indicates a SET expression for UPDATE statements
        // Pattern: Expression<Func<SomeType, SomeType>>
        // Example: System.Linq.Expressions.Expression<System.Func<TodoWebApi.Models.Todo, TodoWebApi.Models.Todo>>
        
        if (!expressionType.Contains("Expression<") || !expressionType.Contains("Func<")) return false;
        
        // Extract the generic type arguments from Func<T1, T2>
        // Match pattern: Func<type1, type2>
        var funcMatch = System.Text.RegularExpressions.Regex.Match(
            expressionType, 
            @"Func<([^,<>]+(?:<[^>]+>)?),\s*([^,<>]+(?:<[^>]+>)?)>");
        
        if (!funcMatch.Success) return false;
        
        var inputType = funcMatch.Groups[1].Value.Trim();
        var outputType = funcMatch.Groups[2].Value.Trim();
        
        // If input and output types are the same, it's a SET expression
        // Compare the simple type names (without namespace)
        var inputSimple = inputType.Split('.').Last().Split('<')[0];
        var outputSimple = outputType.Split('.').Last().Split('<')[0];
        
        return inputSimple == outputSimple && inputType == outputType;
    }

    private static void GenerateDynamicRender(IndentedStringBuilder sb, string fieldName, IMethodSymbol method)
    {
        var expressionParams = method.Parameters.Where(p => p.Type.ToDisplayString().Contains("Expression<")).ToList();

        if (expressionParams.Count == 1)
        {
            var param = expressionParams[0];
            sb.AppendLine($"var renderedSql = {fieldName}.HasDynamicPlaceholders");
            sb.AppendLine($"    ? {fieldName}.Render(\"{param.Name}\", {param.Name}Sql)");
            sb.AppendLine($"    : {fieldName}.Sql;");
            sb.AppendLine("cmd.CommandText = renderedSql;");
        }
        else if (expressionParams.Count == 2)
        {
            var p1 = expressionParams[0];
            var p2 = expressionParams[1];
            sb.AppendLine($"var renderedSql = {fieldName}.HasDynamicPlaceholders");
            sb.AppendLine($"    ? {fieldName}.Render(\"{p1.Name}\", {p1.Name}Sql, \"{p2.Name}\", {p2.Name}Sql)");
            sb.AppendLine($"    : {fieldName}.Sql;");
            sb.AppendLine("cmd.CommandText = renderedSql;");
        }
        else
        {
            sb.AppendLine($"var renderedSql = {fieldName}.HasDynamicPlaceholders");
            sb.AppendLine($"    ? {fieldName}.Render(dynamicParams)");
            sb.AppendLine($"    : {fieldName}.Sql;");
            sb.AppendLine("cmd.CommandText = renderedSql;");
        }
    }

    private static void GenerateActivityStart(IndentedStringBuilder sb, string methodName, IMethodSymbol method, bool hasDynamicParams)
    {
        var expressionParamNames = new HashSet<string>(
            method.Parameters
                .Where(p => p.Type.ToDisplayString().Contains("Expression<"))
                .Select(p => p.Name));

        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("var activity = global::System.Diagnostics.Activity.Current;");
        sb.AppendLine("if (activity is not null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"activity = activity.AddEvent(new ActivityEvent(\"{methodName}\"));");
        sb.AppendLine("activity.SetTag(\"db.system\", _staticContext.Dialect.DatabaseType);");
        sb.AppendLine("activity.SetTag(\"db.operation\", \"sqlx.execute\");");
        sb.AppendLine("activity.SetTag(\"db.has_transaction\", Transaction != null);");
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY_PARAMS");

        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            
            var paramType = param.Type.ToDisplayString();
            
            // Skip collection types (IEnumerable, List, etc.) as they cannot be converted to string for tags
            if (IsCollectionType(param.Type, out _))
            {
                continue;
            }
            
            // For Expression parameters, use the converted SQL variable
            if (expressionParamNames.Contains(param.Name))
            {
                sb.AppendLine($"activity.SetTag(\"db.param.{param.Name}\", {param.Name}Sql);");
            }
            else
            {
                sb.AppendLine($"activity.SetTag(\"db.param.{param.Name}\", {param.Name});");
            }
        }

        sb.AppendLine("#endif");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
    }
    private static void GenerateParameterBinding(IndentedStringBuilder sb, IMethodSymbol method, string entityName, INamedTypeSymbol? expressionToSqlAttr, Dictionary<string, string> paramNameFields)
    {
        sb.AppendLine("// Bind parameters");
        
        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            if (param.Type.ToDisplayString().Contains("Expression<")) continue; // Already handled in dynamic context
            if (param.Type.ToDisplayString().Contains("IQueryable<")) continue; // Already handled in dynamic context
            if (param.Type.ToDisplayString().Contains("SqlxQueryable<")) continue; // Already handled in dynamic context

            var paramType = param.Type;
            var paramTypeName = paramType.ToDisplayString();
            
            // Skip generic types that contain the entity name but aren't the entity itself
            // e.g., ExpressionToSql<Todo>, List<Todo>, etc.
            if (paramTypeName.Contains("<") && paramTypeName.Contains(entityName))
            {
                continue;
            }
            
            // Check if this is an entity parameter (exact match or ends with entity name)
            if (paramType.Name == entityName)
            {
                sb.AppendLine($"{entityName}ParameterBinder.Default.BindEntity(cmd, {param.Name}, _paramPrefix);");
            }
            // Check if this is a collection parameter (List<T>, IEnumerable<T>, etc.)
            else if (IsCollectionType(paramType, out var elementType))
            {
                // Expand collection into multiple parameters: @ids0, @ids1, @ids2, ...
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"var index = 0;");
                sb.AppendLine($"foreach (var item in {param.Name})");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("var p = cmd.CreateParameter();");
                sb.AppendLine($"p.ParameterName = _paramPrefix + \"{param.Name}\" + index;");
                sb.AppendLine("p.Value = item;");
                sb.AppendLine("cmd.Parameters.Add(p);");
                sb.AppendLine("index++;");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.PopIndent();
                sb.AppendLine("}");
            }
            else
            {
                // Simple parameter binding for scalar types (int, string, etc.)
                var isNullable = IsNullableType(paramType);
                var paramFieldName = paramNameFields.TryGetValue(param.Name, out var fn) ? fn : $"_paramPrefix + \"{param.Name}\"";
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("var p = cmd.CreateParameter();");
                sb.AppendLine($"p.ParameterName = {paramFieldName};");
                if (isNullable)
                {
                    sb.AppendLine($"p.Value = {param.Name} ?? (object)DBNull.Value;");
                }
                else
                {
                    sb.AppendLine($"p.Value = {param.Name};");
                }
                sb.AppendLine("cmd.Parameters.Add(p);");
                sb.PopIndent();
                sb.AppendLine("}");
            }
        }
        
        // Bind SET expression parameters
        var setExpressionParams = method.Parameters
            .Where(p => p.Type.ToDisplayString().Contains("Expression<") && IsSetExpression(p.Type.ToDisplayString()))
            .ToList();
        
        if (setExpressionParams.Any())
        {
            sb.AppendLine();
            sb.AppendLine("// Bind SET expression parameters");
            foreach (var param in setExpressionParams)
            {
                sb.AppendLine($"foreach (var kvp in {param.Name}Params)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("var p = cmd.CreateParameter();");
                sb.AppendLine("p.ParameterName = _paramPrefix + kvp.Key;");
                sb.AppendLine("p.Value = kvp.Value ?? (object)DBNull.Value;");
                sb.AppendLine("cmd.Parameters.Add(p);");
                sb.PopIndent();
                sb.AppendLine("}");
            }
        }
    }

    private static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;
        
        // Check if it's a generic type
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return false;
        
        var typeName = namedType.OriginalDefinition.ToDisplayString();
        
        // Check for common collection types
        if (typeName.StartsWith("System.Collections.Generic.List<") ||
            typeName.StartsWith("System.Collections.Generic.IList<") ||
            typeName.StartsWith("System.Collections.Generic.ICollection<") ||
            typeName.StartsWith("System.Collections.Generic.IEnumerable<") ||
            typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
            typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<"))
        {
            elementType = namedType.TypeArguments.FirstOrDefault();
            return true;
        }
        
        return false;
    }

    private static bool IsNullableType(ITypeSymbol type)
    {
        // Check for nullable reference types
        if (type.NullableAnnotation == NullableAnnotation.Annotated) return true;
        // Check for Nullable<T> value types
        if (type.IsValueType && type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return true;
        // Reference types that are not annotated as non-nullable
        if (!type.IsValueType && type.NullableAnnotation != NullableAnnotation.NotAnnotated) return true;
        return false;
    }

    /// <summary>
    /// Extracts the actual return type from a method's return type.
    /// Handles Task&lt;T&gt;, List&lt;T&gt;, IList&lt;T&gt;, IEnumerable&lt;T&gt;, and nullable types.
    /// </summary>
    private static ITypeSymbol? ExtractActualReturnType(ITypeSymbol returnType)
    {
        // Unwrap Task<T>
        if (returnType is INamedTypeSymbol { IsGenericType: true } taskType &&
            (taskType.OriginalDefinition.ToDisplayString().StartsWith("System.Threading.Tasks.Task<") ||
             taskType.OriginalDefinition.ToDisplayString().StartsWith("Task<")))
        {
            returnType = taskType.TypeArguments.FirstOrDefault() ?? returnType;
        }

        // Unwrap List<T>, IList<T>, IEnumerable<T>
        if (returnType is INamedTypeSymbol { IsGenericType: true } collectionType &&
            (collectionType.OriginalDefinition.ToDisplayString().Contains("List<") ||
             collectionType.OriginalDefinition.ToDisplayString().Contains("IList<") ||
             collectionType.OriginalDefinition.ToDisplayString().Contains("IEnumerable<")))
        {
            returnType = collectionType.TypeArguments.FirstOrDefault() ?? returnType;
        }

        // Unwrap nullable types
        if (returnType.NullableAnnotation == NullableAnnotation.Annotated && returnType is INamedTypeSymbol nullableType)
        {
            return nullableType;
        }

        // Return the unwrapped type (could be entity type or custom type)
        return returnType;
    }

    /// <summary>
    /// Gets the entity type from a method's containing type (repository interface).
    /// </summary>
    private static ITypeSymbol? GetEntityTypeFromMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        
        // Find ICrudRepository<TEntity, TKey> interface
        foreach (var iface in containingType.AllInterfaces)
        {
            if (iface.IsGenericType && 
                (iface.OriginalDefinition.ToDisplayString().Contains("ICrudRepository<") ||
                 iface.OriginalDefinition.ToDisplayString().Contains("IQueryRepository<") ||
                 iface.OriginalDefinition.ToDisplayString().Contains("ICommandRepository<")))
            {
                // First type argument is TEntity
                return iface.TypeArguments.FirstOrDefault();
            }
        }

        return null;
    }

    private static void GenerateExecuteAndReturn(IndentedStringBuilder sb, IMethodSymbol method, string repoFullName, string entityFullName, string entityName, string keyTypeName, bool isReturnInsertedId, string methodName, string fieldName, bool useStaticOrdinals, string ordinalsFieldName, bool isAsync)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var ctParam = method.Parameters.FirstOrDefault(p => p.Name == "cancellationToken");
        var ctName = ctParam?.Name ?? "default";

        // Check for capacity hint parameter (limit, pageSize)
        var capacityHintParam = method.Parameters.FirstOrDefault(p => p.Name is "limit" or "pageSize");
        var capacityHint = capacityHintParam?.Name;

        // Check for tuple return type first (but not for InsertAndGetId which handles its own tuple)
        if (IsTupleReturnType(method.ReturnType) && !isReturnInsertedId)
        {
            GenerateTupleReturn(sb, method, repoFullName, entityName, ctName, methodName, fieldName);
            return;
        }

        // Normalize return type for comparison (handle both short and full type names)
        var normalizedReturnType = returnType
            .Replace("System.Threading.Tasks.", "")
            .Replace("System.Collections.Generic.", "");

        // Extract actual return type for custom types (different from entity type)
        var actualReturnType = ExtractActualReturnType(method.ReturnType);
        var entityType = GetEntityTypeFromMethod(method);
        var isCustomReturnType = actualReturnType != null && entityType != null && 
                                 !SymbolEqualityComparer.Default.Equals(actualReturnType, entityType) &&
                                 actualReturnType.TypeKind == TypeKind.Class; // Only for class types, not primitives
        
        // Use custom return type name if it's a custom type, otherwise use entity name
        var actualReturnTypeName = isCustomReturnType ? actualReturnType!.Name : entityName;
        var actualReturnTypeFullName = isCustomReturnType ? actualReturnType!.ToDisplayString() : entityFullName;

        // Check for sync TKey return (InsertAndGetId without Task) - MUST BE FIRST!
        if (!isAsync && isReturnInsertedId)
        {
            // Sync InsertAndGetId return
            sb.AppendLine("// Append last inserted ID query to the INSERT statement");
            sb.AppendLine("cmd.CommandText += _staticContext.Dialect.InsertReturningIdSuffix;");
            sb.AppendLine();
            sb.AppendLine("using var reader = cmd.ExecuteReader(System.Data.CommandBehavior.Default);");
            sb.AppendLine();
            sb.AppendLine("// For multi-statement SQL (INSERT; SELECT), move to the result set containing the ID");
            sb.AppendLine("// For RETURNING clause, the ID is already in the current result set");
            sb.AppendLine("if (!reader.Read())");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("reader.NextResult();");
            sb.AppendLine("reader.Read();");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine($"var insertedId = ({keyTypeName})Convert.ChangeType(reader.GetValue(0), typeof({keyTypeName}));");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, insertedId, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.inserted_id\", insertedId);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return insertedId;");
        }
        // Check for sync list return (List<Entity> without Task)
        else if (!isAsync && (normalizedReturnType.Contains("List<") || normalizedReturnType.Contains("IList<") || normalizedReturnType.Contains("IEnumerable<")))
        {
            // Sync list return
            sb.AppendLine("using var reader = cmd.ExecuteReader(System.Data.CommandBehavior.Default);");
            if (capacityHint != null)
            {
                sb.AppendLine($"var result = global::Sqlx.ResultReaderExtensions.ToList({actualReturnTypeName}ResultReader.Default, reader, {capacityHint});");
            }
            else
            {
                sb.AppendLine($"var result = {actualReturnTypeName}ResultReader.Default.ToList(reader);");
            }
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result.Count);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        // Check for sync single entity return (Entity? without Task) or custom return type
        else if (!isAsync && (normalizedReturnType.Contains($"{entityName}?") || normalizedReturnType.Contains($"{entityName}") ||
                 returnType.Contains($"{entityFullName}?") || returnType.Contains($"{entityFullName}") ||
                 (isCustomReturnType && (normalizedReturnType.Contains($"{actualReturnTypeName}?") || normalizedReturnType.Contains($"{actualReturnTypeName}") ||
                  returnType.Contains($"{actualReturnTypeFullName}?") || returnType.Contains($"{actualReturnTypeFullName}")))))
        {
            // Sync single entity return
            sb.AppendLine("using var reader = cmd.ExecuteReader(System.Data.CommandBehavior.Default);");
            // Check if return type is nullable
            var isNullableReturn = normalizedReturnType.Contains("?") || returnType.Contains("?") || 
                                   method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated;
            if (isNullableReturn)
            {
                sb.AppendLine($"var result = {actualReturnTypeName}ResultReader.Default.FirstOrDefault(reader);");
            }
            else
            {
                // Non-nullable return type - use null-forgiving operator
                sb.AppendLine($"var result = {actualReturnTypeName}ResultReader.Default.FirstOrDefault(reader)!;");
            }
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            if (isNullableReturn)
            {
                sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result != null ? 1 : 0);");
            }
            else
            {
                sb.AppendLine("activity?.SetTag(\"db.rows_affected\", 1);");
            }
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        // Check for sync bool return (bool without Task)
        else if (!isAsync && (normalizedReturnType == "bool" || normalizedReturnType == "Boolean" || returnType.Contains("System.Boolean")))
        {
            // Sync boolean return
            sb.AppendLine("var result = cmd.ExecuteScalar();");
            sb.AppendLine("var exists = Convert.ToInt32(result) == 1;");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, exists, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("return exists;");
        }
        // Check for sync long return (long without Task)
        else if (!isAsync && (normalizedReturnType == "long" || normalizedReturnType == "Int64" || returnType.Contains("System.Int64")))
        {
            // Sync long return (count)
            sb.AppendLine("var result = cmd.ExecuteScalar();");
            sb.AppendLine("var count = Convert.ToInt64(result);");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, count, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return count;");
        }
        // Check for sync int return (int without Task)
        else if (!isAsync && (normalizedReturnType == "int" || normalizedReturnType == "Int32" || returnType.Contains("System.Int32")))
        {
            // Sync int return (affected rows)
            sb.AppendLine("var result = cmd.ExecuteNonQuery();");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (normalizedReturnType.Contains("Task<List<") || normalizedReturnType.Contains("Task<IList<") || normalizedReturnType.Contains("Task<IEnumerable<"))
        {
            // Return list of entities
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            if (capacityHint != null)
            {
                // Use capacity hint for list pre-allocation
                sb.AppendLine($"var result = await global::Sqlx.ResultReaderExtensions.ToListAsync({actualReturnTypeName}ResultReader.Default, reader, {capacityHint}, {ctName}).ConfigureAwait(false);");
            }
            else
            {
                sb.AppendLine($"var result = await {actualReturnTypeName}ResultReader.Default.ToListAsync(reader, {ctName}).ConfigureAwait(false);");
            }
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result.Count);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (normalizedReturnType.Contains($"Task<{entityName}?>") || normalizedReturnType.Contains($"Task<{entityName}>") ||
                 returnType.Contains($"Task<{entityFullName}?>") || returnType.Contains($"Task<{entityFullName}>") ||
                 (isCustomReturnType && (normalizedReturnType.Contains($"Task<{actualReturnTypeName}?>") || normalizedReturnType.Contains($"Task<{actualReturnTypeName}>") ||
                  returnType.Contains($"Task<{actualReturnTypeFullName}?>") || returnType.Contains($"Task<{actualReturnTypeFullName}>"))))
        {
            // Return single entity
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            // Check if return type is nullable
            var isNullableReturn = normalizedReturnType.Contains("?") || returnType.Contains("?") || 
                                   method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated ||
                                   (method.ReturnType is INamedTypeSymbol { IsGenericType: true } taskType &&
                                    taskType.TypeArguments.Length > 0 &&
                                    taskType.TypeArguments[0].NullableAnnotation == NullableAnnotation.Annotated);
            if (isNullableReturn)
            {
                sb.AppendLine($"var result = await {actualReturnTypeName}ResultReader.Default.FirstOrDefaultAsync(reader, {ctName}).ConfigureAwait(false);");
            }
            else
            {
                // Non-nullable return type - use null-forgiving operator
                sb.AppendLine($"var result = (await {actualReturnTypeName}ResultReader.Default.FirstOrDefaultAsync(reader, {ctName}).ConfigureAwait(false))!;");
            }
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            if (isNullableReturn)
            {
                sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result != null ? 1 : 0);");
            }
            else
            {
                sb.AppendLine("activity?.SetTag(\"db.rows_affected\", 1);");
            }
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (normalizedReturnType.Contains("Task<int>") || returnType.Contains("Task<Int32>"))
        {
            // Return affected rows
            sb.AppendLine($"var result = await cmd.ExecuteNonQueryAsync({ctName}).ConfigureAwait(false);");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (isReturnInsertedId)
        {
            // Check if return type is tuple (int, TKey) for affected rows + id
            var isTupleReturn = IsTupleReturnType(method.ReturnType);
            
            // Return inserted ID - append dialect-specific suffix to the INSERT statement
            sb.AppendLine("// Append last inserted ID query to the INSERT statement");
            sb.AppendLine("cmd.CommandText += _staticContext.Dialect.InsertReturningIdSuffix;");
            
            // Use ExecuteReader for all cases to handle multi-statement SQL correctly
            // For "INSERT ...; SELECT last_insert_rowid()", the ID is in the second result set
            // For "INSERT ... RETURNING id", the ID is in the first result set
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            
            if (isTupleReturn)
            {
                // Return (affectedRows, insertedId) tuple
                sb.AppendLine("var affectedRows = reader.RecordsAffected;");
                sb.AppendLine();
                sb.AppendLine("// For multi-statement SQL (INSERT; SELECT), move to the result set containing the ID");
                sb.AppendLine("// For RETURNING clause, the ID is already in the current result set");
                sb.AppendLine($"if (!await reader.ReadAsync({ctName}).ConfigureAwait(false))");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"await reader.NextResultAsync({ctName}).ConfigureAwait(false);");
                sb.AppendLine($"await reader.ReadAsync({ctName}).ConfigureAwait(false);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine($"var insertedId = ({keyTypeName})Convert.ChangeType(reader.GetValue(0), typeof({keyTypeName}));");
                sb.AppendLine();
                sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
                sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
                sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, (affectedRows, insertedId), elapsed);");
                sb.AppendLine("#endif");
                GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
                sb.AppendLine();
                sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
                sb.AppendLine("if (activity is not null)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("activity.SetTag(\"db.rows_affected\", affectedRows);");
                sb.AppendLine("activity.SetTag(\"db.inserted_id\", insertedId);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine("#endif");
                sb.AppendLine();
                sb.AppendLine("return (affectedRows, insertedId);");
            }
            else
            {
                // Return just insertedId
                sb.AppendLine("// For multi-statement SQL (INSERT; SELECT), move to the result set containing the ID");
                sb.AppendLine("// For RETURNING clause, the ID is already in the current result set");
                sb.AppendLine($"if (!await reader.ReadAsync({ctName}).ConfigureAwait(false))");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"await reader.NextResultAsync({ctName}).ConfigureAwait(false);");
                sb.AppendLine($"await reader.ReadAsync({ctName}).ConfigureAwait(false);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine($"var insertedId = ({keyTypeName})Convert.ChangeType(reader.GetValue(0), typeof({keyTypeName}));");
                sb.AppendLine();
                sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
                sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
                sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, insertedId, elapsed);");
                sb.AppendLine("#endif");
                GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
                sb.AppendLine();
                sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
                sb.AppendLine("activity?.SetTag(\"db.inserted_id\", insertedId);");
                sb.AppendLine("#endif");
                sb.AppendLine();
                sb.AppendLine("return insertedId;");
            }
        }
        else if (normalizedReturnType.Contains("Task<long>") || returnType.Contains("Task<Int64>"))
        {
            // Return count
            sb.AppendLine($"var result = await cmd.ExecuteScalarAsync({ctName}).ConfigureAwait(false);");
            sb.AppendLine("var count = Convert.ToInt64(result);");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, count, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("return count;");
        }
        else if (normalizedReturnType.Contains("Task<bool>") || returnType.Contains("Task<Boolean>"))
        {
            // Return boolean
            sb.AppendLine($"var result = await cmd.ExecuteScalarAsync({ctName}).ConfigureAwait(false);");
            sb.AppendLine("var exists = Convert.ToInt32(result) == 1;");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, exists, elapsed);");
            sb.AppendLine("#endif");
            GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
            sb.AppendLine();
            sb.AppendLine("return exists;");
        }
        else
        {
            // Default: execute non-query
            sb.AppendLine($"await cmd.ExecuteNonQueryAsync({ctName}).ConfigureAwait(false);");
        }
    }

    private static void AppendConditionalBlock(IndentedStringBuilder sb, string condition, Action generateContent)
    {
        sb.AppendLine($"#if {condition}");
        generateContent();
        sb.AppendLine("#endif");
    }

    private static void GenerateCatchBlock(IndentedStringBuilder sb, string repoFullName, string methodName, string fieldName)
    {
        sb.AppendLine("catch (Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        AppendConditionalBlock(sb, "!SQLX_DISABLE_INTERCEPTOR", () =>
        {
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuteFail(\"{methodName}\", cmd, {fieldName}, ex, elapsed);");
        });
        sb.AppendLine();
        AppendConditionalBlock(sb, "!SQLX_DISABLE_METRICS", () =>
        {
            sb.AppendLine($"global::Sqlx.Diagnostics.SqlTemplateMetrics.RecordError(\"{repoFullName}\", \"{methodName}\", {fieldName}.TemplateSql, Stopwatch.GetTimestamp() - startTime, ex);");
        });
        sb.AppendLine();
        AppendConditionalBlock(sb, "!SQLX_DISABLE_ACTIVITY", () =>
            sb.AppendLine("activity?.SetStatus(ActivityStatusCode.Error, ex.Message);"));
        sb.AppendLine();
        AppendConditionalBlock(sb, "SQLX_DISABLE_INTERCEPTOR && SQLX_DISABLE_ACTIVITY", () =>
            sb.AppendLine("_ = ex; // Suppress unused variable warning"));
        sb.AppendLine();
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateSqlTemplateReturnMethod(IndentedStringBuilder sb, IMethodSymbol method, string fieldName)
    {
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine("/// <inheritdoc/>");
        sb.AppendLine($"public global::Sqlx.SqlTemplate {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"return {fieldName};");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateIQueryableReturnMethod(IndentedStringBuilder sb, IMethodSymbol method, string entityFullName, string connectionExpression)
    {
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        var returnType = method.ReturnType.ToDisplayString();

        sb.AppendLine("/// <inheritdoc/>");
        sb.AppendLine($"public {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"return global::Sqlx.SqlQuery<{entityFullName}>.For(_staticContext.Dialect).WithConnection({connectionExpression});");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateFinallyBlock(IndentedStringBuilder sb, string methodName, string fieldName) =>
        AppendConditionalBlock(sb, "!SQLX_DISABLE_ACTIVITY", () =>
        {
            sb.AppendLine("finally");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("if (activity is not null)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine("var durationMs = elapsed * 1000.0 / Stopwatch.Frequency;");
            sb.AppendLine("activity.SetTag(\"db.duration_ms\", durationMs);");
            sb.AppendLine($"activity.SetTag(\"db.statement.prepared\", {fieldName}.Sql);");
            sb.AppendLine("activity.SetTag(\"db.statement\", cmd.CommandText);");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.PopIndent();
            sb.AppendLine("}");
        });

    private static void GenerateInterceptorMethods(IndentedStringBuilder sb) =>
        AppendConditionalBlock(sb, "!SQLX_DISABLE_INTERCEPTOR", () =>
        {
            sb.AppendLine("partial void OnExecuting(string operationName, DbCommand command, global::Sqlx.SqlTemplate template);");
            sb.AppendLine("partial void OnExecuted(string operationName, DbCommand command, global::Sqlx.SqlTemplate template, object? result, long elapsedTicks);");
            sb.AppendLine("partial void OnExecuteFail(string operationName, DbCommand command, global::Sqlx.SqlTemplate template, Exception exception, long elapsedTicks);");
        });

    private static void GenerateMetricsRecording(IndentedStringBuilder sb, string repoFullName, string methodName, string templateFieldName)
    {
        AppendConditionalBlock(sb, "!SQLX_DISABLE_METRICS", () =>
        {
            sb.AppendLine($"global::Sqlx.Diagnostics.SqlTemplateMetrics.RecordExecution(\"{repoFullName}\", \"{methodName}\", {templateFieldName}.TemplateSql, Stopwatch.GetTimestamp() - startTime);");
        });
    }

    private static void GenerateTupleReturn(IndentedStringBuilder sb, IMethodSymbol method, string repoFullName, string entityName, string ctName, string methodName, string fieldName)
    {
        var tupleElements = GetTupleElements(method.ReturnType);
        if (tupleElements.Count == 0) return;

        sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
        sb.AppendLine();

        // Generate variables for each tuple element
        for (int i = 0; i < tupleElements.Count; i++)
        {
            var (elementType, elementName) = tupleElements[i];
            var varName = elementName ?? $"item{i + 1}";
            var typeName = elementType.ToDisplayString();
            var elementEntityName = elementType.Name;

            if (IsListType(elementType))
            {
                // List type - read all rows using ToListAsync extension
                var listElementType = GetListElementType(elementType);
                var listElementName = listElementType?.Name ?? "object";
                sb.AppendLine($"// Read result set {i + 1} as list");
                sb.AppendLine($"var {varName} = await {listElementName}ResultReader.Default.ToListAsync(reader, {ctName}).ConfigureAwait(false);");
            }
            else
            {
                // Single entity type - read first row using FirstOrDefaultAsync extension
                sb.AppendLine($"// Read result set {i + 1} as single entity");
                sb.AppendLine($"var {varName} = await {elementEntityName}ResultReader.Default.FirstOrDefaultAsync(reader, {ctName}).ConfigureAwait(false);");
            }

            // Move to next result set if not the last element
            if (i < tupleElements.Count - 1)
            {
                sb.AppendLine();
                sb.AppendLine($"// Move to next result set");
                sb.AppendLine($"if (!await reader.NextResultAsync({ctName}).ConfigureAwait(false))");
                sb.AppendLine("{");
                sb.PushIndent();
                // Initialize remaining elements with default values
                for (int j = i + 1; j < tupleElements.Count; j++)
                {
                    var (remainingType, remainingName) = tupleElements[j];
                    var remainingVarName = remainingName ?? $"item{j + 1}";
                    if (IsListType(remainingType))
                    {
                        var listElementType = GetListElementType(remainingType);
                        sb.AppendLine($"var {remainingVarName} = new List<{listElementType?.ToDisplayString() ?? "object"}>();");
                    }
                    else
                    {
                        sb.AppendLine($"var {remainingVarName} = default({remainingType.ToDisplayString()});");
                    }
                }
                // Build tuple return with all variables
                var allVarNames = tupleElements.Select((e, idx) => e.Name ?? $"item{idx + 1}").ToList();
                sb.AppendLine($"return ({string.Join(", ", allVarNames)});");
                sb.PopIndent();
                sb.AppendLine("}");
            }
            sb.AppendLine();
        }

        // Build final tuple return
        var finalVarNames = tupleElements.Select((e, idx) => e.Name ?? $"item{idx + 1}").ToList();
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
        sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, ({string.Join(", ", finalVarNames)}), elapsed);");
        sb.AppendLine("#endif");
        GenerateMetricsRecording(sb, repoFullName, methodName, fieldName);
        sb.AppendLine();
        sb.AppendLine($"return ({string.Join(", ", finalVarNames)});");
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>
    /// Checks if the return type is a tuple (ValueTuple).
    /// </summary>
    private static bool IsTupleReturnType(ITypeSymbol returnType)
    {
        // Unwrap Task<T> if present
        if (returnType is INamedTypeSymbol namedType && 
            namedType.OriginalDefinition.ToDisplayString().StartsWith("System.Threading.Tasks.Task<"))
        {
            returnType = namedType.TypeArguments[0];
        }

        // Check if it's a ValueTuple
        var typeName = returnType.ToDisplayString();
        return typeName.StartsWith("(") || typeName.Contains("ValueTuple");
    }

    /// <summary>
    /// Gets the tuple element types from a tuple return type.
    /// </summary>
    private static List<(ITypeSymbol Type, string? Name)> GetTupleElements(ITypeSymbol returnType)
    {
        var elements = new List<(ITypeSymbol Type, string? Name)>();

        // Unwrap Task<T> if present
        if (returnType is INamedTypeSymbol taskType && 
            taskType.OriginalDefinition.ToDisplayString().StartsWith("System.Threading.Tasks.Task<"))
        {
            returnType = taskType.TypeArguments[0];
        }

        if (returnType is INamedTypeSymbol tupleType && tupleType.IsTupleType)
        {
            foreach (var element in tupleType.TupleElements)
            {
                elements.Add((element.Type, element.IsExplicitlyNamedTupleElement ? element.Name : null));
            }
        }

        return elements;
    }

    /// <summary>
    /// Determines if a type represents a list/collection of entities.
    /// </summary>
    private static bool IsListType(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.Contains("List<") || typeName.Contains("IList<") || 
               typeName.Contains("IEnumerable<") || typeName.Contains("ICollection<");
    }

    /// <summary>
    /// Gets the element type from a List/IEnumerable type.
    /// </summary>
    private static ITypeSymbol? GetListElementType(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: > 0 } namedType
            ? namedType.TypeArguments[0] : null;

    /// <summary>
    /// Checks if a SQL template uses the {{columns}} placeholder, indicating fixed column order.
    /// </summary>
    private static bool UsesColumnsPlaceholder(string sqlTemplate)
    {
        return sqlTemplate.Contains("{{columns}}") || sqlTemplate.Contains("{{columns ");
    }
}
