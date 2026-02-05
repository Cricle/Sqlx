// <copyright file="TypeConversionGeneratorTest.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

public enum TestStatus
{
    Pending = 0,
    Active = 1,
    Completed = 2
}

[Sqlx.Annotations.Sqlx]
public class EntityWithEnum
{
    public int Id { get; set; }
    public TestStatus Status { get; set; }
    public TestStatus? OptionalStatus { get; set; }
}

[TestClass]
public class TypeConversionGeneratorTest
{
    [TestMethod]
    public void GeneratedReader_ConvertsIntToEnum_Success()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 as id, 2 as status, 1 as optional_status";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new EntityWithEnumResultReader();
        var entity = resultReader.Read(reader);
        
        Assert.AreEqual(1, entity.Id);
        Assert.AreEqual(TestStatus.Completed, entity.Status);
        Assert.AreEqual(TestStatus.Active, entity.OptionalStatus);
    }

    [TestMethod]
    public void GeneratedReader_HandlesNullableEnum_Success()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 as id, 0 as status, NULL as optional_status";
        
        using var reader = cmd.ExecuteReader();
        reader.Read();
        
        var resultReader = new EntityWithEnumResultReader();
        var entity = resultReader.Read(reader);
        
        Assert.AreEqual(1, entity.Id);
        Assert.AreEqual(TestStatus.Pending, entity.Status);
        Assert.IsNull(entity.OptionalStatus);
    }
}
