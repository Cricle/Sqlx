// -----------------------------------------------------------------------
// <copyright file="AbstractGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

/// <summary>
/// Stored procedures generator.
/// </summary>
public abstract class AbstractGenerator : ISourceGenerator
{
    /// <inheritdoc/>
    public abstract void Initialize(GeneratorInitializationContext context);

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        // Retrieve the populated receiver
        if (context.SyntaxContextReceiver is not ISqlxSyntaxReceiver receiver)
        {
            return;
        }

        INamedTypeSymbol? sqlxAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");
        INamedTypeSymbol? rawSqlAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.RawSqlAttribute");
        INamedTypeSymbol? expressionToSqlAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.ExpressionToSqlAttribute");

        if (sqlxAttributeSymbol == null || rawSqlAttributeSymbol == null || expressionToSqlAttributeSymbol == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Messages.SP0001, null));
            return;
        }

        var hasNullableAnnotations = context.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;

        // Group the fields by class, and generate the source
        foreach (IGrouping<ISymbol?, IMethodSymbol> group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            var key = (INamedTypeSymbol)group.Key!;
            var ctx = new ClassGenerationContext(key, group.ToList(), sqlxAttributeSymbol, context);
            var sb = new IndentedStringBuilder(string.Empty);

            if (ctx.CreateSource(sb)) context.AddSource($"{key.ToDisplayString().Replace(".", "_")}.Sql.g.cs", SourceText.From(sb.ToString().Trim(), Encoding.UTF8));
        }
    }
}
