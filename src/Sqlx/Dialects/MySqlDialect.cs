// <copyright file="MySqlDialect.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// MySQL dialect implementation.
/// </summary>
public sealed class MySqlDialect : SqlDialect
{
    /// <inheritdoc/>
    public override string DatabaseType => "MySql";

    /// <inheritdoc/>
    public override Annotations.SqlDefineTypes DbType => Annotations.SqlDefineTypes.MySql;

    /// <inheritdoc/>
    public override string ColumnLeft => "`";

    /// <inheritdoc/>
    public override string ColumnRight => "`";

    /// <inheritdoc/>
    public override string ParameterPrefix => "@";

    #region String Functions

    /// <inheritdoc/>
    public override string Concat(params string[] parts) =>
        $"CONCAT({string.Join(", ", parts)})";

    /// <inheritdoc/>
    public override string Length(string expression) => $"CHAR_LENGTH({expression})";

    /// <inheritdoc/>
    public override string Substring(string expression, string start, string length) =>
        $"SUBSTR({expression}, {start}, {length})";

    #endregion

    #region Date/Time Functions

    /// <inheritdoc/>
    public override string CurrentTimestamp => "NOW()";

    /// <inheritdoc/>
    public override string CurrentDate => "CURDATE()";

    /// <inheritdoc/>
    public override string CurrentTime => "CURTIME()";

    /// <inheritdoc/>
    public override string DatePart(string part, string expression) =>
        $"EXTRACT({part} FROM {expression})";

    /// <inheritdoc/>
    public override string DateAdd(string interval, string number, string expression) =>
        $"DATE_ADD({expression}, INTERVAL {number} {interval})";

    /// <inheritdoc/>
    public override string DateDiff(string interval, string startDate, string endDate) =>
        $"TIMESTAMPDIFF({interval}, {startDate}, {endDate})";

    #endregion

    #region Null Handling

    /// <inheritdoc/>
    public override string IfNull(string expression, string defaultValue) =>
        $"IFNULL({expression}, {defaultValue})";

    #endregion

    #region Conditional

    /// <inheritdoc/>
    public override string Iif(string condition, string trueValue, string falseValue) =>
        $"IF({condition}, {trueValue}, {falseValue})";

    #endregion

    #region Numeric Functions

    /// <inheritdoc/>
    public override string Mod(string dividend, string divisor) =>
        $"MOD({dividend}, {divisor})";

    #endregion

    #region Last Inserted ID

    /// <inheritdoc/>
    public override string LastInsertedId => "SELECT LAST_INSERT_ID()";

    #endregion
}
