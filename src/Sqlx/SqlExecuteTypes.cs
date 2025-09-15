// -----------------------------------------------------------------------
// <copyright file="SqlExecuteTypes.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Annotations
{
    /// <summary>
    /// Defines SQL operation types for CRUD operations.
    /// </summary>
    public enum SqlExecuteTypes
    {
        /// <summary>SELECT operation.</summary>
        Select = 0,
        /// <summary>UPDATE operation.</summary>
        Update = 1,
        /// <summary>INSERT operation.</summary>
        Insert = 2,
        /// <summary>DELETE operation.</summary>
        Delete = 3,
        /// <summary>Batch INSERT operation.</summary>
        BatchInsert = 4,
        /// <summary>Batch UPDATE operation.</summary>
        BatchUpdate = 5,
        /// <summary>Batch DELETE operation.</summary>
        BatchDelete = 6,
        /// <summary>ADO.NET BatchCommand operation.</summary>
        BatchCommand = 7
    }
}
