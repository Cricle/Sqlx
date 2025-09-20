namespace Sqlx
{
    public partial class ExpressionToSql<T>
    {
        /// <summary>SQL Server dialect</summary>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);
        /// <summary>MySQL dialect</summary>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);
        /// <summary>PostgreSQL dialect</summary>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);
        /// <summary>SQLite dialect</summary>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);
        /// <summary>Oracle dialect</summary>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);
        /// <summary>DB2 dialect</summary>
        public static ExpressionToSql<T> ForDB2() => new(SqlDefine.DB2);
    }
}
