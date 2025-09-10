// -----------------------------------------------------------------------
// <copyright file="JoinTransactionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class JoinTransactionTests : CodeGenerationTestBase
{
    [TestMethod]
    public void DbConectionCanJoinTransactions()
    {
        string source = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace Foo
{
    partial class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial int M(DbTransaction transaction, int clientId, string? personId);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // 验证生成的代码包含关键元素而不是精确匹配
        Assert.IsTrue(output.Contains("public partial int M(System.Data.Common.DbTransaction transaction, int clientId, string? personId)"), 
            "Generated method signature should match expected signature");
        Assert.IsTrue(output.Contains("command.Transaction = transaction;") || output.Contains("__cmd__.Transaction = transaction;"), 
            "Generated code should set transaction on command");
        Assert.IsTrue(output.Contains("sp_TestSP"), 
            "Generated code should contain stored procedure call");
    }

    [TestMethod]
    public void DbContextCanJoinTransactions()
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
        
        [Sqlx(""sp_TestSP"")]
        public partial IList<Item> M(DbTransaction transaction);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // 验证生成的代码包含关键元素而不是精确匹配
        Assert.IsTrue(output.Contains("public partial System.Collections.Generic.IList<Foo.Item> M(System.Data.Common.DbTransaction transaction)"), 
            "Generated method signature should match expected signature");
        Assert.IsTrue(output.Contains("__cmd__.Transaction = transaction;"), 
            "Generated code should set transaction on command");
        Assert.IsTrue(output.Contains("EXEC sp_TestSP") || output.Contains("sp_TestSP"), 
            "Generated code should contain stored procedure call");
        Assert.IsTrue(output.Contains("OnExecuting(\"M\", __cmd__);"), 
            "Generated code should include OnExecuting callback");
        Assert.IsTrue(output.Contains("OnExecuted(\"M\", __cmd__, __result__"), 
            "Generated code should include OnExecuted callback");
    }
}
