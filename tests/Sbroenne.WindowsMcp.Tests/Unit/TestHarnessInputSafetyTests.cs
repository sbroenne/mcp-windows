using System.Reflection;
using Sbroenne.WindowsMcp.Tests.Integration;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class TestHarnessInputSafetyTests
{
    [Fact]
    public void HarnessInputTests_RequireDesktopOptInBeforeConstruction()
    {
        Assert.Equal(typeof(DesktopInputTestBase), typeof(TestHarnessInputTests).BaseType);
    }

    [Fact]
    public void HarnessInputTests_UseSkippableFacts()
    {
        var facts = typeof(TestHarnessInputTests).GetMethods()
            .Select(method => method.GetCustomAttribute<FactAttribute>())
            .Where(attribute => attribute is not null).ToArray();

        Assert.NotEmpty(facts);
        Assert.All(facts, attribute => Assert.IsType<SkippableFactAttribute>(attribute));
    }

    [Fact]
    public void HarnessInputTests_AreExcludedFromNonDesktopRuns()
    {
        Assert.Contains(typeof(TestHarnessInputTests).GetCustomAttributesData(), attribute =>
            attribute.AttributeType == typeof(TraitAttribute) &&
            attribute.ConstructorArguments[0].Value as string == "Category" &&
            attribute.ConstructorArguments[1].Value as string == "RequiresDesktop");
    }
}
