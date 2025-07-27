// -----------------------------------------------------------------------
// <copyright file="MethodGenerationContext.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

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
        if (this.ConnectionParameter != null)
        {
            parameters = parameters.Remove(this.ConnectionParameter);
        }

        if (this.DbContextParameter != null)
        {
            parameters = parameters.Remove(this.DbContextParameter);
        }

        if (this.TransactionParameter != null)
        {
            parameters = parameters.Remove(this.TransactionParameter);
        }

        if (this.CustomSqlParameter != null)
        {
            parameters = parameters.Remove(this.CustomSqlParameter);
        }

        if (this.CancellationTokenParameter != null)
        {
            parameters = parameters.Remove(this.CancellationTokenParameter);
        }

        this.SqlParameters = parameters;
    }

    internal IMethodSymbol MethodSymbol { get; }

    internal ClassGenerationContext ClassGenerationContext { get; }

    internal bool UseDbConnection => this.ClassGenerationContext.ConnectionField != null || this.ConnectionParameter != null;

    internal bool IsNotDbContext => !this.UseDbConnection && ((this.IsList || this.IsEnumerable || this.IsAsyncEnumerable) && (IsTuple(this.ItemType) || IsScalarType(this.ItemType)));

    internal bool HasTransaction => !this.UseDbConnection
            && ((IsScalarType(AbstractGenerator.UnwrapNullableType(this.ReturnType))
            || this.ReturnType.SpecialType == SpecialType.System_Void
            || this.ReturnType.Name == "Task") || this.TransactionParameter != null || (this.IsList && (IsTuple(this.ItemType) || IsScalarType(this.ItemType))));

    internal IParameterSymbol? ConnectionParameter { get; }

    internal IParameterSymbol? TransactionParameter { get; }

    internal IParameterSymbol? DbContextParameter { get; }

    internal IParameterSymbol? CustomSqlParameter { get; }

    internal IParameterSymbol? CancellationTokenParameter { get; }

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
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            if (parameterSymbol.Type.IsDbConnection())
            {
                return parameterSymbol;
            }
        }

        return null;
    }

    private static IParameterSymbol? GetOutputResultsetParameter(IMethodSymbol methodSymbol)
    {
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            if (parameterSymbol.Type.IsList() && (parameterSymbol.RefKind == RefKind.Out || parameterSymbol.RefKind == RefKind.Ref))
            {
                return parameterSymbol;
            }
        }

        return null;
    }

    private static IParameterSymbol? GetTransactionParameter(IMethodSymbol methodSymbol)
    {
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            if (parameterSymbol.Type.IsDbTransaction())
            {
                return parameterSymbol;
            }
        }

        return null;
    }

    private static IParameterSymbol? GetDbContextParameter(IMethodSymbol methodSymbol)
    {
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            if (parameterSymbol.Type.IsDbContext())
            {
                return parameterSymbol;
            }
        }

        return null;
    }

    private static IParameterSymbol? GetCancellationTokenParameter(IMethodSymbol methodSymbol)
    {
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            if (parameterSymbol.Type.IsCancellationToken())
            {
                return parameterSymbol;
            }
        }

        return null;
    }

    private static IParameterSymbol? GetCustomSqlParameter(IMethodSymbol methodSymbol)
    {
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            var customSqlAttributeCandidate = parameterSymbol.GetAttributes()
                .FirstOrDefault(_ => _.AttributeClass?.Name == "RawSqlAttribute");
            if (customSqlAttributeCandidate != null)
            {
                return parameterSymbol;
            }
        }

        return null;
    }
}