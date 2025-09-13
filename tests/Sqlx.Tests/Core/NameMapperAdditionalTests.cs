// -----------------------------------------------------------------------
// <copyright file="NameMapperAdditionalTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class NameMapperAdditionalTests
{
    [TestMethod]
    public void MapNameToSnakeCase_Handles_SpecialChars_Lowercases_All()
    {
        Assert.AreEqual("@id", Sqlx.NameMapper.MapNameToSnakeCase("@Id"));
        Assert.AreEqual("#ver1", Sqlx.NameMapper.MapNameToSnakeCase("#Ver1"));
    }

    [TestMethod]
    public void MapName_MixedCase_To_SnakeCase()
    {
        Assert.AreEqual("user_name", Sqlx.NameMapper.MapName("UserName"));
        Assert.AreEqual("http_response_code", Sqlx.NameMapper.MapName("HttpResponseCode"));
        Assert.AreEqual("xml_id", Sqlx.NameMapper.MapName("XmlId"));
    }
}


