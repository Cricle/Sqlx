// -----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Sqlx;

/// <summary>
/// Stored procedures generator for C#.
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class CSharpGenerator : AbstractGenerator
{
    // Attributes are now provided by Sqlx project - no need to generate them

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpGenerator"/> class.
    /// </summary>
    public CSharpGenerator()
    {
        // Initialize performance monitoring for source generation
    }

    // Syntax receiver moved to partial file: CSharpGenerator.SyntaxReceiver.cs

    /// <summary>
    /// Called to initialize the generator and register for the various 
    /// <see cref="SyntaxNode"/>
    /// callbacks.
    /// </summary>
    /// <param name="context">The generator context.</param>
    public override void Initialize(GeneratorInitializationContext context)
    {
        // Register as SyntaxReceiver to ensure context.SyntaxReceiver works
        context.RegisterForSyntaxNotifications((SyntaxReceiverCreator)(() => new CSharpSyntaxReceiver()));
    }
}

