// <copyright file="SqlBuilderSubquerySafetyTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.Helpers;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.QueryBuilder;

[TestClass]
public class SqlBuilderSubquerySafetyTests
{
    private static PlaceholderContext CreateContext()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int32, false),
        };

        return new PlaceholderContext(SqlDefine.SQLite, "users", columns);
    }

    [TestMethod]
    public void AppendSubquery_WithConflictingGeneratedParameters_KeepsAllValuesDistinct()
    {
        using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
        mainQuery.Append($"SELECT * FROM users WHERE age >= {18} AND score <= {90} AND id IN ");

        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.Append($"SELECT user_id FROM orders WHERE total > {1000}");

        mainQuery.AppendSubquery(subquery);
        mainQuery.Append($" AND status = {"active"}");
        var template = mainQuery.Build();

        Assert.IsTrue(template.Sql.Contains("age >= @p0"));
        Assert.IsTrue(template.Sql.Contains("score <= @p1"));
        Assert.IsTrue(template.Sql.Contains("total > @p2"));
        Assert.IsTrue(template.Sql.Contains("status = @p3"));

        Assert.AreEqual(4, template.Parameters.Count);
        SqlAssertions.AssertParametersContain(template.Parameters, "p0", 18);
        SqlAssertions.AssertParametersContain(template.Parameters, "p1", 90);
        SqlAssertions.AssertParametersContain(template.Parameters, "p2", 1000);
        SqlAssertions.AssertParametersContain(template.Parameters, "p3", "active");
    }

    [TestMethod]
    public void AppendSubquery_WithParameterPrefixTokens_RewritesWholeTokensOnly()
    {
        using var mainQuery = new SqlBuilder(CreateContext());
        mainQuery.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE id <> @p1 AND id IN ",
            new Dictionary<string, object?> { ["p1"] = -1 });

        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.AppendRaw("SELECT id FROM orders WHERE ");
        for (var i = 0; i <= 10; i++)
        {
            if (i > 0)
            {
                subquery.AppendRaw(" AND ");
            }

            subquery.AppendRaw($"col{i} = ");
            subquery.Append($"{i}");
        }

        mainQuery.AppendSubquery(subquery);
        var template = mainQuery.Build();

        Assert.IsTrue(template.Sql.Contains("col10 = @p11"), template.Sql);
        Assert.IsFalse(template.Sql.Contains("@p20"), template.Sql);
        SqlAssertions.AssertParametersContain(template.Parameters, "p1", -1);
        SqlAssertions.AssertParametersContain(template.Parameters, "p11", 10);
    }

    [TestMethod]
    public void AppendSubquery_DoesNotRewriteParametersInsideStringLiterals()
    {
        using var mainQuery = new SqlBuilder(CreateContext());
        mainQuery.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE id <> @p1 AND id IN ",
            new Dictionary<string, object?> { ["p1"] = -1 });

        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.AppendRaw("SELECT id FROM orders WHERE note = '@p1 literal' AND first = ");
        subquery.Append($"{10}");
        subquery.AppendRaw(" AND second = ");
        subquery.Append($"{20}");

        mainQuery.AppendSubquery(subquery);
        var template = mainQuery.Build();

        Assert.IsTrue(template.Sql.Contains("'@p1 literal'"), template.Sql);
        Assert.IsTrue(template.Sql.Contains("second = @p2"), template.Sql);
        SqlAssertions.AssertParametersContain(template.Parameters, "p2", 20);
    }

    [TestMethod]
    public void AppendSubquery_WithMismatchedDialect_ThrowsInvalidOperationException()
    {
        using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
        using var subquery = new SqlBuilder(SqlDefine.MySql);

        subquery.Append($"SELECT id FROM orders WHERE total > {100}");

        Assert.ThrowsException<InvalidOperationException>(() => mainQuery.AppendSubquery(subquery));
    }
}
