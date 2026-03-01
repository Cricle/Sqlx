# Test Duplication Analysis Report

**Generated**: 2026-02-28 22:08:30
**Total Test Files Analyzed**: 141

## Duplicate File Groups

### Dialect Tests (10 files)
- `DB2OracleDialectTests.cs` (514 lines)
- `DialectComprehensiveTests.cs` (799 lines)
- `DialectSpecificE2ETests.cs` (437 lines)
- `DialectSpecificTests.cs` (398 lines)
- `DialectWhereClauseTests.cs` (315 lines)
- `SetExpressionDialectTests.cs` (367 lines)
- `SetPlaceholderDialectTests.cs` (161 lines)
- `SetPlaceholderInlineDialectTests.cs` (232 lines)
- `SqlDialectBoundaryTests.cs` (361 lines)
- `SqlDialectTests.cs` (295 lines)

**Merge Target**: `Placeholders/DialectTests.cs` or appropriate category

### DynamicUpdate Tests (3 files)
- `DynamicUpdateAdvancedTests.cs` (679 lines)
- `DynamicUpdateTests.cs` (187 lines)
- `DynamicUpdateWithAnyPlaceholderTests.cs` (462 lines)

**Merge Target**: `Placeholders/DynamicUpdateTests.cs` or appropriate category

### ExpressionBlockResult Tests (3 files)
- `ExpressionBlockResultAnyPlaceholderAdvancedTests.cs` (699 lines)
- `ExpressionBlockResultAnyPlaceholderTests.cs` (309 lines)
- `ExpressionBlockResultTests.cs` (352 lines)

**Merge Target**: `Placeholders/ExpressionBlockResultTests.cs` or appropriate category

### Performance Tests (2 files)
- `ConcurrencyAndPerformanceTests.cs` (352 lines)
- `PerformanceOptimizationTests.cs` (948 lines)

**Merge Target**: `Placeholders/PerformanceTests.cs` or appropriate category

### Placeholder Tests (13 files)
- `ArgPlaceholderHandlerTests.cs` (239 lines)
- `ArgPlaceholderTests.cs` (160 lines)
- `IfPlaceholderHandlerTests.cs` (372 lines)
- `PlaceholderContextTests.cs` (155 lines)
- `PlaceholderEdgeCaseTests.cs` (418 lines)
- `PlaceholderGenerationValidationTests.cs` (530 lines)
- `PlaceholderHandlerBoundaryTests.cs` (560 lines)
- `PlaceholderProcessorTests.cs` (132 lines)
- `PlaceholderTests.cs` (610 lines)
- `TablePlaceholderTests.cs` (1265 lines)
- `VarPlaceholderHandlerTests.cs` (414 lines)
- `WhereObjectPlaceholderTests.cs` (528 lines)
- `WherePlaceholderHandlerTests.cs` (339 lines)

**Merge Target**: `Placeholders/PlaceholderTests.cs` or appropriate category

### ResultReader Tests (3 files)
- `DynamicResultReaderTests.cs` (751 lines)
- `ResultReaderStrictTests.cs` (475 lines)
- `ResultReaderTests.cs` (489 lines)

**Merge Target**: `Placeholders/ResultReaderTests.cs` or appropriate category

### SetExpression Tests (5 files)
- `SetExpressionEdgeCaseTests.cs` (532 lines)
- `SetExpressionExtensionsTests.cs` (214 lines)
- `SetExpressionFunctionOutputTests.cs` (166 lines)
- `SetExpressionFunctionTests.cs` (328 lines)
- `SetExpressionIntegrationTests.cs` (219 lines)

**Merge Target**: `Placeholders/SetExpressionTests.cs` or appropriate category

### SetPlaceholder Tests (8 files)
- `LimitOffsetPlaceholderHandlerTests.cs` (262 lines)
- `SetPlaceholderExpressionTests.cs` (193 lines)
- `SetPlaceholderHandlerTests.cs` (331 lines)
- `SetPlaceholderInlineEdgeCaseTests.cs` (309 lines)
- `SetPlaceholderInlineIntegrationTests.cs` (239 lines)
- `SetPlaceholderInlineTests.cs` (171 lines)
- `SetPlaceholderStrictTests.cs` (549 lines)
- `SetPlaceholderUpdateScenarioTests.cs` (461 lines)

**Merge Target**: `Placeholders/SetPlaceholderTests.cs` or appropriate category

### SourceGenerator Tests (3 files)
- `SourceGeneratorAutoDiscoveryTests.cs` (420 lines)
- `SourceGeneratorOptimizationTests.cs` (401 lines)
- `SourceGeneratorTests.cs` (347 lines)

**Merge Target**: `Placeholders/SourceGeneratorTests.cs` or appropriate category

### SqlxQueryable Tests (5 files)
- `SqlxQueryableE2ETests.cs` (378 lines)
- `SqlxQueryableExecutionTests.cs` (456 lines)
- `SqlxQueryableRemovedMethodsTests.cs` (156 lines)
- `SqlxQueryableStrictTests.cs` (1106 lines)
- `SqlxQueryableTests.cs` (1013 lines)

**Merge Target**: `Placeholders/SqlxQueryableTests.cs` or appropriate category

### SubQuery Tests (4 files)
- `JoinAliasAndFromSubQueryTests.cs` (450 lines)
- `SqlxJoinOrderBySubQueryTests.cs` (540 lines)
- `SubQueryComprehensiveTests.cs` (1212 lines)
- `SubQueryTests.cs` (226 lines)

**Merge Target**: `Placeholders/SubQueryTests.cs` or appropriate category

### ValuesPlaceholder Tests (6 files)
- `ValuesPlaceholderDialectTests.cs` (136 lines)
- `ValuesPlaceholderHandlerTests.cs` (298 lines)
- `ValuesPlaceholderInlineEdgeCaseTests.cs` (447 lines)
- `ValuesPlaceholderInlineIntegrationTests.cs` (411 lines)
- `ValuesPlaceholderInlineTests.cs` (355 lines)
- `ValuesPlaceholderParamTests.cs` (108 lines)

**Merge Target**: `Placeholders/ValuesPlaceholderTests.cs` or appropriate category

## Summary Statistics

- **Duplicate Groups Identified**: 12
- **Total Files in Duplicate Groups**: 65
- **Duplication Rate**: 46.1%
- **Potential File Reduction**: 53 files

## File Merge Mapping

### Dialect Group → `Dialects/DialectTests.cs`

Source files to merge:- DB2OracleDialectTests.cs
- DialectComprehensiveTests.cs
- DialectSpecificE2ETests.cs
- DialectSpecificTests.cs
- DialectWhereClauseTests.cs
- SetExpressionDialectTests.cs
- SetPlaceholderDialectTests.cs
- SetPlaceholderInlineDialectTests.cs
- SqlDialectBoundaryTests.cs
- SqlDialectTests.cs

### ExpressionBlockResult Group → `Expressions/ExpressionBlockResultTests.cs`

Source files to merge:- ExpressionBlockResultAnyPlaceholderAdvancedTests.cs
- ExpressionBlockResultAnyPlaceholderTests.cs
- ExpressionBlockResultTests.cs

### Performance Group → `Integration/PerformanceTests.cs`

Source files to merge:- ConcurrencyAndPerformanceTests.cs
- PerformanceOptimizationTests.cs

### ResultReader Group → `EntityProvider/ResultReaderTests.cs`

Source files to merge:- DynamicResultReaderTests.cs
- ResultReaderStrictTests.cs
- ResultReaderTests.cs

### SetPlaceholder Group → `Placeholders/SetPlaceholderTests.cs`

Source files to merge:- LimitOffsetPlaceholderHandlerTests.cs
- SetPlaceholderExpressionTests.cs
- SetPlaceholderHandlerTests.cs
- SetPlaceholderInlineEdgeCaseTests.cs
- SetPlaceholderInlineIntegrationTests.cs
- SetPlaceholderInlineTests.cs
- SetPlaceholderStrictTests.cs
- SetPlaceholderUpdateScenarioTests.cs

### SqlxQueryable Group → `QueryBuilder/SqlxQueryableTests.cs`

Source files to merge:- SqlxQueryableE2ETests.cs
- SqlxQueryableExecutionTests.cs
- SqlxQueryableRemovedMethodsTests.cs
- SqlxQueryableStrictTests.cs
- SqlxQueryableTests.cs

### SubQuery Group → `QueryBuilder/SubQueryTests.cs`

Source files to merge:- JoinAliasAndFromSubQueryTests.cs
- SqlxJoinOrderBySubQueryTests.cs
- SubQueryComprehensiveTests.cs
- SubQueryTests.cs

### TypeConverter Group → `Core/TypeConverterTests.cs`

Source files to merge:- TypeConverterAdvancedTests.cs

### ValuesPlaceholder Group → `Placeholders/ValuesPlaceholderTests.cs`

Source files to merge:- ValuesPlaceholderDialectTests.cs
- ValuesPlaceholderHandlerTests.cs
- ValuesPlaceholderInlineEdgeCaseTests.cs
- ValuesPlaceholderInlineIntegrationTests.cs
- ValuesPlaceholderInlineTests.cs
- ValuesPlaceholderParamTests.cs

## Refactoring Priority

Based on file count and estimated complexity:

1. **High Priority** (9+ files)
   - SetPlaceholder tests (9 files)
   
2. **Medium Priority** (5-8 files)
   - ValuesPlaceholder tests (6 files)
   - Dialect tests (6 files)
   
3. **Low Priority** (2-4 files)
   - ExpressionBlockResult tests (3 files)
   - TypeConverter tests (2 files)
   - SubQuery tests (2 files)
   - SqlxQueryable tests (2 files)
   - ResultReader tests (2 files)
   - Performance tests (2 files)

## Next Steps

1. Create test infrastructure (TestBase, helpers)
2. Start with high-priority SetPlaceholder tests
3. Progress through medium and low priority groups
4. Validate after each merge operation
