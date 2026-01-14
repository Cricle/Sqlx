// <copyright file="IParameterBinder.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Data.Common;

/// <summary>
/// Binds entity parameters to DbCommand (AOT-friendly, reflection-free).
/// </summary>
public interface IParameterBinder<TEntity>
{
    /// <summary>Binds entity properties as parameters.</summary>
    void BindEntity(DbCommand command, TEntity entity, string parameterPrefix = "@");

#if NET6_0_OR_GREATER
    /// <summary>Binds entity properties to batch command.</summary>
    void BindEntity(DbBatchCommand command, TEntity entity, Func<DbParameter> parameterFactory, string parameterPrefix = "@");
#endif
}
