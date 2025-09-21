using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Tests
{
    [TestClass]
    public class SimpleSqlTests
    {
        [TestMethod]
        public void Execute_WithParameters_ShouldWork()
        {
            var result = SimpleSql.Execute("SELECT * FROM Users WHERE Id = {id}",
                new Dictionary<string, object?> { ["id"] = 123 });
            Assert.AreEqual("SELECT * FROM Users WHERE Id = 123", result);
        }

        [TestMethod]
        public void Template_Set_ShouldWork()
        {
            var template = SimpleSql.Create("SELECT * FROM Users WHERE Age > {age}");
            var result = template.Set("age", 18);
            Assert.AreEqual("SELECT * FROM Users WHERE Age > 18", result);
        }

        [TestMethod]
        public void Params_Builder_ShouldWork()
        {
            var parameters = Params.New().Add("id", 123).Add("name", "测试");
            var result = SimpleSql.Execute("SELECT * FROM Users WHERE Id = {id} AND Name = {name}", parameters);
            Assert.AreEqual("SELECT * FROM Users WHERE Id = 123 AND Name = '测试'", result);
        }
    }
}
