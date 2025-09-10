// -----------------------------------------------------------------------
// <copyright file="CSharpGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Sqlx.Core;
using System.Diagnostics;

/// <summary>
/// Stored procedures generator for C#.
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class CSharpGenerator : AbstractGenerator
{
    /// <summary>
    /// Gets the C# attribute source using the optimized generator.
    /// </summary>
    private static string CSharpAttributeSource => AttributeSourceGenerator.GenerateAttributeSource();

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpGenerator"/> class.
    /// </summary>
    public CSharpGenerator()
    {
        // Initialize performance monitoring for source generation
#if DEBUG
        Debug.WriteLine("🚀 Sqlx CSharpGenerator initialized with advanced optimizations");
#endif
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
#if DEBUG
        System.Diagnostics.Debug.WriteLine("CSharpGenerator.Initialize called");
#endif
        context.RegisterForSyntaxNotifications(() => new CSharpSyntaxReceiver());
        context.RegisterForPostInitialization(ctx =>
        {
            // Generate the required attributes at compile time
            ctx.AddSource("SqlxAttributes.g.cs", CSharpAttributeSource);
        });
    }
}

