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

        // 新增诊断消息用于改进用户体验
        public static DiagnosticDescriptor SP0022 { get; } = new DiagnosticDescriptor("SP0022", "SqlExecuteType table name suggestion", "Consider specifying table name in SqlExecuteType attribute: [SqlExecuteType(SqlOperation.{0}, \"TableName\")].", "Sqlx", DiagnosticSeverity.Info, true, "Explicitly specifying table name in SqlExecuteType attribute improves code clarity and reduces ambiguity.");

        public static DiagnosticDescriptor SP0023 { get; } = new DiagnosticDescriptor("SP0023", "TableName attribute detected", "Using TableName attribute for table resolution. This can be overridden by specifying tableName parameter in SqlExecuteType.", "Sqlx", DiagnosticSeverity.Info, true, "TableName attribute provides default table name. SqlExecuteType parameter takes precedence when both are specified.");

        public static DiagnosticDescriptor SP0024 { get; } = new DiagnosticDescriptor("SP0024", "Async method best practice", "Async method '{0}' should have CancellationToken parameter for better resource management.", "Sqlx", DiagnosticSeverity.Info, true, "Adding CancellationToken parameter enables proper cancellation support and improves application responsiveness.");

        public static DiagnosticDescriptor SP0025 { get; } = new DiagnosticDescriptor("SP0025", "Connection parameter suggestion", "Consider using IDbConnection parameter instead of field/property for better testability.", "Sqlx", DiagnosticSeverity.Info, true, "Method-level IDbConnection parameter enables easier unit testing and dependency injection.");

        public static DiagnosticDescriptor SP0026 { get; } = new DiagnosticDescriptor("SP0026", "Potential SQL injection risk", "SQL template contains potential injection risk. Use parameterized queries and avoid string concatenation.", "Sqlx", DiagnosticSeverity.Warning, true, "Always use parameterized queries to prevent SQL injection attacks. Avoid concatenating user input directly into SQL strings.");

        public static DiagnosticDescriptor SP0027 { get; } = new DiagnosticDescriptor("SP0027", "Performance suggestion", "Consider using {0} for better performance in this scenario.", "Sqlx", DiagnosticSeverity.Info, true, "Performance optimization suggestions based on query patterns and usage context.");

        public static DiagnosticDescriptor SP0028 { get; } = new DiagnosticDescriptor("SP0028", "Type mapping suggestion", "Entity property '{0}' type '{1}' may require explicit DbType mapping for optimal performance.", "Sqlx", DiagnosticSeverity.Info, true, "Explicit DbType mapping can improve query performance and ensure correct data type handling.");

        public static DiagnosticDescriptor SP0029 { get; } = new DiagnosticDescriptor("SP0029", "Naming convention", "Method name '{0}' doesn't follow recommended naming conventions. Consider using {1}.", "Sqlx", DiagnosticSeverity.Info, true, "Following consistent naming conventions improves code readability and maintainability.");

        public static DiagnosticDescriptor SP0030 { get; } = new DiagnosticDescriptor("SP0030", "Return type optimization", "Return type '{0}' can be optimized to '{1}' for this query pattern.", "Sqlx", DiagnosticSeverity.Info, true, "Optimized return types can improve performance and memory usage.");

        // 高级诊断规则 - 性能和架构分析
        public static DiagnosticDescriptor SP0031 { get; } = new DiagnosticDescriptor("SP0031", "Potential N+1 query", "Method '{0}' may cause N+1 query problem. Consider using JOIN or batch loading.", "Sqlx", DiagnosticSeverity.Warning, true, "N+1 queries can cause severe performance issues. Use JOIN queries or batch loading instead of individual queries in loops.");

        public static DiagnosticDescriptor SP0032 { get; } = new DiagnosticDescriptor("SP0032", "Large result set warning", "Query may return large result set. Consider adding pagination with LIMIT/OFFSET.", "Sqlx", DiagnosticSeverity.Info, true, "Large result sets can cause memory issues. Implement pagination to improve performance and user experience.");

        public static DiagnosticDescriptor SP0033 { get; } = new DiagnosticDescriptor("SP0033", "Complex JOIN detected", "Complex JOIN operation detected. Verify query performance and consider optimization.", "Sqlx", DiagnosticSeverity.Info, true, "Complex JOINs can impact performance. Monitor execution time and consider query optimization techniques.");

        public static DiagnosticDescriptor SP0034 { get; } = new DiagnosticDescriptor("SP0034", "Subquery optimization", "Subquery detected. Consider using JOIN for better performance: '{0}'.", "Sqlx", DiagnosticSeverity.Info, true, "Subqueries often perform worse than JOINs. Consider rewriting as JOIN operations for better performance.");

        public static DiagnosticDescriptor SP0035 { get; } = new DiagnosticDescriptor("SP0035", "Transaction scope recommendation", "Multiple database operations detected. Consider wrapping in transaction for consistency.", "Sqlx", DiagnosticSeverity.Info, true, "Multiple database operations should be wrapped in transactions to ensure data consistency and atomicity.");

        public static DiagnosticDescriptor SP0036 { get; } = new DiagnosticDescriptor("SP0036", "Index usage hint", "Query on column '{0}' may benefit from database index. Consider adding index for better performance.", "Sqlx", DiagnosticSeverity.Info, true, "Queries on frequently searched columns should have appropriate database indexes for optimal performance.");

        public static DiagnosticDescriptor SP0037 { get; } = new DiagnosticDescriptor("SP0037", "Result caching suggestion", "Frequently accessed query '{0}' may benefit from caching. Consider implementing result caching.", "Sqlx", DiagnosticSeverity.Info, true, "Frequently executed queries with stable results should be cached to improve application performance.");

        public static DiagnosticDescriptor SP0038 { get; } = new DiagnosticDescriptor("SP0038", "Async/await pattern", "Synchronous database call detected. Consider using async pattern for better scalability.", "Sqlx", DiagnosticSeverity.Warning, true, "Synchronous database calls can block threads. Use async patterns for better application scalability and responsiveness.");

        public static DiagnosticDescriptor SP0039 { get; } = new DiagnosticDescriptor("SP0039", "Connection pooling optimization", "Consider optimizing database connection usage. Use connection pooling and dispose patterns correctly.", "Sqlx", DiagnosticSeverity.Info, true, "Proper connection management is crucial for performance. Ensure connections are properly disposed and pooled.");

        public static DiagnosticDescriptor SP0040 { get; } = new DiagnosticDescriptor("SP0040", "Query complexity warning", "Query complexity is high. Consider breaking into simpler operations or using stored procedures.", "Sqlx", DiagnosticSeverity.Warning, true, "Complex queries can be hard to maintain and optimize. Consider simplifying or using stored procedures for complex business logic.");
    }
}
