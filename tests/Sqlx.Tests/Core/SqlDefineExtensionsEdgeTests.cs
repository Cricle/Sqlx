// -----------------------------------------------------------------------
// <copyright file="SqlDefineExtensionsEdgeTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Collections.Generic;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlDefineExtensionsEdgeTests
{
    [TestMethod]
    public void WrapAndJoinColumns_WithNull_ReturnsEmpty()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.WrapAndJoinColumns((IEnumerable<string>?)null);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void CreateAndJoinParameters_WithNull_ReturnsEmpty()
    {
        var sql = SqlDefine.SqlServer;
        var result = sql.CreateAndJoinParameters((IEnumerable<string>?)null);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void CreateParameter_WithEmptyName_ReturnsPrefixOnly()
    {
        var sql = SqlDefine.MySql;
        var result = sql.CreateParameter("");
        Assert.AreEqual("@", result);
    }

    [TestMethod]
    public void UsesParameterPrefix_Sqlite_Supports_At_And_AtSqlite()
    {
        var sql = SqlDefine.SQLite;
        Assert.IsTrue(sql.UsesParameterPrefix("@"));
        Assert.IsTrue(sql.UsesParameterPrefix("@sqlite"));
    }
}


