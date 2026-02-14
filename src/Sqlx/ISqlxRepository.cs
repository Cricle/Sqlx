// -----------------------------------------------------------------------
// <copyright file="ISqlxRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Data.Common;

namespace Sqlx
{
    /// <summary>
    /// Interface for repositories that can be managed by SqlxContext.
    /// Repositories implementing this interface can have their Connection and Transaction properties set by the context.
    /// </summary>
    /// <remarks>
    /// This interface enables zero-reflection property assignment in SqlxContext.
    /// The source generator automatically implements this interface for all repositories.
    /// </remarks>
    public interface ISqlxRepository
    {
        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        DbConnection? Connection { get; set; }

        /// <summary>
        /// Gets or sets the database transaction.
        /// </summary>
        DbTransaction? Transaction { get; set; }
    }
}
