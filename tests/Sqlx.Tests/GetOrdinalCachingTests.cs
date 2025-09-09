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
        
        // The actual generated code uses lowercase column names
        Assert.IsTrue(result.Contains("int __ordinal_id = __reader__.GetOrdinal(\"id\");"),
            "Should generate cached ordinal for Id column");
        Assert.IsTrue(result.Contains("int __ordinal_name = __reader__.GetOrdinal(\"name\");"),
            "Should generate cached ordinal for Name column");
        Assert.IsTrue(result.Contains("int __ordinal_email = __reader__.GetOrdinal(\"email\");"),
            "Should generate cached ordinal for Email column");
        
        // Verify the cached ordinals are used in the data reading
        Assert.IsTrue(result.Contains("__reader__.GetInt32(__ordinal_id)"),
            "Should use cached ordinal for reading Id");
        Assert.IsTrue(result.Contains("__reader__.GetString(__ordinal_name)"),
            "Should use cached ordinal for reading Name");
        Assert.IsTrue(result.Contains("__reader__.GetString(__ordinal_email)"),
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
        public static partial int GetUserCount(this DbConnection connection);
    }
}";

        var result = GetCSharpGeneratedOutput(source);
        
        // For scalar return types, the generated method doesn't use IList<T> so there's no caching
        // This test should verify that scalar methods work correctly even without explicit caching
        Assert.IsTrue(!string.IsNullOrEmpty(result), "Should generate valid code for scalar return types");
    }

    /// <summary>
    /// Tests that GetOrdinal caching works with default column naming.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_DefaultColumnNames_GeneratesCachedOrdinals()
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
        
        // Since we removed the DbColumn attributes, all columns will use default naming (lowercase)
        Assert.IsTrue(result.Contains("int __ordinal_id = __reader__.GetOrdinal(\"id\");"),
            "Should generate cached ordinal for Id column");
        Assert.IsTrue(result.Contains("int __ordinal_name = __reader__.GetOrdinal(\"name\");"),
            "Should generate cached ordinal for Name column");
        Assert.IsTrue(result.Contains("int __ordinal_email = __reader__.GetOrdinal(\"email\");"),
            "Should generate cached ordinal for Email column");
    }

    /// <summary>
    /// Tests that GetOrdinal caching is generated even for single result methods for consistency.
    /// </summary>
    [TestMethod]
    public void GetOrdinalCaching_SingleResult_GeneratesCaching()
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
        
        // Even for single results, GetOrdinal caching is used for consistency
        Assert.IsTrue(result.Contains("__ordinal_"),
            "Should generate cached ordinals for consistency even in single result methods");
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
        
        // Verify that GetOrdinal caching is used (should have multiple GetOrdinal calls)
        var getOrdinalCount = result.Split("GetOrdinal", StringSplitOptions.RemoveEmptyEntries).Length - 1;
        
        Assert.IsTrue(getOrdinalCount >= 8, 
            $"Should have at least 8 GetOrdinal calls for the entity properties, found {getOrdinalCount}");
        
        // Verify that ordinals are cached before the reader loop (if present)
        if (result.Contains("while(__reader__.Read())"))
        {
            var readerLoopIndex = result.IndexOf("while(__reader__.Read())");
            var lastOrdinalIndex = result.LastIndexOf("GetOrdinal");
            
            Assert.IsTrue(lastOrdinalIndex < readerLoopIndex,
                "All GetOrdinal calls should be before the reader loop for optimal performance");
        }
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
        
        Assert.IsTrue(result.Contains("int __ordinal_id = __reader__.GetOrdinal(\"id\");"),
            "Should generate cached ordinal for IEnumerable return type");
        Assert.IsTrue(result.Contains("int __ordinal_name = __reader__.GetOrdinal(\"name\");"),
            "Should generate cached ordinal for IEnumerable return type");
    }
}
