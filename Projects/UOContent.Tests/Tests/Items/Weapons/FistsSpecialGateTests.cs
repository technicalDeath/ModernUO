using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FistsSpecialGateTests
{
    [Fact]
    public void Requests_ClearBothRemovedWrestlingReadyStates()
    {
        var mobile = new Mobile { StunReady = true, DisarmReady = true };

        Fists.StunRequest(mobile);
        Assert.False(mobile.StunReady);

        Fists.DisarmRequest(mobile);
        Assert.False(mobile.DisarmReady);

        mobile.Delete();
    }

    [Fact]
    public void Swing_ClearsPersistedWrestlingReadyStatesBeforeResolution()
    {
        var attacker = new Mobile { StunReady = true, DisarmReady = true };
        var defender = new Mobile();
        var fists = new Fists();

        fists.OnSwing(attacker, defender);

        Assert.False(attacker.StunReady);
        Assert.False(attacker.DisarmReady);

        fists.Delete();
        attacker.Delete();
        defender.Delete();
    }
}
