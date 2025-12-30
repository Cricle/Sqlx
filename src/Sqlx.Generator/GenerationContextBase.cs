// -----------------------------------------------------------------------
// <copyright file="GenerationContextBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

internal abstract class GenerationContextBase
{
    /// <summary>
    /// Gets the <see cref="System.Data.Common.DbConnection"/> if the method paramters has.
    /// </summary>
    internal abstract ISymbol? DbConnection { get; }

    /// <summary>
    ///  Gets the <see cref="System.Data.Common.DbTransaction"/> if the method paramters has.
    /// </summary>
    internal abstract ISymbol? TransactionParameter { get; }

    /// <summary>
    ///  Gets the DbContext if the method paramters has.
    /// </summary>
    internal abstract ISymbol? DbContext { get; }

    protected static ISymbol? GetSymbol(ISymbol? symbol, Func<ISymbol, bool> check)
    {
        if (symbol == null) return null;

        if (symbol is IMethodSymbol ms)
            return ms.Parameters.FirstOrDefault(x => check(x));
        if (symbol is INamedTypeSymbol nts)
        {
            return nts.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => check(x)) ??
                   nts.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(x => check(x)) ??
                   GetSymbol(nts.BaseType, check);
        }

        return null;
    }
}
