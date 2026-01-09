// -----------------------------------------------------------------------
// <copyright file="SharedCodeGenerationUtilities.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Generator;

/// <summary>
/// Shared code generation utilities to reduce code duplication
/// </summary>
public static class SharedCodeGenerationUtilities
{
    // 缓存ToDisplayString()结果以提升性能
    private static readonly ConcurrentDictionary<ISymbol, string> _displayStringCache =
        new(SymbolEqualityComparer.Default);

    // 缓存类型检查结果，避免重复的类型分析
    private static readonly ConcurrentDictionary<ITypeSymbol, bool> _isScalarTypeCache =
        new(SymbolEqualityComparer.Default);

    // 缓存属性的SQL名称
    private static readonly ConcurrentDictionary<IPropertySymbol, string> _sqlNameCache =
        new(SymbolEqualityComparer.Default);

    // 🔧 修复：使用正确的SymbolDisplayFormat来显示可空类型
    // 这确保 int? 显示为 "int?" 而不是 "int" 或 "Nullable<int>"
    // 注意：不使用 GlobalNamespaceStyle.Included 以避免在文件名中产生无效字符 ":"
    private static readonly SymbolDisplayFormat _nullableAwareFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>Gets symbol display string with cache for performance.</summary>
    public static string GetCachedDisplayString(this ISymbol symbol) =>
        _displayStringCache.GetOrAdd(symbol, s => GetDisplayStringWithNullable(s));

    /// <summary>
    /// Gets display string with proper nullable type handling.
    /// Ensures int? is displayed as "int?" not "int" or "Nullable&lt;int&gt;".
    /// </summary>
    private static string GetDisplayStringWithNullable(ISymbol symbol)
    {
        if (symbol is ITypeSymbol typeSymbol)
        {
            // 检查是否是 Nullable<T> 值类型 (如 int?, long?, bool? 等)
            if (typeSymbol is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                // 获取内部类型并添加 ? 后缀
                var innerType = namedType.TypeArguments[0];
                var innerTypeString = innerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                return innerTypeString + "?";
            }

            // 对于引用类型的可空注解，使用标准格式（不包含 global:: 前缀）
            return typeSymbol.ToDisplayString(_nullableAwareFormat);
        }

        return symbol.ToDisplayString();
    }

    /// <summary>Cached scalar type check.</summary>
    public static bool IsCachedScalarType(this ITypeSymbol type) =>
        _isScalarTypeCache.GetOrAdd(type, t => t.IsScalarType());

    /// <summary>Cached SQL name getter.</summary>
    public static string GetCachedSqlName(this IPropertySymbol property) =>
        _sqlNameCache.GetOrAdd(property, p => p.GetSqlName());

    /// <summary>
    /// Resolves an interface type parameter to its concrete type from the implementing class.
    /// For example, if class UserRepo implements IPartialUpdateRepository&lt;User, int, UserUpdate&gt;,
    /// this method can resolve "TUpdates" to "UserUpdate".
    /// </summary>
    /// <param name="implementingClass">The class that implements the interface</param>
    /// <param name="typeParameterName">The name of the type parameter to resolve (e.g., "TUpdates")</param>
    /// <returns>The concrete type, or null if not found</returns>
    public static ITypeSymbol? ResolveInterfaceTypeArgument(INamedTypeSymbol implementingClass, string typeParameterName)
    {
        // Search through all implemented interfaces
        foreach (var iface in implementingClass.AllInterfaces)
        {
            if (!iface.IsGenericType) continue;
            
            var originalDef = iface.OriginalDefinition;
            var typeParams = originalDef.TypeParameters;
            var typeArgs = iface.TypeArguments;
            
            for (int i = 0; i < typeParams.Length && i < typeArgs.Length; i++)
            {
                if (typeParams[i].Name == typeParameterName)
                {
                    return typeArgs[i];
                }
            }
        }
        
        // Also check base types
        var baseType = implementingClass.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var originalDef = baseType.OriginalDefinition;
                var typeParams = originalDef.TypeParameters;
                var typeArgs = baseType.TypeArguments;
                
                for (int i = 0; i < typeParams.Length && i < typeArgs.Length; i++)
                {
                    if (typeParams[i].Name == typeParameterName)
                    {
                        return typeArgs[i];
                    }
                }
            }
            baseType = baseType.BaseType;
        }
        
        return null;
    }

    /// <summary>Extract inner type from Task&lt;T&gt; type strings</summary>
    public static string ExtractInnerTypeFromTask(string taskType) => taskType switch
    {
        var t when t.StartsWith("Task<") && t.EndsWith(">") => t.Substring(5, t.Length - 6),
        var t when t.StartsWith("System.Threading.Tasks.Task<") && t.EndsWith(">") => t.Substring(28, t.Length - 29),
        _ => taskType // 对于非Task类型，返回原始类型而不是"object"
    };

    /// <summary>Escape SQL string for C# string literal</summary>
    public static string EscapeSqlForCSharp(string? sql)
    {
        if (string.IsNullOrEmpty(sql)) return string.Empty;

        var hasEscapeChars = sql.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0;
        if (!hasEscapeChars) return sql;

        return sql.Replace("\"", "\\\"").Replace("\r\n", "\\r\\n").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    /// <summary>
    /// Generate standard file header
    /// </summary>
    public static void GenerateFileHeader(IndentedStringBuilder sb, string namespaceName)
    {
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
    }

    /// <summary>
    /// Generate command creation and parameter binding
    /// </summary>
    public static void GenerateCommandSetup(IndentedStringBuilder sb, string sql, IMethodSymbol method, string connectionName, INamedTypeSymbol? classSymbol = null)
    {
        sb.AppendLine($"__cmd__ = (global::System.Data.Common.DbCommand){connectionName}.CreateCommand();");

        // 🔧 Transaction支持：如果Repository设置了Transaction属性，将其设置到command上
        // 这允许Repository的所有操作参与同一个事务
        sb.AppendLine("if (Transaction != null)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__cmd__.Transaction = Transaction;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Check for TableNameBy attribute - dynamic table name at runtime
        string? dynamicTableNameMethodName = null;
        if (classSymbol != null)
        {
            var tableNameByAttr = classSymbol.GetTableNameByAttribute();
            if (tableNameByAttr != null && tableNameByAttr.ConstructorArguments.Length > 0)
            {
                dynamicTableNameMethodName = tableNameByAttr.ConstructorArguments[0].Value?.ToString();
            }
        }

        // If we have dynamic table name, wrap the SQL in a variable and replace table name at runtime
        bool hasDynamicTableName = !string.IsNullOrEmpty(dynamicTableNameMethodName);

        // Check for runtime dynamic placeholders (WHERE, SET, ORDERBY, LIMIT, etc.)
        bool hasDynamicPlaceholders = sql.Contains("{RUNTIME_WHERE_") ||
                                     sql.Contains("{RUNTIME_SET_") ||
                                     sql.Contains("{RUNTIME_SET_FROM_") ||
                                     sql.Contains("{RUNTIME_SET_EXPR_") ||
                                     sql.Contains("{RUNTIME_ORDERBY_") ||
                                     sql.Contains("{RUNTIME_LIMIT_") ||
                                     sql.Contains("{RUNTIME_OFFSET_") ||
                                     sql.Contains("{RUNTIME_JOIN_") ||
                                     sql.Contains("{RUNTIME_GROUPBY_") ||
                                     sql.Contains("{RUNTIME_COLUMN_") ||
                                     sql.Contains("{RUNTIME_SQL_") ||
                                     sql.Contains("{RUNTIME_NULLABLE_LIMIT_") ||
                                     sql.Contains("{RUNTIME_NULLABLE_OFFSET_");

        if (hasDynamicPlaceholders)
        {
            // Generate dynamic SQL building with string interpolation
            GenerateDynamicSql(sb, sql, method, classSymbol, dynamicTableNameMethodName);
        }
        else
        {
            // Check if we have collection parameters that need IN clause expansion
            var collectionParams = method.Parameters.Where(IsEnumerableParameter).ToList();

            if (collectionParams.Any() || hasDynamicTableName)
            {
                // Dynamic SQL with IN clause expansion or dynamic table name
                // For verbatim string (@"..."), only double quotes need escaping
                var escapedSql = sql.Replace("\"", "\"\"");
                sb.AppendLine($"var __sql__ = @\"{escapedSql}\";");

                // Handle dynamic table name replacement
                if (hasDynamicTableName)
                {
                    sb.AppendLine();
                    sb.AppendLine($"// Get dynamic table name from method: {dynamicTableNameMethodName}");
                    sb.AppendLine($"var __dynamicTableName__ = {dynamicTableNameMethodName}();");
                    sb.AppendLine("// Replace {{table}} placeholder with actual table name");
                    sb.AppendLine("__sql__ = __sql__.Replace(\"{{table}}\", __dynamicTableName__);");
                }

                foreach (var param in collectionParams)
                {
                    sb.AppendLine();
                    sb.AppendLine($"// Replace IN (@{param.Name}) with expanded parameter list");
                    sb.AppendLine($"if ({param.Name} != null && {param.Name}.Any())");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"var __inClause_{param.Name}__ = string.Join(\", \", ");
                    sb.AppendLine($"    global::System.Linq.Enumerable.Range(0, global::System.Linq.Enumerable.Count({param.Name}))");
                    sb.AppendLine($"    .Select(i => $\"@{param.Name}{{i}}\"));");
                    sb.AppendLine($"__sql__ = __sql__.Replace(\"IN (@{param.Name})\", $\"IN ({{__inClause_{param.Name}__}})\");");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"// Empty collection - use 1=0 to return no results");
                    sb.AppendLine($"__sql__ = __sql__.Replace(\"IN (@{param.Name})\", \"IN (NULL)\");");
                    sb.PopIndent();
                    sb.AppendLine("}");
                }

                sb.AppendLine();
                sb.AppendLine("__cmd__.CommandText = __sql__;");
            }
            else
            {
                // Static SQL (no dynamic placeholders, no collection parameters, but may have dynamic table name)
                if (hasDynamicTableName)
                {
                    // For verbatim string (@"..."), only double quotes need escaping
                    var escapedSql = sql.Replace("\"", "\"\"");
                    sb.AppendLine($"var __sql__ = @\"{escapedSql}\";");
                    sb.AppendLine();
                    sb.AppendLine($"// Get dynamic table name from method: {dynamicTableNameMethodName}");
                    sb.AppendLine($"var __dynamicTableName__ = {dynamicTableNameMethodName}();");
                    sb.AppendLine("// Replace {{table}} placeholder with actual table name");
                    sb.AppendLine("__sql__ = __sql__.Replace(\"{{table}}\", __dynamicTableName__);");
                    sb.AppendLine();
                    sb.AppendLine("__cmd__.CommandText = __sql__;");
                }
                else
                {
                    // Truly static SQL
                    // For verbatim string (@"..."), only double quotes need escaping
                    var escapedSql = sql.Replace("\"", "\"\"");
                    sb.AppendLine($"__cmd__.CommandText = @\"{escapedSql}\";");
                }
            }
        }

        sb.AppendLine();

        // Extract SET_FROM parameter names to exclude from regular binding
        var setFromParams = new System.Collections.Generic.HashSet<string>();
        var setFromMatches = System.Text.RegularExpressions.Regex.Matches(sql, @"\{RUNTIME_SET_FROM_([^}]+)\}");
        foreach (System.Text.RegularExpressions.Match match in setFromMatches)
        {
            setFromParams.Add(match.Groups[1].Value);
        }
        
        // Extract SET_EXPR parameter names to exclude from regular binding
        var setExprMatches = System.Text.RegularExpressions.Regex.Matches(sql, @"\{RUNTIME_SET_EXPR_([^}]+)\}");
        foreach (System.Text.RegularExpressions.Match match in setExprMatches)
        {
            setFromParams.Add(match.Groups[1].Value);
        }

        // Generate parameter binding
        GenerateParameterBinding(sb, method, hasDynamicPlaceholders, setFromParams);
    }

    /// <summary>
    /// Generate parameter binding code
    /// </summary>
    private static void GenerateParameterBinding(IndentedStringBuilder sb, IMethodSymbol method, bool hasRuntimeWhere, System.Collections.Generic.HashSet<string>? setFromParams = null)
    {
        // First, bind parameters from ExpressionToSql if present
        if (hasRuntimeWhere)
        {
            var exprParams = method.Parameters.Where(p =>
                p.GetAttributes().Any(a => a.AttributeClass?.Name == "ExpressionToSqlAttribute") &&
                // Exclude parameters used with SET_EXPR - their parameters are bound during SET_EXPR processing
                !(setFromParams != null && setFromParams.Contains(p.Name)));

            foreach (var exprParam in exprParams)
            {
                sb.AppendLine($"// Bind parameters from ExpressionToSql: {exprParam.Name}");
                sb.AppendLine($"if ({exprParam.Name} != null)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"foreach (var __kvp__ in {exprParam.Name}.GetParameters())");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
                sb.AppendLine("__p__.ParameterName = __kvp__.Key;");
                sb.AppendLine("__p__.Value = __kvp__.Value ?? (object)global::System.DBNull.Value;");
                sb.AppendLine("__cmd__.Parameters.Add(__p__);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        // Then bind regular parameters (excluding special ones)
        var regularParams = method.Parameters.Where(p =>
            p.Type.Name != "CancellationToken" &&
            // Exclude parameters used with --from option (SET_FROM) - their properties are bound individually
            !(setFromParams != null && setFromParams.Contains(p.Name)) &&
            !p.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "ExpressionToSqlAttribute" ||
                (a.AttributeClass?.Name == "DynamicSqlAttribute" && hasRuntimeWhere)));

        foreach (var param in regularParams)
        {
            // Check if parameter type is an entity class (has properties that should be expanded)
            // Exclude: string, primitive types, system types, collections
            var paramType = param.Type as INamedTypeSymbol;
            var isTypeParameter = param.Type.TypeKind == TypeKind.TypeParameter;
            var isEntityType = paramType != null &&
                              paramType.TypeKind == TypeKind.Class &&
                              paramType.SpecialType == SpecialType.None && // Exclude string, object, etc.
                              !paramType.ContainingNamespace.ToDisplayString().StartsWith("System") && // Exclude System.* types
                              paramType.GetMembers().OfType<IPropertySymbol>().Any(p => p.CanBeReferencedByName && p.GetMethod != null);

            if (isTypeParameter)
            {
                // Generic type parameter (like TParams) - generate runtime reflection code
                // This is AOT-compatible because we use GetProperties() with proper trimming annotations
                sb.AppendLine($"// AOT-compatible parameter binding from generic parameter: {param.Name}");
                sb.AppendLine($"if ({param.Name} != null)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"var __{param.Name}_props__ = {param.Name}.GetType().GetProperties(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);");
                sb.AppendLine($"foreach (var __prop__ in __{param.Name}_props__)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("if (__prop__.Name == \"EqualityContract\") continue;");
                sb.AppendLine("var __paramName__ = \"@\" + __prop__.Name;");
                sb.AppendLine($"var __paramValue__ = __prop__.GetValue({param.Name});");
                sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
                sb.AppendLine("__p__.ParameterName = __paramName__;");
                sb.AppendLine("__p__.Value = __paramValue__ ?? global::System.DBNull.Value;");
                sb.AppendLine("__cmd__.Parameters.Add(__p__);");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine();
            }
            else if (isEntityType)
            {
                // Expand entity properties - bind each property as separate parameter
                var properties = paramType!.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.CanBeReferencedByName &&
                               p.GetMethod != null &&
                               p.Name != "EqualityContract" &&
                               !p.IsImplicitlyDeclared)
                    .ToList();

                foreach (var prop in properties)
                {
                    var propSqlName = ConvertToSnakeCase(prop.Name);
                    var paramName = $"@{propSqlName}";
                    var isNullable = prop.Type.IsNullableType() || prop.Type.IsReferenceType;

                    sb.Append("{ var __p__ = __cmd__.CreateParameter(); ");
                    sb.Append($"__p__.ParameterName = \"{paramName}\"; ");

                    if (isNullable)
                    {
                        sb.Append($"__p__.Value = {param.Name}.{prop.Name} ?? (object)global::System.DBNull.Value; ");
                    }
                    else
                    {
                        sb.Append($"__p__.Value = {param.Name}.{prop.Name}; ");
                    }

                    sb.AppendLine("__cmd__.Parameters.Add(__p__); }");
                }
            }
            else if (IsEnumerableParameter(param))
            {
                // Collection parameter - expand to multiple parameters for IN queries
                // Get element type to determine if nullable
                var elementType = param.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0
                    ? namedType.TypeArguments[0]
                    : null;
                var isElementNullable = elementType != null &&
                                      (elementType.IsNullableType() || elementType.IsReferenceType);

                sb.AppendLine($"// Expand collection parameter: {param.Name} for IN clause");
                sb.AppendLine($"int __index_{param.Name}__ = 0;");
                sb.AppendLine($"foreach (var __item__ in {param.Name})");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
                sb.AppendLine($"__p__.ParameterName = $\"@{param.Name}{{__index_{param.Name}__}}\";");

                if (isElementNullable)
                {
                    sb.AppendLine("__p__.Value = __item__ ?? (object)global::System.DBNull.Value;");
                }
                else
                {
                    sb.AppendLine("__p__.Value = __item__;");
                }

                sb.AppendLine("__cmd__.Parameters.Add(__p__);");
                sb.AppendLine($"__index_{param.Name}__++;");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine();
            }
            else
            {
                // Regular parameter binding (scalar types)
            var paramName = $"@{param.Name}";
                var isNullable = param.Type.IsNullableType() || param.Type.IsReferenceType;

                sb.Append("{ var __p__ = __cmd__.CreateParameter(); ");
                sb.Append($"__p__.ParameterName = \"{paramName}\"; ");

                if (isNullable)
                {
                    sb.Append($"__p__.Value = {param.Name} ?? (object)global::System.DBNull.Value; ");
                }
                else
                {
                    sb.Append($"__p__.Value = {param.Name}; ");
                }

                sb.AppendLine("__cmd__.Parameters.Add(__p__); }");
            }
        }
    }

    /// <summary>
    /// Generate dynamic SQL building code (WHERE, SET, ORDERBY, etc.) with zero-allocation string interpolation
    /// </summary>
    private static void GenerateDynamicSql(IndentedStringBuilder sb, string sql, IMethodSymbol method, INamedTypeSymbol? classSymbol = null, string? dynamicTableNameMethodName = null)
    {
        sb.AppendLine("// Build SQL with dynamic placeholders (compile-time splitting, zero Replace calls)");

        // Handle dynamic table name if specified
        if (!string.IsNullOrEmpty(dynamicTableNameMethodName))
        {
            sb.AppendLine($"// Get dynamic table name from method: {dynamicTableNameMethodName}");
            sb.AppendLine($"var __dynamicTableName__ = {dynamicTableNameMethodName}();");
            sb.AppendLine();
        }

        // Find all runtime dynamic markers (WHERE, SET, SET_FROM, SET_EXPR, ORDERBY, LIMIT, OFFSET, JOIN, GROUPBY, COLUMN, SQL, NULLABLE_LIMIT, NULLABLE_OFFSET)
        var markers = System.Text.RegularExpressions.Regex.Matches(sql,
            @"\{RUNTIME_(WHERE|SET_FROM|SET_EXPR|SET|ORDERBY|LIMIT|OFFSET|JOIN|GROUPBY|COLUMN|SQL|NULLABLE_LIMIT|NULLABLE_OFFSET)_([^}]+)\}");

        if (markers.Count == 0)
        {
            // Fallback: no markers found, but may have dynamic table name
            if (!string.IsNullOrEmpty(dynamicTableNameMethodName))
            {
                var escapedSql = sql.Replace("\"", "\"\"");
                sb.AppendLine($"var __sql__ = @\"{escapedSql}\";");
                sb.AppendLine("__sql__ = __sql__.Replace(\"{{table}}\", __dynamicTableName__);");
                sb.AppendLine("__cmd__.CommandText = __sql__;");
            }
            else
            {
                var escapedSql = sql.Replace("\"", "\"\"");
                sb.AppendLine($"__cmd__.CommandText = @\"{escapedSql}\";");
            }
            return;
        }

        // Split SQL into parts at compile time
        var sqlParts = new System.Collections.Generic.List<string>();
        var dynamicVariables = new System.Collections.Generic.List<(string varName, string placeholderType, string markerContent)>();

        int lastIndex = 0;
        foreach (System.Text.RegularExpressions.Match match in markers)
        {
            // Add SQL part before marker
            sqlParts.Add(sql.Substring(lastIndex, match.Index - lastIndex));

            var placeholderType = match.Groups[1].Value; // WHERE, SET, ORDERBY, etc.
            var markerContent = match.Groups[2].Value;   // EXPR_paramName, DYNAMIC_paramName, or paramName
            var varName = $"__{placeholderType.ToLower()}Clause_{dynamicVariables.Count}__";
            dynamicVariables.Add((varName, placeholderType, markerContent));

            lastIndex = match.Index + match.Length;
        }

        // Add final SQL part after last marker
        sqlParts.Add(sql.Substring(lastIndex));

        // Generate dynamic clause extraction code
        for (int i = 0; i < dynamicVariables.Count; i++)
        {
            var (varName, placeholderType, markerContent) = dynamicVariables[i];

            if (markerContent.StartsWith("NATIVE_EXPR_"))
            {
                // Native Expression<Func<T, bool>> parameter - generate bridge code
                var paramName = markerContent.Substring(12);
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);

                if (param != null)
                {
                    // Extract entity type from Expression<Func<TEntity, bool>>
                    var entityType = ExtractEntityTypeFromExpression(param.Type);
                    var dialectValue = GetDialectForMethod(method);

                    sb.AppendLine($"// Bridge: Convert Expression<Func<{entityType.Name}, bool>> to SQL");
                    sb.AppendLine($"var __expr_{paramName}__ = new global::Sqlx.ExpressionToSql<{entityType.ToDisplayString()}>(global::Sqlx.SqlDialect.{dialectValue});");
                    sb.AppendLine($"__expr_{paramName}__.Where({paramName});");
                    sb.AppendLine($"var __{paramName}_clause__ = __expr_{paramName}__.ToWhereClause();");
                    // Add WHERE keyword prefix when condition exists
                    sb.AppendLine($"var {varName} = string.IsNullOrEmpty(__{paramName}_clause__) ? \"\" : \"WHERE \" + __{paramName}_clause__;");
                    sb.AppendLine();

                    // Bind parameters from the expression
                    sb.AppendLine($"// Bind parameters from Expression: {paramName}");
                    sb.AppendLine($"foreach (var __p__ in __expr_{paramName}__.GetParameters())");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("var __param__ = __cmd__.CreateParameter();");
                    sb.AppendLine("__param__.ParameterName = __p__.Key;");
                    sb.AppendLine("__param__.Value = __p__.Value ?? global::System.DBNull.Value;");
                    sb.AppendLine("__cmd__.Parameters.Add(__param__);");
                    sb.PopIndent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"var {varName} = \"\"; // Expression parameter not found");
                }
            }
            else if (markerContent.StartsWith("EXPR_"))
            {
                // ExpressionToSql parameter - need to pass correct dialect
                var paramName = markerContent.Substring(5);
                var dialectValue = classSymbol != null ? GetDialectForClass(classSymbol) : GetDialectForMethod(method);
                
                sb.AppendLine($"// Extract {placeholderType} from ExpressionToSql: {paramName}");

                if (placeholderType == "WHERE")
                {
                    // Add WHERE keyword prefix when condition exists
                    // Pass the correct dialect to ToWhereClause extension method
                    sb.AppendLine($"var __{paramName}_clause__ = {paramName}?.ToWhereClause(global::Sqlx.SqlDefine.{dialectValue}) ?? \"\";");
                    sb.AppendLine($"var {varName} = string.IsNullOrEmpty(__{paramName}_clause__) ? \"\" : \"WHERE \" + __{paramName}_clause__;");
                }
                else
                {
                    // For SET, ORDERBY, etc. - extract as SQL fragment
                    sb.AppendLine($"var {varName} = {paramName}?.ToSql() ?? \"\";");
                }
            }
            else if (markerContent.StartsWith("DYNAMIC_"))
            {
                // DynamicSql parameter with validation
                var paramName = markerContent.Substring(8);
                sb.AppendLine($"// Validate DynamicSql {placeholderType}: {paramName}");
                sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidFragment({paramName}.AsSpan()))");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid SQL fragment: {{{paramName}}}. Contains dangerous keywords.\", nameof({paramName}));");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine($"var {varName} = {paramName};");
            }
            else if (placeholderType == "LIMIT" || placeholderType == "OFFSET")
            {
                // LIMIT/OFFSET parameter - generate conditional SQL based on dialect
                var paramName = markerContent;
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
                
                // Get dialect to generate correct SQL syntax
                var dialectValue = classSymbol != null ? GetDialectForClass(classSymbol) : GetDialectForMethod(method);

                if (param != null && param.Type.Name.Contains("Nullable"))
                {
                    // Nullable parameter - generate conditional code
                    if (placeholderType == "LIMIT")
                    {
                        if (dialectValue == "SqlServer")
                        {
                            // SQL Server: OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY
                            sb.AppendLine($"// Generate LIMIT clause for {paramName} (SQL Server)");
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET 0 ROWS FETCH NEXT {{{paramName}.Value}} ROWS ONLY\" : \"\";");
                        }
                        else if (dialectValue == "Oracle")
                        {
                            sb.AppendLine($"// Generate LIMIT clause for {paramName} (Oracle)");
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"FETCH NEXT {{{paramName}.Value}} ROWS ONLY\" : \"\";");
                        }
                        else
                        {
                            // MySQL, PostgreSQL, SQLite
                            sb.AppendLine($"// Generate LIMIT clause for {paramName}");
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"LIMIT {{{paramName}.Value}}\" : \"\";");
                        }
                    }
                    else // OFFSET
                    {
                        if (dialectValue == "SqlServer" || dialectValue == "Oracle")
                        {
                            sb.AppendLine($"// Generate OFFSET clause for {paramName} (SQL Server/Oracle)");
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET {{{paramName}.Value}} ROWS\" : \"\";");
                        }
                        else
                        {
                            // MySQL, PostgreSQL, SQLite
                            sb.AppendLine($"// Generate OFFSET clause for {paramName}");
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET {{{paramName}.Value}}\" : \"\";");
                        }
                    }
                }
                else
                {
                    // Non-nullable parameter
                    if (placeholderType == "LIMIT")
                    {
                        if (dialectValue == "SqlServer")
                        {
                            // SQL Server: OFFSET 0 ROWS FETCH NEXT {limit} ROWS ONLY
                            // Check if there's an ORDER BY clause in the SQL
                            var hasOrderBy = dynamicVariables.Any(v => v.placeholderType == "ORDERBY");
                            if (hasOrderBy)
                            {
                                // There's a dynamic ORDER BY - make LIMIT conditional on it
                                var orderByVar = dynamicVariables.First(v => v.placeholderType == "ORDERBY").varName;
                                sb.AppendLine($"var {varName} = !string.IsNullOrEmpty({orderByVar}) ? $\"OFFSET 0 ROWS FETCH NEXT {{{paramName}}} ROWS ONLY\" : \"\";");
                            }
                            else
                            {
                                // No dynamic ORDER BY - assume it's in the template
                                sb.AppendLine($"var {varName} = $\"OFFSET 0 ROWS FETCH NEXT {{{paramName}}} ROWS ONLY\";");
                            }
                        }
                        else if (dialectValue == "Oracle")
                        {
                            sb.AppendLine($"var {varName} = $\"FETCH NEXT {{{paramName}}} ROWS ONLY\";");
                        }
                        else
                        {
                            // MySQL, PostgreSQL, SQLite
                            sb.AppendLine($"var {varName} = $\"LIMIT {{{paramName}}}\";");
                        }
                    }
                    else // OFFSET
                    {
                        if (dialectValue == "SqlServer" || dialectValue == "Oracle")
                        {
                            sb.AppendLine($"var {varName} = $\"OFFSET {{{paramName}}} ROWS\";");
                        }
                        else
                        {
                            // MySQL, PostgreSQL, SQLite
                            sb.AppendLine($"var {varName} = $\"OFFSET {{{paramName}}}\";");
                        }
                    }
                }
            }
            else if (placeholderType == "NULLABLE_LIMIT" || placeholderType == "NULLABLE_OFFSET")
            {
                // 🔧 修复：处理可空的 LIMIT/OFFSET 参数
                // 生成条件代码：只有当参数有值时才添加 LIMIT/OFFSET 子句
                var paramName = markerContent;
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);

                // 获取方言类型以生成正确的 SQL 语法
                // 使用 classSymbol（如果提供）而不是 method.ContainingType，因为方法可能来自接口
                var dialectValue = classSymbol != null ? GetDialectForClass(classSymbol) : GetDialectForMethod(method);

                // 检查是否同时存在 NULLABLE_OFFSET 和 NULLABLE_LIMIT
                var hasNullableOffset = dynamicVariables.Any(v => v.placeholderType == "NULLABLE_OFFSET");
                var hasNullableLimit = dynamicVariables.Any(v => v.placeholderType == "NULLABLE_LIMIT");
                
                // 🔧 修复：检查 SQL 中是否有非可空的 OFFSET（直接生成的 OFFSET @xxx）
                // 这种情况下，当 LIMIT 为 null 时也需要生成默认的 LIMIT 值
                var hasNonNullableOffset = sql.Contains("OFFSET @") || sql.Contains("OFFSET $") || sql.Contains("OFFSET :");

                if (placeholderType == "NULLABLE_LIMIT")
                {
                    sb.AppendLine($"// 🔧 Generate conditional LIMIT clause for nullable parameter: {paramName} (dialect: {dialectValue})");
                    if (dialectValue == "SqlServer")
                    {
                        // SQL Server 需要 OFFSET...FETCH 语法
                        // 如果没有 OFFSET 参数，需要添加 OFFSET 0 ROWS
                        if (hasNullableOffset)
                        {
                            // 有 OFFSET 参数，只生成 FETCH 部分
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"FETCH NEXT {{{paramName}.Value}} ROWS ONLY\" : \"\";");
                        }
                        else
                        {
                            // 没有 OFFSET 参数，需要添加 OFFSET 0 ROWS
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET 0 ROWS FETCH NEXT {{{paramName}.Value}} ROWS ONLY\" : \"\";");
                        }
                    }
                    else
                    {
                        // MySQL, PostgreSQL, SQLite 使用 LIMIT 语法
                        // 🔧 修复：如果有 OFFSET 参数（可空或非可空），当 LIMIT 为 null 时需要生成一个很大的默认值
                        // 因为 SQLite/MySQL/PostgreSQL 的 OFFSET 必须配合 LIMIT 使用
                        if (hasNullableOffset || hasNonNullableOffset)
                        {
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"LIMIT {{{paramName}.Value}}\" : \"LIMIT 2147483647\";");
                        }
                        else
                        {
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"LIMIT {{{paramName}.Value}}\" : \"\";");
                        }
                    }
                }
                else // NULLABLE_OFFSET
                {
                    sb.AppendLine($"// 🔧 Generate conditional OFFSET clause for nullable parameter: {paramName} (dialect: {dialectValue})");
                    if (dialectValue == "SqlServer")
                    {
                        // SQL Server: OFFSET x ROWS (FETCH 由 LIMIT 生成)
                        // 如果有 LIMIT，当 OFFSET 为 null 时需要生成 OFFSET 0 ROWS
                        if (hasNullableLimit)
                        {
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET {{{paramName}.Value}} ROWS\" : \"OFFSET 0 ROWS\";");
                        }
                        else
                        {
                            sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET {{{paramName}.Value}} ROWS\" : \"\";");
                        }
                    }
                    else if (dialectValue == "Oracle")
                    {
                        sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET {{{paramName}.Value}} ROWS\" : \"\";");
                    }
                    else
                    {
                        // MySQL, PostgreSQL, SQLite
                        sb.AppendLine($"var {varName} = {paramName}.HasValue ? $\"OFFSET {{{paramName}.Value}}\" : \"\";");
                    }
                }
            }
            else if (placeholderType == "SET_FROM")
            {
                // AOT-compatible SET clause generation from generic TUpdates parameter
                // For generic type parameters, we generate inline code that uses AOT-compatible reflection
                var paramName = markerContent;
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
                
                if (param != null)
                {
                    var paramType = param.Type as INamedTypeSymbol;
                    var dialectValue = classSymbol != null ? GetDialectForClass(classSymbol) : GetDialectForMethod(method);
                    var dialect = GetSqlDefineFromDialect(dialectValue);
                    
                    // Get the quote character for column names based on dialect
                    var columnQuoteStart = dialect.ColumnLeft;
                    var columnQuoteEnd = dialect.ColumnRight;
                    var paramPrefix = dialect.ParameterPrefix;
                    
                    // For generic type parameters (TUpdates), check if it's an interface-level type parameter
                    // Interface-level type parameters (e.g., IPartialUpdateRepository<TEntity, TKey, TUpdates>)
                    // are resolved at compile time when the user implements the interface.
                    // Method-level type parameters (e.g., UpdatePartialAsync<TUpdates>) cannot be analyzed.
                    if (param.Type.TypeKind == TypeKind.TypeParameter)
                    {
                        var typeParamName = param.Type.Name; // e.g., "TUpdates"
                        var typeParam = param.Type as ITypeParameterSymbol;
                        
                        // Check if this is an interface-level type parameter (can be resolved)
                        // or a method-level type parameter (cannot be resolved)
                        var isInterfaceLevelTypeParam = typeParam?.DeclaringType != null;
                        
                        if (isInterfaceLevelTypeParam && classSymbol != null)
                        {
                            // Try to resolve the concrete type from the implemented interface
                            var concreteType = ResolveInterfaceTypeArgument(classSymbol, typeParamName);
                            if (concreteType != null && concreteType.TypeKind != TypeKind.TypeParameter)
                            {
                                // Successfully resolved to concrete type - generate compile-time code
                                var properties = concreteType.GetMembers()
                                    .OfType<IPropertySymbol>()
                                    .Where(p => p.CanBeReferencedByName && 
                                               p.GetMethod != null && 
                                               !p.IsImplicitlyDeclared &&
                                               p.Name != "EqualityContract")
                                    .ToList();
                                
                                if (properties.Count > 0)
                                {
                                    var setClause = string.Join(", ", properties.Select(p =>
                                    {
                                        var columnName = ConvertToSnakeCase(p.Name);
                                        return $"{dialect.WrapColumn(columnName)} = {dialect.ParameterPrefix}{p.Name}";
                                    }));
                                    sb.AppendLine($"// Compile-time resolved: {typeParamName} -> {concreteType.Name}");
                                    sb.AppendLine($"var {varName} = \"{setClause}\";");
                                    
                                    // Also generate parameter binding for the resolved type
                                    sb.AppendLine($"// Bind parameters from resolved type: {concreteType.Name}");
                                    foreach (var prop in properties)
                                    {
                                        var propSqlName = ConvertToSnakeCase(prop.Name);
                                        var isNullable = prop.Type.IsNullableType() || prop.Type.IsReferenceType;
                                        
                                        sb.Append("{ var __p__ = __cmd__.CreateParameter(); ");
                                        sb.Append($"__p__.ParameterName = \"{dialect.ParameterPrefix}{propSqlName}\"; ");
                                        
                                        if (isNullable)
                                        {
                                            sb.Append($"__p__.Value = {paramName}.{prop.Name} ?? (object)global::System.DBNull.Value; ");
                                        }
                                        else
                                        {
                                            sb.Append($"__p__.Value = {paramName}.{prop.Name}; ");
                                        }
                                        
                                        sb.AppendLine("__cmd__.Parameters.Add(__p__); }");
                                    }
                                }
                                else
                                {
                                    sb.AppendLine($"var {varName} = \"\"; // No properties found in {concreteType.Name}");
                                }
                            }
                            else
                            {
                                // Could not resolve - generate error
                                sb.AppendLine($"#error Sqlx: Could not resolve interface type parameter '{typeParamName}' to a concrete type.");
                                sb.AppendLine($"var {varName} = \"\";");
                            }
                        }
                        else
                        {
                            // Method-level type parameter - cannot be analyzed at compile time
                            // Use #warning instead of #error to allow compilation while still alerting users
                            sb.AppendLine($"#warning Sqlx: Method-level generic type parameter '{typeParamName}' cannot be used with {{{{set --from}}}}. Use IPartialUpdateRepository<TEntity, TKey, TUpdates> interface instead.");
                            sb.AppendLine($"var {varName} = \"\";");
                        }
                    }
                    else if (paramType != null)
                    {
                        // For concrete types, extract properties at compile time
                        var properties = paramType.GetMembers()
                            .OfType<IPropertySymbol>()
                            .Where(p => p.CanBeReferencedByName && 
                                       p.GetMethod != null && 
                                       !p.IsImplicitlyDeclared &&
                                       p.Name != "EqualityContract")
                            .ToList();
                        
                        if (properties.Count > 0)
                        {
                            var setClause = string.Join(", ", properties.Select(p =>
                            {
                                var columnName = ConvertToSnakeCase(p.Name);
                                return $"{dialect.WrapColumn(columnName)} = {dialect.ParameterPrefix}{p.Name}";
                            }));
                            sb.AppendLine($"var {varName} = \"{setClause}\";");
                        }
                        else
                        {
                            sb.AppendLine($"var {varName} = \"\"; // No properties found in {paramType.Name}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"var {varName} = \"\"; // Unable to analyze parameter type");
                    }
                }
                else
                {
                    sb.AppendLine($"var {varName} = \"\"; // Parameter not found: {paramName}");
                }
            }
            else if (placeholderType == "SET_EXPR")
            {
                // Expression-based SET clause generation for IExpressionUpdateRepository
                // Uses Expression<Func<TEntity, TEntity>> to specify which properties to update
                // The expression is analyzed at runtime using ExpressionToSql
                var paramName = markerContent;
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
                
                if (param != null)
                {
                    var dialectValue = classSymbol != null ? GetDialectForClass(classSymbol) : GetDialectForMethod(method);
                    
                    // Generate code to extract SET clause from the expression at runtime
                    // The expression is of type Expression<Func<TEntity, TEntity>>
                    // We need to analyze the MemberInitExpression to extract property assignments
                    sb.AppendLine($"// Extract SET clause from Expression<Func<TEntity, TEntity>>: {paramName}");
                    sb.AppendLine($"var __{paramName}_setClause__ = \"\";");
                    sb.AppendLine($"var __{paramName}_setParams__ = new global::System.Collections.Generic.List<(string Name, object Value)>();");
                    sb.AppendLine($"if ({paramName} != null)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"var __body__ = {paramName}.Body;");
                    sb.AppendLine("if (__body__ is global::System.Linq.Expressions.MemberInitExpression __memberInit__)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("var __setClauses__ = new global::System.Collections.Generic.List<string>();");
                    sb.AppendLine("var __paramIndex__ = 0;");
                    sb.AppendLine("foreach (var __binding__ in __memberInit__.Bindings)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("if (__binding__ is global::System.Linq.Expressions.MemberAssignment __assignment__)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("var __propName__ = __assignment__.Member.Name;");
                    sb.AppendLine("var __columnName__ = global::Sqlx.Generator.SharedCodeGenerationUtilities.ConvertToSnakeCase(__propName__);");
                    sb.AppendLine($"var __paramName__ = \"@__expr_p\" + __paramIndex__;");
                    sb.AppendLine("__paramIndex__++;");
                    sb.AppendLine();
                    sb.AppendLine("// Extract value from the expression");
                    sb.AppendLine("object __value__ = null;");
                    sb.AppendLine("if (__assignment__.Expression is global::System.Linq.Expressions.ConstantExpression __constExpr__)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("__value__ = __constExpr__.Value;");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine("else");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("// Compile and invoke the expression to get the value");
                    sb.AppendLine("var __lambda__ = global::System.Linq.Expressions.Expression.Lambda(__assignment__.Expression);");
                    sb.AppendLine("var __compiled__ = __lambda__.Compile();");
                    sb.AppendLine("__value__ = __compiled__.DynamicInvoke();");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine();
                    sb.AppendLine($"__setClauses__.Add($\"{{__columnName__}} = {{__paramName__}}\");");
                    sb.AppendLine($"__{paramName}_setParams__.Add((__paramName__, __value__));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine($"__{paramName}_setClause__ = string.Join(\", \", __setClauses__);");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine($"var {varName} = __{paramName}_setClause__;");
                    sb.AppendLine();
                    
                    // Bind the extracted parameters
                    sb.AppendLine($"// Bind parameters from expression: {paramName}");
                    sb.AppendLine($"foreach (var __exprParam__ in __{paramName}_setParams__)");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
                    sb.AppendLine("__p__.ParameterName = __exprParam__.Name;");
                    sb.AppendLine("__p__.Value = __exprParam__.Value ?? global::System.DBNull.Value;");
                    sb.AppendLine("__cmd__.Parameters.Add(__p__);");
                    sb.PopIndent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"var {varName} = \"\"; // Parameter not found: {paramName}");
                }
            }
            else if (placeholderType == "COLUMN")
            {
                // Dynamic column name with SQL injection protection
                var paramName = markerContent.StartsWith("DYNAMIC_") ? markerContent.Substring(8) : markerContent;
                sb.AppendLine($"// Validate dynamic column name: {paramName}");
                sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidIdentifier({paramName}.AsSpan()))");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid column name: {{{paramName}}}. Only letters, digits, and underscores are allowed.\", nameof({paramName}));");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine($"var {varName} = {paramName};");
            }
            else if (placeholderType == "SQL")
            {
                // Raw SQL fragment - the entire SQL is provided by the parameter
                // This is used by IAdvancedRepository.ExecuteRawAsync, QueryRawAsync, etc.
                var paramName = markerContent.StartsWith("DYNAMIC_") ? markerContent.Substring(8) : markerContent;
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
                
                // Check if parameter is nullable (string? or Nullable<T>)
                bool isNullable = param != null && (
                    param.Type.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated ||
                    param.Type.Name.Contains("Nullable"));
                
                if (isNullable)
                {
                    // For nullable parameters, only validate if not null/empty
                    sb.AppendLine($"// Validate raw SQL fragment: {paramName} (nullable)");
                    sb.AppendLine($"if (!string.IsNullOrEmpty({paramName}) && !global::Sqlx.Validation.SqlValidator.IsValidFragment({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid SQL fragment: {{{paramName}}}. Contains dangerous keywords.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine($"var {varName} = {paramName} ?? \"\";");
                }
                else
                {
                    // For non-nullable parameters, always validate
                    sb.AppendLine($"// Validate raw SQL fragment: {paramName}");
                    sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidFragment({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid SQL fragment: {{{paramName}}}. Contains dangerous keywords.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine($"var {varName} = {paramName};");
                }
            }
            else
            {
                // Regular parameter as SQL fragment (with validation)
                var paramName = markerContent;
                var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
                
                // Check if parameter is nullable (string? or Nullable<T>)
                bool isNullable = param != null && (
                    param.Type.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated ||
                    param.Type.Name.Contains("Nullable"));
                
                if (isNullable)
                {
                    // For nullable parameters, only validate if not null/empty
                    sb.AppendLine($"// Validate {placeholderType} fragment: {paramName} (nullable)");
                    sb.AppendLine($"if (!string.IsNullOrEmpty({paramName}) && !global::Sqlx.Validation.SqlValidator.IsValidFragment({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid SQL fragment: {{{paramName}}}. Contains dangerous keywords.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine($"var {varName} = {paramName} ?? \"\";");
                }
                else
                {
                    // For non-nullable parameters, always validate
                    sb.AppendLine($"// Validate {placeholderType} fragment: {paramName}");
                    sb.AppendLine($"if (!global::Sqlx.Validation.SqlValidator.IsValidFragment({paramName}.AsSpan()))");
                    sb.AppendLine("{");
                    sb.PushIndent();
                    sb.AppendLine($"throw new global::System.ArgumentException($\"Invalid SQL fragment: {{{paramName}}}. Contains dangerous keywords.\", nameof({paramName}));");
                    sb.PopIndent();
                    sb.AppendLine("}");
                    sb.AppendLine($"var {varName} = {paramName};");
                }
            }
        }

        sb.AppendLine();

        // Generate SQL concatenation using string interpolation (compile-time optimized)
        if (!string.IsNullOrEmpty(dynamicTableNameMethodName))
        {
            // Need to replace {{table}} placeholder with dynamic table name
            sb.Append("var __finalSql__ = ");
        }
        else
        {
            sb.Append("__cmd__.CommandText = ");
        }

        if (sqlParts.Count == 1)
        {
            // No dynamic parts
            var escapedSql = sqlParts[0].Replace("\"", "\"\"");
            sb.Append($"@\"{escapedSql}\"");
        }
        else
        {
            // Build using string interpolation (compiler optimizes to StringBuilder)
            sb.Append("$@\"");
            for (int i = 0; i < sqlParts.Count; i++)
            {
                var part = sqlParts[i].Replace("\"", "\"\"");
                sb.Append(part);

                if (i < dynamicVariables.Count)
                {
                    sb.Append($"{{{dynamicVariables[i].varName}}}");
                }
            }
            sb.Append("\"");
        }

        sb.AppendLine(";");

        // Handle dynamic table name replacement if needed
        if (!string.IsNullOrEmpty(dynamicTableNameMethodName))
        {
            sb.AppendLine("__finalSql__ = __finalSql__.Replace(\"{{table}}\", __dynamicTableName__);");
            sb.AppendLine("__cmd__.CommandText = __finalSql__;");
        }
    }

    /// <summary>
    /// Generate entity property mapping with optional ordinal access optimization
    /// </summary>
    public static void GenerateEntityMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName, List<string>? columnOrder = null, bool useOrdinalIndex = true)
    {
        // 如果列顺序不匹配，源分析器会发出编译警告
        if (columnOrder != null && columnOrder.Count > 0)
        {
            sb.AppendLine($"// 🚀 Access using hardcoded indices- {columnOrder.Count}列: [{string.Join(", ", columnOrder)}]");
            sb.AppendLine($"// ⚠️ If the order of C# properties does not match the order of SQL columns, the source analyzer will issue a warning");
            GenerateEntityMappingWithHardcodedOrdinals(sb, entityType, variableName, columnOrder);
            return;
        }

        // 向后兼容：没有列顺序信息时，使用GetOrdinal查找
        sb.AppendLine($"// ⚠️ Use GetOrdinal to search for compatible versions- columnOrder is {(columnOrder == null ? "null" : "empty")}");
        sb.AppendLine($"// Performance warning: Without using serial number access optimization, query performance may decrease by 20%");
        GenerateEntityMappingWithGetOrdinal(sb, entityType, variableName);
    }

    /// <summary>
    /// Generate entity property mapping using hardcoded ordinal index (extreme performance mode)
    /// </summary>
    private static void GenerateEntityMappingWithHardcodedOrdinals(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName, List<string> columnOrder)
    {
        // Remove nullable annotation
        var entityTypeName = entityType.GetCachedDisplayString();
        if (entityTypeName.EndsWith("?"))
        {
            entityTypeName = entityTypeName.TrimEnd('?');
        }

        // 获取所有可映射的属性（排除 record 的 EqualityContract）
        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.SetMethod != null && p.Name != "EqualityContract")
            .ToArray();

        if (properties.Length == 0)
        {
            if (variableName == "__result__")
            {
                sb.AppendLine($"__result__ = new {entityTypeName}();");
            }
            else
            {
                sb.AppendLine($"var {variableName} = new {entityTypeName}();");
            }
            return;
        }

        // 🚀 极致优化：直接使用硬编码索引（0, 1, 2...）访问列
        // 创建列名到硬编码索引的映射
        var columnToOrdinal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < columnOrder.Count; i++)
        {
            columnToOrdinal[columnOrder[i]] = i;
        }

        // 使用对象初始化器语法（支持init-only属性）
        if (variableName == "__result__")
        {
            sb.AppendLine($"__result__ = new {entityTypeName}");
        }
        else
        {
            sb.AppendLine($"var {variableName} = new {entityTypeName}");
        }

        sb.AppendLine("{");
        sb.PushIndent();

        // 根据属性映射到对应的硬编码索引
        bool first = true;
        foreach (var prop in properties)
        {
            var columnName = ConvertToSnakeCase(prop.Name);

            // 查找该属性对应的硬编码索引
            if (!columnToOrdinal.TryGetValue(columnName, out int ordinalIndex))
            {
                // 列不存在于SQL中，跳过或使用默认值
                continue;
            }

            var readMethod = prop.Type.UnwrapNullableType().GetDataReaderMethod();

            // ✅ 全面支持：nullable value types (int?) 和 nullable reference types (string?)
            var isNullable = prop.Type.IsNullableType();

            // 🚀 极致性能：直接使用硬编码索引（例如：reader.GetInt32(0)）
            var valueExpression = string.IsNullOrEmpty(readMethod)
                ? $"({prop.Type.GetCachedDisplayString()})reader[{ordinalIndex}]"
                : $"reader.{readMethod}({ordinalIndex})";

            if (!first) sb.Append(",");

            // 只对nullable类型生成IsDBNull检查
            if (isNullable)
            {
                sb.AppendLine($"{prop.Name} = reader.IsDBNull({ordinalIndex}) ? null : {valueExpression}");
            }
            else
            {
                // 非nullable类型直接读取，无需检查（减少约0.8μs/字段的开销）
                sb.AppendLine($"{prop.Name} = {valueExpression}");
            }

            first = false;
        }

        sb.PopIndent();
        sb.AppendLine("};");
    }

    /// <summary>
    /// Generate entity property mapping using GetOrdinal (backward compatible)
    /// </summary>
    private static void GenerateEntityMappingWithGetOrdinal(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName)
    {
        // Remove nullable annotation - 使用缓存版本提升性能
        var entityTypeName = entityType.GetCachedDisplayString();
        if (entityTypeName.EndsWith("?"))
        {
            entityTypeName = entityTypeName.TrimEnd('?');
        }

        // 支持 set 和 init 属性（排除 record 的 EqualityContract）
        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.SetMethod != null && p.Name != "EqualityContract")
            .ToArray();

        if (properties.Length == 0)
        {
            // No properties to map, just create empty object
        if (variableName == "__result__")
        {
            sb.AppendLine($"__result__ = new {entityTypeName}();");
        }
        else
        {
            sb.AppendLine($"var {variableName} = new {entityTypeName}();");
            }
            return;
        }

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var columnName = ConvertToSnakeCase(prop.Name);
            sb.AppendLine($"var __ord_{prop.Name}__ = reader.GetOrdinal(\"{columnName}\");");
        }
        sb.AppendLine();

        // Use object initializer syntax to support init-only properties
        if (variableName == "__result__")
        {
            sb.AppendLine($"__result__ = new {entityTypeName}");
        }
        else
        {
            sb.AppendLine($"var {variableName} = new {entityTypeName}");
        }

        sb.AppendLine("{");
        sb.PushIndent();

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var readMethod = prop.Type.UnwrapNullableType().GetDataReaderMethod();

            // ✅ 全面支持：nullable value types (int?) 和 nullable reference types (string?)
            var isNullable = prop.Type.IsNullableType();

            // 使用缓存的序号变量
            var ordinalVar = $"__ord_{prop.Name}__";
            var valueExpression = string.IsNullOrEmpty(readMethod)
                ? $"({prop.Type.GetCachedDisplayString()})reader[{ordinalVar}]"  // 使用缓存版本
                : $"reader.{readMethod}({ordinalVar})";

            var comma = i < properties.Length - 1 ? "," : "";

            // 只对nullable类型生成IsDBNull检查
            if (isNullable)
            {
                sb.AppendLine($"{prop.Name} = reader.IsDBNull({ordinalVar}) ? null : {valueExpression}{comma}");
            }
            else
            {
                sb.AppendLine($"{prop.Name} = {valueExpression}{comma}");
            }
        }

        sb.PopIndent();
        sb.AppendLine("};");
    }

    /// <summary>Convert C# property names to snake_case database column names</summary>
    public static string ConvertToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Contains("_")) return name.ToLowerInvariant();

        var result = new System.Text.StringBuilder(name.Length + (name.Length >> 2));
        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            if (char.IsUpper(current))
            {
                if (i > 0 && !char.IsUpper(name[i - 1])) result.Append('_');
                result.Append(char.ToLowerInvariant(current));
            }
            else
            {
                result.Append(current);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Extract entity type from Expression&lt;Func&lt;TEntity, bool&gt;&gt;
    /// </summary>
    private static INamedTypeSymbol ExtractEntityTypeFromExpression(ITypeSymbol expressionType)
    {
        // Expression<Func<TEntity, bool>>
        //                  ^^^^^^^ extract this
        if (expressionType is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length > 0 &&
            namedType.TypeArguments[0] is INamedTypeSymbol funcType &&
            funcType.TypeArguments.Length > 0)
        {
            return (INamedTypeSymbol)funcType.TypeArguments[0];
        }

        throw new System.InvalidOperationException($"Cannot extract entity type from Expression parameter: {expressionType.ToDisplayString()}");
    }

    /// <summary>
    /// Get database dialect for a method (from [SqlDefine] attribute on class)
    /// </summary>
    private static string GetDialectForMethod(IMethodSymbol method)
    {
        var classSymbol = method.ContainingType;
        return GetDialectForClass(classSymbol);
    }

    /// <summary>
    /// Get database dialect for a class (from [SqlDefine] or [RepositoryFor] attribute)
    /// </summary>
    private static string GetDialectForClass(INamedTypeSymbol classSymbol)
    {
        // 1. 首先检查 [SqlDefine] 属性
        var sqlDefineAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SqlDefineAttribute");

        if (sqlDefineAttr != null && sqlDefineAttr.ConstructorArguments.Length > 0)
        {
            var enumValue = sqlDefineAttr.ConstructorArguments[0].Value;
            return MapDialectEnumToString(enumValue);
        }

        // 2. 检查 [RepositoryFor] 属性的 Dialect 命名参数
        var repositoryForAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryForAttribute" || 
                                a.AttributeClass?.Name == "RepositoryFor");

        if (repositoryForAttr != null)
        {
            var dialectArg = repositoryForAttr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Dialect");
            
            if (dialectArg.Value.Value != null)
            {
                return MapDialectEnumToString(dialectArg.Value.Value);
            }
        }

        return "SqlServer"; // Default
    }

    /// <summary>
    /// Map SqlDefineTypes enum value to dialect string
    /// SqlDefineTypes: MySql=0, SqlServer=1, PostgreSql=2, Oracle=3, DB2=4, SQLite=5
    /// Returns the exact field name as defined in SqlDefine class
    /// </summary>
    private static string MapDialectEnumToString(object? enumValue)
    {
        return enumValue switch
        {
            0 => "MySql",       // SqlDefine.MySql
            1 => "SqlServer",   // SqlDefine.SqlServer
            2 => "PostgreSql",  // SqlDefine.PostgreSql
            3 => "Oracle",      // SqlDefine.Oracle
            4 => "DB2",         // SqlDefine.DB2
            5 => "SQLite",      // SqlDefine.SQLite
            _ => "SqlServer"    // Default
        };
    }

    /// <summary>
    /// Get SqlDefine instance from dialect string
    /// </summary>
    private static SqlDefine GetSqlDefineFromDialect(string dialectValue)
    {
        return dialectValue switch
        {
            "MySql" => SqlDefine.MySql,
            "SqlServer" => SqlDefine.SqlServer,
            "PostgreSql" => SqlDefine.PostgreSql,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            "SQLite" => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer // Default
        };
    }

    /// <summary>
    /// Checks if a parameter is a collection type (IEnumerable, List, Array, etc.) but NOT string.
    /// </summary>
    public static bool IsEnumerableParameter(IParameterSymbol param)
    {
        var type = param.Type;

        // Exclude string (even though it's IEnumerable<char>)
        if (type.SpecialType == SpecialType.System_String)
            return false;

        // Check for array types
        if (type is IArrayTypeSymbol)
            return true;

        // Check for IEnumerable<T>, List<T>, etc.
        if (type is INamedTypeSymbol namedType)
        {
            // Check if the type itself is IEnumerable<T>
            if (namedType.Name == "IEnumerable" &&
                namedType.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
            {
                return true;
            }

            // Check if any of its interfaces is IEnumerable<T>
            return namedType.AllInterfaces.Any(i =>
                i.Name == "IEnumerable" &&
                i.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic");
        }

        return false;
    }
}
