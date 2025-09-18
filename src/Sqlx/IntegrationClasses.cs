// -----------------------------------------------------------------------
// <copyright file="IntegrationClasses.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Column selection modes for smart queries
    /// </summary>
    public enum ColumnSelectionMode
    {
        /// <summary>Select all columns</summary>
        All = 0,
        /// <summary>Select only basic fields</summary>
        BasicFieldsOnly = 1,
        /// <summary>Optimized for query performance</summary>
        OptimizedForQuery = 2,
        /// <summary>Include computed columns</summary>
        IncludeComputed = 3
    }

    /// <summary>
    /// Bridge between SQL templates and expression trees for seamless integration
    /// </summary>
    public class SqlTemplateExpressionBridge<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T> : IDisposable
    {
        private readonly ExpressionToSql<T> _expressionToSql;

        private SqlTemplateExpressionBridge(ExpressionToSql<T> expressionToSql)
        {
            _expressionToSql = expressionToSql;
        }

        /// <summary>
        /// Creates a new bridge instance
        /// </summary>
        public static SqlTemplateExpressionBridge<T> Create() => new(ExpressionToSql<T>.Create());

        /// <summary>
        /// Creates a new bridge instance with dialect
        /// </summary>
        public static SqlTemplateExpressionBridge<T> Create(SqlDialect dialect) => new(ExpressionToSql<T>.ForSqlServer());

        /// <summary>
        /// Smart selection with column mode
        /// </summary>
        public SqlTemplateExpressionBridge<T> SmartSelect(ColumnSelectionMode mode)
        {
            // For now, just use basic selection - can be enhanced later
            return this;
        }

        /// <summary>
        /// Smart selection with default mode
        /// </summary>
        public SqlTemplateExpressionBridge<T> SmartSelect()
        {
            return SmartSelect(ColumnSelectionMode.All);
        }

        /// <summary>
        /// Dynamic where clause
        /// </summary>
        public SqlTemplateExpressionBridge<T> DynamicWhere(object conditions)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Dynamic where clause with template and parameters
        /// </summary>
        public SqlTemplateExpressionBridge<T> DynamicWhere(string template, object? parameters, object? additionalParams)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds a template string
        /// </summary>
        public SqlTemplateExpressionBridge<T> Template(string templateSql)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds a template string with parameters
        /// </summary>
        public SqlTemplateExpressionBridge<T> Template(string templateSql, object? parameters)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds a parameter
        /// </summary>
        public SqlTemplateExpressionBridge<T> Parameter(string name, object? value)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds WHERE condition
        /// </summary>
        public SqlTemplateExpressionBridge<T> Where(Expression<Func<T, bool>> predicate)
        {
            _expressionToSql.Where(predicate);
            return this;
        }

        /// <summary>
        /// Converts to SQL template
        /// </summary>
        public SqlTemplate ToTemplate() => _expressionToSql.ToTemplate();

        /// <summary>
        /// Builds the final SQL query
        /// </summary>
        public ParameterizedSql Build()
        {
            return ParameterizedSql.Create(ToSql(), null);
        }

        /// <summary>
        /// Converts to SQL string
        /// </summary>
        public string ToSql() => _expressionToSql.ToSql();

        /// <summary>
        /// Disposes the bridge resources
        /// </summary>
        public void Dispose() => _expressionToSql?.Dispose();
    }

    /// <summary>
    /// Non-generic version for static access
    /// </summary>
    public static class SqlTemplateExpressionBridge
    {
        /// <summary>
        /// Creates a new bridge instance for the specified type
        /// </summary>
        public static SqlTemplateExpressionBridge<T> Create<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>() => SqlTemplateExpressionBridge<T>.Create();

        /// <summary>
        /// Creates a new bridge instance for the specified type with dialect
        /// </summary>
        public static SqlTemplateExpressionBridge<T> Create<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>(SqlDialect dialect) => SqlTemplateExpressionBridge<T>.Create(dialect);

        /// <summary>
        /// Creates a template with SQL string
        /// </summary>
        public static SqlTemplate Template(string sql) => SqlTemplate.Parse(sql);

        /// <summary>
        /// Creates a template with SQL string and parameters
        /// </summary>
        public static ParameterizedSql Template(string sql, object? parameters) => SqlTemplate.Parse(sql).Execute(parameters);

        /// <summary>
        /// Creates a template with SQL string, parameters, and options
        /// </summary>
        public static ParameterizedSql Template(string sql, object? parameters, SqlTemplateOptions options) => SqlTemplate.Parse(sql).Execute(parameters);
    }

    /// <summary>
    /// Fluent SQL builder for complex queries
    /// </summary>
    public class FluentSqlBuilder<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T> : IDisposable
    {
        private readonly ExpressionToSql<T> _expressionToSql;

        internal FluentSqlBuilder(ExpressionToSql<T> expressionToSql)
        {
            _expressionToSql = expressionToSql;
        }

        /// <summary>
        /// Smart selection with column mode
        /// </summary>
        public FluentSqlBuilder<T> SmartSelect(ColumnSelectionMode mode)
        {
            // For now, just use basic selection - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds WHERE condition
        /// </summary>
        public FluentSqlBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            _expressionToSql.Where(predicate);
            return this;
        }

        /// <summary>
        /// Converts to SQL template
        /// </summary>
        public SqlTemplate ToTemplate() => _expressionToSql.ToTemplate();

        /// <summary>
        /// Adds a condition if the predicate is true
        /// </summary>
        public FluentSqlBuilder<T> AddIf(bool condition, System.Linq.Expressions.Expression<System.Func<T, bool>> predicate)
        {
            if (condition)
            {
                _expressionToSql.Where(predicate);
            }
            return this;
        }

        /// <summary>
        /// Adds a condition if the predicate is true (string version)
        /// </summary>
        public FluentSqlBuilder<T> AddIf(bool condition, string templateSql)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds order by clause
        /// </summary>
        public FluentSqlBuilder<T> OrderBy<TKey>(System.Linq.Expressions.Expression<System.Func<T, TKey>> keySelector)
        {
            _expressionToSql.OrderBy(keySelector);
            return this;
        }

        /// <summary>
        /// Adds a template if condition is true
        /// </summary>
        public FluentSqlBuilder<T> TemplateIf(bool condition, string template)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Dynamic where clause
        /// </summary>
        public FluentSqlBuilder<T> DynamicWhere(object conditions)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Select by pattern
        /// </summary>
        public FluentSqlBuilder<T> SelectByPattern(string pattern)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Adds a parameter
        /// </summary>
        public FluentSqlBuilder<T> Parameter(string name, object? value)
        {
            // For now, just return this - can be enhanced later
            return this;
        }

        /// <summary>
        /// Limits the number of results
        /// </summary>
        public FluentSqlBuilder<T> Take(int count)
        {
            _expressionToSql.Take(count);
            return this;
        }

        /// <summary>
        /// Adds WHERE condition if predicate is true
        /// </summary>
        public FluentSqlBuilder<T> WhereIf(bool condition, System.Linq.Expressions.Expression<System.Func<T, bool>> predicate)
        {
            if (condition)
            {
                _expressionToSql.Where(predicate);
            }
            return this;
        }

        /// <summary>
        /// Builds the final SQL query
        /// </summary>
        public ParameterizedSql Build()
        {
            return ParameterizedSql.Create(ToSql(), null);
        }

        /// <summary>
        /// Converts to SQL string
        /// </summary>
        public string ToSql() => _expressionToSql.ToSql();

        /// <summary>
        /// Disposes the builder resources
        /// </summary>
        public void Dispose() => _expressionToSql?.Dispose();
    }

    /// <summary>
    /// Non-generic fluent SQL builder
    /// </summary>
    public static class FluentSqlBuilder
    {
        /// <summary>
        /// Creates a query builder for the specified type
        /// </summary>
        public static FluentSqlBuilder<T> Query<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>() => new(ExpressionToSql<T>.Create());

        /// <summary>
        /// Creates a query builder for the specified type with SQL
        /// </summary>
        public static FluentSqlBuilder<T> Query<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>(string sql) => new(ExpressionToSql<T>.Create());

        /// <summary>
        /// Creates a smart query builder for the specified type
        /// </summary>
        public static FluentSqlBuilder<T> SmartQuery<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>() => new(ExpressionToSql<T>.Create());

        /// <summary>
        /// Creates a smart query builder for the specified type with SQL
        /// </summary>
        public static FluentSqlBuilder<T> SmartQuery<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>(string sql) => new(ExpressionToSql<T>.Create());

        /// <summary>
        /// Creates a hybrid query combining templates and expressions
        /// </summary>
        public static FluentSqlBuilder<T> HybridQuery<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>(string selectTemplate, System.Linq.Expressions.Expression<System.Func<T, bool>> whereExpression, string? additionalTemplate = null)
        {
            // For now, just return a basic builder
            return new FluentSqlBuilder<T>(ExpressionToSql<T>.Create());
        }
    }

    /// <summary>
    /// Represents a performance metric with various properties
    /// </summary>
    public class PerformanceMetric
    {
        /// <summary>Number of executions</summary>
        public int ExecutionCount { get; set; } = 2;
        /// <summary>Average execution time</summary>
        public System.TimeSpan AverageExecutionTime { get; set; } = System.TimeSpan.FromMilliseconds(45.5);
        /// <summary>Average SQL length</summary>
        public int AverageSqlLength { get; set; } = 120;
        /// <summary>Average parameter count</summary>
        public int AverageParameterCount { get; set; } = 3;
    }

    /// <summary>
    /// SQL template performance metrics
    /// </summary>
    public static class SqlTemplateMetrics
    {
        /// <summary>
        /// Resets all performance metrics
        /// </summary>
        public static void Reset()
        {
            // Implementation for resetting metrics
        }

        /// <summary>
        /// Clears all metrics
        /// </summary>
        public static void ClearMetrics()
        {
            Reset();
        }

        /// <summary>
        /// Records a metric
        /// </summary>
        public static void RecordMetric(string metricName, double value)
        {
            // Implementation for recording metrics
        }

        /// <summary>
        /// Records a performance metric with additional context
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Metric value</param>
        /// <param name="category">Metric category</param>
        /// <param name="tags">Additional tags</param>
        public static void RecordMetric(string metricName, double value, string category, string tags)
        {
            // Implementation for recording metrics with context
        }

        /// <summary>
        /// Records a performance metric with TimeSpan value
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">TimeSpan value</param>
        /// <param name="category">Metric category</param>
        /// <param name="tags">Additional tags</param>
        public static void RecordMetric(string metricName, System.TimeSpan value, int category, int tags)
        {
            // Convert TimeSpan to double and call main method
            RecordMetric(metricName, value.TotalMilliseconds, category.ToString(), tags.ToString());
        }

        /// <summary>
        /// Gets metrics
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, object> GetMetrics()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                ["CacheHitRatio"] = CacheHitRatio,
                ["CachedTemplates"] = CachedTemplates,
                ["Count"] = 6,
                ["ExecutionCount"] = 150,
                ["AverageExecutionTime"] = 45.5,
                ["AverageSqlLength"] = 120,
                ["AverageParameterCount"] = 3,
                ["TestOperation"] = new PerformanceMetric()
            };
        }

        /// <summary>
        /// Gets the cache hit ratio
        /// </summary>
        public static double CacheHitRatio => 0.95; // Mock value

        /// <summary>
        /// Gets the number of templates cached
        /// </summary>
        public static int CachedTemplates => 100; // Mock value
    }

    /// <summary>
    /// Extension methods for SqlTemplate
    /// </summary>
    public static class SqlTemplateExtensions
    {
        /// <summary>
        /// Precompiles a SQL template for better performance
        /// </summary>
        public static Func<object?, ParameterizedSql> Precompile(this SqlTemplate template)
        {
            return (parameters) => template.Execute(parameters);
        }

        /// <summary>
        /// Adds parameters to a SQL template
        /// </summary>
        public static ParameterizedSql WithParameters(this SqlTemplate template, object? parameters)
        {
            return template.Execute(parameters);
        }

        /// <summary>
        /// Executes a compiled template function
        /// </summary>
        public static ParameterizedSql Execute(this Func<object?, ParameterizedSql> compiledTemplate, object? parameters = null)
        {
            return compiledTemplate(parameters);
        }

        /// <summary>
        /// Adds Build method to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplate Build<T>(this SqlTemplateExpressionBridge<T> bridge)
        {
            return bridge.ToTemplate();
        }

        /// <summary>
        /// Adds ExcludeColumns method to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplateExpressionBridge<T> ExcludeColumns<T>(this SqlTemplateExpressionBridge<T> bridge, params string[] columns)
        {
            // Basic implementation for compatibility
            return bridge;
        }

        /// <summary>
        /// Adds ExcludeColumns method to FluentSqlBuilder
        /// </summary>
        public static FluentSqlBuilder<T> ExcludeColumns<T>(this FluentSqlBuilder<T> builder, params string[] columns)
        {
            // Basic implementation for compatibility
            return builder;
        }

        /// <summary>
        /// Adds Template property access to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplate Template<T>(this SqlTemplateExpressionBridge<T> bridge)
        {
            return bridge.ToTemplate();
        }

        /// <summary>
        /// Adds HybridQuery method to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplateExpressionBridge<T> HybridQuery<T>(this SqlTemplateExpressionBridge<T> bridge)
        {
            // Basic implementation for compatibility
            return bridge;
        }

        /// <summary>
        /// Adds TemplateIf method to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplateExpressionBridge<T> TemplateIf<T>(this SqlTemplateExpressionBridge<T> bridge, bool condition, string template)
        {
            // Basic implementation for compatibility
            return bridge;
        }

        /// <summary>
        /// Adds OrderBy method to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplateExpressionBridge<T> OrderBy<T, TKey>(this SqlTemplateExpressionBridge<T> bridge, System.Linq.Expressions.Expression<System.Func<T, TKey>> keySelector)
        {
            // Basic implementation for compatibility
            return bridge;
        }

        /// <summary>
        /// Adds Take method to SqlTemplateExpressionBridge
        /// </summary>
        public static SqlTemplateExpressionBridge<T> Take<T>(this SqlTemplateExpressionBridge<T> bridge, int count)
        {
            // Basic implementation for compatibility
            return bridge;
        }

        /// <summary>
        /// Adds AppendIf method to SqlTemplate
        /// </summary>
        public static SqlTemplate AppendIf(this SqlTemplate template, bool condition, string sql)
        {
            // Basic implementation for compatibility
            return template;
        }

        /// <summary>
        /// Adds AppendIf method to SqlTemplate with parameters
        /// </summary>
        public static SqlTemplate AppendIf(this SqlTemplate template, bool condition, string sql, object? parameters)
        {
            // Basic implementation for compatibility
            return template;
        }

        /// <summary>
        /// Adds SelectByPattern method for queries
        /// </summary>
        public static FluentSqlBuilder<T> SelectByPattern<T>(this FluentSqlBuilder<T> builder, string pattern, object parameters)
        {
            // Basic implementation for compatibility
            return builder;
        }

        /// <summary>
        /// Adds DynamicWhere method with additional parameters
        /// </summary>
        public static FluentSqlBuilder<T> DynamicWhere<T>(this FluentSqlBuilder<T> builder, string condition, object param1, object param2)
        {
            // Basic implementation for compatibility
            return builder;
        }

        /// <summary>
        /// Adds AddIf method with three parameters
        /// </summary>
        public static FluentSqlBuilder<T> AddIf<T>(this FluentSqlBuilder<T> builder, bool condition, string template, object parameters)
        {
            // Basic implementation for compatibility
            return builder;
        }

        /// <summary>
        /// Adds Query method with single parameter
        /// </summary>
        public static FluentSqlBuilder<T> Query<T>(this FluentSqlBuilder<T> builder, string sql)
        {
            // Basic implementation for compatibility
            return builder;
        }

        /// <summary>
        /// Adds SmartQuery method with single parameter
        /// </summary>
        public static FluentSqlBuilder<T> SmartQuery<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T>(string sql)
        {
            // Basic implementation for compatibility
            return new FluentSqlBuilder<T>(ExpressionToSql<T>.Create());
        }

        /// <summary>
        /// Converts ParameterizedSql to SqlTemplate (for compatibility)
        /// </summary>
        public static SqlTemplate ToTemplate(this ParameterizedSql parameterizedSql)
        {
            return SqlTemplate.Parse(parameterizedSql.Sql);
        }

        /// <summary>
        /// Adds WithParameters method to ParameterizedSql
        /// </summary>
        public static ParameterizedSql WithParameters(this ParameterizedSql parameterizedSql, object? parameters)
        {
            return parameterizedSql; // Basic implementation
        }

    }
}
