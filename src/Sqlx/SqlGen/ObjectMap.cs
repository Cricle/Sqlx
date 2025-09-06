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
                ? namedTypeSymbol.GetMembers().OfType<IPropertySymbol>().ToList()
                : new List<IPropertySymbol>();
        }

        public IParameterSymbol Symbol { get; }

        public ISymbol ElementSymbol { get; }

        public bool IsList => !SymbolEqualityComparer.Default.Equals(Symbol.Type, ElementSymbol);

        public List<IPropertySymbol> Properties { get; }
    }
}
