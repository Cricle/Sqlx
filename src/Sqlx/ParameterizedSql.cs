namespace Sqlx
{
    /// <summary>Represents a parameterized SQL query with type-safe parameter handling.</summary>
    public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?>? Parameters)
    {
        /// <summary>Creates a new parameterized SQL instance with the specified SQL and parameters.</summary>
        /// <param name="sql">The SQL query text.</param>
        /// <param name="parameters">Optional parameters for the SQL query.</param>
        /// <returns>A new ParameterizedSql instance.</returns>
        public static ParameterizedSql Create(string sql, IReadOnlyDictionary<string, object?>? parameters = null) => new(sql, parameters);

        /// <summary>Renders the SQL query by replacing parameter placeholders with actual values.</summary>
        /// <returns>The rendered SQL query with parameters substituted.</returns>
        public string Render()
        {
            if (Parameters == null || Parameters.Count == 0) return Sql;

            var result = Sql;
            foreach (var kvp in Parameters)
                result = result.Replace(kvp.Key, FormatValue(kvp.Value));
            return result;
        }

        private static string FormatValue(object? value) => value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            Guid g => $"'{g}'",
            decimal or double or float => value.ToString()!,
            _ => value?.ToString() ?? "NULL"
        };
    }
}
