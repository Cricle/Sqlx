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
/// Language specific interface for syntax receiver which is used to collect information about methods and repository classes.
/// </summary>
internal interface ISqlxSyntaxReceiver : ISyntaxReceiver
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

    /// <summary>
    /// Called for every syntax node in the compilation.
    /// </summary>
    /// <param name="syntaxNode">The syntax node to visit.</param>
    new void OnVisitSyntaxNode(SyntaxNode syntaxNode);
}
