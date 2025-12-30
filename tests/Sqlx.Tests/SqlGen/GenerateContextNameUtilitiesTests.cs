// -----------------------------------------------------------------------
// <copyright file="GenerateContextNameUtilitiesTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.SqlGen;

namespace Sqlx.Tests.SqlGen;

[TestClass]
public class GenerateContextNameUtilitiesTests
{
    [TestMethod]
    public void GetColumnName_Converts_Cases_And_Preserves_Underscores()
    {
        Assert.AreEqual("user_name", GenerateContext.GetColumnName("UserName"));
        Assert.AreEqual("user_name", GenerateContext.GetColumnName("userName"));
        Assert.AreEqual("upper_case", GenerateContext.GetColumnName("UPPER_CASE"));
        Assert.AreEqual("", GenerateContext.GetColumnName(""));
    }

    [TestMethod]
    public void GetParamterName_Joins_Prefix_And_ColumnName()
    {
        Assert.AreEqual("@user_name", GenerateContext.GetParamterName("@", "UserName"));
        Assert.AreEqual("$id", GenerateContext.GetParamterName("$", "Id"));
    }
}


