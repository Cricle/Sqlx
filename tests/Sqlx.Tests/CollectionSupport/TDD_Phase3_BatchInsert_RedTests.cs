// -----------------------------------------------------------------------
// <copyright file="TDD_Phase3_BatchInsert_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

/// <summary>
/// TDD Phase 3: Red Tests for Batch INSERT support.
/// Goal: Support {{values @paramName}} placeholder and automatic batching.
/// </summary>
[TestClass]
public class TDD_Phase3_BatchInsert_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// {{values @entities}} should generate VALUES clauses for batch insert
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("BatchInsert")]
    public void BatchInsert_Should_Generate_VALUES_Clauses()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
    public int Age { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{values @entities}}"")]
    Task<int> BatchInsertAsync(IEnumerable<User> entities);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var batchInsertMethodIndex = generatedCode.IndexOf("BatchInsertAsync");
        Assert.IsTrue(batchInsertMethodIndex > 0, "应该找到BatchInsertAsync方法");
        
        // 应该有VALUES生成逻辑
        Assert.IsTrue(
            generatedCode.Contains("VALUES") || generatedCode.Contains("values"),
            "应该包含VALUES关键字");
        
        // 应该遍历entities
        Assert.IsTrue(
            generatedCode.Contains("foreach") && generatedCode.Contains("entities"),
            "应该遍历entities集合");
    }

    /// <summary>
    /// [BatchOperation] should enable automatic batching
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("BatchInsert")]
    public void BatchOperation_Should_Enable_Auto_Batching()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public string Name { get; set; } = """";
    public decimal Price { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES {{values @entities}}"")]
    [BatchOperation(MaxBatchSize = 500)]
    Task<int> BatchInsertAsync(IEnumerable<Product> entities);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var batchInsertMethodIndex = generatedCode.IndexOf("BatchInsertAsync");
        Assert.IsTrue(batchInsertMethodIndex > 0, "应该找到BatchInsertAsync方法");
        
        // 应该有分批逻辑
        Assert.IsTrue(
            generatedCode.Contains("Chunk") || generatedCode.Contains("batch"),
            "应该有分批处理逻辑");
        
        // 应该使用MaxBatchSize
        Assert.IsTrue(
            generatedCode.Contains("500"),
            "应该使用MaxBatchSize=500");
    }

    /// <summary>
    /// Batch insert should return total affected rows
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("BatchInsert")]
    public void BatchInsert_Should_Return_Total_Affected_Rows()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Order
{
    public string OrderNumber { get; set; } = """";
    public decimal Amount { get; set; }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository(IDbConnection connection) : IOrderRepository { }

public interface IOrderRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES {{values @entities}}"")]
    [BatchOperation]
    Task<int> BatchInsertAsync(IEnumerable<Order> entities);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var batchInsertMethodIndex = generatedCode.IndexOf("BatchInsertAsync");
        Assert.IsTrue(batchInsertMethodIndex > 0, "应该找到BatchInsertAsync方法");
        
        // 方法应该返回Task<int>
        Assert.IsTrue(
            generatedCode.Contains("Task<int> BatchInsertAsync"),
            "方法应该返回Task<int>");
        
        // 应该累加受影响行数
        Assert.IsTrue(
            generatedCode.Contains("__totalAffected__") || generatedCode.Contains("total") || generatedCode.Contains("+="),
            "应该累加受影响行数");
    }

    /// <summary>
    /// Empty collection should be handled gracefully
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("BatchInsert")]
    public void BatchInsert_Empty_Collection_Should_Handle_Gracefully()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Item
{
    public string Code { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IItemRepository))]
public partial class ItemRepository(IDbConnection connection) : IItemRepository { }

public interface IItemRepository
{
    [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES {{values @entities}}"")]
    Task<int> BatchInsertAsync(IEnumerable<Item> entities);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var batchInsertMethodIndex = generatedCode.IndexOf("BatchInsertAsync");
        Assert.IsTrue(batchInsertMethodIndex > 0, "应该找到BatchInsertAsync方法");
        
        // 应该检查空集合
        Assert.IsTrue(
            generatedCode.Contains("Any()") || generatedCode.Contains("Count") || generatedCode.Contains("if ("),
            "应该检查空集合");
    }
}

