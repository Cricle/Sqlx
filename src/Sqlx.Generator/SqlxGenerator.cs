// <copyright file="SqlxGenerator.cs" company="Sqlx">
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
/// Source generator for [Sqlx] attribute - generates IEntityProvider, IResultReader, and IParameterBinder.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class SqlxGenerator : IIncrementalGenerator
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
                if (name is "Sqlx" or "SqlxAttribute")
                    return classDecl;
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty) return;

        var sqlxAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");
        if (sqlxAttr is null) return;

        var ignoreAttr = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
        var columnAttr = compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.Schema.ColumnAttribute");

        var processedTypes = new HashSet<string>();
        var generatedTypes = new List<GeneratedTypeInfo>();

        foreach (var classDecl in classes.Distinct())
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
                // Get target type
                INamedTypeSymbol targetType;
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is INamedTypeSymbol targetTypeArg)
                    targetType = targetTypeArg;
                else
                    targetType = hostTypeSymbol;

                var typeKey = targetType.ToDisplayString();
                if (processedTypes.Contains(typeKey)) continue;
                processedTypes.Add(typeKey);

                // Get generation options
                var genEntityProvider = GetNamedArgBool(attr, "GenerateEntityProvider", true);
                var genResultReader = GetNamedArgBool(attr, "GenerateResultReader", true);
                var genParameterBinder = GetNamedArgBool(attr, "GenerateParameterBinder", true);

                var info = new GeneratedTypeInfo(targetType, genEntityProvider, genResultReader, genParameterBinder);
                generatedTypes.Add(info);

                var source = GenerateSource(targetType, ignoreAttr, columnAttr, genEntityProvider, genResultReader, genParameterBinder);
                context.AddSource($"{targetType.Name}.Sqlx.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        // Generate ModuleInitializer
        if (generatedTypes.Count > 0)
        {
            var moduleInitSource = GenerateModuleInitializer(generatedTypes);
            context.AddSource("Sqlx.ModuleInit.g.cs", SourceText.From(moduleInitSource, Encoding.UTF8));
        }
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

    private static string GenerateSource(INamedTypeSymbol typeSymbol, INamedTypeSymbol? ignoreAttr, INamedTypeSymbol? columnAttr,
        bool genEntityProvider, bool genResultReader, bool genParameterBinder)
    {
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : typeSymbol.ContainingNamespace.ToDisplayString();
        var typeName = typeSymbol.Name;
        var fullTypeName = typeSymbol.ToDisplayString();

        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic && p.GetMethod is not null)
            .Where(p => ignoreAttr is null || !p.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, ignoreAttr)))
            .ToList();

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
            GenerateEntityProvider(sb, typeName, fullTypeName, properties, columnAttr);
            sb.AppendLine();
        }

        if (genResultReader)
        {
            GenerateResultReader(sb, typeName, fullTypeName, properties, columnAttr);
            sb.AppendLine();
        }

        if (genParameterBinder)
        {
            GenerateParameterBinder(sb, typeName, fullTypeName, properties, columnAttr);
        }

        return sb.ToString();
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
        // GetOrdinals
        sb.AppendLine("public int[] GetOrdinals(IDataReader reader) => new int[]");
        sb.AppendLine("{");
        sb.PushIndent();
        for (int i = 0; i < properties.Count; i++)
            sb.AppendLine($"reader.GetOrdinal(Col{i}),");
        sb.PopIndent();
        sb.AppendLine("};");
        sb.AppendLine();
        // Read(IDataReader)
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
        // Read(IDataReader, int[])
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
        // BindEntity(DbCommand)
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
        // BindEntity(DbBatchCommand) - .NET 6+
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
            var typeName = info.Type.Name;
            var ns = info.Type.ContainingNamespace.IsGlobalNamespace ? "Global" : info.Type.ContainingNamespace.ToDisplayString();
            if (info.GenerateEntityProvider)
                sb.AppendLine($"global::Sqlx.SqlQuery<{fullTypeName}>.EntityProvider = global::{ns}.{typeName}EntityProvider.Default;");
            if (info.GenerateResultReader)
                sb.AppendLine($"global::Sqlx.SqlQuery<{fullTypeName}>.ResultReader = global::{ns}.{typeName}ResultReader.Default;");
            if (info.GenerateParameterBinder)
                sb.AppendLine($"global::Sqlx.SqlQuery<{fullTypeName}>.ParameterBinder = global::{ns}.{typeName}ParameterBinder.Default;");
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
