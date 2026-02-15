# Requirements Document

## Introduction

This feature provides a high-performance, AOT-compatible SQL builder for safely constructing dynamic SQL queries from fragments. It leverages existing Sqlx infrastructure (PlaceholderContext, SqlTemplate, IParameterBinder) and uses ArrayPool for memory efficiency. The builder is designed for interoperability scenarios where SQL needs to be dynamically assembled from external sources while maintaining injection safety.

## Glossary

- **SqlBuilder**: The fluent API component for safely concatenating SQL fragments (reuses existing Sqlx infrastructure)
- **SQL_Fragment**: A validated SQL string segment with associated parameters
- **PlaceholderContext**: Existing Sqlx component providing dialect and metadata (reused)
- **ArrayPool**: .NET's ArrayPool<char> for efficient memory management
- **Safe_Concatenation**: Combining SQL fragments while preventing injection via parameterization

## Requirements

### Requirement 1: Safe SQL Fragment Creation

**User Story:** As a developer, I want to create SQL fragments from strings, so that I can safely build dynamic SQL queries.

#### Acceptance Criteria

1. WHEN a developer creates a SQL fragment from a literal string, THE SqlBuilder SHALL store it as a safe fragment
2. WHEN a developer creates a SQL fragment with parameters, THE SqlBuilder SHALL use existing IParameterBinder infrastructure
3. THE SqlBuilder SHALL reuse existing PlaceholderContext for dialect information
4. THE SqlBuilder SHALL support all database dialects via existing SqlDefine infrastructure

### Requirement 2: Parameter Management

**User Story:** As a developer, I want to safely add parameters to SQL fragments, so that I can prevent SQL injection attacks.

#### Acceptance Criteria

1. WHEN a parameter is added, THE SqlBuilder SHALL generate unique parameter names using existing naming conventions
2. WHEN multiple fragments are combined, THE SqlBuilder SHALL merge parameter dictionaries efficiently
3. WHEN a parameter value is null, THE SqlBuilder SHALL handle it according to SQL NULL semantics
4. THE SqlBuilder SHALL use existing TypeConverter infrastructure for type handling

### Requirement 3: SQL Fragment Concatenation

**User Story:** As a developer, I want to concatenate SQL fragments safely, so that I can build complex queries from smaller parts.

#### Acceptance Criteria

1. WHEN two SQL fragments are concatenated, THE SqlBuilder SHALL use ArrayPool<char> for efficient string building
2. WHEN fragments are concatenated with whitespace, THE SqlBuilder SHALL ensure proper spacing
3. WHEN an empty fragment is concatenated, THE SqlBuilder SHALL skip it without allocation
4. THE SqlBuilder SHALL minimize allocations by reusing pooled arrays

### Requirement 4: Dialect-Aware Formatting

**User Story:** As a developer, I want SQL fragments to respect database dialects, so that generated SQL works across different databases.

#### Acceptance Criteria

1. WHEN a SQL fragment is created, THE SqlBuilder SHALL use existing dialect infrastructure (SqlDefine)
2. WHEN identifiers need quoting, THE SqlBuilder SHALL use dialect.QuoteIdentifier()
3. WHEN boolean values are used, THE SqlBuilder SHALL use dialect.FormatBoolean()
4. THE SqlBuilder SHALL leverage all existing dialect methods without duplication

### Requirement 5: Integration with Existing Sqlx Infrastructure

**User Story:** As a developer, I want the SQL builder to integrate seamlessly with existing Sqlx components, so that I can use it alongside current features.

#### Acceptance Criteria

1. WHEN a SQL fragment is built, THE SqlBuilder SHALL produce output compatible with DbConnection.ExecuteAsync()
2. WHEN parameters are bound, THE SqlBuilder SHALL use the same Dictionary<string, object?> format as existing code
3. THE SqlBuilder SHALL work with existing DbExecutor and DbBatchExecutor
4. THE SqlBuilder SHALL be usable in both synchronous and asynchronous contexts

### Requirement 6: Fluent API Design

**User Story:** As a developer, I want a fluent API for building SQL, so that I can write readable and maintainable query construction code.

#### Acceptance Criteria

1. THE SqlBuilder SHALL provide method chaining for all operations
2. WHEN building complex queries, THE SqlBuilder SHALL support conditional fragment inclusion via Append() overloads
3. WHEN adding fragments, THE SqlBuilder SHALL provide helper methods (AppendWhere, AppendJoin, AppendOrderBy)
4. THE SqlBuilder SHALL provide Build() method returning (string sql, Dictionary<string, object?> parameters)

### Requirement 7: Validation and Error Handling

**User Story:** As a developer, I want clear error messages when SQL construction fails, so that I can quickly identify and fix issues.

#### Acceptance Criteria

1. WHEN parameter name conflicts occur, THE SqlBuilder SHALL throw ArgumentException with clear message
2. WHEN null dialect is provided, THE SqlBuilder SHALL throw ArgumentNullException
3. WHEN Build() is called on empty builder, THE SqlBuilder SHALL return empty string and empty parameters
4. THE SqlBuilder SHALL validate inputs at method call time (fail-fast)

### Requirement 8: Performance and Memory Efficiency

**User Story:** As a developer, I want SQL building to be performant, so that it doesn't impact application performance.

#### Acceptance Criteria

1. THE SqlBuilder SHALL use ArrayPool<char> for all string building operations
2. WHEN building large queries, THE SqlBuilder SHALL minimize allocations via pooled buffers
3. WHEN disposing, THE SqlBuilder SHALL return rented arrays to the pool
4. THE SqlBuilder SHALL be fully AOT-compatible with zero reflection (struct-based design)
5. THE SqlBuilder SHALL implement IDisposable to ensure proper cleanup of pooled resources

### Requirement 9: Interoperability with External Systems

**User Story:** As a developer, I want to safely integrate SQL from external sources, so that I can build queries from configuration or user-defined templates.

#### Acceptance Criteria

1. WHEN receiving SQL fragments from external sources, THE SqlBuilder SHALL validate and sanitize inputs
2. WHEN combining external SQL with internal queries, THE SqlBuilder SHALL ensure parameter safety
3. THE SqlBuilder SHALL provide methods to append raw SQL (AppendRaw) with clear security warnings
4. THE SqlBuilder SHALL provide methods to append parameterized SQL (Append with parameters)

### Requirement 10: SqlTemplate Integration

**User Story:** As a developer, I want to use SqlTemplate placeholders in SqlBuilder, so that I can leverage existing template functionality.

#### Acceptance Criteria

1. WHEN appending a SqlTemplate string, THE SqlBuilder SHALL process placeholders using existing PlaceholderProcessor
2. WHEN using {{columns}}, {{table}}, {{values}} placeholders, THE SqlBuilder SHALL resolve them using PlaceholderContext
3. THE SqlBuilder SHALL support all existing placeholder types (columns, table, values, set, where, if, var)
4. THE SqlBuilder SHALL merge SqlTemplate parameters with builder parameters

### Requirement 11: Subquery Support

**User Story:** As a developer, I want to nest SqlBuilder instances as subqueries, so that I can build complex queries compositionally.

#### Acceptance Criteria

1. WHEN appending another SqlBuilder as a subquery, THE SqlBuilder SHALL merge parameters from both builders
2. WHEN nesting SqlBuilders, THE SqlBuilder SHALL wrap subquery SQL in parentheses
3. WHEN parameter names conflict, THE SqlBuilder SHALL rename parameters to avoid conflicts
4. THE SqlBuilder SHALL support unlimited nesting depth
