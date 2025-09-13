// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlCompositionIntegrationTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class ExpressionToSqlCompositionIntegrationTests : CodeGenerationTestBase
{
    [TestMethod]
    public void ExpressionToSql_Composition_Methods_And_Clauses_Appear_In_Generated_Source()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface IItemService
    {
        [Sqlx]
        IList<Item> Query([ExpressionToSql] ExpressionToSql<Item> filter);
    }

    [RepositoryFor(typeof(IItemService))]
    public partial class ItemRepository : IItemService
    {
        private readonly DbConnection connection;
        public ItemRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var generated = GetCSharpGeneratedOutput(source);

        // Assert - builder methods present
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> Where("));
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> And("));
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> OrderBy<"));
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> OrderByDescending<"));
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> Take("));
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> Skip("));
        Assert.IsTrue(generated.Contains("public ExpressionToSql<T> Set<TValue>("));

        // Assert - clause builders present
        Assert.IsTrue(generated.Contains("public string ToSql()"));
        Assert.IsTrue(generated.Contains("public string ToWhereClause()"));
        Assert.IsTrue(generated.Contains("public string ToAdditionalClause()"));

        // Assert - SQL text patterns embedded in generator
        Assert.IsTrue(generated.Contains(" ORDER BY "));
        Assert.IsTrue(generated.Contains(" OFFSET "));
        Assert.IsTrue(generated.Contains(" LIMIT "));
        Assert.IsTrue(generated.Contains(" UPDATE "));
        Assert.IsTrue(generated.Contains(" SET "));
    }
}


