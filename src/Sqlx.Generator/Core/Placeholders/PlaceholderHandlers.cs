// -----------------------------------------------------------------------
// <copyright file="PlaceholderHandlers.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// Base class for placeholder handlers with common functionality.
/// </summary>
public abstract class PlaceholderHandlerBase : IPlaceholderHandler
{
    public abstract string Name { get; }
    public abstract string Process(PlaceholderContext context);

    protected PlaceholderOptions ParseOptions(PlaceholderContext context) => new(context.Options);

    protected List<IPropertySymbol> GetFilteredProperties(
        INamedTypeSymbol entityType,
        PlaceholderOptions options,
        string? excludeProperty = null,
        bool requireSetter = false)
    {
        var excludeSet = options.GetExcludeSet();
        if (excludeProperty != null) excludeSet.Add(excludeProperty);
        var onlySet = options.GetOnlySet();

        return entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName &&
                       p.GetMethod != null &&
                       p.Name != "EqualityContract" &&
                       !p.IsImplicitlyDeclared &&
                       (!requireSetter || p.SetMethod != null) &&
                       !excludeSet.Contains(p.Name) &&
                       (onlySet.Count == 0 || onlySet.Contains(p.Name)))
            .ToList();
    }

    protected static string ToSnakeCase(string name) => SharedCodeGenerationUtilities.ConvertToSnakeCase(name);

    protected static string WrapIfColumn(string value, SqlDefine dialect)
    {
        if (value.StartsWith("'") || value.StartsWith("\"") ||
            decimal.TryParse(value, out _) ||
            value.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("[") || value.StartsWith("`"))
            return value;
        return dialect.WrapColumn(value);
    }

    protected static bool IsNullableType(ITypeSymbol type) =>
        (type is INamedTypeSymbol nt && nt.IsGenericType && nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) ||
        type.NullableAnnotation == NullableAnnotation.Annotated;
}

#region Core Handlers

/// <summary>{{table}} - Table name placeholder.</summary>
public sealed class TableHandler : PlaceholderHandlerBase
{
    public override string Name => "table";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var tableName = !string.IsNullOrEmpty(options.FirstArg) ? options.FirstArg :
                       !string.IsNullOrEmpty(context.Type) && context.Type != "quoted" ? context.Type :
                       context.TableName;
        var snakeTableName = ToSnakeCase(tableName);
        return context.Type == "quoted" ? context.Dialect.WrapColumn(snakeTableName) : snakeTableName;
    }
}

/// <summary>{{columns}} - Column names placeholder.</summary>
public sealed class ColumnsHandler : PlaceholderHandlerBase
{
    public override string Name => "columns";

    public override string Process(PlaceholderContext context)
    {
        if (context.EntityType == null)
        {
            context.Result.Warnings.Add("Cannot infer columns without entity type");
            return "*";
        }

        var options = ParseOptions(context);
        var properties = GetFilteredProperties(context.EntityType, options, context.Type == "auto" ? "Id" : null);

        var regexPattern = options.Get("regex");
        if (!string.IsNullOrEmpty(regexPattern))
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                properties = properties.Where(p => regex.IsMatch(p.Name)).ToList();
            }
            catch { context.Result.Warnings.Add($"Invalid regex pattern: {regexPattern}"); }
        }

        var shouldQuote = context.Type != "raw";
        var sb = new StringBuilder();
        context.Result.ColumnOrder.Clear();

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var columnName = ToSnakeCase(properties[i].Name);
            sb.Append(shouldQuote ? context.Dialect.WrapColumn(columnName) : columnName);
            context.Result.ColumnOrder.Add(columnName);
        }
        return sb.ToString();
    }
}

/// <summary>{{values}} - Parameter values placeholder.</summary>
public sealed class ValuesHandler : PlaceholderHandlerBase
{
    public override string Name => "values";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null && SharedCodeGenerationUtilities.IsEnumerableParameter(param))
                return $"({context.Dialect.ParameterPrefix}{paramName})";
        }

        if (options.FirstArg.StartsWith("@") && context.Method != null)
        {
            var batchParamName = options.FirstArg.Substring(1);
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == batchParamName);
            if (param != null && SharedCodeGenerationUtilities.IsEnumerableParameter(param))
                return $"__RUNTIME_BATCH_VALUES_{batchParamName}__";
        }

        if (context.EntityType == null)
        {
            if (context.Method == null) return string.Empty;
            return string.Join(", ", context.Method.Parameters
                .Where(p => p.Type.Name != "CancellationToken")
                .Select(p => $"{context.Dialect.ParameterPrefix}{p.Name}"));
        }

        var properties = GetFilteredProperties(context.EntityType, options, context.Type == "auto" ? "Id" : null);
        return string.Join(", ", properties.Select(p => $"{context.Dialect.ParameterPrefix}{ToSnakeCase(p.Name)}"));
    }
}

/// <summary>{{set}} - UPDATE SET clause placeholder.</summary>
public sealed class SetHandler : PlaceholderHandlerBase
{
    public override string Name => "set";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);

        // Support {{set --from paramName}} for ExpressionToSql.ToSetClause()
        var fromParam = options.Get("from");
        if (!string.IsNullOrEmpty(fromParam) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == fromParam);
            if (param != null && IsExpressionToSqlParameter(param))
                return $"{{{{RUNTIME_SET_EXPR_{fromParam}}}}}";
        }

        if (context.Type?.StartsWith("@") == true && context.Method != null)
        {
            var paramName = context.Type.Substring(1);
            if (context.Method.Parameters.Any(p => p.Name == paramName))
                return $"{{RUNTIME_SET_{paramName}}}";
        }

        if (context.EntityType == null)
        {
            if (context.Method == null) return string.Empty;
            return string.Join(", ", context.Method.Parameters
                .Where(p => p.Type.Name != "CancellationToken" && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{context.Dialect.WrapColumn(ToSnakeCase(p.Name))} = {context.Dialect.ParameterPrefix}{p.Name}"));
        }

        var properties = GetFilteredProperties(context.EntityType, options, "Id", requireSetter: true);
        return string.Join(", ", properties.Select(p =>
        {
            var columnName = ToSnakeCase(p.Name);
            return $"{context.Dialect.WrapColumn(columnName)} = {context.Dialect.ParameterPrefix}{columnName}";
        }));
    }

    private static bool IsExpressionToSqlParameter(IParameterSymbol param)
    {
        var typeName = param.Type.ToDisplayString();
        return typeName.Contains("ExpressionToSql<") || typeName.Contains("ExpressionToSqlBase");
    }
}


/// <summary>{{where}} - WHERE clause placeholder.</summary>
public sealed class WhereHandler : PlaceholderHandlerBase
{
    public override string Name => "where";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
                return IsExpressionParameter(param) ? $"{{{{RUNTIME_WHERE_NATIVE_EXPR_{paramName}}}}}" : $"{{{{RUNTIME_WHERE_EXPR_{paramName}}}}}";
        }

        var paramSource = context.Type?.StartsWith("@") == true ? context.Type :
                         options.FirstArg.StartsWith("@") ? options.FirstArg : null;

        if (paramSource != null && context.Method != null)
        {
            var pName = paramSource.Substring(1).Trim();
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == pName);
            if (param != null)
                return IsExpressionParameter(param) ? $"{{{{RUNTIME_WHERE_NATIVE_EXPR_{pName}}}}}" : $"{{{{RUNTIME_WHERE_{pName}}}}}";
        }

        if (context.Method != null && string.IsNullOrEmpty(paramName) && paramSource == null)
        {
            var expressionParam = context.Method.Parameters.FirstOrDefault(p => HasExpressionToSqlAttribute(p) || IsExpressionParameter(p));
            if (expressionParam != null)
                return $"{{{{RUNTIME_WHERE_EXPR_{expressionParam.Name}}}}}";
        }

        return context.Type switch
        {
            "id" => $"{context.Dialect.WrapColumn("id")} = {context.Dialect.ParameterPrefix}id",
            "auto" => GenerateAutoWhere(context),
            _ => "1=1"
        };
    }

    private static bool IsExpressionParameter(IParameterSymbol param)
    {
        var typeName = param.Type.ToDisplayString();
        return typeName.Contains("Expression<Func<") && typeName.Contains(", bool>>");
    }

    private static bool HasExpressionToSqlAttribute(IParameterSymbol param) =>
        param.GetAttributes().Any(a => a.AttributeClass?.Name is "ExpressionToSqlAttribute" or "ExpressionToSql");

    private string GenerateAutoWhere(PlaceholderContext context)
    {
        if (context.Method?.Parameters.Any(p => p.Type.Name != "CancellationToken") != true)
            return "1=1";
        return string.Join(" AND ", context.Method.Parameters
            .Where(p => p.Type.Name != "CancellationToken")
            .Select(p => $"{context.Dialect.WrapColumn(ToSnakeCase(p.Name))} = {context.Dialect.ParameterPrefix}{p.Name}"));
    }
}

/// <summary>{{limit}} - Pagination LIMIT placeholder.</summary>
public sealed class LimitHandler : PlaceholderHandlerBase
{
    public override string Name => "limit";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var preset = GetPresetLimit(context.Type);
        if (preset.HasValue) return FormatLimit(context.Dialect, preset.Value);

        var paramName = options.Get("param");
        if (string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var limitParam = context.Method.Parameters.FirstOrDefault(p => p.Name.Equals("limit", StringComparison.OrdinalIgnoreCase));
            if (limitParam != null) paramName = limitParam.Name;
        }

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            if (param != null)
            {
                if (IsNullableType(param.Type)) return $"{{RUNTIME_NULLABLE_LIMIT_{paramName}}}";
                return FormatLimitWithParam(context.Dialect, paramName);
            }
        }

        var count = options.Get("count", options.Get("limit", options.FirstArg));
        if (string.IsNullOrEmpty(count)) count = "20";
        return FormatLimit(context.Dialect, int.TryParse(count, out var n) ? n : 20);
    }

    private static int? GetPresetLimit(string? type) => type?.ToLowerInvariant() switch
    {
        "tiny" => 5, "small" => 10, "medium" => 50, "large" => 100, "page" or "default" => 20, _ => null
    };

    private static string FormatLimit(SqlDefine dialect, int count)
    {
        if (dialect.Equals(SqlDefine.SqlServer)) return $"OFFSET 0 ROWS FETCH NEXT {count} ROWS ONLY";
        if (dialect.Equals(SqlDefine.Oracle)) return $"FETCH FIRST {count} ROWS ONLY";
        return $"LIMIT {count}";
    }

    private static string FormatLimitWithParam(SqlDefine dialect, string paramName)
    {
        var prefix = dialect.ParameterPrefix;
        if (dialect.Equals(SqlDefine.SqlServer)) return $"{{RUNTIME_LIMIT_{paramName}}}";
        if (dialect.Equals(SqlDefine.Oracle)) return $"FETCH FIRST {prefix}{paramName} ROWS ONLY";
        return $"LIMIT {prefix}{paramName}";
    }
}

/// <summary>{{offset}} - Pagination OFFSET placeholder.</summary>
public sealed class OffsetHandler : PlaceholderHandlerBase
{
    public override string Name => "offset";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        if (string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var offsetParam = context.Method.Parameters.FirstOrDefault(p => p.Name.Equals("offset", StringComparison.OrdinalIgnoreCase));
            if (offsetParam != null) paramName = offsetParam.Name;
        }

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            if (param != null)
            {
                if (IsNullableType(param.Type)) return $"{{RUNTIME_NULLABLE_OFFSET_{paramName}}}";
                return FormatOffsetWithParam(context.Dialect, paramName);
            }
        }

        var offset = options.Get("offset", options.Get("skip", options.FirstArg));
        if (string.IsNullOrEmpty(offset)) offset = "0";
        var isSqlServerOrOracle = context.Dialect.Equals(SqlDefine.SqlServer) || context.Dialect.Equals(SqlDefine.Oracle);
        return isSqlServerOrOracle ? $"OFFSET {offset} ROWS" : $"OFFSET {offset}";
    }

    private static string FormatOffsetWithParam(SqlDefine dialect, string paramName)
    {
        var prefix = dialect.ParameterPrefix;
        return (dialect.Equals(SqlDefine.SqlServer) || dialect.Equals(SqlDefine.Oracle))
            ? $"OFFSET {prefix}{paramName} ROWS"
            : $"OFFSET {prefix}{paramName}";
    }
}

/// <summary>{{wrap column}} - Identifier wrapper placeholder.</summary>
public sealed class WrapHandler : PlaceholderHandlerBase
{
    public override string Name => "wrap";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var column = !string.IsNullOrEmpty(context.Type) ? context.Type :
                    !string.IsNullOrEmpty(options.FirstArg) ? options.FirstArg : "";
        if (string.IsNullOrEmpty(column))
        {
            context.Result.Warnings.Add("{{wrap}} requires a column name");
            return "";
        }
        return context.Dialect.WrapColumn(ToSnakeCase(column));
    }
}

/// <summary>{{orderby column}} - ORDER BY clause placeholder.</summary>
public sealed class OrderByHandler : PlaceholderHandlerBase
{
    public override string Name => "orderby";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null) return $"{{RUNTIME_ORDERBY_{paramName}}}";
        }

        var column = !string.IsNullOrEmpty(context.Type) ? context.Type : options.FirstArg;
        if (string.IsNullOrEmpty(column))
        {
            context.Result.Warnings.Add("{{orderby}} requires a column name or --param option");
            return "";
        }

        var direction = options.Has("desc") ? " DESC" : (options.Has("asc") ? " ASC" : "");
        var columns = column.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c))
            .Select(c => context.Dialect.WrapColumn(ToSnakeCase(c)));
        return $"ORDER BY {string.Join(", ", columns)}{direction}";
    }
}

#endregion

#region Dialect Handlers

/// <summary>{{bool_true}} - Boolean true literal placeholder.</summary>
public sealed class BoolTrueHandler : IPlaceholderHandler
{
    public string Name => "bool_true";
    public string Process(PlaceholderContext context) => context.Dialect.Equals(SqlDefine.PostgreSql) ? "true" : "1";
}

/// <summary>{{bool_false}} - Boolean false literal placeholder.</summary>
public sealed class BoolFalseHandler : IPlaceholderHandler
{
    public string Name => "bool_false";
    public string Process(PlaceholderContext context) => context.Dialect.Equals(SqlDefine.PostgreSql) ? "false" : "0";
}

/// <summary>{{current_timestamp}} - Current timestamp placeholder.</summary>
public sealed class CurrentTimestampHandler : IPlaceholderHandler
{
    public string Name => "current_timestamp";
    public string Process(PlaceholderContext context)
    {
        if (context.Dialect.Equals(SqlDefine.SqlServer)) return "GETDATE()";
        if (context.Dialect.Equals(SqlDefine.Oracle)) return "SYSDATE";
        return "CURRENT_TIMESTAMP";
    }
}

/// <summary>{{random}} - Random function placeholder.</summary>
public sealed class RandomHandler : IPlaceholderHandler
{
    public string Name => "random";
    public string Process(PlaceholderContext context) =>
        (context.Dialect.Equals(SqlDefine.SqlServer) || context.Dialect.Equals(SqlDefine.MySql)) ? "RAND()" : "RANDOM()";
}

/// <summary>{{coalesce}} - COALESCE function placeholder.</summary>
public sealed class CoalesceHandler : PlaceholderHandlerBase
{
    public override string Name => "coalesce";

    public override string Process(PlaceholderContext context)
    {
        var content = !string.IsNullOrEmpty(context.Type) ? context.Type : context.Options;
        var parts = content?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray() ?? Array.Empty<string>();
        if (parts.Length == 0)
        {
            context.Result.Warnings.Add("{{coalesce}} requires at least one column name");
            return "COALESCE(NULL, 0)";
        }
        var processedParts = parts.Select(p => WrapIfColumn(p, context.Dialect)).ToArray();
        return $"COALESCE({string.Join(", ", processedParts)})";
    }
}

/// <summary>{{ifnull}} - IFNULL/ISNULL function placeholder.</summary>
public sealed class IfNullHandler : PlaceholderHandlerBase
{
    public override string Name => "ifnull";

    public override string Process(PlaceholderContext context)
    {
        var content = !string.IsNullOrEmpty(context.Type) ? context.Type : context.Options;
        var parts = content?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray() ?? Array.Empty<string>();
        if (parts.Length < 2)
        {
            context.Result.Warnings.Add("{{ifnull}} requires column and default value");
            return "IFNULL(NULL, 0)";
        }

        var column = WrapIfColumn(parts[0], context.Dialect);
        var defaultValue = parts[1];

        if (context.Dialect.Equals(SqlDefine.SqlServer)) return $"ISNULL({column}, {defaultValue})";
        if (context.Dialect.Equals(SqlDefine.Oracle)) return $"NVL({column}, {defaultValue})";
        if (context.Dialect.Equals(SqlDefine.PostgreSql)) return $"COALESCE({column}, {defaultValue})";
        return $"IFNULL({column}, {defaultValue})";
    }
}

/// <summary>Aggregate function placeholder - supports COUNT, SUM, AVG, MAX, MIN.</summary>
public sealed class AggregateHandler : PlaceholderHandlerBase
{
    private readonly string _function;
    public AggregateHandler(string function) => _function = function.ToUpperInvariant();
    public override string Name => _function.ToLowerInvariant();

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var column = options.Get("column", "");
        if (string.IsNullOrEmpty(column))
            column = !string.IsNullOrEmpty(context.Type) ? context.Type : options.FirstArg;

        if (string.IsNullOrEmpty(column) || column == "*" || column == "all")
            column = "*";
        else if (column != "*" && !column.Contains("(") && !column.Contains("."))
            column = ToSnakeCase(column);

        var distinct = options.Has("distinct") || column == "distinct";
        if (column == "distinct") column = "*";

        var useCoalesce = options.Has("coalesce") || !string.IsNullOrEmpty(options.Get("default"));
        var defaultValue = options.Get("default", "0");
        var result = distinct ? $"{_function}(DISTINCT {column})" : $"{_function}({column})";
        return useCoalesce ? $"COALESCE({result}, {defaultValue})" : result;
    }
}

/// <summary>{{batch_values}} - Batch INSERT values placeholder.</summary>
public sealed class BatchValuesHandler : PlaceholderHandlerBase
{
    public override string Name => "batch_values";

    public override string Process(PlaceholderContext context)
    {
        if (context.Method != null)
        {
            var batchParam = context.Method.Parameters.FirstOrDefault(p => SharedCodeGenerationUtilities.IsEnumerableParameter(p));
            if (batchParam != null) return $"__RUNTIME_BATCH_VALUES_{batchParam.Name}__";
        }
        context.Result.Warnings.Add("{{batch_values}} requires an IEnumerable parameter");
        return "{{BATCH_VALUES_PLACEHOLDER}}";
    }
}

#endregion
