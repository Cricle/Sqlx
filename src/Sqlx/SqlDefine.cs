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
    public string ParamterPrefx { get; }

    public SqlDefine(string columnLeft, string columnRight, string stringLeft, string stringRight, string paramterPrefx)
    {
        ColumnLeft = columnLeft;
        ColumnRight = columnRight;
        StringLeft = stringLeft;
        StringRight = stringRight;
        ParamterPrefx = paramterPrefx;
    }

    public void Deconstruct(out string columnLeft, out string columnRight, out string stringLeft, out string stringRight, out string paramterPrefx)
    {
        columnLeft = ColumnLeft;
        columnRight = ColumnRight;
        stringLeft = StringLeft;
        stringRight = StringRight;
        paramterPrefx = ParamterPrefx;
    }

    public static readonly SqlDefine MySql = new SqlDefine("`", "`", "'", "'", "@");
    public static readonly SqlDefine SqlServer = new SqlDefine("[", "]", "'", "'", "@");
    public static readonly SqlDefine PgSql = new SqlDefine("\"", "\"", "'", "'", "@");

    public string WrapString(string input) => $"{StringLeft}{input}{StringRight}";

    public string WrapColumn(string input) => $"{ColumnLeft}{input}{ColumnRight}";
}
