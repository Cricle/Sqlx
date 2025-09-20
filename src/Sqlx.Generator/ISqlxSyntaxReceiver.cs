// -----------------------------------------------------------------------
// <copyright file="ISqlxSyntaxReceiver.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

/// <summary>
/// Simple interface for syntax receiver collections.
/// </summary>
internal interface ISqlxSyntaxReceiver
{
    /// <summary>
    /// Gets list of collected methods.
    /// </summary>
    List<IMethodSymbol> Methods { get; }

    /// <summary>
    /// Gets list of collected repository classes.
    /// </summary>
    List<INamedTypeSymbol> RepositoryClasses { get; }

    /// <summary>
    /// Gets list of collected method syntax nodes for later processing.
    /// </summary>
    List<MethodDeclarationSyntax> MethodSyntaxNodes { get; }

    /// <summary>
    /// Gets list of collected class syntax nodes for later processing.
    /// </summary>
    List<ClassDeclarationSyntax> ClassSyntaxNodes { get; }
}
