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
        [Sqlx("SELECT * FROM [User] WHERE [IsActive] = 1")]
        public partial Task<IList<User>> GetActiveUsersAsync();

        [Sqlx("SELECT * FROM [User] WHERE [Id] = @id")]
        public partial Task<User?> GetUserByIdAsync(int id);

        [Sqlx("SELECT * FROM [User] WHERE [Age] BETWEEN @minAge AND @maxAge")]
        public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

        [Sqlx("SELECT COUNT(*) FROM [User] WHERE [DepartmentId] = @deptId")]
        public partial Task<int> GetUserCountByDepartmentAsync(int deptId);

        // 额外的测试方法
        [Sqlx("SELECT COUNT(*) FROM [User]")]
        public partial int GetTotalUserCount();
    }
}
