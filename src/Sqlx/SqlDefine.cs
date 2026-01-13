// <copyright file="SqlDefine.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;

/// <summary>
/// Provides predefined database dialect configurations for SQL generation.
/// </summary>
public static class SqlDefine
{
    /// <summary>SQL Server dialect configuration.</summary>
    public static readonly SqlDialect SqlServer = new SqlServerDialect();

    /// <summary>MySQL dialect configuration.</summary>
    public static readonly SqlDialect MySql = new MySqlDialect();

    /// <summary>PostgreSQL dialect configuration.</summary>
    public static readonly SqlDialect PostgreSql = new PostgreSqlDialect();

    /// <summary>SQLite dialect configuration.</summary>
    public static readonly SqlDialect SQLite = new SQLiteDialect();

    /// <summary>Oracle dialect configuration.</summary>
    public static readonly SqlDialect Oracle = new OracleDialect();

    /// <summary>DB2 dialect configuration.</summary>
    public static readonly SqlDialect DB2 = new DB2Dialect();

    /// <summary>Alias for PostgreSQL.</summary>
    public static readonly SqlDialect PgSql = PostgreSql;

    /// <summary>Alias for SQLite.</summary>
    public static readonly SqlDialect Sqlite = SQLite;

    /// <summary>Gets the appropriate SQL dialect for the specified database type.</summary>
    public static SqlDialect GetDialect(Annotations.SqlDefineTypes databaseType) => databaseType switch
    {
        Annotations.SqlDefineTypes.MySql => MySql,
        Annotations.SqlDefineTypes.SqlServer => SqlServer,
        Annotations.SqlDefineTypes.PostgreSql => PostgreSql,
        Annotations.SqlDefineTypes.SQLite => SQLite,
        Annotations.SqlDefineTypes.Oracle => Oracle,
        Annotations.SqlDefineTypes.DB2 => DB2,
        _ => throw new NotSupportedException($"Database type '{databaseType}' is not supported.")
    };
}

/// <summary>
/// Base class for SQL dialect configurations.
/// Provides extensible database-specific SQL generation.
/// </summary>
public abstract class SqlDialect
{
    /// <summary>Gets the database type name.</summary>
    public abstract string DatabaseType { get; }

    /// <summary>Gets the database type enum.</summary>
    public abstract Annotations.SqlDefineTypes DbType { get; }

    /// <summary>Gets the left delimiter for column/table names.</summary>
    public abstract string ColumnLeft { get; }

    /// <summary>Gets the right delimiter for column/table names.</summary>
    public abstract string ColumnRight { get; }

    /// <summary>Gets the left delimiter for string literals.</summary>
    public virtual string StringLeft => "'";

    /// <summary>Gets the right delimiter for string literals.</summary>
    public virtual string StringRight => "'";

    /// <summary>Gets the parameter prefix.</summary>
    public abstract string ParameterPrefix { get; }

    #region Identifier Quoting

    /// <summary>Wraps a column/table name with dialect-specific delimiters.</summary>
    public virtual string WrapColumn(string name) =>
        string.IsNullOrEmpty(name) ? string.Empty : $"{ColumnLeft}{name}{ColumnRight}";

    /// <summary>Wraps a string value with dialect-specific delimiters.</summary>
    public virtual string WrapString(string value) =>
        value is null ? "NULL" : $"{StringLeft}{EscapeString(value)}{StringRight}";

    /// <summary>Creates a parameter with dialect-specific prefix.</summary>
    public virtual string CreateParameter(string name) => $"{ParameterPrefix}{name}";

    /// <summary>Escapes special characters in string values.</summary>
    protected virtual string EscapeString(string value) =>
        value.Replace(StringLeft, StringLeft + StringLeft);

    #endregion

    #region Boolean Literals

    /// <summary>Gets the SQL literal for boolean true.</summary>
    public virtual string BoolTrue => "1";

    /// <summary>Gets the SQL literal for boolean false.</summary>
    public virtual string BoolFalse => "0";

    #endregion

    #region String Functions

    /// <summary>Gets the string concatenation expression.</summary>
    public abstract string Concat(params string[] parts);

    /// <summary>Gets the UPPER function call.</summary>
    public virtual string Upper(string expression) => $"UPPER({expression})";

    /// <summary>Gets the LOWER function call.</summary>
    public virtual string Lower(string expression) => $"LOWER({expression})";

    /// <summary>Gets the TRIM function call.</summary>
    public virtual string Trim(string expression) => $"TRIM({expression})";

    /// <summary>Gets the LTRIM function call.</summary>
    public virtual string LTrim(string expression) => $"LTRIM({expression})";

    /// <summary>Gets the RTRIM function call.</summary>
    public virtual string RTrim(string expression) => $"RTRIM({expression})";

    /// <summary>Gets the LENGTH/LEN function call.</summary>
    public virtual string Length(string expression) => $"LENGTH({expression})";

    /// <summary>Gets the SUBSTRING function call.</summary>
    public virtual string Substring(string expression, string start, string length) =>
        $"SUBSTRING({expression}, {start}, {length})";

    /// <summary>Gets the REPLACE function call.</summary>
    public virtual string Replace(string expression, string oldValue, string newValue) =>
        $"REPLACE({expression}, {oldValue}, {newValue})";

    /// <summary>Gets the COALESCE function call.</summary>
    public virtual string Coalesce(params string[] expressions) =>
        $"COALESCE({string.Join(", ", expressions)})";

    #endregion

    #region Date/Time Functions

    /// <summary>Gets the current timestamp expression.</summary>
    public abstract string CurrentTimestamp { get; }

    /// <summary>Gets the current date expression.</summary>
    public abstract string CurrentDate { get; }

    /// <summary>Gets the current time expression.</summary>
    public abstract string CurrentTime { get; }

    /// <summary>Gets the date part extraction expression.</summary>
    public abstract string DatePart(string part, string expression);

    /// <summary>Gets the date addition expression.</summary>
    public abstract string DateAdd(string interval, string number, string expression);

    /// <summary>Gets the date difference expression.</summary>
    public abstract string DateDiff(string interval, string startDate, string endDate);

    #endregion

    #region Numeric Functions

    /// <summary>Gets the ABS function call.</summary>
    public virtual string Abs(string expression) => $"ABS({expression})";

    /// <summary>Gets the ROUND function call.</summary>
    public virtual string Round(string expression, string decimals) =>
        $"ROUND({expression}, {decimals})";

    /// <summary>Gets the CEILING/CEIL function call.</summary>
    public virtual string Ceiling(string expression) => $"CEILING({expression})";

    /// <summary>Gets the FLOOR function call.</summary>
    public virtual string Floor(string expression) => $"FLOOR({expression})";

    /// <summary>Gets the MOD function call.</summary>
    public virtual string Mod(string dividend, string divisor) =>
        $"MOD({dividend}, {divisor})";

    #endregion

    #region Aggregate Functions

    /// <summary>Gets the COUNT function call.</summary>
    public virtual string Count(string expression = "*") => $"COUNT({expression})";

    /// <summary>Gets the SUM function call.</summary>
    public virtual string Sum(string expression) => $"SUM({expression})";

    /// <summary>Gets the AVG function call.</summary>
    public virtual string Avg(string expression) => $"AVG({expression})";

    /// <summary>Gets the MIN function call.</summary>
    public virtual string Min(string expression) => $"MIN({expression})";

    /// <summary>Gets the MAX function call.</summary>
    public virtual string Max(string expression) => $"MAX({expression})";

    #endregion

    #region Pagination

    /// <summary>Gets the LIMIT clause.</summary>
    public virtual string Limit(string count) => $"LIMIT {count}";

    /// <summary>Gets the OFFSET clause.</summary>
    public virtual string Offset(string count) => $"OFFSET {count}";

    /// <summary>Gets the pagination clause (LIMIT + OFFSET combined).</summary>
    public virtual string Paginate(string limit, string offset) =>
        $"LIMIT {limit} OFFSET {offset}";

    #endregion

    #region Null Handling

    /// <summary>Gets the IFNULL/ISNULL/NVL function call.</summary>
    public virtual string IfNull(string expression, string defaultValue) =>
        $"COALESCE({expression}, {defaultValue})";

    /// <summary>Gets the NULLIF function call.</summary>
    public virtual string NullIf(string expression1, string expression2) =>
        $"NULLIF({expression1}, {expression2})";

    #endregion

    #region Type Casting

    /// <summary>Gets the CAST expression.</summary>
    public virtual string Cast(string expression, string targetType) =>
        $"CAST({expression} AS {targetType})";

    #endregion

    #region Conditional

    /// <summary>Gets the CASE WHEN expression.</summary>
    public virtual string CaseWhen(string condition, string thenValue, string elseValue) =>
        $"CASE WHEN {condition} THEN {thenValue} ELSE {elseValue} END";

    /// <summary>Gets the IIF function (if supported) or CASE WHEN equivalent.</summary>
    public virtual string Iif(string condition, string trueValue, string falseValue) =>
        CaseWhen(condition, trueValue, falseValue);

    #endregion

    #region Last Inserted ID

    /// <summary>Gets the SQL to retrieve the last inserted ID.</summary>
    public abstract string LastInsertedId { get; }

    #endregion
}
