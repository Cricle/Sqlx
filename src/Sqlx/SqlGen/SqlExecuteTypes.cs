// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypes.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen;

internal enum SqlExecuteTypes
{
    Select = 0,
    Update = 1,
    Insert = 2,
    Delete = 3,
    BatchInsert = 4,
    BatchUpdate = 5,
    BatchDelete = 6,
}
