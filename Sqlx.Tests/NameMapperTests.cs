// -----------------------------------------------------------------------
// <copyright file="NameMapperTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class NameMapperTests
{
    [DataTestMethod]
    [DataRow("personId", "person_id")]
    [DataRow("name", "name")]
    [DataRow("Name", "name")]
    [DataRow("PersonId", "person_id")]
    public void MyTestMethod(string parameterName, string expectedStoredProcedureParameter)
    {
        var storedProcedureParameter = NameMapper.MapName(parameterName);

        Assert.AreEqual(expectedStoredProcedureParameter, storedProcedureParameter);
    }
}
