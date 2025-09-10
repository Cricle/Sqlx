# 📋 Changelog

All notable changes to Sqlx will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### 🆕 Added
- **ADO.NET BatchCommand Support** - Native batch operations using `SqlExecuteTypes.BatchCommand`
  - High-performance bulk insert/update/delete operations
  - Automatic parameter binding for collection entities
  - Support for both sync and async operations
  - Transaction support and null value handling
  
- **ExpressionToSql Modulo Operator** - Complete arithmetic operations support
  - Added `%` (modulo) operator support in LINQ expressions
  - All arithmetic operators: `+`, `-`, `*`, `/`, `%`
  - Works in WHERE clauses, ORDER BY, and SET operations
  - Support for all numeric data types

### 🚀 Improved
- **CI/CD Pipeline Optimization**
  - Consolidated 3 workflow files into 1 unified `ci-cd.yml`
  - NuGet publishing only triggered by `v*` tags (e.g., `v1.0.0`)
  - Added NuGet package caching for faster builds
  - Automatic GitHub Release creation
  - Environment protection for production deployments

- **Code Quality Enhancements**
  - 41% reduction in BatchCommand implementation code (71→42 lines)
  - 37% reduction in example code (68→43 lines)
  - Improved error handling and validation
  - Better code organization and documentation

### 🧪 Testing
- **Comprehensive Unit Tests** - 12 new test methods added
  - 6 BatchCommand tests covering all scenarios
  - 6 ExpressionToSql modulo operator tests
  - Edge case handling and error condition testing
  - Complex entity and transaction support testing

### 📚 Documentation
- **New Quick Start Guide** - `docs/NEW_FEATURES_QUICK_START.md`
  - Simple examples for both new features
  - Best practices and performance considerations
  - Installation and upgrade instructions
  
- **Optimization Summary** - `docs/OPTIMIZATION_SUMMARY.md`
  - Detailed breakdown of all improvements
  - Performance metrics and code quality indicators
  - Technical debt reduction summary

### 🔧 Technical Improvements
- Simplified parameter generation logic
- Better null reference handling
- Improved code comments and organization
- Arithmetic operators properly categorized in generated code
- More efficient batch command parameter binding

## Usage Examples

### BatchCommand
```csharp
[SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
Task<int> BatchInsertAsync(IEnumerable<Product> products);

// Usage
var count = await service.BatchInsertAsync(products);
```

### Modulo Operator
```csharp
var evenRecords = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Id % 2 == 0)  // New modulo support
    .OrderBy(u => u.Name);
```

### Release Process
```bash
# Create and push version tag to trigger NuGet publish
git tag v1.0.0
git push origin v1.0.0
```

---

## Previous Releases

### [1.0.0] - Initial Release
- Basic CRUD operations with source generation
- Repository pattern implementation
- Multiple database dialect support
- ExpressionToSql for dynamic queries
- Comprehensive documentation and examples
