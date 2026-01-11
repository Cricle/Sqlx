using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Renders the SQL query by replacing parameter placeholders with their actual values.
        /// </summary>
        /// <returns>The rendered SQL query with all parameters substituted.</returns>
        /// <remarks>
        /// <para><strong>Warning:</strong> This method is intended for debugging and logging purposes only.</para>
        /// <para>For production use, always use parameterized queries to prevent SQL injection.</para>
        /// <para>Parameters are replaced in order of decreasing name length to avoid substring conflicts.</para>
        /// </remarks>
        public string Render()
        {
            if (Parameters == null || Parameters.Count == 0) return Sql;

            var result = Sql;
            
            // Sort parameters by name length (descending) to avoid substring replacement issues
            // Example: @userId should be replaced before @user to prevent incorrect substitution
            var sortedParams = Parameters.OrderByDescending(kvp => kvp.Key.Length);
            
            foreach (var kvp in sortedParams)
                result = result.Replace(kvp.Key, FormatValue(kvp.Value));
                
            return result;
        }

        /// <summary>
        /// Formats a parameter value for SQL rendering.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A SQL-safe string representation of the value.</returns>
        private static string FormatValue(object? value) => value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",  // Escape single quotes
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            Guid g => $"'{g}'",
            decimal or double or float => value.ToString()!,
            _ => value?.ToString() ?? "NULL"
        };
    }
}
