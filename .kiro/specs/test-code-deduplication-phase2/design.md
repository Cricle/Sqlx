# 设计文档：测试代码去重 - 第二阶段

## 概述

本设计文档描述了如何通过创建测试辅助类、拆分大型测试文件、提取公共断言逻辑和优化测试设置代码来进一步减少测试代码的重复性（从41%降低到更低水平）。

## 架构

### 整体策略

1. **测试辅助层** - 创建可重用的测试工具类
2. **测试拆分** - 将大型测试文件按功能域拆分
3. **断言抽象** - 提取常见的验证逻辑
4. **设置优化** - 减少重复的初始化代码

### 目录结构

```
tests/Sqlx.Tests/
├── Helpers/
│   ├── TestEntityFactory.cs      # 测试实体工厂
│   ├── SqlAssertions.cs          # SQL断言辅助类
│   └── TestDataBuilder.cs        # 测试数据构建器
├── QueryBuilder/
│   ├── SqlBuilderBasicTests.cs   # 基础功能测试（从SqlBuilderTests拆分）
│   ├── SqlBuilderParameterTests.cs # 参数化测试
│   ├── SqlBuilderPlaceholderTests.cs # 占位符测试
│   └── SqlBuilderDialectTests.cs # 方言特定测试
├── Placeholders/
│   ├── ValuesPlaceholderBasicTests.cs # 基础测试（从ValuesPlaceholderTests拆分）
│   ├── ValuesPlaceholderDialectTests.cs # 方言测试
│   └── ValuesPlaceholderEdgeCaseTests.cs # 边界情况测试
└── ...
```

## 组件和接口

### 1. TestEntityFactory

提供创建常用测试实体的工厂方法。

```csharp
namespace Sqlx.Tests.Helpers;

/// <summary>
/// Factory for creating test entities with default or custom values.
/// </summary>
public static class TestEntityFactory
{
    /// <summary>
    /// Creates a TestEntity with default values.
    /// </summary>
    public static TestEntity CreateTestEntity(
        int? id = null,
        string? userName = null,
        bool? isActive = null,
        DateTime? createdAt = null)
    {
        return new TestEntity
        {
            Id = id ?? 1,
            UserName = userName ?? "test_user",
            IsActive = isActive ?? true,
            CreatedAt = createdAt ?? DateTime.Now
        };
    }

    /// <summary>
    /// Creates multiple TestEntity instances.
    /// </summary>
    public static TestEntity[] CreateTestEntities(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestEntity(id: i, userName: $"user{i}"))
            .ToArray();
    }

    /// <summary>
    /// Creates a TestUser with default values.
    /// </summary>
    public static TestUser CreateTestUser(
        int? id = null,
        string? name = null,
        string? email = null)
    {
        return new TestUser
        {
            Id = id ?? 1,
            Name = name ?? "Test User",
            Email = email ?? "test@example.com"
        };
    }

    // Additional factory methods for other common test entities...
}
```

### 2. SqlAssertions

提供SQL相关的自定义断言方法。

```csharp
namespace Sqlx.Tests.Helpers;

/// <summary>
/// Custom assertions for SQL-related validations.
/// </summary>
public static class SqlAssertions
{
    /// <summary>
    /// Asserts that SQL contains expected text (case-insensitive).
    /// </summary>
    public static void AssertSqlContains(string actualSql, string expectedFragment)
    {
        Assert.IsTrue(
            actualSql.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase),
            $"Expected SQL to contain '{expectedFragment}' but got: {actualSql}");
    }

    /// <summary>
    /// Asserts that SQL matches expected pattern with normalized whitespace.
    /// </summary>
    public static void AssertSqlEquals(string actualSql, string expectedSql)
    {
        var normalizedActual = NormalizeSql(actualSql);
        var normalizedExpected = NormalizeSql(expectedSql);
        
        Assert.AreEqual(normalizedExpected, normalizedActual,
            $"SQL mismatch.\nExpected: {expectedSql}\nActual: {actualSql}");
    }

    /// <summary>
    /// Asserts that parameters contain expected key-value pairs.
    /// </summary>
    public static void AssertParametersContain(
        IDictionary<string, object?> parameters,
        string key,
        object? expectedValue)
    {
        Assert.IsTrue(parameters.ContainsKey(key),
            $"Parameters should contain key '{key}'");
        Assert.AreEqual(expectedValue, parameters[key],
            $"Parameter '{key}' should equal '{expectedValue}'");
    }

    /// <summary>
    /// Asserts that SQL is properly parameterized (no inline values).
    /// </summary>
    public static void AssertSqlIsParameterized(string sql, int expectedParameterCount)
    {
        var parameterMatches = Regex.Matches(sql, @"@p\d+");
        Assert.AreEqual(expectedParameterCount, parameterMatches.Count,
            $"Expected {expectedParameterCount} parameters but found {parameterMatches.Count}");
    }

    private static string NormalizeSql(string sql)
    {
        return Regex.Replace(sql.Trim(), @"\s+", " ");
    }
}
```

### 3. TestDataBuilder

提供流畅的API来构建复杂的测试数据。

```csharp
namespace Sqlx.Tests.Helpers;

/// <summary>
/// Builder for creating complex test data with fluent API.
/// </summary>
public class TestDataBuilder
{
    private readonly List<TestEntity> _entities = new();
    private int _nextId = 1;

    public TestDataBuilder WithEntity(Action<TestEntity>? configure = null)
    {
        var entity = TestEntityFactory.CreateTestEntity(id: _nextId++);
        configure?.Invoke(entity);
        _entities.Add(entity);
        return this;
    }

    public TestDataBuilder WithEntities(int count)
    {
        for (int i = 0; i < count; i++)
        {
            WithEntity();
        }
        return this;
    }

    public TestEntity[] Build()
    {
        return _entities.ToArray();
    }
}
```

## 数据模型

无新的数据模型，使用现有的测试实体。

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: 测试辅助类不改变测试行为

*对于任何*使用测试辅助类重构的测试，重构前后的测试结果应该相同（通过或失败状态一致）

**验证: 需求 1.1, 1.2, 1.3**

### 属性 2: 拆分后的测试覆盖率保持不变

*对于任何*被拆分的大型测试文件，拆分后所有测试方法的总数应该等于拆分前的测试方法数

**验证: 需求 2.1, 2.3**

### 属性 3: 自定义断言等价于原始断言

*对于任何*使用自定义断言替换的原始断言，两者应该在相同输入下产生相同的验证结果

**验证: 需求 3.1, 3.2, 3.3**

### 属性 4: 测试独立性保持

*对于任何*优化后的测试，它应该能够独立运行而不依赖其他测试的执行顺序或状态

**验证: 需求 4.3**

### 属性 5: 所有测试通过

*对于*整个测试套件，重构后应该有3,316个测试通过，0个失败

**验证: 需求 5.1, 5.2, 5.3**

## 错误处理

1. **测试失败** - 如果重构导致测试失败，立即回滚并分析原因
2. **编译错误** - 确保所有重构后的代码能够编译通过
3. **测试数量不匹配** - 如果拆分后测试数量不对，检查是否有遗漏

## 测试策略

### 单元测试

- 验证TestEntityFactory创建的实体具有正确的默认值
- 验证SqlAssertions的断言逻辑正确
- 验证TestDataBuilder构建的数据符合预期

### 集成测试

- 在实际测试中使用新的辅助类，确保它们能正常工作
- 验证拆分后的测试文件能够独立运行
- 运行完整的测试套件，确保所有3,316个测试通过

### 测试配置

- 使用现有的test.runsettings配置
- 确保所有测试都能在CI/CD环境中运行
- 最小100次迭代（如果有属性测试）

## 实施计划

### 阶段 1: 创建测试辅助类（优先级：高）

1. 创建 `tests/Sqlx.Tests/Helpers/` 目录
2. 实现 TestEntityFactory
3. 实现 SqlAssertions
4. 实现 TestDataBuilder
5. 编写单元测试验证辅助类功能

### 阶段 2: 拆分大型测试文件（优先级：高）

按以下顺序拆分：

1. **SqlBuilderTests.cs** (1726行) → 拆分为4个文件
   - SqlBuilderBasicTests.cs - 基础功能
   - SqlBuilderParameterTests.cs - 参数化
   - SqlBuilderPlaceholderTests.cs - 占位符
   - SqlBuilderDialectTests.cs - 方言特定

2. **ValuesPlaceholderTests.cs** (1517行) → 拆分为3个文件
   - ValuesPlaceholderBasicTests.cs - 基础功能
   - ValuesPlaceholderDialectTests.cs - 方言测试
   - ValuesPlaceholderEdgeCaseTests.cs - 边界情况

3. **TablePlaceholderTests.cs** (1265行) → 拆分为3个文件
   - TablePlaceholderBasicTests.cs
   - TablePlaceholderDialectTests.cs
   - TablePlaceholderEdgeCaseTests.cs

### 阶段 3: 应用测试辅助类（优先级：中）

1. 在新拆分的测试文件中使用TestEntityFactory
2. 使用SqlAssertions替换重复的断言代码
3. 在需要复杂测试数据的地方使用TestDataBuilder

### 阶段 4: 验证和优化（优先级：高）

1. 运行所有测试，确保3,316个测试通过
2. 测量代码重复率，确认降低
3. 清理临时文件和分析脚本
4. 更新文档

## 性能考虑

- 测试辅助类应该是轻量级的，不增加测试执行时间
- 拆分后的测试文件应该能够并行执行
- 避免在辅助类中引入复杂的逻辑

## 安全考虑

- 测试数据不应包含真实的敏感信息
- 确保测试辅助类不会意外修改全局状态

## 可维护性

- 测试辅助类应该有清晰的文档和示例
- 拆分后的测试文件应该有明确的命名和职责
- 保持测试代码的简洁性和可读性
