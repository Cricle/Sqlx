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
        public static readonly ParameterizedSql Empty = new(string.Empty, new Dictionary<string, object?>(0));

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
        public static ParameterizedSql Create(string sql, Dictionary<string, object?> parameters)
        {
            return new ParameterizedSql(sql, parameters);
        }

        /// <summary>
        /// Renders to final SQL string with inlined parameters
        /// </summary>
        /// <returns>Rendered SQL string</returns>
        public string Render => SqlParameterRenderer.RenderToSql(this);

        /// <summary>
        /// Checks if the SQL contains a specific substring
        /// </summary>
        /// <param name="value">The substring to search for</param>
        /// <returns>True if the SQL contains the substring</returns>
        public bool Contains(string value) => Sql.Contains(value);

        /// <summary>
        /// Returns a string representation of the ParameterizedSql.
        /// </summary>
        public override string ToString() => $"ParameterizedSql {{ Sql = {Sql}, Parameters = {Parameters?.Count ?? 0} params }}";
    }

    /// <summary>
    /// SQL parameter renderer for inlining parameters into SQL
    /// </summary>
    internal static class SqlParameterRenderer
    {
        public static string RenderToSql(ParameterizedSql parameterizedSql)
        {
            if (parameterizedSql.Parameters.Count == 0) return parameterizedSql.Sql;

            var sql = parameterizedSql.Sql;
            foreach (var kvp in parameterizedSql.Parameters)
            {
                sql = sql.Replace(kvp.Key, FormatParameterValue(kvp.Value));
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
}
