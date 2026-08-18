using IconReplacer.AppModel;

namespace IconReplacer.Core.Tests;

public sealed class DistributionChannelPolicyTests
{
    [Theory]
    [InlineData("store")]
    [InlineData("STORE")]
    [InlineData("microsoft-store")]
    [InlineData("Microsoft-Store")]
    public void StoreAliasesUseStoreManagedUpdates(string channel)
    {
        Assert.True(DistributionChannelPolicy.IsStoreChannel(channel));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("direct")]
    [InlineData("github")]
    [InlineData("portable")]
    public void DirectAndUnknownChannelsKeepDirectUpdatePolicy(string? channel)
    {
        Assert.False(DistributionChannelPolicy.IsStoreChannel(channel));
    }

    [Fact]
    public void CompiledChannelIsKnown()
    {
        Assert.Contains(
            DistributionChannelPolicy.Current,
            new[] { "direct", "store" },
            StringComparer.OrdinalIgnoreCase);
    }
}
