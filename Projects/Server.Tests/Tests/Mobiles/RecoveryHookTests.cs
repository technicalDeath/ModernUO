using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class RecoveryHookTests
{
    private sealed class TestMobile : Mobile
    {
    }

    [Fact]
    public void HealHandlerCanInterceptBeforeHitPointsChange()
    {
        var mobile = new TestMobile();
        var previous = Mobile.HealHandler;
        var before = mobile.Hits;

        try
        {
            Mobile.HealHandler = static (_, _, _) => true;
            mobile.Heal(10, mobile);
            Assert.Equal(before, mobile.Hits);
        }
        finally
        {
            Mobile.HealHandler = previous;
            mobile.Delete();
        }
    }

    [Fact]
    public void CurePoisonHandlerCanInterceptBeforeCure()
    {
        var mobile = new TestMobile();
        var previous = Mobile.CurePoisonHandler;

        try
        {
            Mobile.CurePoisonHandler = static (_, _) => true;
            Assert.False(mobile.CurePoison(mobile));
        }
        finally
        {
            Mobile.CurePoisonHandler = previous;
            mobile.Delete();
        }
    }
}
