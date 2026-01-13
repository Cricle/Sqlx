using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;
using System.Reflection;

namespace Sqlx.Tests;

/// <summary>
/// Tests to verify AOT compatibility - no reflection usage in core types.
/// </summary>
[TestClass]
public class AotCompatibilityTests
{
    private static readonly string[] ReflectionNamespaces = new[]
    {
        "System.Reflection",
        "System.Runtime.Emit",
    };

    private static readonly string[] ForbiddenKeywords = new[]
    {
        "dynamic",
        "Type.GetType",
        "Activator.CreateInstance",
        "MethodInfo",
        "PropertyInfo",
        "FieldInfo",
        "Assembly.Load",
    };

    [TestMethod]
    public void IEntityProvider_Interface_DoesNotRequireReflection()
    {
        // IEntityProvider interface should be implementable without reflection
        var interfaceType = typeof(IEntityProvider);
        
        // Verify interface members don't require reflection types
        var members = interfaceType.GetMembers();
        foreach (var member in members)
        {
            if (member is PropertyInfo prop)
            {
                Assert.IsFalse(
                    IsReflectionType(prop.PropertyType),
                    $"Property {prop.Name} uses reflection type {prop.PropertyType.Name}");
            }
            else if (member is MethodInfo method)
            {
                Assert.IsFalse(
                    IsReflectionType(method.ReturnType),
                    $"Method {method.Name} returns reflection type {method.ReturnType.Name}");
                
                foreach (var param in method.GetParameters())
                {
                    Assert.IsFalse(
                        IsReflectionType(param.ParameterType),
                        $"Method {method.Name} parameter {param.Name} uses reflection type {param.ParameterType.Name}");
                }
            }
        }
    }

    [TestMethod]
    public void IParameterBinder_Interface_DoesNotRequireReflection()
    {
        var interfaceType = typeof(IParameterBinder<>);
        
        var members = interfaceType.GetMembers();
        foreach (var member in members)
        {
            if (member is MethodInfo method)
            {
                Assert.IsFalse(
                    IsReflectionType(method.ReturnType),
                    $"Method {method.Name} returns reflection type {method.ReturnType.Name}");
                
                foreach (var param in method.GetParameters())
                {
                    // Skip generic type parameters
                    if (param.ParameterType.IsGenericParameter) continue;
                    
                    Assert.IsFalse(
                        IsReflectionType(param.ParameterType),
                        $"Method {method.Name} parameter {param.Name} uses reflection type {param.ParameterType.Name}");
                }
            }
        }
    }

    [TestMethod]
    public void IResultReader_Interface_DoesNotRequireReflection()
    {
        var interfaceType = typeof(IResultReader<>);
        
        var members = interfaceType.GetMembers();
        foreach (var member in members)
        {
            if (member is MethodInfo method)
            {
                // Skip generic return types
                if (!method.ReturnType.IsGenericParameter)
                {
                    Assert.IsFalse(
                        IsReflectionType(method.ReturnType),
                        $"Method {method.Name} returns reflection type {method.ReturnType.Name}");
                }
            }
        }
    }

    [TestMethod]
    public void ColumnMeta_Record_IsAotCompatible()
    {
        // ColumnMeta should be a simple record without reflection
        var type = typeof(ColumnMeta);
        
        // Records are classes but are AOT compatible as they don't use reflection
        Assert.IsTrue(type.IsClass, "ColumnMeta should be a class (record)");
        
        // Constructor should not use reflection
        var constructors = type.GetConstructors();
        foreach (var ctor in constructors)
        {
            foreach (var param in ctor.GetParameters())
            {
                Assert.IsFalse(
                    IsReflectionType(param.ParameterType),
                    $"Constructor parameter {param.Name} uses reflection type");
            }
        }
        
        // Verify it can be instantiated without reflection
        var instance = new ColumnMeta("test", "Test", DbType.String, false);
        Assert.IsNotNull(instance);
        Assert.AreEqual("test", instance.Name);
    }

    [TestMethod]
    public void PlaceholderProcessor_DoesNotUseDynamic()
    {
        var type = typeof(PlaceholderProcessor);
        
        // Check that no methods return dynamic
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        foreach (var method in methods)
        {
            Assert.AreNotEqual(
                "Object", 
                method.ReturnType.Name,
                $"Method {method.Name} should not return Object (potential dynamic usage)");
        }
    }

    [TestMethod]
    public void SqlTemplate_DoesNotUseDynamic()
    {
        var type = typeof(SqlTemplate);
        
        // Check properties don't use dynamic
        var properties = type.GetProperties();
        foreach (var prop in properties)
        {
            Assert.IsFalse(
                prop.PropertyType == typeof(object) && prop.Name != "DynamicParameters",
                $"Property {prop.Name} should not be of type object (potential dynamic usage)");
        }
    }

    [TestMethod]
    public void PlaceholderContext_DoesNotRequireReflection()
    {
        var type = typeof(PlaceholderContext);
        
        var properties = type.GetProperties();
        foreach (var prop in properties)
        {
            Assert.IsFalse(
                IsReflectionType(prop.PropertyType),
                $"Property {prop.Name} uses reflection type {prop.PropertyType.Name}");
        }
    }

    [TestMethod]
    public void SqlDefine_StaticInstances_ArePrecomputed()
    {
        // Verify SqlDefine instances are static and don't require runtime reflection
        Assert.IsNotNull(SqlDefine.SQLite);
        Assert.IsNotNull(SqlDefine.MySql);
        Assert.IsNotNull(SqlDefine.PostgreSql);
        Assert.IsNotNull(SqlDefine.SqlServer);
        Assert.IsNotNull(SqlDefine.Oracle);
        
        // Verify they are different instances
        Assert.AreNotSame(SqlDefine.SQLite, SqlDefine.MySql);
        Assert.AreNotSame(SqlDefine.MySql, SqlDefine.PostgreSql);
    }

    [TestMethod]
    public void IPlaceholderHandler_Interface_IsAotCompatible()
    {
        var interfaceType = typeof(IPlaceholderHandler);
        
        var members = interfaceType.GetMembers();
        foreach (var member in members)
        {
            if (member is MethodInfo method)
            {
                Assert.IsFalse(
                    IsReflectionType(method.ReturnType),
                    $"Method {method.Name} returns reflection type");
            }
        }
    }

    private static bool IsReflectionType(Type type)
    {
        if (type == null) return false;
        
        var ns = type.Namespace ?? string.Empty;
        
        // Check if type is from reflection namespace
        foreach (var reflectionNs in ReflectionNamespaces)
        {
            if (ns.StartsWith(reflectionNs))
            {
                // Exception: Type itself is allowed (for EntityType property)
                if (type == typeof(Type)) return false;
                return true;
            }
        }
        
        return false;
    }
}
