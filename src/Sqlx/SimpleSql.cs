#nullable enable
using System;
using System.Collections.Generic;

namespace Sqlx
{
    /// <summary>
    /// 极简SQL模板 - 统一实现
    /// </summary>
    public static class SimpleSql
    {
        /// <summary>创建模板</summary>
        public static Template Create(string template) => new(template);

        /// <summary>直接执行</summary>
        public static string Execute(string sql, Dictionary<string, object?> parameters) => Create(sql).With(parameters);

        /// <summary>无参执行</summary>
        public static string Execute(string sql) => sql;

        /// <summary>批量处理</summary>
        public static IEnumerable<string> Batch(string template, IEnumerable<Dictionary<string, object?>> parametersList)
        {
            var t = Create(template);
            foreach (var p in parametersList) yield return t.With(p).ToSql();
        }
    }

    /// <summary>
    /// SQL模板实例
    /// </summary>
    public readonly struct Template
    {
        private readonly string _template;
        private readonly Dictionary<string, object?> _parameters;

        internal Template(string template)
        {
            _template = template ?? throw new ArgumentNullException(nameof(template));
            _parameters = new Dictionary<string, object?>();
        }

        private Template(string template, Dictionary<string, object?> parameters)
        {
            _template = template;
            _parameters = parameters;
        }

        /// <summary>设置参数</summary>
        public Template With(Dictionary<string, object?> parameters)
        {
            if (parameters.Count == 0) return this;
            var newParams = new Dictionary<string, object?>(_parameters);
            foreach (var kvp in parameters) newParams[kvp.Key] = kvp.Value;
            return new Template(_template, newParams);
        }

        /// <summary>设置单个参数</summary>
        public Template Set(string name, object? value) => With(new() { [name] = value });

        /// <summary>生成SQL</summary>
        public string ToSql()
        {
            if (_parameters.Count == 0) return _template;
            var result = _template;
            foreach (var kvp in _parameters)
            {
                result = result.Replace($"{{{kvp.Key}}}", FormatValue(kvp.Value));
            }
            return result;
        }

        /// <summary>转为参数化SQL</summary>
        public ParameterizedSql ToParameterized()
        {
            var sql = _template;
            var parameters = new Dictionary<string, object?>();
            foreach (var kvp in _parameters)
            {
                sql = sql.Replace($"{{{kvp.Key}}}", $"@{kvp.Key}");
                parameters[$"@{kvp.Key}"] = kvp.Value;
            }
            return ParameterizedSql.Create(sql, parameters);
        }

        private static string FormatValue(object? value) => value switch
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

        public static implicit operator string(Template template) => template.ToSql();
        public override string ToString() => ToSql();
    }

    /// <summary>参数构建器</summary>
    public class Params
    {
        private readonly Dictionary<string, object?> _dict = new();
        public static Params New() => new();
        public Params Add(string name, object? value)
        {
            _dict[name] = value;
            return this;
        }
        public static implicit operator Dictionary<string, object?>(Params p) => new(p._dict);
    }

    /// <summary>
    /// 字符串扩展
    /// </summary>
    public static class StringSqlExtensions
    {
        public static Template AsTemplate(this string template) => SimpleSql.Create(template);
        public static string SqlWith(this string template, Dictionary<string, object?> parameters) => SimpleSql.Execute(template, parameters);
    }
}
