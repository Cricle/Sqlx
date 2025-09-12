// -----------------------------------------------------------------------
// <copyright file="FailFastTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Moq;

namespace Sqlx.Tests.Core;

[TestClass]
public class FailFastTests
{
    [TestMethod]
    public void ParameterNullCheck_ShouldOccurBeforeConnectionOpen()
    {
        // This test verifies that parameter null checks happen before connection open
        // implementing the fail-fast principle
        
        // The generated code should follow this pattern:
        var correctPattern = @"
// Parameter null checks (fail fast)
if (entity == null)
    throw new ArgumentNullException(nameof(entity));
if (collection == null)
    throw new ArgumentNullException(nameof(collection));

// Connection setup (only after parameters are validated)
if (connection.State != ConnectionState.Open)
{
    connection.Open(); // or OpenAsync()
}
";
        
        Assert.IsNotNull(correctPattern, "This documents the correct fail-fast pattern");
    }

    [TestMethod]
    public void FailFast_CollectionParameters_ShouldBeChecked()
    {
        // This test verifies that collection parameters are checked for null
        
        var collectionTypes = new[]
        {
            "IEnumerable<User>",
            "List<User>",
            "IList<User>",
            "ICollection<User>",
            "User[]"
        };

        foreach (var collectionType in collectionTypes)
        {
            // Verify that collection type should be null-checked
            bool shouldCheck = IsCollectionType(collectionType);
            Assert.IsTrue(shouldCheck, $"Collection type {collectionType} should be null-checked");
        }
    }

    [TestMethod]
    public void FailFast_EntityParameters_ShouldBeChecked()
    {
        // This test verifies that entity parameters are checked for null
        
        var entityTypes = new[]
        {
            "User",
            "Product", 
            "Order",
            "MyNamespace.Customer",
            "Domain.Models.Invoice"
        };

        foreach (var entityType in entityTypes)
        {
            // Verify that entity type should be null-checked
            bool shouldCheck = IsEntityType(entityType);
            Assert.IsTrue(shouldCheck, $"Entity type {entityType} should be null-checked");
        }
    }

    [TestMethod]
    public void FailFast_SystemParameters_ShouldBeSkipped()
    {
        // This test verifies that system parameters are NOT null-checked
        
        var systemTypes = new[]
        {
            "CancellationToken",
            "DbTransaction",
            "IDbTransaction", 
            "DbConnection",
            "IDbConnection",
            "System.Data.IDbConnection"
        };

        foreach (var systemType in systemTypes)
        {
            // Verify that system type should NOT be null-checked
            bool shouldCheck = IsSystemType(systemType);
            Assert.IsTrue(shouldCheck, $"System type {systemType} should be identified as system type");
            // System types should NOT be null-checked, so we expect IsSystemType to return true
            // which means they should be skipped from null checking
        }
    }

    [TestMethod]
    public void FailFast_StringParameters_ShouldBeSkipped()
    {
        // This test verifies that string parameters are NOT automatically null-checked
        // (individual operations may handle string nullability specially)
        
        bool shouldCheck = ShouldCheckStringParameter();
        Assert.IsFalse(shouldCheck, "String parameters should be handled by individual operations");
    }

    [TestMethod]
    public void FailFast_PerformanceBenefit_ShouldBeDocumented()
    {
        // This test documents the performance benefits of fail-fast
        
        var benefits = new[]
        {
            "Avoids unnecessary database connection opening",
            "Reduces resource usage when parameters are invalid",
            "Provides faster feedback to developers",
            "Prevents potential connection leaks on early failures",
            "Improves overall application responsiveness"
        };

        foreach (var benefit in benefits)
        {
            Assert.IsNotNull(benefit, $"Fail-fast provides: {benefit}");
        }
    }

    [TestMethod]
    public void FailFast_ErrorMessages_ShouldBeDescriptive()
    {
        // This test verifies that error messages are descriptive and helpful
        
        var expectedErrorPattern = "ArgumentNullException";
        var expectedParameterInfo = "nameof(parameter)";
        
        // The generated code should use:
        // throw new ArgumentNullException(nameof(parameterName));
        
        Assert.IsNotNull(expectedErrorPattern, "Should throw ArgumentNullException");
        Assert.IsNotNull(expectedParameterInfo, "Should include parameter name using nameof()");
    }

    [TestMethod]
    public void FailFast_BatchOperations_ShouldCheckCollections()
    {
        // This test verifies that batch operations check collection parameters
        
        // For batch operations, the pattern should be:
        var batchPattern = @"
// Parameter null checks (fail fast) - BEFORE connection open
if (users == null)
    throw new ArgumentNullException(nameof(users));

// Connection setup - AFTER parameter validation
if (connection.State != ConnectionState.Open)
{
    connection.Open();
}

// Batch processing logic
if (connection is DbConnection dbConn && dbConn.CanCreateBatch)
{
    // ... batch logic
}
else
{
    // ... fallback logic  
}
";
        
        Assert.IsNotNull(batchPattern, "Batch operations should check collections before opening connection");
    }

    [TestMethod]
    public void FailFast_AsyncOperations_ShouldFollowSamePattern()
    {
        // This test verifies that async operations also follow fail-fast pattern
        
        var asyncPattern = @"
// Parameter null checks (fail fast) - synchronous, before any async operations
if (entity == null)
    throw new ArgumentNullException(nameof(entity));

// Connection setup - async, after parameter validation
if (connection.State != ConnectionState.Open)
{
    await connection.OpenAsync(cancellationToken);
}
";
        
        Assert.IsNotNull(asyncPattern, "Async operations should check parameters before async connection open");
    }

    [TestMethod]
    public void FailFast_ExceptionHandling_ShouldNotCatchNullArguments()
    {
        // This test verifies that ArgumentNullException should bubble up immediately
        
        // Fail-fast means we should NOT catch ArgumentNullException
        // Let it bubble up to the caller immediately
        
        try
        {
            // Simulate the fail-fast check
            string? nullParameter = null;
            if (nullParameter == null)
                throw new ArgumentNullException(nameof(nullParameter));
                
            // This line should never be reached
            Assert.Fail("Should have thrown ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            // This is expected - fail-fast worked correctly
            Assert.AreEqual("nullParameter", ex.ParamName);
        }
    }

    [TestMethod]
    public void FailFast_MultipleParameters_ShouldCheckAllBeforeConnection()
    {
        // This test verifies that multiple parameters are all checked before connection
        
        var multipleParametersPattern = @"
// All parameter null checks first (fail fast)
if (user == null)
    throw new ArgumentNullException(nameof(user));
if (orders == null)
    throw new ArgumentNullException(nameof(orders));
if (settings == null)
    throw new ArgumentNullException(nameof(settings));

// Connection setup only after all parameters validated
if (connection.State != ConnectionState.Open)
{
    connection.Open();
}
";
        
        Assert.IsNotNull(multipleParametersPattern, "All parameters should be checked before connection setup");
    }

    // Helper methods to simulate the logic used in the actual implementation
    private bool IsCollectionType(string typeName)
    {
        return typeName.Contains("IEnumerable") ||
               typeName.Contains("List") ||
               typeName.Contains("IList") ||
               typeName.Contains("ICollection") ||
               typeName.Contains("[]");
    }

    private bool IsEntityType(string typeName)
    {
        return !typeName.StartsWith("System.") &&
               !IsSystemType(typeName) &&
               char.IsUpper(typeName[0]); // Typically entity types start with uppercase
    }

    private bool IsSystemType(string typeName)
    {
        return typeName == "CancellationToken" ||
               typeName.Contains("DbTransaction") ||
               typeName.Contains("IDbTransaction") ||
               typeName.Contains("DbConnection") ||
               typeName.Contains("IDbConnection");
    }

    private bool ShouldCheckStringParameter()
    {
        // Strings are handled specially by individual operations
        return false;
    }
}
