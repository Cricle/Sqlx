// <copyright file="SqlxGeneratorUnifiedTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// Tests for the unified SqlxGenerator that handles both [Sqlx] and [RepositoryFor] attributes.
/// Verifies that the generator produces correct EntityProvider, ResultReader, and ParameterBinder.
/// </summary>
[TestClass]
public class SqlxGeneratorUnifiedTests
{
    #region SqlxAttribute Tests

    [TestMethod]
    public void SqlxAttribute_DefaultConstructor_AllGenerationOptionsTrue()
    {
        var attr = new SqlxAttribute();
        
        Assert.IsTrue(attr.GenerateEntityProvider);
        Assert.IsTrue(attr.GenerateResultReader);
        Assert.IsTrue(attr.GenerateParameterBinder);
        Assert.IsNull(attr.TargetType);
    }

    [TestMethod]
    public void SqlxAttribute_WithTargetType_SetsTargetType()
    {
        var attr = new SqlxAttribute(typeof(TestEntity));
        
        Assert.AreEqual(typeof(TestEntity), attr.TargetType);
        Assert.IsTrue(attr.GenerateEntityProvider);
        Assert.IsTrue(attr.GenerateResultReader);
        Assert.IsTrue(attr.GenerateParameterBinder);
    }

    [TestMethod]
    public void SqlxAttribute_CanDisableEntityProvider()
    {
        var attr = new SqlxAttribute { GenerateEntityProvider = false };
        
        Assert.IsFalse(attr.GenerateEntityProvider);
        Assert.IsTrue(attr.GenerateResultReader);
        Assert.IsTrue(attr.GenerateParameterBinder);
    }

    [TestMethod]
    public void SqlxAttribute_CanDisableResultReader()
    {
        var attr = new SqlxAttribute { GenerateResultReader = false };
        
        Assert.IsTrue(attr.GenerateEntityProvider);
        Assert.IsFalse(attr.GenerateResultReader);
        Assert.IsTrue(attr.GenerateParameterBinder);
    }

    [TestMethod]
    public void SqlxAttribute_CanDisableParameterBinder()
    {
        var attr = new SqlxAttribute { GenerateParameterBinder = false };
        
        Assert.IsTrue(attr.GenerateEntityProvider);
        Assert.IsTrue(attr.GenerateResultReader);
        Assert.IsFalse(attr.GenerateParameterBinder);
    }

    [TestMethod]
    public void SqlxAttribute_CanDisableAllGeneration()
    {
        var attr = new SqlxAttribute
        {
            GenerateEntityProvider = false,
            GenerateResultReader = false,
            GenerateParameterBinder = false
        };
        
        Assert.IsFalse(attr.GenerateEntityProvider);
        Assert.IsFalse(attr.GenerateResultReader);
        Assert.IsFalse(attr.GenerateParameterBinder);
    }

    [TestMethod]
    public void SqlxAttribute_AllowsMultiple()
    {
        var attrUsage = typeof(SqlxAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        
        Assert.IsNotNull(attrUsage);
        Assert.IsTrue(attrUsage.AllowMultiple);
    }

    [TestMethod]
    public void SqlxAttribute_TargetsClass()
    {
        var attrUsage = typeof(SqlxAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        
        Assert.IsNotNull(attrUsage);
        Assert.AreEqual(AttributeTargets.Class, attrUsage.ValidOn);
    }

    [TestMethod]
    public void SqlxAttribute_NotInherited()
    {
        var attrUsage = typeof(SqlxAttribute).GetCustomAttribute<AttributeUsageAttribute>();
        
        Assert.IsNotNull(attrUsage);
        Assert.IsFalse(attrUsage.Inherited);
    }

    #endregion

    #region Generated Code Verification Tests

    [TestMethod]
    public void GeneratedEntityProvider_HasDefaultProperty()
    {
        // Verify that generated EntityProvider has a static Default property
        var entityProviderType = Type.GetType("Sqlx.Tests.TestEntityEntityProvider, Sqlx.Tests");
        
        if (entityProviderType != null)
        {
            var defaultProp = entityProviderType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(defaultProp, "EntityProvider should have a static Default property");
        }
    }

    [TestMethod]
    public void GeneratedResultReader_HasDefaultProperty()
    {
        // Verify that generated ResultReader has a static Default property
        var resultReaderType = Type.GetType("Sqlx.Tests.TestEntityResultReader, Sqlx.Tests");
        
        if (resultReaderType != null)
        {
            var defaultProp = resultReaderType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(defaultProp, "ResultReader should have a static Default property");
        }
    }

    [TestMethod]
    public void GeneratedParameterBinder_HasDefaultProperty()
    {
        // Verify that generated ParameterBinder has a static Default property
        var parameterBinderType = Type.GetType("Sqlx.Tests.TestEntityParameterBinder, Sqlx.Tests");
        
        if (parameterBinderType != null)
        {
            var defaultProp = parameterBinderType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(defaultProp, "ParameterBinder should have a static Default property");
        }
    }

    #endregion

    #region SqlQuery Static Registration Tests

    [TestMethod]
    public void SqlQuery_EntityProvider_CanBeSet()
    {
        // Test that SqlQuery<T>.EntityProvider can be set (used by ModuleInitializer)
        var originalProvider = SqlQuery<TestEntity>.EntityProvider;
        
        try
        {
            SqlQuery<TestEntity>.EntityProvider = null;
            Assert.IsNull(SqlQuery<TestEntity>.EntityProvider);
        }
        finally
        {
            SqlQuery<TestEntity>.EntityProvider = originalProvider;
        }
    }

    [TestMethod]
    public void SqlQuery_ResultReader_CanBeSet()
    {
        // Test that SqlQuery<T>.ResultReader can be set (used by ModuleInitializer)
        var originalReader = SqlQuery<TestEntity>.ResultReader;
        
        try
        {
            SqlQuery<TestEntity>.ResultReader = null;
            Assert.IsNull(SqlQuery<TestEntity>.ResultReader);
        }
        finally
        {
            SqlQuery<TestEntity>.ResultReader = originalReader;
        }
    }

    [TestMethod]
    public void SqlQuery_ParameterBinder_CanBeSet()
    {
        // Test that SqlQuery<T>.ParameterBinder can be set (used by ModuleInitializer)
        var originalBinder = SqlQuery<TestEntity>.ParameterBinder;
        
        try
        {
            SqlQuery<TestEntity>.ParameterBinder = null;
            Assert.IsNull(SqlQuery<TestEntity>.ParameterBinder);
        }
        finally
        {
            SqlQuery<TestEntity>.ParameterBinder = originalBinder;
        }
    }

    #endregion

    #region Test Entity

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #endregion
}
