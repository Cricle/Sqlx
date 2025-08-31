// -----------------------------------------------------------------------
// <copyright file="ObjectMap.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    internal sealed class ObjectMap
    {
        public ObjectMap(IParameterSymbol symbol)
        {
            Symbol = symbol;
            if (symbol.Name == "List" || symbol.Name == "IList" || symbol.Name == "IEnumerable")
            {
                ElementSymbol = ((INamedTypeSymbol)symbol.Type).TypeArguments[0]!;
            }
            else
            {
                ElementSymbol = symbol.Type;
            }

            Properties = ((INamedTypeSymbol)ElementSymbol).GetMembers().OfType<IPropertySymbol>().ToList();
        }

        public IParameterSymbol Symbol { get; }

        public ISymbol ElementSymbol { get; }

        public bool IsList => !SymbolEqualityComparer.Default.Equals(Symbol, ElementSymbol);

        public List<IPropertySymbol> Properties { get; }
    }
}
