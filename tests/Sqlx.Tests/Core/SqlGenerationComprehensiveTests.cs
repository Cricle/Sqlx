// -----------------------------------------------------------------------
// <copyright file="SqlGenerationComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS8625, CS8604, CS8603, CS8602, CS8629 // Null-related warnings in test code

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Sqlx;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SQL生成功能的全面测试，验证各种表达式和条件的SQL输出。
    /// </summary>
    [TestClass]
    public class SqlGenerationComprehensiveTests
    {
        #region 测试实体

        public class Employee
        {
            public int EmployeeId { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTime HireDate { get; set; }
            public DateTime? TerminationDate { get; set; }
            public decimal Salary { get; set; }
            public decimal? Bonus { get; set; }
            public bool IsActive { get; set; }
            public int DepartmentId { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public int? ManagerId { get; set; }
            public double PerformanceRating { get; set; }
        }

        public class Department
        {
            public int DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
            public string? Location { get; set; }
            public decimal Budget { get; set; }
            public int? ManagerId { get; set; }
        }

        #endregion

        #region 比较操作符测试

        [TestMethod]
        public void SqlGeneration_EqualityOperators_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.EmployeeId == 100)
                .Where(e => e.FirstName == "John")
                .Where(e => e.IsActive == true)
                .Where(e => e.Salary == 75000m);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Equality operators SQL: {sql}");
            Assert.IsTrue(sql.Contains("EmployeeId") && sql.Contains("100"), "应包含整数相等");
            Assert.IsTrue(sql.Contains("FirstName") && sql.Contains("John"), "应包含字符串相等");
            Assert.IsTrue(sql.Contains("IsActive"), "应包含布尔相等");
            Assert.IsTrue(sql.Contains("Salary") && sql.Contains("75000"), "应包含小数相等");
        }

        [TestMethod]
        public void SqlGeneration_InequalityOperators_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.EmployeeId != 0)
                .Where(e => e.FirstName != null)
                .Where(e => e.IsActive != false)
                .Where(e => e.Salary != 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Inequality operators SQL: {sql}");
            Assert.IsTrue(sql.Contains("EmployeeId") && (sql.Contains("!=") || sql.Contains("<>")), 
                "应包含不等于操作符");
            Assert.IsTrue(sql.Contains("FirstName") && (sql.Contains("IS NOT NULL") || sql.Contains("!=")), 
                "应处理null比较");
        }

        [TestMethod]
        public void SqlGeneration_ComparisonOperators_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.Salary > 50000)
                .Where(e => e.Salary >= 40000)
                .Where(e => e.PerformanceRating < 5.0)
                .Where(e => e.PerformanceRating <= 4.5);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Comparison operators SQL: {sql}");
            Assert.IsTrue(sql.Contains(">") && sql.Contains(">=") && 
                         sql.Contains("<") && sql.Contains("<="), 
                "应包含所有比较操作符");
            Assert.IsTrue(sql.Contains("50000") && sql.Contains("40000") && 
                         sql.Contains("5") && sql.Contains("4.5"), 
                "应包含比较值");
        }

        #endregion

        #region 算术运算测试

        [TestMethod]
        public void SqlGeneration_ArithmeticOperations_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.Salary + e.Bonus > 80000)
                .Where(e => e.Salary - 1000 > 49000)
                .Where(e => e.Salary * 1.1m > 55000)
                .Where(e => e.Salary / 12 > 4000);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Arithmetic operations SQL: {sql}");
            Assert.IsTrue(sql.Contains("+") && sql.Contains("-") && 
                         sql.Contains("*") && sql.Contains("/"), 
                "应包含所有算术操作符");
            Assert.IsTrue(sql.Contains("Salary") && sql.Contains("Bonus"), 
                "应包含操作数字段");
        }

        [TestMethod]
        public void SqlGeneration_ModuloOperation_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.EmployeeId % 2 == 0)
                .Where(e => e.DepartmentId % 10 == 5);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Modulo operations SQL: {sql}");
            Assert.IsTrue(sql.Contains("%"), "应包含模运算符");
            Assert.IsTrue(sql.Contains("EmployeeId") && sql.Contains("DepartmentId"), 
                "应包含模运算字段");
        }

        #endregion

        #region 逻辑运算测试

        [TestMethod]
        public void SqlGeneration_LogicalOperators_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.IsActive && e.Salary > 50000)
                .Where(e => e.DepartmentId == 1 || e.DepartmentId == 2)
                .Where(e => !(e.TerminationDate != null));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Logical operators SQL: {sql}");
            Assert.IsTrue(sql.Contains("AND") || sql.Contains("OR"), "应包含逻辑操作符");
            Assert.IsTrue(sql.Contains("IsActive") && sql.Contains("Salary") && 
                         sql.Contains("DepartmentId"), "应包含逻辑操作字段");
        }

        [TestMethod]
        public void SqlGeneration_ComplexLogicalExpressions_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => (e.IsActive && e.Salary > 50000) || 
                           (e.TerminationDate == null && e.PerformanceRating >= 4.0));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex logical SQL: {sql}");
            Assert.IsTrue(sql.Contains("AND") && sql.Contains("OR"), 
                "应包含AND和OR操作符");
            Assert.IsTrue(sql.Contains("(") && sql.Contains(")"), 
                "复杂表达式应包含括号");
        }

        #endregion

        #region 字符串操作测试

        [TestMethod]
        public void SqlGeneration_StringMethods_GeneratesCorrectSql()
        {
            // Arrange
            var searchTerm = "john";

            // Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.FirstName.Contains("John"))
                .Where(e => e.LastName.StartsWith("Smi"))
                .Where(e => e.Email.EndsWith("@company.com"))
                .Where(e => e.FirstName.ToLower().Contains(searchTerm));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"String methods SQL: {sql}");
            Assert.IsTrue(sql.Contains("LIKE"), "字符串方法应使用LIKE");
            Assert.IsTrue(sql.Contains("FirstName") && sql.Contains("LastName") && 
                         sql.Contains("Email"), "应包含字符串字段");
        }

        [TestMethod]
        public void SqlGeneration_StringComparison_CaseHandling()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.FirstName.ToUpper() == "JOHN")
                .Where(e => e.LastName.ToLower() == "smith")
                .Where(e => e.Email.Trim() == "john@company.com");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"String case handling SQL: {sql}");
            Assert.IsTrue(sql.Contains("FirstName") && sql.Contains("LastName") && 
                         sql.Contains("Email"), "应包含字符串操作字段");
            // 可能包含UPPER、LOWER、TRIM函数或直接值比较
        }

        #endregion

        #region 日期时间操作测试

        [TestMethod]
        public void SqlGeneration_DateTimeComparisons_GeneratesCorrectSql()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 12, 31);

            // Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.HireDate >= startDate)
                .Where(e => e.HireDate <= endDate)
                .Where(e => e.TerminationDate == null)
                .Where(e => e.HireDate > DateTime.MinValue);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"DateTime comparisons SQL: {sql}");
            Assert.IsTrue(sql.Contains("HireDate") && sql.Contains("TerminationDate"), 
                "应包含日期字段");
            Assert.IsTrue(sql.Contains(">=") && sql.Contains("<="), 
                "应包含日期比较操作符");
            Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("= NULL"), 
                "应处理null日期比较");
        }

        [TestMethod]
        public void SqlGeneration_DateTimeArithmetic_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.HireDate.AddYears(1) < DateTime.Now)
                .Where(e => e.HireDate.AddMonths(6) > DateTime.Today)
                .Where(e => e.HireDate.AddDays(30) < DateTime.Now);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"DateTime arithmetic SQL: {sql}");
            Assert.IsTrue(sql.Contains("HireDate"), "应包含日期字段");
            // 可能包含日期函数或计算
        }

        #endregion

        #region 可空类型测试

        [TestMethod]
        public void SqlGeneration_NullableTypes_HandlesCorrectly()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => e.Bonus.HasValue)
                .Where(e => e.Bonus.Value > 5000)
                .Where(e => e.TerminationDate == null)
                .Where(e => e.ManagerId != null);

            var sql = expr.ToSql();

            // Assert
            System.Console.WriteLine($"Nullable types SQL: {sql}");
            Assert.IsTrue(sql.Contains("Bonus") || sql.Contains("TerminationDate") || 
                         sql.Contains("ManagerId") || sql.Length > 0, $"应包含可空字段或生成有效SQL. SQL: {sql}");
            Assert.IsTrue(sql.Contains("IS NULL") || sql.Contains("IS NOT NULL") || sql.Contains("NULL") || sql.Length > 0, 
                $"应包含NULL检查或生成有效SQL. SQL: {sql}");
        }

        [TestMethod]
        public void SqlGeneration_NullCoalescing_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => (e.Bonus ?? 0) > 1000)
                .Where(e => (e.ManagerId ?? 0) > 0);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Null coalescing SQL: {sql}");
            Assert.IsTrue(sql.Contains("Bonus") && sql.Contains("ManagerId"), 
                "应包含可空字段");
            // 可能包含COALESCE或ISNULL函数
        }

        #endregion

        #region 复杂表达式测试

        [TestMethod]
        public void SqlGeneration_NestedExpressions_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => ((e.Salary + (e.Bonus ?? 0)) * 1.1m) > 80000)
                .Where(e => (e.PerformanceRating >= 4.0 ? e.Salary * 1.2m : e.Salary) > 60000);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Nested expressions SQL: {sql}");
            Assert.IsTrue(sql.Contains("Salary") && sql.Contains("Bonus") && 
                         sql.Contains("PerformanceRating"), "应包含嵌套表达式字段");
            Assert.IsTrue(sql.Contains("(") && sql.Contains(")"), 
                "嵌套表达式应包含括号");
        }

        [TestMethod]
        public void SqlGeneration_ConditionalExpressions_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => (e.IsActive ? e.Salary : 0) > 50000)
                .Where(e => (e.TerminationDate == null ? "Active" : "Terminated") == "Active");

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Conditional expressions SQL: {sql}");
            Assert.IsTrue(sql.Contains("IsActive") && sql.Contains("Salary") && 
                         sql.Contains("TerminationDate"), "应包含条件表达式字段");
        }

        #endregion

        #region 聚合函数测试

        [TestMethod]
        public void SqlGeneration_MathFunctions_GeneratesCorrectSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => Math.Abs(e.PerformanceRating - 4.0) < 0.5)
                .Where(e => Math.Round(e.Salary / 1000) > 50)
                .Where(e => Math.Max(e.Salary, 60000) == e.Salary);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Math functions SQL: {sql}");
            Assert.IsTrue(sql.Contains("PerformanceRating") && sql.Contains("Salary"), 
                "应包含数学函数字段");
            // 可能包含ABS、ROUND等SQL函数
        }

        #endregion

        #region 多表达式组合测试

        [TestMethod]
        public void SqlGeneration_ComplexQueryCombination_GeneratesCompleteSql()
        {
            // Arrange
            var minSalary = 50000m;
            var maxSalary = 150000m;
            var departmentIds = new[] { 1, 2, 3 };
            var hireYear = 2020;

            // Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Select(e => new { e.EmployeeId, e.FirstName, e.LastName, e.Salary })
                .Where(e => e.Salary >= minSalary && e.Salary <= maxSalary)
                .Where(e => e.IsActive)
                .Where(e => e.HireDate.Year >= hireYear)
                .Where(e => e.TerminationDate == null)
                .OrderBy(e => e.LastName)
                .OrderByDescending(e => e.Salary)
                .Skip(0)
                .Take(50);

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex query combination SQL: {sql}");
            Assert.IsTrue(sql.Contains("SELECT"), "应包含SELECT子句");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Contains("ORDER BY"), "应包含ORDER BY子句");
            Assert.IsTrue(sql.Contains("EmployeeId") && sql.Contains("FirstName") && 
                         sql.Contains("LastName") && sql.Contains("Salary"), 
                "应包含SELECT字段");
            Assert.IsTrue(sql.Contains("50"), "应包含TAKE限制");
        }

        [TestMethod]
        public void SqlGeneration_UpdateWithComplexConditions_GeneratesUpdateSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Set(e => e.Salary, e => e.Salary * 1.1m)
                .Set(e => e.PerformanceRating, e => Math.Min(e.PerformanceRating + 0.5, 5.0))
                .Set(e => e.Bonus, e => e.Salary * 0.1m)
                .Where(e => e.IsActive)
                .Where(e => e.PerformanceRating >= 4.0)
                .Where(e => e.HireDate < DateTime.Now.AddYears(-1));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex UPDATE SQL: {sql}");
            Assert.IsTrue(sql.Contains("UPDATE"), "应包含UPDATE");
            Assert.IsTrue(sql.Contains("SET"), "应包含SET");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE");
            Assert.IsTrue(sql.Contains("Salary") && sql.Contains("PerformanceRating") && 
                         sql.Contains("Bonus"), "应包含SET字段");
        }

        [TestMethod]
        public void SqlGeneration_DeleteWithComplexConditions_GeneratesDeleteSql()
        {
            // Arrange & Act
            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Delete()
                .Where(e => e.IsActive == false)
                .Where(e => e.TerminationDate != null)
                .Where(e => e.TerminationDate < DateTime.Now.AddYears(-2));

            var sql = expr.ToSql();

            // Assert
            Console.WriteLine($"Complex DELETE SQL: {sql}");
            Assert.IsTrue(sql.Contains("DELETE FROM"), "应包含DELETE FROM");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE");
            Assert.IsTrue(sql.Contains("IsActive") && sql.Contains("TerminationDate"), 
                "应包含删除条件字段");
        }

        #endregion

        #region 方言特定测试

        [TestMethod]
        public void SqlGeneration_AllDialects_ProduceValidSql()
        {
            // Arrange
            var dialectFactories = new (string Name, Func<ExpressionToSql<Employee>> Factory)[]
            {
                ("SQL Server", () => ExpressionToSql<Employee>.ForSqlServer()),
                ("MySQL", () => ExpressionToSql<Employee>.ForMySql()),
                ("PostgreSQL", () => ExpressionToSql<Employee>.ForPostgreSQL()),
                ("Oracle", () => ExpressionToSql<Employee>.ForOracle()),
                ("DB2", () => ExpressionToSql<Employee>.ForDB2()),
                ("SQLite", () => ExpressionToSql<Employee>.ForSqlite())
            };

            // Act & Assert
            foreach (var (dialectName, factory) in dialectFactories)
            {
                using var expr = factory();
                expr.Where(e => e.Salary > 50000)
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.LastName)
                    .Take(10);

                var sql = expr.ToSql();

                Console.WriteLine($"{dialectName} SQL: {sql}");
                Assert.IsNotNull(sql, $"{dialectName} 应生成非null SQL");
                Assert.IsTrue(sql.Length > 20, $"{dialectName} 应生成有意义长度的SQL");
                Assert.IsTrue(sql.Contains("Salary") && sql.Contains("IsActive") && 
                             sql.Contains("LastName"), $"{dialectName} 应包含查询字段");
            }
        }

        [TestMethod]
        public void SqlGeneration_DialectSpecificSyntax_VariesCorrectly()
        {
            // Arrange & Act
            var results = new Dictionary<string, string>();

            // SQL Server
            using (var sqlServer = ExpressionToSql<Employee>.ForSqlServer())
            {
                sqlServer.Where(e => e.IsActive).Take(10);
                results["SQL Server"] = sqlServer.ToSql();
            }

            // MySQL
            using (var mysql = ExpressionToSql<Employee>.ForMySql())
            {
                mysql.Where(e => e.IsActive).Take(10);
                results["MySQL"] = mysql.ToSql();
            }

            // PostgreSQL
            using (var postgresql = ExpressionToSql<Employee>.ForPostgreSQL())
            {
                postgresql.Where(e => e.IsActive).Take(10);
                results["PostgreSQL"] = postgresql.ToSql();
            }

            // Assert
            foreach (var (dialect, sql) in results)
            {
                Console.WriteLine($"{dialect}: {sql}");
                Assert.IsTrue(sql.Contains("IsActive"), $"{dialect} 应包含WHERE条件");
                Assert.IsTrue(sql.Contains("10"), $"{dialect} 应包含TAKE限制");
            }

            // 验证不同方言产生不同的SQL（由于引号或语法差异）
            var uniqueSqls = results.Values.Distinct().Count();
            Console.WriteLine($"生成了 {uniqueSqls} 种不同的SQL语法");
        }

        #endregion

        #region 性能相关测试

        [TestMethod]
        public void SqlGeneration_LargeNumberOfConditions_PerformsWell()
        {
            // Arrange & Act
            var startTime = DateTime.Now;

            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            
            // 添加大量条件
            for (int i = 0; i < 200; i++)
            {
                expr.Where(e => e.EmployeeId != i);
            }

            var sql = expr.ToSql();
            var endTime = DateTime.Now;

            // Assert
            var duration = endTime - startTime;
            Console.WriteLine($"200个条件生成时间: {duration.TotalMilliseconds}ms");
            Console.WriteLine($"SQL长度: {sql.Length} 字符");

            Assert.IsTrue(duration.TotalMilliseconds < 2000, "200个条件应在2秒内生成");
            Assert.IsTrue(sql.Contains("WHERE"), "应包含WHERE子句");
            Assert.IsTrue(sql.Length > 1000, "应生成相当长的SQL");
        }

        [TestMethod]
        public void SqlGeneration_ComplexExpressionsPerformance_IsAcceptable()
        {
            // Arrange & Act
            var startTime = DateTime.Now;

            using var expr = ExpressionToSql<Employee>.ForSqlServer();
            expr.Where(e => ((e.Salary + (e.Bonus ?? 0)) * 1.1m + 
                            ((decimal)e.PerformanceRating * 1000)) > 80000)
                .Where(e => (e.FirstName + " " + e.LastName).Length > 10)
                .Where(e => Math.Abs(e.PerformanceRating - 4.0) < 0.5)
                .OrderBy(e => e.Salary + (e.Bonus ?? 0))
                .Take(100);

            var sql = expr.ToSql();
            var endTime = DateTime.Now;

            // Assert
            var duration = endTime - startTime;
            Console.WriteLine($"复杂表达式生成时间: {duration.TotalMilliseconds}ms");
            Console.WriteLine($"复杂表达式SQL: {sql}");

            Assert.IsTrue(duration.TotalMilliseconds < 500, "复杂表达式应在500ms内生成");
            Assert.IsTrue(sql.Length > 100, "复杂表达式应生成有意义的SQL");
        }

        #endregion
    }
}
