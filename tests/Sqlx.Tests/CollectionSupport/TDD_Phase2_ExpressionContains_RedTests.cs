// -----------------------------------------------------------------------
// <copyright file="TDD_Phase2_ExpressionContains_RedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.CollectionSupport;

/// <summary>
/// TDD Phase 2: Red Tests for Expression Contains support.
/// Goal: Support ids.Contains(x.Property) in Expression parameters, generating IN clauses.
/// </summary>
[TestClass]
public class TDD_Phase2_ExpressionContains_RedTests : CodeGenerationTestBase
{
    /// <summary>
    /// Expression with Contains should generate IN clause
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("Expression-Contains")]
    public void Expression_Contains_Should_Generate_IN_Clause()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }

public interface IUserRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var getWhereMethodIndex = generatedCode.IndexOf("GetWhereAsync");
        Assert.IsTrue(getWhereMethodIndex > 0, "应该找到GetWhereAsync方法");
        
        // 应该使用ExpressionToSql引擎桥接
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<"),
            "应该使用ExpressionToSql引擎");
        
        // 应该有Where方法调用
        Assert.IsTrue(
            generatedCode.Contains(".Where(predicate)"),
            "应该调用Where方法传入predicate");
    }

    /// <summary>
    /// Expression Contains with List should work
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("Expression-Contains")]
    public void Expression_Contains_With_List_Should_Work()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Product
{
    public long Id { get; set; }
    public string Category { get; set; } = """";
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(IDbConnection connection) : IProductRepository { }

public interface IProductRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @filter}}"")]
    Task<List<Product>> FilterAsync(Expression<Func<Product, bool>> filter);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var filterMethodIndex = generatedCode.IndexOf("FilterAsync");
        Assert.IsTrue(filterMethodIndex > 0, "应该找到FilterAsync方法");
        
        // 应该有ExpressionToSql引擎
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<"),
            "应该使用ExpressionToSql引擎");
    }

    /// <summary>
    /// Expression with multiple conditions including Contains should work
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("CollectionSupport")]
    [TestCategory("Expression-Contains")]
    public void Expression_Multiple_Conditions_With_Contains_Should_Work()
    {
        // Arrange
        var source = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Test;

public class Order
{
    public long Id { get; set; }
    public string Status { get; set; } = """";
    public decimal Amount { get; set; }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository(IDbConnection connection) : IOrderRepository { }

public interface IOrderRepository
{
    [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @condition}}"")]
    Task<List<Order>> SearchAsync(Expression<Func<Order, bool>> condition);
}
";

        // Act
        var generatedCode = GetCSharpGeneratedOutput(source);

        // Assert
        var searchMethodIndex = generatedCode.IndexOf("SearchAsync");
        Assert.IsTrue(searchMethodIndex > 0, "应该找到SearchAsync方法");
        
        // 应该使用ExpressionToSql
        Assert.IsTrue(
            generatedCode.Contains("ExpressionToSql<"),
            "应该使用ExpressionToSql引擎");
        
        // 应该绑定参数
        Assert.IsTrue(
            generatedCode.Contains("GetParameters()"),
            "应该绑定Expression生成的参数");
    }
}

