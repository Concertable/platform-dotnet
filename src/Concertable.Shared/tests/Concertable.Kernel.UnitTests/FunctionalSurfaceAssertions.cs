using System.Reflection;

namespace Concertable.Kernel.UnitTests;

internal static class FunctionalSurfaceAssertions
{
    public static void HasOnlyNamedCaseImplicitConversions(
        Type carrierType,
        params Type[] caseTypes)
    {
        var conversions = carrierType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "op_Implicit")
            .ToArray();

        Assert.Equal(caseTypes.Length, conversions.Length);
        Assert.All(conversions, conversion => Assert.Equal(carrierType, conversion.ReturnType));

        foreach (var caseType in caseTypes)
        {
            Assert.Single(
                conversions,
                conversion => Assert.Single(conversion.GetParameters()).ParameterType == caseType);
        }
    }
}
