// -----------------------------------------------------------------------
// <copyright file="SqlDefineExtensionsTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlDefineExtensionsTests
{
    [TestMethod]
    public void WrapColumns_Wraps_With_Dialect_Symbols()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.WrapAndJoinColumns("Id", "Name");
        Assert.AreEqual("[Id], [Name]", result);
    }

    [TestMethod]
    public void CreateParameters_Joins_With_Prefix()
    {
        var sql = SqlDefine.MySql;
        var result = sql.CreateAndJoinParameters("Id", "Name");
        Assert.AreEqual("@Id, @Name", result);
    }

    [TestMethod]
    public void CreateSetClauses_Builds_Assignments()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.CreateSetClauses("Id", "Name");
        Assert.AreEqual("[Id] = @Id, [Name] = @Name", result);
    }

    [TestMethod]
    public void CreateWhereConditions_Builds_Comparisons()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.CreateWhereConditions("Id", "Name");
        Assert.AreEqual("[Id] = @Id AND [Name] = @Name", result);
    }

    [TestMethod]
    public void UsesParameterPrefix_Detects_Prefix()
    {
        var sql = SqlDefine.PgSql;
        Assert.IsTrue(sql.UsesParameterPrefix("$"));
        Assert.IsFalse(sql.UsesParameterPrefix("@"));
    }

    [TestMethod]
    public void GetEffectiveParameterPrefix_Returns_Prefix()
    {
        var sql = SqlDefine.SQLite;
        var prefix = sql.GetEffectiveParameterPrefix();
        Assert.AreEqual("@", prefix);
    }
}


