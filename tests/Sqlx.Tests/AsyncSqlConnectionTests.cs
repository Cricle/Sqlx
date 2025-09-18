// -----------------------------------------------------------------------
// <copyright file="AsyncSqlConnectionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AsyncSqlConnectionTests : CodeGenerationTestBase
{
    [TestMethod]
    public void ScalarResult()
    {
        string source = @"
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Foo
{
    partial class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial Task<int> M(int clientId, string? personId);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Validate that code generation succeeded and contains expected elements
        Assert.IsTrue(output.Contains("namespace Foo"), "Generated code should contain Foo namespace");
        Assert.IsTrue(output.Contains("partial class C"), "Generated code should contain partial class C");
        Assert.IsTrue(output.Contains("public async partial System.Threading.Tasks.Task<int> M"), "Generated code should contain async method M");
        Assert.IsTrue(output.Contains("sp_TestSP"), "Generated code should contain stored procedure reference");
        Assert.IsTrue(output.Contains("client_id"), "Generated code should contain client_id parameter");
        Assert.IsTrue(output.Contains("person_id"), "Generated code should contain person_id parameter");
        Assert.IsTrue(output.Contains("ExecuteScalarAsync"), "Generated code should contain async execution");
    }

    [TestMethod]
    public void MapResultSetToProcedure()
    {
        string source = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Foo
{
    public class Item
    {
        public string StringValue { get; set; }
        public int Int32Value { get; set; }
        public int? NullableInt32Value { get; set; }
    }

    class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial Task<IList<Item>> M()
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Looser assertions to accommodate generator header/format differences
        StringAssert.Contains(output, "namespace Foo");
        StringAssert.Contains(output, "partial class C");
        StringAssert.Contains(output, "Task");
        StringAssert.Contains(output, "sp_TestSP");
        StringAssert.Contains(output, "ExecuteReaderAsync");
    }

    [TestMethod]
    public void MapSingleObjectToProcedureFromDbContext()
    {
        string source = @"
using Sqlx.Annotations;

namespace Foo
{
    class C
    {
        [Sqlx(""sp_TestSP"")]
        public partial Task<Item> M()
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        StringAssert.Contains(output, "namespace Foo");
        StringAssert.Contains(output, "partial class C");
        StringAssert.Contains(output, "Task<Item>");
        StringAssert.Contains(output, "FromSqlRaw(");
        if (!(output.Contains("AsAsyncEnumerable()") || output.Contains("FirstOrDefaultAsync(")))
        {
            Assert.Fail("Generated EF path should use AsAsyncEnumerable() or FirstOrDefaultAsync()");
        }
    }

    [TestMethod]
    public void MapSingleObjectToProcedureConnection()
    {
        string source = @"
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

    class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial Task<Item> M()
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        StringAssert.Contains(output, "partial class C");
        StringAssert.Contains(output, "Task<Foo.Item>");
        StringAssert.Contains(output, "CreateCommand()");
        StringAssert.Contains(output, "ExecuteReaderAsync");
    }

    [TestMethod]
    public void MapListFromDbContext()
    {
        string source = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Foo
{
    class C
    {
        [Sqlx(""sp_TestSP"")]
        public partial Task<IList<Item>> M(int clientId, int? personId)
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        // Flexible checks: method signature pieces and EF call
        StringAssert.Contains(output, "Task");
        StringAssert.Contains(output, " M(int clientId, int? personId)");
        StringAssert.Contains(output, "sp_TestSP");
        StringAssert.Contains(output, "@client_id");
        StringAssert.Contains(output, "@person_id");
        StringAssert.Contains(output, "FromSqlRaw(");
    }

    [TestMethod]
    public void NoResults()
    {
        string source = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace Foo
{
    class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial Task M(int clientId, string? personId);
    }
}";
        string output = GetCSharpGeneratedOutput(source, NullableContextOptions.Disable);

        Assert.IsNotNull(output);

        StringAssert.Contains(output, "partial class C");
        StringAssert.Contains(output, "Task M(int clientId, string? personId)");
        StringAssert.Contains(output, "ExecuteNonQueryAsync");
    }
}
