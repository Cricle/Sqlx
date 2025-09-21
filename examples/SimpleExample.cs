using System.Collections.Generic;
using Sqlx;

namespace Examples
{
    public class SimpleExample
    {
        public static void BasicUsage()
        {
            // 直接执行
            var sql1 = SimpleSql.Execute("SELECT * FROM Users WHERE Id = {id}", 
                new Dictionary<string, object?> { ["id"] = 123 });

            // 模板复用
            var template = SimpleSql.Create("SELECT * FROM Users WHERE Age > {age}");
            var sql2 = template.Set("age", 18);
            var sql3 = template.Set("age", 30);

            // 批量处理
            var users = new[]
            {
                new Dictionary<string, object?> { ["id"] = 1, ["name"] = "张三" },
                new Dictionary<string, object?> { ["id"] = 2, ["name"] = "李四" }
            };
            var sqls = SimpleSql.Batch("INSERT INTO Users (Id, Name) VALUES ({id}, {name})", users);

            // 参数构建器
            var params1 = Params.New().Add("id", 123).Add("name", "测试");
            var sql4 = SimpleSql.Execute("SELECT * FROM Users WHERE Id = {id} AND Name = {name}", params1);
        }
    }
}
