// -----------------------------------------------------------------------
// <copyright file="ExpressionExtensionsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Expression
{
    [TestClass]
    public class ExpressionExtensionsTests
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal Price { get; set; }
        }

        #region ToWhereClause Tests

        [TestMethod]
        public void ToWhereClause_NullPredicate_ReturnsEmpty()
        {
            Expression<Func<TestEntity, bool>>? predicate = null;
            var result = predicate!.ToWhereClause();
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ToWhereClause_EqualCondition_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("id") && result.Contains("=") && result.Contains("1"));
        }

        [TestMethod]
        public void ToWhereClause_StringEqual_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Name == "test";
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("name") && result.Contains("'test'"));
        }

        [TestMethod]
        public void ToWhereClause_GreaterThan_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Age > 18;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("age") && result.Contains(">") && result.Contains("18"));
        }

        [TestMethod]
        public void ToWhereClause_LessThan_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Age < 65;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("age") && result.Contains("<") && result.Contains("65"));
        }

        [TestMethod]
        public void ToWhereClause_AndCondition_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1 && x.IsActive;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("AND"));
        }

        [TestMethod]
        public void ToWhereClause_OrCondition_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1 || x.Id == 2;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("OR"));
        }

        [TestMethod]
        public void ToWhereClause_WithSqlServerDialect_UsesCorrectFormat()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1;
            var result = predicate.ToWhereClause(SqlDefine.SqlServer);
            Assert.IsTrue(result.Contains("[id]") || result.Contains("id"));
        }

        [TestMethod]
        public void ToWhereClause_WithMySqlDialect_UsesCorrectFormat()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1;
            var result = predicate.ToWhereClause(SqlDefine.MySql);
            Assert.IsTrue(result.Contains("`id`") || result.Contains("id"));
        }

        [TestMethod]
        public void ToWhereClause_BooleanProperty_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.IsActive;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("is_active"));
        }

        [TestMethod]
        public void ToWhereClause_NotEqual_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id != 0;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("<>") || result.Contains("!="));
        }

        [TestMethod]
        public void ToWhereClause_GreaterThanOrEqual_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Age >= 18;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains(">="));
        }

        [TestMethod]
        public void ToWhereClause_LessThanOrEqual_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Age <= 65;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("<="));
        }

        #endregion

        #region GetParameters Tests

        [TestMethod]
        public void GetParameters_NullPredicate_ReturnsEmptyDictionary()
        {
            Expression<Func<TestEntity, bool>>? predicate = null;
            var result = predicate!.GetParameters();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetParameters_ConstantValue_ExtractsParameter()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue(1));
        }

        [TestMethod]
        public void GetParameters_StringConstant_ExtractsParameter()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Name == "test";
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue("test"));
        }

        [TestMethod]
        public void GetParameters_CapturedVariable_ExtractsParameter()
        {
            var searchId = 42;
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == searchId;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue(42));
        }

        [TestMethod]
        public void GetParameters_CapturedStringVariable_ExtractsParameter()
        {
            var searchName = "John";
            Expression<Func<TestEntity, bool>> predicate = x => x.Name == searchName;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue("John"));
        }

        [TestMethod]
        public void GetParameters_MultipleConditions_ExtractsAllParameters()
        {
            var minAge = 18;
            var maxAge = 65;
            Expression<Func<TestEntity, bool>> predicate = x => x.Age >= minAge && x.Age <= maxAge;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count >= 2);
            Assert.IsTrue(result.ContainsValue(18));
            Assert.IsTrue(result.ContainsValue(65));
        }

        [TestMethod]
        public void GetParameters_OrCondition_ExtractsAllParameters()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Id == 1 || x.Id == 2;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count >= 2);
            Assert.IsTrue(result.ContainsValue(1));
            Assert.IsTrue(result.ContainsValue(2));
        }

        [TestMethod]
        public void GetParameters_DecimalValue_ExtractsParameter()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.Price > 99.99m;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue(99.99m));
        }

        [TestMethod]
        public void GetParameters_DateTimeValue_ExtractsParameter()
        {
            var date = new DateTime(2024, 1, 1);
            Expression<Func<TestEntity, bool>> predicate = x => x.CreatedAt > date;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue(date));
        }

        [TestMethod]
        public void GetParameters_BooleanValue_ExtractsParameter()
        {
            Expression<Func<TestEntity, bool>> predicate = x => x.IsActive == true;
            var result = predicate.GetParameters();
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.ContainsValue(true));
        }

        #endregion

        #region Cache Tests

        [TestMethod]
        public void GetParameters_SameExpression_UsesCachedCompilation()
        {
            var searchId = 100;
            Expression<Func<TestEntity, bool>> predicate1 = x => x.Id == searchId;
            Expression<Func<TestEntity, bool>> predicate2 = x => x.Id == searchId;

            var result1 = predicate1.GetParameters();
            var result2 = predicate2.GetParameters();

            Assert.IsTrue(result1.ContainsValue(100));
            Assert.IsTrue(result2.ContainsValue(100));
        }

        [TestMethod]
        public void GetParameters_DifferentValues_ExtractsCorrectly()
        {
            var value1 = 10;
            var value2 = 20;

            Expression<Func<TestEntity, bool>> predicate1 = x => x.Id == value1;
            Expression<Func<TestEntity, bool>> predicate2 = x => x.Id == value2;

            var result1 = predicate1.GetParameters();
            var result2 = predicate2.GetParameters();

            Assert.IsTrue(result1.ContainsValue(10));
            Assert.IsTrue(result2.ContainsValue(20));
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void ToWhereClause_ComplexExpression_GeneratesCorrectSql()
        {
            var minAge = 18;
            var name = "John";
            Expression<Func<TestEntity, bool>> predicate = x =>
                (x.Age >= minAge && x.IsActive) || x.Name == name;

            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("age"));
            Assert.IsTrue(result.Contains("is_active"));
            Assert.IsTrue(result.Contains("name"));
        }

        [TestMethod]
        public void GetParameters_MethodCallInExpression_HandlesCorrectly()
        {
            var names = new List<string> { "John", "Jane" };
            Expression<Func<TestEntity, bool>> predicate = x => names.Contains(x.Name!);

            // Should not throw
            var result = predicate.GetParameters();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void ToWhereClause_NegatedBoolean_GeneratesCorrectSql()
        {
            Expression<Func<TestEntity, bool>> predicate = x => !x.IsActive;
            var result = predicate.ToWhereClause();
            Assert.IsTrue(result.Contains("is_active"));
        }

        #endregion
    }
}
