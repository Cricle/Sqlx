using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.DynamicPlaceholder;

/// <summary>
/// 测试 [DynamicSql] 特性
/// </summary>
[TestClass]
public class DynamicSqlAttributeTests
{
    #region 基本特性测试

    [TestMethod]
    public void DynamicSqlAttribute_DefaultConstructor_SetsTypeToIdentifier()
    {
        // Act
        var attribute = new DynamicSqlAttribute();

        // Assert
        Assert.AreEqual(DynamicSqlType.Identifier, attribute.Type);
    }

    [TestMethod]
    public void DynamicSqlAttribute_SetType_UpdatesType()
    {
        // Arrange
        var attribute = new DynamicSqlAttribute();

        // Act
        attribute.Type = DynamicSqlType.Fragment;

        // Assert
        Assert.AreEqual(DynamicSqlType.Fragment, attribute.Type);
    }

    [TestMethod]
    public void DynamicSqlAttribute_AllTypes_CanBeSet()
    {
        // Arrange
        var types = new[]
        {
            DynamicSqlType.Identifier,
            DynamicSqlType.Fragment,
            DynamicSqlType.TablePart
        };

        // Act & Assert
        foreach (var type in types)
        {
            var attribute = new DynamicSqlAttribute { Type = type };
            Assert.AreEqual(type, attribute.Type, $"Expected Type to be {type}");
        }
    }

    #endregion

    #region AttributeUsage 测试

    [TestMethod]
    public void DynamicSqlAttribute_AttributeUsage_RestrictsToParameter()
    {
        // Arrange
        var attributeType = typeof(DynamicSqlAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(attributeUsage, "DynamicSqlAttribute should have AttributeUsage");
        Assert.AreEqual(AttributeTargets.Parameter, attributeUsage.ValidOn,
            "DynamicSqlAttribute should only be valid on parameters");
        Assert.IsFalse(attributeUsage.AllowMultiple,
            "DynamicSqlAttribute should not allow multiple instances");
    }

    #endregion

    #region 反射测试（模拟编译器行为）

    // 测试接口定义
    private interface ITestRepository
    {
        void MethodWithIdentifier([DynamicSql] string tableName);
        void MethodWithFragment([DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);
        void MethodWithTablePart([DynamicSql(Type = DynamicSqlType.TablePart)] string suffix);
    }

    [TestMethod]
    public void DynamicSqlAttribute_OnParameter_CanBeRetrieved()
    {
        // Arrange
        var method = typeof(ITestRepository).GetMethod(nameof(ITestRepository.MethodWithIdentifier));
        var parameter = method!.GetParameters()[0];

        // Act
        var attribute = parameter.GetCustomAttribute<DynamicSqlAttribute>();

        // Assert
        Assert.IsNotNull(attribute, "DynamicSqlAttribute should be present on parameter");
        Assert.AreEqual(DynamicSqlType.Identifier, attribute.Type);
    }

    [TestMethod]
    public void DynamicSqlAttribute_WithFragmentType_CanBeRetrieved()
    {
        // Arrange
        var method = typeof(ITestRepository).GetMethod(nameof(ITestRepository.MethodWithFragment));
        var parameter = method!.GetParameters()[0];

        // Act
        var attribute = parameter.GetCustomAttribute<DynamicSqlAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(DynamicSqlType.Fragment, attribute.Type);
    }

    [TestMethod]
    public void DynamicSqlAttribute_WithTablePartType_CanBeRetrieved()
    {
        // Arrange
        var method = typeof(ITestRepository).GetMethod(nameof(ITestRepository.MethodWithTablePart));
        var parameter = method!.GetParameters()[0];

        // Act
        var attribute = parameter.GetCustomAttribute<DynamicSqlAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(DynamicSqlType.TablePart, attribute.Type);
    }

    #endregion

    #region DynamicSqlType 枚举测试

    [TestMethod]
    public void DynamicSqlType_HasExpectedValues()
    {
        // Assert
        Assert.AreEqual(0, (int)DynamicSqlType.Identifier);
        Assert.AreEqual(1, (int)DynamicSqlType.Fragment);
        Assert.AreEqual(2, (int)DynamicSqlType.TablePart);
    }

    [TestMethod]
    public void DynamicSqlType_AllValuesAreDefined()
    {
        // Act
        var values = Enum.GetValues(typeof(DynamicSqlType)).Cast<DynamicSqlType>().ToArray();

        // Assert
        Assert.AreEqual(3, values.Length, "DynamicSqlType should have exactly 3 values");
        CollectionAssert.Contains(values, DynamicSqlType.Identifier);
        CollectionAssert.Contains(values, DynamicSqlType.Fragment);
        CollectionAssert.Contains(values, DynamicSqlType.TablePart);
    }

    [TestMethod]
    public void DynamicSqlType_CanConvertToString()
    {
        // Act & Assert
        Assert.AreEqual("Identifier", DynamicSqlType.Identifier.ToString());
        Assert.AreEqual("Fragment", DynamicSqlType.Fragment.ToString());
        Assert.AreEqual("TablePart", DynamicSqlType.TablePart.ToString());
    }

    #endregion

    #region 命名空间和可访问性测试

    [TestMethod]
    public void DynamicSqlAttribute_IsInSqlxNamespace()
    {
        // Arrange
        var attributeType = typeof(DynamicSqlAttribute);

        // Assert
        Assert.AreEqual("Sqlx.Annotations", attributeType.Namespace);
    }

    [TestMethod]
    public void DynamicSqlType_IsInSqlxNamespace()
    {
        // Arrange
        var enumType = typeof(DynamicSqlType);

        // Assert
        Assert.AreEqual("Sqlx.Annotations", enumType.Namespace);
    }

    [TestMethod]
    public void DynamicSqlAttribute_IsPublic()
    {
        // Arrange
        var attributeType = typeof(DynamicSqlAttribute);

        // Assert
        Assert.IsTrue(attributeType.IsPublic, "DynamicSqlAttribute should be public");
    }

    [TestMethod]
    public void DynamicSqlAttribute_IsSealed()
    {
        // Arrange
        var attributeType = typeof(DynamicSqlAttribute);

        // Assert
        Assert.IsTrue(attributeType.IsSealed, "DynamicSqlAttribute should be sealed");
    }

    #endregion
}

