using System.Data.Common;
using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services
{
    public partial class TestUserService : IUserService
    {
        private readonly DbConnection connection;

        public TestUserService(DbConnection connection)
        {
            this.connection = connection;
        }

        // 实现IUserService接口的所有方法
        [Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]
        public partial Task<IList<User>> GetActiveUsersAsync();

        [Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
        public partial Task<User?> GetUserByIdAsync(int id);

        [Sqlx("SELECT * FROM [user] WHERE [age] BETWEEN @min_age AND @max_age")]
        public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

        [Sqlx("SELECT COUNT(*) FROM [user] WHERE [department_id] = @dept_id")]
        public partial Task<int> GetUserCountByDepartmentAsync(int deptId);

        // 额外的测试方法
        [Sqlx("SELECT COUNT(*) FROM [user]")]
        public partial int GetTotalUserCount();
    }
}
