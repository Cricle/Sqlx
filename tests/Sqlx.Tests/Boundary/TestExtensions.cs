using System;
using System.Data;

namespace Sqlx.Tests.Boundary;

/// <summary>
/// Shared test extensions for Boundary tests
/// </summary>
public static class BoundaryTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql, IDbTransaction? transaction = null, object? param = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        if (transaction != null)
        {
            cmd.Transaction = transaction;
        }

        if (param != null)
        {
            foreach (var prop in param.GetType().GetProperties())
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@" + prop.Name;
                p.Value = prop.GetValue(param) ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
        }

        cmd.ExecuteNonQuery();
    }
}

