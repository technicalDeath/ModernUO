using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class TargetabilityHookTests
{
    private sealed class TestMobile : Mobile
    {
    }

    [Fact]
    public void CanTargetHandlerCanMakeMobileUntargetable()
    {
        var mobile = new TestMobile();
        var previous = Mobile.CanTargetHandler;

        try
        {
            Mobile.CanTargetHandler = static _ => false;
            Assert.False(mobile.CanTarget);
        }
        finally
        {
            Mobile.CanTargetHandler = previous;
            mobile.Delete();
        }
    }

    [Fact]
    public void CanTargetRemainsTrueWhenHandlerIsNotInstalled()
    {
        var mobile = new TestMobile();
        var previous = Mobile.CanTargetHandler;

        try
        {
            Mobile.CanTargetHandler = null;
            Assert.True(mobile.CanTarget);
        }
        finally
        {
            Mobile.CanTargetHandler = previous;
            mobile.Delete();
        }
    }
}
