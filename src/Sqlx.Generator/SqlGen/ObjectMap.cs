// -----------------------------------------------------------------------
// <copyright file="ObjectMap.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen
{
    using Microsoft.CodeAnalysis;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class ObjectMap
    {
        public ObjectMap(IParameterSymbol symbol)
        {
            Symbol = symbol;
            // Check the type name, not the parameter name
            var typeName = symbol.Type.Name;
            if (typeName == "List" || typeName == "IList" || typeName == "IEnumerable")
            {
                ElementSymbol = ((INamedTypeSymbol)symbol.Type).TypeArguments[0]!;
            }
            else
            {
                ElementSymbol = symbol.Type;
            }

            Properties = ElementSymbol is INamedTypeSymbol namedTypeSymbol
                ? namedTypeSymbol.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.CanBeReferencedByName && 
                                p.Name != "EqualityContract" &&  // Filter out record internal property
                                !p.IsStatic &&                    // Filter out static properties
                                !p.IsIndexer &&                   // Filter out indexers
                                p.GetMethod != null &&            // Must have getter
                                p.GetMethod.DeclaredAccessibility == Accessibility.Public) // Must be public
                    .ToList()
                : new List<IPropertySymbol>();
        }

        public IParameterSymbol Symbol { get; }

        public ISymbol ElementSymbol { get; }

        public bool IsList => !SymbolEqualityComparer.Default.Equals(Symbol.Type, ElementSymbol);

        public List<IPropertySymbol> Properties { get; }
    }
}
