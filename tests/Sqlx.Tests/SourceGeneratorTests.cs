using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.Data;
using System.Data.Common;

namespace Sqlx.Tests;

/// <summary>
/// Tests for source generator output and behavior.
/// These tests verify that generated code works correctly.
/// </summary>
[TestClass]
public class SourceGeneratorTests
{
    #region EntityProvider Tests

    [TestMethod]
    public void GeneratedEntityProvider_HasCorrectEntityType()
    {
        var provider = TestEntityEntityProvider.Default;
        
        Assert.AreEqual(typeof(TestEntity), provider.EntityType);
    }

    [TestMethod]
    public void GeneratedEntityProvider_HasAllColumns()
    {
        var provider = TestEntityEntityProvider.Default;
        
        Assert.AreEqual(4, provider.Columns.Count);
    }

    [TestMethod]
    public void GeneratedEntityProvider_ColumnNamesAreSnakeCase()
    {
        var provider = TestEntityEntityProvider.Default;
        
        var idColumn = provider.Columns.FirstOrDefault(c => c.PropertyName == "Id");
        var userNameColumn = provider.Columns.FirstOrDefault(c => c.PropertyName == "UserName");
        var createdAtColumn = provider.Columns.FirstOrDefault(c => c.PropertyName == "CreatedAt");
        
        Assert.IsNotNull(idColumn);
        Assert.AreEqual("id", idColumn.Name);
        
        Assert.IsNotNull(userNameColumn);
        Assert.AreEqual("user_name", userNameColumn.Name);
        
        Assert.IsNotNull(createdAtColumn);
        Assert.AreEqual("created_at", createdAtColumn.Name);
    }

    [TestMethod]
    public void GeneratedEntityProvider_DbTypesAreCorrect()
    {
        var provider = TestEntityEntityProvider.Default;
        
        var idColumn = provider.Columns.First(c => c.PropertyName == "Id");
        var userNameColumn = provider.Columns.First(c => c.PropertyName == "UserName");
        var isActiveColumn = provider.Columns.First(c => c.PropertyName == "IsActive");
        var createdAtColumn = provider.Columns.First(c => c.PropertyName == "CreatedAt");
        
        Assert.AreEqual(DbType.Int32, idColumn.DbType);
        Assert.AreEqual(DbType.String, userNameColumn.DbType);
        Assert.AreEqual(DbType.Boolean, isActiveColumn.DbType);
        Assert.AreEqual(DbType.DateTime, createdAtColumn.DbType);
    }

    [TestMethod]
    public void GeneratedEntityProvider_NullabilityIsCorrect()
    {
        var provider = TestEntityWithNullableEntityProvider.Default;
        
        var idColumn = provider.Columns.First(c => c.PropertyName == "Id");
        var nameColumn = provider.Columns.First(c => c.PropertyName == "Name");
        var descriptionColumn = provider.Columns.First(c => c.PropertyName == "Description");
        
        Assert.IsFalse(idColumn.IsNullable);
        Assert.IsFalse(nameColumn.IsNullable);
        Assert.IsTrue(descriptionColumn.IsNullable);
    }

    [TestMethod]
    public void GeneratedEntityProvider_SingletonInstance()
    {
        var provider1 = TestEntityEntityProvider.Default;
        var provider2 = TestEntityEntityProvider.Default;
        
        Assert.AreSame(provider1, provider2);
    }

    #endregion

    #region ParameterBinder Tests

    [TestMethod]
    public void GeneratedParameterBinder_BindsAllParameters()
    {
        var binder = TestEntityParameterBinder.Default;
        var entity = new TestEntity
        {
            Id = 1,
            UserName = "test",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1)
        };
        
        using var command = new TestDbCommand();
        binder.BindEntity(command, entity);
        
        Assert.AreEqual(4, command.Parameters.Count);
    }

    [TestMethod]
    public void GeneratedParameterBinder_UsesCorrectParameterNames()
    {
        var binder = TestEntityParameterBinder.Default;
        var entity = new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = DateTime.Now };
        
        using var command = new TestDbCommand();
        binder.BindEntity(command, entity);
        
        Assert.IsNotNull(command.Parameters["@id"]);
        Assert.IsNotNull(command.Parameters["@user_name"]);
        Assert.IsNotNull(command.Parameters["@is_active"]);
        Assert.IsNotNull(command.Parameters["@created_at"]);
    }

    [TestMethod]
    public void GeneratedParameterBinder_UsesCustomPrefix()
    {
        var binder = TestEntityParameterBinder.Default;
        var entity = new TestEntity { Id = 1, UserName = "test", IsActive = true, CreatedAt = DateTime.Now };
        
        using var command = new TestDbCommand();
        binder.BindEntity(command, entity, "$");
        
        Assert.IsNotNull(command.Parameters["$id"]);
        Assert.IsNotNull(command.Parameters["$user_name"]);
    }

    [TestMethod]
    public void GeneratedParameterBinder_HandlesNullValues()
    {
        var binder = TestEntityWithNullableParameterBinder.Default;
        var entity = new TestEntityWithNullable { Id = 1, Name = "test", Description = null };
        
        using var command = new TestDbCommand();
        binder.BindEntity(command, entity);
        
        var descParam = command.Parameters["@description"];
        Assert.AreEqual(DBNull.Value, descParam.Value);
    }

    [TestMethod]
    public void GeneratedParameterBinder_SingletonInstance()
    {
        var binder1 = TestEntityParameterBinder.Default;
        var binder2 = TestEntityParameterBinder.Default;
        
        Assert.AreSame(binder1, binder2);
    }

    #endregion

    #region PlaceholderContext Integration Tests

    [TestMethod]
    public void PlaceholderContext_WithGeneratedProvider_WorksCorrectly()
    {
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "test_entities",
            TestEntityEntityProvider.Default.Columns);
        
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[user_name]"));
        Assert.IsTrue(template.Sql.Contains("[test_entities]"));
    }

    [TestMethod]
    public void PlaceholderContext_ExcludeWithGeneratedProvider_WorksCorrectly()
    {
        var context = new PlaceholderContext(
            SqlDefine.SQLite,
            "test_entities",
            TestEntityEntityProvider.Default.Columns);
        
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);
        
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[user_name]"));
        Assert.IsTrue(template.Sql.Contains("@user_name"));
    }

    #endregion

    #region Column Attribute Tests

    [TestMethod]
    public void GeneratedEntityProvider_RespectsColumnAttribute()
    {
        var provider = TestEntityWithColumnAttrEntityProvider.Default;
        
        var customColumn = provider.Columns.FirstOrDefault(c => c.PropertyName == "CustomName");
        
        Assert.IsNotNull(customColumn);
        Assert.AreEqual("custom_column_name", customColumn.Name);
    }

    #endregion
}

#region Test Entities

[SqlxEntity]
[SqlxParameter]
public class TestEntity
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

[SqlxEntity]
[SqlxParameter]
public class TestEntityWithNullable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

[SqlxEntity]
public class TestEntityWithColumnAttr
{
    public int Id { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.Column("custom_column_name")]
    public string CustomName { get; set; } = string.Empty;
}

#endregion

#region Test Helpers

/// <summary>
/// Simple test DbCommand implementation for testing parameter binding.
/// </summary>
public class TestDbCommand : DbCommand
{
    private readonly TestDbParameterCollection _parameters = new();
    
#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member
    public override string? CommandText { get; set; } = string.Empty;
#pragma warning restore CS8764
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public new TestDbParameterCollection Parameters => _parameters;

    public override void Cancel() { }
    public override int ExecuteNonQuery() => 0;
    public override object? ExecuteScalar() => null;
    public override void Prepare() { }
    protected override DbParameter CreateDbParameter() => new TestDbParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
}

public class TestDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; }
    public override bool IsNullable { get; set; }
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member
    public override string ParameterName { get; set; } = string.Empty;
    public override string SourceColumn { get; set; } = string.Empty;
#pragma warning restore CS8765
    public override int Size { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }

    public override void ResetDbType() { }
}

public class TestDbParameterCollection : DbParameterCollection, IEnumerable<DbParameter>
{
    private readonly List<TestDbParameter> _parameters = new();

    public override int Count => _parameters.Count;
    public override object SyncRoot => _parameters;

    public new TestDbParameter this[string name] => _parameters.First(p => p.ParameterName == name);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override int Add(object value)
    {
        _parameters.Add((TestDbParameter)value);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (TestDbParameter p in values)
            _parameters.Add(p);
    }

    public override void Clear() => _parameters.Clear();

    public override bool Contains(object value) => _parameters.Contains(value);
    public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);

    public override void CopyTo(Array array, int index) => throw new NotImplementedException();

    public override IEnumerator<DbParameter> GetEnumerator() => _parameters.Cast<DbParameter>().GetEnumerator();

    public override int IndexOf(object value) => _parameters.IndexOf((TestDbParameter)value);
    public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);

    public override void Insert(int index, object value) => _parameters.Insert(index, (TestDbParameter)value);

    public override void Remove(object value) => _parameters.Remove((TestDbParameter)value);

    public override void RemoveAt(int index) => _parameters.RemoveAt(index);
    public override void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);

    protected override DbParameter GetParameter(int index) => _parameters[index];
    protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);

    protected override void SetParameter(int index, DbParameter? value) => _parameters[index] = (TestDbParameter)value!;
    protected override void SetParameter(string parameterName, DbParameter? value)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0) _parameters[idx] = (TestDbParameter)value!;
    }
}

#endregion
