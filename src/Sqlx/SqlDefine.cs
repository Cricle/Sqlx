// -----------------------------------------------------------------------
// <copyright file="SqlDefine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

internal readonly record struct SqlDefine
{
    public string ColumnLeft { get; }
    public string ColumnRight { get; }
    public string StringLeft { get; }
    public string StringRight { get; }
    public string ParameterPrefix { get; }

    public SqlDefine(string columnLeft, string columnRight, string stringLeft, string stringRight, string parameterPrefix)
    {
        ColumnLeft = columnLeft;
        ColumnRight = columnRight;
        StringLeft = stringLeft;
        StringRight = stringRight;
        ParameterPrefix = parameterPrefix;
    }

    public void Deconstruct(out string columnLeft, out string columnRight, out string stringLeft, out string stringRight, out string parameterPrefix)
    {
        columnLeft = ColumnLeft;
        columnRight = ColumnRight;
        stringLeft = StringLeft;
        stringRight = StringRight;
        parameterPrefix = ParameterPrefix;
    }

    public static readonly SqlDefine MySql = new SqlDefine("`", "`", "'", "'", "@");
    public static readonly SqlDefine SqlServer = new SqlDefine("[", "]", "'", "'", "@");
    public static readonly SqlDefine PgSql = new SqlDefine("\"", "\"", "'", "'", "$");
    public static readonly SqlDefine Oracle = new SqlDefine("\"", "\"", "'", "'", ":");
    public static readonly SqlDefine DB2 = new SqlDefine("\"", "\"", "'", "'", "?");
    public static readonly SqlDefine SQLite = new SqlDefine("[", "]", "'", "'", "@sqlite");

    public string WrapString(string input) => $"{StringLeft}{input}{StringRight}";

    public string WrapColumn(string input) => $"{ColumnLeft}{input}{ColumnRight}";
}
