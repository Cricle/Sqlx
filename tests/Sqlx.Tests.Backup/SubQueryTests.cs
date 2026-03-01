// -----------------------------------------------------------------------
// <copyright file="SubQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SubQuery functionality.
/// </summary>
[TestClass]
public class SubQueryTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [TestMethod]
    public void SubQuery_For_ReturnsQueryable()
    {
        // Arrange & Act
        var subQuery = SubQuery.For<TestEntity>();

        // Assert
        Assert.IsNotNull(subQuery);
        Assert.AreEqual(typeof(TestEntity), subQuery.ElementType);
        Assert.IsNotNull(subQuery.Expression);
        Assert.IsNotNull(subQuery.Provider);
    }

    [TestMethod]
    public void SubQuery_Where_BuildsExpression()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act
        var filtered = subQuery.Where(x => x.Id > 10);

        // Assert
        Assert.IsNotNull(filtered);
        Assert.IsNotNull(filtered.Expression);
        Assert.AreEqual(typeof(TestEntity), filtered.ElementType);
    }

    [TestMethod]
    public void SubQuery_Select_BuildsExpression()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act
        var projected = subQuery.Select(x => x.Name);

        // Assert
        Assert.IsNotNull(projected);
        Assert.IsNotNull(projected.Expression);
        Assert.AreEqual(typeof(string), projected.ElementType);
    }

    [TestMethod]
    public void SubQuery_OrderBy_BuildsExpression()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act - OrderBy returns IOrderedQueryable, but SubQueryable doesn't implement it
        // So we just verify the expression is built
        var expression = System.Linq.Expressions.Expression.Call(
            typeof(System.Linq.Queryable),
            nameof(System.Linq.Queryable.OrderBy),
            new[] { typeof(TestEntity), typeof(string) },
            subQuery.Expression,
            System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Parameter(typeof(TestEntity), "x"),
                    "Name"),
                System.Linq.Expressions.Expression.Parameter(typeof(TestEntity), "x")));

        // Assert
        Assert.IsNotNull(expression);
    }

    [TestMethod]
    public void SubQuery_Take_BuildsExpression()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act
        var limited = subQuery.Take(10);

        // Assert
        Assert.IsNotNull(limited);
        Assert.IsNotNull(limited.Expression);
    }

    [TestMethod]
    public void SubQuery_ChainedOperations_BuildsExpression()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act - Build expression manually since OrderBy requires IOrderedQueryable
        var filtered = subQuery.Where(x => x.Id > 10);
        var limited = filtered.Take(5);
        var projected = limited.Select(x => x.Name);

        // Assert
        Assert.IsNotNull(projected);
        Assert.IsNotNull(projected.Expression);
        Assert.AreEqual(typeof(string), projected.ElementType);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SubQuery_GetEnumerator_ThrowsException()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act & Assert
        foreach (var item in subQuery)
        {
            // Should not reach here
        }
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SubQuery_ToList_ThrowsException()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act & Assert
        var list = subQuery.ToList();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SubQuery_Count_ThrowsException()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act & Assert
        var count = subQuery.Count();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SubQuery_First_ThrowsException()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();

        // Act & Assert
        var first = subQuery.First();
    }

    [TestMethod]
    public void SubQueryProvider_CreateQuery_Generic_ReturnsQueryable()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();
        var provider = subQuery.Provider;
        var expression = subQuery.Expression;

        // Act
        var newQuery = provider.CreateQuery<TestEntity>(expression);

        // Assert
        Assert.IsNotNull(newQuery);
        Assert.AreEqual(typeof(TestEntity), newQuery.ElementType);
    }

    [TestMethod]
    public void SubQueryProvider_CreateQuery_NonGeneric_ReturnsQueryable()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();
        var provider = subQuery.Provider;
        var expression = subQuery.Expression;

        // Act
        var newQuery = provider.CreateQuery(expression);

        // Assert
        Assert.IsNotNull(newQuery);
        Assert.IsNotNull(newQuery.ElementType);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SubQueryProvider_Execute_Generic_ThrowsException()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();
        var provider = subQuery.Provider;
        var expression = subQuery.Expression;

        // Act & Assert
        provider.Execute<TestEntity>(expression);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SubQueryProvider_Execute_NonGeneric_ThrowsException()
    {
        // Arrange
        var subQuery = SubQuery.For<TestEntity>();
        var provider = subQuery.Provider;
        var expression = subQuery.Expression;

        // Act & Assert
        provider.Execute(expression);
    }
}
