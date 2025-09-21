#nullable enable
using System;
using System.Collections.Generic;

namespace Sqlx
{
    /// <summary>SQL template</summary>
    public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        public static SqlTemplate Parse(string sql) => new(sql ?? throw new ArgumentNullException(nameof(sql)), new Dictionary<string, object?>());
        public ParameterizedSql Execute(Dictionary<string, object?> parameters) => ParameterizedSql.Create(Sql, parameters);
        public SqlTemplateBuilder Bind() => new(this);
    }

    /// <summary>Template builder</summary>
    public sealed class SqlTemplateBuilder
    {
        private readonly SqlTemplate _template;
        private readonly Dictionary<string, object?> _parameters = new();

        internal SqlTemplateBuilder(SqlTemplate template) => _template = template;
        public SqlTemplateBuilder Param<T>(string name, T value)
        {
            _parameters[name] = value;
            return this;
        }
        public ParameterizedSql Build() => new(_template.Sql, _parameters);
    }
}