// <copyright file="RuntimeDialectCodeGen.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// Code generation helpers for runtime dialect support.
/// </summary>
internal static class RuntimeDialectCodeGen
{
    /// <summary>
    /// Generates the dialect field and property for runtime dialect support.
    /// </summary>
    public static void GenerateDialectField(
        IndentedStringBuilder sb,
        string? primaryCtorDialectParameterName)
    {
        sb.AppendLine("// Runtime dialect support");

        if (primaryCtorDialectParameterName != null)
        {
            sb.AppendLine($"private readonly global::Sqlx.SqlDialect _dialect = {primaryCtorDialectParameterName} ?? throw new global::System.ArgumentNullException(nameof({primaryCtorDialectParameterName}));");
        }
        else
        {
            sb.AppendLine("private readonly global::Sqlx.SqlDialect _dialect;");
        }

        sb.AppendLine();
        sb.AppendLine("/// <summary>Gets the SQL dialect used by this repository.</summary>");
        sb.AppendLine("public global::Sqlx.SqlDialect Dialect => _dialect;");
        sb.AppendLine();
    }
    
    /// <summary>
    /// Generates constructor with dialect parameter if user hasn't defined one.
    /// </summary>
    public static void GenerateConstructorIfNeeded(
        IndentedStringBuilder sb,
        string repoName,
        bool hasUserDialectConstructor,
        string? connectionFieldName)
    {
        if (hasUserDialectConstructor)
        {
            // User has defined constructor with dialect parameter
            // Just ensure _dialect is initialized (they should do it)
            sb.AppendLine("// Note: User-defined constructor should initialize _dialect field");
            return;
        }
        
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Initializes a new instance of the repository.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <param name=\"connection\">Database connection.</param>");
        sb.AppendLine("/// <param name=\"dialect\">SQL dialect to use.</param>");
        sb.AppendLine($"public {repoName}(global::System.Data.Common.DbConnection connection, global::Sqlx.SqlDialect dialect)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        if (connectionFieldName != null)
        {
            sb.AppendLine($"{connectionFieldName} = connection ?? throw new global::System.ArgumentNullException(nameof(connection));");
        }
        
        sb.AppendLine("_dialect = dialect ?? throw new global::System.ArgumentNullException(nameof(dialect));");
        sb.PopIndent();
        sb.AppendLine("}");
        
        sb.AppendLine();
    }
    
    /// <summary>
    /// Generates runtime PlaceholderContext with lazy initialization.
    /// </summary>
    public static void GenerateRuntimeContext(
        IndentedStringBuilder sb,
        string entityName,
        string tableName,
        bool hasEntityType)
    {
        sb.AppendLine("// Runtime PlaceholderContext (lazy-initialized per instance)");
        sb.AppendLine("private global::Sqlx.PlaceholderContext? _context;");
        sb.AppendLine();
        sb.AppendLine("private global::Sqlx.PlaceholderContext Context");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("get");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("if (_context != null) return _context;");
        sb.AppendLine();
        
        if (hasEntityType)
        {
            sb.AppendLine("_context = new global::Sqlx.PlaceholderContext(");
            sb.PushIndent();
            sb.AppendLine("dialect: _dialect,");
            sb.AppendLine($"tableName: \"{tableName}\",");
            sb.AppendLine($"columns: {entityName}EntityProvider.Default.Columns);");
            sb.PopIndent();
        }
        else
        {
            sb.AppendLine("_context = new global::Sqlx.PlaceholderContext(");
            sb.PushIndent();
            sb.AppendLine("dialect: _dialect,");
            sb.AppendLine($"tableName: \"{tableName}\",");
            sb.AppendLine("columns: global::System.Array.Empty<global::Sqlx.ColumnMeta>());");
            sb.PopIndent();
        }
        
        sb.AppendLine();
        sb.AppendLine("return _context;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        
        // Generate parameter prefix property
        sb.AppendLine("private string ParamPrefix => _dialect.ParameterPrefix;");
        sb.AppendLine();
    }
    
    /// <summary>
    /// Generates runtime template field with caching.
    /// </summary>
    public static void GenerateRuntimeTemplateField(
        IndentedStringBuilder sb,
        string fieldName,
        string template,
        bool usesVar)
    {
        if (usesVar)
        {
            // For {{var}} templates, use instance caching
            sb.AppendLine($"private global::Sqlx.SqlTemplate? {fieldName};");
        }
        else
        {
            // For regular templates, use static dictionary caching
            sb.AppendLine($"private static readonly global::System.Collections.Concurrent.ConcurrentDictionary<global::Sqlx.SqlDialect, global::Sqlx.SqlTemplate> {fieldName}Cache = new();");
            sb.AppendLine($"private global::Sqlx.SqlTemplate? {fieldName};");
        }
    }
    
    /// <summary>
    /// Generates code to get or create template at runtime.
    /// </summary>
    public static void GenerateGetTemplate(
        IndentedStringBuilder sb,
        string fieldName,
        string template,
        bool usesVar)
    {
        if (usesVar)
        {
            // For {{var}} templates, prepare once per instance with dynamic context
            sb.AppendLine($"if ({fieldName} == null)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{fieldName} = global::Sqlx.SqlTemplate.Prepare(");
            sb.PushIndent();
            sb.AppendLine($"\"{EscapeString(template)}\",");
            sb.AppendLine("GetDynamicContext());");
            sb.PopIndent();
            sb.PopIndent();
            sb.AppendLine("}");
        }
        else
        {
            // For regular templates, use cached template
            sb.AppendLine($"if ({fieldName} == null)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{fieldName} = {fieldName}Cache.GetOrAdd(_dialect, d => global::Sqlx.SqlTemplate.Prepare(");
            sb.PushIndent();
            sb.AppendLine($"\"{EscapeString(template)}\",");
            sb.AppendLine("Context));");
            sb.PopIndent();
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }
    
    private static string EscapeString(string str) =>
        str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
}
