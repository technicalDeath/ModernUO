using Xunit;
using Server.Targeting;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class TargetabilityHookTests
{
    private sealed class TestMobile : Mobile
    {
    }

    private sealed class TestTarget : Target
    {
        public TestTarget() : base(12, false, TargetFlags.None)
        {
        }

        public bool Rejected { get; private set; }

        protected override void OnTargetUntargetable(Mobile from, object targeted) => Rejected = true;
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

    [Fact]
    public void TargetInvocationRejectsMobileWhenHookDeniesTargetability()
    {
        var from = new TestMobile();
        var target = new TestMobile();
        var targetRequest = new TestTarget();
        var previous = Mobile.CanTargetHandler;

        try
        {
            Mobile.CanTargetHandler = static _ => false;
            targetRequest.Invoke(from, target);
            Assert.True(targetRequest.Rejected);
        }
        finally
        {
            Mobile.CanTargetHandler = previous;
            target.Delete();
            from.Delete();
        }
    }
}
