using System;
using Server;
using Server.SkillHandlers;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UorSkillDelayTests
{
    [Fact]
    public void DetectHiddenUsesTheMarch2000OneSecondDelay()
    {
        var previous = Core.Expansion;
        var player = new Mobile();
        try
        {
            Core.Expansion = Expansion.UOR;
            Assert.Equal(TimeSpan.FromSeconds(1), DetectHidden.OnUse(player));
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
        }
    }
}
