// <copyright file="RepositoryGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

            var sqlDefine = GetSqlDefine(typeSymbol, sqlDefineAttr);
            var tableName = GetTableName(typeSymbol, tableNameAttr);
            var entityType = GetEntityType(serviceType);

            if (entityType is null) continue;

            var source = GenerateSource(typeSymbol, serviceType, entityType, sqlDefine, tableName, 
                sqlTemplateAttr, returnInsertedIdAttr, expressionToSqlAttr, compilation);
            context.AddSource($"{typeSymbol.Name}.Repository.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GetSqlDefine(INamedTypeSymbol typeSymbol, INamedTypeSymbol? sqlDefineAttr)
    {
        if (sqlDefineAttr is null) return "SQLite";
        var attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlDefineAttr));
        if (attr?.ConstructorArguments.Length > 0)
        {
            var value = attr.ConstructorArguments[0].Value;
            if (value is int intValue)
            {
                // Map SqlDefineTypes enum values to SqlDefine static field names
                return intValue switch
                {
                    0 => "MySql",
                    1 => "SqlServer",
                    2 => "PostgreSql",
                    3 => "Oracle",
                    4 => "DB2",
                    5 => "SQLite",
                    _ => "SQLite"
                };
            }
            return value?.ToString() ?? "SQLite";
        }
        return "SQLite";
    }

    private static string GetTableName(INamedTypeSymbol typeSymbol, INamedTypeSymbol? tableNameAttr)
    {
        if (tableNameAttr is null) return "unknown";
        var attr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, tableNameAttr));
        if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
            return name;
        return "unknown";
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

        // Generate static PlaceholderContext
        GeneratePlaceholderContext(sb, entityName, entityFullName, sqlDefine, tableName);
        sb.AppendLine();

        // Generate static SqlTemplate fields for each method
        var methods = GetMethodsWithSqlTemplate(serviceType, sqlTemplateAttr);
        GenerateSqlTemplateFields(sb, methods, sqlTemplateAttr);
        sb.AppendLine();

        // Generate method implementations
        foreach (var method in methods)
        {
            GenerateMethodImplementation(sb, method, entityFullName, entityName, keyTypeName, 
                sqlTemplateAttr, returnInsertedIdAttr, expressionToSqlAttr);
            sb.AppendLine();
        }

        // Generate interceptor partial methods
        GenerateInterceptorMethods(sb);

        sb.PopIndent();
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GeneratePlaceholderContext(IndentedStringBuilder sb, string entityName, string entityFullName, string sqlDefine, string tableName)
    {
        sb.AppendLine("// PlaceholderContext - shared across all methods");
        sb.AppendLine($"private const global::Sqlx.Annotations.SqlDefineTypes _dialectType = global::Sqlx.Annotations.SqlDefineTypes.{sqlDefine};");
        sb.AppendLine($"private static readonly global::Sqlx.PlaceholderContext _placeholderContext = new global::Sqlx.PlaceholderContext(");
        sb.PushIndent();
        sb.AppendLine($"dialect: global::Sqlx.SqlDefine.{sqlDefine},");
        sb.AppendLine($"tableName: \"{tableName}\",");
        sb.AppendLine($"columns: {entityName}EntityProvider.Default.Columns);");
        sb.PopIndent();
    }

    private static List<IMethodSymbol> GetMethodsWithSqlTemplate(INamedTypeSymbol serviceType, INamedTypeSymbol? sqlTemplateAttr)
    {
        var methods = new List<IMethodSymbol>();
        if (sqlTemplateAttr is null) return methods;

        // Get methods from the interface and all its base interfaces
        var allInterfaces = serviceType.AllInterfaces.Concat(new[] { serviceType });
        foreach (var iface in allInterfaces)
        {
            foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlTemplateAttr)))
                {
                    // Avoid duplicates
                    if (!methods.Any(m => m.Name == member.Name && m.Parameters.Length == member.Parameters.Length))
                    {
                        methods.Add(member);
                    }
                }
            }
        }

        return methods;
    }

    private static void GenerateSqlTemplateFields(IndentedStringBuilder sb, List<IMethodSymbol> methods, INamedTypeSymbol? sqlTemplateAttr)
    {
        sb.AppendLine("// Static SqlTemplate fields - prepared once at initialization");
        foreach (var method in methods)
        {
            var template = GetSqlTemplate(method, sqlTemplateAttr);
            if (template is null) continue;

            var fieldName = $"_{ToCamelCase(method.Name)}Template";
            sb.AppendLine($"private static readonly global::Sqlx.SqlTemplate {fieldName} = global::Sqlx.SqlTemplate.Prepare(");
            sb.PushIndent();
            sb.AppendLine($"\"{EscapeString(template)}\",");
            sb.AppendLine("_placeholderContext);");
            sb.PopIndent();
            
            // Generate static ordinals for methods that use {{columns}} and return entities
            // When SQL uses {{columns}}, column order matches EntityProvider.Columns order (0, 1, 2, ...)
            if (UsesStaticColumns(template) && ReturnsEntity(method))
            {
                var ordinalsFieldName = $"_{ToCamelCase(method.Name)}Ordinals";
                sb.AppendLine($"private static readonly int[] {ordinalsFieldName} = Enumerable.Range(0, _placeholderContext.Columns.Count).ToArray();");
            }
        }
    }

    /// <summary>
    /// Checks if the SQL template uses {{columns}} placeholder which means column order is static.
    /// </summary>
    private static bool UsesStaticColumns(string template)
    {
        return template.Contains("{{columns}}") || template.Contains("{{columns ");
    }

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
        if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string template)
            return template;
        return null;
    }

    private static void GenerateMethodImplementation(
        IndentedStringBuilder sb,
        IMethodSymbol method,
        string entityFullName,
        string entityName,
        string keyTypeName,
        INamedTypeSymbol? sqlTemplateAttr,
        INamedTypeSymbol? returnInsertedIdAttr,
        INamedTypeSymbol? expressionToSqlAttr)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var fieldName = $"_{ToCamelCase(methodName)}Template";
        var ordinalsFieldName = $"_{ToCamelCase(methodName)}Ordinals";
        var template = GetSqlTemplate(method, sqlTemplateAttr);
        var useStaticOrdinals = template != null && UsesStaticColumns(template) && ReturnsEntity(method);
        var isReturnInsertedId = returnInsertedIdAttr is not null && 
            method.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, returnInsertedIdAttr));

        // Check if this is a SqlTemplate return type (debug/inspection method)
        if (returnType == "Sqlx.SqlTemplate" || returnType == "global::Sqlx.SqlTemplate" || returnType.EndsWith(".SqlTemplate"))
        {
            GenerateSqlTemplateReturnMethod(sb, method, fieldName);
            return;
        }

        // Build parameter list
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine("/// <inheritdoc/>");
        sb.AppendLine($"public async {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR || !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("var startTime = Stopwatch.GetTimestamp();");
        sb.AppendLine("#endif");
        sb.AppendLine();

        // Activity tracking
        GenerateActivityStart(sb, methodName, method);

        sb.AppendLine();
        sb.AppendLine("using DbCommand cmd = _connection.CreateCommand();");
        sb.AppendLine("if (Transaction != null) cmd.Transaction = Transaction;");

        // Check if method has dynamic parameters (Expression parameters only)
        var hasDynamicParams = HasDynamicParameters(method, expressionToSqlAttr);
        
        if (hasDynamicParams)
        {
            // For expressions, need to render
            GenerateDynamicContextAndRender(sb, method, fieldName, expressionToSqlAttr);
        }
        else
        {
            sb.AppendLine($"cmd.CommandText = {fieldName}.Sql;");
        }

        sb.AppendLine();

        // Bind parameters
        GenerateParameterBinding(sb, method, entityName, expressionToSqlAttr);

        sb.AppendLine();
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine($"OnExecuting(\"{methodName}\", cmd, {fieldName});");
        sb.AppendLine("#endif");
        sb.AppendLine();

        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Execute and return based on return type
        GenerateExecuteAndReturn(sb, method, entityFullName, entityName, keyTypeName, isReturnInsertedId, methodName, fieldName, useStaticOrdinals, ordinalsFieldName);

        sb.PopIndent();
        sb.AppendLine("}");

        // Catch block
        GenerateCatchBlock(sb, methodName, fieldName);

        // Finally block
        GenerateFinallyBlock(sb, methodName, fieldName);

        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static bool HasDynamicParameters(IMethodSymbol method, INamedTypeSymbol? expressionToSqlAttr)
    {
        foreach (var param in method.Parameters)
        {
            // Check for Expression<Func<T, bool>> parameters - these are truly dynamic
            if (param.Type.ToDisplayString().Contains("Expression<"))
                return true;
        }
        return false;
    }

    private static void GenerateDynamicContextAndRender(IndentedStringBuilder sb, IMethodSymbol method, string fieldName, INamedTypeSymbol? expressionToSqlAttr)
    {
        // This method only handles Expression parameters that need runtime SQL generation
        var expressionParams = method.Parameters.Where(p => p.Type.ToDisplayString().Contains("Expression<")).ToList();

        sb.AppendLine("// Create dynamic parameters for runtime rendering");
        sb.AppendLine($"var dynamicParams = new Dictionary<string, object?>({expressionParams.Count})");
        sb.AppendLine("{");
        sb.PushIndent();

        foreach (var param in expressionParams)
        {
            // Expression parameter - convert to SQL
            sb.AppendLine($"[\"{param.Name}\"] = global::Sqlx.ExpressionExtensions.ToWhereClause({param.Name}, _placeholderContext.Dialect),");
        }

        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"var renderedSql = {fieldName}.HasDynamicPlaceholders");
        sb.AppendLine($"    ? {fieldName}.Render(dynamicParams)");
        sb.AppendLine($"    : {fieldName}.Sql;");
        sb.AppendLine("cmd.CommandText = renderedSql;");
    }

    private static void GenerateActivityStart(IndentedStringBuilder sb, string methodName, IMethodSymbol method)
    {
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("var activity = global::System.Diagnostics.Activity.Current;");
        sb.AppendLine("if (activity is not null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"activity = activity.AddEvent(new ActivityEvent(\"{methodName}\"));");
        sb.AppendLine("activity.SetTag(\"db.system\", _placeholderContext.Dialect.DatabaseType);");
        sb.AppendLine("activity.SetTag(\"db.operation\", \"sqlx.execute\");");
        sb.AppendLine("activity.SetTag(\"db.has_transaction\", Transaction != null);");
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY_PARAMS");

        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            sb.AppendLine($"activity.SetTag(\"db.param.{param.Name}\", {param.Name});");
        }

        sb.AppendLine("#endif");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
    }

    private static void GenerateParameterBinding(IndentedStringBuilder sb, IMethodSymbol method, string entityName, INamedTypeSymbol? expressionToSqlAttr)
    {
        sb.AppendLine("// Bind parameters");
        
        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            if (param.Type.ToDisplayString().Contains("Expression<")) continue; // Already handled in dynamic context

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
                sb.AppendLine($"{entityName}ParameterBinder.Default.BindEntity(cmd, {param.Name}, _placeholderContext.Dialect.ParameterPrefix);");
            }
            else
            {
                // Simple parameter binding for scalar types (int, string, etc.)
                var isNullable = IsNullableType(paramType);
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("var p = cmd.CreateParameter();");
                sb.AppendLine($"p.ParameterName = _placeholderContext.Dialect.ParameterPrefix + \"{param.Name}\";");
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

    private static void GenerateExecuteAndReturn(IndentedStringBuilder sb, IMethodSymbol method, string entityFullName, string entityName, string keyTypeName, bool isReturnInsertedId, string methodName, string fieldName, bool useStaticOrdinals, string ordinalsFieldName)
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
            GenerateTupleReturn(sb, method, entityName, ctName, methodName, fieldName);
            return;
        }

        // Normalize return type for comparison (handle both short and full type names)
        var normalizedReturnType = returnType
            .Replace("System.Threading.Tasks.", "")
            .Replace("System.Collections.Generic.", "");

        if (normalizedReturnType.Contains("Task<List<") || normalizedReturnType.Contains("Task<IList<") || normalizedReturnType.Contains("Task<IEnumerable<"))
        {
            // Return list of entities - use static ordinals when available
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            if (useStaticOrdinals && capacityHint != null)
            {
                // Use capacity hint for list pre-allocation
                sb.AppendLine($"var result = await global::Sqlx.ResultReaderExtensions.ToListAsync({entityName}ResultReader.Default, reader, {ordinalsFieldName}, {capacityHint}, {ctName}).ConfigureAwait(false);");
            }
            else if (useStaticOrdinals)
            {
                sb.AppendLine($"var result = await global::Sqlx.ResultReaderExtensions.ToListAsync({entityName}ResultReader.Default, reader, {ordinalsFieldName}, {ctName}).ConfigureAwait(false);");
            }
            else
            {
                sb.AppendLine($"var result = await {entityName}ResultReader.Default.ToListAsync(reader, {ctName}).ConfigureAwait(false);");
            }
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result.Count);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (normalizedReturnType.Contains($"Task<{entityName}?>") || normalizedReturnType.Contains($"Task<{entityName}>") ||
                 returnType.Contains($"Task<{entityFullName}?>") || returnType.Contains($"Task<{entityFullName}>"))
        {
            // Return single entity - use static ordinals when available
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            if (useStaticOrdinals)
            {
                sb.AppendLine($"var result = await global::Sqlx.ResultReaderExtensions.FirstOrDefaultAsync({entityName}ResultReader.Default, reader, {ordinalsFieldName}, {ctName}).ConfigureAwait(false);");
            }
            else
            {
                sb.AppendLine($"var result = await {entityName}ResultReader.Default.FirstOrDefaultAsync(reader, {ctName}).ConfigureAwait(false);");
            }
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("activity?.SetTag(\"db.rows_affected\", result != null ? 1 : 0);");
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
            sb.AppendLine("cmd.CommandText += _placeholderContext.Dialect.InsertReturningIdSuffix;");
            
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
            sb.AppendLine();
            sb.AppendLine("return exists;");
        }
        else
        {
            // Default: execute non-query
            sb.AppendLine($"await cmd.ExecuteNonQueryAsync({ctName}).ConfigureAwait(false);");
        }
    }

    private static void GenerateCatchBlock(IndentedStringBuilder sb, string methodName, string fieldName)
    {
        sb.AppendLine("catch (Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
        sb.AppendLine($"OnExecuteFail(\"{methodName}\", cmd, {fieldName}, ex, elapsed);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("activity?.SetStatus(ActivityStatusCode.Error, ex.Message);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#if SQLX_DISABLE_INTERCEPTOR && SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("_ = ex; // Suppress unused variable warning");
        sb.AppendLine("#endif");
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

    private static void GenerateFinallyBlock(IndentedStringBuilder sb, string methodName, string fieldName)
    {
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
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
        sb.AppendLine("#endif");
    }

    private static void GenerateInterceptorMethods(IndentedStringBuilder sb)
    {
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine("partial void OnExecuting(string operationName, DbCommand command, global::Sqlx.SqlTemplate template);");
        sb.AppendLine("partial void OnExecuted(string operationName, DbCommand command, global::Sqlx.SqlTemplate template, object? result, long elapsedTicks);");
        sb.AppendLine("partial void OnExecuteFail(string operationName, DbCommand command, global::Sqlx.SqlTemplate template, Exception exception, long elapsedTicks);");
        sb.AppendLine("#endif");
    }

    private static void GenerateTupleReturn(IndentedStringBuilder sb, IMethodSymbol method, string entityName, string ctName, string methodName, string fieldName)
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
        sb.AppendLine();
        sb.AppendLine($"return ({string.Join(", ", finalVarNames)});");
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

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
    private static ITypeSymbol? GetListElementType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }
        return null;
    }
}
