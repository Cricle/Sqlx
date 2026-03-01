# Test Code Refactoring Plan

**Created**: 2026-02-28
**Status**: Ready for Execution

## Executive Summary

Based on duplication analysis, we identified:
- **65 files** (46.1% of total) in duplicate groups
- **12 duplicate groups** requiring consolidation
- **Potential reduction**: 53 files (from 141 to ~88 files)

## Refactoring Phases

### Phase 1: Preparation ✅
- [x] Create backup
- [x] Establish baseline metrics
- [x] Analyze code duplication
- [x] Generate refactoring plan

### Phase 2: Infrastructure Creation
**Goal**: Create reusable test helpers and base classes

**Tasks**:
1. Create `TestHelpers/` directory
2. Implement `TestBase.cs` - base class for all tests
3. Implement `SqlAssertions.cs` - SQL assertion helpers
4. Implement `TestDataBuilder.cs` - fluent test data builders
5. Implement `PlaceholderTestHelper.cs` - placeholder test utilities
6. Implement `DatabaseTestHelper.cs` - database test utilities

**Estimated Impact**: Foundation for all subsequent refactoring

### Phase 3: High-Priority Merges
**Goal**: Consolidate the most duplicated test groups

#### 3.1 Placeholder Tests (13 files → 1 file)
**Target**: `Placeholders/PlaceholderTests.cs`

**Source Files**:
- ArgPlaceholderHandlerTests.cs (239 lines)
- ArgPlaceholderTests.cs (160 lines)
- IfPlaceholderHandlerTests.cs (372 lines)
- PlaceholderContextTests.cs (155 lines)
- PlaceholderEdgeCaseTests.cs (418 lines)
- PlaceholderGenerationValidationTests.cs (530 lines)
- PlaceholderHandlerBoundaryTests.cs (560 lines)
- PlaceholderProcessorTests.cs (132 lines)
- TablePlaceholderTests.cs (1265 lines)
- VarPlaceholderHandlerTests.cs (414 lines)
- WhereObjectPlaceholderTests.cs (528 lines)
- WherePlaceholderHandlerTests.cs (339 lines)

**Total Lines**: 5,112 lines
**Organization Strategy**: Use nested classes for each placeholder type

#### 3.2 Dialect Tests (10 files → 1 file)
**Target**: `Dialects/DialectTests.cs`

**Source Files**:
- DB2OracleDialectTests.cs (514 lines)
- DialectComprehensiveTests.cs (799 lines)
- DialectSpecificTests.cs (398 lines)
- DialectWhereClauseTests.cs (315 lines)
- SetExpressionDialectTests.cs (367 lines)
- SetPlaceholderDialectTests.cs (161 lines)
- SetPlaceholderInlineDialectTests.cs (232 lines)
- SqlDialectBoundaryTests.cs (361 lines)
- SqlDialectTests.cs (295 lines)

**Total Lines**: 3,442 lines
**Organization Strategy**: Parameterized tests for all dialects + specific feature tests

#### 3.3 SetPlaceholder Tests (8 files → 1 file)
**Target**: `Placeholders/SetPlaceholderTests.cs`

**Source Files**:
- SetPlaceholderHandlerTests.cs (331 lines)
- SetPlaceholderExpressionTests.cs (193 lines)
- SetPlaceholderInlineTests.cs (171 lines)
- SetPlaceholderInlineEdgeCaseTests.cs (309 lines)
- SetPlaceholderInlineIntegrationTests.cs (239 lines)
- SetPlaceholderStrictTests.cs (549 lines)
- SetPlaceholderUpdateScenarioTests.cs (461 lines)
- LimitOffsetPlaceholderHandlerTests.cs (262 lines)

**Total Lines**: 2,515 lines
**Organization Strategy**: Nested classes for Handler, Expression, Inline, EdgeCases, Integration, UpdateScenarios

### Phase 4: Medium-Priority Merges

#### 4.1 ValuesPlaceholder Tests (6 files → 1 file)
**Target**: `Placeholders/ValuesPlaceholderTests.cs`

**Source Files**:
- ValuesPlaceholderHandlerTests.cs (298 lines)
- ValuesPlaceholderDialectTests.cs (136 lines)
- ValuesPlaceholderInlineTests.cs (355 lines)
- ValuesPlaceholderInlineEdgeCaseTests.cs (447 lines)
- ValuesPlaceholderInlineIntegrationTests.cs (411 lines)
- ValuesPlaceholderParamTests.cs (108 lines)

**Total Lines**: 1,755 lines

#### 4.2 SqlxQueryable Tests (5 files → 1 file)
**Target**: `QueryBuilder/SqlxQueryableTests.cs`

**Source Files**:
- SqlxQueryableTests.cs (1013 lines)
- SqlxQueryableStrictTests.cs (1106 lines)
- SqlxQueryableExecutionTests.cs (456 lines)
- SqlxQueryableRemovedMethodsTests.cs (156 lines)

**Total Lines**: 2,731 lines

#### 4.3 SetExpression Tests (5 files → 1 file)
**Target**: `Expressions/SetExpressionTests.cs`

**Source Files**:
- SetExpressionExtensionsTests.cs (214 lines)
- SetExpressionFunctionTests.cs (328 lines)
- SetExpressionFunctionOutputTests.cs (166 lines)
- SetExpressionEdgeCaseTests.cs (532 lines)
- SetExpressionIntegrationTests.cs (219 lines)

**Total Lines**: 1,459 lines

### Phase 5: Low-Priority Merges

#### 5.1 SubQuery Tests (4 files → 1 file)
**Target**: `QueryBuilder/SubQueryTests.cs`

**Total Lines**: 2,428 lines

#### 5.2 ExpressionBlockResult Tests (3 files → 1 file)
**Target**: `Expressions/ExpressionBlockResultTests.cs`

**Total Lines**: 1,360 lines

#### 5.3 ResultReader Tests (3 files → 1 file)
**Target**: `EntityProvider/ResultReaderTests.cs`

**Total Lines**: 1,715 lines

#### 5.4 DynamicUpdate Tests (3 files → 1 file)
**Target**: `Core/DynamicUpdateTests.cs`

**Total Lines**: 1,328 lines

#### 5.5 SourceGenerator Tests (3 files → 1 file)
**Target**: `Generators/SourceGeneratorTests.cs`

**Total Lines**: 1,168 lines

#### 5.6 Performance Tests (2 files → 1 file)
**Target**: `Integration/PerformanceTests.cs`

**Total Lines**: 1,300 lines

### Phase 6: File Organization
**Goal**: Move remaining files to appropriate directories

**Categories**:
- `Core/` - Core functionality tests
- `Attributes/` - Attribute tests
- `Context/` - Context tests
- `Integration/` - Integration tests
- `E2E/` - End-to-end tests (keep existing structure)

### Phase 7: Cleanup and Optimization
**Goal**: Remove old files and optimize test performance

**Tasks**:
1. Delete obsolete test files
2. Optimize test fixtures
3. Standardize naming conventions
4. Update test configuration files
5. Generate final metrics report

## Success Metrics

### Target Metrics
- **File Count**: 141 → ~88 files (37% reduction)
- **Code Duplication**: 46% → <15%
- **Test Coverage**: Maintain or improve current level
- **Test Pass Rate**: 100%

### Quality Metrics
- All tests compile and run
- Consistent naming conventions
- Clear test organization
- Comprehensive documentation

## Risk Mitigation

### Backup Strategy
- Full backup created at `tests/Sqlx.Tests.Backup/`
- Incremental validation after each phase
- Rollback capability maintained

### Validation Strategy
- Run tests after each merge
- Compare test counts before/after
- Monitor code coverage
- Use `[Obsolete]` attribute before deletion

## Timeline Estimate

- **Phase 1**: ✅ Complete
- **Phase 2**: 2-3 hours (infrastructure)
- **Phase 3**: 4-6 hours (high-priority merges)
- **Phase 4**: 3-4 hours (medium-priority merges)
- **Phase 5**: 2-3 hours (low-priority merges)
- **Phase 6**: 1-2 hours (organization)
- **Phase 7**: 1-2 hours (cleanup)

**Total Estimated Time**: 13-20 hours

## Next Action

Begin Phase 2: Create test infrastructure by implementing helper classes.

**Command**: Start task 2.1 in tasks.md
