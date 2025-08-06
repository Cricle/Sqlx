// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContext.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Sqlx.Extensions;

internal class MethodGenerationContext
{
    internal MethodGenerationContext(ClassGenerationContext classGenerationContext, IMethodSymbol methodSymbol)
    {
        this.ClassGenerationContext = classGenerationContext;
        this.MethodSymbol = methodSymbol;

        this.ConnectionParameter = GetConnectionParameter(methodSymbol);
        this.TransactionParameter = GetTransactionParameter(methodSymbol);
        this.DbContextParameter = GetDbContextParameter(methodSymbol);
        this.CustomSqlParameter = GetCustomSqlParameter(methodSymbol);
        this.CancellationTokenParameter = GetCancellationTokenParameter(methodSymbol);
        var parameters = methodSymbol.Parameters;
        RemoveIfExists(ref parameters, this.ConnectionParameter);
        RemoveIfExists(ref parameters, this.DbContextParameter);
        RemoveIfExists(ref parameters, this.TransactionParameter);
        RemoveIfExists(ref parameters, this.CustomSqlParameter);
        RemoveIfExists(ref parameters, this.CancellationTokenParameter);
        this.SqlParameters = parameters;
    }

    internal IMethodSymbol MethodSymbol { get; }

    internal ClassGenerationContext ClassGenerationContext { get; }

    internal bool UseDbConnection => this.ClassGenerationContext.DbConnectionSymbol != null || this.ConnectionParameter != null;

    internal bool IsNotDbContext => !this.UseDbConnection && ((this.IsList || this.IsEnumerable || this.IsAsyncEnumerable) && (IsTuple(this.ItemType) || IsScalarType(this.ItemType)));

    internal bool HasTransaction => !this.UseDbConnection
            && ((IsScalarType(AbstractGenerator.UnwrapNullableType(this.ReturnType))
            || this.ReturnType.SpecialType == SpecialType.System_Void
            || this.ReturnType.Name == "Task") || this.TransactionParameter != null || (this.IsList && (IsTuple(this.ItemType) || IsScalarType(this.ItemType))));

    /// <summary>
    /// Gets the <see cref="System.Data.Common.DbConnection"/> if the method paramters has.
    /// </summary>
    internal IParameterSymbol? ConnectionParameter { get; }

    /// <summary>
    ///  Gets the <see cref="System.Data.Common.DbTransaction"/> if the method paramters has.
    /// </summary>
    internal IParameterSymbol? TransactionParameter { get; }

    /// <summary>
    ///  Gets the DbContext if the method paramters has.
    /// </summary>
    internal IParameterSymbol? DbContextParameter { get; }

    /// <summary>
    ///  Gets the SqlAttribute if the method paramters has.
    /// </summary>
    internal IParameterSymbol? CustomSqlParameter { get; }

    /// <summary>
    ///  Gets the <see cref="System.Threading.CancellationToken"/> if the method paramters has.
    /// </summary>
    internal IParameterSymbol? CancellationTokenParameter { get; }

    /// <summary>
    /// Gets the method paramters remove the extars.
    /// </summary>
    internal ImmutableArray<IParameterSymbol> SqlParameters { get; }

    internal bool IsTask => this.MethodSymbol.ReturnType.Name == "Task";

    internal bool IsDataReader => this.MethodSymbol.ReturnType.Name == "DbDataReader";

    internal ITypeSymbol ReturnType => this.MethodSymbol.ReturnType.UnwrapTaskType();

    internal bool IsList => this.ReturnType.IsList();

    internal bool IsEnumerable => IsEnumerable(this.ReturnType);

    internal bool IsAsyncEnumerable => IsAsyncEnumerable(this.ReturnType);

    internal ITypeSymbol ItemType => UnwrapListItem(this.ReturnType);

    private static IParameterSymbol? GetConnectionParameter(IMethodSymbol methodSymbol)
    {
        return GetParameter(methodSymbol, x => x.Type.IsDbConnection());
    }

    private static void RemoveIfExists(ref ImmutableArray<IParameterSymbol> pars, IParameterSymbol? symbol)
    {
        if (symbol != null)
        {
            pars = pars.Remove(symbol);
        }
    }

    private static IParameterSymbol? GetOutputResultsetParameter(IMethodSymbol methodSymbol)
    {
        return methodSymbol.Parameters.FirstOrDefault(x => x.Type.IsList() && (x.RefKind == RefKind.Out || x.RefKind == RefKind.Ref));
    }

    private static IParameterSymbol? GetTransactionParameter(IMethodSymbol methodSymbol)
    {
        return GetParameter(methodSymbol, x => x.Type.IsDbTransaction());
    }

    private static IParameterSymbol? GetDbContextParameter(IMethodSymbol methodSymbol)
    {
        return GetParameter(methodSymbol, x => x.Type.IsDbContext());
    }

    private static IParameterSymbol? GetCancellationTokenParameter(IMethodSymbol methodSymbol)
    {
        return GetParameter(methodSymbol, x => x.Type.IsCancellationToken());
    }

    private static IParameterSymbol? GetParameter(IMethodSymbol methodSymbol, Func<IParameterSymbol, bool> check)
    {
        return methodSymbol.Parameters.FirstOrDefault(check);
    }

    private static IParameterSymbol? GetCustomSqlParameter(IMethodSymbol methodSymbol)
    {
        return methodSymbol.Parameters.Where(x => x.GetAttributes().Any(x => x.AttributeClass?.Name == "RawSqlAttribute")).FirstOrDefault();
    }
}