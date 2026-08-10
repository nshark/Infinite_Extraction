using System.Reflection;
using IE.Controller;
using IE.Model.Diagnostics;
using Xunit;

namespace IE.Tests.Architecture;

/// <summary>
/// Enforces the no-floating-point rule for IE.Model and IE.Controller.
///
/// Float and double are not bit-identical across platforms, compilers, and JIT
/// levels, so any float on the core state path is a desync waiting to happen in
/// networked play. Fractional values in these two assemblies must be fixed-point
/// integers (percent, basis points, scaled-by-1000). Floats live in IE.ViewMath.
///
/// Scope is deliberately wide: fields, properties, parameters, return types, AND
/// method-body locals, because a local is enough to make a calculation drift.
/// </summary>
public sealed class NoFloatingPointTests
{
    private static readonly Assembly[] DeterministicAssemblies =
    {
        typeof(Log).Assembly,
        typeof(GameController).Assembly,
    };

    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void ModelAndControllerContainNoFloatingPoint()
    {
        List<string> offenders = new();

        foreach (Assembly assembly in DeterministicAssemblies)
        {
            foreach (Type type in GetTypes(assembly))
            {
                InspectType(type, assembly.GetName().Name ?? "<unknown>", offenders);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Floating-point found on the deterministic state path. Use fixed-point " +
            "integers, or move the math to IE.ViewMath:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static void InspectType(Type type, string assemblyName, List<string> offenders)
    {
        string origin = $"{assemblyName}::{type.FullName}";

        foreach (FieldInfo field in type.GetFields(AllMembers))
        {
            if (IsFloatingPoint(field.FieldType))
            {
                offenders.Add($"{origin}.{field.Name} -- field of type {Describe(field.FieldType)}");
            }
        }

        foreach (PropertyInfo property in type.GetProperties(AllMembers))
        {
            if (IsFloatingPoint(property.PropertyType))
            {
                offenders.Add($"{origin}.{property.Name} -- property of type {Describe(property.PropertyType)}");
            }
        }

        foreach (MethodBase method in Members(type))
        {
            InspectMethod(method, origin, offenders);
        }
    }

    private static IEnumerable<MethodBase> Members(Type type)
    {
        foreach (MethodInfo method in type.GetMethods(AllMembers))
        {
            yield return method;
        }

        foreach (ConstructorInfo constructor in type.GetConstructors(AllMembers))
        {
            yield return constructor;
        }
    }

    private static void InspectMethod(MethodBase method, string origin, List<string> offenders)
    {
        if (method is MethodInfo info && IsFloatingPoint(info.ReturnType))
        {
            offenders.Add($"{origin}.{method.Name} -- returns {Describe(info.ReturnType)}");
        }

        foreach (ParameterInfo parameter in method.GetParameters())
        {
            if (IsFloatingPoint(parameter.ParameterType))
            {
                offenders.Add($"{origin}.{method.Name}({parameter.Name}) -- parameter of type {Describe(parameter.ParameterType)}");
            }
        }

        MethodBody? body = SafeGetBody(method);
        if (body is null)
        {
            return;
        }

        foreach (LocalVariableInfo local in body.LocalVariables)
        {
            if (IsFloatingPoint(local.LocalType))
            {
                offenders.Add($"{origin}.{method.Name} -- local #{local.LocalIndex} of type {Describe(local.LocalType)}");
            }
        }
    }

    private static MethodBody? SafeGetBody(MethodBase method)
    {
        if (method.IsAbstract || (method.MethodImplementationFlags & MethodImplAttributes.InternalCall) != 0)
        {
            return null;
        }

        try
        {
            return method.GetMethodBody();
        }
        catch (Exception)
        {
            // Some runtime-provided members have no inspectable body. Nothing we
            // wrote lands here, so skipping is safe.
            return null;
        }
    }

    private static bool IsFloatingPoint(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type == typeof(float) || type == typeof(double))
        {
            return true;
        }

        if (type.HasElementType)
        {
            return IsFloatingPoint(type.GetElementType());
        }

        return type.IsGenericType && type.GetGenericArguments().Any(IsFloatingPoint);
    }

    private static IEnumerable<Type> GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static string Describe(Type type) => type.FullName ?? type.Name;
}
