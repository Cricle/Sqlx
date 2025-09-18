namespace Sqlx
{
    public partial class ExpressionToSql<T>
    {
        /// <summary>SQL Server方言</summary>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);

        /// <summary>MySQL方言</summary>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);

        /// <summary>PostgreSQL方言</summary>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);


        /// <summary>SQLite方言</summary>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);

        /// <summary>Oracle方言</summary>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);

        /// <summary>DB2方言</summary>
        public static ExpressionToSql<T> ForDB2() => new(SqlDefine.DB2);

        /// <summary>默认方言(SQL Server)</summary>
        public static ExpressionToSql<T> Create() => new(SqlDefine.SqlServer);
    }
}
