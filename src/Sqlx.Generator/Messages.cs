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

        public static DiagnosticDescriptor SP0002 { get; } = new DiagnosticDescriptor("SP0002", "Invalid parameter type", "Parameter type '{0}' is not supported.", "Sqlx", DiagnosticSeverity.Error, true, "The parameter type is not supported by Sqlx code generation.");

        public static DiagnosticDescriptor SP0003 { get; } = new DiagnosticDescriptor("SP0003", "Missing return type", "Method must have a valid return type.", "Sqlx", DiagnosticSeverity.Error, true, "The method must specify a valid return type for code generation.");

        public static DiagnosticDescriptor SP0004 { get; } = new DiagnosticDescriptor("SP0004", "Invalid SQL syntax", "SQL command contains invalid syntax: '{0}'.", "Sqlx", DiagnosticSeverity.Error, true, "The SQL command contains syntax errors that cannot be processed.");

        public static DiagnosticDescriptor SP0005 { get; } = new DiagnosticDescriptor("SP0005", "Entity mapping error", "Cannot map entity '{0}' to database table.", "Sqlx", DiagnosticSeverity.Error, true, "The entity type cannot be mapped to a database table structure.");

        public static DiagnosticDescriptor SP0006 { get; } = new DiagnosticDescriptor("SP0006", "Async method missing CancellationToken", "Async method should accept CancellationToken parameter.", "Sqlx", DiagnosticSeverity.Error, true, "Async methods should include a CancellationToken parameter for proper cancellation support.");

        public static DiagnosticDescriptor SP0007 { get; } = new DiagnosticDescriptor("SP0007", "No RawSqlAttribute or SqlxAttribute tag", "No command text", "Sqlx", DiagnosticSeverity.Error, true, "The method must have either a RawSqlAttribute or SqlxAttribute to specify the SQL command.");

        public static DiagnosticDescriptor SP0008 { get; } = new DiagnosticDescriptor("SP0008", "Execute no query return must be int or Task<int>", "Return type error", "Sqlx", DiagnosticSeverity.Error, true, "Methods with ExecuteNoQueryAttribute must return int or Task<int> to represent the number of affected rows.");

        public static DiagnosticDescriptor SP0009 { get; } = new DiagnosticDescriptor("SP0009", "Repository interface not found", "Repository interface '{0}' could not be found.", "Sqlx", DiagnosticSeverity.Error, true, "The specified repository interface does not exist or is not accessible.");

        public static DiagnosticDescriptor SP0010 { get; } = new DiagnosticDescriptor("SP0010", "Table name not specified", "Table name must be specified for entity '{0}'.", "Sqlx", DiagnosticSeverity.Error, true, "The entity type must have a table name specified via TableNameAttribute or convention.");

        public static DiagnosticDescriptor SP0011 { get; } = new DiagnosticDescriptor("SP0011", "Primary key not found", "Entity '{0}' does not have a primary key property.", "Sqlx", DiagnosticSeverity.Error, true, "The entity should have a property named 'Id' or marked with a key attribute.");

        public static DiagnosticDescriptor SP0012 { get; } = new DiagnosticDescriptor("SP0012", "Duplicate method name", "Method name '{0}' is already defined in this repository.", "Sqlx", DiagnosticSeverity.Error, true, "Repository methods must have unique names within the same interface.");

        public static DiagnosticDescriptor SP0013 { get; } = new DiagnosticDescriptor("SP0013", "Invalid connection type", "Connection type '{0}' is not supported.", "Sqlx", DiagnosticSeverity.Error, true, "The connection type must implement IDbConnection or DbConnection.");

        public static DiagnosticDescriptor SP0014 { get; } = new DiagnosticDescriptor("SP0014", "SqlDefine configuration error", "SqlDefine configuration is invalid: '{0}'.", "Sqlx", DiagnosticSeverity.Error, true, "The SqlDefine attribute contains invalid database dialect configuration.");

        public static DiagnosticDescriptor SP0015 { get; } = new DiagnosticDescriptor("SP0015", "Code generation failed", "Code generation failed for method '{0}': {1}.", "Sqlx", DiagnosticSeverity.Error, true, "An error occurred during code generation for the specified method.");
    }
}
