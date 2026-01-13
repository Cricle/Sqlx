using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

[TestClass]
public class ColumnMetaTests
{
    [TestMethod]
    public void ColumnMeta_RecordEquality_WorksCorrectly()
    {
        var col1 = new ColumnMeta("id", "Id", DbType.Int64, false);
        var col2 = new ColumnMeta("id", "Id", DbType.Int64, false);
        var col3 = new ColumnMeta("name", "Name", DbType.String, false);
        
        Assert.AreEqual(col1, col2);
        Assert.AreNotEqual(col1, col3);
    }

    [TestMethod]
    public void ColumnMeta_Properties_AreAccessible()
    {
        var col = new ColumnMeta("user_name", "UserName", DbType.String, true);
        
        Assert.AreEqual("user_name", col.Name);
        Assert.AreEqual("UserName", col.PropertyName);
        Assert.AreEqual(DbType.String, col.DbType);
        Assert.IsTrue(col.IsNullable);
    }

    [TestMethod]
    public void ColumnMeta_WithExpression_CreatesNewInstance()
    {
        var col1 = new ColumnMeta("id", "Id", DbType.Int64, false);
        var col2 = col1 with { IsNullable = true };
        
        Assert.IsFalse(col1.IsNullable);
        Assert.IsTrue(col2.IsNullable);
        Assert.AreEqual(col1.Name, col2.Name);
    }
}
