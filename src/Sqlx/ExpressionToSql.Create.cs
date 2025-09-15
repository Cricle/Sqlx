namespace Sqlx
{
    public partial class ExpressionToSql<T>
    {
        /// <summary>
        /// Creates an ExpressionToSql builder for SQL Server dialect.
        /// </summary>
        public static ExpressionToSql<T> ForSqlServer()
            => new ExpressionToSql<T>(SqlDefine.SqlServer);

        /// <summary>
        /// Creates an ExpressionToSql builder for MySQL dialect.
        /// </summary>
        public static ExpressionToSql<T> ForMySql()
            => new ExpressionToSql<T>(SqlDefine.MySql);

        /// <summary>
        /// Creates an ExpressionToSql builder for PostgreSQL dialect.
        /// </summary>
        public static ExpressionToSql<T> ForPostgreSQL()
            => new ExpressionToSql<T>(SqlDefine.PgSql);

        /// <summary>
        /// Creates an ExpressionToSql builder for Oracle dialect.
        /// </summary>
        public static ExpressionToSql<T> ForOracle()
            => new ExpressionToSql<T>(SqlDefine.Oracle);

        /// <summary>
        /// Creates an ExpressionToSql builder for DB2 dialect.
        /// </summary>
        public static ExpressionToSql<T> ForDB2()
            => new ExpressionToSql<T>(SqlDefine.DB2);

        /// <summary>
        /// Creates an ExpressionToSql builder for SQLite dialect.
        /// </summary>
        public static ExpressionToSql<T> ForSqlite()
            => new ExpressionToSql<T>(SqlDefine.Sqlite);

        /// <summary>
        /// Creates an ExpressionToSql builder with default (SQL Server) dialect.
        /// </summary>
        public static ExpressionToSql<T> Create()
            => new ExpressionToSql<T>(SqlDefine.SqlServer);

    }
}
