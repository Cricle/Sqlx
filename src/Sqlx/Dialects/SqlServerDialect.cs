// <copyright file="SqlServerDialect.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// SQL Server dialect implementation.
/// </summary>
public sealed class SqlServerDialect : SqlDialect
{
    /// <inheritdoc/>
    public override string DatabaseType => "SqlServer";

    /// <inheritdoc/>
    public override Annotations.SqlDefineTypes DbType => Annotations.SqlDefineTypes.SqlServer;

    /// <inheritdoc/>
    public override string ColumnLeft => "[";

    /// <inheritdoc/>
    public override string ColumnRight => "]";

    /// <inheritdoc/>
    public override string ParameterPrefix => "@";

    #region String Functions

    /// <inheritdoc/>
    public override string Concat(params string[] parts) => string.Join(" + ", parts);

    /// <inheritdoc/>
    public override string Length(string expression) => $"LEN({expression})";

    #endregion

    #region Date/Time Functions

    /// <inheritdoc/>
    public override string CurrentTimestamp => "GETDATE()";

    /// <inheritdoc/>
    public override string CurrentDate => "CAST(GETDATE() AS DATE)";

    /// <inheritdoc/>
    public override string CurrentTime => "CAST(GETDATE() AS TIME)";

    /// <inheritdoc/>
    public override string DatePart(string part, string expression) =>
        $"DATEPART({part}, {expression})";

    /// <inheritdoc/>
    public override string DateAdd(string interval, string number, string expression) =>
        $"DATEADD({interval}, {number}, {expression})";

    /// <inheritdoc/>
    public override string DateDiff(string interval, string startDate, string endDate) =>
        $"DATEDIFF({interval}, {startDate}, {endDate})";

    #endregion

    #region Pagination

    /// <inheritdoc/>
    public override string Limit(string count) => $"TOP {count}";

    /// <inheritdoc/>
    public override string Paginate(string limit, string offset) =>
        $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

    #endregion

    #region Null Handling

    /// <inheritdoc/>
    public override string IfNull(string expression, string defaultValue) =>
        $"ISNULL({expression}, {defaultValue})";

    #endregion

    #region Conditional

    /// <inheritdoc/>
    public override string Iif(string condition, string trueValue, string falseValue) =>
        $"IIF({condition}, {trueValue}, {falseValue})";

    #endregion

    #region Numeric Functions

    /// <inheritdoc/>
    public override string Mod(string dividend, string divisor) =>
        $"MOD({dividend}, {divisor})";

    #endregion

    #region Last Inserted ID

    /// <inheritdoc/>
    public override string LastInsertedId => "SELECT SCOPE_IDENTITY()";

    #endregion
}
