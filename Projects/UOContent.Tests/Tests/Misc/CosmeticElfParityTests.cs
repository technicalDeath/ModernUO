using System;
using Server;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Misc;

[Collection("Sequential UOContent Tests")]
public class CosmeticElfParityTests
{
    private static PlayerMobile NewPlayer(Race race, bool female, int x)
    {
        var player = new PlayerMobile();
        player.DefaultMobileInit();
        player.Player = true;
        player.Female = female;
        player.RawStr = 60;
        player.RawDex = 35;
        player.RawInt = 25;
        player.Race = race;
        player.MoveToWorld(new Point3D(x, 900, 0), Map.Felucca);
        return player;
    }

    [Fact]
    public void UorElfDerivesTheSameGameplayValuesAsHuman()
    {
        var previous = Core.Expansion;
        PlayerMobile human = null;
        PlayerMobile elf = null;

        try
        {
            Core.Expansion = Expansion.UOR;
            RegenRates.Configure();
            human = NewPlayer(Race.Human, false, 910);
            elf = NewPlayer(Race.Elf, false, 911);

            Assert.Equal(human.MaxWeight, elf.MaxWeight);
            Assert.Equal(human.HitsMax, elf.HitsMax);
            Assert.Equal(human.StamMax, elf.StamMax);
            Assert.Equal(human.ManaMax, elf.ManaMax);
            Assert.Equal(human.RacialSkillBonus, elf.RacialSkillBonus);
            Assert.Equal(human.Skills[SkillName.Anatomy].Value, elf.Skills[SkillName.Anatomy].Value);

            foreach (var type in Enum.GetValues<ResistanceType>())
            {
                Assert.Equal(human.GetMaxResistance(type), elf.GetMaxResistance(type));
                Assert.Equal(human.GetResistance(type), elf.GetResistance(type));
            }

            human.ComputeBaseLightLevels(out var humanGlobal, out var humanPersonal);
            elf.ComputeBaseLightLevels(out var elfGlobal, out var elfPersonal);
            Assert.Equal(humanGlobal, elfGlobal);
            Assert.Equal(humanPersonal, elfPersonal);

            Assert.Equal(Mobile.GetHitsRegenRate(human), Mobile.GetHitsRegenRate(elf));
            Assert.Equal(Mobile.GetStamRegenRate(human), Mobile.GetStamRegenRate(elf));
            Assert.Equal(Mobile.GetManaRegenRate(human), Mobile.GetManaRegenRate(elf));
        }
        finally
        {
            Core.Expansion = previous;
            human?.Delete();
            elf?.Delete();
        }
    }

    [Fact]
    public void MlElfStillDiffersSoTheParityTestIsNotVacuous()
    {
        var previous = Core.Expansion;
        PlayerMobile human = null;
        PlayerMobile elf = null;

        try
        {
            Core.Expansion = Expansion.ML;
            human = NewPlayer(Race.Human, false, 912);
            elf = NewPlayer(Race.Elf, false, 913);

            Assert.Equal(human.ManaMax + 20, elf.ManaMax);
            Assert.NotEqual(human.MaxWeight, elf.MaxWeight);
        }
        finally
        {
            Core.Expansion = previous;
            human?.Delete();
            elf?.Delete();
        }
    }

    [Theory]
    [InlineData(false, 605, 607)]
    [InlineData(true, 606, 608)]
    public void ElfKeepsItsNativeBodyAndHairThroughDeathResurrectionAndSave(bool female, int alive, int ghost)
    {
        var previous = Core.Expansion;
        PlayerMobile elf = null;
        PlayerMobile copy = null;

        try
        {
            Core.Expansion = Expansion.UOR;
            elf = NewPlayer(Race.Elf, female, 914 + (female ? 1 : 0));
            elf.Hue = 0x4DE | 0x8000;
            elf.HairItemID = female ? 0x2FCC : 0x2FBF;
            elf.HairHue = 0x853;
            elf.FacialHairItemID = 0;

            Assert.Equal(alive, elf.Body.BodyID);
            Assert.True(elf.Body.IsHuman);
            Assert.Equal(female, elf.Body.IsFemale);

            elf.Kill();

            Assert.False(elf.Alive);
            Assert.Equal(ghost, elf.Body.BodyID);
            Assert.True(elf.Body.IsGhost);
            Assert.Same(Race.Elf, elf.Race);

            elf.Resurrect();

            Assert.True(elf.Alive);
            Assert.Equal(alive, elf.Body.BodyID);
            Assert.Equal(0x4DE | 0x8000, elf.Hue);
            Assert.Equal(female ? 0x2FCC : 0x2FBF, elf.HairItemID);
            Assert.Equal(0x853, elf.HairHue);
            Assert.Equal(0, elf.FacialHairItemID);

            var writer = new BufferWriter(true);
            elf.Serialize(writer);
            var bytes = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();

            copy = new PlayerMobile(World.NewMobile);
            copy.Deserialize(new BufferReader(bytes));

            Assert.Same(Race.Elf, copy.Race);
            Assert.Equal(alive, copy.Body.BodyID);
            Assert.Equal(0x4DE | 0x8000, copy.Hue);
            Assert.Equal(female ? 0x2FCC : 0x2FBF, copy.HairItemID);
            Assert.Equal(0x853, copy.HairHue);
            Assert.Equal(0, copy.FacialHairItemID);
        }
        finally
        {
            Core.Expansion = previous;
            elf?.Delete();
            copy?.Delete();
        }
    }

    [Theory]
    [InlineData(605, true, false)]
    [InlineData(606, false, true)]
    public void ElfBodiesAreHumanTypedWithTheRightGender(int body, bool male, bool female)
    {
        var b = new Body(body);

        Assert.True(b.IsHuman);
        Assert.Equal(male, b.IsMale);
        Assert.Equal(female, b.IsFemale);
        Assert.False(b.IsGhost);
        Assert.True(new Body(body + 2).IsGhost);
        Assert.False(new Body(body + 2).IsHuman);
    }

    [Fact]
    public void UorElfCannotEquipElvenOnlyGearButCanWearHumanGear()
    {
        var previous = Core.Expansion;
        PlayerMobile elf = null;
        var elvenBoots = new ElvenBoots();
        var boots = new Boots();

        try
        {
            Core.Expansion = Expansion.UOR;
            elf = NewPlayer(Race.Elf, false, 916);

            Assert.True(CosmeticElfPolicy.CheckRace(boots, elf));
            Assert.False(CosmeticElfPolicy.CheckRace(elvenBoots, elf));
        }
        finally
        {
            Core.Expansion = previous;
            elf?.Delete();
            elvenBoots.Delete();
            boots.Delete();
        }
    }
}
