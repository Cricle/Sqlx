// -----------------------------------------------------------------------
// <copyright file="SqlGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for SQL generation functionality.
/// </summary>
[TestClass]
public class SqlGeneratorTests
{
    /// <summary>
    /// Tests that the SQL execute types enum has all expected values defined.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_EnumValues_Defined()
    {
        // Test that the SQL execute types enum has the expected values
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 0)); // Select
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 1)); // Update
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 2)); // Insert
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 3)); // Delete
    }
}
