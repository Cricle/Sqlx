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
/// Unified source generator for Sqlx - handles both [Sqlx] and [RepositoryFor] attributes.
/// Generates IEntityProvider, IResultReader, IParameterBinder, and Repository implementations.
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
                if (name is "Sqlx" or "SqlxAttribute" or "RepositoryFor" or "RepositoryForAttribute")
                    return classDecl;
            }
        }
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty) return;

        var sqlxAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");
        var repositoryForAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryForAttribute");
        var ignoreAttr = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
        var columnAttr = compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.Schema.ColumnAttribute");
        var sqlDefineAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlDefineAttribute");
        var tableNameAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.TableNameAttribute");
        var sqlTemplateAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlTemplateAttribute");
        var returnInsertedIdAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ReturnInsertedIdAttribute");
        var expressionToSqlAttr = compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute");

        var processedTypes = new HashSet<string>();
        var generatedTypes = new List<GeneratedTypeInfo>();

        foreach (var classDecl in classes.Distinct())
        {
            if (classDecl is null) continue;

            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol) continue;

            // Process [Sqlx] attributes
            if (sqlxAttr is not null)
            {
                var attrs = typeSymbol.GetAttributes()
                    .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlxAttr))
                    .ToList();

                foreach (var attr in attrs)
                {
                    INamedTypeSymbol targetType;
                    if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is INamedTypeSymbol targetTypeArg)
                        targetType = targetTypeArg;
                    else
                        targetType = typeSymbol;

                    var typeKey = targetType.ToDisplayString();
                    if (processedTypes.Contains(typeKey)) continue;
                    processedTypes.Add(typeKey);

                    var genEntityProvider = GetNamedArgBool(attr, "GenerateEntityProvider", true);
                    var genResultReader = GetNamedArgBool(attr, "GenerateResultReader", true);
                    var genParameterBinder = GetNamedArgBool(attr, "GenerateParameterBinder", true);

                    var info = new GeneratedTypeInfo(targetType, genEntityProvider, genResultReader, genParameterBinder);
                    generatedTypes.Add(info);

                    var source = GenerateSqlxSource(targetType, ignoreAttr, columnAttr, genEntityProvider, genResultReader, genParameterBinder);
                    context.AddSource($"{targetType.Name}.Sqlx.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }

            // Process [RepositoryFor] attributes
            if (repositoryForAttr is not null)
            {
                var repoForAttrData = typeSymbol.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, repositoryForAttr));
                
                if (repoForAttrData is not null)
                {
                    var serviceType = repoForAttrData.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
                    if (serviceType is not null)
                    {
                        var sqlDefine = GetSqlDefine(typeSymbol, sqlDefineAttr);
                        var tableName = GetTableName(typeSymbol, tableNameAttr);
                        var entityType = GetEntityType(serviceType);

                        if (entityType is not null)
                        {
                            var source = GenerateRepositorySource(typeSymbol, serviceType, entityType, sqlDefine, tableName,
                                sqlTemplateAttr, returnInsertedIdAttr, expressionToSqlAttr, compilation);
                            context.AddSource($"{typeSymbol.Name}.Repository.g.cs", SourceText.From(source, Encoding.UTF8));
                        }
                    }
                }
            }
        }

        // Generate ModuleInitializer
        if (generatedTypes.Count > 0)
        {
            var moduleInitSource = GenerateModuleInitializer(generatedTypes);
            context.AddSource("Sqlx.ModuleInit.g.cs", SourceText.From(moduleInitSource, Encoding.UTF8));
        }
    }

    #region Sqlx Generation (EntityProvider, ResultReader, ParameterBinder)

    private static bool GetNamedArgBool(AttributeData attr, string name, bool defaultValue)
    {
        foreach (var arg in attr.NamedArguments)
        {
            if (arg.Key == name && arg.Value.Value is bool b)
                return b;
        }
        return defaultValue;
    }

    private static string GenerateSqlxSource(INamedTypeSymbol typeSymbol, INamedTypeSymbol? ignoreAttr, INamedTypeSymbol? columnAttr,
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

    #endregion

    #region Repository Generation

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

    private static string GenerateRepositorySource(
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

        var connectionInfo = FindDbConnection(repoType);

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
        if (entityNs is not null && entityNs != ns)
        {
            sb.AppendLine($"using {entityNs};");
        }
        sb.AppendLine();

        sb.AppendLine($"public partial class {repoName}");
        sb.AppendLine("{");
        sb.PushIndent();

        if (connectionInfo.NeedsField && !HasMember(repoType, connectionInfo.FieldName))
        {
            sb.AppendLine($"private readonly {connectionInfo.TypeName} {connectionInfo.FieldName};");
            sb.AppendLine();
        }

        if (!HasMember(repoType, "Transaction"))
        {
            sb.AppendLine("/// <summary>Gets or sets the transaction for this repository.</summary>");
            sb.AppendLine("public DbTransaction? Transaction { get; set; }");
            sb.AppendLine();
        }

        GeneratePlaceholderContext(sb, entityName, entityFullName, sqlDefine, tableName);
        sb.AppendLine();

        var methods = GetMethodsWithSqlTemplate(serviceType, sqlTemplateAttr);
        var methodFieldNames = BuildMethodFieldNames(methods);
        GenerateSqlTemplateFields(sb, methods, sqlTemplateAttr, methodFieldNames);
        sb.AppendLine();

        foreach (var method in methods)
        {
            GenerateMethodImplementation(sb, method, entityFullName, entityName, keyTypeName,
                sqlTemplateAttr, returnInsertedIdAttr, expressionToSqlAttr, connectionInfo, methodFieldNames);
            sb.AppendLine();
        }

        GenerateInterceptorMethods(sb);

        sb.PopIndent();
        sb.AppendLine("}");

        return sb.ToString();
    }

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

    private readonly struct ConnectionInfo
    {
        public string AccessExpression { get; init; }
        public string TypeName { get; init; }
        public string FieldName { get; init; }
        public bool NeedsField { get; init; }
    }

    private static ConnectionInfo FindDbConnection(INamedTypeSymbol repoType)
    {
        foreach (var member in repoType.GetMembers())
        {
            if (member is IFieldSymbol field && !field.IsStatic && IsDbConnectionType(field.Type))
            {
                return new ConnectionInfo
                {
                    AccessExpression = field.Name,
                    TypeName = field.Type.ToDisplayString(),
                    FieldName = field.Name,
                    NeedsField = false
                };
            }
        }

        foreach (var member in repoType.GetMembers())
        {
            if (member is IPropertySymbol prop && !prop.IsStatic && prop.GetMethod != null && IsDbConnectionType(prop.Type))
            {
                return new ConnectionInfo
                {
                    AccessExpression = prop.Name,
                    TypeName = prop.Type.ToDisplayString(),
                    FieldName = prop.Name,
                    NeedsField = false
                };
            }
        }

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
                        var fieldName = "_" + param.Name;
                        return new ConnectionInfo
                        {
                            AccessExpression = fieldName,
                            TypeName = param.Type.ToDisplayString(),
                            FieldName = fieldName,
                            NeedsField = true
                        };
                    }
                }
            }
        }

        return new ConnectionInfo
        {
            AccessExpression = "_connection",
            TypeName = "DbConnection",
            FieldName = "_connection",
            NeedsField = false
        };
    }

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

    private static bool HasMember(INamedTypeSymbol type, string memberName)
    {
        return type.GetMembers(memberName).Length > 0;
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

        var processedSignatures = new HashSet<string>();

        foreach (var member in serviceType.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlTemplateAttr)))
            {
                var signature = GetMethodSignature(member);
                processedSignatures.Add(signature);
                methods.Add(member);
            }
        }

        foreach (var iface in serviceType.AllInterfaces)
        {
            foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, sqlTemplateAttr)))
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

    private static string GetMethodSignature(IMethodSymbol method)
    {
        var paramTypes = string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"{method.Name}({paramTypes})";
    }

    private static void GenerateSqlTemplateFields(IndentedStringBuilder sb, List<IMethodSymbol> methods, INamedTypeSymbol? sqlTemplateAttr, Dictionary<string, string> methodFieldNames)
    {
        sb.AppendLine("// Static SqlTemplate fields - prepared once at initialization");

        foreach (var method in methods)
        {
            var template = GetSqlTemplate(method, sqlTemplateAttr);
            if (template is null) continue;

            var signature = GetMethodSignature(method);
            var fieldName = methodFieldNames[signature];
            sb.AppendLine($"private static readonly global::Sqlx.SqlTemplate {fieldName} = global::Sqlx.SqlTemplate.Prepare(");
            sb.PushIndent();
            sb.AppendLine($"\"{EscapeString(template)}\",");
            sb.AppendLine("_placeholderContext);");
            sb.PopIndent();

            if (UsesStaticColumns(template) && ReturnsEntity(method))
            {
                var ordinalsFieldName = fieldName.Replace("Template", "Ordinals");
                sb.AppendLine($"private static readonly int[] {ordinalsFieldName} = Enumerable.Range(0, _placeholderContext.Columns.Count).ToArray();");
            }
        }
    }

    private static bool UsesStaticColumns(string template)
    {
        return template.Contains("{{columns}}") || template.Contains("{{columns ");
    }

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
        INamedTypeSymbol? expressionToSqlAttr,
        ConnectionInfo connectionInfo,
        Dictionary<string, string> methodFieldNames)
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

        if (returnType == "Sqlx.SqlTemplate" || returnType == "global::Sqlx.SqlTemplate" || returnType.EndsWith(".SqlTemplate"))
        {
            GenerateSqlTemplateReturnMethod(sb, method, fieldName);
            return;
        }

        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine("/// <inheritdoc/>");
        sb.AppendLine($"public async {returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR || !SQLX_DISABLE_ACTIVITY");
        sb.AppendLine("var startTime = Stopwatch.GetTimestamp();");
        sb.AppendLine("#endif");
        sb.AppendLine();

        GenerateActivityStart(sb, methodName, method);

        sb.AppendLine();
        sb.AppendLine($"using DbCommand cmd = {connectionInfo.AccessExpression}.CreateCommand();");
        sb.AppendLine("if (Transaction != null) cmd.Transaction = Transaction;");

        var hasDynamicParams = HasDynamicParameters(method);

        if (hasDynamicParams)
        {
            GenerateDynamicContextAndRender(sb, method, fieldName);
        }
        else
        {
            sb.AppendLine($"cmd.CommandText = {fieldName}.Sql;");
        }

        sb.AppendLine();

        GenerateParameterBinding(sb, method, entityName);

        sb.AppendLine();
        sb.AppendLine("#if !SQLX_DISABLE_INTERCEPTOR");
        sb.AppendLine($"OnExecuting(\"{methodName}\", cmd, {fieldName});");
        sb.AppendLine("#endif");
        sb.AppendLine();

        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        GenerateExecuteAndReturn(sb, method, entityFullName, entityName, keyTypeName, isReturnInsertedId, methodName, fieldName, useStaticOrdinals, ordinalsFieldName);

        sb.PopIndent();
        sb.AppendLine("}");

        GenerateCatchBlock(sb, methodName, fieldName);
        GenerateFinallyBlock(sb, methodName, fieldName);

        sb.PopIndent();
        sb.AppendLine("}");
    }

    private static bool HasDynamicParameters(IMethodSymbol method)
    {
        foreach (var param in method.Parameters)
        {
            var typeName = param.Type.ToDisplayString();
            if (typeName.Contains("Expression<"))
                return true;
        }
        return false;
    }

    private static void GenerateDynamicContextAndRender(IndentedStringBuilder sb, IMethodSymbol method, string fieldName)
    {
        var expressionParams = method.Parameters.Where(p => p.Type.ToDisplayString().Contains("Expression<")).ToList();

        sb.AppendLine("// Create dynamic parameters for runtime rendering");
        sb.AppendLine($"var dynamicParams = new Dictionary<string, object?>({expressionParams.Count})");
        sb.AppendLine("{");
        sb.PushIndent();

        foreach (var param in expressionParams)
        {
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

    private static void GenerateParameterBinding(IndentedStringBuilder sb, IMethodSymbol method, string entityName)
    {
        sb.AppendLine("// Bind parameters");

        foreach (var param in method.Parameters)
        {
            if (param.Name == "cancellationToken") continue;
            if (param.Type.ToDisplayString().Contains("Expression<")) continue;

            var paramType = param.Type;
            var paramTypeName = paramType.ToDisplayString();

            if (paramTypeName.Contains("<") && paramTypeName.Contains(entityName))
            {
                continue;
            }

            if (paramType.Name == entityName)
            {
                sb.AppendLine($"{entityName}ParameterBinder.Default.BindEntity(cmd, {param.Name}, _placeholderContext.Dialect.ParameterPrefix);");
            }
            else
            {
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
        if (type.NullableAnnotation == NullableAnnotation.Annotated) return true;
        if (type.IsValueType && type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) return true;
        if (!type.IsValueType && type.NullableAnnotation != NullableAnnotation.NotAnnotated) return true;
        return false;
    }

    private static void GenerateExecuteAndReturn(IndentedStringBuilder sb, IMethodSymbol method, string entityFullName, string entityName, string keyTypeName, bool isReturnInsertedId, string methodName, string fieldName, bool useStaticOrdinals, string ordinalsFieldName)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var ctParam = method.Parameters.FirstOrDefault(p => p.Name == "cancellationToken");
        var ctName = ctParam?.Name ?? "default";

        var capacityHintParam = method.Parameters.FirstOrDefault(p => p.Name is "limit" or "pageSize");
        var capacityHint = capacityHintParam?.Name;

        if (IsTupleReturnType(method.ReturnType) && !isReturnInsertedId)
        {
            GenerateTupleReturn(sb, method, entityName, ctName, methodName, fieldName);
            return;
        }

        var normalizedReturnType = returnType
            .Replace("System.Threading.Tasks.", "")
            .Replace("System.Collections.Generic.", "");

        if (normalizedReturnType.Contains("Task<List<") || normalizedReturnType.Contains("Task<IList<") || normalizedReturnType.Contains("Task<IEnumerable<"))
        {
            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");
            if (useStaticOrdinals && capacityHint != null)
            {
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
            var isTupleReturn = IsTupleReturnType(method.ReturnType);

            sb.AppendLine("// Append last inserted ID query to the INSERT statement");
            sb.AppendLine("cmd.CommandText += _placeholderContext.Dialect.InsertReturningIdSuffix;");

            sb.AppendLine($"using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, {ctName}).ConfigureAwait(false);");

            if (isTupleReturn)
            {
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

        for (int i = 0; i < tupleElements.Count; i++)
        {
            var (elementType, elementName) = tupleElements[i];
            var varName = elementName ?? $"item{i + 1}";
            var typeName = elementType.ToDisplayString();
            var elementEntityName = elementType.Name;

            if (IsListType(elementType))
            {
                var listElementType = GetListElementType(elementType);
                var listElementName = listElementType?.Name ?? "object";
                sb.AppendLine($"// Read result set {i + 1} as list");
                sb.AppendLine($"var {varName} = await {listElementName}ResultReader.Default.ToListAsync(reader, {ctName}).ConfigureAwait(false);");
            }
            else
            {
                sb.AppendLine($"// Read result set {i + 1} as single entity");
                sb.AppendLine($"var {varName} = await {elementEntityName}ResultReader.Default.FirstOrDefaultAsync(reader, {ctName}).ConfigureAwait(false);");
            }

            if (i < tupleElements.Count - 1)
            {
                sb.AppendLine();
                sb.AppendLine($"// Move to next result set");
                sb.AppendLine($"if (!await reader.NextResultAsync({ctName}).ConfigureAwait(false))");
                sb.AppendLine("{");
                sb.PushIndent();
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
                var allVarNames = tupleElements.Select((e, idx) => e.Name ?? $"item{idx + 1}").ToList();
                sb.AppendLine($"return ({string.Join(", ", allVarNames)});");
                sb.PopIndent();
                sb.AppendLine("}");
            }
            sb.AppendLine();
        }

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

    private static bool IsTupleReturnType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.ToDisplayString().StartsWith("System.Threading.Tasks.Task<"))
        {
            returnType = namedType.TypeArguments[0];
        }

        var typeName = returnType.ToDisplayString();
        return typeName.StartsWith("(") || typeName.Contains("ValueTuple");
    }

    private static List<(ITypeSymbol Type, string? Name)> GetTupleElements(ITypeSymbol returnType)
    {
        var elements = new List<(ITypeSymbol Type, string? Name)>();

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

    private static bool IsListType(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.Contains("List<") || typeName.Contains("IList<") ||
               typeName.Contains("IEnumerable<") || typeName.Contains("ICollection<");
    }

    private static ITypeSymbol? GetListElementType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }
        return null;
    }

    #endregion
}
