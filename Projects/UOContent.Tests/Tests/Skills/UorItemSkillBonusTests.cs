using Server;
using Server.Items;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UorItemSkillBonusTests
{
    [Theory]
    [InlineData(Expansion.UOR, 0.0)]
    [InlineData(Expansion.AOS, 10.0)]
    public void LaterEraSkillBonusAppliesOnlyWhenTheSkillExists(Expansion expansion, double expected)
    {
        var previous = Core.Expansion;
        var owner = new Item(0x1);
        var player = new Mobile();

        try
        {
            Core.Expansion = expansion;
            var bonuses = new AosSkillBonuses(owner);
            bonuses.SetValues(0, SkillName.Necromancy, 10.0);
            bonuses.AddTo(player);

            Assert.Equal(expected, player.Skills[SkillName.Necromancy].Value);
            bonuses.Remove();
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            owner.Delete();
        }
    }
}
