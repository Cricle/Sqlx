using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests.QueryBuilder;

[TestClass]
public class SubQueryParameterizationTests
{
    [Sqlx]
    public partial class ParameterizedUser
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    [Sqlx]
    public partial class ParameterizedOrder
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    [TestMethod]
    public void ToSqlWithParameters_SubQueryWithFilter_ParameterizesSubQueryConstants()
    {
        var query = SqlQuery<ParameterizedUser>.ForSqlite()
            .Select(user => new
            {
                user.Id,
                ActiveOrders = SubQuery.For<ParameterizedOrder>()
                    .Where(order => order.Status == "active")
                    .Count()
            });

        var (sql, parameters) = query.ToSqlWithParameters();
        var parameterList = parameters.ToList();

        Assert.IsTrue(sql.Contains("[status] = @p0"), sql);
        Assert.AreEqual(1, parameterList.Count);
        Assert.AreEqual("active", parameterList[0].Value);
    }

    [TestMethod]
    public void ToSqlWithParameters_OuterAndSubQueryFilters_MergeParametersInOrder()
    {
        var query = SqlQuery<ParameterizedUser>.ForSqlite()
            .Where(user => user.Name == "Alice")
            .Select(user => new
            {
                user.Id,
                MatchingOrders = SubQuery.For<ParameterizedOrder>()
                    .Where(order => order.Amount > 100 && order.Status == "completed")
                    .Count()
            });

        var (sql, parameters) = query.ToSqlWithParameters();
        var parameterList = parameters.ToList();

        Assert.IsTrue(sql.Contains("[name] = @p0"), sql);
        Assert.IsTrue(sql.Contains("[amount] > @p1"), sql);
        Assert.IsTrue(sql.Contains("[status] = @p2"), sql);
        Assert.AreEqual(3, parameterList.Count);
        Assert.AreEqual("Alice", parameterList[0].Value);
        Assert.AreEqual(100m, parameterList[1].Value);
        Assert.AreEqual("completed", parameterList[2].Value);
    }

    [TestMethod]
    public void ToSqlWithParameters_OuterAndSubQueryConstants_UseUniqueParameterNames()
    {
        var query = SqlQuery<ParameterizedUser>.ForSqlite()
            .Where(user => user.Name == "Alice")
            .Select(user => new
            {
                user.Id,
                MatchingOrders = SubQuery.For<ParameterizedOrder>()
                    .Where(order => order.Status == "completed")
                    .Count()
            });

        var (sql, parameters) = query.ToSqlWithParameters();
        var parameterList = parameters.ToList();

        Assert.IsTrue(sql.Contains("[name] = @p0"), sql);
        Assert.IsTrue(sql.Contains("[status] = @p1"), sql);
        Assert.AreEqual(2, parameterList.Count);
        Assert.AreEqual("Alice", parameterList[0].Value);
        Assert.AreEqual("completed", parameterList[1].Value);
    }
}
