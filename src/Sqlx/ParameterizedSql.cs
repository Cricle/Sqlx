#nullable enable

using System.Collections.Generic;

namespace Sqlx
{
    /// <summary>
    /// Represents a parameterized SQL statement with SQL text and parameter values
    /// This is an execution-time instance, not a reusable template definition
    /// </summary>
    public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        /// <summary>
        /// Empty ParameterizedSql instance
        /// </summary>
        public static readonly ParameterizedSql Empty = new(string.Empty, new Dictionary<string, object?>());

        /// <summary>
        /// Creates parameterized SQL using dictionary
        /// </summary>
        /// <param name="sql">SQL statement</param>
        /// <param name="parameters">Parameter dictionary</param>
        /// <returns>ParameterizedSql instance</returns>
        public static ParameterizedSql CreateWithDictionary(string sql, Dictionary<string, object?> parameters)
        {
            return new ParameterizedSql(sql, parameters);
        }

        /// <summary>Creates parameterized SQL using object parameters</summary>
        public static ParameterizedSql Create(string sql, object? parameters)
        {
            var paramDict = ExtractParametersSafe(parameters);
            return new ParameterizedSql(sql, paramDict);
        }

        /// <summary>
        /// Renders to final SQL string with inlined parameters
        /// </summary>
        /// <returns>Rendered SQL string</returns>
        public string Render()
        {
            return SqlParameterRenderer.RenderToSql(this);
        }

        #region Private Helper Methods

        private static Dictionary<string, object?> ExtractParametersSafe(object? parameters)
        {
            var result = new Dictionary<string, object?>();
            if (parameters is Dictionary<string, object?> dict)
            {
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            // For AOT compatibility, we only support Dictionary<string, object?> parameters
            // Users should use Dictionary<string, object?> directly
            return result;
        }

        #endregion

        /// <summary>
        /// Checks if the SQL contains a specific substring
        /// </summary>
        /// <param name="value">The substring to search for</param>
        /// <returns>True if the SQL contains the substring</returns>
        public bool Contains(string value) => Sql.Contains(value);

        /// <summary>
        /// Returns a string representation of the ParameterizedSql.
        /// </summary>
        public override string ToString()
        {
            var paramCount = Parameters?.Count ?? 0;
            return $"ParameterizedSql {{ Sql = {Sql}, Parameters = {paramCount} params }}";
        }
    }

    /// <summary>
    /// SQL parameter renderer for inlining parameters into SQL
    /// </summary>
    internal static class SqlParameterRenderer
    {
        public static string RenderToSql(ParameterizedSql parameterizedSql)
        {
            if (parameterizedSql.Parameters.Count == 0)
            {
                return parameterizedSql.Sql;
            }

            var sql = parameterizedSql.Sql;
            foreach (var kvp in parameterizedSql.Parameters)
            {
                var value = FormatParameterValue(kvp.Value);
                sql = sql.Replace(kvp.Key, value);
            }

            return sql;
        }

        private static string FormatParameterValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "1" : "0",
                System.DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                System.DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
                System.Guid g => $"'{g}'",
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}
