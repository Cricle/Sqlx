// <copyright file="OracleDialect.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// Oracle dialect implementation.
/// </summary>
public sealed class OracleDialect : SqlDialect
{
    /// <inheritdoc/>
    public override string DatabaseType => "Oracle";

    /// <inheritdoc/>
    public override Annotations.SqlDefineTypes DbType => Annotations.SqlDefineTypes.Oracle;

    /// <inheritdoc/>
    public override string ColumnLeft => "\"";

    /// <inheritdoc/>
    public override string ColumnRight => "\"";

    /// <inheritdoc/>
    public override string ParameterPrefix => ":";

    #region String Functions

    /// <inheritdoc/>
    public override string Concat(params string[] parts) => string.Join(" || ", parts);

    /// <inheritdoc/>
    public override string Substring(string expression, string start, string length) =>
        $"SUBSTR({expression}, {start}, {length})";

    #endregion

    #region Date/Time Functions

    /// <inheritdoc/>
    public override string CurrentTimestamp => "SYSTIMESTAMP";

    /// <inheritdoc/>
    public override string CurrentDate => "SYSDATE";

    /// <inheritdoc/>
    public override string CurrentTime => "TO_CHAR(SYSDATE, 'HH24:MI:SS')";

    /// <inheritdoc/>
    public override string DatePart(string part, string expression) =>
        $"EXTRACT({part} FROM {expression})";

    /// <inheritdoc/>
    public override string DateAdd(string interval, string number, string expression) =>
        interval.ToUpperInvariant() switch
        {
            "DAY" => $"({expression} + {number})",
            "MONTH" => $"ADD_MONTHS({expression}, {number})",
            "YEAR" => $"ADD_MONTHS({expression}, {number} * 12)",
            "HOUR" => $"({expression} + {number}/24)",
            "MINUTE" => $"({expression} + {number}/1440)",
            "SECOND" => $"({expression} + {number}/86400)",
            _ => $"({expression} + INTERVAL '{number}' {interval})"
        };

    /// <inheritdoc/>
    public override string DateDiff(string interval, string startDate, string endDate) =>
        interval.ToUpperInvariant() switch
        {
            "DAY" => $"({endDate} - {startDate})",
            "MONTH" => $"MONTHS_BETWEEN({endDate}, {startDate})",
            _ => $"({endDate} - {startDate})"
        };

    #endregion

    #region Pagination

    /// <inheritdoc/>
    public override string Limit(string count) => $"FETCH FIRST {count} ROWS ONLY";

    /// <inheritdoc/>
    public override string Paginate(string limit, string offset) =>
        $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

    #endregion

    #region Null Handling

    /// <inheritdoc/>
    public override string IfNull(string expression, string defaultValue) =>
        $"NVL({expression}, {defaultValue})";

    #endregion

    #region Last Inserted ID

    /// <inheritdoc/>
    public override string LastInsertedId => "SELECT SEQ.CURRVAL FROM DUAL";

    #endregion
}
