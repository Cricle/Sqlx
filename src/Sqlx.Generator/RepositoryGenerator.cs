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
        }
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

        sb.AppendLine($"public async {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("var startTime = Stopwatch.GetTimestamp();");
        sb.AppendLine();

        // Check if method has dynamic parameters (Expression, limit, offset, etc.)
        var hasDynamicParams = HasDynamicParameters(method, expressionToSqlAttr);
        
        if (hasDynamicParams)
        {
            GenerateDynamicContextAndRender(sb, method, fieldName, expressionToSqlAttr);
            sb.AppendLine("var sqlText = renderedSql;");
        }
        else
        {
            sb.AppendLine($"var sqlText = {fieldName}.Sql;");
        }

        sb.AppendLine();

        // Activity tracking
        GenerateActivityStart(sb, methodName, method);

        sb.AppendLine();
        sb.AppendLine("using DbCommand cmd = _connection.CreateCommand();");
        sb.AppendLine("if (Transaction != null) cmd.Transaction = Transaction;");
        sb.AppendLine("cmd.CommandText = sqlText;");
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
        GenerateExecuteAndReturn(sb, method, entityFullName, entityName, keyTypeName, isReturnInsertedId, methodName, fieldName);

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
            // Check for Expression<Func<T, bool>> parameters
            if (param.Type.ToDisplayString().Contains("Expression<"))
                return true;
            
            // Check for limit/offset parameters
            if (param.Name is "limit" or "offset" or "pageSize")
                return true;
        }
        return false;
    }

    private static void GenerateDynamicContextAndRender(IndentedStringBuilder sb, IMethodSymbol method, string fieldName, INamedTypeSymbol? expressionToSqlAttr)
    {
        sb.AppendLine("// Create dynamic parameters for runtime rendering");
        sb.AppendLine("var dynamicParams = new Dictionary<string, object?>");
        sb.AppendLine("{");
        sb.PushIndent();

        foreach (var param in method.Parameters)
        {
            if (param.Type.ToDisplayString().Contains("Expression<"))
            {
                // Expression parameter - convert to SQL
                sb.AppendLine($"[\"{param.Name}\"] = global::Sqlx.ExpressionExtensions.ToWhereClause({param.Name}, _placeholderContext.Dialect),");
            }
            else if (param.Name is "limit" or "offset" or "pageSize")
            {
                var key = param.Name == "pageSize" ? "pageSize" : param.Name;
                sb.AppendLine($"[\"{key}\"] = {param.Name},");
            }
        }

        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"var renderedSql = {fieldName}.HasDynamicPlaceholders");
        sb.AppendLine($"    ? {fieldName}.Render(dynamicParams)");
        sb.AppendLine($"    : {fieldName}.Sql;");
    }

    private static void GenerateActivityStart(IndentedStringBuilder sb, string methodName, IMethodSymbol method)
    {
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("var activity = global::System.Diagnostics.Activity.Current;");
        sb.AppendLine("if (activity is not null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"activity.AddEvent(new ActivityEvent(\"{methodName}\"));");
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
            if (param.Name is "limit" or "offset" or "pageSize") continue; // Already handled in dynamic context

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
                // Simple parameter binding
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

    private static void GenerateExecuteAndReturn(IndentedStringBuilder sb, IMethodSymbol method, string entityFullName, string entityName, string keyTypeName, bool isReturnInsertedId, string methodName, string fieldName)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var ctParam = method.Parameters.FirstOrDefault(p => p.Name == "cancellationToken");
        var ctName = ctParam?.Name ?? "default";

        // Normalize return type for comparison (handle both short and full type names)
        var normalizedReturnType = returnType
            .Replace("System.Threading.Tasks.", "")
            .Replace("System.Collections.Generic.", "");

        if (normalizedReturnType.Contains("Task<List<") || normalizedReturnType.Contains("Task<IList<") || normalizedReturnType.Contains("Task<IEnumerable<"))
        {
            // Return list of entities
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            sb.AppendLine($"var result = await {entityName}ResultReader.Default.ReadAsync(reader, {ctName}).ConfigureAwait(false);");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("if (activity is not null) activity.SetTag(\"db.rows_affected\", result.Count);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (normalizedReturnType.Contains($"Task<{entityName}?>") || normalizedReturnType.Contains($"Task<{entityName}>") ||
                 returnType.Contains($"Task<{entityFullName}?>") || returnType.Contains($"Task<{entityFullName}>"))
        {
            // Return single entity
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            sb.AppendLine($"var entities = await {entityName}ResultReader.Default.ReadAsync(reader, {ctName}).ConfigureAwait(false);");
            sb.AppendLine("var result = entities.Count > 0 ? entities[0] : default;");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, result, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("if (activity is not null) activity.SetTag(\"db.rows_affected\", result != null ? 1 : 0);");
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
            sb.AppendLine("if (activity is not null) activity.SetTag(\"db.rows_affected\", result);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return result;");
        }
        else if (isReturnInsertedId)
        {
            // Return inserted ID - append SELECT last_insert_id to the INSERT statement for single round-trip
            sb.AppendLine("// Append last inserted ID query to the INSERT statement");
            sb.AppendLine("var insertSqlWithId = sqlText + _dialectType switch");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("global::Sqlx.Annotations.SqlDefineTypes.SqlServer => \"; SELECT SCOPE_IDENTITY()\",");
            sb.AppendLine("global::Sqlx.Annotations.SqlDefineTypes.MySql => \"; SELECT LAST_INSERT_ID()\",");
            sb.AppendLine("global::Sqlx.Annotations.SqlDefineTypes.PostgreSql => \" RETURNING id\",");
            sb.AppendLine("global::Sqlx.Annotations.SqlDefineTypes.SQLite => \"; SELECT last_insert_rowid()\",");
            sb.AppendLine("global::Sqlx.Annotations.SqlDefineTypes.Oracle => \" RETURNING id INTO :id\",");
            sb.AppendLine("global::Sqlx.Annotations.SqlDefineTypes.DB2 => \"; SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1\",");
            sb.AppendLine("_ => throw new NotSupportedException($\"Last insert ID not supported for {_dialectType}\")");
            sb.PopIndent();
            sb.AppendLine("};");
            sb.AppendLine("cmd.CommandText = insertSqlWithId;");
            sb.AppendLine($"var idResult = await cmd.ExecuteScalarAsync({ctName}).ConfigureAwait(false);");
            sb.AppendLine($"var insertedId = ({keyTypeName})Convert.ChangeType(idResult!, typeof({keyTypeName}));");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
            sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
            sb.AppendLine($"OnExecuted(\"{methodName}\", cmd, {fieldName}, insertedId, elapsed);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
            sb.AppendLine("if (activity is not null) activity.SetTag(\"db.inserted_id\", insertedId);");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("return insertedId;");
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
        sb.AppendLine("#if SQLX_DISABLE_INTERCEPTOR && SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("catch");
        sb.AppendLine("#else");
        sb.AppendLine("catch (Exception ex)");
        sb.AppendLine("#endif");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
        sb.AppendLine($"OnExecuteFail(\"{methodName}\", cmd, {fieldName}, ex, elapsed);");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("if (activity is not null) activity.SetStatus(ActivityStatusCode.Error, ex.Message);");
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

        sb.AppendLine($"public global::Sqlx.SqlTemplate {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"return {fieldName};");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateFinallyBlock(IndentedStringBuilder sb, string methodName, string fieldName)
    {
        sb.AppendLine("finally");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("#if !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("if (activity is not null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var elapsed = Stopwatch.GetTimestamp() - startTime;");
        sb.AppendLine("var durationMs = elapsed * 1000.0 / Stopwatch.Frequency;");
        sb.AppendLine("activity.SetTag(\"db.duration_ms\", durationMs);");
        sb.AppendLine($"activity.SetTag(\"db.statement.prepared\", {fieldName}.Sql);");
        sb.AppendLine("activity.SetTag(\"db.statement\", sqlText);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateInterceptorMethods(IndentedStringBuilder sb)
    {
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine("partial void OnExecuting(string operationName, DbCommand command, global::Sqlx.SqlTemplate template);");
        sb.AppendLine("partial void OnExecuted(string operationName, DbCommand command, global::Sqlx.SqlTemplate template, object? result, long elapsedTicks);");
        sb.AppendLine("partial void OnExecuteFail(string operationName, DbCommand command, global::Sqlx.SqlTemplate template, Exception exception, long elapsedTicks);");
        sb.AppendLine("#endif");
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
}
