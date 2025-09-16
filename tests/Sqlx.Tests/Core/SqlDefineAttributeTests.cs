// -----------------------------------------------------------------------
// <copyright file="SqlDefineAttributeTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// SqlDefineAttribute 单元测试
    /// 验证 SqlDefine 特性支持枚举类型参数
    /// </summary>
    [TestClass]
    public class SqlDefineAttributeTests
    {
        #region Enum Constructor Tests - 验证枚举构造函数

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_MySql_SetsCorrectProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute(SqlDefineTypes.MySql);

            // Assert
            Assert.AreEqual(SqlDefineTypes.MySql, attribute.DialectType);
            Assert.AreEqual("MySql", attribute.DialectName);
            Assert.IsNull(attribute.ColumnLeft);
            Assert.IsNull(attribute.ColumnRight);
            
            Console.WriteLine($"✅ MySql 枚举构造函数: {attribute.DialectType} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_SqlServer_SetsCorrectProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute(SqlDefineTypes.SqlServer);

            // Assert
            Assert.AreEqual(SqlDefineTypes.SqlServer, attribute.DialectType);
            Assert.AreEqual("SqlServer", attribute.DialectName);
            
            Console.WriteLine($"✅ SqlServer 枚举构造函数: {attribute.DialectType} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_PostgreSql_SetsCorrectProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute(SqlDefineTypes.PostgreSql);

            // Assert
            Assert.AreEqual(SqlDefineTypes.PostgreSql, attribute.DialectType);
            Assert.AreEqual("PostgreSql", attribute.DialectName);
            
            Console.WriteLine($"✅ PostgreSql 枚举构造函数: {attribute.DialectType} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_Oracle_SetsCorrectProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute(SqlDefineTypes.Oracle);

            // Assert
            Assert.AreEqual(SqlDefineTypes.Oracle, attribute.DialectType);
            Assert.AreEqual("Oracle", attribute.DialectName);
            
            Console.WriteLine($"✅ Oracle 枚举构造函数: {attribute.DialectType} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_DB2_SetsCorrectProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute(SqlDefineTypes.DB2);

            // Assert
            Assert.AreEqual(SqlDefineTypes.DB2, attribute.DialectType);
            Assert.AreEqual("DB2", attribute.DialectName);
            
            Console.WriteLine($"✅ DB2 枚举构造函数: {attribute.DialectType} -> {attribute.DialectName}");
        }

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_SQLite_SetsCorrectProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute(SqlDefineTypes.SQLite);

            // Assert
            Assert.AreEqual(SqlDefineTypes.SQLite, attribute.DialectType);
            Assert.AreEqual("SQLite", attribute.DialectName);
            
            Console.WriteLine($"✅ SQLite 枚举构造函数: {attribute.DialectType} -> {attribute.DialectName}");
        }

        #endregion

        #region String Constructor Backward Compatibility Tests - 验证字符串构造函数向后兼容性

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_ValidDialect_ParsesEnumCorrectly()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute("MySql");

            // Assert
            Assert.AreEqual(SqlDefineTypes.MySql, attribute.DialectType);
            Assert.AreEqual("MySql", attribute.DialectName);
            
            Console.WriteLine($"✅ 字符串构造函数解析: MySql -> {attribute.DialectType}");
        }

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_CaseInsensitive_ParsesCorrectly()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute("sqlserver");

            // Assert
            Assert.AreEqual(SqlDefineTypes.SqlServer, attribute.DialectType);
            Assert.AreEqual("sqlserver", attribute.DialectName);
            
            Console.WriteLine($"✅ 大小写不敏感解析: sqlserver -> {attribute.DialectType}");
        }

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_InvalidDialect_KeepsStringOnly()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute("InvalidDialect");

            // Assert
            Assert.IsNull(attribute.DialectType);
            Assert.AreEqual("InvalidDialect", attribute.DialectName);
            
            Console.WriteLine($"✅ 无效方言处理: InvalidDialect -> {attribute.DialectType}");
        }

        [TestMethod]
        public void SqlDefineAttribute_StringConstructor_NullDialect_ThrowsException()
        {
            // Arrange, Act & Assert
            var exception = Assert.ThrowsException<ArgumentNullException>(() => 
                new SqlDefineAttribute((string)null!));
            
            Assert.AreEqual("dialectName", exception.ParamName);
            
            Console.WriteLine($"✅ NULL 参数检查: {exception.Message}");
        }

        #endregion

        #region Custom Constructor Tests - 验证自定义构造函数

        [TestMethod]
        public void SqlDefineAttribute_CustomConstructor_SetsAllProperties()
        {
            // Arrange & Act
            var attribute = new SqlDefineAttribute("`", "`", "'", "'", "@");

            // Assert
            Assert.IsNull(attribute.DialectType);
            Assert.IsNull(attribute.DialectName);
            Assert.AreEqual("`", attribute.ColumnLeft);
            Assert.AreEqual("`", attribute.ColumnRight);
            Assert.AreEqual("'", attribute.StringLeft);
            Assert.AreEqual("'", attribute.StringRight);
            Assert.AreEqual("@", attribute.ParameterPrefix);
            
            Console.WriteLine($"✅ 自定义构造函数: `{attribute.ColumnLeft}column{attribute.ColumnRight}` with {attribute.ParameterPrefix}param");
        }

        [TestMethod]
        public void SqlDefineAttribute_CustomConstructor_NullParameters_ThrowsException()
        {
            // Test each null parameter
            Assert.ThrowsException<ArgumentNullException>(() => 
                new SqlDefineAttribute(null!, "]", "'", "'", "@"));
            Assert.ThrowsException<ArgumentNullException>(() => 
                new SqlDefineAttribute("[", null!, "'", "'", "@"));
            Assert.ThrowsException<ArgumentNullException>(() => 
                new SqlDefineAttribute("[", "]", null!, "'", "@"));
            Assert.ThrowsException<ArgumentNullException>(() => 
                new SqlDefineAttribute("[", "]", "'", null!, "@"));
            Assert.ThrowsException<ArgumentNullException>(() => 
                new SqlDefineAttribute("[", "]", "'", "'", null!));
            
            Console.WriteLine("✅ 自定义构造函数 NULL 参数检查通过");
        }

        #endregion

        #region Enum Values Completeness Tests - 验证枚举值完整性

        [TestMethod]
        public void SqlDefineTypes_AllEnumValues_HaveCorrectIntegerValues()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0, (int)SqlDefineTypes.MySql);
            Assert.AreEqual(1, (int)SqlDefineTypes.SqlServer);
            Assert.AreEqual(2, (int)SqlDefineTypes.PostgreSql);
            Assert.AreEqual(3, (int)SqlDefineTypes.Oracle);
            Assert.AreEqual(4, (int)SqlDefineTypes.DB2);
            Assert.AreEqual(5, (int)SqlDefineTypes.SQLite);
            
            Console.WriteLine("✅ 所有枚举值都有正确的整数映射");
        }

        [TestMethod]
        public void SqlDefineTypes_EnumToString_ReturnsCorrectNames()
        {
            // Arrange & Act & Assert
            Assert.AreEqual("MySql", SqlDefineTypes.MySql.ToString());
            Assert.AreEqual("SqlServer", SqlDefineTypes.SqlServer.ToString());
            Assert.AreEqual("PostgreSql", SqlDefineTypes.PostgreSql.ToString());
            Assert.AreEqual("Oracle", SqlDefineTypes.Oracle.ToString());
            Assert.AreEqual("DB2", SqlDefineTypes.DB2.ToString());
            Assert.AreEqual("SQLite", SqlDefineTypes.SQLite.ToString());
            
            Console.WriteLine("✅ 所有枚举值都有正确的字符串表示");
        }

        [TestMethod]
        public void SqlDefineTypes_AllValues_CanBeUsedInAttribute()
        {
            // Test that all enum values can be used in attribute constructor
            var enumValues = (SqlDefineTypes[])Enum.GetValues(typeof(SqlDefineTypes));
            
            foreach (var enumValue in enumValues)
            {
                // This should not throw any exception
                var attribute = new SqlDefineAttribute(enumValue);
                Assert.IsNotNull(attribute);
                Assert.AreEqual(enumValue, attribute.DialectType);
                
                Console.WriteLine($"✅ 枚举值 {enumValue} 可以正常用于特性构造");
            }
        }

        #endregion

        #region Practical Usage Tests - 验证实际使用场景

        // 模拟实际使用场景的测试类
        [SqlDefine(SqlDefineTypes.MySql)]
        private class MySqlRepository
        {
            [SqlDefine(SqlDefineTypes.SqlServer)]
            public void MethodWithDifferentDialect() { }
        }

        [SqlDefine("PostgreSql")]
        private class StringDialectRepository
        {
            [SqlDefine(SqlDefineTypes.Oracle)]
            public void MethodWithEnumDialect() { }
        }

        [TestMethod]
        public void SqlDefineAttribute_ClassLevelEnum_CanBeApplied()
        {
            // Arrange
            var type = typeof(MySqlRepository);
            
            // Act
            var attributes = type.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            
            // Assert
            Assert.AreEqual(1, attributes.Length);
            var attribute = (SqlDefineAttribute)attributes[0];
            Assert.AreEqual(SqlDefineTypes.MySql, attribute.DialectType);
            
            Console.WriteLine($"✅ 类级别枚举特性: {attribute.DialectType}");
        }

        [TestMethod]
        public void SqlDefineAttribute_MethodLevelEnum_CanBeApplied()
        {
            // Arrange
            var method = typeof(MySqlRepository).GetMethod(nameof(MySqlRepository.MethodWithDifferentDialect))!;
            
            // Act
            var attributes = method.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            
            // Assert
            Assert.AreEqual(1, attributes.Length);
            var attribute = (SqlDefineAttribute)attributes[0];
            Assert.AreEqual(SqlDefineTypes.SqlServer, attribute.DialectType);
            
            Console.WriteLine($"✅ 方法级别枚举特性: {attribute.DialectType}");
        }

        [TestMethod]
        public void SqlDefineAttribute_MixedUsage_BothStringAndEnum_Work()
        {
            // Test class with string attribute
            var classType = typeof(StringDialectRepository);
            var classAttributes = classType.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            var classAttribute = (SqlDefineAttribute)classAttributes[0];
            
            Assert.AreEqual("PostgreSql", classAttribute.DialectName);
            Assert.AreEqual(SqlDefineTypes.PostgreSql, classAttribute.DialectType);
            
            // Test method with enum attribute
            var method = classType.GetMethod(nameof(StringDialectRepository.MethodWithEnumDialect))!;
            var methodAttributes = method.GetCustomAttributes(typeof(SqlDefineAttribute), false);
            var methodAttribute = (SqlDefineAttribute)methodAttributes[0];
            
            Assert.AreEqual(SqlDefineTypes.Oracle, methodAttribute.DialectType);
            Assert.AreEqual("Oracle", methodAttribute.DialectName);
            
            Console.WriteLine($"✅ 混合使用: 类({classAttribute.DialectName}) + 方法({methodAttribute.DialectType})");
        }

        #endregion

        #region Performance Tests - 性能测试

        [TestMethod]
        public void SqlDefineAttribute_EnumConstructor_Performance_IsAcceptable()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Create 10000 attributes with enum constructor
            for (int i = 0; i < 10000; i++)
            {
                var attribute = new SqlDefineAttribute(SqlDefineTypes.MySql);
                Assert.IsNotNull(attribute);
            }
            
            stopwatch.Stop();
            
            // Should be much faster than 100ms for 10000 creations
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"枚举构造函数性能测试失败: {stopwatch.ElapsedMilliseconds}ms > 100ms");
            
            Console.WriteLine($"✅ 枚举构造函数性能: 10000次创建耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void SqlDefineAttribute_StringParsing_Performance_IsAcceptable()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Create 1000 attributes with string constructor (parsing is slower)
            for (int i = 0; i < 1000; i++)
            {
                var attribute = new SqlDefineAttribute("MySql");
                Assert.IsNotNull(attribute);
            }
            
            stopwatch.Stop();
            
            // Should be faster than 50ms for 1000 creations
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50, 
                $"字符串解析性能测试失败: {stopwatch.ElapsedMilliseconds}ms > 50ms");
            
            Console.WriteLine($"✅ 字符串解析性能: 1000次创建耗时 {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}

