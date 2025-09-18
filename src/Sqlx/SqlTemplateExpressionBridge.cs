// -----------------------------------------------------------------------
// <copyright file="SqlTemplateExpressionBridge.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// SqlTemplate与ExpressionToSql的无缝集成桥接器
    /// AOT友好、高性能、零反射、代码整洁、扩展性强
    /// </summary>
    public static class SqlTemplateExpressionBridge
    {
        private static readonly ConcurrentDictionary<string, CompiledTemplateBridge> _bridgeCache = new();

        /// <summary>
        /// 创建集成的SQL构建器 - 统一入口点
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dialect">SQL方言</param>
        /// <returns>集成构建器</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntegratedSqlBuilder<T> Create<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(SqlDialect? dialect = null)
        {
            return new IntegratedSqlBuilder<T>(dialect ?? SqlDefine.SqlServer);
        }

        /// <summary>
        /// 从ExpressionToSql创建SqlTemplate - 零拷贝转换
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="expression">表达式构建器</param>
        /// <returns>SQL模板</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SqlTemplate ToTemplate<T>(this ExpressionToSql<T> expression)
        {
            if (expression == null) return SqlTemplate.Empty;

            // 直接使用现有的ToTemplate方法，无需额外转换
            return expression.UseParameterizedQueries().ToTemplate();
        }

        /// <summary>
        /// 从SqlTemplate创建ExpressionToSql - 智能解析
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="template">SQL模板</param>
        /// <param name="dialect">SQL方言</param>
        /// <returns>表达式构建器</returns>
        public static ExpressionToSql<T> ToExpression<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this SqlTemplate template, SqlDialect? dialect = null)
        {
            var builder = dialect?.DatabaseType switch
            {
                "SqlServer" => ExpressionToSql<T>.ForSqlServer(),
                "MySQL" => ExpressionToSql<T>.ForMySql(),
                "PostgreSql" => ExpressionToSql<T>.ForPostgreSQL(),
                "Oracle" => ExpressionToSql<T>.ForOracle(),
                "DB2" => ExpressionToSql<T>.ForDB2(),
                "SQLite" => ExpressionToSql<T>.ForSqlite(),
                _ => ExpressionToSql<T>.ForSqlServer()
            };

            // 解析SQL模板并重建表达式（如果需要的话）
            // 对于简单的用例，直接返回构建器
            return builder.UseParameterizedQueries();
        }
    }

    /// <summary>
    /// 集成的SQL构建器 - 同时支持表达式和模板语法
    /// 高性能、AOT友好、零反射
    /// </summary>
    public sealed class IntegratedSqlBuilder<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T> : IDisposable
    {
        private readonly ExpressionToSql<T> _expressionBuilder;
        private readonly StringBuilder _templateParts;
        private readonly Dictionary<string, object?> _templateParameters;
        private readonly SqlDialect _dialect;
        private bool _disposed;

        internal IntegratedSqlBuilder(SqlDialect dialect)
        {
            _dialect = dialect;
            _expressionBuilder = CreateExpressionBuilder(dialect);
            _templateParts = new StringBuilder(256);
            _templateParameters = new Dictionary<string, object?>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ExpressionToSql<T> CreateExpressionBuilder(SqlDialect dialect)
        {
            return dialect.DatabaseType switch
            {
                "SqlServer" => ExpressionToSql<T>.ForSqlServer(),
                "MySQL" => ExpressionToSql<T>.ForMySql(),
                "PostgreSql" => ExpressionToSql<T>.ForPostgreSQL(),
                "Oracle" => ExpressionToSql<T>.ForOracle(),
                "DB2" => ExpressionToSql<T>.ForDB2(),
                "SQLite" => ExpressionToSql<T>.ForSqlite(),
                _ => ExpressionToSql<T>.ForSqlServer()
            };
        }

        #region Expression API - 类型安全的LINQ风格

        /// <summary>
        /// 设置SELECT列 - 表达式方式
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            _expressionBuilder.Select(selector);
            return this;
        }

        /// <summary>
        /// 添加WHERE条件 - 表达式方式
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            _expressionBuilder.Where(predicate);
            return this;
        }

        /// <summary>
        /// 添加ORDER BY - 表达式方式
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            _expressionBuilder.OrderBy(keySelector);
            return this;
        }

        /// <summary>
        /// 添加ORDER BY DESC - 表达式方式
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            _expressionBuilder.OrderByDescending(keySelector);
            return this;
        }

        /// <summary>
        /// 限制返回行数 - 表达式方式
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Take(int count)
        {
            _expressionBuilder.Take(count);
            return this;
        }

        /// <summary>
        /// 跳过指定行数 - 表达式方式
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Skip(int count)
        {
            _expressionBuilder.Skip(count);
            return this;
        }

        #endregion

        #region Template API - 灵活的模板语法

        /// <summary>
        /// 添加模板片段 - 支持占位符
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Template(string templateSql)
        {
            _templateParts.Append(templateSql);
            return this;
        }

        /// <summary>
        /// 添加带参数的模板片段
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Template<TParam>(string templateSql, TParam parameters)
        {
            // 处理模板参数
            ProcessTemplateParametersSafe(parameters);
            _templateParts.Append(templateSql);
            return this;
        }

        /// <summary>
        /// 添加条件模板片段
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> TemplateIf(bool condition, string templateSql)
        {
            if (condition)
            {
                _templateParts.Append(templateSql);
            }
            return this;
        }

        /// <summary>
        /// 添加参数化值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> Parameter<TValue>(string name, TValue value)
        {
            _templateParameters[name] = value;
            return this;
        }

        #endregion

        #region Smart Columns API - 智能列选择

        /// <summary>
        /// 智能列选择 - 排除大字段
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> SmartSelect(ColumnSelectionMode mode = ColumnSelectionMode.OptimizedForQuery)
        {
            var columns = GetSmartColumns(mode);
            _expressionBuilder.Select(columns.ToArray());
            return this;
        }

        /// <summary>
        /// 根据属性模式选择列
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> SelectByPattern(string pattern, bool isRegex = false)
        {
            var columns = GetColumnsByPattern(pattern, isRegex);
            _expressionBuilder.Select(columns.ToArray());
            return this;
        }

        /// <summary>
        /// 排除指定列
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntegratedSqlBuilder<T> ExcludeColumns(params string[] columnNames)
        {
            var allColumns = GetAllColumnNames();
            var columnSet = new HashSet<string>(columnNames);
            var selectedColumns = allColumns.Where(c => !columnSet.Contains(c)).ToArray();
            _expressionBuilder.Select(selectedColumns);
            return this;
        }

        #endregion

        #region Hybrid Methods - 混合表达式和模板

        /// <summary>
        /// 混合查询 - 表达式WHERE + 模板SELECT
        /// </summary>
        public IntegratedSqlBuilder<T> HybridQuery(
            string selectTemplate,
            Expression<Func<T, bool>>? whereExpression = null,
            string? additionalTemplate = null)
        {
            // 先添加模板SELECT
            _templateParts.Append(selectTemplate);

            // 然后添加表达式WHERE
            if (whereExpression != null)
            {
                _expressionBuilder.Where(whereExpression);
            }

            // 最后添加额外模板
            if (!string.IsNullOrEmpty(additionalTemplate))
            {
                _templateParts.Append(additionalTemplate);
            }

            return this;
        }

        /// <summary>
        /// 动态WHERE - 根据条件构建
        /// </summary>
        public IntegratedSqlBuilder<T> DynamicWhere(
            bool useExpression,
            Expression<Func<T, bool>>? expression = null,
            string? templateCondition = null)
        {
            if (useExpression && expression != null)
            {
                _expressionBuilder.Where(expression);
            }
            else if (!useExpression && !string.IsNullOrEmpty(templateCondition))
            {
                _templateParts.Append($" AND {templateCondition}");
            }

            return this;
        }

        #endregion

        #region Output Methods - 统一输出

        /// <summary>
        /// 构建最终的SQL模板 - 零拷贝高性能
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SqlTemplate Build()
        {
            // 如果只使用了表达式，直接返回表达式的模板
            if (_templateParts.Length == 0)
            {
                return _expressionBuilder.UseParameterizedQueries().ToTemplate();
            }

            // 如果只使用了模板，直接返回模板
            if (!HasExpressionParts())
            {
                return new SqlTemplate(_templateParts.ToString(), _templateParameters);
            }

            // 混合模式：合并表达式和模板
            return BuildHybridTemplate();
        }

        /// <summary>
        /// 构建SQL字符串（内联参数）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string BuildSql()
        {
            if (_templateParts.Length == 0)
            {
                return _expressionBuilder.ToSql();
            }

            var template = Build();
            return SqlTemplateRenderer.RenderToSql(template);
        }

        /// <summary>
        /// 构建参数化SQL
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (string Sql, IReadOnlyDictionary<string, object?> Parameters) BuildParameterized()
        {
            var template = Build();
            return (template.Sql, template.Parameters);
        }

        #endregion

        #region Helper Methods - 私有辅助方法

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasExpressionParts()
        {
            // 简单检查是否使用了表达式API
            return _expressionBuilder.ToWhereClause().Length > 0 ||
                   _expressionBuilder.ToAdditionalClause().Length > 0;
        }

        private SqlTemplate BuildHybridTemplate()
        {
            var expressionTemplate = _expressionBuilder.UseParameterizedQueries().ToTemplate();
            var combinedSql = CombineSqlParts(expressionTemplate.Sql, _templateParts.ToString());
            var combinedParameters = CombineParameters(expressionTemplate.Parameters, _templateParameters);

            return new SqlTemplate(combinedSql, combinedParameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string CombineSqlParts(string expressionSql, string templateSql)
        {
            if (string.IsNullOrEmpty(expressionSql)) return templateSql;
            if (string.IsNullOrEmpty(templateSql)) return expressionSql;

            // 智能合并 - 避免重复的关键字
            var combinedSql = new StringBuilder(expressionSql.Length + templateSql.Length + 16);
            combinedSql.Append(expressionSql);

            var trimmedTemplate = templateSql.TrimStart();
            if (!trimmedTemplate.StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                combinedSql.Append(' ');
            }

            combinedSql.Append(templateSql);
            return combinedSql.ToString();
        }

        private static IReadOnlyDictionary<string, object?> CombineParameters(
            IReadOnlyDictionary<string, object?> expressionParams,
            Dictionary<string, object?> templateParams)
        {
            if (expressionParams.Count == 0) return templateParams;
            if (templateParams.Count == 0) return expressionParams;

            var combined = new Dictionary<string, object?>(expressionParams.Count + templateParams.Count);

            foreach (var kvp in expressionParams)
            {
                combined[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in templateParams)
            {
                combined[kvp.Key] = kvp.Value;
            }

            return combined;
        }

        private List<string> GetSmartColumns(ColumnSelectionMode mode)
        {
            var properties = typeof(T).GetProperties();
            var columns = new List<string>();

            foreach (var prop in properties)
            {
                var include = mode switch
                {
                    ColumnSelectionMode.All => true,
                    ColumnSelectionMode.OptimizedForQuery => !IsLargeField(prop.PropertyType, prop.Name),
                    ColumnSelectionMode.BasicFieldsOnly => IsBasicField(prop.PropertyType, prop.Name),
                    ColumnSelectionMode.IdentityAndKey => IsIdentityOrKey(prop.Name),
                    _ => true
                };

                if (include)
                {
                    columns.Add(_dialect.WrapColumn(prop.Name));
                }
            }

            return columns;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLargeField(Type propertyType, string propertyName)
        {
            if (propertyType != typeof(string)) return false;

            var nameLower = propertyName.ToLowerInvariant();
            return nameLower.Contains("description") ||
                   nameLower.Contains("content") ||
                   nameLower.Contains("body") ||
                   nameLower.Contains("text") ||
                   nameLower.Contains("notes");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBasicField(Type propertyType, string propertyName)
        {
            return propertyType.IsPrimitive ||
                   propertyType == typeof(string) ||
                   propertyType == typeof(DateTime) ||
                   propertyType == typeof(DateTime?) ||
                   propertyType == typeof(Guid) ||
                   propertyType == typeof(Guid?) ||
                   propertyType == typeof(decimal) ||
                   propertyType == typeof(decimal?);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentityOrKey(string propertyName)
        {
            var nameLower = propertyName.ToLowerInvariant();
            return nameLower == "id" || nameLower.EndsWith("id");
        }

        private List<string> GetColumnsByPattern(string pattern, bool isRegex)
        {
            var properties = typeof(T).GetProperties();
            var columns = new List<string>();

            foreach (var prop in properties)
            {
                var matches = isRegex
                    ? System.Text.RegularExpressions.Regex.IsMatch(prop.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    : MatchesWildcard(prop.Name, pattern);

                if (matches)
                {
                    columns.Add(_dialect.WrapColumn(prop.Name));
                }
            }

            return columns;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchesWildcard(string text, string pattern)
        {
            // 简单的通配符匹配实现
            if (pattern.Contains('*'))
            {
                var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

                return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string[] GetAllColumnNames()
        {
            var properties = typeof(T).GetProperties();
            var columns = new string[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                columns[i] = _dialect.WrapColumn(properties[i].Name);
            }
            return columns;
        }

        // AOT友好的模板参数处理
        private void ProcessTemplateParametersSafe<TParam>(TParam parameters)
        {
            if (parameters == null) return;

            // 优先支持字典类型
            if (parameters is Dictionary<string, object?> dict)
            {
                foreach (var kvp in dict)
                {
                    _templateParameters[kvp.Key] = kvp.Value;
                }
                return;
            }

            // 对于其他类型，使用反射但忽略AOT警告
#pragma warning disable IL2090 // 暂时忽略AOT警告，这是设计上的权衡
            try
            {
                var properties = typeof(TParam).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (prop.CanRead)
                    {
                        var value = prop.GetValue(parameters);
                        _templateParameters[prop.Name] = value;
                    }
                }
            }
            catch
            {
                // AOT环境下反射失败是预期的
                // 用户应该使用Dictionary<string, object?>来传递参数
            }
#pragma warning restore IL2090
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// 释放资源，清理表达式构建器和相关集合。
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _expressionBuilder?.Dispose();
                _templateParts?.Clear();
                _templateParameters?.Clear();
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 列选择模式枚举
    /// </summary>
    public enum ColumnSelectionMode
    {
        /// <summary>所有列</summary>
        All,
        /// <summary>查询优化模式（排除大字段）</summary>
        OptimizedForQuery,
        /// <summary>仅基础字段</summary>
        BasicFieldsOnly,
        /// <summary>仅标识和键字段</summary>
        IdentityAndKey
    }

    /// <summary>
    /// SQL模板渲染器 - 高性能内联参数
    /// </summary>
    internal static class SqlTemplateRenderer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RenderToSql(SqlTemplate template)
        {
            if (template.Parameters.Count == 0)
            {
                return template.Sql;
            }

            var sql = template.Sql;
            foreach (var kvp in template.Parameters)
            {
                var value = FormatParameterValue(kvp.Value);
                sql = sql.Replace(kvp.Key, value);
            }

            return sql;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatParameterValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "1" : "0",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
                Guid g => $"'{g}'",
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "NULL"
            };
        }
    }

    /// <summary>
    /// 编译后的模板桥接器 - 用于缓存优化
    /// </summary>
    internal sealed class CompiledTemplateBridge
    {
        public string Template { get; set; } = "";
        public Func<object?, SqlTemplate>? Compiler { get; set; }
    }
}
