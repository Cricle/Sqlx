// -----------------------------------------------------------------------
// <copyright file="GetOrdinalCachingTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

/// <summary>
/// Tests for GetOrdinal caching optimization in generated code.
/// </summary>
[TestClass]
public class GetOrdinalCachingTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that GetOrdinal caching is generated for list return types.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_ListReturnType_GeneratesCachedOrdinals()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name, Email FROM Users"")]
        public static partial IList<User> GetUsers(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        Assert.IsTrue(result.Contains("int __ordinal_Id = __reader__.GetOrdinal(\"Id\");"),
            "Should generate cached ordinal for Id column");
        Assert.IsTrue(result.Contains("int __ordinal_Name = __reader__.GetOrdinal(\"Name\");"),
            "Should generate cached ordinal for Name column");
        Assert.IsTrue(result.Contains("int __ordinal_Email = __reader__.GetOrdinal(\"Email\");"),
            "Should generate cached ordinal for Email column");
        
        // Verify the cached ordinals are used in the data reading
        Assert.IsTrue(result.Contains("__reader__.GetInt32(__ordinal_Id)"),
            "Should use cached ordinal for reading Id");
        Assert.IsTrue(result.Contains("__reader__.GetString(__ordinal_Name)"),
            "Should use cached ordinal for reading Name");
        Assert.IsTrue(result.Contains("__reader__.GetString(__ordinal_Email)"),
            "Should use cached ordinal for reading Email");
    }

    /// <summary>
    /// Tests that GetOrdinal caching works with tuple return types.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_TupleReturnType_GeneratesCachedOrdinals()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users"")]
        public static partial IList<(int Id, string Name)> GetUserTuples(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        Assert.IsTrue(result.Contains("int __ordinal_Id = __reader__.GetOrdinal(\"Id\");"),
            "Should generate cached ordinal for tuple Id field");
        Assert.IsTrue(result.Contains("int __ordinal_Name = __reader__.GetOrdinal(\"Name\");"),
            "Should generate cached ordinal for tuple Name field");
        
        // Verify the cached ordinals are used in tuple construction
        Assert.IsTrue(result.Contains("__reader__.GetInt32(__ordinal_Id)"),
            "Should use cached ordinal for reading tuple Id");
        Assert.IsTrue(result.Contains("__reader__.GetString(__ordinal_Name)"),
            "Should use cached ordinal for reading tuple Name");
    }

    /// <summary>
    /// Tests that GetOrdinal caching works with scalar return types.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_ScalarReturnType_GeneratesCachedOrdinal()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public static partial class TestClass
    {
        [Sqlx(""SELECT COUNT(*) FROM Users"")]
        public static partial IList<int> GetUserCounts(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        Assert.IsTrue(result.Contains("int __ordinal_Column0 = __reader__.GetOrdinal(\"Column0\");"),
            "Should generate cached ordinal for scalar column");
        Assert.IsTrue(result.Contains("__reader__.GetInt32(__ordinal_Column0)"),
            "Should use cached ordinal for reading scalar value");
    }

    /// <summary>
    /// Tests that GetOrdinal caching handles custom column names with DbColumn attribute.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_CustomColumnNames_GeneratesCachedOrdinals()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        [DbColumn(""user_id"")]
        public int Id { get; set; }
        
        [DbColumn(""user_name"")]
        public string Name { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT user_id, user_name, Email FROM Users"")]
        public static partial IList<User> GetUsers(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        Assert.IsTrue(result.Contains("int __ordinal_user_id = __reader__.GetOrdinal(\"user_id\");"),
            "Should generate cached ordinal for custom column name user_id");
        Assert.IsTrue(result.Contains("int __ordinal_user_name = __reader__.GetOrdinal(\"user_name\");"),
            "Should generate cached ordinal for custom column name user_name");
        Assert.IsTrue(result.Contains("int __ordinal_Email = __reader__.GetOrdinal(\"Email\");"),
            "Should generate cached ordinal for default column name Email");
    }

    /// <summary>
    /// Tests that GetOrdinal caching is not generated for single result methods.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_SingleResult_DoesNotGenerateCaching()
    {
        var source = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users WHERE Id = @id"")]
        public static partial User GetUser(this DbConnection connection, int id);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        // For single results, we don't need caching since there's only one row
        Assert.IsFalse(result.Contains("__ordinal_"),
            "Should not generate cached ordinals for single result methods");
    }

    /// <summary>
    /// Tests GetOrdinal caching performance characteristics.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_PerformanceCharacteristics_CorrectPattern()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class LargeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT * FROM LargeTable"")]
        public static partial IList<LargeEntity> GetLargeEntities(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        // Count the number of GetOrdinal calls - should be exactly equal to number of properties
        var ordinalCount = result.Split("GetOrdinal").Length - 1;
        var expectedPropertyCount = 8; // Number of properties in LargeEntity
        
        Assert.AreEqual(expectedPropertyCount, ordinalCount,
            $"Should generate exactly {expectedPropertyCount} GetOrdinal calls for caching");
        
        // Verify all ordinals are cached before the reader loop
        var readerLoopIndex = result.IndexOf("while(__reader__.Read())");
        var lastOrdinalIndex = result.LastIndexOf("GetOrdinal");
        
        Assert.IsTrue(lastOrdinalIndex < readerLoopIndex,
            "All GetOrdinal calls should be before the reader loop for optimal performance");
    }

    /// <summary>
    /// Tests that GetOrdinal caching works with IEnumerable return types.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_IEnumerableReturnType_GeneratesCachedOrdinals()
    {
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public static partial class TestClass
    {
        [Sqlx(""SELECT Id, Name FROM Users"")]
        public static partial IEnumerable<User> GetUsers(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        Assert.IsTrue(result.Contains("int __ordinal_Id = __reader__.GetOrdinal(\"Id\");"),
            "Should generate cached ordinal for IEnumerable return type");
        Assert.IsTrue(result.Contains("int __ordinal_Name = __reader__.GetOrdinal(\"Name\");"),
            "Should generate cached ordinal for IEnumerable return type");
    }
}
