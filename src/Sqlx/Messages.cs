// -----------------------------------------------------------------------
// <copyright file="Messages.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx
{
    using Microsoft.CodeAnalysis;

    internal static class Messages
    {
        public static DiagnosticDescriptor SP0001 { get; } = new DiagnosticDescriptor("SP0001", "No stored procedure attribute", "Internal analyzer error.", "Internal", DiagnosticSeverity.Error, true, "Internal analyzer error occurred during code generation.");

        public static DiagnosticDescriptor SP0006 { get; } = new DiagnosticDescriptor("SP0006", "No such DbConnection or DbContext field, property or paramter", "No connection", "Sqlx", DiagnosticSeverity.Error, true, "No database connection source (DbConnection or DbContext) could be found in the class or method parameters.");

        public static DiagnosticDescriptor SP0007 { get; } = new DiagnosticDescriptor("SP0007", "No RawSqlAttribute or SqlxAttribute tag", "No command text", "Sqlx", DiagnosticSeverity.Error, true, "The method must have either a RawSqlAttribute or SqlxAttribute to specify the SQL command.");

        public static DiagnosticDescriptor SP0008 { get; } = new DiagnosticDescriptor("SP0008", "Execute no query return must be int or Task<int>", "Return type error", "Sqlx", DiagnosticSeverity.Error, true, "Methods with ExecuteNoQueryAttribute must return int or Task<int> to represent the number of affected rows.");

        public static DiagnosticDescriptor SP0009 { get; } = new DiagnosticDescriptor("SP0009", "Tuple dbcontext must has DbSetTypeAttribute tag", "No dbset", "Sqlx", DiagnosticSeverity.Error, true, "DbContext methods returning tuples must specify the entity type using DbSetTypeAttribute.");

        public static DiagnosticDescriptor SP0002 { get; } = new DiagnosticDescriptor("SP0002", "Invalid SQL syntax", "SQL syntax error", "Sqlx", DiagnosticSeverity.Error, true, "The provided SQL contains syntax errors or invalid constructs.");

        public static DiagnosticDescriptor SP0003 { get; } = new DiagnosticDescriptor("SP0003", "Parameter type mismatch", "Parameter error", "Sqlx", DiagnosticSeverity.Error, true, "Method parameter types do not match the expected SQL parameter types.");

        public static DiagnosticDescriptor SP0004 { get; } = new DiagnosticDescriptor("SP0004", "Missing required parameter", "Missing parameter", "Sqlx", DiagnosticSeverity.Error, true, "Required SQL parameters are missing from the method signature.");

        public static DiagnosticDescriptor SP0005 { get; } = new DiagnosticDescriptor("SP0005", "Unsupported return type", "Return type error", "Sqlx", DiagnosticSeverity.Error, true, "The method return type is not supported for the specified SQL operation.");

        public static DiagnosticDescriptor SP0010 { get; } = new DiagnosticDescriptor("SP0010", "Compilation error", "Code generation error", "Sqlx", DiagnosticSeverity.Error, true, "An error occurred during code generation or compilation.");

        public static DiagnosticDescriptor SP0011 { get; } = new DiagnosticDescriptor("SP0011", "Invalid entity mapping", "Entity mapping error", "Sqlx", DiagnosticSeverity.Error, true, "Entity properties cannot be properly mapped to database columns.");

        public static DiagnosticDescriptor SP0012 { get; } = new DiagnosticDescriptor("SP0012", "Unsupported database dialect", "Dialect error", "Sqlx", DiagnosticSeverity.Error, true, "The specified database dialect is not supported or configured incorrectly.");

        public static DiagnosticDescriptor SP0013 { get; } = new DiagnosticDescriptor("SP0013", "Invalid method signature", "Method signature error", "Sqlx", DiagnosticSeverity.Error, true, "The method signature is invalid for the specified SQL operation type.");

        public static DiagnosticDescriptor SP0014 { get; } = new DiagnosticDescriptor("SP0014", "Repository interface error", "Interface error", "Sqlx", DiagnosticSeverity.Error, true, "The repository interface contains invalid method definitions or constraints.");

        public static DiagnosticDescriptor SP0015 { get; } = new DiagnosticDescriptor("SP0015", "Transaction handling error", "Transaction error", "Sqlx", DiagnosticSeverity.Error, true, "Error in transaction handling or transaction parameter usage.");
    }
}
