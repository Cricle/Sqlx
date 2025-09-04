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

        public static DiagnosticDescriptor SP0002 { get; } = new DiagnosticDescriptor("SP0002", "Invalid method signature", "Method signature is not supported for code generation.", "Sqlx", DiagnosticSeverity.Error, true, "The method signature contains unsupported elements for code generation.");

        public static DiagnosticDescriptor SP0003 { get; } = new DiagnosticDescriptor("SP0003", "Id property cannot be guessed", "Cannot find id property for entity type {0}.", "Sqlx", DiagnosticSeverity.Error, true, "The entity type does not have a detectable ID property for database operations.");

        public static DiagnosticDescriptor SP0004 { get; } = new DiagnosticDescriptor("SP0004", "Entity property corresponding to parameter cannot be guessed", "Cannot find property in entity type {0} corresponding to parameter {1}.", "Sqlx", DiagnosticSeverity.Error, true, "The entity type does not contain a property that matches the specified parameter.");

        public static DiagnosticDescriptor SP0005 { get; } = new DiagnosticDescriptor("SP0005", "Unknown method for generation", "Unknown method {0} for generation.", "Sqlx", DiagnosticSeverity.Error, true, "The method cannot be processed by the code generator.");

        public static DiagnosticDescriptor SP0006 { get; } = new DiagnosticDescriptor("SP0006", "No such DbConnection or DbContext field, property or paramter", "No connection", "Sqlx", DiagnosticSeverity.Error, true, "No database connection source (DbConnection or DbContext) could be found in the class or method parameters.");

        public static DiagnosticDescriptor SP0007 { get; } = new DiagnosticDescriptor("SP0007", "No RawSqlAttribute or SqlxAttribute tag", "No command text", "Sqlx", DiagnosticSeverity.Error, true, "The method must have either a RawSqlAttribute or SqlxAttribute to specify the SQL command.");

        public static DiagnosticDescriptor SP0008 { get; } = new DiagnosticDescriptor("SP0008", "Execute no query return must be int or Task<int>", "Return type error", "Sqlx", DiagnosticSeverity.Error, true, "Methods with ExecuteNoQueryAttribute must return int or Task<int> to represent the number of affected rows.");

        public static DiagnosticDescriptor SP0009 { get; } = new DiagnosticDescriptor("SP0009", "Tuple dbcontext must has DbSetTypeAttribute tag", "No dbset", "Sqlx", DiagnosticSeverity.Error, true, "DbContext methods returning tuples must specify the entity type using DbSetTypeAttribute.");

        public static DiagnosticDescriptor SP0010 { get; } = new DiagnosticDescriptor("SP0010", "Invalid parameter type", "Parameter type {0} is not supported for code generation.", "Sqlx", DiagnosticSeverity.Error, true, "Parameter type is not supported for SQL generation.");

        public static DiagnosticDescriptor SP0011 { get; } = new DiagnosticDescriptor("SP0011", "Invalid return type", "Return type {0} is not supported for code generation.", "Sqlx", DiagnosticSeverity.Error, true, "Return type is not supported for SQL generation.");

        public static DiagnosticDescriptor SP0012 { get; } = new DiagnosticDescriptor("SP0012", "Missing required attribute", "Missing required attribute {0} on method {1}.", "Sqlx", DiagnosticSeverity.Error, true, "Required attribute is missing for code generation.");

        public static DiagnosticDescriptor SP0013 { get; } = new DiagnosticDescriptor("SP0013", "Invalid expression", "Expression {0} cannot be translated to SQL.", "Sqlx", DiagnosticSeverity.Error, true, "Expression cannot be translated to SQL.");

        public static DiagnosticDescriptor SP0014 { get; } = new DiagnosticDescriptor("SP0014", "Unsupported operation", "Operation {0} is not supported in SQL generation.", "Sqlx", DiagnosticSeverity.Error, true, "Operation is not supported in SQL generation.");

        public static DiagnosticDescriptor SP0015 { get; } = new DiagnosticDescriptor("SP0015", "Configuration error", "Configuration error: {0}", "Sqlx", DiagnosticSeverity.Error, true, "Configuration error in SQL generation.");
    }
}
