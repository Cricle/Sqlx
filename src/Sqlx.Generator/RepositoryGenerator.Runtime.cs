// <copyright file="RepositoryGenerator.Runtime.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

// This file contains runtime dialect support extensions for RepositoryGenerator

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

/// <summary>
/// Extensions for runtime dialect support in RepositoryGenerator.
/// </summary>
internal static class RepositoryGeneratorRuntimeExtensions
{
    /// <summary>
    /// Checks if the repository has a user-defined constructor with SqlDialect parameter.
    /// </summary>
    public static bool HasDialectConstructor(this INamedTypeSymbol repoType, out IMethodSymbol? constructor)
    {
        constructor = null;
        
        foreach (var ctor in repoType.InstanceConstructors)
        {
            if (ctor.IsImplicitlyDeclared) continue;
            
            // Check if has SqlDialect parameter
            foreach (var param in ctor.Parameters)
            {
                var typeName = param.Type.ToDisplayString();
                if (typeName == "Sqlx.SqlDialect" || typeName.EndsWith(".SqlDialect"))
                {
                    constructor = ctor;
                    return true;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Gets the parameter name for a primary constructor SqlDialect parameter, if present.
    /// </summary>
    public static string? GetPrimaryConstructorDialectParameterName(this INamedTypeSymbol repoType)
    {
        foreach (var ctor in repoType.InstanceConstructors)
        {
            if (ctor.IsImplicitlyDeclared || ctor.DeclaringSyntaxReferences.Length == 0 || ctor.Parameters.Length == 0)
            {
                continue;
            }

            var syntax = ctor.DeclaringSyntaxReferences[0].GetSyntax();
            if (syntax is ConstructorDeclarationSyntax)
            {
                continue;
            }

            foreach (var param in ctor.Parameters)
            {
                var typeName = param.Type.ToDisplayString();
                if (typeName == "Sqlx.SqlDialect" || typeName.EndsWith(".SqlDialect"))
                {
                    return param.Name;
                }
            }
        }

        return null;
    }
}
