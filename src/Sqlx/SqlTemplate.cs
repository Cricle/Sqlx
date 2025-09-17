// -----------------------------------------------------------------------
// <copyright file="SqlTemplate.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Data.Common;
using System.Linq;

namespace Sqlx.Annotations
{
    /// <summary>
    /// Represents a SQL template with parameterized command text and parameters.
    /// </summary>
    public readonly record struct SqlTemplate(string Sql, DbParameter[] Parameters)
    {
        /// <summary>
        /// Determines whether the specified object is equal to the current SqlTemplate.
        /// </summary>
        public bool Equals(SqlTemplate other)
        {
            return Sql == other.Sql &&
                   (Parameters?.Length ?? 0) == (other.Parameters?.Length ?? 0) &&
                   (Parameters == null && other.Parameters == null ||
                    Parameters != null && other.Parameters != null &&
                    Parameters.SequenceEqual(other.Parameters, DbParameterComparer.Instance));
        }

        /// <summary>
        /// Returns the hash code for this SqlTemplate.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Sql?.GetHashCode() ?? 0) * 397;
                hashCode ^= (Parameters?.Length ?? 0);
                if (Parameters != null)
                {
                    foreach (var param in Parameters)
                    {
                        hashCode = (hashCode * 397) ^ DbParameterComparer.Instance.GetHashCode(param);
                    }
                }
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a string representation of the SqlTemplate.
        /// </summary>
        public override string ToString()
        {
            return $"SqlTemplate {{ Sql = {Sql}, Parameters = {(Parameters != null ? $"System.Data.Common.DbParameter[{Parameters.Length}]" : "null")} }}";
        }
    }

    /// <summary>
    /// Comparer for DbParameter arrays.
    /// </summary>
    internal class DbParameterComparer : System.Collections.Generic.IEqualityComparer<DbParameter>
    {
        internal static readonly DbParameterComparer Instance = new();

        public bool Equals(DbParameter? x, DbParameter? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            return x.ParameterName == y.ParameterName &&
                   Equals(x.Value, y.Value) &&
                   x.DbType == y.DbType;
        }

        public int GetHashCode(DbParameter obj)
        {
            if (obj == null) return 0;

            return (obj.ParameterName?.GetHashCode() ?? 0) ^
                   (obj.Value?.GetHashCode() ?? 0) ^
                   obj.DbType.GetHashCode();
        }
    }
}
