// Test following the TodoWebApi pattern
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Collections.Generic;

namespace Sqlx.Tests.RepositoryFor
{
    [TableName("students")]
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Interface inherits from ICrudRepository (like TodoRepository)
    public interface IStudentRepository : ICrudRepository<Student, int>
    {
        // Additional methods can be added here
    }

    // Repository implementation following TodoWebApi pattern
    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName("students")]
    [RepositoryFor(typeof(IStudentRepository))]
    public partial class StudentRepository(DbConnection connection) : IStudentRepository
    {
    }

    [TestClass]
    public class TodoPatternTest
    {
        [TestMethod]
        public void TodoPattern_ShouldCompile()
        {
            // Verify the repository type is correctly generated
            var repoType = typeof(StudentRepository);
            
            // Assert
            Assert.IsNotNull(repoType);
            Assert.IsTrue(repoType.GetInterfaces().Any(i => i == typeof(IStudentRepository)));
            Assert.IsTrue(repoType.GetInterfaces().Any(i => i.Name.Contains("ICrudRepository")));
            Assert.IsTrue(repoType.GetMethods().Any(m => m.Name == "GetByIdAsync"));
            Assert.IsTrue(repoType.GetMethods().Any(m => m.Name == "InsertAsync"));
        }
    }
}

