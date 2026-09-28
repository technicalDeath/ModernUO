using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Server;
using Server.Engines.CharacterCreation;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Misc;

[Collection("Sequential UOContent Tests")]
public class UorCharacterCreationSkillTests
{
    [Theory]
    [InlineData(SkillName.Necromancy)]
    [InlineData(SkillName.Focus)]
    [InlineData(SkillName.Chivalry)]
    [InlineData(SkillName.Bushido)]
    [InlineData(SkillName.Ninjitsu)]
    [InlineData(SkillName.Spellweaving)]
    [InlineData(SkillName.Mysticism)]
    [InlineData(SkillName.Imbuing)]
    [InlineData(SkillName.Throwing)]
    public void UorCreationDoesNotGrantLaterEraSkillPoints(SkillName laterSkill)
    {
        var previous = Core.Expansion;
        var validate = typeof(CharacterCreation).GetMethod(
            "ValidateSkills", BindingFlags.Static | BindingFlags.NonPublic);
        var skills = new[] { (laterSkill, (byte)50), (SkillName.Anatomy, (byte)50) };

        try
        {
            Core.Expansion = Expansion.UOR;
            var valid = (bool)validate!.Invoke(null, [0, skills])!;

            Assert.True(!valid || skills[0].Item2 == 0);
            Assert.Equal((byte)50, skills[1].Item2);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Theory]
    [InlineData(SkillName.Necromancy)]
    [InlineData(SkillName.Focus)]
    [InlineData(SkillName.Chivalry)]
    [InlineData(SkillName.Bushido)]
    [InlineData(SkillName.Ninjitsu)]
    [InlineData(SkillName.Spellweaving)]
    [InlineData(SkillName.Mysticism)]
    [InlineData(SkillName.Imbuing)]
    [InlineData(SkillName.Throwing)]
    public void UorTrainersDoNotTeachLaterEraSkills(SkillName laterSkill)
    {
        var previous = Core.Expansion;
        var trainer = (Samurai)RuntimeHelpers.GetUninitializedObject(typeof(Samurai));

        try
        {
            Core.Expansion = Expansion.UOR;
            Assert.False(trainer.CheckTeach(laterSkill, null));
            Assert.True(trainer.CheckTeach(SkillName.Anatomy, null));
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    [Theory]
    [InlineData(SkillName.Anatomy, 105.0)]
    [InlineData(SkillName.Necromancy, 100.0)]
    public void UorPowerScrollCannotRaiseTheConfiguredEraCapOrLaterSkill(SkillName skillName, double cap)
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var scroll = new PowerScroll(skillName, cap);
        try
        {
            Core.Expansion = Expansion.UOR;
            player.DefaultMobileInit();
            player.Player = true;
            player.AddToBackpack(scroll);

            Assert.False(scroll.CanUse(player));
            scroll.Use(player);
            Assert.Equal(100.0, player.Skills[skillName].Cap);
            Assert.False(scroll.Deleted);
        }
        finally
        {
            Core.Expansion = previous;
            player.Delete();
            scroll.Delete();
        }
    }

    [Fact]
    public void UorPlayerCannotEquipThrowingWeapon()
    {
        var previous = Core.Expansion;
        var player = new PlayerMobile(World.NewMobile);
        var weapon = new Boomerang();
        try
        {
            Core.Expansion = Expansion.UOR;
            player.DefaultMobileInit();
            player.Player = true;

            Assert.False(weapon.CanEquip(player));
            Assert.False(player.EquipItem(weapon));
        }
        finally
        {
            Core.Expansion = previous;
            weapon.Delete();
            player.Delete();
        }
    }
}
