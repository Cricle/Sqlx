// -----------------------------------------------------------------------
// <copyright file="SqlDefine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core
{
    /// <summary>
    /// Internal SQL dialect definition for code generation.
    /// </summary>
    public readonly struct SqlDefine
    {
        /// <summary>
        /// 获取列名的左边界符。
        /// </summary>
        public string ColumnLeft { get; }
        /// <summary>
        /// 获取列名的右边界符。
        /// </summary>
        public string ColumnRight { get; }
        /// <summary>
        /// 获取字符串的左边界符。
        /// </summary>
        public string StringLeft { get; }
        /// <summary>
        /// 获取字符串的右边界符。
        /// </summary>
        public string StringRight { get; }
        /// <summary>
        /// 获取参数的前缀符。
        /// </summary>
        public string ParameterPrefix { get; }

        /// <summary>
        /// 初始化 SqlDefine 结构的新实例。
        /// </summary>
        /// <param name="columnLeft">列名的左边界符。</param>
        /// <param name="columnRight">列名的右边界符。</param>
        /// <param name="stringLeft">字符串的左边界符。</param>
        /// <param name="stringRight">字符串的右边界符。</param>
        /// <param name="parameterPrefix">参数的前缀符。</param>
        public SqlDefine(string columnLeft, string columnRight, string stringLeft, string stringRight, string parameterPrefix)
        {
            ColumnLeft = columnLeft;
            ColumnRight = columnRight;
            StringLeft = stringLeft;
            StringRight = stringRight;
            ParameterPrefix = parameterPrefix;
        }

        /// <summary>
        /// 获取 MySQL 数据库的 SQL 方言定义。
        /// </summary>
        public static readonly SqlDefine MySql = new SqlDefine("`", "`", "'", "'", "@");
        /// <summary>
        /// 获取 SQL Server 数据库的 SQL 方言定义。
        /// </summary>
        public static readonly SqlDefine SqlServer = new SqlDefine("[", "]", "'", "'", "@");
        /// <summary>
        /// 获取 PostgreSQL 数据库的 SQL 方言定义。
        /// </summary>
        public static readonly SqlDefine PgSql = new SqlDefine("\"", "\"", "'", "'", "$");
        /// <summary>
        /// 获取 Oracle 数据库的 SQL 方言定义。
        /// </summary>
        public static readonly SqlDefine Oracle = new SqlDefine("\"", "\"", "'", "'", ":");
        /// <summary>
        /// 获取 DB2 数据库的 SQL 方言定义。
        /// </summary>
        public static readonly SqlDefine DB2 = new SqlDefine("\"", "\"", "'", "'", "?");
        /// <summary>
        /// 获取 SQLite 数据库的 SQL 方言定义。
        /// </summary>
        public static readonly SqlDefine SQLite = new SqlDefine("[", "]", "'", "'", "$");

        /// <summary>
        /// 使用字符串界符包裹指定的字符串。
        /// </summary>
        /// <param name="input">要包裹的字符串。</param>
        /// <returns>包裹后的字符串。</returns>
        public string WrapString(string input) => $"{StringLeft}{input}{StringRight}";
        /// <summary>
        /// 使用列名界符包裹指定的列名。
        /// </summary>
        /// <param name="input">要包裹的列名。</param>
        /// <returns>包裹后的列名。</returns>
        public string WrapColumn(string input) => $"{ColumnLeft}{input}{ColumnRight}";

        /// <summary>
        /// 将 SqlDefine 结构解构为各个组成部分。
        /// </summary>
        /// <param name="columnLeft">列名的左边界符。</param>
        /// <param name="columnRight">列名的右边界符。</param>
        /// <param name="stringLeft">字符串的左边界符。</param>
        /// <param name="stringRight">字符串的右边界符。</param>
        /// <param name="parameterPrefix">参数的前缀符。</param>
        public void Deconstruct(out string columnLeft, out string columnRight, out string stringLeft, out string stringRight, out string parameterPrefix)
        {
            columnLeft = ColumnLeft;
            columnRight = ColumnRight;
            stringLeft = StringLeft;
            stringRight = StringRight;
            parameterPrefix = ParameterPrefix;
        }
    }

    /// <summary>
    /// SQL dialect types enumeration for code generation.
    /// </summary>
    public enum SqlDefineTypes
    {
        /// <summary>
        /// MySQL 数据库方言类型。
        /// </summary>
        MySql = 0,
        /// <summary>
        /// SQL Server 数据库方言类型。
        /// </summary>
        SqlServer = 1,
        /// <summary>
        /// PostgreSQL 数据库方言类型。
        /// </summary>
        PostgreSql = 2,
        /// <summary>
        /// Oracle 数据库方言类型。
        /// </summary>
        Oracle = 3,
        /// <summary>
        /// DB2 数据库方言类型。
        /// </summary>
        DB2 = 4,
        /// <summary>
        /// SQLite 数据库方言类型。
        /// </summary>
        SQLite = 5,
    }
}

