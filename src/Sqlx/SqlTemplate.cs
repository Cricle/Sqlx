#nullable enable

using System;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// SQL dialect type enumeration
    /// </summary>
    public enum SqlDialectType
    {
        /// <summary>SQL Server dialect</summary>
        SqlServer = 0,
        /// <summary>MySQL dialect</summary>
        MySql = 1,
        /// <summary>PostgreSQL dialect</summary>
        PostgreSql = 2,
        /// <summary>SQLite dialect</summary>
        SQLite = 3,
        /// <summary>Oracle dialect</summary>
        Oracle = 4,
        /// <summary>DB2 dialect</summary>
        DB2 = 5
    }

    /// <summary>
    /// SQL template options for configuration
    /// </summary>
    public class SqlTemplateOptions
    {
        /// <summary>SQL dialect type</summary>
        public SqlDialectType Dialect { get; set; } = SqlDialectType.SqlServer;
        /// <summary>Whether to use cache</summary>
        public bool UseCache { get; set; } = true;
        /// <summary>Whether to validate parameters</summary>
        public bool ValidateParameters { get; set; } = true;
        /// <summary>Whether to use parameterized queries</summary>
        public bool UseParameterizedQueries { get; set; } = true;

        /// <summary>Whether to enable safe mode</summary>
        public bool SafeMode { get; set; } = true;
        /// <summary>Whether to enable caching</summary>
        public bool EnableCaching { get; set; } = true;
        /// <summary>Custom functions dictionary</summary>
        public Dictionary<string, object?> CustomFunctions { get; set; } = new();

        /// <summary>Creates options for SQL Server</summary>
        public static SqlTemplateOptions ForSqlServer() => new() { Dialect = SqlDialectType.SqlServer };
        /// <summary>Creates options for MySQL</summary>
        public static SqlTemplateOptions ForMySql() => new() { Dialect = SqlDialectType.MySql };

        /// <summary>Creates options for PostgreSQL</summary>
        public static SqlTemplateOptions ForPostgreSQL() => new() { Dialect = SqlDialectType.PostgreSql };
        /// <summary>Creates options for SQLite</summary>
        public static SqlTemplateOptions ForSQLite() => new() { Dialect = SqlDialectType.SQLite };
        /// <summary>Default options</summary>
        public static SqlTemplateOptions Default => new();
    }
    /// <summary>
    /// 编译时 SQL 模板 - 提供安全的参数化查询功能
    /// 动态 SQL 功能已迁移到 ExpressionToSql，此类专注于编译时安全性
    /// </summary>
    public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
    {
        /// <summary>Empty SQL template</summary>
        public static readonly SqlTemplate Empty = new(string.Empty, new Dictionary<string, object?>());

        /// <summary>
        /// 创建编译时安全的 SQL 模板（推荐使用 SqlTemplateAttribute）
        /// </summary>
        /// <param name="sql">SQL 字符串</param>
        /// <returns>模板定义</returns>
        public static SqlTemplate Parse(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL template cannot be null or empty", nameof(sql));
            return new(sql, new Dictionary<string, object?>());
        }






        /// <summary>Executes template with parameters</summary>
        public ParameterizedSql Execute(object? parameters = null) => ParameterizedSql.Create(Sql, parameters);

        /// <summary>Executes template with parameter dictionary</summary>
        public ParameterizedSql Execute(Dictionary<string, object?> parameters) => ParameterizedSql.CreateWithDictionary(Sql, parameters);

        /// <summary>
        /// Creates fluent parameter binder
        /// </summary>
        /// <returns>Parameter binder</returns>
        public SqlTemplateBuilder Bind() => new(this);

        /// <summary>Checks if this is a pure template</summary>
        public bool IsPureTemplate => Parameters.Count == 0;


        /// <summary>Renders template with parameters</summary>
        public ParameterizedSql Render(object? parameters) => Execute(parameters);

        /// <summary>Renders template with parameter dictionary</summary>
        public ParameterizedSql Render(Dictionary<string, object?> parameters) => Execute(parameters);


        /// <summary>String representation</summary>
        public override string ToString() => $"SqlTemplate {{ Sql = {Sql}, Parameters = {Parameters.Count} params }}";
    }

    /// <summary>
    /// Fluent parameter binder
    /// </summary>
    public sealed class SqlTemplateBuilder
    {
        private readonly SqlTemplate _template;
        private readonly Dictionary<string, object?> _parameters = new();

        internal SqlTemplateBuilder(SqlTemplate template) => _template = template;

        /// <summary>
        /// Binds parameter
        /// </summary>
        public SqlTemplateBuilder Param<T>(string name, T value)
        {
            _parameters[name] = value;
            return this;
        }

        /// <summary>
        /// Batch binds parameters (AOT-friendly)
        /// </summary>
        public SqlTemplateBuilder Params(object? parameters)
        {
            if (parameters == null) return this;
            if (parameters is Dictionary<string, object?> dict)
            {
                foreach (var kvp in dict) _parameters[kvp.Key] = kvp.Value;
            }
            return this;
        }

        /// <summary>
        /// Builds final SQL
        /// </summary>
        public ParameterizedSql Build() => new(_template.Sql, _parameters);
    }

}




