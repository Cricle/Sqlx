// -----------------------------------------------------------------------
// <copyright file="NameMapperNullArgumentTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class NameMapperNullArgumentTests
{
    [TestMethod]
    public void MapName_Null_Throws()
    {
        Assert.ThrowsException<System.ArgumentNullException>(() => Sqlx.NameMapper.MapName(null!));
    }

    [TestMethod]
    public void MapNameToSnakeCase_Null_Throws()
    {
        Assert.ThrowsException<System.ArgumentNullException>(() => Sqlx.NameMapper.MapNameToSnakeCase(null!));
    }
}


