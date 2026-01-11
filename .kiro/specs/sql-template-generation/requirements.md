# Requirements Document

## Introduction

本功能旨在为 Sqlx 框架添加基于返回类型的 SQL 生成能力。当 Repository 方法返回 SqlTemplate 类型时，系统只生成 SQL 和参数而不执行查询；当返回其他类型时，系统正常执行查询。这提供了一种简单直观的方式来调试和检查生成的 SQL。

## Glossary

- **SqlTemplate**: 表示可重用的 SQL 模板，包含 SQL 字符串和参数字典
- **Repository**: 使用 Sqlx 生成器创建的数据访问层接口
- **Source_Generator**: Roslyn 源代码生成器，在编译时生成代码
- **Return_Type**: 方法的返回类型，用于决定是生成 SQL 还是执行查询
- **Execution_Mode**: 执行模式，根据返回类型决定是否执行数据库操作

## Requirements

### Requirement 1: 基于返回类型的行为切换

**User Story:** 作为开发者，我想要通过方法返回类型来控制是生成 SQL 还是执行查询，以便我可以用简单直观的方式调试 SQL。

#### Acceptance Criteria

1. WHEN Repository 方法返回 SqlTemplate 类型时，THE System SHALL 只生成 SQL 和参数，不执行数据库操作
2. WHEN Repository 方法返回其他类型时，THE System SHALL 正常执行数据库查询并返回结果
3. THE Source_Generator SHALL 根据返回类型生成不同的实现代码
4. THE System SHALL 使用相同的 SQL 生成逻辑，无论返回类型是什么
5. THE System SHALL 保持方法参数的一致性，无论返回类型是什么

### Requirement 2: 支持 SqlTemplate 返回类型

**User Story:** 作为开发者，我想要在 Repository 接口中定义返回 SqlTemplate 的方法，以便我可以获取生成的 SQL 而不执行。

#### Acceptance Criteria

1. THE Source_Generator SHALL 识别返回 SqlTemplate 类型的方法
2. WHEN 方法返回 SqlTemplate 时，THE Generated_Code SHALL 创建包含 SQL 和参数的 SqlTemplate 对象
3. THE Generated_Code SHALL 将方法参数转换为参数字典
4. THE Generated_Code SHALL 不包含数据库连接或执行逻辑
5. THE Generated_Code SHALL 返回完整的 SqlTemplate 对象

### Requirement 3: 保持现有执行行为

**User Story:** 作为开发者，我想要现有的非 SqlTemplate 返回类型的方法保持原有行为，以便不影响现有代码。

#### Acceptance Criteria

1. WHEN 方法返回实体类型时，THE System SHALL 执行查询并返回实体对象
2. WHEN 方法返回集合类型时，THE System SHALL 执行查询并返回集合
3. WHEN 方法返回 void 或 Task 时，THE System SHALL 执行命令操作
4. THE System SHALL 保持现有方法的所有功能和行为
5. THE System SHALL 不修改现有的代码生成逻辑

### Requirement 4: 复用 SQL 生成逻辑

**User Story:** 作为开发者，我想要 SqlTemplate 返回方法和执行方法使用相同的 SQL 生成逻辑，以便保证 SQL 的一致性。

#### Acceptance Criteria

1. THE System SHALL 使用相同的 SqlGenerator 类生成 SQL
2. THE System SHALL 使用相同的 GenerateContext 类管理上下文
3. THE System SHALL 使用相同的方言配置生成 SQL
4. THE Generated_SQL SHALL 在两种模式下完全一致
5. THE System SHALL 不创建重复的 SQL 生成代码

### Requirement 5: 支持所有数据库方言

**User Story:** 作为开发者，我想要 SqlTemplate 返回方法支持所有数据库方言，以便我可以为不同数据库调试 SQL。

#### Acceptance Criteria

1. THE System SHALL 使用 Repository 配置的 SqlDialect
2. THE System SHALL 正确生成列名包装符（如 []、``、""）
3. THE System SHALL 正确生成参数前缀（如 @、$、:）
4. THE Generated_SQL SHALL 符合目标数据库的语法规范
5. THE System SHALL 支持所有现有的数据库方言

### Requirement 6: 类型安全

**User Story:** 作为开发者，我想要 SqlTemplate 返回方法保持类型安全，以便在编译时发现错误。

#### Acceptance Criteria

1. THE Generated_Method SHALL 保持与接口定义相同的参数类型
2. WHEN 参数类型不匹配时，THE Compiler SHALL 报告编译错误
3. THE System SHALL 正确处理可空类型参数
4. THE System SHALL 正确处理复杂类型参数（如对象、集合）
5. THE System SHALL 将参数转换为字典格式存储在 SqlTemplate 中

### Requirement 7: 简单实现

**User Story:** 作为开发者，我想要生成的代码简单易懂，以便我可以轻松理解实现逻辑。

#### Acceptance Criteria

1. THE Generated_Code SHALL 只包含必要的 SQL 生成和参数处理逻辑
2. THE Generated_Code SHALL 使用清晰的变量命名
3. THE Generated_Code SHALL 包含适当的注释
4. THE Generated_Code SHALL 遵循现有的代码风格
5. THE Generated_Code SHALL 最小化复杂度

### Requirement 8: 向后兼容

**User Story:** 作为开发者，我想要新功能不影响现有代码，以便我可以逐步采用新功能。

#### Acceptance Criteria

1. THE System SHALL 保持现有方法的签名和行为不变
2. THE System SHALL 不修改现有的 SqlTemplate 类
3. THE System SHALL 不修改现有的 SQL 生成逻辑
4. THE New_Feature SHALL 作为可选功能添加
5. THE System SHALL 保持与现有测试的兼容性
