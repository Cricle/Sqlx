using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests;

[TestClass]
public class DebugSubQueryTest
{
    [Sqlx]
    public partial class DebugUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [TestMethod]
    public void SubQuery_Where_With_GroupBy_Key_ResolvesCorrectly()
    {
        var sql = SqlQuery<DebugUser>.ForSqlite()
            .GroupBy(x => x.Id % 3)
            .Select(x => new { 
                x.Key, 
                A = SubQuery.For<DebugUser>().Where(e => e.Id == x.Key).OrderBy(q => q.Name).FirstOrDefault() 
            })
            .ToSql();

        // x.Key should be resolved to ([id] % 3)
        Assert.AreEqual(
            "SELECT ([id] % 3) AS Key, (SELECT [id], [name] FROM [DebugUser] WHERE [id] = ([id] % 3) ORDER BY [name] ASC LIMIT 1) AS A FROM [DebugUser] GROUP BY ([id] % 3)",
            sql);
    }
}
