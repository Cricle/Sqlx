using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests.ExpressionToSql;

public interface IExpressionComplexRepository
{
    // 嵌套AND/OR
    [SqlTemplate("SELECT * FROM expr_test WHERE (age > @minAge AND salary > @minSalary) OR (is_active = @active AND score > @minScore)")]
    Task<List<ExprTestModel>> GetByComplexConditionAsync(int minAge, double minSalary, bool active, double minScore);

    [SqlTemplate("SELECT * FROM expr_test WHERE age >= @minAge AND salary >= @minSalary AND is_active = @active")]
    Task<List<ExprTestModel>> GetByMultipleAndAsync(int minAge, double minSalary, bool active);

    [SqlTemplate("SELECT * FROM expr_test WHERE category = 'Sales' OR category = 'IT' OR department = 'Dept-A'")]
    Task<List<ExprTestModel>> GetByMultipleOrAsync();

    // 字符串操作
    [SqlTemplate("SELECT * FROM expr_test WHERE name LIKE '%' || @keyword || '%'")]
    Task<List<ExprTestModel>> GetByNameContainsAsync(string keyword);

    [SqlTemplate("SELECT * FROM expr_test WHERE name LIKE @prefix || '%'")]
    Task<List<ExprTestModel>> GetByNameStartsWithAsync(string prefix);

    [SqlTemplate("SELECT * FROM expr_test WHERE email LIKE '%' || @suffix")]
    Task<List<ExprTestModel>> GetByEmailEndsWithAsync(string suffix);

    [SqlTemplate("SELECT * FROM expr_test WHERE LENGTH(name) > @minLength")]
    Task<List<ExprTestModel>> GetByNameLengthAsync(int minLength);

    // 数学运算
    [SqlTemplate("SELECT * FROM expr_test WHERE age + @increment > @threshold")]
    Task<List<ExprTestModel>> GetByAgeAdditionAsync(int increment, int threshold);

    [SqlTemplate("SELECT * FROM expr_test WHERE salary * @multiplier > @threshold")]
    Task<List<ExprTestModel>> GetBySalaryMultiplyAsync(double multiplier, double threshold);

    [SqlTemplate("SELECT * FROM expr_test WHERE age % @divisor = @remainder")]
    Task<List<ExprTestModel>> GetByAgeModuloAsync(int divisor, int remainder);

    // 比较运算
    [SqlTemplate("SELECT * FROM expr_test WHERE age >= @minAge AND age <= @maxAge")]
    Task<List<ExprTestModel>> GetByAgeBetweenAsync(int minAge, int maxAge);

    [SqlTemplate("SELECT * FROM expr_test WHERE category != @excludeCategory")]
    Task<List<ExprTestModel>> GetByCategoryNotEqualAsync(string excludeCategory);

    [SqlTemplate("SELECT * FROM expr_test WHERE is_active = 1")]
    Task<List<ExprTestModel>> GetByNegatedActiveAsync();

    // 复合条件
    [SqlTemplate("SELECT * FROM expr_test WHERE (age > 30 AND salary > 60000) OR (is_active = 1 AND score > 85 AND category = 'IT')")]
    Task<List<ExprTestModel>> GetByComplexBusinessLogicAsync();

    [SqlTemplate("SELECT * FROM expr_test WHERE ((age > 25 AND salary > 45000) OR (score > 80)) AND is_active = 1")]
    Task<List<ExprTestModel>> GetByThreeLayerNestingAsync();

    [SqlTemplate("SELECT * FROM expr_test WHERE (age * 2 > 60) AND (salary / 1000 < 60) AND (score >= 85)")]
    Task<List<ExprTestModel>> GetByMixedOperatorsAsync();
}

[RepositoryFor(typeof(IExpressionComplexRepository))]
public partial class ExpressionComplexRepository(IDbConnection connection) : IExpressionComplexRepository { }
