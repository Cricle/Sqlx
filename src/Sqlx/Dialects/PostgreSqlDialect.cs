// <copyright file="PostgreSqlDialect.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// PostgreSQL dialect implementation.
/// </summary>
public sealed class PostgreSqlDialect : SqlDialect
{
    /// <inheritdoc/>
    public override string DatabaseType => "PostgreSql";

    /// <inheritdoc/>
    public override Annotations.SqlDefineTypes DbType => Annotations.SqlDefineTypes.PostgreSql;

    /// <inheritdoc/>
    public override string ColumnLeft => "\"";

    /// <inheritdoc/>
    public override string ColumnRight => "\"";

    /// <inheritdoc/>
    public override string ParameterPrefix => "$";

    #region Boolean Literals

    /// <inheritdoc/>
    public override string BoolTrue => "true";

    /// <inheritdoc/>
    public override string BoolFalse => "false";

    #endregion

    #region String Functions

    /// <inheritdoc/>
    public override string Concat(params string[] parts) => string.Join(" || ", parts);

    #endregion

    #region Date/Time Functions

    /// <inheritdoc/>
    public override string CurrentTimestamp => "CURRENT_TIMESTAMP";

    /// <inheritdoc/>
    public override string CurrentDate => "CURRENT_DATE";

    /// <inheritdoc/>
    public override string CurrentTime => "CURRENT_TIME";

    /// <inheritdoc/>
    public override string DatePart(string part, string expression) =>
        $"EXTRACT({part} FROM {expression})";

    /// <inheritdoc/>
    public override string DateAdd(string interval, string number, string expression) =>
        $"({expression} + INTERVAL '{number} {interval}')";

    /// <inheritdoc/>
    public override string DateDiff(string interval, string startDate, string endDate) =>
        $"EXTRACT({interval} FROM ({endDate} - {startDate}))";

    #endregion

    #region Numeric Functions

    /// <inheritdoc/>
    public override string Ceiling(string expression) => $"CEIL({expression})";

    /// <inheritdoc/>
    public override string Mod(string dividend, string divisor) =>
        $"MOD({dividend}, {divisor})";

    #endregion

    #region Type Casting

    /// <inheritdoc/>
    public override string Cast(string expression, string targetType) =>
        $"({expression})::{targetType}";

    #endregion

    #region Last Inserted ID

    /// <inheritdoc/>
    public override string LastInsertedId => "SELECT lastval()";

    /// <inheritdoc/>
    public override string InsertReturningIdSuffix => " RETURNING id";

    #endregion
}
