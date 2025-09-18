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

        public static DiagnosticDescriptor SP0007 { get; } = new DiagnosticDescriptor("SP0007", "No SqlxAttribute tag", "No command text", "Sqlx", DiagnosticSeverity.Error, true, "The method must have a SqlxAttribute to specify the SQL command (RawSqlAttribute has been merged into SqlxAttribute).");

        public static DiagnosticDescriptor SP0008 { get; } = new DiagnosticDescriptor("SP0008", "Execute no query return must be int or Task<int>", "Return type error", "Sqlx", DiagnosticSeverity.Error, true, "Methods with ExecuteNoQueryAttribute must return int or Task<int> to represent the number of affected rows.");

        public static DiagnosticDescriptor SP0009 { get; } = new DiagnosticDescriptor("SP0009", "Repository interface not found", "Repository interface '{0}' could not be found.", "Sqlx", DiagnosticSeverity.Error, true, "The specified repository interface does not exist or is not accessible.");

        public static DiagnosticDescriptor SP0010 { get; } = new DiagnosticDescriptor("SP0010", "Table name not specified", "Table name must be specified for entity '{0}'.", "Sqlx", DiagnosticSeverity.Error, true, "The entity type must have a table name specified via TableNameAttribute or convention.");

        public static DiagnosticDescriptor SP0011 { get; } = new DiagnosticDescriptor("SP0011", "Primary key not found", "Entity '{0}' does not have a primary key property.", "Sqlx", DiagnosticSeverity.Error, true, "The entity should have a property named 'Id' or marked with a key attribute.");

        public static DiagnosticDescriptor SP0012 { get; } = new DiagnosticDescriptor("SP0012", "Duplicate method name", "Method name '{0}' is already defined in this repository.", "Sqlx", DiagnosticSeverity.Error, true, "Repository methods must have unique names within the same interface.");

        public static DiagnosticDescriptor SP0013 { get; } = new DiagnosticDescriptor("SP0013", "Invalid connection type", "Connection type '{0}' is not supported.", "Sqlx", DiagnosticSeverity.Error, true, "The connection type must implement IDbConnection or DbConnection.");

        public static DiagnosticDescriptor SP0014 { get; } = new DiagnosticDescriptor("SP0014", "SqlDefine configuration error", "SqlDefine configuration is invalid: '{0}'.", "Sqlx", DiagnosticSeverity.Error, true, "The SqlDefine attribute contains invalid database dialect configuration.");

        public static DiagnosticDescriptor SP0015 { get; } = new DiagnosticDescriptor("SP0015", "Code generation failed", "Code generation failed for method '{0}': {1}.", "Sqlx", DiagnosticSeverity.Error, true, "An error occurred during code generation for the specified method.");

        // New diagnostic messages for improved user guidance
        public static DiagnosticDescriptor SP0016 { get; } = new DiagnosticDescriptor("SP0016", "SELECT * detected", "Avoid using SELECT * in SQL. Specify explicit columns for better performance and maintainability.", "Sqlx", DiagnosticSeverity.Warning, true, "Use explicit column names instead of SELECT * for better performance, security, and maintainability.");

        public static DiagnosticDescriptor SP0017 { get; } = new DiagnosticDescriptor("SP0017", "Missing [Sqlx] attribute", "Method appears to be a database operation but lacks [Sqlx] attribute. Add [Sqlx] to generate implementation.", "Sqlx", DiagnosticSeverity.Info, true, "Database operation methods require explicit [Sqlx] attribute. Automatic CRUD inference is disabled by design.");

        public static DiagnosticDescriptor SP0018 { get; } = new DiagnosticDescriptor("SP0018", "Consider using SqlTemplate", "For complex CRUD operations, consider using SqlTemplate.Select(), Insert(), Update(), or Delete() builders.", "Sqlx", DiagnosticSeverity.Info, true, "SqlTemplate provides a fluent API for building type-safe SQL with better maintainability.");

        public static DiagnosticDescriptor SP0019 { get; } = new DiagnosticDescriptor("SP0019", "SqlTemplate parameter available", "This method can accept SqlTemplate as parameter by setting AcceptsSqlTemplate = true in [Sqlx] attribute.", "Sqlx", DiagnosticSeverity.Info, true, "Enable SqlTemplate parameter support for dynamic SQL generation scenarios.");

        public static DiagnosticDescriptor SP0020 { get; } = new DiagnosticDescriptor("SP0020", "DELETE without WHERE", "DELETE statements should always include WHERE conditions for safety.", "Sqlx", DiagnosticSeverity.Warning, true, "DELETE statements without WHERE conditions can accidentally delete all data. Always include proper WHERE conditions.");

        public static DiagnosticDescriptor SP0021 { get; } = new DiagnosticDescriptor("SP0021", "UPDATE without WHERE", "UPDATE statements should include WHERE conditions to avoid unintended data changes.", "Sqlx", DiagnosticSeverity.Warning, true, "UPDATE statements without WHERE conditions will modify all rows. Include WHERE conditions for targeted updates.");
    }
}
