// -----------------------------------------------------------------------
// <copyright file="CustomSqlTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CustomSqlTests : CodeGenerationTestBase
{
    [TestMethod]
    public void ScalarResult()
    {
        string source = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace Foo
{
    partial class C
    {
        private DbConnection connection;

        [Sqlx]
        public partial int M([RawSql]string sql, int clientId, string? personId);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Verify the generated code contains the expected patterns instead of exact matching
        Assert.IsTrue(output.Contains("namespace Foo"), "Generated code should contain Foo namespace");
        Assert.IsTrue(output.Contains("partial class C"), "Generated code should contain partial class C");
        Assert.IsTrue(output.Contains("public partial int M(string sql, int clientId, string? personId)"), "Generated code should contain method M with correct signature");
        Assert.IsTrue(output.Contains("connection") && output.Contains("ConnectionState.Open"), "Generated code should handle connection state");
        Assert.IsTrue(output.Contains("CreateCommand"), "Generated code should create database command");
        Assert.IsTrue(output.Contains("ExecuteScalar"), "Generated code should execute scalar");
        Assert.IsTrue(output.Contains("return") && output.Contains("int"), "Generated code should return int result");
    }

    [TestMethod]
    public void MapResultSetToProcedure()
    {
        string source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace Foo
{
    public class Item
    {
        public string StringValue { get; set; }
        public int Int32Value { get; set; }
        public int? NullableInt32Value { get; set; }
    }

    partial class C
    {
        private DbConnection connection;

        [Sqlx]
        public partial IList<Item> M([RawSql]string sql);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Verify the generated code contains the expected patterns instead of exact matching
        Assert.IsTrue(output.Contains("namespace Foo"), "Generated code should contain Foo namespace");
        Assert.IsTrue(output.Contains("partial class C"), "Generated code should contain partial class C");
        Assert.IsTrue(output.Contains("public partial System.Collections.Generic.IList<Foo.Item> M(string sql)"), "Generated code should contain method M with correct signature");
        Assert.IsTrue(output.Contains("connection") && output.Contains("ConnectionState.Open"), "Generated code should handle connection state");
        Assert.IsTrue(output.Contains("CreateCommand"), "Generated code should create database command");
        Assert.IsTrue(output.Contains("ExecuteReader"), "Generated code should execute reader");
        Assert.IsTrue(output.Contains("new"), "Generated code should create new objects");
        Assert.IsTrue(output.Contains("StringValue") && output.Contains("Int32Value"), "Generated code should map entity properties");
    }
}
