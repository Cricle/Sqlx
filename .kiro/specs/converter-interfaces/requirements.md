# Requirements Document

## Introduction

本功能旨在将Sqlx的代码生成分解为三个独立的可复用组件：SQL模板生成、参数绑定、结果读取。核心设计原则：

- **AOT兼容，无反射**：所有代码在编译时生成，运行时无反射调用
- **高性能**：直接属性访问，缓存列序号，最小化分配
- **使用简单**：用户只需定义接口，源生成器自动生成所有实现
- **编译优先**：占位符系统在编译时处理，运行时只需填充参数执行
- **多数据库支持**：占位符系统支持不同SQL方言

源生成器帮助我们免去手动编写这些逻辑，生成的代码是AOT友好的、高性能的。

## Glossary

- **Code_Generator**: Sqlx源生成器，负责生成Repository实现代码
- **Entity_Provider**: 实体信息提供者，提供实体的列信息、属性映射等元数据（编译时生成）
- **Parameter_Binder**: 参数绑定器，负责将方法参数绑定到DbCommand（编译时生成）
- **Result_Reader**: 结果读取器，负责将DbDataReader转换为目标类型（编译时生成）
- **Placeholder_System**: 占位符系统，在Sqlx库中实现，支持可扩展的占位符处理
- **SqlTemplate**: SQL模板，包含SQL语句和参数字典
- **AOT**: Ahead-of-Time编译，要求无运行时反射

## Requirements

### Requirement 1: IEntityProvider接口定义

**User Story:** As a developer, I want an interface that provides entity metadata without reflection, so that my application is AOT compatible.

#### Acceptance Criteria

1. THE Code_Generator SHALL define an `IEntityProvider<TEntity>` interface in the Sqlx namespace
2. THE IEntityProvider interface SHALL include a property `IReadOnlyList<string> ColumnNames` for column names
3. THE IEntityProvider interface SHALL include a method `void WriteValues(TEntity entity, Action<string, object?> writer)` for iterating property values without reflection
4. THE IEntityProvider interface SHALL include a method `TEntity CreateInstance()` for creating entity instances without reflection
5. THE Code_Generator SHALL generate a concrete implementation for each entity type at compile time
6. THE IEntityProvider interface SHALL NOT include table name (table name is provided externally)

### Requirement 2: IParameterBinder接口定义

**User Story:** As a developer, I want a parameter binding interface that works without reflection, so that parameter binding is fast and AOT compatible.

#### Acceptance Criteria

1. THE Code_Generator SHALL define an `IParameterBinder<TEntity>` interface in the Sqlx namespace
2. THE IParameterBinder interface SHALL include a method `void BindEntity(DbCommand command, TEntity entity)` for entity parameter binding
3. THE IParameterBinder interface SHALL include a method `void BindValue(DbCommand command, string name, object? value, DbType dbType)` for single parameter binding
4. THE generated implementation SHALL use direct property access without reflection
5. THE generated implementation SHALL be AOT compatible

### Requirement 3: IResultReader接口定义

**User Story:** As a developer, I want a result reading interface that maps results without reflection, so that result mapping is fast and AOT compatible.

#### Acceptance Criteria

1. THE Code_Generator SHALL define an `IResultReader<TEntity>` interface in the Sqlx namespace
2. THE IResultReader interface SHALL include synchronous methods `TEntity Read(DbDataReader reader)` and `List<TEntity> ReadAll(DbDataReader reader)`
3. THE IResultReader interface SHALL include asynchronous methods `Task<TEntity> ReadAsync(DbDataReader reader, CancellationToken ct)` and `Task<List<TEntity>> ReadAllAsync(DbDataReader reader, CancellationToken ct)`
4. THE IResultReader interface SHALL include a method `int[] GetOrdinals(DbDataReader reader)` for caching column ordinals
5. THE generated implementation SHALL use direct property assignment without reflection

### Requirement 4: 占位符系统移至Sqlx库

**User Story:** As a developer, I want the placeholder system to be in the Sqlx library, so that it can be extended and reused.

#### Acceptance Criteria

1. THE Sqlx library SHALL include a `PlaceholderProcessor` class for processing SQL templates
2. THE PlaceholderProcessor SHALL support `{{columns}}` placeholder using IEntityProvider.ColumnNames
3. THE PlaceholderProcessor SHALL support `{{values}}` placeholder for INSERT statements
4. THE PlaceholderProcessor SHALL support `{{set}}` placeholder for UPDATE statements
5. THE PlaceholderProcessor SHALL support different SQL dialects (SQLite, MySQL, PostgreSQL, SqlServer)
6. THE PlaceholderProcessor SHALL be extensible to support custom placeholders

### Requirement 5: 编译时代码生成

**User Story:** As a developer, I want all implementations to be generated at compile time, so that there is no runtime reflection.

#### Acceptance Criteria

1. THE Code_Generator SHALL generate `EntityNameEntityProvider` class for each entity type at compile time
2. THE Code_Generator SHALL generate `EntityNameParameterBinder` class for each entity type at compile time
3. THE Code_Generator SHALL generate `EntityNameResultReader` class for each entity type at compile time
4. THE generated code SHALL NOT use System.Reflection namespace
5. THE generated code SHALL NOT use dynamic keyword
6. THE generated code SHALL be compatible with .NET Native AOT

### Requirement 6: 执行方法简化

**User Story:** As a developer, I want the generated execution methods to be simpler and use the generated helpers, so that the code is maintainable.

#### Acceptance Criteria

1. THE generated execution methods SHALL call `GetXxxSql()` to obtain SqlTemplate
2. THE generated execution methods SHALL use generated ParameterBinder for parameter binding
3. THE generated execution methods SHALL use generated ResultReader for result mapping
4. THE generated execution methods SHALL maintain the same public API as before
5. THE generated execution methods SHALL support both synchronous and asynchronous execution

### Requirement 7: 性能优化

**User Story:** As a developer, I want the generated code to be highly performant, so that there is no overhead compared to hand-written code.

#### Acceptance Criteria

1. THE generated ResultReader SHALL cache column ordinals for repeated reads
2. THE generated ParameterBinder SHALL use direct property access
3. THE generated code SHALL minimize object allocations
4. THE generated code SHALL avoid boxing for value types where possible

### Requirement 8: 使用简单

**User Story:** As a developer, I want to use Sqlx without understanding the internal implementation, so that I can focus on business logic.

#### Acceptance Criteria

1. THE user SHALL only need to define repository interface with SqlTemplate attributes
2. THE user SHALL only need to add RepositoryFor attribute to partial class
3. THE Code_Generator SHALL automatically generate all helper classes
4. THE generated code SHALL be invisible to the user unless they want to inspect it
5. THE public API SHALL remain unchanged from current implementation

### Requirement 9: 拦截器和可观测性

**User Story:** As a developer, I want to keep interceptors and observability features, so that I can monitor and debug database operations.

#### Acceptance Criteria

1. THE generated code SHALL preserve OnExecuting partial method for pre-execution hooks
2. THE generated code SHALL preserve OnExecuted partial method for post-execution hooks
3. THE generated code SHALL preserve OnExecuteFail partial method for error handling
4. THE generated code SHALL support OpenTelemetry integration
5. THE interceptors SHALL receive DbCommand and timing information

### Requirement 10: TDD开发模式

**User Story:** As a developer, I want to develop this feature using TDD, so that the code is well-tested and maintainable.

#### Acceptance Criteria

1. THE development SHALL follow TDD (Test-Driven Development) approach
2. THE existing unit tests MAY be removed and rewritten as needed
3. THE new tests SHALL cover all interface methods
4. THE new tests SHALL verify AOT compatibility (no reflection)
5. THE new tests SHALL verify multi-dialect support

### Requirement 11: 占位符系统统一参数

**User Story:** As a developer, I want the placeholder system to have unified parameter handling, so that parameter binding is consistent.

#### Acceptance Criteria

1. THE PlaceholderProcessor SHALL use a unified parameter context for all placeholders
2. THE parameter context SHALL include entity provider, SQL dialect, and parameter prefix
3. THE PlaceholderProcessor SHALL support parameter name generation with consistent naming
4. THE PlaceholderProcessor SHALL handle parameter value extraction from entities
5. THE PlaceholderProcessor SHALL be designed for extensibility with custom placeholder handlers
