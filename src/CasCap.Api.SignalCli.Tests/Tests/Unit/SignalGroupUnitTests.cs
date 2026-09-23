namespace CasCap.Tests.Unit;

/// <summary>Unit tests for <see cref="SignalGroup"/> identifier matching.</summary>
[Trait("Category", "Unit")]
public class SignalGroupUnitTests
{
    [Theory]
    [InlineData("group.cHJlZml4ZWQ=", true)]
    [InlineData("aW50ZXJuYWw=", true)]
    [InlineData("GROUP.cHJlZml4ZWQ=", false)]
    [InlineData("unknown", false)]
    [InlineData(null, false)]
    public void Matches_RecognizesBothOpaqueIdentifierForms(string? candidate, bool expected)
    {
        // Regression for #6: group responses and inbound envelopes use distinct identifiers.
        var group = new SignalGroup
        {
            Id = "group.cHJlZml4ZWQ=",
            InternalId = "aW50ZXJuYWw=",
            Name = "Synthetic group",
        };

        Assert.Equal(expected, group.Matches(candidate));
    }
}