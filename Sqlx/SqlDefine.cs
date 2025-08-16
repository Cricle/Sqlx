// -----------------------------------------------------------------------
// <copyright file="SqlDefine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

internal sealed record SqlDefine(string ColumnLeft, string ColumnRight, string StringLeft, string StringRight, string ParamterPrefx)
{
    public static readonly SqlDefine MySql = new SqlDefine("`", "`", "'", "'", "@");
    public static readonly SqlDefine SqlService = new SqlDefine("[", "]", "'", "'", "@");
    public static readonly SqlDefine PgSql = new SqlDefine("\"", "\"", "'", "'", ":");

    public string WrapString(string input) => $"{StringLeft}{input}{StringRight}";

    public string WrapColumn(string input) => $"{ColumnLeft}{input}{ColumnRight}";
}
