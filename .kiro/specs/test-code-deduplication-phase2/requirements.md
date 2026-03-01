# 需求文档：测试代码去重 - 第二阶段

## 简介

在第一阶段的测试重构后，测试代码仍然存在约41%的重复性。本阶段将通过创建测试辅助类、提取公共逻辑和优化测试结构来进一步减少重复。

## 术语表

- **Test Helper**: 测试辅助类，提供可重用的测试设置和断言方法
- **Test Base Class**: 测试基类，包含通用的测试初始化和清理逻辑
- **Test Factory**: 测试数据工厂，用于创建测试实体
- **Custom Assertion**: 自定义断言方法，封装常见的验证逻辑

## 需求

### 需求 1: 创建测试辅助类

**用户故事:** 作为开发者，我希望有一个测试辅助类来提供常用的测试数据创建方法，以减少重复的测试数据构造代码。

#### 验收标准

1. THE System SHALL create a TestHelper class with factory methods for common test entities
2. THE System SHALL provide methods to create TestEntity, TestUser, and other frequently used test objects
3. THE System SHALL support parameterized creation with optional customization
4. THE System SHALL place the helper class in a Tests/Helpers directory

### 需求 2: 拆分大型测试文件

**用户故事:** 作为开发者，我希望将超过1000行的大型测试文件拆分为更小、更专注的测试类，以提高可维护性。

#### 验收标准

1. WHEN a test file exceeds 1000 lines, THE System SHALL split it into multiple focused test classes
2. THE System SHALL group related tests by functionality or feature
3. THE System SHALL maintain all existing test coverage
4. THE System SHALL update file names to reflect the focused scope

### 需求 3: 提取公共断言逻辑

**用户故事:** 作为开发者，我希望有自定义断言方法来封装常见的验证逻辑，以减少重复的断言代码。

#### 验收标准

1. THE System SHALL create custom assertion methods for frequently used validation patterns
2. THE System SHALL provide assertions for SQL string validation
3. THE System SHALL provide assertions for entity comparison
4. THE System SHALL place custom assertions in a Tests/Assertions directory

### 需求 4: 优化测试设置代码

**用户故事:** 作为开发者，我希望减少重复的测试初始化代码，以提高测试代码的简洁性。

#### 验收标准

1. THE System SHALL identify common test initialization patterns
2. THE System SHALL extract common setup logic to helper methods
3. THE System SHALL maintain test isolation and independence
4. THE System SHALL ensure all tests continue to pass after refactoring

### 需求 5: 验证测试覆盖率

**用户故事:** 作为开发者，我希望确保重构后测试覆盖率不降低，所有测试仍然通过。

#### 验收标准

1. WHEN refactoring is complete, THE System SHALL run all tests
2. THE System SHALL verify that all 3,316 tests pass
3. THE System SHALL confirm no tests were accidentally removed
4. THE System SHALL measure and report code duplication reduction
