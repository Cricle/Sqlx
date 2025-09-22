using System.Globalization;

namespace Sqlx
{
    /// <summary>Parameterized SQL</summary>
    public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?>? Parameters)
    {
        public static ParameterizedSql Create(string sql, IReadOnlyDictionary<string, object?>? parameters = null) => new(sql, parameters);
        public string Render
        {
            get
            {
                if (Parameters == null || Parameters.Count == 0) return Sql;

                var sql = Sql;
                foreach (var kvp in Parameters)
                {
                    sql = sql.Replace(kvp.Key, FormatParameterValue(kvp.Value));
                }

                return sql;
            }
        }

        private static string FormatParameterValue(object? value) => value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
            Guid g => $"'{g}'",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "NULL"
        };
    }
}
