using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class MurdererHookTests
{
    private sealed class TestMobile : Mobile
    {
    }

    [Fact]
    public void AdditionalMurdererHandlerExtendsStockStatus()
    {
        var mobile = new TestMobile();
        var previous = Mobile.AdditionalMurdererHandler;

        try
        {
            Mobile.AdditionalMurdererHandler = static _ => true;
            Assert.True(mobile.Murderer);
            Mobile.AdditionalMurdererHandler = static _ => false;
            Assert.False(mobile.Murderer);
            mobile.Kills = 5;
            Assert.True(mobile.Murderer);
            Mobile.LegacyMurdererCountsEnabled = false;
            Assert.False(mobile.Murderer);
        }
        finally
        {
            Mobile.AdditionalMurdererHandler = previous;
            Mobile.LegacyMurdererCountsEnabled = true;
            mobile.Delete();
        }
    }
}
