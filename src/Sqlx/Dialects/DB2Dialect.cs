// <copyright file="DB2Dialect.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// IBM DB2 dialect implementation.
/// </summary>
public sealed class DB2Dialect : SqlDialect
{
    /// <inheritdoc/>
    public override string DatabaseType => "DB2";

    /// <inheritdoc/>
    public override Annotations.SqlDefineTypes DbType => Annotations.SqlDefineTypes.DB2;

    /// <inheritdoc/>
    public override string ColumnLeft => "\"";

    /// <inheritdoc/>
    public override string ColumnRight => "\"";

    /// <inheritdoc/>
    public override string ParameterPrefix => "?";

    #region String Functions

    /// <inheritdoc/>
    public override string Concat(params string[] parts) =>
        $"CONCAT({string.Join(", ", parts)})";

    /// <inheritdoc/>
    public override string Substring(string expression, string start, string length) =>
        $"SUBSTR({expression}, {start}, {length})";

    #endregion

    #region Date/Time Functions

    /// <inheritdoc/>
    public override string CurrentTimestamp => "CURRENT TIMESTAMP";

    /// <inheritdoc/>
    public override string CurrentDate => "CURRENT DATE";

    /// <inheritdoc/>
    public override string CurrentTime => "CURRENT TIME";

    /// <inheritdoc/>
    public override string DatePart(string part, string expression) =>
        $"{part}({expression})";

    /// <inheritdoc/>
    public override string DateAdd(string interval, string number, string expression) =>
        $"({expression} + {number} {interval})";

    /// <inheritdoc/>
    public override string DateDiff(string interval, string startDate, string endDate) =>
        $"TIMESTAMPDIFF({GetTimestampDiffCode(interval)}, {startDate}, {endDate})";

    private static string GetTimestampDiffCode(string interval) => interval.ToUpperInvariant() switch
    {
        "YEAR" => "256",
        "MONTH" => "64",
        "DAY" => "16",
        "HOUR" => "8",
        "MINUTE" => "4",
        "SECOND" => "2",
        _ => "16" // Default to days
    };

    #endregion

    #region Pagination

    /// <inheritdoc/>
    public override string Limit(string count) => $"FETCH FIRST {count} ROWS ONLY";

    /// <inheritdoc/>
    public override string Paginate(string limit, string offset) =>
        $"OFFSET {offset} ROWS FETCH FIRST {limit} ROWS ONLY";

    #endregion

    #region Last Inserted ID

    /// <inheritdoc/>
    public override string LastInsertedId => "SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1";

    #endregion
}
