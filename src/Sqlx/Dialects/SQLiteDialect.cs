// <copyright file="SQLiteDialect.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// SQLite dialect implementation.
/// </summary>
public sealed class SQLiteDialect : SqlDialect
{
    /// <inheritdoc/>
    public override string DatabaseType => "SQLite";

    /// <inheritdoc/>
    public override Annotations.SqlDefineTypes DbType => Annotations.SqlDefineTypes.SQLite;

    /// <inheritdoc/>
    public override string ColumnLeft => "[";

    /// <inheritdoc/>
    public override string ColumnRight => "]";

    /// <inheritdoc/>
    public override string ParameterPrefix => "@";

    #region String Functions

    /// <inheritdoc/>
    public override string Concat(params string[] parts) => string.Join(" || ", parts);

    #endregion

    #region Date/Time Functions

    /// <inheritdoc/>
    public override string CurrentTimestamp => "CURRENT_TIMESTAMP";

    /// <inheritdoc/>
    public override string CurrentDate => "DATE('now')";

    /// <inheritdoc/>
    public override string CurrentTime => "TIME('now')";

    /// <inheritdoc/>
    public override string DatePart(string part, string expression) =>
        $"STRFTIME('%{GetStrftimeFormat(part)}', {expression})";

    /// <inheritdoc/>
    public override string DateAdd(string interval, string number, string expression) =>
        $"DATETIME({expression}, '+{number} {interval}')";

    /// <inheritdoc/>
    public override string DateDiff(string interval, string startDate, string endDate) =>
        $"(JULIANDAY({endDate}) - JULIANDAY({startDate}))";

    private static string GetStrftimeFormat(string part) => part.ToUpperInvariant() switch
    {
        "YEAR" => "Y",
        "MONTH" => "m",
        "DAY" => "d",
        "HOUR" => "H",
        "MINUTE" => "M",
        "SECOND" => "S",
        _ => part
    };

    #endregion

    #region Null Handling

    /// <inheritdoc/>
    public override string IfNull(string expression, string defaultValue) =>
        $"IFNULL({expression}, {defaultValue})";

    #endregion

    #region Last Inserted ID

    /// <inheritdoc/>
    public override string LastInsertedId => "SELECT last_insert_rowid()";

    #endregion
}
