using Sbroenne.WindowsMcp.Tests.Integration;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class NotepadTypingEvidenceTests
{
    [Fact]
    public void NotepadCollection_IsExclusiveAndDoesNotLaunchAHarness()
    {
        var collection = Assert.Single(typeof(NotepadTypingTests).CustomAttributes,
            attribute => attribute.AttributeType == typeof(CollectionAttribute));
        Assert.Equal("NotepadTyping", collection.ConstructorArguments[0].Value);
        var definition = Assert.Single(typeof(NotepadTypingTestGroup).CustomAttributes,
            attribute => attribute.AttributeType == typeof(CollectionDefinitionAttribute));
        Assert.Equal("NotepadTyping", definition.ConstructorArguments[0].Value);
        Assert.Contains(definition.NamedArguments,
            argument => argument.MemberName == "DisableParallelization" && argument.TypedValue.Value is true);
        Assert.Empty(typeof(NotepadTypingTestGroup).GetInterfaces());
    }

    [Fact]
    public void IndividualInputs_PreserveOriginalCodeUnitCalls()
    {
        const string text = "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done";
        var inputs = NotepadTypingTests.GetIndividualInputs(text, wholeCharacters: false).ToArray();

        Assert.Equal(text.Length, inputs.Length);
        Assert.All(inputs, input => Assert.Single(input));
        Assert.Equal(text, string.Concat(inputs));
    }

    [Fact]
    public void CompleteCharacterInputs_KeepEmojiInOneCall()
    {
        const string text = "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done";
        var inputs = NotepadTypingTests.GetIndividualInputs(text, wholeCharacters: true).ToArray();

        Assert.Equal(text.Length - 1, inputs.Length);
        Assert.Contains("\U0001f680", inputs);
        Assert.DoesNotContain(inputs, input => input.Length == 1 && char.IsSurrogate(input[0]));
        Assert.Equal(text, string.Concat(inputs));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("", true)]
    [InlineData("a\r\nb\t ", false)]
    [InlineData("a\r\nb\t ", true)]
    public void IndividualInputs_DoNotChangeRequestedText(string text, bool wholeCharacters)
    {
        ArgumentNullException.ThrowIfNull(text);
        Assert.Equal(text, string.Concat(NotepadTypingTests.GetIndividualInputs(text, wholeCharacters)));
    }
}
