// -----------------------------------------------------------------------
// <copyright file="CancellationTokenTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

[TestClass]
public class CancellationTokenTests : CodeGenerationTestBase
{
    [TestMethod]
    [Ignore("Temporarily disabled due to source generator issues with async Task<T> methods")]
    public void ScalarResult()
    {
        string source = @"
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Foo
{
    partial class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial Task<int> M(int clientId, string? personId, CancellationToken cancellationToken);
    }
}";
        VerifyCSharp(source, NullableContextOptions.Disable);
    }

    [TestMethod]
    [Ignore("Temporarily disabled due to source generator issues with async Task methods")]
    public void NoResults()
    {
        string source = @"
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Foo
{
    partial class C
    {
        private DbConnection connection;

        [Sqlx(""sp_TestSP"")]
        public partial Task M(int clientId, string? personId, CancellationToken cancellationToken);
    }
}";
        VerifyCSharp(source, NullableContextOptions.Disable);
    }

    [TestMethod]
    [Ignore("Temporarily disabled due to source generator issues with async Task<T> methods")]
    public void MapResultSetToProcedure()
    {
        string source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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
        public partial Task<IList<Item>> M(CancellationToken cancellationToken);
    }
}";
        VerifyCSharp(source, NullableContextOptions.Disable);
    }

    [TestMethod]
    public void MapListFromDbContext()
    {
        string source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Foo
{
    public class Item { }
    
    partial class C
    {
        private DbConnection connection;
        
        [Sqlx(""sp_TestSP"")]
        public partial Task<IList<Item>> M(int clientId, int? personId, CancellationToken cancellationToken);
    }
}";
        VerifyCSharp(source, NullableContextOptions.Disable);
    }
}
