// -----------------------------------------------------------------------
// <copyright file="ReferenceEqualityComparer.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Sqlx
{
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

        private ReferenceEqualityComparer()
        {
        }

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
