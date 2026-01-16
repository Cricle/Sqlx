// <copyright file="SqlxGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

/// <summary>
/// Source generator for Sqlx - generates IEntityProvider, IResultReader, and IParameterBinder.
/// Discovers types from:
/// 1. [Sqlx] attribute on classes
/// 2. SqlQuery&lt;T&gt; generic type arguments
/// 3. [SqlTemplate] method parameters marked as entity types
/// </summary>
[Generator(LanguageNames.CSharp)]
public class SqlxGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Classes with [Sqlx] attribute
        var sqlxClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetSqlxTarget(ctx))
            .Where(static m => m is not null);

        // 2. SqlQuery<T> usages
        var sqlQueryUsages = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSqlQueryUsage(s),
                transform: static (ctx, _) => GetSqlQueryTypeArg(ctx))
            .Where(static m => m is not null);

        // 3. [SqlTemplate] method parameters
        var sqlTemplateParams = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is MethodDeclarationSyntax m && m.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetSqlTemplateParamTypes(ctx))
            .Where(static m => m is not null)
            .SelectMany(static (types, _) => types!);

        // Combine all sources
        var allTypes = sqlxClasses
            .Collect()
            .Combine(sqlQueryUsages.Collect())
            .Combine(sqlTemplateParams.Collect());

        var compilationAndTypes = context.CompilationProvider.Combine(allTypes);
        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => Execute(source.Left, source.Right, spc));
    }

    private static bool IsSqlQueryUsage(SyntaxNode node)
    {
        // Match: SqlQuery<T>.ForXxx() or SqlQuery<T>.xxx
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "SqlQuery" &&
                genericName.TypeArgumentList.Arguments.Count == 1)
            {
                return true;
            }
        }
        return false;
    }

    private static ClassDeclarationSyntax? GetSqlxTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        foreach (var attrList in classDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                // Match: Sqlx, SqlxAttribute, Annotations.Sqlx, Sqlx.Annotations.Sqlx, etc.
                if (name.EndsWith("Sqlx") || name.EndsWith("SqlxAttribute"))
                    return classDecl;
            }
        }
        return null;
    }

    private static TypeSyntax? GetSqlQueryTypeArg(GeneratorSyntaxContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (memberAccess.Expression is GenericNameSyntax genericName &&
            genericName.Identifier.Text == "SqlQuery" &&
            genericName.TypeArgumentList.Arguments.Count == 1)
        {
            return genericName.TypeArgumentList.Arguments[0];
        }
        return null;
    }

    private static IEnumerable<TypeSyntax>? GetSqlTemplateParamTypes(GeneratorSyntaxContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;
        
        // Check if method has [SqlTemplate] attribute
        bool hasSqlTemplate = false;
        foreach (var attrList in methodDecl.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name.EndsWith("SqlTemplate") || name.EndsWith("SqlTemplateAttribute"))
                {
                    hasSqlTemplate = true;
                    break;
                }
            }
            if (hasSqlTemplate) break;
        }
        if (!hasSqlTemplate) return null;

        var result = new List<TypeSyntax>();
        
        // 1. Extract entity type from return type (Task<T>, Task<List<T>>, List<T>, etc.)
        if (methodDecl.ReturnType != null)
        {
            var returnEntityType = ExtractEntityTypeFromReturnType(methodDecl.ReturnType);
            if (returnEntityType != null)
            {
                result.Add(returnEntityType);
            }
        }

        // 2. Collect parameter types that are entity types (not primitives, not Expression<>)
        foreach (var param in methodDecl.ParameterList.Parameters)
        {
            if (param.Type is null) continue;
            
            // Skip Expression<>, CancellationToken, primitives
            var typeStr = param.Type.ToString();
            if (IsPrimitiveOrSkipType(typeStr))
                continue;

            // Check for identifier types (potential entity types)
            if (param.Type is IdentifierNameSyntax || 
                param.Type is QualifiedNameSyntax ||
                (param.Type is NullableTypeSyntax nullable && 
                 (nullable.ElementType is IdentifierNameSyntax || nullable.ElementType is QualifiedNameSyntax)))
            {
                result.Add(param.Type);
            }
        }
        
        return result.Count > 0 ? result : null;
    }

    private static TypeSyntax? ExtractEntityTypeFromReturnType(TypeSyntax returnType)
    {
        // Handle nullable: T? -> T
        if (returnType is NullableTypeSyntax nullable)
        {
            returnType = nullable.ElementType;
        }

        // Handle generic types: Task<T>, List<T>, IEnumerable<T>, etc.
        if (returnType is GenericNameSyntax genericName)
        {
            var typeName = genericName.Identifier.Text;
            
            // Task<T> or ValueTask<T>
            if ((typeName == "Task" || typeName == "ValueTask") && genericName.TypeArgumentList.Arguments.Count == 1)
            {
                // Recursively extract from Task<T>
                return ExtractEntityTypeFromReturnType(genericName.TypeArgumentList.Arguments[0]);
            }
            
            // List<T>, IList<T>, IEnumerable<T>, ICollection<T>, IReadOnlyList<T>, etc.
            if ((typeName == "List" || typeName == "IList" || typeName == "IEnumerable" || 
                 typeName == "ICollection" || typeName == "IReadOnlyList" || typeName == "IReadOnlyCollection") &&
                genericName.TypeArgumentList.Arguments.Count == 1)
            {
                var innerType = genericName.TypeArgumentList.Arguments[0];
                // Handle nullable element: List<T?> -> T
                if (innerType is NullableTypeSyntax nullableInner)
                {
                    innerType = nullableInner.ElementType;
                }
                
                // Only return if it's a potential entity type (not primitive)
                var innerTypeStr = innerType.ToString();
                if (!IsPrimitiveOrSkipType(innerTypeStr) && 
                    (innerType is IdentifierNameSyntax || innerType is QualifiedNameSyntax))
                {
                    return innerType;
                }
                return null;
            }
        }

        // Direct entity type (non-generic)
        var typeStr = returnType.ToString();
        if (!IsPrimitiveOrSkipType(typeStr) && 
            (returnType is IdentifierNameSyntax || returnType is QualifiedNameSyntax))
        {
            return returnType;
        }

        return null;
    }

    private static bool IsPrimitiveOrSkipType(string typeStr)
    {
        return typeStr.StartsWith("Expression<") ||
               typeStr == "CancellationToken" ||
               typeStr == "void" ||
               typeStr == "int" || typeStr == "int?" ||
               typeStr == "long" || typeStr == "long?" ||
               typeStr == "short" || typeStr == "short?" ||
               typeStr == "byte" || typeStr == "byte?" ||
               typeStr == "string" || typeStr == "string?" ||
               typeStr == "bool" || typeStr == "bool?" ||
               typeStr == "decimal" || typeStr == "decimal?" ||
               typeStr == "double" || typeStr == "double?" ||
               typeStr == "float" || typeStr == "float?" ||
               typeStr == "DateTime" || typeStr == "DateTime?" ||
               typeStr == "DateTimeOffset" || typeStr == "DateTimeOffset?" ||
               typeStr == "Guid" || typeStr == "Guid?" ||
               typeStr == "object" ||
               typeStr == "Task" ||
               typeStr == "ValueTask";
    }

    private static void Execute(
        Compilation compilation,
        ((ImmutableArray<ClassDeclarationSyntax?> SqlxClasses, ImmutableArray<TypeSyntax?> SqlQueryTypes), ImmutableArray<TypeSyntax?> SqlTemplateTypes) sources,
        SourceProductionContext context)
    {
        var (sqlxAndQuery, sqlTemplateTypes) = sources;
        var (sqlxClasses, sqlQueryTypes) = sqlxAndQuery;

        var sqlxAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");
        var ignoreAttr = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
        var columnAttr = compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.Schema.ColumnAttribute");

        var processedTypes = new HashSet<string>();
        var generatedTypes = new List<GeneratedTypeInfo>();

        // 1. Process [Sqlx] attributed classes
        if (sqlxAttr is not null && !sqlxClasses.IsDefaultOrEmpty)
        {
            foreach (var classDecl in sqlxClasses.Distinct())
            {
                if (classDecl is null) continue;

                var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol hostTypeSymbol) continue;

                var attrs = hostTypeSymbol.GetAttributes()
                    .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlxAttr))
                    .ToList();
                if (attrs.Count == 0) continue;

                foreach (var attr in attrs)
                {
                    INamedTypeSymbol targetType;
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is INamedTypeSymbol targetTypeArg)
                        targetType = targetTypeArg;
                    else
                        targetType = hostTypeSymbol;

                    var typeKey = targetType.ToDisplayString();
                    if (processedTypes.Contains(typeKey)) continue;
                    processedTypes.Add(typeKey);

                    var genEntityProvider = GetNamedArgBool(attr, "GenerateEntityProvider", true);
                    var genResultReader = GetNamedArgBool(attr, "GenerateResultReader", true);
                    var genParameterBinder = GetNamedArgBool(attr, "GenerateParameterBinder", true);

                    if (TryGenerateForType(targetType, ignoreAttr, columnAttr, genEntityProvider, genResultReader, genParameterBinder, context, out var info))
                    {
                        generatedTypes.Add(info);
                    }
                }
            }
        }

        // 2. Process SqlQuery<T> type arguments
        if (!sqlQueryTypes.IsDefaultOrEmpty)
        {
            foreach (var typeSyntax in sqlQueryTypes.Distinct())
            {
                if (typeSyntax is null) continue;
                ProcessTypeSyntax(compilation, typeSyntax, processedTypes, generatedTypes, ignoreAttr, columnAttr, context);
            }
        }

        // 3. Process [SqlTemplate] parameter types
        if (!sqlTemplateTypes.IsDefaultOrEmpty)
        {
            foreach (var typeSyntax in sqlTemplateTypes.Distinct())
            {
                if (typeSyntax is null) continue;
                ProcessTypeSyntax(compilation, typeSyntax, processedTypes, generatedTypes, ignoreAttr, columnAttr, context);
            }
        }

        // Generate ModuleInitializer
        if (generatedTypes.Count > 0)
        {
            var moduleInitSource = GenerateModuleInitializer(generatedTypes);
            context.AddSource("Sqlx.ModuleInit.g.cs", SourceText.From(moduleInitSource, Encoding.UTF8));
        }
    }

    private static void ProcessTypeSyntax(
        Compilation compilation,
        TypeSyntax typeSyntax,
        HashSet<string> processedTypes,
        List<GeneratedTypeInfo> generatedTypes,
        INamedTypeSymbol? ignoreAttr,
        INamedTypeSymbol? columnAttr,
        SourceProductionContext context)
    {
        // Get semantic model for the syntax tree containing this type
        var semanticModel = compilation.GetSemanticModel(typeSyntax.SyntaxTree);
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
        
        if (typeInfo.Type is not INamedTypeSymbol namedType) return;
        
        // Skip primitives, system types, anonymous types
        if (namedType.SpecialType != SpecialType.None) return;
        if (namedType.IsAnonymousType) return;
        if (namedType.ContainingNamespace?.ToDisplayString().StartsWith("System") == true) return;

        var typeKey = namedType.ToDisplayString();
        if (processedTypes.Contains(typeKey)) return;
        processedTypes.Add(typeKey);

        // Generate with default options (all true)
        if (TryGenerateForType(namedType, ignoreAttr, columnAttr, true, true, true, context, out var info))
        {
            generatedTypes.Add(info);
        }
    }

    private static bool TryGenerateForType(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? ignoreAttr,
        INamedTypeSymbol? columnAttr,
        bool genEntityProvider,
        bool genResultReader,
        bool genParameterBinder,
        SourceProductionContext context,
        out GeneratedTypeInfo info)
    {
        info = default;

        // Skip if no public properties
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic && p.GetMethod is not null)
            .Where(p => ignoreAttr is null || !p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, ignoreAttr)))
            .ToList();

        if (properties.Count == 0) return false;

        var source = GenerateSource(typeSymbol, properties, columnAttr, genEntityProvider, genResultReader, genParameterBinder);
        
        // Use full type name for unique file name
        var fullTypeName = typeSymbol.ToDisplayString();
        var safeFileName = fullTypeName.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(",", "_");
        context.AddSource($"{safeFileName}.Sqlx.g.cs", SourceText.From(source, Encoding.UTF8));

        info = new GeneratedTypeInfo(typeSymbol, genEntityProvider, genResultReader, genParameterBinder);
        return true;
    }

    private static bool GetNamedArgBool(AttributeData attr, string name, bool defaultValue)
    {
        foreach (var arg in attr.NamedArguments)
        {
            if (arg.Key == name && arg.Value.Value is bool b)
                return b;
        }
        return defaultValue;
    }

    private static string GenerateSource(INamedTypeSymbol typeSymbol, List<IPropertySymbol> properties, INamedTypeSymbol? columnAttr,
        bool genEntityProvider, bool genResultReader, bool genParameterBinder)
    {
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;
        var fullTypeName = typeSymbol.ToDisplayString();
        
        // For nested types, use containing type names as prefix to avoid conflicts
        var generatedTypeName = GetGeneratedTypeName(typeSymbol);

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
        sb.AppendLine();

        if (genEntityProvider)
        {
            GenerateEntityProvider(sb, generatedTypeName, fullTypeName, properties, columnAttr);
            sb.AppendLine();
        }

        if (genResultReader)
        {
            GenerateResultReader(sb, generatedTypeName, fullTypeName, properties, columnAttr);
            sb.AppendLine();
        }

        if (genParameterBinder)
        {
            GenerateParameterBinder(sb, generatedTypeName, fullTypeName, properties, columnAttr);
        }

        return sb.ToString();
    }

    private static string GetGeneratedTypeName(INamedTypeSymbol typeSymbol)
    {
        // For nested types, include containing type names
        var parts = new List<string>();
        var current = typeSymbol;
        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }
        return string.Join("_", parts);
    }

    private static void GenerateEntityProvider(IndentedStringBuilder sb, string typeName, string fullTypeName,
        List<IPropertySymbol> properties, INamedTypeSymbol? columnAttr)
    {
        sb.AppendLine($"public sealed class {typeName}EntityProvider : global::Sqlx.IEntityProvider");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"public static {typeName}EntityProvider Default {{ get; }} = new();");
        sb.AppendLine($"private static readonly Type _entityType = typeof({fullTypeName});");
        sb.AppendLine("private static readonly IReadOnlyList<global::Sqlx.ColumnMeta> _columns = new global::Sqlx.ColumnMeta[]");
        sb.AppendLine("{");
        sb.PushIndent();
        foreach (var prop in properties)
        {
            var columnName = GetColumnName(prop, columnAttr);
            var dbType = GetDbType(prop.Type);
            var isNullable = IsNullable(prop);
            sb.AppendLine($"new global::Sqlx.ColumnMeta(\"{columnName}\", \"{prop.Name}\", DbType.{dbType}, {isNullable.ToString().ToLowerInvariant()}),");
        }
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine("public Type EntityType => _entityType;");
        sb.AppendLine("public IReadOnlyList<global::Sqlx.ColumnMeta> Columns => _columns;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateResultReader(IndentedStringBuilder sb, string typeName, string fullTypeName,
        List<IPropertySymbol> properties, INamedTypeSymbol? columnAttr)
    {
        sb.AppendLine($"public sealed class {typeName}ResultReader : global::Sqlx.IResultReader<{fullTypeName}>");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"public static {typeName}ResultReader Default {{ get; }} = new();");
        sb.AppendLine();
        for (int i = 0; i < properties.Count; i++)
        {
            var columnName = GetColumnName(properties[i], columnAttr);
            sb.AppendLine($"private const string Col{i} = \"{columnName}\";");
        }
        sb.AppendLine();
        sb.AppendLine("public int[] GetOrdinals(IDataReader reader) => new int[]");
        sb.AppendLine("{");
        sb.PushIndent();
        for (int i = 0; i < properties.Count; i++)
            sb.AppendLine($"reader.GetOrdinal(Col{i}),");
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"public {fullTypeName} Read(IDataReader reader) => new {fullTypeName}");
        sb.AppendLine("{");
        sb.PushIndent();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var readExpr = GetReaderExpression(prop.Type, $"reader.GetOrdinal(Col{i})", IsNullable(prop));
            sb.AppendLine($"{prop.Name} = {readExpr},");
        }
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine($"public {fullTypeName} Read(IDataReader reader, int[] ordinals) => new {fullTypeName}");
        sb.AppendLine("{");
        sb.PushIndent();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var readExpr = GetReaderExpression(prop.Type, $"ordinals[{i}]", IsNullable(prop));
            sb.AppendLine($"{prop.Name} = {readExpr},");
        }
        sb.PopIndent();
        sb.AppendLine("};");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static void GenerateParameterBinder(IndentedStringBuilder sb, string typeName, string fullTypeName,
        List<IPropertySymbol> properties, INamedTypeSymbol? columnAttr)
    {
        sb.AppendLine($"public sealed class {typeName}ParameterBinder : global::Sqlx.IParameterBinder<{fullTypeName}>");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"public static {typeName}ParameterBinder Default {{ get; }} = new();");
        sb.AppendLine();
        sb.AppendLine($"public void BindEntity(DbCommand cmd, {fullTypeName} e, string prefix = \"@\")");
        sb.AppendLine("{");
        sb.PushIndent();
        foreach (var p in properties)
        {
            var col = GetColumnName(p, columnAttr);
            var val = IsNullable(p) || p.Type.IsReferenceType ? $"e.{p.Name} ?? (object)DBNull.Value" : $"e.{p.Name}";
            sb.AppendLine($"{{ var p = cmd.CreateParameter(); p.ParameterName = prefix + \"{col}\"; p.Value = {val}; cmd.Parameters.Add(p); }}");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#if NET6_0_OR_GREATER");
        sb.AppendLine($"public void BindEntity(DbBatchCommand cmd, {fullTypeName} e, Func<DbParameter> f, string prefix = \"@\")");
        sb.AppendLine("{");
        sb.PushIndent();
        foreach (var p in properties)
        {
            var col = GetColumnName(p, columnAttr);
            var val = IsNullable(p) || p.Type.IsReferenceType ? $"e.{p.Name} ?? (object)DBNull.Value" : $"e.{p.Name}";
            sb.AppendLine($"{{ var p = f(); p.ParameterName = prefix + \"{col}\"; p.Value = {val}; cmd.Parameters.Add(p); }}");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("#endif");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static string GenerateModuleInitializer(List<GeneratedTypeInfo> types)
    {
        var sb = new IndentedStringBuilder(null);
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Sqlx.Generated;");
        sb.AppendLine();
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();
        sb.AppendLine("internal static class SqlxInitializer");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("[ModuleInitializer]");
        sb.AppendLine("internal static void Initialize()");
        sb.AppendLine("{");
        sb.PushIndent();
        foreach (var info in types)
        {
            var fullTypeName = info.Type.ToDisplayString();
            var generatedTypeName = GetGeneratedTypeName(info.Type);
            var ns = info.Type.ContainingNamespace.IsGlobalNamespace ? "Global" : info.Type.ContainingNamespace.ToDisplayString();
            if (info.GenerateEntityProvider)
            {
                sb.AppendLine($"global::Sqlx.SqlQuery<{fullTypeName}>.EntityProvider = global::{ns}.{generatedTypeName}EntityProvider.Default;");
                sb.AppendLine($"global::Sqlx.EntityProviderRegistry.Register(typeof({fullTypeName}), global::{ns}.{generatedTypeName}EntityProvider.Default);");
            }
            if (info.GenerateResultReader)
                sb.AppendLine($"global::Sqlx.SqlQuery<{fullTypeName}>.ResultReader = global::{ns}.{generatedTypeName}ResultReader.Default;");
            if (info.GenerateParameterBinder)
                sb.AppendLine($"global::Sqlx.SqlQuery<{fullTypeName}>.ParameterBinder = global::{ns}.{generatedTypeName}ParameterBinder.Default;");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GetColumnName(IPropertySymbol prop, INamedTypeSymbol? columnAttr)
    {
        if (columnAttr is not null)
        {
            var attr = prop.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, columnAttr));
            if (attr?.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                return name;
        }
        return ToSnakeCase(prop.Name);
    }

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static bool IsNullable(IPropertySymbol prop) =>
        prop.NullableAnnotation == NullableAnnotation.Annotated ||
        (prop.Type.IsValueType && prop.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

    private static string GetDbType(ITypeSymbol type)
    {
        var typeName = GetUnderlyingTypeName(type);
        return typeName switch
        {
            "Int32" => "Int32",
            "Int64" => "Int64",
            "Int16" => "Int16",
            "Byte" => "Byte",
            "Boolean" => "Boolean",
            "String" => "String",
            "DateTime" => "DateTime",
            "DateTimeOffset" => "DateTimeOffset",
            "Decimal" => "Decimal",
            "Double" => "Double",
            "Single" => "Single",
            "Guid" => "Guid",
            _ => "Object",
        };
    }

    private static string GetUnderlyingTypeName(ITypeSymbol type)
    {
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol namedType)
            return namedType.TypeArguments[0].Name;
        return type.Name;
    }

    private static string GetReaderExpression(ITypeSymbol type, string ordinal, bool isNullable)
    {
        var typeName = GetUnderlyingTypeName(type);
        var method = typeName switch
        {
            "Int32" => "GetInt32",
            "Int64" => "GetInt64",
            "Int16" => "GetInt16",
            "Byte" => "GetByte",
            "Boolean" => "GetBoolean",
            "String" => "GetString",
            "DateTime" => "GetDateTime",
            "Decimal" => "GetDecimal",
            "Double" => "GetDouble",
            "Single" => "GetFloat",
            "Guid" => "GetGuid",
            _ => null,
        };
        if (method is null)
            return isNullable ? $"reader.IsDBNull({ordinal}) ? default : reader.GetValue({ordinal})" : $"reader.GetValue({ordinal})";
        if (isNullable)
            return $"reader.IsDBNull({ordinal}) ? default : reader.{method}({ordinal})";
        return $"reader.{method}({ordinal})";
    }

    private readonly struct GeneratedTypeInfo
    {
        public INamedTypeSymbol Type { get; }
        public bool GenerateEntityProvider { get; }
        public bool GenerateResultReader { get; }
        public bool GenerateParameterBinder { get; }

        public GeneratedTypeInfo(INamedTypeSymbol type, bool genEntityProvider, bool genResultReader, bool genParameterBinder)
        {
            Type = type;
            GenerateEntityProvider = genEntityProvider;
            GenerateResultReader = genResultReader;
            GenerateParameterBinder = genParameterBinder;
        }
    }
}
