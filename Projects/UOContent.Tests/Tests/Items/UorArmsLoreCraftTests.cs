using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UorArmsLoreCraftTests
{
    [Theory]
    [InlineData(Expansion.UOR, 0)]
    [InlineData(Expansion.AOS, 14)]
    public void ExceptionalPlateUsesResistanceBonusOnlyAfterAos(Expansion era, int expectedBonus)
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var armor = new PlateChest();
        try
        {
            Core.Expansion = era;
            player.DefaultMobileInit();
            player.Player = true;
            if (DefBlacksmithy.CraftSystem is null)
            {
                DefBlacksmithy.Initialize();
            }

            player.Skills.ArmsLore.Base = 100.0;
            armor.OnCraft(2, false, player, DefBlacksmithy.CraftSystem,
                typeof(IronIngot), null, null, 0);

            Assert.Equal(expectedBonus, armor.PhysicalBonus + armor.FireBonus + armor.ColdBonus +
                armor.PoisonBonus + armor.EnergyBonus);
        }
        finally
        {
            Core.Expansion = previous;
            armor.Delete();
            player.Delete();
        }
    }
}
