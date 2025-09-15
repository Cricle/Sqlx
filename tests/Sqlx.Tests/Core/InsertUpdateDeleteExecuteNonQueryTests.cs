// -----------------------------------------------------------------------
// <copyright file="InsertUpdateDeleteExecuteNonQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests to ensure INSERT, UPDATE, DELETE operations use ExecuteNonQuery instead of ExecuteScalar.
/// </summary>
[TestClass]
public class InsertUpdateDeleteExecuteNonQueryTests : CodeGenerationTestBase
{
    [TestMethod]
    public void InsertOperationWithIntReturn_ShouldUseExecuteNonQuery()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(User))]
public partial class UserRepository
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
    public partial int InsertUser(User user);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}";

        // Act
        var result = Compile(source);

        // Assert - Should use ExecuteNonQuery, not ExecuteScalar
        Assert.IsTrue(result.Contains("ExecuteNonQuery"));
        Assert.IsFalse(result.Contains("ExecuteScalar"));
        Assert.IsTrue(result.Contains("return __methodResult__") || result.Contains("return __result__"));
    }

    [TestMethod]
    public void BooleanReturnInsert_ShouldUseExecuteNonQuery()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(User))]
public partial class UserRepository
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
    public partial bool InsertUser(User user);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}";

        // Act
        var result = Compile(source);

        // Check for any variable declarations that might be early
        System.Console.WriteLine("=== Generated Code Debug ===");
        System.Console.WriteLine(result);
        System.Console.WriteLine("=== End Generated Code ===");
        
        var lines = result.Split('\n');
        var suspiciousDeclarations = new System.Collections.Generic.List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Look for various patterns of early variable declarations
            if (line.Contains("__cmd__") && (line.Contains("= null") || line.Contains("= default")))
            {
                suspiciousDeclarations.Add($"Line {i+1}: {line}");
            }
            if (line.Contains("__result__") && (line.Contains("= null") || line.Contains("= default")))
            {
                suspiciousDeclarations.Add($"Line {i+1}: {line}");
            }
            if (line.Contains("__exception__") && (line.Contains("= null") || line.Contains("= default")))
            {
                suspiciousDeclarations.Add($"Line {i+1}: {line}");
            }
            if (line.Contains("totalAffected") && line.Contains("= 0;") && !line.Contains("+="))
            {
                suspiciousDeclarations.Add($"Line {i+1}: {line}");
            }
        }
        
        if (suspiciousDeclarations.Count > 0)
        {
            var suspiciousText = string.Join("\n", suspiciousDeclarations);
            Assert.Fail($"Found potential early variable declarations:\n{suspiciousText}\n\nFull generated code:\n{result}");
        }

        // Assert - Should use ExecuteNonQuery and convert to bool
        Assert.IsTrue(result.Contains("ExecuteNonQuery"));
        Assert.IsFalse(result.Contains("ExecuteScalar"));
        Assert.IsTrue(result.Contains("return __methodResult__ > 0") || result.Contains("return __result__ > 0")); // Boolean conversion from affected rows
    }

    [TestMethod]
    public void UpdateOperationWithIntReturn_ShouldUseExecuteNonQuery()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(User))]
public partial class UserRepository
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    [SqlExecuteType(SqlExecuteTypes.Update, ""Users"")]
    public partial int UpdateUser(User user);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}";

        // Act
        var result = Compile(source);

        // Assert
        Assert.IsTrue(result.Contains("ExecuteNonQuery"));
        Assert.IsFalse(result.Contains("ExecuteScalar"));
        Assert.IsTrue(result.Contains("return __methodResult__") || result.Contains("return __result__"));
    }

    [TestMethod]
    public void DeleteOperationWithIntReturn_ShouldUseExecuteNonQuery()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(User))]
public partial class UserRepository
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    [SqlExecuteType(SqlExecuteTypes.Delete, ""Users"")]
    public partial int DeleteUser(int id);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}";

        // Act
        var result = Compile(source);

        // Assert
        Assert.IsTrue(result.Contains("ExecuteNonQuery"));
        Assert.IsFalse(result.Contains("ExecuteScalar"));
        Assert.IsTrue(result.Contains("return __methodResult__") || result.Contains("return __result__"));
    }

    [TestMethod]
    public void TrueScalarOperationWithCount_ShouldUseExecuteScalar()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Data.Common;

[RepositoryFor(typeof(User))]
public partial class UserRepository
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
    
    [Sqlx(""SELECT COUNT(*) FROM Users"")]
    public partial int CountUsers();
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}";

        // Act
        var result = Compile(source);

        // Assert - True scalar operations should still use ExecuteScalar
        Assert.IsTrue(result.Contains("ExecuteScalar"));
        Assert.IsFalse(result.Contains("ExecuteNonQuery"));
    }
}
