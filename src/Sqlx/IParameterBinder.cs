// <copyright file="IParameterBinder.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Data.Common;

/// <summary>
/// Binds entity parameters to DbCommand without reflection.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IParameterBinder<TEntity>
{
    /// <summary>
    /// Binds all entity properties as parameters to the command.
    /// </summary>
    /// <param name="command">The database command.</param>
    /// <param name="entity">The entity to bind.</param>
    /// <param name="parameterPrefix">The parameter prefix (e.g., "@", "$", ":").</param>
    void BindEntity(DbCommand command, TEntity entity, string parameterPrefix = "@");
}
