// -----------------------------------------------------------------------
// <copyright file="SqlDefineExtensionsIEnumerableTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Collections.Generic;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlDefineExtensionsIEnumerableTests
{
    [TestMethod]
    public void WrapAndJoinColumns_IEnumerable_Works()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.WrapAndJoinColumns(new List<string> { "Id", "Name" });
        Assert.AreEqual("[Id], [Name]", result);
    }

    [TestMethod]
    public void CreateAndJoinParameters_IEnumerable_Works()
    {
        var sql = SqlDefine.MySql;
        var result = sql.CreateAndJoinParameters(new List<string> { "Id", "Name" });
        Assert.AreEqual("@Id, @Name", result);
    }

    [TestMethod]
    public void CreateSetClauses_IEnumerable_Works()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.CreateSetClauses(new List<string> { "Id", "Name" });
        Assert.AreEqual("[Id] = @Id, [Name] = @Name", result);
    }

    [TestMethod]
    public void CreateWhereConditions_IEnumerable_Works()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.CreateWhereConditions(new List<string> { "Id", "Name" });
        Assert.AreEqual("[Id] = @Id AND [Name] = @Name", result);
    }
}


