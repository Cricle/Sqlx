using System;
using System.Collections.Generic;

namespace Sqlx
{
    /// <summary>Represents a reusable SQL template that can be executed with different parameters.</summary>
    public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        /// <summary>Parses a SQL string into a template.</summary>
        /// <param name="sql">The SQL string to parse.</param>
        /// <returns>A new SqlTemplate instance.</returns>
        public static SqlTemplate Parse(string sql) => new(sql ?? throw new ArgumentNullException(nameof(sql)), new Dictionary<string, object?>());

        /// <summary>Executes the template with the specified parameters.</summary>
        /// <param name="parameters">Optional parameters to use for execution. If null, uses template's default parameters.</param>
        /// <returns>A ParameterizedSql instance ready for execution.</returns>
        public ParameterizedSql Execute(IReadOnlyDictionary<string, object?>? parameters = null) =>
            ParameterizedSql.Create(Sql, parameters ?? Parameters);

        /// <summary>Creates a builder for fluent parameter binding.</summary>
        /// <returns>A new SqlTemplateBuilder instance.</returns>
        public SqlTemplateBuilder Bind() => new(this);
    }

    /// <summary>Builder class for fluent parameter binding to SQL templates.</summary>
    public sealed class SqlTemplateBuilder
    {
        private readonly SqlTemplate _template;
        private readonly Dictionary<string, object?> _parameters = new();

        internal SqlTemplateBuilder(SqlTemplate template) => _template = template;

        /// <summary>Adds a parameter to the template.</summary>
        /// <typeparam name="T">The type of the parameter value.</typeparam>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public SqlTemplateBuilder Param<T>(string name, T value)
        {
            _parameters[name] = value;
            return this;
        }

        /// <summary>Builds the final ParameterizedSql with all bound parameters.</summary>
        /// <returns>A ParameterizedSql instance ready for execution.</returns>
        public ParameterizedSql Build() => ParameterizedSql.Create(_template.Sql, _parameters);
    }
}
