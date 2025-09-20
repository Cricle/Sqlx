// -----------------------------------------------------------------------
// <copyright file="SqlxGeneratorService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlx.Generator.Core;

/// <summary>
/// Unified service implementation for Sqlx code generation.
/// Combines type inference, method analysis, and code generation capabilities.
/// </summary>
public class SqlxGeneratorService : ISqlxGeneratorService
{
    private readonly ITypeInferenceService _typeInferenceService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly ISqlTemplateEngine _templateEngine;
    private readonly AttributeHandler _attributeHandler;
    private readonly MethodAnalyzer _methodAnalyzer;

    /// <summary>
    /// Gets the type inference service.
    /// </summary>
    public ITypeInferenceService TypeInferenceService => _typeInferenceService;

    /// <summary>
    /// Gets the code generation service.
    /// </summary>
    public ICodeGenerationService CodeGenerationService => _codeGenerationService;

    /// <summary>
    /// Gets the template engine.
    /// </summary>
    public ISqlTemplateEngine TemplateEngine => _templateEngine;

    /// <summary>
    /// Gets the attribute handler.
    /// </summary>
    public AttributeHandler AttributeHandler => _attributeHandler;

    /// <summary>
    /// Gets the method analyzer.
    /// </summary>
    public MethodAnalyzer MethodAnalyzer => _methodAnalyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxGeneratorService"/> class.
    /// </summary>
    public SqlxGeneratorService()
    {
        _typeInferenceService = new TypeInferenceService();
        _codeGenerationService = new CodeGenerationService();
        _templateEngine = new SqlTemplateEngine();
        _attributeHandler = new AttributeHandler();
        _methodAnalyzer = new MethodAnalyzer();
    }

    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromInterface(INamedTypeSymbol serviceInterface)
        => _typeInferenceService.InferEntityTypeFromServiceInterface(serviceInterface);

    /// <inheritdoc/>
    public INamedTypeSymbol? InferEntityTypeFromMethod(IMethodSymbol method)
        => _typeInferenceService.InferEntityTypeFromMethod(method);

    /// <inheritdoc/>
    public INamedTypeSymbol? GetServiceInterface(INamedTypeSymbol repositoryClass, Compilation compilation)
    {
        var repositoryForAttr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RepositoryForAttribute");

        if (repositoryForAttr?.ConstructorArguments.Length > 0)
        {
            var typeArg = repositoryForAttr.ConstructorArguments[0];
            if (typeArg.Value is INamedTypeSymbol serviceType)
                return serviceType;
        }

        // Fallback: Parse syntax directly when semantic model fails (common in test environments)
        return GetServiceInterfaceFromSyntax(repositoryClass, compilation);
    }

    private INamedTypeSymbol? GetServiceInterfaceFromSyntax(INamedTypeSymbol repositoryClass, Compilation compilation)
    {
        // Get the syntax reference for the repository class
        var syntaxRef = repositoryClass.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef?.GetSyntax() is not ClassDeclarationSyntax classSyntax)
            return null;

        // Look for RepositoryFor attribute in syntax
        foreach (var attrList in classSyntax.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var attrName = attr.Name.ToString();
                if (attrName is "RepositoryFor" or "RepositoryForAttribute")
                {
                    // Parse the typeof() argument
                    if (attr.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeofExpr)
                    {
                        // Try to resolve the type name to a symbol
                        var semanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
                        var typeInfo = semanticModel.GetTypeInfo(typeofExpr.Type);
                        if (typeInfo.Type is INamedTypeSymbol interfaceType)
                            return interfaceType;
                    }
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public string GetTableName(INamedTypeSymbol? entityType, INamedTypeSymbol? tableNameAttributeSymbol)
        => _typeInferenceService.GetTableNameFromEntity(entityType, tableNameAttributeSymbol);

    /// <inheritdoc/>
    public string GetTableName(INamedTypeSymbol repositoryClass, INamedTypeSymbol serviceInterface, INamedTypeSymbol? tableNameAttributeSymbol)
        => _typeInferenceService.GetTableName(repositoryClass, serviceInterface, tableNameAttributeSymbol);

    /// <inheritdoc/>
    public MethodAnalysisResult AnalyzeMethod(IMethodSymbol method)
        => _methodAnalyzer.AnalyzeMethod(method);


    /// <inheritdoc/>
    public void GenerateRepositoryImplementation(GenerationContext context)
    {
        if (context.RepositoryClass == null)
        {
            return;
        }

        // Skip if the class has SqlTemplate attribute
        if (context.RepositoryClass.GetAttributes().Any(attr => attr.AttributeClass?.Name == "SqlTemplate"))
        {
            return;
        }

        // Re-enabled repository generation after fixing yield issue
        // Get the service interface from RepositoryFor attribute
        var serviceInterface = GetServiceInterface(context.RepositoryClass, context.Compilation);
        if (serviceInterface == null)
        {
            return;
        }

        var entityType = InferEntityTypeFromInterface(serviceInterface);
        var tableName = GetTableName(entityType, null);

        // Generate the repository implementation
        GenerateRepositoryClass(context, serviceInterface, entityType, tableName);

        var generatedCode = context.StringBuilder.ToString().Trim();

        // Add source to compilation
        var sourceText = SourceText.From(generatedCode, Encoding.UTF8);
        var fileName = $"{context.RepositoryClass.ToDisplayString().Replace(".", "_")}.Repository.g.cs";

        context.ExecutionContext.AddSource(fileName, sourceText);
    }

    /// <inheritdoc/>
    public void GenerateAttributes(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
        => _attributeHandler.GenerateOrCopyAttributes(sb, method, entityType, tableName);

    private void GenerateFallbackMethod(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var isAsync = returnType.Contains("Task");
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine($"public {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        if (isAsync)
        {
            if (returnType == "Task" || returnType == "System.Threading.Tasks.Task")
            {
                sb.AppendLine("return Task.CompletedTask;");
            }
            else
            {
                var innerType = ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"return Task.FromResult(default({innerType}));");
            }
        }
        else
        {
            if (returnType != "void")
            {
                sb.AppendLine($"return default({returnType});");
            }
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void GenerateRepositoryClass(GenerationContext context, INamedTypeSymbol serviceInterface, INamedTypeSymbol? entityType, string tableName)
    {
        var repositoryClass = context.RepositoryClass!;
        var sb = context.StringBuilder;

        // Generate file header
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();

        // Generate usings
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Data.Common;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();

        // Generate namespace
        var namespaceName = repositoryClass.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        // Generate class declaration
        sb.AppendLine($"partial class {repositoryClass.Name} : {serviceInterface.ToDisplayString()}");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate methods
        var interfaceMethods = serviceInterface.GetMembers().OfType<IMethodSymbol>().ToList();
        foreach (var method in interfaceMethods)
        {
            GenerateRepositoryMethodDirect(sb, method, entityType, tableName);
        }

        // Generate interceptor methods (OnExecuting, OnExecuted)
        var codeGenerationService = new CodeGenerationService();
        codeGenerationService.GenerateInterceptorMethods(sb, repositoryClass);

        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateRepositoryMethodDirect(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        // Use unified template engine for SQL generation
        var sqlxAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true);
        
        if (sqlxAttr?.ConstructorArguments.FirstOrDefault().Value is string sql)
        {
            // Use template engine to process SQL template
            var templateResult = _templateEngine.ProcessTemplate(sql, method, entityType, tableName);
            GenerateMethodFromProcessedTemplate(sb, method, templateResult, entityType);
        }
        else
        {
            // Generate fallback implementation for methods without proper attributes
            GenerateFallbackMethodForRepository(sb, method);
        }
    }

    private void GenerateFallbackMethodForRepository(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        var isAsync = returnType.Contains("Task");

        sb.AppendLine($"public {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate appropriate fallback based on method attributes or name
        if (HasSqlxAttribute(method))
        {
            // If method has Sqlx attribute but no appropriate generator, generate a basic implementation
            GenerateBasicSqlImplementation(sb, method, isAsync);
        }
        else
        {
            // Generate simple default return for methods without SQL attributes
            GenerateSimpleFallback(sb, returnType, isAsync);
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private bool HasSqlxAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "SqlxAttribute" ||
            attr.AttributeClass?.Name == "Sqlx");
    }

    private void GenerateBasicSqlImplementation(IndentedStringBuilder sb, IMethodSymbol method, bool isAsync)
    {
        // Get the SQL from the Sqlx attribute
        var sqlxAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name?.Contains("Sqlx") == true);

        var sql = sqlxAttr?.ConstructorArguments.FirstOrDefault().Value as string ?? "SELECT 1";
        var returnType = method.ReturnType.ToDisplayString();

        sb.AppendLine("// Generated basic implementation for Sqlx method");
        sb.AppendLine("using var cmd = connection.CreateCommand();");
        sb.AppendLine($"cmd.CommandText = @\"{sql}\";");

        // Add parameters
        foreach (var param in method.Parameters)
        {
            if (param.Name != "cancellationToken")
            {
                sb.AppendLine($"var param_{param.Name} = cmd.CreateParameter();");
                sb.AppendLine($"param_{param.Name}.ParameterName = \"@{param.Name}\";");
                sb.AppendLine($"param_{param.Name}.Value = {param.Name} ?? (object)DBNull.Value;");
                sb.AppendLine($"cmd.Parameters.Add(param_{param.Name});");
            }
        }

        if (isAsync)
        {
            if (returnType.Contains("Task<") && !returnType.Contains("void"))
            {
                sb.AppendLine("var result = await cmd.ExecuteScalarAsync();");
                var innerType = ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"return result != null ? ({innerType})result : default({innerType});");
            }
            else
            {
                sb.AppendLine("await cmd.ExecuteNonQueryAsync();");
                if (returnType != "Task" && returnType != "System.Threading.Tasks.Task")
                {
                    sb.AppendLine("return;");
                }
            }
        }
        else
        {
            if (returnType != "void")
            {
                sb.AppendLine("var result = cmd.ExecuteScalar();");
                sb.AppendLine($"return result != null ? ({returnType})result : default({returnType});");
            }
            else
            {
                sb.AppendLine("cmd.ExecuteNonQuery();");
            }
        }
    }

    private void GenerateSimpleFallback(IndentedStringBuilder sb, string returnType, bool isAsync)
    {
        if (isAsync)
        {
            if (returnType == "Task" || returnType == "System.Threading.Tasks.Task")
            {
                sb.AppendLine("return Task.CompletedTask;");
            }
            else
            {
                // Extract inner type from Task<T>
                var innerType = ExtractInnerTypeFromTask(returnType);
                sb.AppendLine($"return Task.FromResult(default({innerType}));");
            }
        }
        else
        {
            if (returnType != "void")
            {
                sb.AppendLine($"return default({returnType});");
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

    private string GetDbTypeForParameter(IParameterSymbol parameter)
    {
        return parameter.Type.SpecialType switch
        {
            SpecialType.System_String => "global::System.Data.DbType.String",
            SpecialType.System_Int32 => "global::System.Data.DbType.Int32",
            SpecialType.System_Int64 => "global::System.Data.DbType.Int64",
            SpecialType.System_Boolean => "global::System.Data.DbType.Boolean",
            SpecialType.System_DateTime => "global::System.Data.DbType.DateTime",
            SpecialType.System_Decimal => "global::System.Data.DbType.Decimal",
            SpecialType.System_Double => "global::System.Data.DbType.Double",
            _ => "global::System.Data.DbType.Object"
        };
    }

    private void GenerateMethodFromProcessedTemplate(IndentedStringBuilder sb, IMethodSymbol method, SqlTemplateResult templateResult, INamedTypeSymbol? entityType)
    {
        var methodName = method.Name;
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", method.Parameters.Select(p =>
            $"{p.Type.ToDisplayString()} {p.Name}"));

        // Generate method signature
        sb.AppendLine($"public {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate method variables
        sb.AppendLine($"{ExtractInnerTypeFromTask(returnType)} __result__ = default!;");
        sb.AppendLine("global::System.Data.IDbCommand? __cmd__ = null;");
        sb.AppendLine("var __stopwatch__ = global::System.Diagnostics.Stopwatch.StartNew();");
        sb.AppendLine();

        // Generate connection setup
        var connectionFieldName = GetConnectionFieldName(method.ContainingType);
        sb.AppendLine($"if ({connectionFieldName}.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{connectionFieldName}.Open();");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"__cmd__ = {connectionFieldName}.CreateCommand();");

        // Set SQL command
        sb.AppendLine($"__cmd__.CommandText = @\"{templateResult.ProcessedSql}\";");

        // Add parameters
        foreach (var param in method.Parameters.Where(p => p.Type.Name != "CancellationToken"))
        {
            sb.AppendLine($"var param_{param.Name} = __cmd__.CreateParameter();");
            sb.AppendLine($"param_{param.Name}.ParameterName = \"@{param.Name}\";");
            
            // Handle nullable value types properly
            if (param.Type.IsValueType)
            {
                sb.AppendLine($"param_{param.Name}.Value = {param.Name};");
            }
            else
            {
                sb.AppendLine($"param_{param.Name}.Value = {param.Name} ?? (object)global::System.DBNull.Value;");
            }
            
            sb.AppendLine($"param_{param.Name}.DbType = {GetDbTypeForParameter(param)};");
            sb.AppendLine($"__cmd__.Parameters.Add(param_{param.Name});");
            sb.AppendLine();
        }

        // Add try-catch block with interceptors
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();
        
        // Call OnExecuting interceptor
        sb.AppendLine($"OnExecuting(\"{methodName}\", __cmd__);");
        sb.AppendLine();

        // Execute based on return type
        var operationType = InferOperationType(method.Name);
        if (operationType == OperationType.Select)
        {
            if (IsCollectionReturnType(method.ReturnType))
            {
                GenerateCollectionExecution(sb, method, entityType);
            }
            else if (IsScalarReturnType(method.ReturnType))
            {
                GenerateScalarExecution(sb, method);
            }
            else
            {
                GenerateSingleEntityExecution(sb, method, entityType);
            }
        }
        else
        {
            // Insert, Update, Delete operations
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }

        sb.AppendLine();
        sb.AppendLine("__stopwatch__.Stop();");
        
        // Call OnExecuted interceptor
        sb.AppendLine($"OnExecuted(\"{methodName}\", __cmd__, __result__, __stopwatch__.ElapsedTicks);");
        
        // Return statement
        if (!method.ReturnsVoid)
        {
            if (returnType.Contains("Task"))
            {
                sb.AppendLine("return global::System.Threading.Tasks.Task.FromResult(__result__);");
            }
            else
            {
                sb.AppendLine("return __result__;");
            }
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (global::System.Exception __ex__)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine("__stopwatch__.Stop();");
        sb.AppendLine($"OnExecuteFail(\"{methodName}\", __cmd__, __ex__, __stopwatch__.ElapsedTicks);");
        sb.AppendLine("throw;");
        
        sb.PopIndent();
        sb.AppendLine("}");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private string GetConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        // Find connection field/property (simplified version)
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.Type.AllInterfaces.Any(i => i.Name == "IDbConnection"));
        
        if (connectionField != null)
            return connectionField.Name;

        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Type.AllInterfaces.Any(i => i.Name == "IDbConnection"));
        
        if (connectionProperty != null)
            return connectionProperty.Name;

        return "_connection"; // Fallback to common field name
    }

    private OperationType InferOperationType(string methodName)
    {
        var name = methodName.ToLowerInvariant();
        if (name.Contains("select") || name.Contains("get") || name.Contains("find") || name.Contains("query"))
            return OperationType.Select;
        if (name.Contains("insert") || name.Contains("create") || name.Contains("add"))
            return OperationType.Insert;
        if (name.Contains("update") || name.Contains("modify"))
            return OperationType.Update;
        if (name.Contains("delete") || name.Contains("remove"))
            return OperationType.Delete;
        
        return OperationType.Select; // Default to select
    }

    private bool IsCollectionReturnType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.AllInterfaces.Any(i =>
                i.Name == "IEnumerable" ||
                i.Name == "ICollection" ||
                i.Name == "IList");
        }
        return false;
    }

    private bool IsScalarReturnType(ITypeSymbol type)
    {
        // Remove Task wrapper if present
        if (type is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length > 0)
        {
            type = namedType.TypeArguments[0];
        }

        return type.SpecialType == SpecialType.System_Int32 ||
               type.SpecialType == SpecialType.System_Boolean ||
               type.SpecialType == SpecialType.System_Int64 ||
               type.SpecialType == SpecialType.System_Decimal ||
               type.SpecialType == SpecialType.System_Double;
    }

    private void GenerateCollectionExecution(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType)
    {
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("var results = new global::System.Collections.Generic.List<" + (entityType?.Name ?? "object") + ">();");
        sb.AppendLine("while (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityMapping(sb, entityType, "item");
            sb.AppendLine("results.Add(item);");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("__result__ = results;");
    }

    private void GenerateSingleEntityExecution(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType)
    {
        sb.AppendLine("using var reader = __cmd__.ExecuteReader();");
        sb.AppendLine("if (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityMapping(sb, entityType, "__result__");
        }
        else
        {
            sb.AppendLine("__result__ = default;");
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

    private void GenerateScalarExecution(IndentedStringBuilder sb, IMethodSymbol method)
    {
        sb.AppendLine("var scalarResult = __cmd__.ExecuteScalar();");
        sb.AppendLine("if (scalarResult != null && scalarResult != global::System.DBNull.Value)");
        sb.AppendLine("{");
        sb.PushIndent();

        var returnType = method.ReturnType;
        // Remove Task wrapper if present
        if (returnType is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length > 0)
        {
            returnType = namedType.TypeArguments[0];
        }

        if (returnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("__result__ = global::System.Convert.ToInt32(scalarResult);");
        }
        else if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine("__result__ = global::System.Convert.ToBoolean(scalarResult);");
        }
        else
        {
            sb.AppendLine($"__result__ = ({returnType.ToDisplayString()})scalarResult;");
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

    private void GenerateEntityMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName)
    {
        // Use proper type name for creation
        var typeName = entityType.ToDisplayString();
        sb.AppendLine($"{variableName} = new {typeName}();");

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.SetMethod != null)
            .ToList();

        foreach (var prop in properties)
        {
            sb.AppendLine($"if (reader[\"{prop.Name}\"] != global::System.DBNull.Value)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{variableName}.{prop.Name} = ({prop.Type.ToDisplayString()})reader[\"{prop.Name}\"];");
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }

    private enum OperationType
    {
        Select,
        Insert,
        Update,
        Delete
    }
}
