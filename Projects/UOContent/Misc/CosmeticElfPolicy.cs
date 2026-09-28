using Server.Items;

namespace Server.Misc;

public static class CosmeticElfPolicy
{
    public static Race GameplayRace(Mobile mobile) =>
        Core.Expansion == Expansion.UOR && mobile.Player && mobile.AccessLevel == AccessLevel.Player &&
        mobile.Race == Race.Elf ? Race.Human : mobile.Race;

    public static bool CheckRace(Item item, Mobile mobile)
    {
        if (GameplayRace(mobile) == mobile.Race)
        {
            return item.CheckRace(mobile);
        }

        if (Race.IsAllowedRace(Race.Human, item.RequiredRaces))
        {
            return true;
        }

        mobile.SendMessage("This item is not available to your character under the shard's rules.");
        return false;
    }
}
