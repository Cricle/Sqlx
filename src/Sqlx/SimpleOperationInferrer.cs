#nullable enable

using System;
using System.Linq.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Simple operation inferrer for SQL template analysis
    /// </summary>
    public static class SimpleOperationInferrer
    {
        /// <summary>
        /// Infers SQL operation type from SQL text
        /// </summary>
        /// <param name="sql">SQL text to analyze</param>
        /// <returns>Inferred SQL operation type</returns>
        public static SqlOperation InferFromSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return SqlOperation.Select;

            var trimmed = sql.Trim().ToUpperInvariant();

            if (trimmed.StartsWith("SELECT"))
                return SqlOperation.Select;
            if (trimmed.StartsWith("INSERT"))
                return SqlOperation.Insert;
            if (trimmed.StartsWith("UPDATE"))
                return SqlOperation.Update;
            if (trimmed.StartsWith("DELETE"))
                return SqlOperation.Delete;

            return SqlOperation.Select;
        }

        /// <summary>
        /// Infers SQL operation type from method name
        /// </summary>
        /// <param name="methodName">Method name to analyze</param>
        /// <returns>Inferred SQL operation type</returns>
        public static SqlOperation InferFromMethodName(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                return SqlOperation.Select;

            var lower = methodName.ToLowerInvariant();

            if (lower.Contains("get") || lower.Contains("find") || lower.Contains("query") || lower.Contains("list") || lower.Contains("search"))
                return SqlOperation.Select;
            if (lower.Contains("add") || lower.Contains("create") || lower.Contains("insert"))
                return SqlOperation.Insert;
            if (lower.Contains("update") || lower.Contains("modify") || lower.Contains("edit"))
                return SqlOperation.Update;
            if (lower.Contains("delete") || lower.Contains("remove"))
                return SqlOperation.Delete;

            return SqlOperation.Select;
        }

        /// <summary>
        /// Analyzes SQL for complexity
        /// </summary>
        /// <param name="sql">SQL to analyze</param>
        /// <returns>True if SQL is complex</returns>
        public static bool IsComplexSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;

            var upper = sql.ToUpperInvariant();
            return upper.Contains("JOIN") ||
                   upper.Contains("UNION") ||
                   upper.Contains("SUBQUERY") ||
                   upper.Contains("WITH") ||
                   upper.Contains("HAVING") ||
                   upper.Contains("GROUP BY");
        }
    }
}
