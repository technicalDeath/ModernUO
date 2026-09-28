using Server;
using Server.Misc;
using Xunit;

namespace UOContent.Tests.Skills;

[Collection("Sequential UOContent Tests")]
public class UorUnavailableSkillTests
{
    [Theory]
    [InlineData(SkillName.Necromancy, Expansion.AOS)]
    [InlineData(SkillName.Focus, Expansion.AOS)]
    [InlineData(SkillName.Chivalry, Expansion.AOS)]
    [InlineData(SkillName.Bushido, Expansion.SE)]
    [InlineData(SkillName.Ninjitsu, Expansion.SE)]
    [InlineData(SkillName.Spellweaving, Expansion.ML)]
    [InlineData(SkillName.Mysticism, Expansion.SA)]
    [InlineData(SkillName.Imbuing, Expansion.SA)]
    [InlineData(SkillName.Throwing, Expansion.SA)]
    public void UorGenericSkillEntryPointsRejectLaterEraSkills(
        SkillName skillName,
        Expansion firstAvailableExpansion
    )
    {
        var previous = Core.Expansion;
        var mobile = new Mobile();
        var skill = mobile.Skills[skillName];

        try
        {
            Core.Expansion = Expansion.UOR;

            Assert.False(SkillCheck.IsSkillAvailable(skillName));
            Assert.False(SkillCheck.Mobile_SkillCheckLocation(mobile, skillName, 0.0, 100.0));
            Assert.False(SkillCheck.Mobile_SkillCheckDirectLocation(mobile, skillName, 1.0));
            Assert.False(SkillCheck.Mobile_SkillCheckTarget(mobile, skillName, null, 0.0, 100.0));
            Assert.False(SkillCheck.Mobile_SkillCheckDirectTarget(mobile, skillName, null, 1.0));

            var baseBefore = skill.BaseFixedPoint;
            SkillCheck.Gain(mobile, skill);
            Assert.Equal(baseBefore, skill.BaseFixedPoint);

            Core.Expansion = firstAvailableExpansion;
            Assert.True(SkillCheck.IsSkillAvailable(skillName));
        }
        finally
        {
            Core.Expansion = previous;
            mobile.Delete();
        }
    }
}
