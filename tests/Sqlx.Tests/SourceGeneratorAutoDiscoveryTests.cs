// <copyright file="SourceGeneratorAutoDiscoveryTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for source generator auto-discovery of entity types from:
/// 1. [Sqlx] attribute
/// 2. SqlQuery&lt;T&gt; generic type arguments
/// 3. [SqlTemplate] method return types and parameters
/// </summary>
[TestClass]
public class SourceGeneratorAutoDiscoveryTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        // Ensure generated initializer is called
        Sqlx.Generated.SqlxInitializer.Initialize();
    }

    #region [Sqlx] Attribute Discovery Tests

    [TestMethod]
    public void SqlxAttribute_GeneratesEntityProvider()
    {
        // AutoDiscoveryEntity is marked with [Sqlx]
        var provider = AutoDiscoveryEntityEntityProvider.Default;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(AutoDiscoveryEntity), provider.EntityType);
    }

    [TestMethod]
    public void SqlxAttribute_GeneratesResultReader()
    {
        var reader = AutoDiscoveryEntityResultReader.Default;
        
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void SqlxAttribute_GeneratesParameterBinder()
    {
        var binder = AutoDiscoveryEntityParameterBinder.Default;
        
        Assert.IsNotNull(binder);
    }

    [TestMethod]
    public void SqlxAttribute_RegistersToSqlQuery()
    {
        Assert.IsNotNull(SqlQuery<AutoDiscoveryEntity>.EntityProvider);
        Assert.IsNotNull(SqlQuery<AutoDiscoveryEntity>.ResultReader);
        Assert.IsNotNull(SqlQuery<AutoDiscoveryEntity>.ParameterBinder);
    }

    [TestMethod]
    public void SqlxAttribute_RegistersToEntityProviderRegistry()
    {
        var provider = EntityProviderRegistry.Get(typeof(AutoDiscoveryEntity));
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(AutoDiscoveryEntity), provider.EntityType);
    }

    [TestMethod]
    public void SqlxAttribute_EntityProvider_HasCorrectColumns()
    {
        var provider = AutoDiscoveryEntityEntityProvider.Default;
        
        Assert.AreEqual(4, provider.Columns.Count);
        Assert.AreEqual("id", provider.Columns[0].Name);
        Assert.AreEqual("Id", provider.Columns[0].PropertyName);
        Assert.AreEqual("name", provider.Columns[1].Name);
        Assert.AreEqual("Name", provider.Columns[1].PropertyName);
        Assert.AreEqual("created_at", provider.Columns[2].Name);
        Assert.AreEqual("CreatedAt", provider.Columns[2].PropertyName);
        Assert.AreEqual("is_active", provider.Columns[3].Name);
        Assert.AreEqual("IsActive", provider.Columns[3].PropertyName);
    }

    #endregion

    #region Nested Class Tests

    [TestMethod]
    public void SqlxAttribute_NestedClass_GeneratesWithUniqueName()
    {
        // Nested class should have OuterClass_InnerClass naming
        var provider = OuterClass_NestedEntityEntityProvider.Default;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(OuterClass.NestedEntity), provider.EntityType);
    }

    [TestMethod]
    public void SqlxAttribute_NestedClass_RegistersCorrectly()
    {
        Assert.IsNotNull(SqlQuery<OuterClass.NestedEntity>.EntityProvider);
        Assert.AreEqual(typeof(OuterClass.NestedEntity), SqlQuery<OuterClass.NestedEntity>.EntityProvider!.EntityType);
    }

    [TestMethod]
    public void SqlxAttribute_NestedClass_HasCorrectColumns()
    {
        var provider = OuterClass_NestedEntityEntityProvider.Default;
        
        Assert.AreEqual(2, provider.Columns.Count);
        Assert.AreEqual("nested_id", provider.Columns[0].Name);
        Assert.AreEqual("nested_value", provider.Columns[1].Name);
    }

    #endregion

    #region SqlQuery<T> Auto-Discovery Tests

    [TestMethod]
    public void SqlQueryUsage_AutoDiscoveredEntity_HasProvider()
    {
        // SqlQueryDiscoveredEntity is used in SqlQuery<T> but not marked with [Sqlx]
        // The source generator should auto-discover it
        var provider = SqlQuery<SqlQueryDiscoveredEntity>.EntityProvider;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(SqlQueryDiscoveredEntity), provider.EntityType);
    }

    [TestMethod]
    public void SqlQueryUsage_AutoDiscoveredEntity_HasResultReader()
    {
        var reader = SqlQuery<SqlQueryDiscoveredEntity>.ResultReader;
        
        Assert.IsNotNull(reader);
    }

    [TestMethod]
    public void SqlQueryUsage_CanBuildQuery()
    {
        var query = SqlQuery<SqlQueryDiscoveredEntity>.ForSqlite()
            .Where(e => e.Value > 10);

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("[value]"));
        Assert.IsTrue(sql.Contains("> 10"));
    }

    #endregion

    #region [SqlTemplate] Return Type Discovery Tests

    [TestMethod]
    public void SqlTemplateReturnType_TaskOfEntity_Discovered()
    {
        // ReturnTypeEntity is used as Task<ReturnTypeEntity> return type
        var provider = SqlQuery<ReturnTypeEntity>.EntityProvider;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(ReturnTypeEntity), provider.EntityType);
    }

    [TestMethod]
    public void SqlTemplateReturnType_TaskOfListOfEntity_Discovered()
    {
        // ListReturnEntity is used as Task<List<ListReturnEntity>> return type
        var provider = SqlQuery<ListReturnEntity>.EntityProvider;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(ListReturnEntity), provider.EntityType);
    }

    [TestMethod]
    public void SqlTemplateReturnType_TaskOfNullableEntity_Discovered()
    {
        // NullableReturnEntity is used as Task<NullableReturnEntity?> return type
        var provider = SqlQuery<NullableReturnEntity>.EntityProvider;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(NullableReturnEntity), provider.EntityType);
    }

    #endregion

    #region [SqlTemplate] Parameter Type Discovery Tests

    [TestMethod]
    public void SqlTemplateParameter_EntityType_Discovered()
    {
        // ParameterEntity is used as method parameter type
        var provider = SqlQuery<ParameterEntity>.EntityProvider;
        
        Assert.IsNotNull(provider);
        Assert.AreEqual(typeof(ParameterEntity), provider.EntityType);
    }

    #endregion

    #region Deduplication Tests

    [TestMethod]
    public void Deduplication_SameTypeMultipleSources_OnlyOneProvider()
    {
        // DeduplicationEntity is marked with [Sqlx] AND used in SqlQuery<T>
        // Should only generate one provider
        var provider1 = DeduplicationEntityEntityProvider.Default;
        var provider2 = SqlQuery<DeduplicationEntity>.EntityProvider;

        Assert.AreSame(provider1, provider2);
    }

    [TestMethod]
    public void Deduplication_MultipleUsages_SingleRegistration()
    {
        // Verify EntityProviderRegistry has only one entry
        var provider = EntityProviderRegistry.Get(typeof(DeduplicationEntity));
        
        Assert.IsNotNull(provider);
        Assert.AreSame(DeduplicationEntityEntityProvider.Default, provider);
    }

    #endregion

    #region Column Metadata Tests

    [TestMethod]
    public void AutoDiscovered_ColumnMeta_HasCorrectDbTypes()
    {
        var provider = AutoDiscoveryEntityEntityProvider.Default;
        
        Assert.AreEqual(DbType.Int64, provider.Columns[0].DbType); // Id
        Assert.AreEqual(DbType.String, provider.Columns[1].DbType); // Name
        Assert.AreEqual(DbType.DateTime, provider.Columns[2].DbType); // CreatedAt
        Assert.AreEqual(DbType.Boolean, provider.Columns[3].DbType); // IsActive
    }

    [TestMethod]
    public void AutoDiscovered_ColumnMeta_HasCorrectNullability()
    {
        var provider = NullablePropertiesEntityEntityProvider.Default;
        
        // Non-nullable
        Assert.IsFalse(provider.Columns.First(c => c.PropertyName == "Id").IsNullable);
        // Nullable reference type
        Assert.IsTrue(provider.Columns.First(c => c.PropertyName == "NullableName").IsNullable);
        // Nullable value type
        Assert.IsTrue(provider.Columns.First(c => c.PropertyName == "NullableAge").IsNullable);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void Integration_AutoDiscoveredEntity_WorksWithSqlQuery()
    {
        var query = SqlQuery<AutoDiscoveryEntity>.ForSqlite()
            .Where(e => e.Name == "test" && e.IsActive)
            .OrderBy(e => e.CreatedAt)
            .Take(10);

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("[name]"));
        Assert.IsTrue(sql.Contains("[is_active]"));
        Assert.IsTrue(sql.Contains("[created_at]"));
        Assert.IsTrue(sql.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void Integration_AutoDiscoveredEntity_WorksWithJoin()
    {
        var query = SqlQuery<AutoDiscoveryEntity>.ForSqlite()
            .Join(
                SqlQuery<SqlQueryDiscoveredEntity>.ForSqlite(),
                a => a.Id,
                b => b.Id,
                (a, b) => new { a.Name, b.Value });

        var sql = query.ToSql();

        Assert.IsTrue(sql.Contains("JOIN"));
        Assert.IsTrue(sql.Contains("[name]"));
        Assert.IsTrue(sql.Contains("[value]"));
    }

    #endregion
}

#region Test Entity Classes

/// <summary>
/// Entity marked with [Sqlx] attribute for auto-discovery testing.
/// </summary>
[Sqlx]
public class AutoDiscoveryEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Outer class containing nested entity for testing nested class naming.
/// </summary>
public class OuterClass
{
    [Sqlx]
    public class NestedEntity
    {
        public int NestedId { get; set; }
        public string NestedValue { get; set; } = string.Empty;
    }
}

/// <summary>
/// Entity discovered through SqlQuery&lt;T&gt; usage (not marked with [Sqlx]).
/// </summary>
public class SqlQueryDiscoveredEntity
{
    public int Id { get; set; }
    public int Value { get; set; }
}

// Force SqlQuery<SqlQueryDiscoveredEntity> usage for auto-discovery
internal static class SqlQueryDiscoveredEntityUsage
{
    private static readonly IQueryable<SqlQueryDiscoveredEntity> _query = SqlQuery<SqlQueryDiscoveredEntity>.ForSqlite();
}

/// <summary>
/// Entity discovered through [SqlTemplate] return type Task&lt;T&gt;.
/// </summary>
public class ReturnTypeEntity
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Entity discovered through [SqlTemplate] return type Task&lt;List&lt;T&gt;&gt;.
/// </summary>
public class ListReturnEntity
{
    public int Id { get; set; }
    public string Item { get; set; } = string.Empty;
}

/// <summary>
/// Entity discovered through [SqlTemplate] return type Task&lt;T?&gt;.
/// </summary>
public class NullableReturnEntity
{
    public int Id { get; set; }
    public string? OptionalData { get; set; }
}

/// <summary>
/// Entity discovered through [SqlTemplate] method parameter.
/// </summary>
public class ParameterEntity
{
    public int Id { get; set; }
    public string Filter { get; set; } = string.Empty;
}

/// <summary>
/// Entity marked with [Sqlx] AND used in SqlQuery&lt;T&gt; for deduplication testing.
/// </summary>
[Sqlx]
public class DeduplicationEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Force SqlQuery<DeduplicationEntity> usage
internal static class DeduplicationEntityUsage
{
    private static readonly IQueryable<DeduplicationEntity> _query = SqlQuery<DeduplicationEntity>.ForSqlite();
}

/// <summary>
/// Entity with nullable properties for testing nullability detection.
/// </summary>
[Sqlx]
public class NullablePropertiesEntity
{
    public int Id { get; set; }
    public string? NullableName { get; set; }
    public int? NullableAge { get; set; }
}

/// <summary>
/// Interface with [SqlTemplate] methods for return type discovery testing.
/// </summary>
public interface IAutoDiscoveryRepository
{
    [SqlTemplate("SELECT * FROM return_type WHERE id = @id")]
    Task<ReturnTypeEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    [SqlTemplate("SELECT * FROM list_return")]
    Task<List<ListReturnEntity>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT * FROM nullable_return WHERE id = @id")]
    Task<NullableReturnEntity?> GetNullableAsync(int id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO parameter (filter) VALUES (@filter)")]
    Task<int> InsertAsync(ParameterEntity entity, CancellationToken ct = default);
}

#endregion
